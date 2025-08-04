using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Common.Interfaces
{
    /// <summary>
    /// Repository interface for PaymentTransaction entities.
    /// </summary>
    public interface IPaymentTransactionRepository
    {
        IQueryable<PaymentTransaction> GetQueryable();
        Task<PaymentTransaction> GetByIdAsync(Guid id);
        Task<PaymentTransaction> GetByStripeChargeIdAsync(string stripeChargeId);
        Task<List<PaymentTransaction>> GetBySubscriptionIdAsync(Guid subscriptionId);
        Task<List<PaymentTransaction>> GetByOrganizationIdAsync(Guid organizationId);
        Task<List<PaymentTransaction>> GetPendingPaymentsAsync();
        Task<List<PaymentTransaction>> GetFailedPaymentsAsync();
        Task<List<PaymentTransaction>> GetRetryablePaymentsAsync();
        Task<List<PaymentTransaction>> GetByStatusAsync(PaymentStatus status);
        Task<decimal> GetTotalRevenueForPeriodAsync(DateTime startDate, DateTime endDate);
        Task AddAsync(PaymentTransaction transaction);
        void Update(PaymentTransaction transaction);
        void Delete(PaymentTransaction transaction);
    }
}
