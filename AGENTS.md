# Kombi Shift 2 — agent context

This repository is a Unity 3D driving game inspired by Harare commuter kombis. This file is the shared project orientation for AI agents; use it alongside the relevant code and scene rather than assuming this is a web application.

## Open and run

- Unity version: **6000.6.2f1**.
- Main playable scene: `Assets/KombiShift2/Scenes/KombiMainMenu.unity`. The driving scene is `Assets/KombiShift2/Scenes/KombiShift_FirstShift.unity`.
- The editor menu **Kombi Shift > Play Main Menu** opens the game.
- Controls: WASD/arrows drive, Space parking brake/boarding, S reverse, C camera, right mouse orbit, wheel zoom, L lights, V wipers, Escape pause, R restart, T repair.
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
- Service points sit 1.55 m left of the forward route centre. Boarding requires the correct lane and heading. Rivals queue without overtaking, and reverse traffic must restore its serialized travel direction at startup.
- **Kombi Shift > Repair Passenger Seats And Left Stops** repairs the saved driving scene and player prefab, backing them up under `Review/BeforeSeatingLane-*`. **Inspect Passenger Seats And Left Stops** checks the authored layout and renders disposable previews without saving staged passengers.

## Working with an open Unity Editor

Respect the user's need to keep Unity open. Do not close or terminate it, force Play mode to stop, discard unsaved changes, or remove its project lock to launch another Editor. Scene repairs must wait until Play mode stops and the driving scene is saved. Use the scoped repair menu or `SeatingLaneWorkbench`; if scripts have not imported, ask the user to use **Assets > Refresh**. Do not overwrite scene files externally while the Editor holds an older in-memory copy. Preserve unrelated recovery scenes under `Assets/_Recovery` and keep local repair backups out of commits.

## Validation and known boundaries

The project has Unity EditMode tests under `Assets/KombiShift2/Tests/Editor` and focused editor/play-mode verification runners under `Scripts/Development`. Prefer the smallest relevant Unity validation; do not treat historical files in `Docs/Validation` as proof that a new change passed. Human playtesting is not implied by automated verification.

Follow `.github/instructions/unittestMcp.instructions.md` for unit/component testing: use Unit Test MCP, and do not substitute terminal test commands when those tools are unavailable. Full-scene integration and visual verification retain their own workflow. The seating repair evidence is under `Docs/Validation/SeatingLane`: its recorded run passed 26 scene-service checks and inspected supported benches and full-cabin previews. Rerun relevant checks after subsequent implementation changes; do not claim this evidence validates newer work. Source changes do not regenerate standalone builds or update existing launchers automatically.

The route is a connected, bidirectional itinerary, not a general lane-graph/pathfinding system. Traffic drives on the signed left lane. Rivals always use that lane and queue behind obstacles; overtaking into opposing traffic is disabled. All passenger service points are on the forward left lane, and boarding requires the vehicle to face the stop direction. Road-rule zones are triggers and must not be used as solid obstacles. Visible signs belong on the roadside, outside the drivable road surface. Road markings are non-colliding white meshes authored by `ShiftExpansionUpgrade`; route guidance belongs on the minimap only, with no yellow route trace on the road. Only a few speed-limit signs and the required hump warnings are visible; crossing and no-passing rules remain as triggers without extra sign clutter. Humps use the City Asphalt material and are placed on straight road segments. Give Way signage and behavior are disabled; serialized enum value 2 stays reserved for compatibility.
