# Forest Archmage — first playable MVP

Open `ForestMvp.unity` and press Play, then **숲으로 출발**.
Unity menu: **Bounce Fit → MVP → Create or Open Playable Scene**.

## Controls

- Phone landscape: lower-left drag aims; lower-right drag moves. Both touches work independently. Attacks fire automatically. Aim is constrained to forward ±80°.
- Desktop: WASD moves. Move the mouse cursor over the battlefield to aim; no click is needed. Attacks fire automatically. Esc pauses.
- Camera/stage progress automatically. Collect XP close to the raccoon before the orbs scroll off the bottom.
- Balls reflect from side/far walls, enter recall at the rear wall, and follow the moving player. Returning balls do no damage; inventory becomes available only on arrival. There is no short projectile TTL.
- Level up pauses combat and offers three eligible cards. Basic attack uses one of four active slots; three passive slots.
- Fire Lv2 + Ice Lv2 enables a fusion card: two skills become one and free a slot.
- Normal run: boss enters at 6 minutes; kill it to win. The clock pauses during card choice and manual pause.

Editor/development buttons: XP +30, spawn boss, 45-second test run. These buttons and combat debug APIs are excluded from non-development players.

## Architecture and current limits

`ForestCombat` SubScene bakes the PoolContainer catalog and 11 combat prefabs. `MvpGame` waits for `EntityPoolSystem` prewarming, then `MvpCombat` rents entities in the default ECS World; movement is a Burst parallel job. Isolated tests/probes create a separate World with the same pool API. A Burst parallel narrow-phase job finds Ball/enemy and player contact candidates; Burst jobs generate wave and shooter firing plans. Damage, XP awards and entity-pool rent/return still run in entity order on the main thread. The job input/output arrays are reused across steps. SpriteRenderers are pooled. This is an ECS gameplay MVP, not completed mobile performance optimization. `MvpProgression` supplies run-local build stats; `MvpGame` freezes simulation during menus; `MvpHud` is uGUI with safe-area layout.

Enemies and balls are unchanged copies from Bounce-Defense. Raccoon and forest are primitive placeholders. Full production art, meta progression, persistence, audio polish and release services are not part of this slice. Existing framework/BootScene remains separate. The project default is the MVP 3D URP asset so ECS rendering systems initialize with SRP before scene Awake. The scene also restores its prior quality override when destroyed.

## Reproducible checks

- Test Runner → EditMode → `BounceFit.Mvp.Tests` (37 tests).
- In Play mode, **Bounce Fit → MVP → Run Play Smoke Checks** verifies menu callbacks, pause/card clock, victory/defeat and restart. It writes `Captures/MVP/play-smoke.txt`. It does not simulate physical touch or certify device usability.
- **Bounce Fit → MVP → Run Seeded Balance Probe** runs the actual simulation with a simple bot and writes seed-level results under `Captures/MVP/`. Bot results are regression evidence, not a substitute for human playtests.

The living design and provisional tuning values are in `docs/game-design/GAME_DESIGN.md`.

## 기능별 런타임 설정 (ScriptableObject)

씬의 `MvpGame.config`는 `Resources/MVP/DefaultConfig.asset`에 연결됩니다.
Unity 메뉴 **Bounce Fit → MVP → Configuration → Select Default Config**로 열고,
참조된 SO를 선택하여 Play 중 Inspector에서 수정하세요. 실제 파일은
`Assets/Projects/MVP/Configuration/`에 있습니다.

| SO | 주요 설정 |
| --- | --- |
| Ball | startingCount(기본 보유 개수), speed, returnSpeed, fireInterval, baseDamage, 반경·속성 효과 |
| Player | moveSpeed, maxHealth, hitRadius, contactRadius, invulnerabilitySeconds |
| Stage | scrollSpeed, 벽·이동 영역, bossAtSeconds, aimHalfAngle, 시뮬레이션 간격·개체 상한 |
| Enemy | 출현 간격·수량·체력, 일반/보스 탄막 속도·피해·패턴 |
| Experience | 구슬 개수·XP·확률, 자석 범위·속도, 획득 거리 |
| Progression | 레벨업 XP, 스킬/패시브 슬롯, 선택 카드 수, 강화·융합 조건 |
| Input | 모바일 스틱 반경·입력 영역·조준 데드존 |
| Presentation | 카메라 위치·각도·FOV, 표시 크기, 목표 FPS |

