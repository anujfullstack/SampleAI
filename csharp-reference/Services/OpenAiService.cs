using Azure.AI.OpenAI;
using Azure;
using NLToSQLApp.Models;
using NLToSQLApp.Services;
using System.Text;

namespace NLToSQLApp.Services;

/// <summary>
/// Implementation of OpenAI service for SQL generation
/// </summary>
public class OpenAiService : IOpenAiService
{
    private readonly OpenAIClient _openAiClient;
    private readonly ILogger<OpenAiService> _logger;
    private readonly string _deploymentName;

    public OpenAiService(IConfiguration configuration, ILogger<OpenAiService> logger)
    {
        var endpoint = "https://anuj-mc3k41mk-swedencentral.openai.azure.com/";
        var apiKey = "5W32mmvE6J12kntNuAYvxAHyrQCfUOS1nYKgumMFawbSrzw1S3uiJQQJ99BFACfhMk5XJ3w3AAAAACOG3hyq";
        _deploymentName = "gpt4o-deployment";
        
        _openAiClient = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _logger = logger;
    }

    /// <summary>
    /// Generate SQL query from natural language using Azure OpenAI
    /// </summary>
    /// <param name="schema">Database schema information</param>
    /// <param name="userQuery">User's natural language query</param>
    /// <returns>Generated SQL query with token usage information</returns>
    public async Task<SqlGenerationResult> GenerateSqlAsync(
        Dictionary<string, List<ColumnInfo>> schema, 
        string userQuery)
    {
        try
        {
            var schemaText = FormatSchemaForPrompt(schema);
            var systemPrompt = CreateSystemPrompt(schemaText);
            
            _logger.LogInformation("Generating SQL for query: {Query}", userQuery);

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userQuery)
                },
                MaxTokens = 500,
                Temperature = 0.1f
            };

            var response = await _openAiClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var choice = response.Value.Choices.First();
            var generatedSql = choice.Message.Content.Trim();

            // Clean up SQL (remove markdown formatting if present)
            generatedSql = CleanupSqlResponse(generatedSql);

            var tokenUsage = new TokenUsage
            {
                Prompt = response.Value.Usage.PromptTokens,
                Completion = response.Value.Usage.CompletionTokens,
                Total = response.Value.Usage.TotalTokens
            };

            _logger.LogInformation("Generated SQL successfully. Tokens used: {Total}", tokenUsage.Total);

            return new SqlGenerationResult
            {
                Sql = generatedSql,
                TokenUsage = tokenUsage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SQL for query: {Query}", userQuery);
            throw;
        }
    }

    /// <summary>
    /// Format database schema for inclusion in the prompt
    /// </summary>
    /// <param name="schema">Database schema information</param>
    /// <returns>Formatted schema text</returns>
    private static string FormatSchemaForPrompt(Dictionary<string, List<ColumnInfo>> schema)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Database Schema:");
        
        foreach (var table in schema)
        {
            sb.AppendLine($"\nTable: {table.Key}");
            foreach (var column in table.Value)
            {
                var constraints = new List<string>();
                if (column.PrimaryKey) constraints.Add("PRIMARY KEY");
                if (column.NotNull) constraints.Add("NOT NULL");
                
                var constraintText = constraints.Any() ? $" ({string.Join(", ", constraints)})" : "";
                sb.AppendLine($"  - {column.Name}: {column.Type}{constraintText}");
            }
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Create system prompt for SQL generation
    /// </summary>
    /// <param name="schemaText">Formatted schema text</param>
    /// <returns>System prompt</returns>
    private static string CreateSystemPrompt(string schemaText)
    {
        return $@"You are a SQL expert. Given the following database schema, generate a valid SQLite query to answer the user's question.

{schemaText}

Rules:
1. Only generate SELECT queries (no INSERT, UPDATE, DELETE, DROP, etc.)
2. Use proper SQL syntax for SQLite
3. Return only the SQL query without any explanation or markdown formatting
4. Use appropriate JOINs when needed
5. Consider using aggregate functions (COUNT, SUM, AVG, etc.) when appropriate
6. Use LIMIT to prevent returning too many results if not specified
7. Make sure column names and table names are correct according to the schema";
    }

    /// <summary>
    /// Clean up SQL response by removing markdown formatting
    /// </summary>
    /// <param name="sql">Raw SQL response</param>
    /// <returns>Cleaned SQL</returns>
    private static string CleanupSqlResponse(string sql)
    {
        // Remove markdown code blocks
        sql = sql.Replace("```sql", "").Replace("```", "");
        
        // Remove extra whitespace
        sql = sql.Trim();
        
        return sql;
    }
}