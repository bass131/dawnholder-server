# 캐릭터 목록·생성·선택 서버 처리

로그인한 계정의 캐릭터 세 칸을 조회하고, 이름과 직업을 정해 만들고, 고른 캐릭터로 마을에 들어가는 서버 처리를 연결합니다.
이름 검사와 실제 저장은 Core 기능을 사용하고, 화면 제작은 Content가 맡습니다.
선택한 캐릭터의 직업은 저장된 서버 값으로 결정하며, 같은 계정의 길드 가입 보상이 중복되지 않게 유지합니다.
Core가 저장 기능을 검증하는 동안 전송 형식과 대역 시험을 준비하고, 실제 연결 뒤 서버·Unity 양쪽을 검증하는 안입니다.
Core 준비 시점부터 약 3~5작업일을 예상하며, 10-23~24 연결 확인과 10-28 기능 동결을 목표로 합니다. 일정은 추정입니다.

**상태: 사용자 A 승인 완료·계약 고정 중.** 메인 `msg_fb6c227d3b39`가 초안 v0.1의 범위·완료조건·PR 경계와 지금 병행 착수 승인을 전달했다. Core 결과 전에는 계약·대역 시험을 준비하고 실제 저장 연결·최종 검증은 Core 결과 뒤에 한다. 신규 선행/독립 모델은 Sol xhigh로 확인됐다. Unity 정확 파일 소유·패킷 예약을 고정하기 전 해당 계약을 발행하지 않으며, 중량 실행 창은 아직 없다.

## 재개 지점

**다음 첫 일:** 승인 기록을 commit하고 Core의 공개 소비 접점·Content의 Unity 파일 소유·패킷 예약을 대조해 버전/hash를 가진 계약을 고정한다. Unity 소유 미합의 구간과 중량 실행은 보류하고, 영향 없는 서버/wire 대역 선행 시험 준비를 이어간다.

- 사용자 선택은 **A — 이 범위로 지금 병행 착수**다. 승인 당시 goal 172줄과 메인 정리본은 근거 `approval-01/inputs/`에 같은 bytes로 보존했다. 기존 이름·슬롯·직업 정책과 C1~C9·PR 경계는 바꾸지 않았다.
- 현재 작업 branch와 기준 main은 `.backups/verification/2026-10-18-character-server/entry-git.json`, 세션·Run·메시지 원시는 같은 근거 폴더에 있다. 이전 길드 goal의 Run·Task·Dispatch는 재사용하지 않는다.
- Core는 API v2의 미커밋 구현·독립 정적 회귀를 보고했고 실제 DB·저장재접속·강 판정은 남아 있다. Content의 Unity 소비자 소유권은 회신 대기다. API 문서·정적 회귀를 실제 DB/최종 통합 완료로 해석하지 않는다.

## 요구사항 원천과 적용 결정

- 진입·초안 작성 지시: 메인 `msg_bbdc401da68a`(2026-10-18T04:45:09Z). 메인 창에 제출된 다음 사용자 원문을 메인이 전달했으며, 이 pane의 직접 사용자 입력으로 격상하지 않는다.
  > 그리고 일단 지금은 빠르게 기반작업을 우선순위로 잡고, 기반 작업 마무리 되면 컨텐츠쪽 유지보수랑 추가사항 빠르게 찍어내자 / CORE: DB 테이블이랑 스키마 구성 / 나머지 파트 흐름 : 로그인화면 - 캐릭터 선택창(기존 캐릭터 선택 or 캐릭터 생성) -> 마을 진입(여기서 길드 거점 or 사냥터) -> 사냥터 진입 후 클리어 -> 재화로 길드 거점 성장 및 추가보상