- 이동·Ball 속도·회수 속도·스크롤은 현재 런에 반영됩니다. 이미 날아가는 Ball 속도도 변경됩니다.
- startingCount는 기본 보유량이며 획득한 Ball 강화 횟수가 더해집니다. 개수를 낮춰도 비행 중인 Ball은 삭제하지 않고, 회수 이후 새 한도를 적용합니다.
- 최대 체력은 현재 잃은 체력을 보존하여 반영됩니다. 적 체력, 발사체 피해, 생성된 XP의 값 등 생성 시 결정되는 값은 다음 생성부터 적용됩니다. 진행 중인 쿨다운은 유지됩니다.
- 시작 위치·랜덤 시드와 루트 Config Set 교체는 다음 런부터 적용됩니다. 슬롯 한도를 낮춰도 이미 획득한 스킬은 제거하지 않습니다.
- Play 중 SO 에셋을 수정하면 Editor 종료 후에도 값이 남을 수 있습니다. 별도의 게임 저장 기능은 아닙니다. 원본 설정 보존이 필요하면 SO들을 복제한 세트를 씬에 할당하세요.
- **Run Live Config Checks** 메뉴는 실행 중 임시 SO로 실제 연결을 검증한 뒤 원래 설정으로 복구합니다.

검증: Burst 충돌·스폰 계획 전환 후 EditMode 37/37 통과. 실제 SubScene 베이크/대여/세션 반납 검사와 SO 런타임 변경 검사 6/6 통과. 빌드는 실행하지 않았다.


## SubScene 베이킹 및 엔티티 풀

- `ForestMvp.unity` → `ForestCombat.unity` SubScene → `EntityPoolAuthoring` → `Assets/Projects/Assets/Configuration/PoolContainer.asset`.
- 프리팹은 `Assets/Projects/MVP/Prefabs/`의 11종: 플레이어, 기본/불/얼음/번개/융합 Ball, 일반 적/슈터/보스, 적 탄, XP.
- PoolContainer의 `initialCapacity`는 미리 생성할 수, `maxCapacity`는 보유 상한입니다. 현재 상한 0은 필요 시 확장입니다. 풀 상한에 도달하면 해당 발사는 건너뜁니다. 이 구조 설정은 SubScene 로딩 시 적용되며, 전투 Config SO의 속도·피해량 등은 계속 런타임 조절할 수 있습니다.
- 회수 완료·처치·화면 이탈은 `EntityPool.Return`, 재시작은 `ReturnSession`을 사용합니다. `Disabled` 엔티티는 이동/충돌 쿼리에서 제외됩니다. 대여 시 체력·속도·감속·타격·회수 상태를 초기화합니다.
- 기본 Ball과 액티브 Ball 모두 `Ball.speed`, `Ball.fireInterval`을 사용합니다. 발사 간격은 공격속도 강화 배율을 함께 적용하며, 회수되지 않은 Ball을 중복 발사하지 않습니다.
- Editor Play에서 **Bounce Fit → MVP → Run Baked Pool Checks**로 실제 베이크된 프리팹 사용, 미리 생성된 엔티티 대여, 재시작 후 풀 재사용을 검사합니다.
- 렌더링은 기존 SpriteRenderer 풀을 사용하는 하이브리드 방식입니다. 피해 적용과 풀 대여·반납은 메인 스레드에 남아 있습니다. 모바일 기기에서의 CPU 프로파일링과 구조 변경의 Job/ECB 전환은 별도 작업입니다.

추가 검증: Play 시작·베이크 풀 검사·종료까지 Console 오류 0건. 기존 SRP 초기화/종료 오류는 프로젝트 기본 URP 지정으로 해소했다.

충돌 후보와 슈터 40개 동시 발사는 Burst의 병렬 경로로 검증했다. 실제 기기 FPS 개선 수치는 아직 측정하지 않았다.


## 설정값 보존

`Create or Open Playable Scene`, `Create and Connect Defaults`, `Connect Baked Combat Pools`는 이미 존재하는
Config Set, 기능별 SO, PoolContainer의 값을 다시 쓰지 않습니다. 기본값은 에셋이 처음 생성될 때만 채웁니다.
에디터 메뉴 **Bounce Fit → MVP → Configuration → Verify Reconnect Preserves Tuning**으로 현재 설정을
재연결한 뒤에도 값이 동일한지 검사할 수 있습니다. 이 검사는 실제 설정값을 바꾸지 않습니다.

## Modular raccoon v001
Generated from the approved crooked-staff concept; production provenance and prompts: `Sources/Assets/CHR_RaccoonArchmage_01/asset.json`. Twelve 512px RGBA sprites in `Resources/MVP/Characters/Raccoon`; three authored angles mirrored into five views. `Resources/MVP/CharacterPresentation.asset` controls anchors, scale, orb muzzle and procedural recoil live. Its editor creation command preserves an existing asset. Existing gameplay configuration values are not reset.
