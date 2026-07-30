# Documentation

The repository root is the canonical Unity project. Start with the main
[`README`](../README.md) for setup, controls, architecture, and build commands.

## Architecture

- [`architecture/oop-data-algorithms.html`](architecture/oop-data-algorithms.html):
  visual explanation of the runtime object model, data structures, algorithms,
  and main execution flows.

## Chemistry

- [`chemistry/compound-generation-matrix.md`](chemistry/compound-generation-matrix.md):
  X/Y/Z compound model, charge balancing, physical-property estimation, and
  validation.
- [`chemistry/dynamic-reaction-engine.md`](chemistry/dynamic-reaction-engine.md):
  dynamic reaction resolution and safety classification.
- [`chemistry/reaction-condition-engine.md`](chemistry/reaction-condition-engine.md):
  temperature, concentration, pH, catalyst, redox, and reusable products.
- [`chemistry/compound-matrix-3d.html`](chemistry/compound-matrix-3d.html):
  interactive Three.js explorer for the generated compound space.

## Gameplay

- [`gameplay/player-guide-vi-en.md`](gameplay/player-guide-vi-en.md):
  bilingual player workflow and complete controls.
- [`gameplay/lab-scene-production-plan.md`](gameplay/lab-scene-production-plan.md):
  scene hierarchy, scale, physics, and interaction contracts.
- [`gameplay/staged-sample-reaction-presentation.md`](gameplay/staged-sample-reaction-presentation.md):
  sample staging and reaction close-view contract.
- [`gameplay/model-asset-review-2026-07-30.md`](gameplay/model-asset-review-2026-07-30.md):
  approved model decisions and integration requirements.
- [`gameplay/procedural-reference-props.md`](gameplay/procedural-reference-props.md):
  original procedural replacements and collision rules.

## Product Design

- [`design/economy-and-progression.md`](design/economy-and-progression.md):
  future economy, quest, unlock, and progression roadmap.
- [`design/inorganic-synthesis-tree.md`](design/inorganic-synthesis-tree.md):
  starter materials and inorganic synthesis progression.

## Release

- [`release/windows-portable-layout.md`](release/windows-portable-layout.md):
  Windows package layout and release verification.

Machine-readable companions use the same basename with a `.json` extension.
Current validation and approved-model integration reports live in
[`BuildReports`](../BuildReports/). Build and smoke reports are generated for
release checkpoints and should only be committed while they match the current
source state.
