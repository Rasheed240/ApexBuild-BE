using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ApexBuild.Application.Common.Interfaces;

namespace ApexBuild.Application.Features.Projects.Queries.GetTopProjectProgress;

public class GetTopProjectProgressQueryHandler : IRequestHandler<GetTopProjectProgressQuery, GetTopProjectProgressResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTopProjectProgressQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetTopProjectProgressResponse> Handle(GetTopProjectProgressQuery request, CancellationToken cancellationToken)
    {
        var count = request.Count <= 0 ? 3 : request.Count;

        var projects = (await _unitOfWork.Projects.GetAllAsync(cancellationToken))
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToList();

        var result = new GetTopProjectProgressResponse();

        foreach (var p in projects)
        {
            // Count tasks belonging to project's departments
            var total = await _unitOfWork.Tasks.CountAsync(t => t.Department.ProjectId == p.Id, cancellationToken);
            var completed = await _unitOfWork.Tasks.CountAsync(t => t.Department.ProjectId == p.Id && t.Status == Domain.Enums.TaskStatus.Completed, cancellationToken);
            var progress = total == 0 ? 0 : (int)System.Math.Round((completed * 100.0M) / total);

            result.Items.Add(new ProjectProgressDto
            {
                Id = p.Id,
                Name = p.Name,
                TotalTasks = total,
                CompletedTasks = completed,
                Progress = progress
            });
        }

        return result;
    }
}
