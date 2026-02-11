using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;
using MediatR;

namespace ApexBuild.Application.Features.Users.Commands.AcceptInvite
{
    public class AcceptInviteCommandHandler : IRequestHandler<AcceptInviteCommand, AcceptInviteResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;

        public AcceptInviteCommandHandler(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtTokenService)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
        }

        public async Task<AcceptInviteResponse> Handle(AcceptInviteCommand request, CancellationToken cancellationToken)
        {
            // Get invitation
            var invitation = await _unitOfWork.Invitations.GetByTokenAsync(request.Token, cancellationToken);

            if (invitation == null)
            {
                throw new InvalidOperationException("Invalid invitation token");
            }

            if (invitation.Status != InvitationStatus.Pending)
            {
                throw new InvalidOperationException("Invitation has already been processed");
            }

            if (invitation.ExpiresAt < DateTime.UtcNow)
            {
                invitation.Status = InvitationStatus.Expired;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                throw new InvalidOperationException("Invitation has expired");
            }

            // Check if user exists
            var existingUser = await _unitOfWork.Users.GetByEmailAsync(invitation.Email, cancellationToken);
            User user;

            if (existingUser != null)
            {
                user = existingUser;
            }
            else
            {
                // Create new user with all provided fields
                user = new User
                {
                    Email = invitation.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    MiddleName = request.MiddleName,
                    PhoneNumber = request.PhoneNumber,
                    PasswordHash = _passwordHasher.HashPassword(request.Password),
                    EmailConfirmed = true,
                    EmailConfirmedAt = DateTime.UtcNow,
                    Status = UserStatus.Active,
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    Address = request.Address,
                    City = request.City,
                    State = request.State,
                    Country = request.Country,
                    Bio = request.Bio
                };

                await _unitOfWork.Users.AddAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Assign role - add to user's UserRoles collection so EF Core tracks it
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = invitation.RoleId,
                ProjectId = invitation.ProjectId,
                OrganizationId = invitation.OrganizationId,
                IsActive = true,
                ActivatedAt = DateTime.UtcNow
            };

            // Add UserRole to the User's collection so EF Core tracks it
            user.UserRoles.Add(userRole);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Create WorkInfo if project is specified
            if (invitation.ProjectId.HasValue)
            {
                var workInfo = new WorkInfo
                {
                    UserId = user.Id,
                    ProjectId = invitation.ProjectId.Value,
                    OrganizationId = invitation.OrganizationId,
                    DepartmentId = invitation.DepartmentId,
                    Position = invitation.Position ?? "Team Member",
                    StartDate = DateTime.UtcNow,
                    Status = ProjectUserStatus.Active,
                    IsActive = true
                };

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Add to ProjectUser
                var projectUser = new ProjectUser
                {
                    ProjectId = invitation.ProjectId.Value,
                    UserId = user.Id,
                    Status = ProjectUserStatus.Active,
                    JoinedAt = DateTime.UtcNow,
                    AddedBy = invitation.InvitedByUserId
                };

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Add user to organization if organization is specified
            if (invitation.OrganizationId.HasValue)
            {
                // Check if user is already a member of the organization
                var existingMembership = await _unitOfWork.OrganizationMembers.FindAsync(
                    om => om.UserId == user.Id && om.OrganizationId == invitation.OrganizationId.Value,
                    cancellationToken);

                if (!existingMembership.Any())
                {
                    // Create organization membership
                    var organizationMember = new OrganizationMember
                    {
                        OrganizationId = invitation.OrganizationId.Value,
                        UserId = user.Id,
                        Position = invitation.Position ?? "Team Member",
                        IsActive = true,
                        JoinedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.OrganizationMembers.AddAsync(organizationMember, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            // Update invitation
            invitation.Status = InvitationStatus.Accepted;
            invitation.AcceptedAt = DateTime.UtcNow;
            invitation.InvitedUserId = user.Id;

            await _unitOfWork.Invitations.UpdateAsync(invitation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Generate tokens
            var userWithRoles = await _unitOfWork.Users.GetWithRolesAsync(user.Id, cancellationToken);
            var accessToken = _jwtTokenService.GenerateAccessToken(userWithRoles!);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            user.LastLoginAt = DateTime.UtcNow;

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new AcceptInviteResponse
            {
                UserId = user.Id,
                Email = user.Email,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Message = "Invitation accepted successfully"
            };
        }
    }
}
