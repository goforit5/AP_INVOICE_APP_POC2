using Microsoft.Azure.Cosmos;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;  // For List<T>
using System.Linq;                // For FirstOrDefault

public sealed class InvoiceHandler
{
    private readonly Container _cosmosContainer;
    private readonly Container _logContainer;
    private readonly Container _errorContainer;
    private readonly BlobServiceClient _blobClient;
    private readonly ILogger<InvoiceHandler> _logger;

    public InvoiceHandler(CosmosClient cosmosClient, BlobServiceClient blobClient, ILogger<InvoiceHandler> logger)
    {
        _logger = logger;
        _logger.LogInformation("Initializing InvoiceHandler with Cosmos endpoint: {Endpoint}", 
            cosmosClient.Endpoint);

        try
        {
            _cosmosContainer = cosmosClient.GetDatabase("ap-invoice-db").GetContainer("invoices");
            _logContainer = cosmosClient.GetDatabase("ap-invoice-db").GetContainer("logs");
            _errorContainer = cosmosClient.GetDatabase("ap-invoice-db").GetContainer("errors");
            _blobClient = blobClient;

            // Test Cosmos DB connectivity
            var response = _cosmosContainer.ReadContainerAsync().GetAwaiter().GetResult();
            _logger.LogInformation("Successfully connected to Cosmos container");
        }
        catch (CosmosException ex)
        {
            _logger.LogError("Cosmos DB Connection Error: Status={Status}, SubStatus={SubStatus}, Message={Message}", 
                ex.StatusCode, ex.SubStatusCode, ex.Message);
            if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogError("Possible IP restriction issue. Your client IP needs to be added to Cosmos DB firewall.");
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error initializing Cosmos DB connection");
            throw;
        }
    }

