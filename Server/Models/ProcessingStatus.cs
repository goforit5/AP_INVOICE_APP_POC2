// ProcessingStatus.cs
public class ProcessingStatus
{
    public required string CurrentStatus { get; set; }  // "Pending", "InProgress", "Completed", "Failed"
    public required string CurrentStep { get; set; }    // The name of the current processing step
    public DateTime LastUpdated { get; set; }
}
