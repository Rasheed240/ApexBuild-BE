using ApexBuild.Domain.Entities;

namespace ApexBuild.Application.Common.Interfaces;

public interface ITaskUpdateRepository : IRepository<TaskUpdate>
{
    Task<IEnumerable<TaskUpdate>> GetUpdatesByTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskUpdate>> GetUpdatesByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<TaskUpdate?> GetWithDetailsAsync(Guid updateId, CancellationToken cancellationToken = default);
    Task<bool> HasDailyReportForDateAsync(Guid taskId, Guid userId, DateTime date, CancellationToken cancellationToken = default);
    Task<(IEnumerable<TaskUpdate> Items, int TotalCount)> GetPendingForReviewAsync(Guid? organizationId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
