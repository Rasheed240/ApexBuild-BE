using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Application.Common.Interfaces;
using MediatR;

namespace ApexBuild.Application.Features.Authentication.Commands.ConfirmEmail;

public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, ConfirmEmailResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmEmailCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ConfirmEmailResponse> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.EmailConfirmationToken == request.Token,
            cancellationToken);

        if (user == null)
        {
            throw new BadRequestException("Invalid confirmation token");
        }

        if (user.EmailConfirmed)
        {
            return new ConfirmEmailResponse
            {
                Message = "Email is already confirmed",
                IsAlreadyConfirmed = true
            };
        }

        user.EmailConfirmed = true;
        user.EmailConfirmedAt = DateTime.UtcNow;
        user.EmailConfirmationToken = null;

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ConfirmEmailResponse
        {
            Message = "Email confirmed successfully",
            IsAlreadyConfirmed = false
        };
    }
}

