---
owner: youngho
phase: 04
status: done
grade: 복잡
summary: roster 전송 중복(EnterGameWorld/MapMigration)을 GameMap.SendInitialRosterTo()로 통합하며 발산한 closing-skip 가드를 옳은 쪽(Owner null 제외 = BroadcastToAll 정합)으로 통일 + rewind 검증(ValidateRewind)·facingByte 헬퍼 추출. wire 불변, trust-boundary reviewer 🟢.
completed: 2026-06-11
---

# Phase 04 완료 — roster 전송 통합 + rewind/facing 헬퍼

> M4.10 네 번째 Phase. trust-boundary(헌법 §2/§3) 위험 깃발 → reviewer 동반. 거동·wire 불변이 목표인 추출이되, 발산한 가드를 옳은 쪽으로 *수렴*시키는 의도된 미세 거동 정합 1건 포함.

---

## TL;DR

전수조사가 **이미 발산(drift) 중**이라 경고한 중복 둘을 봉합했다.

1. **roster 전송 통합** — player roster(`S_PlayerJoin`) + 살아있는 enemy roster(`S_EntitySpawn`)를 새 진입 세션에 1:1 Send하는 두 루프가 `GameSession.EnterGameWorld`(최초 진입)와 `MapMigration.Execute`(맵 이동)에 복붙돼 있었고, **closing-skip 가드가 갈라져 있었다**: EnterGameWorld `Owner != null && IsClosing`(Owner null이면 *전송*) vs MapMigration `Owner == null skip; IsClosing skip`(Owner null 제외). → `GameMap.SendInitialRosterTo(target, existingPlayers)` 한 메서드로 통합하고 **가드를 옳은 쪽(Owner null 제외 = `BroadcastToAll`/`SendPlayerHp` 정책 정합)으로 통일**.
2. **rewind/facing 헬퍼** — rewind 범위 검증 3줄(음수/미래/상한 초과)이 4벌 → `CombatSystem.ValidateRewind(long,long)` + `CombatConstants.MaxRewindTicks=4L`(매직 `4` 봉인). facingByte 변환(`FacingDir>=0?1:0`) 4곳 → `PlayerEntity.FacingByte` 프로퍼티.

**산출물**:
- `GameMap.SendInitialRosterTo` 신설 — snapshot은 호출부 잔류(AddPlayer 전 `existing`/`existingInDest`), 전송 루프만 추출. enemy는 `this.Enemies` 직접(self-exclusion 무관).
- `GameSession.EnterGameWorld` + `MapMigration.Execute` 두 호출부 → 한 줄. EnterGameWorld의 broadcast joinNotice를 roster 뒤로 이동(각 클라 수신 순서 불변).
- `CombatSystem.ValidateRewind` static 헬퍼 + 4 호출처(ProcessAttack/Teleport/Dash/Thunderbolt) 교체.
- `PlayerEntity.FacingByte` + 4 호출처 교체.
- `ValidateRewindTests`(10케이스 신설) + PacketRoundTrip S_PlayerJoin/S_EntitySpawn 4케이스.

---

## AC 검증 결과

| 완료조건 | 검증 | 결과 |
|---|---|---|
| dotnet test green | unit 541 → **548/552 passed**(Skipped 4=LongRunning 의도, Failed 0) | ✅ |
| roster 단일 경로 + 가드 일치 | EnterGameWorld·MapMigration 둘 다 `SendInitialRosterTo`, `Owner != null && ...IsClosing` 잔존 0 | ✅ |
| rewind 헬퍼 1곳 + 매직 4 봉인 | ValidateRewind 4 호출처 재사용, 리터럴 `4`(rewind 로직) 잔존 0, MaxRewindTicks=4L | ✅ |
| facingByte 헬퍼 1곳 | FacingByte 프로퍼티 4곳 재사용, 인라인 표현식 잔존 0 | ✅ |
| reviewer trust-boundary 통과 | 🟢 5축 통과, 🔴 0 (wire 불변 구조적 보장 + 가드 정합) | ✅ |
| ProtocolVersion 11 불변 | PacketRoundTrip(S_PlayerJoin/S_EntitySpawn) + PDL 무변경 | ✅ |
| 봇 회귀 0 | 6 시나리오 PASS + MapMigrationTests 14/14 | ✅ |

**빌드/테스트** (WSL2): Build 0 Error. unit 548 passed / 0 failed.
**봇** (ADR-029): MultiRoster·EmergencyCombat·Dash·Teleport·Ranged·Thunderbolt 전부 PASS(rewind 통과로 데미지 적용 + facing 정합 실증).

---

## 결정 흐름

