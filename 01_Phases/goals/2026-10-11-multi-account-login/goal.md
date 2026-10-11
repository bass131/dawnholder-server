# 다중 계정 로그인 — 접속 때 계정 이름으로 세션에 계정 붙이기

## 재개 지점

World의 두 번째 goal이다. 접속할 때 계정 이름을 받아 세션에 계정을 붙이고, 같은 계정이 동시에 둘 들어오지 못하게 한다. 사용자가 범위 초안 v1을 그대로 승인했다(아래 「적용 중인 사용자 결정」). 기준·상태·결과는 이 파일에 모으고 [CURRENT](../../../00_Document/operations/CURRENT.md)는 이 목표를 가리킨다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/world-active`.
- branch: `feat/multi-account-login-20261011`(base main `6782cd674f03d33b989943b409d68883d0fb5b21`, PR228 병합 뒤). Orca가 만든 로컬 branch `bass131/world-active`는 원격에 올리지 않는다.
- 근거 폴더 E: `.backups/verification/2026-10-11-multi-account-login/`(Git 제외). 진입·범위 기록은 E/`lead-entry/`에 있다. 승인 범위는 [scope-draft-v1.md](../../../.backups/verification/2026-10-11-multi-account-login/lead-entry/scope-draft-v1.md)(SHA256 `1d73a1be…8540`), 조사 메모는 [research-notes.md](../../../.backups/verification/2026-10-11-multi-account-login/lead-entry/research-notes.md)(SHA256 `7cae47d1…ad08`)다. 리드 맥락 메모는 E/`lead-entry/context-memo.md`와 E/`goal-write-context.md`다.
- 리드: 신규 `claude-opus-5-5` xhigh(화면 「Opus 5.5 ⚡xhigh」, backend unknown), 태그 `[World 리드 Opus]`, handle `term_fddebc03-9972-41d8-8941-e7c98af2dabe`, Run `run_a927bbaff1f5`(회신 주소 `run:run_a927bbaff1f5`). 메인 주소는 메인 term handle이다. 작업자 태그는 `[World Sol]`(Astra 구현자는 `[World 구현 Astra]`), 검증자는 `[World 검증자]`다.
- **현재 위치**: 범위 승인 뒤 goal 첫 commit을 했다. 다음은 아래 「설계」 절 작성이다.
- **남은 순서**: 설계 절 → 선행 시험(신규 `claude-opus-5-5`) → 구현(신규 `gpt-6-astra` xhigh) → 독립 검증(신규 `claude-opus-5-5`) → 봇·실화면 확인 → 제품 PR·병합 승인 → Gardener → 종료 기록 PR → 종료 점검 → R-8.
- 다음 goal은 자동으로 시작하지 않는다.

## 진척 단계

- [x] 범위 승인
- [>] 설계 절 작성
- [ ] 선행 시험 작성
- [ ] 구현
- [ ] 독립 검증
- [ ] 봇·실화면 확인
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

리드가 작성한다. 다음 commit에서 이 절을 채운 뒤 선행 시험 계약을 발행한다.

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

## 현재 결과

### 착수 — 2026-10-11

- 리드 진입: Run `run_a927bbaff1f5`(06:24:46Z), READY `msg_7b0f365216d2`. 진입 지시와 우편함 원시는 E/`lead-entry/`에 있다.
- 조사: 읽기 전용 내부 에이전트 둘이 지금 접속 흐름과 영속화 계정 모델을 조사했고, 리드가 원문 줄을 표본 대조했다(E/`lead-entry/research-notes.md` 끝). 외부 실행이나 독립 검증이 아니다.
- 범위 승인 뒤 최신 main `6782cd67`에서 이 branch를 만들고 이 파일과 CURRENT의 World 줄을 첫 commit에 넣었다. 빌드·서버 시험·봇·Unity·DB는 실행하지 않았다.
