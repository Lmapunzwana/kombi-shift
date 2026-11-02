# Verification — 2 November 2026

Unity 6000.6.2f1 Built-In standalone concept adaptation.

- Linux automated concept regression: **40 passed, 0 failed** in the prior run.
- Windows x64 Mono executable: **40 passed, 0 failed under Wine 11.20** in the prior run.
- Final native Windows 11 development player: **41 passed, 0 failed**; report `concept-results.json`. This includes the new RivalAI debug-label assertions.
- Checks cover managers, destinations, pickup bounds, stable braking, multiple boarding, pause, onboard patience, locked fares, matching drops, tips, late passengers, paid fuel, pool reuse, target/timeout/fuel/health outcomes, highway economy and weather, rival states, audio and replay.
- WebGL build succeeds. The final browser player renders the city, HUD, passenger manifest and menus; the previous blank-world failure is corrected by preserving runtime-created components. Browser automation observed movement and service outcomes, but is not a full keyboard/touch-device certification.
- Earlier native keyboard checks established acceleration, movement and braking. Automated tests are not human usability or balance tests.

## Controlled pooling measurement

Windows 11 development player on Intel UHD Graphics 620; Unity 6000.6.2f1; 240 sampled frames per mode after warm-up; identical CBD route x=-3/z=0..100; fixed 60 Hz simulation; nine traffic vehicles plus one rival; twelve pedestrians and six hazards. One pedestrian and one pothole recycle per frame. Baseline creates/destroys those objects; pooled mode reuses them. Traffic stays pooled in both modes to isolate this change. This is a controlled automated churn fixture, not a human-driven route benchmark.

Mean GC allocated per frame: baseline 14,160.73 bytes, pooled 13,585.64 bytes (about 4.1% lower). Mean main-thread time: 21.08 ms versus 20.63 ms. Mean wall frame time: 21.27 ms versus 20.77 ms; p95 23.48 ms versus 23.18 ms. These are one measured run; the small timing differences do not establish a general FPS improvement. Profiler counters were valid. JSON, frame CSVs, and native `baseline.raw`/`pooled.raw` captures are retained in Validation. Runtime screenshots are in `TendaiEvidence`. Tendai still needs to capture Unity Profiler-window screenshots from the raw captures.

Human tests, Android/phone testing and foundation HDRP integration are not claimed. Audio uses credited recording plus temporary synthesized lines; replace with reviewed local voice recordings for production.
