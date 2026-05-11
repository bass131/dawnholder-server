# CONTEXT — 세션 핸드오프

> **이 문서를 읽는 Claude에게**: 헌법(`CLAUDE.md`)과 함께 가장 먼저 읽으세요.
> 이전 세션의 톤·결정을 잇기 위한 핸드오프 노트입니다 (헌법과 충돌 시 헌법이 이김).
>
> **사용자에게**: 새 Claude Code 세션 시작 시 가장 먼저 읽힐 파일.
>
> **유지 정책 (2026-05-09 결정)**: **누적이 아니라 응축**. ~200줄 넘으면 큰
> 마일스톤 끝날 때마다 처음부터 재작성. 옛 디테일은 `git history` +
> `00_Document/learning-journal/` + 노션 "Dawnholder 협업 히스토리"에서 찾기.

---

## TL;DR — Claude는 다음 톤으로 응답하세요

1. **학부생 멘토링 모드**. 시니어가 주니어 가르치듯 친절히. 전문 용어 첫 사용 시 풀이.
2. **trade-off 항상 설명**. "A 골랐어요"가 아니라 "A vs B 중 A, 이유는…, 단점은…".
3. **솔직함 우선**. 위험 미리 짚기. 마감 못 지킬 것 같으면 정직하게.
4. **5단계 보고**. 코드 작업 끝나면 🎯 무엇 / 🤔 왜 / 🛠️ 어떻게 / 🧪 테스트 / ➡️ 다음.
5. **Phase 완료 시 학습 일지 권유**. `/journal:phase` 등.

상세 톤 가이드: 헌법(`CLAUDE.md`) "사용자 컨텍스트" 섹션.

---

## 사용자 컨텍스트 (변하지 않는 것)

- **신분**: 학부생. 도메인 학부생 수준, 판단 본능은 시니어 진입로.
- **언어**: 한국어 (대화). 코드/식별자 영어.
- **위치**: 한국 (KST).
- **목표**: 게임 회사 백엔드 포지션 포트폴리오 + 백엔드/네트워킹 실전 학습.
- **프로젝트**: Dawnholder Project (메이플 같은 캐주얼 RPG + 길드 거점 타이쿤).
- **솔직함 패턴**: 모르는 건 모른다고, 마감 현실도 솔직히. 환영하고 같이 솔직하게.

---

## 하드 일정

- **6월** — 캡스톤 1 발표 (수업 중간, "진행 중" OK)
- **11월 19일** — 졸업작품 본 마감

→ Phase A (~6월): M1~M3 도달, 두 명 같은 맵 데모. Phase B (7~11월): M4~M8 MVP.

---

## 팀 구조 (2026-05-06 미팅 후)

| 역할 | 이름 | 영역 | 합류 시점 |
|------|------|------|-----------|
| 본인 (팀장) | 유영호 | 백엔드 코어 | 사용 중 |
| 팀원 1 | 김인규 | Unity 클라 아트 리소스 및 컨텐츠 개발 | 6월 말 학기 후 |
| 팀원 2 | 정유현 | Unity 클라 UI/입력 및 컨텐츠 개발 | 6월 말 학기 후 |
| 팀원 3 | 박정우 | 관리 시스템 (MES, **별도 레포**) | 7월 이후 (구독비 사유) |

- 팀원 전원 개발 경험 거의 백지. 온보딩은 그 수준에서.
- **캡스톤 1 시점 = 본인 단독 작업 가정.**

---

## ⏸️ 현재 멈춤 지점 (2026-05-11)

**★ M2 Phase 05 완료 — client prediction + snap reconcile, 4/4 완료조건 wire 검증**. 다음 작업 = **Phase 06 input replay reconcile** 진입 직전.

### M2 진행 현황 (commit hash로 추적)
- ✅ Phase 01 — Unity 씬 + Player/Ground/Camera (`f26fc92`)
- ✅ Phase 02 — 20 TPS GameLoop + GameMap actor (`011bcaf`)
- ✅ Phase 03 — 접속 핸드셰이크 (S_EnterMap) + ConcurrentQueue 마샬링 + 헌법 #1 첫 실전 (`d0b94d3`)
- ✅ Phase 04 — C_MoveIntent + S_Snapshot + 헌법 #3 검증 골격 (lag 의도 노출) (`d9f8351`)
- ✅ Phase 05 — client prediction + snap reconcile, **헌법 #1 코드 시연**(cheat dx=-1000) (`b02df49` + DONE.md `a118552`)
- ⏳ Phase 06 — input replay reconcile (snap → 부드러운 따라잡음) (**다음 진입**)

