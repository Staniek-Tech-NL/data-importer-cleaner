# Domain model

The initial aggregate is an `ImportedDataset` containing column definitions and rows. A row contains cells addressed by stable column identifiers.

Validation rules inspect cells and return issues without changing data. Cleaning rules return replacement cells and a description of a deterministic change. Duplicate definitions contain one or more column identifiers and state whether source or normalized working values are compared.

Column profiling summarizes technical and semantic types, empty and repeated values, invalid values and numeric ranges without changing imported cells. The initial semantic classifier recognizes columns whose non-empty values are valid email addresses.

An `ImportProfile` owns unique source-column mappings. Each mapping can rename a source column for downstream use or exclude it from the working preview. Profiles preserve culture settings, increment their version when configuration changes and are stored locally in SQLite.

Validation and cleaning contracts remain intentionally separate. Their concrete rules are introduced in their corresponding milestones.
