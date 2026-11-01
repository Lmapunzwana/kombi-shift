# Jayden’s Kombi Shift preview

Open **UnityStarter** as its own Unity 6000.6.2f1 project. Open the included generated scene and press Play. A local Linux build is started with `./Run_Kombi_Shift.sh` when present.

[Setup and controls](UnityStarter/README.md) · [Validation](UnityStarter/Validation/RESULTS.md) · [Art direction](Docs/Jayden/README.md) · [Integration notes](Docs/Jayden/LOCAL_PREVIEW.md)

The starter includes a procedural kombi/city, arcade physics, passengers/fare economy, rival traffic, two weather/difficulty settings, HUD/menus, audio controls and shift completion/replay. Its Linux release build passed 32 control/gameplay regression checks and a native keyboard-event probe. It is a generated prototype for review, not the team’s finished game or assessed human playtest evidence.

This nested project deliberately keeps the team foundation’s pinned Unity/HDRP configuration separate. Import its scripts/assets through the documented scene-builder command only after review. Build outputs, caches and raw machine logs are not committed.

The latest revision adds stable brake hold, a visible hwindi/sliding-door service sequence, real recorded engine sound and temporary synthesized calls. Arrows and cast shadows are removed and the palette is subdued. Press Space to stop/hold, E to board/alight, H for horn; Menu > Test Sound checks game output.
