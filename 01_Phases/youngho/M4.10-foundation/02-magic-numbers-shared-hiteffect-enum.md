---
owner: youngho
phase: 02
status: done
grade: 복잡
summary: 흩어진 게임플레이 매직넘버(히트박스 0.5f·de-aggro 1.5f·epsilon 등)를 98_Shared 단일 출처로 + HitEffect enum 신설(wire 무변경)
---

# Phase 02: 흩어진 매직넘버 98_Shared 단일화 + HitEffect enum

> **상태**: done
> **마일스톤**: M4.10
> **등급**: 복잡 (shared + server 2 도메인)
> **담당**: shared + server Worker(Sonnet)
> **의존**: Phase 01 (컨벤션 v6 §2.5 DRY + 단일 진실 기준)

---

## 🎯 목표

전수조사가 잡은 **흩어진 게임플레이 매직 넘버**를 `98_Shared` 단일 출처로 모은다. 같은 값이 production 3~5곳 + 테스트에 리터럴로 박혀 있어, 밸런스 튜닝 시 한 곳만 고치면 나머지가 stale해진다(단일 진실 부재). 동시에 raw byte로 비교되던 `hitEffect`(0/1/2/3)를 `98_Shared`의 **`HitEffect` enum**으로 승격해, 런타임 의미가 코드에 박히게 한다. 이 Phase가 끝나면 **각 매직넘버가 단 하나의 명명 상수에서 나오고, hitEffect는 enum으로 비교**된다 — 값은 한 톨도 안 바뀌므로 거동 불변·회귀 0이 목표다.

---

## ⏪ 사전 조건

- [ ] Phase 01 완료 — 컨벤션 v6 §2.5(DRY: 데이터 소유 객체로 추출) + 단일 진실 기준 박힘
- [ ] 전수조사 output의 `rootCauses` 중 "전투 검증·변환 보일러플레이트"(hitEffect enum 부재) + "흩어진 게임플레이 매직 넘버" 섹션에서 정확한 file:line 확인
- [ ] 현 `98_Shared/GameData/`(CombatConstants/Physics/Formulas) 구조 파악 — 새 상수가 어디 들어갈지

---

## 📝 작업 내용

> shared(상수·enum 신설) → server(리터럴을 상수 참조로 교체) 순. 단일 진실의 뿌리가 shared.

**shared (98_Shared/GameData + 98_Shared/Protocol)**:
- [ ] **히트박스 half-extent 0.5f** — `CombatConstants.EntityHitboxHalfExtent = 0.5f`(또는 동형) 신설. 현재 `EnemyEntity.Hitbox`(L31), `BossStates.ApplyBossAttack`(L45 playerBox), `HitboxTests`에 산재. PlayerEntity엔 Hitbox 프로퍼티 자체가 없어 BossStates에 인라인으로만 박힘 — 상수로 봉인
- [ ] **de-aggro 히스테리시스 1.5f** — `CombatConstants.DeAggroHysteresis = 1.5f` 신설. 현재 `EnemyStates.ResolveAfterHit`(L33), `EnemyStates.ChaseState.Tick`(L145), `BossStates.BossMoveState.Tick`(L151) 3 production + 테스트 2곳 = 5곳 산재
- [ ] **속도 near-zero epsilon 0.05f** — `CombatConstants.VelocityEpsilon = 0.05f` 신설. 현재 `PlayerCombatStates.AttackState.Tick`(L40), `HitState.Tick`(L76), `EnemyStates.EnemyHitState`(L174) 3곳 (지수 감쇠 종료 판정)
- [ ] **FacingEpsilon 0.001f** — 클라 3개 Motion 클래스 산재 → 단일 상수(클라 공유 상수 위치 또는 98_Shared)
- [ ] **GroundEpsilon 0.0001f 인라인** — `Physics.AtRest`가 정의한 `GroundEpsilon` 상수가 있는데 다른 곳은 `0.0001f`를 인라인으로 박음 → 인라인을 `GroundEpsilon` 참조로 교체
- [ ] **EnemyDefaultHp 이중관리 제거** — `GameMap` 내 `EnemyDefaultHp.ByKind = {30,100,60}` 배열(Normal/Boss/Golem)이 `EnemyStats` factory의 `MaxHp`와 숫자 이중관리(주석으로 "일치 의무"만, 컴파일 강제 없음). 배열을 폐기하고 `SpawnEnemy`가 `EnemyStats` factory `MaxHp`를 직접 읽도록 → HP 단일 진실을 EnemyStats에만
- [ ] **HitEffect enum 신설** — `98_Shared`에 `enum HitEffect : byte { Melee=0, Projectile=1, Lightning=2, DashImpact=3 }`. SkillId enum 선례와 동형. byte 직렬화이므로 **wire 모양 무변경**

**server (02_Server)**:
- [ ] 위 매직넘버 사용처를 모두 **상수 참조로 교체** — `new Vector2(0.5f, 0.5f)` → 상수, `1.5f`/`0.05f` 리터럴 → 상수. 값은 동일하므로 동치
- [ ] **hitEffect raw byte 비교를 enum으로** — `CombatSystem.ProcessAttack`(hitEffect=0 근접), `SkillSystem`(=3 Dash), `DeferredDamageSystem`(=1 투사체/=2 낙뢰)의 리터럴을 `HitEffect` enum 멤버로. **S_HitResult 필드는 여전히 byte** — 송신 시 `(byte)HitEffect.Melee`, 디코드 시 `(HitEffect)pkt.hitEffect`로 캐스팅
- [ ] **클라 측 hitEffect 비교**(`ClientPacketHandlers.HitResultHandler` L307 `hitEffect == 1 || 2 || 3`)도 `HitEffect` enum 비교로 교체 (98_Shared 공유)

