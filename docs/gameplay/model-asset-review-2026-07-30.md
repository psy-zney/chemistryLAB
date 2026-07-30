# 3D model asset review — 2026-07-30

## Decision

Only the Erlenmeyer flask and test tube are approved for production use in
this revision. Their source pages and license are verified. The other files
remain in the original download folder and are not included in the game or
repository until their source licenses are supplied.

The approved production selection is recorded in
[`model-asset-selection.json`](model-asset-selection.json). The broader
candidate audit was temporary and is summarized below.

## Review matrix

| Candidate | Geometry | Scale/bounds | Decision | Reason |
|---|---:|---:|---|---|
| Erlenmeyer Flask GLB | 9,920 triangles | 0.088 × 0.132 × 0.088 m | Approved | Clean, correctly scaled, verified Pixabay license |
| Test Tube GLB | 2,880 triangles | 0.023 × 0.152 × 0.023 m | Approved | Clean, correctly scaled, verified Pixabay license |
| Hazmat Suit FBX | 12,280 triangles | 1.219 × 1.862 × 0.622 m | Hold | Geometry and human scale are suitable, but source/license are unverified and the supplied material setup is incomplete |
| Hotplate FBX (`3d-model.fbx`) | 1,550 triangles | 0.445 × 0.549 × 0.098 m | Hold | Efficient mesh, but source/license are unverified and the proportions need correction before replacing the procedural hotplate |
| Chemical Laboratory FBX | 400,420 triangles | 0.379 × 0.168 × 0.528 m | Reject as scene | 393 meshes, duplicate imported identifiers, discarded self-intersecting polygons, incorrect scale, no verified license |
| Chemistry Bottles GLB | 113,920 triangles | 6.272 × 2.993 × 3.093 m | Hold | Visually useful but too dense and incorrectly scaled; needs source/license, separation and LODs |
| Laboratory FBX | 1,092,218 triangles | 127.371 × 62.968 × 112.957 m | Reject | Excessive geometry, embedded camera/light, unusable import scale and no verified license |

## Production integration

- The reviewed GLB sources are archived under `SourceAssets/`. Their geometry
  was imported once and baked into native Unity mesh assets. The player build
  therefore does not require glTFast or include the sources' four embedded 2K
  textures.
- `ErlenmeyerFlaskMesh.asset` becomes a normalized tabletop prefab with a
  lightweight glass material and simple sphere/box colliders. It is the visible
  reaction vessel at the central bench and fume hood.
- `TestTubeMesh.asset` becomes a normalized prefab with the same lightweight
  glass material and a capsule collider, and is used in the bench rack.
- The reaction vessel keeps its existing parent `VesselInteractable`.
  Therefore the imported visual does not change the rule that chemistry can
  occur only after a sample is placed in a vessel on a bench or in the fume
  hood. Merely holding a chemical still cannot trigger a reaction.
- The build pipeline regenerates approved prefabs from the native meshes before
  creating the desktop scene, so a clean checkout has the same result as the
  reviewed workspace.

## Requirements before reconsidering held assets

1. Provide the original download page for each file.
2. Confirm a license that permits redistribution inside the game repository.
3. Keep author/source/license attribution with the asset.
4. For room packs, split furniture and architecture into reusable prefabs.
5. Add LODs and simple colliders; do not use the visual mesh as a room collider.
6. Normalize to metres and remove embedded cameras, lights, demo scenes and
   render-pipeline switch packages.

## Original procedural replacements

The held/rejected files are no longer needed for the current room. Their broad
equipment categories were used to define four independently built replacements:
the hotplate/stirrer, PPE suit display, reagent-bottle rack, and gas-wash train.
No geometry, texture, material, rig, or animation from an unverified download is
included. See
[`procedural-reference-props.md`](procedural-reference-props.md) and
[`procedural-reference-props.json`](procedural-reference-props.json).
