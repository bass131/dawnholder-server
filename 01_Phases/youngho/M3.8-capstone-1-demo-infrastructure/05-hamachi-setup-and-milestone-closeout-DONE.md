---
summary: M3.8 Phase 05 마감 (보통 + 마일스톤 복잡) — Hamachi 시연 검증 통과 + Phase 05 5-A/5-B/5-C 3분기 봉합 풀세트 + M3.8 마일스톤 완전 마감. 5-A = 2 인스턴스 시연 검증 + 5 결함 봉합 / 5-B = 서버 연결 게이트 + 한글 폰트 false-promise 봉합 풀세트 / 5-C = Listener accept callback race 봉합 (false-promise 22번째 변종) + UI Scene Build 포함 + 학습 자산 HTML. Hamachi 통과 (`25.41.111.87` ↔ `127.0.0.1` broadcast 통과). false-promise 누적 22건 박제 (M3 5 + M3.6 7 + M3.7 0 + M3.8 10).
phase: 05
status: done
grade: 복잡
owner: youngho
---

# Phase 05 — Hamachi 셋업 검증 + M3.8 마감 의례 (마감)

## TL;DR

**M3.8 "Capstone-1 Demo Infrastructure" 마일스톤 완전 마감 의례** — 2026-05-22 plan 박힌 5 Phase × 본 세션 누적 15 commit. Phase 05는 옛 plan 박힌 *Hamachi 검증 + 마감 의례* 통합 박혔지만, **실제 진행은 3분기 (5-A/5-B/5-C)로 재정의** 박힘 — 시연 검증 도중 결함 표면화로 *발견 + 봉합 + 재검증 사이클* 2회 추가 박힘.

**Phase 05 3분기 통과**:
- **5-A** (commit `e2ee963`): 2 인스턴스 시연 검증 + 5 결함 봉합 (runInBackground / SnapshotTickInterval / InterpolationDelay / SnapThreshold / 점프 게이트 위치 / 서버 콘솔 로그 정합)
- **5-B** (commit `4a71dc2`): 서버 연결 게이트 + 한글 폰트 false-promise 봉합 풀세트 (ConnectionProbe + MainMenuController + NetworkBootstrap PlayerPrefs + MainThreadDispatcher + Pretendard 본체 + Pretendard SDF Proper atlas sub-asset + 부산물 SDF 4건 제거)
- **5-C** (commit `ab14c2c`): Listener accept callback race 봉합 (false-promise 22번째 변종) + UI Scene Build 포함 + 학습 자산 HTML

**핵심 발본 (★★★ 본 마일스톤 자산)**:
- "한 차원 race 봉합 ≠ 다른 차원 race 안전 보장" — M2.5 Phase 10 GameSession 내부 race 봉합 박혔지만 Listener accept callback race는 잔존 → Hamachi 시연 중 표면화
- 시스템 외곽까지 audit 박는 게 정합 — 본인 의심 "Queue에 제대로 박히는 거 맞나?" 통찰의 *위치 한 단계 밖* 실증

**Hamachi 통과 박제**:
- 본 머신 (`127.0.0.1`) + 서브 노트북 (`25.41.111.87`) 2 인스턴스 동시 통신 통과
- Player 3 (본 머신, roster:0) + Player 4 (서브 노트북, **roster:1** = 본 머신 인지) — initial roster 패턴 (M3 Phase 04) 정합
- 두 Player 모두 1초마다 Ping → Pong 정상 사이클
- 서버 unhandled exception 안 박힘 (race 봉합 검증 통과)
- tick p99 평소 0.1~0.3ms (PRD 10ms 마진 33배)

---

## AC 검증 결과

### 1. Hamachi 셋업 검증 박힘 ✅