### 2026-05-11 본 세션 사이드 트랙
- ✅ **Phase 03·04 라운드트립 회귀 안전망** (Phase 05 commit에 포함) — `PacketRoundTripTests`에 EnterMap/LeaveMap/MoveIntent/Snapshot Write→Read 왕복 +38 케이스. dotnet test 25→63 PASS. PacketGenerator byte/sbyte 정정 + .NET Std 2.1 float 경유 회귀 가드.
- ✅ **240Hz framerate-bound 송신 발견** (Phase 05 검증 부산물) — 클라가 매 frame 송신 시 wire rate 300-500/s, 서버 20Hz 대비 96% 낭비 + 정상 사용자가 rate-limit cheat 의심으로 잘못 분류. 임시 정정: 임계 100→500 + 윈도우당 첫1회만 로깅. 본질 fix는 Phase 06 fixed timestep accumulator.
- ✅ **SnapThreshold 0.5→1.0 튜닝** — 검증 데이터(자연 drift 0.5 직상)가 임계 너무 빡빡함을 보여줘 1.0으로 조정. Phase 06 fixed simulation 후 다시 좁힐 여지.
- ✅ **노션 세션 로그 박제** — Codex CLI 위임으로 STAR + 용어 풀이.

### M1 완료 요약 (이전 시점, [`CONTEXT_History.md`](CONTEXT_History.md) + `-DONE.md` 참조)
Phase 01~07 + 회귀 안전망. 솔루션 부트스트랩 / ServerCore 7파일 / 04_ClientNet Y2 분리 / Listener wire-up / framing+Ping-Pong / PacketGenerator 이주 / PDL 정합. M2 진입 전 도구대 정합도 완료.

### 다음 세션 첫 액션
1. **Phase 06 진입** — `01_Phases/M2-first-connection/06-input-replay-reconcile.md` 통독 → 6 step 분해 같은 패턴 (Phase 05 흐름 참조)
2. **사전 정리 (Phase 06 시작 전)**: `clientTick = (uint)` 음수 캐스트 정합(Phase 04 본 리뷰 🟡, replay에서 폭발) + framerate-bound 송신 throttle (Phase 06 본 작업에 자연 흡수 예상)
3. (옵션 — 강력 권유) **Phase 05 학습 일지** — 기억 휘발 전. 특히 ★★★ `/journal:concept Server Authority 코드 시연` (cheat dx=-1000 wire 박힘, 면접 결정타)

### 학습 일지 후보 (밀린 것 + 본 세션 추가, 시간 흐르기 전)
- ★★★ `/journal:concept Server Authority 코드 시연 (헌법 #1)` — Phase 05 cheat 시뮬 wire 박힘. 면접 결정타.
- ★★ `/journal:concept Client-side prediction` — Phase 05 직접 구현 + 한계 발견.
- ★★ `/journal:concept Prediction 본질적 한계 (방향 전환 클러스터링)` — Phase 05 본인 시각 관찰 + 데이터 부합.
- ★★ `/journal:bug 한글 경로 도구 호환성 (Burst + WDAC)` — Phase 03·04 두 사건 같은 뿌리.
- `/journal:bug framerate-bound 송신 (240Hz 모니터)` — Phase 05 검증 부산물.
- `/journal:concept Map=Actor + ConcurrentQueue 마샬링` — Phase 03.
- `/journal:concept Intent vs State 분리` — Phase 04.
- `/journal:concept Trust Boundary 실전 (헌법 #3)` — Phase 04.

---

## 보류 중

### 사례 기반 가드 결정 (보류 중)
코드가 더 들어온 뒤 *진짜 필요한 가드*가 어떤 것인지 보고 결정:

