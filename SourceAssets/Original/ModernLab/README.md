# Modern lab artwork

Original geometry and deterministic 1K material textures created for this
project in Blender 5.2.1 LTS. No downloaded geometry, texture or rig is used in
this kit. See `provenance.json` for the object/material audit and source paths.
The previously approved Pixabay flask and test tubes retain their own
attribution and geometry; they are not part of this original kit.

Run from the canonical project root:

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --python SourceAssets/Original/ModernLab/build_lab_kit.py
```

`ModernLab.blend` uses metres. Export coordinates map Unity `(x,y,z)` to
Blender `(x,-z,y)`; Unity prefabs contain the FBX axis conversion on a visual
child beneath an identity root. Meshes are joined per module and material,
with applied geometry transforms, bevels, checked normals and two UV sets.
Unity regenerates secondary lightmap UVs on import. The runtime room is
assembled after startup and therefore does not claim baked illumination.

Runtime: `Assets/ChemistryLab/Art/ModernLab/ModernLabKit.fbx` and six PNGs.
Integration: `Chemistry Lab > Desktop > Integrate Modern Lab Art` creates the
materials/prefabs in `Assets/ChemistryLab/Resources/Art`. The Windows build
also executes this integration. `ModernLabArt` places models underneath the
existing gameplay anchors and hides replaced primitive renderers while
preserving simple colliders. Decorative imported meshes have no colliders.

This is project-owned original artwork, supplied for use and modification in
Chemistry Lab. There are no third-party attribution obligations for this kit.
