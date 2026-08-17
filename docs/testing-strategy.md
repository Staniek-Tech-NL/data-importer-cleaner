# Testing strategy

Tests are organized by architectural boundary.

- Domain tests cover invariants, value preservation, row flags and deterministic definitions.
- Application tests cover workflow orchestration and ports using in-memory fakes.
- Infrastructure tests cover SQLite mappings, file adapters and repository behavior.
- App tests exercise ViewModel orchestration, explicit culture propagation, profile application, preview limits and export state; visual smoke checks cover the WPF shell.

Test count is not a target by itself. Each test should protect a business rule, an integration boundary or a previously observed failure.

Culture-sensitive cases must always name the culture explicitly, including `nl-NL`, `pl-PL` and `en-US` examples.

CI publishes Cobertura data and enforces measured line-coverage baselines instead of an aspirational global percentage: App 55%, Application 70%, Domain 65% and Infrastructure 80%. `scripts/Test-Coverage.ps1` prints the current per-assembly result in the GitHub Actions job summary and fails regressions below those baselines.

The `Performance` category runs a complete 50,000-row synthetic pipeline with a deliberately generous 30-second budget. Its purpose is to detect severe regressions; measured stage timings belong in `performance.md` and must identify the verification environment.
