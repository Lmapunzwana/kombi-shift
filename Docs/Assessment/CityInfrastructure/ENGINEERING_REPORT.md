# Engineering report — Kombi Shift 2 existing-city infrastructure

Updated 2026-11-11T11:55:35+00:00. Validated source base: `e9c4f0e` plus the uncommitted changes recorded in this worktree. Unity **6000.6.2f1**, branch `feat/kombishift2-unity6`.

## Outcome and scope

The existing NYBlock02 city remains the playable environment. The implementation adds shared textured asphalt across its 25 identified road meshes, copied physical pothole surfaces, a derived and physically audited navigation network, physical pooled civilian traffic, junction controls, nine real selectable fictional routes, route fares, individual drop-off guidance, and integrated passenger/vehicle consequences. The follow-up rebuilds white edge/lane and yellow center markings over the playable graph, places signal posts at roadside corners, and lets safely spaced same-direction traffic platoons proceed while retaining red-light and cross-traffic gating. The existing RCC controller, imported city sources, 12-seat layout, two challenge stages and working menus remain in use.

This is a substantial playable city upgrade, not a claim that every extracted fragment or every commercial driving-simulator behaviour is complete. The validated playable component has **4191 nodes** and **8466 directional links**. Unsafe links and inaccessible fragments remain excluded and documented. Fictional Harare labels do not make the New York geometry geographically Harare, and fares/fines are game values.

## Preservation and road verification

The source comparison uses pre-city commit `7a6a733`. The original city prefab instance remains present, imported city assets have no modifications, and no unexpected original transform/property overrides were found. Allowed changes are road mesh/material references and foliage collision enable states. The runtime/editor inventory still contains **12,611 city mesh renderers**.

The final actual-scene road audit reports **0 unsupported links**, **0 blocked links**, **9 reachable configured trips**, and **34 physical potholes** across **25 road surfaces**. The regenerated paint has **31,788 triangles** across two non-collidable meshes; all **164 signal posts** across 41 signalised junctions pass the roadside-placement check. The body envelope is 1.9 m wide, 0.9 m high, centred 0.95 m above the path; road support is sampled at both endpoints and the midpoint. This is an authored-network clearance check, not exhaustive proof of every dynamic turning trajectory.

The raw extraction contains 4533 nodes. 342 lie outside the final playable component, and 205 directional exclusions are recorded. Tree-card walls were replaced with narrow trunks while preserving their artwork; actual trunks, kerbs, buildings and unsupported road fragments are not deleted merely to make a path pass. Original flat road collision is replaced by the copied depressed surface, so it does not fill the potholes.

Evidence: [scene audit](../../Validation/CityInfrastructure/scene-road-audit.json), [preservation](../../Validation/CityInfrastructure/city-preservation.json), [graph exclusions](../../Validation/CityInfrastructure/validated-graph.json).

## Executed scene scenarios

| Scenario | Result | Machine record |
|---|---|---|
| Morning selected-trip scenario | 30 passed; 0 failed | `city-service-results.json` |
| Rush Hour selected-trip scenario | 30 passed; 0 failed | `city-service-rush.json` |
| Real menu → Borrowdale startup | 15 passed; 0 failed | `trip-market-results.json` |
| Actual RCC pothole crossing | 7 passed; 0 failed | `pothole-physics.json` |
| Existing passenger-service regression | 26 passed; 0 failed | `passenger-regression.json` |

The updated runs include direct red-signal entry rejection and same-direction platoon admission checks. The selected-trip fixtures use real scene objects, shared passenger queues, actual boarding/alighting coroutines, actual fares, physical traffic and complete terminal outcomes. The vehicle is deliberately positioned between stops for service assertions; these are not human or autonomous full-route driving sessions. The native control smoke and pothole fixture separately exercise real vehicle physics. No unit/component coverage run is claimed: Unit Test MCP was unavailable, and the repository prohibits substituting terminal unit-test commands.

The menu fixture found and fixed cached route lists being cleared when switching cards. Scene reload initially duplicated the verification runner; a singleton guard was added and Rush Hour was rerun independently. The final records above are the passing runs. Later visual refinements keep actors crouched during cabin entry/egress and ground stops on actual pavement without treating furniture as ground.

## Measured pothole behaviour

| Initial speed | Wheel contact depression | Peak vertical body speed | Peak wheel force | Condition lost | Wear impacts |
|---|---|---|---|---|---|
| 10.8 km/h | 0.178 m | 0.161 m/s | 13024 N | 0.000 | 0 |
| 43.2 km/h | 0.176 m | 0.474 m/s | 25661 N | 0.195 | 2 |

Each trial settles the suspension before moving through the same actual city defect. The faster crossing produced greater contact force, vertical body response and wear, while the slow crossing caused no wear. Angular displacement was present in both trials and was not monotonically larger with speed. No artificial launch impulse or camera-only substitute was used. See [pothole physics](../../Validation/CityInfrastructure/pothole-physics.json).

