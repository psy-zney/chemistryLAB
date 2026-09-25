# AGY 3.8 high — implementation and validation handoff

## Trigger and exact status

The explicitly requested Astra high matrix agent reported a usage limit at
approximately 15:20 Asia/Ho_Chi_Minh on 2026-09-25: try again at 19:28. No quota
percentage tool is exposed to the parent. This is a real limit notification,
not a measured current remaining percentage. User instructions require this
handoff to take priority now. Continue with **gemini-3.8-flash-high**, effort
**high**; do not substitute another model. AGY conversation:
`17854f19-f47f-4931-a870-144ad09e8154`.

The original request authorizes noninteractive implementation, tests, GitHub
Pages updates and publication when permissions exist. Repo/GitHub access now
works. Do not create another schedule. Existing single scheduled run is over;
this is a user-requested continuation.

Read AGENTS.md, README.md, docs/README.md,
[resume ledger](redesign-resume-2026-09-25.md), and
[historical full design](AGY-3.8-redesign.md). No `.codegraph` exists.

## Preserve existing changes

Entry HEAD `d15a246`, branch main. Entry user modifications:
`ProjectSettings/ProjectSettings.asset` (+97 Unity serialized settings lines),
`BuildReports/desktop-validation-report.json`, untracked historical handoff,
and `scripts/export-matrix.mjs`. Do not discard them. Snapshots of the tracked
files exist in ignored `Logs/*before-resume*`. Keep ProjectSettings out of any
commit unless your deliberate platform change requires it and is documented.
The exporter is unfinished user work intentionally continued, not disposable.
All other task changes are current collaborative implementation. No commit,
push or new deployment yet.

## Real collaboration so far

AGY completed a design critique and independently agreed with Astra findings:
stale staged batch duplication, condition-change safety bypass, paused input,
reaction replay/residue loss, and unknown chemistry mislabeled as inert.
AGY implemented player/HUD/touch code in the same conversation. Astra agents
implemented lifecycle and 2D data/API. Parent implemented exporter integration,
Pages UI, static validation, CI and parity export. The runtime agent finished;
the matrix agent hit the limit during final documentation/audit, so inspect
its actual working files. **No joint compilation has run yet.**

## File map and current behavior

### Runtime lifecycle (Astra; finished edits)

- `Runtime/Bootstrap/DesktopLabGame.cs`: stale nonempty batch ID rejection;
  one committed reaction per vessel; common consequences for load/heat/dilute;
  once-only collection; original additions retained as historical input;
  loading/condition changes require cleanup after commitment; guided prompts.
- `Runtime/Chemistry/VesselReactionLifecycle.cs` + meta: Preview versus Advance,
  committed outcome, collection flag, reset, focused validation.
- `Runtime/Chemistry/ChemistryData.cs`: public ReactionOutcome fields
  `ReactionCommitted`, `ProductCollected`, `RecordedInputGrams`,
  `CollectedProductGrams`, `UnallocatedInputGrams`; NoMatch explanation means
  unsupported, not proof of no reaction.
- `Runtime/Chemistry/SynthesizedInventory.cs`: new validation hooked into
  existing ValidateOrThrow path.
- Bootstrap smoke additions: `staleBatchLoadBlockedVerified`,
  `conditionCommitVerified`, `onceOnlyCollectionVerified`. They use a temporary
  inventory and isolated safety model, restore originals. Test two-tray batch
  exhaustion, cold peroxide/MnO2 blocked then heat to commitment, no replay on
  heat/dilute, one collection, retained inputs and cleanup.

**Essential integration still needed:** HUD must render committed/collected
state correctly. Label additions as recorded inputs after commitment; show
cleanup required. Display signed input-minus-collected bookkeeping honestly;
do not present it as chemically complete residual composition. No identified
solvent/byproduct mass closure or sequential mixture chemistry is implemented.

### 2D matrix (Astra; final audit interrupted)

- Runtime JSON `Resources/Chemistry/compound-generation-matrix.json` now has
  `matrix2D` with anionId rows, cationId columns, condition/evidence registries,
  default formalComposition status, annotations, scoped literature examples.
- `CompoundMatrixDataContracts.cs`, `CompoundGenerationModels.cs`,
  `CompoundGenerationMatrix.cs`, new `CompoundMatrix2D.cs` + meta.
- API `CompoundGenerationMatrix.Cells`, `TryGetCell(anionId,cationId,out cell)`,
  `Conditions`, `Evidence`, `ToLegacyCoordinate(cationId,anionId)`.
