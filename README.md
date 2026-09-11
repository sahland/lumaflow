# LumaFlow development project

This Unity project is the source of truth for LumaFlow development. Open it with
the editor version in `ProjectSettings/ProjectVersion.txt`.

- `Packages/com.sahland.lumaflow` contains the distributable framework.
- `Assets/LumaFlow.Tests` contains runtime, editor and performance tests.
- `Assets` also contains development scenes and supporting assets.
- `Tools` contains validation scripts, icon tooling and asset provenance.
- `.github/workflows` contains the project's automated checks.

Make framework changes here together with their tests and documentation.
The separate `sahland/lumaflow` package repository is a distribution copy;
do not develop a second implementation there. Package publication must use a
reviewed development commit and preserve the package-root layout expected by UPM.
The existing public repository and its history remain separate.

Run Unity Test Runner in EditMode and PlayMode before releasing. Additional
validation scripts are in `Tools/LumaFlow/Validation`. Known release-tooling
limitations and the remaining work are tracked in `HARDENING.md`.

Unity caches, build output, profiler captures and the separately maintained
website are excluded from version control. No remote is configured automatically
for this development repository.