Unity models WheelCollider suspension through spring/damper behaviour; the implementation therefore changes actual road contact and bounded suspension parameters rather than relying on camera shake. [Unity 6.6 WheelCollider suspension](https://docs.unity.com/en-us/engine/6000.6/manual/physics-section/physics-overview/collision-section/collider-shapes/wheel-colliders/suspension).

## Measured performance

| Stage | Window | Vehicles | Mean frame ms | P95 frame ms | Physics marker ms | Last AI decision ms | Editor allocated MiB |
|---|---|---:|---:|---:|---:|---:|---:|
| Morning | comparison AI 20ms + full city visibility | 28 | 23.826 | 40.811 | 5.082 | 0.265 | 822.6 |
| Morning | AI 120ms + city distance culling | 28 | 20.618 | 34.304 | 5.006 | 0.290 | 822.6 |
| RushHour | comparison AI 20ms + full city visibility | 48 | 71.168 | 167.772 | 22.194 | 0.269 | 705.1 |
| RushHour | AI 120ms + city distance culling | 48 | 34.798 | 70.715 | 11.936 | 0.295 | 705.1 |

Hardware reported by the current run: Mesa Intel UHD Graphics 620. Each window lasts 15 seconds. The comparison uses 20 ms AI decisions and full city visibility; the optimized window uses 120 ms decisions and distance culling. Traffic continues to move between consecutive windows, so these are observed Editor measurements rather than a perfectly controlled causal benchmark. The current Rush Hour comparison window had unusually high frame/physics times; the optimized window improved, but still showed spikes. Frame timing is not a guaranteed player FPS; allocated memory includes the Editor, and the AI column measures individual decision work rather than total per-frame AI cost. The physics column comes from available `Physics.Simulate` profiler samples. [Unity ProfilerRecorder](https://docs.unity.com/en-us/engine/6000.6/script-reference/unity/profiling/profilerrecorder).

Pooling, bounded physics buffers, reusable materials, cached map rasterisation and distant simulation sleeping reduce repeated work. No benchmark is invented and no weakest-device result is assumed.

## Builds and player checks

| Target | Build result | Errors | Warnings | Size | Build time |
|---|---|---:|---:|---:|---:|
| StandaloneLinux64 | Succeeded | 0 | 39 | 387.2 MiB | 228.8 s |
| WebGL | Succeeded | 0 | 44 | 195.2 MiB | 169.2 s |

Linux player smoke: **PASS**. The explicit `--kombi-smoke` entry loaded the selected city and tested acceleration (32.03 m), steering (46.7°), braking (35.6→0.8 km/h), reverse (9.39 m) and grounded stability using the new executable. OpenGL Core was forced on Mesa Intel UHD 620. Binary/managed assembly hashes and the exact native result are recorded in [native-smoke.json](../../Validation/CityInfrastructure/native-smoke.json).

Web browser status: **Partial: main menu and Trip Market rendered; gameplay keyboard check pending**. A successful WebGL compilation alone is not browser compatibility evidence. Refer to the current browser record for the actual load/start/input observations and limitations.

Builds are local workspace artifacts under `Builds/CityInfrastructure2026`; they are not committed as large binaries. Windows-native execution, additional hardware, browser audio/accessibility and exhaustive long-session traffic behaviour need identified device testing. Build warnings, including existing deprecated API usage, are retained in build records rather than represented as zero-warning success.

## Assessment traceability and human evidence

The design/GDD addendum is [DESIGN_AND_ARCHITECTURE.md](DESIGN_AND_ARCHITECTURE.md), architecture/state diagrams are [DIAGRAMS.md](DIAGRAMS.md), and the 32-area manual/automated plan is [VALIDATION_PLAN.md](VALIDATION_PLAN.md). The Trip Market and actual game screenshots are in the validation folder. Earlier PDFs, decks and expansion snapshots are historical and do not validate this version.

The user confirmed that tester feedback exists. The actual three tester identities/aliases, dates, observations, severity and retest outcomes have been requested but have not been supplied in this chat. [Human testing status](../../Validation/CityInfrastructure/HUMAN_TESTING.md) remains pending; automated fixtures are not substituted for three people. Genuine contributions, assigned-reading connections and live defence understanding remain human evidence. No grades, successful human sessions or member contributions are fabricated.

## Remaining boundaries

- At-grade road extraction and inferred widths need manual review for unusual geometry; arbitrary stacked roads/overpasses are not a supported automatic extraction case.
- Only the audited connected component supplies routes. Excluded fragments are not described as a functional whole-city network.
- Physical traffic has safe priorities and passing constraints, but exhaustive all-junction, long-duration congestion/deadlock and rare collision behaviour still require playtesting. Distant traffic sleeps rather than running thousands of full-physics vehicles.
- The player/rival controllers and stylized passenger rigs retain their project-specific handling/animation limits. This is an original simulation inspired by driving games, not proprietary ETS code or feature parity.
- Human feedback, multiple devices and second-platform interaction observations must be tied to actual builds.

## Attribution and reproducibility

No replacement city, OpenStreetMap data, new paid asset or proprietary driving-simulator code was imported. The already included worn-asphalt textures are from Poly Haven, whose asset licence is CC0; the included notices remain. [Poly Haven asset licence](https://polyhaven.com/license). Existing imported assets, RCC/EasyRoads and fonts retain their original attribution/permission review. Codex assisted code, integration, fixtures and documentation; this does not establish individual student authorship or understanding.

Rebuild navigation with the documented triangle-export → graph-bake → physical audit → exclusion/SCC → trip-catalogue pipeline, then re-run relevant scene scenarios. Commit the baked Resources data and generated project-owned meshes. Do not edit imported city sources, remove the project lock, close a user's Editor, overwrite unsaved scenes, or rewrite shared Git history.

## 12 November source review

The performance table above now matches `city-service-results.json` (12:02 UTC) and `city-service-rush.json` (12:03 UTC), rather than the earlier windows previously transcribed here. These remain historical Editor measurements and do not validate the new source changes. `validated-graph.json` records an intermediate retained-link count of 8,974; the later actual-scene audit records 8,466. Use the final audit for the shipped navigation scope.

Read [../CODE_REVIEW_IMPROVEMENTS.md](../CODE_REVIEW_IMPROVEMENTS.md) for the new source implementation and validation limits. No historical pass record, human tester worksheet, build result or binary document has been regenerated as proof of the changes.
