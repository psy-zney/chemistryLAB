# Chemistry Lab 3D Agent Guide

This file is the persistent operating context for coding agents. Repository
instructions override generic skill advice.

## Project Identity

- The repository root is the only canonical Unity project.
- Engine: Unity `6000.5.3f1`, C#, Windows desktop, first-person 3D.
- Production code and assets live under `Assets/ChemistryLab`.
- The product is an educational chemistry simulation, not real laboratory
  operating or safety guidance.
- Do not restore removed web, mobile, prototype, planning, cache, or duplicate
  Unity projects unless the user explicitly asks for them.

## Read First

1. Read `README.md` for the current feature set, controls, architecture, and
   build workflow.
2. Read `docs/README.md` as the documentation index.
3. Read only the task-relevant design document:
   - Chemistry: `docs/chemistry/`
   - Gameplay and scene contracts: `docs/gameplay/`
   - Economy and progression: `docs/design/`
   - Packaging: `docs/release/`
4. Use the JSON companion beside a Markdown document when code needs
   machine-readable requirements. Do not load every JSON file by default.

## Non-Negotiable Product Contracts

- Physical interaction is staged. A held chemical must be placed on the
  preparation tray before loading a vessel. No remote loading or reaction in
  the player's hand.
- Unsafe reactions may occur, but the safety system must apply understandable
  health, credit, equipment, ventilation, or evacuation consequences.
- Curated chemistry remains authoritative. Dynamic generation may extend it,
  but must not silently override reviewed equations or property records.
- Chemical formulae and equations remain universal notation. Translate UI,
  prompts, observations, and guidance, not formula symbols.
- Vietnamese and English UI state persists through `PlayerPrefs` and updates
  menus, HUD, prompts, inspectors, and reaction presentation consistently.
- Reduced motion must remain a functional alternative, especially for camera
  motion, reaction close views, flashes, and other strong feedback.

## Runtime Map

- `Runtime/Bootstrap/DesktopLabGame.cs`: composition root, selected chemical,
  vessel state, room construction, and subsystem coordination.
- `Runtime/Player/LabInteractions.cs`: first-person interaction and physical
  object prompts.
- `Runtime/UI/DesktopLabHud.cs`: HUD plus main, pause, and settings menus.
- `Runtime/Core/LabLocalization.cs`: language preference and shared strings.
- `Runtime/Chemistry/`: curated data, compound matrix, redox, dynamic reaction,
  conditions, and synthesized inventory.
- `Runtime/Safety/`: hazard classification and player consequences.
- `Runtime/Audio/`: procedural music, ambience, SFX, and audio preferences.
- `Editor/BuildPipeline/`: validation, Windows build, and release packaging.
- `Resources/Chemistry/`: runtime chemistry JSON and its Unity `.meta` file.

The reaction resolution order is curated reactions, reviewed redox rules, then
bounded dynamic rules. The condition engine then decides whether the matched
reaction can run and computes rate, yield, and blocked outcomes.

## Engineering Rules

- Inspect `git status` before editing. Preserve unrelated user changes.
- Keep changes scoped and follow existing MonoBehaviour/plain-C# boundaries.
- Prefer data-driven chemistry rules over one-off reaction branches.
- Preserve assembly definition boundaries and every required Unity `.meta`
  pairing when moving, adding, or deleting assets.
- Do not add packages without a concrete runtime or editor requirement.
- Do not commit `Library`, `Temp`, `Logs`, `obj`, local builds, editor logs,
  generated scratch images, or transient validation output.
- Keep only current, structured release evidence in `BuildReports/`.
- Keep `SourceAssets/` for licensed source geometry and provenance; runtime-ready
  assets belong under `Assets/ChemistryLab`.
- Update the relevant Markdown and JSON companion together when a documented
  contract changes.

## UI, Gameplay, Level, And Audio Quality

- HUD and menus must remain readable without overlap at common desktop aspect
  ratios, including 16:9, 16:10, and ultrawide.
- Use anchored layouts and stable dimensions. Long Vietnamese and English
  strings must wrap or resize within their controls.
- Keyboard and mouse are the primary controls. Menu back, focus, pause, and
  settings return paths must be predictable.
- Gameplay feedback must communicate cause and result without changing the
  underlying chemistry simulation or blocking input.
- Build and test the laboratory layout at player scale. Preserve clear
  sightlines, walkable corridors, recognizable stations, and the staged sample
  workflow before adding decoration.
- Route sound by purpose and preserve the global audio preference. Repeated SFX
  should avoid harsh repetition; music must not hide safety alarms or reaction
  feedback.

## Installed Skill Routing

Use the minimum applicable global skill and read its `SKILL.md` before applying
it:

- `game-developer`: Unity architecture, physics, profiling, and core gameplay
  implementation.
- `game-feel`: interaction feedback, camera response, easing, and polish.
- `game-ui-ux`: HUD/menu flow, scaling, accessibility, and input navigation.
- `level-design`: laboratory blockout, scale, sightlines, pacing, and guidance.
- `audio-design`: mixer structure, SFX variation, ducking, and adaptive music.

Skill guidance is supplemental. Do not force ECS, pooling, middleware, or a
large abstraction into this small project without measured need and consistency
with the existing codebase.

## Validation

- Run focused static checks first: JSON parsing, Markdown links, `.meta` pairing,
  package consistency, and `git diff --check`.
- For Unity validation, use
  `ChemistryLab.Desktop.Editor.DesktopLabBuild.ValidateOnly` or the matching
  editor menu command.
- Unity batch mode can recreate a large `Library/` and may stall when Unity Hub
  licensing is unavailable. Check the log for licensing/package-manager loops
  before waiting or retrying; do not report those as C# compile failures.
- Do not open Unity merely to validate documentation-only changes.
- Report exactly which checks ran, which did not run, and why.
