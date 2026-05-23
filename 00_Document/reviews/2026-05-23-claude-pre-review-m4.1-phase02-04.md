# Pre-Review for Codex β — 2026-05-23 — M4.1 Phase 02·03·04 풀세트

> **본 파일 정신**: Claude가 박은 점검 자료 + Codex β 호출 prompt. 본인이 별 세션 터미널에서 Codex CLI 직접 호출 의무 (분담 정신 = memory `unity-visual-work-user-owned` + `external-tool-call-user-direct-default` 정합, 2026-05-23 봉합). `/cross-review` 슬래시 Step 3-A/3-B 정합.
>
> **호출 결과 박힌 후 처리 가닥**: Codex 산출물을 `00_Document/reviews/2026-05-23-cross-review-m4.1-phase02-04-codex.md`에 본인이 저장 → Claude γ 비교 산출물 `00_Document/reviews/2026-05-23-cross-review-m4.1-phase02-04.md` 박음.

---

## 1. 점검 범위

### 1.1. 본 세션 누적 commit (5건, 본 cross-review 우선 영역)

| Commit | Phase | 영역 | 핵심 변경 |
|---|---|---|---|
| `3f1d45c` | Phase 02 server | server | Session State Machine Hardening (P0-1+P0-2) — 단위 테스트 6건 + 봇 시나리오 4개 정합 |
| `0586dfe` | Phase 02 client | client | event-based C_CharacterSelect 송신 (race elimination) |
| `fc15e77` | Phase 03 server | server | `02_Server/Network/FrameValidator.cs` 신설 + `Session.cs` inline 교체 + 테스트 8건 |
| `21d2cfb` | Phase 03 client | client | `04_ClientNet/FrameValidator.cs` 신설 + `ClientSession.cs` 분기 박음 + `04_ClientNet/CLAUDE.md:38` stale 정정 |
| `2ac1a8f` | Phase 04 | harness/infra | `<Deterministic>true</Deterministic>` + `<PathMap>` 양쪽 csproj + ClientNet `.gitignore` 화이트리스트 |

### 1.2. main 대비 변경 통계

`git diff --stat main...HEAD` = 48 파일 / 1993 insertions / 189 deletions. 본 cross-review 점검 영역 = 위 5 commit 우선, 그 외 harness/plan 파일 (`.claude/CHANGELOG.md` / `01_Phases/youngho/M4.1-*/`) = 외곽.

### 1.3. 등급

- **M4.1 풀세트**: 대규모 (마일스톤 단위, 6 Phase, 캡스톤 1 마감 6/4)
- **Phase 02**: 복잡 (server + client 2 도메인, trust-boundary 깃발)
- **Phase 03**: 보통 (client+server 2 도메인, trust-boundary 깃발)
- **Phase 04**: 보통 (1 도메인 harness, 가역적)

---

## 2. α (Claude reviewer + 메인 통합) 결과 요약

### 2.1. Phase 02 reviewer Tier 2-A 결과

- 헌법 5축 PASS (위반 0건)
- Critical / High / Medium 결함 0건
- Low 개선 제안 1건 = SessionStateMachineTests race window 시나리오 1건 추가 (별 시점 박을 가닥)
- 학습 자산 ★★ 박힘 = `event-based-race-elimination` + `server-state-machine-flag-vs-enum-trade-off` + `subagent-split-server-client-with-reviewer-integration`

### 2.2. Phase 03 reviewer Tier 2-A 결과

- 헌법 5축 PASS (위반 0건)
- Critical / High / Medium 결함 0건
- Low 개선 제안 = `04_ClientNet.Tests` 미박힘이라 클라 측 drift guard 부재 (server 측 cross-validation 의미 충족, 별 시점 박을 가닥)
- 학습 자산 ★★★ 박힘 = `option-b-variant-third-path-with-drift-guard` (공유 vs 분리 갈래의 third path)
- drift guard 2건 bonus 판정 박힘 (`MaxFrameSize_MatchesSharedConstants_DriftGuard` + `PacketSession_Constants_DelegateToFrameValidator`)
- **사용자 결정 박힘** = Phase 정의 옵션 A (`98_Shared/Network/FrameValidator.cs`) → 옵션 B 변형 (서버/클라 각자) 변경. 사유 = `02_Server/Network/Session.cs:23-25` 박힌 사유 주석 "ServerCore는 의도적으로 Shared 참조 X(재사용성)" 정신 보존

### 2.3. Phase 04 결과 (reviewer 미호출, 메인 직접)

- `<Deterministic>true</Deterministic>` + `<PathMap>$(MSBuildThisFileDirectory)=/_/</PathMap>` 박힘 양쪽 csproj
- `.gitignore` ClientNet 화이트리스트 추가 (Shared.dll 패턴 정합)
- 5회 연속 빌드 후 git status에 *추가* dirty 박힘 X (baseline = 5회 후 정확히 같음)
- **회귀 검증 미박힘** (Phase 정의 5단계 의무 박힘 = `Constants.cs` 한 줄 변경 → dirty → 되돌리기 → clean). 본 cross-review 우선 영역 박을 가닥

---

