# DB 영속 서버로 빌드 시연 준비

11-06 시연을 Windows 빌드 클라이언트와 DB 영속 서버로 할 수 있게 한다. 시연자가 정해진 순서로 DB 서버를 켜고 끄며, 빌드에서 로그인 → 캐릭터 선택·생성 → 마을 입장이 되는 상태가 목표다.

**현재 상태(2026-11-04 23:07 KST):** 인벤토리 24칸 goal이 PR295·PR296 병합으로 끝났다. core-active를 이 branch로 되돌려 main `5dd67b09`를 받았다(로컬 commit `867d967a`). 녹화용 리허설 착수 조건 다섯 중 셋(PR268·Art 격자 PR1·Core 24칸)이 채워졌고, Art PR2 병합과 Content 빌드가 남았다. 지금 보기(완료조건 1)와 DEVELOPMENT 새 절(1차 독립 실사 통과, 결함 0)과 창 8(PR268 검증의 DB 서버 두 판)은 끝난 그대로다. 녹화 실행 순서는 v1이다. 작업자·검증자 pane은 없고, 이 branch는 로컬 commit만 있으며 push하지 않았다. 아래 「11-04 복귀」부터 읽는다.

## 재개 지점

- **11-04 복귀(메인 `msg_32944fc24916`·`msg_4b7239950d8f`):** 인벤토리 24칸 goal이 끝났다. 제품 PR295는 `fbf00de9`, 종료 기록 PR296은 `5dd67b09`로 들어갔고, 리드가 [종료 점검](../2026-11-01-inventory-24-slots/goal.md#종료-점검-준비)을 `msg_0f1aa79e5418`로 보냈다. 메인은 지금 리드를 교체하지 않고 이 pane이 이 goal을 잇게 했다(아래 「적용 중인 메인 결정」 11-04).
  - 2026-11-04T14:06:48Z에 core-active를 24칸 종료 branch(`6a5c53ad`, 변경 0)에서 이 branch(`ad5d3442`)로 바꿨다. 바꾸기 전 core-active 터미널은 리드 하나였고, Unity·dotnet·WSL 세션 프로세스는 0이었다. 사용자 서버 탭은 터미널 목록에 없었다. 근거는 `.backups/verification/2026-10-31-db-persistence-demo-build/lead/return-01/`의 `branch-switch-01.txt`·`terminal-list-before-switch-01.json`·`tasklist-before-switch-01.csv`다.
  - main `5dd67b09`를 받았다(`867d967a`, 충돌 0, tree가 미리보기 `lead/return-preview-5dd67b09-01.txt`와 같음). 서버 쪽 트리(`02_Server`·`04_ClientNet`·`98_Shared`·`99_Tools`)는 `5dd67b09`와 diff 0이다. `ProtocolVersion.Current = 23`이고, 작업 트리 Shared.dll은 v23(sha256 `8d471e72…c025`)이다(같은 폴더 `take-base-verify-01.txt`).
  - 메인 지시: 녹화 순서의 Core 몫(리허설 절차·서버 기동 순서) 준비 계획을 Unity·WSL 창이 필요한 시각·길이와 함께 보고한다. Art PR2가 Unity 창을 약 40분 쓸 예정이라 메인이 겹치지 않게 배정한다.
- **창 8 준비(2026-11-01T01:41Z, 메인 `msg_b40b44ba2d63`):** PR268 검증의 DB 영속 서버 두 판을 core-active의 v22 서버로 돈다. core-active는 24칸 branch(`6b2adad4`, 로컬 커밋, push 안 함)에서 이 branch(`9d35f5cd`)로 바꿨다(`.backups/verification/2026-11-01-inventory-24-slots/lead-branch-switch-01.txt`). 바꾸기 전 core-active Unity는 0이었다(같은 폴더 `lead-branch-switch-check-01.txt`). 작업 트리 Plugins Shared.dll은 v22(sha256 `b8b2828e…ecd3`)다.
  - WSL 복제본은 아직 v23 빌드다. 창 8 첫 단계로 새 Core 작업자가 `sync-wsl.sh build`를 1회 돈다. Content 창 B(01:35:30Z~02:18Z) 동안에는 WSL 실행을 하지 않는다.
  - 사용자 카드 v3: `lead/window8-user-server-card-v3.md`(sha256 `d4a869d0c1a7ca75af844a61c252c385d986ffc7c101ef6a5e7ab6e20d2dbabd`). 1단계 시작 블록은 v2와 bytes가 같다(`lead/window8-card-v3r2-block-compare.txt`). 첫 판(`8f770523…5128`, `msg_3a051b3a622d`)은 origin/main에 없는 DEVELOPMENT 절을 근거로 들었다. 메인 지적(`msg_bb4316bc7ee2`)에 따라 그 근거를 실측 원문 `docs-02/stage-a-observed-lines-v1.txt`와 launcher·Program.cs 줄 번호로 바꿨다(`lead/window8-card-v2-to-v3r2.diff`, `msg_b07763f85e18`). 메인이 sha256·시작 블록 일치를 다시 세고 「창 8은 이 카드로 간다」고 확정했다(`msg_a73bb31b2e79`).
  - 순서표: Core·사용자 줄(P1·W1~W3·W5·W6·W8·P2)은 Core 초안(`msg_a7aa1edf5eef`), Content 줄(W3b·W4·W6b·W7)은 Content 회신(`msg_4dff57d2a72d`)이다. 클라는 PR268 head `cb32652f`의 v22 빌드다. 창 8은 Content 창 B 통과와 Content 검증자의 차단 결함 0 뒤에만 연다.
  - Core가 보는 세 값: 재시작 시간((a) 정지 줄 → (b) Listening), 정지 요약 두 줄, 2판 목록의 캐릭터가 1판과 같은지. 원문은 메인이 사용자 서버 창을 읽어 Content 근거(`pr268-verify-03/window8/server-window/`)에 남긴다.
  - **W1 끝(2026-11-01T02:03Z):** 메인 창 배정 `msg_aad1003cbf55`(01:53:00Z~02:15:00Z). 계약은 생성 스크립트 `build-02/scripts/build-contract-v1.cjs`로 만들었다(`build-02/contract-v1.md` sha256 `bac05af5…2c4c`). 규칙 원문 대조는 ALL VERBATIM이다(`build-02/lead-verbatim-check-v1.txt`).
    - 신규 `[Core Sol]`(첫 화면 `GPT-6.1-Sol max`, backend unknown, Task `task_6f4aebe47fe8`)가 S1 통과 뒤 `sync-wsl.sh build`를 01:56:49Z~01:57:37Z에 1회 돌려 exit 0을 얻었다. worker_done은 `msg_67fe5268ccb4` succeeded, 보고는 `build-02/report.md`(sha256 `a799dc23…e9ec`)다.
    - S3 11항이 통과했다. 복제본 소유 표식이 core-active이고, `GameServer.dll` mtime 01:57:21Z가 빌드 시작 뒤다. 복제본 `ProtocolVersion.cs`는 Windows와 같은 sha256이고 `Current = 22`다. 리드가 정산 뒤 복제본을 직접 다시 읽어 같음을 확인했다(`build-02/lead-sample-check-clone-01.txt`).
    - S4: SQL 서비스 Running, 15433은 sqlservr, 공개 CA hash 일치, 만료 2026-11-17T09:48:58Z. LocalMachine\My에 같은 이름 인증서가 둘 있다. SQL이 실제로 쓰는 thumbprint는 미확인이다.
    - 작업자 heartbeat 간격이 1회 343초였다. 리드가 비차단 기록으로 판정했다(`build-02/lead-reply-heartbeat-v1.txt`).
    - 정산 `retained`/`external_terminal`, pane 닫음. 창 끝 확인에서 WSL 서버·7777·Windows dotnet·Unity가 모두 0이었다(`build-02/lead-window-end-check-01.txt`). 메인에 「창 끝」을 보냈다.
  - **W3(메인 사본 `msg_b5a958fde5e5`):** 사용자 서버가 카드 1단계로 떴다. Owned WSL PID 16172, WSL dotnet PID 505, Listening `2026-11-01T03:11:17.9349601Z`.
  - **창 8 끝(2026-11-01T03:34:20Z, 메인 사본 `msg_3cbec56e9ea5`):** 두 판이 다 돌았고 사용자가 W8 Enter로 정지했다. Content 리드는 1판(`msg_f0f0f290dd92`)과 2판(`msg_fca24e489a94`)의 통과를 보고했다. 서버 창 원문은 메인이 읽어 Content 근거 `content-active/.backups/verification/2026-10-31-demo-autopilot-character-select/pr268-verify-03/window8/server-window/`의 `w3-listening.txt`·`w5-a-stopped.txt`·`w6-b-restarted.txt`·`w8-c-stopped.txt`에 넣었다.
    - Core 값 1, 재시작: (a) 정지 `03:27:04.7844611Z` → 새 프로세스 생성 `03:30:01.6497981Z`(PID 22492) → (b) Listening `03:30:02.4558779Z`. 정지에서 Listening까지 약 2분 58초다. 그 대부분은 메인 「지금」을 기다린 사용자 손 시간이다. 프로세스 생성에서 Listening까지는 약 0.81초다(10-31 지금 보기 약 0.89초).
    - Core 값 2, 정지 요약: (a)·(c) 모두 stdout·stderr `overlong=0, queueDropped=0, decodeErrors=0, readerErrors=0, drained=True`이고 실패 문구는 없다. stdout `excluded`는 (a) 1222, (c) 528이다.
    - Core 값 3, 2판 캐릭터: 두 클라 모두 2판에서 `character create` 없이 1판과 같은 slot 1을 골라 마을까지 갔다(Content 클라 원시 `pr268-verify-03/window8/round-2/`). 재시작 뒤에도 1판 캐릭터가 목록에 남았다는 근거다. 리드는 목록 이름을 따로 대조하지 않았다.
    - Content 중립 관찰(거점 level 2·강화 level 1이 두 판에서 같음)에 Core 해석을 직접 보냈다(`msg_04981e9d8b04`). 두 값은 DB 저장 범위 밖이다(FEATURE_MAP 39·41행, `02_Server`에 `dh.Guild`·`dh.CharacterWeapon`을 읽거나 쓰는 코드 0건). 그래서 판정 근거로 쓰지 않는다. 2판에서 요청 전에 이미 그 값이 보이면 반증이다.
    - P2 전 확인(03:35:13Z): WSL `GameServer.dll` 0, 7777 0, Unity 0, Windows dotnet 0(`.backups/verification/2026-11-01-inventory-24-slots/lead-p2-switch-check-01.txt`).
  - P2: 2026-11-01T03:36:08Z에 core-active를 24칸 branch로 되돌렸다(`.backups/verification/2026-11-01-inventory-24-slots/lead-p2-switch-01.txt`).
  - 다음 서버 실행 카드(리허설)는 결정 107 A대로 1단계 자격 줄을 `$env:DAWNHOLDER_DB_CREDENTIAL_PATH` 우선으로 바꾼다(「적용 중인 사용자 결정」 107 A). 창 8 카드 v3는 바꾸지 않는다.
- **PR268 병합(메인 사본 `msg_11107f0e032c`):** 사용자 승인 줄(head `6f4f721aaf104dc002e16eccadf95dc10e12e3f6`) 뒤 메인이 2026-11-01T04:22:01Z에 병합했다. merge commit은 `3bb767726e7e10417c8347e65e003a1fcfb6099c`다. PR268이 먼저 들어가 v22 재빌드 조건은 없어졌다.
- **11-01 기록 전환(08:27Z경):** 이 기록을 쓰려고 core-active를 잠깐 이 branch로 바꿨다. 바꾸기 전 24칸 branch는 `11fd1824`, 변경 0이었다. 사용자 서버 탭은 정지 뒤 입력 화면, Windows dotnet 0, Unity 0이었다(`.backups/verification/2026-11-01-inventory-24-slots/lead-g1-record-switch-01.txt`). 커밋 뒤 24칸 branch로 돌아간다.
- 다음 첫 일: 메인의 리허설 실행 창 배정을 기다린다. 착수 조건은 다섯이다.
  - PR268 병합(채움, 위 항목)
  - Art 인벤토리 격자 PR1 병합(결정 92 B, `msg_e8defeeffd1f`). 채움: PR294 `a888cd35`(2026-11-01T11:39:32Z)
  - Core 인벤토리 24칸 PR 병합(결정 97 A, `msg_3ce270803253`). 채움: PR295 `fbf00de9`(2026-11-04T12:13:47Z)
  - Art PR2 병합(HUD 골드 제거·스킬칸, 「적용 중인 사용자 결정」 11-01 HUD 지시)
  - Content 빌드 경로·exe sha256
  - 병합 순서(메인 결정 `msg_495eff183bf0`): Art 격자 PR1 → Core 24칸 → Art PR2(HUD 골드 제거·스킬칸 좌하단 4칸·6줄 캡처) → Content 녹화 빌드. Art PR2의 6줄 캡처는 24칸이 main에 있어야 찍힌다. 녹화 빌드 기준(다섯 PR이 모두 들어간 main)은 그대로다.
  - 24칸 PR은 프로토콜 버전을 올린다. 그래서 리허설 전에 Content가 알리는 고정 head(다섯 PR이 모두 들어간 main) 40자를 이 branch에 그대로 받고 서버를 다시 빌드한다(Core가 Content에 보낸 답 `msg_45e700da3330`). Core가 돌려줄 것은 이 branch HEAD 40자, 서버 쪽 트리(`02_Server`·`04_ClientNet`·`98_Shared`·`99_Tools`) diff 0 원시, `ProtocolVersion.Current` 줄, R1 exit다. 새 Core 작업자는 Codex Sol max로 띄운다.
- 24칸 작업: 메인이 범위 초안을 요청했다(`msg_3ce270803253`). 초안은 `.backups/verification/2026-11-01-inventory-24-slots/scope-draft-v1.md`(sha256 `8dba8548…1a95`)다. 메인이 `msg_3dfd5496b065`(18:04:08Z)로 1 가·2 동의·3 가를 승인했다.
  - 새 goal `2026-11-01-inventory-24-slots`를 같은 리드가 먼저 끝내고 이 goal의 리허설로 돌아온다. R-8 교체는 두 goal이 다 닫힌 뒤 한 번이다.
  - 이 goal의 미커밋은 이 branch에 로컬 WIP 커밋으로 두었고 push하지 않는다. 돌아오면 이 branch로 switch하고 Content 고정 head를 merge한다. 리허설 뒤에는 녹화 순서 v2, 서버 계약 갱신, 바뀐 줄의 재실사, Core PR 순으로 간다.
- 그 전에 올 일: Content 리드(`run:run_0a58bc3cfea0`)가 PR268 검증용 DB 서버 한 판의 사용자 서버 기동 시각을 맞추러 연락한다(메인 `msg_f2632f509767`). 받으면 같은 서버 세션 안에서 Core가 함께 볼 확인(같은 창 재시작, 같은 로그인의 같은 캐릭터 등 녹화 순서 v1의 R6·R7)을 Content와 같이 메인에 올린다. 실행 창은 메인 배정 전에는 쓰지 않는다.
- DEVELOPMENT 절 1차 독립 실사(2026-10-31): 신규 `[Core 검증자]` Astra(Task `task_6b358830c0a3`, Dispatch `ctx_72557501f413`, 계약 `verify-01/contract-v1.md` sha256 `1d9d6401…a98f`)가 확인할 것 1~8을 모두 「검토했고 지적 없음」으로 판정했다. 결론은 **문서 실사 통과, 결함 번호 없음**이다(worker_done `msg_871299b11356`, `verify-01/verdict.md` sha256 `292cb149…0677`).
  - 리드가 판정 원문을 다 읽고 원시를 표본 대조했다(`verify-01/lead-sample-check-v1.md`). 표본은 시작 블록, 기존 줄 보존, 링크, 메모 선작성, 쓰기 범위다.
  - 검증 중 공식 질문은 두 건이었다. 하나는 goal hash 변경으로, `msg_164e4901d6d7`로 허용했다. 다른 하나는 메모 선작성 원시로, `msg_2756c5c4448e`로 작업자 대화 기록과 추출 스크립트를 줬다.
  - 이 통과는 리허설·런타임 검증이 아니다. 리허설 뒤 DEVELOPMENT가 바뀌면 그 줄만 새 검증자가 다시 본다.
  - 검증자 첫 화면에 Codex 업데이트 선택창(0.165.0 → 0.166.0)이 떴다. 리드는 입력하지 않고 보고했고(`msg_7ca568429dd5`), 메인이 Esc로 건너뛰었다. 버전은 0.165.0 그대로다(`msg_051a22411fd0`). 기록은 `verify-01/lead-launch-record-v1.md`에 있다.
- DEVELOPMENT 새 절 수정(2026-10-31): 신규 `[Core 구현 Opus]`(Task `task_da29bc8ab106`, worker_done `msg_42adaf27e603`, 계약 `docs-02/contract-v1.md` sha256 `1737284e…2ac5`)가 D1·D2와 실측 정지 줄을 반영했다. 결과는 sha256 `ee8f19d07c342932abcfa84acc9be6ba2971ef9bad7ad8ad940ef963ef67e90e`, blob `b4122591`이고 v1 대비 +21/-7이다.
  - 리드 대조는 `docs-02/lead-review-v1.md`에 있다. 작업자가 더한 「요약 값이 0이 아니거나 `drained=False`면 launcher 문구로 끝남」은 launcher 277~281·590·597행과 맞아 받아들였다.
  - 작업자는 release(`retained`/`external_terminal`)하고 pane을 닫았다.
- 지금 보기 정지(2026-10-31): 사용자 원문(메인 창 Enter 제출, 19:1x KST)은 「대시보드 결정 응답: 1) 84 - 플레이 끝: DB 서버 끄기(Enter)와 Editor 닫기(네 손) → A 둘 다 끝 · 2) 85 - 캐릭터 칸을 지금 4개로 늘릴지(DB 구조 변경 포함), 3칸으로 화면만 먼저 고칠지 → A 3칸으로 화면만 지금」이다. 원천은 메인 `msg_f48e78cac18f`(10:12:25Z)다.
  - 메인이 사용자 탭에서 읽은 정지 줄은 `[Persistence stdout 2026-10-31T10:11:31.8967507Z] Server stopped.`이다. 이어서 stdout·stderr 출력 수집 요약 두 줄이 나왔다. 둘 다 `queueDropped=0, decodeErrors=0, readerErrors=0, drained=True`이고, stdout은 `excluded=2206`, stderr는 `excluded=0`이다. 그 뒤 PowerShell 입력 화면으로 돌아왔고 실패 문구는 없다.
  - 리드가 10:12:50Z에 Windows 쪽에서 읽기 확인을 했다. `netstat`에 `:7777` 줄이 0개였고 PID 23628은 없었다(`lead/stage-a-port-release-check-v1.txt`, sha256 `b5a65e6a…38f3`). Listening 중에 Windows `netstat`이 WSL 소켓을 보여 주는지는 재지 않았다. 그래서 이 확인은 「Windows 쪽에서 7777 점유가 보이지 않음」까지만 뜻한다.
  - 85 A로 칸은 3개 그대로다. Core의 DB·서버 작업은 없다.