- Cell properties: AnionId,CationId,Coordinate,Formula (Unicode/null excluded),
  FormulaAscii,CationCount,AnionCount,Status,ConditionIds,EvidenceIds,Notes,
  ExceptionReason,PropertyReviewStatus,AuthorizesReaction(false).
- Status strings formalComposition/literatureSupported/excluded/unsupported.
  Four narrowly sourced examples: sodium|chloride, barium|sulfate,
  hydrogen|hydroxide, hydrogen|acetate. Others are not blanket-certified.
- 525 ionic cells = 21 anions × 25 cations; 8 ionic exclusions, plus separate
  oxide registry/exclusion. Legacy 565 coordinates/541 formula output retained.
- Ion AtomCounts derived from formula; override atom-count checks implemented
  but not yet run in Unity. Schema parser/validation may need compile fixes.
- `docs/chemistry/compound-generation-matrix.json` changed; Markdown companion
  may still be old and must be brought into agreement.
- Challenge to AGY initial wording: **do not call the entire grid aqueous**.
  Oxide/formal acid entries are not freely existing aqueous ions. An oxide
  registry also contains metal oxides, not all covalent species.

### Player/HUD (AGY; implemented, uncompiled)

- `Runtime/Player/LabInteractions.cs`: dispatch methods, paused keyboard gate,
  Alt cursor release, touch move/look, pointer reset.
- `Runtime/UI/DesktopLabHud.cs`: touch controls, inspector action bar/close,
  bilingual refresh, menu isolation, safe-area integration.
- New `LabTouchZone.cs` + meta and `LabSafeAreaHandler.cs` + meta.
- Review dispatch methods: current DispatchAmount/Temperature/etc only check
  game!=null. They also need appropriate pause/cinematic guards for pointer
  actions, not just keyboard gate. DispatchInteract must recheck current
  physical focus/distance and preserve staging.
- Ensure touch control visibility/ordering doesn't overlay reaction skip,
  inspector fields, safe area or menu; all gameplay controls available while
  touch pads disabled for modal. Initial helper lookup must not allocate every
  frame. Preserve MenuButtonCount=11, PauseButtonCount=3 contracts.
- Desktop inspector action buttons need cursor release without world movement
  or accidental background input. Long VI/EN strings must fit.

### Pages, export and CI (parent; static passes)

- Existing `scripts/export-matrix.mjs` completed with propertyReviewStatus and
  authorizesReaction=false; exports canonical data SHA256, 525 cells.
- New `scripts/validate-project.mjs`: JSON, assets/folder meta, package lock,
  relative Markdown links, schema IDs/charges/reduced ratios/references,
  exclusions, reviewed override precedence, key formulas, export equality.
  Optional `--unity-parity Logs/matrix-unity-parity.json` compares actual C#.
- New `Editor/BuildPipeline/MatrixParityExport.cs` + meta writes actual C# cell
  projection to ignored Logs/matrix-unity-parity.json. Public `Export` entry.
- New docs `compound-matrix-2d.html/.css/.js/.json`: responsive dark table,
  anion/cation selectors, formula search, keyboard arrows/Home/End, selected
  cell ratio/evidence/condition/property detail, VI/EN preference, scoped
  chemistry caveats, error fallback, real source hash. Browser not tested yet.
- Workflow deploy-docs-pages.yml now validates before assembly, adds PR checks
  and source/script/package trigger paths, redirects root to 2D, prevents PR
  deployment. Existing action versions unchanged. Check Node availability or
  pin setup-node with verified action version if necessary.
- docs/README index updated. Root README remains old 3D-primary description,
  controls/gameplay docs and JSON companions need updating after tests.

## Validation already completed

- BEFORE edits: Unity 6000.5.3f1 ValidateOnly success marker and exit 0, 52
  elements/40 chemicals/38 reactions/565 coordinates/541 formulas/45 overrides.
  Report timestamp 2026-09-25T08:03:05.4399930Z. Log resume-baseline.log.
- AFTER partial edits: `node scripts/export-matrix.mjs`,
  `node scripts/validate-project.mjs`, `node --check` on Pages JS,
  `git diff --check` passed at approximately 15:16. At that moment 15 JSON,
  72 non-meta assets, 34 relative links, 36 dependencies, 525 cells/8 exclusions.
- No new Unity compilation/build, runtime smoke, touch device or browser test.
- Authenticated GitHub API grants push/admin. Pages source workflow. Existing
  public root https://zney295.id.vn/chemistryLAB/ HTTP 200 and old 3D redirect.
  Existing successful run 30922021293 is NOT a deployment of these changes.

