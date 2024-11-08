public class CosmosDbConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string InvoiceContainer { get; set; } = string.Empty;
    public string LogContainer { get; set; } = string.Empty;
    public string ErrorContainer { get; set; } = string.Empty;
}
