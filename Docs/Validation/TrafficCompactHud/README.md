# Compact transparent HUD and moving traffic ? 13 November 2026

The speedometer retains its orange segmented dial and telemetry at 70% of the previous size. Both the rectangular black backing and circular fill are removed. White outlined text remains readable over the game.

The traffic sensor was reporting overlapping road meshes as zero-distance obstacles. City traffic and rival obstacle checks now exclude CityRoadSurface geometry while wheel/vehicle collisions with the road remain physical. The sensor is raised above the road. Spawn clearance uses the whole vehicle footprint. Every car within the spawn area remains simulated instead of freezing at the former distance boundary.

Conservative traffic defaults disable overtaking, enforce longer following gaps, honour stop controls and cap cruising at 20 km/h. Full braking holds stopped queues; a full-chassis sweep detects approaching side/corner contacts and applies emergency stopping before contact. Regular wheel/suspension physics and imported city assets remain in use.

Fresh Unity 6000.6.2f1 verification observed normal traffic for 90 simulated seconds: 28 active vehicles, all 28 moved horizontally at least two metres, and zero contact events. The observer counts vehicle contacts and static side impacts, excluding normal ground support. No compile errors or exceptions appeared within that observation window. See result.json, verification-summary.json and traffic-observation.txt. The screenshot is an unmodified actual Game view capture. Normal traffic and player input were not disabled.

This bounded observation does not establish that every route, Rush Hour, player-caused collision or indefinite session is accident-free. Unit/component tests were not run because Unit Test MCP is unavailable; terminal substitutes are prohibited by repository instructions. No saved scenes or vendor sources were edited.

Repeat with Kombi Shift > Validation > Inspect Moving Traffic And Compact HUD. The action starts a fresh driving scene; the observer is created only in Play mode. The Temp request file is an explicit opt-in shortcut consumed once after script import.
