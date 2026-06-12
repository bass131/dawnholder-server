---
owner: youngho
milestone: M4.12
title: M4.9 스킬 잔여 마무리 + 클라 핸들러 분리 + 발표 재빌드
status: planned
grade: 대규모
slug: M4.12-skill-finish
created: 2026-06-11
domains: [shared, server, client, qa]
---

# M4.12 — M4.9 스킬 잔여 마무리 + 클라 핸들러 분리 + 발표 재빌드

> 직전 = M4.11(동기화 정돈). 이 마일스톤이 **M4.9 스킬 시스템의 미보고 잔여를 회수해 마감**하고, 전수조사 B(클라 핸들러 분리)를 합류시킨 뒤, **발표 재빌드**로 닫는다.

---

## Context (왜)

M4.9(스킬 시스템)는 **"기능 골격 완료, 마감 미보고"** 상태다. Dash/Teleport가 서버+클라 양쪽에서 동작하고 이펙트 prefab도 들어왔지만, `-DONE.md`가 **0개** — 사실 박제가 안 된 채 세션이 중단됐다(`wip(skill): ... 세션 중단 보존` 커밋이 증거). **본 마일스톤이 M4.9의 잔여를 회수해 정식으로 마감**한다.

여기에 전수조사 **B(중위험) — 클라 패킷 핸들러 분리**도 합류시킨다. M4.10이 발표 영상 경로라 미뤘던 `ClientPacketHandlers.cs`(909줄) 분리가 이 마일스톤의 몫이다.

그리고 **발표 재빌드가 이 마일스톤 끝의 마지막 게이트**다. 스킬 마감 + 핸들러 분리 + 쿨다운 UI가 다 들어간 클라를 재빌드해 발표 자산으로 박는 것이 종착점이다.

---

## Split B — M4.13과의 관계 (2026-06-12 영호 확정)

이 세션에서 대쉬에 **시각 스터터**(20Hz 뚝뚝 끊김)가 발견됐다. 근본 원인은 대쉬 하나가 아니라 **"클라가 예측 못 하는 서버 임펄스 동작" 클래스**(대쉬·넉백·임펄스공격)의 공통 문제로 드러났다 → "임펄스 동작 클래스 재설계"가 필요해졌다. 그런데 그걸 이 마일스톤에 다 합치면 비대해진다(스킬 마감 + 핸들러 분리 + 발표 + 임펄스 재설계 = 한 마일스톤 과부하). 그래서 **split B**로 나눴다:

- **M4.12** (이 문서): M4.9 스킬 잔여 마감 + **행동 입력 게이트 시스템화(서버 — 상태가 정책·임펄스 데이터 소유)** + 클라 패킷 핸들러 분리(909줄) + 쿨다운 UI + 발표 재빌드. → 기존 5 Phase 골격 유지.
- **M4.13** (별도 신규 — [`../M4.13-impulse-class/_milestone-plan.md`](../M4.13-impulse-class/_milestone-plan.md)): 서버 임펄스 모델 통일 + 대쉬 거동 재설계 + 클라 예측 통일(방식 B) + 넉백/임펄스공격 통합.

### 경계 — 무엇이 어느 쪽인가

| | M4.12 (이 문서) | M4.13 (별도) |
|---|---|---|
| 행동 입력 **게이트** (상태가 행동을 받을지 결정) | ✅ 소관 | — |
| 상태가 **임펄스 데이터 소유** (호출자 직접 세팅 제거) | ✅ 구조 토대 | (이 토대 위에 모델 통일) |
| 대쉬/넉백/임펄스 **거동 재설계** (고정거리·적 밀침·무적) | — | ✅ 소관 |
| 클라 **임펄스 예측**(방식 B)·forceAdopt·보간 복원 | — | ✅ 소관 |

> **대쉬/임펄스 예측은 전부 M4.13 소관 — M4.12 아님.** 이 문서에 대쉬 거동/예측 재설계 내용을 복제하지 않는다. M4.12는 *입구(게이트)와 데이터 소유 구조*까지만 짓는다.

### 핵심 연결 — M4.13 ⟵ M4.12 의존

M4.12의 **"행동 입력 게이트"가 M4.13 임펄스 재설계의 서버 토대**다. 구체적으로:

- 게이트가 *상태가 임펄스 데이터를 소유*하게 만든다(아래 "행동 입력 게이트" 섹션). 이것이 `LungeDecayPerTick` **잔류 사고의 구조적 봉합**이다 — 호출자가 상태 내부 파라미터를 직접 찔러 넣던 구조 자체를 없앤다.
- M4.13은 그 위에서 임펄스 모델(대쉬/넉백/임펄스공격의 `ExternalVelX`)을 통일한다. **상태가 자기 임펄스 데이터를 소유하는 토대가 먼저 깔려야** M4.13이 그 데이터를 일관 모델로 묶을 수 있다.
- 따라서 **순서는 M4.12 먼저** (M4.13 frontmatter `depends_on: M4.12`가 이 의존을 명시).

---

## Phase 분해 (예정 — 개별 .md는 M4.12 착수 시 분해)

| # | Phase (예정) | 위험 | 도메인 | 핵심 |
|---|---|---|---|---|
| 1 | **클래스↔스킬 게이트** (M4.9 Phase 02 회수) | **trust-boundary** | shared + server + client | `SkillCatalog`(98_Shared 단일 진실, SkillId→요구 CharacterClass) 신설 + 서버 클래스 불일치 시전 시 **silent drop + cheat-flag**(헌법 §3) + 클라 입력 게이트를 그 거울로 + **쿨다운 자료구조 단일화**(`LastSkillTick` 단일 → 스킬별). M4.9에서 02를 건너뛰고 03/05를 먼저 커밋한 *의존성 역전*을 봉합. 전수조사 root cause 8("확장성 게이트 부재 — SkillSystem if-else 디스패치")과 겹침 → **스킬 디스패치 일반화로 한 묶음**. |
| 2 | **클라 패킷 핸들러 분리** (전수조사 B) | 중위험 | client | `ClientPacketHandlers.cs`(909줄) inline 핸들러 → `IClientPacketHandler` + dispatch(서버 `Handlers/` 미러, CODE_CONVENTION §3.2) + VFX 보일러플레이트(`SpawnEffect`/`WarnOnce`) 정리. `GameSession` 부분 추출(`TryGetActiveMap` 등). |
| 3 | **M4.9 Phase 01·04·06 2클라 실측 게이트** | 중위험 | client + qa | 비주얼 prefab(01) + Dash/Teleport 클라 연출(04·06)을 2클라 실측으로 통과 확인 — M4.9가 동작은 했으나 게이트 통과가 박제 안 됐던 분량 회수. |
| 4 | **쿨다운 UI HUD + 전체 회귀** | unity-asset | client + qa | `SkillHudController` 신설(클래스별 슬롯 + 쿨다운 fill, `SkillCatalog`와 정합) + **전체 회귀**(dotnet test + 봇 전 시나리오 + Unity 콘솔 0에러). |
| 5 | **발표 재빌드 + 마감 박제** | **irreversible** | qa | `C:\Dev\Build` 클라 재빌드 = PR #96 보스 facing + **M4.10/11/12 전부 포함** + DLL mtime 신선도 확인. dry-run(발표 리허설) + **M4.9·M4.12 마감 박제**(`_milestone-DONE.md` 2건). |

> **split B 경계 확인**: Phase 3의 "Dash/Teleport 클라 연출 실측"은 **M4.9가 이미 만든 연출(prefab·이펙트)의 통과 게이트 회수**다 — *대쉬 거동·예측 재설계가 아니다*(그건 M4.13 소관). Phase 1은 행동 입력 게이트(데이터 소유 구조)까지 짓고, *대쉬 임펄스 모델 통일·클라 예측 통일은 M4.13으로 넘긴다*. 즉 5 Phase 골격은 split 후에도 응집 유지 — M4.13으로 빠진 건 *대쉬 거동/예측*뿐이고, M4.12가 떠받칠 **게이트·데이터 소유 토대는 Phase 1에 그대로** 남는다.

---

## 새 작업 항목 — 행동 입력 게이트 시스템화 (2026-06-11 영호 지목)

> 위 Phase 표 **Phase 1(클래스↔스킬 게이트)을 이 항목이 흡수·확장한다.** 게이트를 "스킬 클래스 검사 1건"이 아니라 *행동 입력을 받을지 결정하는 단일 입구*로 일반화하는 것이 본 항목의 핵심이다.
>
> **이 항목이 split B의 연결 고리다.** 여기서 *상태가 자기 임펄스 데이터를 소유*하게 만드는 구조(아래 설계 방향 + 제약의 "상태 소유 데이터")가 곧 **M4.13 임펄스 동작 클래스 재설계의 서버 토대**가 된다. M4.12는 입구와 데이터 소유 구조를 짓고, M4.13이 그 위에서 임펄스 모델(`ExternalVelX`)을 통일한다.

