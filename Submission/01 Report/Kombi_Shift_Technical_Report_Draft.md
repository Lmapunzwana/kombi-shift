# Kombi Shift - technical report and review workbook

Draft for team review.


## KOMBI SHIFT

Technical report and review workbook - 3 November 2026


### Project overview

Kombi Shift is a 3D arcade driving and light-management game for casual players aged 14 and above. The Harare-inspired setting uses fictionalised route arrangements and a recognisable minibus, stop bays and conductor. One shift asks the player to earn a fare target while managing passenger deadlines, fuel, health and vehicle wear.


### Core loop

Choose CBD morning or rainy highway and optionally hire a hwindi. Drive to a marked stop, brake inside its pickup trigger, board individual passengers, select their destination on the map, and deliver promptly for fares and tips. Buy fuel and repairs at a station when affordable. Reach the fare target to win; zero fuel, zero health or the shift clock ending below target loses.


### Scope and implementation

The implementation uses Unity 6000.6.2f1, Rigidbody 3D arcade physics, explicit manager/controller components, pooled actors and coroutine service sequences. UnityStarter provides Built-In desktop/WebGL support; UnityHDRP is a separate HDRP desktop project. The early WheelCollider prototype task is adapted to arcade physics, as permitted by the sprint plan. No WheelCollider suspension is claimed.


### Status and integrity

This is an AI-assisted technical draft for team review, not proof of human testing or individual authorship. The five members must review, modify where needed, and record their actual contribution. Group/registration numbers, authentic tester findings, independent review, weakest-laptop checks and course-text references still need team evidence.


## AUDIENCE AND JOURNEY

Design hypotheses, storyboard and navigation


### Three proposed personas

Persona A: a Zimbabwean secondary-school player, 16, familiar with kombi culture, seeking short entertaining sessions and clear stop cues. Persona B: a university student, 21, who enjoys optimising fares and routes but has a modest laptop. Persona C: an occasional player, 30, unfamiliar with game controls, who needs readable menus, patient onboarding and touch or mouse alternatives. These are design hypotheses, not interviewed participants.


### Storyboard

1. Title: recognise the setting and locate Start. 2. Selection: compare CBD/highway and the conductor cost. 3. Approach: locate a stop on the map and halt in its painted bay. 4. Boarding: watch the sliding door, walking passengers and hwindi; a progress bar confirms service. 5. Delivery: follow the selected destination, read patience and receive a single payout. 6. Statement: interpret gross fares, tips, costs and take-home, then replay or return.


### Screen flow

Title -> instructions or route selection -> playing -> pause/settings -> resume. Playing -> win/lose statement -> replay or title. Pause also offers restart or return. Keyboard focus guidance, optional touch controls, HUD text scaling, numeric patience with bars, and separate music/effects sliders support accessibility. Navigation uses map/stop markers without a guidance arrow, as Jayden requested.


### Feedback and design rationale

A latched brake hold keeps the player stopped during service; actual door and conductor states explain the boarding delay. Individual destinations create route choices; locked fare quotes make price commitments predictable. Separate cash and earned-fare totals make station purchases meaningful. A high fare can be rejected according to a passenger's tolerance, wait and vehicle health. These design choices require usability and balance confirmation with real players.


## ARCHITECTURE AND PHYSICS

Traceable C# systems and player interaction


### State and communication

GameManager exposes Menu, Playing, Paused and ShiftEnd, and publishes OnStateChanged, OnFareEarned, OnFuelChanged, OnVehicleDamaged and OnShiftEnded. ShiftGame partial files compose the world, gameplay, UI, weather, conductor/audio and diagnostics. EconomyManager holds gross income and expense totals; FuelSystem performs affordable purchases. ConductorModifier defines the hired boarding benefit and cost.


### Vehicle control

VehicleController operates in FixedUpdate, calculates forward/lateral velocity in vehicle space, applies acceleration and tyre grip, and rotates according to speed and steering. Raycasts identify ground while excluding the car. Space requests braking and latches positional constraints once stopped; deliberate acceleration releases the hold. Service/door interlocks prevent departure before passengers and the conductor are ready.


### Wear and hazards

VehicleWear separates brakes and suspension. Repeated braking reduces brake condition; impacts reduce suspension and vehicle health. Worn brakes increase stopping distance; suspension wear reduces lateral grip. Traffic collision damage is capped and rate-limited for fairness. Pothole triggers damage fast-moving cars. Station repairs cost $3 cash, restore 30 health and 35 wear condition, and are location/speed gated.


### Passenger and animation

PassengerAI tracks waiting, boarding, onboard, delivered, expired and rival-taken states. A pause-aware coroutine decreases individual patience while waiting/onboard. Boarding and drop-off coroutines move characters through the door before paying or changing the manifest. A real Animator controller transitions Waiting <-> Walk to door using the Walking bool. Quotes lock on boarding; late passengers pay nothing. Inspect Resources/Animation/Passenger.controller and Runtime/Systems.


## LEVELS AND AI

Two distinct challenge configurations


### CBD morning

