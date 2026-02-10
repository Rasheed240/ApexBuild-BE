using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;
using ApexBuild.Application.Common.Exceptions;

namespace ApexBuild.Application.Features.Contractors.Commands
{
    public class CreateContractorCommandHandler : IRequestHandler<CreateContractorCommand, CreateContractorResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateContractorCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CreateContractorResponse> Handle(CreateContractorCommand request, CancellationToken cancellationToken)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(request.ProjectId, cancellationToken)
                ?? throw new NotFoundException("Project", request.ProjectId);

            var admin = await _unitOfWork.Users.GetByIdAsync(request.ContractorAdminId, cancellationToken)
                ?? throw new NotFoundException("User", request.ContractorAdminId);

            // Generate code
            var existingCount = await _unitOfWork.Contractors.CountAsync(
                c => c.ProjectId == request.ProjectId, cancellationToken);
            var code = $"CONTR-{DateTime.UtcNow:yyyy}-{(existingCount + 1):D3}";

            var contractor = new Contractor
            {
                Id = Guid.NewGuid(),
                CompanyName = request.CompanyName,
                Code = code,
                RegistrationNumber = request.RegistrationNumber,
                ContractNumber = request.ContractNumber,
                ProjectId = request.ProjectId,
                DepartmentId = request.DepartmentId,
                ContractorAdminId = request.ContractorAdminId,
                Specialization = request.Specialization,
                Description = request.Description,
                ContractStartDate = request.ContractStartDate,
                ContractEndDate = request.ContractEndDate,
                ContractValue = request.ContractValue,
                Currency = request.Currency,
                ContractDocumentUrls = request.ContractDocumentUrls,
                Notes = request.Notes,
                Status = ContractorStatus.PendingStart
            };

            // If linked to a department, mark it as outsourced
            if (request.DepartmentId.HasValue)
            {
                var dept = await _unitOfWork.Departments.GetByIdAsync(request.DepartmentId.Value, cancellationToken);
                if (dept != null)
                {
                    dept.IsOutsourced = true;
                    dept.ContractorId = contractor.Id;
                    await _unitOfWork.Departments.UpdateAsync(dept, cancellationToken);
                }
            }

            await _unitOfWork.Contractors.AddAsync(contractor, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateContractorResponse
            {
                Id = contractor.Id,
                Code = contractor.Code,
                CompanyName = contractor.CompanyName,
                Message = $"Contractor '{contractor.CompanyName}' added to project successfully."
            };
        }
    }
}