### 문제 (영호 지목)

특정 동작 State가 끝나고 Idle로 복귀해야 추가 입력을 받는 **예외 처리가 기술(스킬)마다 재발**한다 — 시스템적 설계가 없어서다. 새 스킬을 추가할 때마다 같은 유형의 사고를 반복한다(상태 복귀 타이밍·입력 차단을 매번 손으로 끼워 넣음).

### 증거 (현재 코드 — 2026-06-11 세션 실측)

입력을 막는 장치가 **세 곳에 흩어져 서로 다른 방식**으로 동작한다:

1. **이동 잠금만 상태 선언식** — `AttackState.LocksMovement => true`([`PlayerCombatStates.cs:28`](../../../02_Server/GameServer/Maps/States/PlayerCombatStates.cs)), 베이스 기본 false([`ActorState.cs:18`](../../../02_Server/GameServer/Maps/States/ActorState.cs)). 틱 루프가 이 플래그를 읽어 `inputX=0`/`rawJump=false`로 강제([`GameMap.cs:233-238`](../../../02_Server/GameServer/Maps/GameMap.cs)). 즉 *이동만* 상태가 막는다.
2. **공격/스킬 진입은 상태가 안 막고 개별 쿨다운만** — `CombatSystem.ProcessAttack`은 ①attacker 존재 ②rate-limit 500ms(`AttackCooldownMs`) ③rewind 범위만 검사하고 **현재 행동 State는 보지 않는다**([`CombatSystem.cs:37-58`](../../../02_Server/GameServer/Maps/Systems/CombatSystem.cs)). `SkillSystem`도 스킬별 쿨다운(`GetLastSkillTick`)만 검사([`SkillSystem.cs`](../../../02_Server/GameServer/Maps/Systems/SkillSystem.cs)). 결과 → **Dash commit window 중 평타 진입이 허용된다**(상태가 "지금 행동 받음?"을 답하지 않으므로).
3. **클래스 게이트는 별도 경로로 이미 봉합됨(M4.9 Phase 02, 커밋 `578486d`)** — `SkillCatalog`([`98_Shared/GameData/SkillCatalog.cs`](../../../98_Shared/GameData/SkillCatalog.cs)) 단일 진실 + 서버 핸들러 3단계 silent drop([`SkillUseHandler.cs`](../../../02_Server/GameServer/Handlers/Skill/SkillUseHandler.cs)) + 클라 입력 거울([`LocalPlayerInput.cs`](../../../03_Client/Assets/Scripts/Input/LocalPlayerInput.cs)). 다만 게이트가 *클래스 자격*과 *쿨다운*에만 있고 **"현재 상태가 이 행동을 받는가"는 어디에도 없다** — 이것이 이번에 메울 빈칸이다.

**실사고 선례** — Dash 중 평타 → `Attack→Attack` self-transition no-op(Exit 미실행) → `LungeDecayPerTick 0.85`(Dash 전용값) 잔류 버그. 봉합은 *호출자가 평타 진입 시마다 기본값을 명시 세팅*하는 패치였다([`CombatSystem.cs:72-75`](../../../02_Server/GameServer/Maps/Systems/CombatSystem.cs) — 주석 실재). **호출자가 상태 내부 파라미터(`LungeDecayPerTick`)를 직접 찔러 넣는 구조 자체가 원인** — 상태가 자기 데이터를 소유하지 못해서다.

### 설계 방향 (착수 시 재실측 후 확정 — 지금은 골격)

- **상태가 자기 입장 정책을 선언** — `LocksMovement`와 동형으로 `AcceptsAction(액션종류)`: "이 상태에서 이 행동을 허용하는가"의 답을 **상태가 소유**한다. (현재 `ActorState`에는 `LocksMovement`/`InterruptibleByHit` 두 정책만 있음 — 같은 자리에 추가.)
- **서버 행동 요청 단일 입구**(`TryPerformAction` 류) — ①현재 상태 허용 ②쿨다운 ③클래스 게이트를 **한 곳에서** 검사. 새 기술 추가 = 허용 표 한 줄. (현재는 ②③만 있고 산재 → ①을 합류시켜 입구를 하나로.)
- **클라는 같은 게이트 표를 거울** — `98_Shared` 공유(M4.11 P2의 ε 공유 상수 계약과 같은 정신). `SkillCatalog`가 이미 클래스 게이트를 양쪽 공유 중이므로 그 패턴을 행동 허용까지 확장.
- **기존 Phase 1(클래스 게이트)은 이 단일 입구에 흡수** — 이미 봉합된 `SkillCatalog`/3단계 검증을 *입구의 한 검사 단계*로 재배치(중복 검증 제거, 단일 진입점 정합).

