namespace ApexBuild.Application.Features.Manuals.Queries.GetLatestManual;

public class GetLatestManualResponse
{
    public Guid   Id             { get; set; }
    public string Title          { get; set; } = string.Empty;
    public string Version        { get; set; } = string.Empty;
    public string FileUrl        { get; set; } = string.Empty;
    public long   FileSizeBytes  { get; set; }
    public DateTime UploadedAt   { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
}
