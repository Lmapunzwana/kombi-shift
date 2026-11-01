# Local preview validation — 1 November 2026

Environment: Unity 6000.6.2f1, Linux x86_64, Built-In rendering. Separate preview project; the team foundation was not upgraded.

- C# compilation and Editor scene creation: passed.
- Linux development player build: passed, exit 0.
- Player automated smoke sequence: 18 passed, 0 failed. Exact assertions are in `smoke-results.json` and the diagnostics source.
- Native graphical player: launched successfully; city and rain captures visually inspected for scene/HUD presence.
- Fixed issues found during validation: restart cancelled unrelated diagnostic/capture coroutines; vertical physics settling incorrectly cancelled boarding. Restart now cancels only boarding; boarding uses planar driving speed.

The smoke sequence invokes actions directly and disables traffic during isolated passenger assertions. It does not prove keyboard controls, traffic avoidance, balance, performance or accessible navigation. No human playtest or profiler measurements are claimed. Windows, Android/Web, team-pinned Unity 6000.3.27f2 and HDRP appearance remain untested.

The graphical preview is a procedural prototype: primitives, simple rain, IMGUI and generated audio. Production asset/animation polish and integration with the team's managers remain work for review.

Repository export: cleared generated console passcode/secret, certificate password and signing path/alias fields from the preview PlayerSettings before upload. No credentials are required by this Linux prototype.
