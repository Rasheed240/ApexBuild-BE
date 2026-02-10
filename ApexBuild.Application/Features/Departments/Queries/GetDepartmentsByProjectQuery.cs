using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Departments.Queries
{
    public record GetDepartmentsByProjectQuery(Guid ProjectId) : IRequest<List<DepartmentDto>>;

    public class GetDepartmentsByProjectQueryHandler : IRequestHandler<GetDepartmentsByProjectQuery, List<DepartmentDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public GetDepartmentsByProjectQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<List<DepartmentDto>> Handle(GetDepartmentsByProjectQuery request, CancellationToken cancellationToken)
        {
            var departments = await _unitOfWork.Departments.FindAsync(
                d => d.ProjectId == request.ProjectId, cancellationToken);

            return departments.Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name,
                Code = d.Code,
                Description = d.Description,
                Specialization = d.Specialization,
                Status = d.Status,
                IsOutsourced = d.IsOutsourced,
                ContractorId = d.ContractorId,
                SupervisorId = d.SupervisorId,
                SupervisorName = d.Supervisor != null
                    ? $"{d.Supervisor.FirstName} {d.Supervisor.LastName}".Trim()
                    : null,
                StartDate = d.StartDate,
                EndDate = d.EndDate
            }).ToList();
        }
    }

    public class DepartmentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Specialization { get; set; }
        public DepartmentStatus Status { get; set; }
        public bool IsOutsourced { get; set; }
        public Guid? ContractorId { get; set; }
        public Guid? SupervisorId { get; set; }
        public string? SupervisorName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
