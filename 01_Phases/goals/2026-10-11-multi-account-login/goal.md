# 다중 계정 로그인 — 접속 때 계정 이름으로 세션에 계정 붙이기

## 재개 지점

World의 두 번째 goal이다. 접속할 때 계정 이름을 받아 세션에 계정을 붙이고, 같은 계정이 동시에 둘 들어오지 못하게 한다. 사용자가 범위 초안 v1을 그대로 승인했다(아래 「적용 중인 사용자 결정」). 기준·상태·결과는 이 파일에 모으고 [CURRENT](../../../00_Document/operations/CURRENT.md)는 이 목표를 가리킨다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/world-active`.
- branch: `feat/multi-account-login-20261011`(base main `6782cd674f03d33b989943b409d68883d0fb5b21`, PR228 병합 뒤). Orca가 만든 로컬 branch `bass131/world-active`는 원격에 올리지 않는다.
- 근거 폴더 E: `.backups/verification/2026-10-11-multi-account-login/`(Git 제외). 진입·범위 기록은 E/`lead-entry/`에 있다. 승인 범위는 [scope-draft-v1.md](../../../.backups/verification/2026-10-11-multi-account-login/lead-entry/scope-draft-v1.md)(SHA256 `1d73a1be…8540`), 조사 메모는 [research-notes.md](../../../.backups/verification/2026-10-11-multi-account-login/lead-entry/research-notes.md)(SHA256 `7cae47d1…ad08`)다. 리드 맥락 메모는 E/`lead-entry/context-memo.md`와 E/`goal-write-context.md`다.
- 리드: 신규 `claude-opus-5-5` xhigh(화면 「Opus 5.5 ⚡xhigh」, backend unknown), 태그 `[World 리드 Opus]`, handle `term_940c23cb-4746-4ae6-bcc7-d88ed59ec926`(2026-10-13 재진입 `msg_c1b1e75ea27e` 뒤 `--resume`, 이전 `term_328d1a7f…`·`term_fddebc03…`), Run `run_a927bbaff1f5`(회신 주소 `run:run_a927bbaff1f5`). 메인 주소는 메인 term handle이다. 작업자 태그는 `[World Sol]`(Astra 구현자는 `[World 구현 Astra]`), 검증자는 `[World 검증자]`다.
- **현재 위치**(2026-10-13): RF-01 수정의 약 등급 재검증이 통과했다(아래 「RF-01 재검증」). 독립 검증 단계가 끝났다. 열린 작업자는 없다.
- **다음 행동**: 실화면을 메인에 다시 요청한다. 요청서 E/`screen-request.txt`, harness E/`screen-harness/hold_account.py`(아직 실행 안 함).
  - 시작 직전에 Content·Core·Art에 7777·Unity 사용을 묻는다. 큰 실행 조건은 아래 「실행 자원과 조율」을 따른다.
  - 다른 리드에 알릴 때 서식 검사도 WSL dotnet을 쓴다고 적는다(아래 「RF-01 재검증」의 조율 정정).
- **다시 열 때 필요한 것**: 이 리드의 Claude 세션 ID `1531e871-7aaa-4331-a1db-d831038f8f4a`, Run `run_a927bbaff1f5`. branch는 이 기록 commit까지 원격에 올렸다. world-active에 사용자 미커밋 파일은 없다. 사용자·메인 결정 대기 항목은 없다.
- **남은 순서**(2026-10-13 기준): 실화면 확인 → 제품 PR·사용자 병합 승인 → Gardener → 종료 기록 PR → 종료 점검 → R-8.
- **다음 goal 메모**: 사용자 승인 「10-28 시연 계획과 파트 배정 → A」(아래 「적용 중인 사용자 결정」, 메인 전달 `msg_5ed276291957`)로 다음 goal은 「서버 후속 묶음」 대신 **길드 서버**다. 범위 후보는 기본 길드·길드원, 길드 금고와 던전 클리어 적립, 거점 레벨 올리기, 대장간 무기 강화와 공격력 반영, 패킷과 프로토콜 판(41·42·19 다음 번호)이다. R-8 뒤 새 리드가 범위 초안을 쓰고 메인이 사용자 승인을 받는다. 지금 리드는 준비하지 않는다. 앞선 결정(`msg_6974802727a2`, 서버 후속 묶음)은 이 승인으로 바뀌었다.
- 다음 goal은 자동으로 시작하지 않는다.

## 진척 단계

- [x] 범위 승인
- [x] 설계 절 작성
- [x] 선행 시험 작성
- [x] 구현
- [x] 독립 검증(RF-01 재검증 통과)
- [>] 봇·실화면 확인(봇은 검증에서 17/17, 실화면 남음)
- [ ] 제품 PR 병합
- [ ] Gardener 점검
- [ ] 종료 기록 PR 병합

PR 단계 이름은 PR 번호가 생기면 「PR### 병합」으로 바꾼다.

## 이 목표는

지금 서버에서 플레이어는 연결 하나일 뿐이다. 이름도 계정도 없어서, 끊었다 다시 들어오면 서버는 같은 사람인지 모른다. 이 goal은 접속할 때 계정 이름을 받아 세션에 계정을 붙이고, 같은 계정이 동시에 둘 들어오지 못하게 한다. 마감 로드맵 「마을 광장」의 첫 단계다. 다음 단계인 계정별 캐릭터 불러오기(로딩 게이트, Core)와 광장 봇이 이 위에 올라간다. DB는 쓰지 않는다.

### 흐름 전/후

```text
지금
  [클라이언트] 메인 메뉴: 서버 주소 ─▶ 캐릭터 선택(직업, 오프라인) ─▶ Town 씬에서 접속
  [통신]       C_Handshake ─▶ S_HandshakeResult(ok) ─▶ C_CharacterSelect ─▶ S_EnterMap ...
  [서버]       handshake 완료 + 직업 선택 완료 ─▶ 월드 입장 (플레이어 = 연결 하나, 이름 없음)

이 goal 뒤 (★가 바뀌는 곳, 패킷 이름은 설계에서 정함)
  [클라이언트] 메인 메뉴: 서버 주소 + ★계정 이름 ─▶ 캐릭터 선택(그대로) ─▶ Town 씬에서 접속
  [통신]       C_Handshake ─▶ S_HandshakeResult(ok)
               ─▶ ★로그인 요청(계정 이름) ─▶ ★로그인 결과(성공 / 이름 규칙 위반 / 이미 접속 중)
               ─▶ (성공일 때만) C_CharacterSelect ─▶ S_EnterMap ...
  [서버]       handshake 완료 + ★로그인 성공 + 직업 선택 완료 ─▶ 월드 입장
               ★로그인 성공 때 세션에 「계정 이름 + 계정 번호」를 붙이고 접속 중 계정 목록에 올린다
               ★세션이 끝나면 종료 정리 단계에서 그 계정을 목록에서 내린다
               ★거부하면 결과를 보내고 연결을 끊는다. 다시 시도는 새 연결로 한다
