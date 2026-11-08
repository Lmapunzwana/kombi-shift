# Learn Kombi Shift 2 in Unity

This guide teaches the actual current game in small lessons. First learn what the player does, then locate the objects, then follow the code. Stop Play mode before making permanent changes. Keep a note of every value you change and restore it after practice.

## Lesson 1 Open the right project

Use Unity Hub to add the repository folder containing Assets, Packages and ProjectSettings. Choose Unity 6000.6.2f1. For a ZIP extraction, that folder is HAI311_FinalProject_6/02 Unity Source/KombiShift2. A ZIP copy has no repository history; edits there will not update the shared checkout.

Wait for imports and compilation. In the Project panel open Assets > KombiShift2 > Scenes > KombiMainMenu. Press the Play triangle once. Open How to Play. Choose Morning Shift or Rush Hour using F2/the stage selector, then Start Shift. Press Play again when finished to leave Play mode. The custom Kombi Shift > Play Main Menu command also opens the correct scene and starts it.

Morning gives 600 seconds and requires 1+ delivered passengers. Rush gives 420 seconds and requires 6+, with faster rivals and a shorter head start. Earn $2 when a person reaches a vehicle seat, then a remaining-time bonus on success. Follow the ordered pickups to Mbare. Expiry or insufficient terminal delivery fails.

Practice: explain the objective out loud without reading the guide. Drive, brake with Space, watch boarding, pause with Escape, restart with R, and return to title. Do not start studying hypothetical fuel/repair mechanics from the older report: they are not current rules.

## Lesson 2 Recognise the Unity windows

- Hierarchy: objects in the currently open scene. Think of this as the world's contents list.
- Scene view: your editor workspace for looking around and arranging the world. Its viewpoint is not automatically the player's camera.
- Game view: what the configured game camera and runtime interface show to the player.
- Inspector: components and values belonging to the selected object or asset.
- Project panel: files/assets saved on disk. Selecting a prefab here is different from selecting its instance in the Hierarchy.
- Console: errors, warnings and information. Double-click a script error to find its source. Imported-package warnings do not mean you should ignore new errors.

Select an object in the Hierarchy and press F while the Scene view has focus to frame it. Expand its children. Change only one selection at a time so you can see how the Inspector follows it. If the layout is unfamiliar, use the window names rather than assuming everyone has identical panel positions.

A Scene is a saved world/screen. A GameObject is an object in it. Transform gives position, rotation and scale. Components add physics, visuals or behaviour. Prefabs are reusable configured objects. Materials determine surface appearance using shaders. A C# component connects code to an object.

Practice: point to each window and describe what it is for. Find one Transform, one renderer and one script component. Opening a file is not the same thing as understanding how a scene references it.

## Lesson 3 Explore the playable city

Stop Play. Choose Kombi Shift > Open First Shift, then Show City Overview. Open Project Tour for folder explanations and selection shortcuts.

The scene is grouped into:
1. 00 Systems and Lighting: rules, input, navigation, weather and lighting.
2. 01 Environment: buildings and scenery.
3. 02 Roads and Route: road geometry and route points.
4. 03 Player and Camera: player Hiace, physics and camera.
5. 04 Passenger Stations: pickup stations, people and terminal.
6. 05 Traffic and Rivals: background vehicles and competing kombis.

Find `KombiShift_CityGame_Systems` with Hierarchy search. Select it. Its attached components include KombiShiftGameDirector, KombiNavigationHud and RccHiaceTestBootstrap. The input bootstrap is on this systems object, not necessarily on the car. Read the director's Vehicle and Stops references: these link the rule controller to actual scene objects. Select the referenced objects to follow the connection.

Practice: locate the player, a station, Mbare, a rival and the game camera without changing them. Explain the difference between road art and route points.

## Lesson 4 Understand the folder map

