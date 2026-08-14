# Domain model

The initial aggregate is an `ImportedDataset` containing column definitions and rows. A row contains cells addressed by stable column identifiers.

Validation rules inspect cells and return issues without changing data. Cleaning rules return replacement cells and a description of a deterministic change. Duplicate definitions contain one or more column identifiers and state whether source or normalized working values are compared.

The initial contracts are intentionally small. Concrete CSV/XLSX parsing, validation rules, cleaning rules and mapping models are introduced in their corresponding milestones rather than anticipated in M1.
