using MediatR;
using ApexBuild.Contracts.Responses.DTOs;

namespace ApexBuild.Application.Features.Organizations.Queries.GetOrganizationDepartments;

public record GetOrganizationDepartmentsQuery : IRequest<List<DepartmentDto>>
{
    public Guid OrganizationId { get; init; }
}