```

영속화 설계의 세션 상태(HandshakePending → Selecting → Loading → Resolved → Entering → Active, [D0 설계](../2026-09-29-persistence-design/design.md) 52~56행)에서 로그인은 Selecting 앞에 들어간다. 나중에 Loading이 DB 비동기 단계로 들어와도 로그인 단계를 다시 짜지 않게 설계 절에 순서를 고정한다.

## 범위

### 만들 것

1. 새 패킷 둘: 클라이언트 → 서버 로그인 요청(계정 이름), 서버 → 클라이언트 로그인 결과(성공, 이름 규칙 위반, 이미 접속 중). PDL 맨 아래에 더하고 프로토콜 버전을 하나 올린다. 이름·필드·결과 값은 설계 절과 선행 시험이 고정한다.
2. 서버 로그인 처리기와 세션 단계. 로그인은 handshake 뒤, 직업 선택 전에 연결당 한 번만 받는다. 로그인 성공 전에는 직업 선택과 게임 입력을 처리하지 않는다.
3. 계정 이름 검사. 길이·글자 규칙에 맞지 않거나 길이 머리가 실제 내용과 다르면 예외 없이 거부한다. 규칙은 아래 「확인한 기본값」 1이다.
4. 이름 → 계정 번호 해석기 하나. 같은 이름은 서버를 다시 켜도 늘 같은 비어 있지 않은 번호(GUID)가 된다. 번호는 이 한 곳에서만 정한다. 나중에 Core가 이 해석기를 DB 조회로 바꾼다(Core `msg_775584e3f699` 1).
5. 접속 중 계정 목록. 같은 계정이 이미 접속 중이면 새 로그인을 거부하고 기존 세션은 그대로 둔다. 두 연결이 같은 이름으로 동시에 들어와도 하나만 성공한다. 세션이 끝나면(정상 종료, 갑작스러운 끊김, 로그인 뒤 직업 선택 전 끊김) 종료 정리 경로에서 그 계정을 내린다.
6. 서버 로그 한 줄: 로그인 성공·거부와 계정 이름·결과. 계정 번호와 내부 값은 클라이언트로 보내지 않는다.
7. 클라이언트: 메인 메뉴에 계정 이름 입력칸을 더한다. 접속 뒤 handshake 성공이면 로그인 요청을 보내고, 성공 결과를 받은 뒤에 직업 선택을 보낸다. 거부되면 메인 메뉴로 돌아가 이유를 보인다. 입력한 이름은 다음 실행 때 입력칸에 채운다. 같은 PC의 여러 클라이언트가 서로 덮어쓰지 않게 프로세스 안 값을 우선한다(지금 직업 선택과 같은 방식).
8. 봇: 모든 봇이 handshake 뒤 로그인한다. 여러 봇을 쓰는 시나리오는 봇마다 다른 계정 이름을 쓴다. 새 봇 시나리오 하나로 다중 계정 접속, 같은 계정 이중 로그인 거부, 끊은 뒤 같은 계정 재로그인을 확인한다.
9. 문서: 프로토콜·서버·클라이언트·도구 영역 문서와 기능 지도의 연결 줄, 이 goal 파일, CURRENT의 World 줄.

### 건드릴 곳

- 공유 프로토콜: `99_Tools/PacketGenerator/PDL.xml`, 생성 `98_Shared/Protocol/Generated/GenPackets.cs`, `98_Shared/Protocol/ProtocolVersion.cs`, `03_Client/Assets/Plugins/`의 Shared·ClientNet DLL 사본.
- 서버: `02_Server/GameServer/Sessions/GameSession.cs`(로그인 단계·게이트), `Handlers/Session/`(새 로그인 처리기, `CharacterSelectHandler.cs`), `Handlers/HandlerRegistry.cs`, `Loop/GameWorld.cs`(접속 중 계정 목록과 종료 정리), 새 계정 파일(해석기·목록, 폴더와 이름은 설계에서 정함).
- 서버 시험: `02_Server/GameServer.Tests/`의 해당 파일과 새 시험. 패킷 수·버전을 단정하는 기존 시험 셋(`Items/InventoryProtocolContractTests.cs`, `Maps/BossBehaviorTests.cs`의 버전 시험, 클라이언트 `InventoryProtocolCompatibilityTests.cs`)을 고친다.
- 클라이언트: `03_Client/Assets/Scripts/UI/MainMenuController.cs`와 `03_Client/Assets/Scenes/00.Menu/MainMenu.unity`(입력칸, `.meta`·GUID·다른 직렬화 값 보존), 같은 `Scripts/` 아래 `Network/ClientConnectionLifetime.cs`, `Network/UnityClientSession.cs`(dispatch 표 한 줄과 로그인 송신), `Network/NetworkService.cs`(이름 전달·실패 때 메인 메뉴 복귀), 새 로그인 결과 처리기, 이름 보관 파일. `03_Client/Assets/Tests/`의 EditMode·PlayMode 시험.
- 봇: `99_Tools/headless-bot/Scenarios/ProbeBase.cs`, `Scenarios/Movement/M2BasicMovement.cs`, 여러 봇 시나리오의 이름 지정, 새 시나리오와 `Program.cs`·`99_Tools/run_bot_regression.sh` 등록 줄.
- 문서: `00_Document/domains/protocol.md`·`server.md`·`client.md`·`tooling.md`, `00_Document/FEATURE_MAP.md`, 이 goal 파일, `00_Document/operations/CURRENT.md`의 World 줄.

### 하지 않을 것

- 비밀번호·정식 인증, 계정 가입·삭제 절차.
- DB 저장. 이름 → 계정 번호 대응의 DB 보관은 새 migration과 procedure·결과 계약 변경이라 Core의 별도 goal과 사용자 결정이 필요하다(Core `msg_775584e3f699` 3).
- 저장 슬롯을 계정마다로 넓히는 일. 지금 저장은 slot 1 하나에 고정돼 있다(Core 4). 그래서 이 goal 뒤에도 로그인한 세션의 상태는 저장되지 않는다. 게임 서버는 아직 DB에 연결돼 있지 않다.
- 계정별 캐릭터 불러오기, 저장 직업 사용, 인벤토리·진행·위치 복원. 로딩 게이트와 재접속 복원(Core) 몫이다. 직업은 지금처럼 접속마다 고르고, 재접속은 지금처럼 Town·풀 HP·빈 인벤토리다.
- 기존 접속을 끊고 새 접속을 받는 방식(takeover), 접속 시간 제한, 접속 수 상한.
- 머리 위나 파티 화면의 이름 표시, 광장 봇, 광장 연출.
- 운영툴의 접속 계정 목록·강제 종료(Management 후속, BACKLOG `server-operations-view`).
- 캐릭터 선택 화면 흐름 변경, 서버 주도 캐릭터 선택([ADR-027](../../../00_Document/ADR/harness/ADR-027-client-bootstrap-persistent-services.md) B안), 클라이언트 자동 재접속.
- 마감 로드맵 파일 쓰기.

### 관찰 가능한 완료조건

1. 서로 다른 두 계정 이름으로 동시에 로그인하면 둘 다 월드에 들어가고, 서버의 각 세션에 자기 계정이 붙는다. 서버 시험과 새 봇 시나리오로 확인한다.
2. 이미 접속 중인 계정으로 다시 로그인하면 「이미 접속 중」 결과를 받고 그 연결이 끊긴다. 기존 세션은 그대로 움직이고 패킷을 받는다. 서버 시험과 봇으로 확인한다.
3. 같은 이름의 두 로그인이 동시에 와도 하나만 성공한다. 동시성 시험으로 확인한다.
4. 접속 중인 세션이 정상 종료, 갑작스러운 끊김, 로그인 뒤 직업 선택 전 끊김으로 끝나면, 같은 계정으로 다시 로그인할 수 있다. 서버 시험과 봇(끊은 뒤 재로그인)으로 확인한다.
5. 같은 이름은 해석기를 새로 만들어도(서버 재시작에 해당) 같은 비어 있지 않은 계정 번호가 된다. 다른 이름은 다른 번호가 된다. 서버 시험으로 확인한다.
6. 규칙에 맞지 않는 이름(빈 이름, 너무 긴 이름, 허용 밖 글자, 길이 머리와 실제 내용 불일치)은 예외 없이 「이름 규칙 위반」으로 거부되고 상태가 바뀌지 않는다. 서버 시험으로 확인한다.
7. 로그인 성공 전에 온 직업 선택·게임 입력은 처리되지 않고 월드에 들어가지 않는다. 같은 연결의 두 번째 로그인 요청은 계정을 바꾸지 않는다. 서버 시험으로 확인한다.
8. 클라이언트 메인 메뉴에 계정 이름 입력칸이 보인다. handshake 성공 뒤 로그인 요청을 보내고, 성공 뒤에만 직업 선택을 보낸다. 거부되면 메인 메뉴로 돌아가 이유 글자가 보인다. EditMode·PlayMode 시험과 실화면 한 번으로 확인한다.
9. 새 패킷은 PDL 맨 아래에 붙고 기존 패킷의 번호·이름·필드는 그대로다. 프로토콜 버전이 하나 오르고 옛 버전 클라이언트는 지금처럼 거부된다. 생성 코드는 실제 생성기로 만든다. 서버·봇 빌드와 Unity 컴파일이 통과한다.
10. 기존 행동 시험과 봇 회귀가 통과한다. 로그인 때문에 고친 기존 시험은 실패 전수 분류 표에 이전·새 단정과 요구사항 출처를 남긴다.
11. 게임 틱 안에서 파일·DB·네트워크를 기다리지 않는다. 접속 중 계정 목록을 바꾸는 스레드와 순서를 설계 절에 적고 코드 실사로 확인한다.

### 확인한 기본값

사용자가 초안의 「확인할 기본값」을 코멘트 없이 승인했다(아래 「적용 중인 사용자 결정」).

1. 계정 이름 규칙: 3~16자, 영문 소문자·숫자·밑줄. 대문자는 소문자로 바꿔 같은 계정으로 본다. 한글은 받지 않는다. 화면 표시와 나중 DB 열을 단순하게 두려는 것이다.
2. 같은 계정 이중 접속: 새 접속을 거부하고 기존 세션을 유지한다. 영속화 설계의 사용자 선택(같은 캐릭터 새 중복 접속 거부, [D0 goal](../2026-09-29-persistence-design/goal.md) 12행)을 계정 단위로 그대로 쓴다.
3. 처음 보는 이름: 그 자리에서 새 계정이 된다. 미리 등록한 이름만 받는 방식은 쓰지 않는다. 광장 봇이 계정을 많이 쓰기 때문이다.
4. 직업: 지금처럼 접속마다 고른다. 계정이 직업을 기억하는 것은 로딩 게이트 몫이다.

## PR 경계와 점검

- 제품 PR 하나: 공유 프로토콜·생성 코드·DLL 사본, 서버, 클라이언트, 봇, 문서, goal, CURRENT의 World 줄. 프로토콜을 바꾸면 서버와 클라이언트가 같은 PR에서 함께 바뀌어야 main이 깨지지 않는다.
- 종료 기록 PR 하나: 제품 PR 병합과 로컬 결과 기록 → Gardener 점검 → 결과를 담은 종료 기록 PR → 종료 점검 → 리드 교체(R-8).
- 점검은 goal 종료 점검 하나다. 중간 점검은 두지 않는다.
- 패킷 번호: Content 던전 패킷·창 PR(39·40번, 버전 17→18)이 먼저 병합되면 이 goal은 41·42번·버전 19다. 이 goal이 먼저면 39·40번·버전 18이다. 뒤 PR이 main을 받아 생성 코드를 다시 만들고, 버전 단정 세 곳·버전 이력 주석·DLL 사본을 다시 맞춘다(Content `msg_59c720446c32` 1·4).

## 진행 방식

- 검증 강도: 강. 공유 프로토콜·DLL, 외부 입력 경계(계정 이름), 서버 판정(중복 접속)이다. 제품과 시험 변경도 50줄을 넘을 것으로 본다(추정, 원시 줄 수는 구현 뒤 기록).
- 선행 시험: 신규 `claude-opus-5-5`가 서버 xUnit 시험과 클라이언트 EditMode 시험을 먼저 쓰고 구현 전 실패 원시를 근거 폴더에 둔다(TDD 정본). 화면 요구(입력칸이 보임, 거부 이유 글자)는 PlayMode 시험으로 덮을 수 있는 만큼 덮고, 나머지는 독립 검증의 실화면 한 번으로 대신한다.
- 구현자: `gpt-6-astra` xhigh. 배정 신호 1(외부 입력 해석: 계정 이름), 2(수명: 접속 중 목록과 끊김·동시 로그인), 3(서버↔클라이언트 프로토콜), 4(설계 판단이 남은 새 다파일 기능)에 해당한다.
- 독립 검증자: 신규 `claude-opus-5-5`. 검증자 시범의 「gpt-6-astra 구현의 검증자는 신규 claude-opus-5-5」와 「프로토콜·공유 DLL·보안 경계 변경은 신규 claude-opus-5-5」를 따른다.
- R-7 Fable 설계 검토: 비해당. 4범주 중 실패 수명(끊김 때 계정 내리기)과 오류 분류(로그인 결과)에 닿지만, 시범 자리 셋을 아이템·인벤토리, 인스턴스 맵, Core DB 도구 컨테이너 전환에 다 썼다. 그래서 Content 던전 클리어 PR과 같은 방식으로 리드 설계 절과 선행 시험으로 간다(Content goal의 메인 결정 `msg_cfcaae303755`). 메인이 시범 자리를 더 열면 해당한다.
- goal 위치: 이 파일. branch `feat/multi-account-login-20261011`는 승인 뒤 최신 main `6782cd67`에서 만들었다.
- 진척 단계: 위 「진척 단계」 절. 범위 v1의 안 그대로다.

## 설계

리드가 썼다(기준 main `6782cd67`). 파일·타입·메서드 이름은 기본안이고 선행 시험이 고정한다. 선행 시험이 바꾼 이름은 아래 「선행 시험이 고정한 이름」에 적는다. 범위·완료조건은 바꾸지 않았다.

### 고른 방식과 검토한 다른 방식

| 방식 | 판단 |
|---|---|
| 로그인 판정을 월드 틱의 계정 등록부에서 한다 (고름) | 소켓 스레드는 이름을 검사하고 작업을 큐에 넣기만 한다. 접속 중 목록의 추가·제거는 모두 틱 스레드에서 일어나고, 종료 정리(`GameWorld.DrainSessionCloses`)와 같은 스레드라 잠금이 없다. 인벤토리·파티·퀘스트 등록부와 같은 「큐 + Tick」 모양이다. 대가는 로그인 결과가 최대 한 틱(50ms) 늦는 것이다. 나중에 Core의 DB 조회가 들어와도 결과를 틱에서 적용하는 자리가 이미 있다. |
| 소켓 스레드에서 동시 사전으로 바로 판정 | 고르지 않음. 끊김이 다른 스레드에서 오면 「등록 → 닫힘 재확인 → 내 등록만 지우기」 순서를 지켜야 남는 등록이 없다. 정리는 틱에서, 등록은 소켓에서 일어나 두 스레드의 순서를 시험으로 덮기 어렵다. |
| handshake 패킷에 이름 칸을 더한다 | 고르지 않음. 기존 패킷의 필드를 바꾸고, 버전 거부와 로그인 거부가 한 결과에 섞인다. |
| 계정 번호를 무작위로 발급해 메모리에 둔다 | 고르지 않음. 재시작 때 번호가 바뀌어 나중 DB 행과 맞출 수 없다(Core `msg_775584e3f699` 1). |
| 서버 설정에 등록된 이름만 받는다 | 고르지 않음. 「확인한 기본값」 3(처음 보는 이름은 새 계정). |

### 구성

공유(`98_Shared`, 서버·클라이언트·봇이 함께 쓴다):

- 패킷: PDL 맨 아래에 `C_Login { string accountName }`(클라이언트 → 서버)과 `S_LoginResult { byte result }`(서버 → 클라이언트)를 더한다. 프로토콜 버전을 main보다 하나 올리고 버전 이력 주석에 한 줄을 더한다. 생성 코드는 PacketGenerator로 만든다.
- 결과 값: `Shared.GameData.LoginResult : byte` = `Accepted 0`, `InvalidName 1`, `AlreadyOnline 2`. 위치는 기존 `InventoryResult`와 같은 `98_Shared/GameData/Enums/LoginResult.cs`다.
- 이름 규칙: `Shared.GameData.AccountNameRule`(`98_Shared/GameData/Accounts/AccountNameRule.cs`). `MinLength 3`, `MaxLength 16`, `bool TryNormalize(string? raw, out string normalized)`. ASCII `A-Z`는 소문자로 바꾸고, 바꾼 뒤 모든 글자가 `a-z`·`0-9`·`_`이고 길이가 3~16이면 통과한다. 앞뒤 공백을 자르지 않는다(공백은 허용 밖 글자다). 서버가 권위 판정에 쓰고, 클라이언트는 메뉴 사전 검사에, 봇은 이름 생성 검사에 같은 규칙을 쓴다. 규칙은 공유, 판정은 서버라는 CODE_CONVENTION 「상태와 책임」을 따른다.

서버(`02_Server/GameServer/Accounts/` 새 폴더, 파티·퀘스트·아이템 등록부와 같은 도메인 폴더 관례):

- `AccountIdentity`: `readonly record struct AccountIdentity(string Name, Guid AccountId)`. 세션이 들고 있는 계정이다. 로딩 게이트가 읽을 자리다.
- `AccountIdResolver`: `static Guid Resolve(string normalizedName)`. 계정 번호를 정하는 유일한 곳이다. 지금은 RFC 9562의 이름 기반 v5 UUID(SHA-1)로 계산한다. 이름공간 GUID는 이 파일의 상수 `d05083ec-6067-4d21-9ce9-271da508bbfd`로 고정하고, 이름 바이트는 정규화한 이름의 UTF-8이다. 결과는 RFC의 바이트 순서를 따르므로 Python `uuid.uuid5(UUID("d05083ec-6067-4d21-9ce9-271da508bbfd"), name)`과 같은 문자열이어야 한다(.NET `Guid`의 앞 세 필드 바이트 순서 차이에 주의). 그래서 같은 이름은 재시작 뒤에도 같은 번호이고 빈 GUID가 나오지 않는다. SHA-1은 번호 계산용이며 보안 용도가 아니다. 나중에 Core가 이 메서드 몸통을 DB 조회로 바꾼다. 지금은 쓰는 곳이 하나라 인터페이스를 만들지 않는다(CODE_CONVENTION 「사용처 없는 확장 hook·추상화를 추가하지 않는다」).
- `AccountRegistry`(internal, 월드 틱 소유): `ConcurrentQueue<Action>` 작업 큐와 `Dictionary<string, GameSession>` 접속 중 목록(열쇠는 정규화한 이름). `EnqueueLogin(GameSession session, string normalizedName)`, `Tick()`, `Forget(GameSession session)`. 시험용 조회(`IsOnline(string)` 등)는 선행 시험이 정한다.
- 로그인 처리기 `Handlers/Session/LoginHandler.cs`: `RequiresSelectedClass => false`. `HandlerRegistry`에 한 줄로 등록한다.
- `GameSession`: 로그인 상태(없음 → 요청함 → 성공)와 `AccountIdentity? Account`를 가진다. `TryBeginLogin()`(소켓 스레드, 없음 → 요청함 원자 전환), `CompleteLogin(AccountIdentity)`(틱 스레드, 계정과 상태를 먼저 쓰고 성공 결과를 보냄), `RejectLogin(LoginResult)`(결과를 보내고 연결을 끊음, 기존 `RejectHandshake`와 같은 모양), `IsLoggedIn`. 시험 하위 클래스가 로그인을 우회할 수 있게 `CompleteLogin`은 `protected internal`로 둔다.
- `GameWorld`: `AccountRegistry`를 만들어 들고 `Accounts`로 노출한다. `DrainSessionCloses`에서 `_accounts.Forget(session)`을 부른다. `OnTick`에서는 첫 `DrainSessionCloses` 바로 뒤에 `_accounts.Tick()`을 부른다.

클라이언트(`03_Client/Assets/Scripts/`):

- `Bootstrap/AccountLoadout.cs`: 지금 `ClassLoadout`과 같은 모양이다. 프로세스 안 값 `SessionAccountName`이 1순위이고 PlayerPrefs `AccountName`은 입력칸 채우기용이다.
- `UI/MainMenuController.cs`와 `MainMenu.unity`: 서버 주소 입력칸 **위** 빈 오른쪽 영역에 계정 이름 입력칸(`TMP_InputField`)을 더한다. 처음 설계는 「아래」였지만 실제 서버 칸이 시작 버튼보다 아래에 있어 자리가 없었다. 구현자 질문에 리드가 위로 정했다(`msg_7337a614965c`, anchor x .755~.983·y .26~.32, 서버 칸 크기·글꼴·색 복제). 보임·겹침은 실화면 확인이 본다. 시작 버튼에서 `AccountNameRule.TryNormalize`로 먼저 검사해 틀리면 기존 오류 글자칸에 이유를 쓴다. 시험 접속이 성공하면 정규화한 이름을 `AccountLoadout`에 저장한다. 메뉴가 열릴 때 남아 있는 로그인 거부 안내가 있으면 오류 글자칸에 보이고 지운다.
- `UI/MainMenuNotice.cs`(새, 정적): 씬을 넘어 거부 안내 한 줄을 넘긴다. `Set(string)`, `TryTake(out string)`.
- `Network/ClientConnectionLifetime.cs`: `Connect(endPoint, selectedClass, accountName)`. handshake 성공 때 연결당 한 번 `SendLogin(accountName)`을 보낸다. 로그인 결과가 `Accepted`이면 연결당 한 번 `SendCharacterSelect`를 보낸다. 그 밖이면 거부 결과를 남기고 `LoginRejected` 이벤트를 낸 뒤 연결을 끊는다. `IClientConnectionSession`에 `event Action<LoginResult> LoginResultReceived`와 `void SendLogin(string accountName)`을 더한다.
- `Network/UnityClientSession.cs`: dispatch 표에 `S_LoginResult` 한 줄, `SendLogin`. 새 `Network/Handlers/Session/LoginResultHandler.cs`는 결과 값이 0~2인지 보고 메인 스레드에서 이벤트를 낸다. 범위 밖 값은 로그를 남기고 연결을 끊는다.
- `Network/NetworkService.cs`: `Connect`가 `AccountLoadout`에서 이름을 읽는다. 이름이 없거나 규칙에 맞지 않으면 지금의 잘못된 주소처럼 메인 메뉴로 돌아간다. `LoginRejected`를 받으면 안내를 `MainMenuNotice`에 넣고 메인 메뉴로 돌아간다. 안내 문장은 `InvalidName` → 「계정 이름은 영문 소문자·숫자·밑줄 3~16자로 입력해주세요」, `AlreadyOnline` → 「이미 접속 중인 계정입니다」다.

봇(`99_Tools/headless-bot/`):

- `ProbeBase`: `protected virtual string AccountName`의 기본값은 봇마다 다른 이름(`bot_<실행마다 정하는 4자리 16진수>_<일련번호>`, 16자 이하)이다. handshake 성공 때 `C_Login`을 보내고, `S_LoginResult`를 받아 `LoginResult` 속성과 대기 신호를 세우며, `Accepted`일 때만 `C_CharacterSelect`를 보낸다. `WaitLogin(TimeSpan)`을 더한다.
- `Scenarios/Movement/M2BasicMovement.cs`: 손수 짠 handshake 뒤에 같은 로그인 단계를 넣는다.
- 새 시나리오 `MultiAccountLoginSmoke`(위치는 기존 시나리오 하위 폴더 관례를 따라 구현이 정함): 서로 다른 두 이름의 동시 입장, 같은 이름 세 번째 연결의 `AlreadyOnline`과 끊김, 그동안 첫 연결이 계속 스냅샷을 받음, 첫 연결을 끊은 뒤 같은 이름의 재입장, 규칙 위반 이름의 `InvalidName`. `Program.cs`와 `99_Tools/run_bot_regression.sh`에 등록한다.

### 로그인 흐름과 게이트

1. `C_Handshake` 처리는 지금 그대로다(첫 패킷 강제, 버전 정확 일치).
2. `C_Login`(소켓 스레드, `LoginHandler`):
   - `TryBeginLogin()`이 실패하면(이미 요청했거나 성공함) `[Trust]` 로그를 남기고 버린다. 계정은 바뀌지 않는다(완료조건 7).
   - 패킷을 읽다 길이 머리가 버퍼를 넘으면 예외를 밖으로 내지 않고 `InvalidName`으로 거부한다. `AccountNameRule.TryNormalize`가 실패해도 `InvalidName`으로 거부한다(완료조건 6). 로그에는 원래 이름 대신 길이만 남긴다.
   - 세션에 월드가 없으면 버린다(시험용 `GameSession(null)`).
   - 통과하면 `world.Accounts.EnqueueLogin(session, normalized)`.
3. 로그인 작업(틱 스레드, `AccountRegistry.Tick`):
   - 세션이 닫히는 중이면 아무것도 하지 않는다(등록하지 않음).
   - `AccountIdResolver.Resolve`로 번호를 정한다.
   - 같은 이름이 접속 중이면 `RejectLogin(AlreadyOnline)`. 기존 세션은 건드리지 않는다(완료조건 2).
   - 아니면 목록에 넣고 `CompleteLogin(new AccountIdentity(name, id))`. 상태를 먼저 쓰고 결과를 보낸다. 그래서 클라이언트가 결과를 받고 보낸 `C_CharacterSelect`는 늘 로그인 성공 상태를 본다.
   - 로그 한 줄: `[Login] accepted account=<name>` 또는 `[Login] rejected account=<name> result=AlreadyOnline`.
4. `C_CharacterSelect`(소켓 스레드): `CharacterSelectHandler`가 맨 앞에서 `IsLoggedIn`을 보고, 로그인 전이면 `[Trust]` 로그를 남기고 버린다. 지금의 중복 선택·범위 검사는 그 뒤에 그대로다.
5. 게임 입력: 지금처럼 `RequiresSelectedClass` 게이트로 버린다. 직업 선택이 로그인 뒤에만 되므로 로그인 전 게임 입력도 처리되지 않는다(완료조건 7).

게이트를 직업 선택 한 곳에만 두는 이유: 실제 클라이언트가 직업을 얻는 길은 `CharacterSelectHandler` 하나다. `EnterGameWorldIfReady`와 `OnRecvPacket` 전체에 로그인 게이트를 더하면 직업을 직접 넣고 입장하는 기존 시험 도우미(`CompleteHandshakeAndEnter` → `SetCharacterClass` → `EnterGameWorldIfReady`)가 모두 바뀐다. `EnterGameWorldIfReady()`를 직접 부르는 시험만 28파일 34곳이다(`grep -rn 'EnterGameWorldIfReady()' 02_Server/GameServer.Tests`, main `6782cd67`). 게이트가 지키는 행동은 같고 시험 변경만 커진다. 대신 `C_CharacterSelect` 패킷을 보내 직업 선택 처리기를 거치는 기존 시험 12파일은 로그인 단계를 더해야 하고 실패 전수 분류 표에 넣는다(`grep -ln 'C_CharacterSelect' 02_Server/GameServer.Tests`, main `6782cd67`): `Integration/ServerFixtureLifecycleTests.cs`, `Items/InventoryLifecycleRaceTests.cs`·`InventoryMigrationLifetimeTests.cs`·`InventoryProtocolContractTests.cs`·`InventoryWireContractTests.cs`, `Maps/InstanceMapRetirementTests.cs`·`InstanceMapTestWorld.cs`·`SessionCleanupTests.cs`, `Network/CharacterSelectHandlerTests.cs`·`ServerHostLifecycleTests.cs`·`SessionStateMachineTests.cs`, `Party/QuestNotificationContractTests.cs`. 실제 실패 목록은 선행 시험의 구현 전 실행 원시로 확정한다.

### 스레드와 순서 계약 (완료조건 11)

- 접속 중 목록은 틱 스레드에서만 바뀐다(로그인 작업, 종료 정리의 `Forget`).
- 소켓 스레드는 로그인 상태의 원자 전환, 이름 검사(순수 계산), 큐 넣기만 한다.
- 틱 안에서 파일·DB·네트워크를 기다리지 않는다. 번호 계산은 16바이트 이하의 해시다.
- 끊김과의 순서: `OnDisconnected`가 닫힘 표시를 먼저 쓰고 종료 요청을 큐에 넣는다. 로그인 작업과 종료 정리는 같은 틱 스레드에서 돈다. 그래서 (가) 작업이 닫힘 전에 돌면 등록하고 다음 종료 정리가 내린다. (나) 닫힘 뒤에 돌면 등록하지 않는다. 남는 등록이 없다.
- `Forget`은 목록의 값이 바로 그 세션일 때만 지운다. 늦게 온 정리가 다른 세션의 등록을 지우지 못한다.
- 한 틱 안 순서는 「종료 정리 → 로그인 작업 → 맵 틱 → 종료 정리 → 파티 → 퀘스트 → 인벤토리 → 복사본 정리」다. 같은 틱에 끊긴 계정을 그 틱의 새 로그인이 다시 쓸 수 있다.
- 거부는 결과를 보낸 뒤 연결을 끊는다. 이 「보내고 바로 끊기」는 지금 `RejectHandshake`와 같은 모양이다. 끊는 순간 서버 수신 버퍼에 읽지 않은 데이터가 있으면 운영체제가 연결을 리셋해 결과가 클라이언트에 닿지 않을 수 있다(추론, 미측정). 봇 시나리오가 거부 결과 수신을 단정해 실제로 본다. 닿지 않으면 구현 단계에서 리드에 올린다.

### 영속화 설계와의 대응

- 세션 상태 HandshakePending → Selecting → Loading → Resolved → Entering → Active([D0 설계](../2026-09-29-persistence-design/design.md) 52~56행)에서 로그인은 HandshakePending과 Selecting 사이다. 나중의 Loading은 직업 선택 뒤에 들어오므로 로그인 단계를 다시 짜지 않는다.
- Core가 번호 해석을 DB 조회로 바꿀 때는 로그인 작업이 조회를 맡기고 결과를 틱에서 적용하는 두 단계가 된다. 결과 적용 때 닫힘을 다시 보는 자리는 이미 있다.
- 같은 계정 이중 접속 거부는 D0의 같은 캐릭터 중복 접속 거부(같은 파일 37행)와 같은 방향이다. 기존 세션을 끊고 새로 받는 방식은 쓰지 않는다.

### 시험이 덮을 것과 대체 실측

- 서버 xUnit(선행 시험): 완료조건 1~7과 9의 서버 쪽. 같은 이름 동시 로그인(완료조건 3)은 두 세션이 같은 틱 전에 로그인을 넣은 뒤 한 틱을 돌려 하나만 `Accepted`인지 본다. 계정 번호 기대값은 제품 코드가 아닌 독립 구현(예: WSL Python `uuid.uuid5`)으로 미리 계산해 원시와 함께 둔다. 제품 계산을 복제한 기대값은 통과 차단 사유다.
- 클라이언트 EditMode(선행 시험): `ClientConnectionLifetime`이 handshake 뒤 로그인을 먼저 보내고 `Accepted` 뒤에만 직업 선택을 보내는지, 거부 때 `LoginRejected`를 내고 끊는지. `AccountNameRule` 단정은 서버 시험에 두고 클라이언트는 중복하지 않는다.
- PlayMode: 메인 메뉴 씬에 계정 이름 입력칸이 있고 남은 거부 안내가 오류 글자칸에 보이는지. 선행 시험이 덮을 수 있는 만큼 덮는다.
- 봇: 새 시나리오와 기존 회귀 묶음.
- 실화면 한 번(독립 검증): 메인 메뉴 입력칸이 보이고 다른 요소와 겹치지 않는지, 거부 뒤 메인 메뉴의 이유 글자. PlayMode로 닿지 않는 화면 확인을 대신한다.

### 선행 시험이 고정한 이름

선행 시험(아래 「현재 결과」)이 위 기본안을 그대로 쓴 이름: `AccountNameRule`(`MinLength`·`MaxLength`·`TryNormalize`), `LoginResult`(0·1·2), `C_Login.accountName`, `S_LoginResult.result`, `AccountIdentity(Name, AccountId)`, `AccountIdResolver.Resolve`, `GameWorld.Accounts`, `GameSession.Account`·`IsLoggedIn`, `IClientConnectionSession.SendLogin`·`LoginResultReceived`, `ClientConnectionLifetime.LoginRejected`, `MainMenuNotice.Set`·`TryTake`.

새로 고정한 것:

| 고정한 것 | 이유 |
|---|---|
| `GameWorld.Accounts.OnlineCount : int` | 설계가 남긴 시험용 조회 자리. 「목록이 바뀌지 않는다」를 수로 본다 |
| `GameWorld.Accounts.TryGetOnlineSession(string normalizedName, out GameSession? session) : bool` | 같은 자리. 「등록 주인이 바로 그 세션」을 본다 |
| `ClientConnectionLifetime.LoginRejected`의 형식 `event Action<LoginResult>` | 거부 결과 값을 이벤트로 받는다 |
| `ClientConnectionLifetime.Connect(IPEndPoint endPoint, byte selectedClass, string accountName)` | 설계 문구 순서 |
| `MainMenuController`의 직렬화 필드 `_accountNameInputField` | 기존 `_serverHostInputField`와 짝. 씬 배선 시험이 `SerializedObject`로 읽는다 |
| 메뉴 시작 때 `MainMenuNotice.TryTake`로 안내를 꺼내 지운다 | 설계 「보이고 지운다」 |
| 같은 연결의 두 번째 `C_Login`은 결과 없이 버린다 | 설계 「로그인 흐름과 게이트」 2 |

시험이 부르지 않아 고정하지 않은 설계 이름: `AccountRegistry`, `EnqueueLogin`·`Tick`·`Forget`, `TryBeginLogin`·`CompleteLogin`·`RejectLogin`, `LoginHandler`, `LoginResultHandler`, `AccountLoadout`. 구현이 기본안을 따르되 바꾸면 이유를 보고한다.

리드 판단(사용자 결정 아님, 근거 E/`tdd-result-goal-context.md`):

1. 길이 머리가 실제 내용보다 **작아** 바이트가 남는 `C_Login`도 `InvalidName`으로 거부한다. 완료조건 6의 「길이 머리와 실제 내용 불일치」 그대로다. 선행 시험은 큰 경우만 넣었으므로 구현 계약에 요구로 적고 독립 검증자가 시험으로 본다.
2. T9의 「두 패킷이 가장 큰 두 번호」 단정은 유지한다. 이 goal이 먼저 병합되면 Content 패킷 PR이 버전 단정 셋과 함께 이 시험도 고친다(넷째 자리). Content에 알린다.
3. PlayMode 선행 시험은 쓰지 않았다. 거부 안내가 오류 글자칸에 들어가는 것은 EditMode 시험이 실제 씬과 제품 `Awake/Start`로 본다. 화면의 보임·겹침은 독립 검증의 실화면이 본다.
4. 기존 클라이언트 시험 `03_Client/Assets/Tests/EditMode/ClientConnectionLifetimeTests.cs`의 대역은 인터페이스와 `Connect` 변경으로 컴파일되지 않는다. 구현 계약의 고칠 기존 시험에 넣는다.

## 다른 파트와 맞춘 경계

Content 리드(`msg_59c720446c32`, World 질문 `msg_b9bcc472dc39`):

1. 패킷은 위 「PR 경계와 점검」의 번호 규칙 그대로다. 던전 클리어 PR은 새 번호를 더하지 않는다.
2. 클라이언트에서 겹칠 수 있는 파일은 `UnityClientSession.cs` 하나다. 둘 다 dispatch 표에 줄을 더하는 정도라 뒤 PR이 main을 받을 때 한 줄 충돌 정도로 본다. Content는 접속·타이틀·캐릭터 선택 흐름과 handshake 파일을 건드리지 않는다.
3. 버전 단정 세 곳은 뒤 PR이 다시 올린다.

Core 리드(`msg_775584e3f699`, World 질문 `msg_84e8fc956049`):

1. 계정 번호는 같은 이름이 재시작 뒤에도 같은 번호일 때만 DB 연결에 도움이 된다. 그래서 번호는 해석기 한 곳에서만 정하고, 나중에 Core가 그 해석기를 DB 조회로 바꾼다. 빈 GUID는 금지다.
2. Core 현재 goal은 게임 서버 C#을 건드리지 않는다. 서버 DB 연결(D2 수명·D3 입장)은 아직 일정이 없는 별도 goal이다. 순서는 「World PR 병합 뒤 Core D2·D3 시작」이다. D2도 `GameSession.cs`·`GameWorld.cs`를 건드린다.
3. 이름 → 계정 번호의 DB 이전은 Core 후속 후보이고, 새 migration·procedure·결과 계약 변경이라 별도 goal과 사용자 결정이 필요하다.
4. 계정마다 저장하려면 schema·lock·procedure를 바꾸는 새 migration이 필요하고 Core 로딩 게이트 전에 해야 한다.

## 실행 자원과 조율

- WSL: `99_Tools/sync-wsl.sh`는 원본 경로별 복제 공간을 쓰므로 다른 파트와 겹치지 않는다.
- 포트 7777: 봇 실행 전에 Core·Content·Management 리드에게 Orca 메시지로 알리고, 겹치면 먼저 알린 쪽이 끝낼 때까지 기다린다. Management의 사용자 중간 점검 시각은 메인이 알린다.
- 큰 실행(2026-10-13 메인 재진입 지시 `msg_c1b1e75ea27e`, 10-11 Content 합의 `msg_588b14556202`를 대신함): WSL 서버·7777 봇·Unity·Docker처럼 메모리를 크게 쓰는 실행은 직전 여유 물리·가상이 각각 4194304 KiB 이상인 것을 원시로 남기고, 시작 전에 열린 다른 리드(2026-10-13 기준 Content·Core·Rules·Art)에 알린다.
- Unity: world-active에는 아직 Unity Library가 없어 첫 열기에 전체 import가 필요하다(걸리는 시간은 재지 않았다). Unity MCP 시트는 하나라 쓰기 전에 메인에 요청한다. Content도 Unity를 많이 쓰므로 시간을 미리 맞춘다.
- DB: 쓰지 않는다.

## 요구사항 원천과 적용 결정

메인이 전달한 사용자 결정은 사용자 직접 입력과 구분한다. 메인 판단은 사용자 결정이 아니다. 받은 메시지 원시는 E/`lead-entry/`(entry-*.json, wait-*.json)에 있다.

- 진입 지시: 메인 `msg_4ffc2152ccc9`(2026-10-11T06:21:51Z). World 파트 새 리드, 이번 goal은 다중 계정 로그인 범위 초안부터, 승인 전 저장소 파일·goal branch·작업자 생성 금지, 승인 밖 새 파트(ORCA R-1)의 승인 범위는 인스턴스 맵과 다중 계정 로그인 둘.
- 범위 초안: v1 `msg_148c21badf30`(2026-10-11T06:38:24Z, SHA256 `1d73a1be0b0f4c2adbd4f3eaeee7497117b615584e50da1c4138f08016e58540`).
- 후보 원천: BACKLOG `multi-account-login`(인스턴스 맵 goal 종료 기록 PR에서 등록). 10-08 로드맵 초안 `C:/Dev/DawnHolder_Project/.backups/verification/2026-10-04-persistence-integration/next-goal-instance-map-plan-20261008/roadmap-draft.md` 45·46·57·59행(마을 광장 1단계, 광장 봇의 선행, 로딩 게이트의 선행, DB 없이 가능).
- Content 기술 계약: Content 리드 `msg_59c720446c32`(회신)와 Content 실행 알림 `msg_afe4c0835456`, World 질문 `msg_b9bcc472dc39`.
- Core 경계: Core 리드 `msg_775584e3f699`(회신)와 실행 알림 `msg_9e39897480a3`, World 질문 `msg_84e8fc956049`.
- R-7 비해당 근거: Content goal의 메인 결정 `msg_cfcaae303755`(시범 상한 세 건 사용).

## 적용 중인 사용자 결정

모두 메인 창 Enter 제출, 메인 전달이다. 이 pane의 사용자 직접 입력으로 격상하지 않는다.

- **10-28 시연 계획 승인**(메인 전달 `msg_5ed276291957`, 2026-10-13T03:46:56Z, 사용자 제출 12:4x KST): 「대시보드 결정 응답: 1) 계획 검토 - Main 10-28 시연 영상 시나리오와 파트 배정 → A」, 코멘트 「A로 가되 길드거점은 맵형태로 나중에 시간나면 꼭 만들었으면 좋겠어, 시각적인 성장 이펙트나 이런게 도드라지면 좋겠네, Shader로 해결할 수 있는건 해결하고, …」(전체 원문은 메인 메시지와 계획 원문 `C:/Dev/DawnHolder_Dashboard/plans/demo-video-1028-scenario.md`의 「승인 기록」). 효과: 이 goal은 재검증 → 실화면 → 제품 PR → 병합 승인 → 종료 순서로 끝낸다. 다음 goal은 길드 서버이고 R-8 뒤 새 리드가 범위 초안을 쓴다. 서버 PR이 패킷 계약을 먼저 정하고 Content 길드 화면 PR이 따른다. 지금 리드는 다음 goal을 준비하지 않고 종료 기록 「다음 계획」에 이 결정을 적는다.

- **10-28 시연 영상에 길드 거점**(메인 전달 `msg_850c8aef1678`, 2026-10-13T03:34:07Z, 사용자 제출 12:3x KST): 「대시보드 결정 응답: 1) 10-28 시연 영상 - 길드 거점 최소 순환을 새로 넣을지 → A 길드 거점 넣음」. 효과: 시나리오 승인 전에는 어느 리드도 새 goal을 열거나 준비하지 않고, 진행 중인 goal은 끝까지 간다.

- **마감 준비**(메인 전달 `msg_a657e13c5efc`, 2026-10-11T14:03:06Z, 사용자 제출 14:0xZ): 「일단 슬슬 마감 준비도 하자, 각 파트 마지막 작업 끝나면 재개 포인트 잡고 마무리하는 방향으로 지시해줘」. 효과: 이미 띄운 작업까지만 끝내고 새 작업자·검증자·Gardener는 열지 않는다. 재개 지점을 고치고 자기 branch를 commit·push한 뒤 메인에 「마감 준비 완료」를 보낸다. 18줄 작업자는 이 지시를 읽기 전(14:02Z)에 띄워 「이미 띄운 작업자」로 끝까지 처리했고 메인에 알렸다(`msg_09592295380f`). 묶음 재검증과 실화면은 다음 세션이다.

- **라우팅 핫픽스**(메인 전달 `msg_22f631ad7da7`, 2026-10-11T11:03:16Z, 정본 반영은 다음 Rules goal): 「대시보드 결정 응답: 1) 모델 라우팅 - 모든 리드를 문맥 25만에서 압축하도록 지금 적용할지 → A 전체 적용 · 2) 모델 라우팅 - 리드가 같은 메시지로 두 번 깨는 것을 지금 규칙으로 막을지 → A 지금 적용 · 3) 모델 라우팅 - 로컬 DB 설치·기동 도구의 검증을 지금부터 Astra로 옮길지 → A 지금부터 Astra · 4) 모델 라우팅 - 선행 시험 작성자를 Codex로 옮기는 시점 → A 10-31 평가 때」. 효과: 리드는 우편함 대기가 살아 있을 때 Orca 자동 알림으로 깨면 도구 호출 없이 한 줄로 턴을 끝낸다(대기가 꺼졌거나 불확실하면 check로 처리). 메인이 안전 지점(진행 중인 작업자 0)에서 리드 pane을 `--autocompact 250000`으로 다시 띄우므로 재개에 필요한 것은 이 파일 「재개 지점」과 근거 폴더에 둔다. 압축 뒤 맥락을 잃어 일을 다시 한 사례가 나오면 메인에 바로 알린다. 선행 시험 작성자는 그대로 신규 `claude-opus-5-5`다.
  - 결정 2 정정(메인 전달 `msg_7c26237ca10c`, 11:49:52Z): 처음 문장의 「대기를 다시 연다」는 메인의 덧붙임이었다. 사용자 원문(메인 pane Enter 제출): 「대시보드 결정 응답: 1) 라우팅 정본 반영 - 메모리 부족으로 꺼진 우편함 대기를 리드가 다시 켜지 않는 규칙을 유지할지 → A 유지 · 2) 라우팅 정본 반영 - 규칙 문서 크기가 약 1.8KB 느는 것을 허용할지 → A 허용」. 효과: 대기 background 작업이 돌고 있고(종료·kill 알림을 받지 않음) Orca 자동 알림으로 깼다면 도구 호출 없이 한 줄로 턴을 끝낸다. 메시지 처리는 대기가 끝나 깰 때 한다. 대기가 메모리 회수로 꺼졌거나 살아 있는지 확실하지 않으면 `--wait` 없는 check로 처리하고, 메모리 회수로 꺼진 대기는 스스로 다시 켜지 않는다.

- **Content·Core 우선**(메인 전달 `msg_775122227f2a`, 2026-10-11T07:46:37Z, 사용자 제출 16:5x KST): 「일단 좀 급한게 컨텐츠랑 코어쪽이니까 이 쪽 작업 우선해보자, 운영툴은 재개지점 맥락확보하고 세션 정리해줘」. 효과: 이 goal은 계속한다. 다만 Unity batch·포트 7777·큰 메모리 실행이 Content·Core와 겹치면 그쪽이 먼저다. 이미 시작한 실행은 끝까지 하고, 다음 큰 실행 전에는 Content·Core 리드에 묻는다. 그래서 구현자는 큰 실행 전에 리드의 「진행」을 기다린다. Management 7777 중간 점검 일정은 없어졌다.
- **범위 승인**(메인 전달 `msg_493c03f863cf`, 2026-10-11T06:45:14Z, 사용자 제출 15:4x KST): 「대시보드 결정 응답: 1) 계획 검토 - World 다중 계정 로그인 goal 범위 초안 v1 → A 승인 (초안 msg_148c21badf30) · …」. 효과: 범위 v1 그대로, 질문 1은 A(계정 이름만 입력, 비밀번호 없음), 「확인할 기본값」 1~4도 초안대로다.
- **World 새 리드와 첫 일**(2026-10-10 23:21 KST, 진입 지시 `msg_4ffc2152ccc9`에 전달): 「결정 39 → B 「B로 가자」」. 효과: World 새 리드는 다음 세션에 열고 첫 일은 다중 계정 로그인 범위 초안, 구현은 범위 승인 뒤.
- **리드 재기동**(2026-10-11 15:19 KST 무렵, 같은 진입 지시): 「대시보드 결정 응답: 1) 리드 재기동 - Core·Content·Management 이어서, World 새로 열기 → A 지금 넷 연다 · …」.
- **Core 작업 줄 둘·작업 공간·우선순위**(2026-10-10 16:24·16:29 KST, 같은 진입 지시): 「6) 18 - Core 작업 줄을 둘로 나눌지 → A 두 줄」, 「1) 22 - … → C 코멘트 「World 이름으로 열어서 작업 진행」」, 「5) 17 - 마일스톤 우선순위(기능 동결 10월 28일까지) → A 인스턴스 던전 먼저」. 동결까지 마을 광장은 다중 계정 로그인까지만 노린다는 것은 메인 비교 문서의 추론이고 확정이 아니다.
- **영속화 설계의 사용자 선택**(D0, [D0 goal](../2026-09-29-persistence-design/goal.md) 7·12행): 정식 인증·다중 캐릭터 제외, 같은 캐릭터의 새 중복 접속 거부. 「확인한 기본값」 2와 질문 1 A의 바탕이다.

## 위험

1. 반쯤 열린 연결: 클라이언트가 FIN 없이 사라지면(네트워크가 끊기는 등) 서버가 끊김을 알아차릴 때까지 그 계정이 접속 중으로 남는다. 지금 서버에는 접속 시간 제한이 없다. 새 접속 거부 정책이라 그동안 같은 계정으로 다시 들어오지 못한다. 실제로 겪으면 접속 시간 제한을 다음 후보로 올린다.
2. 패킷 번호 경쟁: Content 패킷 PR과 같은 시기에 열리면 뒤 PR이 재생성과 버전 단정 셋·DLL 사본 갱신을 다시 한다.
3. Unity 비용: world-active의 첫 import와 Unity MCP 시트를 Content와 나눠 쓴다. 클라이언트 쪽 검증이 늦어지면 제품 PR 전체가 늦어진다.
4. 저장 슬롯 고정: 이 goal 뒤에도 계정마다 저장할 수 없다. Core 로딩 게이트 전에 DB 변경 goal이 필요하다.
5. 이름 도용: 비밀번호가 없으므로 남의 계정 이름으로 들어올 수 있다. 접속 중인 계정은 이중 접속 거부로 지켜지지만, 접속하지 않은 계정은 지켜지지 않는다.
6. 병합 관문 그물: gh·merge 또는 push·main 낱말이 함께 든 명령은 막힌다. PR 문구는 파일로 넘기고 push는 따로 실행한다. 막히면 우회하지 않고 메인에 알린다.
7. 기능 동결(10-28): 서버·클라이언트·봇을 한 PR로 바꾸므로 Unity 확인이 늦으면 일정이 밀린다.
8. 거부 안내의 역순 소실: 보통은 거부 결과가 끊김보다 먼저 main 큐에 들어가 안내가 보인다(구현 중 고친 결함). 다만 다른 스레드의 송신 실패나 외부 `Disconnect`가 정리를 먼저 예약하면 뒤의 거부는 끝난 연결의 결과로 무시된다. 그때 사용자는 안내 없이 지금 씬(보통 Town)에 끊긴 채 남는다(구현 보고 「미실행·남은 작업」의 `GameEntryPoint.Start`·`NetworkService.Update` 근거). 고치려면 공유 DLL인 `04_ClientNet` 전송 계층이나 끊김 뒤 메뉴 복귀 흐름을 바꿔야 해서 이번 범위 밖으로 두었다(리드 답 `msg_a9fc34e86079`). 실제로 겪으면 「끊기면 메인 메뉴로 돌아가기」를 후보로 올린다.

## 다음 계획 후보

이 goal의 완료조건 밖이라 다음 계획에서 판단한다. 종료 기록 PR 때 BACKLOG와 대조한다.

- 클라이언트 문서 `client.md` 로그인 문단에 잘못된 길이의 결과도 같은 gate에서 끊는다는 한 구절(재검증 O-3).
- `FEATURE_MAP.md` 「공통 계약」 패킷 줄에 v19 계정 로그인 절 링크(재검증 O-2, World 브랜치부터 없던 링크).
- HUD PlayMode 시험의 실시간 3초 대기 간헐 시간 초과(재검증 O-4). Content 소유로 보여 Content에 알렸다(`msg_75c883fc0f0b`).
- 첫 독립 검증의 설계 관찰 O-1·O-2·O-3·O-5(아래 「독립 검증」 절).
- Unity 시험 harness가 알려진 Unity 부수 효과 두 파일을 「리드 복원 대상」으로 표시(메인이 Rules에 BACKLOG 후보로 넘김, `msg_834e29b98d68`).
- 위임 계약을 낼 때 고른 규칙 절의 원문 전체와 계약 발췌를 기계로 비교(RV-01 재검증 RF-01의 검사 전환 판단).
- 버전을 올릴 때 숫자가 든 시험 이름과 리터럴 버전의 불일치를 찾는 좁은 검사(같은 판정의 RV-01 검사 전환 판단, `Nineteen` 같은 표기 정책이 먼저 필요).
- 이제 로그인까지 하는 시험 helper 이름 정리(`CharacterSelectHandlerTests`의 `BypassHandshake`·`SetupHandshakedSession`). Combat 시험 네 파일이 같은 이름 `BypassHandshake`를 로그인 없이 쓰는 관례라 함께 판단한다(아래 「RO-01 수정」).
- 진입 단계를 바꾸는 goal의 계약을 낼 때 그 단계를 부르는 모든 경로(helper·직접 호출·봇·통합 시험)를 기계로 모아 옆 설명과 대조(아래 「RO-01 수정」의 리드 훑기 누락).
- 옛 진입 설명 두 곳: `GameSession.cs:520`의 월드 진입 시점 축약과 `PingHandlerTests.cs:63~64`의 handshake만 하는 helper 설명(아래 「RO-01 묶음 재검증」의 RO-03, goal 전부터 있던 문구).
- 봇 `ProbeBase.cs:12` 머리말이 「4종」인데 항목은 다섯이다(같은 판정 RO-04, `c5482d6b^`부터 있던 문구).
- fixture가 돌려준 직후 상태(`HasSelectedClass=false`, `map.Players` 비어 있음, `S_EnterMap` 없음)를 관찰하는 시험(같은 판정 RF-01의 검사 전환 판단, 주석 문자열 검사만으로는 잘못된 계약도 통과한다).
- 반복 규칙 후보: 설명 줄을 바꾸는 계약은 그 줄의 주장마다 실제 호출 위치 대조표를 붙인다(리드 설명 대조 누락의 두 번째 발생, 아래 「RO-01 묶음 재검증」). 규칙 정본은 Rules 몫이고, 다음 Rules goal이 10-28 뒤라 메인이 그때 Rules 진입 계약에 넘긴다(`msg_eb45c17c73e9`). 이 goal에서는 「RF-01 수정」 계약에 먼저 적용했다.
- `CharacterSelectHandlerTests.cs:63~64`가 `HasSelectedClass`를 「internal getter」, 접근 이유를 「같은 어셈블리 내 서브클래스」라고 적는다. 실제 선언은 `protected internal`(`GameSession.cs:91`)이고 시험 어셈블리 접근은 `InternalsVisibleTo`(`GameServer.csproj:20~22`)다(아래 「RF-01 수정」 작업자 관찰, goal 전부터 있던 문구).
- 같은 파일의 옛 설명 두 줄: `:24`는 없는 `GetStatsForTest` getter로 검증한다고 적고(실제는 `StatsSet`·로그·`DisconnectCalls`), `:56`은 class 선택 뒤 월드 진입이 시험 대상이라고 적지만 다섯 시험은 월드 진입을 단정하지 않는다(아래 「RF-01 재검증」 O-2·O-3, goal 전부터 있던 문구).

## 현재 결과

### 착수 — 2026-10-11

- 리드 진입: Run `run_a927bbaff1f5`(06:24:46Z), READY `msg_7b0f365216d2`. 진입 지시와 우편함 원시는 E/`lead-entry/`에 있다.
- 조사: 읽기 전용 내부 에이전트 둘이 지금 접속 흐름과 영속화 계정 모델을 조사했고, 리드가 원문 줄을 표본 대조했다(E/`lead-entry/research-notes.md` 끝). 외부 실행이나 독립 검증이 아니다.
- 범위 승인 뒤 최신 main `6782cd67`에서 이 branch를 만들고 이 파일과 CURRENT의 World 줄을 첫 commit에 넣었다. 빌드·서버 시험·봇·Unity·DB는 실행하지 않았다.

### 선행 시험 — 2026-10-11

- 작성자: 신규 `claude-opus-5-5` xhigh(화면 「Opus 5.5 with xhigh effort」, backend unknown), 태그 `[World 검증자]`. 리드 pane 아래 split `term_e9d6cabe-4f04-4ff8-bf03-3c9c48d73d66`(07:05:08Z), Task `task_52e64b8fca7f`, Dispatch `ctx_2b90c21eec6c`. worker_done `msg_800ffc59a8ee`(07:40:13Z, succeeded). release `retained`(external_terminal) 뒤 pane을 닫았다.
- 계약: E/`tdd-contract.md` v1(SHA256 `80d7482b9911ee026510e8b5408e5432023652f998a532d0e9fb398f2cc828b1`), 요약 E/`tdd-spec.md`, 입력 hash E/`tdd-inputs.txt`, 경로 확인 E/`tdd-preflight.txt`, Unity 실행기 E/`tdd-harness/Run-UnityTests.ps1`(SHA256 `140e1543…`).
- 보고: E/`tdd/report.md`(SHA256 `68cd905732cecc7ea65b69ccb4fce956e313f11e8b0ae501e11cbc6474456727`), E/`tdd/verdict.json`(SHA256 `4620c37b…463b`).
- 새 시험: 서버 7파일 694줄(`GameServer.Tests/Accounts/` 셋, `Network/Login*` 넷), 클라이언트 EditMode 3파일 314줄과 Unity가 만든 `.meta` 셋. 서버 메서드 24개(경우 65개), 클라이언트 메서드 7개(경우 8개).
- 실행(원시는 E/`tdd/`):

| 실행 | exit | total / pass / fail / skip |
|---|---|---|
| 서버 WSL 기준선 | 0 | 974 / 969 / 0 / 5 |
| Unity EditMode 기준선(첫 import 200.8초 포함 230.2초) | 0 | 417 / 417 / 0 / 0 |
| 서버 (1) 지금 경계 | 1 | 3 / 2 / 1 / 0 — T8 RED 1, 보호 GREEN 2 |
| Unity (1) 지금 경계 | 2 | 2 / 0 / 2 / 0 — T10 RED 2 |
| 서버 (2) 새 API | 1 | 컴파일 RED(선언 오류 7건) |
| Unity (2) 새 API | 1 | 컴파일 RED(6건) |

- 컴파일러가 선언 단계 오류만 냈으므로 새 심볼 전체는 보고의 정적 목록에 있다.
- 리드 대조: T2 기대 문자열 4개가 Python `uuid.uuid5` 원시와 같다. 새 시험에 이름 문자열로 API를 찾는 reflection이 없다. (1) RED 파일이 (2)에서 바뀌지 않았다. Unity (1) 결과 파일의 두 T10 시험이 Failed다. 작업 트리가 보고의 쓰기 목록과 같다(E/`tdd-result-goal-context.md`).
- 실행 조율: Core SQL 컨테이너 측정과 겹치지 않게 기준선 뒤 07:18Z~07:33Z에 실행을 멈췄다(Core `msg_b796b7140470`·`msg_439f622ca89b`, 리드 `msg_6ebfdd724930`·`msg_d4da3f7632ad`).
- 부수 효과: Unity가 `03_Client/ProjectSettings/ProjectSettings.asset`의 정의 기호 한 줄(`SENTIS_ANALYTICS_ENABLED` 제거)을 다시 썼다. 알려진 BACKLOG `unity-sentis-define-drift`라 리드가 되돌렸고 commit에 넣지 않았다. Claude Code가 작성자의 큰 읽기 출력 하나를 홈의 자기 `tool-results`에 자동 저장했다. 런타임 자체 저장이라 조치하지 않았다.
- 실행하지 않은 것(작성자): PlayMode, 서버 실행, 봇, 7777, DB, Unity Editor·MCP, 패킷 생성기, Windows 빌드, 서식 검사.
- 리드 서식 검사: commit `757b59ac` 뒤 `pwsh -NoProfile -File 99_Tools/format-check.ps1` exit 0, 「Formatting checks passed」(07:43:19Z 시작, 약 13분, 원시 E/`lead-format/format-check.txt`). 도구가 증거를 사용자 TEMP의 `dawnholder-format-<id>/evidence`에 기본으로 남겼다.

### 구현 — 2026-10-11

- 구현자: 신규 `gpt-6-astra` xhigh(첫 화면 「GPT-6-Astra xhigh · Full Access」, backend unknown), 태그 `[World 구현 Astra]`. 리드 pane 아래 split `term_26100139-05c7-4c23-b3c7-3dcc2298f967`, Task `task_1cd120ce26cf`, Dispatch `ctx_2b8c1df07943`(07:58:40Z). worker_done `msg_26812a115ece`(09:08:01Z, succeeded). release `retained`(external_terminal) 뒤 pane을 닫았다.
- 계약: E/`impl-contract.md` v1(SHA256 `245a977f8a72e6b0717f18d9583debf8b3e63125f2052f6a8ae69b38a543b7c7`), 요약 E/`impl-spec.md`, 입력 hash E/`impl-inputs.txt`, 경로 확인 E/`impl-preflight.txt`, 리드 메모 E/`impl-prep-context.md`.
- 구현 중 리드 보충 넷(원시는 E/`impl-answer-*.txt`·`impl-go-*.txt`·`impl-supplement-playmode.txt`):
  1. 입력칸 자리: 서버 칸 위(`msg_7337a614965c`). 위 「설계」 클라이언트에 반영했다.
  2. 생성 코드 줄 끝: 작업본 `GenPackets.cs`가 CRLF 283줄로 섞여 있어(Content `msg_f78373adc6c1` 지적) 첫 Windows 빌드 전 CR만 지워 LF로 맞추는 예외를 허용했다(`msg_714dd50b7a99`). 생성기 출력 SHA256 `cd456910…a522` → 정규화 `0cb097fd…0370`, CR 0.
  3. 기존 PlayMode fixture 로그인 단계(`msg_9eb7fec7a934`): 아래 리드 기록 1.
  4. 거부 안내 소실 결함 수정(`msg_6c2d51ac00e0`, 순서 답 `msg_a9fc34e86079`): 구현자가 자체 정적 실사로 찾았다. 거부 결과가 main 큐에 있는 동안 원격 끊김이 `_closed`를 먼저 세워 `CanApply`·`IsCurrent`가 거부를 버렸다. 범위 안 결함 (c)로 받아 두 거부 값에만 닫힌 전송 검사를 건너뛰게 했다(세대·같은 세션·`_loginSent` 검사는 유지). 구현자 회귀 시험 6메서드 11경우를 `ClientConnectionLifetimeTests.cs`에 더했고, 제품 수정 전 436건 중 새 4건이 「InvalidName/AlreadyOnline 누락」으로 실패한 RED를 남겼다. 남은 역순 경로는 위 「위험」 8이다.
- 보고: E/`impl/report.md`(SHA256 `0b2cde7407de8b2d842cdd6341dabf2d65aec283abdbf60b698b7cf043941f36`). 바뀐 파일 52개(추적 39 +1112/−46, 새 13개 348줄), 수기 제품 C# 22파일 +538/−12.
- 실행(원시는 E/`impl/`):

| 실행 | exit | total / pass / fail / skip |
|---|---|---|
| 서버 WSL 전체, 기존 시험 보완 전 | 1 | 1039 / 906 / 128 / 5 — (a) 4, (b) 124 |
| 서버 WSL 전체, 보완 뒤 | 0 | 1039 / 1034 / 0 / 5 |
| Windows `dotnet build Dawnholder.slnx`(LF 맞춘 뒤) | 0 | 경고 7 / 오류 0 |
| Unity EditMode 전체(보완 뒤) | 0 | 425 / 425 / 0 / 0 |
| PlayMode 수정 전 표본(`HudGoldAndPanelToggleSceneTests`) | 2 | 3 / 0 / 3 / 0 — 「handshake and character selection」 시간 초과 |
| 거부 회귀 추가 뒤 EditMode(RED) | 2 | 436 / 432 / 4 / 0 |
| 최종 EditMode 전체 | 0 | 436 / 436 / 0 / 0 |
| 최종 PlayMode 가짜 서버 lane 다섯 클래스 | 0 | 28 / 28 / 0 / 0 |
| 최종 서식 검사 | 0 | 제품 297파일 수정 0, 도구 시험 244/244 |

- 리드 대조(E/`impl-result-goal-context.md`): 최종 원시 다섯 개가 보고와 같다. 서버 시험 뒤 바뀐 서버·공유·도구 파일이 없어 서버 결과가 최종 트리를 반영한다. 선행 시험 10파일이 그대로다. 작업 트리 목록이 worker_done 파일 목록과 같다. 생성 코드·Shared.dll hash가 보고와 같고 ClientNet DLL은 그대로다. 실패 분류 표 128행((a) 4, (b) 124)의 표본 두 건이 diff와 같다.
- 부수 효과: Unity가 `ProjectSettings.asset` SENTIS 정의 한 줄을 다시 써서 리드가 되돌렸다(BACKLOG `unity-sentis-define-drift`). PlayMode 뒤 `MinimapRT.renderTexture`가 변경으로 보였지만 내용이 HEAD와 같아(SHA256 `7123af77…`) commit에 넣지 않았다.
- 실행 조율: 큰 실행마다 Content·Core 리드의 진행을 받았다(Core `msg_19b197e5ce96`·`msg_37aee9a3d777`·`msg_3de0491cf888`·`msg_15758793c10e`, Content `msg_f78373adc6c1`·`msg_7c7fb65c7591`·`msg_73c72f640159`). Core는 08:29:06Z에 여유 물리 메모리가 약 4GB까지 떨어져 자기 대기가 꺼졌다고 알렸다(World Unity 종료와 서식 검사 시작 무렵).
- 실행하지 않은 것(구현자): 서버 실행, 봇, 7777, 실서버 PlayMode 두 클래스, DB, Unity Editor·MCP, 실화면. 봇 새 시나리오는 빌드와 등록만 했다.
- 리드 기록(첫 발생이라 goal에만 적는다, 교정 정본 「첫 발생은 goal에 기록」):
  1. **PlayMode 계약 누락**: 구현 계약이 Unity EditMode만 적었다. 완료조건 10에는 PlayMode도 들어가고 공용 fixture가 로그인 없이 직업 선택까지 가는 구조라 깨질 것이었다. 리드가 구현 중 찾아 보충했다(분류 (b)).
  2. **맥락 메모 순서**: 독립 검증 계약 초안(E/`verify-contract-draft.md`)을 맥락 메모(E/`verify-prep-context.md`)보다 먼저 썼다. 위임 전 초안이라 결과물에 영향은 없다.
  3. **보류 전달 지연**: Content가 08:49:10Z에 Unity 사용을 알렸는데 리드가 구현자 보류를 08:51:50Z에 보냈다. 그 사이 구현자가 앞선 진행에 따라 PlayMode를 08:50:15Z~08:51:13Z에 돌렸다. Content에 「08:50Z에 보류」라고 잘못 적었고 뒤에 정정했다(`msg_a8855130a311`). Content 검증자는 Unity 0을 기다리는 규칙이라 겹침 피해는 없었다.
  4. **빈 본문 메시지**: 우편함 가드에 막힌 복합 명령 뒤 본문 파일이 없는 채로 send해 Content에 빈 메시지(`msg_3502b131fc8e`)가 갔다. 바로 본문을 넣어 다시 보냈다(`msg_87e039a832cf`). 그 뒤로는 본문 파일을 `test -s`로 확인한 뒤 보낸다.

### 독립 검증 — 2026-10-11

- 검증자: 신규 `claude-opus-5-5` xhigh(최초 명령 `claude --model claude-opus-5-5 --effort xhigh`, 첫 화면 「Opus 5.5 with xhigh effort」, backend unknown), 태그 `[World 검증자]`. 배정은 검증자 시범의 「Astra 구현 → 신규 Opus」와 「프로토콜·공유 DLL → 신규 Opus」다. 리드 pane 아래 split `term_699a4630-4007-4336-ba5f-f12528be7d91`, Task `task_6f7de5b2db44`, Dispatch `ctx_809a116bf6b1`(09:16:47Z). worker_done `msg_e35237580f89`(10:54:02Z, succeeded). release `retained` 뒤 pane을 닫았다.
- 계약: E/`verify-contract.md` v1(SHA256 `e538fceddc61306f01f0eb84532a719d8bdd4f5f9f554d12e9e366c04f130cab`), 요약 E/`verify-spec.md`, 입력 hash E/`verify-inputs.txt`(84개), 경로 확인 E/`verify-preflight.txt`, 리드 메모 E/`verify-prep-context.md`. 강 등급, 대상 commit `c5482d6b`.
- 판정: E/`verify/verdict.md`(SHA256 `a1951067b6c3ad9a6b6c38df3094890092375bf4c501855d90f0b8f83b21fe20`). **차단(VER-01)**. 필수 검증 A~G와 강 등급 실제 진입 둘을 모두 했고 기능·동작 결함은 없었다.
- 실행(원시는 E/`verify/`):

| 실행 | exit | 결과 |
|---|---|---|
| 봇 회귀 17개(첫 항목 `MultiAccountLoginSmoke`) | 0 | PASS=17 FAIL=0. 새 시나리오 로그인 로그: accepted 3, AlreadyOnline 1, InvalidName 1(원래 이름은 서버 로그 0건). 거부 결과 둘이 봇에 닿았다 |
| WSL 서버 전체 | 0 | 1047 / 1042 / 0 / 5(구현 1039 + 검증자 새 시험 8) |
| Windows `dotnet build Dawnholder.slnx`(증분) | 0 | 경고 7 / 오류 0. Shared·ClientNet은 다시 컴파일되지 않아 재현 증명이 아니었다 |
| `--no-incremental` 재빌드 Shared·ClientNet | 0 | 두 DLL이 커밋 bytes와 같다(`f11ad385…`, `dc708e2f…`) |
| 서식 검사 | 0 | 제품 298파일 수정 0, 도구 시험 244/244 |
| Unity EditMode 전체 | 0 | 445 / 445(구현 436 + 검증자 9) |
| Unity PlayMode 가짜 서버 lane | 0 | 32 / 32(다섯 클래스 28 + 검증자 `LoginRejectionMenuTests` 4) |
| 서버 lane(WSL GameServer 7777 + 실서버 PlayMode 두 클래스) | 0 | 2 / 2. 서버 로그에 실제 Unity 클라이언트 세 연결의 `C_Login` → accepted → `C_CharacterSelect` 순서 |

- 검증자 새 시험(commit 대상): 서버 `GameServer.Tests/Accounts/AccountLoginVerificationTests.cs`(D1 작은 길이 머리 4경우, D4 로그인·끊김 경합 두 종류 각 300회, D5 계정 번호 미송신, 로그 형식), EditMode `ClientLoginVerificationTests.cs`(D2 범위 밖 결과 값, D6 거부 직후 끊김 수정 넷), PlayMode `LoginRejectionMenuTests.cs`(D3 거부·이름 문제 뒤 메인 메뉴와 안내 넷). 모두 통과했고 제품 결함으로 RED가 된 시험은 없다.
- 결함:
  - **VER-01**(낮음, 차단, 귀속 구현자): `00_Document/domains/client.md:16`이 「`LoginResultHandler`는 세션의 메인 스레드 gate를 거쳐 결과를 적용」이라고 적는다. 구현 중 고친 뒤로 유효 거부 둘은 닫힌 전송 검사(`CanApply`)를 건너뛰어 정리 전까지 적용된다. CODE_CONVENTION 「주석과 문서」 위반이다. 검사로 바꿀 수 없다고 판단했다(문장 의미 대조).
  - **VER-02**(낮음, 비차단, 귀속 구현자): 구현자 분류 표 121행(`ServerHostLifecycleTests.Stop_…(enterWorld: True)`)은 (b)로 적혔지만 처음 패킷 단정을 바꿨으므로 (a)+(b)가 맞다. 리드 정정: 서버 기존 실패 128건 = (a) 3 + (a)+(b) 2 + (b) 123(검증자 재판정 표 E/`verify/server-failure-rejudge.md`). 구현자 근거 원본은 고치지 않는다.
- 기존 시험 재판정: 64구간 모두 약화 없음. 수정 전 통과였던 5개 시험은 로그인 게이트 때문에 헛통과하던 것을 로그인을 더해 원래 상태로 되돌린 강화다. 구현자 회귀 11경우 중 RED 4건이 결함 경로를 실제로 재현했다.
- 설계 관찰(비차단, 후속 후보): O-1 `AlreadyOnline` 안내 문장도 `MainMenuNotice` 상수로 모으기, O-2 `Duplicate C_Login` 로그 문구 넓히기, O-3 `ClientConnectionLifetime`의 소유 검사와 닫힘 검사를 이름으로 나누기, O-5 봇 계정 이름 일련번호 건너뜀. O-4는 위 「위험」 8, O-7은 「위험」 2다.
- 다른 파트로 넘길 관찰: O-8 서식 검사의 Plugins DLL 가드가 WSL 사본에 없는 파일을 해시해 빈 파일끼리 비교하므로 항상 통과한다(`99_Tools/format-check.sh:42-48·107·138`). 검사 코드 담당인 Core 리드에 알린다.
- 실행 조율: 봇은 Content 수정 단계의 7777 빈 틈에(Content `msg_0e0eec0571bc`), WSL·빌드·서식은 Content·Core 진행 뒤(`msg_39b19125e05a`·`msg_973fa48d7e8d`), Unity·서버 lane은 Content 해제(`msg_bc322f3be185`, 10:21Z)와 Core 컨테이너 종료(`msg_c1b0607f6fab`, 10:40:46Z) 뒤(리드 진행 `msg_bb5a04edb680`)에 돌렸다. Core가 컨테이너를 먼저 띄우려 해 Unity 단계를 한 번 미뤘다(`msg_d6fc95d67a76`).
- 부수 효과: Unity가 `ProjectSettings.asset` SENTIS 한 줄과 `MinimapRT.renderTexture` 줄 끝을 다시 써서 리드가 되돌렸다.
- 리드 기록(첫 발생이라 goal에만 적는다):
  1. **DLL 재현 명령**: 검증 계약 G가 증분 `dotnet build Dawnholder.slnx`를 재현 확인으로 적었다. 입력이 최신이면 Shared·ClientNet을 다시 컴파일하지 않아 재현 증명이 되지 않는다(검증자 관찰 O-6). 검증자가 찾아 리드가 `--no-incremental` 재빌드를 허용했다(`msg_ff3c566f85cc`). 다음 계약부터 `--no-incremental`로 적는다.
  2. **계약 C의 수치**: 검증 계약 C가 구현자의 초기 status 값 「(a) 3, (b) 125」를 적었다. 구현 보고는 4/124로 정밀화했다. 판정에는 영향이 없다.

### main 병합과 재번호 — 2026-10-11

- 왜: Content PR230이 main(merge `999b1b98ba38934568f852adc2d22c9ee333d864`, 10:57:10Z)에 먼저 들어가 패킷 39·40과 버전 18을 썼다(메인 알림 `msg_aa95be30ee82`). 「PR 경계와 점검」 넷째 줄대로 World는 41·42번·버전 19로 옮겼다. VER-01도 같은 계약에 묶었다(확정 실패 집계상 VER-01 1회차 수정).
- 작업자: 신규 `gpt-6-astra` xhigh `[World 구현 Astra]`(배정 근거 implementer-routing 신호 3, backend unknown), 리드 pane 아래 split `term_a22887db-cdfd-49aa-b807-004cb7332b93`, Task `task_4cbea266fa22`, Dispatch `ctx_2b8550f92fbf`(11:04:34Z). worker_done `msg_004a934fda9a`(11:36:02Z, succeeded). release `retained` 뒤 pane을 닫았다. 작업자는 git 상태를 바꾸지 않았고 리드가 stage·commit했다.
- 계약: E/`merge-contract.md` v1(SHA256 `31b04b11db5308bd50374d2717edda7bb3c1eb6c6763c0e837eb3d368ece5fe4`), 리드 메모 E/`merge-prep-context.md`. 보충·답: 보충 1 `msg_2f88b3642582`(Content 시험 `JoinAndEnter`에 로그인 한 단계, Content에 쓰기 전 알림 `msg_3b64363cd9dd`), 답 1 `msg_d68364b8c861`(protocol.md v18 절 첫 문장만 과거 시제, Content 알림 `msg_725c091951ad`), 생성기 진행 `msg_1c2b3e313e90`·`msg_f70d67c11464`, 묶음 진행 `msg_7e07433fb46c`(Content `msg_e2fa43b6adba`·Core `msg_38260c734c85` 허용 뒤), Unity 부수 효과 답 `msg_c8d02ffb8d92`.
- 결과: merge commit `44ffe9e3db04c3fb482c727e2fbacfc427b8b043`(parent `b2953476`·`999b1b98`, 45파일 +2470/−74 중 main 쪽 변경 포함). 보고 E/`merge/report.md`(SHA256 `e18c0c1206a846ab39c47269143566521fa3e27d0a3fa351857e769e6a773dd1`).
  - 충돌 8개(PDL·생성 코드·버전·Shared.dll·버전 단정 세 곳·protocol.md)를 main 쪽 정의·번호·이력·단정은 그대로 두고 World 추가를 뒤로 옮겨 풀었다. PDL 순서는 39 `S_DungeonClearResult`, 40 `C_DungeonClearConfirm`, 41 `C_Login`, 42 `S_LoginResult`다.
  - 생성 코드는 해결한 PDL로 실제 생성기를 돌려 CR 306바이트만 지웠다(최종 SHA256 `d3499106…7d40f`). Shared.dll은 `--no-incremental` 빌드 산출이다(`bba58745…1ed6`). ClientNet DLL은 다시 빌드했지만 bytes가 그대로다(`dc708e2f…e175`, `b2953476`·`999b1b98`과 같음).
  - VER-01: `client.md` 「메뉴와 계정 로그인」 둘째 문단을 유효 거부는 정리 전까지 메인 큐에서 적용하고 `Accepted`·정의 밖 값·다른 패킷은 닫힌 전송에서 적용하지 않는다는 실제 분기로 고쳤다. 「위험」 8의 역순은 「세션 정리 전까지」라는 한계로만 표현했다.
  - 기존 시험 실패 분류: Content `DungeonClearPacketContractTests` 1건 (b) fixture 누락(같은 WSL 필터 명령 전 8/7/1 → 후 8/8/0, 기존 `LifecycleTestWorld.LogIn` 한 줄). 버전 단정 세 곳은 계약이 미리 정한 (a)다.
- 실행(원시 E/`merge/`):

| 실행 | exit | 결과 |
|---|---|---|
| 생성기 | 0 | 로그인 41·42, Content 39·40 유지 |
| Windows `--no-incremental` Shared → ClientNet → slnx | 0 | 경고 7 / 오류 0, DLL hash 위와 같음 |
| WSL 서버 전체 | 0 | 1058 / 1053 / 0 / 5(World 1047 + Content 11) |
| Unity EditMode 전체 | 0 | 446 / 446 |
| Unity PlayMode 가짜 서버 11클래스 | 0 | 45 / 45 |
| 서식 검사 | 0 | 도구 시험 244 / 244 |

- 리드 대조(E/`merge-commit-context.md`): DLL 둘·생성 코드 hash, 서버·EditMode·PlayMode·서식 원시, Content fixture 한 줄, 충돌 표식 0, PDL 순서가 보고와 같다. `git diff --check`의 생성 코드 행 끝 공백 18곳은 생성기 템플릿 출력이다(행 끝 공백 줄 수 `b2953476` 243 → `999b1b98` 257 → 병합 261).
- 실행하지 않은 것(작업자): 봇, 7777·서버 lane, DB, Unity Editor·MCP, 실화면. 재검증이 봇과 서버 lane PlayMode를 맡는다.
- 부수 효과: Unity가 `ProjectSettings.asset` SENTIS 한 줄과 `MinimapRT.renderTexture` 줄 끝을 다시 써서 리드가 commit 전에 되돌렸다.
- 리드 기록:
  1. **Unity 부수 효과 계약 누락(두 번째 발생)**: 병합 계약이 `ProjectSettings.asset`만 적고 `MinimapRT.renderTexture`를 빠뜨려 작업자가 공식 질문을 보냈다(`msg_4f1c3dfb4a4b`). 첫 독립 검증 계약도 같은 누락이었다. 두 번째 발생이라 이 goal의 다음 계약부터 두 파일을 함께 적는다(재검증 초안에 반영). 더 높은 층은 Unity 시험 harness가 전후 git 상태에서 알려진 두 파일을 「리드 복원 대상」으로 따로 표시하는 것이다. 메인에 후보로 올린다.
  2. **큰 실행 묶음 진행**: Content·Core 허용을 받은 뒤 단계별 status만 받게 하여 승인 대기를 생성기 48초·빌드 87초로 줄였다. 다음 큰 실행 묶음도 같은 방식으로 묻는다.

### 재검증 — 2026-10-11

- 검증자: 신규 `claude-opus-5-5` xhigh(최초 명령 `claude --model claude-opus-5-5 --effort xhigh`, 첫 화면 「Opus 5.5 with xhigh effort」, 선택창 없음, backend unknown), 태그 `[World 검증자]`. 배정은 검증자 시범의 「Astra 구현 → 신규 Opus」와 「프로토콜·공유 DLL → 신규 Opus」다. 리드 pane 아래 split `term_bb444d7f-a8be-4ac1-8fa4-23b9ac3052e8`, Task `task_daa6e0025e74`, Dispatch `ctx_d3ae5103199f`(11:43:13Z). worker_done `msg_0acdc8b06df9`(12:22:53Z, succeeded). release `retained` 뒤 pane을 닫았다.
- 계약: E/`reverify-contract.md` v1(SHA256 `463c0c0356895f91faa73d1d98f052f0fa528766f022055a017642e0b622e98b`), 요약 E/`reverify-spec.md`, 입력 E/`reverify-inputs.txt`(43개와 두 numstat), 경로 확인 E/`reverify-preflight.txt`, 리드 메모 E/`reverify-prep-context.md`. 강 등급, 대상 merge `44ffe9e3`, 고정 HEAD `a0a13a3e`.
- 판정: E/`reverify/verdict.md`(SHA256 `1ea975f42f30eb2e1b54505af1a0d4aabf5e680cc5084802bfe65c0fe2064199`). **차단(RV-01)**. 병합 해결·재번호·버전·생성 코드 재현(CR만 지운 결과가 커밋 bytes와 같음)·DLL `--no-incremental` 재현·VER-01 문장별 대조·Content 보존·실제 진입 둘을 모두 했고 기능·동작 결함은 없었다.
- 실행(원시는 E/`reverify/`):

| 실행 | exit | 결과 |
|---|---|---|
| 봇 회귀 17개(7777 진행 `msg_43b62fd66802`) | 0 | PASS=17 FAIL=0. 새 시나리오 accepted 3, AlreadyOnline 1, InvalidName 1, 잘못된 이름 원문은 서버 로그 0건 |
| WSL 서버 전체 | 0 | 1060 / 1055 / 0 / 5(병합 1058 + 검증자 새 시험 2) |
| Unity EditMode 전체 | 0 | 451 / 451(병합 446 + 검증자 5경우) |
| 서버 lane + PlayMode 전체(7777 진행 `msg_99802a855d48`) | PlayMode 2 | 47 / 46 / 1. 실서버 두 클래스 통과, 서버 로그에 실제 Unity 세 연결의 `version=19` handshake → `C_Login` → accepted → `C_CharacterSelect` |
| 실패 분류 재실행 `HudGoldAndPanelToggleSceneTests` 2회(진행 `msg_0a288182b645`) | 0·0 | 3/3·3/3 |
| 서식 검사 2회차 | 0 | 1회차는 Unity와 겹쳐 git 상태가 바뀌어 도구가 중단(위반 아님) |
| 생성기·Windows `--no-incremental` 빌드 셋 | 0 | 생성 코드·두 DLL이 커밋 bytes와 같음 |

- 검증자 새 시험(commit 대상): 서버 `GameServer.Tests/Network/LoginAndDungeonPacketRoutingTests.cs`(G1 한 세션의 로그인 41과 던전 40, 리터럴 bytes 2경우), EditMode `LoginAndDungeonResultDispatchTests.cs`(G2 클라이언트 dispatch 42와 39, 5경우). G3(버전 18 거부)는 기존 `InventoryProtocolContractTests.ProtocolVersion_IsNineteen_AndTheHandshakeRejectsEighteen`이 덮는다. 새 시험 RED는 없다.
- 결함:
  - **RV-01**(낮음, 차단, 귀속 병합 작업자, 리드 계약 표가 「버전 단정 19」로만 적은 점이 기여): `02_Server/GameServer.Tests/Maps/BossBehaviorTests.cs:475` 시험 이름 `ProtocolVersion_Is18`이 `:484` `Assert.Equal(19, ProtocolVersion.Current)`를 단정한다. 이전 버전 올림 둘은 이름을 같이 바꿨고 같은 병합의 다른 버전 시험 둘도 19 이름으로 바꿨다. CODE_CONVENTION 「주석과 문서」 「이름·공개 API·테스트에 의도와 계약을 표현한다」 위반이다.
- PlayMode 실패 1건: `HudGoldAndPanelToggleSceneTests.ClosedPanel_LetsAClickOnItsUseSpotAttack_AndAfterIThatClickUses_AcrossAMapMove`의 실시간 3초 대기 시간 초과(13.9714 > 13.9708). 병합이 이 시험·패널·`C_ItemUse` 경로를 바꾸지 않았고 단독 재실행 두 번 통과라, 검증자는 이번 병합 결함이 아니고 원인은 미확정이라고 분류했다(실패 결과 46/47은 덮지 않음).
- 설계 관찰(비차단): O-1 `DungeonClearPacketContractTests.cs:187` 메서드 주석이 로그인 단계를 말하지 않음, O-2 `FEATURE_MAP.md:26` 패킷 줄이 v18 던전 절만 링크(World 브랜치부터), O-3 `client.md:16`이 잘못된 길이의 끊기를 말하지 않음(반대 서술은 아님), O-4 HUD 시험 실시간 대기의 간헐 실패 위험, O-5 검증자가 서식 검사를 Unity와 겹쳐 돌림.
- 처리: RV-01과 O-1은 이번 goal의 변경이 만든 부정확한 이름·주석이라 한 수정 계약에 묶는다(신규 `gpt-6.1-sol` max, 약 등급 재검증 신규 `gpt-6-astra` xhigh, 리드 메모 E/`reverify-result-goal-context.md`). O-2·O-3은 후속 후보로 둔다. O-4는 그 시험 소유 파트에 알린다. O-5는 다음 계약에 「서식 검사는 Unity와 겹치지 않게」를 적는다.
- 실행 조율: 큰 실행은 Content(`msg_b9fdffeba9dc`)·Core(`msg_3f21046a6140`) 허용 뒤 묶음 진행(`msg_a1c970641968`)으로, 7777은 단계마다 리드가 listener 0을 확인하고 허용했다.
- 부수 효과: Unity가 `ProjectSettings.asset` SENTIS 한 줄과 `MinimapRT.renderTexture` 줄 끝을 다시 써서 리드가 되돌렸다.

### RV-01·O-1 수정 — 2026-10-11

- 작업자: 신규 `gpt-6.1-sol` max(최초 명령 `codex --model gpt-6.1-sol -c model_reasoning_effort=max`, 첫 화면 「GPT-6.1-Sol max · Full Access · never」, 선택창 없음, backend unknown), 태그 `[World Sol]`. 배정은 implementer-routing 「명세가 고정된 1~2파일 수정」이고 Astra 신호는 없다. RV-01의 확정 실패 집계 1회차 수정이다. 리드 pane 아래 split `term_e19f9a3d-5da7-4afe-9f95-5d59ccad20a5`, Task `task_f1537c2f8eeb`, Dispatch `ctx_a74a9627fbc0`. worker_done `msg_8be5bc8c68ef`(12:46:44Z, succeeded). release `retained` 뒤 pane을 닫았다.
- 계약: E/`rv01-contract.md` v1(SHA256 `7f3cef1531f7f4b0c2b12fef9bba0b5ab47b6b96626d24e4bb1b8c462e3f39fe`), 요약 E/`rv01-spec.md`, 입력 E/`rv01-inputs.txt`, 경로 확인 E/`rv01-preflight.txt`, 리드 메모 E/`rv01-prep-context.md`. 약 등급, 고정 HEAD `d6eb876d`.
- 바꾼 것: `BossBehaviorTests.cs:475` 이름 `ProtocolVersion_Is19`, `DungeonClearPacketContractTests.cs:187` 주석과 `:196` 단정 메시지에 로그인 단계. 단정 값·이력 주석·fixture 동작은 그대로다.
- 보고: E/`rv01/report.md`(SHA256 `a0f357853d80a241d9c94493e56074f9b7aac7fd5af39b99b20977fa31e96eff`), 사전 메모 E/`rv01/context-before.md`. 리드 대조 메모 E/`rv01-result-goal-context.md`.

| 실행 | exit | 결과 |
|---|---|---|
| WSL 필터 `BossBehaviorTests` | 0 | 29 / 29 / 0 / 0(변경 전 TRX와 같은 수) |
| WSL 필터 `DungeonClearPacketContractTests` | 0 | 8 / 8 / 0 / 0(같은 수) |
| `git diff --check` | 0 | 출력 없음, 2 files 3+ 3- |

- 리드 대조: 실제 diff가 계약 문구와 같고, 원시 stdout 요약 줄과 exit가 보고와 같다. WSL 전체·서식·Unity·7777은 계약대로 돌리지 않았다(재검증이 맡는다).

### RV-01·O-1 재검증 — 2026-10-11

- 검증자: 신규 `gpt-6-astra` xhigh(첫 화면 「GPT-6-Astra xhigh · Full Access · never」, 선택창 없음, backend unknown), 태그 `[World 검증자]`. 리드 pane 아래 split `term_6e847cf5-cee9-4db7-97a6-58b857099400`, Task `task_a604834faf39`, Dispatch `ctx_8b8f9f7d2278`. 첫 `worker-start`는 Codex가 뜨는 중이라 `agent_unconfigured`로 거절됐고 재시도로 받아들여졌다. worker_done `msg_99659e3625bb`(13:19:47Z, succeeded). release `retained` 뒤 pane을 닫았다.
- 계약: E/`rv01-reverify-contract.md` v1(SHA256 `c599121dc7ffbe8572ce1e319e76915a99481839f5748e6dd3c13c4059a00f8b`), 보충 1 E/`rv01-reverify-supplement-1.md`(SHA256 `1f371a75ceb64b746a317751154cc9387a6b586892e8d6d72109940741a7cb41`), 요약 E/`rv01-reverify-spec.md`, 입력 E/`rv01-reverify-inputs.txt`, 리드 메모 E/`rv01-reverify-prep-context.md`. 약 등급, 대상 `d6eb876d..269229b6`의 시험 두 파일.
- 판정: E/`rv01-reverify/verdict.md`(SHA256 `a231fba52815fe5016a33d9bc2eb1a8695a5572006ca77d1f87ac304963e9050`). **통과**. RV-01과 O-1이 해소됐고 수정의 새 결함은 0이다.

| 실행 | exit | 결과 |
|---|---|---|
| WSL 필터 `BossBehaviorTests`(TRX) | 0 | 29 / 29 / 0 / 0, `ProtocolVersion_Is19` Passed |
| WSL 필터 `DungeonClearPacketContractTests`(TRX) | 0 | 8 / 8 / 0 / 0 |
| WSL 서버 전체(TRX) | 0 | 1060 / 1055 / 0 / 5, `ProtocolVersion_Is19` Passed, skip 다섯은 이전과 같음 |
| 서식 검사 1회(13:03:24Z~13:16:45Z, Unity.exe 0) | 0 | 도구 자체 시험 244 / 244, 최종 검증 339 입력 |
| `git diff --check d6eb876d 269229b6` | 0 | 출력 없음 |

- 리드 원천 대조: 네 result.json의 exit와 stdout 요약 줄, 두 TRX의 outcome 수와 `Is19`, 서식 tests.log·final-validate.log, 보충 원문 대조 2/2, 작업 트리 clean을 직접 읽었다. 메모는 E/`rv01-reverify-result-goal-context.md`.
- 결함:
  - **RF-01**(낮음, 귀속 리드 계약): 계약 v1이 CODE_CONVENTION 「C# 공백 서식」과 「사람의 탐색과 가독성」 절의 일부만 실었다. 검증자 공식 질문 `msg_0f81a895ae0c`에 보충 1로 두 절 전체를 고정해 실행 전에 해소했다. 첫 발생이라 goal에만 기록하고, 검사 전환은 다음 계획 후보로 둔다.
- 설계 관찰과 처리:
  - RO-01: 인벤토리 시험 세 파일 네 줄이 실제 `LogIn` 호출 뒤에도 핸드셰이크와 직업 선택만 말한다(`InventoryWireContractTests.cs:431,440`, `InventoryLifecycleRaceTests.cs:288`, `InventoryMigrationLifetimeTests.cs:326`).
  - 리드가 같은 구현 commit `c5482d6b`의 `LogIn` 추가 지점을 모두 훑어 같은 종류 세 줄을 더 찾았다(`InstanceMapRetirementTests.cs:121,133`, `InstanceMapTestWorld.cs:139`의 한국어 메시지).
  - 일곱 줄 모두 이번 goal의 변경이 만든 부정확한 설명이고 CODE_CONVENTION 「이름·공개 API·테스트에 의도와 계약을 표현한다」에 걸린다. 그래서 O-1처럼 이번 goal에서 고친다(신규 `gpt-6.1-sol` max, 약 등급 재검증). 시험 문구 수정은 이번이 2회차다.
  - Content 브랜치 `2d168f19`는 `InventoryWireContractTests.cs` 250~265행만 바꿔 이 줄들과 겹치지 않는다.
- 실행 조율: 큰 실행은 Content(`msg_926ab980acb3`)·Core(`msg_78ca49c474f1`) 허용 뒤 13:45Z 전 시작 조건으로 진행했다. 이어 Core SQL 진단의 메모리 창을 받아들였다(`msg_16e499d4ecf6`). 서식 검사가 끝난 시각은 Core에 알렸다(`msg_08a95364c78b`). 13:11:48Z와 13:19:57Z에 7777 listener 0, Unity.exe 0, dotnet.exe 0이었다.
- 부수 효과: Unity를 쓰지 않아 되돌린 파일이 없다. 작업 트리는 clean이다.

### RO-01 수정 — 2026-10-11

- 작업자: 신규 `gpt-6.1-sol` max(최초 명령 `codex --model gpt-6.1-sol -c model_reasoning_effort=max`, 첫 화면 「GPT-6.1-Sol max · Full Access · never」, 선택창 없음, backend unknown), 태그 `[World Sol]`. 배정은 implementer-routing 「명세가 고정된 1~2파일 수정」에 가까운 고정 문구 교체다. 리드 pane 아래 split `term_6b5442e6-9427-4f85-9bc2-d3c439569922`, Task `task_d86a6f272327`, Dispatch `ctx_3f24e7cc45e1`. worker_done `msg_4e4ec5470644`(13:53:01Z, succeeded). release `retained` 뒤 pane을 닫았다.
- 계약: E/`ro01-contract.md` v1(SHA256 `4987b15f58119f3e76ce73ebf1df0c66c13c824e425673dc03252f0bb92ba4f7`), 요약 E/`ro01-spec.md`, 입력 E/`ro01-inputs.txt`, 경로 확인 E/`ro01-preflight.txt`, 리드 메모 E/`ro01-prep-context.md`. 약 등급, 고정 HEAD `ac5342bd`. CODE_CONVENTION 세 절은 처음부터 절 전체로 실었다(RF-01 재발 방지).
- 바꾼 것: 시험 fixture 설명 7줄에 로그인 단계를 넣었다. 영어 주석 `InventoryWireContractTests.cs:431`, 영어 단정 메시지 `InventoryWireContractTests.cs:440`·`InventoryLifecycleRaceTests.cs:288`·`InventoryMigrationLifetimeTests.cs:326`, 한국어 단정 메시지 `InstanceMapRetirementTests.cs:121,133`·`InstanceMapTestWorld.cs:139`. 단정 조건·fixture 동작은 그대로다.
- 보고: E/`ro01/report.md`(SHA256 `e8c9fe2a4b97aaa7dffc038b2b1634ca075468ba1991df0a6ad579cbe87ad83f`), 사전 메모 E/`ro01/context-before.md`. 리드 대조 메모 E/`ro01-result-goal-context.md`.

| 실행 | exit | 결과 |
|---|---|---|
| `git diff --check` | 0 | 출력 없음, 5 files 7+ 7- |
| WSL 서버 전체(TRX, 13:43:33Z~13:45:42Z) | 0 | 1060 / 1055 / 0 / 5(바꾸기 전과 같은 수) |

- 실행 조율: 작업자가 status `msg_271e88ea6d42`(여유 물리 6069984 KiB, 가상 4905080 KiB)를 보낸 뒤 공식 질문 `msg_ec670dd139aa`로 물었다. 리드가 진행 `msg_8507544a4549`와 답 `msg_38416c9100be`를 보낸 뒤 한 번 돌렸다.
- 리드 R-2 표본: `server-full.result.json`의 exit와 실행 수, stdout 요약 줄, TRX 건별 outcome 재집계(Passed 1055, NotExecuted 5), 실제 diff와 계약 문구, 사전 메모 생성 시각(13:37:50Z, 첫 소스 쓰기 13:38:20Z 전)을 직접 확인했다. 작업자가 처음 TRX 요약의 `notExecuted="0"`을 건너뜀 수로 읽은 파생 집계는 보존돼 있고, 시험은 다시 돌리지 않았다.
- 같은 종류 18줄을 더 찾았다.
  - 작업자 「남은 것」이 `Network/SessionStateMachineTests.cs:12,20,174`를 보고했다. 리드가 구현 commit `c5482d6b`의 시험·fixture·봇 파일 21개 diff를 모두 다시 보았다.
  - 찾은 줄은 세 파일 18줄이다.
    - `Network/SessionStateMachineTests.cs` 13줄(12·14·19·20·144·150·168·174·198·203·226·231·263): 다섯 시험에 `CompleteLogin`이 들어갔는데 클래스 문서·시험 문서·arrange 주석은 handshake와 class 선택만 말한다.
    - `Network/CharacterSelectHandlerTests.cs` 3줄(23·86·101): `BypassHandshake()`가 이제 로그인도 하는데 설명은 「handshake 우회」「handshake 통과 상태」다.
    - `99_Tools/headless-bot/Scenarios/ProbeBase.cs` 2줄(13·217): 대기 이벤트 목록에 이번 goal이 넣은 `_login`·`_disconnected`가 없고, `HandleExtraPacket` 설명이 기반 클래스가 처리하는 `S_LoginResult`를 뺀다.
  - 앞 훑기는 `LifecycleTestWorld.LogIn` 추가 지점만 찾아 `CompleteLogin`·봇 경로를 놓쳤다. 리드 계약의 누락이고 첫 발생이라 여기에 기록하며, 재발 방지 검사는 다음 계획 후보로 둔다.
  - 해당 없음으로 본 것: 통합·호스트·퀘스트 알림·세션 정리 시험, PlayMode fixture, `M2BasicMovement`(설명이 없거나 구현이 이미 고침), 클라이언트 연결 시험(시험 이름을 `AcceptedLogin_…`로 바꿈), `SessionStateMachineTests.cs:145`(지금도 맞음), Combat 시험의 `BypassHandshake`(이번 goal이 바꾸지 않았고 로그인하지 않음).
  - helper 이름 `BypassHandshake`·`SetupHandshakedSession`은 바꾸지 않는다. 다른 시험 네 파일이 같은 이름을 쓰는 관례라 옆 설명으로 로그인을 밝히고, 이름 정리는 다음 계획 후보로 둔다.
- 처리: RO-01과 같은 이유로 이 goal 안에서 고친다. 시험 설명 수정은 O-1·RO-01에 이어 3회차라 「3회를 넘으면 메인 체크포인트」 전이다. Content 브랜치(`2d168f19`)는 세 파일을 바꾸지 않는다. 재검증은 7줄과 18줄을 묶어 한 번만 한다.
- 부수 효과: Unity를 쓰지 않아 되돌린 파일이 없다.

### RO-01 나머지 수정 — 2026-10-11

- 작업자: 신규 `gpt-6.1-sol` max(최초 명령 `codex --model gpt-6.1-sol -c model_reasoning_effort=max`, 첫 화면 「GPT-6.1-Sol max · Full Access · never」, 선택창 없음, backend unknown), 태그 `[World Sol]`. 리드 pane 아래 split `term_e1a460b2-8804-4ed4-8c59-d24faade88a6`, Task `task_e64b4497da54`, Dispatch `ctx_ae89d165331c`. worker_done `msg_079ccecddfbf`(14:19:19Z, succeeded). release `retained` 뒤 pane을 닫았다.
- 계약: E/`ro01-rest-contract.md` v1(SHA256 `ddb5bb3c2bdb5a275688addb0da3c63e6dc77196a185a6280a1201d57872f16f`), 요약 E/`ro01-rest-spec.md`, 입력 E/`ro01-rest-inputs.txt`, 경로 확인 E/`ro01-rest-preflight.txt`, 리드 메모 E/`ro01-rest-prep-context.md`. 약 등급, 고정 HEAD `c7235bb0`. CODE_CONVENTION 세 절은 조립 스크립트 E/`ro01-rest-build/assemble.mjs`가 원문 행 범위를 그대로 붙이고 원문과 같은지 비교했다(E/`ro01-rest-build/assemble.stdout.json`, 다음 계획 후보 「규칙 절 발췌 기계 비교」의 이번 goal 한정 적용).
- 바꾼 것: 「RO-01 수정」 절에서 찾은 세 파일 18줄의 주석에 로그인 단계를 넣었다. `SessionStateMachineTests.cs` 13줄, `CharacterSelectHandlerTests.cs` 3줄, 봇 `ProbeBase.cs` 2줄(대기 이벤트 `_login`·`_disconnected`, 기반 클래스가 처리하는 `S_LoginResult`)이다. 식별자·단정·실행 코드는 그대로다.
- 보고: E/`ro01-rest/report.md`(SHA256 `4d4ce271ef9f34da99057f7ade828310723061d0cb453772e4206a3d570491d4`), 사전 메모 E/`ro01-rest/context-before.md`. 리드 대조 메모 E/`ro01-rest-result-goal-context.md`.

| 실행 | exit | 결과 |
|---|---|---|
| `git diff --check` | 0 | 출력 없음, 3 files 18+ 18- |
| WSL 서버 전체(봇 컴파일 포함) | 미실행 | 리드 계약 변경으로 다음 묶음 재검증에 넘김 |

- 계약 변경: 작업자가 직전 status `msg_b981175f2114`(14:11:45Z, 여유 물리 3214040 KiB·가상 5067060 KiB)를 보내고 공식 질문 `msg_0d533b7c8523`으로 물었다. 물리가 기준 4194304 KiB에 못 미쳤다. 또 Content 7777 봇이 14:15Z~14:25Z 시작 창을 잡았고(`msg_23a93cf2142c`), 사용자 마감 지시가 왔다. 그래서 리드가 전체 실행을 계약에서 빼고 다른 실행으로 메우지 말라고 답했다(`msg_8a10a801469b`). 컴파일과 전체 시험은 아직 확인되지 않았다.
- 리드 R-2 표본: 계약 표 18행의 「바꾼 뒤」 문구와 실제 줄이 18/18 같다(E/`ro01-rest-build/postcheck.stdout.txt`). `git diff --numstat`는 파일별 3/13/2다. 추가·삭제 줄에 XML 문서 주석을 깨는 `<`·`>`·`&`가 없다. 사전 메모 생성(14:08:03Z)이 첫 소스 쓰기(14:08:49Z)보다 앞선다. 전체 시험 결과 폴더가 없어 미실행 보고와 맞는다.
- 마감 점검(14:20Z, E/`wrapup-process-check.txt`): Unity.exe 0, Windows dotnet.exe 0, WSL dotnet 0, 7777 listener 0, Docker 컨테이너 0이다. World가 띄운 실행은 남지 않았다.
- 부수 효과: Unity를 쓰지 않아 되돌린 파일이 없다.

### RO-01 묶음 재검증 — 2026-10-13

- 검증자: 신규 `gpt-6-astra` xhigh(최초 명령 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh`, 첫 화면 「GPT-6-Astra xhigh」, Codex v0.162.1, backend unknown), 태그 `[World 검증자]`. 리드 pane 아래 split `term_1acba7eb-f589-40e4-8d5d-d88afb6805f0`, Task `task_a508b5e084ac`, Dispatch `ctx_65ef79f3a253`. worker_done `msg_72b80468dffc`(03:57:24Z, succeeded). release `retained` 뒤 pane을 닫았다. 메인에는 기동을 `msg_cf319620eb0c`로 알렸다.
- 계약: E/`ro01-reverify-contract.md` v1(SHA256 `5d13bb67baa34254cc7bc6f60e08836ad387b560210e214a81e9b97276ee4907`), 요약 E/`ro01-reverify-spec.md`, 입력 E/`ro01-reverify-inputs.txt`, 경로 확인 E/`ro01-reverify-preflight.txt`, 리드 메모 E/`ro01-reverify-prep-context.md`. 약 등급, 고정 HEAD `2ab753e7`, 대상은 `c7235bb0`의 7줄과 `10177d2a`의 18줄이다. CODE_CONVENTION 네 절은 조립 스크립트가 원문 행 범위를 붙이고 원문과 같은지 비교했다.
- 판정: E/`ro01-reverify/verdict.md`(SHA256 `3aef4bc93d09faa203bb2de72724e2dfb6280124b155953beb4a564aa1c1ff59`), **차단 RF-01**. 리드 대조 메모는 E/`ro01-reverify-result-goal-context.md`다.

