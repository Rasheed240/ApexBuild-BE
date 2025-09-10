namespace ApexBuild.Contracts.Responses.DTOs
{
    public class OrganizationMemberDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Position { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfileImageUrl { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public bool IsActive { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsOwner { get; set; }
    }
}
