---
summary: Phase 06 — input replay 기반 reconcile + Unity Test Framework 인프라 첫 박제. Phase 05의 텔레포트 snap을 "서버 권위 좌표 + 미-ack 입력 재시뮬" 부드러운 정정으로 교체. PDL int→uint 정합, framerate-bound 송신 차단(50ms throttle).
phase: 06-input-replay-reconcile
work-id: phase06-input-replay-reconcile
status: done
completed_at: 2026-05-16
commit: cee8775
---

# Phase 06 — Input replay 기반 reconcile 완료 박제

**소요 시간**: 약 6시간 (Codex 상의 + asmdef 인프라 박제 포함)

## TL;DR

Phase 05의 *snap*(텔레포트 점프)을 **input replay reconcile**로 교체. 서버가 `lastAckedClientTick`을 알려주면, 클라는 서버 권위 좌표에서 출발해 *아직 ack 못 받은 입력만* 재시뮬레이션 → 자연 위치 정합. PDL 타입 `int→uint` 정합(도메인 의미), 240Hz framerate-bound 송신을 50ms throttle(서버 20 TPS와 1:1 align)로 차단. Unity Test Framework EditMode 인프라(`Dawnholder.Client.asmdef` + Tests asmdef) 첫 박제 — 합류자 시드.

## 5단계 보고

- **무엇을 만들었나** — `PlayerPredictor.OnSnapshot(serverX, serverY, uint ackedClientTick)` replay 알고리즘 + `InputHistory` 자료구조(sealed, UnityEngine X, 좁은 API 5개) + Unity Test Framework 인프라 첫 박제 + 송신 50ms throttle + PDL `int→uint` 정합 + PacketGenerator `uint` 지원.
- **왜 필요한가** — Phase 05의 텔레포트 snap이 200ms latency 환경에서 *시각적 점프*로 보임. 헌법 #1(Server Authority) 유지하면서 *체감 부드럽게* 정정하려면 "서버 위치 + 클라 미-ack 입력 합성" 패턴 필요. 또 240Hz 머신이 240 packet/s 송신하던 framerate-bound 문제도 동반 해결.
- **어떻게 만들었나** — 6 step 분해. (1) PDL int→uint, (2) Phase 04 자리잡이로 서버 wire 자연 닫힘, (3) InputHistory + NUnit 12 테스트 + asmdef 인프라, (4) 송신 throttle + InputHistory.Push wire, (5) OnSnapshot 알고리즘(mispredict 시 서버 위치 + 미-ack replay), (6) 수동 검증. Codex CLI 상의로 InputHistory 위치 결정((B) `03_Client/Prediction/` + UnityEngine 의존 X).
- **테스트 결과** — `dotnet build Dawnholder.slnx` 0 error / `dotnet test` 62/62 통과 / Unity Test Runner NUnit 12/12 / Unity_RunCommand Smoke 5+3 (InputHistory + reconcile 알고리즘 직접) / Unity Editor 실측 `[Reconcile] dx=1.05~1.23 count=19` + 시각적 점프 0회 / 서버 로그 0 error, S_Snapshot 24 bytes (= 20 + uint 4 정확).
- **다음 스텝** — Phase 07 점프·중력 (Y축 비교 + 서버 권위 + prediction). Phase 06에서 박힌 InputHistory + reconcile 알고리즘은 Y축에 자연 확장 (sbyte 입력 → 점프 trigger 변환). Manager 잠복 버그는 별도 미니 정정 Phase 후보.

## AC 검증 결과

**.NET 솔루션**:
```bash
$ dotnet build Dawnholder.slnx
  성공 빌드(3.5초). 경고 0개, 오류 0개.
$ dotnet test Dawnholder.slnx --nologo
  통과! - 실패: 0, 통과: 62, 건너뜀: 0, 전체: 62, 기간: 3 s
```

**Unity Test Framework (EditMode)**:
- `Window → General → Test Runner` → EditMode → `Dawnholder.Client.Tests.EditMode`
- `Run All` → **12/12 통과** (NUnit, 본인 수동 확인)

**Unity_RunCommand Smoke**:
- InputHistory 5/5: Push N → Count=N / EvictUpTo / ReplayFrom / uint.MaxValue 경계 / Clear
- Reconcile 3/3: mispredict 없음 정상 / mispredict + replay 수학 정확(-3 + 3×0.25 = -2.25) / EvictUpTo 정리 검증

**Unity Editor 실측 (`SimulatedLatencyMs = 200`)**:
```
[Reconcile] dx=1.23 at serverTick=690 ack=563 (count=18)
[Reconcile] dx=-1.05 at serverTick=700 ack=573 (count=19)
```
- `ack=` 정확 (uint wire 통과)
- `dx` SnapThreshold(1.0) 살짝 초과만 → 부드러운 정정
- 시각적 점프 0회 (본인 체감 보고)

