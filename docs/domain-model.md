# Domain model

The initial aggregate is an `ImportedDataset` containing column definitions and rows. A row contains cells addressed by stable column identifiers.

Validation rules inspect cells and return issues without changing data. Cleaning rules return replacement cells and a description of a deterministic change. Duplicate definitions contain one or more column identifiers and state whether source or normalized working values are compared.

Column profiling summarizes technical and semantic types, empty and repeated values, invalid values and numeric ranges without changing imported cells. The initial semantic classifier recognizes columns whose non-empty values are valid email addresses.

An `ImportProfile` owns unique source-column mappings. Each mapping can rename a source column for downstream use or exclude it from the working preview. Profiles preserve culture settings, increment their version when configuration changes and are stored locally in SQLite.

Validation definitions bind a rule kind and severity to a source column. Required, email, technical-type, numeric-range, allowed-value and uniqueness rules inspect current values without modifying them. A validation pass clears previous validation flags, records row/column issues and marks rows with coexisting valid, informational, warning, invalid and rejected states. Rows with error-severity issues are included in an in-memory rejected-row report.

Validation configuration is stored with the versioned import profile. Before-cleaning and after-cleaning passes share the same deterministic engine so later cleaning work can distinguish source problems from issues remaining after transformations.

Cleaning definitions bind an ordered rule to a source column. Available rules trim text, normalize internal whitespace and casing, normalize email values, replace configured null tokens, resolve country aliases and parse dates and decimals using an explicit culture. Successful date, decimal and email normalization also updates the column's technical or semantic metadata.

The cleaning engine creates new cells and rows rather than mutating imported values. Each cell continues to expose its original source, parsed value and current working value. Every effective rule application produces a before/after change entry, while modified row state can coexist with unrelated states such as duplicate.

Cleaning configuration is stored with versioned import profiles. The application validates before cleaning, runs rules in deterministic order, profiles the resulting data and validates again so users can distinguish source issues from remaining output issues.

Duplicate detection compares exact invariant representations of current working values by default, with original source comparison available through the application contract. A definition can contain one column or a composite key. Rows with an incomplete key are skipped, and groups plus their members are ordered by source row number.

Resolution is separate from detection. Mark-for-review keeps all rows and adds the duplicate state alongside existing states. Keep-first and keep-last retain a deterministic representative, while remove-all excludes every grouped row from a new in-memory dataset. The detection result retains group membership and key values for review even after rows are removed.

Duplicate-key selection is part of persisted source-column mapping configuration, so a versioned import profile can reproduce the same composite key on a later import.

Export builds a projection of mapped, non-ignored columns and current values before applying an all, valid, invalid or modified row filter. Format-specific writers create CSV, XLSX or SQLite output with invariant scalar formatting and refuse existing destinations. Validation errors can be written to a separate CSV report without exposing unrelated rows.

An import history entry stores source and output file names, timestamps, status and aggregate counts for processed, valid, invalid, modified and removed-duplicate rows. Full imported datasets and cell contents remain outside application persistence.