- 지금 보기 화면 확인(2026-10-31): 사용자가 DB 영속 서버 + effect-active Editor로 새 캐릭터 선택창을 봤다. 목록에는 10-29 시험 캐릭터 「시험새벽1029」가 있었다. 원천은 메인 `msg_5fcbec368280`(10:03:06Z)이다. 사용자 원문(메인 창 Enter 제출, 19:0x KST, 대시보드 결정 78 C 코멘트)은 「플레이에는 문제가 없음, 근데 캐릭터 선택창에 빨간 박스 부분으로 내가 강조한 부분이 쓸데없이 공간을 많이 차지함, 캐릭터 선택 슬롯을 4개로 하고, 빈 캐릭터칸 선택시 새로운 캐릭터 생성 UI로 진입하게 수정, 나머지는 괜찮음 [Image #2]」이다.
  - 4칸 요청은 메인이 사용자 결정 85로 올렸고, 사용자가 A(3칸으로 화면만 지금)를 골랐다(위 정지 항목).
- DEVELOPMENT 새 절 v1(2026-10-31): 신규 `[Core 구현 Opus]`(Task `task_d2aa1d141752`, worker_done `msg_f12f53b5db93`)가 43~116행 새 절과 「결과 해석」 한 문장을 썼다. 결과는 blob `b96c04bf`, numstat 75/1이다. 리드 대조는 `docs-01/lead-review-v1.md`에 있다.
  - D1: 실패 표의 `Persistence credential handoff failed; …`는 launcher 바깥 `catch`가 `Persistence launcher did not complete; …`로 바꿔 던져서 화면에 남지 않는다.
  - D2: 정상 시작 줄은 두 줄이 아니라 세 줄이다(PID 줄 → launched 줄 → 접두 붙은 Listening 줄).
  - 작업자는 release(`retained`/`external_terminal`)하고 pane을 닫았다.
- 지금 보기 시작(2026-10-31): 사용자가 카드 v2 시작 블록을 그대로 붙여 넣어 영속 서버를 켰다. 사용자 원문(메인 창, 18:52 KST)은 「서버 켜졌어」다. 서버 출력은 `Owned WSL process PID 23628, birth UTC 2026-10-31T09:52:15.7540919Z.`와 `[Persistence stdout 2026-10-31T09:52:16.6444165Z] Listening on 0.0.0.0:7777. Press Enter to stop.`이다. 프로세스 생성에서 Listening까지 약 0.89초다. 원천은 메인 `msg_f6b9c253aef7`이다.
- 지금 보기 빌드(2026-10-31): 신규 `[Core 구현 Opus]`(Task `task_a71be586ed73`)가 core-active `sync-wsl.sh build`를 09:27:42Z~09:28:28Z에 1회 돌려 exit 0을 얻었다. 관문(물리·commit 여유)·복제본 소유 표식·`GameServer.dll`·SQL 서비스·15433·공개 CA를 리드가 원시로 대조했다. 인증서 만료는 2026-11-17T09:48:58Z다. 근거는 `build-01/report.md`와 `build-01/raw/`, 메인 보고는 `msg_e820dd1177b3`이다. SQL이 실제로 쓰는 인증서 thumbprint는 미확인이다.
- 사용자 손 카드: `.backups/verification/2026-10-31-db-persistence-demo-build/lead/stage-a-user-server-card-v2.md`(sha256 `3d2eca0293558ddc3b5a12c02a9d4cab3da88aa7f0508f6687783211c0ba1196`). v1은 Legacy 정지 키를 Enter로 잘못 적어 v2에서 Ctrl+C로 고쳤다(메인 `msg_90742ec85de7`). 10-29 실제 카드와의 차이 보고는 메인 `msg_d72b4ba7409a`로 보냈다.
- Run `run_ee7c33fd5873`, 리드 `[Core 리드 Opus]`, branch `feat/db-persistence-demo-build-20261031`(base `f5adcf31`). 근거 폴더는 `.backups/verification/2026-10-31-db-persistence-demo-build/`다.

## 적용 중인 사용자 결정

### 2026-10-31 「DB 버전으로 시연 가능하게」 — 메인 전달

메인 `msg_2aba8d03d206`(2026-10-31T09:02:30Z)이 전달한 사용자 원문이다. 메인 pane에서 Enter로 제출됐다. 이 Core pane 직접 입력으로 격상하지 않는다.

- 직접 플레이 중 사용자: 「캐릭터 선택창으로 넘어가는데 아니라 바로 클래스 선택창으로 넘어가는데?」
- 사용자 지시: 「DB 버전으로 시연 가능하게 해주고, 지금 수정해줘」
- 함께 걸린 사용자 결정 72 B(10-31): 「11-06 시연은 빌드로 한다」. 최종 제출은 11-06이다.
- 메인 설명: 지금 띄운 `run-server.bat` 서버는 DB 없는 Legacy라서 캐릭터 메뉴 요청에 LegacyFlowOnly로 답한다. 그래서 클라이언트가 장면의 Knight·Mage 클래스 버튼으로 간다. 새 캐릭터 선택창은 영속 서버에서만 뜬다.

### 2026-10-31 결정 81 A·80 A·79 C — 지금 바꿈, DB 쓰기 허용, 시연은 영상

메인 `msg_162fe44c938c`(2026-10-31T09:23:32Z)가 전달한 사용자 원문이다. 메인 창에 Enter로 제출됐다(18:2x KST). 이 Core pane 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 2) 81 - 지금 Legacy 서버를 끄고 DB 서버로 바꿀지(새 캐릭터 선택창 보기) → A 지금 바꿈 · 3) 80 - 개발 DB에 시험 계정·캐릭터 행을 새로 만들어도 될지 → A 허용 · 4) 79 - 11-06 시연 PC를 이 데스크톱으로 할지 → C 코멘트 「시연은 영상으로 보여줄 예정임」

