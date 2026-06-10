---
owner: youngho
milestone: M4.10
title: 코드 기반 정비 — 컨벤션 v6 측정 기준 확정 + 저위험 중복 정리
status: planned
grade: 대규모
slug: M4.10-foundation
created: 2026-06-11
domains: [shared, server, qa]
---

# M4.10 — 코드 기반 정비 (컨벤션 v6 + 저위험 중복 정리)

> 직전 main = M4.9 ProtocolVersion 11 마감(스킬 시스템 완성 — 비주얼 + 클래스 게이트 + Dash/Teleport + 쿨다운 UI).
> 이 마일스톤은 **새 기능을 더하지 않는다.** "측정 기준(컨벤션 v6)"을 세우고, 그 기준으로 잡힌 **저위험 중복**을 정리한다.

---

## Context (왜)

발표(6/17) 전 영호가 "스파게티가 많은 것 같다"고 느껴 **프로덕션 게임 코드 전수조사**(11개 영역 × Audit/Verify 2단계)를 돌렸다. 결론은 이렇다:

> **"골격은 건강하다. 진짜 병은 스파게티가 아니라 중복(duplication)이다."**

전수조사가 독립 검증(Verify)으로 확정한 사실:

1. **스파게티는 절반이 오탐**이었다. `EnterGameWorld`·`EnemyAttackHandler`·`ClientPacketHandlers` SRP 위반은 전부 `confirmed=false` — "긴 함수"를 "스파게티"로 과대평가한 것이다. System 분리(CombatSystem/SkillSystem/DeferredDamageSystem 등), FSM 인프라, 상수 중앙화(CombatConstants), trust-boundary 검증(Volatile/Interlocked), tick 논블로킹 원칙이 헌법대로 잘 지켜진다.
2. **진짜 아픈 곳은 중복이다.** 똑같은 "적 사망 처리" 13~15줄 블록이 byte 단위로 **세 파일에 복붙**됐고(작성자 본인 주석이 "CombatSystem과 동일"이라 자인), rewind 검증 3줄과 facingByte 변환이 각 4벌, roster 전송 루프가 두 파일에 거의 동일하게 산재한다.
3. **중복이 이미 미세하게 발산(drift)하기 시작했다.** roster 전송의 closing-skip 가드가 한 곳은 `Owner != null && IsClosing`, 다른 곳은 `Owner == null then IsClosing`으로 미묘하게 다르다 — **잠재 동기화 버그의 씨앗**이다.
4. **흩어진 게임플레이 매직 넘버**가 단일 출처 없이 산재한다(히트박스 0.5f, de-aggro 1.5f, 속도 epsilon 0.05f 등). hitEffect(0/1/2/3)는 raw byte로 비교 — 98_Shared에 enum이 없어 런타임 의미가 코드에 없다.

### CODE_CONVENTION v5 부록 A 미실행 갭

`CODE_CONVENTION.md`(현 v5)는 §0~§6 + 부록 A로 "이상적 도착점"을 선언했지만, 부록 A의 **실측이 stale**하다. 예를 들어 부록 A는 `GameMap (665줄)`을 "4 도메인 God class"로 적었으나, **실측 결과 GameMap은 이미 436줄로 줄었고 6개 System(Combat/Boss/Deferred/EnemyAI/Respawn/Skill)이 분리 완료**됐다 — 부록 A에서 졸업해야 한다. 반대로 `UnityClientSession (665줄)`도 실측은 213줄로 줄었고, **진짜 미실행분은 `ClientPacketHandlers.cs` 909줄**이다(여기엔 inline 핸들러 + VFX 보일러플레이트가 몰려 있음). 컨벤션이 "선언"만 하고 "강제 스윕"을 안 했기에 멤버 정렬·진입점 맵 같은 측정 도구도 비어 있다.

### 이 마일스톤의 책임 경계 (스코프 분리)

전수조사 로드맵(8 step)을 셋으로 갈랐다. M4.10은 **측정 기준 + 저위험 중복**만 담는다:

- **M4.10 (이번)** — 컨벤션 v6(측정 기준) 확정 + 멤버정렬 Roslyn + 진입점 맵 + 전수조사가 `risk=low`로 판정한 서버측 순수 추출(적 사망 통합 / 매직넘버 단일화 / roster 통합 / rewind·facing 헬퍼). 거동 불변, 회귀 0 목표.
- **M4.11 (분리)** — **동기화(synchronization)** 작업. 전수조사가 drift 위험으로 지목한 부분의 *동작 검증*과 RemoteEntity 보간·타임소스 정합 등. M4.10이 만든 ENTRY_POINTS.md 진입점 맵이 M4.11 디버깅 자산이 된다.
- **M4.12 (분리)** — M4.9 잔여 정리 + `UnityClientSession`/`ClientPacketHandlers` 분리(클라 VFX 헬퍼 통합·핸들러 4분할). 촬영(발표) 영상 경로라 발표 이후로 미룬 분량. `risk=medium~high` 구간.

