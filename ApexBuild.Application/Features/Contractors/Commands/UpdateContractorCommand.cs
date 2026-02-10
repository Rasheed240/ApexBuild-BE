using MediatR;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Contractors.Commands
{
    public record UpdateContractorCommand : IRequest<bool>
    {
        public Guid ContractorId { get; init; }
        public string? CompanyName { get; init; }
        public string? Specialization { get; init; }
        public string? Description { get; init; }
        public DateTime? ContractStartDate { get; init; }
        public DateTime? ContractEndDate { get; init; }
        public decimal? ContractValue { get; init; }
        public ContractorStatus? Status { get; init; }
        public List<string>? ContractDocumentUrls { get; init; }
        public string? Notes { get; init; }
        public string? ContractNumber { get; init; }
    }
}
