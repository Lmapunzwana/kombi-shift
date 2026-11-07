# Verification - 3 November 2026

Unity 6000.6.2f1. Built-In desktop/WebGL and HDRP17.6 desktop projects.

- Linux automated concept regression: **40 passed, 0 failed** in the prior run.
- Windows x64 Mono executable: **40 passed, 0 failed under Wine 11.20** in the prior run.
- Final native Windows 11 development player: **41 passed, 0 failed**; report `concept-results.json`. This includes the new RivalAI debug-label assertions.
- Checks cover managers, destinations, pickup bounds, stable braking, multiple boarding, pause, onboard patience, locked fares, matching drops, tips, late passengers, paid fuel, pool reuse, target/timeout/fuel/health outcomes, highway economy and weather, rival states, audio and replay.
- WebGL build succeeds. The final browser player renders the city, HUD, passenger manifest and menus; the previous blank-world failure is corrected by preserving runtime-created components. Browser automation observed movement and service outcomes, but is not a full keyboard/touch-device certification.
- Earlier native keyboard checks established acceleration, movement and braking. Automated tests are not human usability or balance tests.
- Actual local Unity Editor Play Mode: **49 passed, 0 failed** in both projects.
- Final Built-In Linux release: **49 passed, 0 failed**.
- Final Windows Mono release under Wine: **49 passed, 0 failed**. This is not native Windows hardware verification.
- HDRP Linux build succeeds; actual Vulkan rendering of menu, CBD, rainy highway and conductor service captured and inspected. Corrected exposure, sky brightness and linear-colour UI after the first overexposed capture. Engine/call clips loaded; actual audio mixer RMS is nonzero at 48 kHz.
- WebGL builds successfully; final browser checks recorded separately in the handoff. Phone testing is not claimed.

Regression includes driving/steering, stable stop hold and push resistance, pickup bounds, multiple passengers, pause, individual patience, locked fare quotes, correct destinations/tips, late unpaid exits, paid fuel, pool reuse, all outcomes, level differences, rival states, worn-brake stopping distance, suspension damage/reset, manager state changes, real Animator walking, station repair costs and location restrictions.

Windows 11 development player on Intel UHD Graphics 620; Unity 6000.6.2f1; 240 sampled frames per mode after warm-up; identical CBD route x=-3/z=0..100; fixed 60 Hz simulation; nine traffic vehicles plus one rival; twelve pedestrians and six hazards. One pedestrian and one pothole recycle per frame. Baseline creates/destroys those objects; pooled mode reuses them. Traffic stays pooled in both modes to isolate this change. This is a controlled automated churn fixture, not a human-driven route benchmark.

Mean GC allocated per frame: baseline 14,160.73 bytes, pooled 13,585.64 bytes (about 4.1% lower). Mean main-thread time: 21.08 ms versus 20.63 ms. Mean wall frame time: 21.27 ms versus 20.77 ms; p95 23.48 ms versus 23.18 ms. These are one measured run; the small timing differences do not establish a general FPS improvement. Profiler counters were valid. JSON, frame CSVs, and native `baseline.raw`/`pooled.raw` captures are retained in Validation. Runtime screenshots are in `TendaiEvidence`. Tendai still needs to capture Unity Profiler-window screenshots from the raw captures.
## Final controlled pooling fixture

Baseline mean allocation 988.65 bytes/frame; pooled 445.50 (54.9% lower). Mean wall frame time 16.71 vs 16.65 ms; p95 17.80 vs 17.67 ms. Counters valid.

240 samples per mode after warm-up, identical CBD x=-3/z=0..100 route, fixed 60 Hz simulation, nine traffic vehicles plus rival, twelve pedestrians and six hazards. One pedestrian/hazard recycles per frame. Traffic stays pooled in both modes. Final isolated run completed after other test players exited. This is a controlled headless Built-In fixture, not human gameplay or final HDRP GPU/weakest-laptop performance. Frame timings include the cap and do not establish a general FPS gain. Local raw captures remain under Builds/ProfilingEvidence.

## Remaining verification and known Editor issue

Unity's internal SearchDatabase startup indexing emitted an ArgumentOutOfRangeException in batch Editor runs; gameplay checks still completed. No game-code exception was found in final native test/capture logs. Do not label the Editor Console completely warning-free.

Human playtesting, three authentic feedback-based fixes/retests, independent review, actual student contributions, native Windows hardware, phones, weakest team laptop and clean-machine checks remain to be recorded honestly. The course-book editions actually consulted and team registration numbers must be provided by the team.
