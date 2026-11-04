# KombiShift2 gameplay foundation

Initial Unity 6 components:

- `KombiMainMenu`: the first build scene, with a Fernbus-inspired (but Kombi Shift-branded) vehicle hero layout, Start Shift and working volume settings.
- `PlayerBusController`: direct keyboard/controller driving with no automatic throttle or spline-following fallback.
- `PlayerBusCamera`: three player-switchable chase/interior-style views.
- `KombiVehicleController`: WheelCollider driving, braking, reverse, handbrake, wheel visuals, speed limiting and reset.
- `VehicleCameraController`: switchable interior, dashboard, chase and orbit camera anchors.
- `RoadSurfaceZone`: dust, gravel, mud and pothole behavior with optional particles and sound.
- `AlternatingLaneController`: staged left/right construction closures and temporary lanes.

Default controls: W/S or controller vertical axis, A/D or controller horizontal axis, Space for handbrake, R to reset, C to change camera, and right mouse drag to orbit an exterior view.

`KombiMainMenu` is build index 0 and loads `KombiDrive`. The original `BezierWalkerWithSpeed` component is removed from the playable bus when the generated driving scene is built, so it cannot drive itself.
