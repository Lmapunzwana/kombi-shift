# Concept implementation — 3D adaptation

Jayden explicitly selected the existing 3D format and map/stop markers without a guidance arrow. The concept's Rigidbody2D/sprites are therefore adapted to Rigidbody 3D and procedural models.

| Requirement | Implementation/evidence |
|---|---|
| Individual passengers and destinations | PassengerAI, PassengerStop, six-seat manifest and selectable map stops |
| Boarding/drop coroutine and patience | Visible walking/service sequence, per-passenger timers, pause-aware coroutines |
| Distance/time fares and tips | RoadGraph distance, locked fare quotes, prompt-delivery tips, late unpaid drops |
| Fuel/health and paid stations | FuelSystem cash-limited purchases, collision/pothole damage |
| Fare target and losses | GameManager target win, timeout/fuel/health loss; clean score bonus |
| Rival FSM | Cruise/Chase Passenger/Block/Flee, road graph routing, front obstacle casts, own health |
| Two different stages | CBD connected streets versus long rainy highway; grip, fuel, hazards and aggression differ |
| Three traffic types and pooling | Sedan, delivery truck, minibus; pooled traffic, pedestrians and potholes |
| HUD and menus | Manifest patience bars, cash/target/time/fuel/health, controls, pause, volume, replay and exit |
| Accessibility/input | Keyboard/mouse, optional touch controls, HUD scale, text with coloured bars |
| Conductor/audio | Visible hwindi and door service, recorded bus idle, temporary synthesized calls, horn/rain/music |
| Optimisation evidence | Real controlled baseline/pooled captures and CSV/JSON in UnityStarter/Validation |
| Builds | Linux and Windows builds tested automatically; WebGL build and browser verification tracked separately |

See UnityStarter/README.md and Validation/RESULTS.md for controls, limitations and measured results. No human tester data or finished production-art claim is implied. The earlier assessment alignment document records the original design-stage plan, not current implementation status. Team ownership and HDRP integration must still be reconciled; this isolated preview does not overwrite teammates' foundation.
