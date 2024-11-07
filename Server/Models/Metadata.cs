// Metadata.cs
public class Metadata
{
    public required string FileName { get; set; }
    public DateTime UploadedAt { get; set; }
    public required BlobInfo BlobInfo { get; set; }
}

public class BlobInfo
{
    public required string BlobUrl { get; set; }
    public long SizeInBytes { get; set; }
    public required string BlobId { get; set; }  // Blob ID for reference
}
