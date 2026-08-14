# ADR 0001: Domain terminology and value lifecycle

**Status:** Accepted  
**Date:** 2026-08-13

## Context

Names such as `DataSet`, `DataRow` and `DataColumn` collide with established `System.Data` types. A two-value original/cleaned model also loses the distinction between raw source text and its typed interpretation.

## Decision

Use `ImportedDataset`, `ImportedRow`, `ImportedColumn` and `DataCell`. A cell preserves `SourceValue`, `ParsedValue` and `CurrentValue`. Technical representation is modeled with `DataType`; semantic meaning is modeled independently with `SemanticType`. Row conditions use a flags-based `RowState`.

## Consequences

- Source text remains available for review and error reporting.
- Cleaning can replace the working value without altering source evidence.
- Semantic types such as phone, postal code or VAT number can be added without redefining technical parsing types.
- The UI must deliberately choose which value representation to display.
