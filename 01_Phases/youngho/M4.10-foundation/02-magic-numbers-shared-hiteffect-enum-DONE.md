---
owner: youngho
phase: 02
status: done
grade: 복잡
summary: 서버 전용 매직넘버(히트박스 0.5f·de-aggro 1.5f·epsilon 0.05f)를 CombatConstants 단일화 + HitEffect enum 신설(wire 불변) + EnemyDefaultHp 배열 폐기(HP 단일 출처 Formulas). 거동 불변·회귀 0.
completed: 2026-06-11
---

# Phase 02 완료 — 매직넘버 단일화 + HitEffect enum

> M4.10 두 번째 Phase. 거동 불변·동치 교체(값 한 톨도 안 바뀜 = 리팩토링, 튜닝 아님). 회귀 0 달성.

---

## TL;DR

흩어진 서버 전용 게임플레이 매직넘버를 **단일 출처**로 모으고, hitEffect raw byte를 **enum**으로 승격했다. 값은 전부 보존(회귀 0).

**산출물**:
- `98_Shared/GameData/HitEffect.cs` 신설 — `enum HitEffect : byte { Melee=0, Projectile=1, Lightning=2, Dash=3 }` (SkillId 동형, wire byte 불변).
- `02_Server/.../Combat/CombatConstants.cs` — **사용처별 9그룹 정리**(`//──` 헤더, `#region` 미사용) + 신규 3상수 `HitboxHalfExtent=0.5f` / `DeAggroHysteresis=1.5f` / `VelocityEpsilon=0.05f`.
- 서버 매직넘버 사용처(EnemyEntity/BossStates/EnemyStates/PlayerCombatStates) → 상수 참조.
- `GameMap.cs` — `EnemyDefaultHp` 배열{30,100,60} **폐기** → HP 단일 출처 = `EnemyStats.{Normal,Boss,Golem}Default().MaxHp`.
- 서버 hitEffect(CombatSystem/SkillSystem) + 클라 hitEffect(ClientPacketHandlers) → `HitEffect` enum.
- 클라 `Rendering/MotionConstants.cs` 신설 — FacingEpsilon 0.001f 단일화(3 Motion 클래스 참조).
- 테스트 — PacketRoundTrip `[InlineData((byte)3)]`(Dash wire 회귀) + 리터럴→상수 참조.

---

## AC 검증 결과

| 완료조건 | 검증 | 결과 |
|---|---|---|
| dotnet test green (값 불변) | unit 523 passed / 0 failed (integration flaky 1 제외) | ✅ |
| 각 매직넘버 단일 출처 | 0.5/1.5/0.05 각각 CombatConstants 1상수, 리터럴 잔존 0 | ✅ |
| EnemyDefaultHp 배열 폐기 | HP = Formulas factory MaxHp 단일 | ✅ |
| HitEffect enum 양쪽 사용 | 서버 송신 `(byte)HitEffect.X` + 클라 수신 `(HitEffect)pkt.hitEffect` | ✅ |
| ProtocolVersion 11 불변 | PacketRoundTrip(0/1/2/3 byte 왕복) + `ProtocolVersion_Is11()` assert | ✅ |
| Shared.dll→Plugins + Unity error CS 0 | 자동 복사 + 콘솔 0 + scriptCompilationFailed=False | ✅ |
| reviewer 통과 | 🔴 0개 (회귀 직결 3축 wire/값동치/금지구역 전수 통과) | ✅ |

**빌드/테스트** (WSL2):
```
dotnet build Dawnholder.slnx → Build succeeded, 0 Error
dotnet test --filter "!~Integration" → Passed! 523, Failed 0, Skipped 0
남은 1 실패 = LagSimIntegrationTests (memory flaky, 거동 회귀 아님)
```
**Unity** (MCP): 콘솔 error CS 0 + `scriptCompilationFailed=False`.

---

## 결정 흐름