- 같은 전달의 사용자 결정: 「대시보드 결정 응답: 1) 기반 작업 - 마을에서 들어가는 길드 거점을 지금은 패널로 둘지, 별도 맵으로 바꿀지 → A 지금은 패널 · 2) 기반 작업 - Core DB를 흐름 전체 표로 설계하고, 서버 연결은 어디까지 먼저 할지 → A 캐릭터부터 연결」.
- 메인 운영 판단: 서버 패킷 처리는 World, 화면은 Content, DB 표·저장 연결은 Core 목표다. 최초 허가는 범위 초안 작성이었고, 아래 사용자 A 승인 뒤 조건을 지켜 병행 착수한다.
- Core의 기존 승인 기본값은 `01_Phases/goals/2026-10-17-character-storage-foundation/goal.md`의 원천을 따른다. 해당 문서는 현재 별도 `core-active`에서 읽으며 아직 이 main에 통합되지 않았다. 계정당 세 칸, 생성 시 이름·기사/마법사 선택, 이름 NFC 후 2~12자·한글 완성형/ASCII 영문·숫자·서버 전체 유일과 ASCII 대소문자 무시다. World가 이 정책을 변경하거나 별도 정규화하지 않는다.
- 앞 리드 종료 원문: `.backups/verification/2026-10-14-guild-server/laptop-resume-20261017/closeout-01/final-closure-01/final-status.md`의 4항. PR250 병합 이후 새 리드로 진입했으며 역사 goal은 수정하지 않는다.
- 새 branch는 최신 main에서 만들었다. 상세 SHA는 진입 근거에 두고, 계약 발행 때 최신 통합 기준을 다시 고정한다.
- **사용자 범위·착수 승인:** 메인 `msg_fb6c227d3b39`(2026-10-18T05:35:50Z)가 전달한 사용자 원문은 「대시보드 결정 응답: 1) 계획 검토 - World World 다음 목표 - 캐릭터 목록·생성·선택 서버 처리 범위 승인과 착수 시점 → A 승인 (초안 msg_68890d486286)」다. 메인 pane 제출의 전달이며 이 pane 직접 입력으로 격상하지 않는다. 승인 대상은 초안 v0.1 172줄/hash `5ED02523858CCA4612046AA7F4FD007244FB0C2EDEB9CFF247E76962D1BB248A`와 사용자에게 제시된 A 「이 범위로 지금 병행 착수」다. 원문은 근거 `inbox-next-03.json`, 승인본과 메인 정리본은 `approval-01/inputs/`에 보존한다.
- **착수 조건:** Unity 최소 수신 소유와 정확 파일은 Content 쓰기 종료·합의 뒤 계약 전 고정, 49..54/v22는 최신 main/타 파트 예약 대조 뒤 고정, Core 미검증 코드 복사0, 중량 실행은 메인 「실행 창 시작」 뒤다. 범위 승인만으로 이 조건을 생략하지 않는다.

## 적용 중인 사용자 결정

### 신규 선행 시험·독립 검증의 임시 모델

Content가 보존한 메인 전달 전문 `msg_623c6ff92c9b`(2026-10-17T14:41:45Z)의 사용자 원문은 「대시보드 결정 응답: 1) Claude 사용량 - 주간 리셋(화 20:00)까지 새로 여는 선행 시험·독립 검증 세션을 Codex로 돌릴지 → A 지금부터 Codex로」다. A의 설명은 **새 선행 시험·독립 검증 모두 gpt-6.1-sol xhigh**, 기존 Opus는 맡은 작업을 끝내고 **2026-10-20 20:00 KST(11:00 UTC) 리셋 뒤 새 세션부터 원래 배정 복귀**다. 리드·구현자·독립성·태그·권한 경계는 바꾸지 않는다. 원문 보존본은 `approval-01/inputs/content-main-decisions-originals.md`다.

같은 전문의 `msg_e0ebcfcf3db5`(2026-10-17T14:42:35Z)의 추가 사용자 원문은 「대신 Sol의 성능이 빈얃하다고 느낄경우 Astra로 격상」, 「Astra로 격상시에는 Effort는 High로」다. 필수 관찰/실행 누락·원시와 다른 주장·반복 맥락 손실·범위 오해 쓰기 등 **이 작업의 실제 신호**를 리드/메인이 판단할 때 같은 작업의 **다음 새 세션만 gpt-6-astra high**로 올리고 근거를 보고한다. 실행 중 교체·다른 파트의 신호 차용은 하지 않는다.

승인 전달에는 선행 시험을 Opus로 적은 문구가 있어, 원문의 적용 대상이 선행 시험까지 포함됨을 `msg_6e1c369d08c8`로 메인에 보고했다. 메인 `msg_239b72df7331`(2026-10-18T05:39:31Z)은 이 문구가 임시 결정을 놓친 것임을 확인하고 **World 예외 없이 이번 신규 선행/독립 모두 Sol xhigh**로 정정을 수용했다. 초안의 Opus 선행/검증 설명은 위 한시 사용자 결정에 맞춰 정정했다. 리셋 뒤 발행분은 그 시점 원래 배정과 다시 대조한다. AGENTS 정본 개정은 아니다.

