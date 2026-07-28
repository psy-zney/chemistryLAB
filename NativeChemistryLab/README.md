# Native Chemistry Lab

This folder is the active Unity/C# desktop game project. Open this folder in Unity 6 when working on the current 3D Windows version.

## Project Layout

```text
Assets/ChemistryLab/
|-- Editor/
|   `-- BuildPipeline/       Build, validation, smoke-report, and scene tooling
|-- Resources/
|   `-- Chemistry/           JSON source data for the compound-generation matrix
|-- Runtime/
|   |-- Audio/               Procedural ambient, UI, movement, reaction, and hazard sounds
|   |-- Bootstrap/           DesktopLabGame composition root and procedural room builder
|   |-- Chemistry/           Chemical catalogue, periodic table, simulator, dynamic engine
|   |-- Core/                Theme colors, fonts, reduced-motion settings
|   |-- Diagnostics/         F3 runtime debug panel
|   |-- Player/              First-person controller and interactable world objects
|   |-- Safety/              Hazard profiles, PPE state, exposure consequences
|   `-- UI/                  HUD, inspector, pause menu, safety panel, button feedback
`-- Scenes/                  DesktopChemistryLab.unity
```

Runtime and editor code are split by assembly definitions:

- `ChemistryLab.Desktop`
- `ChemistryLab.Desktop.Editor`

## Run

Open `NativeChemistryLab` in Unity `6000.5.3f1` or newer, then open:

```text
Assets/ChemistryLab/Scenes/DesktopChemistryLab.unity
```

The built executable is:

```text
Builds/ChemistryLab3D/ChemistryLab3D.exe
```

Keep the generated `ChemistryLab3D_Data` folder next to the `.exe`.

## Controls

```text
WASD          Move
Mouse         Look
Shift         Sprint
E             Interact
F             Inspector
[ / ]         Adjust sample mass
Q             Clear selected sample
Page Up/Down  Heat/cool the active vessel by 25 °C
F8            Add 50 mL solvent
C             Collect product into persistent inventory
I             Cycle synthesized product batches
F3            Diagnostics
F6            Buy/equip/remove respirator
F7            Connect/disconnect gas trap
F9            Audio on/off
F10           Reduced motion
Esc           Pause
```

## Simulation Scope

- 52 high-school-relevant elements.
- 40 chemicals with physical properties, colors, hazards, handling notes, and 3D material settings.
- 38 curated reactions.
- 9 dynamic reaction rule families.
- 7 explicit reaction-condition profiles plus condition/rate evaluation for every mixture.
- 8 oxidation-reduction rules with electron-balance validation.
- 155 valid dynamic two-chemical pairs from the 780-pair catalogue matrix.
- 27 compound-matrix elements and 46 reusable ions.
- 565 accepted charge-balanced coordinates representing 541 unique formulas,
  including 45 reviewed property overrides.
- Generated physical classes for phase, solubility, appearance/color, hazards, confidence, and validation notes.
- Gas hazard profiles for `CO2`, `H2`, `O2`, `NH3`, `H2S`, `Cl2`, `NO`, `NO2`, and `SO2`.
- Per-vessel temperature, volume, concentration, pH, catalyst, rate, time, yield, and purity state.
- Reusable synthesized batches persisted to `chemistry-inventory.json` under Unity's
  `Application.persistentDataPath`; consuming a batch subtracts its available mass.

The data-driven matrix is stored at:

```text
Assets/ChemistryLab/Resources/Chemistry/compound-generation-matrix.json
```

`CompoundGenerationMatrix` validates this file at startup and during batch validation. Curated reactions remain authoritative; generated compounds support the fallback engine and are labelled `Reviewed` or `RuleDerived` in the HUD.

Resolution order is: curated reaction → reviewed redox rule → dynamic ionic rule →
condition gate → stoichiometry/yield → safety consequence → optional product collection.
Gas products can only be collected in the fume hood with the isolation trap connected.

The simulation is educational. Gameplay values for damage, exposure, and cost are not medical or industrial safety limits.

## Developer Reports

Committed structured reports live in:

```text
BuildReports/
```

Important reports are JSON. Raw editor/player logs are temporary and should not be committed unless first converted into a concise JSON or Markdown report.
