using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Enums;


namespace ApexBuild.Infrastructure.BackgroundJobs
{
    public class ExpiredInvitationCleanupJob
    {
        private readonly IUnitOfWork _unitOfWork;

        public ExpiredInvitationCleanupJob(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task CleanupExpiredInvitationsAsync()
        {
            
            var expiredInvitations = await _unitOfWork.Invitations.GetExpiredInvitationsAsync();

            foreach (var invitation in expiredInvitations)
            {
                invitation.Status = InvitationStatus.Expired;
            }

            await _unitOfWork.Invitations.UpdateRangeAsync(expiredInvitations);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}