---
owner: youngho
phase: 03
status: done
grade: 복잡
summary: 적 사망처리 13~15줄 블록 3중복붙(CombatSystem/DeferredDamageSystem/SkillSystem)을 GameMap.HandleEnemyDeath() 한 메서드로 추출
---

# Phase 03: 적 사망처리 3중복붙 → GameMap.HandleEnemyDeath() 통합

> **상태**: pending
> **마일스톤**: M4.10
> **등급**: 복잡 (server 단일 도메인, 단 GameMap 핵심 편집)
> **담당**: server Worker(Sonnet)
> **의존**: Phase 01 (컨벤션 v6 §2.5 DRY: 데이터 소유 객체로 추출). 04와 둘 다 GameMap 편집 → **03 → 04 순차 권장**

---

## 🎯 목표

전수조사가 **단일 최대 레버리지**로 지목한 중복을 봉합한다. 적 사망 후처리 블록 — `S_EntityDeath` broadcast → Boss면 StageClear → `RemoveEnemy` → Normal이면 Respawn — 13~15줄이 **세 파일에 byte 단위로 복붙**돼 있다(작성자 본인이 `DeferredDamageSystem.cs` 주석에 "CombatSystem과 동일"이라 자인). 이걸 `GameMap.HandleEnemyDeath(EnemyEntity)` 한 메서드로 추출하고, 3 호출처를 `if (target.Hp <= 0) map.HandleEnemyDeath(target);` 한 줄로 교체한다. 이 Phase가 끝나면 **사망 정책(드롭/보상/로그)을 미래에 바꿀 때 한 곳만 고치면 세 경로가 자동 일관**해진다. tick 단일 스레드 경로라 거동 불변·회귀 0이 목표다.

---

## ⏪ 사전 조건

- [ ] Phase 01 완료 — 컨벤션 v6 §2.5(중복 3회 = 추출 의무, 소유 객체 메서드로)
- [ ] 전수조사 output `rootCauses[0]`("적 사망 처리 3중 복붙") + 각 verdict(confirmed=true)에서 정확한 file:line 확인:
  - `CombatSystem.ProcessAttack` ~L154 (Knight 즉시 데미지 경로)
  - `DeferredDamageSystem.Process` ~L62 (Mage 투사체/Thunderbolt deferred 경로)
  - `SkillSystem.ProcessDash` ~L130 (Dash 다중 타겟 foreach 안)
- [ ] `GameMap`의 internal mutator 확인 — `SetStageCleared` / `RemoveEnemy` / `EnqueueRespawn` / `BroadcastToAll`이 이미 internal로 노출(추출이 깨끗한 근거)
- [ ] `BossStageClearTests` 위치·계약 확인 — S_EntityDeath → S_StageClear 순서를 검증 중

---

## 📝 작업 내용

> GameMap에 메서드 신설 → 3 호출처 위임. 사망 *후* 블록만 추출(HP 게이트·S_HitResult 송신은 호출처에 남김).

**server — GameMap (02_Server/GameServer/Maps/GameMap.cs)**:
- [ ] `internal void HandleEnemyDeath(EnemyEntity target)` 신설 — 내부에 사망 후처리 시퀀스를 한 곳에:
  1. `S_EntityDeath` 생성 → `BroadcastToAll`
  2. `target.Kind == Boss && !IsStageCleared` → `SetStageCleared()` + `S_StageClear` broadcast
  3. `RemoveEnemy(target.EntityId)`
  4. `target.Kind == Normal` → `EnqueueRespawn(target)`
- [ ] **순서 보존** — S_EntityDeath → (Boss) S_StageClear 순서를 그대로(BossStageClearTests 계약)
- [ ] (선택) 추출 후 `SetStageCleared`/`RemoveEnemy`/`EnqueueRespawn`의 외부 호출처가 사라지면 surface 축소 가능한지 점검 — 단 04(roster)와 충돌 없는 범위에서만

**server — 3 호출처 교체**:
- [ ] `CombatSystem.ProcessAttack` — 사망 후 블록 삭제, `if (target.Hp <= 0) map.HandleEnemyDeath(target);`로 교체. **S_HitResult 송신과 HP floor는 호출처에 남김**
- [ ] `DeferredDamageSystem.Process` — 동일 교체. 이 경로의 `Math.Max(0, ...)` HP floor는 호출처 유지(사망 블록만 추출)
- [ ] `SkillSystem.ProcessDash` — foreach 루프 안에서 동일 교체. 루프 컨텍스트와 자연스럽게 결합

**qa / 테스트**:
- [ ] `BossStageClearTests` 회귀 확인 — S_EntityDeath → S_StageClear 순서·내용 불변
- [ ] (선택) `HandleEnemyDeath` 단위 테스트 신설 — Boss면 StageClear 1회, Normal이면 Respawn 큐잉, 둘 다 RemoveEnemy

---

## ✅ 완료 조건 (정량)

