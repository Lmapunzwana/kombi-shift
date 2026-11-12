"""Assemble the engineering report from fresh machine evidence, without inventing passes."""
import json,datetime
from pathlib import Path
root=Path(__file__).resolve().parents[1];e=root/'Docs/Validation/CityInfrastructure'
def load(name):
 p=e/name
 return json.loads(p.read_text()) if p.exists() else None
audit=load('scene-road-audit.json');graph=load('validated-graph.json');preserve=load('city-preservation.json');physics=load('pothole-physics.json')
checks=[]
for name,label in [('city-service-results.json','Morning selected-trip scenario'),('city-service-rush.json','Rush Hour selected-trip scenario'),('trip-market-results.json','Real menu → Borrowdale startup'),('pothole-physics.json','Actual RCC pothole crossing'),('passenger-regression.json','Existing passenger-service regression')]:
 d=load(name);checks.append(f"| {label} | {len(d['passed']) if d else 0} passed; {len(d['failed']) if d else 'not run'} failed | `{name}` |" if d else f'| {label} | Not executed | |')
perf=[]
for file in ['city-service-results.json','city-service-rush.json']:
 d=load(file)
 if not d:continue
 for p in d.get('performance',[]):perf.append(f"| {d.get('stage','')} | {p['mode']} | {p['traffic']} | {p['meanFrameMs']:.3f} | {p['p95FrameMs']:.3f} | {p.get('meanPhysicsMs',-1):.3f} | {p['meanDecisionMs']:.4f} | {p['allocatedBytes']/1048576:.1f} |")
builds=[]
for file in ['build-StandaloneLinux64.json','build-WebGL.json']:
 d=load(file)
 builds.append(f"| {d['platform']} | {d['result']} | {d['errors']} | {d['warnings']} | {d['bytes']/1048576:.1f} MiB | {d['seconds']:.1f} s |" if d else f'| {file} | Not executed | — | — | — | — |')
