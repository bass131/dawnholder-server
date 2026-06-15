---
owner: youngho
milestone: M5
phase: 12
title: 클라 파티 패킷 핸들러 + 클라 파티 미러 상태
status: pending
grade: 보통
domain: client
estimated: 1~2h
---

# Phase 12: 클라 파티 패킷 핸들러 + 클라 파티 미러 상태

> **상태**: pending
> **마일스톤**: M5 (트랙 P — 클라 파티/퀘스트 표현 / P1)
> **등급**: 보통 (1 도메인 × 4 파일 / 클라 스크립트 — 컴파일 검증, 기능 검증 아침)
> **담당**: client (Sonnet Worker — 메인 file:line 게이트, Unity 컴파일 검증)

---

## 🎯 목표

서버(트랙 A)가 보내는 파티 패킷(`S_PartyInviteRecv`/`S_PartyUpdate`/`S_PartyError`)을 클라가 **파싱해서 클라 측 파티 미러 상태에 반영**한다. 이게 끝나면 P2~P5의 UI(팝업/멤버 HUD/퀘스트 HUD)가 읽어갈 *클라 파티 상태*가 생긴다. 클라는 권위가 없다 — 서버가 통보한 파티 구성을 **거울처럼 그대로 표시**할 뿐이다.

---

## ⏪ 사전 조건

- [ ] Phase 04 (A3 — 파티 핸들러 happy path) 완료 — 서버가 `S_PartyInviteRecv`/`S_PartyUpdate`를 실제로 송신.
- [ ] Phase 05 (A4 — 파티 신뢰경계) 완료 — `S_PartyError`(거절 사유) 송신.
- [ ] Phase 01 (A0 — PDL 8패킷 + v15 bump) 완료 — 클라가 v15 패킷을 파싱할 수 있게 Shared.dll 재참조.

---

## 📝 작업 내용

- [ ] 신규 핸들러 3개 — `03_Client/Assets/Scripts/Network/Handlers/Party/`:
  - `PartyInviteRecvHandler.cs` — `S_PartyInviteRecv` 파싱 → 초대 정보를 `PartyState`(또는 팝업 트리거용)로 전달.
  - `PartyUpdateHandler.cs` — `S_PartyUpdate` 파싱 → 멤버 목록(member0/member1) 클라 미러 갱신.
  - `PartyErrorHandler.cs` — `S_PartyError` 파싱 → 에러 코드 전달 (UI 피드백용).
- [ ] `UnityClientSession` — 3개 핸들러 dispatch 등록 (각 1줄, 기존 dispatch 패턴 동형).
- [ ] 신규 `03_Client/Assets/Scripts/Gameplay/PartyState.cs` — **클라 파티 미러**. 현재 파티 멤버(entityId 2슬롯), 파티 여부, 갱신 이벤트(UI가 구독). 서버 통보로만 갱신되는 read-mostly 상태.
- [ ] **MainThreadDispatcher 경유** — 네트워크 스레드에서 받은 패킷 처리를 Unity 메인 스레드로 마샬링 (UI 갱신은 메인 스레드 전용).

---

## ✅ 완료 조건

- [ ] `S_PartyUpdate` 수신 → `PartyState` 클라 미러가 멤버 목록으로 갱신됨 (디버그 로그 또는 P3 HUD로 확인).
- [ ] `S_PartyInviteRecv` 수신 → 초대 정보가 전달됨 (P2 팝업 트리거 준비).
- [ ] `S_PartyError` 수신 → 에러 코드 전달.
- [ ] 모든 패킷 처리가 **MainThreadDispatcher 경유** (스레드 안전).
- [ ] **Unity 컴파일 0err** (메인 MCP RunCommand probe).

---

## 🧪 테스트

**자동**: Unity 컴파일 0err (MCP). 패킷 파싱 순수 로직 분리 가능하면 EditMode.
**수동(아침)**: 2-클라 또는 봇+클라로 초대→수락 시 `PartyState` 미러 갱신 로그/HUD 확인.

---

## 📚 학습 포인트

- **클라 미러 상태 (server-authoritative mirror)** — 클라는 파티를 *만들거나 바꾸지 않는다*. 서버 `S_PartyUpdate`를 받아 자기 쪽 `PartyState`를 *복사*할 뿐. UI는 이 미러를 읽는다. 권위는 서버, 클라는 표현 (헌법 §1).
- **네트워크 스레드 → 메인 스레드 마샬링** — 소켓 수신은 별도 스레드, Unity UI/Transform은 메인 스레드 전용. MainThreadDispatcher로 넘기지 않으면 크래시/미정의 동작. "어느 스레드에서 실행되는가"를 항상 의식.
- **dispatch 등록 패턴** — 새 패킷 핸들러 = 핸들러 클래스 1개 + `UnityClientSession` 1줄. 기존 패턴을 따르면 일관성 유지.

---

## ⚠️ 함정 / 주의사항

- **클라는 서버 통보만 표시 (권위 X)** — `PartyState`를 클라가 임의로 바꾸지 않는다. 초대/수락/탈퇴 *요청*은 보내지만, 파티 구성의 진실은 항상 서버 `S_PartyUpdate`.
- **MainThreadDispatcher 누락 = 크래시** — 핸들러에서 직접 UI/이벤트를 건드리면 스레드 위반. 반드시 메인 스레드로 마샬링.
- **member0/member1 2슬롯 (PDL 가변 list 미지원)** — 빈 슬롯은 `entityId=0`. 정원 2 고정 (서버 설계 정합).

---

## ➡️ 다음 Phase

- Phase 13 (P2) — 파티 초대 송신 + 수락/거절 팝업 UI.
- (병렬 가능) Phase 14 (P3) 멤버 HUD, Phase 15 (P4) 퀘스트 HUD — 모두 이 Phase의 `PartyState`/핸들러 의존.

---

## 📋 박제 (완료 후)

- 보통 → work-pin + commit message. 마일스톤 `-DONE.md`에 흡수.

---

## 작업 로그

- 2026-06-14: 생성.
