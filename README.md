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
- Solution builds clean (0 errors/0 warnings); 8/8 xUnit tests pass.
- Both functions verified locally via HTTP and via the web app's donation and employee-update forms.
- Local build references `GiftOfTheGivers.Helpers` as a project reference; Section D.2 swaps this
  for the published Azure Artifacts package (`<PackageReference Include="GiftOfTheGivers.Helpers" Version="1.0.0" />`).

Azure Repos push, branches/PRs, the Pipeline run, the Artifacts feed/publish, and the Function
App deployment are completed in the team's own Azure DevOps / Azure portals.
