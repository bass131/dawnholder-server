---
owner: youngho
milestone: M4.1
phase: 03
title: ClientNet Trust Boundary Symmetry (P0-4 — 클라 framing 검증 대칭)
status: pending
grade: 보통
risk: trust-boundary
estimated: 1~2h
domain: client+shared
---

# Phase 03: ClientNet Trust Boundary Symmetry (P0-4)

> **상태**: pending
> **마일스톤**: M4.1
> **등급**: 보통 (2 도메인 client+shared / ~50줄 / trust-boundary 위험 깃발)
> **담당**: client SubAgent (Sonnet) + shared SubAgent (Sonnet, helper 박을 가닥) — reviewer Tier 2-A 자동 호출

---

## 🎯 목표

**P0-4 (ClientNet frame length 검증 부재) 봉합**. 서버 측 `Listener` framing 검증과 클라 측 `ClientSession` framing 검증 *대칭* 박음. `dataSize == 0 / < headerSize / > maxPacketSize` silent drop 또는 disconnect.

본 Phase가 끝나면 = (a) `04_ClientNet/ClientSession.cs` 또는 `RecvBuffer.cs` framing 검증 분기 박힘, (b) `98_Shared/FrameValidator.cs` (또는 helper 재활용) 신설, (c) 단위 테스트 4건+ 통과, (d) 헤드리스 봇 fuzz 시나리오 통과, (e) `04_ClientNet/CLAUDE.md:38` "M2.5 Phase 09 fix 대상" stale 정정.

---

## ⏪ 사전 조건

- [x] Phase 02 (Session State Machine) 마감 — 서버 측 상태 머신 강제 후 클라 측 framing 대칭 박음 정합
- [x] M2.5 Phase 09 ✅ 마감 = 서버 측 `Listener` framing 검증 박힘 (본 Phase는 클라 측 *동형 패턴* 박음)
- [x] `04_ClientNet/CLAUDE.md:38` 박힌 "본 영역도 같은 helper로 동반 정정 권장" 인지

---

## 📝 작업 내용

### 1단계: 진단 + 옛 패턴 매핑

- [ ] `02_Server/Network/Listener.cs` 또는 `RecvBuffer.cs` 서버 측 framing 검증 helper 식별 (`dataSize` 검증 패턴 박힌 위치)
- [ ] `04_ClientNet/ClientSession.cs:50~55` + `RecvBuffer.cs:53~70` 현재 클라 측 framing 처리 = `dataSize` 읽음 → `buffer.Count < dataSize`만 검사 (M3.6 Phase 04 학습 정합 = 옛 결함 패턴)
- [ ] **클라 측 결함 확정**: `dataSize == 0` (parse 무한 루프) / `dataSize < 4` (header size 미만, parse 깨짐) / `dataSize > maxPacketSize` (메모리 폭발) 검증 *0건*

### 2단계: 공유 helper 박을 가닥 결정

