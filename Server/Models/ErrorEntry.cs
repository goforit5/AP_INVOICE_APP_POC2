// ErrorEntry.cs
using System;

public class ErrorEntry
{
    public string id { get; set; }  // Cosmos DB required id
    public string CorrelationId { get; set; }  // Links to original invoice
    public string FileId { get; set; }
    public string ErrorMessage { get; set; }
    public string StackTrace { get; set; }
    public DateTime Timestamp { get; set; }

    public ErrorEntry(string correlationId)
    {
        id = $"err_{Guid.NewGuid()}";  // Unique error document id
        CorrelationId = correlationId;  // Original invoice id for correlation
        Timestamp = DateTime.UtcNow;
    }
}