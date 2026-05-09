# CONTEXT — 세션 핸드오프

> **이 문서를 읽고 있는 Claude에게**: 이 파일은 이전 두 세션에서 사용자와
> 합의한 맥락을 담고 있습니다. 헌법(`CLAUDE.md`)과 함께 가장 먼저 읽으세요.
> 이 문서는 **이전 세션의 톤과 결정을 이어가기 위한 핸드오프 노트**이지
> 정식 헌법이 아닙니다. 헌법과 충돌하면 헌법이 이깁니다.
>
> **사용자에게**: 이 문서는 새 Claude Code 세션 시작 시 가장 먼저 읽힐 파일.
> 본인이 직접 갱신해도 됩니다 (예: 미팅 결과, 진척 상황).
>
> **유지 정책 (2026-05-09 사용자 결정)**: 누적이 아니라 **응축**.
> 분량이 ~200줄을 넘기 시작하면 큰 마일스톤(예: Phase 묶음 완료) 끝나는 시점에
> **처음부터 재작성**. 갱신 이력은 짧게 요약 1~2줄로. 옛 디테일은 git history와
> learning-journal/ 또는 노션 협업 히스토리에서 찾기.

---

## TL;DR — Claude는 다음 톤으로 응답하세요

1. **학부생 멘토링 모드**. 시니어가 주니어 가르치듯 친절하게. 전문 용어 첫 사용 시 풀이.
2. **trade-off 항상 설명**. "A를 골랐어요"가 아니라 "A vs B 중 A, 이유는…, 단점은…".
3. **솔직함 우선**. 위험은 미리 짚기. 마감 못 지킬 것 같으면 "지킬 수 있다"고 거짓말 안 함.
4. **5단계 보고**. 코드 작업 끝나면 🎯 무엇 / 🤔 왜 / 🛠️ 어떻게 / 🧪 테스트 / ➡️ 다음.
5. **Phase 완료 시 학습 일지 권유**. `/journal-phase` 등.
6. **"AI가 실수하면 마구를 고친다"** 원칙. 발견된 약점은 헌법/Phase/hook에 박음.

상세 톤 가이드는 헌법(`CLAUDE.md`)의 "사용자 컨텍스트" 섹션에 있음.

---

## 사용자 컨텍스트 (변하지 않는 것)

- **신분**: 학부생. 도메인 지식은 학부생 수준이지만 판단 본능은 시니어 진입로.
- **언어**: 한국어 (대화 톤 그대로 유지). 코드/식별자는 영어.
- **위치**: 한국 (시간대 KST).
- **목표**:
  - 게임 회사 백엔드 포지션 지원용 포트폴리오
  - 백엔드/네트워킹 실력을 실전 프로젝트로 학습
- **프로젝트**: Dawnholder Project (메이플 같은 캐주얼 RPG + 길드 거점 타이쿤)
- **솔직함 패턴**: 본인이 모르는 건 모른다고, 팀원 평가도 정직하게, 마감
  현실도 솔직하게 공유함. 이걸 환영하고 같이 솔직하게 가세요.

---

## 하드 일정 (변하지 않음)

- **6월** — 캡스톤 1 발표 (수업 중간 마감, "진행 중" 발표면 OK)
- **11월 19일** — 졸업작품 진짜 마감

→ 일정 두 단계 분리:
- **Phase A (~6월)**: M1~M3까지 도달. 두 명이 같은 맵 데모 + 발표 자료
- **Phase B (7~11월)**: M4~M8. 진짜 MVP 완성

---

## 팀 구조 (2026-05-06 미팅 후 확정)

| 역할 | 이름 | 영역 | 주당 시간 | Claude Code 학습 시작 |
|------|------|------|-----------|----------------------|
| **본인 (팀장)** | 정유현 | 백엔드 코어 (서버/네트워크/DB) | 주 6일 (조정 가능) | 사용 중 |
| **팀원 1** | 김인규 | Unity 클라이언트 UI/입력 | 주 4~5일 | 6월 말 학기 후 |
| **팀원 2** | 박정우 | 관리 시스템(MES) — **별도 레포 + 별도 헌법** | 주 4~5일 | 7월 이후 (Anthropic 구독 비용 사유) |

