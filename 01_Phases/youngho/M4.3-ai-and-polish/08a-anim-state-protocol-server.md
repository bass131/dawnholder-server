---
owner: youngho
milestone: M4.3
phase: 08a
title: 애니 상태머신 — 프로토콜 + 서버 (AnimState enum + animState 필드 + 서버 상태 결정 broadcast)
status: pending
grade: 복잡
risk: irreversible
estimated: 3~4h
domain: shared+server
summary: 서버가 각 entity의 시각 애니 상태를 1바이트로 결정해 위치 스냅샷에 동봉 broadcast — 클라가 받을 데이터 채널 구축
---

# Phase 08a: 애니 상태머신 — 프로토콜 + 서버

> **상태**: pending
> **마일스톤**: M4.3
> **등급**: 복잡 (shared+server 2도메인 + PDL bump irreversible)
> **담당**: shared (AnimState + 패킷 필드) + server (상태 결정 broadcast)

---

## 🎯 목표

애니 상태머신(메이플 스타일)의 **데이터 기반**을 깐다. 서버가 각 entity(플레이어 + 적/보스)의 **현재 시각 애니 상태**(Idle / Walk / Jump / Attack / Hit / Death)를 1바이트(`animState`)로 결정해서, 기존 위치 스냅샷(`S_Snapshot` / `S_EntityState`)에 **동봉 broadcast**한다.

이 Phase가 끝나면 **서버가 "이 녀석은 지금 Walk 중", "저 적은 Attack 중"을 패킷으로 알려주는 채널**이 열린다 (클라 렌더는 08b 몫). 헌법 #1(Server Authority) — 애니 상태도 서버가 권위적으로 판정.

### A안 채택 근거 (사용자 의논 2026-05-30)
서버가 animState를 권위적으로 정해 한 채널로 통일 → 원격/적의 클라 구현이 *전부 "byte 읽기"로 통일*되어 구조가 깔끔(전략 패턴). 대안(이벤트 패킷 조합)은 클라 상태머신이 여러 신호에 흩어져 완성도↓. → A안.

---

## ⏪ 사전 조건

- [x] **Phase 07 완료** (merged) — `S_EntityState` 패킷 + enemy FSM + EnemyState enum
- [x] `S_Snapshot`(vx/vy 포함) + `PlayerStats`(MoveSpeed) — 서버 권위 이동 인프라

---

## 📝 작업 내용

