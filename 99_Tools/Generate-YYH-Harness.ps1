param(
    [string]$TargetRoot = "C:\Dev\YYH_Harness"
)

$ErrorActionPreference = "Stop"

function Write-HarnessFile {
    param(
        [string]$RelativePath,
        [string]$Content
    )

    $path = Join-Path $TargetRoot $RelativePath
    $dir = Split-Path -Parent $path
    if (-not (Test-Path -LiteralPath $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    if ($RelativePath.EndsWith(".sh")) {
        $Content = $Content -replace "`r`n", "`n"
    }

    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($path, $Content, $utf8NoBom)
}

if (-not (Test-Path -LiteralPath $TargetRoot)) {
    New-Item -ItemType Directory -Path $TargetRoot -Force | Out-Null
}

$files = [ordered]@{}

$files["README.md"] = @'
# YYH Harness

범용 AI Agent Harness 템플릿입니다. Claude Code 같은 AI 코딩 환경에서 프로젝트 운영 규칙, 작업 분해, 리뷰, 위험 감지, 세션 핀, 완료 박제를 한 세트로 이식하기 위한 기본 골격입니다.

## 목표

- 신규 프로젝트에 빠르게 AI 협업 운영체계를 심는다.
- 이미 진행 중인 프로젝트에는 기존 구조를 보존하면서 하네스만 얹는다.
- 프로젝트별 도메인 규칙은 `harness.config.json`과 루트 `CLAUDE.md`에서 분리해 관리한다.

## 먼저 알아둘 것

이 템플릿은 애플리케이션 프레임워크가 아니라 **AI 협업 운영 레이어**입니다. 기존 소스 코드를 옮기거나 빌드 시스템을 바꾸지 않습니다. 대신 다음을 프로젝트 루트에 추가합니다.

- AI가 매번 읽을 운영 규칙
- 작업을 Phase로 쪼개는 문서 구조
- 세션 간 작업 좌표를 보존하는 pin
- 위험 작업을 드러내는 hooks
- 리뷰, 계획, QA용 agent/command 골격

프로젝트 고유 규칙은 `CLAUDE.md`와 `harness.config.json`에 적고, 범용 운영 규칙은 `00_Document/policies/`에 둡니다.

## 구성

```text
.claude/                 Claude Code 설정, 명령, 에이전트, 훅, 템플릿
00_Document/             프로젝트 문서, 정책, ADR, 원장
01_Phases/               작업 분해 단위
tools/                   설치, 감사, 동기화 스크립트
CLAUDE.template.md       루트 CLAUDE.md 생성용 템플릿
harness.config.template.json
harness.config.guide.md  config 작성 가이드
```

## 신규 프로젝트 빠른 시작

```powershell
cd C:\Dev\YYH_Harness
Copy-Item harness.config.template.json harness.config.json
notepad harness.config.json
.\tools\install.ps1 -ProjectRoot C:\Dev\YourProject
```

설치 후 대상 프로젝트의 `CLAUDE.md`, `harness.config.json`, `00_Document/PRD.md`를 프로젝트에 맞게 채우세요.

## 기존 프로젝트에 얹기

기존 프로젝트에는 먼저 감사부터 실행합니다.

```powershell
cd C:\Dev\YYH_Harness
.\tools\audit-project.ps1 -ProjectRoot C:\Dev\ExistingProject
.\tools\install.ps1 -ProjectRoot C:\Dev\ExistingProject
```

기본 설치는 기존 파일을 덮어쓰지 않습니다. 이미 같은 경로의 파일이 있으면 유지하고, 누락된 파일만 추가합니다. 템플릿 기준으로 강제 갱신해야 할 때만 `-Force`를 붙입니다.

```powershell
.\tools\install.ps1 -ProjectRoot C:\Dev\ExistingProject -Force
```

`-Force`는 `CLAUDE.md`, `harness.config.json`, `00_Document/PRD.md`, `.claude/commands`, `.claude/hooks` 같은 파일도 덮어쓸 수 있으므로 적용 전 `git status`와 diff를 확인하세요.

## 첫 세션 흐름

설치 후 첫 사용은 다음 순서가 가장 안정적입니다.

1. `harness.config.json`에서 source/test/build/smoke/risk 경계를 채웁니다.
2. `CLAUDE.md`의 `User Context`와 `Project Constitution`을 프로젝트에 맞게 채웁니다.
3. `00_Document/PRD.md`와 `00_Document/ARCHITECTURE.md`에 현재 목표와 구조를 적습니다.
4. Claude Code에서 `/setup` 또는 `/session:start`를 호출합니다.
5. 큰 목표는 `/work:plan <goal>`로 Phase로 쪼갭니다.
6. 구현 완료 후 build/test/smoke를 실행하고, 복잡 이상은 `-DONE.md`로 박제합니다.

## 이식 원칙

- 범용 템플릿은 가능한 한 프로젝트 도메인 지식을 갖지 않습니다.
- 프로젝트 도메인 규칙은 `CLAUDE.md`의 "Project Constitution" 섹션에 둡니다.
- 변경 중인 작업 상태는 `.claude/state/`에만 둡니다. 이 폴더는 템플릿 동기화 대상이 아닙니다.
- 위험 경계, 빌드, 테스트, 배포 명령은 반드시 프로젝트별 config로 선언합니다.

## 템플릿 갱신

이미 설치된 프로젝트에 최신 템플릿의 commands/hooks/policies만 반영하려면:

```powershell
.\tools\sync-harness.ps1 -ProjectRoot C:\Dev\ExistingProject
```

기본 sync는 기존 파일을 덮어쓰지 않습니다. 템플릿 기준으로 갱신해야 할 때만 `-Force`를 사용합니다.

## 어디를 수정해야 하나

| 파일 | 언제 수정하나 |
|---|---|
| `CLAUDE.md` | 프로젝트의 절대 규칙, 사용자 컨텍스트, repo layout |
| `harness.config.json` | 경로, 검증 명령, 위험 경계 |
| `00_Document/PRD.md` | 무엇을 만들지와 만들지 않을 것 |
| `00_Document/ARCHITECTURE.md` | 시스템 구조와 데이터 흐름 |
| `.claude/agents/*` | 프로젝트에 특화된 agent가 필요할 때 |
| `.claude/hooks/*` | 위험 탐지 패턴을 프로젝트에 맞게 조정할 때 |

자세한 config 작성법은 `harness.config.guide.md`를 보세요.
'@

$files["harness.config.template.json"] = @'
{
  "project": {
    "name": "{{PROJECT_NAME}}",
    "type": "generic-software",
    "primary_language": "{{LANGUAGE}}",
    "owner_namespace": "{{OWNER}}"
  },
  "paths": {
    "docs": "00_Document",
    "phases": "01_Phases",
    "source": ["src"],
    "tests": ["tests"],
    "tools": ["tools"]
  },
  "verification": {
    "build": "{{BUILD_COMMAND}}",
    "test": "{{TEST_COMMAND}}",
    "smoke": "{{SMOKE_COMMAND}}"
  },
  "risk_flags": {
    "trust_boundary": ["auth", "security", "payment", "permissions", "network/handlers"],
    "irreversible_commands": ["git push", "gh pr merge", "db migrate", "terraform apply"],
    "contract_paths": ["api", "protocol", "schema", "migrations"],
    "asset_or_visual_paths": ["assets", "public", "ui"]
  },
  "agents": {
    "default_worker": "implementer",
    "reviewer": "reviewer",
    "planner": "planner",
    "qa": "qa"
  }
}
'@

$files["harness.config.guide.md"] = @'
# harness.config 작성 가이드

`harness.config.json`은 범용 하네스를 특정 프로젝트에 맞게 연결하는 어댑터입니다. Claude가 이 파일을 읽고 어디를 수정해야 하는지, 어떤 명령으로 검증해야 하는지, 어디서 멈춰야 하는지를 판단합니다.

## 작성 순서

1. `project`에 프로젝트 이름, 유형, 주 언어, 기본 owner namespace를 적습니다.
2. `paths`에 실제 소스, 테스트, 도구 폴더를 적습니다.
3. `verification`에 로컬에서 실행 가능한 build/test/smoke 명령을 적습니다.
4. `risk_flags`에 사람 승인 또는 추가 리뷰가 필요한 경로와 명령을 적습니다.
5. agent 이름을 바꾸지 않았다면 `agents`는 기본값으로 둡니다.

## 필드 설명

### project

```json
{
  "name": "MyApi",
  "type": "backend-api",
  "primary_language": "TypeScript",
  "owner_namespace": "youngho"
}
```

- `name`: `CLAUDE.md`, Phase 경로, 보고 문서에서 쓰는 프로젝트명입니다.
- `type`: `backend-api`, `web-app`, `unity-game`, `library`, `data-pipeline`처럼 판단에 도움 되는 분류입니다.
- `primary_language`: 가장 많이 수정할 언어입니다.
- `owner_namespace`: `01_Phases/<owner>/...`에 들어갈 기본 작업자 이름입니다.

### paths

```json
{
  "docs": "00_Document",
  "phases": "01_Phases",
  "source": ["src", "apps/api"],
  "tests": ["tests", "src/**/*.test.ts"],
  "tools": ["tools", "scripts"]
}
```

- 실제 repo에 없는 기본값은 반드시 바꾸세요.
- monorepo라면 여러 source/test 경로를 넣습니다.
- generated output, build artifact, vendor 폴더는 넣지 않습니다.

### verification

```json
{
  "build": "npm run build",
  "test": "npm test",
  "smoke": "npm run smoke"
}
```

- 빈 값 대신 실제 명령을 적습니다.
- 아직 smoke가 없다면 `"smoke": ""`처럼 비워두고 PRD나 Phase에 생성 계획을 남깁니다.
- 명령은 로컬 개발자 PC에서 재현 가능해야 합니다.

### risk_flags

```json
{
  "trust_boundary": ["src/auth", "src/payments", "src/api/middleware"],
  "irreversible_commands": ["git push", "gh pr merge", "npm publish", "terraform apply"],
  "contract_paths": ["openapi", "prisma/schema.prisma", "migrations"],
  "asset_or_visual_paths": ["src/ui", "public", "assets"]
}
```

- `trust_boundary`: 외부 입력, 인증, 권한, 결제, 개인정보, 보안 검증이 있는 경로입니다.
- `irreversible_commands`: 실행하면 외부 상태가 바뀌는 명령입니다. AI는 여기서 멈춰야 합니다.
- `contract_paths`: 공개 API, DB schema, protocol, generated type처럼 호환성이 중요한 경로입니다.
- `asset_or_visual_paths`: 사람의 육안 확인이 필요한 UI, 디자인, 에셋 경로입니다.

## 예시: .NET 백엔드

```json
{
  "project": {
    "name": "OrderService",
    "type": "backend-api",
    "primary_language": "C#",
    "owner_namespace": "youngho"
  },
  "paths": {
    "docs": "00_Document",
    "phases": "01_Phases",
    "source": ["src"],
    "tests": ["tests"],
    "tools": ["tools"]
  },
  "verification": {
    "build": "dotnet build",
    "test": "dotnet test",
    "smoke": ""
  },
  "risk_flags": {
    "trust_boundary": ["src/Auth", "src/Controllers", "src/Middleware"],
    "irreversible_commands": ["git push", "gh pr merge", "dotnet ef database update"],
    "contract_paths": ["src/Contracts", "migrations", "openapi"],
    "asset_or_visual_paths": []
  },
  "agents": {
    "default_worker": "implementer",
    "reviewer": "reviewer",
    "planner": "planner",
    "qa": "qa"
  }
}
```

## 예시: 프론트엔드 앱

```json
{
  "project": {
    "name": "Dashboard",
    "type": "web-app",
    "primary_language": "TypeScript",
    "owner_namespace": "youngho"
  },
  "paths": {
    "docs": "00_Document",
    "phases": "01_Phases",
    "source": ["src", "app"],
    "tests": ["tests", "src/**/*.test.tsx"],
    "tools": ["scripts"]
  },
  "verification": {
    "build": "npm run build",
    "test": "npm test",
    "smoke": "npm run e2e"
  },
  "risk_flags": {
    "trust_boundary": ["src/auth", "src/api", "app/api"],
    "irreversible_commands": ["git push", "gh pr merge", "npm publish"],
    "contract_paths": ["openapi", "src/api/contracts"],
    "asset_or_visual_paths": ["src/components", "src/pages", "public", "assets"]
  },
  "agents": {
    "default_worker": "implementer",
    "reviewer": "reviewer",
    "planner": "planner",
    "qa": "qa"
  }
}
```

## 흔한 실수

- `source`를 기본값 `src`로 둔 채 실제 repo에는 `app`만 있는 경우
- `test` 명령이 오래 걸리거나 외부 서비스를 요구하는 경우
- `irreversible_commands`에 배포, publish, migration 명령을 빼먹는 경우
- UI 프로젝트인데 `asset_or_visual_paths`를 비워두는 경우
- generated file을 source로 넣어 AI가 직접 수정하게 만드는 경우
'@

$files["CLAUDE.template.md"] = @'
# {{PROJECT_NAME}} AI Agent Harness

이 문서는 프로젝트에서 AI Agent가 매 작업 전에 따라야 하는 최상위 운영 규칙입니다. 세부 정책은 `00_Document/policies/`에 두고, 이 파일에는 매 응답마다 떠올려야 하는 원칙만 둡니다.

## User Context

- 사용자의 목표, 수준, 선호 응답 톤을 여기에 적습니다.
- 전문 용어는 처음 사용할 때 풀어씁니다.
- 결정에는 trade-off를 함께 설명합니다.
- 불확실하면 추측을 사실처럼 쓰지 말고 확인합니다.

## Project Constitution

프로젝트 고유의 절대 원칙을 여기에 적습니다.

예시:
- 사용자 데이터와 인증 경계는 항상 trust boundary로 취급한다.
- 공개 API, DB schema, wire protocol 변경은 compatibility 검토 없이는 진행하지 않는다.
- 배포, push, merge, migration, billing 관련 작업은 사용자 승인 전에는 실행하지 않는다.

## Repo Layout

`harness.config.json`의 `paths` 값을 기준으로 최신 구조를 유지합니다.

```text
00_Document/   문서, 정책, ADR
01_Phases/     작업 분해와 완료 박제
.claude/       AI Agent 설정, 명령, 훅, 에이전트
```

## Operating Mode

- 사람은 방향, 우선순위, 판단 게이트를 맡습니다.
- AI Agent는 조사, 구현, 검증, 문서화를 진행합니다.
- 비가역 작업은 반드시 멈추고 사용자 승인을 받습니다.
- 작업 좌표는 `.claude/state/current-pin.txt`에 유지합니다.

## Work Grades

작업은 `단순 / 보통 / 복잡 / 대규모`로 분류합니다.

- 단순: 한 파일, 작은 수정, 가역적
- 보통: 한 도메인, 2~3파일, 제한적 영향
- 복잡: 여러 도메인, 설계 판단 또는 검증 부담 있음
- 대규모: 광범위 변경, 비가역 또는 cross-cutting 영향

세부 기준은 `00_Document/policies/grade-and-risk.md`를 따릅니다.

## Required Gates

- 작업 시작: `/session:start` 또는 현재 pin 확인
- 큰 목표: `/work:plan <goal>`
- 구현 완료: 프로젝트 config의 build/test/smoke 중 해당 항목 실행
- 복잡 이상 완료: `-DONE.md` 작성
- PR, merge, push, migration, production deploy: 사용자 명시 승인 필요

## Conflict Priority

`CLAUDE.md` > `00_Document/ADR/` > `00_Document/policies/` > `00_Document/ARCHITECTURE.md` > `00_Document/PRD.md`
'@

$files[".gitignore"] = @'
.claude/state/*
!.claude/state/.gitkeep
harness.config.json
*.log
'@

$files[".claude/settings.json"] = @'
{
  "permissions": {
    "allow": [
      "Read(**)",
      "Glob(**)",
      "Grep(**)",
      "Bash(git status)",
      "Bash(git diff*)",
      "Bash(git log*)",
      "Bash(git show*)",
      "Bash(ls*)",
      "Bash(cat*)"
    ],
    "ask": [
      "Bash(git push*)",
      "Bash(gh pr create*)",
      "Bash(gh pr merge*)",
      "Bash(*migrate*)",
      "Bash(*deploy*)"
    ],
    "deny": [
      "Bash(curl*)",
      "Bash(wget*)",
      "Read(**/.env)",
      "Read(**/.env.*)",
      "Read(**/secrets/**)",
      "Read(**/*secret*)",
      "Read(**/*credential*)"
    ]
  },
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "Bash",
        "hooks": [
          { "type": "command", "command": "$CLAUDE_PROJECT_DIR/.claude/hooks/dangerous-cmd-guard.sh" },
          { "type": "command", "command": "$CLAUDE_PROJECT_DIR/.claude/hooks/risk-detector.sh" }
        ]
      },
      {
        "matcher": "Edit|Write",
        "hooks": [
          { "type": "command", "command": "$CLAUDE_PROJECT_DIR/.claude/hooks/tdd-guard.sh" },
          { "type": "command", "command": "$CLAUDE_PROJECT_DIR/.claude/hooks/risk-detector.sh" }
        ]
      }
    ],
    "PostToolUse": [
      {
        "matcher": "Edit|Write",
        "hooks": [
          { "type": "command", "command": "$CLAUDE_PROJECT_DIR/.claude/hooks/phase-gate-validator.sh" },
          { "type": "command", "command": "$CLAUDE_PROJECT_DIR/.claude/hooks/reviewer-auto-trigger.sh" },
          { "type": "command", "command": "$CLAUDE_PROJECT_DIR/.claude/hooks/convention-size-guard.sh" }
        ]
      },
      {
        "matcher": "",
        "hooks": [
          { "type": "command", "command": "$CLAUDE_PROJECT_DIR/.claude/hooks/circuit-breaker.sh" }
        ]
      }
    ],
    "UserPromptSubmit": [
      {
        "matcher": "",
        "hooks": [
          { "type": "command", "command": "$CLAUDE_PROJECT_DIR/.claude/hooks/pin-injector.sh" }
        ]
      }
    ]
  }
}
'@