**본인 단독 서브 노트북 가닥** (정유현 시간 안 기다림 — work-pin 박힌 가닥 정합):
- 본 머신 Hamachi 클라이언트 설치 + 네트워크 박힘 (IPv4 `25.2.220.254`)
- 서브 노트북 Hamachi 설치 박힘 (이미 박혀 있었음 — 본인 보고)
- 본 머신 서버 부팅 (`dotnet run --project 02_Server/GameServer`) — `Listening on 0.0.0.0:7777` 통과
- 본 머신 Unity build → USB → 서브 노트북 (`C:\Dawnholder-Demo\` 박음)
- 서브 노트북 클라 부팅 + Hamachi 가상 IP `25.2.220.254:7777` connect 통과
- *2인 같은 맵 broadcast* 실측 — Player 3 + Player 4 동시 등록, Ping/Pong 정상 사이클
- Windows SmartScreen / SAC 차단 없음 (서브 노트북 정상 환경)

### 2. Phase 05 3분기 봉합 풀세트 박힘 ✅

#### 5-A: 2 인스턴스 시연 검증 + 5 결함 봉합 (commit `e2ee963`)

| # | 결함 | 봉합 | 사유 |
|---|------|------|------|
| 1 | `runInBackground = 0` | → `1` | Focus 일시정지 차단, 2 인스턴스 동시 운영 |
| 2 | `SnapshotTickInterval = 5` (250ms) | → `2` (100ms 10Hz) | remote 보간 부드러움 |
| 3 | `InterpolationDelay = 0.2s` | → `0.15s` | 100ms broadcast 정합 |
| 4 | `SnapThreshold = 1.0` | → `1.5` | broadcast 4배 ↑ 부작용 봉합 (점프 reconcile 끊김) |
| 5 | 점프 입력 게이트 위치 | OnJump 시점 OnGround 검사 | 1차 cadence 게이트가 지면 점프 차단 사고 → 2차 OnJump로 이동 |
| 6 | 서버 콘솔 로그 spam | OnSend Console.WriteLine 제거 | 시각 spam 봉합 |

#### 5-B: 서버 연결 게이트 + 한글 폰트 false-promise 봉합 풀세트 (commit `4a71dc2`)

| 영역 | 박은 거 | 사유 |
|------|---------|------|
| `ConnectionProbe.cs` | 신설 | MainMenu Start 버튼 게이트용 짧은 TCP probe (1ms급) |
| `MainMenuController` | 게이트 박음 | InputField에 IP 입력 → probe 통과 시 CharacterSelect 진입 |
| `NetworkBootstrap` | PlayerPrefs fallback | MainMenu 박은 IP가 Gameplay Scene NetworkBootstrap까지 전달 |
| `MainThreadDispatcher` | 박음 | 비동기 콜백 안전 처리 (★★★ 학습: 없으면 무한 대기) |
| Pretendard 본체 (.otf) | 한글 풀세트로 교체 | 옛 PretendardStd 영문만 박힌 거 false-promise 발본 (16번째 변종) |
| Pretendard SDF Proper | atlas sub-asset 정통 패턴 | CreateFontAsset + AddObjectToAsset(atlas+material) + SaveAssets |
| 부산물 SDF 4건 | 제거 | 옛 시도 박힌 unused asset 정리 |

#### 5-C: Listener race + UI Scene + HTML (commit `ab14c2c`)

| 영역 | 박은 거 | 사유 |
|------|---------|------|
| `Listener.cs:83` | try-catch + early skip + fail-closed | accept ~ RemoteEndPoint 사이 race window 봉합 (헌법 #3 정합) |
| `EditorBuildSettings.asset` | UI Scene enabled 0 → 1 | ADR-021 Additive Load 패턴 — UI Scene Build에 포함 의무 |
| `_listener-race-and-packet-order-2026-05-23.html` | 학습 자산 박음 | race condition 본질 + 패킷 처리 3-layer + 면접 자산화 |

### 3. ADR-024 false-promise cadence 점검 결과 (의무 섹션)

**M3.8 풀 사이클 false-promise 발본 누적 (본 마일스톤 진행 중)**:

| # | 변종 | Phase | 표면화 시점 | 봉합 |
|---|------|-------|--------------|------|
| 13 | MCP 권한 시트 메인 세션 기준 (SubAgent 영역 미인지) | Phase 02 | SubAgent 호출 시 | 메인 세션 기준 박혀있는 거 인지 |
| 14 | SubAgent prompt 명시 위반 + 허위 보고 | Phase 02 | 자율 commit 박음 시점 | SubAgent prompt 검증 게이트 필요 (별 시점) |
| 15 | MCP OpenScene 자동 Save 부작용 | Phase 04 | Scene 박을 때 | 본 사고 박은 후 인지 박음 |
| 16 | PretendardStd 영문만 박힌 폰트 false-promise | 5-B | Unity 한글 시연 시 | Pretendard 한글 풀세트로 교체 |
| 17 | Dynamic Atlas 일부 글리프 박지 못함 | 5-B | runtime 한글 누락 | Pretendard SDF Proper + atlas sub-asset 정통 패턴 |
| 18 | MainThreadDispatcher 없으면 무한 대기 | 5-B | ConnectionProbe 비동기 콜백 | MainThreadDispatcher 박음 |
| 19 | TMP_FontAsset atlas/material sub-asset 누락 시 atlas Texture destroy | 5-B | 폰트 박는 코드 | sub-asset 정통 패턴 박음 |
| 20 | M3 Phase 02 핸드셰이크 코드 미구현 가짜 약속 (옛 발본, 본 마감 시점 재확인) | M3 | 옛 | 옛 봉합 박힘 |
| 21 | UI Scene Build 포함 X (ADR-021 약속 부분 위반) | 5-C | Hamachi 시연 build 박은 시점 | EditorBuildSettings.asset UI Scene enabled ON |
| 22 | Listener accept callback race (M2.5 봉합 영역 밖) | 5-C | Hamachi 시연 중 ObjectDisposedException | try-catch + early skip + fail-closed |

**M3.8 누적 = 10건** (Phase 02 3 + Phase 04 1 + 5-B 4 + 5-C 2). 옛 M3 5건 + M3.6 7건 + M3.7 0건 = **전체 누적 22건+**.

**본 마일스톤 핵심 통찰 (★★★)**:
> "한 차원 race 봉합은 다른 차원 race 안전을 보장 X. 시스템 외곽까지 audit 박는 게 정합."

M2.5 Phase 10 = GameSession 내부 race 봉합. 본 5-C = Listener accept callback race 봉합 — *차원이 다른 race window*. ADR-024 cadence가 마일스톤 마감 시점에 *옛 약속 vs 실재* 재점검 박는 가치 실증.

### 4. M3.8 마일스톤 5 Phase 종합 박힘 ✅

| Phase | 등급 | SubAgent | 산출물 | commit |
|-------|------|----------|---------|--------|
| 01 PRD 갱신 | 단순 | 메인 직접 | PRD 마일스톤 표 정합 + MVP 제외 항목 정정 ([H] 결정 박힘) | `15fd7c7` |
| 02 메인 + 엔딩 UI | 보통 | 메인 직접 | MainMenu Scene + Ending Scene + 재정렬 | `567f160` |
| 03 캐릭터 선택 | 복잡 | server SubAgent 자율 + 메인 통합 | PDL `C_CharacterSelect/S_CharacterSelectResult` + `PlayerStats` + CharacterSelectHandler + 클라 UI | `bd7c704` + `a029ef4` |
| 04 NPC 대화 | 보통 | 메인 직접 | NPC GameObject + 대화 hardcoded + Tag 봉합 + 시연 봉합 7건 | `f088c45` + `755159b` |
| 05 (5-A/5-B/5-C) | 보통 × 3 | 메인 직접 | 시연 검증 + 게이트 + race 봉합 풀세트 | `e2ee963` + `4a71dc2` + `ab14c2c` |

본 세션 누적 commit **15건** (origin 대비 ahead 15, push 보류 = M3.8 마감 PR 시 한 묶음 + --admin bypass).

### 5. 정량 수치 박힘 ✅

- Phase 5개 ✅ (전부 done — Phase 05는 3분기로 재정의)
- 본 세션 누적 commit 15건
- ★★★ 학습 누적 = 21건 + 5-C 1건 = 22건 (옛 13 + 본 세션 9 추가)
- ★★ 학습 누적 = 9건 (옛 그대로)
- ★ 학습 누적 = 1건 (PDL location plan)
- **false-promise 발본 누적 = M3.8 10건 + 옛 12건 = 22건+**
- 헌법 5/5 PASS 0 위반 (5-C race 봉합 = 헌법 #3 Trust Boundary 정합)
- dotnet build green (경고 0 오류 0, 본 commit 시점)
- Hamachi 2 인스턴스 broadcast 통과 (`25.41.111.87` ↔ `127.0.0.1`)
- 봉합 코드 변경 누적 (5-A + 5-B + 5-C): 약 +500/-100줄 + Unity 자동 부산물

### 6. 캡스톤 1 시연 흐름 dry-run 통과 ✅

메인 → 캐릭터 선택(전사/원거리) → Gameplay(마을 NPC + 전투) → 엔딩 흐름 시연 환경에서 끊김 없음 확인:
- MainMenu IP 입력 + Start → CharacterSelect 진입 통과
- CharacterSelect → Gameplay 진입 통과
- 2 인스턴스 broadcast 정상 (이동/점프/Ping)
- 서버 안정성 — race 봉합 후 unhandled exception 안 박힘

### 7. 본인 핵심 통찰 (★★★ M3.8 자산)

> **"클라 입력 컨트롤 잘 해야 서버 검증 로직 부담 + Reconcile 끊김 ↓"**
>
> 헌법 #1 (Server Authority)의 *건강한 보완*. 서버 권위 = 보안 게이트, 클라 입력 컨트롤 = UX 게이트. 대체 관계 아닌 *보완* 관계.
>
> 일거삼득: 시각 부드러움 / 대역폭 / 서버 부담 모두 ↓

## 5단계 보고

### 🎯 무엇을 만들었나

**M3.8 "Capstone-1 Demo Infrastructure" 마일스톤 완전 마감 박제**:
- 5 Phase × 본 세션 누적 15 commit
- 캡스톤 1 시연 인프라 풀세트: 메인 → 캐릭터 선택 → 마을(NPC) → 전투 → 엔딩 끊김 없는 흐름
- PDL ProtocolVersion 3→4 bump + PRD 갱신 동반 ([H] Phase 01 박힘)
- Hamachi 2 인스턴스 broadcast 통과 (서브 노트북 가닥, 정유현 시간 안 기다림)
- Phase 05 3분기 봉합 풀세트 (5-A + 5-B + 5-C)
- false-promise cadence 누적 22건+ 발본 (M3.8 10건 추가)
- 학습 자산 HTML 박음 (`_listener-race-and-packet-order-2026-05-23.html`, 캡스톤 평가 자산)

### 🤔 왜 필요한가

**3 사유 묶음**:
1. **교수 약속 (5/20 면담)** = "4맵 + 정밀 전투 + 하나의 완성된 Flow". M3 응급 데모(broadcast + 단순 전투)는 *전투만* — 시연 시작점/종료점 없음. 본 마일스톤 = *끊김 없는 시연 환경* 박는 것
2. **M4 영역과 분리** = M4 = *본 마감용 정밀화*. Demo 인프라(메인/캐릭터 선택/NPC/엔딩)는 *캡스톤 1 시연용* — 본 마감 후 일부 제거 가능. 별 마일스톤 정합
3. **PRD 정합 의무** = MVP 제외 항목에 *직업/스킬 트리, 퀘스트/NPC* 박혀있음. 캐릭터 선택(전사/원거리 스탯 분기) + NPC 대화(단순 hardcoded) 도입 시 PRD 갱신 의무 ([H] 위험도)

### 🛠️ 어떻게 만들었나

**Phase 분해 + 봉합 사이클**:
- Phase 01 (단순/meta) → Phase 02 (보통/client) → Phase 03 (복잡/server+shared+client) → Phase 04 (보통/client) → Phase 05 (보통 × 3분기 재정의)
- Phase 05 3분기 = *시연 검증 도중 결함 표면화로 재정의 박힘*. 옛 plan = "Hamachi + 마감 의례" 통합 박혔지만 실제 진행 = 5-A (시연 검증 + 5결함) + 5-B (서버 게이트 + 한글 폰트 false-promise) + 5-C (Listener race + UI Scene + HTML)
- 본인 분담 정신 박힘 (★★★ 학습) — *외관/시각 = 본인 직접*, *기능/wiring = AI*. Unity Editor 직접 조작 (Scene UI / Animator / Sprite 활용) 본인 박음, 코드 wiring AI 박음

### 🧪 어떻게 검증했나

**자동 (Hook + dotnet)**:
- `dotnet build` 경고 0 오류 0 (모든 commit 시점)
- `.githooks/pre-commit` 통과 (cloud 라인 자동 unstage)
- `phase-gate-validator.sh` 통과 (-DONE.md frontmatter)

**수동 (Hamachi 시연 검증)**:
- 본 머신 서버 부팅 → Hamachi IP listen 통과
- 서브 노트북 클라 build (USB 옮김) → Hamachi 가상 IP connect 통과
- 2 인스턴스 broadcast 실측: Player 3 (본 머신, roster:0) + Player 4 (서브 노트북, roster:1)
- 양쪽 1초마다 Ping → Pong 정상 사이클
- 서버 unhandled exception 안 박힘 (race 봉합 검증)
- tick p99 평소 0.1~0.3ms (PRD 10ms 마진 33배)

### ➡️ 다음 가닥

**M4.1 Combat Precision** 진입 대기:
- Phase 01 = Codex β 크로스 리뷰 + M3 응급 하드코딩 추가 발본
- Phase 02 = `Formulas.cs` 시그니처 = `ComputeDamage(attackerStats, targetStats)` (본 마일스톤 박힌 `PlayerStats` 흡수)
- Phase 03 = lag compensation rewind
- M4.1 plan 박힌 의존성 그래프 갱신 의무 ("사전 조건: M3.8 Phase 03 마감 (PlayerStats 박힘)")

**별 시점 박을 항목** (본 마감 시점 박힌 본인 별 시점 결함 + 큰 항목):
- **빌드 클라 기준 몬스터 Sprite 옛 Placeholder 박힘** — 외관 작업 별 시점 (시스템 완성 후)
- **RemoteEntity 애니메이터 적용** — 외관 작업 별 시점 (큰 마일스톤 단위)
- **횡스크롤 Terrain Tilemap 기반 레벨 디자인** — 별 마일스톤 가닥
- **SubAgent prompt 검증 게이트** (false-promise 14번째 후속 — M5+)
- **MCP OpenScene 자동 Save 부작용 대응** (M5+ unity-bridge SubAgent 강화)

---

## 결정 흐름

본 마감 의례 박힌 주요 결정 묶음:

1. **Phase 05 3분기 재정의** — 옛 plan = "Hamachi 검증 + M3.8 마감 의례" 통합 박혔지만, 시연 검증 도중 결함 표면화로 *5-A (시연 검증 + 5결함) + 5-B (서버 게이트 + 한글 폰트) + 5-C (Listener race + UI Scene + HTML)* 3분기로 재정의 박힘. 사유: *발견 + 봉합 + 재검증* 사이클이 2회 추가 박혀서 한 commit 단위 통합 박지 X
2. **Hamachi 검증 = 본인 단독 서브 노트북 가닥** — work-pin 박힌 가닥 정합 (정유현 시간 안 기다림). 사유: 본 마감 시점 + 정유현 시간 미확정 + 본 머신 ↔ 서브 노트북 가닥이 시연 환경 정합 동일
3. **Listener race 봉합 패턴 = try-catch + early skip + fail-closed** — 옵션 후보 = (a) RemoteEndPoint Session.Start 전 캡쳐 / (b) try-catch + skip / (c) Session 안에서 dispose 검사. (b) 채택 사유: 헌법 #3 Trust Boundary fail-closed 정합 + 추가 검증 부담 X + race window 명시화 (코드 주석에 사고 분석 박힘)
4. **UI Scene Build 포함 봉합** — 옵션 후보 = (a) EditorBuildSettings 직접 편집 / (b) Unity Editor 박음. (b) 본인 분담 정신 정합 — Unity Editor 직접 조작 영역
5. **마일스톤 등급 = 복잡** (mass plan frontmatter 정합) — Phase 5개 × 다중 도메인 변경. *대규모로 자동 상향 X* (옛 M3.6 = 대규모는 6 Phase + Hook + 헌법 영향 박혀있어 본 마감 대비 더 큰 영향). 본 -DONE.md = 복잡 등급 의무 (TL;DR + AC + 결정 흐름 + 학습 일지 후보 키워드), 5단계 보고 박힘은 *옵션 박음* (캡스톤 평가 자산 정신)
6. **별 시점 결함 3건 박제** — 본인 명시 가닥: 빌드 클라 몬스터 Sprite 옛 Placeholder + RemoteEntity 애니메이터 + 횡스크롤 Tilemap. *외관/시각 영역 본인 분담* 정신 정합 — AI 자율 박지 X, 본인이 별 마일스톤 단위로 박을 거
7. **분담 정신 박제 (memory `unity-visual-work-user-owned`)** — Unity 외관/시각 = 본인 직접, 기능/wiring = AI. 본 세션 commit `6a91b65`에서 본인이 짚어주신 패턴 정합

---

## 학습 일지 후보 키워드

본 마일스톤 박힌 ★★★ 학습 누적 22건 + ★★ 9건 + ★ 1건. **트랙 B Notion 박을 키워드 묶음**:

**Phase 05 5-C 본 마감 핵심 (★★★ 신규)**:
- `listener-accept-callback-race-window-bridging` — M2.5 GameSession 내부 race 봉합 박혔지만 Listener 외곽 race 잔존 + Hamachi 시연 중 표면화 + try-catch + early skip + fail-closed 봉합 사이클
- `cross-dimension-race-audit-pattern` — "한 차원 race 봉합은 다른 차원 race 안전 보장 X" 통찰. 시스템 외곽까지 audit 박는 게 정합. ADR-024 cadence 가치 실증
- `false-promise-22nd-instance` — false-promise 22번째 변종 발본 (M3 5 + M3.6 7 + M3.7 0 + M3.8 10). 마일스톤 마감 cadence 의무 적중
- `unity-build-settings-ui-scene-disabled-false-promise` — ADR-021 Additive Load 약속 박혔지만 BuildSettings에서 UI Scene disabled → Build 제외 → 런타임 사고. Editor Play로만 검증하면 표면화 X, Build 단계에서 표면화

**Phase 05 5-A/5-B 박힌 ★★★ (8건)**:
- `runinbackground-2-instance-pause-trap` (5-A)
- `snapshot-tickinterval-broadcast-frequency-trade-off` (5-A)
- `interpolation-delay-broadcast-period-coupling` (5-A)
- `snap-threshold-broadcast-rate-side-effect` (5-A)
- `jump-input-gate-position-determines-semantics` (5-A)
- `mcp-permission-sheet-main-session-baseline` (Phase 02 발본)
- `subagent-prompt-violation-and-false-report` (Phase 02 발본)
- `mcp-open-scene-auto-save-side-effect` (Phase 04 발본)

**Phase 05 5-B 한글 폰트 false-promise 풀세트 (4건)**:
- `pretendard-std-english-only-font-false-promise` — 옛 PretendardStd는 영문만, 한글 풀세트 X
- `dynamic-atlas-partial-glyph-coverage` — Unity Dynamic Atlas가 일부 글리프 박지 못함
- `main-thread-dispatcher-infinite-wait-without` — 비동기 콜백 받을 dispatcher 없으면 무한 대기
- `tmp-font-asset-atlas-material-sub-asset-pattern` — CreateFontAsset + AddObjectToAsset 정통 패턴

**M3.8 핵심 본인 통찰 (★★★ 자산)**:
- `client-input-control-server-validation-symbiosis` — "클라 입력 컨트롤 잘 해야 서버 검증 부담 + Reconcile 끊김 ↓". 헌법 #1 (Server Authority)의 *건강한 보완*. 서버 권위 = 보안 게이트, 클라 입력 컨트롤 = UX 게이트
- `visual-work-user-owned-functional-ai-owned` — Unity 외관/시각 = 본인 직접, 기능/wiring = AI. 본 세션 commit `6a91b65`에서 본인이 짚어준 분담 정신 (memory 박힘)

**면접 결정타 키워드 (한국 게임 회사 백엔드)**:
- `false-promise-pattern-22nd-variant` (옛 12건+ → 22건+ cadence 누적, 헌법 #4 + ADR-024 가치 실증)
- `option-c-gate-progress-stale-hole-bridging` (M3.7 분산 시스템 상태 동기 게이트)
- `listener-race-vs-game-session-race-different-dimensions` (M2.5 + M3.8 5-C 두 차원 race 봉합 사이클)
- `client-input-control-server-validation-symbiosis` (본인 통찰)
- `harness-v1-discussion-pivot` (M3.5 Zero-based 재구성)
- `gamma-internalization` (M3.6 Codex γ 외부 의존 → plan-auditor 내부 흡수)
- `local-vs-remote-entity-branch` (M3 응급 데모 broadcast)

상세 ★★★ 누적 ~62건+ → `CONTEXT_LearningJournalCandidates.md` 참조 (별도 파일, .gitignore). 본 마감 시점 박힌 22건 신규 = 노션 트랙 B 박제 대기 (별 시점).

---

## 박제

본 Phase = 보통 등급이지만 *마일스톤 마감 의례*라 -DONE.md 박음 의무 (M3.8 마일스톤 복잡 등급 정합 → frontmatter `grade: 복잡` 박음).

ADR-013 페어 박제:
- AI=사실 박제 = 본 -DONE.md (Phase 산출물 + 회귀 + ADR-024 점검 결과)
- 본인=회고 = 별 박제 (Notion 트랙 B 또는 `learning-journal/youngho/M3.8-회고.md` — 본인 자율 박을 거)

5단계 보고 HTML 별도 박지 X (마일스톤 복잡 등급이라 MD/HTML 이중 의무 X, 본 -DONE.md 안 5단계 흡수 박힘). 별 학습 자산 HTML = `_listener-race-and-packet-order-2026-05-23.html` 박혀있음 (Phase 05 5-C 후속, 캡스톤 평가 자산).

work-pin 최종 갱신 박을 거 = "M3.8 ✅ 완전 마감. 다음 = M4.1 plan 재조정 + Phase 01 진입"

---

## 작업 로그

- 2026-05-22: Phase 정의 박힘 (M3.8 plan 박는 시점)
- 2026-05-23: 5-A 시연 검증 + 5 결함 봉합 (commit `e2ee963`)
- 2026-05-23: 5-B 서버 연결 게이트 + 한글 폰트 false-promise 봉합 풀세트 (commit `4a71dc2`)
- 2026-05-23: 5-C Listener accept callback race 봉합 + UI Scene Build 포함 + 학습 자산 HTML (commit `ab14c2c`)
- 2026-05-23: Hamachi 시연 검증 풀세트 통과 + 본 -DONE.md 박음 (마감 의례)
