# Release process

The project follows semantic versioning. Before v1.0, minor versions may contain breaking changes; release notes must call them out explicitly.

## Release types

- **Development build:** produced by CI for verification; not a supported user release.
- **Pre-release:** tagged with a suffix such as `v0.9.0-rc.1` and used for portfolio/release validation.
- **Stable release:** a signed-off tag such as `v1.0.0` with a downloadable Windows artifact.

## Pre-release checklist

- [x] The intended milestone is complete and remaining work is moved explicitly.
- [x] `CHANGELOG.md` contains user-visible changes and known limitations.
- [x] The version is consistent across release metadata.
- [x] Release build completes with zero warnings and zero errors.
- [x] All automated tests pass on GitHub Actions.
- [x] Database migration behavior is tested.
- [x] No real customer data, local databases, secrets or logs are included.
- [x] README status and support matrix reflect the released build.
- [x] Screenshots were captured from the real stable WPF views.
- [ ] Installation and first-run instructions were tested on a clean Windows environment — explicitly waived for this portfolio release; no clean-machine claim is made.
- [x] The repository has an explicit license before any public release.
- [x] Security policy and release notes are ready to link from the release page.

## Tagging and publication

1. Merge the release pull request to `main`.
2. Confirm the protected-branch checks are green.
3. Create an annotated semantic-version tag.
4. Push the tag to trigger the release workflow.
5. Verify the generated ZIP locally and either complete clean-Windows testing or disclose its explicit waiver.
6. Publish or promote the GitHub release after smoke testing.
7. Move the changelog to the released version and create the next `Unreleased` section.

## Rollback

Do not replace an existing release asset silently. If a published package is defective, mark the release clearly, fix the problem through a pull request and issue a new patch version.
