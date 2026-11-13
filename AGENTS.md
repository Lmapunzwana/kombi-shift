# Kombi Shift 2 — agent context

This repository is a Unity 3D driving game inspired by Harare commuter kombis. This file is the shared project orientation for AI agents; use it alongside the relevant code and scene rather than assuming this is a web application.

## Open and run

- Unity version: **6000.6.2f1**.
- Main playable scene: `Assets/KombiShift2/Scenes/KombiMainMenu.unity`. The driving scene is `Assets/KombiShift2/Scenes/KombiShift_FirstShift.unity`.
- The editor menu **Kombi Shift > Play Main Menu** opens the game.
- Controls: WASD/arrows drive, Space parking brake/boarding, S reverse, C camera, right mouse orbit, wheel zoom, L lights, V wipers, Q left indicator, E right indicator, X cancel indicators, Escape pause, R restart, T repair.
- Unity project dependencies are in `Packages/`; do not edit package manifests unless needed for the task.

## Game loop and rules

Choose Morning Shift or Rush Hour, pick up passengers at roadside stations, and deliver them at Mbare before time expires. Passengers may also request intermediate stops. Fares are paid on delivery; the shift target, time limit, traffic amount, fare and rival challenge are configured in `Assets/Resources/MorningShift.asset` and `RushHour.asset`.

The player vehicle uses Realistic Car Controller Pro (RCC) with project-owned integration and condition scripts. Rival kombis use physical Rigidbody/WheelCollider driving, share passenger queues, and compete for pickups. Background traffic follows route points. Route geometry and station/sign/rule placement are authored by editor upgrade scripts and saved into the driving scene.

## Where to look

| Area | Source |
|---|---|
| Shift progression, trips, result screen | `Assets/KombiShift2/Scripts/Gameplay/KombiShiftGameDirector.cs` |
| Cabin layout and passenger posing | `Assets/KombiShift2/Scripts/Vehicle/KombiPassengerCabin.cs`, `Scripts/Gameplay/StationPassenger.cs` |
| Stations and passenger service | `Assets/KombiShift2/Scripts/Gameplay/PassengerStation.cs`, `PassengerTrip.cs` |
| Rival decisions, driving and lane offsets | `Assets/KombiShift2/Scripts/Traffic/RivalKombiAI.cs`, `RivalDrivingProfile.cs` |
| Traffic and road-rule zones | `Assets/KombiShift2/Scripts/Traffic/`, `Scripts/Gameplay/KombiRoadRuleZone.cs` |
| Navigation/menu and HUD | `Assets/KombiShift2/Scripts/UI/` |
| Scene/route upgrades | `Assets/KombiShift2/Editor/Upgrades/` |
| Scene and play-mode verification | `Assets/KombiShift2/Scripts/Development/`, `Assets/KombiShift2/Editor/Validation/` |
| Current design and user controls | `Docs/Assessment/CURRENT_DESIGN.md`, `README.md` |

Game-specific code belongs under `Assets/KombiShift2`. Avoid modifying vendor code under `Assets/Realistic Car Controller Pro` or `Assets/EasyRoads3D`; extend the project-owned integration instead. Scene upgrade methods may modify and save Unity assets, so run only the specific requested editor action and review the resulting scene diff.

## Passenger seating and station layout

- The player and both rivals have **12 passenger seats**, arranged in three columns and four rows. `KombiPassengerCabin.SeatLocalPosition` and `PassengerScale` define the shared layout. Keep scene vehicles and `Assets/KombiShift2/Prefabs/Vehicles/Player/Hiace_Player.prefab` consistent.
- Stylized passengers use 75% scale while onboard, with anchors above the vehicle floor. Alighting restores their original walking scale. Check actual rendered bodies, heads and feet against the bus interior; body bounding boxes alone do not establish a good fit.
- Each of the seven stations has two physical six-place benches. Every seated marker must have a bench beneath it. `PassengerStation.ClaimArrival` reserves free arrival positions; overflow passengers stand on the pavement rather than sharing an occupied seat or sitting in the air.
- Service points sit 1.55 m left of the forward route centre. Boarding requires the correct lane and heading. Rivals may pass only in a clear second same-direction lane on suitable wide streets; opposing-lane passing remains prohibited. Reverse traffic must restore its serialized travel direction at startup.
- **Kombi Shift > Repair Passenger Seats And Left Stops** repairs the saved driving scene and player prefab, backing them up under `Review/BeforeSeatingLane-*`. **Inspect Passenger Seats And Left Stops** checks the authored layout and renders disposable previews without saving staged passengers.

## Working with an open Unity Editor

Respect the user's need to keep Unity open. Do not close or terminate it, force Play mode to stop, discard unsaved changes, or remove its project lock to launch another Editor. Scene repairs must wait until Play mode stops and the driving scene is saved. Use the scoped repair menu or `SeatingLaneWorkbench`; if scripts have not imported, ask the user to use **Assets > Refresh**. Do not overwrite scene files externally while the Editor holds an older in-memory copy. Preserve unrelated recovery scenes under `Assets/_Recovery` and keep local repair backups out of commits.

## Validation and known boundaries

