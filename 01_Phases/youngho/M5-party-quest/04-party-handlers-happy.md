---
owner: youngho
milestone: M5
phase: 04
title: 파티 초대/수락/탈퇴 핸들러 happy path + 세션 결선
status: pending
grade: 복잡
risk: trust-boundary
domain: server
estimated: 2~3h
---

# Phase 04: 파티 초대/수락/탈퇴 핸들러 happy path + 세션 결선

> **상태**: pending
> **마일스톤**: M5 (트랙 A — 파티 시스템 서버)
> **등급**: 복잡 + **trust-boundary**(Handlers/ + GameSession 변경) → 구현 Worker = **Opus**(routing B)
> **담당**: server (Opus Worker — 메인 file:line 게이트 + reviewer 자동)

---

## 🎯 목표

파티 초대(`C_PartyInvite`)·수락(`C_PartyRespond`)·탈퇴(`C_PartyLeave`)의 **happy path(정상 경로)** 핸들러를 만들고 세션과 결선한다. 초대하면 피초대자에게 `S_PartyInviteRecv`가 1:1로 가고, 수락하면 양 멤버에게 `S_PartyUpdate`가 가고, 탈퇴하면 양쪽에 해산이 통보된다. 거절/race/disconnect 같은 *방어 경로*는 다음 Phase(05).

> 이번 Phase는 "올바른 입력이 들어왔을 때 파티가 제대로 만들어지는가"에 집중한다. trust-boundary가 걸리는 이유는 핸들러가 클라 소켓 입력을 처음 받는 지점이라, *행위자 위장*을 막는 설계(아래)를 여기서 못 박아야 하기 때문.

---

## ⏪ 사전 조건

- [ ] Phase 03 완료 (`PartyRegistry` + `GameWorld.SendToEntity` cross-map 송신 가능).

---

## 📝 작업 내용

- [ ] 신규 핸들러 3개 in `02_Server/GameServer/Handlers/Party/`:
  - `PartyInviteHandler.cs` — `C_PartyInvite{targetEntityId}` → 피초대자(targetEntityId)에게 `S_PartyInviteRecv{inviterEntityId, inviterClass}` 1:1 송신.
  - `PartyRespondHandler.cs` — `C_PartyRespond{inviterEntityId, accept}` → accept이면 `CreateParty` + 양 멤버에게 `S_PartyUpdate` 송신.
  - `PartyLeaveHandler.cs` — `C_PartyLeave` → 파티 해산, 남은 멤버에게 해산 통보(`S_PartyUpdate` 빈 상태 또는 약속된 표현).
- [ ] `02_Server/GameServer/Handlers/HandlerRegistry.cs` — 3핸들러 등록(각 1줄).
- [ ] `02_Server/GameServer/Network/GameSession.cs` — `SubmitPartyInvite` / `SubmitPartyRespond` / `SubmitPartyLeave` 진입점 추가(EnqueueJob 마샬링) + 세션이 자기 `PartyId`를 참조할 수 있도록 필드/접근자.
- [ ] **행위자 entityId는 핸들러가 GameSession에서 강제로 가져온다** — 패킷 안에 "보내는 사람" 필드를 두지 않음(C_Attack 패턴과 동일). 초대 *대상*만 패킷의 `targetEntityId`.

---

## ✅ 완료 조건

- [ ] xUnit e2e: invite → 피초대자 1명에게만 `S_PartyInviteRecv`(다른 사람에겐 안 감).
- [ ] xUnit e2e: accept → 양 멤버 둘 다 `S_PartyUpdate`(파티 결성 반영, 멤버 2명).
- [ ] xUnit e2e: leave → 파티 해산 + 남은 멤버에게 통보.
- [ ] 행위자 entityId가 패킷이 아니라 GameSession에서 결정됨(위장 불가 구조 확인).
- [ ] WSL2 `dotnet build` 0/0 + `dotnet test` green (baseline 회귀 0).
- [ ] reviewer 헌법 hard 위반 0 (trust-boundary = reviewer 자동 호출).

---

## 🧪 테스트

**자동**:
- `PartyHandlerHappyTests` — invite/accept/leave 3흐름 e2e(2 세션 시뮬레이션). 수신자 정확성(1:1 vs 양 멤버) 검증.

**수동**: 없음(클라 UI는 트랙 P. 서버 계약은 헤드리스로 확정).

---

## 📚 학습 포인트

> 학부생 시각.

- **행위자를 패킷에 넣지 않는 이유** (헌법 §3 신뢰 경계) — 만약 `C_PartyInvite`에 `inviterEntityId`(보내는 사람)를 넣으면, 악성 클라가 *남의 entityId로 위장*해 초대를 보낼 수 있다. 행위자는 항상 *서버가 아는 세션 정보*(이 소켓이 누구인지)에서 가져와야 안전하다. C_Attack도 "내가 누구를 때린다"가 아니라 "내 세션이 공격한다"로 처리하는 같은 패턴.
- **1:1 송신 vs 브로드캐스트** — 초대는 피초대자 *한 명*에게만(`SendToEntity`), 파티 업데이트는 *양 멤버*에게. 누구에게 보낼지가 패킷의 의미를 결정한다. cross-map이라 둘 다 `SendToEntity`로 라우팅.
- **핸들러 = 신뢰 경계의 최전선** — 클라 소켓에서 들어온 untrusted 입력이 게임 로직에 닿는 첫 지점. happy path를 짜더라도 "행위자 강제" 같은 보안 골격은 여기서 박아야 한다. 검증 디테일(거절/race)은 05에서 채우지만, 골격은 04부터.

---

## ⚠️ 함정 / 주의사항

- **trust-boundary** — Handlers/ + GameSession 변경 = 위험 깃발 자동. Opus Worker 위임 + reviewer 자동. 행위자 위장 방지 구조를 절대 빠뜨리지 말 것.
- **happy path만** — 이번엔 정상 경로. "자기 자신 초대", "이미 파티 중", "정원 초과", "만료된 초대" 같은 거절 경로는 05에서. 04에서 이걸 짜면 스코프 침범(05와 충돌).
- **GameSession.cs 충돌 관리** — 같은 파일을 05(disconnect 정리)와 Q3(S_PortalLocked)도 건드린다. 의존성 순서(04→05→Q3) 지켜서 merge 충돌 최소화.
- **EnqueueJob 마샬링** — Submit* 진입점은 세션 스레드에서 PartyRegistry actor로 작업을 넘기는 경계. 직접 PartyRegistry 내부를 호출하지 말 것(actor 경계).

---

## ➡️ 다음 Phase

- Phase 05 — 파티 신뢰경계 검증(거절 4종 + 응답 race) + disconnect 정리.

---

## 📋 박제 (완료 후)

- 복잡+trust-boundary → `04-party-handlers-happy-DONE.md`(요약 + 사실 박제 + reviewer 요약 + 학습 키워드). HTML은 마일스톤 마감(R3)에서 종합.

---

## 작업 로그

- 2026-06-14: 생성.
