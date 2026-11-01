# Kombi Shift — proposed visual direction v0.1

## Feel
A lively Zimbabwean commuter shift: warm morning light, dusty verges, bold route boards and a recognisable minibus silhouette. Read the road and passengers at driving speed. Keep the tone energetic and grounded. Use fictional branding; route names require Jayden's approval.

## Palette and type
| Token | Hex | Use |
|---|---|---|
| Ink | #142623 | HUD panels, dark glass |
| Paper | #F4EBD8 | Text on ink, menu background |
| Rank yellow | #FFD34E | Primary action, route stripe, selected states |
| Kombi teal | #167D8D | Livery, navigation accents |
| Brick | #B84D35 | Damage and urgent warnings with text/icon |
| Asphalt | #444C4A | Roads |
| Dust | #B98D63 | Verges and warm environment accents |

Prototype uses system sans-serif and monospace; no external font dependency. In Unity select an available, licensed TMP font and record provenance before distribution. Use large numerals and tabular figures. Never communicate fuel, damage or selection through colour alone.

## Geometry and modular kit
- Metres; Unity Y up, +Z forward. Model at scale 1. Root pivots at ground centre; vehicle wheel pivots at axle centres.
- Proposed road module: 10 m long, 7 m carriageway, 1.5 m pavement each side. Snap lengths to 1 m; agree dimensions with Euclide before modelling intersections.
- Kombi silhouette: approximately 4.8 x 1.8 x 2.1 m (length x width x height); confirm collider and wheelbase with vehicle owner. Separate four wheels, body and windows. No production collider/physics in the art prefab.
- Broad flat colour areas, restrained bevels, simple opaque windows initially. Shared material palette; avoid unique textures per prop.
- Initial budgets are hypotheses: kombi <=8k triangles; passenger <=1.5k; small prop <=1k. Validate full-scene frame time on weakest laptop before treating them as limits.
- Naming: SM_Kombi_A, SM_Road_Straight_10m, PF_RankSign_A, MAT_Kombi_Teal. Do not overwrite shared scenes or other owners' prefabs.

## Interface
HUD: earnings at top left; shift time and fuel at top right; current stop at bottom left; fare action at bottom right. Preserve centre driving view. Reference canvas 1920 x 1080; test 1280 x 720 and 16:10. Safe padding 48 px at reference resolution. Body text >=24 px in the Unity implementation. Menu has one clear Start Shift action. Statement separates gross fares, costs and take-home; amounts must come from EconomyManager when wired by screen owner.

## Lighting and weather
Day: warm directional sun, neutral readable shadows, restrained saturation. Rain: cooler sky, reduced glare, visible lane/stop markers. Use baked environment lighting where feasible and one main directional light; profile rather than assume HDRP is affordable. Rain particles and wet surface materials are separate deliverables. Do not use a full-screen effect that hides pedestrians or traffic.

## Review gates
Review silhouette from chase camera; read HUD at 720p; distinguish stop/traffic/road in greyscale; check wet-road visibility; validate material conversion if the team adopts URP. Runtime menus and fare logic belong to the assigned screen owners.
