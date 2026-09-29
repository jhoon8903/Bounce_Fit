# Raccoon archmage — production plan

## Integrated direction concepts
User selected ModularConcept_v005_CrookedStaff as basis and requested assembled angle views. Five individually generated concept candidates and a comparison slider are saved in directions_review_v001/. This is approval to explore directions, not acceptance of the resulting final geometry. See that folder README for observed size/angle/staff/hand-occlusion drift; production export remains blocked on correcting those inconsistencies. Do not treat nominal angle filenames as calibrated rotations.

## Crooked branch revision — ModularConcept v005 (current)
User prefers naturally curved/bent tree-like shaft over straight shaft. review/ModularConcept_v005_CrookedStaff.png is a built-in image_gen edit of v004: slender asymmetrically curved wood in both assembled and separated views, forest orb and round hand retained. Exact prompt recorded. Review board only; the separate depiction is not registered production geometry. Final part export, pivot/camera matching and alpha QA remain pending. No gameplay/SO/build changes.

## Forest orb revision — ModularConcept v004 (current)
User requested a slender staff shaft and large forest-inspired orb head. review/ModularConcept_v004_ForestOrb.png edits v003 with built-in image_gen: thin wooden shaft, oversized pale-green round orb, two broad leaf cradle shapes and simple sprout emblem; round hand retained. Same change on assembled and isolated weapon views. Only a review board, not separated production assets. Native output saved unaltered with exact prompt. Gameplay-scale leaf/emblem readability, alpha, pivots and five directions remain unvalidated. No gameplay/SO/build changes.

## Round hand revision — ModularConcept v003 (current)
User requested a simple circular hand that communicates gripping, with no detailed arm. Supersedes earlier no-hands restriction: no forearm/fingers, one round paw over the shaft at the robe-side grip. review/ModularConcept_v003_RoundHand.png depicts the round paw on both assembled character and weapon component; enlarged staff preserved. Built-in image_gen targeted edit of v002, one call. Exact prompt saved. Weapon and hand may move together in the concept; independent export/pivots and five-direction layering still need production validation. Review board only, no final engine assets or SO changes.

## Weapon revision — ModularConcept v002
User requested weapon contact with body and/or exaggeration because it is the Ball emission location, then explicitly requested resuming after an error. Generated review/ModularConcept_v002_LargeWeapon.png with built-in image_gen from v001. Staff shaft and orb enlarged; lower shaft touches/overlaps robe side; orb extends beyond head silhouette. No arms or hands. Cream review-board background replaces dark haze. Separate pictured parts remain conceptual, not exported production layers. Remaining soft gradients, precise camera/registration and gameplay launch-anchor alignment are not validated. Previous version retained; prompt recorded. No configuration SO, gameplay code or build changes.

## Modular non-pixel concept — latest user direction
User chose separate head/body/weapon/tail, no arms, exaggerated readable features instead of detail, cute tail motion and head recoil on Ball launch. User requested a concept excluding pixels. This supersedes earlier body + holding-hand plans: head includes ears, body includes tiny feet, weapon has no hand/arm, tail is independent.

review/ModularConcept_v001.png was generated once using built-in image_gen and v004 SD as continuity reference. Left assembled hero; right head/body/weapon/tail concept breakdown. This is a review board, not a usable sprite sheet or separately exported layers. Final production still requires individual complete parts, fixed pivots/overlap/sorting and five-direction consistency. Visible gradients and a brown halo/background differ from the requested flat treatment; reject these for final Build and preserve this file as a concept only. No code, configuration SO or build changes. Exact prompt saved in prompts/ModularConcept_v001.txt.

## Current proportions revision — v004 SD (supersedes v003)
User requested SD character design. MasterCandidate_v004_SD.png is a new review candidate generated with built-in image_gen using v003 as the edit reference. Oversized head, compact short robe/body, stubby limbs and shortened orb staff replace the previous taller proportions. Simple brown/blue/cream/mint identity and rear three-quarter direction retained. Candidate has visible soft gradients despite the flat-fill request; do not claim final palette compliance. Master approval, gameplay camera matching, five directions and staff-layer/muzzle registration remain pending. Previous candidates retained.

## Current style revision — v003 (supersedes v002)
User requested further simplification. MasterCandidate_v003.png removes cape/clasp/folds, reduces tail pattern to one band, and replaces ornamental crystal staff with a straight shaft and round mint orb. Thick outline and large color areas are the new candidate direction. Built-in image generation edited v002 once; previous versions retained. Some subtle gradients remain. Identity/style approval is pending; this is a review image, not a final directional sprite. Five directions, separate staff/muzzle and gameplay camera requirements remain unchanged.

