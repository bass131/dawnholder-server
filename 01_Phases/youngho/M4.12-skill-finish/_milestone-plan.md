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

> 직전 = M4.11(동기화 정돈). 이 마일스톤이 **M4.9 스킬 시스템의 미보고 잔여를 회수해 마감**하고, 전수조사 B(클라 핸들러 분리)를 합류시킨 뒤, **발표(6/17) 재빌드**로 닫는다.

---

## Context (왜)

M4.9(스킬 시스템)는 **"기능 골격 완료, 마감 미보고"** 상태다. Dash/Teleport가 서버+클라 양쪽에서 동작하고 이펙트 prefab도 들어왔지만, `-DONE.md`가 **0개** — 사실 박제가 안 된 채 세션이 중단됐다(`wip(skill): ... 세션 중단 보존` 커밋이 증거). **본 마일스톤이 M4.9의 잔여를 회수해 정식으로 마감**한다.

여기에 전수조사 **B(중위험) — 클라 패킷 핸들러 분리**도 합류시킨다. M4.10이 발표 영상 경로라 미뤘던 `ClientPacketHandlers.cs`(909줄) 분리가 이 마일스톤의 몫이다.

그리고 **발표(6/17) 재빌드가 이 마일스톤 끝의 마지막 게이트**다. 스킬 마감 + 핸들러 분리 + 쿨다운 UI가 다 들어간 클라를 재빌드해 발표 자산으로 박는 것이 종착점이다.

---

## Phase 분해 (예정 — 개별 .md는 M4.12 착수 시 분해)

| # | Phase (예정) | 위험 | 도메인 | 핵심 |
|---|---|---|---|---|
| 1 | **클래스↔스킬 게이트** (M4.9 Phase 02 회수) | **trust-boundary** | shared + server + client | `SkillCatalog`(98_Shared 단일 진실, SkillId→요구 CharacterClass) 신설 + 서버 클래스 불일치 시전 시 **silent drop + cheat-flag**(헌법 §3) + 클라 입력 게이트를 그 거울로 + **쿨다운 자료구조 단일화**(`LastSkillTick` 단일 → 스킬별). M4.9에서 02를 건너뛰고 03/05를 먼저 커밋한 *의존성 역전*을 봉합. 전수조사 root cause 8("확장성 게이트 부재 — SkillSystem if-else 디스패치")과 겹침 → **스킬 디스패치 일반화로 한 묶음**. |
| 2 | **클라 패킷 핸들러 분리** (전수조사 B) | 중위험 | client | `ClientPacketHandlers.cs`(909줄) inline 핸들러 → `IClientPacketHandler` + dispatch(서버 `Handlers/` 미러, CODE_CONVENTION §3.2) + VFX 보일러플레이트(`SpawnEffect`/`WarnOnce`) 정리. `GameSession` 부분 추출(`TryGetActiveMap` 등). |
| 3 | **M4.9 Phase 01·04·06 2클라 실측 게이트** | 중위험 | client + qa | 비주얼 prefab(01) + Dash/Teleport 클라 연출(04·06)을 2클라 실측으로 통과 확인 — M4.9가 동작은 했으나 게이트 통과가 박제 안 됐던 분량 회수. |
| 4 | **쿨다운 UI HUD + 전체 회귀** | unity-asset | client + qa | `SkillHudController` 신설(클래스별 슬롯 + 쿨다운 fill, `SkillCatalog`와 정합) + **전체 회귀**(dotnet test + 봇 전 시나리오 + Unity 콘솔 0에러). |
| 5 | **발표 재빌드 + 마감 박제** | **irreversible** | qa | `C:\Dev\Build` 클라 재빌드 = PR #96 보스 facing + **M4.10/11/12 전부 포함** + DLL mtime 신선도 확인. dry-run(발표 리허설) + **M4.9·M4.12 마감 박제**(`_milestone-DONE.md` 2건). |

---

## 위험

- **위험 깃발 3종이 Phase에 분산된다**: Phase 1 = **trust-boundary**(클래스 게이트 = 헌법 §3 신뢰 경계, 서버 검증 + cheat-flag), Phase 4 = **unity-asset**(쿨다운 UI = prefab 변경), Phase 5 = **irreversible**(발표 재빌드 + PR/머지). 발표 게이트 = **Phase 5가 마지막** — 앞 단계가 다 green이어야 재빌드가 의미 있다.
- **ProtocolVersion**: `SkillCatalog`/게이트는 **wire 무변경 예상**(현 11 유지) — 게이트는 서버가 *기존 패킷을 검증/거부*하는 로직이지 패킷 모양을 바꾸지 않는다. 단 쿨다운 단일화 과정에서 새 필드가 새면 점검 필수 → 그 시점에 STOP → 사용자 의논(irreversible 경로).

---

## 의존

- **M4.10·M4.11 후.** M4.10(컨벤션 v6 + 진입점 맵)·M4.11(동기화 정돈)이 박힌 토대 위에서 스킬 잔여를 마감한다.
- **단 발표 일정상 순서 조정은 착수 시 영호 결정** — 본 계획서는 *논리 의존*만 적는다(6/17 발표가 임박하면 Phase 순서를 발표 우선으로 재배열할 수 있음).

---

> **본 문서는 마일스톤 계획서.** Phase 개별 정의 `.md`는 **M4.12 착수 시점에 분해**한다(위 표는 *예정* 골격, 실측·세분화는 착수 시).