$files[".claude/CHANGELOG.md"] = @'
# Harness Changelog

## Unreleased

- Initial generic YYH Harness template.
'@

$files[".claude/state/.gitkeep"] = @'
'@

$files[".claude/templates/pin-template.txt"] = @'
WORK-ID: <phase-slug 또는 ad-hoc-YYYYMMDD-topic>
PHASE: <NN/MM 또는 ad-hoc> / 등급: <단순/보통/복잡/대규모>
현재 작업: <지금 무엇을 하는지 한 줄>
다음 액션: <구체적인 다음 한 걸음>
주의할 약속: <검증/제약/사용자 게이트>
루프 상태: <bucket-a/b/c 또는 none>
마지막 갱신: <YYYY-MM-DD 또는 commit hash>
'@

$files[".claude/templates/done-md-template.md"] = @'
---
summary: <무엇을 했고 무엇이 가능해졌는지 1줄>
phase: <NN-phase-name>
work-id: <work-id>
status: done
completed_at: <YYYY-MM-DD>
commit: <short-hash-or-pending>
---

# Phase <NN> 완료 박제

## TL;DR

2~4문장으로 사실만 요약합니다.

## 5단계 보고

- **무엇을 만들었나**:
- **왜 필요한가**:
- **어떻게 만들었나**:
- **테스트 결과**:
- **다음 스텝**:

## AC 검증 결과

실행한 명령과 결과를 적습니다. 추측으로 통과 처리하지 않습니다.

```bash
$ <build-or-test-command>
<result>
```

## 결정 흐름

- 대안:
- 채택:
- 이유:
- 단점:

## 막혔던 지점

- 증상:
- 원인:
- 해결:

## 검색 키워드

- <keyword>
'@

$files[".claude/knowledge/README.md"] = @'
# Knowledge

AI가 자주 참조하는 프로젝트 지식을 저장하는 캐시입니다. 처음에는 비워두고, 반복해서 필요한 정보가 생길 때만 추가합니다.

사용 기준은 `00_Document/policies/knowledge-system.md`를 따릅니다.

## 기본 원칙

- 비밀값, 토큰, 개인정보를 넣지 않습니다.
- 검증되지 않은 추측을 사실처럼 넣지 않습니다.
- 프로젝트 전체 규칙은 knowledge가 아니라 `CLAUDE.md` 또는 policy로 승격합니다.
- 설계 결정은 ADR로 승격합니다.
'@

$files[".claude/knowledge/_usage.md"] = @'
# Knowledge Usage

## 언제 읽나

- 같은 설명을 여러 번 반복하게 될 때
- 특정 모듈의 변경 순서가 자주 헷갈릴 때
- build/test 절차가 프로젝트 특유의 예외를 가질 때

## 언제 추가하나

- 사용자가 명시적으로 요청할 때
- 작업 중 같은 지식이 2회 이상 재사용될 때
- `pending-knowledge.md`에서 승격하기로 결정했을 때

## 작성 형식

```markdown
# <topic>

상태: active | superseded
마지막 확인: <YYYY-MM-DD>

## 요약

## 적용 위치

## 주의점

## 관련 문서
```
'@

$files[".claude/agents/_routing.md"] = @'
# Agent Routing

기본 에이전트 풀은 범용 이름을 사용합니다.

| Agent | 역할 | 권한 |
|---|---|---|
| planner | 목표를 Phase로 쪼개고 AC를 검토 | Read only |
| implementer | 일반 구현 작업 | Source read/write |
| qa | 테스트, smoke, 회귀, 데이터 검증 | Tests/tools read/write |
| reviewer | 코드/문서 리뷰, 위험 탐지 | Read only |
| docs | 문서와 ADR 정리 | Docs read/write |
| coordinator | 복잡/대규모 작업 분해와 통합 | Read only + 위임 |
| knowledge-gc | `.claude/knowledge` 정리 | Knowledge read/write |

프로젝트 특화 agent가 필요하면 이 표를 확장하되, 범용 이름은 유지하는 편이 이식성이 좋습니다.
'@

$files[".claude/agents/planner.md"] = @'
---
name: planner
description: 목표를 Phase 단위로 분해하고 완료 조건을 검증하는 계획 에이전트
---

너는 작업 계획 검토자다. 목표를 작은 Phase로 나누고, 각 Phase가 독립적으로 검증 가능한지 확인한다.

출력에는 다음을 포함한다.
- Phase 목록
- 각 Phase의 완료 조건
- 선행 조건
- 위험 깃발
- 사람 판단이 필요한 게이트

코드는 수정하지 않는다.
'@

$files[".claude/agents/implementer.md"] = @'
---
name: implementer
description: 일반 구현 작업을 수행하는 기본 워커
---

너는 구현 담당 에이전트다. 기존 코드 스타일을 먼저 읽고, 최소 범위로 수정하며, 검증 명령을 제안하거나 실행한다.

원칙:
- 프로젝트의 기존 패턴을 따른다.
- 범위 밖 리팩터링을 하지 않는다.
- 보안, 인증, 결제, 마이그레이션, 배포는 trust boundary로 보고 멈춘다.
- 수정 후 어떤 테스트가 필요한지 명확히 남긴다.
'@

$files[".claude/agents/qa.md"] = @'
---
name: qa
description: 테스트, 회귀, smoke, 재현 시나리오 담당
---

너는 QA 에이전트다. 실패를 재현하고, 가장 작은 검증 루프를 만든다.

출력에는 다음을 포함한다.
- 재현 절차
- 실행 명령
- 관찰 결과
- 실패 원인 후보
- 추가 테스트 제안
'@

$files[".claude/agents/reviewer.md"] = @'
---
name: reviewer
description: 변경사항을 리뷰하고 위험, 회귀, 누락 테스트를 찾는 읽기 전용 에이전트
---

너는 코드 리뷰어다. 칭찬보다 결함을 먼저 찾는다.

우선순위:
1. 실제 버그
2. 보안/권한/데이터 손상 위험
3. 호환성 깨짐
4. 누락된 테스트
5. 유지보수성 문제

출력은 severity 순서로 `file:line` 근거를 포함한다. 수정은 하지 않는다.
'@

$files[".claude/agents/docs.md"] = @'
---
name: docs
description: 문서, ADR, 정책, 완료 박제를 정리하는 에이전트
---

너는 문서 담당 에이전트다. 문서는 짧고 검색 가능하게 유지한다.