Everything specific to the current game lives under Assets/KombiShift2:
- Scenes: title and First Shift.
- Scripts/Gameplay: challenge rules, director, stations, passengers and persona data.
- Scripts/Vehicle: keyboard integration, powertrain, cabin, accessories and stability.
- Scripts/Camera: chase/driver/orbit camera.
- Scripts/Traffic: rivals, ordinary traffic and signals.
- Scripts/UI: menu, showcase and navigation HUD.
- Scripts/Weather: weather support; this is not a separate highway level.
- Scripts/Development: verification code, including an explicitly opt-in release runner.
- Prefabs/Vehicles/Player: Hiace_Player reusable vehicle.
- Prefabs/Passengers and Data/PassengerPersonas: appearances/rigs and character definitions.
- Art: imported visual assets; ThirdParty retains original sources/notices.
- Resources/Fonts: fonts loaded by the title code.
- Editor/Project: navigation/tour helpers; Editor/Validation: checks and release tools.

Packages lists Unity dependencies. ProjectSettings stores project configuration. Docs explains design/evidence. Library and Temp are regenerated caches and do not belong in the submission source. Keep .meta files with their assets: their identifiers preserve Unity references.

## Lesson 5 Trace the title into gameplay

Read Scripts/UI/KombiMainMenu.cs. Awake prepares fonts, sounds and saved settings. Update handles focus and keyboard choices. OnGUI draws the current interface. Activate chooses the relevant action; StartShift is a coroutine that transitions and loads KombiShift_FirstShift asynchronously.

Read Scripts/Gameplay/KombiChallenge.cs. Selected holds the chosen stage. Seconds, MinimumPassengers, RivalSpeed and RivalDelay provide different rules. Best and SaveBest use PlayerPrefs for successful stage scores.

Practice: find the exact line that defines 420 seconds. Explain why opening the menu and city scenes does not itself prove two playable levels; the challenge configurations supply the two playable states here.

## Lesson 6 Follow one passenger through the whole system

Read KombiShiftGameDirector.cs first. Start selects stage settings and finds the configured stations/rivals. Update handles pause/restart, decreases the timer, advances empty stops and checks position/speed before service. A full cabin points to the terminal.

At a pickup, the director starts its Board coroutine. PassengerStation reserves the queue, finds separate free seats and starts passenger boarding. StationPassenger moves each person toward the doorway and seat. Once seated, a callback adds the person to the director's onboard list, increments passenger count, credits $2 and publishes OnFareEarned.

If the player drives away before seating, the continuation condition becomes false and the person returns to the bench. A rival uses the same station reservation mechanism, so two kombis cannot both reserve the queue at once.

At Mbare, DropOff starts staggered alighting, waits for people to reach terminal seats, then checks the stage target. Finish records success/failure, adds a success bonus, saves best earnings, stops rival action, freezes the result state and publishes OnShiftFinished. Resume/menu/restart restores running time when appropriate.

Practice: in Play mode watch a passenger before boarding, while walking and after seating. Observe when earnings actually change. After stopping Play, trace the exact callback in code. Explain a coroutine as a sequence spread over frames/time, and an event as a notification to listeners.

## Lesson 7 Explain driving without claiming the whole engine

Select the player and inspect Rigidbody, wheel collider setup and RCC components. RCC supplies the imported vehicle engine. Custom input code converts W/S/A/D and Space into RCC inputs; KombiPowertrain configures engine/gearing and gradually limits throttle between 95 and 105 km/h; KombiVehicleStability provides upright/grounding helpers. RccHiaceTestBootstrap's name is historical. Its legacy autonomous smoke behaviour now requires an explicit test switch.

Rigidbody represents physical motion. Colliders provide contact shapes. WheelColliders model wheel contact. Raycasts probe the world. Frame update and physics update have different purposes; read each component's actual methods rather than assuming all motion is implemented the same way.

KombiDriveCamera controls chase/driver views, orbit/zoom and obstruction/ground checks. KombiPassengerCabin stores doorway/seat references. Accessories handle lights and wipers.

