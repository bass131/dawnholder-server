# CONTEXT — 세션 핸드오프 (템플릿)

> **이 파일의 역할**: `.claude/templates/`에 위치한 **공유 템플릿**입니다.
> 팀원이 처음 합류했을 때 이 파일을 레포 루트의 `CONTEXT.md`로 복사한 뒤,
> 본인 정보(이름, 역할, 합류 시점 등)와 현재 게임 진행 상태를 채워서 사용합니다.
> `CONTEXT.md` 본체는 `.gitignore`로 무시되어 각자 보유 — 협업 셋업 파트 1 결정.
>
> **셋업 절차**: `/setup:*` 슬래시 커맨드가 자동으로 복사·초기 작성을 안내합니다.
> 수동 셋업 시: `cp .claude/templates/CONTEXT-template.md CONTEXT.md` 후 아래 placeholder 채우기.

---

## 이 문서를 읽는 Claude에게

헌법(`CLAUDE.md`)과 함께 가장 먼저 읽으세요. 이전 세션의 톤·결정을 잇기 위한
핸드오프 노트입니다 (헌법과 충돌 시 헌법이 이김).

**사용자에게**: 새 Claude Code 세션 시작 시 가장 먼저 읽힐 파일.

**유지 정책**: **누적이 아니라 응축**. ~200줄 넘으면 큰 마일스톤 끝날 때마다
처음부터 재작성. 옛 디테일은 `git history` + `00_Document/learning-journal/{본인-네임스페이스}/`
+ 노션 "Dawnholder 협업 히스토리"에서 찾기.

---

## TL;DR — Claude는 다음 톤으로 응답하세요

1. **학부생 멘토링 모드**. 시니어가 주니어 가르치듯 친절히. 전문 용어 첫 사용 시 풀이.
2. **trade-off 항상 설명**. "A 골랐어요"가 아니라 "A vs B 중 A, 이유는…, 단점은…".
3. **솔직함 우선**. 위험 미리 짚기. 마감 못 지킬 것 같으면 정직하게.
4. **5단계 보고**. 코드 작업 끝나면 🎯 무엇 / 🤔 왜 / 🛠️ 어떻게 / 🧪 테스트 / ➡️ 다음.
5. **Phase 완료 시 학습 일지 권유**. `/journal:phase` 등.

상세 톤 가이드: 헌법(`CLAUDE.md`) "사용자 컨텍스트" 섹션.

---

## 사용자 컨텍스트 (본인 정보 — 셋업 시 채워주세요)

- **신분**: <학부생 / 기타>
- **이름**: <본인 이름>
- **역할**: <백엔드 코어 / Unity 클라 아트 / Unity 클라 UI·입력 / MES>
- **합류 시점**: <YYYY-MM-DD>
- **언어**: 한국어 (대화). 코드/식별자 영어.
- **위치**: 한국 (KST).
- **목표**: <개인 학습 목표를 적으세요. 예: 게임 회사 백엔드 포지션 포트폴리오>
- **솔직함 패턴**: 모르는 건 모른다고, 마감 현실도 솔직히. AI도 솔직하게.
- **본인의 학습 일지 위치**: `00_Document/learning-journal/<본인-네임스페이스>/`
  (예: `youngho/`, `ingyu/`, `yuhyun/`, `jungwoo/`)

---

## 하드 일정 (프로젝트 공통)

- **6월** — 캡스톤 1 발표 (수업 중간, "진행 중" OK)
- **11월 19일** — 졸업작품 본 마감

→ Phase A (~6월): M1~M3 도달, 두 명 같은 맵 데모. Phase B (7~11월): M4~M8 MVP.

---

## 팀 구조 (2026-05-06 미팅 기준)

| 역할 | 이름 | 영역 | 합류 시점 |
|------|------|------|-----------|
| 팀장 | 유영호 | 백엔드 코어 | 진행 중 |
| 팀원 1 | 김인규 | Unity 클라 아트 리소스 및 컨텐츠 개발 | 6월 말 학기 후 |
| 팀원 2 | 정유현 | Unity 클라 UI/입력 및 컨텐츠 개발 | 6월 말 학기 후 |
| 팀원 3 | 박정우 | 관리 시스템 (MES, **별도 레포**) | 7월 이후 |

- 팀원 전원 개발 경험 거의 백지. 온보딩은 그 수준에서.
- **캡스톤 1 시점 = 팀장 단독 작업 가정** (팀원은 학습 마라톤 진행 중).

---

## ⏸️ 현재 진행 상황 (셋업 시점에 갱신해주세요)

**[합류 시점 기준 게임 진행 상태를 적으세요. 팀장에게 확인하거나
최근 `01_Phases/M{N}-*/{NN}-*-DONE.md` 파일들을 보고 채우기.]**

### 현재 마일스톤
- ✅ M1 Foundation — 완료
- 🔄 M2 First Connection — Phase 05까지 완료, Phase 06 진입 전
- 🔜 M3 Multiplayer
- 🔜 M4 Combat
- ...

### 본인이 합류한 직후 첫 작업
<예: "Unity 클라 입력 모듈 학습부터", "백엔드 마이그레이션 보조", "MES 레포 셋업">

---

## 본인의 다음 액션 (셋업 직후 채워주세요)

1. <첫 작업 시작 전 읽을 문서들 — 헌법, ARCHITECTURE, PRD 통독>
2. <자기 학습 일지 폴더 생성: `00_Document/learning-journal/<본인 네임스페이스>/`>
3. <첫 Phase가 있다면 `/work:plan`으로 분해>
4. ...