**qa / 테스트**:
- [ ] 테스트의 매직넘버 리터럴(de-aggro 1.5f 2곳, 히트박스 0.5f 등)도 상수 참조로 교체 — 값 튜닝 시 테스트도 한 곳만
- [ ] **PacketRoundTrip + ProtocolVersion==11 assert** — HitEffect enum이 wire를 안 바꿨는지 못 박는 회귀 테스트(byte 1칸 그대로)

---

## ✅ 완료 조건 (정량)

- [ ] `dotnet test` **green** — 값 불변이라 회귀 **0** (동치 교체)
- [ ] **각 매직넘버 단일 출처** — 히트박스 0.5f / de-aggro 1.5f / epsilon 0.05f / FacingEpsilon 0.001f / GroundEpsilon이 각각 *정확히 하나의* 명명 상수에서만 나옴 (Grep으로 리터럴 잔존 0 확인)
- [ ] **EnemyDefaultHp 배열 폐기** — HP가 EnemyStats factory MaxHp 단일 출처에서만
- [ ] `HitEffect` enum이 **서버·클라 양쪽 사용** — raw byte 비교 0건
- [ ] **ProtocolVersion 11 불변 확인** — PacketRoundTrip 통과 + `Current == 11` assert green (HitEffect은 byte 의미만, wire 모양 무변경)
- [ ] `Shared.dll` 재빌드 → `03_Client/Assets/Plugins/` 갱신 + Unity 콘솔 error CS 0

---

## 🧪 테스트

**자동**:
- `CombatConstantsTests`(또는 동형) — 각 상수가 기대값(0.5/1.5/0.05/...)과 일치 (값 보존 회귀)
- `PacketRoundTripTests` — S_HitResult가 HitEffect enum 도입 후에도 byte 1칸으로 직렬화/역직렬화 정합 + `ProtocolVersion.Current == 11`
- 기존 `HitboxTests`/`EnemyAiTests`/`EnemyStateTests` — 상수 참조로 바꾼 뒤 green 유지(거동 동일)

**수동**:
- 2클라 Play — 근접/투사체/낙뢰/Dash 4 hitEffect가 enum 교체 후에도 동일 연출(이펙트 분기 동일)

---

## 📚 학습 포인트

- **단일 진실 공급원(SSOT, Single Source Of Truth)**: 같은 숫자가 5곳에 박히면 "5개의 진실"이 된다 — 하나만 고치면 나머지 4개가 거짓말(stale)이 된다. 상수 하나로 모으면 진실이 하나뿐이라 어긋날 수가 없다. 특히 `EnemyDefaultHp` 배열처럼 "주석으로 일치 의무만" 적힌 이중관리는 컴파일러가 안 잡아주므로 가장 위험하다.
- **enum이 raw byte보다 안전한 이유**: `if (hitEffect == 3)`은 3이 *뭔지* 코드에 없다 — 주석이 사라지면 의미가 증발한다. `if (effect == HitEffect.DashImpact)`은 런타임 의미가 *코드 자체에* 박혀 있다. 컴파일러가 오타(`HitEffect.Dashimpact`)도 잡고, IDE가 자동완성으로 가능한 값을 보여준다.
- **"의미만 바꾸고 모양은 안 바꾼다"**: HitEffect를 enum으로 올려도 *wire(네트워크로 나가는 바이트)*는 여전히 byte 1칸이다. C# 타입이 바뀌어도 직렬화 결과가 같으면 프로토콜은 안 바뀐 것 — 그래서 ProtocolVersion bump가 불필요하다. 반대로 byte를 short로 늘리거나 필드 순서를 바꾸면 wire가 바뀌어 반드시 bump해야 한다.

---

## ⚠️ 함정 / 주의사항

- **HitEffect enum이 wire 모양을 바꾸지 않는지 반드시 확인** — byte 1칸 그대로여야 한다. 만약 enum 도입 과정에서 패킷 필드 타입이 바뀌면 ProtocolVersion bump가 필요(irreversible 깃발). PacketRoundTrip + `Current==11` assert로 못 박을 것.
- **EnemyDefaultHp는 컴파일 강제 없는 이중관리** — 배열{30,100,60}과 EnemyStats MaxHp가 따로 산다. 한쪽만 고치면 stale. 배열을 *폐기*하고 EnemyStats를 단일 출처로 만드는 게 핵심(둘을 "동기화"하는 게 아니라 하나를 없애는 것).
- **값은 한 톨도 바뀌면 안 된다** — 이건 "리팩토링"이지 "튜닝"이 아니다. 0.5f를 상수로 빼면서 0.45f로 바꾸면 그건 밸런스 변경(거동 변화)이다. 동치 교체만.
- **우연한 중복 주의**(§0.3) — 히트박스 0.5f와 de-aggro 1.5f가 "둘 다 float 상수"라고 한 상수로 묶으면 안 된다. 이유가 다르면 따로 둔다(같은 값이어도 의미가 다르면 별개 상수).

---

## ➡️ 다음 Phase

- Phase 03 (적 사망 통합) / Phase 04 (roster + rewind/facing 헬퍼) — 둘 다 server 도메인. 03·04는 GameMap 편집이라 순차.

---

## 📋 박제 (완료 후 -DONE.md)

- 복잡 등급 → `-DONE.md` 박음 (매직넘버 SSOT 봉인 + HitEffect enum, wire 무변경 사실 박제).

---

## 작업 로그

- 2026-06-11: 계획 작성 (전수조사 "흩어진 게임플레이 매직 넘버" + "HitEffect enum 부재" rootCause → 98_Shared 단일화. 값 불변 = 회귀 0 목표)