- **Hook 보강** (가드 강제):
  - ✅ `validate-phase-gate.sh` (2026-05-11 박힘, ADR-015) — `-DONE.md` Post-flight 게이트
  - `tdd-guard.sh` (공식·직렬화·상태머신 영역, 테스트 부재 시 차단)
  - `tick-blocking-guard.sh` (`02_Server/GameServer/Loop/`에 `Task.Delay`/`Sleep`/`await Db` 차단)
  - `check-server-authority.sh` 강화 (03_Client/에 데미지/HP/XP 키워드 차단)
  - `HOOK_MODE=warn|block` 토글 — Phase 진행 단계별 적용
- **게이트 ①·③ (보류)**: 게이트 ②(Post-flight) 실전 1회 검증 후 ① Pre-flight(Phase 시작 의식) → ③ Blocked 명시화 순으로 도입 검토.
- **TDD 강제 영역 결정**: 헌법 6번째 원칙으로 박을지, ADR로 박을지. "엄격 vs 미루기" 갈등 인지 중.
- **하네스 정리 점검** (비대화 우려 후속): 슬래시 14개/서브에이전트 6개 중 실사용 빈도 낮은 것 가지치기. `CONTEXT.md` vs `CONTEXT_History.md` vs `-DONE.md` 역할 중복 점검.

### 학습 마라톤 시작 전 (~6월 말)
- `00_Document/TEAM.md` (미팅 결과 박제)
- `.claude/settings.json` 권한 분리 (영역별 쓰기)
- `00_Document/onboarding/` (인규/정우/유현 용, Git/터미널 백지 가정) - 팀장(본인) : 유영호
- `mentor` 서브에이전트
- MES 별도 레포 헌법 골격 (정우 합류 직전)
- 정우 Anthropic 학생 할인 알아보기

### 도구 정책 (필요해질 때까지 보류)
- **WDAC 미서명 DLL 차단 정책 정리** — Unity Burst Enable 시 `error code 4551` (Windows Defender Application Control). 현재는 Burst Disable로 회피 (ADR-017 트레이드오프 명시). Burst가 진짜 필요한 복잡도 시스템 도입 시점에 별도 ADR로 처리. 옵션 후보: (A) Smart App Control 끄기, (B) `BurstCache/` 경로 예외, (C) Burst self-sign + TrustedPublisher 등록.
- **PacketGenerator PacketFormat 템플릿의 `using Shared.Protocol;` 누락** — 2026-05-11 검증 5번에서 표면화 (ADR-002 "직접 짠 코드라 버그 가능" 트레이드오프의 3번째 잠복 버그, M1 Phase 06에서 정정한 2건과 같은 부류). 현재 manager 두 파일(`02_Server/.../Generated/ServerPacketManager.cs`, `04_ClientNet/Generated/ClientPacketManager.cs`)은 한 번도 commit된 적 없고 import 안 되어 build 통과. 정정은 별도 미니 정정 Phase로 처리 — 템플릿에 `using` 추가 → 재생성 → manager 두 파일이 정상 컴파일되는지 확인.

---

## 핵심 결정 요약 (ADR 박혀있음, 빠른 참조)

### 기술 스택
- Unity 6.4 LTS + .NET 10 LTS 권위 서버 (ADR-001 v3 — Unity AI MCP Server 시너지)
- Raw TCP + length-prefixed binary + **자체 PDL** (ADR-002)
- 모노레포 (단, MES는 별도 레포 — ADR-011)
- 20 TPS 서버 틱 (ADR-004)
- PostgreSQL + EF Core 10 (ADR-005)
- `98_Shared/` = .NET Standard 2.1 + DLL + Embedded PDB (ADR-010)
- ServerDev 4월 코드 부분 채택 — **시나리오 B** (ADR-011)
- Unity 클라 socket 분리 모델 — **Y2** (ADR-012, 책임 단위 정제)
- 프로젝트 폴더 ASCII 경로 — `C:\Dev\ClaudeDev` (ADR-017, 한글 경로 영구 해결. WDAC는 별도 보류)

### 게임/스코프
- 두 장르 결합 MVP (RPG + 길드 타이쿤) — ADR-006
- 거점 시설 = 구매/기능 모델만 — ADR-007
- 단일 서버 프로세스 — ADR-008
- 게임 회사 백엔드 포트폴리오 — ADR-009
- 6월 캡스톤 1 = 옵션 C(2인 movement) Stretch / 옵션 B(1인) Fallback

