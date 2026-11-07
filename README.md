# Kombi Shift

A complete playable 3D Harare-inspired shift game, prepared with AI assistance for team review.

## Open locally in Unity

Install **Unity 6000.6.2f1**, then add **UnityHDRP** in Unity Hub for the HDRP desktop project, or **UnityStarter** for the Built-In desktop/WebGL project. Open `Assets/KombiShiftJayden/Generated/KombiShiftStarter.unity` and press Play.

On Jayden's Linux machine, run `./Open_Kombi_Unity.sh` (HDRP) or `./Open_Kombi_Unity.sh builtin`. The **Kombi Shift > Open Game Workbench** menu provides start-level, park-at-stop, service, damage/restore and live-state inspection tools. `Generated/GameTuning.asset` contains persistent balance settings. `Generated/ArtWorkshop.unity` and Catalog prefabs make the models inspectable outside Play.

WASD/arrows drive, Space stops and holds, E boards/drops/refuels, H horns, R recovers ($1), Escape pauses. Click map stops to select a route. At the station a $3 repair restores health and brake/suspension condition. Use pause for audio, text scale and optional touch controls.

## What is implemented

Two materially different stages; six-seat destination/patience manifests; locked distance fares and quick tips; paid fuel/repair; health and separate brake/suspension wear; a four-state rival; three traffic types; pooled pedestrians/hazards/traffic; visible hwindi and sliding-door service; real passenger Animator transitions; complete menus/outcomes/replay; best scores; attributed audio; HDRP day/rain profiles and a browser-compatible fallback.

## Understand and verify

- [Editor walkthrough](Docs/Team/EDITOR_WALKTHROUGH.md)
- [Every sprint task mapped](Docs/Team/TASK_COMPLETION_MATRIX.md)
- [AI and asset declaration](Docs/Team/AI_AND_ASSET_DECLARATION.md)
- [Validation](UnityStarter/Validation/RESULTS.md)
- Submission folder: illustrated PDF/editable report draft, test record templates, evidence and offline defence deck. Source/build ZIPs are local deliverables, not Git assets.

The earlier PR31 has been merged. Subsequent completion work is proposed separately for review. The separate 6000.3/HDRP foundation is not silently overwritten. Do not mix render-pipeline ProjectSettings. Gameplay folders are shared deliberately between the supplied projects; HDRP-specific code/resources remain in UnityHDRP.

## Honest remaining evidence

The code and automated checks do not establish human usability, authentic contributions, native Windows hardware compatibility, weakest-laptop performance, lecturer approval or final balance. No tester bookings/results are fabricated. The conductor speech is still a credited synthesized English placeholder. The early WheelCollider task is adapted to the sprint plan's allowed arcade physics, not represented as WheelCollider simulation. Each member must review and explain the code personally claimed.
