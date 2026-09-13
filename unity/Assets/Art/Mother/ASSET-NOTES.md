# r56 asset provenance

Body, eyes, eyebrows, eyelashes, clothing base, and rig derive from the existing MakeHuman/MPFB CC0 assets already recorded in `../final-mother/ASSET-NOTES.md`.

No new external visual assets were added for r56. The broad long-hair geometry, functional breathing/mouth/brow shapes, animation processing, and simple bottle inspection prop are authored in this workspace. The bottle is part of the review scenes, not the character FBX triangle total.

The adopted direction references are `../parent-model-sheet-v4-e2-long-cute-30s.png` and `../parent-hand-sheet-v2.png`.

The character FBX is a candidate. Unity Editor, Humanoid retargeting, Android hardware, and runtime performance have not been tested.

## r56-seat update

The seated variant reuses the once-assembled r56 mesh. Pose actions and functional eyelid/eye clearance were authored locally; no external assets were added. Neutral ceiling/futon verification surfaces are authored in the review scenes and excluded from the character FBX. User-approved differences from the original r56 prompt: seated face reference height approximately 0.80 m, and the ceiling approaches as the camera rises during carrying. Unity Editor, Humanoid retargeting, Android and performance remain untested.

## Additional approved scene changes

The mother moves 0.25 m toward the baby’s feet. Diaper care ends with a separate event camera looking at the verification futon; its surface and the mother settle by 0.04 m. These review-scene controls are not external assets. All previous candidates are retained.

## r57
The editable source is astra-reference-r56-approved.blend; mesh, shape geometry and all original R56 actions are retained unchanged. Runtime materials and normalized meshes reuse the approved runtime asset. New animation and the opaque Prop_Bottle / Prop_Cloth meshes are authored locally, with no new external assets. The simple window rectangle is only a review reference.

## Upstream provenance (copied from scratch/visual/parent/final-mother/ASSET-NOTES.md)

- Character body, morph targets, eyes, eyebrows, eyelashes, hair, skin, clothing base, and Mixamo Unity rig are derived from official MakeHuman/MPFB assets released under CC0 1.0.
- Source: https://github.com/makehumancommunity/mpfb2
- Official asset license: https://github.com/makehumancommunity/mpfb2/blob/master/LICENSE.ASSETS.md
- Room, bottle, phone, ceiling texture (room-r1) and UI parts / icon (ui-r1) are self-authored. Fonts: Zen Maru Gothic, SIL OFL 1.1 (unity/Assets/Resources/Ui/OFL.txt).
