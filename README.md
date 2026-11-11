# Kombi Shift 2 - Group 6

**Team learning starts at [Docs/Assessment/START_HERE.md](Docs/Assessment/START_HERE.md).**

The current assessed project is this folder. Use Unity **6000.6.2f1** and open `Assets/KombiShift2/Scenes/KombiMainMenu.unity`, or choose **Kombi Shift > Play Main Menu**.

Choose **Trip Market**, select one of nine fictional routes and **Morning Shift** or **Rush Hour**, then choose **Start Trip**. Routes use the preserved city, left-side stops, individual destinations and physical rival/civilian traffic. Fares and demand depend on the selected route and stage. Direct driving-scene entry without a selection retains the older seven-stop scenario.

WASD/arrows drive, Space parking brake, S holds to reverse, RMB orbit, wheel zoom, C camera, L lights, V wipers, Escape pause, R restart. Stop below 2 km/h at the active stop and hold Space; boarding is automatic. Deliver passengers at their requested stops and the selected terminal. Fares are credited after alighting; the HUD separates earnings, fines and repairs. T repairs at stops.

Start with `Docs/Assessment/LEARN_AND_DEFEND.md` for explanations and live-change practice, `Docs/Assessment/CURRENT_DESIGN.md` for the current GDD/report, and `Docs/PROJECT_GUIDE.md` for file navigation. `Docs/Validation/Assessment` contains new staged integration and traffic-query measurements. Historical validation remains separately labelled.

`../../Submission2026` contains current report, slides, tester worksheets and release preparation. The earlier workspace `Submission`, `UnityStarter` and `UnityHDRP` are different versions; their feature claims do not describe this game.

Human playtesting, feedback-based fixes/retests, actual contributions, assigned-book reading, target-machine checks and dependency source hand-in permissions require real evidence. See `Docs/Assessment/AI_AND_ASSET_DECLARATION.md`. No such evidence is invented.

## 9 November service and route expansion

The current source extends the existing roads to approximately 1.25 km/seven stops, adds physical rival driving and utility decisions, individual trip destinations, requests, intermediate alighting, happiness/XP, traffic fines, vehicle condition and T repairs at stops. Fares pay on completed delivery: $2 Morning, $3 Rush. See `Docs/Assessment/EXPANSION_ENGINEERING_REPORT.md` and `EXPANSION_TEST_PLAN.md` for validation scope and remaining gaps. Stage settings are editable under `Assets/Resources`. Existing submission snapshots and prior builds describe the earlier release; new exports use `Builds/Expansion2026` in the parent workspace.

## 11 November existing-city infrastructure

Current implementation and executed evidence: [engineering report](Docs/Assessment/CityInfrastructure/ENGINEERING_REPORT.md). Linux and WebGL builds succeeded; scene, menu and physics fixtures passed 102 checks. Native controls passed. WebGL menu loading was observed; browser driving and human tester records remain pending.

Local Linux build: run `bash Tools/run_city_linux.sh` from this repository. Builds are workspace artifacts under `../../Builds/CityInfrastructure2026`, not committed binaries. For source playback, open the main menu in Unity.
