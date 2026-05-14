# 03-unity-client — Unity 클라이언트 추가 셋업

> `role == "unity-client"` 일 때만 호출됨.
> 02-common 통과 상태 전제. 8 단계.

---

## 1. Unity Hub 설치 확인

```
1단계: Unity Hub 설치 확인할게요.
Windows 시작 메뉴에서 'Unity Hub' 검색해서 실행해주세요.
```

**판정**:
- Unity Hub 실행됨 → "OK, 다음으로"
- Unity Hub 못 찾음 → **STOP**:
  ```
  ⛔ Unity Hub 설치 안 됐어요.
  다운로드: https://unity.com/kr/download
  '개인용 다운로드' → Unity Hub 받아서 설치.
  설치 후 실행하고 알려주세요.
  ```

---

## 2. Unity 6 LTS (6000.4.1f1) 정확한 버전 설치 확인

```
2단계: 본인이 만든 Dawnholder 프로젝트는 Unity 6000.4.1f1 정확히 같은 빌드
사용해요. 다른 버전이면 .meta 충돌 발생하니 주의.

Unity Hub 좌측 'Installs' 탭 열어서 설치된 에디터 목록 알려주세요.
정확한 버전 번호까지 (예: 6000.4.1f1).
```

**판정**:
- `6000.4.1f1` 정확히 설치됨 → "OK, 다음으로"
- 다른 버전 또는 없음 → **STOP**:
  ```
  ⛔ Unity 6000.4.1f1이 없어요.

  설치:
  1. Unity Hub → 'Installs' → 'Install Editor' 클릭
  2. 'Archive' 탭 → 'download archive' 링크 클릭
  3. 웹페이지에서 6000.4.1f1 찾아서 'Unity Hub' 버튼 클릭
  4. Hub로 돌아와 설치 진행 (Windows Build Support 모듈 포함 권장)

  설치 시간 30분~1시간. 끝나면 알려주세요.
  ```

---

## 3. Unity 라이선스 활성화

```
3단계: Unity 라이선스 활성화할게요.
학생이면 Personal 라이선스 무료로 OK.

Unity Hub 우측 상단 톱니바퀴 → 'License Management' →
'Activate New License' → 'Unity Personal' → 'Use Unity Personal' 선택.

로그인 화면 뜨면 본인 Unity ID로 로그인.
Unity ID 없으면 'create one' 클릭해서 무료 가입.

활성화 완료 알림 뜨면 알려주세요.
```

**판정**:
- 활성화 완료 → "OK, 다음으로"
- 막힘 → **STOP**, 화면 상태 묻고 진단

---

## 4. 03_Client 프로젝트 첫 열기

```
4단계: Dawnholder 클라 프로젝트를 Unity Hub로 처음 열겠습니다.

1. Unity Hub → 'Projects' 탭 → 우측 'Open' 옆 화살표 → 'Add project from disk'
2. 본인 ClaudeDev 레포 안의 03_Client 폴더 선택
3. 프로젝트 목록에 'ClaudeDev' (또는 03_Client) 뜨면 Editor Version이
   6000.4.1f1로 보이는지 확인 → 클릭해서 열기

⚠️ 첫 열기는 5~15분 걸려요. Unity가 Library/ 캐시 빌드하는 시간.
   진행률 바 차분히 기다리세요. 끝나면 에디터 창이 뜹니다.

에디터 열리면 알려주세요.
```

**판정**:
- 에디터 정상 열림 → "OK, 다음으로"
- 에러 발생 → **STOP**, 에러 메시지 받고 진단:
  - 패키지 복원 에러 → 인터넷 연결 확인
  - 버전 불일치 → 2단계 재확인

---

## 5. Unity Cloud 프로젝트 새로 만들기

```
5단계: Unity AI / MCP 기능 쓰려면 프로젝트가 Unity Cloud에 연결돼야 해요.
본인 Unity 계정으로 새 Cloud 프로젝트 만들 거예요 (팀장 계정 X — 각자 자기 1000 크레딧 받음).

Unity 에디터 안에서:
1. 우측 상단 'Edit > Project Settings' → 좌측 메뉴 'Services'
2. 'Create Unity Project ID' 클릭
3. Organization 선택 (본인 개인 조직 — 자동 생성됨)
4. Project Name: 본인이 알아보기 쉽게 (예: "Dawnholder-{slug}")
5. 'Create' 클릭

완료되면 'Project ID'가 표시되고 'Connected'로 바뀝니다.
완료 후 알려주세요.
```

**판정**:
- 'Connected' 상태 → "OK, 다음으로"
- 막힘 → **STOP**, 화면 상태 받고 진단

---

## 6. Unity AI Assistant 작동 확인

```
6단계: Unity AI Assistant가 본인 환경에서 작동하는지 확인할게요.
공통 패키지(com.unity.ai.assistant)는 이미 레포에 박혀있어요 — 별도 설치 X.

Unity 에디터에서:
1. 상단 메뉴 'Window > AI > Assistant' (또는 우측 상단 AI 버튼)
2. Assistant 패널 열리면 무료 체험 가입 안내 뜸 → 가입 진행 (1000 크레딧 받음)
3. 가입 후 채팅창에 간단한 질문 입력: "What is a Unity GameObject?"
4. 답변 받으면 OK

⚠️ 베타 무료 체험은 일정 기간 후 끝나요. 이후엔 월 약 1만원 구독 필요.
   본인 부담 — 향후 팀장과 재논의 가능.

답변 받았어요?
```

