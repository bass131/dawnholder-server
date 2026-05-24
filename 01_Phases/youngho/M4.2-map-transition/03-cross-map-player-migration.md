---
owner: youngho
milestone: M4.2
phase: 03
title: 맵 간 player migration 서버 로직
status: pending
grade: 복잡
risk: trust-boundary
estimated: 3~4h
domain: server
---

# Phase 03: 맵 간 player migration 서버 로직

> **상태**: pending
> **마일스톤**: M4.2
> **등급**: 복잡 (맵 간 actor 통신 + state 이전 + 일부 비가역 동시성 / trust-boundary)
> **담당**: server SubAgent

---

## 🎯 목표

`C_EnterPortal` 수신 → portal **근접 검증**(헌법 #3) → 플레이어를 **맵 A에서 제거 +
맵 B에 추가**하되 **state(HP / PlayerStats / 위치)를 보존**한다. 떠난 맵에는 `S_PlayerLeave`,
도착 맵에는 `S_PlayerJoin` + enemy roster(`S_EntitySpawn`) broadcast, 본인에게 `S_MapTransition`.

이게 M4.2의 **심장** — "맵 간 핸드오프"가 실제로 동작하는 Phase.

---

## ⏪ 사전 조건

- [ ] Phase 01 완료 — 맵 레지스트리 (`GetMap(MapId)`)
- [ ] Phase 02 완료 — `C_EnterPortal` / `S_MapTransition` 패킷 + portal 정의

---

## 📝 작업 내용

- [ ] `GameSession`이 **현재 맵 추적** — `_currentMapId` 필드 + `GetMap()`이 이 맵 반환
      (Phase 01 임시 Town 고정 → 실제 추적으로 교체)
- [ ] `EnterPortalHandler` 신설 (`IPacketHandler`) — C_EnterPortal decode → session 캡슐화 메서드 호출
  - `HandlerRegistry`에 한 줄 등록
- [ ] portal 근접 검증 (헌법 #3 Trust Boundary, **tick thread에서**):
  1. portalId가 현재 맵의 유효 portal인가 (범위 검증) — 아니면 silent drop
  2. 플레이어 위치가 portal 좌표 근처인가 (거리 임계) — 멀면 silent drop (텔레포트 핵 차단)
  3. (선택) rate-limit — 연속 portal 남용 차단
- [ ] **맵 간 migration** (맵 A tick thread → 맵 B로 마샬링):
  - 맵 A: `RemovePlayer` + 남은 플레이어에게 `S_PlayerLeave` broadcast
  - state 캡처: HP, PlayerStats, (목적지 spawn 좌표로 위치 재설정)
  - 맵 B: `EnqueueJob`으로 `AddPlayer(owner, destSpawn, stats)` + HP 복원
  - 맵 B 기존 플레이어에게 `S_PlayerJoin` broadcast + **신규 진입자에게** active roster
    (`S_PlayerJoin` 역방향 + `S_EntitySpawn` enemy roster) 다발 전송
  - 본인에게 `S_MapTransition { destMapId, spawnX, spawnY, entityId }`
- [ ] **entity id 정책 — ⚠️ Phase 02 진입 *전* 사용자 확인 + ADR 후보** (plan-auditor 2026-05-25 🟡):
      Phase 02가 `S_MapTransition.entityId` 필드를 PDL에 먼저 박으므로, 정책이 미정이면 헌법 #2
      (은퇴 ID 재사용 금지)에 걸림. "전역 id 풀 유지" vs "맵별 풀 + 재배정" trade-off를 Phase 02 전 결론.
  - 잠정 권장: **재배정** (맵별 독립 풀 단순) → S_MapTransition.entityId로 클라에 새 id 통보.
    단 사용자 최종 확인 후 확정 (전역 vs 맵별 = ADR 박을 가치).
- [ ] 맵 간 통신은 **각 맵 EnqueueJob 경유** — 한 맵의 tick thread가 다른 맵 상태 직접 mutate 금지
      (헌법 Map=Actor, 맵 간은 message channel만)
- [ ] **migration 중간 상태(transient) 처리 방침 명시** (plan-auditor 2026-05-25 🟡): RemovePlayer~AddPlayer
      사이 player가 "어느 맵에도 없는" 순간 도착하는 게임플레이 패킷(attack/move) = **silent drop**.
      `_currentMapId`를 transitioning 마킹하거나, GetMap()이 그 사이 null/이전맵 반환 시 핸들러가 안전 no-op.

---

## ✅ 완료 조건

- [ ] `dotnet build` + `dotnet test` green (회귀 0)
- [ ] portal 밟기 → 맵 이동 → HP/stats 보존 단위 테스트
- [ ] **왕복 이동 state 보존** — Town→HuntingGround→Town 후 HP/stats 동일 (전투로 HP 깎였으면 그 값 유지)
- [ ] 근접 검증 실패(먼 위치에서 portal 요청) → silent drop, 맵 이동 안 됨 (헌법 #3 테스트)
- [ ] 떠난 맵 다른 플레이어가 S_PlayerLeave 수신 / 도착 맵이 S_PlayerJoin 수신 (multi 시뮬)
- [ ] 맵 간 이동이 tick thread invariant 유지 (다른 맵 상태 직접 mutate 0 — EnqueueJob만)

---

## 🧪 테스트

**자동**:
- `MapMigrationTests` — 이동 후 맵 A에 player 없음 / 맵 B에 있음 / state 보존
- `EnterPortalHandlerTests` — happy(근접), reject(원거리), reject(invalid portalId)
- 멀티 플레이어 broadcast 검증 (S_PlayerLeave/Join)

**수동**:
- 봇 2명 같은 맵 → 1명 portal 이동 → 남은 봇이 leave 인지

---

## 📚 학습 포인트

- **Actor 간 메시지 패싱**: 맵 A가 맵 B를 직접 못 건드림 → `EnqueueJob`으로 마샬링.
  분산 시스템의 "메시지로만 통신" 원칙 축소판 (lock 없이 동시성 안전).
- **state 이전(handoff)**: MMORPG zone 서버 핸드오프의 핵심 — 플레이어 데이터를 잃지 않고
  소유권을 다른 actor로 넘기기. M5 영속화 + 분산 서버(ADR-008은 단일 프로세스지만 패턴은 동일)의 기반.
- **헌법 #3 텔레포트 핵 차단**: 근접 검증 없으면 클라가 아무 portal id나 보내 순간이동.

---

## ⚠️ 함정 / 주의사항

- **맵 간 race**: 맵 A에서 RemovePlayer 한 직후 맵 B EnqueueJob 사이에 player가 "어느 맵에도
  없는" 순간 — 이 사이 도착하는 패킷(attack 등) 처리 주의. `_currentMapId` 갱신 시점 명확히.
- **entity id 재배정 시 클라 동기화**: 클라는 옛 id로 자기 entity 추적 중 → S_MapTransition.entityId로
  새 id 받아 교체해야 함 (Phase 04 클라 책임).
- **broadcast 누락**: 떠난 맵에 leave 안 보내면 ghost entity, 도착 맵에 join 안 보내면 안 보임.
- **GameMap ctor enemy가 죽은 상태로 재진입**: HuntingGround enemy를 죽이고 떠났다 돌아오면
  enemy 없음 — respawn 정책은 M4.3 backlog (본 Phase는 enemy 상태 그대로, respawn X).
- 헌법 #5 — migration 로직이 tick thread에서 동기 실행, await/DB/Thread.Sleep 금지.

---

## ➡️ 다음 Phase

- Phase 04 — 클라 4 scene dispatch + portal UX

---

## 📋 박제 (완료 후)

- **복잡 등급** — M4.2 마일스톤 -DONE.md는 Phase 05 통합 박제. 본 Phase는 work-pin + commit.

---

## 작업 로그

- 2026-05-25: 계획 수립 (`/work:plan M4.2`)
</content>
