# chemistryLAB automation memory and detailed AGY handoff

## Run record — 2026-09-25, Asia/Ho_Chi_Minh (UTC+7)

- Automation ID: thi-t-k-l-i-chemistrylab. First scheduled run, requested 04:10 on 25 September 2026. Inspection ran approximately 04:10–04:19 local; exact completion timestamp appended below.
- Existing scheduler config: cron, ACTIVE, DAILY 04:10 COUNT=1, model gpt-6-astra, reasoning_effort high, local/projectless. No second schedule or task was created. Do not interpret ACTIVE with COUNT=1 as authorization for repeated redesign runs. Do not claim actual model runtime identity solely from this configuration.
- Canonical repo: C:\Users\admin\MyProject\chemistryLAB. HEAD d15a246f3555cd5a9e50ce95672e11da89af2e15, main tracking origin/main. Worktree clean at survey. Tracking status is local, not a fresh remote comparison.
- Origin: https://github.com/psy-zney/chemistryLAB. Recent commits d15a246 (3D docs CSS), 4bfdea5 (Unity consolidation/bilingual UI), 2b07c44 (staged workflow docs).
- Read ancestor instruction locations and repo AGENTS.md, root README, docs index, relevant chemistry/gameplay/design docs and runtime/build/Pages sources. No .codegraph at root; skip CodeGraph unless a future run finds one.
- No implementation, commit, push, deployment, or AGY design collaboration occurred. User explicitly requested survey/handoff fallback when AGY is unavailable. Do not report redesign complete.

## Verified blockers and required recovery

1. AGY executable exists at C:\Users\admin\AppData\Local\agy\bin\agy.exe. Help succeeds. `agy models` exits 1: `Please sign in to view available models. Launch the CLI without arguments to sign in.` Backend also reports not logged into Antigravity, Access denied for log/crash files, unavailable local summary DB, and a network socket access-permission failure during telemetry shutdown. No model 3.8 invocation was possible; no model substitution occurred. This establishes unavailability in this scheduled account/environment, not absence of AGY on the user's machine.
2. Repository is readable but outside all scheduled-session writable roots. Approval policy never; do not request escalation, alter ACLs, or claim sandbox disabled. `--dangerously-bypass-approvals-and-sandbox` in a prompt does not change Desktop scheduling permissions. User must launch/configure work against the canonical repo with actual write access, and make AGY authentication/state writable for that execution account.
3. Despite declared writable projectless roots, file creation in both work/audit.cjs and outputs/AGY-HANDOFF.md returns Access denied. Directory creation succeeded, but apply_patch and Set-Content file writes failed. No output artifact exists there. Do not link a nonexistent deliverable. This memory.md is the verified writable fallback and contains the complete handoff. Once permissions are repaired, copy the handoff into repo docs/handoff/AGY-3.8-redesign.md; no such repo file was created in this run.
4. `gh auth status` reports invalid token for psy-zney. Scheduled shell has restricted network; do not conclude the user's token is globally invalid. Restore accessible authentication/network in the execution context and recheck. User may run `gh auth login -h github.com` interactively if authentication remains invalid.
5. Git originally rejected ownership under CodexSandboxOffline versus owner admin. Read-only Git worked with command-scoped `-c safe.directory=C:/Users/admin/MyProject/chemistryLAB`. Global Git config was not changed.
6. Unity 6000.5.3f1 installation was found at C:\Program Files\Unity\Hub\Editor\6000.5.3f1. Unity was not launched: it writes Library/Logs/reports into the nonwritable repo, and the fallback task is survey/handoff. Licensing and build ability are unverified, not known compiler failures.

## What was inspected and verified

