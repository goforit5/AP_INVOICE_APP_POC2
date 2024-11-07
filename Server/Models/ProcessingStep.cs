// ProcessingStep.cs
public class ProcessingStep
{
    public string StepName { get; set; }  // e.g., "DocumentUploaded", "DocumentAnalyzed"
    public string Status { get; set; }    // "Pending", "InProgress", "Completed", "Failed"
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Details { get; set; }
    public ErrorInfo ErrorInfo { get; set; }  // Optional, only used if Status is "Failed"
}