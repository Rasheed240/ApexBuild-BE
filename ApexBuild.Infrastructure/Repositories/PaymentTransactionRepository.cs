using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;
using ApexBuild.Infrastructure.Persistence;

namespace ApexBuild.Infrastructure.Repositories
{
    public class PaymentTransactionRepository : IPaymentTransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public PaymentTransactionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<PaymentTransaction> GetQueryable()
        {
            return _context.PaymentTransactions.AsQueryable();
        }

        public async Task<PaymentTransaction> GetByIdAsync(Guid id)
        {
            return await _context.PaymentTransactions
                .Include(p => p.Organization)
                .Include(p => p.Subscription)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        }

        public async Task<PaymentTransaction> GetByStripeChargeIdAsync(string stripeChargeId)
        {
            return await _context.PaymentTransactions
                .Include(p => p.Organization)
                .Include(p => p.Subscription)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.StripeChargeId == stripeChargeId && !p.IsDeleted);
        }

        public async Task<List<PaymentTransaction>> GetBySubscriptionIdAsync(Guid subscriptionId)
        {
            return await _context.PaymentTransactions
                .Where(p => p.SubscriptionId == subscriptionId && !p.IsDeleted)
                .Include(p => p.Organization)
                .Include(p => p.Subscription)
                .Include(p => p.User)
                .OrderByDescending(p => p.TransactionDate)
                .ToListAsync();
        }

        public async Task<List<PaymentTransaction>> GetByOrganizationIdAsync(Guid organizationId)
        {
            return await _context.PaymentTransactions
                .Where(p => p.OrganizationId == organizationId && !p.IsDeleted)
                .Include(p => p.Organization)
                .Include(p => p.Subscription)
                .Include(p => p.User)
                .OrderByDescending(p => p.TransactionDate)
                .ToListAsync();
        }

        public async Task<List<PaymentTransaction>> GetPendingPaymentsAsync()
        {
            return await _context.PaymentTransactions
                .Where(p => !p.IsDeleted && p.Status == PaymentStatus.Pending)
                .Include(p => p.Organization)
                .Include(p => p.Subscription)
                .OrderByDescending(p => p.TransactionDate)
                .ToListAsync();
        }

        public async Task<List<PaymentTransaction>> GetFailedPaymentsAsync()
        {
            return await _context.PaymentTransactions
                .Where(p => !p.IsDeleted && p.Status == PaymentStatus.Failed)
                .Include(p => p.Organization)
                .Include(p => p.Subscription)
                .OrderByDescending(p => p.TransactionDate)
                .ToListAsync();
        }

        public async Task<List<PaymentTransaction>> GetRetryablePaymentsAsync()
        {
            return await _context.PaymentTransactions
                .Where(p => !p.IsDeleted &&
                       p.Status == PaymentStatus.Failed &&
                       p.RetryCount < p.MaxRetries &&
                       p.NextRetryAt <= DateTime.UtcNow)
                .Include(p => p.Organization)
                .Include(p => p.Subscription)
                .OrderByDescending(p => p.TransactionDate)
                .ToListAsync();
        }

        public async Task<List<PaymentTransaction>> GetByStatusAsync(PaymentStatus status)
        {
            return await _context.PaymentTransactions
                .Where(p => !p.IsDeleted && p.Status == status)
                .Include(p => p.Organization)
                .Include(p => p.Subscription)
                .OrderByDescending(p => p.TransactionDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalRevenueForPeriodAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.PaymentTransactions
                .Where(p => !p.IsDeleted &&
                       p.Status == PaymentStatus.Completed &&
                       p.TransactionDate >= startDate &&
                       p.TransactionDate <= endDate)
                .SumAsync(p => p.TotalAmount);
        }

        public async Task AddAsync(PaymentTransaction transaction)
        {
            await _context.PaymentTransactions.AddAsync(transaction);
        }

        public void Update(PaymentTransaction transaction)
        {
            _context.PaymentTransactions.Update(transaction);
        }

        public void Delete(PaymentTransaction transaction)
        {
            transaction.IsDeleted = true;
            transaction.DeletedAt = DateTime.UtcNow;
            _context.PaymentTransactions.Update(transaction);
        }
    }
}
