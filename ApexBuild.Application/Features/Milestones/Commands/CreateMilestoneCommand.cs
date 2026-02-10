using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Milestones.Commands
{
    public record CreateMilestoneCommand : IRequest<CreateMilestoneResponse>
    {
        public Guid ProjectId { get; init; }
        public Guid? DepartmentId { get; init; }
        public Guid? CreatedByUserId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public DateTime DueDate { get; init; }
        public int OrderIndex { get; init; } = 1;
        public string? Notes { get; init; }
    }

    public record CreateMilestoneResponse
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
    }

    public class CreateMilestoneCommandHandler : IRequestHandler<CreateMilestoneCommand, CreateMilestoneResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        public CreateMilestoneCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<CreateMilestoneResponse> Handle(CreateMilestoneCommand request, CancellationToken cancellationToken)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(request.ProjectId, cancellationToken)
                ?? throw new NotFoundException("Project", request.ProjectId);

            var milestone = new ProjectMilestone
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                DepartmentId = request.DepartmentId,
                CreatedByUserId = request.CreatedByUserId,
                Title = request.Title,
                Description = request.Description,
                DueDate = request.DueDate,
                OrderIndex = request.OrderIndex,
                Notes = request.Notes,
                Status = MilestoneStatus.Upcoming,
                Progress = 0
            };

            await _unitOfWork.Milestones.AddAsync(milestone, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateMilestoneResponse { Id = milestone.Id, Title = milestone.Title };
        }
    }
}
