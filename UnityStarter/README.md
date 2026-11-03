> Complete project update, 3 November: see [root guide](../README.md) and [Editor walkthrough](../Docs/Team/EDITOR_WALKTHROUGH.md). Separate wear/paid repairs, Inspector tuning/audio, prefab-backed passengers, real Animator transitions and an Art Workshop are now included. HDRP desktop lives in ../UnityHDRP.

# Kombi Shift — playable 3D concept adaptation

Open this folder in Unity 6000.6.2f1, open `Assets/KombiShiftJayden/Generated/KombiShiftStarter.unity`, and press Play. The world is generated at runtime. This is an isolated Built-In project; do not upgrade or overwrite the team foundation project to import it.

## Play

Run `../Run_Kombi_Shift.sh` on Linux. Windows output is `../Builds/Windows/KombiShift.exe`. Serve `../Builds/Web` over HTTP for WebGL; do not open its HTML through file://.

W/S accelerate/reverse, A/D steer, Space brakes and holds, E services the selected stop or nearby fuel station, H horns, R recovers for $1, Escape pauses. Click stop markers on the map to select a stop. Touch steering and brake controls can be enabled in pause/settings. HUD text scale and separate sound volumes persist.

## Implemented loop

Choose CBD morning or rainy highway and optionally hire a visible hwindi. Stop inside a pickup trigger and board up to six individual passengers. Each has a destination, quoted fare and patience timer. Deliver matching passengers promptly for fares and tips; late passengers pay nothing. Fare quotes use route distance and lock when boarded. A higher fare multiplier can discourage boarding. Fuel is purchased from cash at a station. Collisions and potholes reduce health. Earn the target before time, fuel or health runs out; a clean shift earns a score bonus.

CBD has six stops on connected streets, shorter routes, 100 starting fuel and an $18 target. Highway has four widely spaced stops, wet grip, more potholes, 45 starting fuel, higher fuel consumption and a $28 target. Both shifts last 300 seconds. These are initial balance values, not human-tested findings.

The rival follows road waypoints and competes for waiting passengers through Cruise, Chase Passenger, Block and Flee states, with front obstacle casts and its own damage response. Three traffic vehicle types, waiting pedestrians and potholes are reused through pools.

## Source

`Runtime/Systems` contains GameManager, VehicleController, PassengerStop, PassengerAI, RivalAI, FuelSystem, SpawnManager, UIManager, AudioManager, RoadGraph and level definitions. GameManager publishes OnFareEarned, OnFuelChanged, OnVehicleDamaged and OnShiftEnded. `KombiDriver` is the compatibility bridge to VehicleController. ShiftGame partial files assemble the procedural environment, UI, conductor/audio, gameplay and diagnostics.

`Editor/StarterBuild.cs` exposes Linux, Windows, Web and Profile build entry points under `KombiShift.JaydenStarter.Editor.StarterBuild`. `link.xml` preserves components created at runtime for IL2CPP. The scene generator is available through Kombi Shift > Jayden > Create Starter Scene.

## Presentation and limits

The user chose to keep 3D and use map/stop markers without a guidance arrow. Cast shadows remain disabled by request. Procedural vehicles, buildings and characters are prototype art, not attributed sprite packs or finished production models. Engine audio includes an attributed bus recording; hwindi lines are temporary synthesized English speech, not a recorded Shona performance. Audio credits and preparation scripts are included. An original generated percussion/marimba-style loop and rain accompany the effects.

Linux and Windows-under-Wine automated checks pass 40/40. WebGL builds successfully and its final browser scene/menu renders. This is not a native Windows hardware test or phone test. See Validation for measured pooling evidence and the exact test scope. Human playtesting, balancing, production art/voice work, team HDRP integration and panel preparation remain. AI-assisted implementation must be reviewed and understood by Jayden before claiming personal authorship.
