using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Milestones.Commands
{
    public record CompleteMilestoneCommand(Guid MilestoneId) : IRequest<bool>;

    public class CompleteMilestoneCommandHandler : IRequestHandler<CompleteMilestoneCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        public CompleteMilestoneCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<bool> Handle(CompleteMilestoneCommand request, CancellationToken cancellationToken)
        {
            var milestone = await _unitOfWork.Milestones.GetByIdAsync(request.MilestoneId, cancellationToken)
                ?? throw new NotFoundException("Milestone", request.MilestoneId);

            milestone.Status = MilestoneStatus.Completed;
            milestone.CompletedAt = DateTime.UtcNow;
            milestone.Progress = 100;

            await _unitOfWork.Milestones.UpdateAsync(milestone, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