Read-only Node audit executed through stdin, without writing source or audit scripts:
- 14 tracked JSON files parsed successfully.
- 67 tracked non-meta asset files and 28 asset subdirectories: all had corresponding .meta; no orphan tracked Assets .meta found.
- 36 direct package versions match Packages/packages-lock.json.
- Inline relative Markdown file-link scan found 33 candidate matches. One was LaTeX `[...](\\text{OH})` in docs/design/inorganic-synthesis-tree.md:59, manually confirmed as a regex false positive. The remaining 32 local links exist. Anchors, raw paths, HTML links, remote URLs, and full Markdown grammar were not validated.
- Git diff --check passed on unchanged worktree.
- Runtime matrix JSON: 27 elements, 46 ions = 25 cations + 21 anions; no duplicate ion IDs. Cartesian domain 525 ion pairs; GCD construction neutralizes all 525 formal ratios. Eight ionic exclusions leave 517 accepted ionic coordinates; 48 extra element/oxidation-state oxide coordinates bring total to 565 and 541 unique formulas. 45 override records and nine total exclusions. Counts were independently reconstructed from read source and JSON, NOT obtained by executing C#.
- Charge balancing and aggregate parity do not prove existence, stability, solubility, equilibrium, reaction validity, or atomic correctness of every override. Entire chemical catalog has NOT been scientifically certified.
- Only two project asmdefs found (Runtime and Editor); no dedicated test assembly found. Existing validators and runtime smoke routine are substantial and should be extended.
- Committed BuildReports/desktop-validation-report.json is historical, generated 2026-07-30T21:38:08.3233760Z. Its success is not a current build/test result. It reports 52 elements, 40 chemicals, 38 curated reactions, nine dynamic families, seven condition profiles, eight redox rules, 155/780 resolved pairs.
- No desktop/mobile visual or interaction test ran. No fresh Unity validation/build ran.

## Current code and product map

All paths below are relative to canonical repo, not the projectless automation cwd.

- AGENTS.md: canonical Unity project only; preserve sample staging, curated chemistry precedence, universal formula notation, VI/EN persistence, reduced motion, assembly and .meta boundaries.
- Assets/ChemistryLab/Runtime/Bootstrap/DesktopLabGame.cs: composition root, inventory/selection, independent station trays/vessels, reaction/safety/audio coordination, procedural room. Line 12 hardcodes MissionReactionId=copper-hydroxide; mission completion around 494; current smoke coroutine around 2989. Large file: isolate new state/rules rather than keep adding UI/gameplay branches.
- Runtime/Player/LabInteractions.cs: interactable objects, controller; Input.GetKeyDown calls around 438–528, mouse axes at 542–543, movement at 553–556, E interaction around 616. No touch/safe-area implementation found in examined controller/HUD.
- Runtime/UI/DesktopLabHud.cs: main/pause/settings, inspectors, reaction card; CanvasScaler around 861–871. Existing scaling alone does not demonstrate mobile support.
- Runtime/Core/LabLocalization.cs and LabTheme.cs: shared strings, persistence and styling/accessibility.
- Runtime/Chemistry/ChemistryData.cs and ExtendedChemistryData.cs: chemical catalog, curated reactions and simulation. Preserve curated → reviewed redox → bounded dynamic resolution, followed by condition gating.
- Runtime/Chemistry/CompoundMatrixDataContracts.cs: JSON records for elements/ions/overrides/exclusions. Ion id, signed charge, formula, molar mass, polyatomic and hazard fields already exist; no per-cell evidence/conditions schema.
- Runtime/Chemistry/CompoundGenerationModels.cs: ion/compound result types, CationId/AnionId/Counts, confidence and properties. Numeric confidence constants are software heuristics, not measured scientific probabilities.
- Runtime/Chemistry/CompoundGenerationMatrix.cs: TryGenerateIonicCompound at 122, exclusion before GCD at 140–149; charge-neutral formula and reviewed overrides; EnsureGenerated at 422 builds cation×anion and then a separate oxide series; Coordinate at 728 returns cationId|anionId. Current text calls this X/Y/Z, but core ionic product lookup already has two keys.
- Runtime/Chemistry/DynamicReactionEngine.cs: species map, generated-product registration around 134, pair matching around 227/314. It checks sorted pairs and returns first successful match. Multi-reagent behavior needs explicit policy/tests. Precipitation uses coarse insolubility classification; assess concentration/state assumptions before expanding.
- Runtime/Chemistry/ReactionConditions.cs: temperature, volume, concentration, educational pH/rate/yield constraints. Weak-electrolyte estimates are not full equilibria.
- Runtime/Chemistry/RedoxReactionEngine.cs: reviewed branches/electron counts; do not derive redox feasibility from ionic formula generation.
- Runtime/Chemistry/SynthesizedInventory.cs: collected batch mass/purity, persistence and generated-species registration. Preserve save compatibility and exact once-only collection/withdrawal; audit residual solution/byproducts explicitly.
- Runtime/Safety/LabSafetySystem.cs, Runtime/Audio/DesktopLabAudio.cs: consequences and feedback; preserve functionality.
- Assets/ChemistryLab/Resources/Chemistry/compound-generation-matrix.json and its .meta: authoritative runtime source. Documents in docs/chemistry/ are explanatory companions, not duplicate authority.
- Editor/BuildPipeline/DesktopLabBuild.cs: ValidateOnly at 45; BuildWindows at 67; ValidateWindowsPackage at 122; validators at 131 onwards. BuildWindows regenerates scene/assets and cleans its generated output directory; inspect existing user artifacts before execution.
- Runtime Bootstrap smoke: launch built game with -smokeTest and -reportPath. Tests VI/EN menus, settings return, staged transfer, remote/hand rejection, reaction card/camera, mission product, audio. It exits and writes a report; flags verified in source.
- Packages/manifest.json, packages-lock.json: current Unity dependencies; no package changes needed merely for planning.
- ProjectSettings/ProjectVersion.txt: 6000.5.3f1.
- docs/gameplay/staged-sample-reaction-presentation.md/.json and player-guide-vi-en.md/.json: paired workflow docs must stay synchronized.
- docs/design/economy-and-progression.md and inorganic-synthesis-tree.md contain roadmap ideas; do not mistake future currency/shop/NPC proposals for implemented gameplay or add monetization as an unrelated redesign dependency.