    public async Task<InvoiceDocument> ProcessNewInvoice(IFormFile file)
    {
        _logger.LogInformation("Starting ProcessNewInvoice");
        var cosmosDocumentId = $"inv_{Guid.NewGuid()}";  // Unique ID for Cosmos DB document
        var blobId = $"blob_{Guid.NewGuid()}";           // Unique ID for Blob Storage file
        var uploadedAt = DateTime.UtcNow;                // Capture the current timestamp
        _logger.LogInformation("Generated IDs - Cosmos: {CosmosId}, Blob: {BlobId}", cosmosDocumentId, blobId);

        try
        {
            // Step 1: Save the file to Blob Storage with a unique blobId
            _logger.LogInformation("Attempting to save to blob storage");
            await LogAsync(blobId, cosmosDocumentId, "Info", "Starting to upload file to Blob Storage.");
            var blobInfo = await SaveToBlob(file, blobId, cosmosDocumentId);
            await LogAsync(blobId, cosmosDocumentId, "Info", $"File uploaded to Blob Storage with Blob ID: {blobId}");
            _logger.LogInformation("Successfully saved to blob storage");

            // Step 2: Create the Metadata object
            _logger.LogInformation("Creating metadata");
            var metadata = new Metadata
            {
                FileName = file.FileName,
                UploadedAt = uploadedAt,
                BlobInfo = blobInfo
            };

            // Step 3: Initialize ProcessingStatus and ProcessingSteps
            var processingStatus = new ProcessingStatus
            {
                CurrentStatus = "InProgress",
                CurrentStep = "DocumentUploaded",
                LastUpdated = DateTime.UtcNow
            };

            var processingSteps = new List<ProcessingStep>
            {
                new ProcessingStep
                {
                    StepName = "DocumentUploaded",
                    Status = "Completed",
                    StartedAt = uploadedAt,
                    CompletedAt = DateTime.UtcNow,
                    Details = "Document uploaded to blob storage."
                }
            };

            // Step 4: Create a new InvoiceDocument with the cosmosDocumentId
            var newInvoice = new InvoiceDocument
            {
                id = cosmosDocumentId,   // Ensure 'id' is set for Cosmos DB
                Metadata = metadata,
                ProcessingStatus = processingStatus,
                ProcessingSteps = processingSteps
                // Initialize other properties like DocumentAnalysis, ValidatedData, etc., if needed
            };

            // Log the JSON representation of the document to ensure 'id' and structure are correct
            string jsonRepresentation = JsonConvert.SerializeObject(newInvoice);
            await LogAsync(cosmosDocumentId, cosmosDocumentId, "Debug", $"Document JSON before inserting into Cosmos DB: {jsonRepresentation}");

            // Validate the document structure explicitly
            if (string.IsNullOrWhiteSpace(newInvoice.id))
            {
                throw new InvalidOperationException("Document 'id' is missing or invalid.");
            }

            // Step 5: Store the new document in Cosmos DB
            _logger.LogInformation("Attempting to save to Cosmos DB");
            var response = await _cosmosContainer.CreateItemAsync(newInvoice);
            await LogAsync(cosmosDocumentId, cosmosDocumentId, "Info", $"Invoice document created in Cosmos DB. Cosmos status code: {response.StatusCode}");
            _logger.LogInformation("Successfully saved to Cosmos DB");

            return newInvoice;
        }
        catch (CosmosException cosmosEx)
        {
            _logger.LogError(cosmosEx, "Cosmos DB Error: {Message}", cosmosEx.Message);
            _logger.LogError("Cosmos Diagnostics: {Diagnostics}", cosmosEx.Diagnostics);
            // Cosmos-specific error logging with Cosmos diagnostics
            await LogErrorAsync(cosmosDocumentId, cosmosDocumentId, $"Cosmos DB error: {cosmosEx.Message}. Cosmos Diagnostics: {cosmosEx.Diagnostics}", cosmosEx.StackTrace);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "General Error in ProcessNewInvoice: {Message}", ex.Message);
            // General error logging
            await LogErrorAsync(cosmosDocumentId, cosmosDocumentId, $"General error: {ex.Message}", ex.StackTrace ?? "No stack trace available");
            throw;
        }
    }

    private async Task<BlobInfo> SaveToBlob(IFormFile file, string blobId, string correlationId)
    {
        _logger.LogInformation("Starting blob upload for file {FileName} with blobId {BlobId}", file.FileName, blobId);
        var containerClient = _blobClient.GetBlobContainerClient("invoices");
        var blobClient = containerClient.GetBlobClient($"{blobId}.pdf");

        try 
        {
            using var stream = file.OpenReadStream();
            _logger.LogDebug("Uploading {SizeInBytes} bytes to blob storage", file.Length);
            await blobClient.UploadAsync(stream, overwrite: true);
            _logger.LogInformation("Successfully uploaded blob {BlobId}", blobId);

            return new BlobInfo
            {
                BlobUrl = blobClient.Uri.ToString(),
                SizeInBytes = file.Length,
                BlobId = blobId    // Store the blob ID for reference
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload blob {BlobId}: {Message}", blobId, ex.Message);
            throw;
        }
    }

    public async Task UpdateProcessingStepAsync(string invoiceId, string stepName, string status, string? details = null, string? errorMessage = null, string? errorCode = null)
    {
        _logger.LogInformation("Updating processing step for invoice {InvoiceId}: Step={StepName}, Status={Status}", 
            invoiceId, stepName, status);

        try
        {
            // Read the invoice document
            var response = await _cosmosContainer.ReadItemAsync<InvoiceDocument>(invoiceId, new PartitionKey(invoiceId));
            var invoiceDocument = response.Resource;

            // Find or create the processing step
            var processingStep = invoiceDocument.ProcessingSteps.FirstOrDefault(ps => ps.StepName == stepName);
            if (processingStep == null)
            {
                processingStep = new ProcessingStep
                {
                    StepName = stepName,
                    Status = status,
                    StartedAt = DateTime.UtcNow,
                    Details = details
                };
                invoiceDocument.ProcessingSteps.Add(processingStep);
            }
            else
            {
                processingStep.Status = status;
                if (status == "Completed" || status == "Failed")
                {
                    processingStep.CompletedAt = DateTime.UtcNow;
                }
                if (!string.IsNullOrEmpty(details))
                {
                    processingStep.Details = details;
                }
                if (status == "Failed" && !string.IsNullOrEmpty(errorMessage))
                {
                    processingStep.ErrorInfo = new ErrorInfo
                    {
                        ErrorMessage = errorMessage,
                        ErrorCode = errorCode
                    };
                }
            }

            // Update ProcessingStatus
            invoiceDocument.ProcessingStatus.CurrentStatus = status == "Failed" ? "Failed" : "InProgress";
            invoiceDocument.ProcessingStatus.CurrentStep = stepName;
            invoiceDocument.ProcessingStatus.LastUpdated = DateTime.UtcNow;

            // Replace the document in Cosmos DB
            await _cosmosContainer.ReplaceItemAsync(invoiceDocument, invoiceId, new PartitionKey(invoiceId));
            _logger.LogInformation("Successfully updated processing step for invoice {InvoiceId}", invoiceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating processing step for invoice {InvoiceId}: {Message}", invoiceId, ex.Message);
            throw;
        }
    }

    private async Task LogAsync(string fileId, string correlationId, string level, string message)
    {
        var logEntry = new LogEntry(correlationId)
        {
            FileId = fileId,
            Level = level,
            Message = message
        };
        await _logContainer.CreateItemAsync(logEntry);
    }

    private async Task LogErrorAsync(string fileId, string correlationId, string errorMessage, string stackTrace)
    {
        var errorEntry = new ErrorEntry(correlationId)
        {
            FileId = fileId,
            ErrorMessage = errorMessage,
            StackTrace = stackTrace ?? "No stack trace available"
        };
        await _errorContainer.CreateItemAsync(errorEntry);
    }
}