- 81 A: 단계 A를 지금 한다. 실행 창은 빌드 완료 보고부터 사용자의 「Server stopped.」까지 core-active 영속 서버 1개·7777이다. 메인이 카드 v2(sha256 `3d2eca02…1196`)를 0단계부터 사용자에게 주고, 끝나면 창을 닫는다.
- 80 A: 단계 A·리허설·녹화에서 개발 DB `Dawnholder_Char_8256bcde8c80`에 새 시험 계정·캐릭터 행 생성을 허용한다. 삭제·DDL 0, 이름은 시험용이 드러나게 정한다.
- 79 C: 11-06에는 현장 실행 대신 미리 녹화한 영상을 보여 준다(메인 해석, 초안 v2에서 확인). 녹화는 이 데스크톱에서 하므로 노트북 goal은 필요 없다. 단계 B는 영상 기준으로 다시 짜 범위 초안 v2로 보낸다.
- 초안 v1에서 메인이 올린 안건 번호: 시연 PC 79, DB 쓰기 80, 지금 바꿀지 81(`msg_0d7e73a9805d` 3항).

### 2026-10-31 결정 82 B·83 A — 자동 진행기로 DB 서버 한 판, PR268은 검증 뒤 병합

메인 `msg_a2f06f299dce`(2026-10-31T09:37:13Z)가 전달한 사용자 원문이다. 메인 창에 Enter로 제출됐다(18:3x KST). 이 Core pane 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 2) 82 - 시연 영상을 두 번 나눠 녹화해 편집으로 이을지, 자동 진행기를 고쳐 DB 서버 한 판으로 찍을지 → B 자동 진행기를 고쳐 한 판 · 3) 83 - 자동 진행기(PR268)를 독립 검증 뒤 병합해 최신 GUI 빌드로 녹화할지 → A 검증 뒤 병합

