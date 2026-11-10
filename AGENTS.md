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
| Stations and passenger service | `Assets/KombiShift2/Scripts/Gameplay/PassengerStation.cs`, `PassengerTrip.cs` |
| Rival decisions, driving and lane offsets | `Assets/KombiShift2/Scripts/Traffic/RivalKombiAI.cs`, `RivalDrivingProfile.cs` |
| Traffic and road-rule zones | `Assets/KombiShift2/Scripts/Traffic/`, `Scripts/Gameplay/KombiRoadRuleZone.cs` |
| Navigation/menu and HUD | `Assets/KombiShift2/Scripts/UI/` |
| Scene/route upgrades | `Assets/KombiShift2/Editor/Upgrades/` |
| Scene and play-mode verification | `Assets/KombiShift2/Scripts/Development/`, `Assets/KombiShift2/Editor/Validation/` |
| Current design and user controls | `Docs/Assessment/CURRENT_DESIGN.md`, `README.md` |

Game-specific code belongs under `Assets/KombiShift2`. Avoid modifying vendor code under `Assets/Realistic Car Controller Pro` or `Assets/EasyRoads3D`; extend the project-owned integration instead. Scene upgrade methods may modify and save Unity assets, so run only the specific requested editor action and review the resulting scene diff.

## Validation and known boundaries

The project has Unity EditMode tests under `Assets/KombiShift2/Tests/Editor` and focused editor/play-mode verification runners under `Scripts/Development`. Prefer the smallest relevant Unity validation; do not treat historical files in `Docs/Validation` as proof that a new change passed. Human playtesting is not implied by automated verification.

The route is a connected, bidirectional itinerary, not a general lane-graph/pathfinding system. Traffic drives on the signed left lane. Keep rival route-following in that designated lane; improve competition through target selection, pace and stage tuning, not unsafe moves into opposing traffic. Road-rule zones are triggers and must not be used as solid obstacles. Visible signs belong on the roadside, outside the drivable road surface.
