# Passenger seating and left-lane repair

Validated 2026-11-10T18:50:09.476601+00:00 with Unity 6000.6.2f1.

- Unity compilation completed successfully.
- Authored-scene inspection: seven forward left-lane service points, each 1.55 m left of its route centre; 84 supported bench positions; twelve unique cabin anchors on each of three vehicles.
- Disposable full-cabin preview: all six passenger models represented; twelve baked avatar mesh bounds are disjoint and remain within the cabin clearance envelope. Onboard scale is 75%, with seats above the vehicle floor. The original walking scale is restored on alighting.
- Existing full-scene passenger-service scenario: **26 passed, 0 failed** (`service-results.json`). This includes concurrent boarding, exclusive player/rival queues, physical rival driving and collection, cancellation/return, twelve-player-passenger capacity, terminal alighting and HUD checks.
- Rendered terminal and full-cabin previews were visually inspected. Preview characters were discarded and were never saved into the driving scene.

`baseline-layout.txt` records the original problematic arrangement. `repair-layout.txt` and `scene-inspection.txt` describe the repaired saved scene. The PNGs show disposable visual staging and the actual scene station.

This is scene integration and visual verification, not human playtesting or a new unit-test coverage run. Unit Test MCP tools were unavailable; no unit/component test commands were run. Standalone builds were not regenerated.
