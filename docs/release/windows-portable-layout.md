# Windows Portable Build Layout

The native Unity player is distributed as one portable folder and one
versioned ZIP. `ChemistryLab3D.exe` cannot be shipped by itself: Unity resolves
`ChemistryLab3D_Data`, `UnityPlayer.dll`, and `MonoBleedingEdge` relative to the
executable.

## Output contract

```text
Builds/ChemistryLab3D/
|-- Windows-x64/                         runnable distribution
|   |-- ChemistryLab3D.exe
|   |-- ChemistryLab3D_Data/             scenes, managed assemblies, resources
|   |-- MonoBleedingEdge/                managed runtime
|   |-- D3D12/                           graphics runtime when emitted by Unity
|   |-- UnityPlayer.dll
|   |-- UnityCrashHandler64.exe
|   |-- build-manifest.json
|   `-- README.txt
`-- Packages/
    |-- ChemistryLab3D-Windows-x64-v<version>.zip
    `-- ChemistryLab3D-Windows-x64-v<version>.zip.sha256
```

The archive adds one top-level `ChemistryLab3D-Windows-x64/` directory. Users
can therefore extract it without scattering DLLs and data folders into their
Downloads or Desktop directory.

## Build pipeline

`DesktopLabBuild.BuildWindows` performs these operations:

1. Validate chemistry, reactions, safety, audio, and generated-compound data.
2. Recreate the native Unity scene and clean only the generated
   `Builds/ChemistryLab3D` root.
3. Build Windows Standalone x64 with `BuildOptions.StrictMode`.
4. Remove top-level `*_BurstDebugInformation_DoNotShip` directories.
5. Verify the EXE, `_Data`, managed assemblies, Unity player, crash handler,
   and Mono runtime.
6. Write the UTF-8 `README.txt` and JSON `build-manifest.json`.
7. Package all runtime files under one ZIP root.
8. Reopen the ZIP, verify its entry count and required roots, then generate a
   SHA-256 checksum.
9. Write package paths, sizes, file count, and checksum to
   `BuildReports/desktop-build-report.json`.

The cleanup operation is guarded so it only accepts the direct
`Builds/ChemistryLab3D` child of this Unity project.

## Release verification

Run the built player smoke test from the `Windows-x64` directory. To verify the
download manually in PowerShell:

```powershell
Get-FileHash `
  Builds/ChemistryLab3D/Packages/ChemistryLab3D-Windows-x64-v1.0.zip `
  -Algorithm SHA256
```

Compare the result with the adjacent `.sha256` file or the
`package.archiveSha256` field in `BuildReports/desktop-build-report.json`.

Do not distribute `Library`, raw Unity logs, or any directory whose name ends
with `BurstDebugInformation_DoNotShip`.
