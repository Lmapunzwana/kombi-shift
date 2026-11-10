# Kombi Shift 2 expansion — engineering report

10 November 2026. Unity 6000.6.2f1. Branch `feat/kombishift2-unity6`. Earlier releases and submission snapshots are preserved. This report describes substantial implementation work; it does not claim all 100 rubric marks or the complete requested definition of done.

## Implemented systems and changed source

| Area | Files under Assets/KombiShift2 | Result |
|---|---|---|
| Competition | Scripts/Traffic/RivalKombiAI.cs; RivalDrivingProfile.cs; KombiTrafficVehicle.cs | Demand/travel/congestion utility, hysteresis, physical wheels and torque, faster competitive profiles, signed left-lane driving, signal/zone caution, visible colours/activity |
| Passenger service | Scripts/Gameplay/PassengerTrip.cs; PassengerStation.cs; KombiShiftGameDirector.cs | Individual player trips, downstream destinations, bell/text requests, intermediate animation, delivery fares, one-shot missed penalty and recovery |
| Service quality | Scripts/Gameplay/KombiServiceQuality.cs | Separate XP and bounded passenger happiness, cooldowns, motion/collision/law reactions and smooth-service rewards |
| Condition | Scripts/Vehicle/KombiVehicleCondition.cs; RccHiaceTestBootstrap.cs | Impact speed/impulse filtering, bounded damage, throttle/brake adapter, paid stopped-service repair |
| Law | Scripts/Gameplay/KombiTrafficLaw.cs; KombiRoadRuleZone.cs; KombiRoadRules.cs | Configurable zones, speed grace, continuous offence incidents, stop/no-entry/direction/passing detection and offence records |
| World/stages | Scenes/KombiShift_FirstShift.unity; Editor/Upgrades/ShiftExpansionUpgrade.cs; Art/Generated; Scripts/Gameplay/KombiStageSettings.cs; KombiChallenge.cs; Assets/Resources | 674.2 m → 1,249.3 m itinerary (85.3% longer), 127 waypoints, seven stations, navigation for player/rivals/traffic, roadside signs, full-corridor hidden tree-collider repair, shared physical hump mesh, stage traffic/fare/rival challenge differences |
| UI | Scripts/UI/KombiNavigationHud.cs; KombiMainMenu.cs | Existing design extended with requests, XP/happiness, condition, limit, fines, deliveries, rival proximity and repair help |
| Verification | Tests/Editor; Scripts/Development/ExpansionVerification.cs; RoadPhysicsVerification.cs; ExpansionVisual.cs; updated AssessmentVerification.cs; Editor/Validation/ExpansionValidation.cs; ExpansionBuild.cs | Repeatable component, UTF, physical scene, RCC hump, regression, render and build workflows |

Docs/Assessment updates include current design/GDD, storyboard, event/state diagrams, traceability, readiness, test instructions, defence exercises and AI/asset declaration. Full changed-file inventory is in Docs/Validation/Expansion/changed-files.txt. Existing PDF, deck, source hand-in folders and packaged ZIP have not been silently replaced; they need regeneration after review.

## Validation

Current evidence is under Docs/Validation/Expansion. Historical results retain their original scope/date.

- Rule/component fixtures: **25 passed, 0 failed**.
- Unity Test Framework EditMode: **15 passed, 0 failed** (`utf-editmode.xml`).
- Final actual-scene expansion integration: **28 passed, 0 failed** (`integration.json`). It observes real rival motion, shared-pool boarding, departure and then uses staged player positions for service/economy/reset checks.
- RCC hump physics: **5 passed, 0 failed** (`road-physics.json`). Safe staged crossing: 11.82 m progress, maximum observed vertical speed 1.34 m/s, health 100. Fast staged crossing: 48.92 m progress, health 96.79, upright. These are headless controlled observations, not a comfort study or a general no-launch guarantee.
- Initial expansion integration: 21 passed / 2 failed, retained as `integration-first-run.json`. Two >30 m waypoint gaps were densified. The repair fixture's insufficient funds were corrected by choosing damage affordable from its actual earned balance. Retest then passed 26/0; final departure assertions increased coverage to 28/0.
- Existing stage regressions, powertrain, visual inspection and export results: see final status appended below and pipeline.json. The fare assertions were updated for the deliberate boarding-to-delivery income change, with higher Rush fares. New results use a separate folder.

No human tester, feedback, contribution, textbook reading or member exercise has been fabricated. Rule fixtures and staged integration are not a human-driven complete shift. A successful export is not evidence of native hardware, browser or accessibility usability.