### 제약 (영호 확정, 2026-06-11 — 클린코드 설계 규칙 준수 의무)

본 작업은 [`CODE_CONVENTION.md`](../../../00_Document/conventions/CODE_CONVENTION.md)(v6.1) 정합이 **의무**다:

- **SRP**(§2.2) — 상태(정책 선언) / 게이트(검사) / 실행(mutation) 책임 분리. System 간 직접 호출 X, 데이터 소유 = 컨테이너·엔티티.
- **상태 소유 데이터** — 호출자가 상태 내부 파라미터를 직접 세팅 금지(`0.85` 잔류 사고의 뿌리). `LungeDecayPerTick`류는 상태가 Enter/Exit에서 소유·정리.
- **매직넘버 금지**(§2.5) — 쿨다운·허용값은 공유 상수 계약(`Constants`/`CombatConstants`).
- **의도 드러나는 naming**(§6.1) + **public 클래스 1줄 책임 헤더**(§6.5).
- **헌법 §1**(서버 권위) — 입구 검사는 서버가 권위, 클라 게이트는 UX·트래픽 절감용 거울. **헌법 §3**(신뢰 경계) — 클래스/소유권/rate-limit는 서버 단독 검증 그대로.

### 시점 메모

- **M4.11 P4(고정스텝 심장부)와 동시 수술 금지** — 심장부 2곳(틱 입력 경로 + 고정스텝)을 동시에 여는 위험. 순차로.
- 본 항목은 **계획서 골격 추가**일 뿐 — "미리 박으면 stale" 정책 정합으로 실측·세분화는 착수 시 재확정한다(위 클래스 게이트 증거가 이미 한 번 drift한 것이 그 근거 — 2026-06-11 실측 결과 M4.9 Phase 02에서 봉합 완료된 것을 확인).
- **plan-auditor 검증**은 M4.12 착수 시 Phase 분해와 함께 돈다.

---

## 위험

- **위험 깃발 3종이 Phase에 분산된다**: Phase 1 = **trust-boundary**(클래스 게이트 = 헌법 §3 신뢰 경계, 서버 검증 + cheat-flag), Phase 4 = **unity-asset**(쿨다운 UI = prefab 변경), Phase 5 = **irreversible**(발표 재빌드 + PR/머지). 발표 게이트 = **Phase 5가 마지막** — 앞 단계가 다 green이어야 재빌드가 의미 있다.
- **ProtocolVersion**: `SkillCatalog`/게이트는 **wire 무변경 예상**(현 v12 유지) — 게이트는 서버가 *기존 패킷을 검증/거부*하는 로직이지 패킷 모양을 바꾸지 않는다. 단 쿨다운 단일화 과정에서 새 필드가 새면 점검 필수 → 그 시점에 STOP → 사용자 의논(irreversible 경로).

---

## 의존

- **들어오는 의존 — M4.10·M4.11 후.** M4.10(컨벤션 v6 + 진입점 맵)·M4.11(동기화 정돈)이 박힌 토대 위에서 스킬 잔여를 마감한다.
- **나가는 의존 — M4.13 ⟵ 이 마일스톤.** M4.13(임펄스 동작 클래스 재설계)이 이 마일스톤의 **행동 입력 게이트(상태가 임펄스 데이터 소유)**를 서버 토대로 쓴다 → **M4.12 먼저**(split B). 위 "Split B — M4.13과의 관계" 참조.
- **단 발표 순서 조정은 착수 시 영호 결정** — 본 계획서는 *논리 의존*만 적는다(발표가 임박하면 Phase 순서를 발표 우선으로 재배열할 수 있음 — 일정은 영호 컨트롤).

---

> **본 문서는 마일스톤 계획서.** Phase 개별 정의 `.md`는 **M4.12 착수 시점에 분해**한다(위 표는 *예정* 골격, 실측·세분화는 착수 시).
