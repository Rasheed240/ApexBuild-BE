using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;
using MediatR;

namespace ApexBuild.Application.Features.Tasks.Queries.GetPendingUpdates;

public class GetPendingUpdatesQueryHandler : IRequestHandler<GetPendingUpdatesQuery, GetPendingUpdatesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetPendingUpdatesQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetPendingUpdatesResponse> Handle(GetPendingUpdatesQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
            throw new UnauthorizedException("User must be authenticated");

        // Determine the current user's highest role (org-scoped if organisationId provided)
        var allUserRoles = await _unitOfWork.UserRoles.FindAsync(
            ur => ur.UserId == currentUserId.Value && ur.IsActive,
            cancellationToken);

        if (request.OrganizationId.HasValue)
            allUserRoles = allUserRoles.Where(ur =>
                ur.OrganizationId == request.OrganizationId.Value ||
                ur.Project?.OrganizationId == request.OrganizationId.Value);

        var roleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ur in allUserRoles)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(ur.RoleId, cancellationToken);
            if (role != null) roleNames.Add(role.Name);
        }

        bool isAdminOrAbove = roleNames.Any(r =>
            r is "ProjectAdministrator" or "ProjectOwner" or "PlatformAdmin" or "SuperAdmin");
        bool isSupervisor     = roleNames.Contains("DepartmentSupervisor");
        bool isContractorAdmin = roleNames.Contains("ContractorAdmin");
        bool isFieldWorkerOnly = roleNames.Contains("FieldWorker")
            && !isAdminOrAbove && !isSupervisor && !isContractorAdmin;

        // ── FieldWorker: return only their own submitted updates ─────────────
        if (isFieldWorkerOnly)
        {
            var myUpdates = (await _unitOfWork.TaskUpdates.GetUpdatesByUserAsync(
                currentUserId.Value, cancellationToken)).ToList();

            if (request.OrganizationId.HasValue)
                myUpdates = myUpdates
                    .Where(u => u.Task?.Department?.Project?.OrganizationId == request.OrganizationId.Value)
                    .ToList();

            var total = myUpdates.Count;
            var paged = myUpdates
                .OrderByDescending(u => u.SubmittedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new GetPendingUpdatesResponse
            {
                Items = paged.Select(u => MapToDto(u, isOwnUpdate: true)).ToList(),
                TotalCount = total,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
            };
        }

        // ── Reviewer roles: role-filtered pending updates ────────────────────
        var (items, totalCount) = await _unitOfWork.TaskUpdates.GetPendingForReviewAsync(
            request.OrganizationId, request.PageNumber, request.PageSize, cancellationToken);

        // Further narrow by what this reviewer can actually act on
        var filtered = items.Where(u =>
        {
            if (isAdminOrAbove && u.Status == UpdateStatus.UnderAdminReview) return true;
            if (isSupervisor  && u.Status == UpdateStatus.UnderSupervisorReview) return true;
            if (isContractorAdmin && u.Status == UpdateStatus.UnderContractorAdminReview) return true;
            return false;
        }).ToList();

        return new GetPendingUpdatesResponse
        {
            Items = filtered.Select(u => MapToDto(u, isOwnUpdate: false)).ToList(),
            TotalCount = filtered.Count,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
        };
    }

    private static readonly Dictionary<UpdateStatus, string> StatusLabels = new()
    {
        [UpdateStatus.Submitted]                  = "Submitted",
        [UpdateStatus.UnderContractorAdminReview] = "Awaiting Contractor Admin",
        [UpdateStatus.ContractorAdminApproved]    = "Contractor Admin Approved",
        [UpdateStatus.ContractorAdminRejected]    = "Contractor Admin Rejected",
        [UpdateStatus.UnderSupervisorReview]      = "Awaiting Supervisor",
        [UpdateStatus.SupervisorApproved]         = "Supervisor Approved",
        [UpdateStatus.SupervisorRejected]         = "Supervisor Rejected",
        [UpdateStatus.UnderAdminReview]           = "Awaiting Admin",
        [UpdateStatus.AdminApproved]              = "Fully Approved",
        [UpdateStatus.AdminRejected]              = "Admin Rejected",
    };

    private static PendingUpdateDto MapToDto(TaskUpdate u, bool isOwnUpdate) => new()
    {
        Id                      = u.Id,
        TaskId                  = u.TaskId,
        TaskTitle               = u.Task?.Title ?? string.Empty,
        TaskCode                = u.Task?.Code ?? string.Empty,
        ProjectName             = u.Task?.Department?.Project?.Name ?? string.Empty,
        DepartmentName          = u.Task?.Department?.Name,
        ContractorName          = u.Task?.Contractor?.CompanyName,
        SubmittedByName         = u.SubmittedByUser != null
                                    ? $"{u.SubmittedByUser.FirstName} {u.SubmittedByUser.LastName}"
                                    : string.Empty,
        SubmittedByUserId       = u.SubmittedByUserId,
        Description             = u.Description,
        Status                  = (int)u.Status,
        StatusLabel             = StatusLabels.TryGetValue(u.Status, out var lbl) ? lbl : u.Status.ToString(),
        ProgressPercentage      = u.ProgressPercentage,
        SubmittedAt             = u.SubmittedAt,
        MediaUrls               = u.MediaUrls ?? new List<string>(),
        MediaTypes              = u.MediaTypes ?? new List<string>(),
        ContractorAdminApproved = u.ContractorAdminApproved,
        ContractorAdminFeedback = u.ContractorAdminFeedback,
        SupervisorApproved      = u.SupervisorApproved,
        SupervisorFeedback      = u.SupervisorFeedback,
        AdminApproved           = u.AdminApproved,
        AdminFeedback           = u.AdminFeedback,
        IsOwnUpdate             = isOwnUpdate,
    };
}