### 신규 계약의 우편함·자식 환경

다음 두 결정의 원천은 `msg_bbdc401da68a`가 전달한 사용자 현황판 응답이다. 공용 규칙·helper 정본 반영은 10-28 뒤 Rules 작업이며, 이번 목표에서 운영 도구를 만들지 않는다.

1. 「대시보드 결정 응답: 1) 운영 규칙 - 작업자가 우편함 확인을 다른 명령과 한 번에 묶는 일을 막는 교정을 언제 할지 → A 동결 뒤 Rules 첫 작업으로」. 이번 모든 신규 계약에는 아래 문장을 넣는다.
   > 우편함 check·inbox의 반환은 그 결과만 받는 별도 도구 호출로 읽고 해석한 뒤에 다음 행동을 시작한다. check와 실행 명령을 한 셸 호출이나 한 batch로 묶지 않는다.
2. 같은 전달의 「… 4) 운영 도구 - Windows PowerShell 5.1 자식 실행이 모듈 경로 함정에 걸리는 일을 막는 공용 helper를 언제 만들지 → A 동결 뒤 Rules 후보로」. 이번 모든 신규 계약에는 아래 문장을 넣는다.
   > PowerShell 7 안에서 Windows PowerShell 5.1(powershell.exe)을 자식으로 띄울 때는 띄우기 직전 그 자식의 환경에서만 PSModulePath를 지운다. 현재 세션·사용자·시스템 환경 변수는 바꾸지 않는다.

메인 전달 원문 전체는 근거 폴더 `inbox-before-run.json`에 보존한다. 첫 peek를 다른 읽기와 묶은 것은 이 결정을 수신하기 전이며 사전 메모와 READY에 공개했다. 수신 뒤에는 check·inbox를 단독 도구 호출로 처리한다. 공용 helper 구현이나 미래 세션의 기술적 강제를 완료했다고 주장하지 않는다.

## 범위

### 만들 것

1. 로그인한 계정의 캐릭터 **목록·생성·ID 선택** 요청을 처리하는 World adapter. 서버가 보유한 계정 문맥으로 Core `CharacterCatalogService`를 호출하고 불변 결과를 World 큐에서 반영한다.
2. 요청/응답 세 쌍의 전송 정의, 입력·길이 검사, 결과 코드의 의미별 매핑, 서버·봇·Unity의 양쪽 직렬화 계약 검증. 기존 번호·필드와 권위 직업 결과48은 보존한다.
3. 연결당 요청 하나, 요청 상관값·세대·원래 deadline, 닫힌 연결·늦은 완료 폐기, 선택 handle 종료를 명시한 수명 처리. Core host의 미결 생성 소유권을 사용한다.
4. 호스트 시작 시 고정된 Legacy/Stored 모드를 소비한다. 명시 Legacy만 기존 직업 선택 흐름을 허용하며 Stored 실패와 구버전 선택 요청으로 저장을 우회하지 못하게 한다.
5. 선택 완료 → 기존 권위 직업 결과48 → 실제 입장 → inventory/guild 등록의 보존과 실제 서버 경로 검증. 계정 단위 길드 가입 seed와 기존 경제·강화 수명은 유지한다.

### 건드릴 곳

아래는 제안 소유 경계다. 작업자 발행 전에 실제 최신 경로·함수와 Core/Content 쓰기 종료를 대조해 좁은 파일 목록으로 고정한다. 디렉터리 표기는 그 안의 전면 수정 허가가 아니다.

