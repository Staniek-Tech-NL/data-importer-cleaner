# Changelog

All notable changes to this project will be documented in this file.

## [0.1.0] - Unreleased

### Added

- .NET 10 solution with Domain, Application, Infrastructure and WPF projects.
- Domain foundations for imported data, validation, cleaning and deterministic duplicate definitions.
- Versioned import-profile model.
- Generic host, dependency injection and structured logging setup.
- EF Core SQLite persistence model and initial migration.
- WPF application shell.
- Automated domain, architecture and SQLite initialization tests.
- Windows GitHub Actions build and test workflow.
- Complete English project, architecture, safety, development, roadmap and release documentation.
- Architecture decision records for the domain model, processing passes and duplicate matching.
- Contribution, conduct, governance, support and security policies.
- Structured issue forms and pull request template.
- Dependabot configuration for NuGet and GitHub Actions.
- Tag-driven Windows packaging and draft GitHub release workflow.
- CSV import with delimiter detection, quoted-field handling, selectable encoding and culture-aware type inference.
- XLSX workbook discovery, worksheet selection, header detection and empty-row handling.
- Read-only data preview with basic technical type inference in the WPF application.
- Column profiling with empty, unique, duplicate and invalid counts plus numeric statistics.
- Initial email semantic classification.
- Interactive source-to-target column mapping and ignored-column preview.
- Reusable, versioned import profiles persisted in SQLite.
- Required, email, technical-type, numeric-range, allowed-value and uniqueness validation rules.
- Configurable info, warning and error severities with before/after-cleaning validation passes.
- Row and column issue presentation, row-state updates and rejected-row reports.
- Validation rule configuration persisted with versioned import profiles.
- Deterministic trimming, whitespace, casing, email and null-token cleaning rules.
- Configurable country aliases and culture-aware date and decimal normalization.
- Ordered cleaning passes that preserve source, parsed and current values.
- Before/after change review with validation before and after cleaning.
- Cleaning rule configuration persisted with versioned import profiles.
- Exact duplicate detection over one or more normalized working-value columns.
- Stable duplicate groups with source row numbers and auditable key values.
- Mark-for-review, keep-first, keep-last and remove-all duplicate resolution actions.
- Dedicated WPF duplicate-key configuration and group review screen.
- Duplicate-key column selection persisted with versioned import profiles.
