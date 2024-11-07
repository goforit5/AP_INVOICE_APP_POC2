// ProcessingStatus.cs
public class ProcessingStatus
{
    public string CurrentStatus { get; set; }  // "Pending", "InProgress", "Completed", "Failed"
    public string CurrentStep { get; set; }    // The name of the current processing step
    public DateTime LastUpdated { get; set; }
}