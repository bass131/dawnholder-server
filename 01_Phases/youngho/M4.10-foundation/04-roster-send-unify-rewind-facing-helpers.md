---
owner: youngho
phase: 04
status: pending
grade: 복잡
summary: roster 전송 중복(EnterGameWorld/MapMigration)을 GameMap.SendInitialRosterTo()로 통합(closing-skip 가드 일치) + rewind/facingByte 검증 헬퍼 추출
---

# Phase 04: roster 전송 통합 + rewind/facing 검증 헬퍼

> **상태**: pending
> **마일스톤**: M4.10
> **등급**: 복잡 (server 도메인)
> **담당**: server Worker(Sonnet) + reviewer
> **의존**: Phase 01 (컨벤션 v6 §2.5 DRY). 03과 둘 다 GameMap 편집 → **03 → 04 순차**
> **위험**: trust-boundary (roster = 헌법 §2 패킷 구성 영역, reviewer 동반 필수)

---

## 🎯 목표

전수조사가 **이미 발산(drift) 중**이라 경고한 중복 두 종을 봉합한다.

1. **roster 전송 중복** — player roster(`S_PlayerJoin`) + enemy roster(`S_EntitySpawn`) 전송 루프가 `GameSession.EnterGameWorld`와 `MapMigration.Execute`의 destMap 람다에 거의 동일하게 복붙(MapMigration 주석 "EnterGameWorld 패턴 정합"이라 자인). **이미 발산했다**: closing-skip 가드가 한 곳은 `Owner != null && IsClosing`, 다른 곳은 `Owner == null then IsClosing`으로 *다르다* — 잠재 동기화 버그의 실증. → `GameMap.SendInitialRosterTo(session)` 한 메서드로 통합하고 **가드를 옳은 쪽으로 통일**한다.
2. **rewind 범위 검증 3줄**(음수/미래/200ms 초과)이 `CombatSystem.ProcessAttack` + `SkillSystem` 3개 Process*에 **4벌** → `ValidateRewind` 헬퍼 + `CombatConstants.MaxRewindTicks`(매직 넘버 4 봉인). **facingByte 변환**(`FacingDir >= 0 ? 1 : 0`)이 4곳 → 헬퍼/프로퍼티.

이 Phase가 끝나면 **roster 전송이 단일 경로(가드 일치)가 되고, rewind 검증·facing 변환이 헬퍼 한 곳에서 나온다** — wire 동일·거동 불변이 목표다.

---

## ⏪ 사전 조건

- [ ] **Phase 03 완료** — GameMap 편집이 겹치므로 03의 HandleEnemyDeath가 먼저 머지된 상태에서 착수(충돌 방지)
- [ ] Phase 02 완료 — `CombatConstants`에 상수 추가 패턴 확립(MaxRewindTicks도 여기 들어감)
- [ ] 전수조사 output `rootCauses` 중 "roster/enemy 전송 + trust-boundary 패킷 구성 두 파일 중복" + "전투 검증·변환 보일러플레이트 N벌" 섹션 file:line:
  - roster: `GameSession.EnterGameWorld` (player ~L165, enemy ~L193) vs `MapMigration.Execute` (player ~L147, enemy ~L162)
  - rewind: `CombatSystem.ProcessAttack` ~L49-51, `SkillSystem.ProcessTeleport` ~L41 / `ProcessDash` ~L87 / `ProcessThunderbolt` ~L170
  - facingByte: `CombatSystem.ProcessAttack` ~L79, `SkillSystem` ~L65/L148/L210
- [ ] **closing-skip 가드 두 버전 중 어느 게 맞는지 코드 Read로 확정** — `Owner != null && IsClosing` vs `Owner == null then IsClosing`. 어느 쪽이 의도(닫히는 세션은 skip)에 맞는지 판단

---

## 📝 작업 내용

> roster 통합(trust-boundary, reviewer 동반) → rewind/facing 헬퍼(로직 1:1 동일).

