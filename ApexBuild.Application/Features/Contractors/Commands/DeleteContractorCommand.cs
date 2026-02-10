using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;

namespace ApexBuild.Application.Features.Contractors.Commands
{
    public record DeleteContractorCommand(Guid ContractorId) : IRequest<bool>;

    public class DeleteContractorCommandHandler : IRequestHandler<DeleteContractorCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        public DeleteContractorCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<bool> Handle(DeleteContractorCommand request, CancellationToken cancellationToken)
        {
            var contractor = await _unitOfWork.Contractors.GetByIdAsync(request.ContractorId, cancellationToken)
                ?? throw new NotFoundException("Contractor", request.ContractorId);

            await _unitOfWork.Contractors.DeleteAsync(contractor, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