- 82 B: 녹화는 PR268 자동 진행기를 고쳐 DB 영속 서버 한 판으로 찍는다. Core 추천(나눠 녹화)과 다르다.
- 83 A: PR268은 최신 main을 합치고 독립 검증을 거쳐 병합한 뒤, 그 main 빌드로 녹화한다.
- 같은 메시지의 메인 결정(사용자 결정 아님): 자동 진행기 수정 goal은 R-8 뒤 새 Content 리드가 맡는다. 빌드 계약도 같은 리드다.
- Core에 바뀌는 것: 녹화 실행 순서를 자동 진행기 두 클라(기사·마법사) + 영속 서버 한 판 기준으로 다시 적는다. Core 몫은 서버 쪽 순서·대기 시간·정지/재시작 위치다. 녹화용 리허설은 PR268 병합 뒤 main 빌드로 하고, 그 전에는 DEVELOPMENT 새 절을 먼저 한다. F8·F9 치트의 영속 모드 동작과 DB 반영, 캐릭터 이름 규칙·계정당 수·삭제 경로를 읽기 전용 소스 확인으로 정리해 Content 새 리드에게 넘길 서버 계약으로 메인에 보낸다.

### 2026-10-31 결정 87 C — Codex CLI 0.166.0 업데이트

메인 `msg_91cb212879ea`(2026-10-31T11:34:25Z)가 전달한 사용자 원문이다. 메인 창에 Enter로 제출됐다(20:3x KST). 이 Core pane 직접 입력으로 격상하지 않는다.

> … 2) 87 - 새 Codex 창마다 뜨는 0.166.0 업데이트 창을 「다음 버전까지 건너뛰기」로 한 번 넘길지 → C 코멘트 「업데이트 해도 돼니까 직접 해줘」

- 10-30 결정 57 A(0.165.0 유지)를 대신한다. 메인이 11:33:50Z~11:34:03Z에 `npm install -g @openai/codex@0.166.0`(exit 0)을 실행했다. 그 뒤 `codex --version`은 `codex-cli 0.166.0`이다.
- Core에 바뀌는 것: 다음 `[Core 검증자]` 기동 기록의 버전 칸은 0.166.0으로 적는다. 첫 화면에 업데이트 선택창이 뜨거나 `GPT-6-Astra xhigh`가 없으면 키를 누르지 않고 메인에 알린다(R-6). 대체 모델로 바꾸지 않는다. 끝난 verify-01의 0.165.0 기록은 그대로 둔다.

### 2026-10-31 결정 90 A — 녹화 길만 Claude로

메인 `msg_f2632f509767`(2026-10-31T14:53:28Z)이 전달한 사용자 원문이다. 메인 창에 Enter로 제출됐다. 이 Core pane 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 1) 90 - Claude 주간 사용량 82% — 이 속도면 내일 저녁 소진, 리셋(화 20:00)까지 약 50시간 멈춤 → A 녹화 길만 Claude로

- A안 글은 「Content(자동 진행기·선택창 3칸)와 Core(녹화)만 계속한다. Art는 PR4 캡처·검증까지만 하고 75(버튼 진하게)는 리셋 뒤로 미룬다. 메인은 대기 간격을 늘리고, 기다리는 리드는 현황판 대기로 둬 자동 깨움을 줄인다」이다. 리셋은 2026-11-03 20:00 KST다.
- Core는 녹화 길이라 계속한다. 대기 중 턴을 아끼고, 사용량이 리셋 전에 바닥나도 이어 갈 수 있게 재개 지점을 지금 상태와 맞춰 둔다.

### 2026-10-31 결정 92 B·93 A와 작업자 Codex 전환

메인 `msg_e8defeeffd1f`(2026-10-31T16:46:14Z)가 전달한 사용자 원문이다. 메인 pane에서 Enter로 제출됐다. 이 Core pane 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 1) 92 - 인벤토리 4칸 격자 개편을 리셋 뒤에 할지, 지금 녹화 준비와 함께 할지 → B 지금 함께 · 2) 93 - 아이콘만 남은 칸에서 아이템을 어떻게 쓸지(지금은 줄마다 「사용」 버튼) → A 우클릭으로 사용

> 그러면 작업자를 Codex로 바꿔줘, 이번에 사용한도 다시 2배 되었거든

- 92의 앞선 사용자 지시는 「그리고 추가 개선안으로 인벤토리는 4xN 형태로 만들어줘, 그리고 각 칸은 아이콘만 띄우고, 마웃스 포인터로 올려놨을때 상세표기가 나오게 개편하자」다. Art가 인벤토리 4열 격자를 녹화 준비와 함께 만든다.
- 녹화 영상에 새 격자가 나와야 한다. 그래서 녹화 빌드의 입력에 PR268과 함께 Art 인벤토리 격자 PR의 병합이 더해진다. Core 리허설의 착수 조건도 같이 바뀐다. 순서가 바뀌면 메인이 알린다.
- 작업자 전환은 메인 해석이다(사용자 결정 아님). 「작업자」는 구현자와 선행 시험 작성자다. 새 Core 작업자 계약부터 신규 `codex --model gpt-6.1-sol -c model_reasoning_effort=max --disable fast_mode`, 태그 `[Core Sol]`을 쓴다.
- 같은 계약에서 Sol 구현이 두 번째 독립 불합격을 받으면 다음 시도는 신규 `gpt-6-astra` xhigh, 태그 `[Core 구현 Astra]`다.
- 리드는 Claude Opus 그대로이고, 독립 검증자는 신규 GPT-6-Astra xhigh 그대로다.
- R-5 첫 화면에 모델이나 max가 없으면 대체하지 않고 메인에 보고한다. Codex 0.166.0이라 업데이트 창은 없어야 하며, 뜨면 메인 지시대로 Esc로 넘긴다.
- 지금 돌고 있는 Core 작업자는 없다. CLAUDE.md 4A 절의 정본 반영은 메인이 따로 한다.

