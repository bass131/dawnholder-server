# DB 영속 서버로 빌드 시연 준비

11-06 시연을 Windows 빌드 클라이언트와 DB 영속 서버로 할 수 있게 한다. 시연자가 정해진 순서로 DB 서버를 켜고 끄며, 빌드에서 로그인 → 캐릭터 선택·생성 → 마을 입장이 되는 상태가 목표다.

**현재 상태(2026-11-06 10:3x KST):** PR299(이 goal의 Core 문서)가 2026-11-06T01:03:28Z에 병합됐다(merge commit `2d49a93e`). 진짜 녹화는 01:07Z~01:16Z에 끝났다. 서버 시작·정지·재시작은 사용자 결정 137 A로 메인이 대신 했다. 진짜 녹화의 Core 세 값은 모두 통과다. 재시작은 프로세스 시작 → Listening 0.777초였고, 정지 요약 두 번 모두 결함 0이었다. 2회차 상대 기사는 1회차 캐릭터를 다시 골랐다(Content 실행 기록, 독립 검증 아님). 종료 Gardener는 정리 후보 2개(BACKLOG로 이관)와 종료 기록 정정 1건(G-01)으로 끝났다. 남은 것은 종료 기록 PR300 병합과 종료 점검이다. 아래 「11-06 종료 Gardener」와 「11-06 PR299 병합과 진짜 녹화」부터 읽는다.

## 재개 지점