Practice: identify a real wheel collider and its role. Explain which code maps keyboard input, which configures speed and which package supplies the physics. Do not claim downloaded engine code as personally written.

## Lesson 8 Explain autonomous rivals and traffic

Select either rival under Traffic and Rivals. Read RivalKombiAI.cs. Its named states are Cruise, ChasePassenger, PullIn, Boarding and Depart. It chooses waiting, unreserved stations using route-index distance; follows waypoints; pulls into the stop; boards; and cruises/departs when appropriate. Obstacle sphere casts make it wait rather than teleport through vehicles. This is waypoint logic, not a universal road pathfinder, and it does not implement the older Block/Flee design.

KombiTrafficVehicle follows route points and responds to vehicles/signals. Its reusable query buffers avoid repeated hit-array creation. A full buffer falls back to a complete query so optimisation does not silently lose obstacles. The measured fixture compares query modes on the current scene, not whole-game FPS.

Practice: explain one state change and the condition causing it. Watch a rival actually approach/board. Explain one limitation and why a buffer needs a saturation fallback.

## Lesson 9 Inspect visuals animation and sound

Select a passenger prefab and its humanoid source rig. StationPassenger procedurally blends seated and walking poses; it is not claiming downloaded walking clips or an Animator transition graph. Visual appearance, rig and movement logic are separate concerns.

Select a material and identify its shader/textures. Select the camera and a light. Inspect the title's runtime audio generation and RCC's engine audio settings. Master volume affects listening level; device audibility still needs people to test it. No-shadow presentation is a deliberate setting, not evidence of measured GPU speed by itself.

Practice: explain the difference between model, material, rig, animation and script. Mute/unmute through supported controls and test on the actual presentation machine.

## Lesson 10 Read evidence correctly

Open Docs/Validation/Assessment and Docs/Assessment/READINESS.md. Both stages passed 17 staged integration checks in Editor/Linux and Windows under Wine. Browser checks are saved separately. The tests position the vehicle at stations and isolate rivals for some service cases; they are not human full-shift tests. Wine does not prove native Windows hardware support. A successful export does not prove every user's machine works.

Use TESTING_INSTRUCTIONS.md and the blank records for at least three real testers. Record observations/severity, make at least three genuine feedback-based fixes, and retest the same tasks. Do not copy a technical fix into a fabricated tester story. Fill contributions only with actual reviewed/modified work.

## Lesson 11 Make one safe change and explain it

Work in the shared repository checkout. Save/commit your own work before pulling another person's changes. Stop Play, record the original value and change Morning duration in KombiChallenge.Seconds. Run Morning and confirm the displayed clock changes. Stop Play and restore the value. Alternatively change the boarding payment in the director callback or rival speed in the stage configuration, then check the actual observable effect.

A serialized Inspector value can override a script's default, and some values are deliberately set at runtime. Read the code before assuming that editing a default changes the current scene. Many Play-mode edits disappear when Play stops; treat that as temporary experimentation.

For the panel, explain the expected effect before running the change. Show the responsible file/component, make the change, observe it and restore it. Every member should be able to do this for a simple rule, not just their assigned study area.

## Lesson 12 Rehearse as a group

Use the slide deck as prompts, not a script to memorise. Rehearse concept/audience (5 min), both gameplay states (3 min), source plus a live change (5 min), then questions (5 min). The brief describes a 20-minute session although those blocks total 18; keep transition margin.

Each member should answer: What does the player do? What changes between stages? When is money credited? What causes failure? How does a queue reservation work? Which part is imported? What did I actually review/change? What does our evidence establish? What remains incomplete?

Suggested first study areas: Leroy architecture/integration; Jayden art/UI/audio; Tadiwanashe passengers/scoring; Euclide input/vehicle/camera; Tendai rivals/optimisation. These are learning assignments, not authorship claims, and everyone still learns the whole loop.
