# Dawnholder

![banner](00_Document/assets/readme-banner.png)

> .NET 10 권위 서버와 Unity 6 클라이언트로 구현한 2D MMORPG 프로토타입.
> KNUT 4인 캡스톤 프로젝트에서 서버와 AI 협업 환경을 설계·구현했습니다. (팀장 유영호)

---

## 프로젝트 소개

**Dawnholder**는 RPG 전투와 길드 타이쿤 요소를 결합한 2D MMORPG 프로토타입입니다. 서버가 이동과 전투를 판정하고, 클라이언트는 예측·재조정으로 조작 지연을 줄입니다. 로컬 환경에서 멀티플레이 전 과정을 시연할 수 있도록 구성했습니다.

게임 구현과 함께 Claude Code 기반의 **AI 협업 환경**도 설계했습니다. 저장소 규칙, 역할별 서브에이전트, 슬래시 커맨드, 자동 검증 훅, 작업 계획과 결과 기록을 통해 AI가 만든 변경을 검토하고 통제합니다.

### 핵심 구현

- **권위 서버**: 서버가 게임 상태를 소유하며 이동과 전투를 판정하고, 클라이언트는 입력과 화면 표현을 담당
- **클라이언트 예측·재조정**: 로컬 플레이어의 예측 경로와 원격 엔티티의 보간 경로를 분리
- **엔드투엔드 구성**: Unity 클라이언트, .NET 서버, MSSQL, PacketGenerator 기반 공유 프로토콜, CI/CD와 자동 검증 훅을 연동
- **AI 협업 환경**: 규칙, 역할별 에이전트, 명령어, 검증 훅, 지식 캐시를 5개 계층으로 분리
- **실행 데모**: self-contained GameServer + Unity 클라이언트 시연 빌드 (2026-06 캡스톤 1차 발표 완료)

### 게임 미리보기

![인게임 패럴랙스 배경 — CastleValley Sunset](00_Document/assets/readme-art-castlevalley.png)

*인게임 패럴랙스 배경 「CastleValley Sunset」. 7개 레이어로 분리한 소스는 `03_Client/Assets/Art/Environment/BackGround/Parallax/`에서 확인할 수 있습니다.*

| 플레이어블 — Knight | 플레이어블 — Mage |
|---|---|
| ![Knight idle 스프라이트 스트립](00_Document/assets/readme-art-knight.png) | ![Mage idle 스프라이트 스트립](00_Document/assets/readme-art-mage.png) |

*캐릭터 idle 스프라이트 시트 일부. 김인규가 ComfyUI 기반 AI 파이프라인으로 제작했으며, 원본 시트·스킬 이펙트·NPC·보스 리소스는 `03_Client/Assets/Art/`에 있습니다.*

---

## 개발 환경 설정 — 팀원용

> 화면별 설명이 필요한 경우 [팀원 사용 가이드](00_Document/team-guide.html)를 브라우저에서 열어보세요. 아래 내용은 빠른 설정을 위한 요약입니다.

다음 네 단계로 개발 환경을 구성할 수 있습니다. `/setup` 명령이 환경을 검증하고 역할에 맞는 설정을 안내합니다.

### 1. 사전 설치

다음 도구를 설치합니다:

| 항목 | 다운로드 |
|---|---|
| Git for Windows | https://git-scm.com/download/win |
| .NET 10 SDK | https://dotnet.microsoft.com/download/dotnet/10.0 |
| MSSQL LocalDB (Express 또는 Developer 에디션, "LocalDB" 옵션 체크) | https://www.microsoft.com/sql-server/sql-server-downloads |
| VS Code | https://code.visualstudio.com/ |
| Claude Code (VS Code 확장) | VS Code 확장 패널에서 `claude-code` 검색 |

> 백엔드 개발자는 Visual Studio 또는 Rider를 사용할 수도 있지만, 이 가이드는 VS Code를 기준으로 합니다.

> Unity 클라이언트 개발자는 Unity Hub와 Unity 6 LTS `6000.4.7f1`도 설치해야 합니다.

### 2. 저장소 복제

한글이 없는 ASCII 경로에 저장소를 복제합니다. 한글 경로에서는 자동 검증 훅이 정상 동작하지 않을 수 있습니다([ADR-017](00_Document/ADR/tech-stack/ADR-017-ascii-path.md)).

```bash
cd /c/Dev    # 권장 위치
git clone <레포 URL> ClaudeDev
cd ClaudeDev
```

### 3. VS Code에서 Claude Code 실행

```bash
code .
```

VS Code 오른쪽 아래에 권장 확장 설치 알림이 표시되면 **Install All**을 선택합니다. C# Dev Kit, GitLens, Claude Code 등 협업에 필요한 확장이 설치됩니다.

설치 후 VS Code의 Claude Code 패널을 엽니다.

### 4. `/setup` 호출

Claude Code 채팅창에:

```
/setup
```

`/setup`은 다음 설정을 순서대로 처리합니다:

- 이름 입력 → 영문 식별자와 역할 결정
- 환경 검증 8단계 (Git Bash, .NET, MSSQL, VS Code 통합 터미널 등)
- 역할별 설정 (백엔드 또는 Unity 클라이언트)
- 개인 작업 공간 초기화 (작업 좌표 핀 — `.claude/state/current-pin.txt`)
- 개인 노션 페이지와 첫 작업 안내

---

## 매일 작업 흐름

```
세션 시작:   /session:start         (work-pin 좌표 인지 + 최근 변경 확인)
작업:        Phase 단위로 구현 및 검증
Phase 끝:    -DONE.md에 결과를 기록한 뒤
             /session:end          (commit + PR + 노션 기록 + 다음 작업 안내)
```

