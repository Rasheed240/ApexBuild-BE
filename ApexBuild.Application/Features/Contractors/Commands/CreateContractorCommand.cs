using MediatR;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Contractors.Commands
{
    public record CreateContractorCommand : IRequest<CreateContractorResponse>
    {
        public Guid ProjectId { get; init; }
        public Guid? DepartmentId { get; init; }
        public Guid ContractorAdminId { get; init; }
        public string CompanyName { get; init; } = string.Empty;
        public string? RegistrationNumber { get; init; }
        public string? ContractNumber { get; init; }
        public string Specialization { get; init; } = string.Empty;
        public string? Description { get; init; }
        public DateTime ContractStartDate { get; init; }
        public DateTime ContractEndDate { get; init; }
        public decimal? ContractValue { get; init; }
        public string Currency { get; init; } = "USD";
        public List<string>? ContractDocumentUrls { get; init; }
        public string? Notes { get; init; }
    }

    public record CreateContractorResponse
    {
        public Guid Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public string CompanyName { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }
}