---

## 학습 일지 후보 (밀린 것 + 본인 추가)

본인이 학습하면서 일지로 박을 후보들을 여기 누적. Phase 완료 시 또는 시간 여유 시 작성:

- `/journal:concept <개념>` — 깊이 학습한 개념을 본인 말로
- `/journal:bug <사건>` — 막혔던 디버깅 사건 (디테일 안 잊었을 때)
- `/journal:phase` — Phase 통째 회고

---

## 보류 중 / 미해결 (본인 작업 중 발견한 것 누적)

<본인이 작업하다가 "이건 나중에 결정해야" 또는 "이건 막혀서 미루는 중" 항목들을 여기 적으세요.>

---

## 핵심 결정 요약 (ADR 박혀있음, 빠른 참조)

### 기술 스택
- Unity 6.4 LTS + .NET 10 LTS 권위 서버 (ADR-001 v3)
- Raw TCP + length-prefixed binary + **자체 PDL** (ADR-002)
- 모노레포 (단, MES는 별도 레포 — ADR-011)
- 20 TPS 서버 틱 (ADR-004)
- MSSQL (개발용 LocalDB, Windows 통합 인증) + EF Core 10 (ADR-005 v2)
- `98_Shared/` = .NET Standard 2.1 + DLL + Embedded PDB (ADR-010)
- Unity 클라 socket 분리 모델 — **Y2** (ADR-012)
- 프로젝트 폴더 ASCII 경로 — `C:\Dev\ClaudeDev` (ADR-017)

### 게임/스코프
- 두 장르 결합 MVP (RPG + 길드 타이쿤) — ADR-006
- 거점 시설 = 구매/기능 모델만 — ADR-007
- 단일 서버 프로세스 — ADR-008
- 게임 회사 백엔드 포트폴리오 — ADR-009

### Harness 작동 원칙
- 학부생 멘토링 + 5단계 보고 + `-DONE.md` 박제 + 학습 일지 권유 (헌법)
- 자동 실행 안 함 — Phase 끝 → 보고 → 사용자 확인 → 다음 Phase 수동
- **문서 세분화**: 사전형 .md 220줄 임계 + 헌법만 350줄 예외 (ADR-014)
- **박제 분업**: `-DONE.md` = AI 작성 / `learning-journal/` = 본인 작성 (ADR-013)
- **Post-flight 게이트**: `-DONE.md` Write/Edit 시 `validate-phase-gate.sh` (ADR-015)
- **작업 봉투 + 핀**: ADR-018 (망각 안전망)
- **훅 환경**: Git Bash on Windows + PATH 셋업 필수 (ADR-020)

전체 ADR 목록 = `00_Document/ADR/INDEX.md` 참조.

---

## 자산 위치 (빠른 참조)

폴더는 탐색기 정렬 고정용 숫자 prefix를 갖습니다.

```
00_Document/PRD.md, ARCHITECTURE.md, ADR.md     ← 결정·구조 (ADR.md는 thin landing)
00_Document/ADR/{tech-stack,gameplay,harness}/   ← ADR 본문 카테고리별
00_Document/ADR/INDEX.md                         ← ADR 전체 목록 + 1줄 요약
00_Document/ADR_History.md                       ← ADR 변경 이력 (외부화)
00_Document/commands-index.md                    ← 슬래시 커맨드 카탈로그
00_Document/learning-journal/<본인 네임스페이스>/  ← Phase 학습 일지 (각자)
  └── concepts/                                  ← 개념 일지 (각자)
01_Phases/M{N}-{slug}/                           ← 작업 단위 ({NN}-*.md 정의 + {NN}-*-DONE.md 박제 페어)
02_Server/, 03_Client/, 98_Shared/, 99_Tools/    ← 게임 코드
04_ClientNet/                                    ← 클라용 socket 라이브러리 (Y2)
.claude/agents/                                  ← 서브에이전트
.claude/commands/{learn,journal,work,session,setup}/  ← 슬래시 커맨드
.claude/hooks/                                   ← 자동 검증 훅
.claude/templates/                               ← 공유 템플릿 (본 파일 포함)
.claude/state/current-pin.txt                    ← 각자의 작업 좌표 (.gitignore)
Dawnholder.slnx                                  ← .NET 솔루션 (02_Server + 04_ClientNet + 98_Shared)
global.json                                      ← .NET SDK 핀 (10.0.203+)
노션 "Dawnholder 협업 히스토리" DB               ← 세션 STAR 박제
```

---

## 다음 Claude를 위한 마지막 안내

1. **이 문서 + 헌법 통독 후 짧게 인지 확인**:
   "CONTEXT 잘 읽었어요. 본인은 <역할>이고 현재 <마일스톤 상태>인 것 맞죠?"
2. **5단계 보고는 코드 작업 후에만** (대화/의논엔 안 씀).
3. **사용자가 던지는 짧은 메시지**엔 짧게 공감 + 다음 액션 가볍게.
4. **새 정보**(미팅 결과, Unity 업데이트 등)가 들어오면 즉시 재정렬.
5. **이 문서는 살아있는 응축본**. 큰 변화 시 갱신하되 누적 X — 정책은 맨 위 참조.

---

## 갱신 이력

> 이력은 `CONTEXT_History.md` 참조 (헌법: 문서 세분화 정책 — 누적 섹션 외부화).
> 새 갱신 발생 시 본 파일이 아니라 `CONTEXT_History.md`에 한 줄씩 추가.
