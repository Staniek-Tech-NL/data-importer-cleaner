# Security policy

## Supported versions

The project has not published a supported binary release yet.

| Version | Supported |
| --- | --- |
| `main` | Best effort during development |
| Public releases | None yet |

This table will be updated when the first release is published.

## Reporting a vulnerability

Do not open a public issue for a suspected vulnerability or data-exposure problem.

Use GitHub's private vulnerability reporting for this repository. Include:

- the affected component and revision;
- clear reproduction steps using synthetic data;
- the potential impact;
- any suggested mitigation;
- whether the report contains information that should remain confidential.

Do not include real customer files, personal data, credentials or production secrets.

The maintainer will acknowledge a valid report as soon as practical, assess severity, coordinate a fix and publish an advisory when appropriate. Timelines depend on impact and project availability; no service-level agreement is currently offered.

## Security boundaries

Security-sensitive areas include file parsing, path handling, spreadsheet formula injection, export behavior, logging, local database storage, dependency updates and any future feature that processes untrusted content.
