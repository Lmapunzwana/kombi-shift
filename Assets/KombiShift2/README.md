# KombiShift2 gameplay foundation

Initial Unity 6 components:

- `KombiVehicleController`: WheelCollider driving, braking, reverse, handbrake, wheel visuals, speed limiting and reset.
- `VehicleCameraController`: switchable interior, dashboard, chase and orbit camera anchors.
- `RoadSurfaceZone`: dust, gravel, mud and pothole behavior with optional particles and sound.
- `AlternatingLaneController`: staged left/right construction closures and temporary lanes.

Default controls: W/S or controller vertical axis, A/D or controller horizontal axis, Space for handbrake, R to reset, C to change camera, and right mouse drag to orbit an exterior view.