현재 사용하는 슬래시 커맨드 13개는 [`00_Document/commands-index.md`](00_Document/commands-index.md)에 정리되어 있습니다. 명령 체계의 변경 배경은 ADR-022와 ADR-025에서 확인할 수 있습니다.

---

## 협업 규칙

- **PR 기반 병합**: 팀원은 `main`에 직접 push하지 않고 PR로 변경을 병합합니다.
- **영역별 소유권**: [`.github/CODEOWNERS`](.github/CODEOWNERS)를 기준으로 담당 영역을 나눕니다. 백엔드는 팀장, 클라이언트는 김인규·정유현이 담당합니다.
- **하네스 변경 공유**: 규칙·ADR·하네스 변경은 [`.claude/CHANGELOG.md`](.claude/CHANGELOG.md)에 기록하며, `/session:start`가 이를 확인합니다.
- **로컬 작업 상태 분리**: `current-pin.txt`는 Git에서 제외하고 각 팀원이 세션 간 작업 위치를 이어받는 데 사용합니다(ADR-025).

---

## AI 협업 환경 — 5계층

AI가 만든 변경이 정해진 역할과 검증 절차를 거치도록 운영 요소를 다섯 계층으로 분리했습니다. 전체 규칙은 [`CLAUDE.md`](CLAUDE.md)에서 확인할 수 있습니다.

| 계층 | 자산 | 역할 |
|---|---|---|
| L1 — 규칙 | [`CLAUDE.md`](CLAUDE.md) + [`00_Document/policies/`](00_Document/policies/INDEX.md) | 반드시 지켜야 할 원칙과 11개 운영 정책을 분리해 관리 |
| L2 — 역할 | [`.claude/agents/`](.claude/agents/) | 구현 4개, 검토 2개, 조정·지식 관리 2개로 구성한 서브에이전트 8개 |
| L3 — 명령 | [`.claude/commands/`](.claude/commands/) | 작업·세션·점검·엔진·설정을 다루는 슬래시 커맨드 13개 |
| L4 — 검증 | [`.claude/hooks/`](.claude/hooks/) | 자동 검증 훅 9개 (dangerous-cmd-guard / tdd-guard / circuit-breaker / risk-detector / shared-discipline-guard / pin-injector / phase-gate-validator / convention-size-guard / reviewer-auto-trigger) |
| L5 — 지식 | [`.claude/knowledge/`](.claude/knowledge/) | 공통·서버·공유·클라이언트·QA 영역별 지식 캐시와 정리 에이전트 |

관련 문서:

- [`00_Document/PRD.md`](00_Document/PRD.md) — 무엇을 만드는지
- [`00_Document/ARCHITECTURE.md`](00_Document/ARCHITECTURE.md) — 어떻게 만드는지
- [`00_Document/ADR/`](00_Document/ADR/) — 주요 기술·운영 결정과 근거
- [`00_Document/policies/`](00_Document/policies/INDEX.md) — 11개 운영 정책
- [`00_Document/REVIEW_CHECKLIST.md`](00_Document/REVIEW_CHECKLIST.md) — reviewer 에이전트의 5개 점검 기준
- [`01_Phases/`](01_Phases/) — 팀원별 Phase 정의와 `-DONE` 결과 기록

---

## 폴더 구조

```
00_Document/        요구사항·아키텍처·ADR·운영 정책·검토 기준
01_Phases/          팀원별 작업 정의와 완료 기록
02_Server/          권위 서버 (.NET 10) — 팀장 단독
03_Client/          Unity 클라이언트 (6000.4.7f1) — 인규/유현 공유
04_ClientNet/       클라이언트용 소켓 라이브러리 (Y2 모델) — 팀장 단독
98_Shared/          서버·클라이언트 공유 프로토콜 (.NET Standard 2.1 DLL) — 팀장 단독
99_Tools/           PacketGenerator 등 도구 — 팀장 단독
.claude/            AI 협업 환경 (agents / commands / hooks / knowledge / templates / setup-steps)
.github/            CODEOWNERS + GitHub 설정
.vscode/            VS Code 협업 최소 셋 (Git Bash 통합 터미널, 자동 저장 등)
```

---

## 팀 구조

| 역할 | 이름 | 영역 |
|---|---|---|
| 팀장 | 유영호 (@bass131) | 백엔드 코어 + 하네스 + 문서 |
| 팀원 1 | 김인규 | Unity 클라이언트 아트 리소스 + 콘텐츠 (ComfyUI 활용) |
| 팀원 2 | 정유현 | Unity 클라이언트 UI/입력 + 콘텐츠 |
| 팀원 3 | 박정우 | MES 관제 시스템 (별도 레포 — ADR-011) |

---

## 일정

- ✅ **6월** — 캡스톤 1차 발표 완료 (self-contained GameServer + Unity 클라 시연)
- **11월 19일** — 졸업작품 본 마감 (현재 M7.x 진행 중)

---

## 문제 해결

| 상황 | 조치 |
|---|---|
| 환경 설정 | `/setup`을 다시 실행하거나 [팀원 사용 가이드](00_Document/team-guide.html) 확인 |
| 구현 중 개념 확인 | 관련 코드와 문서를 바탕으로 Claude Code에 질문 |
| Git/PR | 팀장(유영호)에게 문의 |
| 빌드 오류 | 오류 로그를 포함해 Claude Code에 질문 |
| 규칙·결정의 배경 | [`00_Document/ADR/INDEX.md`](00_Document/ADR/INDEX.md) 확인 |
| 회고 | 개인 노션에 자유 형식으로 기록 |

---

## 라이선스

이 저장소는 졸업작품 포트폴리오의 열람을 위해 공개했습니다.
별도 라이선스가 명시되지 않은 코드와 문서의 재사용 권한은 부여하지 않습니다 (all rights reserved).
