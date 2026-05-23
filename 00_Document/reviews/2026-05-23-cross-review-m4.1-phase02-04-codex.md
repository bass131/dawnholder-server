# Codex β cross-review raw — 2026-05-23 — M4.1 Phase 02·03·04

> 본 파일: Codex(β)가 본 Claude의 의뢰(`2026-05-23-m4.1-phase02-04-codex-prompt.md`)에 응답한 raw 출력. 사용자(전달자)가 별 세션에서 받아 본 Claude 세션으로 전달.
>
> γ 비교 분석은 [`2026-05-23-cross-review-m4.1-phase02-04.md`](2026-05-23-cross-review-m4.1-phase02-04.md) 참조.

---

## A. 옵션 B 변형 third path

- 헌법 #4 동등 보호 정합 여부: **부분 정합**
- 면접 어필 가치: **보강 필요**
- 보강 제안: 방향은 좋습니다. ServerCore 재사용성 때문에 production dependency를 `98_Shared`로 끌어오지 않는 선택은 설명 가능합니다. 다만 현재 drift guard는 서버 FrameValidator ↔ Shared.Constants ↔ PacketSession까지만 묶고, 클라 `04_ClientNet/FrameValidator.cs`까지 자동 검출하지는 못합니다. 그래서 "동등 보호"라고 부르려면 테스트 1개가 더 필요합니다.

추천:

- `FrameValidatorSymmetryTests` 같은 테스트가 서버/클라 helper를 둘 다 참조해 `MinFrameSize`, `MaxFrameSize`, 대표 입력별 결과를 비교.
- `04_ClientNet/FrameValidator.cs:5` 주석은 서버 counterpart를 `Session.cs`가 아니라 `02_Server/Network/FrameValidator.cs:1`로 정정.
- 면접 답변은 "공유를 못 해서 복붙했다"가 아니라, "production dependency는 분리하되 contract test로 wire invariant를 고정했다"로 말해야 진짜 힘이 납니다.

## B. Deterministic build

- PathMap 형식 정합: **정합, 단 주석 보강 권장**
- 다른 머신/CI 같은 hash 가능성: **중간**
- 알려진 함정: `<PathMap>$(MSBuildThisFileDirectory)=/_/</PathMap>` 자체는 MSBuild Csc의 PathMap 속성 용도와 맞습니다. Deterministic은 "입력이 동일하면 binary content가 동일"하다는 옵션이고, PathMap은 compiler output에 들어가는 physical path를 source path로 바꾸는 기능입니다.

다만 "같은 소스 + 다른 환경 = 같은 hash 보장"은 과한 표현입니다. Microsoft 문서상 deterministic 입력에는 compiler 정확한 버전, 참조 assembly, current directory, Source Link metadata, culture, encoding, analyzer 등이 포함됩니다. `global.json`도 `rollForward: latestFeature`라 팀원 SDK feature band가 달라지면 hash가 달라질 수 있습니다. CI까지 진짜 재현성을 원하면 `ContinuousIntegrationBuild=true`를 CI 조건부로 추가하는 쪽이 정석입니다. MS 쪽도 CI에서 경로 정규화를 켜라고 안내합니다.

또 하나: PathMap은 local debugging breakpoint 경험을 해칠 수 있다는 경고가 있습니다. Unity F12/embedded source 목적이면 감수 가능한 trade-off지만, 주석은 "local dirty 방지"와 "cross-machine reproducible"을 분리해 적는 게 좋습니다.

## C. Event-based race elimination

- race 차단 정합: **정합, 일부 잠복은 Low**
- enum 응집 cadence: **정합**

클라 흐름은 건강합니다. `03_Client/Assets/Scripts/Network/NetworkBootstrap.cs:82`에서 session 생성 직후 event를 구독하고, `03_Client/Assets/Scripts/Network/UnityClientSession.cs:198`에서 main-thread dispatcher 안에서 `HandshakeOk = true` 후 event를 발화합니다. `_characterSelectSent` guard도 있어 중복 fire 방어가 있습니다.

주의점은 "잘못된 순서 패킷은 전부 disconnect"가 실제 코드와 다릅니다. handshake 전 비-handshake는 disconnect지만, character select 전 MoveIntent/Attack은 silent drop입니다. 보안상 처리는 닫혀 있으니 P0는 아니지만, 문서/설명에서는 "disconnect" 대신 "처리하지 않음, 일부는 disconnect"라고 말해야 false-promise를 피합니다.

enum은 지금 당장 강제 리팩터링할 정도는 아닙니다. 다만 `_handshakeCompleted`, `_stats`, `_enteredWorld`, `_closing`, `_entityId`가 이미 합쳐져 상태를 만들고 있으니 다음에 account auth / map select / loading-ready 같은 pre-world gate가 하나 더 생기면 `SessionLifecycleState` enum으로 묶는 게 맞습니다.

## 보조

- false-promise 잠복:
    - `98_Shared/CLAUDE.md:19`: `Current=3` 문서, 실제 `98_Shared/Protocol/ProtocolVersion.cs:33`는 `Current = 4`.
    - `CLAUDE.md:101`: `Dawnholder.slnx`가 `02_Server/`와 `98_Shared/`만 묶는다고 되어 있지만 실제 slnx는 `04_ClientNet`, `99_Tools`도 포함.
    - `02_Server/CLAUDE.md:11`: `GameServer/Network`에 "ServerCore + GameSession"이라고 되어 있는데 실제 ServerCore는 `02_Server/Network/`.
    - `04_ClientNet/CLAUDE.md:12`: Layout에 새 `FrameValidator.cs` 누락.
- SubAgent 자율 commit 패턴: 정합, 단 prompt 계약 보강 권장. 결과 commit은 작고 검토 가능해서 결함은 아닙니다. 다음부터는 "commit 금지 / commit 허용"을 prompt에 명시하세요.

## 종합

- M4.1 Phase 02-04 풀세트 PR 머지 자신감: **조건부 GO**
- 즉시 봉합 권장 결함:
    1. `CLAUDE.md` / `98_Shared/CLAUDE.md` / `02_Server/CLAUDE.md` / `04_ClientNet/CLAUDE.md` false-promise sweep.
    2. 클라-서버 FrameValidator symmetry test 1개 추가.
    3. deterministic build 주석에서 "cross-machine hash 보장" 표현을 완화하고 CI 조건부 `ContinuousIntegrationBuild`를 backlog 또는 바로 추가.

코드 자체는 STOP급 결함은 안 보였습니다. 이번 건은 "실행 안정성"보다 "문서 약속과 자동 안전망의 마지막 10%"가 남아 있는 상태로 봅니다.
