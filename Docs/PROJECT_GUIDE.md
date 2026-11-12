# Kombi Shift: project guide

Updated assessment work: see `Assessment/CURRENT_DESIGN.md` and `Assessment/LEARN_AND_DEFEND.md`. The current game now has selectable Morning (600s, 1+) and Rush Hour (420s, 6+) configurations, stage best earnings, event notifications, completed alighting before results, actual rival cruise/depart movement, buffered traffic queries, and pause audio/menu controls. Older limitations below describe features that remain outside the selected scope; stage assets are resolved by KombiShiftConfiguration.cs; KombiChallenge.cs preserves the public stage/score API.

Open this repository folder (the one containing `Assets`, `Packages` and `ProjectSettings`) in Unity 6000.6.2f1. This is the cleaned version of `feat/kombishift2-unity6`, based on revision `ddd18ae`. Updates are maintained on `feat/kombishift2-unity6`.

## Open and play

1. Open this project folder in Unity Hub, using one editor window for it.
2. Wait for importing and compilation to finish.
3. Choose **Kombi Shift → Play Main Menu**. This opens the menu and starts Play mode.
4. Choose **How to Play** or **Settings**, then **Trip Market**, select a route/stage and start the trip.
5. Drive to a gold stop marker. Stop below 2 km/h and hold Space while the passengers board together.
6. Serve individual passenger destinations, then finish at the selected terminal. Direct-scene entry retains the original Mbare scenario.
7. Stop Play before making permanent scene changes.

To inspect the city instead, choose **Kombi Shift → Open First Shift**, then **Show City Overview**. **Project Tour** explains the folders and can select them in the Project panel. **Inspect Passenger Station** frames a waiting queue.

## Scene hierarchy

| Group | What to inspect |
|---|---|
| 00 Systems and Lighting | Shift director, navigation HUD, keyboard input bootstrap, weather, lights and RCC scene services. |
| 01 Environment | City buildings, park and street scenery. |
| 02 Roads and Route | Road surfaces, markings, paths and waypoint references. |
| 03 Player and Camera | The Hiace, its wheel colliders, body collider, passenger cabin and chase/driver camera. |
| 04 Passenger Stations | Stop zones, shelters, seated people and Mbare terminal. |
| 05 Traffic and Rivals | Moving traffic and the two competing kombis. |

## Folder map

Everything specific to the game is under `Assets/KombiShift2`:

| Folder | Contents |
|---|---|
| Scenes | Only `KombiMainMenu.unity` and `KombiShift_FirstShift.unity`. |
| Scripts/Gameplay | Shift progression, station reservations, group boarding and passenger/persona behaviour. |
| Scripts/Vehicle | `RccHiaceTestBootstrap` supplies the current keyboard input; its name is historical. `KombiVehicleStability` handles upright arcade driving; accessories control lights/wipers. |
| Scripts/Camera | Chase and driver view, mouse orbit, zoom, ground clearance and view ownership. |
| Scripts/UI | Live menu, showcase movement and circular minimap/speed display. |
| Scripts/Traffic | Background traffic, rival pickups and signal support. |
| Scripts/Weather | Weather states, effects and transitions. |
| Scripts/Compatibility | A small compatibility component referenced by imported city objects. |
| Scripts/Development | Editor verification runners plus an opt-in release integration runner; ordinary game launches do not activate automated tests. |
| Prefabs/Vehicles/Player | `Hiace_Player.prefab`, the reusable player vehicle. Open Prefab Mode to inspect its setup. |
| Prefabs/Passengers | The six reusable passenger appearances and humanoid rigs. |
| Data/PassengerPersonas | Six definitions with appearance references and future economy/patience tuning values. |
| Art/Characters | Current humanoid model sources, textures and materials. Loose prefab textures now live in Textures. |
| Art/Vehicles/Traffic | The vehicle meshes/materials used by traffic. |
| Art/Roads | Prepared road materials and meshes. |
| Art/Environment | City, bus-stop and park assets used by the current scene. |
| Resources/Fonts | Barlow Condensed fonts and their SIL OFL licence. |
| Generated | Referenced wheel meshes, road materials, passenger/station materials and the live menu stage. |
| Shaders | Rendering code referenced by materials. |
| ThirdParty | Original art sources and provenance notes required by current assets. |
| Editor/Project | Project Tour, scene opening, city/station framing and menu preview. |
| Editor/Upgrades | Scene/prefab upgrade tools. Use deliberately: they modify saved assets. |
| Editor/Validation | Setup and dependency audits. |