### 2026-10-31 결정 97 A — 인벤토리 24칸

메인 `msg_3ce270803253`(2026-10-31T17:52:45Z)이 전달한 사용자 원문이다. 메인 pane에서 Enter로 제출됐다. 이 Core pane 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 1) 97 - 인벤토리를 24칸(4×6)으로 늘릴지, 8칸(4×2)으로 둘지 → A 24칸으로 늘림

- 그 앞 사용자 원문은 「아니면 인벤토리 크기를 더 늘려도 돼, 내 생각에는 메이플스토리 인벤토리 크기 정도면 상관없어」다.
- 97 A 선택지 문장은 「Core가 지금 인벤토리 패킷과 서버 칸 수를 24로 바꾸고 서버·Unity 양쪽을 검증한다. Art 격자는 칸 수를 따라 4×6이 되고 창은 지금 390×720 가까이 된다. 시연 빌드는 Core PR까지 기다린다」다.
- 이 goal에 바뀌는 것은 리허설 착수 조건 하나다(재개 지점). 24칸 작업 자체는 새 goal 기본이다.

### 2026-11-01 결정 104 A — W1 빌드 끝나는 대로 창 8 사용자 손

메인 `msg_1f7f9ba6bd67`(2026-11-01T01:55:16Z)이 전달한 사용자 원문이다. 메인 pane에서 01:54Z경 Enter로 제출됐다. 이 Core pane 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 1) 104 - 자동 진행기 PR268 마지막 확인(DB 서버 두 판) 사용자 손을 언제 할지 → A 빌드 끝나는 대로 바로

- 메인 지시: W1 「창 끝」(S3 한 줄 포함)이 오면 메인이 바로 사용자 손(카드 v3 `d4a869d0…` 1단계)을 연다. 창 8 동안 core-active는 이 branch(v22) 그대로이고, WSL 실행은 사용자 서버만이다.
- 메인 지시: P2(24칸 branch 복귀)는 W8 「멈췄음」과 메인의 (c) 화면 확인 뒤에만 한다.

### 2026-11-01 결정 107 A — DB 자격 파일 경로는 사용자 PowerShell 프로필 변수

메인 `msg_cb9dbb9c4100`(2026-11-01T02:45:21Z)이 전달한 사용자 원문이다. 메인 pane에서 02:42Z경 Enter로 제출됐다. 이 Core pane 직접 입력으로 격상하지 않는다. 메인은 범위 판단이 아니라 다음 서버 실행 카드의 손 절차 변경이라고 했다.

> 앞으로 자격증명 경로도 같이 결정대기에 포함시켜줘, 코드 내부에 삽입하는게 보안적으로 안 좋은건 알겠는데, 매번 입력하려니까 불편하네

> 대시보드 결정 응답: 2) 107 - DB 자격 파일 경로를 매번 치지 않게: 내 PowerShell 프로필에 한 번 저장할지, 결정 카드에 경로를 넣을지 → A 프로필에 한 번 저장

- 창 8은 카드 v3(`d4a869d0…dbabd`) 그대로 간다. 지금 바꾸지 않는다.
- 창 8 뒤 다음 서버 실행 카드부터 1단계 블록의 자격 줄 하나를 아래 두 줄로 바꾼다. 나머지 줄과 launcher·제품 코드는 그대로다.

  ~~~powershell
  $credentialPath = $env:DAWNHOLDER_DB_CREDENTIAL_PATH
  if ([string]::IsNullOrWhiteSpace($credentialPath)) { $credentialPath = Read-Host '본인이 보관한 DB 자격 파일 전체 경로' }
  ~~~

- 카드 「준비」 절은 「프로필에 저장했으면 묻지 않는다, 없으면 그 입력 요청 한 곳에만 직접 친다」로 고친다. 「경로를 채팅·메시지에 붙이지 않는다」와 「AI가 파일 내용·크기·hash를 보지 않는다」는 그대로 둔다.
- 변수 이름은 `DAWNHOLDER_DB_CREDENTIAL_PATH`로 고정한다. 사용자가 자기 PowerShell 7 프로필에 직접 넣고, 메인이 그 한 줄을 안내한다. AI는 프로필 파일과 그 변수 값을 읽거나 출력하지 않는다. 카드·계약·스크립트에는 값 대신 변수 이름만 쓴다.
- 정본(DEVELOPMENT 등) 반영은 시연 뒤 후보로 둔다.

### 2026-11-01 결정 109 A — 녹화·리허설 이름은 Core 초안

메인 `msg_51e76196ae82`(2026-11-01T04:26:35Z)가 전달한 사용자 원문이다. 메인 pane에서 Enter로 제출됐다. 이 Core pane 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 1) 109 - 11-06 녹화 영상에 보일 캐릭터 이름: Core 초안(새벽기사·새벽법사)으로 갈지, 직접 정할지 → A Core 초안 그대로

- 리허설: 계정 `rh1101_knight`·`rh1101_mage`, 캐릭터 「리허설기사」·「리허설법사」.
- 녹화: 계정 `rec_knight`·`rec_mage`, 캐릭터 「새벽기사」·「새벽법사」.
- 원천은 Core 초안 `msg_035b91a40065`(`.backups/verification/2026-11-01-inventory-24-slots/msg-recording-build-answers-to-content.txt` 3절)다. 완료조건 5(리허설과 녹화의 로그인·이름을 미리 다르게 정함)의 이름 부분이 채워졌다.

### 2026-11-01 결정 110 A — 녹화 두 클라 창은 모니터마다 1920×1080

메인 `msg_a675e965eb96`(2026-11-01T04:38:41Z)이 전달한 사용자 원문이다. 메인 pane에서 Enter로 제출됐다. 이 Core pane 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 1) 110 - 녹화 때 기사·마법사 두 클라 창: 모니터마다 큰 창 하나씩, 한 모니터에 작은 창 둘 나란히 → A 모니터마다 1920×1080

- 기사 클라는 주 모니터, 마법사 클라는 보조 모니터(주 모니터 왼쪽)에 각각 1920×1080 창이다(`-screen-width 1920 -screen-height 1080 -screen-fullscreen 0`).
- 녹화는 사용자가 쓰는 도구로 두 화면을 따로 잡아 편집에서 합친다. 메인·리드는 녹화 도구를 띄우지 않는다.
- Core 할 일은 녹화 실행 순서 R2·R4의 창 크기를 이 값으로 맞추는 것뿐이다(메인 지시). R2·R4는 아래 「녹화 실행 순서」에 반영했다. 창 위치·잘림 확인은 Content 계약 고정본 몫이다(Core 답 `.backups/verification/2026-11-01-inventory-24-slots/msg-recording-contract-v4-answers-to-content.txt`).

### 2026-11-01 사용자 직접 지시 — HUD 골드 제거·스킬칸 좌하단 4칸, 녹화는 그 뒤

메인 `msg_6a8fa9c2a0a8`(2026-11-01T08:20:30Z)이 사본으로 전달한 사용자 직접 지시 원문이다. 메인 pane에 제출됐다. 이 Core pane 직접 입력으로 격상하지 않는다.

> OK 여기서 이제 인벤토리외에 존재하는 골드 표기 UI는 제거하고, 스킬칸은 좌측 하단으로 옮겨버리자
> 스킬칸은 최대 4개로 각각 하나씩 표기하고, 빈 스킬칸은 70% 투명도로 표기
>
> 거기까지 진행하고 마무리하고 영상 새로 시연용으로 찍고 마무리하자

