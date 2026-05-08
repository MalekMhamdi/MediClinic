// PROJECT: ClinicIntelligente.Core.Application
// PATH: Interfaces/Services/ILabTechService.cs

using ClinicIntelligente.Core.Application.DTOs.LabTech;

namespace ClinicIntelligente.Core.Application.Interfaces.Services;

public interface ILabTechService
{
    /// <summary>All pending and in-progress orders assigned to the lab</summary>
    Task<IReadOnlyList<PendingLabOrderDto>> GetPendingOrdersAsync();

    /// <summary>All orders with optional status filter</summary>
    Task<IReadOnlyList<PendingLabOrderDto>> GetAllOrdersAsync(string? status = null);

    /// <summary>Detail of a single lab order including result if submitted</summary>
    Task<LabOrderDetailDto> GetOrderDetailAsync(int labOrderId);

    /// <summary>All results submitted by this technician</summary>
    Task<IReadOnlyList<LabResultSummaryDto>> GetMyResultsAsync(int technicienId);

    /// <summary>Submit a result for a lab order — sets order status to Done</summary>
    Task<LabResultSummaryDto> SubmitResultAsync(int technicienId, SubmitLabResultDto dto);

    /// <summary>Update comment or attach PDF to an existing result</summary>
    Task UpdateResultAsync(int labResultId, int technicienId, UpdateLabResultDto dto);
}