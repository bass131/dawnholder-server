---
owner: youngho
milestone: M4.2
title: Map Transition (진짜 4맵 분리 + portal handoff + 클라 scene dispatch)
status: done
grade: 복잡
risk: trust-boundary
estimated: 11~16h (총합, 5 Phase 확정)
domain: server+shared+client
---

# M4.2 — Map Transition (확정 분해)

> **상태**: planned — 2026-05-25 `/work:plan M4.2`로 본격 분해 (M4.1 마감 직후)
> **scope 결정 (2026-05-25)**: **데모 핵심 우선** — 4맵 분리 + portal handoff + 클라 scene 전환까지.
>   cheat-flag table + Serilog 도입은 **M4.3로 이월** (캡스톤 발표 데모 화면에 안 보이는 인프라 +
>   1주 일정 안전 마진 확보). 헌법 #3 cheat-flag 약속은 M4.3에서 봉합 예정.
> **시작**: 2026-05-25 (M4.1 마감 직후)
> **마감 목표**: 2026-06-09 (캡스톤 1 발표 1일 전)
> **사전 조건**: M3.8 마감 + M4.1 마감 ✅ (둘 다 완료)

---

## 🎯 마일스톤 목표

**M3 응급 단일 맵 3-zone trick을 진짜 4맵 분리로 승격** + 맵 간 portal handoff
(서버 권위 player migration) + 클라 측 scene 전환. 캡스톤 1 발표 데모 후반부
("정밀화된 멀티플레이어 RPG" — M4.1 정밀 전투 + M4.2 4맵 분리 종합).

**핵심 기술 승격**:
- `GameWorld._map` 단일 맵 → `Dictionary<MapId, GameMap>` 맵 레지스트리
- `GameSession.GetMap()` 단일 참조 → 플레이어가 "현재 맵" 추적
- `GameMap` ctor 고정 enemy spawn → 맵별 구성 주입 (마을=빈 / 사냥터=Normal / 보스방=Boss / 종료=빈)
- portal entity + `S_MapTransition` 패킷 (ProtocolVersion 5→6 bump)
- 맵 간 player state 이전 (HP / PlayerStats / 위치)

---

## 📂 확정 Phase 분해 (5개)

| # | Phase | 등급 | 도메인 | 예상 | 끝나면 데모 |
|---|---|---|---|---|---|
| 01 | 맵 레지스트리 + MapId enum 골격 | 보통 | server | 1.5~2h | 서버 로그에 4맵 tick 도는 것 확인, 회귀 0 |
| 02 | portal 정의 + S_MapTransition 패킷 + PDL bump | 복잡 | shared+server | 2~3h | portal 좌표/목적지 서버 정의 + 패킷 양쪽 wire-up |
| 03 | 맵 간 player migration 로직 | 복잡 | server | 3~4h | headless 봇이 맵 이동 시 state 보존하며 핸드오프 |
| 04 | 클라 4 scene dispatch + portal UX | 복잡 | client | 3~5h | Unity에서 portal 밟으면 scene 전환 (본인 외관 협업) |
| 05 | 통합 검증 + 봇 맵 이동 시나리오 + 마감 | 보통 | qa+server | 1.5~2h | dotnet test green + 왕복 이동 회귀 안전망 + -DONE.md |

**총 등급 = 복잡** (마일스톤 자체). Phase 02·03 trust-boundary 깃발이지만 데모 핵심 scope
축소로 대규모 자동 상향까지는 안 감 (3+ 도메인 동시 X — Phase별 1 도메인 위주).

> ⚠️ **비가역 깃발 동원 패턴 (plan-auditor 2026-05-25 🔴 봉합)**: Phase 02의 `ProtocolVersion 5→6 bump`은
> 헌법 명시 `irreversible` 깃발 — scope 축소로 무력화 안 되는 비가역 축 (한번 v6 올리면 v5 롤백 불가,
> 모든 팀원 재빌드 영향). 등급은 "복잡" 유지하되 **동원 패턴 보강**: Phase 05 마감 시
> **reviewer SubAgent Tier 2-A 통합 점검 의무** (헌법/ADR/도메인 패턴 5축). 한 사람 머리로 조용히
> 마감 X. entity id 정책(아래 의존성 그래프 주석)도 Phase 02 진입 *전* 사용자 확인 + ADR 후보.

