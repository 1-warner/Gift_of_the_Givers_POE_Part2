# Gift of the Givers Foundation — POE Part 2

Part 2 of the Applied Programming (APPR6312) Portfolio of Evidence. It extends the Part 1
ASP.NET Core (.NET 8) web application with Azure Functions, a shared NuGet package, an Azure
Pipelines CI definition, and Azure Artifacts packaging.

Part 1 repo: https://github.com/1-warner/Gift_of_the_Givers_POE_Part1

## Solution layout
| Project | Purpose |
|---|---|
| `Part1/GiftOfTheGiversApp.csproj` | The Part 1 web app, patched to call the Functions (Section A) and use the Helpers package (Section D). |
| `GiftOfTheGivers.Functions/` | Two HTTP-triggered Azure Functions (isolated worker, .NET 8): `GenerateTaxCertificate`, `LogProjectUpdate`. |
| `GiftOfTheGivers.Helpers/` | Shared class library packaged as a NuGet package (`TaxCertificateHelper`, `DonationTotalsHelper`). |
| `GiftOfTheGivers.Tests/` | xUnit tests for the Helpers library (run by CI). |
| `azure-pipelines.yml` | Azure Pipelines CI: restore, build, test, publish results. |
| `nuget.config` | NuGet feed config (Azure Artifacts feed left as a template — set ORG/FEED). |

## Build & run locally
```bash
dotnet restore GiftOfTheGivers.Solution.sln
dotnet build   GiftOfTheGivers.Solution.sln
dotnet test    GiftOfTheGivers.Tests/GiftOfTheGivers.Tests.csproj
```
To run the Functions locally: start Azurite (storage emulator), copy
`GiftOfTheGivers.Functions/local.settings.json.example` to `local.settings.json`, then
`func start` in the Functions project while the web app runs (web app `Functions:BaseUrl`
points at `http://localhost:7071/api/`).

## Status
- Solution builds clean (0 errors); 12/12 xUnit tests pass.
- Both functions verified locally via HTTP (8 cases, including error paths) and via the web app's
  donation and employee-update forms.
- Code is in Azure Repos: `ST10448224/GiftOfTheGivers-Relief`. Three feature branches were merged
  into `main` through pull requests !1, !2 and !3.
- CI runs on every push to `main` and on pull requests: restore, build, test, pack, publish.
- `GiftOfTheGivers.Helpers` 1.0.0 is published to the Azure Artifacts feed
  `GiftOfTheGivers-Packages` by the pipeline itself, using the build-service identity.
- The web app consumes it as a package, not a project reference:
  `<PackageReference Include="GiftOfTheGivers.Helpers" Version="1.0.0" />`.

### Restoring the private feed
`nuget.config` points at the project-scoped Azure Artifacts feed, so a restore needs credentials:

| Where | How |
|---|---|
| Azure Pipelines | `NuGetAuthenticate@1` task, using the pipeline's build-service identity |
| Visual Studio | prompts to sign in on the first restore |
| Command line | set `AZURE_ARTIFACTS_PAT` to a PAT with **Packaging (Read)** scope |

A `packageSourceMapping` entry restricts the private feed to `GiftOfTheGivers.*`, so every other
dependency still resolves from nuget.org and cannot be shadowed by the private feed.
