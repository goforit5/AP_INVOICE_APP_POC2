using System;

public class ErrorEntry
{
    public string Id { get; } = $"error_{Guid.NewGuid()}";
    public string FileId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string ErrorMessage { get; set; }
    public string StackTrace { get; set; }  // Optional, for debugging
}
