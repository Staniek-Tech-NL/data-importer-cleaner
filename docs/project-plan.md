# Project plan

## Product statement

Data Importer & Cleaner is a Windows desktop utility that transforms inconsistent CSV and Excel files into clean, validated and reusable business datasets through a controlled and reviewable workflow.

## Target users

- freelancers and small businesses;
- operations and administrative teams;
- analysts preparing files for another system;
- users who need repeatable cleaning without writing scripts.

## Product objective

The application demonstrates production-minded desktop development and practical data processing. It is not a spreadsheet editor. Its responsibility is to import, understand, validate, transform, deduplicate and export tabular business data without changing the source file.

## MVP workflow

```text
Select file
  → Select worksheet when required
  → Preview
  → Profile columns
  → Map source columns
  → Pre-validate
  → Apply deterministic cleaning
  → Post-validate
  → Detect exact normalized duplicates
  → Review original versus current values
  → Export data and error report
```

## MVP capabilities

### Input

- CSV with comma, semicolon or tab delimiters;
- quoted fields, header row and configurable encoding;
- automatic delimiter detection;
- XLSX workbook and worksheet discovery;
- worksheet selection, header detection and empty-row handling.

### Profiling and mapping

- technical type inference for text, integer, decimal, date, boolean and unknown values;
- semantic classification beginning with email;
- empty, unique, duplicate and invalid counts;
- numeric minimum, maximum and average;
- source-to-target mapping and ignored columns;
- reusable, versioned import profiles.

### Validation

- required values;
- technical type validation;
- email validation;
- numeric ranges;
- allowed values;
- uniqueness;
- info, warning and error severities;
- validation before and after cleaning.

### Cleaning

- trim and internal-whitespace normalization;
- upper, lower and title casing;
- email normalization;
- configurable null tokens;
- configurable country aliases;
- culture-aware date and decimal normalization;
- original, parsed and current value preservation.

### Duplicates and review

- single-column and composite keys;
- exact comparison after deterministic normalization by default;
- keep first, keep last, remove or mark for review;
- row states that can coexist, such as modified, warning and duplicate;
- before/after review before export.

### Output and persistence

- CSV, XLSX and SQLite exports;
- filters for all, valid, invalid and modified rows;
- separate rejected-row report;
- import summary and local history metadata;
- profile and application-state persistence in SQLite.

## Non-functional requirements

- The source file is never modified.
- The UI remains responsive during long-running work.
- Operations support cancellation where practical.
- User-facing errors are understandable without technical knowledge.
- Technical details are logged without row contents by default.
- Initial practical target: 10,000–50,000 rows, subject to measured benchmarks.
- CI must build with zero warnings and run all automated tests.

## Explicitly outside v1.0

- AI or machine-learning cleaning;
- fuzzy, phonetic or probabilistic duplicate matching;
- cloud synchronization and scheduled imports;
- Google Sheets, JSON, XML, ODS, Parquet or legacy XLS;
- REST APIs and third-party ERP integrations;
- authentication, roles and multi-user workflows;
- data-warehouse-scale streaming.

## Delivery milestones

| Milestone | Outcome |
| --- | --- |
| M1 — Foundation | Layered solution, WPF shell, DI, logging, SQLite, EF migration, tests, CI and baseline documentation |
| M2 — File import | CSV/XLSX readers, worksheet selection, header handling, preview and basic inference |
| M3 — Profiling and mapping | Statistics, mapping, ignored columns and persisted import profiles |
| M4 — Validation | Configurable validation rules, severities, issue list and row/column highlighting |
| M5 — Cleaning | Deterministic transformations, culture-aware parsing and before/after review |
| M6 — Deduplication | Exact normalized matching, duplicate groups and resolution actions |
| M7 — Export | CSV/XLSX/SQLite output, filtered exports, error report, summary and history |
| M8 — Portfolio release | Full test suite, performance evidence, screenshots, case study and packaged release |

## Definition of done for v1.0

Version 1.0 is complete when a user can finish the full MVP workflow, save and reuse a profile, export cleaned data and an independent error report, inspect a processing summary, and do all of this without altering the input file.

New capabilities that do not directly improve importing, understanding, validating, cleaning, deduplicating, reviewing or exporting data belong in a later release.