원칙:
- 정책과 프로젝트 사실을 섞지 않는다.
- 결정에는 이유와 trade-off를 적는다.
- 오래된 문서는 삭제보다 archive 또는 ADR supersede를 선호한다.
'@

$files[".claude/agents/coordinator.md"] = @'
---
name: coordinator
description: 복잡/대규모 작업을 분해하고 워커 결과를 통합하는 에이전트
---

너는 조율자다. 직접 구현보다 분해, 의존성 정리, 결과 통합을 맡는다.

출력:
- 작업 분해
- 위임 대상
- 병렬 가능 여부
- 통합 순서
- 사람 게이트
- 최종 검증 명령
'@

$files[".claude/agents/knowledge-gc.md"] = @'
---
name: knowledge-gc
description: `.claude/knowledge`를 정리하는 수동 트리거 에이전트
---

너는 지식 캐시 정리 담당이다. 사용자가 명시적으로 요청할 때만 실행한다.

작업:
- 중복 지식 병합
- 오래된 지식 비활성화 제안
- 프로젝트 정책으로 승격할 후보 식별
- 색인 갱신
'@

$files[".claude/commands/work/plan.md"] = @'
# /work:plan <goal>

목표를 `01_Phases/<owner>/M{N}-{slug}/` 아래의 Phase 파일로 쪼갠다.

## 언제 쓰나

- 목표가 하루 이상 걸릴 것 같을 때
- 여러 파일/도메인을 건드릴 때
- 사용자가 "이걸 어떻게 나눠서 하면 좋을까?"라고 물었을 때
- 기존 프로젝트에 하네스를 얹은 뒤 첫 작업 단위를 만들 때

## 입력

```text
/work:plan <goal>
```

예시:

```text
/work:plan 로그인 API와 기본 세션 관리를 붙이자
/work:plan 기존 React 앱에 접근성 점검 루틴을 추가하자
```

## 절차

1. `CLAUDE.md`, `harness.config.json`, `00_Document/PRD.md`를 읽는다.
2. `00_Document/ARCHITECTURE.md`가 있으면 현재 구조를 확인한다.
3. 목표를 3~7개 Phase로 나눈다. 너무 크면 milestone을 먼저 나눈다.
4. 각 Phase에 목표, 사전 조건, 작업 내용, 완료 조건, 테스트, 위험 깃발을 적는다.
5. 복잡 이상이면 `planner` 또는 `coordinator` 검토를 붙인다.
6. 사용자가 바로 실행할 수 있는 다음 Phase를 제안한다.

## 산출물

```text
01_Phases/<owner>/M<N>-<slug>/
  _milestone-plan.md
  01-<phase>.md
  02-<phase>.md
```

## 좋은 Phase 기준

- 완료 조건이 명령 또는 관찰로 검증 가능하다.
- 한 Phase가 여러 설계 결정을 동시에 요구하지 않는다.
- 사람 승인이 필요한 작업은 Phase 안에 명시되어 있다.
- 테스트 또는 smoke 방법이 있다. 아직 없다면 "테스트 생성" Phase를 따로 둔다.

## 멈춤 조건

- PRD와 목표가 충돌한다.
- 공개 API, DB schema, 배포, 비용 발생 등 bucket-c 판단이 필요하다.
- 목표가 너무 커서 Phase 대신 상위 milestone 정의가 먼저 필요하다.
'@

$files[".claude/commands/session/start.md"] = @'
# /session:start

세션 시작 루틴.

## 목적

이전 세션의 맥락을 복구하고, 지금 바로 해야 할 다음 액션을 정합니다.

## 절차

1. `git status`로 작업 트리 상태를 확인한다.
2. `.claude/state/current-pin.txt`를 읽는다. 없으면 `.claude/templates/pin-template.txt`에서 생성한다.
3. `CLAUDE.md`와 최근 `.claude/CHANGELOG.md`를 확인한다.
4. 현재 작업, 다음 액션, 사람 게이트 여부를 사용자에게 짧게 보고한다.

## 출력

- 현재 branch와 dirty 여부
- 현재 WORK-ID / PHASE
- 다음 액션
- 주의할 위험 깃발

## 주의

작업 트리에 사용자가 만든 변경이 있으면 되돌리지 않습니다. 관련 변경이면 읽고 이어가고, 무관하면 건드리지 않습니다.
'@

$files[".claude/commands/session/end.md"] = @'
# /session:end

세션 마감 루틴.

## 목적

작업 결과를 검증하고 다음 세션이 이어받을 수 있게 좌표를 남깁니다.

## 절차

1. 변경 요약을 확인한다.
2. 필요한 build/test/smoke를 실행한다.
3. 복잡 이상 작업이면 `-DONE.md`를 작성한다.
4. `.claude/state/current-pin.txt`를 다음 액션 기준으로 갱신한다.
5. push, PR, merge, deploy, migration은 사용자 승인 전 실행하지 않는다.

## 산출물

- 갱신된 `.claude/state/current-pin.txt`
- 필요한 경우 `01_Phases/.../*-DONE.md`
- 검증 명령과 결과 요약
- 다음 액션

## done으로 볼 수 없는 경우

- build/test/smoke가 실패했다.
- 완료 조건을 실행하지 않고 추측했다.
- bucket-c 작업이 남았는데 사용자 승인이 없다.
- 문서나 pin이 현재 상태와 다르다.
'@

$files[".claude/commands/session/review.md"] = @'
# /session:review

구현이 아니라 이해와 점검을 위한 세션.

## 언제 쓰나

- 구현은 끝났지만 사용자가 이유를 더 깊게 이해하고 싶을 때
- pending-comprehension 원장에 쌓인 항목을 풀 때
- 큰 설계 결정의 trade-off를 다시 확인할 때

## 절차

1. `00_Document/ledgers/pending-comprehension.md`를 읽는다.
2. 사용자가 고른 항목을 깊게 설명한다.
3. 이해가 끝난 항목은 원장에서 체크한다.
4. 코드 변경은 기본적으로 하지 않는다.

## 출력

- 핵심 개념 설명
- 왜 그렇게 구현했는지
- 대안과 단점
- 사용자가 다음에 확인하면 좋은 파일/라인
'@

$files[".claude/commands/engine/goal.md"] = @'
# /engine:goal <goal> [--done=<condition>]

목표 도달형 루프 드라이버.

## 언제 쓰나

- done 조건이 명확하고 기계적으로 검증 가능할 때
- 사람이 매 스텝 지시하지 않아도 되는 반복 작업일 때
- build/test/lint/smoke를 통과할 때까지 작은 수정 루프를 돌릴 때

## 입력

```text
/engine:goal <goal> --done=<검증 가능한 조건>
```

예시:

```text
/engine:goal 테스트 깨진 원인을 고쳐라 --done="npm test 통과"
/engine:goal PRD 문서 링크 깨짐을 정리하라 --done="dangling link 0개"
```

## 원칙

- done 조건은 기계적으로 검증 가능해야 한다.
- build/test/smoke 출력이 대화에 남아야 한다.
- bucket-c 작업은 멈추고 사용자 승인을 받는다.
- 반복 실패 시 circuit breaker 신호를 확인한다.

## 기본 루프

1. 목표와 done 조건 정리
2. 현재 상태 조사
3. 최소 변경 구현
4. 검증 실행
5. 실패 원인 반영 후 반복
6. 통과 시 pin과 완료 문서 갱신

## 멈춤 조건

- 같은 실패가 3회 이상 반복된다.
- `risk-detector`가 irreversible 또는 trust-boundary를 알린다.
- 검증 명령이 외부 서비스, 비용, 배포, migration을 요구한다.
- done 조건 자체가 모호하거나 AI 자기판단에 의존한다.

## 출력

- 시도한 변경 요약
- 실행한 검증 명령과 결과
- 남은 위험 또는 사람 게이트
- 다음 액션
'@

$files[".claude/commands/harness-review.md"] = @'
# /harness-review [scope]

하네스 자체를 읽기 전용으로 점검한다.

## 언제 쓰나

- 하네스 템플릿을 수정한 뒤
- 기존 프로젝트에 이식한 뒤 첫 점검
- hooks/commands/agents/policies 사이에 불일치가 의심될 때
- 큰 PR 전에 운영 규칙이 실제 repo와 맞는지 보고 싶을 때