- 팀원 전원 **개발 경험 거의 백지** (Git/터미널 포함). 온보딩은 그 수준에서 시작.
- ComfyUI 리소스/콘텐츠 작업: 인규가 클라 UI와 병행하거나 추후 정리 (1차 미팅에선 분리 안 됨).
- MES 별도 레포 + 별도 헌법 결정 (ADR-011 외 별도 헌법 골격은 정우 합류 직전에 작성).
- "도구 합의: 옵션 C — 다 같이 Claude Code"는 합의됨. 학습 마라톤 시작이 6월 말~7월로 미뤄짐.

---

## ⏸️ 현재 멈춰있는 정확한 지점

**Phase 01 대기 중.** PR #1·#2 모두 머지 완료(2026-05-09 후속 세션, `--no-ff` 두 번 + 원격 브랜치/워크트리 정리). main이 최신 상태 — 미팅 결정 박제 + `/log-session` 구현 + CONTEXT 갱신 다 포함. 묶음 B (팀/조직 정렬)는 사용자 결정으로 **잠시 대기 (2026-05-09)** 유지. Phase 01 코드 이주는 본인 시간 날 때 시작.

### 머지된 PR (참조용)

| PR | 제목 | 머지 |
|----|------|------|
| [#1](https://github.com/bass131/dawnholder-server/pull/1) | Align project decisions after team meeting (scenario B) | merged |
| [#2](https://github.com/bass131/dawnholder-server/pull/2) | feat(harness): add /log-session slash command | merged |

머지 순서는 #1 → #2 (의미적 의존성: #2의 CONTEXT 커밋이 #1의 슬래시 커맨드 구현을 전제로 씀). 자세한 머지 히스토리는 `git log --oneline` 참조.

### 진척 상황 (2026-05-09 기준)

**완료**:
- ✅ 팀원 미팅 끝남. 결과 → "팀 구조" 섹션 갱신됨.
- ✅ ServerDev (4월 학습용 코드) 분석 완료. 시나리오 B 결정 (코어 채택 + 게임 로직 새로).
- ✅ DLL + Embedded PDB 코드 공유 방식 확정.
- ✅ ADR-001 갱신 (.NET 8 → .NET 10 LTS + .NET Standard 2.1).
- ✅ ADR-002 갱신 (MessagePack → 자체 PDL).
- ✅ ADR-010 신규 (DLL 공유 방식).
- ✅ ADR-011 신규 (ServerDev 코드 부분 채택).
- ✅ PRD 캡스톤 1 발표 섹션 추가 (옵션 C / B fallback).
- ✅ CLAUDE.md Stack 섹션 갱신.

**완료 (2026-05-09 추가)**:
- ✅ `/log-session` 슬래시 커맨드 구현 → PR #2 머지 완료. 명세(`docs/skill-specs/log-session.md`, PR #1)를 실행 파일(`.claude/commands/log-session.md`)로 옮김. 트리거 A(수동) 채택. 첫 실호출 테스트는 이 세션 끝에서 시도 예정.
- ✅ PR #1·#2 main에 머지(`--no-ff` 두 번) + 원격 브랜치/로컬 워크트리 정리. `.claude/settings.local.json` 메인 레포로 이동.

**보류 중 (묶음 B — 사용자 "잠시 대기" 결정 2026-05-09)**:
- ⏳ `docs/TEAM.md` 작성 (미팅 결과 박제. CONTEXT.md엔 요약만 있음).
- ⏳ `.claude/settings.json` 권한 분리 (영역별 쓰기 권한 — client/server/shared/tools).

**보류 중 (학습 마라톤 시작 전, 6월 말까지 시간 여유)**:
- ⏳ `docs/onboarding/` (인규/정우용. 거의 백지 가정 — Git/터미널부터).
- ⏳ `mentor` 서브에이전트 추가.
- ⏳ MES 별도 레포 헌법 골격 (정우 합류 직전 — 7월 이후).
- ⏳ 정우 Anthropic 학생 할인/저렴한 플랜 같이 알아보기.

**보류 중 (Phase 01 끝나고 Phase 02 진입 직전 처리, 2026-05-09 결정)**:
- ⏳ TDD 강제 영역(공식·직렬화·상태머신) — 헌법 6번째 원칙 후보로 박을지 결정 + Hook 작성.
- ⏳ `tdd-guard.sh` Hook — 위 영역 파일 수정 시 테스트 부재면 차단(또는 warn).
- ⏳ `tick-blocking-guard.sh` Hook — `server/GameServer/Loop/`에 `Task.Delay`/`Thread.Sleep`/`await Db` 차단.
- ⏳ `check-server-authority.sh` 강화 — `client/`에서 데미지/HP/XP 키워드 grep 차단.
- ⏳ `HOOK_MODE=warn|block` 토글 — Phase 01은 warn, Phase 02부터 block.
- 이유: Hook은 코드가 있어야 가치가 생김. Phase 01은 코드 이주라 코드가 거의 없음. 미리 박으면 YAGNI + 추측 기반이라 다시 손볼 가능성 큼.

**다음 코드 작업 (묶음 C — Phase 01 재정의)**:
- Phase 01 = "ServerDev 코드 이주 + DLL 빌드 파이프라인 셋업". 단순 솔루션 부트스트랩이 아님.
- 자세한 흐름은 다음 섹션 참조.

---

## 다음 Phase = Phase 01 (코드 이주 + 셋업)

**조건**: 본인이 시간 있을 때. 묶음 B(TEAM.md/권한 분리)는 Phase 01 전후 어느 쪽이든 OK.

**예상 시점**: 다음 주 (사용자가 "이번 주는 힘들고 다음 주에"라고 함).

**Phase 01 재정의** (기존 `phases/M1-foundation/01-solution-bootstrap.md` 갱신 필요):
"빈 솔루션 만들기"가 아니라 **"ServerDev 4월 코드 채택 + DLL 빌드 파이프라인"**.

**완료 조건**:
1. ✅ `shared/Net/` (Listener, Session, RecvBuffer, SendBuffer, JobQueue) — ServerCore에서 이주, .NET Standard 2.1로 빌드.
2. ✅ `shared/Protocol/` + `tools/PacketGenerator/` — PDL.xml + 코드 생성기 이주. 발견된 버그(`PacketFormat.cs:178` 하드코딩 `C_Chat`) 수정.
3. ✅ `tools/qa-sim/` — DummyClient 이주, .NET 10으로.
4. ✅ `server/` — .NET 10 콘솔 호스트 부팅 (게임 로직은 Phase 02부터).
5. ✅ `client/` — Unity 6.4 LTS 빈 프로젝트, `Plugins/` 폴더에 shared.dll/.pdb 자동 복사 빌드 스크립트 동작.
6. ✅ Unity 에디터에서 `using Net;` 입력 시 IntelliSense 뜨고 F12로 원본 .cs 코드 보임.
7. ✅ `dotnet build` 한 번으로 server + shared + tools 다 빌드되고, Unity 새로고침이 자동 됨.

**시작 흐름**:
1. 사용자가 새 세션 열고 "Phase 01 시작하자"
2. Claude는 헌법 + PRD + ADR-001/002/010/011 + 갱신된 `phases/M1-foundation/01-solution-bootstrap.md` 통독
3. 통독 후 사용자에게 5대 절대 원칙 인지 확인 + 시나리오 B 인지 확인
4. ServerDev 폴더(`C:\Users\bass1\바탕 화면\ServerDev\Dawnholder_Server\`)에서 ReadOnly 참고하며 이주
5. 단계별 빌드 검증 (shared만 → server → tools → client 순)
6. 완료 후 5단계 보고 + 학습 일지 권유

**가장 위험한 부분**: shared의 .NET Standard 2.1 빌드가 Unity Plugins/에서 정상 인식되는지. 첫 시도에 안 될 가능성 큼 — 시행착오 예상. 막히면 `/why DLL Plugins/` 같은 학습 도구로 풀어가기.

---

## 핵심 결정들 (요약)

상세는 ADR(`docs/ADR.md`)에 박제됨. 여기는 빠른 참조용.

### 기술 스택
- Unity 6.4 LTS + **.NET 10 LTS** 권위 서버 (ADR-001, 2026-05-06 갱신)
- Raw TCP + length-prefixed binary + **자체 PDL + 코드 생성기** (ADR-002, 2026-05-06 갱신)
- 모노레포 (ADR-003) — 단, MES는 별도 레포 (ADR-011)
- 20 TPS 서버 틱 (ADR-004)
- PostgreSQL + EF Core (ADR-005)
- **shared/ = .NET Standard 2.1 + DLL + Embedded PDB** (ADR-010, 신규)
- **ServerDev 4월 코드 부분 채택** (ADR-011, 신규 — 시나리오 B)

### 게임/스코프
- 두 장르 결합 MVP (ADR-006) — RPG + 길드 타이쿤 분리 안 함
- 거점 시설 = 구매/기능제공 모델만 (ADR-007) — 자원 흐름 안 함
- 단일 서버 프로세스 (ADR-008) — 분산/샤딩 안 함
- 게임 회사 백엔드 포트폴리오 타겟 (ADR-009)
- **6월 캡스톤 1 = 옵션 C(2인 movement) Stretch / 옵션 B(1인) Fallback** (PRD 갱신)

### Harness 작동 원칙
- 학부생 멘토링 모드 (헌법 "사용자 컨텍스트" 섹션)
- 5단계 보고 템플릿 (모든 작업 후)
- 자동 실행 안 함 — 한 Phase 끝 → 보고 → 사용자 확인 → 다음 Phase 수동
- 학습 일지: 본인이 쓰고 AI는 인터뷰만 (가짜 학습 방지)

---

## 대화 흐름 패턴 (다음 Claude를 위해)

지난 두 세션에서 효과적이었던 패턴들:

### 결정 사항을 모을 때
- `ask_user_input_v0` 도구로 2~3개 질문 던짐 (한 번에 다 묻지 말고)
- 답변 받기 전 미리 추천 + 이유 공유 (사용자가 답하기 쉽게)
- 답변 받은 후 "거기 맞춰 진행"

### 새로운 정보가 드러났을 때
- 톤 전환을 두려워하지 않음. "어, 이러면 가정이 깨지네"로 명확히 짚기
- 예: 사용자가 "6월 마감"을 알려줬을 때, 그 전에 짠 PRD가 안 맞는다고 즉시 인정
- 사실 정정 후 새 정보 기반으로 다시 정렬

### 사용자 직감 존중
- 사용자가 "복잡해지지 않을까?"라고 직감하면 그 직감 자체를 시니어 안목으로 인정
- 추가 작업 전 비용/이득 솔직하게 짚어주기
- "안 하는 게 맞을 수도 있어요"도 정직하게 말함

### Harness 자체의 진화
- 사용자가 외부 자료 가져오면 "내 거랑 비교해줘"는 대환영
- 발견된 약점은 즉시 헌법/Phase/hook에 박는 행동까지
- 한 번 박으면 나중에 같은 실수 반복 안 됨

---

## 만들어진 자산 (참조용)

```
project-root/
├── CLAUDE.md                          ← 헌법 (가장 먼저 읽기)
├── CONTEXT.md                         ← 이 파일
├── START_HERE.md                      ← 첫날 가이드
├── docs/
│   ├── PRD.md                         ← Dawnholder 1차 작성됨
│   ├── ARCHITECTURE.md
│   ├── ADR.md                         ← ADR-001~009
│   └── learning-journal/              ← 3종 템플릿 + README
├── phases/
│   ├── README.md / _template.md
│   └── M1-foundation/                 ← 4개 Phase 작성됨
├── .claude/
│   ├── agents/                        ← 6개: netcode/gameplay/client/content/persistence/qa-sim
│   ├── commands/                      ← 12개 슬래시 커맨드 (/log-session 포함, PR #2)
│   ├── hooks/                         ← 2개
│   └── settings.json
└── (외부 산출물)
    ├── dawnholder-team-meeting-brief.html  ← 팀 미팅용
    └── dawnholder-weekly-plan.pdf          ← 다음 7일 플랜
```

### 슬래시 커맨드 빠른 참조

**학습용**: `/why`, `/explain`, `/concept`, `/recap`, `/dumb-it-down`
**작업용**: `/plan`, `/review`, `/new-packet`, `/new-monster`, `/load-test`
**일지**: `/journal-phase`, `/journal-concept`, `/journal-bug`
**기록**: `/log-session` (PR #2, 첫 실호출 테스트 대기)

---

## 미해결 질문 / 결정 보류 중인 것

이 항목들은 후속 작업에서 결정됨. 다음 Claude는 사용자가 관련 주제 꺼낼 때 이 목록 참조하세요.

**해결된 것 (2026-05-06)**:
- ✅ MES 방향 → 별도 레포 + 별도 헌법 (ADR-011 외, 골격은 정우 합류 직전 작성)
- ✅ ServerDev 코드 채택 여부 → 시나리오 B (ADR-011)
- ✅ Shared 코드 공유 방식 → DLL + Embedded PDB (ADR-010)

**남은 미해결 질문**:
- [ ] **캡스톤 1 발표 정확한 날짜** — 6월 중순 가정. 학교 일정 확정 시 PRD 갱신.
- [ ] **인증 방식**: 단순 닉네임 → JWT? 세션? (ADR-012 후보)
- [ ] **캐릭터 데이터 스키마**: 정규화 vs JSONB (ADR-013 후보)
- [ ] **채팅 시스템**: TCP로 전송 vs 별도 채널 (ADR-014 후보) — MVP 후
- [ ] **로그 저장**: 로컬 파일 vs 외부 sink (ADR-015 후보)
- [ ] **헤드리스 봇 자동화 방식** (ADR-016 후보)
- [ ] **정우 Anthropic 학생 할인/저렴한 플랜** — 합류 직전에 같이 알아보기

---

## 다음 Claude를 위한 마지막 안내

이 문서를 읽고 사용자와 처음 대화 시작할 때:

1. **이 문서를 읽었음을 짧게 확인**. "CONTEXT.md 잘 읽었어요. {현재 멈춤 지점} 이어서 가는 거 맞죠?" 정도.
2. **정식 보고 형식 안 씀** (5단계 보고는 코드 작업 후에만).
3. **사용자가 막 던지는 짧은 메시지**(예: "이번 주는 힘드네")에는 짧게 공감 + 다음 액션 가볍게 짚기.
4. **사용자가 새 정보 가져오면** (예: 미팅 결과) 거기 맞춰 즉시 재정렬.
5. **이 문서 자체도 살아있는 문서**. 큰 변화 있으면 이 문서를 갱신하세요. 갱신 시 맨 아래 "갱신 이력" 섹션에 한 줄.

---

## 갱신 이력

| 날짜 | 누가 | 무엇 |
|------|------|------|
| (셋업 다음 날) | Claude (이전 세션) | 최초 작성 — 두 세션 맥락 핸드오프 |
| 2026-05-06 | Claude (2번째 세션) | 미팅 결과 박제 + ServerDev 4월 코드 분석 + 시나리오 B 결정 + ADR-001/002 갱신 + ADR-010/011 신규 + PRD 캡스톤 1 섹션 추가 + Phase 01 재정의 |
| 2026-05-09 | Claude (3번째 세션) | PR #1 미머지 상태에서 `/log-session` 슬래시 커맨드 구현 → PR #2 (방법 B = main 기준 별개 PR). 묶음 B는 사용자 "잠시 대기" 결정. PR 상태 섹션 추가. |
| 2026-05-09 (후속) | Claude (4번째 세션) | PR #1·#2 main에 `--no-ff` 머지 + 원격 브랜치/워크트리 정리. main이 최신 상태로 도달. CONTEXT의 PR 미머지 표기 → 머지 완료로 갱신. |
| 2026-05-09 (후속2) | Claude (4번째 세션 연장) | Harness 정리: `docs/commands-index.md` 신규(14개 카탈로그), `/review` 5축으로 보강(테스트 강제 영역 + 도메인 패턴), `docs/skill-specs/log-session.md` 삭제(스킬 완성됨), CLAUDE.md 가리키는 경로 갱신. Hook 보강(3번 작업)은 Phase 02 진입 직전으로 미룸 — YAGNI. |
