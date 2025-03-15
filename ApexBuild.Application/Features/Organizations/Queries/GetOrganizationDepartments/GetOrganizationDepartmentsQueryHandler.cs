using MediatR;
using Microsoft.EntityFrameworkCore;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Contracts.Responses.DTOs;

namespace ApexBuild.Application.Features.Organizations.Queries.GetOrganizationDepartments;

public class GetOrganizationDepartmentsQueryHandler : IRequestHandler<GetOrganizationDepartmentsQuery, List<DepartmentDto>>
{
    private readonly IDepartmentRepository _departmentRepository;

    public GetOrganizationDepartmentsQueryHandler(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<List<DepartmentDto>> Handle(GetOrganizationDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var departments = await _departmentRepository.GetDepartmentsByOrganizationAsync(request.OrganizationId, cancellationToken);

        return departments.Select(d => new DepartmentDto
        {
            Id = d.Id,
            Name = d.Name,
            Code = d.Code,
            ProjectId = d.ProjectId,
            OrganizationId = d.OrganizationId,
            SupervisorId = d.SupervisorId
        }).ToList();
    }
}