## Current style revision — v002
User rejected the strong 3D material/rendering direction because it would require matching 3D-looking enemies and backgrounds. Supersedes the soft toy-like rendering below: casual illustrated 2D sprite, bold warm dark outlines, simplified color areas and cel shading, no fur/fabric/wood textures. Keep recognizable character, robe and staff. MasterCandidate_v002.png is the new identity/style review candidate, created with built-in image generation from v001. Directional production still awaits master approval. Some soft color gradients remain in v002; simplify further if required. Elevated gameplay camera, five-direction registration and split staff layers remain production requirements. Preserve v001 as history.

Updated 2026-09-28. User requested five directions, a wizard-robed character with Animal Crossing-like proportions, staff-tip Ball emission, and a visible aiming guide like Bounce-Defense.

## Identity and master
- New original tanuki/raccoon-dog forest archmage; rounded head and ears, compact body, warm brown fur, dark facial mask, short fluffy tail, soft toy-like 2.5D shading.
- Indigo wizard robe with simple cream/gold trim; exposed ears and recognizable animal silhouette; wooden staff with a pale mint crystal. Large simple details readable on mobile.
- First deliverable: one rear three-quarter full-body master candidate, viewed from above at roughly 45–48 degrees. Master approval is pending; do not produce directional derivatives before approval.
- Built-in image generation, one candidate call, no automatic retries or paid API fallback. Master candidate is kept in Sources, not imported as final production art.

## Directional production after master approval
- Five directions relative to stage forward: -80, -40, 0, +40, +80 degrees. Negative is the player's left. No front-facing pose.
- Five registered body sprites and five matching staff/holding-hand layers; do not mirror asymmetrical equipment. One combined character preview per direction may be assembled from approved layers.
- Final body/staff canvases: 512 x 512 RGBA PNG, sRGB. Fixed common canvas, scale, camera, lighting and foot pivot; approximately 80–88% full assembly height with at least 24 px safe margins. Do not independently trim directions or layers.
- Temporary pure magenta #FF00FF background for generated cutouts, removed and alpha-checked before Build. Palette excludes magenta. No baked ground shadow, glow halo, aiming line or projectile in body/staff art.
- Record foot pivot, hand grip and staff-tip muzzle anchor for each direction. Aim is continuous across the forward 160-degree arc, independent of the five visual direction bins. Re-evaluate sprite articulation against the actual perspective camera during integration.
- Unity destination: Assets/Projects/MVP/Art/Characters/RaccoonArchmage/.
- Filenames: CHR_RaccoonArchmage_01_Body_<direction>_v001.png and CHR_RaccoonArchmage_01_Staff_<direction>_v001.png. Preserve revisions.

## Supporting aiming/casting assets
- Staff-tip cast effect: separate from character art. Match the approved crystal design; generate a representative effect only after identity approval. Start with one 256 x 256 RGBA texture for a reusable particle effect, not a baked animation sheet.
- Aim guide: procedural dotted geometry/material, following Bounce-Defense TrajectoryPreview. It requires accurate trajectory and muzzle alignment rather than a painted line image. Reuse the analytic circle texture approach; share origin and collision/reflection rules with actual Ball launch.
- Use a common Muzzle anchor for Ball spawn, trajectory preview and cast effect. Perspective projection must be verified so the 2D simulation launch point and visible staff tip agree on screen.
- Existing Ball sprites remain available for the MVP. Any redesigned Balls form a separate asset set.

## Gates and constraints
- Master identity approval, then representative direction/layer and in-game alignment review, then remaining directions.
- Final output QA: alpha/key spill, shared registration, hand/staff occlusion, muzzle consistency, silhouette at mobile display size, and exact five-direction coverage.
- No game builds requested. Preserve all user-edited configuration ScriptableObject values. This plan does not authorize resetting or rebuilding those assets.

## Implementation references
- Bounce-Defense/Assets/Project/Prefabs/Char/Char.prefab
- Bounce-Defense/Assets/Project/Scripts/Game.Runtime/Combat/LaunchController.cs — Origin resolves launchOrigin.
- Bounce-Defense/Assets/Project/Scripts/Game.Runtime/Motor/TrajectoryPreview.cs — procedural dots and reflected path share launch origin/direction.
- Bounce-Defense/Assets/Project/Scripts/Game.Objects/Char/AnchorView.cs — staff aim rotation/recoil.
- Bounce-Defense/Assets/Project/Scripts/Game.Objects/Char/StaffView.cs — separate cast aura.
