# Cross-Review by Codex β — 2026-05-23 — M4.1 Phase 01

> 입력: `00_Document/reviews/2026-05-23-pre-m4-cross-review-claude.md`  
> 관점: Claude α 발견 22건의 외부 대조 + ARCHITECTURE.md M4 사전 과제 8건 외 하드코딩/false-promise 잔재 점검.

---

## 1. α 발견 22건 점검

**판정**: α 22건의 큰 분류는 대체로 정합. 단, C1/C2는 "M3.8 봉합 완료"라고 통째로 닫기보다 **host 전달 봉합 완료 / port·timeout 외화는 잔여**로 쪼개는 편이 정확하다.

| 축 | β 판정 | 근거 |
|---|---:|---|
| 정합 | 18건 | S1~S7, C3~C11, T1/T2의 이관 방향은 현재 plan/M4 backlog와 대체로 맞음 |
| 보완 필요 | 4건 | C1/C2는 host만 PlayerPrefs로 전달됨, Sh1은 Constants 외 Physics도 같은 설계 상수 계열, C12는 "수정 불필요"보다 "서버 hitbox와 별개"라고 표현하는 게 안전 |
| α 외 추가 | 별도 4건 | 아래 §2 참조 |

### α 분류 보정

- **C1/C2 M3.8 봉합 완료 표기**: 절반만 맞음. `NetworkBootstrap`은 `ServerHost`만 PlayerPrefs에서 읽고, `serverPort=7777`은 그대로 Inspector/default 값이다. `MainMenuController`도 성공 시 `ServerHost`만 저장한다. 따라서 "Hamachi IP 입력 → Gameplay 연결"은 봉합 완료지만, port/timeout까지 config화됐다고 읽히면 과장이다.
- **M3.8 PlayerStats 박힘**: `GameSession._stats`와 `PlayerStats.Warrior/Ranger()` factory는 실제로 박혀 있다. 다만 `GameMap.AddPlayer()`는 아직 `PlayerEntity`에 stats를 연결하지 않고, `PlayerEntity.Hp=100/MaxHp=100`이 남아 있다. 이는 α S2/S3/S4처럼 **M4.1 Phase 02 흡수**가 정확하다.
- **Sh1 설계 의도 박힘**: `98_Shared/GameData/Constants.cs`는 설계 의도 박힘으로 보는 데 동의. 추가로 `Physics.cs`의 `Gravity/JumpSpeed/GroundY`도 같은 성격의 shared gameplay constants다. runtime config 후보가 아니라 tuning/table 후보에 가깝다.
- **C12 LocalPlayer prefab collider/Rigidbody2D**: 서버 권위 hitbox와 직접 연결하지 않는 한 즉시 수정 불필요. Phase 03 hitbox는 Unity collider를 신뢰하지 말고 서버 side shape로 박는 게 정합.

---

## 2. β만 잡은 보완 발견

| ID | 위치 | 박힌 값/패턴 | β 분류 | 비고 |
|---|---|---|---|---|
| B1 | `03_Client/Assets/Scripts/Input/LocalPlayerController.cs:72` | `const float AttackRangeSq = 9.0f` | **즉시 봉합(M4.1 Phase 03)** | 서버 `CombatConstants.AttackRangeSquared`와 중복. 클라는 target 추천만 하지만, Phase 03 AABB 전환 시 drift가 UX miss로 바로 보일 수 있음 |
| B2 | `02_Server/GameServer/Program.cs:13` | listen port `7777` | **M5+ 또는 별 시점** | M3.8 Hamachi 검증은 7777 고정으로 통과. 클라우드/운영 진입 때 appsettings/env로 이동하면 됨 |
| B3 | `98_Shared/CLAUDE.md:19`, `00_Document/ARCHITECTURE.md:213` | ProtocolVersion `Current=3` 문서 잔재, 실제 코드 `Current=4` | **즉시 봉합(문서 sweep)** | 코드 하드코딩은 아니지만 false-promise 계열. M4.1 Phase 03의 4→5 bump 때 같이 정정 권장 |
| B4 | `03_Client/Assets/Scripts/UI/HudController.cs:28~30` | mock HP/Gold `100/100/0` | **M4.3 또는 별 시점** | M1/M3 UI skeleton 잔재. 현재 combat은 enemy HP만 표시하므로 plan 변경감은 아님 |