## 3. Codex β 점검 가닥 (본인 직접 호출 시 우선순위)

### 3.1. 🔴 최우선 점검 영역 (양쪽 잡힘 시 즉시 봉합)

**3.1.1. Phase 03 옵션 B 변형 third path 헌법 #4 동등 보호 정합**:
- 본 결정 = Phase 정의 옵션 A (`98_Shared/Network/FrameValidator.cs`) 권장 박혔는데 사용자 결정으로 옵션 B 변형 (서버/클라 각자 박음 + 같은 시그니처 + 같은 상수 + 동기화 약속 주석) 변경
- α 판정 = 헌법 #4 "복사-붙여넣기 금지" *동등 보호* 정합 (drift guard 2건 + cross-reference 주석 박힘)
- **점검 질문**: 옵션 B 변형이 진짜 헌법 #4 *동등 보호*인가? 
- 점검 위치:
  - `02_Server/Network/FrameValidator.cs` (신설, 50 lines)
  - `04_ClientNet/FrameValidator.cs` (신설, 73 lines)
  - `02_Server/GameServer.Tests/Network/FrameValidatorTests.cs:78-91` (drift guard 2건)
  - `02_Server/Network/Session.cs:21-22` (PacketSession 인용)
  - `00_Document/ADR/ADR-012-y2-socket-split.md` (Y2 분리 정신 정합)

**3.1.2. Phase 04 deterministic build 정합**:
- `<Deterministic>true</Deterministic>` + `<PathMap>$(MSBuildThisFileDirectory)=/_/</PathMap>` 박음
- **점검 질문**: 
  - PathMap 형식 `$(MSBuildThisFileDirectory)=/_/` = MS 가이드 정합? 다른 환경 (CI/CD, 정유현 머신, 정우 합류 시 머신) 같은 hash 박힐지?
  - `<EmbedAllSources>true</EmbedAllSources>` + `<DebugType>embedded</DebugType>` 두 옵션과 deterministic 결합 결함 잠복?
  - 5회 연속 git status clean 검증 = *본 머신* 한정. CI 환경 또는 다른 머신에서 *진짜 같은 hash* 박히는지 별 점검 의무 (M5+ 영역이지만 사전 점검 의미 ↑)
- 점검 위치:
  - `98_Shared/Shared.csproj:13-23`
  - `04_ClientNet/Dawnholder.Client.Net.csproj:20-26`

**3.1.3. Phase 02 event-based race elimination 정합**:
- 옛 폴링/timing-based race 박혀있던 client `CharacterSelectController` 영역 → event-based 송신
- **점검 질문**:
  - event-based 송신이 진짜 race window 차단? 또 다른 race 잠복 (예: 같은 event 두 번 fire, event handler 잔재)?
  - server `Session.cs` 상태 머신 flag (`_handshakeCompleted` + `_characterSelected`)이 4번째 flag 추가 시 Rule of Three 통과 = enum 응집 후보 (work-pin 박힘) — 본 cadence 검증
- 점검 위치:
  - `03_Client/Assets/Scripts/Scenes/CharacterSelectController.cs:46~`
  - `02_Server/GameServer/Network/GameSession.cs:32 변경분` (32 lines 박힘)
  - `02_Server/GameServer.Tests/Network/SessionStateMachineTests.cs` (276 lines 신설)

### 3.2. 🟡 보조 점검 영역 (β만 잡힘 시 본인 판단)

**3.2.1. false-promise 변종 잠복 검출**:
- M4.1 누적 false-promise 2건 발본 (Codex 슬래시 23번째 + `04_ClientNet/CLAUDE.md:38` stale)
- **점검 질문**: 본 5 commit 또는 main 대비 변경분에 *옛 약속 vs 실재 코드* 어긋남 잠복?
- 점검 자료:
  - `98_Shared/CLAUDE.md` (ProtocolVersion 박힌 라인) 
  - `02_Server/CLAUDE.md` (Layout 표 + Async/Logging 컨벤션)
  - `04_ClientNet/CLAUDE.md` (M4.1 Phase 03 봉합 박힌 가닥)

**3.2.2. ClientNet `.gitignore` 화이트리스트 추가 영향 점검**:
- 옛 Shared.dll 화이트리스트 패턴 정합 박음
- **점검 질문**: 옛 미commit 사고 (정유현 pull 사고, 2026-05-17) 학습 정합 확인 + 다른 머신 pull 시 outdated 회피 정합

**3.2.3. SubAgent 자율 commit 패턴 (별 가닥)**:
- server SubAgent (Phase 03)가 *prompt에 "commit 박지 마세요" 명시 X* 상태에서 자율 `fc15e77` commit 박음
- **점검 질문**: 본 패턴 정합인지 / 결함인지 / 메인 통합 후 commit 정신 어긋남인지

### 3.3. 🟢 통과 확인 영역 (양쪽 통과 박는 게 정합)

- `dotnet build` green (경고 0, 오류 0) — 본 머신 5회 연속 통과
- `dotnet test` 194/194 통과 (server SubAgent 실측, SAC 차단 X)
- 헌법 5축 reviewer Tier 2-A GO (Phase 02 + Phase 03 둘 다)
- pre-commit hook 5회 통과 (cloud 라인 자동 unstage)

