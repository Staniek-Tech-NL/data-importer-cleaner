# Contributing

Thank you for helping improve Data Importer & Cleaner. The repository is public for inspection, but its all-rights-reserved license does not grant permission to copy, modify or redistribute the source.

Bug reports, feature proposals and documentation-correction reports are welcome. Submit a code or documentation change only after receiving prior written permission from the copyright holder. Opening a pull request does not grant a license to use the project outside the review process.

## Before you start

1. Read the [project plan](docs/project-plan.md), [roadmap](docs/roadmap.md) and [business rules](docs/business-rules.md).
2. Search existing issues and pull requests to avoid duplicate work.
3. Open or comment on an issue before implementing a substantial change.
4. Confirm that the proposal fits the current milestone and v1.0 scope.

Small documentation corrections should normally be reported as an issue unless the maintainer has invited a pull request.

## Development setup

Follow the [development guide](docs/development.md). The minimum local quality check is:

```powershell
dotnet restore DataCleaner.slnx
dotnet build DataCleaner.slnx --configuration Release --no-restore
dotnet test DataCleaner.slnx --configuration Release --no-build --no-restore
```

## Pull requests

Pull requests are accepted only when the maintainer has explicitly authorized the proposed change.

A pull request should:

- solve one clearly described problem;
- link the relevant issue when one exists;
- include tests for new or changed behavior;
- preserve source-file immutability and data privacy;
- update documentation and `CHANGELOG.md` when relevant;
- avoid unrelated formatting or refactoring;
- pass all CI checks with zero build warnings.

Draft pull requests are welcome for early technical feedback. Mark the pull request ready only when its acceptance criteria are satisfied.

## Coding expectations

- Keep dependencies pointing inward according to `docs/architecture.md`.
- Do not introduce WPF, EF Core or file-format dependencies into the domain layer.
- Keep validation separate from cleaning.
- Make culture-sensitive parsing explicit and testable.
- Use synthetic test data only.
- Never log imported cell or row contents by default.

## Commit and branch style

Use a focused branch such as `feature/123-csv-preview` or `fix/245-locked-file`. Conventional Commit-style subjects are recommended, for example `feat: add worksheet discovery`.

## Reporting bugs

Use the bug report form and provide a minimal synthetic reproduction. Never attach real customer files, credentials, personal data, local databases or sensitive logs.

## Proposing features

Explain the business problem before suggesting an implementation. Features outside the documented MVP will normally be assigned to a post-1.0 milestone rather than added to the active milestone.

## Review and acceptance

Maintainers may request changes for correctness, scope, architecture, tests, data safety, accessibility or documentation. A contribution may be declined when it expands the MVP without sufficient product value or creates maintenance cost disproportionate to the problem solved.

Participation in this project requires adherence to the [Code of Conduct](CODE_OF_CONDUCT.md).
