# Gift of the Givers Foundation — Web Application (POE Part 1)

A C# **ASP.NET Core (.NET 8) MVC** prototype for the Gift of the Givers Foundation, built for
Part 1 of the Portfolio of Evidence. It demonstrates the look, feel and core functionality of a
disaster-relief coordination platform.

**Live demo:** https://giftofthegivers-app-st10448224-g7ekd0b8dyg6d8gp.southafricanorth-01.azurewebsites.net

## Features
- Branded, responsive UI (Bootstrap) with logo, navigation and hero section
- **ASP.NET Core Identity** authentication with two roles: **Employee** and **Donor**
- **Donations** — one-time or recurring, in ZAR / USD / EUR, with a downloadable **PDF tax certificate** (QuestPDF); anonymous guest donations supported
- **Volunteer** registration form (name, skills, availability)
- **Employee dashboard** (role-restricted) — post relief-project updates and view volunteer sign-ups

## Tech stack
- ASP.NET Core 8 MVC · Entity Framework Core
- **SQLite** local database (schema created from EF migrations on first run)
- QuestPDF for tax-certificate generation
- Deployed to **Azure App Service** (South Africa North); a matching schema is designed in **Azure SQL Database** (see `../GiftOfTheGivers_Schema.sql`)

## Demo accounts
| Role | Email | Password |
|------|-------|----------|
| Employee | employee@giftofthegivers.org | Employee#123 |
| Donor | donor@example.com | Donor#123 |

## Run locally
```bash
dotnet restore
dotnet run
```
The app creates and seeds `app.db` automatically on first run.