- [ ] `dotnet test` **green**
- [ ] **BossStageClearTests 회귀 0** — S_EntityDeath → S_StageClear 순서 계약 통과(추출 전후 동일)
- [ ] **봇 회귀 0** — DashSmoke / ThunderboltAoeSmoke / RangedHitSmoke 등 사망 경로를 타는 시나리오 전부 추출 전과 동일 결과
- [ ] **3 호출처가 단일 메서드 호출** — Grep `S_EntityDeath death = new`가 production code에서 *HandleEnemyDeath 안 1곳*만 적중(3곳 → 1곳)
- [ ] **S_EntityDeath / S_StageClear wire 불변** — PacketRoundTripTests로 byte 모양 검증(BossStageClearTests는 broadcast *순서*만 잡고 byte 모양은 못 잡음).
- [ ] 거동 불변 — 추출 전후 봇 로그·테스트 출력 diff 없음

---

## 🧪 테스트

**자동**:
- `BossStageClearTests` — 보스 사망 시 S_EntityDeath 직후 S_StageClear, 순서·중복(StageClear 1회) 계약
- 기존 `CombatTests`/스킬 관련 테스트 — Normal 적 사망 시 RemoveEnemy + Respawn 큐잉 정합

**수동**:
- 헤드리스 봇 DashSmoke — Knight 대쉬로 경로 적 처치 → 적 제거 + (Normal) 리스폰 + (Boss면) StageClear, 추출 전과 동일

---

## 📚 학습 포인트

- **DRY 추출의 정석**: 중복은 "묶는다"가 아니라 "데이터를 소유한 객체의 메서드로 옮긴다". 여기서 `SetStageCleared`/`RemoveEnemy`/`EnqueueRespawn`을 *소유한* 건 GameMap이다 — 그래서 사망 처리도 GameMap의 책임이 맞다. System이 다른 System을 직접 부르지 않고 *map을 경유*하는 §2.2 규율과도 정합한다. 엉뚱한 곳(예: static 유틸)에 빼면 소유권이 흐려진다.
- **계약 테스트가 추출 회귀를 어떻게 방어하나**: `BossStageClearTests`는 "S_EntityDeath 다음에 S_StageClear가 온다"는 *관찰 가능한 계약*을 검증한다. 내부 구현을 3복붙에서 1메서드로 바꿔도, 이 계약(외부에서 보이는 순서)만 보존되면 테스트가 green이다 — 즉 "리팩토링이 거동을 안 바꿨다"를 자동으로 증명해준다. 계약 테스트가 있으면 추출이 안전하다.
- **"무엇을 추출하고 무엇을 남기나"**: 사망 *후* 블록만 추출하고, `if (target.Hp <= 0)` 게이트와 S_HitResult 송신은 호출처에 남긴다. 왜냐하면 HP 적용 *타이밍*이 경로마다 다르기 때문(Knight 즉시 / deferred 도착 / Dash 루프). 공통인 부분만 빼고 다른 부분은 남기는 게 추출의 경계 설정이다.

---

## ⚠️ 함정 / 주의사항

- **tick 단일 스레드 경로라 거동 불변이어야 함** — 세 호출처 모두 map의 tick 스레드 안에서 실행된다. HandleEnemyDeath를 GameMap에 두면 호출 규율(§1.1, System은 tick 스레드 안에서만)과 정합하고 lock도 불필요. 추출이 스레드 경계를 넘으면 안 된다.
- **S_EntityDeath → StageClear 순서가 테스트 계약이라 순서 보존 필수** — 메서드 안에서 broadcast 순서를 바꾸면 BossStageClearTests가 깨진다. 원래 순서 그대로 옮길 것.
- **HP 게이트·S_HitResult를 메서드 안으로 끌고 들어가지 말 것** — HP 적용 타이밍이 호출처마다 달라(특히 DeferredDamageSystem의 floor), 게이트를 메서드로 옮기면 경로별 차이가 뭉개진다. 추출 범위는 "사망 후 블록"만.
- **패킷을 손으로 구성해 BroadcastToAll하는 코드 추출 중 필드/순서 변형 위험 = 04와 동급 비가역(헌법 §2)** — S_EntityDeath/S_StageClear는 패킷 직접 구성 영역이라, 추출하며 필드 하나라도 빠지거나 순서가 바뀌면 wire 불일치(되돌리기 비싼 사고). PacketRoundTrip으로 byte 동일을 못 박을 것.
- **04와 GameMap 충돌** — 04(roster)도 GameMap을 편집한다. 병렬로 하면 머지 충돌 — 03 먼저 끝내고 04 착수(순차).

---

## ➡️ 다음 Phase

- Phase 04 (roster 전송 통합 + rewind/facing 헬퍼) — GameMap 편집이 겹치므로 03 완료 후 착수.

---

## 📋 박제 (완료 후 -DONE.md)

- 복잡 등급 → `-DONE.md` 박음 (전수조사 단일 최대 레버리지 = 적 사망 3복붙 봉합, DRY 추출 사실 박제).

---

## 작업 로그

- 2026-06-11: 계획 작성 (전수조사 rootCauses[0] "적 사망 처리 3중 복붙, 단일 최대 레버리지" + verdict 3건 confirmed=true → GameMap.HandleEnemyDeath 추출)
