# SpaceMap

Aplicacion desktop para analizar visualmente el uso de disco en Windows usando .NET 8 y WPF.

## Workspace

- `src/SpaceMap.App`: shell WPF, vistas y view models.
- `src/SpaceMap.Core`: dominio, DTOs y casos de uso.
- `src/SpaceMap.Infrastructure`: escaneo, persistencia, observabilidad y shell nativo.
- `tests/`: pruebas core, integración y desktop.

## Comandos

```powershell
dotnet restore
dotnet build SpaceMap.sln
dotnet test SpaceMap.sln
dotnet run --project src/SpaceMap.App/SpaceMap.App.csproj
```