| 소유 | 제안 대상 | 경계 |
|---|---|---|
| World 제품 | `02_Server/GameServer/Handlers/Session/`, `Handlers/HandlerRegistry.cs`, `Sessions/GameSession.cs`와 같은 책임의 메뉴 adapter | 세션 경계·패킷 처리. 기존 세션에 모든 저장 책임을 합치지 않는다. 새 파일명은 책임에 맞춰 계약 때 확정한다. |
| World/Core 접점 | `Loop/GameWorld.cs`, `Program.cs`의 소비·주입 부분 | Core가 전달한 host mode/catalog/admission 접점을 받는 최소 변경만. Core는 Program/ServerHost/RuntimeStartup/AdmissionServices/Lifetime을 소유한다. Core 쓰기 종료·통합 뒤 소비하며 동시 쓰기 금지. |
| World 공유 계약 | `99_Tools/PacketGenerator/PDL.xml`, `98_Shared/Protocol/Generated/GenPackets.cs`, `ProtocolVersion.cs`, 같은 책임의 신규 공개 결과 enum | PDL에서 재생성한다. 생성기 자체 개선은 제외한다. 현재 generator는 manager 생성 생략이 기본이므로 존재하지 않는 manager를 추가하지 않는다. |
| World 시험 | `02_Server/GameServer.Tests/`의 패킷·세션·입장 회귀, `99_Tools/headless-bot/`의 좁은 메뉴 시나리오·등록 | 선행 시험/독립 검증자와 제품 구현자 소유권을 분리한다. 관련 기존 실패를 전수 분류한다. |
| Content 조율 | Unity `Network/UnityClientSession.cs`, 세션 결과 소비·상관값 경계, 관련 EditMode 시험 | **확정 소유권 미정**. 화면은 Content 소유다. wire 소비자 변경을 맡길 파트·파일을 조율 후 메인과 승인 기준에 반영하며 미합의 파일을 World가 쓰지 않는다. |
| DLL·양쪽 빌드 | `98_Shared/Shared.csproj`, `04_ClientNet/`의 현 빌드 경로, Unity `Assets/Plugins/Shared/`·`ClientNet/` 산출물 | 프로젝트 설정을 바꾸는 뜻이 아니다. 실제 재생성/복사와 양쪽 해시·소비자 버전을 확인한다. 기존 `.meta`·GUID·직렬화 값을 보존한다. |
| 리드 문서 | 이 goal, 승인 뒤 고정할 패킷 계약, `CURRENT.md`의 World 줄, 관련 기능·서버·protocol 계약 | 현재는 goal 초안과 CURRENT 자기 줄만 쓴다. 실제 계약 변화와 함께 필요한 현재 문서만 갱신한다. |

### 하지 않을 것

- DB schema·SQL·저장 repository·Core Catalog·복구 큐·host 미결 소유 로직의 구현. World는 전달된 접점을 소비하며 결함은 Core에 반환한다.
- 캐릭터 선택/생성 화면·씬·버튼·미술·전체 UI 흐름 제작. 화면 작업과 목표는 Content가 별도로 승인받는다.
- 삭제·이름 변경·비밀번호 인증·게임 중 캐릭터 교체·별도 취소 패킷, 계정 로그인 정책 변경.
- 재화·인벤토리·무기·길드의 영속 저장 연결, 신규 보상/성장 수치·seed 수명 변경, 별도 길드 맵.
- 패킷 번호 재사용, 기존 선택 패킷의 ID 선택 재해석, Stored 오류의 Legacy fallback, 무한 재시도·새 OperationId로 미확정 작업 재생성.
- 생성기 기능 확장·list/새 binary GUID codec, TRX 집계 helper·봇 목록 드리프트 검사·공용 운영 규칙 정비. Gardener 후보는 이번에 채택하지 않는다.
- 무관한 코드 정리, 전역 설정 변경, 승인 밖 DB·프로세스·Unity 실행, World의 병합 실행·자동 병합 예약.

### 관찰 가능한 완료조건

