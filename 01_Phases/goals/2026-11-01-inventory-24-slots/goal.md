# 인벤토리 24칸(4×6)

인벤토리 칸 수를 8에서 24로 늘린다. 서버가 보내는 인벤토리 snapshot(36번)과 공유 칸 수 상수를 24로 바꾸고, 클라가 받는 쪽까지 같은 PR에서 맞춘다. 화면 격자(4열 × 6줄)는 Art가 만든다.

**현재 상태(2026-11-01 12:3x KST):** 1차 독립 검증(verify-01)이 계약 범위에서 통과했고 메인 R-2 대조도 일치했다. 창 8 지원을 마치고 이 branch로 돌아왔다. Art 격자 PR을 기다린다. PlayMode·실제 접속·Art 시험 3건·CURRENT 줄은 격자 PR 병합 뒤 2차 좁은 재검증에서 본다.

## 재개 지점

- **11-01 재진입(2026-10-31T23:4xZ, 메인 `msg_595eef26f2df`):** 리드는 같은 대화를 이었다(새 handle `term_de9ad49a-99bc-457b-9b06-c888a71b5c37`, run-use consumer_generation 2). 사용자 원문 「지난번에 하던 작업 이어서 진행해서 마무리해보자」.
  - 메인이 2단계 나머지를 WSL 전용 창으로 지금 배정했다. impl-01 report 원천 대조는 통과했다(메인 보고 `msg_f10c78e1f37b`).
  - impl-02 진행 중: 계약 `impl-02/contract-v1.md`(sha256 `6023df6b…1fb0`, 실행 전용), pane `term_023624f0-6160-47cc-85d3-fc40fb4256cc`, Task `task_f5b01da72959`, Dispatch `ctx_82966a060f97`, 23:51:17Z worker-start(ready·observed). 첫 화면 `GPT-6.1-Sol max · Full Access`, Codex v0.166.0, 「⚠ 2 warnings」 표시(내용 미확인, impl-01 첫 화면에는 없었다).
  - GenPackets 공백 64건은 손대지 않는다(메인에 알림 `msg_f10c78e1f37b`).
  - impl-02 끝(worker_done `msg_16c45ccfd1dd`, `impl-02/report.md` sha256 `fef973aa…7f3e`, 리드 원시 대조함). 결과는 다음과 같다.
    - sync·build exit 0(80 Warning(s)·0 Error(s)).
    - Plugins Shared.dll은 v23(sha256 `8d471e729f36b71e7574381bf2772004eaabed362219c87b5f26ca44acd5c025`, 117,248B)이고 `.meta`는 불변이다.
    - 서버 시험 `Failed: 2, Passed: 2550, Skipped: 28`. 봇 `PASS=1 FAIL=0`, snapshot len=204·칸 24.
    - pane은 00:10Z에 닫았다. 창 반납은 `msg_d03abe67bd1e`, 리드의 0 확인은 `lead-window-return-check-01.txt`다.
  - 실패 2건은 36번을 8칸으로 얼려 둔 Core 보존 표 두 행이다(`CharacterMenuPacketFixtures.cs:72`, `CharacterLoadResultPacketTests.cs:73`). 메인이 귀속에 동의했다(`msg_8dd18145aa72`). impl-03(신규 Sol max)이 그 두 행과 v23 주석만 고친다. 재시험 창은 Content U3 창(00:12Z~00:55Z) 뒤 메인이 연다.
  - Content 소유 `DungeonClearTestWorld.ViewOf`(칸 0~7만 셈, 지금 실패 아님)는 Content 리드에 물었다(`msg_e6d433cf9c2c`). Content가 동의했다(`msg_97c01628f0e1`). 범위는 slots 배열만이고, PR 본문에 「Content 소유 시험 1파일 수정(Content 리드 동의)」을 적는다.
  - impl-03 진행 중: 계약 `impl-03/contract-v1.md`(sha256 `08a49cbf…bcbc`), pane `term_6902d00c-6eaa-4aa4-bd4d-7acb421217aa`, Task `task_a94af9d802c1`, Dispatch `ctx_c9adeebf6a5a`, 00:14:03Z split.
  - **첫 발생 기록(기동 방식):** worker-start(00:15:13Z)가 `turn_start_unobserved`였다. 입력창 draft는 literal 537자와 붙여넣기 조각 셋(1036·2255·9986)으로 나뉘어 있었다. 글자 합 13,814가 예상 약 13,818과 4자 차이였는데, 리드가 텍스트 없는 Enter 1회로 제출했다.
    - 메인 지적(`msg_2126f554f738`): ±2자 조건 밖일 수 있었다.
    - 사후 대조: rollout `~/.codex/sessions/2026/11/01/rollout-2026-11-01T09-14-09-01a19474-dbcd-7363-b82d-d0d464bfb5e0.jsonl` 12행의 사용자 입력(14,224자, 00:16:05Z)이 계약 전문(줄 끝 개행 제외 9,030자)을 offset 5194부터 끝까지 그대로 담는다. 대조 결과는 일치이고, 원시는 `impl-03/rollout-contract-compare-v1.json`이다.
    - 다음 기동부터는 계약을 짧은 포인터(경로+SHA256)로 넘긴다.
  - impl-03 끝(worker_done `msg_d93dec1a2f8c`, `impl-03/report.md`). 재시험 결과는 `Failed: 0, Passed: 2552, Skipped: 28`이다(impl-03 `raw/test.out` 260행, 재시험 창 00:31Z~00:55Z `msg_0d670d1d6797`). 창 반납은 `msg_2884ae7621b2`다. 시험 3파일은 로컬 커밋 `53a4724c`에 담았다.
  - impl-04 진행 중: Unity EditMode 전체 1회(창 00:41Z~01:41Z `msg_d69a1c55932c`, cold import). 계약 `impl-04/contract-v1.md`(sha256 `806c3e5a…e774`)를 짧은 포인터 `impl-04/spec-pointer-v1.txt`(740B)로 넘겼다. worker-start는 ready·observed였다. pane `term_32c48fce-9e48-4f6b-815e-9fd6520cf215`, Task `task_78fbeb45082d`, Dispatch `ctx_4c2a657ae64d`.
  - impl-04 실행 결과(00:44:30Z~00:54:57Z, 리드가 `impl-04/raw/editmode/results.xml`을 직접 읽음): `total=1744 passed=1741 failed=3 skipped=0`, Unity exit 2, unity.log `error CS` 0줄.
    - 이번 변경의 Core 시험은 모두 통과했다. `Snapshot204Bytes_AppliesAll24SlotsOnTheMainDrain` 1, `Legacy76BytesOrMismatchedHeader` 2, `InvalidCountInAppendedSlot` 16, `InventoryProtocolCompatibilityTests` 9, 길드 호환 9건이다.
    - 실패 3건은 Art 소유 화면 시험이다. 지금 main의 목록형 창이 칸 24개를 늘어놓아 깨졌다. `InventoryWindowPlacementTests.CurrencyAndUseResult_TakeSeparateLines_BelowTheEightSlots`와 `UiWindowCompositionRenderTests.Inventory_OnCamera_DrawsApprovedPiecesPerSpecAndKeepsTextReadableAtMaxLegalState`의 (1920,1080)·(1280,720)이다.
    - drift: Unity가 `ProjectSettings.asset`의 `SENTIS_ANALYTICS_ENABLED;`를 지웠다. `impl-04/raw/drift.patch`로 남기고 그 파일만 복원했다. Unity 기본 `AppData/LocalLow/DefaultCompany/03_Client/TestResults.xml`은 자동 부산물이라 지우지 않았다.
    - 창 끝은 `msg_0cc2861a5cfa`, 리드의 0 확인은 `lead-unity-window-end-check-01.txt`다.
    - impl-04 끝(worker_done `msg_5dba07c81615`, `impl-04/report.md` sha256 `b0b0dd0a…e203`). worker-release는 `retained`(external_terminal)였고, 01:07Z `terminal close`(ptyKilled true) 뒤 terminal list에서 사라졌다.
  - Art 귀속: Art 리드에 알렸다(`msg_49fdbff9fdea`). Art 리드 답(`msg_ffcbf9121d67`)은 세 시험을 Art 소유로 받아 격자 PR이 다루게 R-8 인계에 넣는다는 것이다. 격자 goal은 R-8 뒤 새 Art 리드가 연다. 줄 수는 MaxSlots÷4로 상수에서 읽어, 같은 시험이 main(8칸)에서 2줄, Core PR 뒤 6줄로 통과하는 방향이다. 확정은 새 Art 리드의 RED 계약에서 하며, 새 리드가 정하면 Core 리드에게 먼저 알린다.
    - 새 Art 리드(`msg_d509ea07114e`, 2026-11-01T03:41:16Z): 주소 `run:run_6687e0512b2c`(handle `term_db29056f-784c-4528-ac6d-766c32a98d44`). 격자 branch는 `feat/art-inventory-icon-grid-20261101`(기준 `ffe7f983`)이고 범위 초안 단계다. 24칸 계약은 그대로 이어받는다. Art PR은 `InventorySnapshotHandler`·`InventorySnapshotState`를 쓰지 않고, 병합은 Art PR이 먼저이며, I24-A01~03은 격자 PR이 MaxSlots 계산으로 고친다. 클라 상태의 공개 모양을 바꿔야 하면 Core가 이 주소로 먼저 알린다.
  - verify-01(1차 독립 검증) 진행 중: 계약 `verify-01/contract-v1.md`(sha256 `3dea3991…9cee`, 포인터 기동), 신규 `[Core 검증자]` GPT-6-Astra xhigh, pane `term_bb1826a4-b541-4bec-bc34-2dec4139c65e`, Task `task_97438ae0b201`, Dispatch `ctx_c6f33ba9e8e1`. 범위는 메인 `msg_9cbd433df89c`대로다(아래 「적용 중인 메인 결정」).
    - R 끝(「V 준비됨」 `msg_af074258bc4e`). 새 시험은 서버 `02_Server/GameServer.Tests/Items/InventorySnapshotBoundaryTests.cs`(17건)와 클라 `03_Client/Assets/Tests/EditMode/InventorySnapshotBoundaryTests.cs`(11건)이다. 검증자 소유이고 untracked다.
    - 창: V1 WSL 01:16:30Z~01:40:00Z(메인 `msg_4f3f943ff5ba`). V2 Unity는 Content 창 B 뒤다(메인 `msg_b7412bb67113`).
    - 관측 I24-V01: CURRENT의 Core 줄이 아직 10-24 goal을 가리킨다. 만들 것 6의 리드 몫이다. 메인 결정 A(`msg_ca4c45fa6bd1`, 사용자 보고 대상)로 verify-01에서 떼어 냈다. 리드가 PR 준비 때 이 goal로 고치고, 앞 goal(10-31 DB 영속 시연)이 리허설 대기라는 상태도 같은 줄이나 그 goal 링크로 남긴다. 격자 PR 뒤 2차 재검증이 이 줄을 함께 본다.
    - V1 끝(01:16:30Z~01:23:37Z, 창 끝 `msg_082e75193912`): 서버 시험 `Failed: 0, Passed: 2569, Skipped: 28, Total: 2597`, 봇 `PASS=1 FAIL=0`(snapshot len=204·칸 24), 재빌드 Shared.dll과 재생성 GenPackets.cs가 커밋본과 같다. 리드 0 확인은 `verify-01/lead-v1-window-end-check-01.txt`다.
    - V2 끝(Unity PID 21964, 01:28:01Z~01:32:24Z, 창 끝 `msg_57e96dda7a9a`): `total=1755 passed=1752 failed=3`, 새 클라 11건 통과, 실패 3은 impl-04와 같은 Art 시험이다. drift(`SENTIS_ANALYTICS_ENABLED;`)는 patch로 남기고 복원했다. 리드 0 확인은 `verify-01/lead-v2-window-end-check-01.txt`다.
    - **verify-01 통과(계약 범위 한정)**: worker_done `msg_ba2a9160f95d`, 판정 `verify-01/verdict.md`(sha256 `a6b24bfd…047f`). 독립 시험 28건, 서버 2569/0/28, 봇 PASS, Core EditMode 81/81, DLL·생성물 재현 일치다. I24-V02는 원문 전체 실사(`verify-01/rules-full-audit.md`)에서 추가 위반이 없어 메인 결정 A로 풀렸다. I24-A01~03(Art 화면 시험 3건)은 격자 PR 뒤 2차 재검증에서 해소를 본다.
    - 리드 원천 표본 대조: `verify-01/lead-sample-check-v1.md`. 새 서버 시험 17건의 실행 근거는 이름별 원시 없이 수 일치(2552→2569)와 rsync 동기화로 받은 간접 근거다.
    - 검증자 pane은 01:4xZ에 닫았다(worker-release `retained`, ptyKilled true). 새 시험 두 파일과 Unity가 만든 `.meta`는 PR에 넣는다(검증자 작성, 리드 커밋).
    - 메인 R-2 대조는 일치다(`msg_0db07390adce`, 01:42:52Z). 메인이 verdict.md·lead-sample-check-v1.md의 sha256, results.xml 1755/1752/3과 실패 말단 셋, v1-test.out 끝줄 2569/0/28/2597을 직접 셌다.
    - 메인 요청(차단 아님) 원문: 「새 서버 시험 17건의 이름별 원시가 없는 한계는 격자 PR 뒤 좁은 재검증 계약에 넣어라. trx 로그나 이름 목록으로 17건이 실제로 돈 것을 직접 남기게 해라.」 2차 재검증 계약은 서버 시험을 trx 로그나 이름별 결과로 남기게 하고, 검증자가 `InventorySnapshotBoundaryTests` 17건의 이름과 결과를 직접 세어 판정하게 한다.
  - **첫 발생 기록(계약의 규칙 원문):** I24-V02. 리드가 낸 impl-01~04 계약의 CODE_CONVENTION 관련 절이 원문 전체가 아니라 발췌였다. 「상태와 책임」은 13항목 중 4, 「C# 공백 서식」은 첫 문단, 「역할별 적용」은 6문단 중 2였다(검증자 `msg_17d5d2e595dc`, `verify-01/raw/r-contract-text.out`·`r-contract-sections.out`).
    - 원인: 리드가 손으로 옮기며 이 작업에 닿는 문장만 골랐다. 정본은 절 전체를 요구한다.
    - 처분: 메인 결정 A(`msg_d379bdf205d1`, 사용자 보고 대상). verify-01이 원문 전체로 생략된 부분까지 실제 diff를 판정한다. 위반이 없으면 이 결정으로 차단을 풀고, 위반이 나오면 결함 번호로 신규 Sol max가 고친다.
    - 재발 방지: Core 계약의 규칙 원문은 `verify-01/scripts/build-contract-v1.cjs`처럼 원본 파일에서 스크립트로 잘라 붙인다. 새 반복 규칙은 만들지 않는다(메인 동의).