## Design proposal to critique together — NOT implemented or co-approved by AGY

Preserve native Unity game, add genuine mobile controls within the same project; user explicitly requested mobile and this takes precedence over repo's default desktop-only scope. Do not assume GitHub Pages is the playable game: Pages currently hosts documentation. Do not resurrect a duplicate web game.

Target loop: choose an educational objective → inspect available chemicals/ions → predict product/observation and conditions → select measured sample → place on the station's preparation tray → transfer into that physical vessel → observe outcome and explanation → analyze/collect/account for residue → clean/reset → record learning/progress and choose another task. Keep free experimentation alongside guided tasks. Start with a small set of verified objectives rather than inflate reaction count.

Explicit gameplay states: ChoosingTask, SelectingSample, HoldingSample, StagedAtStation, Loaded, BlockedByConditions, Reacting, Observing, Collectable, Collected, Cleanup. State ownership must prevent double load, wrong-station loading, repeated reaction consumption or collection. Merely selecting an ion/cell never triggers synthesis.

Desktop UI: compact persistent objective and station status, contextual action prompt, expandable chemical/result inspector; make amount, temperature, volume and safety state discoverable without memorizing function keys. Keep menus keyboard navigable with visible focus, consistent Esc/back, and input ownership that prevents player movement while editing a control.

Mobile UI: landscape Unity layout with safe-area-aware panels, left movement and right look zones, explicit context action, inspect, inventory, back/pause and amount controls. Separate pointer IDs and UI capture so joystick/look do not activate a vessel, and modal/pause cancels input. Minimum proposed touch targets 48 logical pixels; test on device, font scaling, VI/EN, not just scaled desktop mockup. Add platform build targets only once required SDK/support modules are verified; report missing Android/iOS prerequisites explicitly.

Feedback: show reason/action for blocked conditions, distinguish no net reaction from unsupported chemistry and excluded combinations, annotate estimated properties, explain reagent shortage without inventing measurements. Use text/icon plus color, captions and optional motion. Reduced motion keeps equation/observations and functional controls while suppressing camera moves/flashes.

## Proposed 2D data contract and chemistry guardrails

Rows = anionId, columns = cationId; e.g. matrix[anionId][cationId]. Use explicit named DTO fields to avoid silently reversing existing cationId|anionId coordinates. Preserve old IDs and provide conversion helpers/tests, not positional indices in saves.

Ion record: stable id; ascii formula plus generated display formula; signed integer charge; atomic composition; polyatomic flag; molar mass with provenance/precision; labels VI/EN; evidence IDs. Fe(II) and Fe(III) are separate IDs (existing iron-two and iron-three).

