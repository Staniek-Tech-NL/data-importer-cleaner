# Domain model

The initial aggregate is an `ImportedDataset` containing column definitions and rows. A row contains cells addressed by stable column identifiers.

Validation rules inspect cells and return issues without changing data. Cleaning rules return replacement cells and a description of a deterministic change. Duplicate definitions contain one or more column identifiers and state whether source or normalized working values are compared.

Column profiling summarizes technical and semantic types, empty and repeated values, invalid values and numeric ranges without changing imported cells. The initial semantic classifier recognizes columns whose non-empty values are valid email addresses.

An `ImportProfile` owns unique source-column mappings. Each mapping can rename a source column for downstream use or exclude it from the working preview. Profiles preserve culture settings, increment their version when configuration changes and are stored locally in SQLite.

Validation definitions bind a rule kind and severity to a source column. Required, email, technical-type, numeric-range, allowed-value and uniqueness rules inspect current values without modifying them. A validation pass clears previous validation flags, records row/column issues and marks rows with coexisting valid, informational, warning, invalid and rejected states. Rows with error-severity issues are included in an in-memory rejected-row report.

Validation configuration is stored with the versioned import profile. Before-cleaning and after-cleaning passes share the same deterministic engine so later cleaning work can distinguish source problems from issues remaining after transformations.

Validation and cleaning remain intentionally separate. Concrete cleaning rules are introduced in their corresponding milestone.
