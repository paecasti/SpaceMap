# SpaceMap

SpaceMap is a Windows desktop application for visual disk-usage analysis. It maps folders and files into a navigable view so you can quickly understand what is consuming storage on a drive or directory.

The app is designed for computers that are already under pressure: low free disk space, heavy folder trees, or limited available memory. For that reason, SpaceMap intentionally limits the mapping speed instead of scanning as aggressively as possible. This keeps memory usage more predictable and avoids making an already saturated computer worse while the scan is running.

SpaceMap focuses on safe, read-only inspection. It scans the filesystem, shows progressive results, restores the last confirmed snapshot on startup, and provides native actions such as opening a path in Explorer or copying a path to the clipboard.

## Features

- Read-only disk mapping for Windows 10/11.
- Folder-first treemap navigation.
- Progressive scan results while the mapping process is still running.
- Automatic restore of the last confirmed snapshot.
- Directory breakdown with sorting and minimum-size filtering.
- Native path actions for opening or copying selected paths.
- Conservative scan pacing to reduce RAM pressure on saturated machines.

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
