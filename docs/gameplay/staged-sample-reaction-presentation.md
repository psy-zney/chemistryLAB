# Staged sample and reaction presentation contract

## Physical interaction sequence

The desktop game no longer transfers a chemical directly from the first-person
hand into a reaction vessel.

1. Aim at a source bottle and press **E** to hold a measured sample.
2. Aim at the preparation tray beside the intended vessel and press **E**.
3. The hand becomes empty and a bottle appears physically on that tray.
4. Aim at the vessel and press **E** to load the staged sample.
5. The staged bottle disappears only after a successful, in-range transfer.

Pressing E on the preparation tray again while the hand is empty retrieves the
staged sample. Each workbench and fume-hood vessel owns an independent tray, so
a sample staged at one station cannot be loaded remotely into the other.

## Reaction presentation

When the newly loaded ingredient resolves to a reaction:

- the normal reaction audio, liquid colour, and particle effect begin;
- solution colour blends from the measured ingredients toward the resolved
  product colour; concentration changes opacity, while a precipitate makes the
  liquid visibly cloudy;
- gas outcomes show small bubbles rising inside the liquid and disappearing at
  its surface; precipitate outcomes show suspended particles settling into a
  coloured layer at the bottom of the vessel;
- hot aqueous contents (at least 90 °C, without a gas outcome) can show faint
  white thermal haze at the vessel mouth for the reaction duration;
- hazard severity alone does not make gas visible: the current outcomes do
  not carry reviewed gas colours, so colourless hazardous gas uses bubbles
  and the existing safety HUD instead of invented coloured smoke;
- the first-person arms are hidden temporarily;
- the camera eases to a 42-degree close view about 0.51 m from the physical vessel;
- a central card shows the balanced `ReactionOutcome.Equation`;
- the same card shows temperature, concentration, pH, rate, catalyst, and
  observed phenomenon from the outcome that drove the simulation;
- **Space**, **E**, or **Esc** skips the close view and restores the exact
  original local camera transform and field of view.

Reduced-motion mode keeps the equation card but does not move the camera.
It also changes liquid colour immediately and lowers VFX emission while keeping
gas, precipitate, and fume states visible. Cleaning the vessel clears all
particles and sediment. These visuals read the committed `ReactionOutcome` and
do not change reaction or safety calculations.

The solution mesh follows the inner envelope of the approved flask rather
than a cylinder. Fill height is computed by integrating that shape against
the existing outcome volume; a narrow static meniscus makes the surface
readable. Colour development uses the committed completion-time estimate
(minimum visual duration 0.75 seconds), and the final hue is the outcome hue.
Emission ramps up and tapers off. Bubbles vary from 1.5–4 mm and are removed at
the liquid surface; suspended particles stay in the solution and settle to a
persistent bottom layer whose hue comes from the same outcome. Each vessel
owns its mesh, material, effect timer and particle buffer. Cleanup destroys no
gameplay anchor and clears both vessels' visible state.

Pause freezes particle simulation, colour development, sediment growth and
camera easing through scaled time. Reduced motion lowers emission and travel
speed, shows the final colour/sediment immediately and has no flash or shake.
Enabling reduced motion during a close view restores the original camera
state. No fire/explosion is added: the current outcome enum does not authorize
such an effect.

## Runtime invariants

- A held sample cannot be added to any vessel.
- A staged sample cannot be loaded outside the normal station reach.
- A failed or remote load leaves the staged sample on its tray.
- No chemistry is evaluated merely by holding or placing a sample.
- The equation display does not maintain separate chemistry data; it renders
  the same outcome returned by `ReactionSimulator`.
- Product collection remains a separate action after the reaction.

The desktop smoke report verifies these constraints with
`samplePlacementFlowVerified`, `reactionEquationPresentationVerified`, and
`reactionCameraVerified`.

## Bounded reaction lifecycle (schema 2.0)

Each vessel commits at most one reaction between cleanups. Loading, heating and dilution share the same transition and apply safety, presentation and mission consequences once. After commitment, collect the primary product once, then use the sink before further additions or condition changes. Cleanup clears both vessels.

Recorded inputs remain visible after collection. The signed difference between recorded input mass and collected product is bookkeeping, not a complete solvent/byproduct conservation model. Sequential reactions in residual mixtures are not implemented. A staged inventory sample whose batch was depleted elsewhere is rejected without removing the tray sample or adding material to the vessel.

The extended smoke assertions are `staleBatchLoadBlockedVerified`, `conditionCommitVerified`, and `onceOnlyCollectionVerified`. Their presence in source is not a test result; inspect the current runtime smoke report.
