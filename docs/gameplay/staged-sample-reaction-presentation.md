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
- the first-person arms are hidden temporarily;
- the camera eases to a 42-degree close view of the physical vessel;
- a central card shows the balanced `ReactionOutcome.Equation`;
- the same card shows temperature, concentration, pH, rate, catalyst, and
  observed phenomenon from the outcome that drove the simulation;
- **Space**, **E**, or **Esc** skips the close view and restores the exact
  original local camera transform and field of view.

Reduced-motion mode keeps the equation card but does not move the camera.

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
