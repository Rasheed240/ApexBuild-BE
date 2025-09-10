namespace ApexBuild.Contracts.Responses.DTOs;

public class DepartmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? SupervisorId { get; set; }
}
