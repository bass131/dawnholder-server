---
owner: youngho
milestone: M5
phase: 05
title: 파티 신뢰경계 검증 + disconnect 정리
status: pending
grade: 복잡
risk: trust-boundary
domain: server
estimated: 2h
---

# Phase 05: 파티 신뢰경계 검증 + disconnect 정리

> **상태**: pending
> **마일스톤**: M5 (트랙 A — 파티 시스템 서버)
> **등급**: 복잡 + **trust-boundary**(거절 경로 + disconnect race) → 구현 Worker = **Opus**(routing B)
> **담당**: server (Opus Worker — 메인 file:line 게이트 + reviewer 자동)

---

## 🎯 목표

04의 happy path에 **방어 경로**를 채운다: 거절 4종(자기 자신 초대 / 이미 파티 중 / 정원 초과 / 초대 만료)을 `S_PartyError`로 거부하고, 잘못된 응답(`inviterEntityId` 불일치)은 silent drop하며, 멤버가 disconnect하면 파티를 정리한다. 신뢰 경계를 지나는 모든 입력을 검증하고, disconnect race를 actor 마샬링으로 막는다.

> 04가 "정상이면 동작"이라면, 05는 "악성·비정상 입력이 들어와도 안 깨짐"이다. 파티는 첫 플레이어 간 협동 시스템이라 신뢰 경계가 특히 중요하다 — 남을 멋대로 파티에 넣거나, 끊긴 멤버가 유령 파티로 남으면 안 된다.

---

## ⏪ 사전 조건

- [ ] Phase 04 완료 (happy path 핸들러 3종 + GameSession Submit* 결선).

---

## 📝 작업 내용

- [ ] `02_Server/GameServer/Party/PartyRegistry.cs`:
  - **pending invite 추적** — 초대 발신 시 `(inviter, target)` 보류 기록 + **타임아웃**(Tick 드레인에서 만료 체크).
  - 거절 4종 → `S_PartyError{reason}` (reason 0~3):
    - 0 = 자기 자신 초대 (`targetEntityId == 본인`)
    - 1 = 이미 파티 중 (초대자 또는 대상이 파티 보유)
    - 2 = 정원 초과 (파티 정원 2 — 사실상 이미 파티 중과 겹칠 수 있으나 명시 reason)
    - 3 = 초대 만료 (응답 시점에 pending invite 없음/타임아웃)
  - **응답 race silent drop** — `C_PartyRespond.inviterEntityId`가 pending invite의 실제 초대자와 불일치하면 조용히 무시(에러 X — 위조/지연 응답).
- [ ] `02_Server/GameServer/Network/GameSession.cs`:
  - `OnDisconnected` → `PartyRegistry.EnqueueJob`으로 파티 정리 작업 마샬링(파티 보유 시 해산 + 남은 멤버 통보).
  - disconnect race 방어 — `_closing` Interlocked 플래그로 이중 정리/뒤늦은 송신 차단.

---

## ✅ 완료 조건

- [ ] xUnit: 거절 4종 각각 → `S_PartyError` reason 0/1/2/3 정확.
- [ ] xUnit: 응답 race(`inviterEntityId` 불일치) → silent drop(에러도 파티 변경도 없음).
- [ ] xUnit: 초대 타임아웃 후 응답 → reason 3(만료).
- [ ] xUnit: 멤버 disconnect → 파티 해산 + 남은 멤버 통보, 이중 정리 없음.
- [ ] WSL2 `dotnet build` 0/0 + `dotnet test` green (baseline 회귀 0).
- [ ] reviewer 헌법 hard 위반 0 (trust-boundary = reviewer 자동 호출).

---

## 🧪 테스트

**자동**:
- `PartyTrustBoundaryTests` — 거절 4종 reason 매핑 + 응답 race silent drop + 초대 만료 + disconnect 해산/race.

**수동**: 없음(방어 경로는 헤드리스로 망라. 봇 e2e는 R1).

---

## 📚 학습 포인트

> 학부생 시각.

- **거절 vs silent drop 구분** — *정당한 사용자의 잘못된 요청*(자기 초대 등)은 `S_PartyError`로 이유를 알려준다(UX). 그러나 *위조/지연된 응답*(inviterEntityId 불일치)은 조용히 버린다 — 에러를 보내면 공격자에게 "여기 무슨 일이 있다"는 정보를 주고, 오래된 패킷에 반응해 상태가 흔들린다. "정상 실수는 알리고, 의심 입력은 침묵"이 신뢰 경계의 미세 규율.
- **타임아웃이 필요한 이유** — pending invite를 무기한 들고 있으면, 응답 안 온 초대가 쌓여 메모리 누수 + "오래전 초대를 지금 수락" 같은 이상 동작이 난다. Tick 드레인에서 만료를 청소하면 자연 정리된다.
- **disconnect race** — 멤버가 끊기는 순간, 그 세션 스레드와 PartyRegistry actor 스레드가 동시에 같은 파티를 건드릴 수 있다. `OnDisconnected`에서 직접 정리하면 race. 대신 `EnqueueJob`으로 actor에게 넘기면 직렬화돼 안전. `_closing` Interlocked는 "이미 정리 중"을 표시해 이중 처리/뒤늦은 송신을 막는다.

---

## ⚠️ 함정 / 주의사항

- **trust-boundary** — 거절/검증 로직이 핵심. Opus Worker + reviewer 자동. 거절 reason은 정확히 4종(0~3) 매핑.
- **disconnect 정리는 반드시 EnqueueJob 경유** — `OnDisconnected`에서 PartyRegistry 내부를 직접 호출하면 actor 경계 위반 + race. 마샬링 필수.
- **`_closing` Interlocked** — disconnect가 두 번 들어오거나(정상 종료 + 소켓 에러) 정리 중 송신 시도가 겹치면 NPE/이중 해산. Interlocked로 한 번만.
- **GameSession.cs는 04 다음, Q3 이전** — 같은 파일을 여러 Phase가 만지므로 의존성 순서 엄수(04→05→Q3).

---

## ➡️ 다음 Phase

- Phase 06 — HandleEnemyDeath killer 전파(트랙 Q 시작 — 퀘스트 카운트 토대).

---

## 📋 박제 (완료 후)

- 복잡+trust-boundary → `05-party-trust-boundary-DONE.md`(요약 + 사실 박제 + reviewer 요약 + 거절 reason 표 + 학습 키워드). HTML은 마일스톤 마감(R3)에서 종합.

---

## 작업 로그

- 2026-06-14: 생성.
