# Verification - 3 November 2026

Unity 6000.6.2f1. Built-In desktop/WebGL and HDRP17.6 desktop projects.

- Actual local Unity Editor Play Mode: **49 passed, 0 failed** in both projects.
- Final Built-In Linux release: **49 passed, 0 failed**.
- Final Windows Mono release under Wine: **49 passed, 0 failed**. This is not native Windows hardware verification.
- HDRP Linux build succeeds; actual Vulkan rendering of menu, CBD, rainy highway and conductor service captured and inspected. Corrected exposure, sky brightness and linear-colour UI after the first overexposed capture. Engine/call clips loaded; actual audio mixer RMS is nonzero at 48 kHz.
- WebGL builds successfully; final browser checks recorded separately in the handoff. Phone testing is not claimed.

Regression includes driving/steering, stable stop hold and push resistance, pickup bounds, multiple passengers, pause, individual patience, locked fare quotes, correct destinations/tips, late unpaid exits, paid fuel, pool reuse, all outcomes, level differences, rival states, worn-brake stopping distance, suspension damage/reset, manager state changes, real Animator walking, station repair costs and location restrictions.

## Final controlled pooling fixture

Baseline mean allocation 988.65 bytes/frame; pooled 445.50 (54.9% lower). Mean wall frame time 16.71 vs 16.65 ms; p95 17.80 vs 17.67 ms. Counters valid.

240 samples per mode after warm-up, identical CBD x=-3/z=0..100 route, fixed 60 Hz simulation, nine traffic vehicles plus rival, twelve pedestrians and six hazards. One pedestrian/hazard recycles per frame. Traffic stays pooled in both modes. Final isolated run completed after other test players exited. This is a controlled headless Built-In fixture, not human gameplay or final HDRP GPU/weakest-laptop performance. Frame timings include the cap and do not establish a general FPS gain. Local raw captures remain under Builds/ProfilingEvidence.

## Remaining verification and known Editor issue

Unity's internal SearchDatabase startup indexing emitted an ArgumentOutOfRangeException in batch Editor runs; gameplay checks still completed. No game-code exception was found in final native test/capture logs. Do not label the Editor Console completely warning-free.

Human playtesting, three authentic feedback-based fixes/retests, independent review, actual student contributions, native Windows hardware, phones, weakest team laptop and clean-machine checks remain to be recorded honestly. The course-book editions actually consulted and team registration numbers must be provided by the team.
