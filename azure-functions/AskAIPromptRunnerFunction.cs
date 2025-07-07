using MeetApp.AzureOpenAIHub.DataManager;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Azure.Search.Documents.Indexes;
using Azure.AI.OpenAI;
using Azure.Search.Documents.Models;
using MeetApp.AzureOpenAIHub.Model;
using Azure.Search.Documents;
using System.Text;
using OpenAI.Chat;
using System.Text.RegularExpressions;
using MeetApp.AzureOpenAIHub.Scheams;


namespace MeetApp.AzureOpenAIHub.AzureOpenAIFunctions;

public class AskAIPromptRunnerFunction : BaseFunction
{
    private readonly ILogger _logger;
    private readonly SQLQueryHelper _sqlHelper;
    private readonly SemanticRedisCache _embeddingCache;

    public AskAIPromptRunnerFunction(ILoggerFactory loggerFactory, SemanticRedisCache embeddingCache) : base(loggerFactory.CreateLogger<BaseFunction>())
    {
        _logger = loggerFactory.CreateLogger<AskAIPromptRunnerFunction>();
        var dbHelperLogger = new ConsoleTokenUsageDBHelper();
        _sqlHelper = new SQLQueryHelper(new SQLTokenUsageDBHelper());
        _embeddingCache = embeddingCache;
    }
    /// <summary>
    /// HTTP trigger function for processing natural language queries about participants.
    /// Handles the complete flow: query embedding, schema search, SQL generation, and result formatting.
    /// Includes comprehensive token tracking and quota management.
    /// </summary>
    /// <param name="req">HTTP request containing the natural language query</param>
    /// <returns>Structured query results with data, charts, and insights</returns>
    [Function("AskAIPromptRunner")]
    public async Task<HttpResponseData> AskAIPromptRunner([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation("");
        _logger.LogInformation("--------------------------------------------------------");
        _logger.LogInformation("Processing natural language query request");
        var requestId = Guid.NewGuid().ToString();
        var totalTokensUsed = 0;
        var userId = "anonymous-user";
        // Parse request body to extract query and metadata
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var queryRequest = JsonSerializer.Deserialize<QueryRequest>(requestBody);
        if (queryRequest == null || string.IsNullOrWhiteSpace(queryRequest.Query))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Query is required");
            return badResponse;
        }
        try
        {
            // Extract metadata from request
            userId = queryRequest.UserId ?? userId;
            _logger.LogInformation($"Processing query: {queryRequest.Query} for ApplicationId: {queryRequest.ApplicationId} with EventId: {queryRequest.EventId}");

            var tokenUsageResponse = await base.GetTokenUsage(queryRequest.ApplicationId, queryRequest.EventId, AIEventTypes.ParticipantNQL.ToString(),
                queryRequest.UserId, requestId);

            if (!tokenUsageResponse.Success)
            {
                return await CreateBadResponse(req, HttpStatusCode.NotAcceptable, new SchemaManagementResult
                {
                    Success = false,
                    Message = tokenUsageResponse.Message,
                    TokensUsed = 0,
                    RequestId = requestId
                });
            }
            // Process the natural language query and track tokens
            var (result, tokensUsed) = await ProcessNaturalLanguageQuery(queryRequest, openAIClient, searchClient, queryRequest.Query);
            totalTokensUsed = tokensUsed;

            // Log successful token usage
            await base.LogTokenUsageAsync(applicationId: queryRequest.ApplicationId,
                eventId: queryRequest.EventId, userId: userId, totalTokens: totalTokensUsed, eventType: AIEventTypes.ParticipantNQL.ToString(),
                isSuccessful: result.Success, errorMessage: result.Success ? null : result.Error, requestId: requestId);
            // Create successful response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            var jsonResponse = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await response.WriteStringAsync(jsonResponse);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query");

            // Log failed token usage (if any tokens were used before failure)
            if (totalTokensUsed > 0)
            {
                await base.LogTokenUsageAsync(applicationId: queryRequest.ApplicationId,
               eventId: queryRequest.EventId, userId: userId, totalTokens: totalTokensUsed, eventType: AIEventTypes.ParticipantNQL.ToString(),
                isSuccessful: false, errorMessage: ex.Message, requestId: requestId);
            }
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error processing query: {ex.Message}");
            return errorResponse;
        }
    }

