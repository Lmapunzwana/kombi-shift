# Current source readiness — 9 November expansion

Read [EXPANSION_ENGINEERING_REPORT.md](EXPANSION_ENGINEERING_REPORT.md) for current executed checks, build status and limitations. The 8 November PDFs, deck, submission folders and release evidence remain historical snapshots; do not present them as evidence for the new systems.

## Implemented in current source

- Physical WheelCollider rival driving, utility station evaluation, driver profiles, left-lane offsets, clearance-based passing, identifiable colours and activity feedback.
- A connected approximately 1.25 km itinerary using existing roads, seven stations, extended traffic/rival/minimap navigation and shallow physical speed humps.
- Individual player passenger trips, intermediate drop-off requests and animation, recovery from missed stops, delivery fares and cumulative delivered totals.
- Separate happiness and XP, rate-limited reactions, severity-based impact condition, bounded RCC input modifiers and paid stopped-service repair.
- Configurable traffic-law zones, speed grace and continuous incidents, fine records and secondary HUD metrics.
- Stage settings assets, lower Morning traffic, greater Rush pressure and higher Rush delivery fares.
- Rule/component, Unity Test Framework, actual-scene integration, RCC hump and existing regression fixtures; new evidence is kept under Docs/Validation/Expansion.

## Required human work

1. At least three authentic tester sessions on identified builds/devices.
2. Severity analysis and at least three actual observation-linked fixes plus same-task retests.
3. Real member contributions/understanding, live walkthrough/change practice and Q&A.
4. Actual assigned-book editions/sections and design connections.
5. Native Windows, weakest-machine, browser audio/input/accessibility and fresh import/extraction checks.
6. Imported dependency/source hand-in permission, including the existing RCC source redistribution restriction.
7. Any genuine early feedback that exists; current design and storyboard are not backdated evidence.
8. Use the current dated expansion PDF/report; refresh the deck and final submission snapshots after human review.

## Engineering limitations to review

The road itinerary is not a general branching lane graph. Full intersection right-of-way, robust endpoint turning/alternate-route recovery and controlled oncoming/passing scenarios need further validation. Give-way is caution/speed control rather than a complete yield-violation classifier. Rival offences are only partially recorded. Dedicated severe-damage smoke/engine audio and complete whole-game before/after CPU/GPU profiling are not claimed. Actual platform results and any remaining test failures belong in the engineering report.
