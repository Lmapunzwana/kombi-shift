# Kombi Shift — Jayden standalone Unity starter

## Open the complete local project

This folder now includes Packages and ProjectSettings for an isolated **Unity 6000.6.2f1 Built-In preview**. Add this folder to Unity Hub to open it directly. Open `Assets/KombiShiftJayden/Generated/KombiShiftStarter.unity` and press Play. The local Linux build can be launched with `../Run_Kombi_Shift.sh`.

Validation: Linux player build succeeded and **22 control-to-physics and gameplay checks passed**, plus a native keyboard-event test. See `Validation/RESULTS.md`. Native city/rain screenshots were inspected. No human playtest, team-version or HDRP validation is claimed.

## Import scripts into an existing project

1. Use a Unity 6 project. For the shared game use the exact Editor version pinned by Leroy (foundation PR reported 6000.3.27f2). Do not upgrade the team project just for this pack. Back up/branch the project before importing.
2. Copy the **KombiShiftJayden** folder inside this pack's **Assets** folder into your project's **Assets** folder. Keep Runtime and Editor subfolders intact. Do not copy this README into a C# script.
3. A pre-generated Built-In scene is included at `Assets/KombiShiftJayden/Generated/KombiShiftStarter.unity`; double-click it for the Built-In demo. For HDRP/URP or a fresh scene, wait for Unity to compile. Open **Kombi Shift > Jayden > Create Starter Scene** in the top menu. Save any current scene when asked.
4. The command creates a new isolated scene in `Assets/KombiShiftJayden/Generated`, a persistent material and a root object. Press **Play**. The world and interface appear at runtime.
5. Click **HOW TO PLAY**, or **START SHIFT**, select the challenge and optionally hire a hwindi, then **DRIVE**. Click inside the Game view for keyboard focus.
6. To build: add the generated scene to a separate Build Profile's scene list and make it the first scene. Build a desktop standalone player with your installed target module. Test the built executable. This tool does not modify the team's build scene list.

For the simplest isolated demo use Unity's Built-In 3D template. The scene builder also selects HDRP/Lit or URP/Lit when the project already has that pipeline configured. It does not install or configure a render pipeline. In HDRP, complete HDRP Wizard setup and supply the project's normal global exposure/sky Volume; if the scene appears dark, tune the main directional light and exposure in your look-development setup. The generated Sun uses ordinary Unity intensity defaults, not a calibrated HDRP physical-light setup. No cross-platform/HDRP compatibility is claimed without testing.

## Presentation and control revision

The preview now uses rounded vehicle geometry, 24 detailed shop blocks with windows/awnings/fascias, pavements, road markings, street trees/lights, rank shelters, benches, fuel pumps, textured asphalt, shadows and weather fog. Menus and HUD use a dedicated layout with route diagram, speed, fare buttons, status bars, contextual boarding action and mouse-held drive buttons.

**Click the game window, then press ENTER or START DRIVING.** Keyboard controls use direct keys without requiring axis mappings. A zero-friction body contact with controlled tyre grip and explicit ground filtering lets the vehicle accelerate from rest reliably. Switching to another app automatically pauses a normal play session; return and click Resume or press Escape. The background setting keeps captures/diagnostics responsive, not gameplay running unattended.

The updated world-label shader is authored for this Built-In preview. HDRP/URP import requires an equivalent pipeline-compatible label material and normal lighting validation; it is not a verified pipeline conversion.

## Controls

| Input | Action |
|---|---|
| W/S or up/down | Accelerate / reverse |
| A/D or left/right | Steer while moving |
| Space | Brake |
| E, while stopped | Board at yellow marker; drop off at next marker; refuel near station |
| R | Recover to road, $1 cost |
| Escape | Pause/resume |
| Mouse | Menus, fare −/+ buttons, boarding action, held drive buttons, volume settings |
| Enter | Start driving from menu/selection |

Supports direct keyboard input through Unity Input System when enabled, otherwise the legacy Input Manager, with GUI key-state fallback and window-focus feedback. No Input Actions asset required. Keyboard/mouse desktop controls only; no touch or controller UI. Menus use a custom IMGUI layout and Unity’s bundled runtime font to avoid additional package/font setup. These remain procedural prototype assets for human review.

## Generated content

