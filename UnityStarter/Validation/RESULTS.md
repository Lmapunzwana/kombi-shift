# Operational validation — 1 November 2026

Environment: Unity 6000.6.2f1, Linux x86_64, Built-In renderer. The team foundation project was not upgraded.

## Passed

- Linux release build, scripts, custom world-label shader and imported audio assets.
- **32 regression assertions**, no failures: acceleration/steering/braking/reverse; stop held after brake release; parked resistance to horizontal pushes; service motion lock; visible hired hwindi and sliding door; boarding across pause; animated alighting before a single fare payout; fare refusal; damage/refuelling/end states/replay; other kombis waiting at ranks and departing; audio asset loading and requested shadow disabling.
- Native X11 keyboard event sequence (W/D/Space/S/Escape): forward, steering, reverse, pause/resume registered. Maximum speed **18.20 m/s**, maximum displacement **28.06 m**. See `native-input-results.json`. This is automated input, not a human playtest.
- Native audio output sampled at **48 kHz** with nonzero RMS, recorded engine loaded/playing, and all hwindi speech clips loaded. See `audio-proof.json`. This verifies the game mixer produces signal; it does not prove the listener's physical speaker routing or perceived loudness. System speaker volume was read as unmuted and was not changed.
- Menu/city/rain and hwindi calling/boarding/alighting captures inspected at 1280x800; no runtime exceptions found in those runs.

## Relevant corrections

- Raw W/S polling disagreed with logical events on this environment. The shared input path now combines polling with bounded event-held state, explicit key-up/focus clearing, and deduplicated action edges. Mouse drive values require an actual held mouse button; steering alone does not apply throttle.
- A stop request completes braking and latches parking constraints. Keeping an old accelerator held cannot launch the vehicle after stopping; neutral plus deliberate acceleration releases it. Service and door closure interlocks also prevent departure.
- Hwindi is enabled by default, has visible gestures/calls and a working sliding doorway, guides boarding, collects fare and signals readiness. Passenger alighting completes before payment. Other kombis approach rank bays, dwell and rejoin their lanes with forward obstacle checks.
- Decorative arrows and cast shadows are removed. Colours are muted off-white/blue-grey, concrete, faded plaster and asphalt, with painted stop bays.

## Reproduce

Build with `KombiShift.JaydenStarter.Editor.StarterBuild.Linux`. Run regression with `--kombi-smoke --kombi-exit --kombi-report=/absolute/path/report.json`.

For native input, close other preview windows; launch with `--kombi-input-probe --kombi-exit`, then run `python3 UnityStarter/Validation/native_keyboard_probe.py` on the same X11 display. Read `Builds/input-probe.json` after exit.

For visual/audio evidence, launch with `--kombi-capture-dir=/absolute/path/captures`. It runs a clearly automated menu/city/rain/service demonstration and saves captures plus `audio-proof.json`, then returns to menu. Normal play should be launched without diagnostic arguments.

## Limits

Procedural low-poly art and simplified arcade service/traffic; not a full transport simulation. Hwindi speech is a temporary synthesized English placeholder, not a real recorded conductor; recorded engine source/licence and all preparation are in `Resources/Audio/CREDITS.md`. Human playtesting, long sessions, gamepad/touch controls, profiler comparisons, team-pinned Unity/HDRP/URP and Windows/Android/Web remain unverified.

Generated signing/passcode/password fields are cleared before repository export. Caches, binaries and raw machine logs are not committed.
