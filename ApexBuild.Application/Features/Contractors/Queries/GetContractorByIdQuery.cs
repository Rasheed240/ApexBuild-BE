using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Contractors.Queries
{
    public record GetContractorByIdQuery(Guid ContractorId) : IRequest<ContractorDetailDto>;

    public class GetContractorByIdQueryHandler : IRequestHandler<GetContractorByIdQuery, ContractorDetailDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        public GetContractorByIdQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<ContractorDetailDto> Handle(GetContractorByIdQuery request, CancellationToken cancellationToken)
        {
            var contractor = await _unitOfWork.Contractors.GetByIdWithDetailsAsync(request.ContractorId, cancellationToken)
                ?? throw new NotFoundException("Contractor", request.ContractorId);

            return new ContractorDetailDto
            {
                Id = contractor.Id,
                CompanyName = contractor.CompanyName,
                Code = contractor.Code,
                RegistrationNumber = contractor.RegistrationNumber,
                ContractNumber = contractor.ContractNumber,
                ProjectId = contractor.ProjectId,
                DepartmentId = contractor.DepartmentId,
                DepartmentName = contractor.Department?.Name,
                ContractorAdminId = contractor.ContractorAdminId,
                ContractorAdminName = $"{contractor.ContractorAdmin?.FirstName} {contractor.ContractorAdmin?.LastName}".Trim(),
                Specialization = contractor.Specialization,
                Description = contractor.Description,
                ContractStartDate = contractor.ContractStartDate,
                ContractEndDate = contractor.ContractEndDate,
                ContractValue = contractor.ContractValue,
                Currency = contractor.Currency,
                ContractDocumentUrls = contractor.ContractDocumentUrls,
                Status = contractor.Status,
                Notes = contractor.Notes,
                IsExpiringSoon = contractor.IsExpiringSoon,
                MemberCount = contractor.Members.Count,
                Members = contractor.Members.Select(m => new ContractorMemberDto
                {
                    UserId = m.UserId,
                    FullName = $"{m.User?.FirstName} {m.User?.LastName}".Trim(),
                    Email = m.User?.Email ?? string.Empty,
                    Position = m.Position,
                    StartDate = m.StartDate,
                    EndDate = m.EndDate
                }).ToList()
            };
        }
    }

    public class ContractorDetailDto
    {
        public Guid Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? RegistrationNumber { get; set; }
        public string? ContractNumber { get; set; }
        public Guid ProjectId { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public Guid ContractorAdminId { get; set; }
        public string ContractorAdminName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime ContractStartDate { get; set; }
        public DateTime ContractEndDate { get; set; }
        public decimal? ContractValue { get; set; }
        public string Currency { get; set; } = "USD";
        public List<string>? ContractDocumentUrls { get; set; }
        public ContractorStatus Status { get; set; }
        public string? Notes { get; set; }
        public bool IsExpiringSoon { get; set; }
        public int MemberCount { get; set; }
        public List<ContractorMemberDto> Members { get; set; } = new();
    }

    public class ContractorMemberDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