---

## 4. Codex β 호출 명령어 (본인 별 세션 터미널에서)

### 4.1. 권장 호출 (main 대비 풀세트)

```bash
cd C:\Dev\ClaudeDev
codex review --base main --title "M4.1 Phase 02-04 cross review"
```

본 명령어 = main HEAD (`2cd0fbc`) 대비 본 브랜치 (`feature/m4.1-combat-precision` HEAD `2ac1a8f`) 전체 변경분 점검. M4.1 풀세트 영역 정합.

### 4.2. 또 한 가닥 (uncommitted만, 본 시점 X — 모두 commit 박힘)

```bash
codex review --uncommitted
```

본 시점 워킹 디렉토리에 commit 박지 않은 영역 = 부산물 (Fonts / Packages / ProjectSettings, work-pin 박힘) 박혀있으니 *본 명령어 박을 가닥 X* (부산물만 점검됨).

### 4.3. sandbox 권한 옵션 (memory `codex-sandbox-permission-current-dir` 박힌 표)

`codex review`는 sandbox 옵션 *X* (memory 박힘). 본 머신 현재 디렉토리 자동 접근. 사고 X. 단 `codex exec` 호출 시점 = `--sandbox workspace-write` 명시 의무 (별 가닥).

### 4.4. 입력 자료 박을 가닥

본 pre-review MD를 prompt에 *직접 박지 X*. `codex review --base main` 호출 시 Codex가 자동으로 git diff 인지 박음. 단 본인이 *수동 prompt* 박을 가닥 = 위 3.1/3.2/3.3 점검 가닥 짧게 prompt에 박음 (예: "M4.1 Phase 02-04 cross review. 우선순위 = Phase 03 옵션 B 변형 third path 헌법 #4 동등 보호 정합 / Phase 04 deterministic build 정합 / Phase 02 event-based race 정합.").

---

## 5. 본인 응답 후 처리 가닥

### 5.1. (A) Codex 결과 첨부 시

본인이 Codex 출력 (raw 또는 요약) 가져옴 → Claude는:
1. `00_Document/reviews/2026-05-23-cross-review-m4.1-phase02-04-codex.md`에 raw 박음
2. γ 비교 산출물 박음 (`/cross-review` Step 5 정합)
3. 결함 분류 (양쪽 잡힘 / α만 잡음 / β만 잡음) + 봉합 가닥 결정

### 5.2. (B) β 스킵 (Codex 환경 없음 또는 시간 부족)

- α 단독 진행 — work-pin "별 시점 박을 가닥"에 *β 미발동* 박힘
- M4.1 마일스톤 마감 PR 전 별 시점 박을 가닥

### 5.3. (C) Codex가 봉합 박은 경우

- 본인이 Codex 직접 봉합 박았으면 diff 가져옴 → Claude γ 비교 + 후속 처리

---

## 6. 함정 / 주의 가닥

### 6.1. β 신뢰 맹목 함정 (`/cross-review` Hard rule 박힘)

Codex도 false positive 가능. "Codex가 말하면 무조건 맞다" X. γ 비교 의무. 본 마일스톤 옛 학습 = γ 4회차 (M3 Phase 02 Codex β 7건 발견) 정합 — 본인 판단 가닥 박음.

### 6.2. 본 cross-review = *사후 점검*

α reviewer가 이미 Phase 02 + Phase 03 GO 박혔음. 본 β cross-review = *추가 시각*. PR 머지 전 권장 시점이라 본 점검 통과면 M4.1 풀세트 PR 자신감 ↑.

### 6.3. Phase 04 회귀 검증 미박힘 함정

본 cross-review에 *회귀 검증 가닥 짚는지* 본인 우선순위. β가 deterministic + PathMap 정합 박혔다 박으면 회귀 검증 별 시점 박을 가닥. 결함 잡힘 시 즉시 봉합.

### 6.4. SubAgent prompt 명시 가닥 학습 자산 후보

server SubAgent Phase 03 자율 commit 패턴 = 본 cross-review β 점검 결과 따라 별 시점 박을 가닥 (work-pin 박힘). 본 가닥은 *외부 시각 가치 ↑* 영역.

---

## 7. 본 명세서 정합 학습 자산

- **third path 패턴 (★★★)** — 공유 vs 분리 갈래 갈등 → 양쪽 박되 drift guard 박은 패턴. 본 cross-review β 점검 통과 시 학습 자산 박힘 정합. β가 *third path 정신 X* 박으면 가짜 학습 자산 위험 발본.
- **외부 시각 = 학습 자산 검증 게이트** — 본인 작성 + α reviewer 정합 + β cross-review 통과 = 3중 안전망. 한국 게임 회사 백엔드 면접 어필 정합 (Zero-based 사고 + 검증 cadence + 외부 시각 흡수).

---

## 8. 작업 로그

- 2026-05-23: 본 pre-review MD 박힘. 본인이 별 세션 터미널에서 `codex review --base main` 호출 의무. 결과 가져오면 γ 비교 산출물 박음.
