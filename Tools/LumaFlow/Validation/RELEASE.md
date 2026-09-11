# Release payloads

Build from a committed development revision, using a new output directory:

```sh
python Tools/LumaFlow/Validation/package_release.py --commit HEAD --output Artifacts/release
```

The tool produces two UPM-format `.tgz` archives and `release-report.json` with
the source commit, file counts and SHA-256 hashes. It reads committed files via
`git archive`, excludes development tools and validation documentation, preserves
asset metadata, checks the roundtrip archive and runs the icon validator against
the extracted payload. Uncommitted package edits are not shipped.

- `git-upm`: includes the existing MIT license from the public distribution
  repository, preserved in `Git-UPM-LICENSE.txt`.
- `asset-store`: omits that independent root license, matching the requested
  submission packaging. Framework sources and assets are identical between channels.

Both are UPM payloads; the Asset Store payload is not a legacy `.unitypackage`.
It is an input for the publisher's UPM submission workflow, not proof of store
approval. No uploader, publication or GitHub release runs automatically.
This packaging rule does not remove third-party notices: adding a third-party
dependency requires reviewing and explicitly including its notices.

For Unity import and smoke tests, run the existing tarball validation script
against each archive with an installed Unity executable. Archive validation alone
does not establish Unity compatibility or submission readiness.

The development project is published on the `development` branch of
`sahland/lumaflow`. Its `main` branch keeps the existing package-root layout.
Do not merge the project root into `main`; promotion must export the package.

## CI validation

The workflow keeps the full development-project suite on Unity 6000.4.5f1.
Separate clean Built-In jobs install each channel archive on Unity 6000.0.0f1
and 6000.4.5f1. Each runs EditMode and PlayMode smoke tests and builds a standalone
development player from a minimal scene. This is compilation/build evidence, not
a visual or launched-player test. The generated player and test reports are
uploaded. Missing smoke results fail the job, including zero-test runs.

`prepare_tarball_project.py` creates the consumer without copying development
ProjectSettings, URP packages or the embedded framework. The archive dependency
is relative so the project can move into a GameCI container. To reproduce locally:

```sh
python Tools/LumaFlow/Validation/prepare_tarball_project.py --archive Artifacts/release/com.sahland.lumaflow-0.1.1-asset-store.tgz --output Artifacts/consumer --unity 6000.4.5f1 --channel asset-store
```

Run Unity Test Runner for both modes in that project. Windows validation requires
the Windows standalone target; CI uses Linux standalone. GameCI also needs Unity
activation credentials configured as repository secrets, as documented at
https://game.ci/docs/github/test-runner/.
