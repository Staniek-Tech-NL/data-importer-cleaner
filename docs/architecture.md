# Architecture

## Dependency direction

```text
DataCleaner.App ────────> DataCleaner.Application ────────> DataCleaner.Domain
       │                           ▲
       └────> DataCleaner.Infrastructure ─────────────────> DataCleaner.Domain
```

The domain project has no WPF, EF Core, CSV or Excel dependencies. The application layer declares use-case and persistence ports. Infrastructure supplies adapters. The WPF application composes the system using the generic host.

## Processing pipeline

```text
Import → Profile → Map → Pre-validation → Clean → Post-validation
       → Deduplicate → Review → Export
```

Pre-validation and post-validation use the same validation engine. Cleaning never becomes an implicit part of validation.

Export selection and row filtering live in the application layer. Infrastructure writers own CSV, Open XML workbook and SQLite serialization, while all formats share the same create-new-file safety boundary. Processing history reuses the local application SQLite database and contains metadata only.

## Domain decisions

### Dataset terminology

The domain uses `ImportedDataset`, `ImportedRow`, `ImportedColumn` and `DataCell` to avoid collisions with `System.Data` types.

### Technical and semantic types

`DataType` describes representation (`Text`, `Integer`, `Decimal`, `Date`, `Boolean`, `Unknown`). `SemanticType` adds business meaning such as `Email`. This permits future semantic types without distorting parsing rules.

### Value lifecycle

Each cell preserves three logical values:

- `SourceValue`: exact text read from the file;
- `ParsedValue`: typed interpretation using explicit import settings;
- `CurrentValue`: value after deterministic transformations.

Replacing `CurrentValue` creates a new `DataCell`; source and parsed values remain unchanged.

### Row state

`RowState` is a flags enum because a row can be modified, contain warnings and be a duplicate simultaneously. The UI may derive a dominant visual state without losing domain information.

### Duplicate comparison

The default is exact matching after deterministic normalization (`CurrentNormalizedValue`). Exact source matching remains an explicit option. Fuzzy and probabilistic matching are outside v1.0.

### Import profiles

Profiles carry a monotonically increasing `ProfileVersion` plus creation and update timestamps. Full revision history is not required for the MVP.

## Persistence boundary

SQLite stores profiles, rule configuration and import-history metadata. Complete imported datasets are not retained by default. Logs must not contain source row values.
