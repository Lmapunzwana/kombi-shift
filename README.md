# Kombi Shift — current Unity project

Open this folder in Unity **6000.6.2f1**. This cleaned project is based on `feat/kombishift2-unity6` revision `ddd18ae`.

Choose **Kombi Shift > Project Tour** for a guided explanation of the folders. Start with **Kombi Shift > Open Main Menu**, then Play and Start Shift.

Only two active scenes remain: `KombiMainMenu` and `KombiShift_FirstShift`. Earlier scenes, bus prototypes and demo assets are preserved outside the project in the sibling `_Archive_KombiShift2_Before_Cleanup` folder.

Read [the project guide](Docs/PROJECT_GUIDE.md) for controls, the folder map, system connections, known unfinished work and archive recovery. See [cleanup validation](Docs/CLEANUP_VALIDATION.txt) for the Unity checks.

The cleanup and gameplay updates are maintained on `feat/kombishift2-unity6`. Asset imports and original licence/attribution requirements remain relevant; see Docs/ASSET_REGISTER.csv and the source packs.

The current First Shift includes seated station queues, visible walk-in boarding, two rival kombis competing for those queues, a circular minimap and a compact speed/gear display. Stop below 2 km/h and hold Space while boarding. See Docs/PROJECT_GUIDE.md for the new components and remaining persona economy limitations.

The scene hierarchy is grouped into systems, environment, roads, player, stations and traffic. Another 220.2 MB of unused content is archived outside the project in `_Archive_Structure_Cleanup`; see its manifest and Docs/PROJECT_GUIDE.md. Vehicle grounding checks are in Docs/Validation/Grounding.
