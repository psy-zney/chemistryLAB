# Original procedural props derived from functional references

## Policy

The unverified downloaded models are not imported, copied, traced, or shipped.
They were used only to identify the type of laboratory equipment that the room
needed. All replacement geometry is built from Unity primitives by
`ProceduralLabPropFactory`.

The committed replacements have original topology, proportions, materials, and
assembly. They contain no third-party mesh, texture, rig, animation, camera,
light, script, or shader.

## Implemented replacements

| Original reference category | Committed replacement | Runtime role |
| --- | --- | --- |
| Bench hotplate/stirrer | Original 0.44 × 0.16 × 0.36 m hotplate with ceramic deck, control fascia, two dials, display, indicator, feet, and a box collider | `ThermalControlInteractable` for the workbench vessel |
| Hazmat/PPE suit | Original yellow protective suit display with locker, hanger, hood, visor, respirator, sleeves, legs, boots, and a cabinet box collider | Physical `RespiratorStationInteractable`: aim and press E to buy, wear, or remove the respirator |
| Chemistry bottle collection | Original five-bottle amber/clear reagent rack with tray, rail, uprights, shoulders, necks, caps, and labels | Decorative analysis-bench prop |
| Laboratory gas apparatus | Original two-stage gas wash train with simple glass vessels, dip tubes, interconnect, intake, and scrubber liquid | Physical `GasTrapInteractable`: aim and press E to connect or disconnect gas isolation |

## Scene-scale correction

The same revision corrects the largest placeholder scale problems:

- room ceiling: 3.6 m;
- workbench surface: approximately 1.01 m;
- fume-hood work surface: approximately 1.01 m;
- interactive reagent bottle: approximately 0.313 m including cap;
- Erlenmeyer reaction vessel: 0.18 m;
- test tube: 0.15 m.

## Gameplay and collision contract

- The hotplate owns one box collider and keeps the existing thermal interaction.
- The PPE display owns one cabinet collider; aiming at it exposes the live PPE
  purchase/equip prompt and briefly enables its focus outline.
- The gas-wash train owns one root collider; aiming at it exposes the live
  connect/disconnect prompt and briefly enables its focus marker.
- F6 and F7 remain available as accessibility shortcuts; walking to the
  physical PPE and gas-wash stations and pressing E is the primary interaction.
- Decorative reagent-rack parts and the child geometry of both safety props
  have no colliders.
- Chemistry remains valid only in the workbench or fume-hood vessel.
- Holding a sample never calls the reaction engine.
- Heating, dilution, collection, and vessel loading still require station reach.

The desktop smoke report records `originalReferenceProps: 4`, verifies both
physical safety stations and their colliders, and continues to verify
`handOnlyReactionBlocked` and `remoteVesselOperationBlocked`.

The four functional reference props are retained. The original Blender kit in
`SourceAssets/Original/ModernLab` adds authored visual children for furniture,
architecture, preparation trays, reagent bottles and bench tools. It does not
replace the safety/hotplate gameplay anchors or copy unverified source meshes.
Held, staged and shelf bottles share the metre-scale bottle mesh; dynamic
chemical contents and formula labels are attached beneath an identity Unity
root, outside the FBX axis-conversion child. Decorative meshes have no colliders.
