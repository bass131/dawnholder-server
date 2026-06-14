---
owner: youngho
milestone: M5
phase: 03
title: GameWorld 통합 + cross-map 1:1 송신 헬퍼
status: pending
grade: 복잡
domain: server
estimated: 2~3h
---

# Phase 03: GameWorld 통합 + cross-map 1:1 송신 헬퍼

> **상태**: pending
> **마일스톤**: M5 (트랙 A — 파티 시스템 서버)
> **등급**: 복잡 (틱 루프 결선 + cross-map thread 안전 — 잘못하면 thread 위반)
> **담당**: server (Sonnet Worker — 메인 file:line 게이트)

---

## 🎯 목표

`GameWorld`가 `PartyRegistry`를 소유하고, 매 틱(`OnTick`)에서 `PartyRegistry.Tick()`을 호출해 큐를 드레인하게 결선한다. 그리고 파티 멤버가 서로 다른 맵에 있어도 메시지를 보낼 수 있도록 **cross-map 1:1 송신 헬퍼 `SendToEntity(entityId, payload)`**를 만든다. 이 헬퍼는 멤버가 현재 있는 맵을 찾아 *그 맵의 EnqueueJob 안에서* `session.Send`를 호출한다(thread 안전).

> 이게 파티 시스템의 가장 위험한 thread 지점이다. 각 맵은 자기 틱을 가진 actor(자기 스레드)다. 다른 맵의 세션에 직접 `Send`하면 그 맵의 스레드를 침범한다 = race. 반드시 대상 맵의 EnqueueJob을 통해 그 맵 스레드 위에서 송신해야 한다.

---

## ⏪ 사전 조건

- [ ] Phase 02 완료 (`PartyRegistry` actor + `Tick()` 드레인 메서드 존재).

---

## 📝 작업 내용

- [ ] `02_Server/GameServer/Loop/GameWorld.cs` — `PartyRegistry` 인스턴스 소유(필드 + 초기화).
- [ ] 같은 파일 `OnTick`(또는 틱 콜백)에서 `PartyRegistry.Tick()` 호출 추가 — 매 틱 큐 드레인.
- [ ] `SendToEntity(int entityId, ReadOnlySpan<byte> payload)` 헬퍼 구현:
  - entityId → 현재 맵 찾기(맵 레지스트리/플레이어 위치 조회).
  - 찾은 맵의 `EnqueueJob` 안에서 해당 세션 `session.Send(payload)` 호출.
  - 멤버가 어느 맵에도 없으면(로그아웃 등) silent 무시 또는 false 반환.
- [ ] `PartyRegistry`가 송신이 필요할 때 `GameWorld.SendToEntity`를 호출할 수 있도록 결선(콜백 주입 또는 GameWorld 참조 — actor 경계 유지).

---

## ✅ 완료 조건

- [ ] xUnit: `PartyRegistry.Tick()`이 매 틱 호출되어 큐 작업이 드레인됨.
- [ ] xUnit: `SendToEntity`가 멤버의 *현재 맵*을 찾아 그 맵 `EnqueueJob` 경유로 송신(직접 `session.Send` 아님 — thread 안전 증명).
- [ ] xUnit: 서로 다른 맵의 두 멤버 각각에게 `SendToEntity`가 올바른 맵으로 라우팅.
- [ ] WSL2 `dotnet build` 0/0 + `dotnet test` green (baseline 회귀 0).

---

## 🧪 테스트

**자동**:
- `GameWorldCrossMapTests` — Tick 드레인 동작 + SendToEntity 라우팅(멤버 맵 EnqueueJob 경유) + 멤버가 다른 맵일 때 각각 정확 송신.

**수동**: 없음(thread 안전성은 헤드리스 검증 — 봇 e2e는 R1에서).

---

## 📚 학습 포인트

> 학부생 시각.

- **맵 = actor, 각자 스레드** — 이 게임은 맵(방)마다 독립 틱 actor다. actor A의 스레드에서 actor B의 내부 상태(B의 세션 송신 버퍼)를 직접 건드리면 thread-safety가 깨진다. 메시지 패싱(EnqueueJob)으로 *B의 스레드 위에서* 처리하게 넘기는 게 actor 모델의 핵심.
- **cross-map 송신의 라우팅 문제** — 보내려는 대상이 어느 맵에 있는지는 *지금 이 순간*에만 알 수 있다(멤버가 맵 이동 중일 수 있음). 그래서 송신 시점에 entityId→맵 조회를 하고, 그 맵의 잡 큐로 마샬링한다. "where is this entity now?"를 매번 푸는 게 cross-map의 본질.
- **thread marshaling(스레드 마샬링)** — 한 스레드의 작업을 다른 스레드의 큐로 넘겨 그쪽에서 실행시키는 패턴. UI 프레임워크의 `Invoke`, 게임 서버의 `EnqueueJob`이 같은 개념. lock 없이 cross-thread 안전을 얻는 방법.

---

## ⚠️ 함정 / 주의사항

- **다른 맵 tick 스레드 직접 호출 금지** = thread 위반(헌법 절대 원칙). `SendToEntity`는 *반드시* 대상 맵의 `EnqueueJob`을 경유. 직접 `session.Send`를 호출하면 그 맵 스레드를 침범한다.
- **틱 루프 블로킹 금지** (헌법 §5) — `Tick()` 드레인은 비동기 대기·동기 DB·`Thread.Sleep` 0. 순수 in-memory 큐 처리만.
- **멤버가 맵 전환 중인 순간** — entityId 조회가 일시적으로 실패할 수 있다. 이때 crash 대신 silent 무시(다음 틱 재시도 또는 그냥 드랍). 송신 실패가 파티 상태를 깨뜨리지 않게.
- **GameWorld ↔ PartyRegistry 결선이 actor 경계를 안 깨게** — PartyRegistry가 GameWorld를 직접 참조하더라도, 송신은 GameWorld의 thread-safe 헬퍼(`SendToEntity`)를 통해서만. PartyRegistry가 세션을 직접 만지면 안 됨.

---

## ➡️ 다음 Phase

- Phase 04 — 파티 초대/수락/탈퇴 핸들러 happy path + 세션 결선.

---

## 📋 박제 (완료 후)

- 복잡 등급 → `03-gameworld-crossmap-send-DONE.md`(요약 + 사실 박제 + 학습 키워드 — 특히 cross-map thread 패턴). HTML은 마일스톤 마감(R3)에서 종합.

---

## 작업 로그

- 2026-06-14: 생성.
