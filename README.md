# Kombi Shift — Sprint 0 scaffold

This is a **partial Unity Assets scaffold**, not yet a complete or runnable Unity project. It deliberately does not fabricate `ProjectVersion.txt`, `manifest.json`, HDRP package versions or `.unity` scenes.

## Bootstrap in Unity
1. Agree on and install the **same Unity Editor version** on every machine.
2. Create a new Unity HDRP project using that version.
3. Copy this scaffold's `Assets/Scripts` and `Docs` into the project, plus `.gitignore` and `.gitattributes` at repository root.
4. Commit Unity-generated `Assets`, `Packages`, and `ProjectSettings` including `ProjectSettings/ProjectVersion.txt` and `Packages/manifest.json`. Commit `.meta` files.
5. Run `git lfs install`; configure one-reviewer PR protection when supported. The private remote repository already exists.
6. Create a Bootstrap scene and add one GameManager component. Create one EconomyManager object in the gameplay scene.
7. Test state transitions, passenger/fare events and the build on every workstation.

## Ownership
Leroy: integration/core. Jayden: art/HDRP. Euclide: vehicle. Tendai: AI/traffic. Tadiwa: passengers/HUD.

## Important
The Unity version is **not specified in the source sprint plan**. Do not invent a version lock. Economy values are placeholders and must be balanced against gameplay tests.
