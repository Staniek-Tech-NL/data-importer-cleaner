# Data Importer & Cleaner

Data Importer & Cleaner is a Windows desktop application for turning inconsistent CSV and Excel files into clean, validated and reusable business data through a safe, reviewable workflow.

> [!IMPORTANT]
> The project is under active development. Milestones M1–M6 are complete; export and processing history are next. There is no public end-user release yet.

## Why this project exists

Business data often arrives with inconsistent headers, whitespace, date formats, decimal separators, country names, missing values and duplicates. Fixing these files manually is slow and difficult to audit. This project provides a deterministic desktop workflow that preserves the source data and makes every transformation reviewable before export.

The repository is also a portfolio case study in layered .NET desktop development, data processing, validation, persistence, automated testing and release engineering.

## Planned workflow

```text
Import → Preview → Profile → Map → Pre-validation → Clean
       → Post-validation → Deduplicate → Review → Export
```

Validation and cleaning remain separate. The validation engine can run before and after cleaning so that users can distinguish source problems from issues that remain in the final dataset.

## Project status

| Area | Status | Target milestone |
| --- | --- | --- |
| Layered solution and WPF shell | Complete | M1 |
| Dependency injection and logging | Complete | M1 |
| SQLite model and initial migration | Complete | M1 |
| Domain foundations and processing contracts | Complete | M1 |
| CSV import and preview | Complete | M2 |
| XLSX import and worksheet selection | Complete | M2 |
| Data profiling and column mapping | Complete | M3 |
| Validation engine | Complete | M4 |
| Cleaning engine and before/after review | Complete | M5 |
| Deterministic duplicate detection | Complete | M6 |
| CSV/XLSX/SQLite export and history | Planned | M7 |
| Portfolio release | Planned | M8 |

See the [roadmap](docs/roadmap.md) and [project plan](docs/project-plan.md) for scope and delivery details.

## Core engineering principles

- **Immutable source:** the input file is never modified or overwritten.
- **Traceable values:** every cell preserves its source, parsed and current value.
- **Deterministic behavior:** transformations and duplicate matching are predictable and reproducible.
- **Explicit culture:** ambiguous numbers and dates are never silently interpreted without culture settings.
- **Review before export:** users can inspect original and transformed values before producing output.
- **Privacy by default:** imported row contents are not persisted or logged by default.
- **Focused MVP:** AI cleaning, fuzzy matching, cloud sync and multi-user features remain outside v1.0.

## Architecture

```text
DataCleaner.App ────────> DataCleaner.Application ────────> DataCleaner.Domain
       │                           ▲
       └────> DataCleaner.Infrastructure ─────────────────> DataCleaner.Domain
```

- `DataCleaner.Domain` contains framework-independent business concepts.
- `DataCleaner.Application` declares workflows and ports for files and persistence.
- `DataCleaner.Infrastructure` contains EF Core, SQLite and CSV/XLSX adapters.
- `DataCleaner.App` is the WPF composition and presentation layer.

The domain avoids names such as `DataSet` and `DataRow` that collide with `System.Data`. Technical data types and semantic types are modeled separately. More detail is available in [Architecture](docs/architecture.md) and [Domain model](docs/domain-model.md).

## Technology

- .NET 10 and C#
- WPF with an MVVM-oriented presentation layer
- Microsoft.Extensions.Hosting, dependency injection and logging
- Entity Framework Core and SQLite
- Open XML SDK for XLSX workbooks
- xUnit and coverlet
- GitHub Actions and Dependabot

## Repository structure

```text
src/
  DataCleaner.App/
  DataCleaner.Application/
  DataCleaner.Domain/
  DataCleaner.Infrastructure/

tests/
  DataCleaner.Application.Tests/
  DataCleaner.Domain.Tests/
  DataCleaner.Infrastructure.Tests/

docs/
  decisions/
  architecture.md
  business-rules.md
  data-safety.md
  development.md
  domain-model.md
  project-plan.md
  roadmap.md
  testing-strategy.md
```

## Prerequisites

- Windows 10 or Windows 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PowerShell 7 or Windows PowerShell
- Git, if you intend to contribute

The required SDK feature band is pinned in [`global.json`](global.json).

## Getting started

```powershell
git clone <repository-url>
cd data-importer-cleaner
dotnet tool restore
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build --no-restore
dotnet run --project src/DataCleaner.App/DataCleaner.App.csproj
```

Replace `<repository-url>` after the repository has been published. The application creates its local SQLite database under the current user's local application-data directory.

## Quality gates

Every pull request is expected to meet these checks:

- restore succeeds;
- Release build produces zero warnings and zero errors;
- all automated tests pass;
- no real customer or production data is committed;
- user-visible or architectural changes update the relevant documentation;
- the changelog is updated when the change is release-relevant.

## Documentation

Start with the [documentation index](docs/README.md). Key documents include:

- [Project plan](docs/project-plan.md)
- [Roadmap](docs/roadmap.md)
- [Architecture](docs/architecture.md)
- [Business rules](docs/business-rules.md)
- [Data safety](docs/data-safety.md)
- [Development guide](docs/development.md)
- [Testing strategy](docs/testing-strategy.md)
- [Release process](docs/release-process.md)

## Contributing and support

Contributions are welcome once the repository is public. Read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request. Use the supplied GitHub issue forms for reproducible bugs, scoped feature proposals and documentation improvements.

For help choosing the correct channel, see [SUPPORT.md](SUPPORT.md). Please report security vulnerabilities privately as described in [SECURITY.md](SECURITY.md).

## License

A license has not been selected yet. Until a license file is added, the source is not offered under an open-source license. Selecting and adding a license is a required step before the first public release.
