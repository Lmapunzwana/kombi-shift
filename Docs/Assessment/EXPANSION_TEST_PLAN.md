# Expansion test and human evidence plan

Use Unity 6000.6.2f1 with BranchProjects/KombiShift2. Preserve historical results. Fresh evidence goes to Docs/Validation/Expansion. Headless tests do not establish rendering, sound audibility or driven comfort.

## Automated commands

Pass -projectPath with this Unity project folder. Use -batchmode -nographics for physics/rule checks. Keep -quit only for synchronous editor methods, not play-mode runners.

- -executeMethod KombiExpansionValidation.Prepare -quit: editable stage assets, scene audit and rule/component fixtures.
- -runTests -testPlatform EditMode -testFilter ExpansionRulesTests -testResults /absolute/path/results.xml: Unity Test Framework rules.
- -executeMethod KombiExpansionVerification.Run: physical rival observation then staged boarding, intermediate alighting, requests, fare, XP, fine, repair, completion, timeout and reset.
- -executeMethod KombiAssessmentVerification.Morning or .RushHour: existing 17-check regression extended for delivery fares and stage pricing. Fresh outputs use Expansion/Regression.
- -executeMethod KombiPowertrainVerification.Run: existing driven powertrain fixture; inspect its dated output location.
- -executeMethod KombiExpansionBuild.Linux / .Windows / .Web -quit: export separately to Builds/Expansion2026, preserving prior releases.

Never apply ShiftExpansionUpgrade.Apply twice. The scene already contains the upgrade. Refine is idempotent waypoint densification. Configure stage assets in Assets/Resources and road rules on Shift Expansion 2026 children. Test changes before saving them as balance evidence.

## Required physical/manual tasks

Record build hash, date, platform/GPU/browser, anonymous tester ID and observed outcome. T01–T03 are placeholders, not participants.

1. Find both stages and explain delivery income versus boarding. Complete Morning without facilitator driving.
2. Stop at a queue, observe walkers and concurrent seat allocation. Depart during boarding; check uncollected passengers return.
3. Follow a stop request, brake and alight; confirm cabin count falls, cumulative delivery and fare rise. Deliberately miss one request and recover at another station.
4. Drive safely, then brake/steer harshly; compare XP/happiness and rate limiting. Confirm tone is audible at different volume settings.
5. Observe each rival from departure to a real pickup and departure; compare colours, names, marker and queue reduction. Watch conservative versus aggressive following/passing on both stages.
6. Follow a slow vehicle. Observe a pass only with clearance; add oncoming traffic and verify inhibition. Observe lane return, junctions, endpoint turns and prolonged blockage. Report any collision or path deadlock.
7. Cross a hump at 10, 20 and 40 km/h using RCC. Record wheel contact, peak vertical acceleration, launch/roll risk, comfort and condition. Repeat after any collider changes.
8. Cross speed/stop/no-entry/direction/no-passing zones; verify one fine per incident and no repeated continuous-speed charging. Check intersections and sign readability both directions.
9. Make minor and severe impacts. Check proportional damage, controllability and lower power. Try insufficient-funds repair, paid repair and moving/remote repair rejection.
10. Pause/resume, camera/orbit, volume, restart and results on 900×600, 1280×720 and target resolutions. Read alerts without relying on colour alone.
11. Compare Rush pressure, traffic and higher fares. Record completion time, waiting, misses, rival collections and net earnings.
12. Profile ordinary gameplay on the weakest machine before/after a measured optimisation, including CPU/GPU frame times, GC/memory and build. Headless fixture timings do not substitute for this.

Use existing PLAYTEST_SESSIONS.csv and FEEDBACK_FIX_RETEST.csv. Record at least three genuine findings, severity, actual fix/file/commit, and the same task retested. Technical automation findings are not human feedback. Record actual textbook editions and member contributions separately. Native Windows, browser, fresh import and licence clearance remain team evidence requirements.