**server — roster 통합 (02_Server/GameServer/Maps/GameMap.cs)**:
- [ ] `internal void SendInitialRosterTo(GameSession target)` 신설 — GameMap이 Players/Enemies/BroadcastToAll을 소유하므로 자연스러운 자리:
  1. 기존 player 전원 → closing-skip 후 `S_PlayerJoin` Send
  2. 살아있는 enemy 전원(IsDead skip) → `S_EntitySpawn` Send
- [ ] **closing-skip 가드를 한 가지로 통일** — Read로 확정한 *옳은* 버전으로 (현재 두 곳 발산 중)
- [ ] `GameSession.EnterGameWorld` 람다 + `MapMigration.Execute` destMap 람다 양쪽을 `map.SendInitialRosterTo(session)` 한 줄로 수렴
- [ ] **roster snapshot 순서 의존성**(AddPlayer 전 snapshot)은 *호출부에 남김* — 전송 헬퍼만 추출해 race 추론 영향 0

**server — rewind/facing 헬퍼**:
- [ ] `CombatConstants.MaxRewindTicks = 4` 신설 (200ms @ 20TPS) — 4벌 리터럴 `4`를 명명 상수로
- [ ] `ValidateRewind(long clientTick, long serverTick)` 헬퍼 추출(예: CombatSystem internal static) — 음수/미래/MaxRewindTicks 초과 3줄을 1메서드로. `CombatSystem.ProcessAttack` + `SkillSystem` 3개 Process*가 호출
- [ ] **facingByte 헬퍼** — `PlayerEntity.FacingByte => FacingDir >= 0 ? (byte)1 : (byte)0` 프로퍼티(또는 static `ToFacingByte`). 4곳 인라인 표현식을 이 프로퍼티 참조로

**reviewer (trust-boundary 동반)**:
- [ ] roster 통합 후 **wire 동일** 점검 — `S_PlayerJoin`/`S_EntitySpawn` 패킷 구성이 통합 전과 byte 단위로 동일한지(헌법 §2)
- [ ] 가드 통일이 *기능*을 바꾸지 않는지 — 닫히는 세션 skip 거동이 두 경로에서 동일해졌는지

**qa / 테스트**:
- [ ] `ValidateRewind` 단위 테스트 — 음수/미래/초과 경계값에서 reject, 유효 범위에서 accept
- [ ] roster 통합 후 맵 이동(MapMigration) 봇 시나리오 회귀 — 진입 시 player/enemy roster가 통합 전과 동일하게 도착
- [ ] PacketRoundTrip + ProtocolVersion==11 assert

---

## ✅ 완료 조건 (정량)

- [ ] `dotnet test` **green**
- [ ] **roster 전송 단일 경로 + 가드 일치** — `EnterGameWorld`/`MapMigration` 둘 다 `SendInitialRosterTo` 호출, closing-skip 가드가 단 한 버전(Grep으로 발산 0 확인)
- [ ] **rewind 검증 헬퍼 1곳** — `ValidateRewind`가 4 호출처에서 재사용, 매직 `4`가 `MaxRewindTicks` 상수로(리터럴 4 잔존 0). facingByte 변환도 헬퍼 1곳
- [ ] **reviewer trust-boundary 통과** — 헌법 §2 패킷 구성 정합 + 통합 후 wire 동일 확인
- [ ] **ProtocolVersion 11 불변** — PacketRoundTrip green + `Current == 11`

---

## 🧪 테스트

**자동**:
- `ValidateRewindTests` — clientTick 음수 / serverTick 초과(미래) / `serverTick - clientTick > MaxRewindTicks` 각각 reject, 유효 범위 accept
- `MapMigrationTests`(또는 동형) — 맵 이동 시 SendInitialRosterTo 경로로 player/enemy roster 정합
- `PacketRoundTripTests` — S_PlayerJoin/S_EntitySpawn wire 불변 + ProtocolVersion 11

**수동**:
- 2클라 맵 이동 실측 — 다른 맵으로 넘어갈 때 그 맵의 기존 플레이어·적이 통합 전과 동일하게 보임(roster 누락 0)

---

## 📚 학습 포인트