Cell record: anionId, cationId; status reviewed/ruleDerived/unsupported/excluded; formula or null; minimal cationCount and anionCount; named compound/species references; phase and solvent/context; solubility classification and optional sourced equilibrium constants with temperature; applicable condition IDs; exclusion/exception IDs and readable reason; evidence/source IDs. Do not fabricate Ksp, rate, temperature/pH ranges or precision when absent.

Separate reaction records: explicit reactant/product species and coefficients, phases, catalysts, condition predicates, net ionic/molecular equations, atom/charge conservation, evidence and observations. A neutral cell/formula is a compositional candidate, never evidence that a reaction occurs or that a stable bottleable compound exists. Do not use a list of only nine exclusions as proof that all other 517 cells are real compounds.

Let qc>0, qa<0, g=gcd(qc,abs(qa)); cationCount=abs(qa)/g; anionCount=qc/g. Verify charge sum zero, counts positive and coprime; parentheses around repeated polyatomic ions. Examples matching CURRENT IDs and formula behavior:
- sulfate × sodium → Na2SO4, cation:anion=2:1.
- phosphate × calcium → Ca3(PO4)2, 3:2.
- hydroxide × copper-two → Cu(OH)2, 1:2.
- acetate × hydrogen currently uses a reviewed CH3COOH override: weak-acid speciation must not be inferred from complete ionic dissociation.
- hydroxide × ammonium is excluded as isolated NH4OH, representing aqueous ammonia equilibrium separately.
- hydroxide × silver is excluded as an isolated hydroxide; a separately reviewed transformation is required to show oxide/water products.
- Existing oxide:element:oxidationState routes (including nonmetal oxides) are not all aqueous ion cells. Keep a separate covalent/oxide catalog or explicit registry; do not force them into fictitious aqueous cations simply to preserve count 565.

Conceptual flow:
Ion catalogs → anion row/cation column → validate identity and evidence → balance formula → cell status/exceptions → property/condition references.
Vessel contents + conditions → curated/redox/bounded reaction resolver → conservation validation → actual ReactionOutcome → HUD, inventory, safety and notebook.

Existing exclusions: ammonium|oxide, ammonium|hydroxide, hydrogen|bicarbonate, silver|hydroxide, mercury-two|hydroxide, aluminium|bicarbonate, iron-three|bicarbonate, chromium-three|bicarbonate, oxide:H:1. Preserve rationale while reviewing scope and adding evidence; do not delete exceptions to increase coverage.

Sources retrieved for targeted verification, not full catalog review:
- https://openstax.org/books/chemistry-2e/pages/4-2-classifying-chemical-reactions — precipitation/ionic-equation classification and solubility rules.
- https://openstax.org/books/chemistry-atoms-first-2e/pages/15-1-precipitation-and-dissolution — concentration-dependent Q versus Ksp; dilute ideal-solution concentration approximation must be labeled.
- https://goldbook.iupac.org/terms/view/S05742 — Ksp defined from ion activities and dissociation equilibrium.
Use primary/authoritative chemical references per reaction/property. Exact conservation is necessary but insufficient for feasibility; salts may hydrolyze, oxidize/reduce, complex or decompose. Review problematic cation/anion families before enabling gameplay. If knowledge is missing, unsupported is a valid educational outcome.

## Collaboration and implementation sequence for AGY 3.8 high and Astra high

