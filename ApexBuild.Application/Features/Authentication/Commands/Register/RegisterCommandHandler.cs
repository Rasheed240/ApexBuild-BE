using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;


namespace ApexBuild.Application.Features.Authentication.Commands.Register
{
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly ISubscriptionService _subscriptionService;

        public RegisterCommandHandler(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IEmailService emailService,
            ISubscriptionService subscriptionService)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _subscriptionService = subscriptionService;
        }

        public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            // Check if email exists
            if (await _unitOfWork.Users.EmailExistsAsync(request.Email, cancellationToken))
            {
                throw new BadRequestException("Email already exists");
            }

            // Create user with all provided fields
            var user = new User
            {
                Email = request.Email.ToLower(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                MiddleName = request.MiddleName,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                EmailConfirmationToken = Guid.NewGuid().ToString(),
                Status = UserStatus.Active,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Address = request.Address,
                City = request.City,
                State = request.State,
                Country = request.Country,
                Bio = request.Bio
            };

            // Add user to context first to get ID
            await _unitOfWork.Users.AddAsync(user, cancellationToken);

            // Save user to get ID
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Create organization - REQUIRED as per user requirement
            var orgCode = request.OrganizationCode ?? await GenerateOrganizationCodeAsync(cancellationToken);
            
            // Check if code already exists
            if (await _unitOfWork.Organizations.GetByCodeAsync(orgCode, cancellationToken) != null)
            {
                throw new BadRequestException($"Organization with code '{orgCode}' already exists. Please choose a different organization code.");
            }

            var organization = new Organization
            {
                Name = request.OrganizationName,
                Code = orgCode,
                Description = request.OrganizationDescription,
                OwnerId = user.Id,
                IsActive = true,
                IsVerified = false
            };

            await _unitOfWork.Organizations.AddAsync(organization, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Add user as OrganizationMember
            var ownerMember = new OrganizationMember
            {
                OrganizationId = organization.Id,
                UserId = user.Id,
                Position = "Owner",
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };

            organization.Members.Add(ownerMember);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Assign Platform Admin role for the organization (organization-scoped)
            var platformAdminRole = await _unitOfWork.Roles.GetByRoleTypeAsync(RoleType.PlatformAdmin, cancellationToken);
            if (platformAdminRole != null)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = platformAdminRole.Id,
                    OrganizationId = organization.Id, // Scope role to the organization
                    IsActive = true,
                    ActivatedAt = DateTime.UtcNow
                };

                await _unitOfWork.UserRoles.AddAsync(userRole, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Create trial subscription - 30 days trial with 5 licenses
            var (subscriptionSuccess, subscription, subscriptionError) = await _subscriptionService.CreateSubscriptionAsync(
                organization.Id,
                user.Id,
                numberOfLicenses: 5,
                trialDays: 30
            );

            if (!subscriptionSuccess || subscription == null)
            {
                // If subscription creation fails, we still proceed but log it
                // The organization is created but without subscription
                // Admin can add subscription later
                System.Diagnostics.Debug.WriteLine($"Warning: Failed to create trial subscription: {subscriptionError}");
            }
            else
            {
                // Assign license to the owner
                await _subscriptionService.AssignLicenseAsync(organization.Id, user.Id);
            }

            // Send confirmation email AFTER successful save
            await _emailService.SendEmailConfirmationAsync(user.Email, user.FullName, user.EmailConfirmationToken!);

            return new RegisterResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                OrganizationId = organization.Id,
                OrganizationName = organization.Name,
                Message = "Registration successful! Your organization and trial subscription have been created. Please check your email to confirm your account."
            };
        }

        private async Task<string> GenerateOrganizationCodeAsync(CancellationToken cancellationToken)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = "ORG";
            
            // Find all organizations with codes matching the pattern for this year
            var allOrganizations = await _unitOfWork.Organizations.FindAsync(
                o => !o.IsDeleted && o.Code.StartsWith($"{prefix}-{year}-", StringComparison.OrdinalIgnoreCase),
                cancellationToken);
            
            int sequence = 1;
            if (allOrganizations.Any())
            {
                var sequences = allOrganizations
                    .Select(o =>
                    {
                        var parts = o.Code.Split('-');
                        if (parts.Length >= 3 && int.TryParse(parts[2], out int seq))
                            return seq;
                        return 0;
                    })
                    .Where(s => s > 0)
                    .ToList();

                if (sequences.Any())
                {
                    sequence = sequences.Max() + 1;
                }
            }

            return $"{prefix}-{year}-{sequence:D3}";
        }
    }
}