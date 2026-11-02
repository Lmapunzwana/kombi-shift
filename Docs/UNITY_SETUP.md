# Unity setup and verification

Selected Editor: **6000.3.27f2**, Unity 6.3 LTS. HDRP direct dependency: **17.3.0**. This is the project selection, not a claim that anyone has installed or tested it.

Sources checked 1 November 2026:
- https://unity.com/releases/editor/whats-new/6000.3.27f2
- https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.3/manual/index.html

1. Install the exact Editor through Unity Hub, including Windows Build Support. Activate your Unity licence normally.
2. Clone the repository, fetch `sprint/0-foundation`, run `git lfs install --local` and `git lfs pull`.
3. Open this repository root in that Editor. Package Manager resolves HDRP and writes `Packages/packages-lock.json`. Commit that actual lock and generated ProjectSettings; do not replace them with guessed data.
4. Run **Kombi Shift > Configure project conventions**. Run **Window > Rendering > HDRP Wizard**, fix required HDRP configuration and create/assign its pipeline and global settings assets. Jayden owns the final look. Save and commit generated assets with .meta files. The manifest alone does not activate HDRP.
5. Run **Kombi Shift > Open Bootstrap**, enter Play Mode, click **Run smoke checks from Menu**. Record the exact PASS/FAIL and Console errors. This temporary IMGUI harness exercises the core, not vehicle gameplay.
6. Run `KombiShift.Editor.SprintZeroSetup.BuildWindows` through Unity's `-executeMethod`, or build Windows x86_64 using the Editor. Run the output and repeat smoke checks. A failed/missing build module is a failed build check.
7. Each member fills their row in `TEAM_SMOKE_TESTS.csv` with machine, commit and evidence. No prefilled pass is supplied.

The initial scene and settings are authored project seed files. Import, HDRP configuration, compilation, graphical output and builds remain unverified until the Editor executes them. The runtime installer only acts on the Bootstrap scene; feature owners use their own scenes/prefabs. Do not add another EconomyManager to Bootstrap.