### Harness 작동 원칙
- 학부생 멘토링 + 5단계 보고 + `-DONE.md` 박제 + 학습 일지 권유 (헌법)
- 자동 실행 안 함 — Phase 끝 → 보고 → 사용자 확인 → 다음 Phase 수동
- **문서 세분화**: 사전형 .md 220줄 임계 + 헌법만 350줄 예외 (ADR-014)
- **박제 분업**: `-DONE.md` = AI 작성(사실/결정/증상). `learning-journal/` = 본인 작성(회고/면접 답변). Phase 폴더에 짝꿍으로 (ADR-013)
- **Post-flight 게이트**: `-DONE.md` Write/Edit 시 `validate-phase-gate.sh` 형식 강제 (자동 실행은 비채택, 학습 호흡 보존) (ADR-015)
- 학습 일지: 본인이 쓰고 AI는 인터뷰만 (가짜 학습 방지). `-DONE.md`를 사실 베이스로 활용.

### Notion 협업 히스토리 문서 분업 (ADR-016)
- **정책 위치**: [`.claude/templates/done-md-template.md`](.claude/templates/done-md-template.md) "Notion 협업 분업 원칙" 섹션 (영속). CONTEXT 응축 시 유실 방지를 위해 템플릿으로 이주됨 (2026-05-11).
- **요지**: Claude=사실 박제 / Codex=Notion 재편집·면접 답변 / 본인=회고·학습 일지. 자세한 8단 구조·용어 처리·원칙은 위 링크 참조.

---

## 자산 위치 (빠른 참조)

폴더는 탐색기 정렬 고정용 숫자 prefix를 갖습니다.

```
00_Document/PRD.md, ARCHITECTURE.md, ADR.md     ← 결정·구조
00_Document/ADR_History.md                       ← ADR 변경 이력 (외부화)
00_Document/commands-index.md                    ← 14개 슬래시 커맨드 카탈로그
00_Document/learning-journal/M{N}-{slug}/        ← Phase 학습 일지
  └── concepts/                                  ← 개념 일지
01_Phases/M{N}-{slug}/                           ← 작업 단위 ({NN}-*.md 정의 + {NN}-*-DONE.md 박제 페어)
02_Server/, 03_Client/, 98_Shared/, 99_Tools/    ← 게임 코드
.claude/agents/                                  ← 6개 서브에이전트
.claude/commands/{learn,journal,work,session}/   ← 14개 슬래시 커맨드 (4 카테고리)
.claude/hooks/                                   ← 3개 (validate-shared / check-authority / validate-phase-gate)
Dawnholder.slnx                                  ← .NET 솔루션 (02_Server + 98_Shared)
global.json                                      ← .NET SDK 핀 (10.0.203)
노션 "Dawnholder 협업 히스토리" DB               ← 세션 STAR 박제
```

---

## 미해결 질문 (남은 ADR 후보)

- **ADR 후보**: 인증 방식 (단순 닉네임 → JWT? 세션?)
- **ADR 후보**: 캐릭터 데이터 스키마 (정규화 vs JSONB)
- **ADR 후보**: 채팅 시스템 (TCP 전송 vs 별도 채널) — MVP 후
- **ADR 후보**: 로그 저장 (로컬 파일 vs 외부 sink)
- **ADR 후보**: 헤드리스 봇 자동화 방식
- 캡스톤 1 발표 정확한 날짜 (6월 중순 가정)

---

## 다음 Claude를 위한 마지막 안내

1. **이 문서 + 헌법 통독 후 짧게 인지 확인**: "CONTEXT 잘 읽었어요. M1 끝났고 M2 진입 전 `/work:plan` 분해부터 가는 거 맞죠?"
2. **5단계 보고는 코드 작업 후에만** (대화/의논엔 안 씀).
3. **사용자가 던지는 짧은 메시지**엔 짧게 공감 + 다음 액션 가볍게.
4. **새 정보**(미팅 결과, Unity 업데이트 등)가 들어오면 즉시 재정렬.
5. **이 문서는 살아있는 응축본**. 큰 변화 시 갱신하되 누적 X — 정책은 맨 위 참조.

---

## 갱신 이력

> 이력은 [`CONTEXT_History.md`](CONTEXT_History.md) 참조 (헌법: 문서 세분화 정책 — 누적 섹션 외부화).
>
> 새 갱신 발생 시 본 파일이 아니라 `CONTEXT_History.md`에 한 줄씩 추가.
