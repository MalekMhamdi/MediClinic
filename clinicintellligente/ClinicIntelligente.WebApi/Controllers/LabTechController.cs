// PROJECT: ClinicIntelligente.WebApi
// PATH: Controllers/LabTechController.cs

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClinicIntelligente.Core.Application.DTOs.LabTech;
using ClinicIntelligente.Core.Application.Interfaces.Services;

namespace ClinicIntelligente.WebApi.Controllers;

[ApiController]
[Route("api/v1/labtech")]
[Authorize(Roles = "LabTechnicien")]
[Produces("application/json")]
public class LabTechController : ControllerBase
{
    private readonly ILabTechService _service;

    public LabTechController(ILabTechService service) => _service = service;

    private int GetTechId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    // ── LAB ORDERS ───────────────────────────────────────────────────

    /// <summary>All pending and in-progress orders</summary>
    [HttpGet("orders/pending")]
    [ProducesResponseType(typeof(IReadOnlyList<PendingLabOrderDto>), 200)]
    public async Task<IActionResult> GetPendingOrders()
        => Ok(await _service.GetPendingOrdersAsync());

    /// <summary>All orders with optional status filter (Pending / Done / InProgress)</summary>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(IReadOnlyList<PendingLabOrderDto>), 200)]
    public async Task<IActionResult> GetAllOrders([FromQuery] string? status = null)
        => Ok(await _service.GetAllOrdersAsync(status));

    /// <summary>
    /// Get full details of a specific lab order:
    /// patient info, doctor info, analysis type, and result if already submitted.
    /// </summary>
    [HttpGet("orders/{id:int}")]
    [ProducesResponseType(typeof(LabOrderDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetOrderDetail(int id)
        => Ok(await _service.GetOrderDetailAsync(id));

    // ── LAB RESULTS ──────────────────────────────────────────────────

    /// <summary>
    /// Get all results I have submitted (filtered to my technicien ID).
    /// </summary>
    [HttpGet("results/mine")]
    [ProducesResponseType(typeof(IReadOnlyList<LabResultSummaryDto>), 200)]
    public async Task<IActionResult> GetMyResults()
        => Ok(await _service.GetMyResultsAsync(GetTechId()));

    /// <summary>
    /// Submit a new lab result for a pending order.
    /// - Sets lab order status to Done automatically.
    /// - PDF path can be a file path or a URL to the uploaded file.
    ///
    /// Example body:
    /// {
    ///   "labOrderId": 1,
    ///   "resultPdfPath": "/uploads/results/ecg-patient1.pdf",
    ///   "medicalComment": "Normal sinus rhythm. No abnormality detected."
    /// }
    /// </summary>
    [HttpPost("results")]
    [ProducesResponseType(typeof(LabResultSummaryDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SubmitResult([FromBody] SubmitLabResultDto dto)
    {
        var created = await _service.SubmitResultAsync(GetTechId(), dto);
        return StatusCode(201, created);
    }

    /// <summary>
    /// Update the comment or PDF path on a result you previously submitted.
    /// Only the technician who submitted the result can update it.
    ///
    /// Example body:
    /// {
    ///   "comment": "Revised comment after doctor review.",
    ///   "resultPdfPath": "/uploads/results/ecg-patient1-v2.pdf"
    /// }
    /// </summary>
    [HttpPatch("results/{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateResult(int id, [FromBody] UpdateLabResultDto dto)
    {
        await _service.UpdateResultAsync(id, GetTechId(), dto);
        return NoContent();
    }
}