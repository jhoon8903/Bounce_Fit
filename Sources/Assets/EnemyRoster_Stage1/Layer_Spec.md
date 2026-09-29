# Layer/pose contract
Rear0 → Rider10 → Front20 → Equipment30 by default. E04/E06/E09/E14 use Rear0 → Equipment5 → Rider10 → Front20. Boss Base0 → Core10 → LeftTurret20/RightTurret20.
The word Rear denotes draw order, not rear half of undercarriage. Entire wheel set lives there.
One static pose per layer. Projectile recoil/motion can transform children later; no authored animation frames supplied.
Normalized PNG pivot (.5,.5). All prefab children local(0,0,0) scale1: pixels carry calibrated alignment. Root lies at canvas center, NOT ground contact. In XZ game orient root to view camera rotation and place/scale from visual ground anchor; gameplay integration is a separate step.
Swapping Rider preserves vehicle layers, but different portrait proportions may require small per-instance local offsets. Do not independently crop aligned PNGs.
Inspect assembled and individual part previews on contrasting backgrounds. Reject face obstruction, duplicated wheel/track volume, other-layer fragments, cut edges, absent alpha, or mismatched source/canvas registration.

