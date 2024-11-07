using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

[ApiController]
[Route("api/[controller]")]
public class InvoiceController : ControllerBase
{
    private readonly InvoiceHandler _invoiceHandler;

    public InvoiceController(InvoiceHandler invoiceHandler)
    {
        _invoiceHandler = invoiceHandler;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadInvoice(IFormFile file)
    {
        if (file == null)
            return BadRequest("No file uploaded.");

        try
        {
            var result = await _invoiceHandler.ProcessNewInvoice(file);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeInvoice([FromQuery] string invoiceId)
    {
        if (string.IsNullOrWhiteSpace(invoiceId))
            return BadRequest("Invoice ID is required.");

        try
        {
            // Here, you would call a method to start the document analysis
            // For example:
            await _invoiceHandler.UpdateProcessingStepAsync(invoiceId, "DocumentAnalyzed", "InProgress", "Document analysis started.");

            // Simulate document analysis
            // ...

            // After analysis is complete
            await _invoiceHandler.UpdateProcessingStepAsync(invoiceId, "DocumentAnalyzed", "Completed", "Document analysis completed successfully.");

            return Ok("Document analysis initiated.");
        }
        catch (Exception ex)
        {
            // Update the processing step to Failed
            await _invoiceHandler.UpdateProcessingStepAsync(invoiceId, "DocumentAnalyzed", "Failed", "Document analysis failed.", ex.Message);

            // Log the exception or handle it as needed
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}