The remaining top-level vendor folders keep their original names because package tooling depends on their paths:

- **Realistic Car Controller Pro**: shared vehicle physics, audio, settings and editor support.
- **EasyRoads3D**: road materials, shaders and tooling.
- **Plugins**: Bezier path support used by navigation.
- **XR**: inherited settings still referenced by project configuration; desktop play needs no headset.

## Controls

| Input | Action |
|---|---|
| W / Up | Accelerate |
| S / Down | Brake; hold to reverse with automatic gears |
| A / D or Left / Right | Steer |
| Space | Parking brake on all four wheels; cuts throttle |
| Right mouse + drag | Orbit chase view / look around cabin |
| Mouse wheel | Zoom chase camera |
| C or camera button | Switch chase/driver view |
| L / V | Headlights / wipers |
| Escape | Pause/resume |
| R | Restart shift |
| F5 | Cycle weather when debug weather keys are enabled |

The main-menu Settings panel has volume, mouse sensitivity and chase distance. Arrow keys/Tab and Enter also navigate the menu.

## Current vehicle setup

The player uses four fixed wheel-collider mounts, one Rigidbody, upright arcade physics and a gradual governor from 95–105 km/h (12–16 km/h in reverse), plus an emergency physics ceiling at 110 km/h. The body retains yaw steering, vertical suspension and physical collisions. Bounded rollover assistance preserves real suspension pitch and roll; those axes are not frozen.

The old demonstration-car meshes, decorative body-tilt/customisation objects and unused hood-camera Rigidbody/joint were removed. Steering helpers no longer write body rotation directly. Wheel deformation and detachment are disabled so a collision cannot bend the wheel mounts and leave the kombi resting on its body. The original 3D car model still provides the visual appearance.

## What works, and what remains

Powertrain tuning now uses a 5,000 RPM engine range with automatic upshifts at 3,200 RPM and reduced engine-loop gain/pitch. It replaces the abrupt 55 km/h throttle cutoff.

Current systems include live menu feedback, mouse camera controls, seated queues, simultaneous walk-in boarding, cabin seating, rival pickups, traffic, circular navigation, shift timer, fare counters, pause/restart and terminal drop-off. Cast/received shadows are disabled.

Fares are credited only after actual alighting. Selected trips use cached distances along the advertised itinerary and fractional route fares; the direct-scene scenario uses the stage fare. The ledger keeps gross fares, simulated fines, repairs and the success time bonus separate. Passenger requests, happiness, damage and stopped repairs are implemented. Fuel stations and a separate rainy-highway level remain outside scope. Procedural humanoid poses are used; downloaded sitting/walking clips are not claimed.

## Evidence and testing limits

- `Docs/CLEANUP_VALIDATION.txt`: both scenes load, zero missing scripts, active player and build scene list.
- `Docs/Validation/Grounding`: setup audit, before-fix runtime evidence, archived-reference check, loaded-vehicle grounding results and actual Game-view captures.
- `Docs/Validation/MenuCamera`: menu, orbit/zoom, driver/chase view and driving evidence.
- Other validation files retain passenger service and rival pickup checks.

Grounding checks use a dynamic Rigidbody and actual wheel-collider input. They include loaded boarding, hard turns, braking, reverse, a staged 12 cm kerb and a stopped traffic-body impact. Individual cases are staged at the starting position. These are automated checks, not human full-shift testing or proof of every collider in the city.

## Removed content and recovery

Another **220.2 MB** of unused content is outside the Unity project in `../_Archive_Structure_Cleanup`; its manifest records each path and reason. This includes the unused Scavenger pack, duplicate character import, unassigned texture copies, disconnected roadworks prototypes, plugin demonstration art, test terrain and the historical static-menu cover/tool.

Earlier prototype scenes and packs remain in `../_Archive_KombiShift2_Before_Cleanup`. Files changed for the current fix are backed up in `../_Backup_Before_Structure_And_Grounding`. Unity does not import these sibling folders.

Restore only selected files together with their `.meta` files. Do not drag an entire archive into Assets: this would reintroduce clutter and duplicate asset identities.

## Unity project folders outside Assets

- **Packages**: package dependencies.
- **ProjectSettings**: input, physics, rendering and build configuration.
- **Docs**: project guide, attribution and validation evidence.
- **Library, Temp, Logs**: generated editor caches/logs, excluded from source control.
- **UserSettings**: personal editor layout/preferences.
- **.git**: repository history and local changes.
