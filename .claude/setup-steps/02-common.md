# 02-common — 공통 환경 검증

> 모든 역할이 거치는 환경 검증 8 단계.
> 어느 단계든 실패 시 STOP, 사용자 도움 안내 후 같은 단계 재시도.

---

## 진행 원칙

- **한 단계씩**. 한꺼번에 여러 검증 안 함.
- **검증 명령은 사용자에게 실행 요청**. Claude가 직접 bash 호출하지 않음 (사용자 컴퓨터 환경에서만 의미 있음).
- **실패 시 STOP**. 학부생 백지 가정. "왜 실패했는지" 먼저 추측 안 함. 에러 메시지 그대로 받고 다음 행동 안내.

---

## 1. 한글 경로 검증 (ADR-017)

```
1단계: 현재 작업 위치에 한글 들어있나 확인할게요.
PowerShell 또는 Git Bash에서 다음 실행:

  pwd

결과 알려주세요.
```

**판정**: 결과 문자열에 한글(가-힣) 포함 여부 확인.

- 한글 없음 → "OK, 다음 단계로 가요" + 2단계로
- 한글 있음 → **STOP**:
  ```
  ⛔ 경로에 한글이 들어있어요: {pwd 결과}

  본인 헌법(ADR-017)에 따라 이 레포는 ASCII 경로에만 둘 수 있어요.
  팀장(유영호)이 48시간 silent fail 함정을 겪은 이슈입니다.

  해결: 레포를 다음과 같은 ASCII 경로로 옮기세요.
  - 권장: C:\Dev\ClaudeDev
  - 또는: C:\Projects\ClaudeDev

  옮긴 후 그 경로에서 Claude Code를 다시 실행하고 /setup 호출해주세요.
  ```

---

## 2. Git 설치 + Bash PATH 확인

```
2단계: Git이 깔려있고 Bash가 PATH에 있는지 확인할게요.
다음 두 명령 실행해서 결과 알려주세요:

  git --version
  where bash      (PowerShell의 경우)
  which bash      (Git Bash의 경우)
```

**판정**:
- `git --version`이 버전 출력 + bash 경로가 보임 → "OK, 다음으로"
- `git --version` 실패 → **STOP**:
  ```
  ⛔ Git이 설치 안 됐어요.
  Git for Windows 설치: https://git-scm.com/download/win
  설치 후 PowerShell 재시작 → /setup 재호출
  ```
- `bash` 못 찾음 → **STOP**:
  ```
  ⛔ Bash가 PATH에 없어요. 본인 헌법(ADR-020)의 핵심 함정입니다.

  Claude Code의 자동 검증 훅(work-envelope, phase-gate 등)이 bash로
  돌아요. PATH에 bash 없으면 훅이 silent fail합니다 — 본인 환경에서는
  잘 도는 것처럼 보이지만 실제로는 검증 안 됨.

  해결:
  1. Git for Windows가 설치됐는지 확인 (1단계 통과면 OK)
  2. Windows 시스템 환경 변수에 다음 경로 추가:
     C:\Program Files\Git\bin
  3. PowerShell + Claude Code 재시작
  4. /setup 재호출

  설정 방법 모르면 알려주세요. 단계별로 안내할게요.
  ```

---

## 3. .NET 10 SDK 설치 확인

```
3단계: .NET 10 SDK 설치 확인할게요.
다음 명령 실행 결과 알려주세요:

  dotnet --list-sdks
```

**판정**:
- `10.0.203` 이상 있음 → "OK, 다음으로"
- `dotnet` 명령 실패 → **STOP**:
  ```
  ⛔ .NET SDK 설치 안 됐어요.
  설치: https://dotnet.microsoft.com/download/dotnet/10.0
  Windows x64 SDK Installer 받아서 설치.
  ```
- 다른 버전만 있음 (예: 8.x, 9.x) → **STOP**:
  ```
  ⛔ .NET 10 SDK가 없어요. 현재: {목록}
  본인 레포는 global.json으로 10.0.203+ 핀됨.
  .NET 10 SDK 추가 설치: https://dotnet.microsoft.com/download/dotnet/10.0
  ```

---

## 4. MSSQL LocalDB 설치 확인

```
4단계: MSSQL LocalDB 설치 확인할게요.
다음 명령 실행 결과 알려주세요:

  sqllocaldb info
```

**판정**:
- `MSSQLLocalDB` 등 인스턴스 목록 출력 → "OK, 다음으로"
- 명령 실패 → **STOP**:
  ```
  ⛔ MSSQL LocalDB 설치 안 됐어요.

  본인 레포는 ADR-005 v2에 따라 MSSQL LocalDB + Windows 통합 인증을
  쓰도록 결정됐어요 (PostgreSQL에서 정정됨).

  설치:
  1. https://www.microsoft.com/sql-server/sql-server-downloads
  2. 'Express' 또는 'Developer' 에디션 (둘 다 무료) 다운로드
  3. 설치 시 'LocalDB' 옵션 반드시 체크
  4. 설치 후 PowerShell 재시작
  5. sqllocaldb info 다시 실행

  설치 막히면 알려주세요.
  ```

