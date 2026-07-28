# Validation and build history

This document is the durable, human-readable replacement for transient Unity,
build, capture, smoke-test and development-server logs. The machine-readable
record is [`validation-history.json`](validation-history.json).

## Retention policy

- Raw `*.log` files are ignored because Unity, Gradle, CMake and development
  servers regenerate them frequently.
- Important outcomes, failure causes and resolutions are recorded here and in
  JSON before raw logs are removed.
- Screenshots and release archives remain build artifacts. They are not promoted
  into Git unless a later task explicitly requests it.
- The cleanup snapshot covered **41 files / 14,000,762 bytes**. None of those raw
  logs were tracked by Git.

## Native Chemistry Lab — verified shipping state

Environment:

- Unity `6000.5.3f1`
- Windows Standalone x64
- Native C#/UGUI application; no browser runtime

Final build:

- Result: **pass**
- Unity warnings: **0**
- Reported player size: **94,030,828 bytes**
- Executable: `NativeChemistryLab/Builds/ChemistryLab3D/ChemistryLab3D.exe`

Reaction validation:

- **38/38** reaction definitions passed.
- Both reactant addition orders were checked.
- **11/11** fume-hood rules blocked unsafe workbench use.
- All four effect classes were covered: heat, precipitate, gas and colour.
- Four procedural audio signal classes were checked for finite, non-silent,
  bounded waveforms.

Shipping smoke test:

| Check | Result |
| --- | ---: |
| Chemicals | 40 |
| Reactions | 38 |
| High-school elements | 52 |
| Mission product estimate | 3.673 g |
| Runtime audio clips | 14 |
| Pause buttons | 3 |
| Default camera FOV | 66.0° |

Visual verification:

- `desktop-visual.png` — main first-person room view passed.
- `periodic-visual.png` — periodic-table view passed.
- `pause-audio-ui.png` — pause and audio menu passed.
- `runtime-debug-ui.png` — F3 diagnostics overlay passed.

Release archive:

- Path: `NativeChemistryLab/Builds/ChemistryLab3D-Windows.zip`
- Size: `35,175,997` bytes
- Entries: `158`
- SHA-256:
  `E33970D5823E1FD413723F08BC35B76BFE33AF2DEA72B95020010EDD3DA67E45`

## Important resolved failures

### Initial headless smoke test

The first smoke test threw `ArgumentNullException` while constructing a
`Material`: no suitable shader was available under the null graphics device.
The desktop project was updated to ship a runtime material resource. All later
smoke tests passed.

### Screenshot capture modes

Initial captures failed when Unity ran without a visible render surface:

- The first desktop capture failed under the initial D3D12/non-visible setup.
- A windowed attempt had no terminal capture marker.
- The first pause capture failed in D3D11 batch mode.

The test was changed to launch a short-lived visible D3D11 window. Desktop,
periodic-table, pause and debug captures then passed and the player exited
automatically.

### Obsolete Unity API warning

The first audio-enabled build reported one `CS0618` warning for
`FindFirstObjectByType`. The HUD now uses `FindAnyObjectByType`; the shipping
build reports zero warnings.

## Other project validation

### Root Unity project

The root project was revalidated with Unity `6000.5.3f1` after reconciling the
legacy presentation contract:

- `Reaction.NameKey` was mapped to the stable reaction `Id`.
- `Reaction.FormulaEquation` was mapped to `EquationDisplay`.
- The obsolete TextMeshPro word-wrapping call was replaced.

Final result: **pass**, with **0 compiler errors** and **0 compiler warnings**.
The runtime catalogue contains **13 chemicals**, **5 reactions** and **16
reaction participants**. The structured result is stored in
`root-catalogue-validation.json`.

### `LAB-animated` prototype

The Vite `6.3.5` production build passed and transformed 2,035 modules. Its
generated `dist` directory remains ignored because it is reproducible output.

### Legacy `ChemistryLabGame` mobile project

- Android release APK build: **pass**, Unity exit code `0`, duration
  `42.909 s`, output size `32,599,297` bytes.
- Unity reported a non-fatal stale Burst JIT cache DLL load issue, recovered,
  and completed the player build successfully. The structured result is stored
  in `mobile-android-build-report.json`.
- iOS export: **blocked**, Unity exit code `1`. Unity iOS Build Support was not
  installed, and a final iOS export requires macOS.

## Raw-log cleanup inventory

The following raw logs were summarized and then removed.

### Development servers

- `LAB-animated/vite-dev.log`
- `LAB-animated/vite-dev.err.log`
- `output/playwright/vite.out.log`
- `output/playwright/vite.err.log`

### Root Unity project

- `Logs/upm.log`
- `Logs/Packages-Update.log`
- `Logs/Editor.log`
- `Logs/Editor-prev.log`
- `Logs/catalogue-validation.log`
- `Logs/AssetImportWorkerHW2-prev.log`
- `Logs/AssetImportWorkerHW1-prev.log`
- `Logs/AssetImportWorkerHW0-prev.log`
- `output/unity-native/build.log`

### Native desktop laboratory

- `output/unity-native/smoke.log`
- `output/unity-native/smoke-v2.log`
- `output/unity-native/smoke-audio.log`
- `output/unity-native/smoke-shipping.log`
- `output/unity-native/isolated-build.log`
- `output/unity-native/isolated-build-audio.log`
- `output/unity-native/isolated-build-final.log`
- `output/unity-native/isolated-build-release.log`
- `output/unity-native/isolated-build-shipping.log`
- `output/unity-native/capture.log`
- `output/unity-native/capture-windowed.log`
- `output/unity-native/capture-visible.log`
- `output/unity-native/capture-periodic.log`
- `output/unity-native/capture-pause-audio.log`
- `output/unity-native/capture-pause-audio-visible.log`
- `output/unity-native/capture-debug-visible.log`
- `NativeChemistryLab/Logs/upm.log`
- `NativeChemistryLab/Logs/shadercompiler-UnityShaderCompiler.exe-0.log`
- `NativeChemistryLab/Logs/AssetImportWorkerHW0.log`
- `NativeChemistryLab/Logs/AssetImportWorkerHW0-prev.log`

### Legacy mobile project and native toolchain

- `ChemistryLabGame/Logs/upm.log`
- `ChemistryLabGame/Logs/Packages-Update.log`
- `ChemistryLabGame/Logs/AssetImportWorkerHW0.log`
- `ChemistryLabGame/Logs/AssetImportWorkerHW0-prev.log`
- `ChemistryLabGame/Builds/Android/unity-build.log`
- `ChemistryLabGame/Builds/iOS/unity-ios-build.log`
- `ChemistryLabGame/build/outputs/logs/unity---stop-build.log`
- `ChemistryLabGame/.utmp/RelWithDebInfo/p5p2g1s4/armeabi-v7a/CMakeFiles/CMakeOutput.log`

## Related commits

- `d0d6e5a` — high-school substances and reactions
- `950e3db` — procedural laboratory audio
- `fe724ca` — playable 3D runtime, UI and diagnostics
- `9b0c8a8` — isolated Unity project and validation pipeline
- `7663ed4` — controls, tests and design-audit documentation
- `fc6a9da` — professional feature-folder and assembly restructure
- `24cf2ec` — root Unity catalogue compatibility and presentation
- `8aef83a` — Android/iOS Unity build targets
- `fd213c5` — animated interface reference prototype
- `b0406f6` — economy and progression design documents
