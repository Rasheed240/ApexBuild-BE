namespace ApexBuild.Application.Features.Projects.Queries.GetProjectMembers
{
    public class GetProjectMembersResponse
    {
        public List<ProjectMemberDto> Members { get; set; } = new();
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    public class ProjectMemberDto
    {
        public Guid UserId { get; set; }
        public Guid? WorkInfoId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public string Position { get; set; } = string.Empty;
        public string? EmployeeId { get; set; }
        public string? DepartmentName { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? ContractorName { get; set; }
        public Guid? ContractorId { get; set; }
        public string ContractType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Responsibilities { get; set; }
        public string? ReportingTo { get; set; }
        public string? PhoneNumber { get; set; }
        public string? RoleName { get; set; }
    }
}