1. **가드 통일 방향 = Owner null 제외(MapMigration 버전)** — 추측 금지(plan 명령), 코드 Read로 확정. `GameMap.BroadcastToAll`(`if p.Owner==null continue`) + `SendPlayerHp`(`if p.Owner==null return`)가 이미 "Owner null = 유효 연결 없음 → skip" 정책. EnterGameWorld의 `Owner != null && IsClosing`은 Owner null entity를 roster에 *포함*시켜 유령 플레이어를 그릴 수 있는 잠재 버그 → BroadcastToAll 정합 쪽으로 수렴. **미세 거동 변경 1건이지만 의도된 정합화**(실서버 AddPlayer는 항상 Owner 세팅 → Owner null PlayerEntity는 유닛 테스트에만, production 영향 0).

2. **broadcast joinNotice 위치 이동 안전성** — EnterGameWorld는 원래 `player roster → broadcast → enemy roster` 순서였는데 통합 후 `SendInitialRosterTo(player+enemy) → broadcast`로 바뀐다. wire 불변인 이유: enemy-spawn 수신자는 `self`뿐, broadcast는 `except: self` → 두 수신 집합이 **disjoint**. self 스트림은 old/new 둘 다 `EnterMap→Hp→player-roster→enemy-roster`로 동일(broadcast는 self에 애초에 안 닿음), others는 둘 다 joinNotice만. → 각 클라가 받는 순서 불변. (struct 불변으로는 *부수효과 순서*를 못 잡으므로 손으로 disjoint 논증 — reviewer 독립 확인.)

3. **추출 경계 = snapshot은 호출부, 전송 루프만 헬퍼** — player snapshot(`new(map.Players)`)은 AddPlayer *전*에 찍어야 self 제외가 성립하므로 호출부 잔류. 헬퍼는 snapshot된 목록을 인자로 받아 self 포함 위험 0. enemy는 신규 player가 enemy가 아니므로 self-exclusion 무관 → `this.Enemies.Values` 직접 순회.

4. **ValidateRewind 부등호 1:1** — 3조건(`<0`/`>serverTick`/`-> MaxRewindTicks`)의 부등호·경계를 한 톨도 안 바꿈. `>4`만 reject, `==4`는 통과(`DiffFour_ReturnsTrue`로 박제). lag-comp 거동 불변.

---

## 학습 일지 후보 키워드

- **"wire 불변을 어떻게 *증명*하나" 두 갈래**: (1) **구조적 불변** — 패킷 struct 정의(98_Shared/)를 한 줄도 안 건드렸고 필드 대입식이 텍스트 동일하면, 직렬화 byte는 *논리적으로* 같을 수밖에 없다(diff만으로 단정 가능, 98_Shared diff 0줄이 1차 보장). (2) **회귀 가드** — PacketRoundTrip 추가는 "지금 안 깨졌음" 증명이 아니라 "*미래에* 누가 struct를 건드리면 알림". 거동 불변 리팩토링에서 테스트의 진짜 역할은 현재 증명보다 미래 회귀 차단.
- **부수효과 순서는 struct 불변으로 안 잡힌다**: broadcast 위치 이동 같은 *수신 스트림 순서*는 패킷 정의 불변과 무관한 코드 흐름 문제. "수신 집합이 disjoint한가"를 손으로 따져야 한다(self vs except-self). 이게 trust-boundary 추출에서 가장 미묘한 점검.
- **복붙은 언젠가 발산한다(가드 drift 실례)**: roster 전송이 두 곳에 복붙된 뒤 한쪽 closing-skip 가드만 누가 고쳐 두 경로가 갈라졌다. 통합 = 발산을 *불가능하게* 만들기. "맵 이동 시엔 되는데 첫 입장 시엔 안 되는" 유령 버그의 씨앗을 제거.
- **추출 경계의 "무엇을 남기나"**: snapshot 순서 의존성(self 제외를 위한 AddPlayer 전 찍기)은 race 안전성의 핵심이라 호출부에 남기고, 전송 루프만 뺀다. 순서 의존성을 헬퍼로 끌고 들어가면 호출부 race 추론이 흐려진다.

---

## 후속 후보 (이번 범위 밖)

- **봇 결함**: `MapTransitionScenario.cs`가 `Program.cs`(99_Tools/headless-bot) args 분기에 미등록 → 봇 직접 실행 불가(기능은 MapMigrationTests 14개가 커버). Phase 05/마감 시 1줄 분기 추가. qa 영역.
- **🟡 (reviewer)**: `GameSession.SubmitSkillUse`의 `attackerClientTick`이 `int`(ProcessAttack/ValidateRewind는 `long`) — pre-existing widening, 무영향. 언젠가 skill/attack tick 타입 통일 시 표면 정리.
- Phase 03 후속(SetStageCleared/RemoveEnemy surface 축소)은 04에서 GameMap을 또 편집했으므로 이제 점검 가능 — Phase 05에서.
