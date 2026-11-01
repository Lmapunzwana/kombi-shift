# Local Unity preview

The `UnityStarter` directory is an isolated, complete Unity 6000.6.2f1 Built-In project for previewing Jayden's generated prototype. It does not change the team foundation's 6000.3.27f2/HDRP settings. From the repository, add `UnityStarter` in Unity Hub, open the generated scene and press Play. A local Linux release binary, when built, lives under ignored `Builds/Linux` and can be opened through `Run_Kombi_Shift.sh`.

To rebuild with an installed Unity Editor:

```sh
/path/to/Unity -batchmode -nographics -quit -projectPath /absolute/path/to/UnityStarter -executeMethod KombiShift.JaydenStarter.Editor.StarterBuild.Linux -logFile /tmp/kombi-build.log
```

The Linux binary requires Linux; it is not a Windows build. Build outputs and Unity caches are excluded from the repository. The scripts can also be copied from `UnityStarter/Assets/KombiShiftJayden` into the team project; use its Create Starter Scene menu to obtain pipeline-appropriate material references, and validate HDRP exposure/lighting. Do not run both sets of managers in one scene without integrating their contracts.

Automated smoke mode: launch the Linux player with `--kombi-smoke --kombi-exit --kombi-report=/absolute/path/results.json`. It tests generated objects, physics movement, boarding/pause, drop-off payout/event, fare refusal, damage, refuelling, early failure, rain, successful completion, replay and menu return. The test isolates passenger checks from traffic contact and invokes actions directly; it is not a human playtest or keyboard-input test.

## Controls and visual revision

Click the preview window and press Enter or Start Driving. WASD/arrows drive; Space brakes; E interacts; Escape pauses. Mouse-held A/W/S/D/brake buttons are provided. The player pauses automatically on focus loss in a normal session. Native W/D/Space/S/Escape events were delivered to the game’s actual X11 client window and movement/steering/pause telemetry was recorded; this is automated evidence, not human playtesting.

The visual revision adds detailed city facades, street furniture, a rounded kombi, shadows/fog, depth-tested signs and a redesigned HUD. See the updated Validation folder for precise results and limitations.
