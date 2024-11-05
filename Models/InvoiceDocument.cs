public class InvoiceDocument
{
    public string Id { get; } = $"inv_{Guid.NewGuid()}";
    public DocumentMetadata Metadata { get; set; } = new DocumentMetadata();
    public BlobInfo BlobInfo { get; set; }
    public MatchInfo? Matches { get; set; }
}
