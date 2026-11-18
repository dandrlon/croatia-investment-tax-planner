using Microsoft.AspNetCore.Mvc;
using TaxPlanner.Api.Services;

namespace TaxPlanner.Api.Controllers;

[ApiController]
[Route("api/imports")]
public class ImportsController(ImportService imports) : ControllerBase
{
    private const long MaxFileBytes = 10 * 1024 * 1024;

    /// <summary>Imports a Trading 212 transaction history CSV export.</summary>
    /// <remarks>Rows that were already imported are skipped, so the same or an overlapping export can be uploaded again.</remarks>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileBytes + 1024 * 1024)]
    [RequestSizeLimit(MaxFileBytes + 1024 * 1024)]
    [ProducesResponseType<ImportResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportResult>> Import(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Problem(statusCode: 400, title: "No file uploaded",
                detail: "Send a Trading 212 CSV export in the multipart form field 'file'.");

        if (file.Length > MaxFileBytes)
            return Problem(statusCode: 400, title: "File too large", detail: "The file must be 10 MB or smaller.");

        await using var stream = file.OpenReadStream();
        var outcome = await imports.ImportAsync(stream, file.FileName, ct);
        if (outcome.Result is null)
            return Problem(statusCode: 400, title: "The file could not be imported", detail: outcome.RejectionReason);

        return Ok(outcome.Result);
    }
}