## Required next steps (do in order, noninteractive)

1. Inspect current diff/compile readiness. Fix paused dispatch, HUD committed
   state and mobile input/layout; finish matrix Markdown/JSON together. Review
   helper validation assumptions. Do not invent source citations or chemistry.
2. Run static commands below, regenerate export after any data edit.
3. Run Unity ValidateOnly; monitor logs, distinguish license/package loops from
   compiler errors; correct actual errors. No parallel Unity instances.
4. Run MatrixParityExport.Export then JS --unity-parity; this catches divergence
   between C# formulas/statuses/ratios and the documentation exporter.
5. Run Windows BuildWindows then smoke and ValidateWindowsPackage. Inspect
   output root before cleanup (currently no build output was found). Build
   regenerates scene/material/native assets; inspect resulting diff, avoid
   committing unrelated generated churn. Do not overwrite original user edits.
6. Run browser on local HTTP server: 1280x720/1920x1080 and 390x844/844x390;
   test matrix load, dropdowns, Na2SO4 search, excluded ammonium/hydroxide,
   keyboard arrows, language persistence, no viewport overflow, source links.
   Use purpose-built browser tools or Playwright. Capture ignored screenshots.
7. Exercise Unity screen captures at 16:9,16:10,ultrawide; use built-in capture
   arguments discovered from source. Test real multitouch if available; if
   unavailable state it. Installed Android/iOS modules do not prove SDK/signing
   or physical-device behavior. Implement a development Android build entry
   only if viable and report platform limitations accurately.
8. Update root README, bilingual player guide MD/JSON and staged presentation
   MD/JSON with actual behavior/controls, bounded reaction loop and limitations.
   Preserve formula symbols. Record test results with timestamp/source identity.
9. Review entire diff against user contracts. In same AGY conversation critique
   actual implementation and repair issues. Run affected tests again.
10. If checks pass, stage task-owned changes explicitly (exclude original
    ProjectSettings change), commit to authorized branch/main, push, monitor
    Pages Actions for exact commit, verify deployed 2D HTML/JSON source hash and
    redirects. User has authorized publication; no extra permission prompt.
    Never push known failing code to imply completion. If blocked, preserve
    detailed actionable status in this handoff and report exact remaining work.

## Commands

```powershell
Set-Location C:\Users\admin\MyProject\chemistryLAB
git status --short
node scripts/export-matrix.mjs
node scripts/validate-project.mjs
node scripts/export-matrix.mjs --check
git diff --check
$unityExe = 'C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe'
# Start-Process -WindowStyle Hidden -PassThru with these arguments; poll logs.
& $unityExe -batchmode -nographics -quit -projectPath C:\Users\admin\MyProject\chemistryLAB -executeMethod ChemistryLab.Desktop.Editor.DesktopLabBuild.ValidateOnly -logFile C:\Users\admin\MyProject\chemistryLAB\Logs\redesign-validation.log
& $unityExe -batchmode -nographics -quit -projectPath C:\Users\admin\MyProject\chemistryLAB -executeMethod ChemistryLab.Desktop.Editor.MatrixParityExport.Export -logFile C:\Users\admin\MyProject\chemistryLAB\Logs\matrix-parity.log
node scripts/validate-project.mjs --unity-parity Logs/matrix-unity-parity.json
& $unityExe -batchmode -quit -projectPath C:\Users\admin\MyProject\chemistryLAB -executeMethod ChemistryLab.Desktop.Editor.DesktopLabBuild.BuildWindows -logFile C:\Users\admin\MyProject\chemistryLAB\Logs\redesign-build.log
& .\Builds\ChemistryLab3D\Windows-x64\ChemistryLab3D.exe -smokeTest -reportPath C:\Users\admin\MyProject\chemistryLAB\BuildReports\redesign-smoke-report.json -logFile C:\Users\admin\MyProject\chemistryLAB\Logs\redesign-smoke.log
& $unityExe -batchmode -nographics -quit -projectPath C:\Users\admin\MyProject\chemistryLAB -executeMethod ChemistryLab.Desktop.Editor.DesktopLabBuild.ValidateWindowsPackage -logFile C:\Users\admin\MyProject\chemistryLAB\Logs\redesign-package.log
gh run list --repo psy-zney/chemistryLAB --workflow deploy-docs-pages.yml --limit 5
```

Use real process exit/log success markers and fresh structured reports. Do not
report static validation as gameplay testing or all chemistry as certified.
