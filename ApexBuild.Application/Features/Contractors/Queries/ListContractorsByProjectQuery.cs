using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Contractors.Queries
{
    public record ListContractorsByProjectQuery(Guid ProjectId) : IRequest<List<ContractorSummaryDto>>;

    public class ListContractorsByProjectQueryHandler : IRequestHandler<ListContractorsByProjectQuery, List<ContractorSummaryDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public ListContractorsByProjectQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<List<ContractorSummaryDto>> Handle(ListContractorsByProjectQuery request, CancellationToken cancellationToken)
        {
            var contractors = await _unitOfWork.Contractors.GetByProjectAsync(request.ProjectId, cancellationToken);

            return contractors.Select(c => new ContractorSummaryDto
            {
                Id = c.Id,
                CompanyName = c.CompanyName,
                Code = c.Code,
                Specialization = c.Specialization,
                DepartmentName = c.Department?.Name,
                ContractorAdminName = $"{c.ContractorAdmin?.FirstName} {c.ContractorAdmin?.LastName}".Trim(),
                ContractStartDate = c.ContractStartDate,
                ContractEndDate = c.ContractEndDate,
                Status = c.Status,
                IsExpiringSoon = c.IsExpiringSoon,
                MemberCount = c.Members.Count
            }).ToList();
        }
    }

    public class ContractorSummaryDto
    {
        public Guid Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public string ContractorAdminName { get; set; } = string.Empty;
        public DateTime ContractStartDate { get; set; }
        public DateTime ContractEndDate { get; set; }
        public ContractorStatus Status { get; set; }
        public bool IsExpiringSoon { get; set; }
        public int MemberCount { get; set; }
    }
}