| 실행 | exit | 결과 |
|---|---|---|
| WSL 서버 전체(TRX, 봇 빌드 포함, 03:37:46Z~03:40:00Z) | 0 | 1060 / 1055 / 0 / 5, `HeadlessBot` `Build succeeded.`(18줄의 첫 컴파일 확인) |
| 서식 검사(03:40:41Z~03:53:28Z) | 0 | 「Formatting checks passed」, tests.log 244/244, 339 inputs |

- 실행 조율: 메인 큰 실행 규칙(`msg_c1b1e75ea27e`)대로 시작 전 Content·Core·Rules 리드에 알렸다. Content 7777 봇은 04:50Z 이후로 잡혔다(`msg_a0ae40a472da`). 직전 여유는 서버 때 물리 11015304 KiB·가상 18836548 KiB, 서식 때 물리 10567024 KiB·가상 17196176 KiB였고, 두 번 모두 Unity.exe 0이었다.
- RF-01(낮음, 차단, 리드 계약·goal 귀속): `CharacterSelectHandlerTests.cs:86` 「handshake·로그인 우회 + tick 1회 → EnterGameWorld 람다 처리 완료 상태.」의 후반이 틀렸다.
  - `SetupHandshakedSession`은 OnConnected → `BypassHandshake`(handshake·`CompleteLogin`) → `Tick(1)`뿐이다.
  - 월드 입장 job은 `GameSession.EnterGameWorld`에서만 넣고, `EnterGameWorldIfReady`는 클래스 선택이 있어야 그것을 부른다. 바로 뒤 시험(:103)도 선택 전 `StatsSet=false`를 단정한다.
  - 이 후반은 수정 전에도 있었고 「RO-01 나머지 수정」 계약이 그대로 고정했다. 두 구현자는 계약 25줄을 정확히 따랐다.