| 번호 | 조건 | 필요한 관측 |
|---|---|---|
| C1 | 목록은 빈 계정/1명/3명·슬롯 순서를 정확히 반환한다. 조회만으로 게임 행이나 player/owner를 만들지 않는다. | Core 대역 호출·결과/부작용0, 실제 Core 연동 빈 계정/생성 뒤 조회. 3명 초과·중복 슬롯·잘못된 class는 정상 목록으로 자르지 않는다. |
| C2 | 생성은 서버 계정 문맥과 Core PrepareCreate의 descriptor를 사용한다. Core 오류의 의미를 보존하며 World가 이름 정규화/유일성 검사를 복제하지 않는다. | 입력 경계·이름/슬롯/직업 한도, Core 거부 매핑·동시/중복 요청, dispatch 전 host 소유 등록을 통과한 접점. 생성 성공만으로 입장/길드 등록0. |
| C3 | 선택 ID는 그 계정 소유 캐릭터만 허용하고 저장된 class로 입장한다. | 없는ID/타계정ID의 같은 외부 거부·다른 내부 진단, 성공 응답 → 48 → 입장 → 길드 순서, 저장 class와 상반된 로컬 class가 권위에 영향0. |
| C4 | 요청중/입장중/게임중/닫힘과 재접속 세대를 구분한다. | 동시 추가 요청이 원래 pending을 손상시키지 않음, 중복 requestId가 Core 새 작업을 만들지 않음, 종료 직전/직후 handle 수령·늦은 완료에서 player/inventory/guild 등록0. |
| C5 | 미확정 결과를 확정 성공·실패로 바꾸지 않고 원래 ID/descriptor/deadline을 보존한다. | Unknown/StillUnknown·송신 실패·닫힘 시험. 최종 미확정 안내 뒤 종료와 Core host 소유 지속, 새 5초·새ID·임의 rollback0. 실제 DB 복구 내부 안전성은 Core 독립 판정도 필요하다. |
| C6 | 시작 모드가 고정되며 Stored 실패의 우회 입장이 없다. | Legacy 정상 흐름, Stored 초기화 실패·실행 오류·기존 직업 선택 요청, 허위 빈 성공 목록/fallback0. |
| C7 | 새 wire가 양쪽에서 동일하게 해석되고 기존 ID1..48이 보존된다. | byte 고정 프레임·최대길이·잘린/홀수/과대/잘못된 UTF-16·reserved/중복ID·명시 LE·GUID D형, PDL 재생성 diff·정확 버전 handshake·서버/봇/Unity 소비·실제 DLL hash와 Unity 컴파일. |
| C8 | 기존 길드·개인·공동 상태의 수명을 유지한다. | 같은 계정의 다른 캐릭터 재접속으로 seed 추가0, 실제 입장 이후 등록, 기존 개인 연결 수명/공동 프로세스 수명·던전 보상·강화 회귀. |
| C9 | 신규 독립 검증자가 강 등급의 diff·근거·시험·현재 실제 경로 실행을 판정한다. | 원시/새 독립 시험/실제 서버 수신→Core adapter→응답/입장 경로, 실제 Core DB 저장→재접속 접점, 양쪽 소비자 확인. 미실행은 판정 보류이며 문서·대역만으로 전체 통과하지 않는다. |

## 패킷·API 계약의 현재 제안

기준 main의 제품 코드는 protocol21·PDL48개다. `domains/protocol.md`에는 역사 v20 설명이 남아 있으므로 현재 값은 실제 `ProtocolVersion.Current`와 PDL/생성 enum을 기준으로 한다. 이번 제품 변화 시 현재 설명을 함께 맞춘다.

| 제안 ID | 제안 이름 | 핵심 내용 |
|---|---|---|
| 49/50 | `C_CharacterListRequest` / `S_CharacterListResult` | requestId와 고정3슬롯·occupiedMask |
| 51/52 | `C_CharacterCreateRequest` / `S_CharacterCreateResult` | slot·rawName·class와 Core가 만든 summary |
| 53/54 | `C_CharacterSelectRequest` / `S_CharacterSelectResult` | CharacterId 선택과 선택 결과. 직업 입력 없음 |

**49..54와 차기 protocol22는 제안값·미승인·미예약이다.** 최신 main·타 파트 예약을 다시 대조하고 사용자 범위 승인 뒤 계약으로 고정한다. 충돌하면 기존 ID를 재사용하지 않고 메인과 조정한다. 기존 ID/필드 불변, equality handshake 유지, 서버·봇·Unity/DLL을 같은 wire로 맞춘다.

