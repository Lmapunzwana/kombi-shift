# Kombi Shift — Game Design Document (Sprint 0 skeleton)

## Project overview
3D Unity kombi-driving simulation; deliver passengers, collect fares, manage fuel and daily owner remittance. Two levels planned.

## Player experience
Drive → reach passenger stop → board → deliver → collect fare → manage costs → shift statement.

## Target platforms
PC HDRP primary. Second platform: **UNDECIDED** pending lecturer exception by 3 November; URP fallback if required.

## Core systems
- GameManager: Menu, Playing, Paused, ShiftEnd; Win/GameOver outcomes.
- Economy: gross fares, fuel, repairs, remittance, net shift result.
- Vehicle: 3D Rigidbody/WheelCollider or arcade physics; chase camera; wear and potholes.
- Passengers: stops, patience, fare tolerance, boarding, delivery.
- Rival/traffic: waypoint navigation and rival state machine.
- UI: menu, HUD, pause/settings, level/hwindi select, shift-end, win/game-over.

## Levels
Level 1: base road network and first complete shift. Level 2: rainy highway, slippery handling, tighter fuel, higher remittance.

## Visual and audio direction
HDRP desktop look; constrained poly count; baked lighting where appropriate; particle rain and volume overrides. Jayden owns style guide and assets.

## Controls and accessibility
TBD by vehicle/UI owners; document keyboard bindings and readable HUD.

## Architecture
One scene per owner; shared prefabs and pure C# event contracts. Bootstrap GameManager persists across scenes.

## Testing and evaluation
6 November playtest (at least three testers), retest 10 November, weakest-laptop performance check by 5 November.

## Assets, licensing, AI declaration
Maintain asset attribution and daily AI assistance logs.

## Open decisions
Second platform, economy balance, target FPS and lowest-spec machine. Editor selected: 6000.3.27f2; HDRP: 17.3.0; actual import/package lock pending. Scene ownership: Bootstrap (Leroy), Environment (Jayden), VehicleTest (Euclide), TrafficTest (Tendai), PassengerUITest (Tadiwa).
