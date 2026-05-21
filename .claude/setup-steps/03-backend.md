# 03-backend — 백엔드 추가 셋업

> `role == "backend"` 일 때만 호출됨.
> 02-common 통과 상태 전제. 5 단계.

---

## 1. MSSQL LocalDB 인스턴스 시작 + 접속 확인

```
1단계: LocalDB 인스턴스를 시작하고 접속 확인할게요.
다음 두 명령 순서대로 실행하고 결과 알려주세요:

  sqllocaldb start MSSQLLocalDB
  sqlcmd -S "(localdb)\MSSQLLocalDB" -E -Q "SELECT @@VERSION"
```

**판정**:
- 첫 명령 OK + 두 번째에서 SQL Server 버전 문자열 출력 → "OK, 다음으로"
- 첫 명령 실패 → **STOP**:
  ```
  ⛔ MSSQLLocalDB 인스턴스 시작 실패. 에러 메시지 알려주세요.
  공통 4단계에서 LocalDB 설치는 통과했는데 인스턴스가 망가졌을 수 있어요.

  복구 시도:
    sqllocaldb delete MSSQLLocalDB
    sqllocaldb create MSSQLLocalDB
    sqllocaldb start MSSQLLocalDB
  ```
- 두 번째 명령(접속) 실패 → **STOP**:
  ```
  ⛔ LocalDB 접속 실패. -E 옵션은 Windows 통합 인증을 의미해요.
  본인 Windows 계정이 LocalDB에 sysadmin 권한 있어야 함.

  에러 메시지 알려주세요.
  ```

---

## 2. PacketGenerator 빌드 검증

```
2단계: 팀장이 만든 PacketGenerator 도구가 본인 환경에서 도는지 확인할게요.
이 도구는 백엔드 작업의 핵심 — 프로토콜 결정(ADR 시리즈)이 실제로
작동하려면 이 도구가 안정적으로 돌아야 해요.

레포 루트에서 다음 실행 결과 알려주세요:

  dotnet build 99_Tools/PacketGenerator/PacketGenerator.csproj
```

**판정**:
- `Build succeeded` → 다음 안내:
  ```
  빌드 OK. 한 번 실행해서 정상 동작하는지 보겠습니다:

    dotnet run --project 99_Tools/PacketGenerator
  ```
  - 정상 실행 (에러 없이 종료) → "OK, 다음으로"
  - 실행 에러 → **STOP**, 에러 메시지 받고 진단
- 빌드 실패 → **STOP**, 에러 메시지 받고 진단

---

## 3. 서버 단독 실행 확인

```
3단계: 서버가 실제로 부팅되는지 확인할게요.
빌드 통과만으로는 충분치 않아요 — 실제 시작 시 누락된 의존성 있을 수 있음.

다음 실행하고 처음 10~20줄 출력 알려주세요. 서버가 listening 상태 되면
Ctrl+C로 종료해주세요.

  dotnet run --project 02_Server/GameServer/GameServer.csproj
```

**참고**: 02_Server 안 정확한 프로젝트 이름은 본인 솔루션 구조에 맞게 조정.
사용자가 `02_Server/` 하위에 어떤 csproj 있는지 모르겠다면 다음 안내:

```
02_Server/ 폴더 안에 어떤 .csproj 파일들 있는지 확인할게요:

  Get-ChildItem -Path 02_Server -Filter *.csproj -Recurse
  (또는 Git Bash: find 02_Server -name "*.csproj")

결과 알려주세요.
```

**판정**:
- 서버 정상 부팅 (listening 또는 ready 메시지) → "OK, 다음으로"
- 부팅 실패 → **STOP**, 에러 메시지 받고 진단:
  - DB 연결 에러 → 1단계 LocalDB 재확인
  - 포트 충돌 → 다른 프로세스가 같은 포트 사용 중
  - 설정 누락 → appsettings.json 확인

---

## 4. VS Code 권장 확장 작동 확인

```
4단계: 공통 5단계에서 VS Code 권장 확장 팝업 떴는지 확인했는데,
백엔드 작업에 필수인 두 확장이 실제 작동하는지 마지막 검증할게요.

VS Code에서 02_Server/ 폴더 안 아무 .cs 파일을 열어주세요.
열고 나서 다음 두 가지 확인해주세요:

1. 우측 하단에 'C# Dev Kit'이 로드 중 또는 ready 표시 되나요?
2. .cs 파일의 코드에 색깔이 입혀지고, 마우스 올리면 타입 정보 툴팁 뜨나요?

둘 다 OK면 알려주세요. 안 되면 어느 게 안 되는지 알려주세요.
```

**판정**:
- 둘 다 OK → "OK, 마지막 단계로"
- C# Dev Kit 로드 안 됨 → **STOP**:
  ```
  ⛔ C# Dev Kit이 작동 안 해요. 학부생 백지에서 자주 막히는 지점.

  체크 순서:
  1. VS Code 좌측 확장(Ctrl+Shift+X) → 'C# Dev Kit' 검색 → 설치됐는지 확인
  2. 안 깔려있으면 설치 → VS Code 재시작
  3. 깔려있는데 작동 안 하면: F1 → "Developer: Reload Window" 실행
  4. 그래도 안 되면 VS Code 완전 종료 후 재실행

  결과 알려주세요.
  ```
- 색깔/툴팁 안 뜸 → 위와 동일하게 진단

---

## 5. 헌법/ADR 안내

```
5단계: 백엔드 셋업 다 끝났어요. 마지막으로 한 가지 권유.

본인 첫 작업 시작하기 전에 헌법(CLAUDE.md)은 한 번 읽어두는 게 좋아요.
길지만 본인이 따라야 할 약속이 박혀있어서, 안 읽고 시작하면 나중에
충돌 발생할 수 있어요.

ADR 문서들은 직접 안 읽어도 돼요 — 일하다 "이건 왜 이렇게 돼있지?"
싶은 게 생기면 저한테 *자연어로* 물어보세요. 학부생 멘토링 톤으로
풀어 설명할게요. (옛 /learn:* 슬래시는 M3.5 새 하네스 v1에서 제거됨 — ADR-022)

CLAUDE.md 한 번 훑었어요? (대충 훑은 정도면 OK, 정독은 아직)
```

사용자 OK 응답 → 단계 03-backend 완료. `.claude/setup-steps/04-finalize.md` 진행.

---

## 변수 박힘 상태

- 단계 01·02 변수 유지
- 백엔드 도구 검증 통과
