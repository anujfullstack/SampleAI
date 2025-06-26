using Microsoft.AspNetCore.Mvc;
using NLToSQLApp.DTOs;
using NLToSQLApp.Services;

namespace NLToSQLApp.Controllers;

/// <summary>
/// Controller for handling natural language to SQL queries
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QueryController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly ISqliteSchemaService _schemaService;
    private readonly IOpenAiService _openAiService;
    private readonly ILogger<QueryController> _logger;

    public QueryController(
        IFileService fileService,
        ISqliteSchemaService schemaService,
        IOpenAiService openAiService,
        ILogger<QueryController> logger)
    {
        _fileService = fileService;
        _schemaService = schemaService;
        _openAiService = openAiService;
        _logger = logger;
    }

    /// <summary>
    /// Process a natural language query and return SQL results
    /// </summary>
    /// <param name="request">The query request containing projectId and query</param>
    /// <returns>Query results with generated SQL and token usage</returns>
    [HttpPost("ask")]
    public async Task<ActionResult<QueryResponse>> AskAsync([FromBody] QueryRequest request)
    {
        try
        {
            _logger.LogInformation("Processing query for project {ProjectId}: {Query}", 
                request.ProjectId, request.Query);

            // Download SQLite file from Azure File Share
            var dbPath = await _fileService.DownloadDatabaseAsync(request.ProjectId);
            
            // Extract database schema
            var schema = await _schemaService.ExtractSchemaAsync(dbPath);
            
            // Generate SQL query using OpenAI
            var sqlResult = await _openAiService.GenerateSqlAsync(schema, request.Query);
            
            // Execute the SQL query
            var data = await _schemaService.ExecuteQueryAsync(dbPath, sqlResult.Sql);
            
            // Clean up temporary file
            await _fileService.CleanupAsync(dbPath);
            
            var response = new QueryResponse
            {
                Data = data,
                Sql = sqlResult.Sql,
                TokenUsage = sqlResult.TokenUsage,
                Schema = schema.Keys.ToList()
            };

            _logger.LogInformation("Query processed successfully. Returned {RecordCount} records", 
                data.Count);

            return Ok(response);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning("Database file not found for project {ProjectId}: {Message}", 
                request.ProjectId, ex.Message);
            return NotFound($"Database for project {request.ProjectId} not found");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid SQL query generated: {Message}", ex.Message);
            return BadRequest($"Invalid query: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query for project {ProjectId}", request.ProjectId);
            return StatusCode(500, "An error occurred while processing your query");
        }
    }
}