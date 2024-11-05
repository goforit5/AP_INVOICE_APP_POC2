using Azure.Cosmos;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

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
        var fileId = $"inv_{Guid.NewGuid()}";
        var newInvoice = new InvoiceDocument
        {
            Metadata = new DocumentMetadata
            {
                FileName = file.FileName,
            }
        };

        try
        {
            var blobInfo = await SaveToBlob(file, fileId);
            newInvoice.BlobInfo = blobInfo;

            await _cosmosContainer.CreateItemAsync(newInvoice);
            await LogAsync(fileId, "Info", "Invoice document created in Cosmos DB.");

            return newInvoice;
        }
        catch (Exception ex)
        {
            await LogErrorAsync(fileId, ex.Message, ex.StackTrace);
            throw;
        }
    }

    private async Task<BlobInfo> SaveToBlob(IFormFile file, string fileId)
    {
        var containerClient = _blobClient.GetBlobContainerClient("invoices");
        var blobClient = containerClient.GetBlobClient($"{fileId}.pdf");

        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, overwrite: true);

        await LogAsync(fileId, "Info", $"File uploaded to Blob Storage with ID {fileId}");

        return new BlobInfo
        {
            BlobUrl = blobClient.Uri.ToString(),
            SizeInBytes = file.Length
        };
    }

    private async Task LogAsync(string fileId, string level, string message)
    {
        var logEntry = new LogEntry
        {
            FileId = fileId,
            Level = level,
            Message = message
        };
        await _logContainer.CreateItemAsync(logEntry);
    }

    private async Task LogErrorAsync(string fileId, string errorMessage, string stackTrace)
    {
        var errorEntry = new ErrorEntry
        {
            FileId = fileId,
            ErrorMessage = errorMessage,
            StackTrace = stackTrace
        };
        await _errorContainer.CreateItemAsync(errorEntry);
    }
}