### 공유 (shared)
- [ ] **`AnimState` enum 신설** — `98_Shared/GameData/AnimState.cs` (클라/서버 공유, 헌법 #4). byte 기반:
  ```
  Idle = 0, Walk = 1, Jump = 2, Attack = 3, Hit = 4, Death = 5
  ```
  - 시각 표현 상태 전용. 서버 AI 행동상태(`EnemyState`: Idle/Patrol/Chase)와 **개념 분리** — EnemyState는 "AI가 뭘 하려는가", AnimState는 "화면에 뭘 그리는가". (예: Patrol·Chase 둘 다 AnimState.Walk로 매핑)
- [ ] **PDL: `S_Snapshot`에 `<byte name="animState"/>` append** (맨 끝, append-only) — 플레이어 애니 상태(RemotePlayer 렌더용)
- [ ] **PDL: `S_EntityState`에 `<byte name="animState"/>` append** (기존 `state` byte 뒤, append-only) — 적/보스 애니 상태
- [ ] **`Protocol.Version` 7→8 bump** (irreversible 깃발). Phase 09 `S_EnemyAttack`도 v8에 포함(한 PR 묶음 — 추가 bump 없음)
- [ ] **PacketGenerator 재생성 + `dotnet build` + Shared.dll 복사** (99_Tools/CLAUDE.md 후속 의무 3종) — `PDL.xml` + `GenPackets.cs` + `Shared.dll` 동반

### 서버 (server)
- [ ] **각 entity의 animState 결정 로직** — tick 루프 안에서 계산 (헌법 #5: `await`/`Thread.Sleep` 금지):
  - **플레이어**: `vx` 절대값 > epsilon → Walk, else Idle. 공중(grounded=false) → Jump. attack/hit/death 이벤트 발생 틱 → 해당 상태 (짧은 우선순위: Death > Hit > Attack > Jump > Walk > Idle)
  - **적/보스**: AI state(Patrol/Chase) → Walk, Idle → Idle. attack 수행 틱 → Attack. 피격 틱 → Hit. 사망 → Death
- [ ] **snapshot 생성부에 animState 채워 broadcast** — `S_Snapshot` 만드는 곳(GameSession/GameMap player snapshot) + `S_EntityState` 만드는 곳(EnemyAISystem/GameMap)에 결정된 animState 주입
- [ ] **상태 우선순위/지속(latch) 정책 확정**: Attack/Hit는 순간 이벤트 → 1틱만 보내면 클라가 놓침. 최소 N틱(예: 애니 길이만큼) 유지(latch)하는 카운터 패턴. tick 수 기반(헌법 #5)

### 테스트
- [ ] `AnimStateTests` (server) — 입력/상태 조합 → 기대 animState 매핑 검증 (Idle/Walk/Jump/Attack/Hit/Death 각 케이스). 우선순위 검증(피격 중 이동 입력 → Hit 우선)
- [ ] 헤드리스 봇 `EnemyAiSmoke` 확장 또는 로그 — `S_EntityState.animState`가 Patrol 시 Walk(1)로 실려오는지 확인 (08b 없이 서버 단독 검증)

---

## ✅ 완료 조건

- [ ] `AnimState` enum이 98_Shared에 정의 + Shared.dll에 포함 (클라가 dll로 참조 가능 상태)
- [ ] `Protocol.Version == 8`, `S_Snapshot`/`S_EntityState` 둘 다 `animState` byte 포함, **기존 필드 순서 불변**(append-only — ID/필드 재배열 0)
- [ ] `dotnet build Dawnholder.slnx --no-incremental` 0 error + `dotnet test --no-build` green (회귀 0 + 신규 `AnimStateTests`)
- [ ] 헤드리스 봇 또는 서버 로그로 **animState가 실제 패킷에 실려 나감** 확인 (적 Patrol→Walk, Idle→Idle 최소 검증)
- [ ] PacketGenerator 산출물 3종(PDL/GenPackets/Shared.dll) 동반 — stale dll 0
- [ ] 헌법 #5 준수: animState 결정이 tick thread 동기, blocking call 0

---

## 🧪 테스트

**자동**:
- `AnimStateTests` — 상태 결정 함수 순수성(같은 입력 → 같은 animState) + 우선순위
- 기존 movement/combat/enemy 테스트 회귀 0 (필드 추가가 직렬화 깨지 않음)

**수동**:
- 서버 단독 + 헤드리스 봇 → `S_EntityState` 로그에서 animState 값 관찰 (08b 클라 렌더 전 서버 검증)

---

## 📚 학습 포인트

- **AI 상태 vs 애니 상태 분리**: `EnemyState`(서버 FSM 판단)와 `AnimState`(시각 표현)는 다른 레이어. 하나로 합치면 "공격 모션 중엔 추격 못 함" 같은 결합 발생. 분리가 정석.
- **append-only 프로토콜 진화**: 기존 패킷에 필드를 *맨 끝에* 더하면 옛 디코더는 앞부분만 읽어 호환(이론). 단 의미가 바뀌므로 Version bump로 stale 클라를 cutoff (헌법 #2).
- **순간 이벤트의 latch**: Attack/Hit처럼 1틱짜리 이벤트를 20TPS로 1번만 보내면 50ms 윈도우라 클라가 놓치거나 깜빡임. 최소 지속 틱 카운터로 "붙잡아" 안정적으로 전달.
- **서버 권위 애니(헌법 #1)**: 클라가 "쟤 지금 공격 중"을 추측하지 않고 서버가 판정해 보냄 → 모든 클라가 같은 모션을 같은 타이밍에 봄.

---

## ⚠️ 함정 / 주의사항

- **PDL append-only 절대 준수** (헌법 #2): `animState`는 반드시 각 패킷 **맨 끝**에. 중간 삽입 = 모든 후속 필드 오프셋 깨짐 = desync. ID 재사용 0.
- **PacketGenerator 후속 3종 누락 = 다른 머신 빌드 회귀** (99_Tools/CLAUDE.md, 정유현 PR #19 사고): PDL 수정 후 재생성 + build + dll 복사 + 3산출물 동반 commit.
- **S_Snapshot은 최빈 패킷**: 매 SnapshotTickInterval(100ms)마다 전 플레이어에게 감. +1바이트는 미미하나, animState 결정 로직이 무겁지 않게(헌법 #5 tick budget).
- **latch와 사망 정합**: Death animState 보낸 뒤 entity가 S_EntityDeath로 사라지는 타이밍 — death 모션 재생 시간 확보(09 despawn 지연과 연동). 08a는 animState=Death 송신까지, 실제 지연은 09에서.
- **기존 `state` byte 혼동 금지**: `S_EntityState`는 이제 `state`(AI: Patrol/Chase 시각구분용) + `animState`(애니) 둘 다 가짐. 클라(08b)는 animState로 모션, state로 선택적 강조.

---

## ➡️ 다음 Phase

- **Phase 08b** — 클라 애니 구조 (받은 animState를 Animator로 렌더 + enemy 위치 보간)

---

## 📋 박제 (완료 후)

- **복잡 등급** — `08a-anim-state-protocol-server-DONE.md` 박음. (PDL bump + 신규 enum = 결정 기록 가치)

---

## 작업 로그

- 2026-05-30: 계획 수립 (`/work:plan` 애니 상태머신 재편 — 기존 08 enemy-ai-client를 08a/08b로 분리)
