using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversApp.Models;

namespace GiftOfTheGiversApp.Data;

public static class SeedData
{
    public const string EmployeeRole = "Employee";
    public const string DonorRole = "Donor";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { EmployeeRole, DonorRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        // Demo Employee account
        await EnsureUserAsync(userManager, "employee@giftofthegivers.org", "Employee#123", EmployeeRole);
        // Demo Donor account
        await EnsureUserAsync(userManager, "donor@example.com", "Donor#123", DonorRole);

        // Demo relief projects with an update
        if (!await db.ReliefProjects.AnyAsync())
        {
            var kzn = new ReliefProject
            {
                Name = "KZN Flood Relief",
                Description = "Emergency food and shelter for flood-affected families.",
                Location = "KwaZulu-Natal",
                Status = "Active",
                StartDate = new DateTime(2026, 9, 1)
            };
            var cape = new ReliefProject
            {
                Name = "Cape Fire Response",
                Description = "Support for communities affected by wildfires.",
                Location = "Western Cape",
                Status = "Active",
                StartDate = new DateTime(2026, 9, 10)
            };
            db.ReliefProjects.AddRange(kzn, cape);
            await db.SaveChangesAsync();

            db.ProjectUpdates.Add(new ProjectUpdate
            {
                ProjectId = kzn.ProjectId,
                AuthorName = "employee@giftofthegivers.org",
                Body = "Distributed 200 food parcels in the first week of the KZN flood response."
            });
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureUserAsync(UserManager<IdentityUser> userManager,
        string email, string password, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            await userManager.CreateAsync(user, password);
        }
        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);
    }
}