## Build and execution status

New build output root: `Builds/Expansion2026` in the parent workspace. Existing `Builds/Assessment2026` releases are preserved. Each build writes platform/result/error/warning/size/time metadata under Docs/Validation/Expansion. See appended final status for actual outcomes. Current source must be used to import/run the expanded scene; previous binaries contain the earlier rules.

## Known limitations and remaining definition-of-done work

1. The expanded map is a connected bidirectional itinerary using three existing road splines. It is not a general branching lane graph, and this work does not add an independently routed alternate street network or certify all road widths/intersections/endpoint turns. Blocked rivals wait and reconsider demand; they cannot plan an alternate road. No teleport recovery is used.
2. Rivals remain in their designated lane and do not overtake into opposing traffic. Endpoint-turn, intersection-priority and long deadlock scenarios still need deterministic integration coverage and human review. Current tests prove physical pickup/departure, not every manoeuvre's safety.
3. Conservative/competitive/aggressive differences and more competitive stage pace/head starts are implemented, but targets and risk parameters are initial tuning, not measured balance results. A unified complete rival offence/economy log is not implemented.
4. Give-way has caution/speed behaviour; it is not a complete reliable yielding-offence detector. Signs use lightweight readable text rather than a full authored sign-art set. Zone extents and player passing classification require route playtesting.
5. Player passengers have individual intermediate trips. Rival passengers still follow their rival toward Mbare. Completed passenger records retain satisfaction, but there is no separately presented aggregate end-of-trip star rating.
6. Condition reduces power/acceleration and braking with controllability bounds. Suspension discomfort/damage is represented through hump consequences, not a complete per-wheel alignment/suspension wear simulator. Dedicated severe-damage smoke/impact VFX/engine-audio changes remain polish work.
7. New whole-game before/after CPU/GPU optimisation profiles and weak-machine results are not claimed. Existing historical traffic-query measurements are controlled editor evidence. Mesh/material reuse, nonalloc casts and half-second manifest sampling are implementation choices pending full gameplay profiling.
8. Legacy fixture assumptions were updated for the expanded stations, destination-aware progression, delivery fares and saved camera settings; only results listed as executed establish current validation. Complete cross-platform, resolution, sound, accessibility and long-session testing remains necessary.

## Rubric evidence still requiring the team

| Rubric area | Remaining evidence/work |
|---|---|
| Design/GDD 12 | Human balance review; regenerate illustrated PDF from current design |
| Storyboard/prototype 8 | Updated current storyboard is supplied; genuine early prototype feedback remains historical evidence to supply |
| Architecture 17 / core 13 | Further manoeuvre, junction, long-shift and nonregression coverage; member understanding |
| AI/level/progression 10 | Branching lane-graph scope gap, endpoint/junction tests, human difficulty balance |
| UI/art/animation/audio 9 | Human audibility/accessibility/legibility; lecturer-specific Animator expectations; remaining damage polish |
| Playtesting/iteration 9 | Three authentic testers, observations/severity, three observation-linked fixes and same-task retests |
| Optimisation/deployment 7 | Actual gameplay before/after profiling, native Windows/weak-machine/browser checks and fresh import |
| Report/source/evidence 5 | Regenerate PDF/deck/submission snapshots, verify asset source permissions and final inventory |
| Panel defence 10 | Every member personally practises and records actual understanding, contribution and live parameter exercises |

Use EXPANSION_TEST_PLAN.md, EXPANSION_DEFENCE.md and the genuine tester CSVs. Resolve existing imported-asset source hand-in permission. Local commits and builds do not grant redistribution rights. No shared-history rewrite is used; the completed source/evidence commits are to be pushed to the requested branch.

## Final platform and regression status

| Executed check | Result | Scope |
|---|---|---|
| Expansion rule/component fixtures | 25/0 | Editor component/rule assertions |
| Unity Test Framework EditMode | 15/0 | Automated rule tests |
| Expansion scene integration | 28/0 | Physical rival pickup/departure plus staged player service |
| RCC hump physics | 5/0 | Safe/excessive staged crossings |
| Existing Morning regression | 18/0 | Editor staged integration |
| Existing Rush regression | 18/0 | Editor staged integration |
| Legacy passenger service | 26/0 | Expanded queues, physical rival collection, cancellation and delivery |
| Grounding after repairs | 18/0 | Dynamic physics with explicit controlled impacts |
| Existing powertrain | 11/0 | Dynamic wheel driving, gearing, RPM, governor and braking |
| Rendered menu/camera | 22/0 | Normal graphics-enabled Editor, saved PNGs |
| Linux standalone Morning / Rush | 18/0 each | Native Linux process, headless staged verification |
| Windows executable Morning / Rush | 18/0 each | Final repaired export under Wine, not native Windows hardware |
| WebGL Morning / Rush | 18/0 each | Actual browser; before the final scene-only collision/spawn repairs |

