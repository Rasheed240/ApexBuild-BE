using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Features.Projects.Queries.GetTopProjectProgress;

namespace ApexBuild.Application.Features.Projects.Queries.GetProjectProgress;

public class GetProjectProgressQueryHandler : IRequestHandler<GetProjectProgressQuery, ProjectProgressDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProjectProgressQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProjectProgressDto> Handle(GetProjectProgressQuery request, CancellationToken cancellationToken)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
        {
            return new ProjectProgressDto
            {
                Id = request.ProjectId,
                Name = string.Empty,
                TotalTasks = 0,
                CompletedTasks = 0,
                Progress = 0
            };
        }

        var total = await _unitOfWork.Tasks.CountAsync(t => t.Department.ProjectId == request.ProjectId, cancellationToken);
        var completed = await _unitOfWork.Tasks.CountAsync(t => t.Department.ProjectId == request.ProjectId && t.Status == Domain.Enums.TaskStatus.Completed, cancellationToken);
        var progress = total == 0 ? 0 : (int)System.Math.Round((completed * 100.0M) / total);

        return new ProjectProgressDto
        {
            Id = project.Id,
            Name = project.Name,
            TotalTasks = total,
            CompletedTasks = completed,
            Progress = progress
        };
    }
}
