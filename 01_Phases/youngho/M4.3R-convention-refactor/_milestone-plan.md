---
owner: youngho
milestone: M4.3R
title: Code Convention 전체 리팩토링 (God class 분리 + 네이밍 정합)
status: done
grade: 대규모
risk: trust-boundary
estimated: 14~21h (총합, 7 Phase)
domain: server+client+shared+cross
---

# M4.3R — Code Convention 전체 리팩토링

> **상태**: planned — 2026-05-29 `/work:plan`로 확정 (convention-audit 워크플로우 백로그 9건 기반)
> **시작**: 2026-05-29
> **목표 마감**: 캡스톤 1 발표(2026-06-10) 전 R1(Phase 02·03) 완료 — 발표 "자가측정→리팩토링" 서사 자산. 나머지는 발표 후/M4.4 묶음 가능.

---

## 🎯 마일스톤 목표

ADR-028로 수립한 Code Convention을 **코드베이스에 처음으로 전면 적용**한다. convention-audit 워크플로우(73 production 파일 도메인별 병렬 감사)가 산출한 백로그 9건을 리팩토링한다. 핵심은 **God class 3개 분리**(GameMap §2.2 / UnityClientSession §3.2+§2.2 / GameSession §2.2 trust-boundary)와 **네이밍 prefix 정합**(§3.3). **순수 리팩토링 — 동작 보존이 절대 조건**(PDL/Protocol 변경 0, 외부 행동 불변).

### ⚠️ 등급 = 대규모 사유

