# GitHub project management

## Repository identity

Recommended repository name: `data-importer-cleaner`

Recommended description:

> A Windows desktop application for importing, profiling, validating, cleaning and exporting messy CSV and Excel business data through a safe and repeatable workflow.

Recommended topics:

```text
dotnet csharp wpf mvvm csv excel sqlite ef-core
data-cleaning data-validation data-import etl desktop-application
```

## Milestones

Create GitHub milestones `M1` through `M8` using the outcomes in `docs/project-plan.md`. Close a milestone only when its deliverable can be demonstrated, built and tested. Version labels and milestones should not be used interchangeably: the milestone tracks work; the version identifies a release.

## Suggested labels

### Type

- `type: bug`
- `type: feature`
- `type: documentation`
- `type: test`
- `type: maintenance`

### Area

- `area: domain`
- `area: application`
- `area: infrastructure`
- `area: ui`
- `area: import`
- `area: validation`
- `area: cleaning`
- `area: export`

### Status and priority

- `status: triage`
- `status: ready`
- `status: blocked`
- `priority: high`
- `priority: medium`
- `priority: low`

Use `good first issue` and `help wanted` only when the issue has clear acceptance criteria and does not require undocumented project knowledge.

## Issue policy

- One issue should describe one independently verifiable outcome.
- Acceptance criteria describe observable behavior, not implementation steps.
- Bugs include a minimal synthetic reproduction and environment details.
- Feature proposals explain the business problem and MVP alignment.
- Out-of-scope v1.0 ideas are labeled and moved to a later milestone instead of expanding the current milestone.

## Pull request policy

- Work is merged through pull requests, including maintainer changes when practical.
- PRs remain small enough to review and test coherently.
- Every PR links an issue unless it is a trivial documentation or maintenance correction.
- Squash merging is recommended for a clean portfolio history.
- The PR title follows a Conventional Commit-style prefix.

## Recommended repository settings

After publication:

1. Set `main` as the default branch.
2. Protect `main` and require the `build-and-test` status check.
3. Require branches to be up to date before merging.
4. Require conversation resolution.
5. Disable force pushes and branch deletion for `main`.
6. Enable Issues and Discussions; use Discussions for questions and ideas that are not ready for implementation.
7. Enable the dependency graph, Dependabot alerts, secret scanning, push protection and code scanning where available.
8. Enable automatic deletion of merged branches.
9. Add the repository description, topics and a social preview image.

## Project board

A lightweight board is sufficient:

```text
Backlog → Ready → In progress → In review → Done
```

Limit active implementation to the current milestone. Design notes and rejected scope remain in issues or ADRs so decisions are searchable.
