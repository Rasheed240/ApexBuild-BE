namespace ApexBuild.Application.Features.Users.Queries.GetProfileCompletion;

public record GetProfileCompletionResponse
{
    public int CompletionPercentage { get; init; }
    public int CompletedFields { get; init; }
    public int TotalFields { get; init; }
    public List<string> MissingFields { get; init; } = new();
    public bool IsProfileComplete { get; init; }
}