- **창 8 다녀옴(2026-11-01T01:41Z~03:36Z):** core-active는 앞 goal branch로 가서 창 8(PR268 DB 서버 두 판)을 지원했다. 기록은 앞 goal 「재개 지점」에 있다. 03:36:08Z에 이 branch `6b2adad4`로 돌아왔다(`lead-p2-switch-01.txt`). 돌아온 뒤 작업 트리는 깨끗하고, Plugins Shared.dll은 v23(`8d471e72…c025`), `ProtocolVersion.Current`는 23이다. 전환 전 확인(`lead-p2-switch-check-01.txt`)에서 WSL 서버·7777·Unity·Windows dotnet은 모두 0이었다.
  - WSL 복제본은 지금 앞 goal의 v22 빌드다(앞 goal `build-02`). 이 goal의 다음 WSL 실행은 `sync-wsl.sh`가 v23 소스로 다시 동기화·빌드한다. 2차 재검증 계약에 이 사실을 적는다.
- **다음 첫 일:** Art 격자 PR을 기다린다. 격자 PR이 들어가면 main을 합치고(client.md 50행은 격자 PR 쪽을 따르고 38행만 남김, 메인 결정 B-2 A), CURRENT Core 줄을 고친 뒤 2차 좁은 재검증(새 Astra 세션: PlayMode·실제 접속·Art 시험 3건·병합 diff·CURRENT 줄·새 서버 시험 17건 이름별 원시)을 연다. 그 뒤 PR·준비 보고다.
  - WSL 실행은 `impl-01/lead-reply-sdk-v1.txt`(계약 2단계 보충 v1.1) 방법을 쓴다. Windows dotnet은 쓰지 않는다. global.json의 SDK 10.0.301이 Windows에 없다.