- 메인 정리: 실행은 Art PR2(Art 격자 PR1 병합 뒤)다. Core 24칸 순서는 그대로다(Art PR1 병합 → Core 24칸).
- Core 할 일 (1), 확인: 녹화 순서 R0~R8, 자동 진행용 서버 계약 v1, 서버·공유·도구·봇, Core 소유 클라 시험은 HUD 「Gold」 상자나 스킬칸 자리를 보지 않는다. 원시는 `.backups/verification/2026-11-01-inventory-24-slots/lead-hud-gold-skill-grep-01.txt`(24칸 branch HEAD `11fd1824`)다. Art 리드 `msg_20e41580df04`와 메인 `msg_23564042e812`로 알렸다.
  - Art에 전한 경계 하나: HUD 골드는 `HudController`의 `InventoryState` 구독만 걷고, `InventoryState`와 36번 수신은 그대로 둔다.
  - 경계 둘: 서버 `SkillCatalog` 기준 기사 1개·마법사 2개라서 4칸이면 빈 칸이 3개·2개다. 빈 칸은 `C_SkillUse`를 보내지 않아야 한다(보내면 서버 `[Trust]` 로그가 녹화 서버 로그에 섞임).
- Core 할 일 (2), 녹화 기준: 녹화 빌드 기준을 「PR-A·PR268·Art 격자 PR1·Core 24칸·Art PR2가 모두 들어간 main」으로 고쳤다(아래 「녹화 실행 순서」 머리 줄). Content 계약 초안 v6(sha256 `7f56cd4b3279ace6c84694ef85ba094835424fe7d780ec051e703e34a65f107a`, `msg_01d67753c36f`)도 같은 다섯 PR을 적었다. Core 확인 메시지 `msg_e0f89b875125`와 엇갈렸고 내용은 같다.
- 정정: Core가 처음 메시지에 「Art PR1 → Core 24칸 → Art PR2」로 적은 것은 정해진 순서가 아니었다. Art `msg_494eb3ba541b`·메인 `msg_a3fe4d659794`로 바로잡았다. Core 24칸 PR이 바꾸는 파일은 HUD 쪽 파일과 겹치지 않는다(client.md는 Core 38행, HUD 44·46행). 목록 원시는 `.backups/verification/2026-11-01-inventory-24-slots/lead-core24-changed-files-01.txt`다.

## 적용 중인 메인 결정

메인 `msg_0d7e73a9805d`(2026-10-31T09:12:08Z). 사용자 결정이 아니다.

- 범위 초안 v1(sha256 `8e7e6b77dd87524ee86bced12de7a7a43405308dcbe9b620f5b1b2985719ab9c`)은 사용자 지시와 차이가 없어 착수를 허가한다. 「코드 수정 0, 결함일 때만 최소 수정」과 「시연용 새 시작 도구 안 만듦」을 받는다. 시연자가 명령을 틀리는 관측이 나오면 메인에 올린다.
- 빌드 소유: 72 B 빌드를 맡은 goal은 아직 없다. Content 계약으로 받는다. 상대는 R-8로 새로 열릴 Content 리드이며, 메인이 그 진입 메시지에 이 계약을 넣는다. 리허설은 중간 빌드로 하고, 시연 빌드는 마지막 병합 뒤 고정 head로 다시 만든다.
- 실행 창: core-active의 `sync-wsl.sh build` 1회만 지금 허가한다. 7777·DB 연결·launcher는 결정 81 답 뒤 별도 배정이다. 실행 직전 물리 여유 6GiB 이상·commit 여유 4GiB 이상을 저장 출력으로 남기고, 7777은 Legacy가 쓰는 중이라는 사실을 기록한다.
- 신규 `[Core 구현 Opus]` 계약은 메인 대조 없이 발행해도 된다. 사용자 복사용 launcher 명령은 10-29 실제 명령과 대조한 결과를 메인에 먼저 보낸다.
- 인증서 만료·SQL 서비스는 빌드 뒤 launcher 전에 읽기 전용으로 확인한다. 서비스 상태 조회만이며 시작·변경은 하지 않는다.

메인 `msg_ff94804f5f76`(2026-10-31T09:27:58Z). 사용자 결정이 아니다.

- 범위 초안 v2(sha256 `855d9a16a3d247f3444b6024f6b825db2e565991a4d0d8bfd012ce9b8e339731`)는 79 C와 차이가 없어 단계 B v2를 받는다. 녹화 구성이 정해질 때까지 단계 B 중 DEVELOPMENT 새 절만 먼저 해도 된다.
- PR268 대조: 메인도 `CharacterSelectController.cs:62~66`과 PR268의 `OnKnightClicked`/`OnMageClicked` 호출을 직접 봤고 판단이 같다. 로그인 단계가 찾는 `AccountNameInput`·`ServerHostInput`은 PR281 뒤 main의 `00.Menu/MainMenu.unity`에도 남아 있어 그 단계는 막히지 않는다. PR268 head `1237cb58`, 주인은 메인(사용자 결정 21A), 독립 검증 없음.
- 녹화 구성은 사용자 결정 82, PR268 검증·병합은 사용자 결정 83으로 올라갔다. 추천은 둘 다 A(나눠 녹화, 검증 뒤 병합)다.
- 녹화는 메인이 한다. 10-28 PR268 실측에 쓴 저장소 밖 ffmpeg 데스크톱 녹화 방식이다.
- DB 장면의 서버 시작·자격 경로 입력·정지 Enter는 사용자 손이다. 82 A면 DB 장면의 클라 조작도 사용자 손이다.
- 녹화 클라는 빌드다(72 B). 빌드는 Content 계약 그대로다.
- 사용자용 PowerShell 7 탭 「DB 서버 (사용자)」(`term_3a5ec295`, core-active 루트)를 메인이 열어 뒀다. Core는 그 탭에 입력하지 않는다.

메인 `msg_e6e568f58c4c`(2026-10-31T10:30:22Z). 사용자 결정이 아니다.

- 서버 계약 v1.1(sha256 `ced3f61f…a2ed`)은 메인이 새 Content 리드 진입 메시지에 v1 대신 경로·sha256으로 넣는다.
- 문서 PR은 나누지 않는다. PR 경계는 리허설 기록까지 Core PR 하나다. Content는 메인이 넘기는 경로로 DEVELOPMENT 절과 서버 계약을 읽는다.
- 리허설 전까지 DEVELOPMENT 절의 신규 `[Core 검증자]` 독립 실사를 먼저 해도 된다(리드 재량). 리허설 뒤에는 바뀐 줄만 다시 본다.
- PR268 병합과 Content 빌드가 준비되면 메인이 리허설 실행 창을 배정한다.

메인 `msg_f2632f509767`(2026-10-31T14:53:28Z). 사용자 결정 90 A를 뺀 나머지는 메인 결정이다.

- Content 새 리드(`run:run_0a58bc3cfea0`)가 범위 승인을 받았다. 순서는 PR-A(캐릭터 선택창 3칸) → PR268(자동 진행기) → 빌드 계약이다.
- PR268 병합 전 검증에서 독립 검증자가 DB 영속 서버로 한 판을 돈다. 사용자 서버 기동이 한 번 더 필요하다. Content 리드가 그 시각을 Core와 맞추러 연락한다.
- 같은 사용자 서버 세션 안에서 Core 쪽 확인을 함께 할 수 있으면 그 안을 Content와 같이 올린다.
- 기다리는 동안 대기 종료·heartbeat는 한 줄로 끝낸다. 메인이 현황판 Core 상태를 「대기」로 둔다.

메인 `msg_495eff183bf0`(2026-11-01T08:26:45Z). 사용자 결정이 아니다.

- 병합 순서는 Art 격자 PR1 → Core 24칸 → Art PR2 → Content 녹화 빌드다. 재개 지점의 착수 조건에 반영했다. 11-01에는 24칸 branch에서 받아 `lead/pending-goal-edits.md` 5절에 두었고, 11-04 복귀 때 옮겼다.

메인 `msg_4b7239950d8f`(리드 수신 2026-11-04T14:04:22Z). 사용자 결정이 아니다. 「Goal까지 마무리 진행」은 메인이 전달한 사용자 지시다(메인 `msg_9a38be78866f`).

