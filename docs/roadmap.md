# Roadmap

The roadmap communicates intent, not a contractual delivery date. Scope may move between pre-1.0 versions, but v1.0 remains limited to the documented MVP.

## Current status

**Current milestone:** none — v1.0.1 is published
**Completed milestone:** M8 — Portfolio release
**Release status:** v1.0.1 audit-hardening release published on GitHub

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

- [x] exact normalized matching;
- [x] single and composite keys;
- [x] duplicate groups and resolution actions.

### v0.6.0 — Export and history

- [x] CSV, XLSX and SQLite export;
- [x] export filters and error report;
- [x] processing summary and local history.

### v0.9.0 — Release candidate

- [x] performance measurements and responsiveness work;
- [x] complete automated test coverage for MVP behavior;
- [x] accessibility and UI polish;
- [x] synthetic demonstration datasets;
- [x] final UI screenshots generated from the real WPF views with synthetic data;
- [x] portfolio case study and locally verified RC1 package.

### v1.0.0 — Stable portfolio release

- [x] verified end-to-end MVP workflow;
- [x] signed-off documentation and release notes;
- [x] locally verified downloadable Windows artifact;
- [x] no known critical defects;
- [x] public GitHub release.

### v1.0.1 — Audit hardening

- [x] deterministic data culture throughout the processing pipeline;
- [x] saved-profile culture applied before parsing;
- [x] explicit profile and culture selection before import;
- [x] preview capped at 1,000 rows without limiting processing or export;
- [x] App/ViewModel regression tests and per-assembly coverage gates;
- [x] refreshed documentation, screenshots and dependency automation;
- [x] public GitHub release built from the hardened code.

## Later candidates

Possible post-1.0 work includes additional semantic types, more export options and saved transformation presets. Fuzzy matching, cloud integrations and multi-user behavior require separate product decisions and are not implied by this roadmap.
