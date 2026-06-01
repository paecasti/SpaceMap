# SpaceMap

Desktop app to map the space used on your disk. SpaceMap is a Windows desktop application built with .NET 8 and WPF for visual disk-usage analysis.

## Workspace

- `src/SpaceMap.App`: WPF shell, views, and view models.
- `src/SpaceMap.Core`: domain models, DTOs, and use cases.
- `src/SpaceMap.Infrastructure`: scanning, persistence, observability, and native shell services.
- `tests/`: core, integration, and desktop tests.

## Commands

```powershell
dotnet restore
dotnet build SpaceMap.sln
dotnet test SpaceMap.sln
dotnet run --project src/SpaceMap.App/SpaceMap.App.csproj
```