    /// <summary>
    /// Main processing method that handles the complete natural language query workflow.
    /// Steps: 1) Generate query embedding, 2) Search schema, 3) Generate SQL, 4) Return results
    /// Returns both the query result and total tokens used.
    /// </summary>
    /// <param name="openAIClient">Azure OpenAI Client</param>
    /// <param name="indexClient">Azure Search Index Client</param>
    /// <param name="naturalLanguageQuery">User's natural language question</param>
    /// <returns>Tuple containing query result and tokens used</returns>
    private async Task<(QueryResult result, int tokensUsed)> ProcessNaturalLanguageQuery(
      QueryRequest _queryRequest, AzureOpenAIClient openAIClient, SearchIndexClient indexClient, string naturalLanguageQuery)
    {
        var totalTokensUsed = 0;

        try
        {
            _logger.LogInformation($"Processing Natural Language Query: {naturalLanguageQuery}");
            _logger.LogInformation("Step 1: Generating query embedding...");

            float[] queryEmbedding = await _embeddingCache.GetOrAddEmbeddingAsync(naturalLanguageQuery, openAIClient);
            // Step 1.5: Try semantic cache for similar SQL
            var cacheHit = await _embeddingCache.FindMostSimilarSqlAsync(queryEmbedding, 0.95);
            if (cacheHit != null)
            {
                _logger.LogInformation($"Semantic cache hit! (similarity: {cacheHit.Value.similarity}) Returning cached SQL.");
                var _data1 = await GetData(_queryRequest, cacheHit.Value.sql);
                // Step 4: Get updated usage stats
                var updatedStats1 = await _tokenUsageHelper.GetUsageStatsAsync(_queryRequest.ApplicationId);
                var queryResult1 = new QueryResult
                {
                    Query = naturalLanguageQuery,
                    GeneratedSQL = cacheHit.Value.sql,
                    Success = true,
                    Data = _data1.Data,
                    Columns = _data1.Columns,
                    Error = _data1.ErrorMessage,
                    UsageStats = updatedStats1,
                };
                return (queryResult1, totalTokensUsed);
            }

            //var queryEmbedding = queryEmbeddingResponse.Value.ToFloats().ToArray();

            // Track tokens used for query embedding (estimate: ~1 token per 4 characters)
            var embeddingTokens = (int)Math.Ceiling(naturalLanguageQuery.Length / 4.0);
            totalTokensUsed += embeddingTokens;

            _logger.LogInformation($"✓ Query embedding generated (size: {queryEmbedding.Length}, tokens: {embeddingTokens})");

            // Step 2: Search for relevant schema using vector similarity
            _logger.LogInformation("Step 2: Searching for relevant schema...");
            var searchClient = indexClient.GetSearchClient(AZURE_SEARCH_INDEX_NAME);

            var vectorQuery = new VectorizedQuery(queryEmbedding)
            {
                KNearestNeighborsCount = 5,
                Fields = { "participantsearchembedding" }
            };

            var searchOptions = new SearchOptions
            {
                VectorSearch = new VectorSearchOptions
                {
                    Queries = { vectorQuery }
                },
                Size = 5,
                Select = { "tableName", "columns", "relationships", "description" },
                SearchMode = SearchMode.All,
            };

            var searchResults = await searchClient.SearchAsync<SchemaDocument>(
                searchText: naturalLanguageQuery, // Use wildcard to match all documents
                options: searchOptions);

            var relevantSchema = new List<SchemaDocument>();

            _logger.LogInformation("Relevant schema found:");

            await foreach (var result in searchResults.Value.GetResultsAsync())
            {
                relevantSchema.Add(result.Document);
            }
            // Step 3: Build context for GPT
            _logger.LogInformation("Step 3: Building context for GPT...");
            var schemaContext = BuildParticipantSchemaContext(relevantSchema);

            // Step 4: Generate SQL using GPT and track tokens
            _logger.LogInformation("Step 4: Generating SQL with GPT...");
            var (sqlQuery, sqlTokens) = await GenerateSQLWithGPT(_queryRequest, openAIClient, naturalLanguageQuery, schemaContext);
            totalTokensUsed += sqlTokens;


            // Store embedding and SQL in cache for future semantic hits
            await _embeddingCache.StoreEmbeddingWithSqlAsync(naturalLanguageQuery, queryEmbedding, sqlQuery);

            await _tokenUsageHelper.LogTokenUsageAsync(_queryRequest.ApplicationId, _queryRequest.EventId, _queryRequest.UserId, sqlTokens,
                AIEventTypes.ParticipantNQL.ToString(), true, null, "");

            // Step 4: Get updated usage stats
            var updatedStats = await _tokenUsageHelper.GetUsageStatsAsync(_queryRequest.ApplicationId);

            // Step 5: Validate SQL for security
            _logger.LogInformation("Step 5: Validating SQL...");
            if (!ValidateSQL(sqlQuery))
            {
                throw new InvalidOperationException("Generated SQL failed validation");
            }
            var _data = await GetData(_queryRequest, sqlQuery);
            var queryResult = new QueryResult
            {
                Query = naturalLanguageQuery,
                GeneratedSQL = sqlQuery,
                Success = true,
                Data = _data.Data,
                Columns = _data.Columns,
                Error = _data.ErrorMessage,
                UsageStats = updatedStats,
            };
            _logger.LogInformation($"✓ Query processed successfully. Total tokens used: {totalTokensUsed}");
            return (queryResult, totalTokensUsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing natural language query");

            var errorResult = new QueryResult
            {
                Query = naturalLanguageQuery,
                Success = false,
                Error = ex.Message
            };

            return (errorResult, totalTokensUsed);
        }
    }




    /// <summary>
    /// Builds comprehensive schema context for GPT prompt.
    /// Creates detailed documentation of relevant database schema for SQL generation.
    /// </summary>
    /// <param name="relevantSchema">List of relevant schema documents from vector search</param>
    /// <returns>Formatted schema context string for GPT prompt</returns>
    /// <summary>
    /// Builds comprehensive schema context for GPT prompt.
    /// Creates detailed documentation of relevant database schema for SQL generation.
    /// </summary>
    /// <param name="relevantSchema">List of relevant schema documents from vector search</param>
    /// <returns>Formatted schema context string for GPT prompt</returns>
    string BuildParticipantSchemaContext(List<SchemaDocument> relevantSchema)
    {
        var context = new StringBuilder();
        context.AppendLine("Participant Database Schema Context:");
        context.AppendLine("=====================================");
        context.AppendLine();
        context.AppendLine("This database manages participants in various applications/events with the following structure:");
        context.AppendLine();

        // Group by table name (should be one doc per table, but this is robust)
        var tableGroups = relevantSchema.GroupBy(s => s.TableName).OrderBy(g => g.Key);

        foreach (var tableGroup in tableGroups)
        {
            var tableDoc = tableGroup.First(); // There should be only one per table

            context.AppendLine($"Table: {tableDoc.TableName}");
            context.AppendLine(new string('-', tableDoc.TableName.Length + 7));

            if (tableDoc.TableName == "Participant")
            {
                context.AppendLine("Purpose: Stores individual participant information including personal details, contact info, and social media profiles");
            }
            else if (tableDoc.TableName == "Participant_ApplicationInstance")
            {
                context.AppendLine("Purpose: Links participants to specific application instances/events, tracks check-in status and platform usage");
            }
            else
            {
                context.AppendLine($"Purpose: {tableDoc.Description}");
            }

            context.AppendLine("Columns:");

            // Order columns: PKs first, then FKs, then others
            foreach (var column in tableDoc.Columns.OrderBy(c => c.IsPrimaryKey ? 0 : c.IsForeignKey ? 1 : 2))
            {
                var constraints = new List<string>();
                if (column.IsPrimaryKey) constraints.Add("PK");
                if (column.IsForeignKey) constraints.Add("FK");
                if (!column.IsNullable) constraints.Add("NOT NULL");
                if (column.IsIdentity) constraints.Add("IDENTITY");

                var constraintStr = constraints.Any() ? $" ({string.Join(", ", constraints)})" : "";
                context.AppendLine($"  - {column.ColumnName}: {column.DataType}{constraintStr}");
                context.AppendLine($"    Description: {column.Description}");
                if (!string.IsNullOrEmpty(column.BusinessContext))
                {
                    context.AppendLine($"    Business Context: {column.BusinessContext}");
                }
                context.AppendLine();
            }
            context.AppendLine();
        }

        // Add relationship information
        context.AppendLine("Table Relationships:");
        context.AppendLine("-------------------");

        foreach (var tableDoc in relevantSchema)
        {
            foreach (var rel in tableDoc.Relationships)
            {
                context.AppendLine($"- {rel.FromTable}.{rel.FromColumn} → {rel.ToTable}.{rel.ToColumn} ({rel.Description})");
            }
        }
        context.AppendLine();

        // Add common query patterns
        context.AppendLine("Common Query Patterns:");
        context.AppendLine("---------------------");
        context.AppendLine("- Use JOINs when querying across both tables");
        context.AppendLine("- Filter by IsDeleted = 0 for active records (if present)");
        context.AppendLine("- Use CheckInStatus for event attendance queries (if present)");
        context.AppendLine("- Platform tracking via JoinedByIOS, JoinedByAndroid, JoinedByPWA columns (if present)");
        context.AppendLine("- Social media queries use LinkedInPublicProfileUrl, etc. (if present)");

        return context.ToString();
    }


    /// <summary>
    /// Generates SQL query using GPT-4 based on natural language input and schema context.
    /// Includes comprehensive security rules and SQL Server syntax requirements.
    /// Returns both the generated SQL and tokens used.
    /// </summary>
    /// <param name="openAIClient">Azure OpenAI Client</param>
    /// <param name="naturalLanguageQuery">User's natural language question</param>
    /// <param name="schemaContext">Database schema context</param>
    /// <returns>Tuple containing generated SQL and tokens used</returns>
    private async Task<(string sql, int tokensUsed)> GenerateSQLWithGPT(QueryRequest _queryRequest, AzureOpenAIClient openAIClient, string naturalLanguageQuery, string schemaContext)
    {
        _logger.LogInformation("Building comprehensive GPT prompt for SQL generation...");
        var sb = new StringBuilder();
        foreach (var ex in ParticipantSchema.GetPrompt())
        {
            sb.AppendLine($"-- Query: {ex.nl}");
            sb.AppendLine(ex.sql);
            sb.AppendLine();
        }


        // Comprehensive system prompt with security rules and SQL Server syntax
        var systemPrompt = @$"
                            You are an expert SQL developer specializing in participant management systems. Your task is to convert natural language questions into precise, secure, and optimized SQL queries using the provided database schema.


                            IMPORTANT: If @EventId is provided and greater than 0, YOU MUST join Participant with Participant_ApplicationInstance ON p.Id = pai.ParticipantId and include pai.ApplicationInstanceId = @EventId in the WHERE clause. This is mandatory for every query.

                            INPUT PARAMETERS:
                                @ApplicationId = {_queryRequest.ApplicationId}
                                @EventId  = {_queryRequest.EventId}

                            CRITICAL SECURITY RULES (MANDATORY):
                            1. ONLY generate SELECT statements - absolutely NO INSERT, UPDATE, DELETE, DROP, CREATE, ALTER, TRUNCATE, EXEC, EXECUTE, or any DDL/DML operations.
                            2. Never include SQL comments (-- or /* */) to prevent injection attacks.
                            3. Use parameterized approaches where possible.
                            4. Validate all inputs and use proper escaping.
                            5. No dynamic SQL construction or EXEC statements.
                            6. No system stored procedures (sp_, xp_) or administrative functions.

                            SQL SERVER SYNTAX REQUIREMENTS:
                            1. Use proper SQL Server T-SQL syntax and functions.
                            2. Use meaningful table aliases: 'p' for Participant, 'pai' for Participant_ApplicationInstance.
                            3. Always use square brackets [TableName] for table names if they contain special characters.
                            4. Use ISNULL() or COALESCE() for NULL handling.
                            5. Use DATEPART(), YEAR(), MONTH(), DAY() for date operations.
                            6. Use LEN() instead of LENGTH() for string length.
                            7. Use CHARINDEX() for string searching.
                            8. Use TOP N instead of LIMIT for result limiting.

                            PARTICIPANT SYSTEM BUSINESS RULES:
                            1. ALWAYS filter out soft-deleted records:
                               - Use 'IsDeleted = 0' for Participant table.
                               - Use 'isDeleted = 0' for Participant_ApplicationInstance table (note lowercase 'i').
                               - Use 'isDeleted = 0' for ApplicationInstance table.
                            2. For active participants: WHERE p.IsDeleted = 0.
                            3. For active relationships: WHERE pai.isDeleted = 0.
                            4. When joining both tables, filter both: WHERE p.IsDeleted = 0 AND pai.isDeleted = 0.
                            5. **ALWAYS include 'p.ApplicationId = @ApplicationId' in the WHERE clause of every query, regardless of other filters.**
                            6. **If @EventId is provided and greater than 0:
                                ALWAYS join the Participant table (p) with the Participant_ApplicationInstance table (pai) ON p.ParticipantId = pai.ParticipantId.
                                ALWAYS include the filter pai.ApplicationInstanceId = @EventId in the WHERE clause.
                                ALWAYS Apply all other relevant filters to both tables as needed.
                                If @EventId is not provided or is 0 or less:
                                Do NOT join with Participant_ApplicationInstance.
                                Do NOT include any EventId filter.

                            RESPONSE FORMAT:
                            - Return ONLY the SQL query without any explanations, markdown formatting, or additional text.
                            - Ensure the query is properly formatted and ready to execute.
                            - Use proper indentation and line breaks for readability.
                            - End with a semicolon if it's a complete statement.

                            DATABASE SCHEMA CONTEXT:
                            {schemaContext}
                            {ParticipantSchema.SystemPrompt()}
                            {sb}
                            {ParticipantSchema.GetParticipantPromptExamples()}";


        try
        {
            _logger.LogInformation("Sending request to GPT-4o for SQL generation...");


            var chatClient = openAIClient.GetChatClient(AZURE_OPENAI_CHAT_MODEL);

            var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage($@"Convert this natural language query to SQL:

                    Query: ""{naturalLanguageQuery}""

                    Requirements:
                    1. Generate a secure SELECT-only query
                    2. Follow all participant system business rules
                    3. Use proper SQL Server syntax
                    4. Include appropriate filtering for soft-deleted records
                    5. Use meaningful aliases and proper formatting
                    6. Consider performance and include relevant ORDER BY clauses
                    7. Handle NULL values appropriately

                    Return only the SQL query.")
                };

            var chatOptions = new ChatCompletionOptions
            {
                Temperature = 0.1f, // Low temperature for consistent, precise SQL generation
                TopP = 0.95f,       // Focused responses
                FrequencyPenalty = 0.0f,
                PresencePenalty = 0.0f
            };

            var response = await chatClient.CompleteChatAsync(messages, chatOptions);

            // Calculate tokens used (estimate based on prompt and response length)
            var promptLength = systemPrompt.Length + naturalLanguageQuery.Length + 200; // Additional prompt text
            var responseLength = response?.Value?.Content?.FirstOrDefault()?.Text?.Length ?? 0;
            var estimatedTokens = (int)Math.Ceiling((promptLength + responseLength) / 4.0);

            if (response?.Value?.Content?.Count > 0)
            {
                var sqlQuery = response.Value.Content[0].Text?.Trim();

                if (string.IsNullOrWhiteSpace(sqlQuery))
                {
                    throw new InvalidOperationException("GPT returned empty SQL query");
                }

                sqlQuery = CleanSQLResponse(sqlQuery);
                _logger.LogInformation($"✓ SQL query generated successfully (tokens: {estimatedTokens})");
                return (sqlQuery, estimatedTokens);
            }
            else
            {
                throw new InvalidOperationException("GPT response was null or empty");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SQL with GPT");

            return (null, 0); // No tokens used for fallback
        }
    }

    /// <summary>
    /// Cleans SQL response from GPT by removing markdown formatting and ensuring proper structure.
    /// </summary>
    /// <param name="sqlResponse">Raw SQL response from GPT</param>
    /// <returns>Cleaned SQL query</returns>
    private string CleanSQLResponse(string sqlResponse)
    {
        if (string.IsNullOrWhiteSpace(sqlResponse))
            return string.Empty;

        // Remove markdown code blocks
        sqlResponse = sqlResponse.Replace("```sql", "").Replace("```", "").Trim();

        // Remove any leading/trailing quotes
        sqlResponse = sqlResponse.Trim('"', '\'');

        // Ensure it ends with semicolon if it doesn't already
        if (!sqlResponse.TrimEnd().EndsWith(";"))
        {
            sqlResponse = sqlResponse.TrimEnd() + ";";
        }
        sqlResponse = Regex.Unescape(sqlResponse); // handles \n, \t etc.
        sqlResponse = Regex.Replace(sqlResponse, @"\s+", " "); // normalize spaces
        return sqlResponse;
    }

    /// <summary>
    /// Validates SQL query for security compliance.
    /// Ensures only SELECT statements and prevents SQL injection attacks.
    /// </summary>
    /// <param name="sql">SQL query to validate</param>
    /// <returns>True if SQL is safe to execute</returns>
    private bool ValidateSQL(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            _logger.LogWarning("SQL validation failed: Empty query");
            return false;
        }

        var sqlUpper = sql.ToUpper().Trim();

        // Must start with SELECT
        if (!sqlUpper.StartsWith("SELECT"))
        {
            _logger.LogWarning("SQL validation failed: Query must start with SELECT");
            return false;
        }

        // Forbidden keywords that could be dangerous
        var forbiddenKeywords = new[]
        {
                "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER",
                "TRUNCATE", "EXEC", "EXECUTE", "SP_", "XP_", "OPENROWSET",
                "BULK", "MERGE", "GRANT", "REVOKE", "DENY", "BACKUP",
                "RESTORE", "SHUTDOWN", "DBCC", "KILL", "WAITFOR"
            };

        foreach (var keyword in forbiddenKeywords)
        {
            // Use regex for whole-word match (e.g., DELETE not IsDeleted)
            if (System.Text.RegularExpressions.Regex.IsMatch(sqlUpper, $@"\b{keyword}\b"))
            {
                _logger.LogWarning($"SQL validation failed: Forbidden keyword detected: {keyword}");
                return false;
            }
        }

        // Check for SQL injection patterns
        if (sqlUpper.Contains("--") || sqlUpper.Contains("/*") || sqlUpper.Contains("*/"))
        {
            _logger.LogWarning("SQL validation failed: SQL comments detected");
            return false;
        }

        _logger.LogInformation("SQL validation passed all security checks");
        return true;
    }


    private async Task<SqlQueryResult> GetData(QueryRequest queryResult, string sqlQuery)
    {

        var data = await _sqlHelper.ExecuteSqlAndGetResultsAsync(queryResult.ApplicationId, queryResult.EventId, sqlQuery);
        return data;

    }

    private async Task<HttpResponseData> CreateBadResponse(HttpRequestData req, HttpStatusCode statusCode, object errorResult)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(errorResult, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
        return response;
    }
}