---

## 🔗 의존성 그래프

```
Phase 01 (맵 레지스트리 골격)
   │  GameWorld 다맵 인프라가 02/03 진입 전제
   ↓
Phase 02 (portal 정의 + S_MapTransition 패킷)
   │  S_MapTransition 패킷이 03 migration + 04 클라 dispatch 진입
   ↓
Phase 03 (맵 간 player migration 서버 로직)
   │  서버 핸드오프 완성이 04 클라 전환 end-to-end 검증 전제
   ↓
Phase 04 (클라 4 scene dispatch + portal UX) — 본인 외관 협업
   │
   ↓
Phase 05 (통합 검증 + 봇 시나리오 + 마감)
```

**병렬 가능**: Phase 04의 **클라 scene 외관 구성**(빈 scene 4개 + portal sprite 배치)은
Phase 02/03 진행 중 본인이 미리 작업 가능 (네트워크 wiring만 Phase 03 후). 나머지는 순차.

---

## ✅ 마일스톤 완료 조건

- [ ] `MapId` enum 4맵 정의 (Town / HuntingGround / BossRoom / Ending)
- [ ] `GameWorld`가 `Dictionary<MapId, GameMap>` 레지스트리로 다맵 tick
- [ ] portal entity (맵별 좌표 + 목적지 MapId, 서버 정의) + 근접 검증 (헌법 #3)
- [ ] `C_EnterPortal` + `S_MapTransition` PDL 패킷 + ProtocolVersion 5→6 bump
- [ ] 맵 간 player state 이전 (HP / PlayerStats / 위치) — 왕복 보존
- [ ] 클라 4 scene 전환 + portal 트리거 입력
- [ ] dotnet test green (회귀 0 + Phase별 신규 테스트)
- [ ] headless-bot 맵 이동 왕복 시나리오 PASS
- [ ] M4.2-마감 `_milestone-DONE.md` (복잡 등급)
- [ ] CHANGELOG entry ([M] — PDL bump + 모든 팀원 빌드 영향)
- [ ] **캡스톤 1 발표 데모 영상 가능 상태** (M4.1 + M4.2 종합)

---

## 🚫 M4.2에서 의도적으로 뺀 것 (M4.3 이월)

- **cheat-flag table** — portal 근접 검증 실패 등 silent drop 이벤트 기록. 헌법 #3 강화.
- **Serilog 도입** — Console.WriteLine → 구조화 로깅 (ARCHITECTURE M5 예정 항목 일부 forward).
- 사유: 캡스톤 데모 화면에 안 보이는 인프라 + 1주 일정 안전 마진. (2026-05-25 scope 결정)

---

## ➡️ 다음 마일스톤

- **M4.3 — AI + Polish** (enemy AI + boss behavior + jump Y mispredict 봉합 +
  **cheat-flag + Serilog 이월분** + PvP ADR + 마감 의례). 캡스톤 1 후 7~10월.

---

## 갱신 이력

- 2026-05-22 — placeholder 박힘 (M4.1 plan 시점, M4 3토막 분할 정합)
- 2026-05-22 — M3.8 신설 흡수 일정 재정렬. 마감 목표 6/10 → 6/9.
- 2026-05-25 — **본격 분해 확정** (`/work:plan M4.2`). scope=데모 핵심 우선 결정
  (cheat-flag/Serilog → M4.3 이월). 옛 3 Phase(04/05/06 통합번호 추정) →
  새 5 Phase(01~05, 1~3h/Phase 원칙). M4.1이 01~06 다 썼으므로 M4.2는 01부터 재시작.
</content>
</invoke>
