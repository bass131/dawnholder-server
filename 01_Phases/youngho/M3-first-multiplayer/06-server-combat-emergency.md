# Phase 06: 서버 응급 전투 인프라 (Combat state + 적 placeholder + 공격 패킷)

> **상태**: pending (Codex β 사전 검증 봉합 박힘, 2026-05-19)
> **마일스톤**: M3 — Multiplayer & Demo Stage
> **예상 소요**: server-only 2.5h / end-to-end 4~5h (Codex 정정)
> **담당 에이전트**: gameplay
> **사전 검증**: [`00_Document/reviews/2026-05-19-m3-phase-06-codex-precommit-review.md`](../../../00_Document/reviews/2026-05-19-m3-phase-06-codex-precommit-review.md) (γ 방식 6회차 = 사전 분해 검증 첫 실측)

---

## 🎯 목표

서버 권위 단순 전투. Codex β 권장 *강한 단순화* — **적 AI 없음, 고정 위치 HP dummy, 공격은 서버 range + cooldown만**. 헌법 #1 서버 권위 + #3 신뢰 경계 *단순화 OK, 위반 X*.

## ⏪ 사전 조건

- [x] Phase 05 완료 (클라 remote entity registry, main `f9e58a0` PR #36)

---

## 📝 작업 내용

### 1. 서버 entity 모델 (`02_Server/GameServer/Combat/` 신설)

- [ ] `02_Server/GameServer/Combat/` 폴더 신설
- [ ] `PlayerEntity.cs:11`에 `int Hp` / `int MaxHp` 필드 추가 (기본 100/100). `IsDead` 프로퍼티(`Hp <= 0`)
- [ ] `EnemyEntity` 신설 — owner 없는 server entity. 필드: `int EntityId / EnemyKind Kind / float X, Y / int Hp, MaxHp / bool IsDead / long LastAttackedTickMs`
- [ ] `EnemyKind` enum — `Normal / Boss` (Phase 07 보스는 본 enum 재사용, 별 entity 분리 X)
- [ ] `GameMap` — `_enemies` Dictionary 보관 분리(players와 별도). broadcast 대상은 players만
- [ ] Enemy spawn = 서버 시작 시 맵 중간 zone에 `Kind=Normal` 1마리 고정 위치, HP 30

### 2. PDL 4패킷 신설 (Codex 권장 모양으로 정정)

**실제 PDL 경로**: `99_Tools/PacketGenerator/PDL.xml` (98_Shared/Protocol/PDL.xml 아님 — 문서 정정)

- [ ] `C_Attack { int targetEntityId }` (ID 11)
  - ⚠️ **attacker 필드 없음** — 서버가 `GameSession._entityId`에서 강제 (헌법 #3, 다른 entityId 도용 차단)
  - ⚠️ direction 모델 X — facing/ray/hitbox 응급 범위 초과 (Codex HIGH #2)
- [ ] `S_EntitySpawn { int entityId, byte entityKind, float x, float y, int currentHp, int maxHp }` (ID 12)
  - ⚠️ **신설 (Codex HIGH #1)** — 클라가 target id를 알 수 있게 함. 누락 시 end-to-end 데모 막힘
  - 신규 client 접속 시 모든 active enemy spawn 다발 전송 (initial roster 패턴, Phase 04 정합)
- [ ] `S_HitResult { int attackerEntityId, int targetEntityId, int damage, int currentHp, int maxHp }` (ID 13)
  - ⚠️ **HpUpdate 통합 (Codex MEDIUM #3)** — damage text + HP bar 동시 처리, broadcast 비용 1배로
- [ ] `S_EntityDeath { int entityId }` (ID 14)
  - ⚠️ idempotent broadcast (한 번만) — `IsDead` 플래그로 보장
- [ ] `ProtocolVersion.Current = 2 → 3` bump (Codex MEDIUM #4, handshake exact equality라 stale client 빠른 cutoff)
- [ ] PacketGenerator 재생성 (`--no-manager`) + Shared.dll commit + Unity DLL 자동 복사 확인
- [ ] HandshakeHandlerTests 기대값 bump 반영 (clientVersion=3 happy, =2 mismatch)

### 3. 서버 공격 처리 (`AttackHandler` + `GameMap.EnqueueJob` 패턴)

- [ ] `AttackHandler` 신설 — Handler 패턴 (Phase 03 박힘) 정합:
  - decode + `session.SubmitAttack(targetEntityId)` 호출만. mutation 직접 X
  - handler 안 await/Task.Delay/Thread.Sleep 금지 (헌법 #5)
- [ ] `GameSession.SubmitAttack(int targetEntityId)` 신설 — map에 job enqueue (tick 안 처리)
- [ ] `GameMap.Tick` 안 attack job 처리:
  - rate-limit 500ms (`session.LastAttackTickMs` 검사, 초과 시 silent drop = no HP change + no broadcast)
  - target exists / `Kind` enemy 또는 boss / `!IsDead` / 같은 map 확인
  - 서버 권위 position만으로 `dist² < range²` hit 판정 (sqrt 회피, lag comp 없음 응급)
  - 고정 데미지 10. enemy `Hp -= 10`, `S_HitResult` broadcast (전원)
  - `Hp <= 0` 시 `IsDead = true` + `S_EntityDeath` broadcast 1회 + map에서 제거 (다음 tick부터 invisible)
- [ ] `GameSession.OnEnterGameWorld` 또는 `EnterMap` 흐름에 active enemy `S_EntitySpawn` 다발 전송 추가

### 4. 단위 테스트 (`AttackHandlerTests`)

- [ ] `Happy` — 공격 → HP 감소 → S_HitResult broadcast 1회
- [ ] `OutOfRange` — range 밖 attack → no HP change + no broadcast (silent)
- [ ] `RateLimitViolation` — 500ms 안 2회 → 1회만 HP 변경, 2회차 silent drop
- [ ] `AuthFailure` — handshake 미완 상태 attack → Disconnect (first-packet 강제 정합)
- [ ] `KillBroadcast` — HP 30 + 데미지 10 × 3회 → 3번째에 `S_EntityDeath` 1회 broadcast + enemy 제거
- [ ] `DuplicateDeath` — 죽은 enemy에 추가 attack → idempotent (HitResult/Death 추가 broadcast X)

### 5. 시연 검증

- [ ] 1인 Unity + 1봇 응급 시연 — 클라 공격 → enemy HP 감소 → 사라짐
- [ ] ⚠️ 점프 공격 miss 가능 (Phase 05 Y mispredict) — **시연은 지상 공격 위주** 또는 range 넉넉히

---

## ✅ 완료 조건

- [ ] **Spawn 흐름** — 클라 접속 시 enemy 1마리 화면 표시 (S_EntitySpawn 수신 + 그리기)
- [ ] **공격 → hit → HP 감소 → broadcast → 클라 표시** — 정상 흐름 시연 가능
- [ ] **enemy HP 0 → S_EntityDeath broadcast 1회 → 클라에서 사라짐**
- [ ] **rate-limit 위반 (500ms 안에 2회)** → "silent drop" (no HP change + no broadcast) — *"거절"이라는 표현은 옛 용어, 테스트 기대값 = no-op* (Codex MEDIUM #5)
- [ ] **공격 range 밖** → no HP change + no broadcast (silent)
- [ ] **handler 단위 테스트 페어 통과** (6건)
- [ ] **ProtocolVersion bump** — `Current = 3` + handshake 테스트 기대값 갱신
- [ ] `dotnet build Dawnholder.slnx --nologo` PASS (경고 0)

---

## 🛡️ Option B (시간 부족 시 — 18:00 트리거)

end-to-end 추정이 4~5h라 5/20 면담 전 위험. **18:00에 진행률 미달이면 Option B 강제 전환**:

- **3패킷으로 축소** — `C_Attack` + `S_EntitySpawn` + `S_HitResult(currentHp/maxHp 포함)`
- **`S_EntityDeath` 별도 생략** — 클라가 `S_HitResult.currentHp == 0` 보고 despawn (server는 map에서 제거 + 다음 spawn에 포함 안 시킴)
- ⚠️ **Phase 07 `S_StageClear`는 별도 유지** — boss death + stage clear는 독립 이벤트 (Codex 명시)
- 시연 핵심 = "공격 → enemy 사라짐"만 보이면 OK. damage text 생략, HP bar만

---

## 🧪 테스트

**자동**: `AttackHandlerTests` 6건 (Happy / OutOfRange / RateLimitViolation / AuthFailure / KillBroadcast / DuplicateDeath)
**수동**: Unity 클라 + 헤드리스 봇 = enemy spawn → 공격 → HP 감소 → death → 사라짐

---

## 📚 학습 포인트

- **Combat state 분리** — `PlayerEntity.Hp`는 *전투 상태*. 이동 상태(position/velocity)와 분리 가치 (응집도, 직무 분리)
- **서버 권위 hit 판정** — 클라 = "I attempted attack on target Y" / 서버 = "you hit/missed, damage Z, currentHp W" (헌법 #1 코드 시연 2번째)
- **attacker 강제 패턴** — 패킷에 attacker 필드 *없음*. `session._entityId`에서 강제 → entityId 도용 불가 (헌법 #3 정합 표준 패턴)
- **`GameMap.EnqueueJob` + Handler decode-only** — Handler는 decode + 검증, mutation은 tick 안 (헌법 #5 정합)
- **응급 단순화 trade-off** — `lag compensation` / `정밀 hitbox` / `데미지 공식 풀세트`는 M4. 응급은 *덜 박더라도 권위·신뢰경계는 지킴*
- **`dist² < range²` 패턴** — sqrt 회피 (성능 + 정밀도). N 작아서 비용 무의미하지만 표준 패턴
- **rate-limit silent drop** — 응급은 no-op (응답 X), 본 마감은 cheat-flag 별도 (Codex MEDIUM #5)
- **packet 통합 vs 분리 trade-off** — HitResult가 HpUpdate 흡수 = broadcast 1배 / damage + HP 동시 처리. 분리는 UI 이벤트 ↔ 상태 갱신 직무 분리지만 응급은 통합 우선
- **ProtocolVersion bump 타이밍** — additive 변경이라도 stale client cutoff 위해 bump 권장 (Codex MEDIUM #4)
- **enemy spawn 패킷 누락 함정** — Codex 사전 검증 발견 HIGH #1. spawn 경로 없이 attack 패킷만 박으면 클라가 target id 못 받음

---

## ⚠️ 함정 / 주의사항

- **클라 데미지 직접 계산** → #1 위반. 클라는 시각 표시만 (서버에서 받은 `currentHp` 그대로)
- **attacker 패킷 필드 포함** → #3 위반 (entityId 도용 가능). 패킷에 *넣지 말 것*
- **rate-limit 누락** → #3 위반 (1초에 1000번 공격 가능)
- **공격 sender 검증 누락** → 다른 entityId 도용 공격 가능 (#3 위반)
- **client 보낸 position으로 range 검증** → #1/#3 위반. 서버 권위 position만
- **lag compensation 안 한 게 헌법 위반은 X**. 단 면담에서 "본 마감엔 lag comp 박을 것" 메모 (M4 backlog)
- **enemy spawn 시점** — 응급은 *서버 시작 시 1회*. 첫 플레이어 접속 시 active enemy roster 다발 전송
- **`S_EntitySpawn` 누락하면 데모 막힘** — Codex HIGH #1, 사전 검증 핵심 발견
- **death broadcast 중복** — `IsDead` flag로 1회 보장. 중복 attack job도 idempotent no-op
- **점프 공격 miss** — Phase 05 Y mispredict 잔류. 시연은 지상 공격 위주 또는 range 넉넉히
- **PDL 경로** — `99_Tools/PacketGenerator/PDL.xml` (98_Shared/Protocol/PDL.xml *아님*)
- **packet ID range 예약 문서 결함** — `98_Shared/CLAUDE.md`의 "1000~ / 3000~" 예약은 현 generator(append 순서 ++packetID)와 불일치 (Codex LOW). 응급은 append-only 11~15, M4에서 정합 fix

---

## ➡️ 다음 Phase

Phase 07 — 서버 보스 + Stage Clear. `EnemyKind.Boss`로 재사용, `S_StageClear` 별 패킷 (ID 15), boss death + stage clear 독립 이벤트.

---

## 작업 로그

- 2026-05-18: pending (Codex β 발견 2 = 전투 과소추정, 강한 단순화로 봉합)
- 2026-05-19: **Codex β 사전 검증 봉합 (γ 6회차 첫 실측)** — 즉시 봉합 5건 박음:
  - HIGH #1: `S_EntitySpawn` 신설 (enemy spawn/identity 패킷 누락 봉합)
  - HIGH #2: `C_Attack { targetEntityId }` 고정, attacker는 session 강제 (direction 모델 폐기)
  - MEDIUM #3: `S_HitResult` + `S_EntityHpUpdate` 통합 (currentHp/maxHp 포함)
  - MEDIUM #4: `ProtocolVersion.Current = 2 → 3` bump 명시
  - MEDIUM #5: "rate-limit 거절" → "silent drop: no HP change + no broadcast" 표현 정정
  - 추가: Option B 정의 (18:00 트리거, 3패킷 축소), PDL 경로 정정, EnemyKind 통합 결정 (별 entity X), packet ID 11~14 + 07=15 확정
