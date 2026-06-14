---
owner: youngho
milestone: M5
phase: 02
title: PartyState + PartyRegistry actor 코어
status: pending
grade: 복잡
domain: server
estimated: 2~3h
---

# Phase 02: PartyState + PartyRegistry actor 코어

> **상태**: pending
> **마일스톤**: M5 (트랙 A — 파티 시스템 서버)
> **등급**: 복잡 (신규 actor 도입 + cross-map 생애주기 데이터 구조)
> **담당**: server (Sonnet Worker — 메인 file:line 게이트)

---

## 🎯 목표

파티를 관리하는 **`PartyRegistry`를 GameWorld 소유의 별도 actor**로 만든다. actor = JobQueue로 작업을 받고 매 틱 드레인하는 단일 스레드 처리 단위(lock 없이 직렬화로 race를 막는 모델). 이번 Phase는 그 *코어 데이터 구조*(`PartyState`)와 *기본 연산*(생성/멤버 추가/해산/PartyId 채번)만 만든다. GameWorld 결선과 cross-map 송신은 다음 Phase(03).

> 왜 actor인가? 파티는 cross-map이다 — 한 멤버는 마을, 다른 멤버는 헌팅장에 있어도 파티가 유지돼야 한다. 그래서 특정 맵이나 세션에 파티를 둘 수 없다(그 맵/세션이 사라지면 파티도 사라짐). GameWorld가 소유하는 독립 actor가 정답. 헌법의 "actor 모델, lock 금지"와도 정합.

---

## ⏪ 사전 조건

- [ ] Phase 01 완료 (8패킷 + v15, `S_PartyUpdate`/`S_PartyError` 참조 가능).

---

## 📝 작업 내용

- [ ] 신규 `02_Server/GameServer/Party/PartyState.cs` — 파티 1개의 데이터:
  - `PartyId` (int, `Interlocked.Increment`로 단조 증가 채번)
  - `LeaderEntityId` (int)
  - `Members` (`List<int>`, 정원 2 — entityId만 보관)
  - `KillCount` (int — 퀘스트용, Q2에서 사용. 이번엔 필드만)
- [ ] 신규 `02_Server/GameServer/Party/PartyRegistry.cs` — actor 골격 + 기본 연산:
  - JobQueue(EnqueueJob) + `Tick()` 드레인 메서드(GameWorld가 매 틱 호출 — 결선은 03).
  - `CreateParty(leaderEntityId, memberEntityId)` → 새 PartyState 반환, 정원 2 채움.
  - `AddMember(partyId, entityId)` → 정원 초과 시 실패(invariant 보호).
  - `Disband(partyId)` → 파티 제거.
  - PartyId → PartyState lookup(dict).
- [ ] entityId 기반 식별 — 멤버는 **session 참조가 아니라 entityId로만** 보관(ADR-026 전역 entityId). disconnect 시 session이 사라져도 entityId는 안정.

---

## ✅ 완료 조건

- [ ] xUnit: `CreateParty` → PartyState 반환, 멤버 2명, 리더 정확.
- [ ] xUnit: `AddMember`가 정원 2 invariant 보호(3번째 추가 거부 또는 실패 반환).
- [ ] xUnit: `Disband` 후 lookup 실패(파티 제거됨).
- [ ] xUnit: PartyId가 연속 생성 시 단조 증가(`Interlocked` 채번 검증).
- [ ] WSL2 `dotnet build` 0/0 + `dotnet test` green (baseline 회귀 0).

---

## 🧪 테스트

**자동**:
- `PartyRegistryTests` — 생성/멤버추가/해산/정원2 invariant/PartyId 단조증가.

**수동**: 없음(순수 데이터 구조 — 헤드리스 검증으로 충분).

---

## 📚 학습 포인트

> 학부생 시각.

- **actor 모델 vs lock** — 여러 스레드가 파티 데이터를 동시 수정하면 race가 난다. 보통은 `lock`으로 막지만, lock은 데드락·성능 함정이 있다. actor 모델은 *모든 변경을 큐에 넣고 한 스레드가 순서대로 처리* → race 자체가 구조적으로 불가능. 헌법이 lock 대신 actor를 강제하는 이유.
- **왜 session이 아니라 entityId로 멤버를 식별하나** — session(소켓 연결 객체)은 disconnect 시 즉시 사라질 수 있다. 파티가 session을 들고 있으면 끊긴 멤버를 가리키다 null 참조·race가 난다. entityId는 전역에서 안정적(ADR-026)이라 이걸 키로 쓰면 disconnect race를 회피한다.
- **invariant(불변식)** — "파티 정원은 항상 ≤2"처럼 *항상 참이어야 하는 조건*. 데이터 구조가 invariant를 스스로 강제하면(추가 메서드가 정원 초과를 거부) 호출자 실수로 깨지지 않는다. 방어적 설계의 기본.

---

## ⚠️ 함정 / 주의사항

- **lock 금지** — actor 모델이므로 `PartyRegistry` 내부에 `lock`을 쓰면 설계 위반. 모든 변경은 EnqueueJob → Tick 드레인 경로로.
- **session 참조 보관 금지** — 멤버는 entityId(int)만. 어디서도 GameSession을 PartyState에 저장하지 말 것(disconnect race 근원).
- **정원 2 고정** — `Members` 리스트지만 정원은 2. PDL이 가변 list 미지원이라(Phase 01) 데이터 모델도 2슬롯 가정과 정합. 정원을 늘리려면 프로토콜부터 재설계.
- **`KillCount` 필드는 이번엔 정의만** — 실제 증가 로직은 Q2(Phase 07). 미리 필드만 둬서 다음 Phase가 PartyState를 또 안 건드리게 함.

---

## ➡️ 다음 Phase

- Phase 03 — GameWorld 통합 + cross-map 1:1 송신 헬퍼.

---

## 📋 박제 (완료 후)

- 복잡 등급 → `02-party-registry-actor-DONE.md`(요약 + 사실 박제 + 학습 키워드). HTML은 마일스톤 마감(R3)에서 종합.

---

## 작업 로그

- 2026-06-14: 생성.
