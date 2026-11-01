# Controls and presentation validation — 1 November 2026

Environment: Unity 6000.6.2f1, Linux x86_64, Built-In rendering; isolated preview, not an upgrade of the team foundation.

## Verified

- Linux release player build and custom world-label shader compilation: passed.
- 22 control-to-physics/gameplay regression assertions: passed, no failures (`smoke-results.json`). These now accelerate from rest through the controller, steer, brake and reverse; they no longer use a direct Rigidbody velocity assignment as proof of driving input.
- Native X11 keyboard event probe: W and D produced throttle/steering, S produced reverse, Space was sent for braking, and Escape paused/resumed. Recorded maximum speed 4.42 m/s and movement 4.06 m in this short automated probe (`native-input-results.json`). This proves event delivery and movement, not performance or balance. Probe ran before the final signage-only adjustments; the control/input sources were unchanged by those adjustments.
- Final menu, city HUD and rainy city screenshots inspected at 1280 x 800. Fixed oversized/always-on-top world lettering, foreground camera obstruction and text readability issues.
- Player logs checked for runtime exceptions during capture and regression: none found.

## Changes prompted by validation

- Shared direct-key input avoids axis configuration dependency; GUI key-state fallback and mouse-held driving buttons are present.
- Zero-friction body contact plus explicit tyre grip and non-alloc ground detection excluding the player's own body allow acceleration from rest.
- Background/focus behavior is explicit. Normal sessions pause on focus loss; the menu says to click the game window. Keyboard testing targets the actual client window, not a window-manager frame with the same title.
- Added detailed facades/street furniture and rounded kombi geometry. World text uses depth testing and stays outside static batches so dynamic font atlas changes do not corrupt signs.

## Reproduce

Build with `KombiShift.JaydenStarter.Editor.StarterBuild.Linux`. For regression launch with `--kombi-smoke --kombi-exit --kombi-report=/absolute/path/report.json`.

For native input, close other preview windows, launch the graphical player with `--kombi-input-probe --kombi-exit`, then run `python3 UnityStarter/Validation/native_keyboard_probe.py` on the same X11 display. It focuses the owned game client and sends the native key sequence. Read `Builds/input-probe.json` after the player exits. This is automated testing, not an authentic human playtest.

## Still unverified

Human playtesting/handling preference, long-session traffic/camera edge cases, gamepad/touch controls, profiler comparisons, team-pinned Unity/HDRP/URP, Windows and Android/Web builds. The new label shader targets Built-In; another pipeline needs its own compatible label material. Art, animation and audio are procedural rather than final external production assets.

Generated signing/passcode/certificate-password fields are cleared before repository export. Binaries, caches and machine logs are not committed.
