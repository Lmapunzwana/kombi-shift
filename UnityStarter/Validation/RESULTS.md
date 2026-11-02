# Verification — 2 November 2026

Unity 6000.6.2f1 Built-In standalone concept adaptation.

- Linux automated concept regression: **40 passed, 0 failed**; report `concept-results.json`.
- Windows x64 Mono executable: **40 passed, 0 failed under Wine 11.20**. Native Windows hardware remains untested.
- Checks cover managers, destinations, pickup bounds, stable braking, multiple boarding, pause, onboard patience, locked fares, matching drops, tips, late passengers, paid fuel, pool reuse, target/timeout/fuel/health outcomes, highway economy and weather, rival states, audio and replay.
- WebGL build succeeds. The final browser player renders the city, HUD, passenger manifest and menus; the previous blank-world failure is corrected by preserving runtime-created components. Browser automation observed movement and service outcomes, but is not a full keyboard/touch-device certification.
- Earlier native keyboard checks established acceleration, movement and braking. Automated tests are not human usability or balance tests.

## Controlled pooling measurement

Development player; 240 sampled frames per mode after warm-up, identical CBD route x=-3/z=0..100, fixed 60 Hz simulation, nine traffic vehicles plus one rival, twelve pedestrians and six hazards. One pedestrian and hazard recycle per frame. Baseline creates/destroys those objects; pooled mode reuses them. Traffic stays pooled in both modes to isolate this change. This is a controlled headless churn fixture, not a full human-driven route benchmark.

Mean GC allocated per frame: baseline 916.63 bytes, pooled 445.50 bytes (about 51.4% lower). Mean wall frame time: 16.73 ms versus 16.67 ms; p95 18.16 ms versus 17.97 ms. Frame timings include the frame cap, so these numbers do not establish a general FPS improvement. Profiler counters were valid. JSON and frame CSVs are retained here; local binary captures are in Builds/ProfilingEvidence/baseline.raw and pooled.raw.

Human tests, Android/phone testing and foundation HDRP integration are not claimed. Audio uses credited recording plus temporary synthesized lines; replace with reviewed local voice recordings for production.
