using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;

namespace ApexBuild.Application.Features.Tasks.Queries.GetTaskUpdates;

public class GetTaskUpdatesQueryHandler : IRequestHandler<GetTaskUpdatesQuery, GetTaskUpdatesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetTaskUpdatesQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetTaskUpdatesResponse> Handle(GetTaskUpdatesQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
            throw new UnauthorizedException("User must be authenticated to view task updates");

        var task = await _unitOfWork.Tasks.GetByIdAsync(request.TaskId, cancellationToken);
        if (task == null || task.IsDeleted)
            throw new NotFoundException("Task", request.TaskId);

        var updates = await _unitOfWork.TaskUpdates.GetUpdatesByTaskAsync(request.TaskId, cancellationToken);

        var dtos = updates.Select(u => new TaskUpdateItemDto
        {
            Id = u.Id,
            TaskId = u.TaskId,
            SubmittedByUserId = u.SubmittedByUserId,
            SubmittedByUserName = u.SubmittedByUser != null
                ? $"{u.SubmittedByUser.FirstName} {u.SubmittedByUser.LastName}"
                : string.Empty,
            SubmittedAt = u.SubmittedAt,
            Description = u.Description,
            Summary = u.Summary,
            Status = (int)u.Status,
            ProgressPercentage = u.ProgressPercentage,
            MediaUrls = u.MediaUrls ?? new List<string>(),
            MediaTypes = u.MediaTypes ?? new List<string>(),

            ContractorAdminApproved = u.ContractorAdminApproved,
            ContractorAdminFeedback = u.ContractorAdminFeedback,
            ReviewedByContractorAdminName = u.ReviewedByContractorAdmin != null
                ? $"{u.ReviewedByContractorAdmin.FirstName} {u.ReviewedByContractorAdmin.LastName}"
                : null,
            ContractorAdminReviewedAt = u.ContractorAdminReviewedAt,

            SupervisorApproved = u.SupervisorApproved,
            SupervisorFeedback = u.SupervisorFeedback,
            ReviewedBySupervisorName = u.ReviewedBySupervisor != null
                ? $"{u.ReviewedBySupervisor.FirstName} {u.ReviewedBySupervisor.LastName}"
                : null,
            SupervisorReviewedAt = u.SupervisorReviewedAt,

            AdminApproved = u.AdminApproved,
            AdminFeedback = u.AdminFeedback,
            ReviewedByAdminName = u.ReviewedByAdmin != null
                ? $"{u.ReviewedByAdmin.FirstName} {u.ReviewedByAdmin.LastName}"
                : null,
            AdminReviewedAt = u.AdminReviewedAt,
        }).ToList();

        return new GetTaskUpdatesResponse
        {
            Updates = dtos,
            TotalCount = dtos.Count,
        };
    }
}