- 보존 패킷 초안은 이전 근거 `character-packet-draft-01/packet-contract-v0.2.md`다. hash는 이번 `input-manifest.json`에 실제 파일에서 계산해 보존한다. 정확 필드·길이·결과값은 승인 후 추적 패킷 계약으로 고정하며 현재 파일을 조용히 바꾸지 않는다.
- 전송 제안: 4바이트 header·숫자 explicit LE·UInt16 **바이트** 길이+UTF-16LE string, ID는 non-empty GUID D문자열, 목록은 고정3칸이다. 큰 문자열 할당/업무 호출 전에 구조·상한을 검사한다. 새로운 wire enum은 Core ordinal cast 대신 의미로 매핑한다.
- 서버의 계정/operation/session/generation/owner/fence/SQL은 wire에 싣지 않는다. NotFound/NotOwned는 외부 선택불가로 통합하고, 서버가 만든 계정·OperationId 입력 오류는 내부 진단을 보존한 IntegrityFailure로 구분한다.
- requestId는 nonzero·연결 내 재사용 금지, uint 끝에서 wrap하지 않고 새 연결을 사용한다. 화면의 `(연결, 종류, requestId)` 상관 처리 구현은 Content와 고정한다.
- 선택 성공 응답은 플레이 준비 완료가 아니다. Stored 성공은 **54 → 48의 저장 class → 실제 맵 입장 패킷 → 길드 가입/44** 순서다. 실패는 성공48/입장/등록을 만들지 않는다.
- Core API v2는 PrepareCreate/불변 descriptor, dispatch 전 host 미결 생성 소유권, 불변 Legacy/Stored를 고정한 **구현 계약**이다. 실제 코드·DB·독립 강 검증 완료 여부는 회신/새 근거로 확인한다.

## 파트 접점과 선행 조건

| 담당 | 필요한 인계 | 현재 상태/World 행동 |
|---|---|---|
| Core | API v1+v2의 의미·버전/hash, mode/catalog/admission 주입 타입, host 미결 생성/close 소유, 새 profile preflight, 실제 DB/독립 판정·PR 기준 | `msg_6e76b2e31572` 질의 → `msg_46c17f907970` 회신. v2 hash 동일, 미커밋 구현·정적 회귀 보고. 실제 SQL/저장재접속/강 판정·schema5 카드/관리자 승인·main 통합 대조는 남았다. |
| Content | 신규 UI 목표 시점, Unity wire 소비자/상관값 경계 소유, 새 패킷 제안 이견·번호 예약 여부 | `msg_2b05a5289e20`로 문의했다. 현재 길드 UI/실행을 중단하거나 새 화면 착수를 지시하지 않는다. 미합의 쓰기와 UI 완료조건 확대를 하지 않는다. |
| 메인 | 사용자 범위·착수 승인, 두 파트의 공유 파일 경계, 중량 실행 창, R-7 시범 해당 여부 | A 승인 완료. R-7 미적용 확정. 공유 실행 큐는 전달 기준 Art → Content → Core DB. World의 새 실행 창은 아직 없다. |

Core main 통합 전에는 고정 API 대역으로 패킷/수명 요구를 준비할 수 있다. 제품 branch에 Core 미검증 변경을 복사해 독자 저장 구현을 만들지 않는다. 최종 통합 때는 승인된 Core 결과와 최신 main에서 새 기준·충돌·실행 계약을 고정한다.

Core 회신 기준 실제 소비 접점은 `CharacterCatalogService(ICharacterCatalogRepository, ICharacterRepository, SafeDefaults, TimeProvider, int capacity=128)`, `CharacterAdmissionServices(CharacterCatalogService, TimeProvider)`의 `Mode/Catalog`, `ServerHost.StorageMode/CharacterCatalog`다. Startup의 profile2 preflight 뒤 Program이 host 생성 전 `world.AttachPersistence(Services)`를 호출한다. 선택 handle의 `Completion/Close(SafeCheckpoint?)`, lease의 `Checkpoint`, host의 `FlushAsync`를 사용한다. Core는 Sessions/GameWorld/PDL을 변경하지 않았다고 보고했다. World는 이 보고를 계약 입력으로 삼되 새 통합판에서 직접 대조한다.

Core는 생성 `_pending.Add` 뒤 dispatch, 기본 capacity128, host 복구는 원ID/descriptor로 1회·최대5초라고 보고했다. 이는 Core 코드 관측 보고이며 실제 SQL 실패 수명 증명은 아니다. 사용자 deadline과 별도 복구 예산을 혼동하지 않는다. **초기 전달의 「Core 실제 DB 검증 중」은 현재 상태가 아니다.** 회신 시점에는 관리자 별도 승인 전·DB 실행0이며 실제 인계 시점은 미정이다. 정적 시험 수치 원시를 World가 다시 실행/실사한 주장은 하지 않는다.

