namespace Saola.Core.Services.Models;

/// <summary>
/// Result of a service operation that does not produce batch output.
/// Used by services like StoryService where the outcome is success/fail
/// with an optional message, rather than a collection of created elements.
///
/// Keeps the component layer free of try/catch blocks — the component
/// checks IsSuccess and surfaces Message as a GH runtime message.
/// </summary>
public sealed class ServiceResult
{
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Success: human-readable summary (e.g. "3 stories set successfully.").
    /// Failure: descriptive error message safe to surface as a GH error.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    public static ServiceResult Ok(string message = "") => new()
    {
        IsSuccess = true,
        Message = message,
    };

    public static ServiceResult Fail(string message) => new()
    {
        IsSuccess = false,
        Message = message,
    };

    public override string ToString() =>
        IsSuccess ? $"OK — {Message}" : $"FAILED — {Message}";
}
