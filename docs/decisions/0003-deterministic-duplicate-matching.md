# ADR 0003: Deterministic duplicate matching

**Status:** Accepted  
**Date:** 2026-08-13

## Context

Business duplicates may differ only by deterministic formatting, such as email casing or surrounding whitespace. Raw equality misses these cases, while fuzzy matching introduces thresholds and unpredictable merges outside the MVP.

## Decision

Duplicate definitions use one or more configured key columns and compare exact current values after deterministic normalization by default. Exact raw-source comparison remains available as an explicit mode. No fuzzy, phonetic or probabilistic matching is included in v1.0.

## Consequences

- Results are reproducible and explainable.
- Cleaning order affects duplicate matching and must be visible in profiles.
- Users can choose source equality for audit-sensitive workflows.
- Similar but non-identical entities are not merged automatically.
