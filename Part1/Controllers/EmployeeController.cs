using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;

namespace GiftOfTheGiversApp.Controllers;

[Authorize(Roles = SeedData.EmployeeRole)]
public class EmployeeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(
        ApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<EmployeeController> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IActionResult> Dashboard()
    {
        ViewBag.Volunteers = await _db.Volunteers.OrderByDescending(v => v.RegisteredAt).ToListAsync();
        ViewBag.Projects = await _db.ReliefProjects.ToListAsync();
        ViewBag.Updates = await _db.ProjectUpdates.Include(u => u.Project)
            .OrderByDescending(u => u.PostedAt).ToListAsync();
        ViewBag.DonationCount = await _db.Donations.CountAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostUpdate(int projectId, string body)
    {
        if (projectId > 0 && !string.IsNullOrWhiteSpace(body))
        {
            var project = await _db.ReliefProjects.FindAsync(projectId);

            var update = new ProjectUpdate
            {
                ProjectId = projectId,
                Body = body,
                AuthorName = User.Identity?.Name,
                PostedAt = DateTime.UtcNow
            };

            _db.ProjectUpdates.Add(update);
            await _db.SaveChangesAsync();

            // A.1 evidence: mirrors the update into Azure Table Storage via the LogProjectUpdate
            // Function, independently of the SQL/SQLite write above. Best-effort - the update is
            // already safely persisted in the database by this point.
            await TryLogUpdateToFunctionAsync(update, project?.Name ?? "Unknown project");
        }
        return RedirectToAction(nameof(Dashboard));
    }

    private async Task TryLogUpdateToFunctionAsync(ProjectUpdate update, string projectName)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("FunctionsClient");

            // updateId is the ProjectUpdates primary key assigned by SaveChangesAsync above. It
            // lets a row in the Table Storage audit log be traced back to the database record.
            var payload = JsonSerializer.Serialize(new
            {
                projectId = update.ProjectId,
                projectName,
                authorName = update.AuthorName,
                body = update.Body,
                updateId = update.UpdateId
            });

            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("LogProjectUpdate", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "LogProjectUpdate function returned {StatusCode} for project {ProjectId}.",
                    response.StatusCode, update.ProjectId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not reach the LogProjectUpdate function for project {ProjectId}.", update.ProjectId);
        }
    }
}
