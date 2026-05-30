---
owner: youngho
milestone: M4.3
phase: 09
title: Boss Behavior — 다단 attack 패턴 + 페이즈 1/2 + 적→플레이어 공격 패킷
status: pending
grade: 복잡
risk: irreversible
estimated: 4~6h
domain: server+shared+client
---

# Phase 09: Boss Behavior — 다단 attack 패턴

> **상태**: pending
> **마일스톤**: M4.3
> **등급**: 복잡 (3 도메인 + PDL bump irreversible)
> **담당**: server (behavior) + shared (패킷) + client (이펙트)

---

## 🎯 목표

지금 맞기만 하고 죽는 보스를, **스스로 공격하는 보스**로 만든다. 보스가 페이즈(HP 임계 기준 1→2)에 따라 **다단 attack 패턴**(예: 근접 강타 → 광역 → 페이즈 2에서 빨라짐)을 tick 타이머로 수행하고, 범위 안 플레이어에게 **서버 권위 데미지**를 입힌다. 적→플레이어 공격을 알리는 패킷(S_EnemyAttack)을 신설한다.

이 Phase가 끝나면 **보스방에서 보스와 실제로 주고받는 전투**가 발표 데모 클라이맥스가 된다.

---

## ⏪ 사전 조건

- [ ] **Phase 07 완료** — enemy AI 인프라(FSM, tick update 루프, 위치 패킷)
- [ ] **Phase 08a 완료** — animState 프로토콜 + 서버 상태 결정 (boss attack을 animState 채널로 송신)
- [ ] **Phase 08b 완료** — enemy 클라 렌더 + AnimatorDriver (보스도 enemy 렌더/driver 재사용)
- [x] M3 Phase 07 boss 골격 (`EnemyKind.Boss`, `S_StageClear`, `GameMap.cs:302` stage clear)

---

## 📝 작업 내용

