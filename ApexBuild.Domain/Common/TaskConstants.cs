namespace ApexBuild.Domain.Common;

/// <summary>
/// Centralized constants for task-related validation and business rules.
/// </summary>
public static class TaskConstants
{
    // Priority levels
    public const int MinPriority = 1;
    public const int MaxPriority = 4;
    public const string PriorityLow = "Low";
    public const string PriorityMedium = "Medium";
    public const string PriorityHigh = "High";
    public const string PriorityCritical = "Critical";

    // Field length limits
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 2000;
    public const int MaxLocationLength = 500;
    public const int MaxTagLength = 100;

    // Estimated hours constraints
    public const double MinEstimatedHours = 0;
    public const double MaxEstimatedHours = 10000;

    /// <summary>
    /// Returns the display name for a given priority value.
    /// </summary>
    public static string GetPriorityName(int priority) => priority switch
    {
        1 => PriorityLow,
        2 => PriorityMedium,
        3 => PriorityHigh,
        4 => PriorityCritical,
        _ => "Unknown"
    };
}