- **11-06 종료 Gardener(2026-11-06T01:33Z~01:49Z):** 신규 `[Core Sol]` GPT-6-Astra xhigh(goal 「2026-11-04 새 Codex 세션은 모두 Astra xhigh」, 첫 화면 Codex v0.166.0, backend unknown)가 읽기 전용으로 점검했다. 계약은 `closeout-01/gardener-contract-v1.md`(sha256 `a731af78efdf4db1b93d0853abb0b79e0293aa30a361df88164e9c60f3c9317c`)이고, 입력 41파일을 hash로 고정했다(`closeout-01/gardener-inputs.json`). 계약은 화면 기록 JSON(terminal-list 등)을 열지 않게 했다.
  - pane `term_6b6588ed-5a70-459c-9a0e-97a101046b8b`, Task `task_334720d37cbd`, Dispatch `ctx_f8091e1855e4`다. worker_done은 `msg_072c5247caad`, 쓰기 종료는 01:48:51Z다. 기동 기록은 `closeout-01/gardener-launch-record-v1.md`다.
  - 보고서는 이 폴더의 [gardener.md](gardener.md)다. 원본 `closeout-01/gardener/context-gardener-report.md`와 bytes가 같다(34,919 B, sha256 `1eec659f0b218c433ca4a2469860d9037a6e304a79ae584c9ba83538ba968888`, `closeout-01/gardener-md-copy-01.txt`).
  - 결론: 독립 실사의 번호 결함 6건(#RH2 넷, #RH4-01, #RH5-01)이 모두 해소됐고, PR299 CI 네 항목도 통과였다. 정리 후보 2개는 아래 「다음 계획 후보」로 옮겼다. 이 점검을 제품·실제 플레이·DB의 독립 통과로 넓히지 않는다.
  - 리드 반환 G-01(결함 수량): 아래 「verify-03 뒤 실사」 줄이 「세 결함 모두」라고 적었다. 번호 기준으로는 #RH4-01과 #RH5-01 두 건이다(#RH5-01은 인용 위치 두 곳). Gardener 공식 질문 `msg_a0bb9b354ba8`에 리드가 `msg_240bfc347cc9`로 받았고, 그 줄을 고쳤다.
  - 처분 B 추가: Gardener 입력을 고르다 리드가 앞서 저장한 터미널 목록 셋에서 사용자 서버 창 preview를 더 찾았다. 메인 처분으로 가렸다(아래 「적용 중인 메인 결정」 `msg_41665d27bd70`).
  - 리드 운영 실수(첫 발생): 01:28Z에 메인 pane에 안내를 넣을 때, 같은 읽기의 `draft` 필드가 「계속 진행해줘」였는데도 확인과 전송을 한 명령으로 묶어 Enter까지 넣었다. 화면 입력줄은 비어 있어 추천 문구였을 가능성이 크다. 리드는 메인에 `msg_8fe5884de120`으로 알리고 메인 기록 확인을 청했다(`closeout-01/main-screen-0128.json`·`main-screen-0129-after-nudge.json`). 첫 발생이라 새 규칙은 만들지 않는다(CODE_CONVENTION 「교정 층과 반복 규칙」). 리드는 draft를 먼저 따로 읽고, 비어 있지 않으면 안내를 넣지 않는다.
  - 정리: worker-release `retained`(external_terminal), 빈 프롬프트 확인 뒤 pane을 닫았다(`closeout-01/gardener-close-v1.json`).
  - 다음 일: 종료 기록 PR을 PR300으로 열었다(본문 `closeout-01/pr-body-v1.md`). 신규 `[Core 검증자]`(Astra xhigh)가 문서 실사를 한다. 통과하면 병합 승인 요청만 메인에 올린다. 종료 점검과 R-8은 메인이 판단한다.
- **11-06 PR299 병합과 진짜 녹화:**
  - 병합: 메인이 사용자 승인 줄 「병합 승인: PR299 head 5135b2615bfa0d250a131d9f02c42d23a90f7503」(메인 pane 제출, 메인 알림 `msg_6566a4984a95`의 사본)을 받아 2026-11-06T01:03:28Z에 병합했다. merge commit은 `2d49a93e63b0f0680f9055973fedb6e652751ced`이고 부모는 `28c4dff8`·`5135b261`이다. state는 MERGED, head는 `5135b261`이다(`closeout-01/pr299-view-01.json`, `closeout-01/origin-main-commit-01.txt`). CI 4/4 SUCCESS와 CLEAN은 메인 알림의 값이다.
  - 메인은 종료 점검과 R-8을 진짜 녹화와 8줄 대조 뒤로 미뤘다(같은 `msg_6566a4984a95`). 그 사이 사용자가 137 A·138 A와 사용자 확인 범위를 정했다(아래 「적용 중인 사용자 결정」).
  - R1 사전 확인(2026-11-06T01:07:23.902Z~01:07:32.551Z, 메인 배정 `msg_56fa1f0ef253`): 리허설과 같은 `lead/rehearsal-01/precheck-v1.sh`로 읽기만 했다. 복제본 소유 표식 core-active, `GameServer.dll` 353,280바이트·mtime `2026-11-04 23:22:11.676808968 +0900`, `Current = 23`과 그 파일 sha256 `a62b3b58…24e2`가 모두 리허설 값과 같았다. WSL GameServer 0, WSL·Windows 7777 LISTEN 0이었다. 원시는 `lead/recording-r1/precheck-01.txt`(sha256 `4869cfa5…0857`)이고, 메인 보고는 `msg_75e14a5f4585`다.
  - 서버 단계(137 A로 메인 대행, 시각은 메인 보고): R3 시작 줄 송신 01:08:18.6Z → Listening 01:08:26.8684063Z(PID 24864, `msg_ce38f9c9dd7d`). R5 Enter 01:12:53Z, R6 재시작 줄 송신 01:13:03Z(`msg_2a18252354ae`). Content의 2회차 끝·두 클라 닫힘 알림은 01:16:28Z(`msg_e1c1970b0adb`), R8 Enter는 01:16:45Z다(`msg_6cb4eebf0da7`). 메인이 보낸 줄은 카드 v6 84·118행과 같은 `& .\99_Tools\Start-PersistenceServer.ps1 @serverArgs`다. 그 창의 `$serverArgs`는 사용자가 앞서 넣은 값이다(메인 probe, 값 출력 없음). 리드는 서버 창을 읽지 않았다.
  - 8줄: 메인이 허용 목록 추출(`server-window-wait-1030.cjs`)로 뽑았고, R5·R6·R8 표식도 메인이 붙였다(`msg_6cb4eebf0da7`). 리드 사본은 `lead/recording-r1/lines-8.txt`(sha256 `fb761ced…15f3`)다. 앞 다섯 줄은 R6 뒤에 먼저 받은 줄(`msg_2a18252354ae`, `lead/recording-r1/lines-r5-r6.txt`)과 bytes가 같다.
  - Core 세 값은 셋 다 통과다. 계산은 `lead/recording-r1/three-values-v1.cjs`이고 결과는 `three-values-01.json`(sha256 `bdf19702…85e3`)이다. 메인 보고는 `msg_82ee4f57c622`다.
    - R6 birth(01:13:06.1706548Z) → Listening(01:13:06.9474589Z): 0.7768041초. 리허설 0.896초, 창 8 약 0.81초, 10-31 지금 보기 약 0.89초와 같은 수준이다.
    - R5 `Server stopped`(01:12:54.3691799Z) → R6 Listening: 12.5782790초. 메인이 정지를 보고 재시작 줄을 보내기까지의 시간이 들어 있다.
    - R5·R8 정지 요약 네 줄이 모두 `overlong=0, queueDropped=0, decodeErrors=0, readerErrors=0, drained=True`라 통과다. stdout `excluded`는 R5 576, R8 515이고 stderr `excluded`는 둘 다 0이다. R8 `Server stopped`는 01:16:45.5985121Z다.
    - 같은 캐릭터: 2회차 끝 기록의 `characterVerdict`가 `sameKnightCharacter=true`, `knightCreated=false`, `knightSlots=1`, `priorKnightSlots=[1]`이다. 원천은 `[Content 구현 Astra]` `msg_e1c1970b0adb`와 Content 원시 `recording-build-01/recording/run-01/r4-2-end.json`(sha256 `9aee44e8…cd28`)이다. 리드가 원시 파일의 그 필드를 직접 읽었다. Content 실행 기록이며 독립 검증은 아니다.
    - 시각은 모두 Windows 시계다(launcher 71행·555~556행).
  - 녹화 영상과 회차별 화면 판정은 Content goal 몫이다. Core는 영상을 판정하지 않았다.
  - 당시 다음 일(종료 Gardener는 위 「11-06 종료 Gardener」로 끝남): 종료 Gardener(신규 `[Core Sol]`, Astra xhigh) → `gardener.md`를 넣은 종료 기록 PR → 신규 `[Core 검증자]` 문서 실사 → 메인에 병합 승인 요청 → 종료 점검. R-8은 메인이 판단한다.
- **11-05~06 리허설과 진짜 녹화 준비:**
  - 고정 head 받기: Content가 알린 고정 head `cfe8bb45`(Art PR2인 PR297의 병합, 2026-11-05T16:57:26Z)를 받아 branch HEAD가 `2cb8910d7f0facf8d7096e35fcb3f7f224aa93ec`가 됐다. 서버 빌드 입력(`sync-wsl.sh` 47~72행 범위)은 build-03 소스 `3b03387b`와 diff 0이라 R1을 다시 돌지 않았다(`lead/take-fixed-head-01/take-verify-01.txt`). Content에는 넷을 전했고(`msg_9203c45e18e9`), 메인 보고 `msg_5d845c8fc131`에서 배정 창 밖 WSL 읽기 1회를 밝혔다.
  - Content 녹화 빌드(`msg_c8dd2ae6e315`): exe sha256 `91a36fa14dd3ccf390b99720805eda0553da1ca1108381b49c98c938f69dc256`, manifest 234파일·778,468,195바이트·sha256 `d10d57c9bdecc7016dbf775eb10f00f3e3f5da60cf7d3ef83f4c25937a15f9e5`, Unity 6000.6.4f1, 빌드 exit 0(220.7초). 경로는 content-active `.backups/verification/2026-10-31-demo-autopilot-character-select/recording-build-01/`이다. 메인이 재빌드 없이 수용했다(아래 메인 결정 `msg_a5a6721188ce`).
  - 사전 확인(2026-11-05T20:15:56.616Z~20:16:05.695Z, 창 안): 복제본 소유 표식이 core-active다. `GameServer.dll`은 353,280바이트이고 build-03 시각 그대로다. 복제본 `Current = 23`의 sha256이 Windows와 같다. WSL GameServer 0, WSL·Windows 7777 LISTEN 0이었다. 원시는 `lead/rehearsal-01/precheck-01.txt`이고, 메인에 「서버 빌드 맞음」을 보냈다(`msg_51df9731f6e9`).
  - 리허설 단계 시각(메인 쪽, `lead/rehearsal-01/results-v1.md`): R3 「서버 떴음」 20:47:07Z, R5 「지금」 21:00:46Z → 「멈췄음」 21:09:47Z, R6 「지금」 21:09:47Z → 「다시 떴음」 21:20:18Z, R7 두 역할 마을 21:21:54Z다. R5·R6 시각은 사용자 응답 시간을 포함한다. 사용자는 카드와 다른 줄을 알리지 않았다(`msg_86e85d6e92fd`). 카드는 v5였다(아래 메인 결정 `msg_67f019755590`).
  - Content 리드 판정(독립 검증 아님): content-active `.backups/verification/2026-10-31-demo-autopilot-character-select/lead/rehearsal-v2-lead-judgement.md`, sha256 `71b93898480241c87347a6a6b98288f95f2b89725d857d4dc2befd079b33db6a`. 세 판 완료·배치와 화면·R4-2 재로그인·R7 선택·소리가 통과다. 결과 메시지는 `msg_c277074f6a51`이다.
    - R4-2 재로그인은 AlreadyOnline 없이 두 역할 slot 1, created=False였다. R7은 재시작 뒤 두 역할이 R4와 같은 slot 1을 골라 마을에 들어갔다.
    - 위험: 녹화 도구가 Player 기동 약 6.8초 뒤에 시작해 첫 프레임이 로그인 근처다(`players.ps1` 171~184행). 진짜 녹화는 두 회차 모두 생성에서 시작하므로 생성 장면(빈 칸 약 1초)을 놓칠 수 있다. → 결정 134 A.
    - R4-2의 기사는 거점 `already max`였다. 길드 거점·금고는 서버 실행 단위 메모리다. → 결정 133 A.
  - Core 세 값: 셋 다 통과다. 같은 칸 확인(R7)은 Content 보고로 봤다. 재시작 시간과 정지 요약은 Core가 정한 서버 창 8줄(`msg_4cdd8331fcb4`)로 냈다. 8줄은 사용자 요청으로 메인이 허용 목록 스크립트로 뽑아 보냈다(아래 메인 결정 `msg_3a07539cc5ae`, 리드 수신 2026-11-05T22:06:37Z). 8줄 원문은 `lead/rehearsal-01/results-v1.md`에 있다.
    - R5 `Server stopped`(21:09:25.530Z) → R6 Listening(21:19:51.393Z): 10분 25.862초. 사용자 응답 시간이 들어 있다.
    - R6 birth(21:19:50.497Z) → Listening: 0.896초. 10-31 단계 A의 약 0.9초와 같은 수준이다.
    - R5·R8 정지 요약 네 줄이 모두 `overlong=0, queueDropped=0, decodeErrors=0, readerErrors=0, drained=True`라 통과다. stdout `excluded`는 R5 2022, R8 2681이고 stderr `excluded`는 둘 다 0이다. R8 `Server stopped`는 22:03:23.042Z다.
    - 시각은 모두 Windows 시계다. birth는 launcher가 띄운 `wsl.exe` 프로세스의 시작 시각(`Start-PersistenceServer.ps1:555~556`)이고, 나머지는 launcher가 줄을 받은 시각(71행)이다. 두 값은 리드가 8줄에서 다시 셌고 메인 참고값과 같다.
  - 같은 출발 상태: Core가 결정 글 `lead/rehearsal-01/msg-decision-same-start-v2.txt`를 메인 `msg_7b111d608c48`·Content `msg_25dbd7e547fa`로 보냈다. 사용자가 133 A·134 A·135 A를 골랐다(아래 사용자 결정).
  - 진짜 녹화용 사용자 카드 v6: `lead/recording-user-server-card-v6.md`, sha256 `4269a171a3427af75e71e815c98e9ca987f878f35cc35f09d23c99a2060f54c2`. v5 대비 diff는 `lead/recording-docs-v2/card-v5-to-v6-r2.diff`이고, 코드 블록 여섯 개는 v5와 같다. 이름을 채우기 전 판(`0d3353cb…0c7d`)을 메인이 대조해 통과시켰다(`msg_544f74c0c0f2`). 이름 세 줄만 바꾼 지금 판은 메인 `msg_8a6b9593cc87`·Content `msg_3a756dd537ba`로 보냈다.
  - 녹화 실행 순서 v2: 아래 「녹화 실행 순서」 절이다. 초안 `lead/recording-order-v2-draft.md`를 메인 `msg_9780d55994de`·Content `msg_c946082e5b5b`로 보냈다.
  - 다음 일
    1. 끝: 8줄로 채울 칸(이 항목과 녹화 순서 R5·R6·R8)을 채웠다. 값은 메인 `msg_7166a0512a1e`·Content `msg_c35cc48cf1b9`로 보냈다.
    2. 끝: 서버 계약 v1.2(`lead/server-contract-for-autopilot-v1.2.md`)를 썼다(범위 「만들 것」 6). v1.1 대비 diff는 `lead/goal-edit-1106/contract-v1.1-to-v1.2.diff`, 8줄 채우기 diff는 `lead/goal-edit-1106/contract-v1.2-8line-fill.diff`, 결함 #RH2-01 수정 diff는 `lead/fix-rh2/contract-v1.2-rh2.diff`다. 지금 판 sha256은 `99f641b4…d56a`다. 처음 판(`a72a4daa…6226`)은 메인 `msg_74ff697a3000`·Content `msg_e23465a75a03`로 알렸다. 같은 사실은 Content에 메시지로 이미 갔다(`msg_b752a2465246`·`msg_b28231a86423`).
    3. 차단 뒤 수정: 신규 `[Core 검증자]` Astra xhigh(Task `task_1613a56c6827`, Dispatch `ctx_665002dac869`, 계약 `verify-02/contract-v1.md` sha256 `e6a97d4b…6204`)가 이 goal의 바뀐 줄·카드 v6·서버 계약 v1.2·리허설 원시를 확인할 것 12개로 실사했다. 결론은 **차단, 문서 결함 4건**이다(worker_done `msg_14f224708bf0`, `verify-02/verdict.md` sha256 `4cb3c065…b88e`). 수치 조작·제품 결함은 없었고, 카드 블록·8줄·두 시간 값·범위는 일치했다. DEVELOPMENT는 더 고칠 필요가 없다고 판단했다.
       - #RH2-01: 서버 계약 v1.2 §7이 한 번 잰 0.896초를 「다시 뜨는 데 1초 안」으로 일반화했다. → 1회 관측으로 한정했다.
       - #RH2-02: 이 goal의 133 A·134 A·135 A 인용에 원천 낫표 밖의 접두가 들어갔다. → 인용 밖으로 뺐다.
       - #RH2-03: `lead/rehearsal-01/results-v1.md`가 R5·R6·R8 표식을 메인 스크립트가 붙였다고 적었다. 스크립트는 숫자 행 번호를 붙인다. → 메인이 보낸 8줄의 표식이라고 고쳤다.
       - #RH2-04: 녹화 순서 R2 칸이 「두 번 통과」의 근거로 Content 판정 17행(R4-2 재로그인)만 걸었다. → 두 관문 원시를 연결했다.
       - 리드 수정 기록은 `lead/fix-rh2/`다. 리드 표본 대조와 검증자 질문 둘의 답은 `verify-02/lead-reply-01/`·`lead-reply-02/`에 있다.
       - 재실사: 신규 `[Core 검증자]` Astra xhigh(Task `task_2118f2065762`, Dispatch `ctx_0b60eb010000`, 계약 `verify-03/contract-v1.md` sha256 `238e3149…a00f`)가 네 수정 위치와 `c2c717e5..42a5aeda` diff를 봤다. 결론은 **통과, #RH2-01~04 해소, 신규 결함 없음**이다(worker_done `msg_b995c5a5e518`, `verify-03/verdict.md` sha256 `11623ab0…e2ab`). 실사 중 경계 사건 하나가 있었다(아래 「적용 중인 메인 결정」 `msg_d20436e197d3`).
       - verify-03 뒤 바뀐 것: 이 goal의 이 줄들·상태 줄·진척과 메인 결정 두 블록(`msg_d7e95c0cb4c3`·`msg_d20436e197d3`), CURRENT Core 줄, 그리고 `lead/rehearsal-01/results-v1.md` 35행(표식 부착 주체를 메인 사실로 바꿈, diff `lead/after-verify03/results-v1-marker-fact.diff`). verify-03은 이것들을 보지 않았다.
       - verify-03 뒤 실사(PR299 head `5135b261`, 저장소 내용 불변): verify-04(Task `task_f2625e387c9a`, worker_done `msg_98979274478d`, `verify-04/verdict.md` sha256 `0a19f885…edcb`)가 리드 메모의 영향 파일 목록에 CURRENT가 빠진 것(#RH4-01)으로 차단했다. 리드가 보충 메모 `lead/after-verify03/lead-context-addendum-rh4-v1.md`를 썼다. verify-05(Task `task_20bc06c7200e`, worker_done `msg_c2b997c8eaf4`, sha256 `4026f0b3…1313`)는 #RH4-01을 해소로 보고, 보충 메모의 인용 표기 두 곳(#RH5-01)으로 차단했다. 리드가 두 인용을 원천 bytes 그대로 고쳤다. verify-06(Task `task_e5a8994bf4e7`, worker_done `msg_a3661df782ad`, sha256 `4c3d1e20…eb25`)이 통과, 신규 결함 없음으로 끝냈다. 두 결함(#RH4-01·#RH5-01) 모두 근거 폴더 기록의 결함이고, 저장소 내용은 바뀌지 않았다.
    4. (끝남) Core 문서 PR을 연다. 검증 뒤 바뀐 줄은 PR head에서 새 검증자가 좁게 다시 본다. 병합은 메인 관문이다. → PR299로 병합됐다(위 「11-06 PR299 병합과 진짜 녹화」).
    5. (끝남) 진짜 녹화 창은 메인이 배정한다. Core 몫은 창 시작의 사전 확인(R1 복제본 읽기·7777)이다. → 위 「11-06 PR299 병합과 진짜 녹화」의 R1이다.
- **창 R1 서버 v23 빌드(2026-11-04, 메인 `msg_6701b9629720`):**
  - 계약은 생성 스크립트 `build-03/scripts/build-contract-v1.cjs`로 만들었다. `build-03/contract-v1.md` sha256 `8b329ced2a9fa6f79c62a16c3bc438c7d514152c3911bb7f49491b39963e36c3`, 규칙 원문 대조 ALL VERBATIM, 경로 확인 OK다.
  - 신규 `[Core Sol]`을 14:16:49Z에 리드 pane split으로 띄웠다. 첫 화면은 `GPT-6.1-Sol max`, 업데이트 창 없음, backend 실제 모델 unknown이다. Task `task_c43fe3632248`, Dispatch `ctx_8fa14238a163`. 이 작업자는 계획 보고(`return-01/msg-recording-core-plan-to-main-v1.txt` 12행)대로 Sol max로 띄웠고, 메인이 그 계획 순서에 동의했다(`msg_6701b9629720`).
  - S1 관문(14:21:12Z~14:21:24Z) 통과: 물리 여유 16,419,905,536바이트, commit 여유 26,803,781,632바이트. HEAD `3b03387bafae1866f6a917f76c75d30f45ecad75`, 작업 트리 깨끗함.
  - `sync-wsl.sh build` 1회, 14:21:41.400Z~14:22:23.432Z(42.032초), exit 0, `80 Warning(s)`·`0 Error(s)`.
  - S3 11항 통과. 복제본 소유 표식이 core-active이고, `GameServer.dll` 353,280바이트·mtime 14:22:11.68Z가 빌드 시작 뒤다. 복제본 `Current = 23`, sha256 `a62b3b58…24e2`가 Windows와 같다. 리드가 정산 뒤 다시 읽어 같음을 확인했다(`build-03/lead-sample-check-clone-02.txt`; `-01`은 Git Bash 경로 변환으로 실패한 시도).
  - S4: SQL 서비스 Running, 15433은 sqlservr, 공개 CA hash 일치, 만료 2026-11-17T09:48:58Z. 연결 시도는 없었다.
  - 정산: worker_done `msg_3ec36ee963a4` succeeded(9분 45초), 작업자 쓰기의 경계 밖 0건, pane 닫음(`ptyKilled: true`).
  - 창 끝 확인(14:28:28Z~14:28:38Z): WSL GameServer 0, WSL 7777 0, Windows 7777 LISTEN 0, dotnet·Unity·GameServer·VBCSCompiler 0. 남은 codex.exe 셋은 작업자 기동 전에 시작한 남의 프로세스로 손대지 않았다.
  - 14:28:38Z 메인에 「창 끝」을 보냈다(`msg_233deae58ac2`). 기록 전체는 `build-03/lead-launch-record-v1.md`다.
- **카드 v4(리허설·녹화 공용, 뒤에 v5·v6으로 바뀜):** `lead/rehearsal-user-server-card-v4.md`, sha256 `7deeb64c646a8a8afe0d3fcbf5eea2a9c2a71d7c882ee1f8204ede70a2e2c63e`(153줄). 107 A대로 1단계 자격 줄을 `$env:DAWNHOLDER_DB_CREDENTIAL_PATH` 우선으로 바꿨다. 나머지 블록 16줄은 v3와 같다(`lead/card-v4-block-compare-01.txt`). PowerShell 구문 분석은 프로필 없이 오류 0이다(`lead/card-v4-start-block-parse-01.txt`). 원천 launcher·Program.cs blob(`1e3476e0`·`f83d026a`)은 v3 때와 같다(`lead/return-01/card-v4-source-lines-01.txt`). 메인 대조 일치(`msg_0014a4730021`).
  - v5(`lead/rehearsal-user-server-card-v5.md`, sha256 `688198016d660400b8d0b8b2426cd59c46f8315dd339c73f017df4466fe41b71`, 생성 `rehearsal-01/make-card-v5.cjs`)는 131 C에 맞춰 개요 「한 판」과 모니터 문장만 바꿨다. 메인 확정은 `msg_67f019755590`이다. 리허설은 v5로 했다.
  - v6은 위 11-05~06 항목이다.
- **Content 녹화 순서 참고(`msg_e58c6d6b1f73`, 답 불요):** 퀘스트 HUD가 켜진 뒤 인벤토리 창을 화면 오른쪽 끝에 두면 윗줄 칸이 HUD 아래에 깔린다는 알림이었다. PR297의 정렬 고침으로 닫혀서(Content 리드 `msg_59ba4e32d30a`) 녹화 순서에 주의 줄을 넣지 않았다.
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
- 11-04까지의 다음 첫 일(다섯 조건이 11-05에 다 채워졌고, 리허설은 맨 위 11-05~06 항목이다): 메인의 리허설 실행 창 배정을 기다린다. 착수 조건은 다섯이다.
  - PR268 병합(채움, 위 항목)
  - Art 인벤토리 격자 PR1 병합(결정 92 B, `msg_e8defeeffd1f`). 채움: PR294 `a888cd35`(2026-11-01T11:39:32Z)
  - Core 인벤토리 24칸 PR 병합(결정 97 A, `msg_3ce270803253`). 채움: PR295 `fbf00de9`(2026-11-04T12:13:47Z)
  - Art PR2 병합(HUD 골드 제거·스킬칸, 「적용 중인 사용자 결정」 11-01 HUD 지시). 채움: PR297 `cfe8bb45`(2026-11-05T16:57:26Z)
  - Content 빌드 경로·exe sha256. 채움: `msg_c8dd2ae6e315`(맨 위 항목)
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
  - 11-05에 바뀜: 녹화 도구는 Content 실행 작업자가 띄우고 끈다(사용자 지시 `msg_7ac87f683832`). 화면은 하나이고 직업별로 한 번씩 녹화한다(결정 131 C). 그래서 110 A의 두 모니터 배치는 쓰지 않는다. 둘 다 아래 11-05 절에 있다.
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

### 2026-11-04 제출 기한과 Codex 세션 배정 — 24칸 goal의 원문을 따름

메인 `msg_76097190e071`이 같은 결정이 이 goal에도 적용된다고 알렸다. 원문은 24칸 goal에 있어서 여기에는 링크만 둔다.

- 제출 기한: [24칸 goal 「2026-11-04 재기동과 제출 기한」](../2026-11-01-inventory-24-slots/goal.md#2026-11-04-재기동과-제출-기한). 2026-11-09(월) 14:00에 USB로 제출하고, 발표는 2026-11-11(수) 16:00이다. 이 goal 첫 줄의 「11-06 시연」은 새 기한으로 읽는다.
- 작업자·검증자 배정: [24칸 goal 「2026-11-02 작업자·검증자 Astra high」](../2026-11-01-inventory-24-slots/goal.md#2026-11-02-작업자검증자-astra-high). 2026-11-04 22:4xZ 이후에는 아래 Astra xhigh 결정으로 바뀌었다.

### 2026-11-04 새 Codex 세션은 모두 Astra xhigh

메인 `msg_2b32a1ec2392`(리드 수신 2026-11-04T22:49:22Z, `.backups/verification/2026-11-01-inventory-24-slots/orca/check-wake-01.json`)가 전달한 사용자 원문이다. 메인 pane에 제출됐다. 이 Core pane 직접 입력으로 격상하지 않는다.

> 이제 앞으로 모든 Codex 작업자는 Astra xhigh, 검증자도 xhigh로 진행해줘

> 이유는 11월 10일에 구독이 끝나는데 그때 갱신 더 안할 예정이라

- 위 11-02 「작업자 Astra High·검증자 high」를 대신한다.
- 앞으로 여는 새 Codex 세션은 역할과 무관하게 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh --disable fast_mode`다. 구현자·선행 시험 작성자·독립 검증자·빌드나 캡처 같은 실행 작업자·Gardener·이미지 생성이 모두 들어간다. 태그는 역할 태그 그대로다.
- 이미 떠 있는 세션은 바꾸지 않는다. 띄우지 않은 계약은 실행 명령 줄을 고친 뒤 SHA256을 다시 재서 대조를 청한다. 정본 반영은 11-09 제출 뒤 Rules 몫으로 메인이 들고 있다.
- 이 goal에서: `build-03/contract-v1.md`는 이미 쓴 계약이라 고치지 않았다. 다음 신규 `[Core 검증자]`는 Astra xhigh로 연다.

### 2026-11-04 「완전 대기」와 11-05 대기 5분 규칙

메인 `msg_cc23e064624b`(리드 수신 2026-11-04T19:26:11Z, 24칸 근거 폴더 `orca/wait-179.json`)와 `msg_a7b20ec8baa0`(24칸 근거 폴더 `orca/check-wake-03.json`, 2026-11-05T17:04:57Z)가 전달한 사용자 원문이다. 메인 pane에 제출됐다. 이 Core pane 직접 입력으로 격상하지 않는다.

> 불필요한 대기를 하는 리드급은 완전 대기상태로 전환해줘

> 앞으로는 너무 오랜시간 대기는 하지 말아줘 5분 넘어서 마지막 작업 이후 응답이 나한테 없으면 대기하지 말아줘 토큰낭비야

- 리드는 작업자나 검증자가 돌고 있을 때만 우편함 대기를 연다. 메인을 기다릴 때는 대기·주기 확인·heartbeat 없이 턴을 끝낸다.
- 다시 일할 때는 메인이나 깨움 도구가 「Orca 메시지를 확인하라」를 넣고, 리드는 `--wait` 없는 check 한 번으로 읽는다.

### 2026-11-05 결정 129 A·130 A와 녹화 자동화 지시

메인 `msg_8870ac1e2126`(24칸 근거 폴더 `orca/check-wake-09.json`, 19:09:03Z)과 `msg_7ac87f683832`(`orca/check-wake-10.json`, 19:16:54Z)가 전달한 사용자 원문이다. 메인 pane에 제출됐다. 이 Core pane 직접 입력으로 격상하지 않는다.

> 1) 129 - 시연 리허설 시점: 준비되는 대로 바로 vs 시각 지정 → A 준비되는 대로 · 2) 130 - 리허설에서 녹화 도구도 시험할지 → A 시험한다

> 녹화는 내가 하는게 아니라 직접 시나리오 봇 돌려서 전부 클로드로 진행해줘

- 녹화 도구는 Content 실행 작업자가 띄우고 멈춘다. 서버 시작·자격 입력·서버 창 Enter는 그대로 사용자 손이다.
- 130 A의 「녹화 도구는 R2 통과 뒤 사용자가 띄운다」는 뒤 지시로 바뀌었다.
- Core 회신(메인 `msg_f748b762af9f`, Content `msg_3d0ad23c6476`): 카드 v4에는 녹화 도구 단계가 없어 고치지 않았다. 녹화 순서는 v2에서 R2 칸만 바꾼다.
- 같은 무렵 Content 리드 `msg_59ba4e32d30a`가 인용한 사용자 결정 123 A(마법사 배경 음악 끔)로 마법사 명령에 `-demo-no-bgm`이 붙는다. 기존 옵션이라 코드 변경은 없다. 원문은 Content goal 몫이다.

### 2026-11-05 결정 131 C — 화면 하나, 직업별로 한 번씩

메인 `msg_67f019755590`(24칸 근거 폴더 `orca/check-wake-12.json`, 19:43:30Z)이 전달한 사용자 원문이다. 메인 pane에 제출됐다. 이 Core pane 직접 입력으로 격상하지 않는다. Content에는 메인 `msg_cf71f4c479f7`로 전달됐다.

> 1) 131 - 리허설·녹화 화면: 보조 모니터 연결 vs 화면 하나 → C 코멘트 「같은 시나리오니까 각각 한번씩 녹화하고 이후에 편집할때 조정」

- R4가 R4-1(기사 녹화)과 R4-2(마법사 녹화) 두 회차가 된다. 녹화하지 않는 쪽은 숨긴 진행용 창이다. R2 관문은 R4-2 앞에서 한 번 더 잰다.
- 화면 잘림 확인은 실행자 측정과 Content 리드 판정으로 바뀌어 사용자 손에서 빠진다.

### 2026-11-05~06 결정 133 A·134 A·135 A — 진짜 녹화의 출발 상태·녹화 늦음·새 이름

메인 `msg_424a61a49d20`(24칸 근거 폴더 `orca/check-wake-18.json`, 21:44:38Z)과 `msg_5b1efce641fb`(`orca/check-wake-20.json`, 21:50:46Z)가 전달한 사용자 원문이다. 메인 pane에 제출됐다. 이 Core pane 직접 입력으로 격상하지 않는다. Core가 올린 결정 글은 `lead/rehearsal-01/msg-decision-same-start-v2.txt`(메인 `msg_7b111d608c48`)다.

대시보드 결정 응답(메인 pane 제출, `msg_424a61a49d20`):

> 1) 133 - 진짜 녹화 두 회차 출발: 회차 사이 서버 재시작 vs 재시작 두 번 → A 회차 사이 재시작 · 2) 134 - 녹화 시작이 늦어 캐릭터 생성 장면을 놓칠 위험: 녹화 스크립트만 고침 vs 클라 빌드 고침 → A 녹화 스크립트만

대시보드 결정 응답(메인 pane 제출, `msg_5b1efce641fb`):

> 1) 135 - 진짜 녹화 1회차 상대 법사의 새 이름: 초안 그대로 vs 다른 이름 → A 초안 그대로

- 133 A: 두 회차 사이에 서버를 한 번 다시 켠다. 1회차는 기사를 녹화하고, 상대 마법사는 새 계정으로 만든다. 2회차는 마법사를 녹화하고, 상대 기사는 1회차 기사를 다시 고른다. 리허설의 R7 판은 이 선택으로 합쳐진다. 사용자 손은 카드 1·3·4·6단계 넷이다.
- 134 A: 녹화가 늦게 걸리는 문제는 Content 녹화 스크립트에서 막는다. 클라 빌드는 그대로다.
- 135 A: 1회차 상대 마법사는 `rec_ally_mage`·「새벽동료」다.
- 메인 지시(같은 `msg_424a61a49d20`): Core는 진짜 녹화용 사용자 카드 v6과 녹화 순서 v2를 쓴다. 카드 v6에는 「사용자가 서버 창의 정해진 줄만 옮긴다」를 넣는다. 결과는 「재개 지점」 맨 위 항목과 아래 「녹화 실행 순서」 v2다.

### 2026-11-06 결정 137 A·138 A — 서버 손과 되돌리기 쉬운 운영 결정은 메인

메인 `msg_def8ae37cce2`(2026-11-06T01:06:27Z, 24칸 근거 폴더 `orca/check-wake-31.json`)가 전달한 사용자 원문이다. 메인 pane에 제출됐다. 이 Core pane 직접 입력으로 격상하지 않는다.

대시보드 결정 응답(메인 pane 제출, `msg_def8ae37cce2`):

> 1) 137 - 서버 시작·정지·재시작을 메인이 대신 할지: 메인이 대신 vs 지금처럼 사용자 손 → A 메인이 대신 · 2) 138 - 운영 결정(이름 초안·재시작 순서·처분 같은 것)을 메인이 추천안으로 정할지 → A 메인이 정함

- 137 A: 메인이 사용자 서버 창에 `& .\99_Tools\Start-PersistenceServer.ps1 @serverArgs` 한 줄과 Enter를 보낸다. 그 창의 `$serverArgs`는 사용자가 처음 넣은 자격 경로를 이미 담고 있다. 서버 창 줄은 허용 목록 추출로만 읽는다. AI는 자격 파일·경로 값을 읽거나 출력하지 않는다.
- 카드 v6의 사용자 손 넷(1·3·4·6단계)은 진짜 녹화에서 메인이 대신 했다. 카드는 고치지 않는다(같은 메시지의 메인 지시).
- 138 A: 되돌리기 쉬운 운영 결정(이름 초안·순서·처분 등)은 메인이 추천안으로 정하고 보고만 한다. 리드도 결정 요청을 이 기준으로 걸러 메인에 올린다.

### 2026-11-06 사용자 확인 범위

메인 `msg_93ec74a2b26b`(2026-11-06T01:07:41Z, 24칸 근거 폴더 `orca/check-wake-32.json`)가 전달한 사용자 원문이다. 메인 pane에 제출됐다. 이 Core pane 직접 입력으로 격상하지 않는다.

> 앞으로는 내가 체크해야 하는 부분은 계획 승인, 결과 받고 피드백, 관리자 권한 승인, 에이전트 단독으로 정하기 어려운 트레이드오프 문제(되돌리기 어려운 경우)

- 메인 적용: 사용자에게 묻는 것은 goal 계획·범위 승인, 결과 보고 뒤 피드백, 관리자 권한 승인, 되돌리기 어려운 trade-off뿐이다. PR 병합의 `병합 승인:` 한 줄은 AGENTS Git 권한 정본이라 그대로다. 그 밖의 운영 결정은 메인이 추천안으로 정한다(138 A를 넓힘).
- 리드는 메인이 정할 수 있는 것을 추천안과 함께 「메인 결정 요청」으로 올린다. 메인은 이 결정을 메인 checkout CLAUDE.md에 적었다(commit 없음). 정본 반영은 11-09 뒤 Rules 후보다.

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
- 녹화는 메인이 한다. 10-28 PR268 실측에 쓴 저장소 밖 ffmpeg 데스크톱 녹화 방식이다. (11-05 사용자 지시 `msg_7ac87f683832`로 Content 실행 작업자가 녹화 도구를 띄우는 것으로 바뀌었다.)
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

메인 `msg_6701b9629720`(2026-11-04T14:12:33Z 배정). 사용자 결정이 아니다.

- 창 R1(WSL 단독 약 20분)을 14:12:33Z부터 배정했다. Unity·Windows dotnet·DB·7777 0 조건이다.
- 계획 (가)~(마) 순서에 동의했다(계획 보고 `msg_547d31cdf5a3`).
- 리허설 창(약 60분)은 Art PR2 병합과 Content 녹화 빌드 고정이 끝나면 사용자 시각을 받아 배정한다. 사용자 서버 탭(core-active 루트 PowerShell 7), R0의 7777 확인, 107 A 프로필 변수는 그때 메인이 사용자에게 함께 묻는다.
- 리허설 이름 겹침은 생성 화면 실패로 드러나면 메인에 올린다.

메인 `msg_0014a4730021`. 사용자 결정이 아니다.

- 카드 v4 시작 블록 대조가 일치했다. v4 sha256 `7deeb64c…2c63e`, v3 `d4a869d0…dbabd` 둘 다 보고값과 같다. 1단계 블록의 차이는 자격 경로 한 줄 → 두 줄(환경 변수 먼저, 비면 Read-Host)뿐이다.
- 지킬 것 세 줄(채팅에 경로 붙이지 않음, AI가 파일·프로필 값을 보지 않음, AI가 시작·입력·종료 Enter를 대신하지 않음)이 남아 있다.
- 이 카드로 리허설과 녹화를 한다. 사용자에게는 리허설 창 배정 때 메인이 전한다. (뒤에 131 C로 v5, 133 A로 v6이 됐다.)

메인 `msg_5631af607d10`. 사용자 결정이 아니다.

- 창 R1 반납(`msg_233deae58ac2`)을 받았고 이어서 Art W6F(Unity 약 45분)를 열었다.
- (다)는 Content 고정 head가 오면 한다. 그때까지 창을 쓰지 않는다.

메인 `msg_a5a6721188ce`(리드 수신 2026-11-05T18:39:40Z, 24칸 근거 폴더 `orca/check-wake-05.json`). 사용자 결정이 아니다.

- Content 녹화 빌드를 수용했다. 독립 판정 불합격 사유 D1·D3는 절차·원시 문제이고, 메인 처분은 수용 A(`msg_b50c04ca0c08`)라 재빌드는 없다.
- R2·R4·R7·클라 종료는 신규 Content 실행 작업자(Astra xhigh) 한 명이 맡는다.
- 분담: Core 안 `msg_ebb4e51fb548`에 Content가 `msg_c8dd2ae6e315`로 동의했다. 메인은 Content 알림(R4 끝·두 클라 닫힘, R7 끝·두 클라 닫힘, R2 미달)으로 R5·R8의 「지금」을 정한다. Core는 창 시작에 복제본 읽기 확인과 7777 확인을 한 뒤 「서버 빌드 맞음」을 메인에 보낸다.
- Content 리허설 실행 계약 v1(`content-active/.backups/verification/2026-10-31-demo-autopilot-character-select/lead/rehearsal-run-contract-v1.md`, sha256 `0d636c3c3dc85b83dcaf069a9b2f09442a4c47b1565c1914b053230f096753e7`, `msg_ac200a0295be`)과 v2(같은 폴더 `rehearsal-run-contract-v2.md`, sha256 `3b3b354740c853d2df9052f4ec6780abd6da05416cf2cc7c29744bd9fe244c1b`, `msg_2a8876645f98`)를 Core 녹화 순서와 대조했다. v2의 차이에 대한 Core 회신은 메인 `msg_81f4008de5a5`·Content `msg_b752a2465246`이다(원문 `lead/rehearsal-01/msg-contract-v2-diff-v1.txt`).
  - R2 `measure-gate.ps1 … -MinimumPhysicalBytes 6GB -MinimumCommitBytes 4GB -AllowDotnet`: Core R2 칸은 메모리 두 값만 정하므로 dotnet 수 면제는 Core 기준과 어긋나지 않는다.

메인 `msg_67f019755590`(리드 수신 2026-11-05T19:43:30Z). 131 C 원문을 뺀 나머지는 메인 판단이다.

- 리허설 카드 v5를 확정했다(`688198016d…1b71`, diff 넷).
- 진짜 녹화는 두 회차가 같은 출발 상태여야 한다(Content에는 `msg_25e147f44c7f`로 「둘 다 캐릭터 생성부터」). 방법과 사용자 손 시간을 Content와 맞춰 리허설 결과와 함께 올린다. → Core 결정 글과 133 A.
- 같은 출발 상태 조율: Core 첫 안 `msg_3a1b8966627b`(같은 계정 선택)는 거뒀다. Content 질문 `msg_2683f720f602`에 Core가 `msg_b28231a86423`으로 답했다(재시작 필요, `GuildRegistry`에 되돌리기 경로 없음). Content 동의는 `msg_5e1339c81084`다.

메인 `msg_5100b8982d34`(리드 수신 2026-11-05T20:47:08Z, 24칸 근거 폴더 `orca/wait-181.json`). 사용자 결정이 아니다.

- 서버 창 원문은 메인이 읽지 않는다. 자격 경로가 보일 수 있어서다.
- 세 값에 서버 창 원문이 필요하면, Core가 받을 줄을 메인에 정확히 적어 보낸다. 메인이 사용자에게 그 줄만 옮겨 달라고 한다. Core가 적어 보낸 8줄은 `msg_4cdd8331fcb4`(원문 `lead/rehearsal-01/msg-to-main-server-lines-v1.txt`)다.

메인 `msg_544f74c0c0f2`(리드 수신 2026-11-05T21:50:46Z). 사용자 결정이 아니다.

- 카드 v6 초안(`0d3353cb…0c7d`)의 대조가 통과했다. v5 → v6 diff 129줄에 코드 블록 줄이 없어 시작 블록·재시작 한 줄·서버 창 문구·실패 문구는 v5와 같다. 사용자 손 넷(1·3·4·6)과 133 A 순서가 맞다.
- 135 이름과 리허설 8줄이 오면 메인이 원문으로 보낸다. 그 사이 goal 기록·검증자 준비는 진행해도 된다.

메인 `msg_3a07539cc5ae`(리드 수신 2026-11-05T22:06:37Z, 24칸 근거 폴더 `orca/check-wake-22.json`). 메인이 전한 사용자 원문은 메인 pane의 「붙힐 8줄과 위치 알려줘」, 「아니면 클로드가 해줄 수 있으면 대신 해줘」, R8 「엔터 완료」다. 이 pane 직접 입력으로 격상하지 않는다.

- 리허설 8줄은 사용자 요청에 따라 메인이 뽑았다. 서버 창을 `orca terminal read`로 메모리에서만 읽고, 허용 목록 정규식(`Server stopped`·`Persistence output`·`Owned WSL process PID`·`Listening`·실패 문구 셋)에 맞는 줄만 출력했다. 원문 전체는 저장·출력하지 않았고, 맞은 줄 안에서 가린 경로 모양은 0이다. 스크립트는 메인 개인 도구 폴더의 `main-notes/2026-11-04/server-window-8lines-0715.cjs`다.
- 같은 출력에 R3 첫 시작의 PID 줄과 Listening 20:45:37.485Z도 나왔다. 실패 문구는 0줄이다.
- 카드 v6의 「메인은 서버 창을 읽지 않는다」는 그대로 둔다. 진짜 녹화에서 사용자가 같은 요청을 하면 메인이 같은 스크립트로 뽑는다. 이 방식을 진짜 녹화의 기본으로 할지는 사용자에게 묻지 않았다.
- 8줄 앞의 「R5:」「R6:」「R8:」 표식은 메인이 행 순서를 보고 보내기 스크립트(`main-notes/2026-11-04/send-8lines-0718.cjs` 7~14행)에 손으로 붙였다. 추출 스크립트는 tail 행 번호와 줄만 출력했다. R3 두 줄은 빼고 보냈다(메인 `msg_d7e95c0cb4c3`, 리드 수신 2026-11-05T23:17:37Z).

메인 `msg_d20436e197d3`(리드 수신 2026-11-05T23:24:13Z, 24칸 근거 폴더 `orca/wait-191.json`). 사용자 결정이 아니다. 경계 사건의 첫 발생 기록이다.

- 사건: 리드가 verify-02 검증자 기동 전·종료 뒤 `orca terminal list --json` 출력을 통째로 저장했다(`verify-02/launch-terminal-list-before-v1.json`·`lead-terminal-list-after-close.json`). 이 출력은 터미널마다 `preview` 필드(300자)를 담아, 사용자 서버 창 미리보기가 두 파일에 들어갔다. verify-03 검증자가 pane 종료 사실을 대조하려고 두 번째 파일을 읽다가 미리보기가 도구 출력에 나타났다. 검증자는 바로 멈추고 공식 질문으로 알렸다(`msg_f14f69163424`). live terminal read·서버 실행·자격 접근은 없었다. 리드는 필드 이름과 길이만 봤다.
- 메인 확인: 메인이 내용을 출력하지 않고 패턴만 봤다. 자격·credential 문구와 자격 파일 확장자가 없었고, 자격 파일 경로가 담겼을 흔적은 없다.
- 처분 B: 두 파일의 서버 창 줄 `preview` 값만 「<가림>」으로 바꿨다. 값은 출력하지 않았다. sha256은 앞 파일 `92858b38…3ebd` → `cd50f2ff…2793`, 뒤 파일 `e327db23…f7a3` → `09b63101…51c1`이다. 기록은 `lead/redact-1106/`이다. verify-03은 리드 판단대로 실사를 이어 끝냈다.
- 재발 방지: 리드는 터미널 목록을 handle·제목·경로만 뽑아 저장한다. 첫 발생이라 새 반복 규칙은 만들지 않는다(CODE_CONVENTION 「교정 층과 반복 규칙」). 메인도 서버 창 읽기는 허용 목록 추출만 쓴다.

메인 `msg_41665d27bd70`(리드 수신 2026-11-06T01:30:05Z, 24칸 근거 폴더 `orca/check-wake-40.json`). 사용자 결정이 아니다. 위 경계 사건의 추가 기록이다.

- 발견: 종료 Gardener 입력을 고르면서 리드가 두 근거 폴더(이 goal·24칸 goal)에 저장된 터미널 목록 23개를 셌다. 내용은 출력하지 않고 개수·길이·표식 문구 유무만 봤다(`closeout-01/scan-terminal-list-previews-v1.cjs`·`-v2.cjs`, 출력 `-01.out`·`-02.out`). 사용자 서버 창 preview가 남은 파일이 셋 더 있었다. 이 goal의 `build-01/lead-terminal-list-after-close.json`·`docs-01/lead-terminal-list-after-close.json`(`term_3a5ec295`, 10-31)과 24칸 근거 폴더 `orca/terminal-list-before-g1-switch.json`(`term_f672813a`, 11-01)이다. 셋 다 위 재발 방지보다 앞선 저장분이다. 리드 요청은 `msg_144a34872f60`이다.
- 메인 확인: 내용을 출력하지 않는 패턴 검사로 셋 다 자격·credential 낱말, 자격 파일 확장자, 자격 입력 요청 문구가 0이었다.
- 처분 B(138 A로 메인이 정함): 세 파일의 해당 preview 값만 「<가림>」으로 바꿨다. sha256 전후는 `closeout-01/redact-02/redact-result-v1.txt`에 있다. 다시 센 결과 가리지 않은 서버 창 preview는 0개다(`closeout-01/scan-terminal-list-previews-03.out`·`-04.out`).
- 새 반복 규칙은 만들지 않았다. 재발 방지 뒤의 새 발생이 아니라 그 전에 저장된 파일이어서다. 저장 구조 후보는 Gardener 후보 2로 BACKLOG `terminal-evidence-field-allowlist`에 옮겼다.

## 요구사항 원천

- 범위 초안 전문: `.backups/verification/2026-10-31-db-persistence-demo-build/lead/scope-draft-v1.md`(sha256 위와 같음). 초안의 「확인한 사실」 표가 근거 줄을 담는다.
- 선행 goal: [캐릭터 저장의 서버 재기동·불러오기 합동 확인](../2026-10-24-character-storage-restart-load/goal.md)의 10-29 합동 실DB 창(S1~S8 통과, PR272), [캐릭터 선택·생성 화면](../2026-10-24-character-select-create-ui/goal.md)(PR273), [캐릭터 목록·생성·선택 서버 처리](../2026-10-18-character-server/goal.md)(PR270).
- 서버·Shared·ClientNet·launcher·sync-wsl 코드는 PR272 병합(`34756e01`) 뒤 main `f5adcf31`까지 바뀌지 않았다. launcher는 10-29 창판 `9992b1c9`(sha256 `F27EEC2D…04C6`)와 main판 `f3e94759`(sha256 `d4957d6a…6754`)가 공백 한 줄만 다르다(`git diff -w` 빈 출력).

## 범위

두 단계로 나눈다. 단계 A는 지금 보기이고 코드 변경이 없다. 단계 B는 녹화 영상 준비다(결정 79 C 뒤 범위 초안 v2, 메인 `msg_ff94804f5f76` 통과). v1의 시연자 카드·빌드 리허설·사용자 리허설은 v2의 녹화 순서·녹화용 리허설·녹화 담당 인계로 바뀌었다. 초안 전문은 `lead/scope-draft-v2.md`다.

### 만들 것

1. **지금 보기:** 메인 배정 창에서 Legacy를 내리고 core-active에서 영속 서버를 띄워, 사용자가 지금 쓰는 Editor로 새 캐릭터 선택창을 본다.
2. **영속 서버 실행 절:** `00_Document/operations/DEVELOPMENT.md`에 「DB 영속 서버 실행」 절을 더한다. 빌드 → launcher 시작 → Enter 정지 순서, 7777 충돌, 실패 문구별 대처를 담는다.
3. **녹화 실행 순서(v3, 결정 82 B):** 자동 진행기 두 클라(기사·마법사) + 영속 서버 한 판 기준의 서버 쪽 순서를 이 goal에 둔다. Legacy Ctrl+C → 영속 서버 Listening → 두 클라 시작·자동 진행 → (DB 장면은 자동 진행이 끝나고 연결이 닫힌 뒤) Enter·재시작·같은 캐릭터 목록 → 녹화 끝 → Enter. Core 몫은 서버 순서·대기 시간·정지/재시작 위치다. 녹화 도구는 메인, 자동 진행기는 Content 몫이다. (11-05 바뀜: 녹화 도구도 Content 실행 작업자가 띄운다, 사용자 지시 `msg_7ac87f683832`. 순서는 결정 131 C·133 A로 두 회차가 됐다.)
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

## 녹화 실행 순서(서버 쪽, v2 — 리허설 뒤)

결정 82 B·131 C·133 A의 진짜 녹화에서 Core가 맡는 서버 쪽 순서다. 클라 순서·자동 진행·녹화 도구는 Content 실행 계약(v3), 사용자 손의 문구는 Core 사용자 카드 v6(`lead/recording-user-server-card-v6.md`)이 정한다. 칸마다 확인 수준을 적었다.

v1에서 바뀐 것:

- 결정 133 A: 두 회차 사이에 서버를 한 번 다시 켠다. R4가 R4-1(기사 녹화)과 R4-2(마법사 녹화)로 나뉘고, R5·R6이 그 사이로 온다. 따로 돌던 R7 판은 없어지고, R4-2의 상대 기사 선택으로 같은 캐릭터 기록을 남긴다.
- 결정 131 C: 화면 하나에서 직업별로 한 번씩 녹화한다. 결정 110 A(모니터마다 1920×1080)는 쓰지 않는다. 녹화하지 않는 쪽은 숨긴 진행용 창이다.
- 녹화 도구는 Content 실행 작업자가 띄우고 끈다(메인 `msg_7ac87f683832`의 사용자 지시). v1의 「녹화 시작·끝은 메인이 정한다」와 「사용자 녹화 도구」를 바꾼다.
- 결정 134 A: 녹화가 늦게 걸리는 문제는 녹화 스크립트에서 막는다. 클라 빌드는 그대로다.
- 메인은 서버 창 원문을 읽지 않는다(`msg_5100b8982d34`). 사용자가 카드 v6 「옮길 줄」의 8줄만 옮긴다. 리허설 8줄은 사용자 요청으로 메인이 허용 목록 스크립트로 뽑았다. 진짜 녹화에서도 사용자가 같은 요청을 하면 같은 스크립트로 뽑는다(`msg_3a07539cc5ae`).
- 빌드 기준: 서버와 녹화 클라는 고정 head `cfe8bb45c0e63820deccbb5c364e461768f09c1e`다. 프로토콜은 v23이다.

| 순서 | 할 일 | 누구 | 확인 수준·근거 |
|---|---|---|---|
| R0 | 7777을 쓰는 서버가 없는지 본다. Legacy가 떠 있으면 그 창 소유자가 Ctrl+C로 끈다 | 소유자 | 리허설 사전 확인에서 WSL·Windows 7777 LISTEN 0(`lead/rehearsal-01/precheck-01.txt`). Legacy 정지 키는 `run-server.bat:7` |
| R1 | 고정 head가 build-03 소스와 서버 빌드 입력 diff 0이면 build-03으로 갈음한다. 녹화 창 시작에 복제본 읽기 확인(소유 표식·`GameServer.dll` 크기와 수정 시각·`Current = 23`)만 한다. diff가 있으면 배정된 Core 작업자가 `wsl -d Ubuntu -- bash 99_Tools/sync-wsl.sh build`를 1회 돈다 | Core 리드(읽기)·Core 작업자(빌드) | `lead/take-fixed-head-01/take-verify-01.txt`(diff 0). 리허설 사전 확인 통과(20:15:56Z~20:16:05Z) |
| R2 | 서버 Listening 뒤, Player 두 개와 녹화 도구를 띄우기 직전에 물리 여유 6GiB·commit 여유 4GiB를 원시로 남긴다. R4-1 전과 R4-2 전에 한 번씩 잰다 | 배정 창의 실행자(Content) | 메인 결정 `msg_0d7e73a9805d`. 리허설 관문 원시 두 개가 모두 `passed=true`다(Content 실행 원시, 독립 검증 아님, Content `recording-build-01/rehearsal/run-02/`). R4-1 전 `r2-gate.json`은 20:49:15Z에 물리 여유 15.48GiB·commit 여유 25.26GiB, R4-2 전 `r2-gate-r4-2.json`은 20:58:43Z에 16.75GiB·26.29GiB였다. R4-2는 15초 쉼과 관문 뒤 0.9초에 기동했다(Content 리드 판정 17행) |
| R3 | 사용자가 카드 v6 1단계 블록으로 서버를 시작한다. 세 줄(PID 줄 → launched 줄 → `[Persistence stdout …] Listening on 0.0.0.0:7777. Press Enter to stop.`)을 기다린다 | 사용자 | 리허설 「서버 떴음」 메인 수신 2026-11-05T20:47:07Z. 10-31 실측 프로세스 생성 → Listening 약 0.9초(1회) |
| R4-1 | 1회차(기사 녹화). 막 켠 서버에서 기사 `rec_knight`·「새벽기사」와 상대 마법사 `rec_ally_mage`·「새벽동료」(결정 135 A)를 새로 만든다. 녹화 대상은 기사 창 하나다. 서버 창에는 키를 누르지 않는다 | 메인·Content 순서 | 리허설 R4-1: 두 역할 생성 → done, 자동 진행 73.2초·76.6초, 기사 거점 level=2(Content `recording-build-01/rehearsal/run-02/r4-1-steps.txt`). 명령과 녹화 인자는 Content 계약 v3 고정본 |
| R5 | 1회차 두 클라가 닫힌 뒤 서버 창 Enter → `Server stopped.`와 출력 요약 두 줄 → 입력 화면 | 사용자 | 리허설: 메인 「지금」 21:00:46Z → 「멈췄음」 수신 21:09:47Z(사용자 응답 포함). 서버 창 `Server stopped` 21:09:25.530Z, 정지 요약 두 줄 통과(stdout `excluded=2022`, 나머지 0·`drained=True`) |
| R6 | 같은 서버 창에서 `& .\99_Tools\Start-PersistenceServer.ps1 @serverArgs` 한 줄로 다시 띄운다. 세 줄을 기다린다. 길드 거점·금고·퀘스트·entity 번호가 처음으로 돌아간다 | 사용자 | 리허설: 메인 「지금」 21:09:47Z → 「다시 떴음」 수신 21:20:18Z, 10분 31초(사용자 응답 포함). 서버 창 기준 정지 → Listening 10분 25.862초(사용자 응답 포함), birth → Listening 0.896초. 되돌림 근거는 서버 계약 v1.1 §2 |
| R4-2 | 2회차(마법사 녹화). R2를 다시 잰 뒤 마법사 `rec_mage`·「새벽법사」를 새로 만들고, 상대 기사 `rec_knight`는 「새벽기사」를 다시 고른다. 그 선택 칸을 R4-1 생성 칸과 대조해 같은 캐릭터 기록으로 남긴다 | 메인·Content 순서 | 리허설 R7: 재시작 뒤 두 역할 slot 1 선택 → town 21:21:54Z, R4 칸과 같다. 리허설 R4-2: 재로그인 AlreadyOnline 0. 캐릭터 목록 → 마을은 생성 2.2~2.5초·선택 2.1~2.3초. 한 판에 생성과 선택이 섞인 조합은 재지 않았다 |
| R8 | 2회차 두 클라가 닫힌 뒤 서버 창 Enter → 세 줄 → 입력 화면. 사용자가 카드 v6 「옮길 줄」의 8줄을 메인에 옮긴다(사용자가 요청하면 메인이 허용 목록 스크립트로 뽑는다). 필요하면 소유자가 Legacy를 다시 띄운다 | 사용자·소유자 | R5와 같다. 리허설 R8: `Server stopped` 22:03:23.042Z, 정지 요약 두 줄 통과(stdout `excluded=2681`, 나머지 0·`drained=True`) |

- 진짜 녹화(2026-11-06): 사용자 결정 137 A로 R3·R5·R6·R8의 「사용자」 손은 메인이 대신 했다. 표와 카드 v6은 고치지 않는다(메인 `msg_def8ae37cce2`). R0는 R1 사전 확인의 7777 LISTEN 0으로 봤다. 단계 시각·8줄·세 값은 재개 지점 「11-06 PR299 병합과 진짜 녹화」에 있다.
- 이름(완료조건 5)
  - 리허설: `rh1101_knight`·「리허설기사」, `rh1101_mage`·「리허설법사」(결정 109 A). 리허설에서 만들어졌으니 다시 쓰지 않는다.
  - 녹화: `rec_knight`·「새벽기사」, `rec_mage`·「새벽법사」(결정 109 A), 1회차 상대 마법사 `rec_ally_mage`·「새벽동료」(결정 135 A, 메인 `msg_5b1efce641fb`).
  - 이름은 DB 전체에서 하나이고 칸은 계정당 3개다. 행은 지우지 않는다.
- 재시작은 R4-1과 R4-2 사이에만 둔다. 회차 도중에 다시 켜면 자동 진행기의 메모리 상태가 처음으로 돌아간다.
- 재시작이 필요한 이유(소스 확인 수준): 길드 거점·금고는 서버 실행 단위 메모리이고 되돌리는 경로가 없다(`GuildRegistry.cs` 80·83·87·93·100·149행, `GuildRules.cs:15`). 리허설에서 재시작 없이 돈 R4-2의 기사는 거점 `already max`였다(Content 리드 판정 46행).
- 서버 checkout은 공개 profile이 있는 core-active다. launcher는 자기 checkout 안의 profile만 받는다.
- 편집에 미리 알 것(Content 판정): 파티 칸은 「직업 (entity 번호)」만 보이고 다른 캐릭터 이름은 없다. 회차 사이 재시작으로 두 영상 모두 낮은 번호부터다. 첫 로그인 칸의 이전 계정 이름은 편집에서 자른다.
- v1(리허설 전)은 commit `2cb8910d`의 같은 절이다. v2의 초안과 옮긴 commit은 `lead/recording-order-v2-draft.md`에 있다.

## PR 경계와 검증

- **Core PR 1:** goal·DEVELOPMENT 새 절·리허설 기록. 제품 수정이 없으면 문서 PR이고 등급은 문서 실사 + 리허설 원시 대조다.
- Core 소유 결함 수정이 생기면 같은 PR에 넣고 강 등급 독립 검증을 받는다. World·Content 결함은 각 파트 PR이다.
- 병합은 PR마다 메인의 정확 head 사용자 승인 관문을 따른다.
- 실행 창: 지금 보기는 서버 빌드 46초(09:27:42Z~09:28:28Z), 서버 켜짐 09:52:15Z~정지 10:11:31Z로 약 19분이었다. 녹화용 리허설 어림은 약 40~60분이다(클라 빌드 생성 시간 별도, 미측정). 실제 리허설은 사전 확인 20:15:56Z → R3 20:47:07Z → R7 마을 21:21:54Z였다(2026-11-05, 메인 쪽 시각). 서버 창 줄로는 R3 Listening 20:45:37.485Z, R5 정지 21:09:25.530Z, R6 Listening 21:19:51.393Z, R8 정지 22:03:23.042Z였다(R3는 8줄 밖, 메인 같은 출력). 실제 녹화 창은 메인이 정한다. Unity·WSL 병행 직전 물리 여유 6GiB·commit 여유 4GiB를 원시로 확인한다.

## 결과와 남은 위험

PR299 병합본(main `2d49a93e`)과 2026-11-06 진짜 녹화 기준이다. 근거 경로는 모두 `.backups/verification/2026-10-31-db-persistence-demo-build/` 아래다.

| 완료조건 | 결과 | 근거 |
|---|---|---|
| 1. 지금 보기 | 충족. 영속 서버 Listening(2026-10-31T09:52:16.644Z)과 사용자 화면의 새 캐릭터 선택창을 봤다. 정지 `Server stopped.`를 봤고, Windows 쪽에서 7777 점유가 보이지 않았다. Windows `netstat`이 WSL 소켓을 보여 주는지는 재지 않았다 | 재개 지점 「지금 보기」 네 항목, `lead/stage-a-port-release-check-v1.txt`, 메인 `msg_f6b9c253aef7`·`msg_5fcbec368280`·`msg_f48e78cac18f` |
| 2. 녹화용 리허설 | 충족. 리허설 세 판이 통과했다(Content 리드 판정, 독립 검증 아님). 재시작 뒤 두 역할이 생성 없이 같은 slot 1을 골라 마을에 들어갔다. 진짜 녹화 2회차 상대 기사도 1회차 캐릭터를 다시 골랐다(`sameKnightCharacter=true`, Content 실행 기록). Core가 DB에서 CharacterId·이름을 따로 대조하지는 않았다 | content-active 리드 판정 `rehearsal-v2-lead-judgement.md`(sha256 `71b93898…db6a`), `lead/rehearsal-01/results-v1.md`, Content `r4-2-end.json`(sha256 `9aee44e8…cd28`) |
| 3. 순서와 실제 명령 일치 | 보고 기준 충족. 카드 v6 코드 블록 여섯 개는 리허설 카드 v5와 같고, 리허설에서 사용자는 카드와 다른 줄을 알리지 않았다. 진짜 녹화에서 메인이 보낸 시작·재시작 줄은 카드 v6 84·118행과 같은 `& .\99_Tools\Start-PersistenceServer.ps1 @serverArgs`다(메인 보고). 리드는 서버 창을 읽지 않아 입력 bytes를 직접 보지 않았다. 자격 경로는 사용자가 넣은 창 변수로만 쓰였다 | `lead/recording-docs-v2/card-v5-to-v6-r2.diff`, 메인 `msg_86e85d6e92fd`·`msg_def8ae37cce2`·`msg_ce38f9c9dd7d` |
| 4. 독립 실사 | 충족(문서 실사 한정). DEVELOPMENT 1차 verify-01 통과, 리허설 뒤 verify-02 차단(문서 결함 4건) → 수정 → verify-03 통과다. 그 뒤 리드 근거 기록 결함 #RH4-01·#RH5-01을 verify-04·05가 짚었고, 고친 뒤 verify-06이 통과했다. 실사는 빌드·DB·게임·서버를 실행하지 않았다. 제품 수정이 없어 강 기준 코드 검증 대상은 없었다 | `verify-01/`~`verify-06/`의 `verdict.md` |
| 5. 녹화 담당 인계 | 충족. 카드 v6·녹화 순서 v2·서버 계약 v1.2·리허설 세 값을 메인·Content에 보냈다. 녹화용 이름은 리허설과 다르게 미리 정했다(결정 109 A·135 A) | 재개 지점 「11-05~06 리허설과 진짜 녹화 준비」 |

남은 위험:

- 재시작 시간(진짜 녹화 0.777초, 리허설 0.896초)은 몇 번의 관측이다. 상한을 보장하지 않는다.
- 리허설 세 판 통과와 진짜 녹화의 같은 캐릭터 판정은 Content 리드 판정·실행 기록이다. 독립 검증이 아니다.
- 카드 v6은 「사용자 손」 문구 그대로다. 진짜 녹화는 137 A로 메인이 대신 했다.
- 녹화 영상의 화면 판정과 편집은 Content·메인 몫이다. Core는 영상을 보지 않았다.
- 리허설·녹화에서 만든 캐릭터 이름은 DB에 남아 다시 쓸 수 없다. 이름 유일 키가 전역이고 행은 지우지 않는다.
- SQL이 실제로 쓰는 인증서 thumbprint는 미확인이다. 공개 CA 만료는 2026-11-17T09:48:58Z다(재개 지점 「지금 보기 빌드」).

## 다음 계획 후보

새 goal은 자동으로 시작하지 않는다. 아래는 이관이며 채택·구현이 아니다.

- Gardener 후보 1, 문서 원문 인용·집계 비교(독립 결함 #RH2-02·#RH5-01, 인용 네 위치): 기존 BACKLOG `powershell-all-evidence`의 추가 관측이다. 검사화 후보는 출처 path·sha256·인용 문자열로 정확한 부분 문자열인지 보는 비교 helper와, 결함 번호·위치·판정 차례를 다른 필드로 세는 집계다. G-01은 집계 문장 하나라 인용 결함 횟수에 더하지 않았다.
- Gardener 후보 2, 터미널 근거 저장의 목적별 필드 허용 목록(이 goal 근거 네 파일과 24칸 goal 한 파일의 preview 처분): 새 BACKLOG `terminal-evidence-field-allowlist`로 등록했다. 담당 후보는 Management·Rules다.

## 종료 점검 준비

- 종료 Gardener: 미해소 독립 결함 0, 후보 2개, 리드 반환 G-01 1건이다(위 「11-06 종료 Gardener」). 보고서는 [gardener.md](gardener.md)다.
- 종료 기록 PR: PR300이다. 이 goal·`gardener.md`·BACKLOG·CURRENT를 담는다. 신규 `[Core 검증자]` 문서 실사가 통과하면 병합 승인 요청만 메인에 올린다.
- 종료 점검과 R-8은 메인이 판단한다. 다음 goal은 자동으로 시작하지 않는다.
- 제출 기한은 2026-11-09 14:00 USB 제출, 발표는 2026-11-11 16:00이다(위 「2026-11-04 제출 기한과 Codex 세션 배정」).

## 진척 단계

- [x] 범위 초안·승인
- [x] 지금 보기(서버 빌드·화면 확인)
- [x] 실행 절·녹화 순서(v1, 리허설 뒤 v2)
- [x] 녹화용 리허설(2026-11-05 세 판 통과, 서버 창 8줄의 Core 세 값 통과)
- [x] 녹화 담당 인계(카드 v6·녹화 순서 v2·서버 계약 v1.2·리허설 세 값을 메인·Content에 보냄)
- [x] 독립 실사(DEVELOPMENT 1차 통과 `verify-01/`, 리허설 뒤 `verify-02/` 차단 → 수정 → `verify-03/` 통과, 근거 기록 결함 둘을 고친 뒤 `verify-06/` 통과)
- [x] PR299 병합
- [x] 진짜 녹화 Core 몫(R1 사전 확인, 세 값 통과, 서버 손은 137 A로 메인 대행)
- [x] 종료 Gardener(후보 2개, 기록 정정 1건)
- [>] PR300 병합
- [ ] 종료 점검