0. Repair environment first. Open authorized writable canonical repo as project, authenticate AGY under same account, run `agy models`, copy the actual 3.8 model ID (do not guess). CLI supports --model, --effort high, --mode plan, --print/-p, --print-timeout, --output-format; use verified help. A permission bypass flag on AGY does not grant OS or scheduler rights. Authenticate GitHub only through normal user-supported flows. Re-read AGENTS/status and CodeGraph presence. Do not discard new user changes.
1. Place this handoff in repo and create a shared decision/review ledger containing HEAD, requirements, evidence, alternatives, decisions, unresolved questions, assigned files and acceptance tests. Astra writes proposed product/data design; AGY critiques real chemistry, mobile interactions and migration. Exchange findings and record actual responses/model IDs. Do not call concurrent edits to the same files. If AGY unavailable again, stop implementation and refresh survey/handoff per user fallback.
2. Establish baseline: static checks, Unity ValidateOnly, existing Windows build/smoke if licensing permits. Capture commit and timestamp in structured results. Do not use July report as baseline evidence. Read only applicable Unity/game UX skill if available; repository skill names are supplemental, not proof those skills are installed.
3. Data work first: add versioned 2D DTO/schema, stable-ion registry, explicit statuses/provenance, migrated overrides/exclusions. Keep a compatibility adapter for consumers and existing batch saves. Test charge/atom/ID/exception invariants and reviewed-record precedence. AGY reviews actual migrated cells and sources before they enable transformations.
4. Reaction work: centralize consumption/products/residuals into outcomes; distinguish no reaction, unknown and blocked. Preserve curated→redox→bounded order; define deterministic behavior for mixtures with >2 reagents. Add negative, permutation and limiting-reagent tests. No unsourced dynamic expansion.
5. Gameplay work: separate tutorial/mission state from giant composition root; implement repeatable goals/notebook and staged state machine, feedback and recovery. Keep freeplay, both stations, safety consequences and inventory persistence working.
6. UI/input work: shared action abstraction then keyboard/mouse and touch implementations, responsive HUD/menu/inspectors, safe area, focus/back/gesture arbitration. Test controls with real outcomes. Include VI/EN and reduced-motion paths in each increment.
7. Documentation: update root README, docs index, chemistry/gameplay Markdown+JSON pairs, add primary 2D explorer/table and diagram with IDs/charges/ratios/status/references. Export versioned matrix fixtures from canonical code/data so JS view does not re-implement chemistry silently. Keep 3D view as an optional projection if useful; label it and update links. Use accessible text table/detail fallback when graphics/CDN fails.
8. CI: add pull-request validation for JSON/schema/IDs/charge balance/atomic conservation/fixture parity/link checks/meta consistency. Extend deploy trigger paths to all schema/export/validator and relevant runtime source changes; current workflow only watches docs, single matrix JSON, itself, README. Add setup/build validation before artifact upload and configure Pages according to current official Actions guidance. Do not claim changing Actions versions without checking upstream.
9. AGY reviews implementation diff against shared decisions and independently challenges edge cases; Astra addresses findings and reruns affected checks. Save actual review and test evidence.
10. Publish only verified work within granted authority: preserve user changes, inspect final diff, commit scoped changes, push authorized branch/main or create review PR as appropriate. If opening a PR, attach it to task, use codex and codex-automation labels when available. Pages deploy must correspond to exact tested commit. Verify workflow success, page HTTP/load, JSON fetch, base-path links and desktop/mobile docs interaction. Report failure and remaining step precisely; don't equate push with deployment.

## Acceptance criteria and checks to add/run

Data: all IDs unique; signs correct; charge neutral and reduced ratios; atom balance in each reviewed molecular/net ionic equation; unknown pairs cannot synthesize; excluded pairs show reason; no fabricated physical constants; preserved reviewed precedence; Fe oxidation-state distinction; 2D/Unity/export parity; existing batch-save migration and repeated load/collect mass conservation.
Chemistry scenarios: verified CuSO4 + 2NaOH → Cu(OH)2 + Na2SO4, precipitation present; NaCl(aq)+KNO3(aq) no net reaction; approved acid/base and carbonate examples; missing/present catalyst; dilute/concentrated redox branches; insufficient reagent; no double consumption. Verify exact phase/conditions and data IDs before codifying fixtures. Full chemistry accuracy cannot be reduced to expected pair counts.
Gameplay: first-run tutorial → reaction → observation → collect → cleanup → next task; both workbench and hood; attempted reaction while holding or distant/wrong-station loading fails without losing sample; blocked-condition recovery; no duplicate completion rewards; inventory save/reload and mass accounting; safety warning/consequence remains consistent.
UI: desktop 1280×720, 1920×1080, 16:10 and ultrawide; mobile landscape e.g. 844×390 and 915×412 logical viewport with safe areas, scaling and device tests. No overlap/cutoff for long VI/EN text; keyboard focus, touch multitouch/capture and modal behavior; reduced motion still complete; no color-only status.
Build/report: exact Unity version; no new compile errors; new validation and smoke JSON with commit/time; package runtime files and checksum; mobile build separately or clearly blocked by platform SDK; unchanged unrelated files.
Pages: real 2D axes/table with cell detail, known examples and exceptions from exported data; no stale 3D-only redirect; base /chemistryLAB/ works; data URL fetch; no broken nav; readable mobile docs; matched deployed commit; new CI job actually executed.

