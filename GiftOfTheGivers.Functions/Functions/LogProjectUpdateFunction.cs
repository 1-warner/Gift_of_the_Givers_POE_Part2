using System.Text.Json;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GiftOfTheGivers.Functions;

/// <summary>
/// HTTP-triggered function that logs an employee-posted relief-project update to Azure Table
/// Storage. The web app's EmployeeController.PostUpdate calls this after it saves the update to
/// the SQL/SQLite database, so every update also lands in an independent, append-only audit log
/// (see Section A.1 of the POE Part 2 report).
/// </summary>
public class LogProjectUpdateFunction
{
    private const string TableName = "ProjectUpdateLog";

    private readonly ILogger<LogProjectUpdateFunction> _logger;
    private readonly IConfiguration _configuration;

    public LogProjectUpdateFunction(ILogger<LogProjectUpdateFunction> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public record LogUpdateRequest(int ProjectId, string ProjectName, string? AuthorName, string Body);

    [Function("LogProjectUpdate")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "LogProjectUpdate")] HttpRequest req)
    {
        LogUpdateRequest? request;
        try
        {
            request = await JsonSerializer.DeserializeAsync<LogUpdateRequest>(
                req.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "LogProjectUpdate received a malformed request body.");
            return new BadRequestObjectResult(new { error = "Request body must be valid JSON." });
        }

        if (request is null || request.ProjectId <= 0 || string.IsNullOrWhiteSpace(request.Body))
        {
            return new BadRequestObjectResult(new { error = "projectId and body are required." });
        }

        // Falls back to the Functions host's own storage account (AzureWebJobsStorage) locally,
        // but a dedicated ProjectUpdatesStorageConnection app setting can point Azure at a
        // separate storage account in production.
        var connectionString = _configuration["ProjectUpdatesStorageConnection"]
            ?? _configuration["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException("No storage connection string is configured for LogProjectUpdate.");

        var tableClient = new TableClient(connectionString, TableName);
        await tableClient.CreateIfNotExistsAsync();

        var entity = new TableEntity(partitionKey: request.ProjectId.ToString(), rowKey: Guid.NewGuid().ToString())
        {
            { "ProjectName", request.ProjectName },
            { "AuthorName", request.AuthorName ?? "Unknown" },
            { "Body", request.Body },
            { "LoggedAtUtc", DateTime.UtcNow }
        };

        await tableClient.AddEntityAsync(entity);

        _logger.LogInformation(
            "Logged project update for project {ProjectId} to Azure Table Storage.", request.ProjectId);

        return new OkObjectResult(new { status = "logged", partitionKey = entity.PartitionKey, rowKey = entity.RowKey });
    }
}
