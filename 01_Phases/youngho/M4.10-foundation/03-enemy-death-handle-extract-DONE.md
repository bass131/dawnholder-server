---
owner: youngho
phase: 03
status: done
grade: 복잡
summary: 적 사망 후처리 13줄 블록 3중복붙(CombatSystem 즉시 / DeferredDamageSystem 지연 / SkillSystem Dash)을 GameMap.HandleEnemyDeath() 단일 메서드로 추출. wire·순서 불변, 봇 5경로 회귀 0.
completed: 2026-06-11
---

# Phase 03 완료 — 적 사망처리 3중복붙 → GameMap.HandleEnemyDeath() 통합

> M4.10 세 번째 Phase. 전수조사가 지목한 **단일 최대 레버리지** 중복을 봉합. behavior-preserving 추출(거동 불변·회귀 0).

---

## TL;DR

적 사망 *후* 처리 — `S_EntityDeath` broadcast → (Boss) StageClear → `RemoveEnemy` → (Normal) Respawn — 13줄이 **세 파일에 byte 단위 복붙**돼 있었다(작성자 본인이 "CombatSystem과 동일" 자인). 이를 `GameMap.HandleEnemyDeath(EnemyEntity)` 한 메서드로 추출하고 3 호출처를 `if (target.Hp <= 0) map.HandleEnemyDeath(target);` 한 줄로 교체했다. 사망 정책(드롭/보상/로그)을 미래에 바꿀 때 **한 곳만 고치면 세 경로가 자동 일관**해진다.

**산출물**:
- `02_Server/.../Maps/GameMap.cs` — `internal void HandleEnemyDeath(EnemyEntity target)` 신설(internal mutator 섹션 끝, `EnqueueRespawn` 뒤). GameMap 내부라 `map.` 접두사 없이 소유 mutator 직접 호출.
- `CombatSystem.ProcessAttack` (Knight 즉시) — if 본문만 교체, **else(생존 HitState + knockback) 보존**.
- `DeferredDamageSystem.Process` (Mage 투사체/Thunderbolt 지연) — if 단문 교체, 위쪽 `Hp -= impact.Damage` + `Math.Max(0,Hp)` floor S_HitResult 보존.
- `SkillSystem.ProcessDash` (Dash foreach 안) — if 단문 교체, 뒤 S_SkillCast broadcast 보존.
- `GameServer.Tests/PacketRoundTripTests.cs` — S_EntityDeath(14)/S_StageClear(15) 라운드트립 + PacketId 4케이스 추가(wire 불변 명시 가드).

---

## AC 검증 결과

| 완료조건 | 검증 | 결과 |
|---|---|---|
| dotnet test green | unit 527 passed / 0 failed (baseline 523 + 신규 4) | ✅ |
| BossStageClearTests 회귀 0 | 3종(순서/idempotent/Normal) green — `deathIdx < stageClearIdx` 순서 계약 통과 | ✅ |
| 봇 회귀 0 (사망 경로) | 5 시나리오 PASS — 3 경로(즉시/지연/Dash) 완전 커버 | ✅ |
| 3 호출처 단일 메서드 호출 | `S_EntityDeath death = new` production 잔존 = GameMap 1곳(3→1) | ✅ |
| S_EntityDeath/S_StageClear wire 불변 | PacketRoundTrip 4케이스 + PDL/Generated 무변경 | ✅ |
| ProtocolVersion 11 불변 | PDL.xml/GenPackets 무변경 → bump 0 | ✅ |
| reviewer 통과 | 🟢 5축 전부 통과, 회귀 표면 0 | ✅ |

**봇 회귀** (WSL2, ADR-029):
```
EmergencyCombatSmoke (즉시 CombatSystem)   PASS  death=True hp 30→-20
BossStageClearSmoke (즉시 + 순서)          PASS  S_EntityDeath→S_StageClear, duplicateSuppressed=True
DashSmoke (SkillSystem.ProcessDash)        PASS  hitEffect3, pathEnemy=True
ThunderboltAoeSmoke (지연 DeferredDamage)  PASS  hitEffect=2, allHpDecreased=True
RangedHitSmoke (지연 DeferredDamage)       PASS  hitEffect=1, travelTicks=2
```

---

## 결정 흐름

1. **추출 위치 = GameMap (데이터 소유자)**. `_enemies`/`_stageCleared`/broadcast를 *소유한* 건 GameMap이다. 사망 후처리를 GameMap 메서드로 두는 게 DRY 추출 정석(소유권 응집) + §2.2(System은 System을 직접 안 부르고 map 경유) 규율과 정합. static 유틸에 빼면 소유권이 흐려진다.

