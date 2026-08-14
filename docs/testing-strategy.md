# Testing strategy

Tests are organized by architectural boundary.

- Domain tests cover invariants, value preservation, row flags and deterministic definitions.
- Application tests cover workflow orchestration and ports using in-memory fakes.
- Infrastructure tests cover SQLite mappings, file adapters and repository behavior.
- UI behavior should be exercised primarily through view-model tests; visual smoke checks cover the WPF shell.

Test count is not a target by itself. Each test should protect a business rule, an integration boundary or a previously observed failure.

Culture-sensitive cases must always name the culture explicitly, including `nl-NL`, `pl-PL` and `en-US` examples.

The `Performance` category runs a complete 50,000-row synthetic pipeline with a deliberately generous 30-second budget. Its purpose is to detect severe regressions; measured stage timings belong in `performance.md` and must identify the verification environment.