## PR 경계와 예상 단계

**제품 PR 한 개**로 전송 정의·버전·서버 adapter·봇/시험·양쪽 wire 소비자와 DLL 근거를 함께 검토한다. PDL/버전만 따로 병합해 기존 클라이언트가 먼저 끊기는 중간 배포는 하지 않는다. Content 화면 PR은 별도 목표이며 이 PR에서 전체 화면/플레이 완료를 주장하지 않는다. Core 제품 PR은 Core 소유다.

Content의 최소 wire 소비자를 별도 제품 PR에 둘 필요가 생기면, 양쪽 준비 전 배포되지 않는 합류점·완료조건을 메인과 먼저 재계획한다. 아직 PR 번호는 없으며 생기면 진척 단계에 실제 `PR번호 병합`을 적는다. 종료에는 규칙에 따른 Gardener·결과 포함 종료 기록 PR 하나·종료 점검을 별도로 둔다.

| 단계 | 예상 소요 | 선행·점검 |
|---|---|---|
| 범위 승인·파트 계약 | 0.5~1작업일 | API/번호/소비자 소유·실행 조건 고정. 승인 초안과 대조 뒤 다음 단계. |
| 새 선행 시험 | 0.5~1작업일 | 코드 goal TDD. 별도 세션에서 요구·보존 시험과 구현 전 실패 원시. 발행 시 임시 모델 결정을 적용한다. |
| 전송·World 연결 | 1~2작업일 | 고정 대역과 Core 인계 기반. wire/수명·오류·등록 경계, 쓰기 종료 확인. |
| 강 독립 검증·수정 | 1~2작업일 | 신규 Opus의 양쪽 소비자/실제 경로/DB 접점. 범위 안 결함 수정·재검증은 새 세션. |
| PR·CI·통합·종료 | 0.5~1작업일 | 각 PR 사용자 head 승인·메인 병합. Gardener·종료 기록/점검. |

독립 가능한 준비를 겹친 **총 경과 3~5작업일은 일정 추정**이며 각 행을 단순 합산한 작업량과 다르다. Core가 10-20~21에 검증 결과를 전달하고 World/Content wire 소유가 일찍 정해지면 10-23~24 연결 검증을 목표로 한다. 10-25~27은 수정·리허설 여유로 보고 10-28 기능 동결 전 완료 여부를 점검한다. Core 지연·공유 자원 창 부족·강 검증 보류는 이 일정을 밀 수 있으며 10-21 촬영 완료를 약속하지 않는다.

중간 점검은 계약 고정 뒤와 최종 Core 통합 뒤다. 완료조건을 막는 범위 안 결함만 고치며, 같은 산출물 수정이 3회를 넘으면 메인 체크포인트를 알린다. 별도 확정 실패 집계와 섞지 않는다.

## 검증과 위임 계획

