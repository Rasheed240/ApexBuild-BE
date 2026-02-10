using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;

namespace ApexBuild.Application.Features.Contractors.Commands
{
    public class UpdateContractorCommandHandler : IRequestHandler<UpdateContractorCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateContractorCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateContractorCommand request, CancellationToken cancellationToken)
        {
            var contractor = await _unitOfWork.Contractors.GetByIdAsync(request.ContractorId, cancellationToken)
                ?? throw new NotFoundException("Contractor", request.ContractorId);

            if (request.CompanyName != null) contractor.CompanyName = request.CompanyName;
            if (request.Specialization != null) contractor.Specialization = request.Specialization;
            if (request.Description != null) contractor.Description = request.Description;
            if (request.ContractStartDate.HasValue) contractor.ContractStartDate = request.ContractStartDate.Value;
            if (request.ContractEndDate.HasValue) contractor.ContractEndDate = request.ContractEndDate.Value;
            if (request.ContractValue.HasValue) contractor.ContractValue = request.ContractValue;
            if (request.Status.HasValue) contractor.Status = request.Status.Value;
            if (request.ContractDocumentUrls != null) contractor.ContractDocumentUrls = request.ContractDocumentUrls;
            if (request.Notes != null) contractor.Notes = request.Notes;
            if (request.ContractNumber != null) contractor.ContractNumber = request.ContractNumber;

            await _unitOfWork.Contractors.UpdateAsync(contractor, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