- Teal/yellow minibus with separate window, stripe and wheel primitives; Rigidbody arcade driving and collisions.
- Road, lane markings, city blocks, rank signs, pothole markers, fuel station and a chase camera.
- Passenger placeholder with timed walk-to-door boarding; pickup, drop-off, patience and fare refusal.
- Two challenge settings on the same route: daytime city and rainy lower-grip driving with higher remittance/fuel use.
- Seven preallocated traffic vehicles reused around the route; one rival with visible Cruise, ChasePassenger, Block and Flee states. Chase shifts toward pickup lane, Block towards player lane, Flee speeds away when player condition is low. This is simple prototype AI, not obstacle-aware navigation.
- Rain particles, procedural wheel movement, generated audio cues, engine response and an original ambient loop.
- Menu, instructions, challenge/hwindi selection, HUD, pause/settings, statement, replay, return and exit.
- Fuel, damage, repair/recovery costs, remittance, hwindi cost and take-home; persistent sound volume settings.

Everything is generated using Unity primitives and original procedural tones. There are no external models, textures, music downloads or fonts to import. Professional models, recorded audio, baked lighting, authored animation clips/Animator transitions and HDRP VolumeProfiles remain production work.

## Gameplay rules and tuning

Default shift is 240 seconds. Win at time expiry with at least three completed trips and non-negative take-home. Empty fuel or zero condition ends the shift early. Each completed passenger pays selected fare x6. Fare greater than $1.60 or condition below 20% is refused. Passengers leave after 60 seconds. Stop within 7m, speed below 1.2m/s and press E; boarding lasts 2.8s, or 1.3s with the $3 hwindi. Remittance is $8 city/$12 rain. Fuel cost accrues as fuel is consumed at $0.08 per unit; refilling restores the tank without a second charge. Collision damage costs $0.04 per condition point. Recovery costs $1. These are starter balance values, not measured or team-approved economy numbers.

Change `shiftSeconds` in the root component Inspector. Vehicle acceleration, maximum speed and steering are public Inspector fields on the runtime vehicle; for persistent defaults edit KombiDriver.cs. Other starting values are clearly named in ShiftGame.cs. Runtime-generated objects reset on leaving Play Mode; change the generator to keep edits across runs.

## Files and ownership

- `Editor/StarterSceneBuilder.cs`: new scene and shader material asset creation; protects unsaved current work.
- `Runtime/ShiftGame.cs`: world construction, menus, state/economy/passenger/traffic/weather/audio prototype.
- `Runtime/KombiDriver.cs`: contact/grip physics, ground filtering, acceleration, steering, braking and collision damage.
- `Runtime/ShiftInput.cs`: shared direct-key / mouse drive input.
- `Runtime/ShiftGame.World.cs`: detailed environment, rounded vehicles, signage, weather and sound generation.
- `Runtime/ShiftGame.UI.cs`: menus, HUD, route map and mouse controls.
- `Runtime/ShiftGame.Diagnostics.cs`: regression, native-event telemetry and capture harness.
- `Resources/WorldLabel.shader`: depth-tested label rendering for Built-In.
- `Validation/native_keyboard_probe.py`: reproducible Linux/X11 native keyboard regression helper.

Namespace `KombiShift.JaydenStarter` keeps this separate from production classes. Run only one ShiftGame instance and no other gameplay manager, camera or AudioListener in this isolated scene. It does not subscribe to Leroy's event contracts or replace teammates' assigned work. `FareEarned` is a local integration event; reconcile data contracts before production integration. This prototype deliberately groups systems to simplify importing; production integration should move gameplay logic to the responsible team owners and retain Jayden's art generators/assets.

## Review and evidence

AI-assisted code prepared with OpenAI Codex, 1 November 2026. Jayden must review and understand it before claiming it. The Linux release build and automated smoke sequence are verified; no human playtest results, performance measurements, independent review, team-version import or other-platform build is asserted.

Before handoff: check Console, drive both modes, board/cancel/drop off, change fare, miss a passenger, collide, refuel, pause mid-boarding, change audio, restart, recover, test each end condition and replay; verify one AudioListener, material colours, visibility and camera. Record real observations and fixes. Capture before/after measurements if using traffic reuse as the assessed optimisation. A profiler marker or reused objects alone is not measurement evidence.

Known limits: no real WheelColliders/suspension, traffic avoidance or full city route network; no gamepad/touch support; no production prefabs, Animator controllers, polished UI, measured balancing or baked lighting. Both challenge settings reuse the same map. HDRP appearance and particle materials require project-specific verification. Exit works in a built player, not as an Editor stop button.

## API references

Unity's Rigidbody API: https://docs.unity.com/en-us/engine/6000.7/script-reference/unityengine/rigidbody/linearvelocity

Unity Input System setup: https://learn.unity.com/tutorial/6712ae08edbc2a05a71b5446

HDRP material implementation reference: https://github.com/Unity-Technologies/Graphics/blob/master/Packages/com.unity.render-pipelines.high-definition/Documentation~/Customizing-HDRP-materials-with-Shader-Graph.md

These references informed API use, not a claim of runtime validation on every version.
