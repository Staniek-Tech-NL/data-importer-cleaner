# Roadmap

The roadmap communicates intent, not a contractual delivery date. Scope may move between pre-1.0 versions, but v1.0 remains limited to the documented MVP.

## Current status

**Current milestone:** M6 — Deduplication
**Completed milestone:** M5 — Cleaning
**Release status:** no public binary release yet

## Version plan

### v0.1.0 — Foundation and first preview

- [x] layered .NET 10 solution;
- [x] WPF application shell;
- [x] dependency injection and logging;
- [x] EF Core SQLite model and initial migration;
- [x] initial domain contracts and tests;
- [x] CI and repository documentation;
- [x] CSV import;
- [x] XLSX import and worksheet selection;
- [x] preview grid and basic type inference.

### v0.2.0 — Profiling and mapping

- [x] column statistics and type inference;
- [x] source-to-target mapping;
- [x] ignored columns;
- [x] versioned import profiles and persistence.

### v0.3.0 — Validation

- [x] required, email, type, range, allowed-value and uniqueness rules;
- [x] severities and validation passes;
- [x] row/column issue presentation;
- [x] rejected-row report model.

### v0.4.0 — Cleaning

- [x] deterministic text, email and null normalization;
- [x] configurable country aliases;
- [x] culture-aware date and decimal normalization;
- [x] before/after review.

### v0.5.0 — Deduplication

- exact normalized matching;
- single and composite keys;
- duplicate groups and resolution actions.

### v0.6.0 — Export and history

- CSV, XLSX and SQLite export;
- export filters and error report;
- processing summary and local history.

### v0.9.0 — Release candidate

- performance measurements and responsiveness work;
- complete automated test coverage for MVP behavior;
- accessibility and UI polish;
- synthetic demonstration datasets;
- screenshots and portfolio case study.

### v1.0.0 — Stable portfolio release

- verified end-to-end MVP workflow;
- signed-off documentation and release notes;
- downloadable Windows artifact;
- no known critical defects.

## Later candidates

Possible post-1.0 work includes additional semantic types, more export options and saved transformation presets. Fuzzy matching, cloud integrations and multi-user behavior require separate product decisions and are not implied by this roadmap.
