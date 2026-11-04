# KombiShift2

KombiShift2 is the Unity 6 continuation of the HAI311 Kombi Shift driving simulation. It is based on the autonomous bus VR environment and is being rebuilt as a player-driven commuter omnibus simulation.

## Current foundation

- Unity `6000.6.2f1`
- Existing New York environment, autonomous bus model, audio and tour assets
- Physics-driving component based on Unity Wheel Colliders
- Switchable interior, dashboard, chase and orbit camera framework
- Dust, gravel, mud and pothole surface zones
- Alternating left/right road-construction stage controller
- Existing autonomous tour retained while manual-driving integration is developed

The manual-driving components have been added, but they still need to be attached and configured on the vehicle in the main scene before the new driving mode is playable.

## Requirements

Install Unity Hub and Unity Editor `6000.6.2f1`. Include Linux or Windows build support as appropriate for your development machine.

The migrated Oculus package is deprecated in Unity 6. OpenXR migration is planned for the VR configuration. Desktop testing does not require a headset.

## Clone and open

```bash
git clone git@github.com:Lmapunzwana/kombi-shift.git
cd kombi-shift
git switch feat/kombishift2-unity6
```

In Unity Hub:

1. Select **Add** and then **Add project from disk**.
2. Select the repository root—the directory containing `Assets`, `Packages` and `ProjectSettings`.
3. Choose Unity Editor `6000.6.2f1`.
4. Allow the initial asset import to finish. It may take several minutes because the project contains large city and vehicle assets.

Do not commit `Library`, `Temp`, `Logs`, generated IDE files or personal `UserSettings`.

## Run the current scene

1. In Unity's Project panel, open `Assets/NewYork.unity`.
2. Wait for shader compilation to finish.
3. Press the **Play** button.

The scene currently runs the inherited autonomous tour. Manual driving becomes available after the vehicle prefab is configured with a Rigidbody, Wheel Colliders, wheel references and camera anchors.

## New gameplay scripts

The migration-specific code is under `Assets/KombiShift2/Scripts`:

- `Vehicle/KombiVehicleController.cs` — acceleration, reverse, steering, service braking, handbrake, speed limits, wheel visuals and vehicle reset.
- `Camera/VehicleCameraController.cs` — interior/exterior camera switching and exterior orbit view.
- `Roadworks/RoadSurfaceZone.cs` — dust, gravel, mud and pothole physical effects, particles and audio hooks.
- `Roadworks/AlternatingLaneController.cs` — construction stages that move traffic between temporary left and right lanes.

Default desktop bindings provided by these components are W/S for throttle and reverse, A/D for steering, Space for the handbrake, R to reset, C to change view, and right-mouse drag to orbit where enabled.

## Planned work

- Configure and tune the first drivable commuter omnibus
- Add dashboard, passenger and exterior camera anchors
- Build alternating-lane roadworks with cones, barriers, dust and flag-control points
- Add commuter stops, passengers, fares and rival traffic
- Move VR support from the deprecated Oculus package to OpenXR
- Apply a VR-friendly URP visual upgrade, with HDRP considered only for a separate high-end desktop profile
- Add build scenes, platform profiles and automated validation

## Project history

The `main` branch contains the earlier Kombi Shift starter project. This branch replaces that starter layout with the migrated Unity 6 project while retaining the repository history.
