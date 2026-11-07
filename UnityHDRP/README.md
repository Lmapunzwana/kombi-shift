# Kombi Shift - HDRP desktop project

Open this directory with Unity **6000.6.2f1**. HDRP **17.6.0** is pinned and its resolved package lock is included. Open `Assets/KombiShiftJayden/Generated/KombiShiftStarter.unity`, then **Kombi Shift > Open Game Workbench**, then Play.

The desktop renderer uses real HDRP settings plus day/rain VolumeProfiles, fixed exposure, gradient sky and ACES tonemapping. Unused expensive features are disabled; cast shadows remain off by Jayden's request. Linux requires Vulkan and a compute-capable GPU; this local build has been rendered on the machine's Mesa Intel graphics. This does not certify another member's laptop.

The companion UnityStarter project supplies the Built-In/WebGL version. HDRP is not supported by WebGL/Android; do not attempt to build WebGL from this folder. Do not overwrite the companion ProjectSettings or import both full projects into one Assets folder.

Game tuning, source, art catalog, animations and workbench have the same paths in both projects. HDRP-specific configuration is under Assets/KombiShiftHDRP; the compatible font-label shader is Assets/Resources/HDRPWorldLabel.shader. The Art Workshop scene allows pre-Play model inspection. The gameplay generator uses the persistent passenger prefab.

See ../Docs/Team/EDITOR_WALKTHROUGH.md and TASK_COMPLETION_MATRIX.md for controls, source explanations, defence exercises and remaining human evidence. Reports and offline presentation are in ../Submission. The team's earlier 6000.3/HDRP17.3 foundation is not the same project/version and is not automatically migrated.