- 비차단 관찰: RO-02(helper 이름)는 이미 다음 계획 후보다. RO-03·RO-04는 다음 계획 후보에 넣었다. RO-04 중 계약 B의 `HandleIncoming`은 실제 이름이 `HandlePacket`이다. 리드 계약 오기이고 행 번호로 대상이 정해져 결과는 바뀌지 않았다.
- 리드 R-2 표본(직접 확인):
  - TRX 건별 outcome을 node로 다시 셌다. 1060개 중 Passed 1055, NotExecuted 5(통합 시험 다섯)다.
  - 서버 stdout의 봇 빌드 줄과 Runtime workspace 키 `1f52305fcfe79cb0a036`(앞선 world-active 실행과 같음), 서버·서식의 exit와 시각을 보았다.
  - 검증자 사전 메모 생성(03:37:02Z)이 첫 실행 원시(03:37:44Z)보다 앞선다.
  - `git diff -U0 ac5342bd 2ab753e7`의 변경 25쌍이 모두 주석·단정 메시지다.
  - RF-01의 호출 경로를 직접 읽어 판정과 같은 결론을 얻었다.
  - 차이 하나: 판정이 링크한 `status-final.txt`가 없다. `final-audit.ps1` 10행이 빈 배열을 `Set-Content`로 넘겨 파일이 생기지 않은 것으로 보인다. 같은 내용(`status: []`, exit 0)은 `file-write-audit.json`에 있고, 리드의 `git status --porcelain`도 0행이다. 판정을 바꾸지 않는 근거 링크 오류로 둔다.