사용자 "전부 지금" 결정으로 3+ 도메인(server+client+shared+cross) × 9 항목 + **trust-boundary 깃발**(GameSession rate-limit/handshake/portal = 헌법 #3) 자동 상향. 따라서 Coordinator + 도메인 Worker + Reviewer 동원, work-pin + -DONE.md(복잡 Phase) + 5단계 보고(마일스톤 마감).

### 핵심 원칙 (헌법 + Convention)

- **§2.2 컨테이너 + System** — 컨테이너(상태+tick+actor 경계)는 남기고 로직만 System으로. System은 컨테이너를 인자로 받고 공유 상태를 변경만.
- **§0.3 과분할 금지** — 단일 도메인 줄수만 긴 건 분리 X. "분리하면 뇌 부담이 정말 주나?" 통과 못하면 안 쪼갬. (handshake state·pending spawn static·PacketFormat 템플릿 = 분리 금지)
- **§1.1 맵=actor** — System은 tick thread 안에서만 호출(EnqueueJob 경유). 맵 내부 lock 금지.
- **헌법 #3 trust-boundary** — GameSession 검증 invariant(rate-limit 임계/handshake 게이트/portal 근접)가 추출 시 흩어지지 않게.
- **동작 보존** — 각 Phase 전후 `dotnet test` 회귀 0 + 헤드리스 봇 스모크 통과로 정량 증명 (Phase 01에서 베이스라인 스냅샷 박음).

---

## 📋 Phase 분해 (7개)

| # | Phase | 등급 | 도메인 | 예상 | risk | 백로그 |
|---|---|---|---|---|---|---|
| 01 | Convention 문구 정합 + 정책 결정 + 베이스라인 회귀 스냅샷 | 보통 | shared+메타 | 1~2h | — | rank 7·8 선행 |
| 02 | 클라 패킷 dispatch 분리 (UnityClientSession → IPacketHandler + RosterTransitionBuffer + SceneRouter) | 복잡 | client | 3~4h | — | rank 1 |
| 03 | GameMap System 분리 (Combat/EnemyAI/Respawn System) | 복잡 | server | 3~4h | — | rank 2 |
| 04 | GameSession trust-boundary 추출 (IntentRateLimiter + MapMigration) | 복잡 | server | 3~5h | trust-boundary | rank 4 |
| 05 | 클라 구조 기회성 (EnemyViewFactory 추출 + PlayerPredictorTests) | 보통 | client | 2~3h | — | rank 3·5 |
| 06 | 클라 네이밍 정합 (isPaused + SerializeField 규칙) | 보통/단순 | client | 1h | — | rank 6·8 |
| 07 | 네트워크 레이어 m_ prefix 일괄 (서버 Network/ + ClientNet 자매) | 보통 | cross | 1~2h | — | rank 7·9 |

**총 등급 = 대규모**. trust-boundary(Phase 04)는 발표 직전 리스크 → 회귀 안전망 강화 + 발표 전/후 타이밍 판단 가능.

---

## 🔗 의존성 그래프

```
Phase 01 (Convention 문구 + 정책결정 + 베이스라인)
   │
   ├──→ Phase 06 (클라 네이밍 — SerializeField 정책 의존)
   └──→ Phase 07 (네트워크 prefix — §3.3 서버적용 명문화 의존)

Phase 02 (클라 dispatch)  ←── 독립 (서버 Handlers 패턴 이미 성숙 = 선행 충족)
Phase 03 (GameMap System) ←── 독립
   │
   ↓
Phase 04 (GameSession 추출) ←── 03 후 (server 도메인 순차 + migration이 GameMap surface 사용)

Phase 05 (클라 기회성) ←── 독립
```

**병렬 가능**: Phase 02(client) ↔ Phase 03(server) — R1, 도메인 다름. Phase 05도 독립이나 client라 02와 순차(한 client Worker).
**순차**: 03 → 04 (server). 01 → 06·07.
**권장 순서**: 01 → [02 ∥ 03] (R1) → 04 (R2) → 05·06·07 (cleanup).

---

## ✅ 마일스톤 완료 조건

- [ ] God class 3개 모두 600줄 미만 (size-guard 경고 3건 → 0)
- [ ] GameMap → CombatSystem/EnemyAISystem/RespawnSystem 분리 (컨테이너는 상태+tick+actor만)
- [ ] UnityClientSession → IPacketHandler + dispatch 테이블 (서버 Handlers/ 미러)
- [ ] GameSession → IntentRateLimiter + MapMigration 추출 (handshake state는 컨테이너 잔류 — §0.3)
- [ ] §3.3 네이밍 prefix 통일 (정책 정합 — m_ 0건, 클라 private `_camelCase`)
- [ ] **동작 보존**: `dotnet test` 회귀 0 (Phase 01 베이스라인 카운트 유지/증가), 헤드리스 봇 스모크 3종 통과
- [ ] PDL/Protocol 변경 0 (순수 리팩토링 — ProtocolVersion 그대로)
- [ ] 각 복잡 Phase reviewer 헌법 hard 위반 0
- [ ] CHANGELOG entry ([M] — Convention 첫 적용 리팩토링, 팀원·AI 영향)
- [ ] 5단계 보고 MD/HTML (마일스톤 마감 — 캡스톤 "자가측정→리팩토링" 서사 자산)

---

## 🚫 이번에 명시적으로 뺀 것 (사유 박음)

- **rank 9 Listener.cs 매개변수 `_prefix` → Phase 07 포함으로 정정** (2026-05-29 사용자 결정): 매개변수 `_endPoint` 류는 §4 casing이 아니라 **§3.3 prefix 위반**(params에 `_`는 field 전용 prefix 오용)으로 재분류. 수동 제외 철회 — Phase 07에서 밑줄 제거. (casing 변환 Pascal/camel·중괄호만 §4 `.editorconfig` M4.4 유지)
- **GameSession handshake state 분리**: §0.3 과분할 — socket lifecycle 강결합. 컨테이너 잔류 (overSplitWarning).
- **PacketFormat.cs 템플릿 파일 분할 / 봇 시나리오 분할**: §0.3 — 로직 0(raw 문자열) / 테스트 하니스 관용구. 감사 0 위반 확정.
- **M4.3 발표 Phase 08~12**: 별 마일스톤. R1(Phase 02) 후 재개 권장 (08 신규 패킷이 dispatch 테이블로 깔끔히 들어감).

---

## 갱신 이력

- 2026-05-29 — **확정** (`/work:plan`). convention-audit 워크플로우(5 에이전트, 73파일, 15 finding → 9 백로그) 기반 7 Phase 분해. 사용자 "전부 지금" 결정 = 대규모. R1(02·03) 발표 전 / trust-boundary(04) 안전망 강화 / 네이밍(06·07) 정책결정 선행.
