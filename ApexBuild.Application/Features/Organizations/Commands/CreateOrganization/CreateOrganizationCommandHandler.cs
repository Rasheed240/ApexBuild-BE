using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Entities;

namespace ApexBuild.Application.Features.Organizations.Commands.CreateOrganization;

public class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommand, CreateOrganizationResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreateOrganizationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<CreateOrganizationResponse> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to create an organization");
        }

        // Generate organization code if not provided
        string orgCode = request.Code ?? await GenerateOrganizationCodeAsync(cancellationToken);

        // Check if code already exists
        if (await _unitOfWork.Organizations.GetByCodeAsync(orgCode, cancellationToken) != null)
        {
            throw new BadRequestException($"Organization with code '{orgCode}' already exists");
        }

        // Create organization
        var organization = new Organization
        {
            Name = request.Name,
            Code = orgCode,
            Description = request.Description,
            RegistrationNumber = request.RegistrationNumber,
            TaxId = request.TaxId,
            Email = request.Email?.ToLower(),
            PhoneNumber = request.PhoneNumber,
            Website = request.Website,
            Address = request.Address,
            City = request.City,
            State = request.State,
            Country = request.Country,
            LogoUrl = request.LogoUrl,
            OwnerId = currentUserId.Value,
            IsActive = true,
            IsVerified = false,
            MetaData = request.MetaData
        };

        await _unitOfWork.Organizations.AddAsync(organization, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Automatically add the owner as a member
        var ownerMember = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = currentUserId.Value,
            Position = "Owner",
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };

        organization.Members.Add(ownerMember);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateOrganizationResponse
        {
            OrganizationId = organization.Id,
            Name = organization.Name,
            Code = organization.Code,
            Message = "Organization created successfully"
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

