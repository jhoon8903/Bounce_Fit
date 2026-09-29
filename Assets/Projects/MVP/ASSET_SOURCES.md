# MVP asset provenance

Images are unchanged copies from `/Volumes/Dock_SSD/Projects/Bounce-Defense/Assets/Project/Sources/GameTextures/`, reused at the user's request on 2026-09-28. Their original TextureImporter metadata is preserved; no raster generation, recoloring, cropping, or reslicing was performed.

- `Ball/*.png` (6 files) → `Resources/MVP/Sprites/`
- `Monster/Mob/*.png` (4 files) → `Resources/MVP/Sprites/`
- `Assets/TextMesh Pro/Fonts/PretendardVariable.ttf` → `Resources/MVP/PretendardVariable.ttf`

Original source project remains authoritative for asset licenses/provenance. These monster sprites are temporary representations of human invaders. Raccoon and forest objects are code-created Unity primitives in `MvpGame.cs`. Final directional character art is not included in this MVP.

## Modular raccoon v001
Generated from the approved crooked-staff concept; production provenance and prompts: `Sources/Assets/CHR_RaccoonArchmage_01/asset.json`. Twelve 512px RGBA sprites in `Resources/MVP/Characters/Raccoon`; three authored angles mirrored into five views. `Resources/MVP/CharacterPresentation.asset` controls anchors, scale, orb muzzle and procedural recoil live. Its editor creation command preserves an existing asset. Existing gameplay configuration values are not reset.
