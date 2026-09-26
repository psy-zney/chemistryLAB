# Modern lab visual upgrade

This upgrade adds original Blender geometry and Unity materials/VFX to the
canonical runtime lab, preserving the curated → redox → dynamic resolution
order, staged sample workflow, safety consequences and CharacterController.

The original kit contains 9 modules, 43 meshes and 22,120 source vertices.
The furniture has resin slabs, bevels, drawer reveals, handles, adjustable
feet and service sockets. The hood has a sash, tracks, work light, extraction
slots and exhaust housing. Shelves, a recessed steel sink, framed architecture,
preparation trays, reagent bottles and bench tools share the same palette.
The previously approved flask/tube meshes retain their scale and provenance.

`ModernLabArtIntegration` creates identity-root Unity prefabs; the FBX axis
conversion stays on a visual child. `ModernLabArt.Install` uses these prefabs
under existing anchors after procedural construction. Decorative meshes have
no colliders, and replaced primitive renderers are hidden while their simple
gameplay colliders remain. Source: [original kit](../../SourceAssets/Original/ModernLab/README.md).

Solution and sediment use the approved flask's inner envelope, with integrated
volume and a narrow meniscus. Emission has a beginning/development/taper, and
particles are clipped at the liquid surface, bottom and vessel radius. State
is independent per vessel and clears at cleanup. Colour and sediment read the
committed outcome; they do not apply damage, credits or mission consequences.
Reduced motion uses immediate colour/sediment, lower emission and lower travel
speed. Camera easing and visual timers freeze on pause.

Local before/after images are in `output/visual-upgrade/before` and `after`.
Room, workbench, hood, shelves, sink, held sample, staged sample, precipitate
and gas use the same camera poses/FOVs at 1920×1080. Additional images cover
reduced motion, colour, hot mixture and VI/EN HUD at 16:9, 16:10 and ultrawide.
These are Unity Play Mode captures. The short video uses 90 Unity frames at
12 fps; Blender's sequencer only encodes those PNGs and renders no lab geometry.
Captures/video are ignored local evidence and are not shipped in the build.

Reproduce the review from the project root (omit `-quit`, because the review
exits after Play Mode finishes):

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.5.3f1/Editor/Unity.exe' -batchmode -projectPath C:/Users/admin/MyProject/chemistryLAB -executeMethod ChemistryLab.Desktop.Editor.LabVisualReview.Capture -reviewOutput output/visual-upgrade/after -logFile Logs/visual-after.log
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --python SourceAssets/Original/ModernLab/encode_unity_review.py
```

The build measurement uses an i5-12450HX and RTX 3050 6GB Laptop GPU at
1920×1080. A hidden window does not present a backbuffer, so the benchmark
forces a real camera render to a 1080p texture each frame, with 90 warmup and
300 measured frames. CPU submission/frame timing and renderer triangle/pass
counters are measured; GPU timestamps were unavailable under both D3D12 and
D3D11. This does not certify 60 FPS during normal window presentation.
The report records unavailable counters explicitly rather than treating zero
as a GPU frame time or draw-call result. Transparent coverage is measured
separately in Unity using opaque depth and an additive replacement shader,
with the HUD excluded. It is fragment coverage, not GPU timing.

The final D3D12 run measured mean CPU frame time 1.337 ms, p95 offscreen
loop time 2.057 ms, 93,030 triangles and 104 SetPass calls. Draw/batch counters
were also unavailable. The workbench coverage image has a maximum of 4
transparent layers and 7.16% covered pixels. These values describe the stated
capture method, without a claim about normal presented frame rate.

Validation completed: 20 Play Mode assertions, actual Windows player smoke,
`DesktopLabBuild.ValidateOnly`, Windows x64 build (0 warnings/errors), package
validation, 19 JSON parses, 142 asset/meta pairs and unique GUIDs, 36 package
dependencies, 37 local Markdown links and `git diff --check`.

Current structured evidence: [visual review](../../BuildReports/modern-lab-visual-review.json),
[validation](../../BuildReports/desktop-validation-report.json),
[Windows build](../../BuildReports/desktop-build-report.json),
[runtime smoke](../../BuildReports/desktop-smoke-report.json).

Remaining limits: the room still spawns at runtime, so no baked lightmap or
baked light-probe result is claimed. There is no refraction or added post
processing. Current chemistry outcomes have one display hue, without separate
solution/metal-coating hues or reviewed gas colours; visuals follow that
record instead of introducing another chemistry dataset. Fire/explosion is
not authorized by the current effect enum. No new pooling/LOD framework or
quality-menu tiers are added without a measured need; existing Unity quality
assets remain. Full GPU/windowed performance certification remains outstanding.
