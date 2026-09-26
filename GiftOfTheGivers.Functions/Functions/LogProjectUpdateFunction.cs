using System.Globalization;
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

    /// <summary>
    /// UpdateId is the primary key of the ProjectUpdates row the web app has just saved. It is
    /// carried into the audit log so a row here can be tied back to the relational record; without
    /// it the two stores can only be matched on a fuzzy combination of project, author and text.
    /// It is optional so that a caller which has not saved to the database (for example a manual
    /// Postman test) is still accepted.
    /// </summary>
    public record LogUpdateRequest(
        int ProjectId,
        string? ProjectName,
        string? AuthorName,
        string Body,
        int? UpdateId = null);

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

        var loggedAtUtc = DateTime.UtcNow;

        var entity = new TableEntity(
            partitionKey: request.ProjectId.ToString(CultureInfo.InvariantCulture),
            rowKey: BuildRowKey(loggedAtUtc))
        {
            { "ProjectName", request.ProjectName ?? "Unknown project" },
            { "AuthorName", request.AuthorName ?? "Unknown" },
            { "Body", request.Body },
            { "LoggedAtUtc", loggedAtUtc },
            { "UpdateId", request.UpdateId }
        };

        await tableClient.AddEntityAsync(entity);

        _logger.LogInformation(
            "Logged project update {UpdateId} for project {ProjectId} to Azure Table Storage as {RowKey}.",
            request.UpdateId, request.ProjectId, entity.RowKey);

        return new OkObjectResult(new
        {
            status = "logged",
            partitionKey = entity.PartitionKey,
            rowKey = entity.RowKey,
            updateId = request.UpdateId
        });
    }

    /// <summary>
    /// Builds a row key that sorts newest-first inside the project's partition.
    /// </summary>
    /// <remarks>
    /// Table Storage always returns a partition's rows in ascending row-key order and offers no
    /// "order by" of its own, so a random GUID key means the audit log for a project comes back in
    /// arbitrary order and the most recent entries can only be found by reading every row.
    /// Prefixing the key with the descending tick count is the standard Table Storage pattern for
    /// a time series: the newest row sorts first, so "the last N updates for this project" becomes
    /// a top-N query. The GUID suffix keeps the key unique when two updates land on the same tick.
    /// </remarks>
    private static string BuildRowKey(DateTime loggedAtUtc)
    {
        var descendingTicks = DateTime.MaxValue.Ticks - loggedAtUtc.Ticks;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{descendingTicks:D19}-{Guid.NewGuid()}");
    }
}