- 리드 교체([R-8](../../../00_Document/operations/ORCA.md#r8-astra-lifecycle)): 지금 하지 않는다. 이 pane이 이 goal의 맥락을 갖고 있어, 제출 기한(11-09 14:00)까지 이어 가는 편이 인수 비용보다 싸다. 교체는 이 goal이 끝날 때 다시 판단한다.
- 24칸 종료 점검의 다음 계획 초안 1(이 goal 복귀·녹화 Core 몫 우선)은 사용자 지시 「Goal까지 마무리 진행」 범위로 진행한다. 초안 2~4(길드 창 11건 대조, SENTIS define 반복, 운영 규칙 후보)는 제출 뒤로 두고 BACKLOG 관측 그대로 둔다.
- 24칸 goal 상단 상태 줄과 CURRENT Core 줄의 병합 전 표현은 이 goal 복귀 기록 commit에서 함께 고친다.
- 녹화 Core 몫 계획 보고에 Unity·WSL 창이 필요한 시각과 길이를 넣는다. Art PR2가 Unity 창을 약 40분 쓸 예정이다.

## 요구사항 원천

- 범위 초안 전문: `.backups/verification/2026-10-31-db-persistence-demo-build/lead/scope-draft-v1.md`(sha256 위와 같음). 초안의 「확인한 사실」 표가 근거 줄을 담는다.
- 선행 goal: [캐릭터 저장의 서버 재기동·불러오기 합동 확인](../2026-10-24-character-storage-restart-load/goal.md)의 10-29 합동 실DB 창(S1~S8 통과, PR272), [캐릭터 선택·생성 화면](../2026-10-24-character-select-create-ui/goal.md)(PR273), [캐릭터 목록·생성·선택 서버 처리](../2026-10-18-character-server/goal.md)(PR270).
- 서버·Shared·ClientNet·launcher·sync-wsl 코드는 PR272 병합(`34756e01`) 뒤 main `f5adcf31`까지 바뀌지 않았다. launcher는 10-29 창판 `9992b1c9`(sha256 `F27EEC2D…04C6`)와 main판 `f3e94759`(sha256 `d4957d6a…6754`)가 공백 한 줄만 다르다(`git diff -w` 빈 출력).

## 범위

두 단계로 나눈다. 단계 A는 지금 보기이고 코드 변경이 없다. 단계 B는 녹화 영상 준비다(결정 79 C 뒤 범위 초안 v2, 메인 `msg_ff94804f5f76` 통과). v1의 시연자 카드·빌드 리허설·사용자 리허설은 v2의 녹화 순서·녹화용 리허설·녹화 담당 인계로 바뀌었다. 초안 전문은 `lead/scope-draft-v2.md`다.

### 만들 것

1. **지금 보기:** 메인 배정 창에서 Legacy를 내리고 core-active에서 영속 서버를 띄워, 사용자가 지금 쓰는 Editor로 새 캐릭터 선택창을 본다.
2. **영속 서버 실행 절:** `00_Document/operations/DEVELOPMENT.md`에 「DB 영속 서버 실행」 절을 더한다. 빌드 → launcher 시작 → Enter 정지 순서, 7777 충돌, 실패 문구별 대처를 담는다.
3. **녹화 실행 순서(v3, 결정 82 B):** 자동 진행기 두 클라(기사·마법사) + 영속 서버 한 판 기준의 서버 쪽 순서를 이 goal에 둔다. Legacy Ctrl+C → 영속 서버 Listening → 두 클라 시작·자동 진행 → (DB 장면은 자동 진행이 끝나고 연결이 닫힌 뒤) Enter·재시작·같은 캐릭터 목록 → 녹화 끝 → Enter. Core 몫은 서버 순서·대기 시간·정지/재시작 위치다. 녹화 도구는 메인, 자동 진행기는 Content 몫이다.
4. **녹화용 리허설 1회(v3, 결정 83 A):** PR268 병합 뒤 main의 Content 빌드로 위 순서를 끝까지 한다. 장면별 소요 시간·서버 재시작 시간·화면 오류를 기록해 녹화 컷 계획에 넘긴다.
5. 리허설을 막는 결함은 번호를 매겨 소유 파트에 보낸다. Core 소유(저장·host·launcher)는 이 goal에서 최소 수정하고, World·Content 소유는 각 파트 계약으로 넘긴다.
6. **자동 진행기용 서버 계약:** F8·F9의 영속 모드 처리, DB에 남는 값과 안 남는 값, 캐릭터 이름·칸·삭제 없음 제약, 서버 쪽 녹화 순서를 Content 새 리드에게 넘긴다. v1은 `lead/server-contract-for-autopilot-v1.md`(sha256 `4187c314…5e51`, 메인 `msg_cb2a6da95190`)이고 리허설 실측으로 갱신한다.

### 건드릴 곳

- 이 goal, `00_Document/operations/DEVELOPMENT.md` 새 절, `00_Document/operations/CURRENT.md` Core 줄, 근거 폴더.
- 결함이 있을 때만 `02_Server/GameServer/Persistence/`, `02_Server/GameServer/Hosting/PersistenceRuntime/`, `99_Tools/Start-PersistenceServer.ps1`과 관련 시험. 실제 파일은 발행 계약에 고정한다.

### 하지 않을 것

- BACKLOG 후보 `persistence-launcher-wsl-executable-resolution`, `persistence-launcher-allowlist-drift-test`, `persistence-environment-hardening`, `persistence-live-observation-followup`, `character-storage-unexecuted-paths`. 시연 경로를 막지 않는다.
- launcher 기능 변경과 시연용 새 시작 도구.
- 자격 자동 입력·AI의 credential 열람, 새 DB·DDL·행 삭제·자동 정리, 인증서·서비스·방화벽·전역 설정 변경.
- 노트북 시연 환경(79 C로 필요 없어짐).
- 실제 녹화·편집·영상 파일 산출, PR268 자동 진행기 수정(클라 코드, Core 소유 아님).
- 클라 화면·씬·Unity 에셋 수정, 프로토콜·PDL·Shared 변경.
- 사용자 손이 필요한 실행의 대리, 메인 배정 밖의 WSL·DB·Unity 실행, Core의 PR 병합.

### 관찰 가능한 완료조건

1. **지금 보기:** 메인 배정 창에서 영속 서버 `Listening on 0.0.0.0:7777`과 사용자 화면의 새 캐릭터 선택창(목록·생성·선택)을 원시로 남긴다. 삭제 경로는 서버·클라 모두 없다(`character_catalog.sql:24`). 정지 뒤 `Server stopped.`와 7777 해제를 확인한다.
2. **녹화용 리허설:** Content 빌드 exe(경로·sha256 기록)로 새 시험 로그인 → 빈 목록 → 생성 → 선택 → Town 입장이 된다. 서버 정지·재시작 뒤 같은 로그인으로 같은 CharacterId·이름·직업이 목록에 남고 다시 입장된다. 화면 캡처·서버 출력 줄·장면별 시각으로 관측한다.
3. **순서와 실제 명령 일치:** 녹화 순서의 서버 명령이 리허설에서 실제로 친 명령과 bytes로 같다. 자격 경로는 입력 요청으로만 남긴다.
4. **독립 실사:** 신규 `[Core 검증자]`가 DEVELOPMENT 새 절·녹화 순서·리허설 원시를 대조한다. 결함 수정이 있으면 그 코드도 강 기준으로 본다.
5. **녹화 담당 인계:** 녹화 순서·리허설 기록·녹화용 시험 로그인과 캐릭터 이름을 메인에 넘긴다. 이름 유일 키가 전역이라 녹화용 이름은 리허설과 다르게 미리 정한다.

## 필요한 DB 쓰기와 실행 경계

| 종류 | 허용할 최소 내용 | 금지/관문 |
|---|---|---|
| 대상 | 공개 profile의 `tcp:localhost,15433` / `Dawnholder_Char_8256bcde8c80` 하나. core-active `logs/windows-sql-8256bcde8c80478494bcefb9cdfaf4f6/` profile sha256 `3c2d2d0d…a2f1`, 공개 CA `0bd1909d…caf3` | 새 대상 생성0, 비밀 출력0 |
| 쓰기 | 결정 80 A: 새 시험 계정·캐릭터 행 생성. 이름은 시험용이 드러나게 정한다 | 행 삭제·DDL·직접 SQL·자동 정리0 |
| 자격 | 사용자만 경로를 입력한다 | AI의 내용·존재·크기·hash 확인0, 시작·Enter 대리0 |
| 포트 | 7777은 한 소유자만. 영속 서버 전 Legacy 정지 | 남의 프로세스 종료0 |

캐릭터 이름 유일 키는 계정을 가리지 않는다(`UQ_Character_NameKey`). 리허설에서 쓴 이름은 시연에서 다시 못 쓴다.

## 설계 선택과 파트 계약

- 선택: 기존 launcher를 사용자 카드로 쓴다. 대안인 새 시작 도구는 강 등급 도구 검증이 새로 필요하고 이득이 명령 길이뿐이라 채택하지 않는다. 리허설에서 사용자가 명령을 틀리면 메인에 올린다.
- 녹화 구성(결정 82 B): 자동 진행기를 고쳐 영속 서버 한 판으로 찍는다. Core 추천(나눠 녹화)은 채택되지 않았다. 지금 PR268 자동 진행기는 영속 서버에서 직업 선택 단계에 멈춘다(소스 판단, `CharacterSelectController.cs:62~66`). 그 수정은 새 Content 리드 몫이고, Core는 서버 계약 v1로 제약을 넘겼다.
- Content: 클라 Windows 빌드를 만든다. 고정 head·빌드 경로·exe sha256을 Core에 넘기고, 빌드에서만 나는 화면 결함은 Content가 고친다. 계약은 R-8로 열릴 새 Content 리드와 맺는다(메인 결정).
- World: 캐릭터 메뉴 처리 결함이 나올 때만 계약한다.

## 녹화 실행 순서(서버 쪽, v1 — 리허설 전)

결정 82 B의 한 판 녹화에서 Core가 맡는 서버 쪽 순서다. 클라 순서와 자동 진행 단계는 Content, 녹화 시작·끝은 메인이 정한다. 확인 수준을 칸마다 적었다. 리허설 뒤 실측으로 v2를 만든다.

- **빌드 기준(11-01 고침):** 서버와 녹화 클라는 PR-A·PR268·Art 격자 PR1·Core 24칸·Art PR2(이름은 Content 녹화 빌드 계약을 따름)가 모두 들어간 같은 main(Content가 알리는 고정 head 40자)이다. 프로토콜은 v23이다. 고친 근거는 「적용 중인 사용자 결정」의 11-01 HUD 지시다.
- R2·R4는 결정 110 A(모니터마다 1920×1080)로 11-01에 고쳤다. 창 8의 960×540 값은 쓰지 않는다.

| 순서 | 할 일 | 누구 | 확인 수준·근거 |
|---|---|---|---|
| R0 | 7777을 쓰는 서버가 없는지 본다. Legacy가 떠 있으면 그 창 소유자가 Ctrl+C로 끈다 | 소유자 | 10-31 단계 A 실행. Legacy 정지 키는 `run-server.bat:7` |
| R1 | 서버 checkout 루트에서 `wsl -d Ubuntu -- bash 99_Tools/sync-wsl.sh build`를 1회 돈다. 녹화 직전 서버 소스가 단계 A 빌드와 같으면 생략할 수 있다 | 배정된 Core 작업자 | 10-31 실측 46초, exit 0(`build-01/`) |
| R2 | 서버 Listening 뒤, 1920×1080 Player 두 개와 사용자 녹화 도구를 띄우기 직전에 물리 여유 6GiB·commit 여유 4GiB를 원시로 남긴다. Content Player는 R2 통과 뒤에만 띄운다 | 배정 창의 실행자 | 메인 결정 `msg_0d7e73a9805d`, 결정 110 A. Unity 빌드 창의 관문은 Content 계약 몫 |
| R3 | 사용자가 서버 창에 DEVELOPMENT 「DB 영속 서버 실행」 시작 블록을 붙여 넣고 자격 경로를 입력한다. 세 줄(PID 줄 → launched 줄 → `[Persistence stdout …] Listening on 0.0.0.0:7777. Press Enter to stop.`)을 기다린다 | 사용자 | 10-31 실측: 프로세스 생성 09:52:15.75Z → Listening 09:52:16.64Z, 약 0.9초(1회) |
| R4 | Listening과 R2 뒤에 두 클라를 시작하고 자동 진행을 녹화한다. 기사는 주 모니터, 마법사는 보조 모니터에 각각 `-screen-width 1920 -screen-height 1080 -screen-fullscreen 0`과 `-monitor`로 띄운다. 플레이 중 서버 로그는 서버 창에 나오지 않는다(launcher는 네 문구만 옮김) | 메인·Content 순서 | launcher 129~136행. 결정 110 A. 명령·창 위치·잘림 확인은 Content 계약 고정본. 창 8(PR268 검증)에서 두 클라가 영속 모드로 1판 생성·2판 선택 뒤 마을까지 갔다. 파티·보스 흐름은 Core가 대조하지 않았다 |
| R5 | DB 장면을 넣으면, 자동 진행이 끝나고 두 클라 연결이 닫힌 뒤에 둔다. 서버 창 Enter → `Server stopped.`와 출력 요약 두 줄 → 입력 화면 | 사용자 | 10-31 정지 실측(`docs-02/stage-a-observed-lines-v1.txt`). 정지에 걸린 시간은 Enter 시각이 없어 미측정 |
| R6 | 같은 서버 창에서 `& .\99_Tools\Start-PersistenceServer.ps1 @serverArgs` 한 줄로 다시 띄운다. 첫 블록의 변수와 입력한 자격 경로를 그대로 쓴다. 다시 세 줄을 기다린다 | 사용자 | 10-29 합동 창 S5에서 같은 한 줄로 재시작했고 S1~S8이 통과했다(PR272). 재시작 시간은 미측정 |
| R7 | R4와 같은 명령(같은 `-monitor` 값)으로 같은 로그인에 접속해 같은 캐릭터가 목록에 남은 것을 보여 주고 입장한다. 진행 상태(퀘스트·길드·금고·강화)는 처음으로 돌아간다 | 메인·Content 순서 | 서버 계약 v1 §2. 10-29 합동 창에서 서버 1(S3 봇 생성) → 재시작 → 서버 2(S6 봇)의 목록에 같은 ID·직업이 남았다 |
| R8 | 녹화 끝 → 클라 종료 → 서버 창 Enter → `Server stopped.` → 입력 화면. 필요하면 소유자가 Legacy를 다시 띄운다 | 사용자·소유자 | R5와 같음 |

- 녹화마다 새로 만드는 캐릭터의 이름과 칸은 DB에 영구히 남는다. 이름은 서버 전체에서 하나이고 칸은 계정당 3개다. 리허설과 녹화의 로그인·이름은 미리 다르게 정한다(완료조건 5).
- R4 전에 서버를 다시 켜면 자동 진행기의 메모리 상태가 처음으로 돌아간다. 그래서 R5~R7은 자동 진행이 끝난 뒤에만 둔다.
- 서버 checkout은 공개 profile이 있는 core-active다. launcher는 자기 checkout 안의 profile만 받는다. 녹화 클라는 Content 계약의 main 빌드다.

## PR 경계와 검증

- **Core PR 1:** goal·DEVELOPMENT 새 절·리허설 기록. 제품 수정이 없으면 문서 PR이고 등급은 문서 실사 + 리허설 원시 대조다.
- Core 소유 결함 수정이 생기면 같은 PR에 넣고 강 등급 독립 검증을 받는다. World·Content 결함은 각 파트 PR이다.
- 병합은 PR마다 메인의 정확 head 사용자 승인 관문을 따른다.
- 실행 창: 지금 보기는 서버 빌드 46초(09:27:42Z~09:28:28Z), 서버 켜짐 09:52:15Z~정지 10:11:31Z로 약 19분이었다. 녹화용 리허설 어림은 약 40~60분이다(클라 빌드 생성 시간 별도, 미측정). 실제 녹화 창은 메인이 정한다. Unity·WSL 병행 직전 물리 여유 6GiB·commit 여유 4GiB를 원시로 확인한다.

## 진척 단계

- [x] 범위 초안
- [x] 범위 승인
- [x] 지금 보기 서버 빌드
- [x] 지금 보기 화면 확인
- [x] 실행 절·녹화 순서(v1, 리허설 뒤 v2)
- [ ] 녹화용 리허설
- [ ] 녹화 담당 인계
- [>] 독립 실사(DEVELOPMENT 1차 통과 `verify-01/`, 리허설 뒤 녹화 순서·리허설 원시·바뀐 줄 남음)
- [ ] 문서 PR 병합
- [ ] 종료 점검
