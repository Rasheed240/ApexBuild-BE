using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Application.Common.Interfaces;
using MediatR;

namespace ApexBuild.Application.Features.Users.Queries.GetProfileCompletion;

public class GetProfileCompletionQueryHandler : IRequestHandler<GetProfileCompletionQuery, GetProfileCompletionResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetProfileCompletionQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetProfileCompletionResponse> Handle(GetProfileCompletionQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            throw new UnauthorizedException("User not authenticated");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", userId.Value);
        }

        var totalFields = 12;
        var completedFields = 0;
        var missingFields = new List<string>();

        // Required fields
        if (!string.IsNullOrWhiteSpace(user.FirstName)) completedFields++;
        else missingFields.Add("First Name");

        if (!string.IsNullOrWhiteSpace(user.LastName)) completedFields++;
        else missingFields.Add("Last Name");

        if (!string.IsNullOrWhiteSpace(user.Email)) completedFields++;
        else missingFields.Add("Email");

        // Optional but important fields
        if (!string.IsNullOrWhiteSpace(user.PhoneNumber)) completedFields++;
        else missingFields.Add("Phone Number");

        if (user.DateOfBirth.HasValue) completedFields++;
        else missingFields.Add("Date of Birth");

        if (!string.IsNullOrWhiteSpace(user.Gender)) completedFields++;
        else missingFields.Add("Gender");

        if (!string.IsNullOrWhiteSpace(user.Address)) completedFields++;
        else missingFields.Add("Address");

        if (!string.IsNullOrWhiteSpace(user.City)) completedFields++;
        else missingFields.Add("City");

        if (!string.IsNullOrWhiteSpace(user.State)) completedFields++;
        else missingFields.Add("State");

        if (!string.IsNullOrWhiteSpace(user.Country)) completedFields++;
        else missingFields.Add("Country");

        if (!string.IsNullOrWhiteSpace(user.Bio)) completedFields++;
        else missingFields.Add("Bio");

        if (!string.IsNullOrWhiteSpace(user.ProfileImageUrl)) completedFields++;
        else missingFields.Add("Profile Image");

        var completionPercentage = (int)Math.Round((double)completedFields / totalFields * 100);

        return new GetProfileCompletionResponse
        {
            CompletionPercentage = completionPercentage,
            CompletedFields = completedFields,
            TotalFields = totalFields,
            MissingFields = missingFields,
            IsProfileComplete = completionPercentage >= 80 // 80% considered complete
        };
    }
}