## 점검 항목

점검 항목:
- `CLAUDE.md`와 policies 충돌
- commands, agents, hooks 사이 불일치
- 프로젝트 config와 실제 repo 구조 차이
- 위험 깃발 누락
- 문서가 프로젝트 고유 내용과 범용 하네스 내용을 섞고 있는지

## 출력

- blocker: 즉시 고쳐야 하는 불일치
- high: 이식 시 사고 가능성이 큰 설명/설정 누락
- medium: 사용성 저하 또는 혼동 가능성
- suggested patch: 수정 방향

## 금지

기본적으로 읽기 전용입니다. 사용자가 명시적으로 "고쳐줘"라고 한 경우에만 파일을 수정합니다.
'@

$files[".claude/commands/cross-review.md"] = @'
# /cross-review <scope>

큰 변경, PR, 비가역 작업 전 외부 시선으로 읽기 전용 점검한다.

## 언제 쓰나

- PR 생성 전
- migration, 배포, public API 변경 전
- 복잡/대규모 Phase 완료 직후
- 내부 reviewer만으로 충분한지 불안할 때

## 입력 예시

```text
/cross-review current diff
/cross-review 01_Phases/youngho/M2-auth
/cross-review PR #12
```

## 출력

출력:
- blocker
- high risk
- missing tests
- open questions
- go/no-go recommendation

## 원칙

수정은 하지 않습니다. 발견과 판단만 제공합니다. 수정은 별도 작업으로 진행합니다.
'@

$files[".claude/commands/refactor-sweep.md"] = @'
# /refactor-sweep [--dry-run]

저위험 리팩터링 후보를 찾고, dry-run 기본으로 보고한다.

## 언제 쓰나

- 기능 구현이 아니라 코드 건강도를 정리하고 싶을 때
- 큰 작업 전 작은 구조 개선 후보를 보고 싶을 때
- 반복되는 냄새를 한 번에 진단하고 싶을 때

## 자동 수정 가능

자동 수정 가능:
- 중복 제거
- 명확한 이름 정리
- 죽은 코드 제거
- 작은 함수 추출

## 자동 수정 금지

자동 수정 금지:
- 인증/권한/결제/데이터 마이그레이션
- 공개 API 또는 schema 변경
- 대규모 구조 변경
- 배포 관련 변경

## 권장 흐름

1. 먼저 `--dry-run`으로 후보만 본다.
2. 사용자가 범위를 고른다.
3. 작은 batch로 수정한다.
4. build/test를 실행한다.
5. 실패하면 해당 batch만 되돌릴 수 있게 요약한다.
'@

$files[".claude/commands/setup.md"] = @'
# /setup

신규 프로젝트 또는 신규 팀원 셋업.

## 목적

하네스가 현재 프로젝트에 맞게 채워졌는지 확인하고, 첫 작업으로 넘어갈 준비를 합니다.

## 절차

1. `harness.config.json` 존재 여부 확인
2. 필수 도구 확인
3. build/test 명령 확인
4. `.claude/state/current-pin.txt` 초기화
5. 첫 `01_Phases/<owner>/M1-...` 제안

## 체크리스트

- `CLAUDE.md`의 placeholder가 남아 있지 않은가
- `harness.config.json`의 source/test 경로가 실제로 존재하는가
- `00_Document/PRD.md`에 현재 목표가 적혀 있는가
- build/test 명령이 로컬에서 실행 가능한가
- 위험 경계가 최소한으로라도 선언되어 있는가

## 출력

- 준비 완료 여부
- 빠진 설정
- 첫 권장 명령
'@

$files[".claude/hooks/hook-common.sh"] = @'
#!/usr/bin/env bash
set -euo pipefail

payload="$(cat || true)"

json_get() {
  local expr="$1"
  python - "$expr" "$payload" <<'PY'
import json, sys
expr = sys.argv[1].split(".")
payload = sys.argv[2]
try:
    data = json.loads(payload) if payload.strip() else {}
    cur = data
    for part in expr:
        cur = cur.get(part, "") if isinstance(cur, dict) else ""
    print(cur if cur is not None else "")
except Exception:
    print("")
PY
}

TOOL_NAME="$(json_get tool_name)"
TOOL_INPUT_COMMAND="$(json_get tool_input.command)"
TOOL_INPUT_FILE="$(json_get tool_input.file_path)"
PROJECT_DIR="${CLAUDE_PROJECT_DIR:-$(pwd)}"
STATE_DIR="$PROJECT_DIR/.claude/state"
mkdir -p "$STATE_DIR"
'@

$files[".claude/hooks/README.md"] = @'
# Hooks

이 폴더의 스크립트는 Claude Code tool hook에서 실행됩니다. 목적은 AI가 위험 작업을 놓치지 않게 돕는 것이며, 모든 훅이 작업을 차단하는 것은 아닙니다.

## Hook 종류

| Hook | 단계 | 성격 | 역할 |
|---|---|---|---|
| `dangerous-cmd-guard.sh` | PreToolUse Bash | must-pass | 명백히 파괴적인 명령 차단 |
| `risk-detector.sh` | PreToolUse Bash/Edit/Write | advisory | 위험 경로/명령을 `.claude/state/risk-flags.txt`에 기록 |
| `tdd-guard.sh` | PreToolUse Edit/Write | advisory | production 파일 수정 시 테스트 갱신 알림 |
| `phase-gate-validator.sh` | PostToolUse Edit/Write | must-pass | `*-DONE.md` 필수 섹션 검사 |
| `reviewer-auto-trigger.sh` | PostToolUse Edit/Write | advisory | 리뷰 권장 변경을 `.claude/state/reviewer-pending.txt`에 기록 |
| `convention-size-guard.sh` | PostToolUse Edit/Write | advisory | 큰 파일 분리 권고 |
| `circuit-breaker.sh` | PostToolUse all | advisory | 같은 도구 반복 사용 감지 |
| `pin-injector.sh` | UserPromptSubmit | advisory | 현재 work pin을 사용자 입력 직전 주입 |

## must-pass와 advisory

- `must-pass`: 문제가 있으면 exit 2로 도구 실행을 막습니다. 사용자는 원인을 해결하거나 명령/문서를 바꿔야 합니다.
- `advisory`: exit 0으로 통과시키지만 상태 파일에 기록하고 stderr로 알립니다. AI는 이 신호를 보고 등급 상향, 리뷰 호출, 사람 게이트를 판단합니다.

## 의존성

- Git Bash 또는 Bash 호환 쉘
- Python 3.6 이상 (`hook-common.sh`가 JSON payload를 파싱할 때 사용)
- Claude Code의 `$CLAUDE_PROJECT_DIR` 환경 변수

Windows에서 실행할 경우 `.sh` 파일은 LF line ending을 유지하는 편이 안전합니다. 생성기는 `.sh` 파일을 LF로 씁니다.

## 상태 파일

`.claude/state/`는 런타임 상태입니다. 템플릿으로 동기화하지 않습니다.

| 파일 | 의미 |
|---|---|
| `current-pin.txt` | 현재 작업 좌표 |
| `risk-flags.txt` | 위험 깃발 기록 |
| `reviewer-pending.txt` | 리뷰 권장 기록 |
| `tdd-guard-log.txt` | 테스트 갱신 권고 기록 |
| `circuit-breaker.log` | 반복 도구 사용 로그 |
| `circuit-tripped.txt` | 반복 사용 중단 신호 |

## 프로젝트별 조정

기본 훅은 범용 패턴만 봅니다. 프로젝트에 맞게 조정해야 하는 대표 항목:

- `risk-detector.sh`: 인증, 결제, 배포, 마이그레이션 경로
- `tdd-guard.sh`: 테스트가 반드시 필요한 production 경로
- `reviewer-auto-trigger.sh`: public contract, schema, generated code 경로
- `convention-size-guard.sh`: 파일 크기 기준

경로 패턴은 `harness.config.json`의 `risk_flags`와 같은 의미를 갖도록 맞추는 것이 좋습니다.

## 오탐 대응

