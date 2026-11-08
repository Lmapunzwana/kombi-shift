# Learn and defend the current game

Everyone first plays Morning and Rush Hour. You should explain the objective without reading a slide. Then trace this path: KombiMainMenu -> KombiChallenge -> KombiShiftGameDirector -> PassengerStation -> StationPassenger -> callback -> director earnings -> terminal outcome.

## Plain terms
Scene: saved screen/world. GameObject: a Unity object. Component: behaviour attached to it. Prefab: reusable configured object. Rigidbody/WheelCollider: physics body/wheel contact. Coroutine: a sequence spread across frames/time. Event: a notification subscribers react to. State machine: named behaviours and conditions for changing them. ScriptableObject: reusable data, not proof that a rule is integrated.

## Questions every member must answer
1. What changes between stages? 600/420 seconds, 1/6 delivery target, 6/9 m/s rival speed, 45/8-second head start plus stagger.
2. When is $2 paid? Only after a passenger reaches a seat through the station callback.
3. What wins/loses? Required delivered passenger count before timeout; empty/insufficient delivery and timer expiry lose.
4. How do two kombis avoid collecting the same people? A station reservation and passenger state/seat checks.
5. What does Escape do? Pauses time and publishes an event; resume/menu restores time.
6. Why coroutine? Boarding/alighting takes time without blocking other frames.
7. Why buffer casts? Reuse hit arrays; fallback if saturated so collision correctness is preserved.
8. What is ours versus imported? Custom rules/UI/passenger/rival/integration code is AI-assisted work to review; RCC supplies its vehicle engine, and art is imported.
9. What do tests not prove? Staged integration is not human driving, good balance or device usability.
10. What remains? Read READINESS, not the old slides.

## Live change practice
Change Morning duration in KombiChallenge.Seconds; run Morning and observe timer. Change $2 in the director callback and verify actual seating income. Change Rush speed in KombiChallenge.RivalSpeed and observe movement. Restore values afterward. Do not claim another member's work merely because their name was assigned to it.

## Responsibility rehearsal suggestions, not contribution claims
Leroy: scene/controller architecture and packaging. Jayden: UI/art/audio/provenance. Tadiwanashe: stations/passengers/scoring. Euclide: vehicle/input/camera. Tendai: rival states/query optimisation. Every member must also understand the whole loop and answer panel-selected questions.

## Timed defence
5 minutes concept/audience/design; 3 minutes both stages; 5 minutes source plus live change; 5 minutes questions. The brief calls the total 20 minutes although these blocks add to 18; retain transition margin. Use actual results and disclose pending human evidence. Practice a saved local demo fallback without presenting it as a live run.
