# Kombi Shift Asset Intake

This document defines how external assets enter the project. Models should not be copied directly into gameplay scenes before they pass the checks below.

## Folder structure

```text
Assets/KombiShift2/ThirdParty/<PackName>/   Original imported pack
Assets/KombiShift2/Art/Vehicles/            Prepared vehicle prefabs
Assets/KombiShift2/Art/Characters/          Prepared passenger and pedestrian prefabs
Assets/KombiShift2/Art/Roads/               Prepared road, pothole and street prefabs
Assets/KombiShift2/Prefabs/Gameplay/        Final gameplay-ready prefabs
```

Keep third-party source files unchanged. Put corrected scale, materials, colliders, rigs and scripts on our own prefabs outside the third-party folder.

## Acceptance checks

### Vehicles

- Licence and source recorded in `ASSET_REGISTER.csv`.
- Approximately real-world scale in metres.
- Forward axis documented and corrected on the wrapper prefab.
- Body, glass and wheel meshes identified.
- Four wheels separated or separable.
- Polygon count suitable for real-time use.
- Materials render correctly in the active Unity pipeline.
- Simple body collider used; visible mesh is not used as a moving MeshCollider.
- WheelCollider positions match wheel centres and radii.
- Camera anchors and passenger capacity defined.

### People

- Licence and source recorded.
- Humanoid rig preferred.
- Idle, walk and waiting animations available or retargetable.
- Materials and skin tones can be varied.
- LOD or low-poly suitability checked.
- Passenger and pedestrian variants created without modifying source files.

### Roads and props

- Modular scale and connection dimensions documented.
- Road surface has a simple, continuous collider.
- Decorative pavement and gameplay road collision remain separate.
- Traffic lights expose separate red, amber and green renderers.
- Potholes use low-complexity collision and a separate trigger for gameplay effects.

## Initial integration order

1. Rebuild the existing city road surfaces with the Poly Haven PBR asphalt set.
2. Prepare one straight road, one curve and one intersection at real-world scale.
3. Create one realistic traffic-light prefab and signal controller.
4. Import the approved 2005 Hiace through the provider's download interface.
5. Build and validate its reusable WheelCollider vehicle rig.
6. Import the approved realistic traffic-car and pedestrian assets.
7. Place the physical pothole, speed-bump and roadworks prefabs.
8. Assemble and test one complete route through the existing city.

## Preparation tools

- `Kombi Shift > Assets > Build Road Asset Foundation` configures the permanent
  Poly Haven texture maps and rebuilds the worn-asphalt material, straight-road
  prefab and physical pothole prefab.
- Select an imported vehicle model and run
  `Kombi Shift > Assets > Prepare Selected Vehicle Model`. The tool creates a
  non-destructive gameplay wrapper with a rigidbody, body collider, four
  WheelColliders, two configured axles and three camera anchors.
- Wheel positions are initially estimated from model bounds. Verify scale,
  forward direction, wheel centres and wheel radius before treating the prefab
  as gameplay-ready. Assign the model's separate wheel meshes as axle visuals.

## Downloads requiring a teammate

Sketchfab and Unity Asset Store downloads may require a free account and cannot
be redistributed outside their asset licences. Use the signed-in provider page,
retain the original licence information, and record the exact model, creator and
licence before preparing a gameplay prefab. Do not commit provider credentials,
unclear downloads or marketplace source files that the licence does not allow us
to redistribute through the repository.
