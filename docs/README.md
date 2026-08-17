# Documentation

This directory contains the maintained project documentation. The source code is authoritative for implementation details; these documents explain intent, boundaries and delivery policy.

## Product and delivery

- [Project plan](project-plan.md) — product goals, MVP scope and milestone deliverables.
- [Roadmap](roadmap.md) — current milestone status and version targets.
- [Business rules](business-rules.md) — non-negotiable product behavior.
- [Data safety](data-safety.md) — privacy, source immutability and logging constraints.

## Engineering

- [Architecture](architecture.md) — solution boundaries and dependency direction.
- [Domain model](domain-model.md) — current domain concepts and invariants.
- [Development guide](development.md) — prerequisites, build commands and coding rules.
- [Testing strategy](testing-strategy.md) — test scope and quality expectations.
- [Performance evidence](performance.md) — verified synthetic 50,000-row measurement.
- [Portfolio case study](portfolio-case-study.md) — product, architecture and delivery evidence.
- [Architecture decisions](decisions/README.md) — durable records of important technical choices.

## Project operations

- [GitHub project management](github-project-management.md) — milestones, labels and branch policy.
- [Release process](release-process.md) — versioning, packaging and publication checklist.
- [v1.0.0 release notes](releases/v1.0.0.md) — stable package evidence and known limitations.
- [RC1 release notes](releases/v0.9.0-rc.1.md) — release-candidate verification history.

## Documentation maintenance

Documentation is updated in the same pull request as the behavior it describes. Planned features must be labeled as planned. Screenshots, benchmarks and supported file-size claims must reflect a verified build rather than an intention.
