using ApexBuild.Domain.Common;

namespace ApexBuild.Domain.Entities;

public class UserManual : BaseAuditableEntity
{
    public string Title      { get; set; } = string.Empty;
    public string Version    { get; set; } = string.Empty;
    public string FileUrl    { get; set; } = string.Empty;
    public string FilePublicId { get; set; } = string.Empty;
    public long   FileSizeBytes { get; set; }
    public Guid   UploadedByUserId { get; set; }
    public User?  UploadedByUser { get; set; }
}