2. **무엇을 추출하고 무엇을 남기나** — 이 작업의 가장 위험한 경계 설정. "사망 *후* 공통 후처리"만 옮기고, **경로마다 다른 부분은 호출처에 남겼다**:
   - HP 게이트 `if (target.Hp <= 0)` — 3곳 모두 호출처 잔류(적용 타이밍이 경로별로 다름: Knight 즉시 / deferred 도착 / Dash 루프).
   - `currentHp` 차이 — CombatSystem `target.Hp`(raw 음수=사망 신호 계약) vs DeferredDamage `Math.Max(0,Hp)` floor vs Dash `target.Hp`(raw). 전부 S_HitResult 구성에 남아 추출 영향 0.
   - CombatSystem의 `else`(생존 HitState + knockback) — Deferred/Dash엔 없던 분기라 원본 그대로 보존.

3. **계약 테스트가 추출 회귀를 방어** — `BossStageClearTests`가 "S_EntityDeath 다음 S_StageClear"라는 *관찰 가능한 계약*을 byte로 assert(`deathIdx < stageClearIdx`). 내부 구현을 3복붙→1메서드로 바꿔도 이 계약만 보존되면 green = "리팩토링이 거동을 안 바꿨다"를 자동 증명. 여기에 PacketRoundTrip 4케이스를 더해 wire 회귀를 한 번 더 명시적으로 못 박음.

4. **DLL drift 2회 복원** — server-only 변경인데 Worker/qa가 `build Dawnholder.slnx`(full solution)를 돌릴 때마다 `Shared.dll`/`Client.Net.dll`이 소스 무변경인데 바이트만 재생성돼 Plugins에 drift. `98_Shared/`·`04_ClientNet/` 소스 빈 diff 확인 후 `git checkout`으로 복원(commit 전 2회). Phase 02는 HitEffect.cs 추가라 legitimate였으나 03은 순수 drift.

---

## 학습 일지 후보 키워드

- **behavior-preserving 추출의 경계 설정**: 추출의 핵심은 "묶기"가 아니라 "무엇을 남기고 무엇을 옮기나". 공통(사망 후처리)만 옮기고 경로별 차이(HP 게이트 타이밍·currentHp floor 여부·생존 HitState)는 호출처에 남긴다. 차이를 메서드 안으로 끌고 들어가면 경로별 거동이 뭉개진다.
- **DRY 추출은 데이터 소유자의 메서드로**: 중복을 빼는 위치는 그 데이터를 *소유한* 객체. GameMap이 `_enemies`/`_stageCleared`를 소유하므로 사망 처리도 GameMap 책임. 소유권 응집이 §2.2(System 간 직접 호출 금지, map 경유)와 자연히 정합.
- **리팩토링 안전망 = 사전 계약 테스트**: "이 변경을 잡아줄 테스트가 있는가?"를 추출 *전에* 확인. `BossStageClearTests`가 broadcast 순서/개수/idempotent를 byte로 assert하고 있어 한 byte라도 어긋나면 빨개진다. 계약 테스트가 있으면 추출이 안전하고, 없으면 먼저 만들고 추출한다.
- **봇 회귀가 단위 테스트를 보완**: 단위 테스트(BossStageClear)는 즉시 경로만 직접 커버. 지연(DeferredDamage)·Dash(SkillSystem) 경로는 헤드리스 봇(ThunderboltAoe/RangedHit/DashSmoke)이 end-to-end로 사망→broadcast→respawn을 실측해 3 경로 전부 거동 불변을 증명.
- **server-only 변경의 DLL drift**: full solution 빌드가 소스 무변경인데도 Plugins의 Shared.dll/Client.Net.dll 바이트를 재생성 → `git checkout`로 복원. 빌드를 돌리는 SubAgent마다 재발하므로 commit 직전 `git status`로 재확인 습관.

---

## 후속 후보 (이번 범위 밖)

- `SetStageCleared`/`RemoveEnemy`/`EnqueueRespawn`의 외부 호출처가 HandleEnemyDeath 한 곳으로 줄었으나 surface 축소는 Phase 04(roster, GameMap 동시 편집)와 충돌 회피 위해 보류 — 04 마감 후 점검 가능.
- commit message에 "산출물 DLL 동반" 표기 권고는 이번엔 drift 복원으로 DLL 자체를 commit에서 제외해 불필요.