Commands below are for the repaired writable/authenticated environment, NOT executed successfully in this run:

```powershell
$repo = 'C:\Users\admin\MyProject\chemistryLAB'
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe'
git -c safe.directory=C:/Users/admin/MyProject/chemistryLAB -C $repo status --short --branch
git -c safe.directory=C:/Users/admin/MyProject/chemistryLAB -C $repo diff --check
# Create task log directory within authorized repo and inspect licensing/package logs if blocked.
& $unity -batchmode -nographics -quit -projectPath $repo -executeMethod ChemistryLab.Desktop.Editor.DesktopLabBuild.ValidateOnly -logFile "$repo\Logs\redesign-validation.log"
& $unity -batchmode -quit -projectPath $repo -executeMethod ChemistryLab.Desktop.Editor.DesktopLabBuild.BuildWindows -logFile "$repo\Logs\redesign-build.log"
& "$repo\Builds\ChemistryLab3D\Windows-x64\ChemistryLab3D.exe" -smokeTest -reportPath "$repo\BuildReports\redesign-smoke-report.json" -logFile "$repo\Logs\redesign-smoke.log"
& $unity -batchmode -nographics -quit -projectPath $repo -executeMethod ChemistryLab.Desktop.Editor.DesktopLabBuild.ValidateWindowsPackage -logFile "$repo\Logs\redesign-package.log"
gh auth status
gh api repos/psy-zney/chemistryLAB/pages
gh run list --repo psy-zney/chemistryLAB --workflow deploy-docs-pages.yml --limit 5
# Inspect the actual run ID, its tested commit, logs, artifact and Pages URL.
```

Do not invent a pre-existing `npm test`, mobile pipeline or new test command; add and document real commands with implementation. Check process exit and log success marker, not just file presence. Respect AGENTS warning about licensing/package-manager loops and stop/report rather than repeatedly launch Unity.

## Pages status and limits at this checkpoint

Existing .github/workflows/deploy-docs-pages.yml assembles docs plus runtime JSON into _site; root redirects to docs/chemistry/compound-matrix-3d.html. Uses checkout@v6, upload-pages-artifact@v4, deploy-pages@v4. Push main with path filters or workflow_dispatch; deployment has pages:write/id-token:write, github-pages environment, concurrency pages. No general Unity/test CI exists in inspected workflows. README says enable Pages source GitHub Actions once; actual settings unverified.

Public fetch attempts for https://psy-zney.github.io/chemistryLAB/ and the 3D viewer returned internal/cache/access errors. GitHub workflow fetch also cache-missed. These are tool retrieval failures, not verified site downtime. No deployment attempted. Remaining publishing prerequisites: working auth/network, repository write authority, Pages source/config verification, tested source changes, successful workflow and live verification.

GPT remaining snapshots: start 96% five-hour / 70% weekly; survey 90% / 69%; 04:16:51 checkpoint 74% / 66%. Never below 25% at these checks. No reset credit used. Refresh near completion; if either core window goes below 25%, immediately prioritize preserving/updating this handoff before more work.

Next action for the user: make the canonical repository and execution account writable, sign into AGY and verify actual 3.8 model access, then resume using this shared handoff. Repair GitHub auth/network before publishing. No need to reauthorize the originally delegated design/build work once these prerequisites are fixed.

## Final run summary
Completed survey/handoff fallback at 2026-09-25T04:20:16.8026736+07:00. Canonical worktree rechecked clean; no implementation/build/gameplay verification/deploy performed. Detailed handoff saved and read back in this memory file; outputs/repo writes unavailable. Final GPT remaining: 69% five-hour, 66% weekly. AGY still unavailable in scheduled account; next steps are writable repo execution context, AGY login/model verification, and GitHub auth/network repair. No second scheduled run was created.