1. 먼저 훅이 출력한 파일명과 이유를 확인합니다.
2. advisory라면 작업을 계속하되 work pin 또는 완료 문서에 사유를 남깁니다.
3. must-pass라면 문서/명령/변경 순서를 고쳐 통과시키는 쪽을 우선합니다.
4. 정말 훅이 잘못된 경우 훅 스크립트를 수정하고 `00_Document/policies/` 또는 ADR에 이유를 남깁니다.
'@

$files[".claude/hooks/dangerous-cmd-guard.sh"] = @'
#!/usr/bin/env bash
set -euo pipefail
. "$(dirname "$0")/hook-common.sh"

cmd="$TOOL_INPUT_COMMAND"

block() {
  echo "dangerous-cmd-guard: blocked command: $1" >&2
  exit 2
}

case "$cmd" in
  *"rm -rf /"*|*"git reset --hard"*|*"git push --force"*|*"gh pr merge"*--admin*|*"format "*|*"del /s /q "*)
    block "$cmd"
    ;;
esac

exit 0
'@

$files[".claude/hooks/risk-detector.sh"] = @'
#!/usr/bin/env bash
set -euo pipefail
. "$(dirname "$0")/hook-common.sh"

target="$TOOL_INPUT_FILE $TOOL_INPUT_COMMAND"
flag=""

