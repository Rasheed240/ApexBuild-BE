namespace ApexBuild.Application.Common.Interfaces;

public interface IPasswordHistoryService
{
    Task<bool> IsPasswordInHistoryAsync(Guid userId, string plainPassword, CancellationToken cancellationToken = default);
    Task AddPasswordToHistoryAsync(Guid userId, string passwordHash, CancellationToken cancellationToken = default);
    Task ClearOldPasswordsAsync(Guid userId, int keepLastN = 5, CancellationToken cancellationToken = default);
}