### 서버 (server)
- [ ] `BossBehavior` 로직 — `EnemyKind.Boss`일 때 patrol/chase 대신 boss 패턴 FSM
  - 페이즈 1: 기본 attack 패턴 (쿨다운 기반 1~2종 공격)
  - 페이즈 2: HP ≤ 50% 시 전환 — 공격 속도/패턴 강화
  - attack 판정: tick 안에서 범위 내 플레이어에게 데미지 (서버 권위 — 헌법 #1)
- [ ] 데미지 공식 재사용 (`98_Shared/GameData/Formulas.cs` — `baseDamage + atk - def`). 보스 Attack 스탯 추가 (`EnemyStats`)
- [ ] 플레이어 HP 감소 = 서버 권위. **사망 정책 확정** (plan-auditor 🔴 봉합): 플레이어 HP 0 도달 시 → **해당 맵 spawn 지점 리스폰 + HP full**. 데모용 임시 정책이며 무적 토글 없음. (제대로 된 사망/리스폰/패널티는 발표 후 컨텐츠 마일스톤에서 ADR로 정의 — 본 Phase는 데모가 멈추지 않는 최소 정책만)
  - 서버 권위 (헌법 #1): 리스폰 위치/HP는 서버가 결정해 broadcast. 필요 시 S_EnemyAttack에 사망/리스폰 신호 또는 기존 snapshot으로 처리 (구현 결정 작업 로그에)
- [ ] **헌법 #5**: 모든 boss 타이머/판정은 tick thread 동기. `await`/`Thread.Sleep` 금지

### 공유 (shared)
- [ ] **PDL: `S_EnemyAttack` 패킷 신설** (append-only) — `attackerId, targetId, damage, targetCurrentHp, attackPattern(byte)` (클라 이펙트 분기용)
- [ ] `Protocol.Version` — **추가 bump 없음** (M4.3 한 PR 묶음 전제: 08a의 7→8 Version 8 안에 S_EnemyAttack도 포함). ⚠️ 애니 상태머신(08a)과 *별도 PR로 분리* 머지할 경우에만 여기서 8→9
- [ ] PacketGenerator 재생성 + Shared.dll 복사

### 클라 (03_Client + 04_ClientNet)
- [ ] `S_EnemyAttack` 핸들러 — 보스 공격 이펙트(패턴별) + 플레이어 피격 표시(HP 바 감소 — 서버 값)
- [ ] **HUD HP mock 제거** (MAX effort 재검토 발견): `HudController`가 현재 `mockHpCurrent=100` 고정. `S_EnemyAttack`(또는 피격 패킷)에서 `HudController.UpdateHP(current, max)` 실제 호출로 연결 — 메서드는 이미 존재하므로 핸들러에서 호출만. (work-pin "HUD 영구 mock" 봉합)
- [ ] 보스 attack 애니 = **08a animState 채널로 송신** (서버가 boss attack 틱에 animState=Attack 설정) → `AnimatorDriver`가 렌더. 별도 트리거 코드 X. Attack 클립/전이는 11(본인 외관 분담)
- [ ] 페이즈 2 전환 시각 연출(옵션, 여유 시 — 발표 어필)

### 테스트
- [ ] `BossBehaviorTests` — 페이즈 전환(HP 50%), attack 쿨다운, 범위 내 플레이어 데미지 적용, 범위 밖 데미지 0
- [ ] 헤드리스 봇 `BossFightSmoke` — 봇이 보스방 진입 → 보스 공격받아 HP 감소 → 보스 처치 → StageClear (기존 BossStageClearSmoke 확장)

---

## ✅ 완료 조건

- [ ] `dotnet build` + `dotnet test --no-incremental` green — 회귀 0 + 신규 `BossBehaviorTests`
- [ ] 보스가 패턴 공격을 수행하고, HP 50%에서 페이즈 2로 전환 (단위 테스트 + Play)
- [ ] 범위 안 플레이어만 데미지, 데미지 = 서버 권위 (클라 임의 변경 불가 — 헌법 #1)
- [ ] **플레이어 HP 0 → spawn 리스폰 + HP full 동작** (단위 테스트 + Play). 데모 중 플레이어 사망해도 멈추지 않음
- [ ] `Protocol.Version == 8`, `S_EnemyAttack` PacketID stable
- [ ] Play 실측: 보스방에서 보스와 양방향 전투 → 처치 → StageClear 정상 (기존 stage clear 회귀 0)
- [ ] 헤드리스 봇 `BossFightSmoke` PASS

---

## 🧪 테스트

**자동**:
- `BossBehaviorTests` — 페이즈 전환 임계, attack 판정 범위/쿨다운, 데미지 공식
- `BossStageClearTests` 회귀 0 (보스가 공격해도 사망 시 StageClear 1회 유지)

**수동**:
- 서버 + 클라/봇 → 보스방 전투 풀 루프 (진입 → 패턴전 → 페이즈 2 → 처치 → StageClear)

---

## 📚 학습 포인트

- **양방향 권위 전투**: Phase 07까지는 플레이어→적 단방향. 이제 적→플레이어도 서버가 판정. "누가 누구를 언제 때렸나"가 전부 서버 진실.
- **타이머 기반 패턴 (tick 카운터)**: `await Task.Delay` 없이 tick 수를 세어 쿨다운/패턴 타이밍 구현 (헌법 #5 정합). 게임 서버 AI의 표준 패턴.
- **페이즈 전환 = 상태 분기**: HP 임계 같은 조건으로 behavior를 바꾸는 보스 디자인. FSM의 확장.
- **패킷에 표현 힌트 싣기**: `attackPattern(byte)`로 클라가 어떤 이펙트를 쓸지 결정 — 서버는 논리, 클라는 표현.

---

## ⚠️ 함정 / 주의사항

- **PDL append-only** (헌법 #2): `S_EnemyAttack` 맨 끝 추가. **Version 추가 bump 없음** — 08a의 7→8 Version 8에 본 패킷도 포함(한 PR 묶음). 애니 상태머신(08a)과 별도 PR 분리 시에만 8→9. CHANGELOG엔 M4.3 프로토콜 변경(animState 필드 + S_EnemyAttack)을 한 항목으로 기록.
- **보스 공격 판정 비대칭 (MAX effort 재검토)**: 보스→플레이어 데미지는 서버가 플레이어 *권위 위치*로 판정. 플레이어는 prediction으로 이미 움직였을 수 있어 "피했는데 맞았다" 가능. 보스 공격에 **telegraph(예고 모션/짧은 딜레이)**를 넣으면 회피가 시각적으로 공정해 보여 발표 체감 ↑ (07 target rewind 비대칭과 짝 — 둘 다 M4.4 정밀 전투 대상).
- **플레이어 사망 처리 미정의 위험**: 보스가 때려서 플레이어 HP 0이 되면? 발표 데모가 멈추지 않게 최소 정책(리스폰/무적 토글) 필요. 본 Phase에서 결정.
- **데미지 신뢰 경계 (헌법 #3)**: 적 데미지는 서버 계산. 클라는 S_EnemyAttack의 currentHp를 표시만. 클라가 "안 맞았다" 주장 불가.
- **tick 폭주**: 보스 패턴 쿨다운을 ms가 아니라 tick 수로 관리. dt 가변에 흔들리지 않게.
- **stage clear 회귀**: 보스가 공격 로직을 가져도 사망→StageClear 1회 보장(`_stageCleared` flag)이 깨지지 않게.

---

## ➡️ 다음 Phase

- Phase 10 — 움직임 체감 polish (07~09와 독립, 병렬 가능)

---

## 📋 박제 (완료 후)

- **복잡 등급** — `09-boss-behavior-DONE.md` 박음.
- ⚠️ **β cross-review 권장** (plan-auditor 🟡): 09는 3도메인 + PDL bump + *첫 양방향 권위 전투* + 사망 정책으로 대규모 경계. reviewer Tier 2-A에 더해 `/cross-review`로 외부 시각 재검증 권장 (irreversible + 신뢰경계 동시).

---

## 작업 로그

- 2026-05-29: 계획 수립 (`/work:plan M4.3`)
