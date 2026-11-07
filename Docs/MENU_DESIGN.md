# Menu and camera redesign

The title screen is now a live Unity scene featuring the same Hiace model used in play. It uses a restrained rank/workshop setting, moving showcase camera, condensed display typography, three animated selection cards, original short UI sounds, controls/settings panels and a start transition. It does not use an AI image or a borrowed game screenshot.

## Online visual references

- Need for Speed Heat menu screenshots: https://www.wsgf.org/dr/need-speed-heat/en — vehicle as the hero, low navigation cards, strong selected state.
- Forza Horizon 5 title-screen image research informed the vehicle-led, moving-camera direction. No Forza artwork, branding or audio was imported.
- Barlow Condensed: https://github.com/google/fonts/tree/main/ofl/barlowcondensed — Copyright 2017 The Barlow Project Authors, SIL Open Font License 1.1. The font files and OFL are in Assets/KombiShift2/Resources/Fonts.

All composition, interface code, animations, stage geometry and UI sound cues for this menu were implemented locally. The existing vehicle retains its original asset licence requirements.

## Camera fault and correction

The civilian character FBX imported a camera named bottom. Two passenger instances introduced two such cameras into the city; these could render over the driving camera and display the underside of the world. Character camera/light import is now disabled at the model importer. Unused SketchUp cameras attached to city models have also been removed from the gameplay scene. The driving camera also owns the game view and disables unrelated output cameras while preserving render-texture cameras.

The chase camera uses vehicle heading on the horizontal plane, world up, ground clearance and obstacle checks. It does not inherit vehicle roll or pitch. The current vehicle uses upright arcade stabilization, a lower centre of mass and continuous collision detection. Painted centre-line meshes no longer have physical colliders. Shelter pieces crossing the chase view are temporarily hidden and restored as the view clears.

## Controls

- Hold right mouse and drag: orbit in chase view or look around in driver view.
- Mouse wheel: zoom chase view.
- C or the on-screen camera button: switch chase/driver view.
- Settings in the main menu: mouse sensitivity and chase distance.
- Arrow keys/Tab and Enter also navigate/select menu cards.

## Evidence

See Docs/Validation/MenuCamera for menu and camera screenshots and the menu-camera-results.json report. The camera/drive checks use a dynamic Rigidbody and the actual driving input path, rather than holding the vehicle kinematic. These tests isolate the camera from background traffic and do not replace a human handling playtest.
