# Validation and manual playtest plan

Statuses are reported in `Docs/Validation/CityInfrastructure`; this checklist does not claim execution by itself. Unit/component tests must use Unit Test MCP under the repository instructions. Those tools were not available in this session; scene integration, physics scenarios, visual checks and builds use their own workflows.

| Area | Procedure and expected observation |
|---|---|
| City preservation | Compare imported prefab instances, asset diffs, original transforms and the 12,611-renderer inventory against `7a6a733`. |
| Asphalt and colliders | Inspect all road-material slots; verify buildings/pavements unchanged and copied collider meshes match rendered roads. |
| Connectivity | Audit road support and body clearance on every playable directed link; retain a strongly connected component and document exclusions. |
| Left-hand lanes | Drive both directions; observe left-side paths, correct stop headings and no accidental opposing-lane passes. |
| Markings | Inspect bends, crossings and junctions; confirm projection onto roads and no duplicated legacy paint. |
| Potholes and suspension | Drive the same actual defect slowly and quickly; record wheel contact depression, pitch/roll, vertical speed and bounded response. |
| Road wear | Compare low/high-speed wear and XP/happiness effects; repair and confirm restored condition. |
| Civilian traffic | Observe sustained driving, acceleration/braking, following, stationary obstructions, cut-ins and resumed movement. |
| Signals | Observe green/amber/all-red cycles; cars wait at red and do not enter blocked exits. |
| Stop/yield/roundabout | Observe stopped entries, right-side priority and clockwise circulation; test queue recovery after clearing an obstruction. |
| Passing | Follow a slower vehicle on a wide straight street; confirm front/rear checks, smooth same-direction lane changes and return. |
| Rival competition | Observe physical arrival, shared reservation exclusion, distinct paint, pickup totals and remaining queue changes. |
| Passenger boarding | Check unique cabin seats, full interior fit, departure cancellation and restored waiting positions. |
| Destinations | Check downstream destinations belong to the selected itinerary and requests appear before arrival. |
| Stopping ring | Verify distance label, alignment/speed states, completion and missed-request feedback. |
| Alighting | Observe walking from cabin through door to a real bench or standing overflow; no instant movement to the doorway. |
| Missed stops | Pass a requested destination; verify complaint, XP/happiness loss, retained passenger trip and legal recovery guidance. |
| Driving quality | Compare smooth driving against harsh braking, acceleration, turning and collisions; penalties must be rate limited. |
| Collision damage | Compare minor/severe impacts, power/speed/brake/steering changes and stopped paid repairs. |
| Enforcement | Test speeding grace/cooldown, signal entry, stop rules, wrong-side driving and existing prohibited-road zones. |
| Speed humps | Cross slowly/quickly and observe actual suspension movement and high-speed discomfort/wear. |
| Trip Market | Inspect all nine route cards, map pan/zoom, fare/demand estimates, stage switching and Start Trip controlling the loaded world. |
| Fare calculation | Check base/distance/peak adjustments and final limits; gross estimates must not be credited before delivery. |
| Navigation | Take a wrong turn; observe recalculation, legal station approach, next-turn text and minimap guidance. |
| Morning completion | Complete enough actual passenger journeys, reach the selected terminal and verify success/results. |
| Rush completion | Verify eight-person queues, larger traffic pool, stronger rivals, stage timer/target and successful delivery. |
| Performance | Measure actual frame, AI, physics and memory values; record hardware, sample windows and workload. |
| Linux build | Compile both scenes and run the actual player build smoke sequence and menu flow. |
| WebGL build | Compile and load through HTTP in a browser; check menu, trip start, input, rendering, audio policy and memory. |
| Human feedback | Three real testers must supply identified build/device, dated observations, severity, fixes and same-task retests. |
```
