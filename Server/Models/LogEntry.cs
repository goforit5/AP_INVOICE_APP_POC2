// LogEntry.cs
using System;

public class LogEntry
{
    public string id { get; set; }  // Cosmos DB required id
    public string CorrelationId { get; set; }  // Links to original invoice
    public string FileId { get; set; }
    public string Level { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }

    public LogEntry(string correlationId)
    {
        id = $"log_{Guid.NewGuid()}";  // Unique log document id
        CorrelationId = correlationId;  // Original invoice id for correlation
        Timestamp = DateTime.UtcNow;
    }
}