Six stops span two connected streets with cross streets. Default shift duration is 300 seconds, fare target $18, starting fuel 100, fuel use 0.035 per metre, fuel price $0.20 per unit, nine traffic vehicles plus the rival, and six potholes. Shorter routes and dry grip make this the introductory level.


### Rainy highway

Four stops at 45, 185, 325 and 465 metres create longer trips. Default shift duration is 300 seconds, target $28, starting fuel 45, fuel use 0.08 per metre, fuel price $0.30, four traffic vehicles plus the rival, twelve potholes, reduced lateral grip, rain/fog and 1.5 rival aggression. Remittance increases from $8 to $12. These are initial values, not balance conclusions from testers.


### Rival state machine

Cruise follows the road network when no passenger is available. ChasePassenger pursues the nearest waiting passenger and can collect them. Block targets a bounded interception point near the player, with a cooldown. Flee chooses a distant road node when damaged or blocked. Front obstacle casts stop unsafe forward movement; limited reverse lets a fleeing rival back away. RoadGraph supplies waypoint paths; the workbench shows the current state live.


### Scoring and economy

Base fare is 1 + route distance * 0.018, multiplied by the selected fare multiplier and locked when boarded. Timely delivery adds up to 25% of base fare as a tip scaled by remaining patience. Fuel and workshop purchases use available cash. A clean shift adds 5 score points. Remittance and the optional $3 conductor cost appear in the statement. Best stage scores persist locally. GameTuning.asset exposes balance changes in the Inspector.


## ART, AUDIO AND OPTIMISATION

Reusable assets, rendering and measured evidence


### Assets and rendering

Procedural meshes form the minibus, buildings, modular roads, shelters, signs and fuel station. A Catalog folder contains reusable prefabs, and passenger clothing varies. The 3D adaptation retains muted finishes and no cast shadows. The HDRP project has real pipeline and day/rain VolumeProfile assets, fixed exposure, gradient sky and ACES tonemapping. It requires a Vulkan-capable Linux or supported desktop GPU; HDRP is not a WebGL/Android pipeline.


### Audio and provenance

The engine includes a CC0 recording by bikesnbassboi, Bus Engine Idling, source: https://freesound.org/people/bikesnbassboi/sounds/540398/. Original procedural horn, door, brake and coin effects, rain and a generated theme accompany it. Conductor speech remains temporary synthesized English using eSpeak NG, not a consented local human recording. Complete preparation/licence details are in Resources/Audio/CREDITS.md and Tools/prepare_audio.py.


### Pooling measurement

The controlled headless development fixture sampled 240 frames per mode after warm-up, with the same CBD x=-3/z=0..100 route, nine traffic vehicles, one rival, twelve pedestrians and six hazards. Recycling one pedestrian and hazard per frame produced mean GC allocation of 988.65 bytes/frame baseline versus 445.50 pooled, about 54.9% lower. Traffic stayed pooled in both modes to isolate pedestrian/hazard churn.


### Measurement limits

Mean wall frame times were 16.71 versus 16.65 ms, with frame-cap waiting included. These results do not establish a general FPS improvement or HDRP performance on the weakest laptop. Raw profiler captures and frame CSV/JSON are retained. These are final Built-In headless measurements; repeat representative gameplay on the final HDRP setup and weakest target laptop.


## TESTING AND HANDOFF

Evidence, remaining work and source references


### Automated verification

The completed Built-In Unity Editor Play Mode regression passes 49 checks. Coverage includes physical movement/hold, trigger gating, multiple passengers, patience, pause, locked quotes, destination payout, tips, late delivery, paid fuel, pool reuse, outcomes, level differences, rival states, wear-dependent braking, real animation, manager states and paid repair restrictions. This is not human playtesting. A Unity SearchDatabase startup exception was observed outside game code and must remain disclosed.


### Human verification plan

Recruit five consenting testers, complete at least three authentic sessions, record ID/date/build/device/tasks/outcome/severity, prioritise the top three problems, implement fixes and retest. Use the blank CSVs provided; no tester responses have been invented. Confirm keyboard/mouse focus, steering and braking, map interpretation, passenger routing, fuel/repair cost comprehension, pause, audio and both stage outcomes.


### Team handoff

Leroy: review architecture, pipeline decision, integration and packaging. Euclide: vehicle/wear/camera review and live parameter change. Tendai: rival/navigation/pooling review and profiler interpretation. Tadiwa: passengers/economy/UI review and authentic test records. Jayden: art/UI/audio review, attribution and final illustrations. Assigned areas are not authorship claims. Open the workbench to inspect and demonstrate each system.


### References and unfinished submission items

Implementation references: Unity Wheel Collider tutorial (used to assess the arcade adaptation), Unity HDRP 17.6 system requirements, and included package source for HDRP asset APIs. The supplied HAI311 assessment and sprint plan define requirements. Add the actual editions/pages consulted from Schell, Salen and Zimmerman, Geig, Felicia, Thorn, Hardman and Buttfield-Addison et al.; no consultation of those editions is claimed here. Finalise registration numbers, contribution records, external review, hardware results and report reflection before submission.
