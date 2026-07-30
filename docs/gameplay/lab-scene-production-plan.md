# Lab Scene Production Plan

This document defines the target for the native Unity desktop lab scene. The
goal is a clear, detailed, physically readable room where chemistry happens only
inside placed lab vessels, not in the player's hand.

## Core Rule

The player's hand is a transport state only.

- Picking a bottle selects a measured sample.
- The held sample can be inspected or put away.
- No reaction is evaluated while the sample is only in hand.
- A reaction can start only after the player interacts with a vessel station:
  the workbench vessel, fume hood vessel, or a future explicitly placed tube.
- Heating, dilution, collection, and gas handling must require the player to be
  within reach of the physical station.

The current runtime enforces this with vessel reach checks before adding
chemicals, collecting products, heating, or diluting. Smoke tests also verify
that holding a sample and trying to operate a distant vessel does not modify the
vessel contents.

## Target Scene Hierarchy

```text
DesktopChemistryLab
|-- Environment
|   |-- LabRoom
|   |-- Floor
|   |-- Walls
|   |-- Ceiling
|   |-- DoorsWindows
|   `-- StaticProps
|-- LabFurniture
|   |-- CentralWorkbench
|   |-- ChemicalCabinets
|   |-- FumeHood
|   |-- SinkStation
|   |-- AnalysisBench
|   `-- SafetyStation
|-- Vessels
|   |-- WorkbenchVesselStation
|   |-- FumeHoodVesselStation
|   `-- TubeRackSlots
|-- Player
|   |-- FirstPersonChemist
|   |-- ChemistCamera
|   `-- PovHands
|-- UI
|   |-- DesktopHudCanvas
|   |-- MainMenu
|   |-- PauseMenu
|   `-- SettingsMenu
|-- Lighting
|   |-- BakedAreaLights
|   |-- KeyDirectionalLight
|   `-- ReflectionProbes
|-- RuntimeSpawns
|   |-- ChemicalBottles
|   |-- SynthesizedProducts
|   `-- ReactionEffects
`-- GameSystems
    |-- DesktopLabGame
    |-- DesktopLabAudio
    `-- RuntimeDiagnostics
```

## Scale Contract

Use Unity units as meters.

| Object | Target Size |
| --- | --- |
| Player height | 1.75-1.82 m |
| Camera eye height | 1.60-1.65 m |
| Workbench height | 0.90-1.05 m |
| Workbench depth | 0.75-1.10 m |
| Fume hood work surface | 0.90-1.05 m |
| Beaker height | 0.11-0.18 m |
| Test tube height | 0.12-0.18 m |
| Chemical bottle height | 0.18-0.32 m |
| Door height | 2.05-2.20 m |
| Ceiling height | 3.0-3.6 m |

## Physics Contract

- Use simple colliders for navigation: box colliders for floor, walls, bench,
  cabinets, hood, sink, and counters.
- Use capsule or box trigger colliders for interactable bottles and vessels.
- Avoid mesh colliders on small glassware unless a simplified convex collider is
  supplied.
- Player movement uses `CharacterController`; do not add rigidbody physics to
  the player.
- Small props may be decorative only. Only vessels, bottles, safety equipment,
  sink, hotplate, and analysis table need gameplay colliders.
- Vessel slots must be anchored to a table or hood surface. They should not be
  parented under the hand rig.

## Interaction Contract

| Interaction | Valid Location | Invalid Location |
| --- | --- | --- |
| Pick chemical | Chemical shelf/tray | Empty air |
| Hold sample | Player hand | Reaction engine |
| Add chemical | Vessel on bench or fume hood | Hand, floor, remote station |
| Heat/cool | Hotplate or vessel station within reach | Remote station |
| Dilute | Vessel station within reach | Remote station |
| Collect product | Vessel station within reach | Hand before reaction |
| Toxic gas control | Fume hood + gas trap | Open bench without controls |

## Model Types To Find

Prefer `FBX` or `GLB`, low/mid-poly, real-world scale, PBR materials, and clear
license. `CC0` is best. `CC-BY` is acceptable only if attribution is recorded.

| Priority | Model Type | Notes |
| --- | --- | --- |
| 1 | Modular lab room kit | Floor, walls, ceiling, windows, trim. Clean scale is more important than detail. |
| 1 | Workbench / lab island | Needs simple box collider and flat placement surface. |
| 1 | Fume hood | Must have visible glass sash, interior work surface, exhaust impression. |
| 1 | Beaker / reaction vessel set | Beaker, test tube, Erlenmeyer flask, graduated cylinder. |
| 1 | Chemical bottle set | Brown glass, clear glass, plastic reagent bottle, caps, labels. |
| 2 | Tube rack with slots | Needs stable slots so tubes sit on the table. |
| 2 | Hotplate / stirrer | Needs an interaction trigger and visible heat surface. |
| 2 | Sink / eyewash station | Used for washing and safety logic. |
| 2 | Safety props | Respirator, gas trap, gloves, goggles, hazard cabinet. |
| 2 | Storage cabinets / shelves | Enough repeated shelf space for chemical bottles. |
| 3 | Microscope / analysis devices | Decorative plus analysis interaction point. |
| 3 | Warning decals/signage | Corrosive, toxic, flammable, oxidizer, PPE required. |
| 3 | Small clutter | Papers, trays, clamps, stands, tubing. Use sparingly. |

## User Asset Tasks

1. Find model sources with explicit license text.
2. Prefer one coherent lab/furniture pack over many mismatched packs.
3. Record source URL, author, license, and required attribution.
4. Avoid assets that require paid plugins or custom shaders.
5. Keep texture sets under control: 1K or 2K is enough for most props.
6. Import one pack at a time and verify scale before adding the next pack.
7. Do not replace gameplay anchors with decorative models. Place models under
   anchors so code still knows where the vessel/station is.

## Implementation Order

1. Keep the current chemistry engine and safety system.
2. Convert static room/furniture from procedural primitives to scene objects.
3. Convert HUD/menu from runtime-created objects to prefabs.
4. Keep bottles and synthesized products runtime-spawned from chemistry data.
5. Add authored vessel slots on the bench and in the fume hood.
6. Bind interaction scripts to real colliders and anchors.
7. Replace placeholder geometry with vetted free models.
8. Bake lighting and keep realtime lights limited.
9. Run validation, smoke test, Windows x64 build, package validation, and SHA256.

