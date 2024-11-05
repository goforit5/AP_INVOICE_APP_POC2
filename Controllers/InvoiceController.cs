using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class InvoiceController : ControllerBase
{
    private readonly InvoiceHandler _invoiceHandler;

    public InvoiceController(InvoiceHandler invoiceHandler)
    {
        _invoiceHandler = invoiceHandler;
    }

    [HttpPost]
    public async Task<IActionResult> UploadInvoice(IFormFile file)
    {
        if (file == null)
            return BadRequest("No file uploaded.");

        var result = await _invoiceHandler.ProcessNewInvoice(file);
        return Ok(result);
    }
}