- [ ] **옵션 A**: `98_Shared/Network/FrameValidator.cs` 신설 — `bool TryValidateFrameHeader(ushort dataSize, out string? reason)` 순수 함수 박음 (헌법 #4 정합, 클라/서버 양쪽 호출).
- [ ] **옵션 B**: 서버 측 helper를 `98_Shared`로 이동 (옛 helper가 internal이면 public 승격 의무).
- [ ] **결정 가닥** = 옵션 A 권장 (서버 측 helper가 socket layer 강결합이면 분리 비용 ↑). 단 서버 측 검증 분기를 helper 호출로 *교체* 의무 (헌법 #4 "복사-붙여넣기 금지" 정합).

### 3단계: 클라 측 framing 검증 분기 박음

- [ ] `04_ClientNet/ClientSession.cs:50~55` (또는 `RecvBuffer.cs`) — `dataSize` 읽은 직후 `FrameValidator.TryValidateFrameHeader` 호출 분기 박음.
- [ ] 검증 실패 시 처리 = **disconnect 권장** (클라 입장에서 서버가 깨진 frame 보내는 = 신뢰할 수 없는 연결, 끊는 게 정합. silent drop은 *서버 측 정신* — 클라 측은 그 반대 = 서버 신뢰 X면 disconnect).
- [ ] `dataSize == 0` 케이스 = 진짜 빈 패킷 vs 깨진 frame 구분 어려움 = disconnect (안전 default).

### 4단계: 서버 측 helper 호출 정합 (헌법 #4)

- [ ] `02_Server/Network/Listener.cs` (또는 RecvBuffer) 옛 검증 분기를 `FrameValidator.TryValidateFrameHeader` 호출로 교체.
- [ ] 회귀 0 확인 (기존 서버 측 검증 동작 그대로 + 새 helper 정합).

### 5단계: 단위 테스트 박음

- [ ] `98_Shared.Tests/Network/FrameValidatorTests.cs` 신설 (4건+):
  1. `ValidateFrame_ZeroSize_Reject` — `dataSize == 0` → reject
  2. `ValidateFrame_TooSmall_Reject` — `dataSize < 4` (header size 미만) → reject
  3. `ValidateFrame_TooLarge_Reject` — `dataSize > Constants.MaxPacketSize` → reject
  4. `ValidateFrame_ValidSize_Accept` — `4 <= dataSize <= MaxPacketSize` → accept
- [ ] 클라 측 단위 테스트 — `04_ClientNet/Tests/` 또는 `02_Server/GameServer.Tests/Network/` 활용:
  - `ClientSession_RecvCorruptedFrame_Disconnect` — fuzz frame 수신 시 disconnect 박힘 확인
- [ ] dotnet test green (회귀 0 + 5건+ 추가)

### 6단계: 헤드리스 봇 fuzz 시나리오

- [ ] `99_Tools/headless-bot/` 또는 별 fuzz tool — *악성 서버* 시뮬레이션 박음 (또는 packet injection 시나리오):
  - `dataSize = 0` 송신 시 클라 disconnect 확인
  - `dataSize = 1/2/3` 송신 시 클라 disconnect
  - `dataSize = 8192/65535` 송신 시 클라 disconnect 또는 silent drop (max 임계 정합)
  - `dataSize = 정상` 정상 처리

### 7단계: 문서 정합

- [ ] `04_ClientNet/CLAUDE.md:38` 박힌 "M2.5 Phase 09 fix 대상과 *동형 패턴*. 서버 fix 시 본 영역도 같은 helper로 동반 정정 권장 (M2.5 Phase 09에서 묶음)" → "**M4.1 Phase 03 봉합 완료 (2026-05-23, `FrameValidator` helper 박힘)**" 정정.
- [ ] `98_Shared/CLAUDE.md` 필요 시 `Network/FrameValidator.cs` 추가 줄.

---

## ✅ 완료 조건

- [ ] `98_Shared/Network/FrameValidator.cs` 신설 (또는 옵션 B 박힘 = 같은 정신)
- [ ] `04_ClientNet/ClientSession.cs` 또는 `RecvBuffer.cs` `FrameValidator.TryValidateFrameHeader` 호출 분기 박힘
- [ ] `02_Server/Network/Listener.cs` 같은 helper 호출로 교체 (헌법 #4 정합)
- [ ] 검증 실패 시 클라 disconnect 박힘 (silent drop X)
- [ ] `dotnet test` 신규 5건+ 통과 + 회귀 0
- [ ] 헤드리스 봇 fuzz 시나리오 통과
- [ ] `04_ClientNet/CLAUDE.md:38` stale 정정 박힘
- [ ] 본 Phase 보통 등급 = -DONE.md 없음, work-pin + commit message 충분 (단 trust-boundary 위험 깃발 = reviewer SubAgent Tier 2-A 자동 호출 의무)

---

## 🧪 테스트

**자동**:
- `FrameValidatorTests` 4건 + 클라 측 fuzz 1건 + 기존 회귀 0

**수동**:
- 헤드리스 봇 fuzz 시나리오 (악성 서버 시뮬)
- Unity 클라 정상 환경에서 일반 packet flow 정상 (회귀 확인)

---

## 📚 학습 포인트

- **헌법 #3 "Trust Boundary" 양쪽 적용** — 옛 = 서버만 검증 (untrusted client input). 새 = 클라/서버 둘 다 (악성 서버 시뮬 대응). 분산 시스템 표준 = "양쪽 다 untrusted 가정". 한국 게임 회사 백엔드 어필 키워드.
- **헌법 #4 정합 helper 패턴** — 옛 = 서버 측 inline 검증 + 클라 측 inline 검증 = 복사-붙여넣기. 새 = `98_Shared/FrameValidator.cs` 순수 함수 + 양쪽 호출 = 헌법 #4 정신 박음. Phase 05 Formulas.cs 패턴과 같은 정신.
- **silent drop vs disconnect 비대칭** — 서버 측 = silent drop (클라 측 reconnect storm 회피). 클라 측 = disconnect (서버 신뢰 X면 끊음). *방향 비대칭이 정합*.
- **stale 문서 정정 정합 (false-promise 변종 발본)** — `04_ClientNet/CLAUDE.md:38` 박힌 "M2.5 Phase 09 fix 대상" = 미봉합 약속. 본 Phase에서 봉합 완료 표기 = false-promise 23번째 변종 정합 cadence.

---

## ⚠️ 함정 / 주의사항

- **disconnect 처리 누락 함정** — 클라 측 framing 검증 실패 시 disconnect 박지 않고 silent drop 박으면 *parse loop 무한* 가능성 (특히 `dataSize == 0`). 4번째 단위 테스트가 검증.
- **헬퍼 위치 비대칭 함정** — 서버 측은 helper 호출, 클라 측은 inline 박으면 옛 *비대칭* 복귀. 둘 다 helper 호출 의무 (헌법 #4).
- **Shared.dll commit 누락 함정 (트라우마)** — `98_Shared/Network/FrameValidator.cs` 신설 = Shared.dll 재빌드 + Unity 측 복사 + commit 의무. CHANGELOG 2026-05-17 학습 정합.
- **fuzz 시나리오 환경 종속 함정** — 헤드리스 봇 fuzz가 본 머신 환경 한정 작동 시 다른 머신 회귀 의문. CI 환경 박지 X 단계라 *수동 검증* 1회 + commit message 박힘 의무.

---

## ➡️ 다음 Phase

- **Phase 04 (Build Artifact Hygiene)** — P0-5 봉합. Phase 03 framing 봉합 후 build 산출물 위생 박음.

---

## 📋 박제 (완료 후)

- 보통 등급 = -DONE.md 없음, work-pin + commit message 충분
- 단, 함정 발견 (예: 옛 *비대칭* 패턴 다른 영역 잠복) 시 *복잡 자동 상향* → -DONE.md 박음
- trust-boundary 위험 깃발 = reviewer SubAgent Tier 2-A 자동 호출 의무

---

## 작업 로그

- 2026-05-23: Phase 정의 박힘 (M4.1 재구성 옵션 A' GO 시점).