- **중복이 어떻게 동기화 버그 씨앗이 되나 (가드 발산 실례)**: roster 전송이 두 곳에 복붙됐고, 그동안 한 곳의 closing-skip 가드만 누가 고쳐서 `Owner != null && IsClosing` vs `Owner == null then IsClosing`으로 *갈라졌다*. 지금은 미세한 차이지만, 이게 바로 "복붙은 언젠가 발산한다"의 실증이다 — 한쪽만 고쳐지면 두 경로가 다르게 행동해 "맵 이동 시엔 되는데 첫 입장 시엔 안 되는" 유령 버그가 된다. 통합 = 발산 불가능하게 만들기.
- **trust-boundary 코드를 손댈 때 reviewer 동반 이유**: roster는 헌법 §2(프로토콜 신성) 영역 — 패킷을 손으로 구성하는 코드다. 통합 과정에서 필드 하나라도 빠지거나 순서가 바뀌면 wire 불일치(클라가 못 읽음)가 된다. 사람 둘이 보는 게 아니라, "이 변경이 wire를 안 바꿨는가"라는 *되돌리기 비싼* 판정을 reviewer가 독립적으로 한 번 더 검증하는 것.
- **헬퍼 추출 시 "무엇을 남기나"**: roster snapshot *순서*(AddPlayer 전에 기존 목록을 찍어야 함)는 race 안전성의 핵심이라 호출부에 남기고, *전송 루프*만 헬퍼로 뺀다. 순서 의존성을 헬퍼 안으로 끌고 들어가면 호출부에서 race 추론이 흐려진다.

---

## ⚠️ 함정 / 주의사항

- **패킷 구성(헌법 §2)이라 통합 후 wire가 동일한지 검증 필수** — S_PlayerJoin/S_EntitySpawn의 필드·순서·byte가 통합 전과 1:1 동일해야 한다. PacketRoundTrip + reviewer 이중 게이트.
- **closing-skip 가드 두 버전 중 *옳은* 쪽으로 통일** — 어느 게 맞는지 추측 말고 코드 Read로 확정. 의도는 "닫히는 중인 세션엔 안 보냄"인데, 두 표현이 *Owner가 null일 때* 다르게 행동한다(한쪽은 null이면 skip, 한쪽은 null이면 IsClosing 체크 자체를 건너뜀). 어느 쪽이 의도에 맞는지 판단 후 통일.
- **03과 GameMap 충돌** — 03(HandleEnemyDeath)도 GameMap을 편집했다. 03 머지 후 착수(순차). 병렬 시 머지 충돌.
- **rewind 헬퍼는 로직 1:1 동일** — 음수/미래/초과 3조건의 부등호·경계를 한 톨도 바꾸지 말 것. 거동이 바뀌면 lag compensation 동작이 달라진다(거동 변화 = 버그).
- **MaxRewindTicks는 long** — tick은 long이므로 상수도 long(`4L` 또는 명시 타입). int로 박으면 비교 시 캐스팅 노이즈.
- **복잡 등급 유지 사유** — 코드 추출만·wire 불변·reviewer trust-boundary 게이트로 *검증 동원*은 대규모급 충족(양식만 복잡). grade-and-risk 자동상향을 회피가 아니라 *명시적 흡수*.

---

## ➡️ 다음 Phase

- Phase 05 (컨벤션 강제 스윕 + 진입점맵 작성 + 전체 회귀 + 마감) — 01~04 전부 머지 후.

---

## 📋 박제 (완료 후 -DONE.md)

- 복잡 등급 → `-DONE.md` 박음 (roster 통합 + 가드 발산 봉합 + rewind/facing 헬퍼, trust-boundary 통과 + wire 동일 사실 박제).

---

## 작업 로그

- 2026-06-11: 계획 작성 (전수조사 "roster 두 파일 중복, 이미 가드 발산" + "rewind 4벌·facingByte 4벌" rootCause → SendInitialRosterTo 통합 + ValidateRewind/FacingByte 헬퍼. trust-boundary 위험 깃발 → reviewer 동반)
