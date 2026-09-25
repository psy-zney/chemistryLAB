# Redesign continuation — 25 September 2026

This is a continuation explicitly requested by the user, not a second scheduled run.
The historical handoff is [AGY-3.8-redesign.md](AGY-3.8-redesign.md).

## Verified environment

- Canonical repository: `C:/Users/admin/MyProject/chemistryLAB`, HEAD `d15a246`.
- No `.codegraph` directory. Read AGENTS, root README and docs index first.
- Repository write probe succeeded. Current execution permissions allow writes;
  the failed scheduled session's permission blocker no longer applies.
- Existing user changes at entry: `ProjectSettings/ProjectSettings.asset`,
  `BuildReports/desktop-validation-report.json`, this directory's historical
  handoff, and `scripts/export-matrix.mjs`. Preserve them. Pre-run tracked file
  snapshots are in ignored `Logs/`.
- `agy models` succeeds and lists `gemini-3.8-flash-high`. Use that exact model
  with `--effort high`; never substitute another model silently.
- GitHub account authentication works; repository API grants push/admin rights.
  Pages source is `workflow`; current public root is
  <https://zney295.id.vn/chemistryLAB/> (HTTP 200, redirects to 3D documentation).
  Last inspected Pages run `30922021293` succeeded for the existing source.
- No GPT account quota tool is exposed in this session. Prior run percentages
  are historical and must not be reported as current remaining quota.

## Baseline evidence

- Static audit: 14 tracked JSON parse; 67 non-meta tracked assets have metadata;
  no orphan tracked meta; 36 direct packages match lock; 32 relative Markdown
  file links resolve; `git diff --check` passes.
- Unity `6000.5.3f1` `DesktopLabBuild.ValidateOnly` completed, exit marker 0,
  `DESKTOP_LAB_DATA_PASS` with 52 elements, 40 chemicals, 38 reactions,
  565 generated coordinates, 541 unique formulas and 45 reviewed records.
  Fresh report timestamp: `2026-09-25T08:03:05.4399930Z`.
  Ignored log: `Logs/resume-baseline.log`. This is data/editor validation,
  not a Windows build, mobile build, visual test, or scientific certification.
- Installed PlaybackEngines include Windows, Android, iOS and WebGL. SDK,
  signing and real-device support remain separately unverified.
- The existing untracked exporter requires `source.matrix2D`, which is absent.
  Do not call this script complete or wire it into deployment until the
  canonical contract and parity tests exist.

## Collaboration record

- An explicitly requested `gpt-6-astra`, reasoning `high`, runtime review
  inspected the staged workflow, inventory, simulator and player input.
- AGY invocation without permission auto-approval was denied its command tool.
  Authorized retry used `--dangerously-skip-permissions` in read-only plan mode.
- AGY conversation `17854f19-f47f-4931-a870-144ad09e8154` read project context
  but reached the configured print timeout without a final response. A focused
  continuation asks it to critique the findings below without further tools.
  Model listing/tool activity alone is not a completed design review.

## Astra findings for joint review

1. `DesktopLabGame.AddSelectedToVessel` treats a nonempty staged batch ID whose
   lookup fails as unlimited catalogue material. Two trays referencing one
   depleted batch can bypass inventory withdrawal. Reject stale batch IDs and
   require successful transactional withdrawal before committing a load.
2. Heating/dilution call `RefreshOutcome`, which updates prediction/UI, while
   safety, mission and presentation occur only in `AddSelectedToVessel`.
   Blocked-to-reactive transitions need the same once-only consequence path.
3. Prediction reads original additions repeatedly; repeated additions can
   repeat consequences, while collection clears surplus and byproducts after
   storing only a primary product. Separate prediction, commitment, products,
   residuals and cleanup; expose approximations rather than imply mass closure.
4. `FirstPersonChemistController.Update` runs equipment/sample hotkeys before
   the pause guard. Gate gameplay actions behind menu/input ownership.
5. `ReactionSimulator.Evaluate` labels unmatched chemistry as no driving force.
   Absence of a supported rule is not evidence of no reaction. Distinguish
   unsupported, reviewed no-net-reaction and conditions-blocked outcomes.

## Design constraints

Rows are stable `anionId`, columns stable `cationId`; preserve legacy
`cationId|anionId` keys through named conversion. Balance by GCD. Keep formal
composition, property provenance, and reaction feasibility separate. Ion
charges/ratios must never authorize synthesis by themselves. Keep explicit
exclusions, reviewed overrides and separate element/oxidation oxide records.
No fabricated equilibrium constants or reaction branches.

Preserve tray-before-vessel workflow, independent stations, curated then redox
then bounded dynamic resolution, VI/EN persistence, reduced motion and safety
consequences. Implement mobile in the canonical Unity project with shared
actions, pointer ownership and safe area; Pages remains documentation.

## Next implementation and acceptance sequence

1. Obtain actual AGY critique and record decisions/ownership before edits.
2. Fix stale batch and paused input; reproduce two-tray depletion and paused
   hotkey cases in runtime smoke checks.
3. Add per-vessel committed outcome/residual state and route load/conditions
   through one transition; verify no repeated consequences or collection.
4. Complete 2D schema, runtime adapter and exporter. Validate unique IDs,
   neutral coprime ratios, exclusions, reviewed precedence and export parity.
5. Implement contextual UI, shared desktop/touch controls and guided loop;
   test both languages, reduced motion and common aspect ratios.
6. Update Markdown/JSON companions and build the 2D Pages table with real
   IDs, ratios, condition/evidence references, diagrams and examples.
7. Add CI static/schema/parity checks before Pages artifact upload; run Unity
   validation, Windows build and smoke. Report device checks independently.
8. AGY reviews actual diff and results; resolve findings. Publish only tested
   source, verify matching Actions commit and live site/data paths.

The historical handoff contains full command lines and broader acceptance
criteria. This continuation records newer evidence without rewriting history.