---

## 5. VS Code 통합 터미널 + 권장 확장 작동 확인

VS Code 자체는 이미 깔려있어요 (지금 본인이 보고 있는 게 그것). 두 가지만 확인:

### 5-A. 통합 터미널이 Git Bash인지 확인

```
5단계 (A): VS Code 안에서 통합 터미널을 열어주세요.
단축키: Ctrl + ` (백틱)

터미널이 열렸을 때, 터미널 우측 상단의 드롭다운에 어떤 셸 이름이
표시되나요? (예: "Git Bash", "PowerShell", "Command Prompt")
```

**판정**:
- "Git Bash" 또는 "bash" → "OK, 다음으로"
- 다른 셸 (PowerShell, Command Prompt 등) → **STOP**:
  ```
  ⛔ 통합 터미널이 Git Bash가 아니에요.

  본인 레포에 박힌 .vscode/settings.json은 통합 터미널을 Git Bash로
  고정하도록 설정돼있어요 (ADR-020 함정 회피). 그게 적용 안 된 상태.

  해결:
  1. VS Code 완전히 종료 후 재실행 (settings.json 새로 읽기)
  2. Ctrl+` 으로 터미널 다시 열기
  3. 우측 상단 드롭다운 다시 확인

  여전히 다른 셸이면:
  - F1 → "Terminal: Select Default Profile" 검색해서 실행
  - 목록에서 'Git Bash' 선택
  - 터미널 다시 열기

  Git Bash 옵션 자체가 안 보이면 공통 2단계의 Git for Windows 설치
  재확인 필요.
  ```

### 5-B. 권장 확장 팝업 확인

5-A 통과 후:

```
5단계 (B): VS Code 우측 하단 또는 좌측 알림 영역에
'이 워크스페이스가 권장 확장 설치를 제안한다' 팝업 떴어요?
```

**판정**:
- "떴어요" → "Install All 또는 모두 설치 눌러서 다 설치한 후 알려주세요"
- "안 떴어요" → 수동 확인 안내:
  ```
  팝업 안 떴으면 수동 확인:
  좌측 확장 패널 (Ctrl+Shift+X) → 검색창에 @recommended 입력
  → Workspace Recommendations 목록에 8개 정도 떠야 합니다.
  - csdevkit, csharp, vstuc, gitlens, powershell, vscode-xml,
    markdown-all-in-one, claude-code

  목록 보이면 모두 설치 (각 항목 옆 ⬇️ 클릭 또는 'Install All').
  ```
- 설치 완료 응답 → "OK, 다음으로"

---

## 6. Git 사용자 설정 확인

```
6단계: Git 사용자 정보 설정 확인할게요.
다음 두 명령 결과 알려주세요:

  git config --global user.name
  git config --global user.email
```

**판정**:
- 둘 다 출력됨 → "OK, 다음으로"
- 둘 중 하나라도 빈 출력 → **사용자에게 입력 요청**:
  ```
  Git 사용자 정보가 비어있어요. 본인 commit에 박힐 정보를 설정할게요.

  다음 두 명령을 실행해주세요 (본인 정보로 채워서):

    git config --global user.name "{slug}"
    git config --global user.email "{본인 이메일}"

  user.name은 영문 식별자 권장 (slug 사용). email은 본인 실제 이메일.
  설정 후 알려주세요.
  ```

---

## 7. 빌드 확인

```
7단계: 마지막 검증으로 솔루션 빌드해볼게요. 시간 좀 걸려요 (1~3분).
레포 루트에서 다음 실행 결과 알려주세요:

  dotnet build Dawnholder.slnx
```

**판정**:
- `Build succeeded` 출력 → "OK, 공통 환경 검증 완료!"
- 에러 → **STOP**:
  ```
  ⛔ 빌드 실패. 에러 메시지 그대로 붙여주세요 (마지막 20줄 정도).

  보통 원인:
  - NuGet 패키지 복원 안 됨 → dotnet restore 시도
  - 한글 경로 (1단계 통과했어야 하는데)
  - .NET 10 SDK 누락 (3단계 통과했어야 하는데)

  에러 메시지 받으면 진단할게요.
  ```

---

## 8. 공통 단계 완료

모든 검증 통과 시 사용자에게:

```
공통 환경 검증 8단계 다 통과했어요. 잘 따라오셨어요.

다음 단계: 역할별 셋업 ({role})
- backend면 MSSQL LocalDB 시작 + PacketGenerator + 서버 실행 검증
- unity-client면 Unity Hub + Unity 6 LTS + AI Assistant + MCP

준비됐어요?
```

사용자 응답 받으면:
- `role == "backend"` → `.claude/setup-steps/03-backend.md` 진행
- `role == "unity-client"` → `.claude/setup-steps/03-unity-client.md` 진행

---

## 변수 박힘 상태 (이후 단계 참조)

- 단계 01의 모든 변수 유지
- 환경 검증 상태: 모두 통과
