// Metadata.cs
public class Metadata
{
    public string FileName { get; set; }
    public DateTime UploadedAt { get; set; }
    public BlobInfo BlobInfo { get; set; }
}

public class BlobInfo
{
    public string BlobUrl { get; set; }
    public long SizeInBytes { get; set; }
    public string BlobId { get; set; }  // Blob ID for reference
}