- 처리:
  - RF-01은 이 goal이 바꾼 시험 줄에 있고 독립 검증 통과를 막는 범위 안 결함이라 기본대로 이 goal 안에서 고친다. 리드가 86행 문장 전체를 실제 호출과 대조해 문구를 정하고, 신규 Sol이 한 줄을 고친 뒤, 신규 검증자가 약 재검증(WSL 전체·서식)을 한다.
  - 시험 설명 수정은 O-1·RO-01·RO-01 나머지에 이어 4회째라 「같은 산출물 수정이 3회를 넘으면」 메인 체크포인트를 알린다.
  - 강 승격: 시범 규칙은 「약에서 결함이 새면 해당 유형을 강으로 올리고 메인에게 알린다」이다. 검증자는 검토 대상이라고 알렸다(`msg_eb4d5bf6e7e4`). 리드 해석은 약 재검증이 결함을 잡았으니 샌 결함이 아니라는 것이다. 메인이 판단한다.
  - 반복: 리드가 바꿀 설명을 실제 호출과 끝까지 대조하지 않은 일의 두 번째 발생이다. 첫 발생은 「RO-01 수정」의 훑기 누락이고, 이번은 계약이 문장의 로그인 부분만 대조하고 월드 진입 완료 주장을 대조하지 않은 것이다. 교정 정본상 반복 규칙 후보이고 규칙 정본은 Rules 몫이라 메인에 반영 판단을 올린다.
