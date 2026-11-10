# Kombi Shift 2 - Group 6

**Team learning starts at [Docs/Assessment/START_HERE.md](Docs/Assessment/START_HERE.md).**

The current assessed project is this folder. Use Unity **6000.6.2f1** and open `Assets/KombiShift2/Scenes/KombiMainMenu.unity`, or choose **Kombi Shift > Play Main Menu**.

Choose **Morning Shift** or **Rush Hour** using the title selector / F2. Morning: 600 seconds, deliver 1+ passengers; Rush Hour: 420 seconds, deliver 6+, faster rivals and a shorter head start. Rivals compete for pickups while staying in their designated lane. Both use the current city route. Win Morning to access Next: Rush Hour from results; either stage is also selectable directly.

WASD/arrows drive, Space parking brake, S holds to reverse, RMB orbit, wheel zoom, C camera, L lights, V wipers, Escape pause, R restart. Stop below 2 km/h at the active stop and hold Space; boarding is automatic. Deliver at Mbare. $2 is credited per seated passenger and success adds a remaining-time bonus. Best earnings save separately per stage.

Start with `Docs/Assessment/LEARN_AND_DEFEND.md` for explanations and live-change practice, `Docs/Assessment/CURRENT_DESIGN.md` for the current GDD/report, and `Docs/PROJECT_GUIDE.md` for file navigation. `Docs/Validation/Assessment` contains new staged integration and traffic-query measurements. Historical validation remains separately labelled.

`../../Submission2026` contains current report, slides, tester worksheets and release preparation. The earlier workspace `Submission`, `UnityStarter` and `UnityHDRP` are different versions; their feature claims do not describe this game.

Human playtesting, feedback-based fixes/retests, actual contributions, assigned-book reading, target-machine checks and dependency source hand-in permissions require real evidence. See `Docs/Assessment/AI_AND_ASSET_DECLARATION.md`. No such evidence is invented.

## 9 November service and route expansion

The current source extends the existing roads to approximately 1.25 km/seven stops, adds physical rival driving and utility decisions, individual trip destinations, requests, intermediate alighting, happiness/XP, traffic fines, vehicle condition and T repairs at stops. Fares pay on completed delivery: $2 Morning, $3 Rush. See `Docs/Assessment/EXPANSION_ENGINEERING_REPORT.md` and `EXPANSION_TEST_PLAN.md` for validation scope and remaining gaps. Stage settings are editable under `Assets/Resources`. Existing submission snapshots and prior builds describe the earlier release; new exports use `Builds/Expansion2026` in the parent workspace.