- **멈춘 지점의 파일 상태(2026-10-31T18:4xZ, 로컬 WIP 커밋에 담음):**
  - 1단계 소스 31파일과 v23 `GenPackets.cs`(WSL 생성, sha256 `62a2ad60353654a0ab652aca2e62717438e4d8d1ee024087e4110dd656505aa9`, CRLF 0)가 바뀌어 있다.
  - Windows Plugins `Shared.dll`은 아직 v22 그대로다(sha256 `b8b2828e27a1a1667a2b5f703a2c7a97822f70fc47ef974c9f3694dcb5a4ecd3`). `.meta`도 그대로다(`7c6ab891ee57e1a71e200cbea3c27cee36de97404e1d6303711daf59b3be682b`). 그래서 지금 core-active로 Unity를 열면 소스와 DLL 판이 어긋난다.
  - 2단계 실행: 메모리 확인 통과(18:26:58Z) → Windows 생성기 loader 실패 1회(18:26:59Z 종료, 생성물 변경 0) → WSL sync·생성기·GenPackets 복사(36번 밖 차이 0, 칸 필드 48) → `sync-wsl.sh build` exit 0(wrapper 종료 18:36:47.8104288Z) → 정리 지시로 멈춤.
  - 남은 프로세스 0: 구현자 `impl-01/raw/process-inspect.out`(18:38:20Z), 리드 `lead-wrapup-process-check-01.txt`(18:39:37Z, Windows dotnet·Unity·Player 0, WSL dotnet·서버·봇 0, 7777 listen 0).
