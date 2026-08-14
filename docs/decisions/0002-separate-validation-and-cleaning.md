# ADR 0002: Separate validation and cleaning

**Status:** Accepted  
**Date:** 2026-08-13

## Context

Some source values fail strict validation but become valid after deterministic cleaning. Combining validation and cleaning would hide that distinction and make transformations harder to audit.

## Decision

Validation and cleaning remain independent engines. The processing pipeline supports a pre-cleaning validation pass and a post-cleaning validation pass using the same validation rules where appropriate.

## Consequences

- The application can report both source quality and final output quality.
- Cleaning rules never decide whether data is valid.
- The workflow performs additional work, but the behavior is clearer and testable.
- The UI must distinguish issues by validation pass when that context matters.
