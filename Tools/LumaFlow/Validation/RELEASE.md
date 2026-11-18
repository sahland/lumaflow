# Release payloads

Current version is `0.2.0`, pending publication. Runtime API contract v1 is frozen;
platform qualification is documented separately from the package version.
Minimum declared Editor: 6000.2.2f1. Refer to the package compatibility policy
for evidence scope and the checks required before changing the release tier.

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

The reviewed matrix lives in `Tools/LumaFlow/Validation/unity-matrix.json`:
6000.2.2f1 and 6000.4.5f1, each with EditMode and PlayMode functional suites.
Both workflow matrices consume that same file through the `unity-matrix` job.
`prepare_suite_project.py` copies the embedded package, complete test sources
and sample into a new Built-In project with an exact ProjectVersion.txt. It does
not downgrade the development project or copy its URP/scene/settings assets.
Functional jobs select `LumaFlow.Editor.Tests` or `LumaFlow.Runtime.Tests` by
mode; performance suites are not executed by these jobs and need their own
Player/device budget checks. XML evidence must contain the expected assembly
with actual passing cases; failed, skipped, inconclusive or empty runs fail.

Separate clean Built-In jobs install each channel archive on Unity 6000.2.2f1
and 6000.4.5f1. Each runs EditMode and PlayMode smoke tests and builds a standalone
development player from a minimal scene. This is compilation/build evidence, not
a visual or launched-player test. The generated player and test reports are
uploaded. Missing smoke results fail the job, including zero-test runs.

This produces four functional jobs and four archive-consumer jobs. Logs and
result artifact names include version and mode/channel to avoid collisions.
`Unity validation gate` runs even after a prerequisite fails and requires every
matrix, activation, payload and icon job to succeed. Configure this exact check
as required in repository branch protection; editing this workflow does not
change GitHub rules. No `continue-on-error` or automatic retry hides failures.

To inspect/prepare the matrix locally without launching Unity:

```sh
python Tools/LumaFlow/Validation/unity_matrix.py
python Tools/LumaFlow/Validation/prepare_suite_project.py --output Artifacts/suite --unity 6000.2.2f1
```

Workflow configuration and local Python tests do not establish that hosted
Unity jobs passed. Preserve the actual GitHub run URL and artifacts as evidence.

`prepare_tarball_project.py` creates the consumer without copying development
ProjectSettings, URP packages or the embedded framework. The archive dependency
is relative so the project can move into a GameCI container. To reproduce locally:

```sh
python Tools/LumaFlow/Validation/prepare_tarball_project.py --archive Artifacts/release/com.sahland.lumaflow-0.2.0-asset-store.tgz --output Artifacts/consumer --unity 6000.4.5f1 --channel asset-store
```

Run Unity Test Runner for both modes in that project. Windows validation requires
the Windows standalone target; CI uses Linux standalone. GameCI also needs Unity
activation credentials configured as repository secrets, as documented at
https://game.ci/docs/github/test-runner/.

## Activating hosted Unity jobs

In repository Settings > Secrets and variables > Actions, configure the Unity
activation method supported by your account and the GameCI image. The image used
by this workflow requests `UNITY_EMAIL` and `UNITY_PASSWORD` for a Personal seat,
adds `UNITY_SERIAL` for Pro, or accepts a supported `UNITY_LICENSE` file content.
Use GitHub's secret fields; never commit credentials or paste them into logs.

The activation preflight checks presence only. Invalid or expired credentials
still fail in Unity. Without activation inputs the preflight fails, Unity jobs
are blocked, and Python/package checks continue independently. This does not
count as a passing Unity run.

After configuring secrets, rerun the workflow from GitHub Actions. Baseline run
34585332523 failed with `Licensing method: <none>` before compilation; Docker
images downloaded successfully. That run does not establish a framework failure.