**서버 로그** (F5 디버그):
- `S_Snapshot OnSend 24 bytes` (= entityId 4 + x 4 + y 4 + serverTick 4 + lastAckedClientTick 4 + header 4) — PDL int→uint 정확 반영
- Tick 메트릭 avg=0.03~0.08ms, max=0.55ms 미만 (50ms budget의 1% 미만, 헌법 #5 OK)
- Exception / 디시리얼라이즈 에러 / 비정상 disconnect 0개

## 결정 흐름 (학습 일지 쓸 때 참고용)

- **PDL 타입 (`int` 유지 vs `uint` 변경)** → uint 채택 → 도메인 의미(비음수 카운터) 우선. BCL `int` 관습보다 *도메인 정직성*. 음수 사고 컴파일러 차단 (실제로 빌드가 `PacketRoundTripTests`의 음수 `InlineData` 거절 → 옛 리뷰 🟡 자연 해결).
- **InputHistory 위치 ((A) `04_ClientNet` vs (B) `03_Client/Prediction/`)** → Codex 상의 후 (B) 채택 → 04_ClientNet은 socket 인프라 좁게 유지(Y2 분리 정신), InputHistory는 prediction 도메인. 미래 재사용 신호 오면 *별도 SharedPrediction dll 추출*이지 04_ClientNet 이주 아님.
- **Manager 잠복 버그 (A) 우회 vs (B) 정정** → (A) 채택 → 본인 옛 결정(CONTEXT.md "보류 중") 일관. untracked 폴더 삭제 + Generator `--no-manager` 권장.
- **send pattern (X) 매 50ms 송신 vs (Y) 변경 시만 + keepalive** → (X) 채택 → 서버 GameMap.Tick(20 TPS)과 1:1 align. M2 단계엔 단순함 우선.
- **OnSnapshot threshold 안 처리 (즉시 update vs 무시)** → 무시 채택 → Phase 05 의도(작은 차이는 prediction 신뢰) 유지. *항상* EvictUpTo로 정리는 함.

## 막혔던 지점

- **Manager 잠복 버그 표면화 (Step 1 빌드)** — `PacketGenerator` 재실행이 `02_Server/.../Generated/ServerPacketManager.cs` 새로 생성 → csproj 와일드카드가 자동 픽업 → 24 컴파일 에러 (`ServerCore`/`PacketSession`/`IPacket` 못 찾음). 원인 = `PacketFormat.managerFormat` 템플릿의 `using Shared.Protocol;` 누락 (CONTEXT.md "보류 중" 박혀있던 잠복). 해결 = (A) untracked 폴더 2개 삭제 + `--no-manager` 권장.
- **`Unity.InputSystem` reference 누락 cascade** — `Dawnholder.Client.asmdef` 신설 직후 `LocalPlayerController.cs:6 using UnityEngine.InputSystem` 못 찾음 (CS0234 + CS0246 ×2). 원인 = Default Assembly에서 자동 picked up 되던 게 *명시 asmdef*에선 `references` 박아야 함. 해결 = `references: ["Unity.InputSystem"]` 추가.
- **Git Bash `\` escape (VS Code task)** — `${workspaceFolder}/Dawnholder.slnx`가 bash 안 들어가면서 `\D` `\C` escape 먹어 `C:DevClaudeDev/...` 망가짐 → MSB1009 프로젝트 파일 없음. 해결 = 상대 경로 + `cwd: ${workspaceFolder}` 옵션.
- **Codex Windows sandbox 1312** — `codex exec` 첫 호출에서 `CreateProcessAsUserW failed: 1312` (ERROR_NO_SUCH_LOGON_SESSION). Codex가 ADR 본문 읽기 실패. 해결 = (다) 그냥 진행 (답 자체는 우리 패키지 컨텍스트만으로 견고). 영구 정정은 별도 ad-hoc Phase 후보.

## 학습 일지 후보 키워드

- `/journal:concept Input replay reconcile (Phase 06 핵심)` — Source/Quake 표준 패턴 직접 구현. 면접 결정타.
- `/journal:concept 도메인 의미 우선 (uint vs int)` — BCL 관습보다 *비음수 카운터*의 진실. 학습 노트 베이스 박혀있음 (`learning-journal/youngho/M2-first-connection/concepts/int-vs-uint-for-tick-counters.md`).
- `/journal:concept Phase 자리잡이 패턴 효과 실증` — Phase 04에서 미리 박은 `lastAckedClientTick` PDL 필드 + `PlayerEntity.LastClientTick` uint 덕에 Phase 06 Step 2가 자연 닫힘. 미래 Phase 설계에 가치.
- `/journal:concept Codex CLI 협업 패턴` — 시니어 멘토 상의 흐름의 첫 e2e (InputHistory 위치 결정). 답 + Codex 5규칙 + 미래 이주 신호 5개.
- `/journal:concept asmdef 설계 + package reference` — Default Assembly → 명시 asmdef 전환 시 *package 어셈블리*(Unity.InputSystem 등) 명시 필요. plugin dll은 자동 OK.
- `/journal:bug Git Bash backslash escape` — VS Code task + Git Bash 통합 함정. ADR-020 함정 클래스와 같은 뿌리.
- `/journal:bug Manager 잠복 버그 표면화` — 잠복 5+개월 만에 표면화 + 옛 결정(별도 미니 Phase) 일관 유지.
- `/journal:concept framerate-bound 송신 → tick-bound` — 환경 독립성. 240Hz와 60Hz 같은 cadence. 헌법 #1의 클라 측 짝꿍.
