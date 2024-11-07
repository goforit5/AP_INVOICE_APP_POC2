using Microsoft.Azure.Cosmos;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;  // For List<T>
using System.Linq;                // For FirstOrDefault

public class InvoiceHandler
{
    private readonly Container _cosmosContainer;
    private readonly Container _logContainer;
    private readonly Container _errorContainer;
    private readonly BlobServiceClient _blobClient;

    public InvoiceHandler(CosmosClient cosmosClient, BlobServiceClient blobClient)
    {
        _cosmosContainer = cosmosClient.GetDatabase("ap-invoice-db").GetContainer("invoices");
        _logContainer = cosmosClient.GetDatabase("ap-invoice-db").GetContainer("logs");
        _errorContainer = cosmosClient.GetDatabase("ap-invoice-db").GetContainer("errors");
        _blobClient = blobClient;
    }

    public async Task<InvoiceDocument> ProcessNewInvoice(IFormFile file)
    {
        var cosmosDocumentId = $"inv_{Guid.NewGuid()}";  // Unique ID for Cosmos DB document
        var blobId = $"blob_{Guid.NewGuid()}";           // Unique ID for Blob Storage file
        var uploadedAt = DateTime.UtcNow;                // Capture the current timestamp

        try
        {
            // Step 1: Save the file to Blob Storage with a unique blobId
            await LogAsync(blobId, cosmosDocumentId, "Info", "Starting to upload file to Blob Storage.");
            var blobInfo = await SaveToBlob(file, blobId, cosmosDocumentId);
            await LogAsync(blobId, cosmosDocumentId, "Info", $"File uploaded to Blob Storage with Blob ID: {blobId}");

            // Step 2: Create the Metadata object
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
            var response = await _cosmosContainer.CreateItemAsync(newInvoice);
            await LogAsync(cosmosDocumentId, cosmosDocumentId, "Info", $"Invoice document created in Cosmos DB. Cosmos status code: {response.StatusCode}");

            return newInvoice;
        }
        catch (CosmosException cosmosEx)
        {
            // Cosmos-specific error logging with Cosmos diagnostics
            await LogErrorAsync(cosmosDocumentId, cosmosDocumentId, $"Cosmos DB error: {cosmosEx.Message}. Cosmos Diagnostics: {cosmosEx.Diagnostics}", cosmosEx.StackTrace);
            throw;
        }
        catch (Exception ex)
        {
            // General error logging
            await LogErrorAsync(cosmosDocumentId, cosmosDocumentId, $"General error: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    private async Task<BlobInfo> SaveToBlob(IFormFile file, string blobId, string correlationId)
    {
        var containerClient = _blobClient.GetBlobContainerClient("invoices");
        var blobClient = containerClient.GetBlobClient($"{blobId}.pdf");

        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, overwrite: true);

        return new BlobInfo
        {
            BlobUrl = blobClient.Uri.ToString(),
            SizeInBytes = file.Length,
            BlobId = blobId    // Store the blob ID for reference
        };
    }

    public async Task UpdateProcessingStepAsync(string invoiceId, string stepName, string status, string details = null, string errorMessage = null, string errorCode = null)
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