- **등급: 강.** 서버 권위·영속 경계·프로토콜/공유 DLL·입력 해석·시간/종료 수명을 바꾸기 때문이다. 현재 제품 diff0이며 변경 파일/줄 수는 아직 미측정이다. 계약/구현 뒤 실제 diff 원시로 기록한다.
- 구현자는 난이도 신호1~4에 따라 **신규 gpt-6-astra xhigh**다. 선행 시험과 독립 검증은 원래 신규 Opus 배정이지만 **위 한시 결정 기간에 새로 여는 세션은 각각 신규 gpt-6.1-sol xhigh**를 적용한다. 제품·시험 소유를 분리하고 발행 시각과 실제 화면 모델을 기록한다. 전송·서버 수명·Unity 소비자는 정확 소유권을 고정한 좁은 계약으로 나눈다. 리드는 제품 구현·테스트 작성·검증 판정을 대신하지 않는다.
- R-7의 실패 수명/오류 분류/비용 상한에 닿지만 **이번 목표의 시범은 미적용**이다. 메인 `msg_355be9930592`와 `msg_fb6c227d3b39`가 시범 한도 사용을 근거로 결정했다. Fable 세션을 열지 않으며 확정 실패3회 뒤 Advisor 규칙은 그대로다.
- 계약에는 사전 맥락·실제 경로 검사·CODE_CONVENTION 관련 절 원문·고정 입력/hash·원시 수치·쓰기 종료·한 작업 뒤 정산/종료·재사용 금지와 위 사용자 결정 두 문장을 넣는다. 같은 파일을 동시에 쓰지 않는다.
- 실행 순서안은 정적 입력/PDL/버전 대조 → 신규 선행 시험 실패 → 구현/생성 → SDK10.0.301 서버/Shared/ClientNet 빌드·회귀 → 현재 서버와 봇 경로 → 승인된 Core DB 저장/재접속 접점 → 격리 Unity6000.6.4f1 소비자/컴파일·관련 EditMode/회귀다. 정확 명령·입력·환경·hash·메모리·프로세스/포트·DLL 복사·정리 관문은 계약 발행 때 고정한다. 이 표는 실행 허가가 아니다.
- Unity·WSL 전체 시험·서버·DB 실증은 메인의 **「실행 창 시작」** 배정 후 승인된 단계 묶음으로만 실행한다. 원본 자산·DLL 부작용과 WSL/Windows 산출물 차이를 명시한다. 다른 파트 실행 창을 사용하지 않는다.
- 신규 독립 검증자는 실제 diff·보고·원시부터 실사하고 독립 테스트를 작성/보완·실행한다. 대역으로 실행한 실제 서버 진입, 실DB 접점, Unity 컴파일/시험, 실제 UI 플레이·Player build·CI를 각각 구분한다. 전체 PASS는 완료조건별 근거가 있을 때만 가능하다. DB/UI/Player 미실행을 과거 결과로 채우지 않는다.
- 이전 길드 검증의 ENV-D01 한 줄 설정 차이는 당시 한정 수락이다. 새 실행에 자동 적용하지 않으며 정확 변경·원본 보존·실행 wrapper/실제 결과와 미입증 범위를 기록해 메인에게 판단받는다.

## 진척 단계

- [x] 범위 초안
- [x] 범위 승인
- [>] 계약 확정
- [ ] 선행 시험
- [ ] 전송·서버 연결
- [ ] 독립 강 검증
- [ ] 제품 병합
- [ ] 종료 기록
- [ ] 종료 점검

## 결정과 단계 결과

- 새 리드의 읽기→사전 맥락→새 branch/Run→READY를 수행했다. 진입 시 추적 변경0, 최신 main과 새 HEAD의 diff0을 확인했다. 사전 맥락은 `.backups/verification/2026-10-18-character-server/context-entry.md`, 원 지시는 `inbox-before-run.json`이다.
- Core/Content에는 준비 상태와 기술 경계만 문의했고, 둘 다 현재 작업 중인 화면을 확인해 터미널 입력을 넣지 않았다. Core 회신으로 실제 준비·host 접점·미실행을 구분했고 Content는 회신 대기다. 작업자·검증자0, 제품/시험/PDL/DLL 변경0, 중량 실행0이다.
- 이 초안 자체점검은 내용·로컬 원천·현재 Git/PDL의 정적 대조다. 독립 문서 통과·코드/DB 검증·목표 완료를 뜻하지 않는다. 승인된 산출물의 독립 세션 절차는 생략하지 않는다.
- 사용자 A 승인 뒤 원본 입력을 동결하고 승인 원문·R-7 미적용·임시 모델 원문과 진척 단계를 반영했다. 승인 기록 commit과 실제 준수·자기 쓰기 감사는 근거 `approval-01/`에 둔다. Content 회신의 합의 방향은 근거 `coordination-note-01.md`이며 Unity 파일 쓰기 배정은 아직 없다.

## 보류와 남은 우려

현재 보류는 Core 실제 DB/최종 강 판정·Content 최소 wire 소유·실행 창이다. Core 준비 없이 실제 DB·종료 소유를 확인할 수 없고, Unity 소비자 없는 버전 변경은 통합을 차단한다. 이 두 가지는 제품 완료조건에 직접 영향을 준다. 범위 승인과 임시 모델 확인은 완료됐으며 영향 없는 서버/wire 계약·대역 시험 준비는 진행한다.

TRX helper와 봇 등록/회귀 목록 드리프트는 앞 목표 Gardener의 미채택 후보이며 새로 채택하지 않는다. 기존 BACKLOG와의 통합 판단은 메인에게 남긴다. 범위 밖 후보 장부나 운영 규칙 정비를 이번 초안에서 늘리지 않는다.
