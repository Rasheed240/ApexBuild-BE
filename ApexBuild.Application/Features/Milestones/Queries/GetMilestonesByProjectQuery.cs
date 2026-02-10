using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Milestones.Queries
{
    public record GetMilestonesByProjectQuery(Guid ProjectId) : IRequest<List<MilestoneDto>>;

    public class GetMilestonesByProjectQueryHandler : IRequestHandler<GetMilestonesByProjectQuery, List<MilestoneDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public GetMilestonesByProjectQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<List<MilestoneDto>> Handle(GetMilestonesByProjectQuery request, CancellationToken cancellationToken)
        {
            var milestones = await _unitOfWork.Milestones.GetByProjectAsync(request.ProjectId, cancellationToken);

            return milestones.Select(m => new MilestoneDto
            {
                Id = m.Id,
                Title = m.Title,
                Description = m.Description,
                ProjectId = m.ProjectId,
                DepartmentId = m.DepartmentId,
                DepartmentName = m.Department?.Name,
                DueDate = m.DueDate,
                CompletedAt = m.CompletedAt,
                Status = m.Status,
                Progress = m.Progress,
                OrderIndex = m.OrderIndex,
                IsOverdue = m.DueDate < DateTime.UtcNow && m.Status != MilestoneStatus.Completed
            }).ToList();
        }
    }

    public class MilestoneDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid ProjectId { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public MilestoneStatus Status { get; set; }
        public decimal Progress { get; set; }
        public int OrderIndex { get; set; }
        public bool IsOverdue { get; set; }
    }
}