- 부수 효과: Unity를 쓰지 않아 되돌린 파일이 없다.

### RF-01 수정 — 2026-10-13

- 메인 판단(`msg_eb45c17c73e9`, 04:05:57Z, 메인 운영 판단이며 사용자 결정이 아니다): 리드 체크포인트 보고 `msg_abb382909792`에 「1 진행, 2 약 유지」로 답했다.
  - RF-01은 이 goal 안에서 고친다. 시험 설명 수정 4회째 체크포인트는 이 메시지로 처리했다. 5회째가 생기면 다시 멈추고 알린다.
  - 강 승격은 하지 않는다. 약 재검증이 잡은 결함이라 「약에서 샌 결함」이 아니라는 리드 해석에 동의했다. 재검증 범위는 RF-01 줄과 같은 파일의 설명 주장 대조로 좁힌다.
  - 반복 규칙 후보는 「다음 계획 후보」에 두고, 이번 수정 계약에 RF-01 줄의 주장별 실제 호출 대조표를 먼저 적용한다.
- 작업자: 신규 `gpt-6.1-sol` max(최초 명령 `codex --model gpt-6.1-sol -c model_reasoning_effort=max`, 첫 화면 「GPT-6.1-Sol max · Full Access · never」, 선택창 없음, backend unknown), 태그 `[World Sol]`. 리드 pane 아래 split `term_3dcfe56c-6f97-4628-9a28-ca0b7ab991df`, Task `task_7d2c067190e9`, Dispatch `ctx_e690ae9d4be9`. worker_done `msg_00ebf698742f`(04:19:15Z, succeeded). release `retained` 뒤 pane을 닫았다. 메인에는 기동을 `msg_4dad1b8fa6bd`로 알렸다.
- 계약: E/`rf01-fix-contract.md` v1(SHA256 `954805b38bf52c0371092b43a05eba4c5f69e64ffc2ee87fef3b68762acde7eb`), 요약 E/`rf01-fix-spec.md`, 입력 E/`rf01-fix-inputs.txt`, 경로 확인 E/`rf01-fix-preflight.txt`, 리드 메모 E/`rf01-fix-prep-context.md`. 약 등급, 고정 HEAD `7e6e6436`. CODE_CONVENTION 세 절은 같은 조립 스크립트로 붙이고 원문과 비교했다.
- 86행 주장별 대조(계약에 실음): 「handshake·로그인 우회」는 `BypassHandshake`(57~61행)가 부르는 `CompleteHandshakeAndEnter`·`CompleteLogin`과 맞고, 「tick 1회」는 92행 `_map.Tick(1)`과 맞다. 「EnterGameWorld 람다 처리 완료 상태」는 틀렸다. 입장 job은 `EnterGameWorld`에서만 넣고 `EnterGameWorldIfReady`는 class 선택이 있어야 부른다. 같은 파일에서 이 goal이 바꾼 23·55·101행도 대조했고 맞다.
- 바꾼 것: `CharacterSelectHandlerTests.cs:86`을 `// handshake·로그인 우회 + tick 1회 → class 선택 전·월드 미진입 상태.`로 바꿨다. 「월드 미진입」은 같은 goal이 고친 `SessionStateMachineTests.cs:19`와 같은 말이다. commit `59ecd28f`.
- 보고: E/`rf01-fix/report.md`, 사전 메모 E/`rf01-fix/context-before.md`, 원시 E/`rf01-fix/execution-results.json`. 리드 대조 메모 E/`rf01-fix-result-goal-context.md`.

