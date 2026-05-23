---
owner: youngho
milestone: M4.1
phase: 02
title: Session State Machine Hardening (P0-1 + P0-2 — 캐릭터 선택 강제 + 월드 진입 게이트)
status: completed
grade: 복잡
risk: trust-boundary
estimated: 2~3h
domain: server
---

# Phase 02: Session State Machine Hardening (P0-1 + P0-2)

> **상태**: pending
> **마일스톤**: M4.1
> **등급**: 복잡 (1 도메인 server / ~100줄 / trust-boundary 위험 깃발)
> **담당**: server SubAgent (Sonnet) + 클라 측 wiring 최소 (client SubAgent 자문 가능, MainMenu → CharacterSelect → Gameplay flow 정합)
> **사용자 보정 1 (2026-05-23)**: **신규 패킷 박지 X**. `S_CharacterSelectRequired` 같은 새 패킷 = PDL bump + 클라 dispatch 새 표면 회피. 서버는 *기존 패킷 drop/disconnect*로 강제 (헌법 #3 정합).

---

## 🎯 목표

**P0-1 (캐릭터 선택 전 월드 입장 가능) + P0-2 (C_CharacterSelect 서버 상태 전이 강제) 풀세트 봉합**. 시연 신뢰도 핵심 = 캐릭터 선택 안 한 채 월드 진입 / 클라가 C_CharacterSelect 안 보내고 입력 박는 가닥 차단.

본 Phase가 끝나면 = (a) `HandshakeHandler`에서 `EnterGameWorld()` 직접 호출 제거, (b) `CharacterSelectHandler`가 success 시 `EnterGameWorld()` 호출, (c) class 선택 전 `C_MoveIntent`/`C_Attack` drop 또는 disconnect (서버 측 silent reject), (d) 클라 `MainMenuController` → `CharacterSelectController` → `Gameplay` flow에서 `C_CharacterSelect` 송신 의무 (옛 옵션 X 강제).

---

## ⏪ 사전 조건

- [x] Phase 01 ✅ 마감 (Codex β 발견 = P0-1/P0-2 결함 확인)
- [x] M3.8 Phase 03 ✅ 마감 = `GameSession._stats` + `HasSelectedClass` + `CharacterSelectHandler` 박힘
- [x] M3.8 5-B ✅ 마감 = `MainMenuController` → `CharacterSelectController` 클라 측 flow 박힘

---

## 📝 작업 내용

### 1단계: 서버 상태 머신 정의 + 강제

- [ ] `02_Server/GameServer/Network/GameSession.cs` — 상태 enum 신설 또는 기존 flag 묶음:
  - `Connected` (handshake 전)
  - `Handshaken` (handshake OK 후, 캐릭터 선택 대기)
  - `CharacterSelected` (`HasSelectedClass = true`, EnterGameWorld 대기 또는 진입 완료)
- [ ] `HandshakeHandler.cs` (또는 GameSession 내부) — handshake 통과 후 `EnterGameWorld()` 직접 호출 *제거*. handshake = `_handshakeCompleted = true` + S_HandshakeResult(ok=true) 회신만, EnterGameWorld 호출 X.
- [ ] `CharacterSelectHandler.cs` — class 선택 success 시 `_session.SetCharacterClass(...)` 호출 *후* `_session.EnterGameWorldIfReady()` 호출 (idempotent 신설 메서드, 두 번 호출 시 두 번째 silent ignore).
- [ ] `GameSession.EnterGameWorldIfReady()` 신설 — `_handshakeCompleted == true && HasSelectedClass == true` 게이트, 둘 다 충족 시 옛 `EnterGameWorld()` 호출.

### 2단계: class 선택 전 입력 drop/disconnect (헌법 #3 정합)

- [ ] `MoveIntentHandler.cs` — class 선택 전 (`!HasSelectedClass`) `C_MoveIntent` 수신 시 silent drop (또는 trust-flag 로그). 조용한 drop이 정합 — disconnect 시 클라 측 reconnect storm 위험.
- [ ] `AttackHandler.cs` — 같은 패턴 (class 선택 전 `C_Attack` silent drop).
- [ ] 옛 사고 패턴 = "spawn 전 입력 수신 race" 방어 (`PlayerEntity` 박히기 전 입력 = `_entityId < 0` 박힘 박혀있음, M2.5 Phase 09 정합) — 본 Phase = 같은 정신 *class 선택 차원*으로 확장.

### 3단계: 클라 측 송신 의무 (M3.8 5-B flow 강화)

- [ ] `03_Client/Assets/Scripts/Scenes/CharacterSelectController.cs` — 선택값 (`CharacterClass.Warrior` 또는 `Ranger`)을 PlayerPrefs 또는 SceneManager state에 저장 후 Gameplay scene 진입.
- [ ] `03_Client/Assets/Scripts/Network/NetworkBootstrap.cs` 또는 `GameplaySceneController.cs` — Gameplay 진입 시 (a) `NetworkBootstrap` 서버 연결 + handshake 완료 후 (b) 저장된 선택값으로 `C_CharacterSelect` 즉시 송신.
- [ ] **race 봉합 (보정)**: `C_CharacterSelect` 송신 시점 = (옵션 A) 클라가 `S_HandshakeResult(ok=true)` 수신 event 후 송신 (event 기반, 권장) vs (옵션 B) 클라가 connect 즉시 송신 + 서버 측 `CharacterSelectHandler`에서 `_handshakeCompleted == false`면 큐 박음. 옵션 A 권장 = scope ↓ + 헌법 #3 정합 (handshake 완료 전 입력 = untrusted), 옵션 B = 서버 큐 박음 추가 비용. server SubAgent 위임 시 옵션 A 명시 의무.
- [ ] 옛 기본 default 클래스 자동 진입 가닥 *제거* — class 선택 안 됐으면 MainMenu로 돌려보냄 + Toast 안내 ("캐릭터 선택 후 진입해주세요").

### 4단계: 단위 테스트 박음

- [ ] `02_Server/GameServer.Tests/Network/SessionStateMachineTests.cs` 신설 (5건+):
  1. `EnterGameWorld_WithoutHandshake_Rejected` — handshake 안 박힘 + CharacterSelect 시도 = silent reject
  2. `EnterGameWorld_WithoutCharacterSelect_Rejected` — handshake OK 후 EnterGameWorld 직접 호출 X (옛 결함 회귀 방어)
  3. `EnterGameWorld_AfterCharacterSelect_Success` — handshake → CharacterSelect → EnterGameWorld 정상 transition
  4. `MoveIntent_BeforeCharacterSelect_Dropped` — class 선택 전 C_MoveIntent silent drop
  5. `Attack_BeforeCharacterSelect_Dropped` — class 선택 전 C_Attack silent drop
  6. `CharacterSelect_DuplicateAfterEnter_Rejected` — class 선택 후 두 번째 C_CharacterSelect 차단 (M3.8 Phase 03 박힘 정신 회귀 방어)
- [ ] dotnet test green (M3 baseline 회귀 0 + 5건+ 추가)

### 5단계: 헤드리스 봇 시나리오 검증

- [ ] `99_Tools/headless-bot/` 봇 시나리오 박음 (또는 기존 시나리오 확장):
  - 시나리오 A: handshake 후 C_CharacterSelect 안 보내고 5초 대기 → 서버 측 player roster 미박힘 확인
  - 시나리오 B: handshake → C_CharacterSelect → roster 박힘 + 일반 movement/attack 정상

### 6단계: Unity 정상 flow 수동 검증

- [ ] MainMenu → CharacterSelect (전사 선택) → Gameplay 진입 시 player roster 박힘 + 데미지 정상 (Phase 05 흡수 후 진짜 검증)
- [ ] MainMenu → Gameplay 직접 진입 시도 (CharacterSelect 우회) → MainMenu로 돌려보냄 + Toast 안내 확인

---

## ✅ 완료 조건

- [ ] `HandshakeHandler`에서 `EnterGameWorld()` 호출 제거 (`grep -n "EnterGameWorld" 02_Server/GameServer/Handlers/HandshakeHandler.cs` = 매치 0)
- [ ] `CharacterSelectHandler`에서 `EnterGameWorldIfReady()` 호출 박힘 (`grep -n "EnterGameWorldIfReady" 02_Server/GameServer/Handlers/CharacterSelectHandler.cs` = 매치 ≥1)
- [ ] `GameSession.EnterGameWorldIfReady()` idempotent 신설 (`_handshakeCompleted && HasSelectedClass` 게이트)
- [ ] `MoveIntentHandler` + `AttackHandler` class 선택 전 silent drop 분기 박힘
- [ ] 클라 `CharacterSelectController` 선택값 저장 + Gameplay 진입 시 자동 송신
- [ ] 클라 default 자동 진입 가닥 제거 (class 선택 X → MainMenu 돌려보냄 + Toast)
- [ ] `dotnet test` 신규 6건 모두 통과 + 옛 baseline 회귀 0
- [ ] 헤드리스 봇 시나리오 A/B 통과
- [ ] Unity 수동 정상 flow + 우회 시도 차단 확인
- [ ] 본 Phase 복잡 등급 = **-DONE.md 박음** (요약 + 사실 박제 + 학습 키워드)
- [ ] reviewer SubAgent Tier 2-A 자동 호출 통과 (5축 점검 + trust-boundary 위험 깃발 검증)

---

## 🧪 테스트

**자동**:
- `SessionStateMachineTests` 6건 + 기존 `HandshakeHandlerTests` / `CharacterSelectHandlerTests` 회귀 0
- 헤드리스 봇 시나리오 A/B

**수동**:
- Unity 클라 정상 flow + 우회 시도 차단
- 시연 dry-run (5분 안에 P0-1/P0-2 결함 재발 없음 확인)

---

## 📚 학습 포인트

- **서버 상태 머신 패턴** — `Connected` → `Handshaken` → `CharacterSelected` → `InWorld` 단계별 게이트 박음 = 분산 시스템 신뢰도 핵심 (Source/Quake/MMORPG 모두 같은 패턴). 학부 백지에서 "그냥 다 박으면 되지" 함정 회피.
- **새 패킷 vs 기존 패킷 drop trade-off** (보정 1 정합) — 새 패킷 박으면 PDL bump + 클라 dispatch 새 표면 = scope ↑. 기존 패킷 drop/disconnect = scope ↓ + 헌법 #3 정합 (untrusted input 차단). 학부생 정신 = "추가보다 차단".
- **idempotent 패턴** — `EnterGameWorldIfReady()` 두 번 호출 시 두 번째 silent ignore = race window 봉합 (handshake 패킷이 늦게 도착하거나 CharacterSelect 패킷이 먼저 도착하는 경우). M2.5 Phase 09 학습 정합.
- **silent drop vs disconnect trade-off** — silent drop = 클라 측 reconnect storm 회피 + UX 부드러움. disconnect = 명시 차단 + cheat 강제 발본. M4.1 응급 = silent drop (UX 우선), M4.2 cheat-flag 도입 시 silent drop + flag 박음 패턴.

---

## ⚠️ 함정 / 주의사항

- **handshake 핸들러에서 EnterGameWorld 호출 제거 누락 함정** — 옛 코드 `02_Server/GameServer/Handlers/HandshakeHandler.cs` 또는 `GameSession` 내부에 박힌 호출 = 본 Phase 핵심 변경. grep으로 매치 0 확인 의무.
- **클라 측 송신 누락 함정** — 클라 `CharacterSelectController` 선택값 저장만 박고 Gameplay에서 `C_CharacterSelect` 송신 빠지면 *서버 측 영원히 EnterGameWorld 안 함* = 데모 박힘. 수동 flow 검증 의무.
- **race window 함정 — handshake 패킷 + CharacterSelect 패킷 순서 보장 X** — 클라가 handshake 송신 → 서버 ack 받기 전 CharacterSelect 송신 시 서버 측 처리 순서 정합 필요. `CharacterSelectHandler`에서 `_handshakeCompleted == false`면 silent drop (또는 큐) 박을 가닥.
- **trust-boundary 위험 깃발** — class 선택 전 입력 silent drop = 헌법 #3 정합, 단 *silent*가 *허용*으로 오인되면 사고. 로그 박음 (warning 레벨) + M4.2 cheat-flag 도입 시 본 drop이 cheat 후보 박힘.
- **신규 패킷 박지 X 강제 (보정 1)** — `S_CharacterSelectRequired` 같은 푸시 패킷 박으면 PDL bump + 클라 dispatch + 테스트 = scope ↑. 서버는 *기존 패킷 drop*으로 충분. 본 강제는 Phase 02 정신 핵심.

---

## ➡️ 다음 Phase

- **Phase 03 (ClientNet Trust Boundary Symmetry)** — P0-4 봉합. Phase 02 서버 측 상태 머신 정합 후 클라/서버 framing 대칭 박음.

---

## 📋 박제 (완료 후)

- 복잡 등급 = **-DONE.md 박음** (요약 + 사실 박제 + 학습 키워드 후보)
- 5단계 보고 X (대규모 등급만)
- HTML X (대규모 등급만)
- trust-boundary 위험 깃발 = reviewer Tier 2-A 자동 호출 의무

---

## 작업 로그

- 2026-05-23: Phase 정의 박힘 (M4.1 재구성 옵션 A' GO 시점). 사용자 보정 1 (신규 패킷 박지 X) 흡수.