- **열린 결정:** 메인·사용자 결정 대기는 없다. 적용 중인 결정은 아래 「적용 중인 사용자 결정」과 「적용 중인 메인 결정」이다(97 A `msg_3ce270803253`, 92 B·93 A `msg_e8defeeffd1f`, 범위 승인 `msg_3dfd5496b065`, 99 C 정리 `msg_6547cf711563`).
- impl-01 기록: 계약 `impl-01/contract-v1.md`, 보충 `impl-01/lead-reply-sdk-v1.txt`(리드 답 `msg_b5e3dcc067a6`), 정리 지시 `impl-01/lead-stop-v1.txt`(`msg_27fc8d686ef9`), 1단계 요약 `impl-01/stage1-summary.md`, 원시 출력 `impl-01/raw/`.
  - 부분 보고: worker_done `msg_193a4c831f08`(18:44:49Z, outcome `failed`는 사용자 정리에 따른 부분 완료), `impl-01/report.md`(sha256 `deb3fe2ce6dadabd313e127def0e6fc36e58c5133a225833440b9e2226efa3be`). 리드의 원천 대조는 다음 세션 첫 일에 함께 한다.
  - 다음 세션 확인거리: `git diff --check`가 GenPackets.cs에서 trailing whitespace 64건을 낸다. 리드 grep 기준 HEAD 393줄 → 작업본 457줄로, 생성 템플릿의 기존 성질이 새 칸 16개만큼 는 것이다. 수기 파일의 diff --check는 0이다. 처리 방침은 독립 검증 전에 정한다.
  - pane `term_c381da69-8cc5-49d3-bd0d-3f45bed6a73b`, Task `task_d81c553e4f6a`, Dispatch `ctx_bfcfb9ffa0af`. worker-release는 `retained`(external_terminal)였고, 18:45:4xZ `terminal close`(ptyKilled true) 뒤 terminal list에서 사라졌다.
  - 첫 화면은 `GPT-6.1-Sol max · Full Access`, Codex v0.166.0이었다. backend 실제 모델은 unknown이다.
- Content 창 8(PR268 DB 한 판)은 core-active의 v22 서버로 돈다(Core 답 `msg_0c71d92c392b`). 창 8 전에 리드가 할 일은 셋이다.
  - 24칸 작업을 이 branch에 커밋한다(이 정리로 WIP 커밋됨).
  - core-active를 `feat/db-persistence-demo-build-20261031`로 바꾼다.
  - 창 8 첫 단계로 작업자가 `sync-wsl.sh build` 1회를 돈다. 끝나면 이 branch로 돌아온다. 바꾸는 동안 core-active Unity Editor는 없어야 한다.
  - 근거: 02_Server·98_Shared는 f5adcf31·58c8d4cb·8b60f725 사이 차이 0줄이다. profile은 core-active `logs/windows-sql-8256bcde…/`에 있다.
