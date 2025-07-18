using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Users.Commands.InviteUser
{
    public class InviteUserCommandHandler : IRequestHandler<InviteUserCommand, InviteUserResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ICurrentUserService _currentUserService;

        public InviteUserCommandHandler(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _currentUserService = currentUserService;
        }

        public async Task<InviteUserResponse> Handle(InviteUserCommand request, CancellationToken cancellationToken)
        {
            // Validate inviter
            var inviter = await _unitOfWork.Users.GetWithRolesAsync(request.InvitedByUserId, cancellationToken);
            if (inviter == null)
            {
                throw new UnauthorizedAccessException("Inviter not found");
            }

            // Check for existing pending invitations
            var existingInvitations = await _unitOfWork.Invitations.GetPendingInvitationsByEmailAsync(
                request.Email,
                cancellationToken);

            var duplicateInvite = existingInvitations.FirstOrDefault(i =>
                i.RoleId == request.RoleId &&
                i.ProjectId == request.ProjectId);

            if (duplicateInvite != null)
            {
                throw new InvalidOperationException("An active invitation already exists for this email and role");
            }

            // Get role details
            var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId, cancellationToken);
            if (role == null)
            {
                throw new InvalidOperationException("Invalid role");
            }

            // Create invitation
            var token = Guid.NewGuid().ToString() + Guid.NewGuid().ToString();
            var invitation = new Invitation
            {
                Email = request.Email.ToLower(),
                InvitedByUserId = request.InvitedByUserId,
                RoleId = request.RoleId,
                ProjectId = request.ProjectId,
                OrganizationId = request.OrganizationId,
                DepartmentId = request.DepartmentId,
                Token = token,
                Status = InvitationStatus.Pending,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                Position = request.Position,
                Message = request.Message,
                MetaData = new Dictionary<string, object>
            {
                { "InviterName", inviter.FullName },
                { "RoleName", role.Name }
            }
            };

            await _unitOfWork.Invitations.AddAsync(invitation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Get project name if applicable
            string? projectName = null;
            if (request.ProjectId.HasValue)
            {
                var project = await _unitOfWork.Projects.GetByIdAsync(request.ProjectId.Value, cancellationToken);
                projectName = project?.Name;
            }

            // Send invitation email
            var invitationUrl = $"{_currentUserService.GetBaseUrl()}/auth/accept-invite?token={token}";
            await _emailService.SendInvitationAsync(
                request.Email,
                inviter.FullName,
                role.Name,
                projectName,
                invitationUrl,
                request.Message);

            return new InviteUserResponse
            {
                InvitationId = invitation.Id,
                Email = invitation.Email,
                InvitationUrl = invitationUrl,
                ExpiresAt = invitation.ExpiresAt
            };
        }
    }
}