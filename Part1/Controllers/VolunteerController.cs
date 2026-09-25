using Microsoft.AspNetCore.Mvc;
using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;

namespace GiftOfTheGiversApp.Controllers;

public class VolunteerController : Controller
{
    private readonly ApplicationDbContext _db;

    public VolunteerController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public IActionResult Index() => View(new Volunteer());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(Volunteer model)
    {
        if (!ModelState.IsValid) return View(model);

        model.RegisteredAt = DateTime.UtcNow;
        _db.Volunteers.Add(model);
        await _db.SaveChangesAsync();

        TempData["VolunteerName"] = model.FullName;
        return RedirectToAction(nameof(ThankYou));
    }

    [HttpGet]
    public IActionResult ThankYou() => View();
}
