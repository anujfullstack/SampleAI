namespace NLToSQLApp.Models;

/// <summary>
/// Represents token usage information from OpenAI API
/// </summary>
public class TokenUsage
{
    /// <summary>
    /// Number of tokens used in the prompt
    /// </summary>
    public int Prompt { get; set; }

    /// <summary>
    /// Number of tokens used in the completion
    /// </summary>
    public int Completion { get; set; }

    /// <summary>
    /// Total number of tokens used
    /// </summary>
    public int Total { get; set; }
}