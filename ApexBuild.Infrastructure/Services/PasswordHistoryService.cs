using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApexBuild.Infrastructure.Services;

public class PasswordHistoryService : IPasswordHistoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public PasswordHistoryService(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<bool> IsPasswordInHistoryAsync(Guid userId, string plainPassword, CancellationToken cancellationToken = default)
    {
        // Get last 5 password hashes from history
        var history = await _context.Set<PasswordHistory>()
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.CreatedAt)
            .Take(5) // Check last 5 passwords
            .Select(ph => ph.PasswordHash)
            .ToListAsync(cancellationToken);

        // Verify the plain password against each hash in history
        foreach (var hash in history)
        {
            if (_passwordHasher.VerifyPassword(plainPassword, hash))
            {
                return true; // Password found in history
            }
        }

        return false; // Password not in history
    }

    public async Task AddPasswordToHistoryAsync(Guid userId, string passwordHash, CancellationToken cancellationToken = default)
    {
        var passwordHistory = new PasswordHistory
        {
            UserId = userId,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Set<PasswordHistory>().AddAsync(passwordHistory, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Keep only last 5 passwords
        await ClearOldPasswordsAsync(userId, 5, cancellationToken);
    }

    public async Task ClearOldPasswordsAsync(Guid userId, int keepLastN = 5, CancellationToken cancellationToken = default)
    {
        var passwordsToDelete = await _context.Set<PasswordHistory>()
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.CreatedAt)
            .Skip(keepLastN)
            .ToListAsync(cancellationToken);

        if (passwordsToDelete.Any())
        {
            _context.Set<PasswordHistory>().RemoveRange(passwordsToDelete);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

