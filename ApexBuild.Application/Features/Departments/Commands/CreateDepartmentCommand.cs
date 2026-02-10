using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Departments.Commands
{
    public record CreateDepartmentCommand : IRequest<CreateDepartmentResponse>
    {
        public Guid ProjectId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? Specialization { get; init; }
        public Guid? SupervisorId { get; init; }
        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }
    }

    public record CreateDepartmentResponse
    {
        public Guid Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
    }

    public class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, CreateDepartmentResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        public CreateDepartmentCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<CreateDepartmentResponse> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(request.ProjectId, cancellationToken)
                ?? throw new NotFoundException("Project", request.ProjectId);

            var count = await _unitOfWork.Departments.CountAsync(
                d => d.ProjectId == request.ProjectId, cancellationToken);

            var code = $"DEPT-{request.Name.Substring(0, Math.Min(3, request.Name.Length)).ToUpper()}-{(count + 1):D3}";

            var dept = new Department
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                Name = request.Name,
                Code = code,
                Description = request.Description,
                Specialization = request.Specialization,
                SupervisorId = request.SupervisorId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = DepartmentStatus.Active
            };

            await _unitOfWork.Departments.AddAsync(dept, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateDepartmentResponse { Id = dept.Id, Code = dept.Code, Name = dept.Name };
        }
    }
}