| 실행 | exit | 결과 |
|---|---|---|
| `git diff --check`·`--stat`·`--numstat`·`status --porcelain` | 0 | 1 file, 1+ 1- |
| 빌드·시험·서식 | 미실행 | 계약대로 다음 재검증자가 한 번 맡는다 |

- 리드 R-2 표본: 실제 `git diff -U0`가 86행 한 줄이고 문구가 계약 표와 같다. 파일 SHA256 `414d371825134b1f6f5191d4df665c2829e25838dee059cec342fe76c723a0d8`이 보고와 같다. 파일 수정 시각(04:15:24Z)이 사전 메모 첫 기록(04:14:59Z)보다 뒤다.
- 작업자 관찰: `:63~64`의 접근 설명이 실제 선언과 다르다. 이 goal이 바꾸지 않은 줄이라 다음 계획 후보에 넣었다.
- 부수 효과: Unity를 쓰지 않아 되돌린 파일이 없다.

### RF-01 재검증 — 2026-10-13

- 검증자: 신규 `gpt-6-astra` xhigh(최초 명령 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh`, 첫 화면 「GPT-6-Astra xhigh · Full Access · never」, 선택창 없음, backend unknown), 태그 `[World 검증자]`. 리드 pane 아래 split `term_341aedd9-aed4-44ee-87a1-daa2053d4c2c`, Task `task_c9214d24568d`, Dispatch `ctx_e02ad1388a18`, verifies `task_7d2c067190e9`. worker_done `msg_af4c1cb0e475`(04:55:13Z, succeeded). release `retained` 뒤 pane을 닫았다. 메인에는 기동을 `msg_d5d9c03e3c89`로 알렸다.
- 계약: E/`rf01-reverify-contract.md` v1(SHA256 `a2ee5a84023ebaf50d49c4a543a219529cc57eef2ee604668ad97d2d171ae89e`), 요약 E/`rf01-reverify-spec.md`, 입력 E/`rf01-reverify-inputs.txt`, 경로 확인 E/`rf01-reverify-preflight.txt`, 리드 메모 E/`rf01-reverify-prep-context.md`. 약 등급, 고정 HEAD `fccff3a1`. CODE_CONVENTION 네 절(119~126·71~82·128~141·162~172)은 같은 조립 스크립트로 붙였고 원문 비교가 모두 일치했다.
- 범위: 메인 판단 `msg_eb45c17c73e9`대로 86행과, 이 goal이 같은 파일에서 바꾼 설명 줄 23·55·101을 주장 단위로 나눠 실제 호출과 대조했다. 검증자가 대조표를 직접 만들었고 열한 주장이 모두 맞았다. 나머지 기존 설명은 범위 밖 관찰로 적었다.
- 큰 실행 조율: 계약 전에 Content·Core·Rules·Art 리드에 창 04:35Z~05:15Z를 알렸다(`msg_b222cd2cec4c`·`msg_2fb6772655be`·`msg_dd682195886f`·`msg_72deeeda1d30`). Content 7777 봇에는 World WSL 전체가 끝난 뒤 시작하라고 답했고(`msg_1bd2bc3c235d`), 끝난 뒤 알렸다(`msg_677e84479129`). 봇은 04:52:44Z~04:54:22Z에 돌아 World 실행과 겹치지 않았다(Content `msg_0bb5656575ef`).
- 조율 정정: 리드가 「서식 검사는 WSL·7777을 쓰지 않는다」고 알렸으나 틀렸다. Content 원시(04:49:13Z)로는 서식 검사가 WSL 안에서 dotnet 다섯 개(`Formatting.Tests`, `Formatting.dll compare`)를 돌렸다. Content가 WSL dotnet 0을 확인한 뒤 봇을 시작해 겹침은 없었다. 첫 발생이라 여기 기록만 하고, 다음 알림부터 「서식 검사도 WSL dotnet을 쓴다(7777은 안 씀)」로 적는다.
- 판정: E/`rf01-reverify/verdict.md`(SHA256 `afc1fcbfc31d152cd17d4ab90095912c251ecd578f17601ad72fda1811cd34b8`) **통과**. 범위 안 결함 0, 원천 RF-01 해소. 사전 메모 E/`rf01-reverify/context-before.md`.

| 실행 | 시각(UTC) | exit | 결과 |
|---|---|---|---|
| WSL 서버 전체(`sync-wsl.sh test`, trx) | 04:35:57~04:38:15 | 0 | 1060 / 통과 1055 / 실패 0 / 건너뜀 5, 대상 다섯 시험 통과, HeadlessBot 빌드 |
| 서식 검사(`format-check.ps1`) | 04:38:34~04:51:20 | 0 | 서식 자체 시험 244/244, 보존 304파일 변경 0, verify 차이 0 |
| `git diff --check 7e6e6436 fccff3a1` | 실사 중 | 0 | 출력 없음 |

- 두 실행 직전 여유 메모리: 물리 11068464 / 가상 14702532 KiB, 물리 9878108 / 가상 13432208 KiB. Unity.exe는 두 번 모두 0개였다. 리드 status는 `msg_d68b89be843c`·`msg_e05245126240`이다.
- 리드 R-2 표본(E/`rf01-reverify-lead-r2.txt`): TRX를 node로 다시 세어 Passed 1055·NotExecuted 5였다(SHA256 `a775319a…b73f`). 대상 다섯 시험 이름이 모두 Passed였고, stdout 19행에 HeadlessBot 빌드 줄이 있었다. 서식 exit 0·244/244, HEAD 유지·status 0행도 판정과 같았다. O-2의 `GetStatsForTest`는 저장소에 주석으로만 있었다.
- 범위 밖 관찰: O-1 helper 이름(이미 다음 계획 후보), O-2 `:24` 없는 getter, O-3 `:56` 월드 진입 시험 대상 설명. O-2·O-3은 다음 계획 후보에 한 줄로 넣었다.
- 미실행: 실화면(별도 단계), 봇 시나리오·DB·Unity.
- 부수 효과: Unity를 쓰지 않아 되돌린 파일이 없다.
