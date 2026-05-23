---
owner: youngho
milestone: M4.1
phase: 02
title: Session State Machine Hardening (P0-1 + P0-2 봉합)
status: done
grade: 복잡
risk: trust-boundary
summary: 캐릭터 선택 강제 + 월드 진입 게이트 풀세트 봉합. server `EnterGameWorldIfReady` idempotent 게이트 + client event 기반 `C_CharacterSelect` 송신 (옵션 A). 신규 6 단위 테스트 통과 + reviewer Tier 2-A GO + 봇 시나리오 4개 정합.
---

# Phase 02 — DONE

**완료 일자**: 2026-05-23
**소요**: ~2.5h (server SubAgent ~10분 / client SubAgent ~5분 / reviewer Tier 2-A ~3분 / 의논 + 보정 박음 잔여)

---

## TL;DR

P0-1 (캐릭터 선택 전 월드 진입 가능) + P0-2 (C_CharacterSelect 서버 상태 전이 강제) 풀세트 봉합. 사용자 보정 1 (신규 패킷 박지 X) 강제 준수 — 기존 패킷 drop/disconnect로 강제 + 클라 event 기반 송신 (옵션 A) 정합. **reviewer Tier 2-A GO + 결함 0건** = 다음 Phase 03 진입 깨끗한 베이스.

---

## 📦 산출물 분담 (server + client 분리 작업)

### server 측 (commit `3f1d45c`)
- `GameSession.EnterGameWorldIfReady()` 신설 — idempotent 게이트 (`_handshakeCompleted && HasSelectedClass && !_enteredWorld`)
- `CompleteHandshakeAndEnter()`에서 `EnterGameWorld()` 직접 호출 제거 → handshake = 상태 전이만
- `CharacterSelectHandler` `SetCharacterClass` 후 `EnterGameWorldIfReady()` 호출
- `MoveIntentHandler` + `AttackHandler` class 선택 전 silent drop + `[Trust]` 경고 로그
- `SessionStateMachineTests.cs` 신설 6건 + `HandshakeHandlerTests` 1건 개명
- 봇 시나리오 4개 `C_CharacterSelect` 송신 박음 (M2BasicMovement/Boss/Emergency/MultiRoster)

### client 측 (본 commit 묶음)
- `CharacterSelectController` `SendSelect` 제거 + `SaveSelectAndLoad` 신설 (PlayerPrefs 저장만)
- `NetworkBootstrap` class 선택 검증 게이트 + `OnHandshakeOk` event handler + `ReturnToMainMenu` + event 해제
- `UnityClientSession` `OnHandshakeOkEvent` event 신설 + `HandleHandshakeResult` invoke (main thread dispatch)

---

## AC 검증 결과

| AC | 결과 | 검증 |
|---|---|---|
| `HandshakeHandler.cs` `EnterGameWorld` 매치 0 | ✅ | grep 통과 |
| `CharacterSelectHandler.cs` `EnterGameWorldIfReady` 매치 1 | ✅ | grep 통과 (line 54) |
| `SessionStateMachineTests` 6건 통과 | ✅ | `dotnet test --filter "FullyQualifiedName~SessionStateMachineTests"` PASS |
| baseline 회귀 0 (총 178건) | ✅ | `dotnet test Dawnholder.slnx` PASS |
| 클라 default Warrior 자동 진입 가닥 제거 | ✅ | `UnityClientSession.Instance == null` 분기 (씬 직통 진입) 완전 제거 박힘 |
| 클라 event 기반 송신 (옵션 A) | ✅ | `OnHandshakeOkEvent` Invoke → `NetworkBootstrap.OnHandshakeOk` → `C_CharacterSelect` 송신 |
| event handler leak 방어 | ✅ | `OnApplicationQuit` 측 `-= OnHandshakeOk` 해제 박힘 |
| reviewer Tier 2-A 통과 | ✅ | 5축 PASS, 결함 0건 |

---

## 결정 흐름

1. **상태 머신 = enum vs flag 묶음** — server SubAgent 결정 = 기존 `_handshakeCompleted` (bool) + `_stats != null` 묶음 + 신규 `_enteredWorld` flag 활용. enum 신설 X. 사유 = 기존 테스트 회귀 위험 + 보정 1 "scope ↓" 정합. reviewer 평가 = 정합 (단 4번째 flag 추가 시 enum 응집 = M4.2 cheat-flag 또는 M5 reconnect 시 Rule of Three).
2. **race 봉합 옵션 A (event 기반)** — 클라가 `S_HandshakeResult(ok=true)` 수신 event 후 송신. 옵션 B (서버 큐 박음) 박지 X. 사유 = scope ↓ + `OnRecvPacket` first-packet 게이트가 race window 자연 봉합. reviewer 학습 포인트 = "event-based race elimination" 백엔드 면접 정공법 답변.
3. **silent drop vs disconnect** — server 측 = silent drop + `[Trust]` 로그 (M4.2 cheat-flag 도입 시 본 drop 박힘). 클라 측 = `ReturnToMainMenu()` 강제 회항 (default 자동 진입 가닥 제거).
4. **commit 분담** — server SubAgent commit (`3f1d45c`) + client + DONE 묶음 commit (본 commit) 분리. 사유 = 분담 commit 분리 정합 + Phase 마감 박제는 한 묶음.

---

## 학습 일지 후보 키워드 (★★ 후보 3건)

- **`event-based-race-elimination`** (reviewer 학습 포인트) — 클라 측 race window를 *동기화 lock* 아니라 *도착 신호 기다리기*로 봉합. C# event 패턴 + `OnApplicationQuit` 측 해제 leak 방어까지 = 한국 게임 회사 백엔드 면접 정공법 답변. M2.5 Phase 09/10 lifecycle race 학습 정합 *클라 측 확장*.
- **`server-state-machine-flag-vs-enum-trade-off`** — 기존 flag 묶음 (`_handshakeCompleted` + `HasSelectedClass` + `_enteredWorld`) vs enum 신설. 학부생 정신 = "기존 패턴 활용 + 4번째 flag 추가 시 enum 응집" (Rule of Three). M5 reconnect 박을 시점 = enum 박을 가닥 박음.
- **`subagent-split-server-client-with-reviewer-integration`** (메타) — Phase 02 풀세트가 server SubAgent + client SubAgent 두 묶음 분담 → reviewer Tier 2-A 통합 검증 정합. 옛 단일 SubAgent 패턴 (Phase 단위 한 SubAgent) → 새 *분담 + 통합 검증* 패턴. M4.1 P0 영역 reviewer 자동 호출 가치 실증.

---

## ➡️ 다음 액션

**Phase 03 진입 GO** — ClientNet Trust Boundary Symmetry (P0-4 / 보통 / client+shared / 1~2h).

작업 가닥 = `04_ClientNet/ClientSession.cs` 또는 `RecvBuffer.cs` framing 검증 분기 + `98_Shared/FrameValidator.cs` (또는 helper 재활용) + 단위 테스트 4건+ + 헤드리스 봇 fuzz + `04_ClientNet/CLAUDE.md:38` stale 정정.

별 시점 가닥 (M4.2 또는 M5):
- 🟡 race window 테스트 1건 추가 (`SessionStateMachineTests` 7번째, *순차 도착 ≠ race* 검증)
- 🟡 `SessionState` enum 응집 (4번째 flag 추가 시 = Rule of Three)