**β 결론**: 새로 잡힌 "예상 못한" 실질 결함은 B1이 제일 중요하다. 나머지는 config/doc/UI skeleton 성격이라 M4.1 재구성 트리거는 아니다.

---

## 3. plan 변경 트리거 판정

**β도 α의 "plan 변경 X" 권유에 동의.**

다만 "아무 것도 안 바꿈"보다는 아래 두 체크만 M4.1 Phase 02/03 완료조건에 살짝 흡수하면 좋다.

1. Phase 03에서 `LocalPlayerController.AttackRangeSq` 제거 또는 shared/client combat hint 상수로 연결.
2. Phase 03 ProtocolVersion 4→5 bump 시 `98_Shared/CLAUDE.md`와 `ARCHITECTURE.md`의 v3 잔재 sweep.

이 둘은 새 Phase를 만들 정도가 아니라, 이미 예정된 Phase 03의 client send 정합/PDL bump 정합에 같이 들어갈 수 있다.

---

## 4. Phase 03 hitbox: AABB vs capsule

**β 의견: M4.1은 AABB 권장, capsule은 M4.3 backlog 권장.**

이유:
- 현재 `C_Attack`은 `targetEntityId`만 있고, 서버 권위 facing/attack arc가 아직 없다. capsule을 제대로 쓰려면 방향, vertical tolerance, target history, jump Y mispredict까지 같이 건드리게 된다.
- AABB는 `attacker attack box intersects target bounds`로 단위 테스트가 쉽고, 기존 `dist² < range²`보다 한 단계 명확한 승격이다.
- capsule은 점프/대각/둥근 몸체 판정 체감이 좋지만, M4.1의 목표인 lag compensation + first precision hitbox에는 scope가 무겁다.

권장 구현 감:
- 서버에 `Hitbox/Aabb` 순수 구조를 두고, Unity `BoxCollider2D` 값은 신뢰하지 않는다.
- `CombatConstants.AttackRange`는 바로 제거하지 말고 AABB half extent 입력으로 남긴다.
- 클라 target 추천은 서버 판정보다 같거나 약간 넓은 후보 검색으로 두고, 최종 성공/실패는 서버가 결정한다.

---

## 5. 종합 4 분류 표 (α + β)

| 분류 | 항목 | 처리 |
|---|---:|---|
| **즉시 봉합 / M4.1 Phase 02·03 흡수** | S2, S3, S4, S5 일부(BaseDamage/AttackRange), **B1**, **B3(문서 sweep)** | Phase 02: PlayerStats/Formulas 연결. Phase 03: AABB/lag comp + client attack range drift 제거 + ProtocolVersion 문서 정합 |
| **M4.2 이관** | S1, C3, C6, C7, C9, S5 일부(map/sorting/config 관련) | 4맵 분리, map data sync, sorting layer/connection config 정리 때 흡수 |
| **M4.3 이관** | C4, C5, C8, C10, C11, **B4** | enemy asset/prefab, UI theme/layout, player HUD authority 표시 정리 |
| **M5+ 또는 별 시점** | S6, T2, **B2**, T1 유지, Sh1/Physics shared constants 유지 | Serilog/runtime config/cloud deploy, bot 옵션 외화, shared constants tuning/table화 |

---

## 6. 최종 결론

- α의 핵심 결론인 **"진짜 plan 재구성 트리거 X"**에 동의한다.
- 단, α가 놓친 `LocalPlayerController.AttackRangeSq = 9.0f`는 Phase 03에서 반드시 같이 봉합해야 한다. 서버 hitbox를 AABB로 바꿔도 클라 target 추천이 옛 원형 range를 붙들고 있으면 데모 체감이 어긋날 수 있다.
- `Constants.cs`는 설계 의도 박힘으로 봐도 된다. 다만 shared 쪽 문서의 ProtocolVersion stale은 M4.1 Phase 03 bump 때 놓치면 false-promise 계열로 재발한다.