native=load('native-smoke.json');browser=load('web-browser.json');trials=physics['trials'] if physics else []
rows='\n'.join(checks);prows='\n'.join(perf);brows='\n'.join(builds)
physical='\n'.join(f"| {t['startSpeed']*3.6:.1f} km/h | {t['contactDepression']:.3f} m | {t['peakVerticalSpeed']:.3f} m/s | {t.get('peakWheelForce',0):.0f} N | {t['healthLost']:.3f} | {t['impacts']} |" for t in trials)
report=f'''# Engineering report — Kombi Shift 2 existing-city infrastructure

Updated {datetime.datetime.now(datetime.timezone.utc).isoformat()}. Implementation source commit: `0798f97` (later evidence/documentation commits may follow). Unity **6000.6.2f1**, branch `feat/kombishift2-unity6`.

## Outcome and scope

The existing NYBlock02 city remains the playable environment. The implementation adds shared textured asphalt across its 25 identified road meshes, copied physical pothole surfaces, a derived and physically audited navigation network, physical pooled civilian traffic, junction controls, nine real selectable fictional routes, route fares, individual drop-off guidance, and integrated passenger/vehicle consequences. The existing RCC controller, imported city sources, 12-seat layout, two challenge stages and working menus remain in use.

This is a substantial playable city upgrade, not a claim that every extracted fragment or every commercial driving-simulator behaviour is complete. The validated playable component has **{graph['playableNodes']} nodes** and **{audit['links']} directional links**. Unsafe links and inaccessible fragments remain excluded and documented. Fictional Harare labels do not make the New York geometry geographically Harare, and fares/fines are game values.

## Preservation and road verification

The source comparison uses pre-city commit `{preserve['baselineCommit']}`. The original city prefab instance remains present, imported city assets have no modifications, and no unexpected original transform/property overrides were found. Allowed changes are road mesh/material references and foliage collision enable states. The runtime/editor inventory still contains **{audit['cityRenderers']:,} city mesh renderers**.

The final actual-scene road audit reports **{audit['unsupportedLaneSamples']} unsupported links**, **{audit['blockedLaneSegments']} blocked links**, **{audit['reachableTrips']} reachable configured trips**, and **{audit['physicalPotholes']} physical potholes** across **{audit['roadSurfaces']} road surfaces**. The body envelope is 1.9 m wide, 0.9 m high, centred 0.95 m above the path; road support is sampled at both endpoints and the midpoint. This is an authored-network clearance check, not exhaustive proof of every dynamic turning trajectory.

The raw extraction contains {graph['nodes']} nodes. {graph['nodes']-graph['playableNodes']} lie outside the final playable component, and {graph['recordedLaneExclusions']} directional exclusions are recorded. Tree-card walls were replaced with narrow trunks while preserving their artwork; actual trunks, kerbs, buildings and unsupported road fragments are not deleted merely to make a path pass. Original flat road collision is replaced by the copied depressed surface, so it does not fill the potholes.

Evidence: [scene audit](../../Validation/CityInfrastructure/scene-road-audit.json), [preservation](../../Validation/CityInfrastructure/city-preservation.json), [graph exclusions](../../Validation/CityInfrastructure/validated-graph.json).

## Executed scene scenarios

| Scenario | Result | Machine record |
|---|---|---|
{rows}

The selected-trip fixtures use real scene objects, shared passenger queues, actual boarding/alighting coroutines, actual fares, physical traffic and complete terminal outcomes. The vehicle is deliberately positioned between stops for service assertions; these are not human or autonomous full-route driving sessions. The native control smoke and pothole fixture separately exercise real vehicle physics. No unit/component coverage run is claimed: Unit Test MCP was unavailable, and the repository prohibits substituting terminal unit-test commands.

The menu fixture found and fixed cached route lists being cleared when switching cards. Scene reload initially duplicated the verification runner; a singleton guard was added and Rush Hour was rerun independently. The final records above are the passing runs. Later visual refinements keep actors crouched during cabin entry/egress and ground stops on actual pavement without treating furniture as ground.

## Measured pothole behaviour

| Initial speed | Wheel contact depression | Peak vertical body speed | Peak wheel force | Condition lost | Wear impacts |
|---|---|---|---|---|---|
{physical}

Each trial settles the suspension before moving through the same actual city defect. The faster crossing produced greater contact force, vertical body response and wear, while the slow crossing caused no wear. Angular displacement was present in both trials and was not monotonically larger with speed. No artificial launch impulse or camera-only substitute was used. See [pothole physics](../../Validation/CityInfrastructure/pothole-physics.json).

Unity models WheelCollider suspension through spring/damper behaviour; the implementation therefore changes actual road contact and bounded suspension parameters rather than relying on camera shake. [Unity 6.6 WheelCollider suspension](https://docs.unity.com/en-us/engine/6000.6/manual/physics-section/physics-overview/collision-section/collider-shapes/wheel-colliders/suspension).

## Measured performance

| Stage | Window | Vehicles | Mean frame ms | P95 frame ms | Physics marker ms | Last AI decision ms | Editor allocated MiB |
|---|---|---:|---:|---:|---:|---:|---:|
{prows}

Hardware reported by the fixtures: Mesa Intel Graphics (RPL-P). Each window lasts 15 seconds. The comparison uses 20 ms AI decisions and full city visibility; the optimized window uses 120 ms decisions and distance culling. Traffic continues to move between consecutive windows, so these are observed Editor measurements rather than a perfectly controlled causal benchmark. Frame timing is not a guaranteed player FPS; allocated memory includes the Editor, and the AI column measures individual decision work rather than total per-frame AI cost. The physics column comes from available `Physics.Simulate` profiler samples. [Unity ProfilerRecorder](https://docs.unity.com/en-us/engine/6000.6/script-reference/unity/profiling/profilerrecorder).

Pooling, bounded physics buffers, reusable materials, cached map rasterisation and distant simulation sleeping reduce repeated work. No benchmark is invented and no weakest-device result is assumed.

## Builds and player checks

| Target | Build result | Errors | Warnings | Size | Build time |
|---|---|---:|---:|---:|---:|
{brows}

Linux player smoke: **{native['result'] if native else 'not yet executed'}**. The explicit `--kombi-smoke` entry loads the selected city and tests acceleration, steering, braking, reverse and grounded stability using the actual executable. OpenGL Core was forced for this Linux/Intel check. Binary/managed assembly hashes and the exact native result are recorded in [native-smoke.json](../../Validation/CityInfrastructure/native-smoke.json).

Web browser status: **{browser.get('result','not yet executed') if browser else 'not yet executed'}**. A successful WebGL compilation alone is not browser compatibility evidence. Refer to the current browser record for the actual load/start/input observations and limitations.

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
'''
p=root/'Docs/Assessment/CityInfrastructure/ENGINEERING_REPORT.md';p.write_text(report)
print('Wrote',p)