> ⚠️ **왜 LocalPlayerMovement는 이번에 안 건드리나**: 전수조사가 "절대 금지"로 못 박았다. 방금 force-adopt/dash/reconcile/source-gating을 봉합한 따끈한 코드라, 쿨다운 배열화·SkillCooldownTracker 분리는 reconcile 발산을 유발할 수 있다. M5 DI 도입과 묶어야 안전.

---

## 설계 결정 (확정)

### 1. 컨벤션은 "측정 기준" — 현 코드를 정당화하지 않는다
v6는 v5의 4철학(§0)을 잇되, **멤버 정렬·진입점 맵·DRY·책임 헤더**라는 *측정 가능한* 4개 도구를 추가한다. 부록 A는 옛 줄 수가 아니라 **실측 스냅샷**으로 갱신한다(GameMap은 졸업, ClientPacketHandlers 909줄을 강조).

### 2. 매직넘버·hitEffect는 98_Shared 단일 진실 — wire 모양 무변경
흩어진 매직넘버를 98_Shared 한 곳으로 모은다. `HitEffect` enum을 98_Shared에 신설하되 **byte 의미만 바꾼다**(wire 모양 그대로) — **ProtocolVersion 11 불변**. enum이라 런타임 의미가 코드에 박혀 raw byte 비교보다 안전하다.

### 3. 적 사망 처리는 GameMap.HandleEnemyDeath() 한 곳으로 — tick 단일 스레드라 거동 불변
3중 복붙(CombatSystem/DeferredDamageSystem/SkillSystem)을 `GameMap.HandleEnemyDeath(EnemyEntity)` 한 메서드로 추출. 호출 mutator(SetStageCleared/RemoveEnemy/EnqueueRespawn)가 이미 GameMap internal이라 추출이 깨끗하고, `BossStageClearTests`(S_EntityDeath→S_StageClear 순서 계약)가 회귀를 즉시 방어한다.

### 4. roster 통합은 trust-boundary — reviewer 동반 + wire 동일 검증
roster 전송 중복을 `GameMap.SendInitialRosterTo(session)`로 통합하되, 이미 발산한 closing-skip 가드를 **옳은 쪽으로 통일**한다(코드 Read로 확정). 헌법 §2(프로토콜 신성) 영역이라 reviewer trust-boundary 통과 + 통합 후 wire 동일을 검증 게이트로 둔다.

---

## Phase 분해 (5개)

| # | Phase | 등급 | 도메인 | 담당 | 의존 | 완료 조건(정량) |
|---|---|---|---|---|---|---|
| 01 | 컨벤션 v6 확정 + Roslyn 멤버정렬 + 진입점맵 골격 | 복잡 | shared(문서·설정) | shared Worker | — (선행) | CODE_CONVENTION v6 박힘 · 부록 A가 실측과 정합(GameMap 졸업) · `.editorconfig` 멤버정렬 룰이 빌드 경고로 작동 · ENTRY_POINTS.md 골격 존재 · 코드 변경 0 |
| 02 | 흩어진 매직넘버 98_Shared 단일화 + HitEffect enum | 복잡 | shared+server | shared+server Worker | 01 | `dotnet test` green(값 불변) · 각 매직넘버 단일 출처 · HitEffect enum 양쪽 사용 · **ProtocolVersion 11 불변** · Shared.dll 재빌드→Plugins 갱신 |
| 03 | 적 사망처리 3중복붙 → GameMap.HandleEnemyDeath() 통합 | 복잡 | server | server Worker | 01 | `dotnet test` green · **BossStageClearTests 회귀 0** · 봇 회귀 0 · 3 호출처가 단일 메서드 호출 |
| 04 | roster 전송 통합 + rewind/facing 검증 헬퍼 | 복잡 | server + reviewer | server Worker + reviewer | 01 (위험: trust-boundary) | `dotnet test` green · roster 단일 경로 + 가드 일치 · rewind 검증 헬퍼 1곳 · **reviewer trust-boundary 통과**(wire 동일) · ProtocolVersion 11 불변 |
| 05 | 컨벤션 강제 스윕 + 진입점맵 작성 + 전체 회귀 + 마감 | 복잡 | 메인+qa | 메인 + qa | 01~04 전부 | Roslyn 멤버정렬 경고 0 · ENTRY_POINTS.md 채워짐 · 전체 `dotnet test`+봇 green · Unity 콘솔 error CS 0 · DONE 박제 |

