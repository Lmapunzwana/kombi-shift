# Speedometer verification ? 12 November 2026

Unity 6000.6.2f1 rendered the actual driving scene. The opt-in capture runner applied production RCC throttle/brake inputs; traffic vehicles and rivals were temporarily disabled in the unsaved Play session. City infrastructure remained active. This is an automated full-scene check, not human playtesting or a standalone build.

Fresh run: speed changed to 38.41 km/h; distance reached 57.80 m; braking stopped the vehicle; stopped time advanced. All four checks in live-capture.json passed. The fresh Editor log segment contained zero exception, compilation-error or inactive-coroutine errors. Previous Console history may still show earlier failed runs.

speedometer-driving.png and speedometer-stopped.png are unmodified ScreenCapture outputs. Camera switching remains accessible below the vehicle, clear of the gauge. The disposable Play session was stopped after capture and the saved main menu reopened, restoring normal traffic and keyboard input for the next run.

Fixes: TMP Essential Resources imported from the installed Unity UI package; resource import waits for Edit mode; gauge initialization waits for resources; traffic-light property blocks recover after Editor reload; boarding rejects inactive stations; capture disables traffic components/agents rather than their shared infrastructure root. Imported vendor code and saved scenes were not changed.

Unit/component tests were not executed: Unit Test MCP is unavailable, and repository rules prohibit terminal substitutes.