- Art 리드(`run:run_6ae742140ce9`)와 격자 PR 순서를 조율한다. 격자 goal은 Art PR5가 들어가고 R-8 교체가 끝난 뒤 새 Art 리드가 연다. Core PR이 먼저 준비되면 준비 상태로 기다린다(메인 `msg_3dfd5496b065` 2항).
- 앞 goal(`2026-10-31-db-persistence-demo-build`)은 리허설 대기로 멈춰 있다. 그 branch에 로컬 WIP 커밋 `9d35f5cd`가 있고 원격에 올리지 않았다. 이 goal이 끝나면 그 branch로 돌아가 이 PR이 들어간 기준 branch를 합친다.
- Run `run_ee7c33fd5873`, 리드 `[Core 리드 Opus]`, Claude 대화 ID `396a3a4c-061f-46a4-94cb-a8cbce46f7a0`. branch `feat/inventory-24-slots-20261101`(base `58c8d4cb9b8a34d66389d5573797e07564c6fa7a`, upstream 없음, 원격에 올리지 않음). 근거 폴더는 `.backups/verification/2026-11-01-inventory-24-slots/`다.

## 적용 중인 사용자 결정

### 2026-11-01 결정 100 A·101 A·102 A — 창 높이, Gardener, 선행 시험 작성자

메인 `msg_cf4fa335afea`(2026-10-31T23:55:02Z)가 전달한 사용자 원문이다. 메인 pane에서 Enter로 제출됐다.

> 대시보드 결정 응답: 1) 100 - 인벤토리 24칸(4열×6줄) 창 높이를 키울지(칸 81px), 지금 높이 720px를 지킬지 → A 창을 키운다(칸 81px) · 2) 101 - 목표 종료 정리 점검 세션(Gardener)을 앞으로도 Codex Sol max로 열지, 정본대로 Claude Opus로 돌릴지 → A Codex Sol max 유지 · 3) 102 - 「작업자를 Codex로」에 선행 시험 작성자도 넣을지(지금 해석), 구현자만으로 좁힐지 → A 시험 작성자 포함(대신 Astra로)

