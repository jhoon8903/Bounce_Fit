# Stage 1 modular enemy assets v002
User-authorized scope: 15 separately swappable human/vehicle combinations and one structure boss. Wheel correction supersedes production_v001 (rejected).
Enemy layers: Rear (complete undercarriage, ALL wheels/tracks, backrest/floor); Rider (head, torso, small hands); Front (upper hood/front rim ONLY, NO wheels/tracks); Equipment (role attachment).
Exceptions: E13 rolling cylinder is Equipment, not duplicated in Rear. E04/E06/E09/E14 equipment sits behind rider (sort5).
Boss layers: Base (ALL tracked platforms), Core, LeftTurret, RightTurret. No wheels/tracks in boss tower layers.
Output: Assets/Projects/MVP/Art/Enemies/Stage1/<ID>/<Part>.png. Enemy512 square RGBA, boss1024 square RGBA, PPU256 centered pivot, full-rect, bilinear, clamp, no mipmaps. Shared registered canvas per unit; layer pixels deliberately offset on common canvas so zero-transform overlay works. Fixed bounds override tight independent trimming.
Prefabs: Assets/Projects/MVP/Prefabs/Enemies/Stage1. Root SortingGroup, independent SpriteRenderer children. Presentation-only; no gameplay spawn mapping, AI, combat collision or boss phase behavior changed.
Camera: one fixed elevated front-right three-quarter view, fronts facing lower left on sprite. These are 2D billboard art, not rotatable3D. Different camera yaw requires another source direction.
Generation: built-in imagegen, one per unit, approved roster references. Exact source PNG and prompts in production_v002. Prompt asks magenta chroma; actual tool returned native RGBA, which is preserved without re-keying. No code redrawing. Deterministic part isolation, uniform rescale within each layer, positioning and compositing recorded in layer_recipes.json.
Assembly changes source parts only through documented registration. Physics target shapes in footprints.json are design metadata pending world/visual alignment, NOT implemented colliders.
Existing SO values must remain unchanged. No game build.

