# Data safety and privacy

Data safety is a product requirement rather than an implementation detail.

## Source immutability

The application opens the selected CSV or XLSX file for reading and performs all parsing and transformations on a working representation. Export always targets a new file. No workflow may overwrite the source path implicitly.

## Value traceability

Every imported cell logically retains:

- the exact source text;
- the typed parsed value;
- the current working value after transformations.

This enables a reliable before/after review and prevents normalized values from erasing evidence of what was supplied.

## Local persistence

SQLite stores application configuration, import profiles and import-history metadata. Complete imported datasets are not persisted by default. History should prefer file name, timestamps, counts, status and output metadata over row content.

## Logging

Logs may include operation names, file type, row counts, elapsed time, rule identifiers and exception metadata. They must not include complete rows, cell values, email addresses, customer names or other imported content by default.

Diagnostic modes that expose values would require explicit user consent, visible warnings, limited retention and a separate security review.

## Repository data

Only synthetic datasets may be committed. Synthetic samples must not be derived from customer files by simple anonymization because combinations of fields can remain identifying.

Before committing a sample:

1. verify that every person, company, identifier and contact detail is fictional;
2. inspect Git history, not only the working tree;
3. confirm that temporary exports and local databases are ignored;
4. run secret scanning after publication.

## Security reporting

Potential vulnerabilities and accidental data-exposure paths are reported privately according to the repository security policy. Public issues must not include confidential sample files or personal data.
