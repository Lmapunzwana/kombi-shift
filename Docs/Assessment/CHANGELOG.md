# Current technical changes - 8 November 2026

These are source-review/technical findings, not invented human tester feedback.

- Added two configurable stages with visible goal/time, rival speed/head-start changes and successful-stage best earnings.
- Added title stage selection and success-to-Rush-Hour flow.
- Added fare, pause and shift-end notifications for code communication/defence.
- Full cabins route to terminal rather than waiting at an unavailable pickup.
- Results wait for terminal passengers to finish alighting; result state freezes the world.
- Rivals with no target cruise and full rivals depart instead of remaining inactive.
- Added pause resume/replay/menu and master-volume access.
- Made legacy autonomous batch smoke mode opt-in; ordinary batch/verification launch no longer runs it unexpectedly.
- Replaced traffic query allocations with reusable buffers and correct saturation fallback; measured current-scene query fixture.
- Added staged integration checks for both stages and current Linux/Web/Windows export methods.
- Prepared matching report, annotated storyboard/flows, source diagrams, 13-slide deck, learning guide, member details and genuine-evidence worksheets.

Automated verification setup issues were corrected before accepting results: player/controller lookup, input component lookup and runner placement during editor scene reloads. Earlier failed runs are not represented as passed checks.

- Browser inspection found clipped instruction lines; enlarged the instruction area and allowed header/detail text its real line height.
- Windows export found inherited Oculus XR loader/API constraints; disabled unused desktop headset initialization and selected Direct3D 11 for Windows.
- Workspace launchers now open/run KombiShift2 by default; earlier launchers are retained in Review/Legacy_Workspace_Launchers.

## 13 November 2026 — ambient city traffic

- Added directed offscreen fleet spawning for 28 Morning and 48 Rush Hour vehicles, kinematic distance LOD and behind-player recycling.
- Added smooth lane connectors, IDM-style following, road-user-only sensing, legal same-direction passing and pothole response.
- Added signal, STOP, give-way, merge and roundabout reservations with bounded deadlock arbitration and stop-line enforcement.
- Completed 10-minute automated Morning and Rush Hour soaks with full fleets, zero red/STOP violations and zero junction waits over 30 seconds.
