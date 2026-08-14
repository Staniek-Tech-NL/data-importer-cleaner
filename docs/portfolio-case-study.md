# Portfolio case study

## Problem

Operational CSV and Excel files commonly combine inconsistent headers, whitespace, casing, date and decimal conventions, missing values and duplicate records. Manual correction is slow, difficult to reproduce and risky because the original file can be overwritten without a reliable audit trail.

## Product response

Data Importer & Cleaner is a local Windows workflow that keeps source values immutable while maintaining parsed and current working values. A user can preview and profile a file, map columns, configure validation and cleaning, review changes, resolve deterministic duplicates and export a new CSV, XLSX or SQLite result plus an independent rejected-row report.

## Engineering decisions

- The framework-independent domain owns value history, row states, validation, cleaning and duplicate definitions.
- Application contracts separate workflows from CSV, Open XML, SQLite and profile/history persistence adapters.
- Validation never mutates values; cleaning never decides validity.
- Duplicate matching is exact and explainable after deterministic normalization; fuzzy matching is deliberately outside v1.0.
- Export refuses existing destinations, preserving both source files and earlier outputs.
- Local history stores aggregate metadata rather than customer row contents.

## Evidence

- 55 automated tests cover domain invariants, workflow orchestration, real file adapters, SQLite migrations and the complete 50,000-row synthetic pipeline.
- Release builds treat analyzer warnings as errors and complete with zero warnings.
- The verified 50,000-row pipeline completes in 2.853 seconds on the documented development machine.
- A self-contained win-x64 RC1 package is produced by the same command shape used in the tag-driven GitHub Actions release workflow.
- The committed demonstration file is synthetic and intentionally includes whitespace, aliases, invalid values and post-normalization duplicates.

## Safety and privacy

Input files are opened read-only and export always creates a new destination. Source, parsed and working values remain distinguishable. Logs and local history exclude row contents by default. No cloud service, authentication or telemetry is required.

## Current release boundary

Version 0.9.0-rc.1 is a portfolio release candidate for Windows 10/11 x64. The source is available for inspection but remains all-rights-reserved; it is not offered under an open-source license. Installation on a separate clean Windows machine and a final captured UI screenshot remain publication checks before promoting the candidate to v1.0.