case "$target" in
  *auth*|*security*|*permission*|*payment*|*migration*|*schema*|*protocol*|*.claude/*)
    flag="risk: review-required"
    ;;
esac

case "$TOOL_INPUT_COMMAND" in
  *"git push"*|*"gh pr create"*|*"gh pr merge"*|*"deploy"*|*"migrate"*)
    flag="risk: human-gate"
    ;;
esac

if [ -n "$flag" ]; then
  line="$(date +%F) $flag :: $target"
  echo "$line" >> "$STATE_DIR/risk-flags.txt"
  echo "risk-detector: $line" >&2
fi

exit 0
'@

$files[".claude/hooks/tdd-guard.sh"] = @'
#!/usr/bin/env bash
set -euo pipefail
. "$(dirname "$0")/hook-common.sh"

file="$TOOL_INPUT_FILE"
case "$file" in
  *test*|*spec*|*.md|"")
    exit 0
    ;;
esac

if [ -n "$file" ]; then
  echo "$(date +%F) tdd-advisory :: changed $file, consider adding/updating tests" >> "$STATE_DIR/tdd-guard-log.txt"
  echo "tdd-guard: consider adding/updating tests for $file" >&2
fi

exit 0
'@

$files[".claude/hooks/phase-gate-validator.sh"] = @'
#!/usr/bin/env bash
set -euo pipefail
. "$(dirname "$0")/hook-common.sh"

file="$TOOL_INPUT_FILE"
case "$file" in
  *-DONE.md)
    for token in "summary:" "status: done" "## TL;DR" "## AC 검증 결과"; do
      if ! grep -q "$token" "$PROJECT_DIR/$file" 2>/dev/null; then
        echo "phase-gate-validator: $file missing '$token'" >&2
        exit 2
      fi
    done
    ;;
esac

exit 0
'@

$files[".claude/hooks/reviewer-auto-trigger.sh"] = @'
#!/usr/bin/env bash
set -euo pipefail
. "$(dirname "$0")/hook-common.sh"

file="$TOOL_INPUT_FILE"
case "$file" in
  *auth*|*security*|*permission*|*payment*|*schema*|*protocol*|*.claude/*)
    echo "$(date +%F) reviewer-advisory :: $file" >> "$STATE_DIR/reviewer-pending.txt"
    echo "reviewer-auto-trigger: reviewer pass recommended for $file" >&2
    ;;
esac

exit 0
'@

$files[".claude/hooks/convention-size-guard.sh"] = @'
#!/usr/bin/env bash
set -euo pipefail
. "$(dirname "$0")/hook-common.sh"

file="$TOOL_INPUT_FILE"
path="$PROJECT_DIR/$file"

if [ -f "$path" ]; then
  lines="$(wc -l < "$path" | tr -d ' ')"
  if [ "$lines" -gt 500 ]; then
    echo "convention-size-guard: $file has $lines lines; consider splitting" >&2
  fi
fi

exit 0
'@

$files[".claude/hooks/circuit-breaker.sh"] = @'
#!/usr/bin/env bash
set -euo pipefail
. "$(dirname "$0")/hook-common.sh"

log="$STATE_DIR/circuit-breaker.log"
echo "$(date +%s) $TOOL_NAME" >> "$log"

recent="$(tail -n 8 "$log" 2>/dev/null | awk '{print $2}' | sort | uniq -c | sort -nr | head -n1 | awk '{print $1}')"
if [ "${recent:-0}" -ge 8 ]; then
  echo "$(date +%F) repeated-tool-use" > "$STATE_DIR/circuit-tripped.txt"
  echo "circuit-breaker: repeated tool use detected; pause and reassess" >&2
fi

exit 0
'@

$files[".claude/hooks/pin-injector.sh"] = @'
#!/usr/bin/env bash
set -euo pipefail
. "$(dirname "$0")/hook-common.sh"

pin="$STATE_DIR/current-pin.txt"
if [ -f "$pin" ]; then
  echo ""
  echo "[current work pin]"
  cat "$pin"
  echo ""
fi

exit 0
'@

$files["00_Document/INDEX.md"] = @'
# Document Index

- `PRD.md`: 무엇을 만드는가
- `ARCHITECTURE.md`: 전체 구조
- `ADR/`: 결정 기록
- `policies/`: 하네스 운영 정책
- `ledgers/`: pending 항목
'@

$files["00_Document/PRD.template.md"] = @'
# PRD

## 목적

## 사용자

## 만들 것

## 만들지 않을 것

## 성공 기준

## 제약
'@

$files["00_Document/ARCHITECTURE.template.md"] = @'
# Architecture

## 시스템 개요

## 주요 모듈

## 데이터 흐름

## 외부 의존성

## 위험 경계

## 검증 전략
'@

$files["00_Document/ADR/INDEX.md"] = @'
# ADR Index

ADR은 중요한 결정을 시간순으로 기록합니다.

형식:
- 상태: proposed / accepted / superseded
- 결정
- 이유
- trade-off
- 후속 작업
'@

$files["00_Document/ADR/000-template.md"] = @'
# ADR-000: <title>

**날짜**: <YYYY-MM-DD>  
**상태**: proposed

## 결정

## 이유

## 대안

## 트레이드오프

## 후속 작업
'@

$files["00_Document/policies/INDEX.md"] = @'
# Policies

| 파일 | 역할 |
|---|---|
| `grade-and-risk.md` | 작업 등급과 위험 깃발 |
| `work-judge.md` | 자율 진행/사람 게이트 분류 |
| `loop-driver.md` | 목표 도달형 루프 운영 |
| `pin-and-done.md` | work-pin과 완료 박제 |
| `reporting-format.md` | 보고 형식 |
| `subagent-routing.md` | 에이전트 라우팅 |
| `review-tiering.md` | 리뷰 단계 |
| `pr-and-merge-gate.md` | PR, merge, 배포 게이트 |
| `knowledge-system.md` | 지식 캐시 |
| `doc-thresholds.md` | 문서 크기와 분할 |
'@

$files["00_Document/policies/grade-and-risk.md"] = @'
# Grade and Risk

이 문서는 작업 크기와 위험도를 빠르게 분류하기 위한 기준입니다. 등급은 "얼마나 오래 걸리나"보다 "얼마나 넓게 영향을 주나"를 우선합니다.

## 작업 등급

| 등급 | 기준 | 처리 |
|---|---|---|
| 단순 | 1파일, 작은 수정, 가역 | 메인 세션 직접 |
| 보통 | 한 도메인, 2~3파일 | implementer 또는 qa |
| 복잡 | 여러 도메인, 설계/검증 부담 | coordinator + reviewer |
| 대규모 | 광범위, 비가역, cross-cutting | plan + coordinator + reviewer + DONE |

## 예시

| 작업 | 기본 등급 | 이유 |
|---|---|---|
| README 오타 수정 | 단순 | 문서 1파일, 가역 |
| 버튼 라벨과 테스트 수정 | 보통 | UI + 테스트, 영향 제한 |
| 로그인 API와 세션 저장 추가 | 복잡 | 인증 경계 + 여러 파일 |
| DB schema 변경과 배포 절차 포함 | 대규모 | 비가역 가능성 + 운영 영향 |

## 위험 깃발

- trust-boundary: 인증, 권한, 보안, 결제, 개인정보, 네트워크 입력
- irreversible: push, merge, deploy, migration, external side effect
- contract: 공개 API, DB schema, protocol, generated contract
- visual/manual: UI, 디자인, 에셋, 육안 검증
- harness: `.claude`, `CLAUDE.md`, policies, hooks

위험 깃발은 등급을 최소 한 단계 올립니다.

## 등급 상향 규칙

- 단순 작업이라도 인증/권한 경로를 건드리면 최소 보통입니다.
- 보통 작업이라도 public API나 DB schema를 바꾸면 최소 복잡입니다.
- 복잡 작업에 migration, deploy, merge가 포함되면 대규모로 봅니다.
- UI/asset 변경은 기능 테스트가 통과해도 사람 육안 검토 항목을 남깁니다.

## 등급이 애매할 때

더 높은 등급으로 시작하고, 실제 조사 후 낮춰도 됩니다. 낮게 잡고 검증을 생략하는 것보다 높게 잡고 게이트를 명시하는 편이 안전합니다.
'@

$files["00_Document/policies/work-judge.md"] = @'
# Work Judge

작업은 세 bucket 중 하나로 분류합니다.

## bucket-a: 기계 판정 가능

빌드, 테스트, lint, smoke처럼 통과/실패가 명확한 작업입니다. AI가 자율 진행할 수 있습니다.

예시:
- 타입 오류 수정
- 깨진 unit test 복구
- 문서 링크 정리
- formatter/lint 위반 수정

## bucket-b: 사람 병행 검토

UI, 문구, 사운드, 디자인처럼 취향 또는 육안 판단이 필요한 작업입니다. AI는 placeholder와 검증 가능한 부분까지 진행하고, 사람 검토 항목을 원장에 남깁니다.

예시:
- 색상, 타이포그래피, 레이아웃 조정
- 사운드/이미지/영상 자산 교체
- UX 문구 톤 결정
- Unity/게임 씬 외관 확인

## bucket-c: 사람 게이트

비가역, 보안, 설계 분기, 배포, PR/merge, 데이터 마이그레이션입니다. AI는 멈추고 사용자 승인을 받아야 합니다.

예시:
- `git push`, PR 생성/merge
- production deploy
- DB migration 적용
- 결제/권한/인증 로직의 설계 변경
- public API breaking change

## 판단 순서

1. 외부 상태를 바꾸는가? 그렇다면 bucket-c.
2. 보안/권한/돈/데이터 손실 가능성이 있는가? 그렇다면 bucket-c.
3. 사람 눈이나 취향이 최종 판단인가? 그렇다면 bucket-b.
4. 명령으로 통과/실패가 분명한가? 그렇다면 bucket-a.

## 기록 위치

- bucket-b 항목은 `00_Document/ledgers/pending-art.md`에 남깁니다.
- 깊게 설명할 항목은 `pending-comprehension.md`에 남깁니다.
- 재사용 지식 후보는 `pending-knowledge.md`에 남깁니다.
'@

$files["00_Document/policies/loop-driver.md"] = @'
# Loop Driver

루프는 목표와 done 조건이 명확할 때 사용합니다.

## 원칙

- done은 기계가 판정할 수 있어야 합니다.
- 검증 출력은 대화 또는 문서에 남깁니다.
- bucket-c에서 멈춥니다.
- 같은 실패가 반복되면 circuit breaker를 확인합니다.

## 기본 순서

1. 목표와 완료 조건 확인
2. 현재 상태 조사
3. 작은 변경
4. 검증
5. 실패 원인 반영
6. 통과 시 pin과 문서 갱신
'@

$files["00_Document/policies/pin-and-done.md"] = @'
# Pin and Done

## work-pin

`.claude/state/current-pin.txt`는 세션 간 작업 좌표입니다.

필드:
- WORK-ID
- PHASE
- 현재 작업
- 다음 액션
- 주의할 약속
- 루프 상태
- 마지막 갱신

## DONE 문서

복잡 이상 작업은 `01_Phases/.../*-DONE.md`를 작성합니다.

포함:
- TL;DR
- 5단계 보고
- AC 검증 결과
- 결정 흐름
- 막혔던 지점
- 검색 키워드
'@

$files["00_Document/policies/reporting-format.md"] = @'
# Reporting Format

일상 응답은 짧게 유지합니다.

복잡 이상 완료 문서에는 5단계 보고를 포함합니다.

- 무엇을 만들었나
- 왜 필요한가
- 어떻게 만들었나
- 테스트 결과
- 다음 스텝
'@

$files["00_Document/policies/subagent-routing.md"] = @'
# Subagent Routing

| 상황 | 권장 Agent |
|---|---|
| 목표 분해 | planner |
| 일반 구현 | implementer |
| 테스트/회귀 | qa |
| 코드 리뷰 | reviewer |
| 문서/ADR | docs |
| 여러 도메인 | coordinator |
| 지식 캐시 정리 | knowledge-gc |

단순 작업은 위임하지 않는 편이 빠릅니다. 복잡 이상은 coordinator가 분해합니다.
'@

$files["00_Document/policies/review-tiering.md"] = @'
# Review Tiering

## Tier 1

작업자가 직접 확인합니다. 단순/보통 작업 기본입니다.

## Tier 2

reviewer가 읽기 전용으로 점검합니다. 위험 깃발, 10줄 이상 실질 변경, public contract 변경 시 권장합니다.

## Tier 3

`/harness-review` 또는 `/cross-review`로 큰 단위 정합성을 봅니다. PR, 대규모 변경, 하네스 변경 전에 권장합니다.
'@

$files["00_Document/policies/pr-and-merge-gate.md"] = @'
# PR and Merge Gate

다음 작업은 사용자 명시 승인 전 실행하지 않습니다.

- `git push`
- `gh pr create`
- `gh pr merge`
- production deploy
- DB migration
- external system write

AI는 준비 상태, diff 요약, 검증 결과, 위험을 보고한 뒤 멈춥니다.
'@

$files["00_Document/policies/knowledge-system.md"] = @'
# Knowledge System

`.claude/knowledge/`는 AI가 자주 참조하는 프로젝트 지식을 저장하는 캐시입니다.

원칙:
- 사실, 정책, 결정, 임시 메모를 구분합니다.
- 오래된 내용은 삭제보다 superseded 표시를 선호합니다.
- 자동 축적하지 않습니다. 사용자가 요청하거나 명확한 재사용 가치가 있을 때만 추가합니다.

## 권장 구조

```text
.claude/knowledge/
  README.md
  _usage.md
  domain-a/
    _index.md
  domain-b/
    _index.md
```

처음부터 세분화하지 않아도 됩니다. 반복해서 참조되는 정보가 생길 때만 도메인을 나눕니다.

## 무엇을 넣나

- 반복해서 설명한 프로젝트 특유의 규칙
- 자주 틀리는 빌드/테스트 절차
- 특정 모듈의 변경 순서
- 외부 문서 대신 프로젝트 안에 보존해야 할 요약

## 무엇을 넣지 않나

- 비밀번호, 토큰, 개인정보
- 일회성 작업 로그
- 이미 `CLAUDE.md`, ADR, policy에 있는 내용의 단순 복사
- 아직 검증되지 않은 AI 추측

## 승격 기준

- 여러 작업에서 재사용되면 knowledge로 둡니다.
- 프로젝트 전체 규칙이 되면 policy나 `CLAUDE.md`로 승격합니다.
- 중요한 설계 결정이면 ADR로 승격합니다.
- 더 이상 맞지 않으면 삭제보다 `superseded` 메모를 남깁니다.
'@

$files["00_Document/policies/doc-thresholds.md"] = @'
# Document Thresholds

문서는 길어지면 분리합니다.

- 정책 문서: 220줄 근처에서 분리 검토
- 루트 `CLAUDE.md`: 350줄 근처에서 응축 검토
- Phase와 DONE 문서는 작업 단위 산출물이므로 억지로 자르지 않습니다.

문서가 커지는 이유가 새 정책이면 새 policy로 분리하고, 결정이면 ADR로 분리합니다.
'@

$files["00_Document/ledgers/INDEX.md"] = @'
# Ledgers

- `pending-art.md`: 육안/취향 검토
- `pending-comprehension.md`: 나중에 깊게 이해할 항목
- `pending-knowledge.md`: knowledge 승격 후보
'@

$files["00_Document/ledgers/pending-art.md"] = @'
# Pending Art / Visual Review

- [ ] <item>
'@

$files["00_Document/ledgers/pending-comprehension.md"] = @'
# Pending Comprehension

- [ ] <item>
'@

$files["00_Document/ledgers/pending-knowledge.md"] = @'
# Pending Knowledge

- [ ] <item>
'@

$files["01_Phases/README.md"] = @'
# Phases

큰 목표를 작은 작업 단위로 나누는 공간입니다.

권장 구조:

```text
01_Phases/
  <owner>/
    M1-<milestone>/
      _milestone-plan.md
      01-<phase>.md
      01-<phase>-DONE.md
```

한 Phase는 1~3시간 안에 검증 가능한 크기를 권장합니다.
'@

$files["01_Phases/_template.md"] = @'
---
owner: <owner>
status: pending
grade: <단순|보통|복잡|대규모>
risk: []
---

# Phase <NN> — <title>

## 목표

## 사전 조건

## 작업 내용

- [ ] 

## 완료 조건

- [ ] 

## 테스트

```bash
<command>
```

## 사람 게이트

- 없음

## 메모
'@

$files["tools/install.ps1"] = @'
param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectRoot,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$TemplateRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

function Copy-HarnessFile {
    param(
        [string]$Source,
        [string]$Destination
    )

    $dir = Split-Path -Parent $Destination
    if (-not (Test-Path -LiteralPath $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    if ((Test-Path -LiteralPath $Destination) -and -not $Force) {
        Write-Host "[SKIP] exists: $Destination"
        return
    }

    Copy-Item -LiteralPath $Source -Destination $Destination -Force
    Write-Host "[COPY] $Destination"
}

function Copy-HarnessTree {
    param(
        [string]$RelativeRoot
    )

    $srcRoot = Join-Path $TemplateRoot $RelativeRoot
    if (-not (Test-Path -LiteralPath $srcRoot)) {
        return
    }

    Get-ChildItem -LiteralPath $srcRoot -Recurse -File | ForEach-Object {
        $relative = $_.FullName.Substring($srcRoot.Length).TrimStart('\', '/')
        $dst = Join-Path (Join-Path $ProjectRoot $RelativeRoot) $relative
        Copy-HarnessFile -Source $_.FullName -Destination $dst
    }
}

if (-not (Test-Path -LiteralPath $ProjectRoot)) {
    New-Item -ItemType Directory -Path $ProjectRoot -Force | Out-Null
}

$copyRoots = @(".claude", "00_Document", "01_Phases")
foreach ($root in $copyRoots) {
    Copy-HarnessTree -RelativeRoot $root
}

foreach ($file in @("CLAUDE.template.md", "harness.config.template.json")) {
    Copy-HarnessFile -Source (Join-Path $TemplateRoot $file) -Destination (Join-Path $ProjectRoot $file)
}

if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot "CLAUDE.md")) -or $Force) {
    Copy-HarnessFile -Source (Join-Path $TemplateRoot "CLAUDE.template.md") -Destination (Join-Path $ProjectRoot "CLAUDE.md")
}

if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot "harness.config.json")) -or $Force) {
    Copy-HarnessFile -Source (Join-Path $TemplateRoot "harness.config.template.json") -Destination (Join-Path $ProjectRoot "harness.config.json")
}

$docCopies = @{
    "00_Document/PRD.template.md" = "00_Document/PRD.md"
    "00_Document/ARCHITECTURE.template.md" = "00_Document/ARCHITECTURE.md"
}

foreach ($pair in $docCopies.GetEnumerator()) {
    $src = Join-Path $TemplateRoot $pair.Key
    $dst = Join-Path $ProjectRoot $pair.Value
    if (-not (Test-Path -LiteralPath $dst) -or $Force) {
        Copy-HarnessFile -Source $src -Destination $dst
    }
}

$state = Join-Path $ProjectRoot ".claude/state"
New-Item -ItemType Directory -Path $state -Force | Out-Null
if (-not (Test-Path -LiteralPath (Join-Path $state "current-pin.txt"))) {
    Copy-HarnessFile -Source (Join-Path $TemplateRoot ".claude/templates/pin-template.txt") -Destination (Join-Path $state "current-pin.txt")
}

Write-Host "YYH Harness installed to $ProjectRoot"
Write-Host "Next: edit CLAUDE.md, harness.config.json, and 00_Document/PRD.md"
'@

$files["tools/audit-project.ps1"] = @'
param(
    [string]$ProjectRoot = (Get-Location).Path
)

$ErrorActionPreference = "Stop"

Write-Host "Project root: $ProjectRoot"

$checks = @(
    "CLAUDE.md",
    "harness.config.json",
    ".claude/settings.json",
    "00_Document/PRD.md",
    "01_Phases"
)

foreach ($item in $checks) {
    $path = Join-Path $ProjectRoot $item
    if (Test-Path -LiteralPath $path) {
        Write-Host "[OK] $item"
    } else {
        Write-Host "[MISSING] $item"
    }
}

Write-Host ""
Write-Host "Git status:"
if (Test-Path -LiteralPath (Join-Path $ProjectRoot ".git")) {
    git -C $ProjectRoot status --short
} else {
    Write-Host "[SKIP] not a git repository"
}
'@

$files["tools/sync-harness.ps1"] = @'
param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectRoot,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$TemplateRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

function Copy-HarnessFile {
    param(
        [string]$Source,
        [string]$Destination
    )

    $dir = Split-Path -Parent $Destination
    if (-not (Test-Path -LiteralPath $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    if ((Test-Path -LiteralPath $Destination) -and -not $Force) {
        Write-Host "[SKIP] exists: $Destination"
        return
    }

    Copy-Item -LiteralPath $Source -Destination $Destination -Force
    Write-Host "[COPY] $Destination"
}

function Copy-HarnessTree {
    param(
        [string]$RelativeRoot
    )

    $srcRoot = Join-Path $TemplateRoot $RelativeRoot
    Get-ChildItem -LiteralPath $srcRoot -Recurse -File | ForEach-Object {
        $relative = $_.FullName.Substring($srcRoot.Length).TrimStart('\', '/')
        $dst = Join-Path (Join-Path $ProjectRoot $RelativeRoot) $relative
        Copy-HarnessFile -Source $_.FullName -Destination $dst
    }
}

Write-Host "Syncing generic harness files to $ProjectRoot"
Write-Host "Default mode copies only missing files. Use -Force to overwrite existing generic harness files."
Write-Host "Never synced by this script: CLAUDE.md, harness.config.json, .claude/state"

Copy-HarnessTree -RelativeRoot ".claude/agents"
Copy-HarnessTree -RelativeRoot ".claude/commands"
Copy-HarnessTree -RelativeRoot ".claude/hooks"
Copy-HarnessTree -RelativeRoot ".claude/templates"
Copy-HarnessTree -RelativeRoot "00_Document/policies"

Write-Host "Sync complete."
'@

$files[".yyh-harness-manifest.json"] = @'
{
  "name": "YYH_Harness",
  "version": "0.1.0",
  "kind": "generic-ai-agent-harness",
  "generated_by": "99_Tools/Generate-YYH-Harness.ps1",
  "notes": [
    "Project runtime state lives in .claude/state and should not be synced as template content.",
    "Project-specific rules belong in CLAUDE.md and harness.config.json.",
    "Generic policies live under 00_Document/policies."
  ]
}
'@

foreach ($entry in $files.GetEnumerator()) {
    Write-HarnessFile -RelativePath $entry.Key -Content $entry.Value
}

$obsoleteFiles = @(
    "00_Document/PROJECT_BRIEF.template.md"
)

foreach ($obsolete in $obsoleteFiles) {
    $obsoletePath = Join-Path $TargetRoot $obsolete
    if (Test-Path -LiteralPath $obsoletePath) {
        Remove-Item -LiteralPath $obsoletePath -Force
    }
}

Write-Host "Generated YYH Harness at $TargetRoot"
Write-Host "Files: $($files.Count)"