- 100 A는 Art 격자 창의 크기다. Core는 화면을 고치지 않는다.
- 메인 해석 101 A: 목표 종료 Gardener는 신규 `codex --model gpt-6.1-sol -c model_reasoning_effort=max --disable fast_mode`로 연다.
- 메인 해석 102 A: 선행 시험 작성자는 신규 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh --disable fast_mode`, 태그 `[Core 시험 Astra]`다. 구현자는 신규 Sol max `[Core Sol]` 그대로다. 이미 떠 있는 impl-02는 바꾸지 않는다.

### 2026-11-01 결정 99 C — 오늘은 여기까지 정리

메인 `msg_6547cf711563`(2026-10-31T18:36:00Z)이 전달한 사용자 원문이다. 메인 pane에서 제출됐다.

> 대시보드 결정 응답: 1) 99 - 자동 진행기 DB 서버 두 판(네 손 약 30분)을 오늘 밤에 할지, 일어나서 할지 → C 코멘트 「오늘은 여기까지하고 전부 정리하자」

- 메인 정리 지시: 새 단계·실행·작업자를 시작하지 않는다. 돌던 명령 하나만 끝내고 멈춘다. 남은 프로세스 0을 확인한다. 미커밋 goal 기록은 로컬 커밋까지만 한다. 새 원격 branch는 만들지 않는다. 「정리 끝」 뒤 우편함 대기를 열지 않는다.

### 2026-10-31 결정 97 A — 인벤토리 24칸

메인 `msg_3ce270803253`(2026-10-31T17:52:45Z)이 전달한 사용자 원문이다. 메인 pane에서 Enter로 제출됐다. 이 Core pane 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 1) 97 - 인벤토리를 24칸(4×6)으로 늘릴지, 8칸(4×2)으로 둘지 → A 24칸으로 늘림

- 그 앞 사용자 원문은 「아니면 인벤토리 크기를 더 늘려도 돼, 내 생각에는 메이플스토리 인벤토리 크기 정도면 상관없어」다.
- 97 A 선택지 문장은 「Core가 지금 인벤토리 패킷과 서버 칸 수를 24로 바꾸고 서버·Unity 양쪽을 검증한다. Art 격자는 칸 수를 따라 4×6이 되고 창은 지금 390×720 가까이 된다. 시연 빌드는 Core PR까지 기다린다」다.

### 2026-10-31 결정 92 B·93 A와 작업자 Codex 전환

메인 `msg_e8defeeffd1f`(2026-10-31T16:46:14Z)가 전달한 사용자 원문이다.

> 대시보드 결정 응답: 1) 92 - 인벤토리 4칸 격자 개편을 리셋 뒤에 할지, 지금 녹화 준비와 함께 할지 → B 지금 함께 · 2) 93 - 아이콘만 남은 칸에서 아이템을 어떻게 쓸지(지금은 줄마다 「사용」 버튼) → A 우클릭으로 사용

> 그러면 작업자를 Codex로 바꿔줘, 이번에 사용한도 다시 2배 되었거든

- 격자·우클릭 사용은 Art 몫이다.
- 작업자 전환은 메인 해석이다. 구현자는 신규 `codex --model gpt-6.1-sol -c model_reasoning_effort=max --disable fast_mode`, 태그 `[Core Sol]`이다. 리드는 Claude Opus, 독립 검증자는 신규 GPT-6-Astra xhigh 그대로다.

## 적용 중인 메인 결정

### 2026-11-01 독립 검증의 두 단계

메인 `msg_9cbd433df89c`(2026-11-01T00:58:04Z). 사용자 결정이 아니며, 메인이 사용자에게 보고할 대상이다.

> 독립 검증은 지금 연다. 이유: Art 격자 PR은 PR292 병합 → R-8 → 새 Art 리드 → 구현·캡처·검증 순서라 몇 시간 뒤다. 서버·프로토콜·봇·Core EditMode 쪽 결함은 지금 찾는 편이 녹화 일정에 안전하다.

- 1차(verify-01) 범위: 서버 시험(WSL)·봇 InventorySmoke·Core 소유 EditMode 시험·프로토콜 v23 호환·diff 실사. 실행 창은 검증자의 「V 준비됨」 뒤 메인에 청한다.
- 2차(격자 PR 병합 뒤): main을 합친 head로 새 Astra 세션이 좁게 다시 본다. 범위는 PlayMode·실제 접속·Art 시험 3건 재확인과 병합 diff다.
- 검증자 계약은 짧은 포인터(경로+SHA256)로 넘긴다. 등급은 강이다(프로토콜·패킷 변경).
- Art 실패 3건의 Art 귀속과 Art 리드 직접 알림에 메인 이견이 없다. Core는 고치지 않는다.

### 2026-11-01 B-2 A — client.md 겹침 해결

메인 사본 `msg_18abd6f81746`(2026-11-01T03:50:26Z). 사용자 결정이 아니다. 메인이 새 Art 리드(`run:run_6687e0512b2c`)의 격자 goal 범위를 승인하면서 정했다.

- 겹침: 이 branch(`7eb3edac`)가 `00_Document/domains/client.md`의 38행(`InventorySnapshotHandler` v23·204바이트·24슬롯)과 50행(`InventoryPanelView` 「공유 칸 수(24)의 슬롯」)을 고친다. 격자 PR도 50행을 다시 쓴다. Art 관측 파일 이름은 `overlap-client-md-hunks-1101-01.txt`다.
- 결정: 격자 PR이 50행의 칸 수를 숫자 없이 「공유 칸 수(InventoryLimits.MaxSlots)」로 적는다. Core는 격자 PR이 기준 branch에 들어간 뒤 그것을 받을 때 50행의 Core 변경을 버리고 38행만 남긴다.
- 이 해결은 병합 관문의 「main 병합·충돌 해결」 재실사 대상이다. 2차 재검증 계약에 넣는다.
- 병합 순서는 그대로 Art 격자 PR → Core 24칸 PR이다.

### 2026-10-31 범위 승인

메인 `msg_3dfd5496b065`(2026-10-31T18:04:08Z). 사용자 결정이 아니다. 범위 초안 v1(sha256 `8dba8548c26a4788b1ed5b9671fcf3bad9b3223f58abb0cf5d1a1c96253f1a95`)을 승인했다.

- 패킷 모양: 36번 끝에 slot8~23을 붙이고 ProtocolVersion을 23으로 올린다. 근거는 protocol.md 9줄 「패킷 ID를 재사용하거나 정의 순서를 바꾸지 않는다. 필드 끝 추가도 기존 읽기 코드에 영향을 준다. 버전 일치로 연결을 제한하는 현재 핸드셰이크와 새 동작을 함께 검증한다」다. 같은 패킷 끝 추가는 ID 재사용이 아니다. 메인 `msg_3ce270803253` 2항의 「새 패킷 ID」는 메인이 바로잡았다.
- 클라 받는 쪽(`InventorySnapshotHandler`, Shared.dll 재빌드·복사, 클라 프로토콜 호환 시험)은 Core가 맡는다. 화면 격자는 Art다. 병합 순서는 Art 격자 PR 먼저, Core 24칸 PR 다음이다.
- goal 배치: 이 새 goal을 같은 리드가 먼저 끝내고 앞 goal 리허설로 돌아간다. R-8 교체는 두 goal이 다 닫힌 뒤 한 번이다. WIP 커밋은 push하지 않는다.
- 실행 창은 Content 창 6 뒤 메인이 순서대로 배정한다. WSL 단계도 창 6과 겹치지 않는다. 구현자 코드 작성은 창 없이 지금 시작해도 된다.
- 76바이트를 직접 만드는 클라 시험의 전수 목록은 계약 전에 받는다. Content 소유 시험이 그 안에 있으면 고치기 전에 Content 리드와 메인에 먼저 알린다.

## 요구사항 원천

- 사용자 97 A와 그 앞 원문(위).
- 범위 초안 v1: `.backups/verification/2026-11-01-inventory-24-slots/scope-draft-v1.md`.
- AGENTS 「공학 조건」: 패킷 ID 재사용 금지, PDL·생성 코드·버전·양쪽 직렬화, 공유 DLL은 서버와 Unity 사용처 양쪽 검증, 빌드 전 생성물·DLL 복사 부작용 확인, `.meta`·GUID 보존.

## 범위

### 만들 것

1. PDL 36번 `S_InventorySnapshot` 끝에 slot8~23의 ItemId·Count 32필드를 붙이고, 주석을 24칸으로 바꾼다. GenPackets.cs를 PacketGenerator로 재생성하고 줄 끝을 LF로 맞춘다.
2. `ProtocolVersion.Current` 23과 이력 줄, `InventoryLimits.MaxSlots` 24와 주석을 바꾼다.
3. 서버 `InventoryPackets.Snapshot`이 24칸을 싣는다. `InventoryState`·`InventoryTransitions`의 판정 규칙은 바꾸지 않는다.
4. 클라 `InventorySnapshotHandler`가 204바이트를 받아 24칸을 반영하고 다른 길이는 버린다. Shared.dll을 WSL 고정 SDK 10.0.301로 다시 빌드해 `03_Client/Assets/Plugins/Shared/`에 복사한다(Windows에는 그 SDK가 없다, 구현자 ask `msg_47941767664c`).
5. 길이·칸 수를 고정한 서버·클라·봇 시험과 봇 소비자를 24칸으로 맞추고, 칸 8~23 오프셋 시험과 76바이트 거부 시험을 더한다.
6. `00_Document/domains/protocol.md`의 36번 줄과 v23 절, 다른 현재 문서의 8칸 표현을 고친다. CURRENT의 Core 줄을 이 goal로 바꾼다.

### 건드릴 곳

- 공유·생성: `99_Tools/PacketGenerator/PDL.xml`, `98_Shared/Protocol/Generated/GenPackets.cs`, `98_Shared/Protocol/ProtocolVersion.cs`, `98_Shared/GameData/Items/InventoryLimits.cs`, `03_Client/Assets/Plugins/Shared/Shared.dll`(`.meta` 보존).
- 서버: `02_Server/GameServer/Items/InventoryPackets.cs`, `02_Server/GameServer.Tests/**`의 길이·칸 수·버전 고정 시험.
- 봇: `99_Tools/headless-bot/Scenarios/Items/InventorySmoke.cs`, `Scenarios/Guild/GuildGrowthProbe.cs` 등 36번 소비자.
- 클라: `03_Client/Assets/Scripts/Network/Handlers/Inventory/InventorySnapshotHandler.cs`, EditMode 시험 `InventoryClientTestSupport.cs`·`InventoryProtocolCompatibilityTests.cs`·`InventoryClientContractTests.cs`.
- 문서: `00_Document/domains/protocol.md` 등 영역 문서, `00_Document/operations/CURRENT.md`의 Core 줄, 이 goal.

### 하지 않을 것

- DB 구조·인벤토리 저장. migration 001~005에 인벤토리 칸 테이블이 없고 서버 Persistence에 저장 경로가 없다.
- 화면 격자·우클릭 사용·마우스 올림 상세 표시와 그 화면 시험(`InventoryPanelView`, `InventoryPanelViewAppearanceTests`, `InventoryWindowPlacementTests`, `UiWindow*`, `BrightUiKitImportTests` 등). Art 몫이다.
- 새 아이템, MaxStack·MaxCurrency, 보상표, 39·44·47 순서.
- 캐릭터 칸 수(사용자 85 A로 3칸 유지).
- 생성기 자체 개선(BACKLOG `packet-generator-line-endings`). 이번엔 생성 뒤 LF로 맞추기만 한다.
- `.meta`·GUID·씬·프리팹 값.

### 76바이트·8칸을 고정한 시험의 목록(리드 grep, 2026-10-31T18:1xZ)

메인 지시 5에 따른 계약 전 목록이다. 구현자가 같은 범위를 독립으로 다시 열거해 보고한다.

| 영역 | 파일 | 고정한 것 |
|---|---|---|
| 클라 EditMode | `InventoryClientTestSupport.cs` | `InventoryWire.SnapshotLength = 76`, `SlotCount = 8`, 손으로 쓴 바이트 인코더 |
| 클라 EditMode | `InventoryProtocolCompatibilityTests.cs` | `new byte[76]`, 머리 `0x4C,0x00,0x24,0x00`, `Assert.AreEqual(8, InventoryLimits.MaxSlots)` |
| 클라 EditMode | `InventoryClientContractTests.cs` | `InventoryWire.SnapshotLength`를 쓰는 수신 버퍼 시험, 사례 이름 「header size 80 on 76 bytes」 |
| 서버 | `Guild/GuildWire.cs` | `InventorySnapshotLength = 76`, `InventorySlotCount = 8` |
| 서버 | `Items/InventoryLifecycleRaceTests.cs`, `InventoryMigrationLifetimeTests.cs`, `InventoryWireContractTests.cs` | `InventorySnapshotLength = 76`, `SlotCount = 8` |
| 서버 | `Items/InventoryProtocolContractTests.cs` | `new byte[76]`, 머리 `0x4C,0x00,0x24,0x00` |
| 서버 | `Items/InventoryRegistrationOwnershipTests.cs` | `const int slotCount = 8` |
| 서버 | `Network/CharacterMenuPacketContractTests.cs` | `ProtocolVersion.Current must be22` |
| 봇 | `Scenarios/Items/InventorySmoke.cs` | `SnapshotLength = 76`, `SlotCount = 8` |

- 클라 PlayMode 시험에는 인벤토리 76바이트를 직접 만드는 곳이 없다. 19파일 모두 생성 `Write()`를 쓰거나 `MaxSlots`를 따른다.
- 클라 EditMode의 `GuildAdmissionPacketCompatibilityTests`·`GuildClientTestTools`·`GuildProtocolCompatibilityTests`, PlayMode `GuildUiConnectionTestTools`의 76은 길드 snapshot(44번)의 길이라 이번과 무관하다.
- PlayMode `InventorySceneLifecycleTests.cs:263`은 줄 수를 `MaxSlots`와 비교한다. 문구만 「eight」이고 값은 상수를 따른다. Art 격자가 칸 24개를 만들면 통과한다.
- Content 소유 시험 중 고칠 것은 이 grep에서 나오지 않았다.

### 관찰 가능한 완료조건

1. 서버가 보내는 36번이 204바이트이고 칸 0~23이 서버 상태와 같다. WSL `sync-wsl.sh test`의 서버 시험이 모두 통과한다. 고친 기존 시험은 실패 분류 표로 남긴다.
2. 클라 handler가 204바이트를 받아 24칸을 반영하고, 76바이트와 다른 길이는 상태를 바꾸지 않는다. Unity EditMode 시험과 인벤토리·길드·보상 관련 PlayMode 시험이 메인이 배정한 Unity 창에서 통과한다.
3. v22 클라가 v23 서버에 붙으면 handshake에서 거부된다.
4. 실제 경로 1회(강 등급):
   - WSL 서버 + headless 봇 `InventorySmoke` 1회.
   - Art 격자가 들어간 main 위에서 Unity 클라가 v23 서버에 붙어 4×6 격자를 보인다.
5. GenPackets.cs의 줄 끝이 LF이고, 커밋한 소스로 WSL 고정 SDK 10.0.301에서 다시 빌드한 Shared.dll과 커밋한 Plugins DLL의 bytes가 같다.
6. 신규 `[Core 검증자]` Astra xhigh의 강 등급 독립 검증이 통과한다.

## 설계 선택과 파트 계약

- 같은 ID 끝 추가: 기존 필드 오프셋은 그대로다. 새 길이는 4 + 4 + 4 + 24×8 = 204바이트다. handshake가 버전 완전 일치라 76과 204가 섞이지 않는다.
- 판정은 바뀌지 않는다. 정의 아이템이 Material·CoinPouch 둘이고 `InventoryState`가 같은 ItemId 두 칸을 막아 점유 칸은 최대 2다. 지금 `InventoryFull`은 묶음 상한 99에서 난다.
- Art와의 계약: 격자는 열 4 고정, 줄 = `InventoryLimits.MaxSlots ÷ 4`로 계산한다. Art 격자 PR이 먼저 병합된다.
- Art 동의(`msg_bacd33a804f4`, Art 격자 범위 v4 sha256 `5dd9cce3…cf2a`):
  - 열 4 고정, 줄은 MaxSlots ÷ 4(올림)이고 창 높이도 줄 수에서 계산한다. 6줄이면 창 높이는 790~810이다.
  - Art 격자 PR이 Art 소유 화면 코드·시험의 8칸 고정을 모두 MaxSlots 계산으로 바꾼다.
  - `InventorySnapshotHandler`·`InventorySnapshotState`는 Art가 쓰지 않는다.
  - Core는 클라 상태의 공개 모양을 바꾸지 않는다(`msg_6d95ee3a3f84`).
  - 6줄 창은 Core PR의 Unity 확인에서 처음 생긴다. 거기서 깨지는 시험은 고치기 전에 귀속을 나눈다. Art 소유는 Art 리드에게, Content 소유는 Content 리드와 메인에게 먼저 알린다.
  - `InventoryServerIntegrationTests`의 주인은 깨질 때 Core·Art·메인이 정한다.
- Content 동의(`msg_6e1243c63eb4`):
  - Content가 고칠 파일은 없다. `InventoryClientTestSupport`·`InventoryClientContractTests`는 Content 출처지만 Content가 쓰지 않으니 Core PR에서 고친다.
  - 병합 뒤에는 그 전에 만든 클라 빌드가 새 서버에 못 붙는다. 녹화 빌드는 병합 뒤 main으로 다시 만든다.
  - PR268 DB 한 판(창 8)은 Core 병합 전 v22로 돈다. 1판은 새 계정 생성, 같은 창 재시작 뒤 2판은 같은 계정 선택이며, 앞 goal의 Core 확인을 함께 잡는다(Core 동의 `msg_a6e92219cbe1`).

## PR 경계와 검증

- Core PR 하나(제품·시험·DLL·문서·이 goal·CURRENT Core 줄). 앞 goal의 문서 PR과 섞지 않는다.
- 검증 등급 **강**. 프로토콜·공유 DLL·서버 경제에 닿는다. 줄 수 어림은 수기 제품 약 60줄, 생성물 약 100줄, 시험 약 10~15파일이다(어림, 미측정).
- 구현자 신규 `[Core Sol]`, 독립 검증자 신규 `[Core 검증자]` Astra xhigh. R-7 비해당.

## 진척 단계

- [x] 범위 초안(v1)
- [x] 범위 승인(메인 `msg_3dfd5496b065`)
- [x] 구현 1단계(소스, 실행 없음)
- [x] 구현 2단계(생성기·DLL·WSL 빌드·서버 시험·봇, 메인 배정 창) — impl-02·impl-03
- [ ] Unity 시험과 실제 접속 — EditMode 끝(impl-04), PlayMode·실제 접속은 Art 격자 뒤
- [x] 1차 독립 검증(verify-01) — 계약 범위 통과
- [ ] 2차 좁은 재검증(격자 PR 병합 뒤)
- [ ] PR·병합 준비 보고
- [ ] 결과 기록과 앞 goal 복귀
