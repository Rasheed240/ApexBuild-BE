using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Enums;
using MediatR;

namespace ApexBuild.Application.Features.Tasks.Queries.GetPendingUpdates;

public class GetPendingUpdatesQueryHandler : IRequestHandler<GetPendingUpdatesQuery, GetPendingUpdatesResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPendingUpdatesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetPendingUpdatesResponse> Handle(GetPendingUpdatesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _unitOfWork.TaskUpdates.GetPendingForReviewAsync(
            request.OrganizationId, request.PageNumber, request.PageSize, cancellationToken);

        var statusLabels = new Dictionary<UpdateStatus, string>
        {
            [UpdateStatus.UnderContractorAdminReview] = "Awaiting Contractor Admin",
            [UpdateStatus.UnderSupervisorReview] = "Awaiting Supervisor",
            [UpdateStatus.UnderAdminReview] = "Awaiting Admin",
        };

        return new GetPendingUpdatesResponse
        {
            Items = items.Select(u => new PendingUpdateDto
            {
                Id = u.Id,
                TaskId = u.TaskId,
                TaskTitle = u.Task?.Title ?? string.Empty,
                TaskCode = u.Task?.Code ?? string.Empty,
                ProjectName = u.Task?.Department?.Project?.Name ?? string.Empty,
                DepartmentName = u.Task?.Department?.Name,
                ContractorName = u.Task?.Contractor?.CompanyName,
                SubmittedByName = u.SubmittedByUser != null
                    ? $"{u.SubmittedByUser.FirstName} {u.SubmittedByUser.LastName}"
                    : string.Empty,
                SubmittedByUserId = u.SubmittedByUserId,
                Description = u.Description,
                Status = (int)u.Status,
                StatusLabel = statusLabels.TryGetValue(u.Status, out var lbl) ? lbl : u.Status.ToString(),
                ProgressPercentage = u.ProgressPercentage,
                SubmittedAt = u.SubmittedAt,
                MediaUrls = u.MediaUrls ?? new List<string>(),
                MediaTypes = u.MediaTypes ?? new List<string>(),
                ContractorAdminApproved = u.ContractorAdminApproved,
                ContractorAdminFeedback = u.ContractorAdminFeedback,
                SupervisorApproved = u.SupervisorApproved,
                SupervisorFeedback = u.SupervisorFeedback,
            }).ToList(),
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
        };
    }
}