---

## 의존성 그래프

```
01(컨벤션 문서) ──→ 02(매직넘버) ──┐
                  ├→ 03(적사망)   ─┼─→ 05(스윕+진입점맵+마감)
                  └→ 04(roster+검증)┘
02·03·04는 01 후 착수. 03·04는 둘 다 GameMap 편집이라 순차 권장(병렬 시 충돌).
```

- **01은 선행**: 컨벤션 v6(측정 기준)·멤버정렬 룰·진입점 맵 골격이 박혀야 02~04가 그 위에 올라탄다.
- **02 ∥ 03 ∥ 04 (단 03·04 순차)**: 02는 shared+server 매직넘버라 독립. 03(적사망)·04(roster)는 둘 다 `GameMap.cs`를 편집하므로 **병렬 시 머지 충돌** — 03 → 04 순차 권장.
- **05는 전체 후**: 멤버정렬 Roslyn 전체 스윕은 02~04 코드가 다 박힌 뒤라야 한 번에 깔끔. 진입점 맵 본문도 통합된 구조 위에서 작성.

---

## 프로토콜 (변경 없음)

- **ProtocolVersion 11 불변.** 이 마일스톤은 패킷을 추가하거나 모양을 바꾸지 않는다.
- `HitEffect` enum 신설은 **byte 의미만 추가** — C_/S_ 패킷 필드 모양 불변. 각 server Phase(02/04) 완료 시 **PacketRoundTrip + ProtocolVersion==11 assert**로 wire 불변을 못 박는다.
- ⚠️ 만약 구현 중 정말 wire 변경이 필요해지면 그 시점에 **STOP → 사용자 의논**(irreversible 깃발 경로). 이 마일스톤 전제는 "wire 무변경"이다.

---

## 검증

1. **dotnet test**(`GameServer.Tests/`): 각 Phase 완료마다 기존 회귀 **0**. 특히 03은 `BossStageClearTests`(사망→StageClear 순서 계약), 02는 값 불변(매직넘버 동치).
2. **헤드리스 봇**(Scenarios/): 기존 전 시나리오(RangedHitSmoke/ThunderboltAoeSmoke/DashSmoke/TeleportSmoke/BossStageClear 등) 회귀 0. 거동 불변이 목표라 봇 결과가 마일스톤 전후로 동일해야 한다.
3. **Roslyn 멤버정렬**: 05에서 전체 스윕 후 경고 0. diff는 크지만 동작 불변 — 회귀 테스트로 확인.
4. **Unity 콘솔**: Shared.dll 재빌드 후 error CS 0.

---

## 리스크 / 범위 밖

- **trust-boundary 위험 깃발**(Phase 04): roster 전송은 헌법 §2 패킷 구성 영역. reviewer 동반 필수 + 통합 후 wire 동일 검증. closing-skip 가드 두 버전 중 *옳은* 쪽으로 통일(코드 Read로 확정).
- **거동 불변이 절대 조건**: 이 마일스톤은 "리팩토링"이지 "기능 변경"이 아니다. 모든 Phase는 추출 전후 봇·테스트 결과가 동일해야 한다. 한 줄이라도 거동이 바뀌면 그건 버그다.
- **부록 A를 실측 없이 박지 말 것**: GameMap은 이미 분리됐다(436줄, 6 System). 옛 665줄로 박으면 컨벤션이 거짓말을 한다.
- **범위 밖 명시**:
  - **LocalPlayerMovement 절대 금지** — reconcile 발산 위험(M5 DI와 묶음).
  - **동기화 동작 검증** — M4.11로 분리.
  - **UnityClientSession/ClientPacketHandlers 분리·클라 VFX 헬퍼·핸들러 4분할** — 발표 영상 경로라 M4.12로 분리.
  - GameSession 5책임 분해 / BossStates 부활 블록 추출 / EnemyEntity Boss 필드 분리 — 발표 후(medium~high).

---

## 승인 후 절차
1. Phase def 5개 작성 → **plan-auditor 자동 호출**(Tier 2-B).
2. 통과 후 01(컨벤션=shared Worker)부터 시작 → 02·03·04 → reviewer(특히 04) → 메인 직접 실측 → commit.
3. PR — 98_Shared 변경(02·HitEffect enum) → Shared.dll → 03_Client CODEOWNERS(정유현) co-review → admin 머지 예상. **각 PR 사용자 명시 GO.**
4. 마감 시 `_milestone-DONE.md`+`.html` 5단계 보고(대규모) + CHANGELOG[M].