The first graphics-enabled *batch* capture produced no PNGs despite exit 0; it is not counted as a visual pass. A normal graphics window subsequently produced the saved screenshots. The first menu/camera run retained a failed zoom assertion that assumed a distance above 8 m regardless of saved settings. The fixture now checks the actual requested delta and clamp without changing the user's preference; its retest passed 22/0. These are technical fixture findings, not human playtest feedback.

| Export | Result | Errors | Warnings | Size reported by Unity |
|---|---|---:|---:|---:|
| StandaloneLinux64 | Succeeded | 0 | 59 | 394,778,802 bytes |
| StandaloneWindows64 | Succeeded | 0 | 59 | 395,244,885 bytes |
| WebGL | Succeeded | 0 | 64 | 200,190,952 bytes |

Release runtime systems originate in commit 593b134; final scene repairs and rival spawn changes are included through aaa41b5. Refreshed exports were built after those scene changes. Scene GUIDs and vendor dependencies were retained. The parent workspace launcher now prefers the validated expansion Linux binary, with the earlier release as fallback. Parent README/launcher edits are workspace changes outside the nested game repository. Push to the requested branch is authorised; the commit history records the reviewable changes.

Browser first-load scene import was slow, and simultaneous Editor/browser work caused memory pressure. The project has runInBackground disabled; a hidden browser can pause a verification run. These constraints must be considered when reproducing browser timing. Audio metadata loading messages were observed during WebGL loading; real audibility is still a human check.

## Collision regression found by the legacy service suite

The adapted legacy suite initially passed 23 checks and failed three linked rival/queue checks. The rival stopped 11 m before Arterial Junction, grounded on all four wheels with zero motor torque and active brakes. Its sensor reported `tree1Plane_001 589` 2.16 m ahead. The rendered road was visibly clear: an imported alpha-cutout tree-plane collider projected into the road corridor. This was a real newly activated-road collision defect, not a passenger-reservation failure.

`ShiftExpansionUpgrade.RepairRouteTreeColliders` disables affected scene-instance billboard mesh colliders and adds narrow trunk capsules; imported art, asset files and scene GUIDs stay intact. Sixteen oversized plane colliders near the route were replaced. The rival then physically reached the second stop, boarded all six people and departed. The legacy service retest passed **26/0**, including cancellation/return, shared queue depletion, seat allocation, minimap layout and final delivery. Its initial failed JSON and physical sensor logs are retained. These are engineering observations, not authentic tester feedback.

All three platform exports were refreshed after the scene repairs; final-check-pipeline.json records their successful completion. Earlier platform execution records remain separately scoped to the pre-repair exports; final native Linux smoke results identify the refreshed release.

## Final repaired-scene verification

- Legacy service: **26 passed / 0 failed**.
- Grounding: **18 passed / 0 failed**, including dynamic boarding, steering, reverse, kerb/traffic-body impacts, wheel contact and no recovery teleport.
- Expansion integration: **28 passed / 0 failed** after the final scene changes.
- Linux, Windows and WebGL refreshed exports all succeeded; final build summaries and content identities identify their exact outputs.

The initial grounding run passed 17/18; its player hit a waiting rival before the steering exercise. Delayed rivals now start behind the player, preserving the intended clear head start. The controlled handling fixture isolates background vehicles and measures peak heading change through opposing turns, while retaining explicit kerb and traffic-body collision tests. Its retest passed 18/18. The route now has 127 points and measures 1,249.3 m versus 674.2 m originally (85.3% longer).

Current report PDF: output/pdf/KombiShift2_Expansion_Report_2026-11-10.pdf. Earlier illustrated report, deck and submission ZIP/source copies remain historical snapshots; update the final hand-in package and deck from this current source.

The refreshed Linux export passed **18/0 in each stage** (`native-linux-final-Morning.json`, `native-linux-final-RushHour.json`). The current dated PDF is provided alongside this Markdown source; regenerate it with `Tools/BuildExpansionReport.py`.

The refreshed Windows export also passed **18/0 per stage under Wine** (`wine-final-Morning.json`, `wine-final-RushHour.json`). WebGL feature integration passed before the final scene-only changes; its refreshed export succeeded, but that last browser run was not repeated. Native Windows hardware and weakest-device checks remain outstanding.