**판정**:
- 답변 받음 → "OK, 다음으로 — 진짜 핵심 단계입니다"
- 막힘 → **STOP**, 진단:
  - Cloud 연결 안 됨 → 5단계 재확인
  - 패키지 누락 → Window > Package Manager에서 `com.unity.ai.assistant` 확인

---

## 7. Unity MCP 셋업

```
7단계: 본 셋업의 핵심 — Unity MCP 연결.
이게 되면 VS Code의 Claude Code에서 Unity 에디터를 직접 조작할 수 있어요.
(씬 변경, 게임오브젝트 생성, 콘솔 메시지 읽기 등)

⚠️ 전제: 6단계 통과 (AI Assistant 패키지 설치 + Cloud 연결).
   AI Assistant 패키지 없으면 아래 메뉴가 안 보입니다.

절차:

A. Unity 에디터에서 MCP Bridge 활성화
   1. Edit > Project Settings > AI > Unity MCP Server
   2. 'Unity Bridge' 상태가 'Running' (녹색)인지 확인
   3. 'Stopped'면 'Start' 클릭
   4. (Relay binary는 ~/.unity/relay/ 에 자동 설치됨. Windows: C:\Users\<본인>\.unity\relay\)

B. Claude Code(VS Code 확장)를 MCP 클라이언트로 등록
   1. 같은 설정 페이지의 'Integrations' 섹션 펼치기
   2. 'Claude Code' 찾아서 'Configure' 클릭 → 자동 설정됨
   3. (자동 설정 안 되면 수동 — 막히면 알려주세요)

C. 연결 승인
   1. VS Code의 Claude Code에서 아무 질문이나 던지기 (예: "Unity 콘솔 메시지 읽어줘")
   2. Unity 에디터의 'Pending Connections' 섹션에 클라이언트 정보 뜸
   3. 'Accept' 클릭 → 한 번 승인하면 다음부터 자동 재연결

D. 검증
   1. VS Code의 Claude Code에서: "Read the Unity console messages and summarize any warnings or errors"
   2. Claude가 Unity_ManageScene 또는 비슷한 도구 호출하면 성공
   3. 에디터 콘솔 메시지가 Claude 답변에 포함되면 OK

⚠️ 알려진 함정 — 무료 체험 종료 후
   Free 라이선스 + 체험 종료 상태에서 MCP 연결하면 'Connection revoked'
   에러 발생할 수 있음. 그땐 월 1만원 구독 또는 팀장과 재논의.

D 검증 통과했어요?
```

**판정**:
- 검증 통과 → "OK, 본 셋업의 핵심 단계 통과! 마지막 단계로"
- A 단계 막힘 (메뉴 안 보임) → 6단계 AI Assistant 패키지 재확인
- B 단계 막힘 → STOP, 자동 설정 안 되는 경우 수동 설정 안내:
  ```
  수동 설정 안내:
  VS Code의 Claude Code MCP 설정 파일에 다음 추가:

  {
    "mcpServers": {
      "unity-mcp": {
        "command": "C:\\Users\\<본인 Windows 사용자>\\.unity\\relay\\relay_win_x64.exe",
        "args": ["--mcp"]
      }
    }
  }

  본인 Windows 사용자명 모르면: echo %USERNAME% 으로 확인.
  ```
- C 단계 막힘 (Pending Connections에 안 뜸) → MCP Bridge 'Running' 상태 재확인
- D 단계 막힘 (Claude가 응답 못 함) → 'Connection revoked'인지 확인, 라이선스 상태 점검

---

## 8. Play 모드 진입 확인

```
8단계: 마지막. 03_Client 프로젝트가 실제 작동하는지 확인.

Unity 에디터 상단 ▶ (Play) 버튼 누르세요. 씬이 실행되면서 에러 없이
Play 모드 들어가면 OK.

Play 모드 진입 됐어요? 콘솔에 빨간 에러 있나요?
```

**판정**:
- Play 모드 OK + 빨간 에러 없음 → "OK, Unity 클라 셋업 8단계 다 통과!"
- 빨간 에러 있음 → **STOP**, 에러 메시지 받고 진단:
  - 누락된 스크립트 / 컴파일 에러 → 02_Server와 동기화 문제일 가능성, 팀장에게 보고
  - 씬 못 찾음 → 본인 작업 영역 아닌 가능성, 팀장 확인

---

## 단계 03-unity-client 완료

```
Unity 클라 셋업 다 끝났어요. 마지막으로 한 가지 권유.

본인 첫 작업 시작하기 전에 헌법(CLAUDE.md)은 한 번 읽어두는 게 좋아요.
길지만 본인이 따라야 할 약속이 박혀있어서, 안 읽고 시작하면 충돌 발생 가능.

ADR 문서들은 직접 안 읽어도 돼요 — 일하다 "이건 왜 이렇게 돼있지?"
싶은 게 생기면 저한테 물어보면 그때 풀어 설명할게요. 또는:

  /learn:why <주제>

로 호출하면 관련 ADR 근거로 학부생 톤으로 알려줘요.

CLAUDE.md 한 번 훑었어요? (대충 훑은 정도면 OK)
```

사용자 OK 응답 → 단계 03-unity-client 완료. `.claude/setup-steps/04-finalize.md` 진행.

---

## 변수 박힘 상태

- 단계 01·02 변수 유지
- Unity 환경 + MCP 검증 통과
