using System;

public class DocumentMetadata
{
    public string FileName { get; set; }
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    public DocumentStatus Status { get; set; } = DocumentStatus.New;
}

public enum DocumentStatus
{
    New,
    Analyzing,
    Analyzed,
    Matching,
    Matched,
    Review,
    Approved,
    Rejected,
    Syncing,
    Synced
}
