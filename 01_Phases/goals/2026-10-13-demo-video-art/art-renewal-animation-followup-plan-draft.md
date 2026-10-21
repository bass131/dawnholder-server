# 아트 전면 개선·애니메이션 후속 계획 초안

2026-10-22 · Art 리드 Sol · **사용자 검토용. 계획·예산·담당 승인 전 생성·구현·실행하지 않는다.** 원천은 Main `msg_88c76e6c8c1a`가 전달한 사용자 요청이며, 전달 원문은 [goal](goal.md#적용-중인-사용자-결정)에 보존한다. 현재 HUD의 재개 지점과 Content → World → Art 실행 순서는 유지한다. 이 초안의 조사에서는 파일·설치 패키지·공식 문서만 읽었고 ImageGen·Unity AI 생성·Unity 실행·MCP 연결·작업자 기동은 모두 0이다.

추천은 **HUD 뒤 마을의 타일·대장간·불꽃을 작은 시연 묶음으로 먼저 확인하고, 플레이어·적·보스·NPC의 전 동작 교체는 10-28 뒤 묶음별로 진행하는 안**이다. 먼저 요소마다 정지 기준 이미지 한 장을 만들고, 그 이미지의 형태·무기·색을 유지해 필요한 동작만 제작한다. 캐릭터를 새 Idle로 바꾸면서 나머지 공격·피격·사망은 옛 디자인으로 남기는 부분 교체는 피한다.

**10-22 08:4x KST 조건부 승인 보충:** Main `msg_d80b44cef580`가 사용자 A를 전달했다. 승인 원천은 이 문서의 commit27cea036/SHA256 a4e0ac3d…6fe7 버전이다. **HUD 완료 뒤 별도 branch/PR의 첫 마을 묶음만** Sol 베이스3회·Unity 시트최대2회·30 Unity Credits 이하로 승인됐다. 실패·취소/재시도도 센다. 생성 없는 견적이 상한/잔액 안인지 먼저 확인하며, 정지3장 사용자 방향 확인 뒤 시트로 간다. Unity 생성/다운로드/slice/.anim의 신규 Opus5.5 에셋 제작 세션은 이 묶음 한정 모델 예외이고 기동·MCP 좌석·창은 Main이 배정한다. 코드 쓰기는 없다. 전체29/51과 나머지 예비/대안은 미승인이다. HUD RED 창은 전용하지 않는다.

사용자 추가 원문 「대신에 기존 애니메이션 자료는 폐기하지말고 혹시 모르니까 남겨줘」를 **이번 묶음과 이후 전체 후속**에 적용한다. 기존 원화·시트·.anim·.controller는 기존 경로/GUID로 보존하고 덮어쓰기·삭제하지 않는다. 새 자료는 별도 경로·새 GUID로 추가하고 연결만 변경한다. 기존 controller를 수정하기보다 새 controller/override를 추가하는 방식을 먼저 검토하고 이전/새 연결을 보고서에 남겨 되돌릴 수 있게 한다. 크기/이음매/기능 문제가 남으면 기존 자료로 시연한다. 현재는 HUD 미완이므로 이 후속의 기동·생성·실행0이다.

## 1 - 시각 기준과 현재 사용처

기준은 기존 [새 콘셉트 계획](concept-reconstruction-plan-draft.md)의 4번 마을·6번 던전·7번 보스다. 따뜻한 광원, 짙은 목재·석재, 청색 깃발·태양 문장의 세계관을 잇되 UI 테두리를 월드 전체에 반복하지 않는다. 생성 그림 속 새 건물·적·스킬을 자동으로 게임 기능으로 추가하지 않는다. 같은 옆보기 시점·광원·그림자 방향과 캐릭터 비율을 정하고, 현재 `AnimatorDriver`의 좌우 반전 방식을 유지한다.

아래 경로는 `03_Client/Assets/` 기준이다. Town/HuntingGround/BossRoom 씬의 GUID 참조를 타일·프리팹·머티리얼까지 정적으로 따라 실제 텍스처 경로를 대조했다. 실행 화면이나 동적 로드 전체를 확인한 결과는 아니다.

| 구분 | 현재 근거 | 후속 제작 단위 / Idle 베이스 수 |
|---|---|---|
| 타일맵 | `Scenes/01.PlayArea/{Town,HuntingGround,BossRoom}.unity`의 `Tilemap_Solid`, `Tilemap_Platform`. 세 맵 모두 `Art/Environment/Others/Cainos/Pixel Art Platformer - Village Props/Texture/TX Tileset Ground.png`·`TX Village Props.png` 참조가 있다. `Art/Tiles/Platform/PlatformPlank.asset`도 현재 자산이다 | 마을 지면·발판, 사냥터 지면·발판, 보스방 지면·발판의 **테마 타일 세트 3개 → 3장**. 타일 한 셀마다 호출하는 뜻이 아니다 |
| 마을 구조물 | Town이 참조하는 `Art/Environment/Structure/Town/home.png`, `Blacksmith's shop0.png`, 위 `TX Village Props.png` | 주택, 대장간 외관, 마을 소품 세트 **3개 → 3장**. 잡화상 건물의 별도 현재 자산은 확인하지 못했으므로 새 상점 건축을 임의 추가하지 않는다 |
| 전투맵 구조물 | HuntingGround/BossRoom도 위 `TX Village Props.png`를 공유하며 BossRoom에는 `TX Chest Animation.png` 참조가 있다. `BG_obj`·`BG_spike`는 씬 이름이며 새 적/새 피해 규칙을 뜻하지 않는다 | 사냥터 폐허·장애물 외관 세트, 보스방 건축·장식 세트 **2개 → 2장**. 상호작용 상자는 아래 환경 항목으로 별도 계산 |
| 플레이어 | `Prefabs/Characters/ClassVisuals/{KnightVisual,MageVisual}.prefab` → `Art/Characters/Playable/{Knight,Mage}/`의 스프라이트와 controller | 기사·마법사 **2종 → 2장**. 로컬/원격은 같은 외형을 재사용해 중복 생성하지 않는다 |
| 적·보스 | `Resources/EnemyVisualTable.asset`의 Kind0/1/2 → `Prefabs/Enemies/{Enemy_Normal,Enemy_Boss,Enemy_Golem}.prefab`. 실제 원화/클립은 `Art/Enemy/{Slime,Boss_Vampire,Golem}/` | 슬라임·골렘·뱀파이어 보스 **3종 → 3장** |
| 현재 NPC | `Prefabs/Characters/{Npc_BlackSmith,Npc_Glocery}.prefab` → `Art/Characters/NPC/{BlackSmith,Glocery}/` | 대장장이·잡화상 **2종 → 2장** |
| 환경 동작 | `Prefabs/Portal/PortalSpin.anim`, 위 `TX Chest Animation.png`, 대장간 외관 | 포탈, 상자, 대장간 불꽃 **3개 → 3장**. 불꽃은 새 장식 레이어 제안이며 경제/피해 판정은 없다 |
| 전투 효과 | `Resources/Effects/`의 아래 목록. Mage 원화 하위 `Skill_Effect/`도 존재한다 | 효과 **11개 → 11장**. 움직이는 효과의 베이스는 Idle 대신 첫 자세·형상 기준도다 |

**베이스 합계: 타일 3 + 구조물 5 + 캐릭터 7 + 환경 3 + 효과 11 = 29요소·Sol image_gen 29회.** 이것은 논리적인 제작 요소 수이며 저장소 PNG 수나 프레임 수가 아니다. 세트 이미지에서 필요한 타일/장식을 나누는 작업도 후속 임포트 시간에 포함한다. 프레임 일관성을 위해 세트 분할이 불가능하면 추가 생성부터 하지 않고 수량을 다시 제시한다.

`Prefabs/Characters/NpcVillager.prefab`는 현재 기본 사각 스프라이트를 쓰는 후보이며 세 맵의 배치는 확인하지 못했다. 현재 7종 수에는 넣지 않는다. 실제 등장 용도를 사용자가 정하면 별도 베이스 1장과 Idle/Talk 2클립을 추가 제안한다. 기존 원경은 세 맵이 공유하는 `Art/Environment/BackGround/Parallax/CastleValley_Sunset_Parallax_Layers/Assets/Art/Backgrounds/CastleValley_Sunset/01_Sky_SunGlow.png`~`06_Valley_Fields_Ruins.png`를 우선 재사용한다. 원경 전면 재제작·새 NPC/맵 추가는 위 29장에 포함하지 않았다.

## 2 - 정지 베이스를 먼저 확정하는 순서

1. Sol 에셋 제작자가 요소별 정지 원PNG 한 장을 `image_gen`으로 만든다. 캐릭터·개별 소품은 투명 배경, 고정 옆보기 시점·기준 발 위치·완전한 무기/신체를 요청한다. 타일은 같은 셀 크기와 접합 경계를 설명한 테마 세트로 요청한다. 새 콘셉트 원본과 기존 실제 자산을 참조하며 글자·UI·불필요한 바닥 그림자는 넣지 않는다.
2. 실제 출력 크기·alpha·원본 경로·SHA256·호출 수를 남기고 사용자가 방향을 확인한다. 요청 해상도와 실제 출력 해상도가 같다고 가정하지 않는다. 한 장의 얼굴/장비/배색을 확정한 뒤에 동작 시트로 넘어간다.
3. 애니메이션 작업자는 해당 PNG를 동일한 첫 프레임 참조로 사용한다. 카메라 회전·turntable은 요청하지 않고 동작·좌우 방향·접지점만 바꾸게 한다. 베이스 수정 후 기존 시트를 몰래 재사용하지 않고 영향받는 동작을 표시한다.

첫 제작은 **마을 타일 세트·대장간 외관·불꽃의 3장**이다. 이후 플레이어 2장, 적/보스 3장, NPC 2장과 나머지 세트/효과를 묶음별로 확인한다. 전체 보정 예비 3회를 포함한 **Sol ImageGen 제안 상한은 32회(29+3)**다. 실패·취소·재시도도 호출로 기록한다. 현재 HUD용 잔여 1회와 하우징용 잔여 1회는 이 후속 예산에 합치거나 전용하지 않는다.

## 3 - 코드·현재 controller에서 뽑은 동작

원천은 `98_Shared/GameData/Enums/{AnimState,SkillId,EnemyKind}.cs`, `Combat/SkillCatalog.cs`, `Scripts/Rendering/AnimatorDriver.cs`, 실제 각 controller다. 기존 시각 상태는 Idle/Walk/Jump/Attack/Hit/Death이고 Channeling은 현재 로컬 마법사의 선예측 경로다. 이 계획으로 원격 채널링 wire·새 AnimState·새 스킬·피해 타이밍을 추가하지 않는다.

| 대상 | 필요한 몸 동작 / 현재 연결 근거 | 첫 시트 수 |
|---|---|---:|
| 기사 | Idle, Move, Jump_Start, Jump_Peek, Attack0, Attack1, Hit, Death. 현재 Knight.controller의 8개 동작을 같은 역할로 교체. `AttackVariant`의 시각 선택을 유지 | 8 |
| 마법사 | Idle, Move, Jump_Start, Jump_Peek, Cast_Immediately, Cast_Channeling, Hit, Death. 현재 Mage.controller의 8개 동작. 평타 시전/채널 준비와 실제 스킬 효과를 구분 | 8 |
| 슬라임 | Idle, Move, Attack, Hit, Death. 현재 `Art/Enemy/Slime/Animator/`의 클립 | 5 |
| 골렘 | Idle, Move, Attack, Hit, Death. 현재 `Art/Enemy/Golem/Animator/`의 클립 | 5 |
| 보스 | Idle, Walk, Stabbing_Start, Stabbing_End, Hit, Die. 현재 Boss_Animator.controller의 6개 동작. 준비와 서버 Strike 수신 뒤 찌르기를 분리 | 6 |
| 대장장이 | Idle, Talk, ForgeHammer 제안. 현재 controller는 Idle 중심이고 Talk 원화가 있다. 과거 무료 망치질 15프레임의 Unity 반영은 아직 별도 미완이며, 새 디자인 완료로 세지 않는다 | 3 |
| 잡화상 | Idle, Talk. 현재 Glocery.controller/클립에 대응 | 2 |
| **몸 동작 소계** | **7종** | **37** |

현재 유효 스킬은 **기사 Dash(Q), 마법사 Thunderbolt(Q)·Teleport(E)**다. 기사 E나 1~6/R 기술을 추가하지 않는다. Dash는 현재 이동/표현 경로와 아래 효과를 잇고 새로운 서버 애니메이션 상태를 요구하지 않는다. 마법사 스킬별 연출도 기존 채널/즉시 시전 몸 동작을 재사용할 수 있다.

효과 11개는 각각 정지 기준 1장·시트 1개로 잡는다. `Resources/Effects/Projectile.prefab`와 `RemoteProjectile.prefab`는 투사체 외형 하나를 공유 대상으로 보고 중복 생성하지 않는다. 나머지는 `ProjectileImpact.prefab`, `LightningStrike.prefab`(Thunderbolt), `DashSkill.prefab`, `DashHit.prefab`, `TeleportDepart.prefab`, `TeleportArrive.prefab`, `BossAttackPattern0.prefab`, `BossAttackPattern1.prefab`, `SlimeDamageEffect.prefab`, `GolemDamageEffect.prefab`다. 보스 두 패턴은 몸 클립을 무조건 두 벌 만드는 대신 현재 `BossAttackEffectSpawner`의 패턴별 효과 슬롯을 보존한다.

환경 시트는 **불꽃 반복·포탈 회전 반복·상자 열기 3개**다. 닫기는 같은 시트의 역순/정지 프레임 재사용을 먼저 검토한다. 지면·충돌 타일과 건물 전체를 흔들지 않는다. 바람 깃발·풀 흔들림·창문 불빛·물결은 실제 사용할 위치/요소를 정한 뒤 수량을 더하는 후순위 후보이며 현재 3개에 포함하지 않았다.

**전체 첫 시트 37+11+3 = 51개**, 보정 예비 5개를 포함한 Unity AI 제안 상한은 **56개 생성 작업**이다. 1시트=1유료 작업을 가정한 계획 수이며 실제 모델이 한 요청을 여러 유료 작업으로 나누면 견적을 다시 확인한다. 기본은 16프레임으로 시도하되 빠른 공격/피격은 필요한 프레임을 골라 기존 표현 길이에 맞춘다. 한 번에 모든 동작을 한 영상으로 만들어 품질을 보장한다고 가정하지 않는다.

## 4 - Unity AI·MCP 가능성: 설치 소스 확인과 미실행 구분

현재 manifest/lock은 Unity Editor **6000.6.4f1**, `com.unity.ai.assistant` **2.20.0-pre.2**, `com.unity.ai.inference` **2.6.1**, 2D Animation **16.0.1**, 2D Sprite **1.0.0**, Tilemap **1.0.0**, Tilemap Extras **9.0.1**다. 별도 generator 패키지명이 manifest에 없지만 Assistant 패키지 안에 Image/Animate/Generators.Tools/MCP 모듈이 있다. Inference를 시트 생성 패키지로 보고하지 않는다.

설치 위치는 `03_Client/Library/PackageCache/com.unity.ai.assistant@1b11b9bfd84a/`다.

| 확인 항목 | 읽은 소스와 결론 |
|---|---|
| 2D 생성 | `Editor/Assistant/AssetGenerators/Tools/GenerateAssetTool.cs`의 `Unity.AssetGeneration.GenerateAsset`, command `GenerateSpritesheet`. MCP availability는 Default이며 adapter가 Default 도구를 등록하는 코드가 있다. prompt와 Unity 안의 Texture2D reference가 필요하다. **2D 시트의 MCP 제공 경로는 설치 소스로 확인, 이 머신의 실제 연결/생성 성공은 미확인** |
| 3D와 구분 | `AssetGeneratorAnimate.cs`의 humanoid AnimationClip 생성/처리와 2D 시트는 다른 기능이다. 3D humanoid 모션을 2D 캐릭터 시트가 된 것으로 보고하지 않는다 |
| 프레임/출력 | `Modules/Unity.AI.Image/Services/Stores/States/SpritesheetSettingsState.cs`: 기본 열4×행4, 출력1024×1024. 기본 16셀일 때 셀당256×256. 이는 로컬 기본값이며 최대 프레임 수 보장은 아니다. `SpriteSheetExtensions.cs`는 영상에서 프레임을 골라 RGBA 텍스처/rect를 만든다 |
| 클립 변환 | `ConvertAssetTool.cs`의 `Unity.AssetGeneration.ConvertSpriteSheetToAnimationClip`은 사전 slice된 Texture2D를 .anim으로 바꾸는 비생성 작업이다. MCP Available 표기가 있다. SDK 기본 frameRate3.33/loopTime=true 경로가 있어 공격·피격·사망에 그대로 적용하지 않고 현 길이/반복 여부를 맞춰야 한다 |
| 해상도/피벗 | GenerateAsset에는 width/height·loop가 있으나 별도 frameCount/pivot 인자는 확인하지 못했다. Sprite Editor의 Multiple slice와 프레임별 접지점 정렬을 별도 작업으로 잡는다. 현재 Knight/BlackSmith PPU180, Mage190, importer 기본 pivot(0.5,0.5)을 확인했다. 새 PPU·피벗을 일괄 강제하지 않고 실제 발 위치/Anchor/기존 world 크기에 맞춘다 |
| 견적 | `Modules/Unity.AI.Generators.Tools/AssetGeneratorQuoteApi.cs`에는 생성 없이 견적을 받는 경로가 있고 GenerateSpritesheet를 다룬다. 코드상의 PointCost와 조직 청구 단위는 승인 뒤 실제 견적으로 대조한다. 현재 견적 API 호출·조직 잔액 열람은 하지 않았다 |

공식 Sprite Generator 설명도 참조 이미지에서 영상과 기본 4×4 시트를 만드는 흐름을 제시한다. 여기서 설명하는 영상/시트와 임포트 후 게임용 타이밍은 구분해야 한다. [Unity Sprite Generator 공식 설명](https://unity.com/blog/unity-ai-sprite-generator).

이 머신은 프로젝트 운영 R-5에 따라 **Claude 세션에만** 최초 실행 옵션 `--mcp-config C:/Users/bass1/.unity/claude-mcp.json`으로 연결하고 좌석은 1개다. 현재 Codex 리드에는 Unity 도구가 노출되지 않았다. 외부 제품의 일반 지원 범위가 이 머신의 연결 권한을 바꾸지 않는다. 공식 MCP도 Editor와 외부 에이전트를 잇는 기능이지만, 여기서는 Main이 좌석 소유 세션과 Unity 창을 배정해야 한다. [Unity MCP 공식 설명](https://unity.com/blog/unity-ai-mcp-how-to-get-started).

**담당 제안:** Sol은 정지 베이스/출처를 만들고, 승인된 에셋 묶음의 **신규 Claude Opus5.5 에셋 제작 세션 하나**가 MCP 연결·시트 생성·다운로드·slice/.anim 제작까지 맡는다. 코드 수정은 맡기지 않는다. 이 MCP 제작 역할과 쓰기 경로는 계획 승인 때 명시하며 현재 모델/권한 예외로 자동 인정하지 않는다. 구현·연결 코드가 필요하면 같은 묶음의 Sol 구현자가 맡고 PR 직전 검증자는 별도 신규 세션이다. 준비 전담·실행 전담·중간 실사 세션을 단계마다 덧붙이지 않는다. 실제 생산 시에는 가능하면 필요한 원PNG를 먼저 모아 한 Unity 창에서 처리하고 좌석을 반환한다.

승인 후 첫 실험은 불꽃 한 시트다. 연결 도구 목록·모델 지원·비생성 견적을 확인하고, 승인 상한 안에서만 생성한다. alpha/형태 유지·고정 카메라·프레임 순서·반복 이음매·발/앵커 위치를 원PNG와 프레임을 직접 보며 확인한다. 한 번의 보정에도 시트가 불안정하면 전체51개 발행으로 넘어가지 않는다.

대안은 **같은 Idle을 참조한 Sol image_gen 시트 1개 시험**이다. 정해진 셀 수·투명 배경·고정 시점으로 요청해 Unity에서 slice하며, 실패하면 기존 정상 애니메이션을 유지한 채 수작업 프레임 제작을 검토한다. 이 대안의 실제 성공도 아직 확인하지 않았다. 대안 전체 전환 시 기본29장+51시트=80회, 보정3+5를 더하면 **Sol 최대88호출**이므로 현재 제안32회에 자동 포함하지 않는다. 대안 1회/전체 전환은 비용을 다시 승인받는다.

## 5 - 생성 횟수·비용·시간 제안

| 항목 | 10-28 전 작은 시연 묶음 | 전체 후속 첫 제작 | 보정 포함 제안 상한 |
|---|---:|---:|---:|
| Sol 정지 베이스 | 3회 | 29회(앞3회 포함) | 32회 |
| Unity AI 시트 생성 | 불꽃1회 | 51회(앞1회 포함) | 56회 |
| 시연 묶음의 보정 | Unity 시트 최대1회 추가 | 위 예비5회 안에서 차감 | 기본1+보정1=2회 |
| 현재 실제 사용 | 0 | 0 | 승인 전 소비0 |

**추천 승인 단위는 시연 묶음부터**다. Sol 베이스3회와 Unity 시트 최대2회, Unity Credits **30 이하를 제안 상한**으로 두고 최초 비생성 견적이 상한을 넘으면 생성하지 않고 다시 판단한다. 30은 측정된 가격/조직 잔액이 아니라 사용자에게 제시하는 제한값이다. Sol 호출의 실제 크레딧·금액은 미산정이며 기존 Codex 작업 예약과 생성 도구 비용을 임의로 같은 단위로 환산하지 않는다.

전체 Unity 비용은 **Σ(실제 시트별 견적 51개)+승인된 보정(최대5개)+필요한 배경 제거 등 별도 작업 견적**이며 지금 총액은 미정이다. 공식 문서의 예시는 이미지1~6, 영상5~15, text-to-motion 약5 Unity Credits이며 고정 단가가 아니다. 특히 humanoid 모션의 약5를 2D 시트 가격으로 곱하지 않는다. 배경 제거/모델 변경/재생성도 무료로 가정하지 않는다. 생산 전에 모델·수량별 견적과 조직 청구/잔액을 확인해 전체 상한을 별도로 승인받는다. [Unity Credits 공식 문서](https://docs.unity.com/en-us/ai/credits/credits-about).

다음 시간은 실측이 없는 **계획용 작업량 추정**이다. 사용자 미관 검토·실행 창 대기·생성 서비스 지연은 별도이며 첫 불꽃 결과로 수정한다.

| 작업 묶음 | 새 베이스 / 시트 | 실작업 예상 | Main 배정 Unity 창 합계 | 일정 제안 |
|---|---:|---:|---:|---|
| 1 - 마을 시연 묶음·생성 경로 확인 | 3 / 1 | 5~8시간 | 2~3시간 | HUD 뒤 승인되면 10-28 전 후보. 기존 지형/스폰은 유지 |
| 2 - 두 플레이어와 기술 효과 | 9 / 23(몸16+효과7) | 14~22시간 | 5~8시간 | 10-28 뒤, 직업별 모든 동작을 한 묶음으로 교체 |
| 3 - 세 적·보스와 공격 효과 | 7 / 20(몸16+효과4) | 12~20시간 | 5~8시간 | 10-28 뒤, 서버 타격과 시각 준비/Strike 동기 보존 |
| 4 - 두 NPC | 2 / 5 | 5~8시간 | 2~3시간 | 10-28 뒤, 대화·대장간 경제 연결은 Content 경계 유지 |
| 5 - 나머지 두 테마 타일·네 구조물·두 환경 | 8 / 2 | 10~16시간 | 4~6시간 | 레벨 담당의 배치 계약 뒤, 10-28 이후 |
| **합계** | **29 / 51** | **46~74시간** | **18~28시간, 항상 직렬1개** | 약6~10작업일+대기. 완료 날짜 확약 없음 |

작업 묶음별로 PR을 나누고 각 PR 직전에 신규 독립 검증 한 번을 둔다. 시간에는 정상적인 임포트·프레임 정렬·현재 기능 자기 확인과 최종 검토를 포함하되 반복 재생성은 예비/추정 범위를 벗어날 수 있다. 10-28 전 묶음은 첫 실제 시트 품질·HUD 완료·공유 창 배정이 맞을 때만 넣는다. 시연 전에 픽셀 크기/접합/원근이 덜 맞거나 기능 회귀가 있으면 새 에셋 전면 반영을 강행하지 않고 기존 동작을 유지한다. 현재 HUD RED15분 창은 이 후속의 생성/임포트 창으로 쓰지 않는다.

## 6 - Art와 레벨 담당의 파일 경계

레벨디자인은 **사용자 결정 A로 개설된 Level 파트**가 계획한다(Main msg_740b5a5277e9, 2026-10-21T23:39:03Z 전달). 현재 리드는 [Level 리드 Sol]이며 지금은 글로만 범위·계획을 작성한다. 아래는 Art 측의 파일 경계 제안이고 Level 초안과의 대조/회신은 아직 대기다. 파트 개설을 제품·맵 데이터 쓰기나 새 실행 승인으로 확대하지 않는다.

| 책임 | 단독 작성 범위 초안 | 협업 경계 |
|---|---|---|
| Art | 원PNG·시트·PNG.meta의 sprite/임포트 설정·Material·clip/controller의 표현 부분·시각 prefab/장식. 타일 그림과 필요한 sprite/GUID를 Level에 전달 | 월드씬·Tile/Palette 직렬화·grid/cell/Collider·스폰·포탈·적 숫자는 Level 단독 작성으로 두고 Art가 쓰지 않는다. 새 그림이 충돌과 다르게 읽히지 않게 확인 |
| Level | `Scenes/01.PlayArea/{Town,HuntingGround,BossRoom}.unity`의 지형 셀·배치·충돌·스폰과 포탈 오브젝트, Tile.asset/Palette·씬의 sprite 참조, **98_Shared/GameData/Maps/map_0~2.{terrain,content}.bin의 단독 작성/재생성 주체** | Art의 외형 크기/발 위치·sprite/GUID·기존 이동/점프 성능을 입력으로 받는다. 세 월드씬과 Tile/Palette는 Level만 쓰며 `Scenes/99.UI/UI.unity`는 기존 Art HUD 범위다 |
| Core | 기존 이동/전투·서버 권위·맵 로더/포맷의 코드 주인. 판정 변경이 필요한 제안은 별도 계약 | Art/레벨이 예술 작업을 이유로 물리·판정·프로토콜을 바꾸지 않는다 |
| Content | NPC/길드/대장간/보상/퀘스트의 실제 상태와 소비 연결 | NPC Talk/불꽃/상자 애니메이션이 재화·보상·퀘스트 판정을 대신하지 않는다 |

현재 `Assets/Editor/TerrainBaker.cs`는 Solid/Platform 타일맵과 Spawn_Player/Spawn_Enemy_Normal·Boss·Golem 마커를 읽어 공유 terrain/content와 클라이언트 `Assets/StreamingAssets/Maps/map_{id}.terrain.bin`을 만든다. **레벨 담당은 씬+공유 bin+클라이언트 terrain 사본을 함께 갱신하고 terrain 두 벌의 byte 일치를 확인한다. content.bin은 현재 서버용으로 StreamingAssets에 복사하지 않는다.** Art는 이 bin을 쓰거나 bake하지 않는다. 포맷/서버 처리 변경은 Core에 넘긴다.

포탈 좌표·목적지·도착점의 현재 정본은 `02_Server/GameServer/Maps/PortalTable.cs`다. TerrainBaker에서 포탈 데이터를 만든다고 가정하지 않는다. Level이 월드씬의 PortalTrigger/오브젝트 배치와 이 테이블의 좌표·목적지 데이터 항목을 함께 계획하며, 승인된 후속 계약에서 단독 반영한다. 서버 근접/해금/이동 판정 로직·포맷 변경은 이 데이터 배치 권한에 포함하지 않고 코드 주인과 별도 조율한다. Art는 PortalTable을 쓰지 않는다.

레벨 담당에게 넘길 설계 질문은 세 가지다. 마을은 NPC/포탈의 읽기 쉬운 동선과 짧은 순환, 사냥터는 현재 점프/적 조합 안에서 높낮이·우회/직행·안전 지점을 섞는 흐름, 보스방은 공격 예고를 읽고 회피할 공간을 만드는 것이다. 실제 치수·충돌·스폰 수·포탈 위치는 레벨 담당이 제안하며 새 기믹/서버 규칙 추가와 구분한다. Art는 같은 셀/크기의 도안, pivot/접지선, 전경/배경 가림·가독성 자료를 전달한다.

## 7 - 이번에 필요한 사용자 결정과 다음 첫 일

1. **10-28 전 작은 묶음:** 위 Main 전달 A로 HUD 뒤 조건부 승인됐다. Sol3회·Unity최대2회·30Credits를 지키며 실제 견적/잔액과 정지3장 방향 확인·Main 기동/좌석/창을 순서대로 따른다. 같은 승인을 반복 요청하지 않는다.
2. **전체 후속:** 29베이스/51시트(예비 포함 Sol32회·Unity56작업)의 대상/수량을 검토할지. 전체 생산 비용 상한은 첫 실제 견적 뒤 별도 확정한다. Sol 시트 대안과 NpcVillager 추가는 이 수량에 자동 포함하지 않는다.
3. **역할·소유:** 단일 좌석의 Claude MCP 에셋 제작 역할/쓰기 경로와 Level·Art의 위 파일 경계를 승인할지. Level 파트 선택은 Main의 사용자 결정 A 전달로 해소됐고 두 초안의 경계 대조는 남아 있다.

현재 다음 일은 기존 HUD 재개다. 조건부 승인된 첫 묶음도 HUD 완료 뒤 별도 범위·브랜치/PR 계약과 Main 기동/창 배정에서 시작한다. 전체 후속은 사용자/메인의 계획 검토를 기다린다. 이 초안 작성으로 현재 HUD PR의 제품 범위나 생성 예산을 확대하지 않는다. 현재 파일만의 원문/수량 대조는 리드가 했으며 독립 검증·실제 Unity/MCP 생성 성공·사용자 미관 통과로 보고하지 않는다.