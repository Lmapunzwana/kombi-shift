# Sprint task review - 3 November 2026

This matrix records implementation and available evidence, not independent approval, merge status or invented student authorship. All gameplay is under the AI-assisted local Unity projects and PR31. The earlier foundation remains separate. A working implementation is not the same as satisfying every assessment evidence requirement.

| Issue | Assigned area | Available result | Still requires real action/evidence |
|---|---|---|---|
| 2 | Leroy: import, HDRP, build | Actual local Editor import; real HDRP assets; build/check logs | Separate original 6000.3 foundation remains unverified; HDRP runtime check status in Validation |
| 3 | Everyone: open/play/design review | Workbench, startup guide and checks | Every member opens on own machine and signs verification |
| 4 | Leroy: platform decision | Built-In WebGL fallback retained beside HDRP desktop | Team/lecturer confirms this dual-project submission approach; no exception claimed |
| 5-6 | Leroy: repo/architecture/email | Existing foundation setup; later integrated gameplay | Existing email status is owner-reported; no new message sent |
| 7 | Jayden: look, style, storyboard, attribution | HDRP pipeline/day-rain profiles, style/prototype, report storyboard and credits | Team review and weakest-laptop rendering check |
| 8 | Euclide: vehicle/track/camera | Drivable Rigidbody arcade vehicle and roads/camera | WheelCollider-specific early prototype is intentionally adapted; no WheelCollider claim |
| 9 | Tendai: waypoints/FSM/pool | RoadGraph, explicit RivalAI, SpawnManager, diagrams | Personal review/explanation |
| 10 | Tadiwa: personas/model/flow/testers | Three proposed personas, destination/patience/tolerance model, screen flow | Book five consenting testers; personas are not interviews |
| 11 | Jayden: kombi/road/passenger/UI kit | Reusable art catalog, runtime world and menus | Final art review and team prefab handoff |
| 12 | Leroy: state/economy/events/integration | GameManager states, actual economy ledger/fuel costs, published events, start-to-end loop | Independent review/merge into agreed team setup |
| 13 | Euclide: handling/camera/hazards | Drive/steer/brake hold, chase camera, potholes, bounded collision damage | Human handling/camera feedback |
| 14 | Tendai: traffic/pool/cruise | Three traffic types, preallocated pools, rival Cruise/navigation | Human traffic fairness testing |
| 15 | Tadiwa: passenger/HUD/menu | Service zones, boarding/drop coroutine, patience, fares, live HUD/menu/instructions | Human usability feedback |
| 16 | Jayden: environment/UI/rain/audio | CBD dressing, props, passenger variants, UI, rain, attributed audio | Art polish review; optional consented local speech replacement |
| 17 | Leroy: statement/outcomes/playtest | Real expense statement, fare target win, time/fuel/health loss | Actual 6 November playtest and severity triage |
| 18 | Euclide: wear/pause/settings | Separate suspension/brakes affect handling; paid repairs; pause/audio settings | Tester-driven handling fixes |
| 19 | Tendai: full rival/select/profiler | Four-state rival, live workbench state, level select, baseline fixture | Representative final HDRP full-route profile |
| 20 | Tadiwa: slider/reactions/records | Fare slider, individual tolerance + wait/health reactions, blank cases CSV | At least three authentic testers; actual top-three findings |
| 21 | Jayden: highway/audio/report | Separate highway, wet materials/rain, mix, credits and screenshots | Replace explicitly temporary voices if required; final visual sign-off |
| 22 | Leroy: economy/retest/freeze | Distinct level economy and settings | Balance with real tester data; 10 November retest/freeze |
| 23 | Euclide: wet/hazards/persistence | Wet grip, potholes, guardrails, persistent controls/audio/HUD scale | Hardware/camera feedback fixes |
| 24 | Tendai: pooling/hwindi/aggression | Before/after fixture, selection, greater highway aggression | Final target-hardware Profiler screenshots and context |
| 25 | Tadiwa: conductor/retested fixes | Explicit ConductorModifier, visible service and per-shift cost | Three real feedback-based fixes with retest; cannot fabricate |
| 26 | Jayden: report/visuals/slides | Illustrated PDF + editable report draft, offline defence deck, AI/assets declaration | Actual reviewed contribution, registration/group details |
| 27 | Leroy: builds/package | Local desktop and WebGL builds, source/evidence packaging workflow | Clean-machine and native Windows hardware test; final submission |
| 28 | Euclide: report/defence | Physics/collision explanation and live tuning exercise | Euclide personally practises/explains |
| 29 | Tendai: report/defence | AI/pooling explanation, measurements and live aggression exercise | Tendai personally practises/explains |
| 30 | Tadiwa: report/defence | Passenger/economy explanation and blank human records | Authentic records and Tadiwa's rehearsal |

## Integration decision

Both supplied projects use the locally installed Unity 6000.6.2f1. The original foundation's 6000.3.27f2/HDRP17.3 pins are not silently changed. `UnityHDRP` uses HDRP17.6 for this Editor; `UnityStarter` retains Built-In/WebGL. Share gameplay edits deliberately between Runtime/Editor folders. Do not copy HDRP ProjectSettings into the browser project. The workbench and walkthrough explain these boundaries.
