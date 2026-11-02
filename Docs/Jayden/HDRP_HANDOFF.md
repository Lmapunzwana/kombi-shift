# HDRP implementation handoff — pending Editor verification

Foundation PR #1 reports Unity 6000.3.27f2 / HDRP 17.3.0. Check ProjectVersion.txt and Packages/manifest.json in that branch before importing. Main had only README.md when inspected on 1 November. No Unity project or generated VolumeProfile is supplied by this pack.

1. Import foundation in its exact Editor; record package resolution and HDRP Wizard results. Follow the installed package's own Wizard guidance for missing settings.
2. Create an isolated Jayden art look-development scene with road sample, neutral grey cube, kombi scale block, passenger scale block and chase-camera reference. Save outside shared scenes.
3. Create actual day and rain VolumeProfile assets through the installed HDRP Editor UI; use physically based exposure/sky controls available in that version. Start with fixed exposure during comparison, modest tone mapping and no motion blur. Tune against readable road shadows and white livery, not arbitrary numeric exposure targets.
4. Save generated HDRP pipeline and quality assets only after coordinating shared ProjectSettings with Leroy. Avoid serialised YAML guessed outside Unity. Commit .meta files with assets.
5. Capture day/rain comparison at the same camera and 720p; check fuel text, lane markings, passenger silhouette and bright highlights. Record graphics API, GPU, resolution, Editor version and quality tier.
6. Profile the weakest laptop with representative traffic and particles; agree a frame-rate target with the team. Record actual timing and adjust shadows, effects and render scale using measured results.
7. Check import, Play Mode, missing materials, console errors and a desktop build. Log evidence and request an independent reviewer. None of these checks is marked complete here.

Second platform is a team decision due 3 November. Keep source models and palette portable while awaiting HDRP exception or URP fallback; this art pack makes no platform support claim.
