# Development guide

## Requirements

- Windows 10 or Windows 11;
- .NET SDK selected by `global.json`;
- Git;
- an editor with C# and WPF support.

## Initial setup

```powershell
dotnet tool restore
dotnet restore DataCleaner.slnx
dotnet build DataCleaner.slnx --configuration Release --no-restore
dotnet test DataCleaner.slnx --configuration Release --no-build --no-restore
```

## Run the application

```powershell
dotnet run --project src/DataCleaner.App/DataCleaner.App.csproj
```

Application state is stored under `%LOCALAPPDATA%\DataCleaner`. Imported business datasets must not be copied there unless a future feature explicitly requires it and the data-safety documentation is updated.

## Regenerating portfolio screenshots

The portfolio capture tool opens the real WPF window against an isolated SQLite database, runs the synthetic import, profiling, validation, cleaning, duplicate and export workflow, renders each selected view directly to PNG, and closes automatically. It is not included in the release package.

```powershell
dotnet run --project tools/DataCleaner.PortfolioCapture/DataCleaner.PortfolioCapture.csproj --configuration Release -- `
  --demo samples/synthetic-customers.csv `
  --capture-portfolio docs/images `
  --data-directory artifacts/portfolio-capture-data
```

Use an empty data directory for every verification run. The capture performs a warm-up render before writing each final image.

## EF Core migrations

Restore the repository-local tool before using EF commands:

```powershell
dotnet tool restore
```

Create a migration:

```powershell
dotnet ef migrations add <MigrationName> `
  --project src/DataCleaner.Infrastructure/DataCleaner.Infrastructure.csproj `
  --startup-project src/DataCleaner.Infrastructure/DataCleaner.Infrastructure.csproj `
  --output-dir Persistence/Migrations
```

Migrations are applied at application startup. Every schema change requires an integration test or an update to the existing database initialization test.

## Coding conventions

- Nullable reference types remain enabled.
- Compiler and analyzer warnings are treated as errors.
- Domain code cannot reference WPF, EF Core or file-format libraries.
- Infrastructure implementations depend on Application abstractions.
- Asynchronous APIs accept a `CancellationToken` when work can block or scale with file size.
- Logs use message templates and must not contain imported row values by default.
- Public behavior and architectural changes update documentation in the same pull request.

## Branch and commit convention

Suggested branch names:

```text
feature/123-csv-delimiter-detection
fix/245-locked-file-message
docs/architecture-import-pipeline
chore/update-ef-core
```

Use concise imperative commits. Conventional Commit prefixes are recommended:

```text
feat: add CSV delimiter detection
fix: preserve quoted empty fields
test: cover Dutch decimal parsing
docs: clarify duplicate comparison rules
chore: update EF Core packages
```

## Local completion check

Before opening a pull request:

```powershell
dotnet restore DataCleaner.slnx
dotnet build DataCleaner.slnx --configuration Release --no-restore
dotnet test DataCleaner.slnx --configuration Release --no-build --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Test-Documentation.ps1
```
