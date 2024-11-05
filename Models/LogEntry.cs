using System;

public class LogEntry
{
    public string Id { get; } = $"log_{Guid.NewGuid()}";
    public string FileId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Level { get; set; }  // e.g., "Info", "Warning", "Error"
    public string Message { get; set; }
}
