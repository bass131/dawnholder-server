---
phase: P02
title: 치트 게이트 빌드 종속 — C_CheatCommand를 #if DEBUG로 완전 배제
milestone: M7.6
owner: youngho
grade: 복잡
risk: trust-boundary (헌법 #3 위반 봉합 — 유일한 감사 헌법위반 SN-02)
depends_on: [P01]
blocks: []
status: in_progress
---

# P02 — 치트 게이트 빌드 종속 (preM8★, 헌법 #3 봉합)

> 근거: 감사 #2 / SN-02 (`../../../00_Document/reviews/2026-06-19-architecture-logic-audit.html`) — **유일한 헌법 위반**.
> 현재 치트가 *런타임 플래그*(`DebugConfig.AllowCheats=true`)에만 의존 → Release/프로덕션 빌드에서도 치트가 살아있고, "배포 시 수동으로 false로 내려야 함" 주석에 안전을 맡김. 헌법 #3(신뢰 경계)은 *빌드 타임 보장*을 요구.

## 🎯 목표

치트 서브시스템 전체를 **`#if DEBUG`로 완전 배제** — Release 빌드에는 치트 코드가 *물리적으로 부재*하고 `C_CheatCommand`가 dispatch 테이블에 *미등록*(unknown PacketID → silent drop)이 되게 한다. DEBUG 빌드 동작은 *불변*(시연 치트 F8 그대로 작동).

## 📏 현황 실측 (2026-06-19, file:line)

| 위치 | 줄 | 현재 |
|---|---|---|
| `Handlers/HandlerRegistry.cs` | 28 | `{ PacketID.C_CheatCommand, new CheatCommandHandler() },` 무조건 등록 |
| `Handlers/Debug/CheatCommandHandler.cs` | 13–26 | 핸들러. 런타임 게이트 `if (!DebugConfig.AllowCheats) return;` (17) |
| `Debug/DebugConfig.cs` | 11–14 | `public static readonly bool AllowCheats = true;` (수동 토글) |
| `Network/GameSession.cs` | 483–496 | `internal void SubmitCheatCommand(byte)` — 핸들러 유일 호출 |
| `Quest/QuestRegistry.cs` | (P01) | `DebugCompleteQuest(int, GameWorld)` — SubmitCheatCommand 유일 호출 |

호출 사슬(전부 치트 전용, 다른 용도 0): `HandlerRegistry → CheatCommandHandler → GameSession.SubmitCheatCommand → world.Quest.EnqueueJob → QuestRegistry.DebugCompleteQuest`.

## 🧭 설계 결정 — 완전 배제(full excision)

치트 사슬의 *모든 멤버가 치트 전용*(이중 목적 0)이므로 각 멤버를 `#if DEBUG`로 감싼다 = 치트 서브시스템이 *DEBUG 빌드에만 존재*.

| 멤버 | 조치 |
|---|---|
| `HandlerRegistry.cs:28` 등록 | `#if DEBUG ... #endif` (★핵심 봉합 — Release 미등록) |
| `CheatCommandHandler.cs` 클래스 | 전체 `#if DEBUG` (유일 참조=위 등록) |
| `DebugConfig.cs` 클래스 | 전체 `#if DEBUG` + 주석 갱신(빌드타임 봉합이 1차, AllowCheats는 DEBUG 내 2차 토글) |
| `GameSession.SubmitCheatCommand` | `#if DEBUG` (유일 호출자=핸들러) |
| `QuestRegistry.DebugCompleteQuest` | `#if DEBUG` (유일 호출자=SubmitCheatCommand) |

**왜 minimal(등록만 #if DEBUG)이 아니라 full**: 등록만 감싸도 dispatch는 봉합되나(C_CheatCommand 미dispatch), 핸들러·메서드가 Release 바이너리에 *도달 불가 dead code*로 잔류 → 미래 개발자가 재등록 위험 + 부채. 치트 멤버는 전부 단일 목적이라 full 배제가 의도를 가장 명확히 박제. trade-off: `#if DEBUG`가 5곳 — 단 각 위치가 *치트 전용 경계*라 의미상 정확(sprawl 아님).

## 🔬 ★ 위반 봉합 별도 증명 (plan-auditor 함정 봉합)

**"회귀 green ≠ 위반 봉합"**: WSL2 통상 회귀는 DEBUG 구성 → 치트가 *정상 작동*(F8 유지)이라 green이어도 *위반 제거를 증명 못 함*. 위반 봉합은 **Release 구성에서 별도 증명**:

### 빌드게이트 증명 테스트 (신설)

`02_Server/GameServer.Tests/Handlers/CheatBuildGateTests.cs`:
```csharp
[Fact]
public void C_CheatCommand_Registration_IsBuildGated()
{
    bool registered = HandlerRegistry.TryGet(PacketID.C_CheatCommand, out _);
#if DEBUG
    Assert.True(registered,  "DEBUG 빌드 = 치트 등록(시연 F8)");
#else
    Assert.False(registered, "Release 빌드 = 치트 미등록 (헌법 #3 빌드타임 봉합)");
#endif
}
```
한 테스트가 *양 구성에서 PASS* — DEBUG는 `#if` 분기, Release는 `#else` 분기. (HandlerRegistry는 internal → 테스트 프로젝트 InternalsVisibleTo 기존 확보 확인 필요.)

### 증명 실행 (둘 다 트랜스크립트 박제)

1. **DEBUG 회귀**(통상): WSL2 build + test → **테스트 수 658 비감소**(657 + 신규 1) / 0 fail. `#if` 분기 = 등록 확인.
2. **RELEASE 증명**: `dotnet build -c Release` + `dotnet test -c Release --no-build` → **빌드 0 error** + 위 테스트 `#else` 분기 PASS = **C_CheatCommand 미등록 정량 확인**. (치트 멤버 #if DEBUG가 Release 컴파일에서 빠져도 빌드 성공 = dangling 참조 0 동시 증명.)

## ✅ 완료 조건 (done 판사, ADR-029 + plan-auditor)

- [ ] DEBUG: WSL2 build 0 error + test **658 비감소** / 0 fail (DEBUG 치트 동작 불변 — F8 시연 유지).
- [ ] **★ RELEASE: `dotnet build -c Release` 0 error + `dotnet test -c Release` 빌드게이트 테스트 `#else` PASS** = C_CheatCommand 미등록 별도 증명(트랜스크립트 박제).
- [ ] `reviewer` 🔴 0.
- [ ] `Protocol.Version` 불변 (PDL/와이어 0 변경 — C_CheatCommand PacketID는 *정의에 잔류*, 서버 *등록*만 빌드 게이트. 은퇴 아님).
- [ ] dangling 0 — 치트 멤버 #if DEBUG가 Release에서 빠져도 미참조 컴파일 에러 0(테스트 포함).
- [ ] DebugConfig/CheatCommandHandler 주석이 빌드타임 봉합 반영(stale "수동 false" 정정).

## ⚠️ 함정

- **PacketID는 은퇴 X**(헌법 #2): `C_CheatCommand` PDL 정의·PacketID는 *그대로 유지* — Release에서 *서버 등록*만 빠짐. PDL/Protocol.Version 건드리지 말 것.
- **DEBUG 동작 불변**: 시연 치트(F8→퀘스트 즉시완료)는 DEBUG에서 *정확히 그대로* 작동. 658-1=657 기존 테스트 동작 불변.
- **Release 테스트 컴파일**: 치트 멤버를 #if DEBUG로 빼면, *그 멤버를 직접 참조하는 테스트*가 있으면 Release 컴파일 깨짐. 사전 grep으로 SubmitCheatCommand/DebugCompleteQuest 직접 참조 테스트 0 확인(있으면 그 테스트도 #if DEBUG).
- **InternalsVisibleTo**: 빌드게이트 테스트가 internal HandlerRegistry.TryGet 호출 — 기존 핸들러 테스트가 이미 internal 접근하므로 확보돼 있을 것(확인).
