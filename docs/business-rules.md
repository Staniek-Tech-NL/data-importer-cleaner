# Business rules

1. The input file is read-only. Export always creates a separate output.
2. Parsing requires an explicit culture whenever a value is ambiguous.
3. A cleaning rule may change only the working value, never the source value.
4. Validation issues identify the source row, column, rule, message and severity.
5. Post-cleaning validation is required before export.
6. Duplicate matching is deterministic and uses configured key columns.
7. Export requires a reviewable before/after representation of modifications.
8. Application persistence contains configuration and processing metadata, not full customer datasets by default.
9. Warning and informational issues do not reject a row; error-severity issues mark it invalid and rejected.
10. Re-running validation replaces validation-related row states while preserving unrelated states such as modified or duplicate.
11. Cleaning rules operate only on current working values and preserve both source and parsed values.
12. Cleaning order is explicit and every effective change is included in the before/after review.
