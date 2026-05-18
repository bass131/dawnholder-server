# Phase 09: Trust-boundary fail-closed

> **상태**: pending
> **마일스톤**: M2.5 Hardening
> **예상 소요**: 2~3시간
> **담당 에이전트**: netcode

---

## 🎯 목표

서버가 클라에서 받는 모든 프레임을 **fail-closed**로 검증한다. M2 마감 직후 γ 감사(`00_Document/reviews/2026-05-18-pre-m3-{claude,codex}-review.md`)에서 발견된 trust-boundary 위반 3건을 한 Phase로 봉합:

1. **Packet length 헤더 상하한 검증** → 위반 시 `Disconnect()`
2. **Decode 예외 발생** → `Disconnect()` (현재는 silent half-open)
3. **Rate-limit 초과 intent** → drop (현재는 *기록만* — 헌법 #3 명시 위반)

M3 broadcast 첫 표적이 될 영역을 사전 차단. "주석으로 박힌 약속은 가짜다" 두 번째 증명을 코드로 갈아엎는 작업.

---

## ⏪ 사전 조건

- [ ] Phase 08 완료 (M2 First Connection 마감, main `aca7795`)
- [ ] `Shared.GameData.Constants`에 신규 상수 추가 가능 (DLL 재빌드 → `Plugins/Shared/Shared.dll` 자동 복사 정합, ADR-010)
- [ ] 본 작업용 feature 브랜치 분기 (예: `feature/youngho-m25-phase09-trustbound`)

---

## 📝 작업 내용

### 1. 길이 상수 박기

- [ ] `98_Shared/GameData/Constants.cs`에 `public const int MaxPacketSize = 4096;` 추가.
- [ ] 주석으로 의미 박기 — "현재 가장 큰 packet (S_Snapshot ~24B) 기준 175배 여유. M3 broadcast 도입 후에도 단일 frame 4KB 한도 정상. 추후 packet-id별 min/max 테이블 도입 자리잡이 (M5+ 후보)."

### 2. PacketSession.OnRecv length 검증

- [ ] `02_Server/Network/Session.cs PacketSession.OnRecv` 안에 `if (buffer.Count < dataSize) break;` 직후 *추가*:
  ```csharp
  if (dataSize < HeaderSize + sizeof(ushort) || dataSize > Constants.MaxPacketSize)
  {
      Console.WriteLine($"[Trust] invalid frame size {dataSize} (min={HeaderSize + sizeof(ushort)}, max={Constants.MaxPacketSize}) — disconnect");
      Disconnect();
      return processLen;
  }
  ```
- [ ] **중요**: `buffer.Count < HeaderSize` (헤더 partial) + `buffer.Count < dataSize` (정상 분할 패킷)는 *기존 `break` 유지* (Codex β 검토 지적). 이 두 케이스는 정상 흐름이며 disconnect 아니라 *다음 recv까지 대기*.

### 3. Session.OnRecvCompleted decode 예외 → Disconnect

- [ ] `02_Server/Network/Session.cs` L259~262 catch 블록:
  ```csharp
  catch (Exception ex)
  {
      Console.WriteLine($"OnRecvCompleted Failed : {ex}");
      Disconnect(); // ← 추가. 현재는 로그만 → half-open 세션 잔존.
  }
  ```
- [ ] `RegisterRecv()`는 호출하지 않음 — Disconnect가 그 길을 차단.

### 4. GameSession.HandleMoveIntent rate-limit drop

- [ ] `02_Server/GameServer/Network/GameSession.cs` L138~145:
  ```csharp
  _intentCountInWindow++;
  if (_intentCountInWindow > IntentRateLimitPerSecond)
  {
      if (!_rateLimitLoggedThisWindow)
      {
          Console.WriteLine($"[Cheat] Player {_entityId}: intent rate exceeded {IntentRateLimitPerSecond}/s — dropping");
          _rateLimitLoggedThisWindow = true;
      }
      return; // ← 추가. 임계 초과 intent는 tick queue 진입 X.
  }
  ```
- [ ] 카운트 증가는 *임계 이상이어도 계속* — 임계 이하로 떨어진 후 폭주 재개 방지.
- [ ] L29~30 주석 갱신: "차단은 여전히 안 함 (Phase 05+에서 정책 결정). 기록만." → "Phase 09(M2.5): 임계 초과 drop + 윈도우당 1회 로그 (헌법 #3 fail-closed)."

### 5. 테스트 — PacketSession length 검증

- [ ] 신설 `02_Server/GameServer.Tests/Network/PacketSessionLengthValidationTests.cs`:
  - **A.** `dataSize=0` → Disconnect 발생, processLen=0
  - **B.** `dataSize=1` → Disconnect, processLen=0
  - **C.** `dataSize=3` (헤더+id 미달) → Disconnect
  - **D.** `dataSize=4` (헤더+id 정확) → Disconnect 안 함 (정상 minimum), packet ID dispatch 호출
  - **E.** `dataSize=MaxPacketSize+1` → Disconnect
  - **F.** `dataSize=10` + buffer.Count=5 (정상 분할 partial) → Disconnect X, break + 다음 recv 대기 (invariant 유지)
- [ ] Test helper: `FakeSession`이 `OnRecvPacket` 호출 추적 + `Disconnect()` 호출 추적.

### 6. 테스트 — decode 예외 disconnect

- [ ] 동일 테스트 파일에 추가:
  - **G.** truncated `C_MoveIntent` (size=4, id만, payload 0byte) → `OnRecv` 안에서 dispatch까지 가지만 `C_MoveIntent.Read`가 `ArgumentOutOfRangeException` → `Session.OnRecvCompleted` catch → Disconnect.
  - 실제 catch는 `OnRecvCompleted`에 있으므로 통합 시나리오로 검증 (또는 `PacketSession.OnRecv`가 raise하는 예외 path 직접 시뮬).

### 7. 테스트 — rate-limit drop

- [ ] 신설 `02_Server/GameServer.Tests/Network/GameSessionRateLimitTests.cs`:
  - **H.** 1초 윈도우 안 500번 `C_MoveIntent` → 500번 모두 entity Pending 적용.
  - **I.** 501번째 → drop. `entity.PendingInputX`는 500번째 값 유지.
  - **J.** 1초 경과 후 윈도우 재시작 → 다시 통과.
- [ ] Stopwatch는 `Stopwatch.StartNew()` 대신 의존성 주입 가능한 인터페이스로 추출? 또는 `Thread.Sleep(1100)` 단순 처리 (테스트 1개 ~1.1초 비용 OK).

### 8. 후속 안건 (본 Phase scope X)

- [ ] `04_ClientNet/ClientSession.cs` 동형 length-check — *별도 ad-hoc로 분리* (Codex β 검토 지적: scope 흐림 차단). 우선순위 낮음 (trusted server 수신 비대칭).
- [ ] `-DONE.md`의 후속 안건 섹션에 한 줄 메모.

---

## ✅ 완료 조건

- [ ] `PacketSessionLengthValidationTests` 6 케이스 (A~F) xUnit 통과
- [ ] **partial packet wait/break invariant 보존**: 케이스 F (정상 분할 패킷)가 Disconnect되지 않음 — 정상 트래픽 회귀 X (Codex β 추가 발견)
- [ ] decode 예외 시 Disconnect 발생 (케이스 G)
- [ ] rate-limit 초과 시 `entity.PendingInputX` 변경 X (케이스 H~J)
- [ ] `dotnet test` 전체 통과 (회귀 0, M1+M2 모든 통합 테스트 포함)
- [ ] 콘솔 로그에 `[Trust]` 또는 `[Cheat]` prefix 명확
- [ ] headless-bot `M2BasicMovement` 시나리오 100회 회귀 통과 (Phase 08 안전망 — 정상 트래픽 영향 X 확증)
- [ ] `/work:review` 위반 0건
- [ ] `09-trust-boundary-fail-closed-DONE.md` 작성 + Post-flight 게이트 통과

---

## 🧪 테스트

**자동 테스트:**
- `PacketSessionLengthValidationTests.cs` — 6 케이스 (A~F) + decode 예외 1 (G)
- `GameSessionRateLimitTests.cs` — 3 케이스 (H~J)
- 기존 `PacketRoundTripTests` 회귀 통과

**수동 테스트:**
- malformed frame을 `nc localhost 7777` 또는 PowerShell `System.Net.Sockets.TcpClient`로 던지기:
  - `0x00 0x00` (size=0) → 서버 콘솔 `[Trust] invalid frame size 0 — disconnect` 출력 + 클라 끊김
  - 정상 `C_Ping` (size=10) 다음 즉시 `0x00 0x40` (size=16384, MaxPacketSize 초과) → 첫 Ping 처리 후 disconnect
- headless-bot 정상 시나리오 → 콘솔 `[Trust]` 로그 0건 (정상 트래픽 무영향 확증)

---

## 📚 학습 포인트

이번 Phase에서 새로 만나거나 깊어지는 개념. AI 보고에서 다뤄줘야 할 키워드.

- **Fail-closed vs fail-open** — 헌법 §3 "관대하게 처리 금지"의 코드 실현. 모호한 입력은 "괜찮을 거야"가 아니라 "끊자". 보안의 기본 자세 (방화벽 default-deny와 같은 정신).
- **약속과 코드의 거리** — 학습 일지 ★★★ "주석으로 박힌 약속은 가짜다" 두 번째 증명 직접 시연 재료. Phase 04 주석 "차단은 여전히 안 함 (Phase 05+에서 정책 결정). 기록만." → Phase 09까지 미뤄진 패턴이 *실제로 봉합되는 순간*.
- **Packet length validation의 비대칭** — min은 헤더+id 필수, max는 자원 보호. 둘 다 없으면 부분 무한 루프 또는 메모리 폭주.
- **Partial packet의 정상 흐름** — 정상 분할 패킷은 disconnect가 아닌 break + 다음 recv 대기. invalid frame과 partial frame을 *구분하는 코드 자리*가 어디인지가 trust-boundary 설계의 핵심.
- **Rate-limit 카운트의 함정** — 임계 초과 후에도 카운트 증가해야 함. 안 그러면 임계 이하 진입 후 폭주 재개 (oscillation attack).
- **Decode 예외의 silent half-open** — try-catch가 좋게 보이지만 "닫지도 다시 듣지도 않는" 패턴은 자원 누수. 예외는 *경로 결정의 신호*로 사용.

---

## ⚠️ 함정 / 주의사항

이 Phase에서 흔히 빠지는 함정.

- **`dataSize == HeaderSize` (=2)인 경우** packet ID도 못 읽음. 진짜 minimum은 `HeaderSize + sizeof(ushort) = 4`. 본 Phase에서 4로 박음.
- **rate-limit drop 후에도 `_intentCountInWindow` 증가** 필수 — 안 그러면 임계 이하 진입 후 폭주 재개.
- **`OnRecv` length 검증 실패 시 반환값** — 0 vs `processLen` 선택. 본 Phase는 `processLen`(이미 처리한 만큼) 반환 — buffer cursor 정합.
- **`Disconnect()`는 한 번만 동작** (`Interlocked.Exchange` 박혀 있음, `Session.cs:128`) — 이미 닫힌 세션 재호출 안전.
- **catch 블록에 Disconnect 추가 시 무한 재귀 X 확인** — `Disconnect()` 안에서 `OnDisconnected` 호출 → GameSession 핸들러 → map job enqueue. 예외 흐름이지만 lock 안 잡혀있는지 확인.
- **테스트 케이스 D (`dataSize=4`)는 정상이어야 함** — minimum 통과. 단, payload 0byte인 packet이 PDL에 없다면 dispatch 후 decode 단계에서 길이 부족 예외 → catch 경로. 본 Phase에선 disconnect 여부보다 *PacketSession length 검증이 통과시킴*에 초점.
- **`MaxPacketSize = 4096` 선택 근거**: 현재 가장 큰 packet (S_Snapshot ~24B) 기준 충분한 여유. 추후 broadcast batch frame 도입 시 재검토.

---

## ➡️ 다음 Phase

- Phase 10 (Session lifecycle race 제거) — disconnect 경로가 본 Phase에서 안정된 후라야 race 테스트 신뢰성 확보.

---

## 작업 로그

> Phase 진행하면서 발견된 이슈, 결정, 메모를 여기 누적.

- 2026-05-18: Phase 분해 완료 (γ 방식 2회 적용 — Claude α 드래프트 + Codex β 검토 + 3건 반영). 시작 대기.