1. **상수 위치 = CombatConstants(02_Server), HitEffect enum만 98_Shared** (사용자 의논 2회).
   plan은 "98_Shared 단일화"라 가정했으나 실측상 `CombatConstants`가 02_Server에 있고, 히트박스/de-aggro/epsilon은 **클라가 한 번도 안 쓰는 서버 전용**. §1.2 콘텐츠/엔진 분리 + §0.3 YAGNI + Shared.dll 재빌드 파급 회피 → CombatConstants가 정합. plan "단일화"의 진의는 SSOT(폴더 아님)라 CombatConstants도 충족. HitEffect만 `S_HitResult`로 양쪽 가는 진짜 공유 → 98_Shared.
   추가 요청: CombatConstants를 사용처별 9그룹으로 정리(영호 비상 가독성 니즈).

2. **금지 구역 회피** — Explore가 "클라 epsilon 0.0001f를 0.05f로 통일", "PlayerPredictor GroundEpsilon 참조"를 권고했으나, 그 두 파일(`LocalPlayerMovement`/`PlayerPredictor`)은 work-pin **절대 금지**(force-adopt 심장부). 클라 0.0001f는 서버 0.05f와 *의도적으로 다른 값*(주석 근거). → 클라 동기화 코어 제외, 서버 매직넘버만.

3. **Enum.IsDefined 타입 버그 봉합** — server Worker가 EnemyDefaultHp 배열 폐기 시 `ByKind.Length` 범위검증을 `Enum.IsDefined(typeof(EnemyKind), (int)KindId)`로 교체했는데, `EnemyKind : byte`라 int 전달 시 `ArgumentException`(underlying type 불일치) → GameMap ctor가 모든 spawn에서 throw → **156 테스트 연쇄 실패**. `(int)` 제거(byte 그대로) 1줄 수정으로 봉합 → 156→0. (Enum.IsDefined 자체는 기존 길이검증과 거동 동치이며 더 정확.)

4. **DeferredDamageSystem 미변경 정합** — `impact.HitEffect`가 byte 타입이라 이미 byte 파이프, 교체 불필요.

---

## 학습 일지 후보 키워드

- **`Enum.IsDefined(Type, object)` underlying-type 함정**: enum이 `: byte` 베이스면 `(int)` 캐스팅 값을 넘기면 `ArgumentException` — value 타입이 underlying type(byte)과 정확히 일치해야 한다. byte 그대로 넘기거나 `(EnemyKind)` 캐스팅. 빌드는 통과하고 런타임에 터져 대량 연쇄 실패로 나타남(테스트가 잡음).
- **SSOT ≠ 특정 폴더**: "98_Shared 단일화"의 진의는 단일 출처지 물리적 위치가 아니다. 서버 전용 규칙은 서버 상수 클래스(CombatConstants)에 모아도 SSOT 달성 + §1.2 정합 + 빌드 파급 회피.
- **self-referential 테스트의 역할 전환**(reviewer 통찰): 테스트 리터럴을 production과 같은 상수로 바꾸면 "값 보존" 검증력이 사라지고(둘 다 같은 출처 참조) "구조 정합" 검증으로 바뀐다. 진짜 회귀 방지는 PacketRoundTrip `InlineData((byte)3)` 같은 *바깥에서 박은 고정 기댓값*이 담당.
- **wire 불변 enum 승격**: C# 타입을 byte→enum으로 올려도 직렬화 결과(byte 1칸)가 같으면 ProtocolVersion bump 불필요. 패킷 필드는 byte 유지, 송신 `(byte)`, 수신 `(Enum)` 캐스팅.

---

## 후속 후보 (이번 범위 밖 — reviewer 🟡)

- `DeferredImpact.HitEffect` 프로퍼티를 byte→HitEffect enum 승격하면 `HitEffect = (byte)HitEffect.Projectile`의 식별자 중복 가독성 함정 해소(DeferredDamageSystem.cs:58,94). Phase 02 동치 범위 초과 → 후속 리팩토링.
- GameMap kind→MaxHp switch를 `EnemyStats.DefaultFor(EnemyKind)` factory dispatch로 (YAGNI, Rule of Three 전까지 보류).
