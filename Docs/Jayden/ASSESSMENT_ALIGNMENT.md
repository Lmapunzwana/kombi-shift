> Updated playable implementation: see [concept mapping](CONCEPT_IMPLEMENTATION.md), [current Unity guide](../../UnityStarter/README.md) and [validation](../../UnityStarter/Validation/RESULTS.md). The earlier design/preview notes below are historical and may describe superseded mechanics.

# Jayden — assessment alignment and gaps

Source reviewed: Game Development Final Project.pdf supplied by Jayden, 1 November 2026. This maps assessment requirements to proposed work; it is not proof that the game meets them. Team ownership follows the sprint plan, not assignments invented from the assessment brief.

## What the current pack supports

| Assessment area | Current artefact | Evidence still needed |
|---|---|---|
| Storyboard, prototype and traceability (8 marks across team) | Six draft frames, three mock screens, style guide | Annotated full journey, authentic early feedback, links to implemented scenes/screens |
| UI, art, animation and audio (9 marks across team) | Palette, geometry conventions, HUD/menu/statement studies | Unity materials and lighting, accessible working menus, animation transitions, audio feedback and volume controls |
| Report, source integrity and evidence (5 marks across team) | Initial attribution and AI-use logs | Final references, reviewed contributions, source/build/evidence package |
| Panel defence (10 marks across team) | Handoff and proposed conventions | Jayden explains and changes personally claimed work live |

Marks are shared rubric categories, not marks allocated to Jayden alone.

## Visual deliverables to add

- Instructions/onboarding: show actual controls, pickup/drop-off, fare consequences, fuel, remittance and completion rules once the team confirms them. Tadiwa implements.
- Pause/settings: resume, return and music/effects controls; show selected/focused states. Euclide implements.
- Level select: visually distinguish daytime city from rainy highway without relying on colour. Tendai implements.
- Hwindi selection: show boarding benefit and daily cost using actual economy values. Tendai implements.
- Win/game-over/restart: explain the outcome, offer replay and return; the statement alone does not prove a complete journey. Leroy implements.
- HUD feedback states: passenger patience, pickup progress, fare reaction, fuel warning, vehicle damage and drop-off payout. Agree layout with Tadiwa and Euclide.
- Animation plan: propose passenger Idle -> WalkToDoor -> Boarding -> Seated and disembark transitions. Confirm the smallest feasible set with Tadiwa; create real clips/controller transitions and record evidence. CSS mockup movement is not Unity animation evidence.
- Audio plan: engine/road ambience, boarding cue, fare payout and warning cue; record source, creator, licence, modifications and attribution. Euclide wires volume controls; avoid invented source licences.

## Annotated flow proposal

Main menu -> instructions -> level selection -> optional hwindi selection -> driving.
Driving -> pause -> resume OR settings -> pause; return to menu must handle an active shift consistently.
Driving -> shift statement -> completion/win OR game-over according to GameManager rules -> replay/level selection OR main menu.

This is a proposal for reconciliation with the foundation screen-flow document, not a replacement for it. Screen transitions, difficulty rules and monetary values require the relevant system owner to confirm.

## Early feedback procedure (not yet run)

Show the prototype to a real target player. Ask them to locate Start, explain the HUD, identify the current destination and interpret take-home. Record date, prototype version, participant ID, observations, severity and the resulting design change. Do not count this as the later playable-build test automatically. The team rubric requires at least three authentic testers and at least three evidence-based fixes with retest.

## Integration gates

1. Import the foundation and create a real look-development scene, materials and VolumeProfiles. Current HTML and documents are design artefacts only.
2. Hand off kombi, road modules, passenger placeholder and UI kit by 4 November noon under the sprint plan.
3. Test UI at 720p and 16:10; verify focus, text readability and cues that combine text/icon with colour. Record actual outcomes.
4. Capture real day/rain gameplay and animation evidence. Keep mockups labelled separately.
5. Coordinate measured performance checks and two distinct playable stages with the team.
6. Team must supply a PC build plus a tested Android/Web build where supported, or document an approved technical exception. Do not assume the HDRP platform issue is resolved.
7. Supply report visuals, final attribution/AI declaration and contribution evidence for the self-contained final ZIP. Deadline is 13 November 2026; panel date remains TBA in the brief.

## Defence preparation

Jayden should explain scale/pivots, material choices, lighting, accessibility and personally authored assets; demonstrate a small change such as material colour or light direction and explain its effect. Practise opening the actual assets and evidence. Do not claim generated material as reviewed or personally implemented before doing that review/work.

The brief calls the session 20 minutes, but its listed segments total 18 minutes (5 + 3 + 5 + 5). Keep the team rehearsal within the published 20-minute envelope and clarify the remaining allocation with the lecturer rather than inventing it.