The project has Unity EditMode tests under `Assets/KombiShift2/Tests/Editor` and focused editor/play-mode verification runners under `Scripts/Development`. Prefer the smallest relevant Unity validation; do not treat historical files in `Docs/Validation` as proof that a new change passed. Human playtesting is not implied by automated verification.

Follow `.github/instructions/unittestMcp.instructions.md` for unit/component testing: use Unit Test MCP, and do not substitute terminal test commands when those tools are unavailable. Full-scene integration and visual verification retain their own workflow. The seating repair evidence is under `Docs/Validation/SeatingLane`: its recorded run passed 26 scene-service checks and inspected supported benches and full-cabin previews. Rerun relevant checks after subsequent implementation changes; do not claim this evidence validates newer work. Source changes do not regenerate standalone builds or update existing launchers automatically.

Selected trips now use a branching road graph derived from the existing city's road meshes. The older authored itinerary remains available for direct-scene regression scenarios. The original NYBlock02 city prefab, source assets and transforms must remain intact. Do not generate a replacement city or infer that the fictional Harare route names are geographically accurate.

## City infrastructure and trip market

- Open the main menu and choose **Trip Market**. Nine configured fictional routes have actual origins, terminals, intermediate stops, fares and demand. `Assets/Resources/CityTrips.json` controls the catalogue; `CityTripSession` turns the selection into actual gameplay. Direct Editor driving-scene entry without a selection retains the original scenario.
- `Assets/Resources/CityRoadNetwork.json` is baked navigation data. The current validated playable component has 4,191 nodes and 8,466 directional links. Other fragments are retained as documented exclusions; do not claim that every extracted road fragment is functional. Re-run the scene audit after geometry or graph changes.
- `CityRoadNetwork` supplies A* paths, directed approach searches without mid-road U-turns, road widths, speed limits, left-lane offsets and passing restrictions. Trip previews must follow all actual stop nodes, and cached route lists must not be cleared when switching cards.
- `CityTrafficFleet` pools physical Rigidbody/WheelCollider vehicles: 28 Morning, 48 Rush. Near vehicles use full physics; distant ones sleep and are reused offscreen. Driver profiles, interval-based decisions, following gaps, junction reservations, signals, stop/yield priorities and clockwise roundabout links control movement. Passing uses a second same-direction lane only, with clearance checks and no nearby junction.
- `CityInfrastructureUpgrade` reuses the 25 imported road-material meshes and one shared textured asphalt material. It retains original meshes in `CityRoadSurface.sourceMesh` and authors copied rendering/collision meshes with real pothole depressions. Current authored profiles contain 34 physical potholes. Do not leave an original flat collider filling a depression.
- White shared road markings are projected onto actual road surfaces. Earlier route-only carriage/edge overlays are disabled to avoid duplicate paint. Road markings have no colliders. Speed humps, signs and existing enforcement zones remain project-owned; Give Way is active again at serialized enum value 2 under the newer city specification.
- Tree artwork remains unchanged. Oversized transparent tree-card collisions are disabled in favour of narrow trunks; do not delete buildings or trees to make navigation pass.
- Preserve real suspension pitch/roll. `KombiVehicleStability` provides bounded rollover assistance instead of freezing those axes. `KombiRoadWear` samples actual wheel contacts; condition modifiers affect speed/power/brakes/steering and suspension without vendor edits.
- Player trips credit fractional fares only after actual alighting. Gross fares, net earnings, simulated fines and repair spending remain distinct. Passengers walk from their seats through the door; staggered egress holds the parking brake. Full city trips must still visit individual passenger destinations.

## City authoring and verification

Use **Kombi Shift > Apply Existing City Infrastructure** only on the saved driving scene outside Play mode. It backs up under `Review/BeforeCityInfrastructure`, preserves imported city sources and saves generated meshes/prefabs under `Assets/KombiShift2/Generated/CityNetwork`.

The reproducible pipeline is: Unity `KombiCityInfrastructureAudit.Run` → `Tools/build_city_graph.py` → Unity surface/clearance audit → `Tools/prune_city_lanes.py` → `Tools/build_city_trips.py` → scoped Unity upgrade/audit. Python baking uses numpy and Pillow; normal gameplay uses only the committed baked assets. Review `Tools/city_lane_exclusions.json` when geometry changes; do not blindly reopen previously unsafe links.

Current city evidence belongs under `Docs/Validation/CityInfrastructure`. Full-scene entry points include `KombiCityVerification.Run`/`Rush`, `CityRoadPhysicsVerification.Run` and `CityMenuVerification.Run`. Verification runners must guard against duplicate instances after scene reload. Build entry points are `KombiCityInfrastructureBuild.Linux` and `.Web`; native `--kombi-smoke` is an explicit automated startup flag. Builds are written outside the repository under the workspace's `Builds/CityInfrastructure2026`.

Read `Docs/Assessment/CityInfrastructure/ENGINEERING_REPORT.md` for executed results and remaining human/platform work. Historical PDFs, decks and expansion evidence do not establish that new city changes passed. Never fabricate tester records, contribution claims, benchmark results or successful builds.
