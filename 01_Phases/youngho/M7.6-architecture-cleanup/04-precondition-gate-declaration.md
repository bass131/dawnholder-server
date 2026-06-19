---
phase: P04
title: 전제조건 게이트 선언화 — HasSelectedClass 복붙 제거 (#5)
milestone: M7.6
owner: youngho
grade: 복잡
risk: trust-boundary (게이트 = 신뢰 경계, 잠재 #3 구멍 봉합)
depends_on: []
status: in_progress
note: "감사 P04는 #8(사망진입점)+#5(게이트선언화). #8은 클라 시각 측면(bucket-b)+게임플레이크리티컬이라 별도 후속(이제 봇 16/16로 HpSyncSmoke 부활증명 가능). 본 Phase는 #5만."
---

# P04 — 전제조건 게이트 선언화 (#5)

> 근거: 감사 #5 / SN-05 (`../../../00_Document/reviews/2026-06-19-architecture-logic-audit.html`) — *위반 아님(현재 8핸들러 모두 게이트 보유)이나 복붙이라 새 핸들러 누락 시 미래 #3 위반 가능 → 구조적 예방*.

## 🎯 목표

8개 핸들러가 각자 첫 줄에 복붙한 `if (!session.HasSelectedClass) { 로그; return; }`를 제거하고, **`IPacketHandler.RequiresSelectedClass` 선언 프로퍼티 + dispatch 전 일괄 게이트**로 전환. 게이트 누락이 *구조적으로 불가능*(컴파일러가 새 핸들러에 선언 강제)해진다. 동작 *불변*(trust 로그 보존).

## 📏 현황 실측 (2026-06-19, file:line)

`if (!session.HasSelectedClass)` 복붙 8핸들러 (전부 `[Trust] C_Xxx before CharacterSelect — silent drop (cheat-flag candidate)` 로그 후 return, **CheatCommand만 silent**):
| 핸들러 | 줄 | 로그 |
|---|---|---|
| AttackHandler | 18 | C_Attack |
| MoveIntentHandler | 19 | C_MoveIntent |
| EnterPortalHandler | 20 | C_EnterPortal |
| SkillUseHandler | 20 | C_SkillUse |
| PartyInviteHandler | 17 | C_PartyInvite |
| PartyRespondHandler | 19 | C_PartyRespond |
| PartyLeaveHandler | 17 | C_PartyLeave |
| CheatCommandHandler | 22 (#if DEBUG) | (silent — 로그 없음) |

- dispatch: `GameSession.OnRecvPacket:182` (post-handshake) — `HandlerRegistry.TryGet(id, out handler)` → `handler.Handle(this, buffer)`.
- `CharacterSelectHandler:18`은 *반대* 게이트(`if (session.HasSelectedClass)` = 재선택 거부) — **본 작업 대상 아님, 잔류**.
- handshake 경로(157-178)는 별도(C_Handshake만 허용) — RequiresSelectedClass 무관.

## 🧭 설계 결정 — 추상 프로퍼티 (default 없이)

`IPacketHandler`에 **`bool RequiresSelectedClass { get; }` 추상 멤버**(default 구현 X) 추가 → 모든 핸들러가 *컴파일러 강제*로 명시 선언(누락 불가 = 안전 default 회피, 구멍 봉합의 핵심).

| 핸들러 | RequiresSelectedClass |
|---|---|
| MoveIntent·Attack·EnterPortal·SkillUse·PartyInvite·PartyRespond·PartyLeave·CheatCommand | `=> true` |
| Handshake·Ping·CharacterSelect | `=> false` |

dispatch 일괄 게이트 (`GameSession.OnRecvPacket:182`, Handle 호출 *전*):
```csharp
if (HandlerRegistry.TryGet(id, out IPacketHandler handler))
{
    if (handler.RequiresSelectedClass && !HasSelectedClass)
    {
        Console.WriteLine($"[Trust] {id} before CharacterSelect — silent drop (cheat-flag candidate)");
        return;
    }
    handler.Handle(this, buffer);
}
```
8핸들러의 `if (!session.HasSelectedClass) {...}` 블록 제거(로그 포함). trust 로그는 **dispatch에서 PacketID로 생성**해 동치 보존.

## ⚠️ 동작 불변 — 미세 차이 1건 (의도)

- **CheatCommand는 원래 silent drop**(로그 없음). 일괄 게이트는 trust 로그를 *모든* RequiresSelectedClass 핸들러에 찍으므로 **CheatCommand의 class-미선택 drop도 이제 로그**됨. DEBUG 전용 + 의미 동일([Trust] drop) + 일관성↑ → 허용. 박제.
- 그 외 7핸들러: 로그 메시지 동치(`C_Xxx` → `{id}`=동일 enum 이름). 게이트 위치만 핸들러→dispatch 이동, *판정·drop 동작 0 변경*.

## ✅ 완료 조건 (done 판사, ADR-029)

- [ ] 빌드 0 error / 신규 warning 0.
- [ ] WSL2 회귀 green — 테스트 수 **658 비감소**.
- [ ] 봇 회귀 16/16 green (e2e 안전망 — class 게이트 경로 회귀 0).
- [ ] `reviewer` 🔴 0.
- [ ] `Protocol.Version` 불변.
- [ ] 게이트 동치: class-미선택 입력이 여전히 drop(8핸들러). 기존 게이트 테스트(EnterPortalHandlerTests:258 drop, GameSessionRateLimitTests:58 등) PASS 유지.
- [ ] 새 핸들러가 RequiresSelectedClass 미선언 시 컴파일 에러(구조적 강제 증명 — 추상 멤버).

## ⚠️ 함정

- **trust-boundary 동치 0 약화**: class-미선택 → drop이 *정확히* 보존. 일괄 게이트가 Handle *전*에 실행(기존 핸들러-첫줄과 동일 시점).
- **로그 동치**: trust 로그 누락 시 cheat-flag 추적 회귀 → dispatch에서 PacketID로 보존 필수. 테스트가 로그 메시지 assert하면 형식 확인.
- **CharacterSelect 잔류**: 반대 게이트(재선택 거부)는 별개 — 건드리지 말 것. RequiresSelectedClass=false.
- **추상 멤버 = 11핸들러 전부 선언**: 누락 시 컴파일 에러(의도 — 그게 구멍 봉합).
