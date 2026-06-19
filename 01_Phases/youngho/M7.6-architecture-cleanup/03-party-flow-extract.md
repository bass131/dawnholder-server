---
phase: P03
title: 파티 오케스트레이션 추출 — GameSession → PartyFlow
milestone: M7.6
owner: youngho
grade: 복잡
risk: trust-boundary (세션 lifecycle + 파티 검증 = 신뢰 경계)
depends_on: [P01]
blocks: []
status: in_progress
---

# P03 — 파티 오케스트레이션 추출 (GameSession → PartyFlow)

> 근거: 감사 #3. GameSession(세션 lifecycle + dispatch)에 *파티 도메인 오케스트레이션*(검증 규칙·결성·해산·통보)이 EnqueueJob 람다로 박혀 있음 → SRP 위반·도메인 누수.

## 🎯 목표

GameSession의 파티 4개 메서드에서 **파티 비즈니스 로직을 `PartyFlow`(파티 오케스트레이션 계층)로 추출**. GameSession은 *세션 auth 게이트 + 위임*만. 동작 *불변*(검증 규칙·순서·통보 동치). 신뢰 경계(행위자=`_entityId` 강제) 보존.

## 📏 현황 실측 (2026-06-19, GameSession.cs)

| 메서드 | 줄 | 구조 |
|---|---|---|
| `SubmitPartyInvite(int)` | 381–423 | auth + EnqueueJob(거절4종 검증 + RecordInvite + 통보) |
| `SubmitPartyRespond(int, byte)` | 425–457 | auth + EnqueueJob(보류매칭 + 위장검증 + Consume + CreateParty + 통보) |
| `SubmitPartyLeave()` | 459–479 | auth + EnqueueJob(GetPartyByEntity + Disband + 통보) |
| `CleanupPartyOnDisconnect()` | 507–530 | world/entity 게이트 + EnqueueJob(RemoveInvites + Disband + 통보) |

공통: ① `_entityId < 0` + `world == null` auth 게이트(세션) → ② entityId/params 캡처 → ③ `world.Party.EnqueueJob(() => {파티 로직})`.

## 🧭 설계 결정 — PartyFlow 정적 오케스트레이션

**신설** `02_Server/GameServer/Party/PartyFlow.cs` — `internal static class`. EnqueueJob 마샬링 + 파티 비즈니스 로직 소유. 행위자 entityId를 *인자로 받음*(세션이 검증한 `_entityId` 전달 = 신뢰 경계 보존).

```
PartyFlow.Invite(GameWorld world, int inviterEntityId, int targetEntityId)
PartyFlow.Respond(GameWorld world, int responderEntityId, int claimedInviter, bool accepted)
PartyFlow.Leave(GameWorld world, int leaverEntityId)
PartyFlow.CleanupOnDisconnect(GameWorld world, int leaverEntityId)
```

각 메서드 = 기존 람다 본문을 *그대로* + `world.Party.EnqueueJob` 마샬링 포함.

GameSession Submit* = 얇아짐 (auth 게이트만 잔류 + 위임):
```csharp
internal void SubmitPartyInvite(int targetEntityId)
{
    if (_entityId < 0) return;
    GameWorld? world = GameWorld.Instance;
    if (world == null) return;
    PartyFlow.Invite(world, _entityId, targetEntityId);
}
```
(`CleanupPartyOnDisconnect`는 기존 게이트 순서[world 먼저, entity 나중] 보존.)

**경계 원칙**: `_entityId`/`world` 게이트 = *세션 관심사*라 GameSession 잔류(세션이 `_entityId` 소유). 파티 규칙·마샬링·통보 = PartyFlow. **행위자 강제 불변식**: GameSession이 PartyFlow에 넘기는 entityId는 *항상* `_entityId`(패킷값 X) — 위장 차단 보존. 관련 trust-boundary 주석을 PartyFlow로 이전.

## ✅ 완료 조건 (done 판사, ADR-029)

- [ ] 빌드 0 error / 신규 warning 0 (baseline SA1202 3건 유지).
- [ ] WSL2 회귀 green — **테스트 수 658 비감소** (P02 기준).
- [ ] 봇 회귀 P03 델타 0 (파티 봇 MultiRosterSmoke 등 baseline 동일).
- [ ] `reviewer` 🔴 0.
- [ ] `Protocol.Version` 불변 (와이어 0 변경).
- [ ] `git diff` = 순수 추출 (파티 검증 로직 *동치 그대로* 이동 — 규칙/순서/통보 변경 0).
- [ ] 02_Server/CLAUDE.md Layout 표에 `Party/PartyFlow.cs` 반영(동일 commit).

## ⚠️ 함정

- **trust-boundary 약화 0**: 행위자=`_entityId` 강제(위장 차단), claimedInviter 일치 검증(SubmitPartyRespond), 거절 4종(self/missing/already/만료) 전부 *동치 보존*. 검증 순서가 바뀌면 안 됨(early-return 순서가 거절 reason 결정).
- **actor 경계 보존**: EnqueueJob 마샬링 유지 — PartyFlow도 직접 PartyRegistry 호출 X(헌법 §5). 람다가 tick thread에서 실행되는 불변식 그대로.
- **auth 게이트 위치**: `_entityId`/`world` null 게이트는 GameSession 잔류(세션이 `_entityId` 소유 — PartyFlow는 검증된 entityId 가정). 각 메서드 기존 게이트 순서 보존.
- **테스트 진입점 불변**: 테스트가 `session.SubmitParty*`를 호출하면 시그니처/동작 불변이라 그대로 PASS. PartyFlow 직접 테스트는 불요(진입점 동치).
