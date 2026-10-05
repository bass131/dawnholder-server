# 아이템·인벤토리·재화

상태: **PR180 병합 완료(`8d1e885`). PR2 결함1~3은 독립 해소됐고, HUD 겹침 결함4는 두 번째 Sol이 한 파일 +12/-3으로 수정·자체 EditMode404/404·PlayMode8/8을 마쳤다. 2026-10-05T14:33Z 새 Opus 리드가 같은 Run을 이어받았다. 결함4 후속 신규 독립 Opus `task_bce6064a15f5`는 batch 범위 판정으로 마감했다(새 차단 결함·회귀 0, #4는 640×480에서 해소 관측). 802×451·1920×1080 MCP 실화면은 아침 새 Opus가 PR head에서 확인한다. PR2 커밋·main 통합·PR·CI를 밤사이 체크포인트로 만든다. PR2 전체 통과·병합과 goal 종료는 미완료다.**

- 담당: Content Astra. 시작 기준 `origin/main` = `955002a932925ff2c4ac81f4a5a99f2024a4b9b2`.
- 작업 공간: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/content-active`, 현재 PR2 branch `feat/items-inventory-ui-20261005`(base `8d1e8856a99e9a5ed74aa291294accd2299aaaf6`). PR1 branch는 `feat/items-inventory-currency-20261005`였다.
- 착수 원천: 메인 Claude `msg_53fec7129c14`(2026-10-04T14:56:48Z). 메인이 전달한 사용자 결정이며 사용자 직접 입력으로 격상하지 않는다.
- 범위 확인: 메인 `msg_763b1fa21bf5`(2026-10-04T15:04:28Z). 4항목·PR 경계 및 사용/세션 정리/기존 계약 보존이 승인 범위 안임을 확인했고 goal commit·Fable 진행과 신규 Opus 선행 TDD 구성을 허용했다.
- 목표의 기준·상태·결과는 이 파일에 둔다. 로컬 원문·계약·판정은 `.backups/verification/2026-10-05-items-inventory-currency/`에 보존한다.

## 범위

| 항목 | 승인된 범위의 구체화 |
|---|---|
| 만들 것 | 서버 권위의 아이템·인벤토리·재화 메모리 상태. 처치 보상 획득, 소유 아이템 사용, 서버 결과 표시. 새 패킷과 최소 클라이언트 인벤토리 UI. 저장은 GameDev가 소유할 인터페이스를 연결할 자리만 둔다. |
| 건드릴 곳 | 신규 `02_Server/GameServer/Items/`·`Handlers/Inventory/`, `98_Shared/GameData/Items/`; PDL·생성 패킷·프로토콜 버전과 양쪽 핸들러; `03_Client/Assets/Scripts/`의 인벤토리 상태·UI·핸들러. GameSession·GameWorld·HandlerRegistry는 GameDev와 순서를 합의한 최소 등록/처치/세션 정리 연결만. 완료조건을 입증하는 서버·프로토콜·봇·Unity 테스트와 해당 기능 문서. |
| 하지 않을 것 | DB 저장·로드·migration 구현, 상점·연구, 거래·우편, 장비 외형 아트. 후속 던전·길드·거점 목표, 프로토콜 생성기의 별도 ID 기능, 범위 밖 정리·전역 설정 변경. Unity scene/prefab 저작은 메인에 요청한다. |
| 관찰 가능한 완료조건 | 봇과 실제 클라이언트에서 처치 → 획득 → 인벤토리 표시 → 재화 증가가 확인된다. 소유 아이템 사용은 서버가 판정하고 표시가 서버 결과에 일치한다. 같은 보상의 중복 반영과 음수 재화를 서버가 거부함을 독립 테스트로 증명한다. 틱 I/O 대기 없이 동작하며 기존 전투·처치·파티·맵 이동·종료 계약을 보존한다. |

팀원 명세는 입력이며 착수 관문이 아니다. 데이터는 교체하기 쉬운 자리표시 정의에 모으고 임의 수치를 최종 기획으로 표현하지 않는다. 재접속 후 유지·서버 재시작 복원은 GameDev 저장 goal의 완료조건이다.

### PR 경계와 점검

1. **데이터·서버 규칙·패킷**: Shared 아이템 정의/데이터, 서버 경제 상태와 규칙, 처치 보상·사용·조회 연결, 양쪽 직렬화/버전 및 봇 시나리오. 서버의 불변식·거부 경로, 실제 서버↔봇 경로, 기존 회귀를 독립 검증한다. Unity 공유 DLL/패킷 소비자 호환성을 확인하며 인벤토리 화면 완성은 PR2다.
2. **클라이언트 인벤토리 UI**: 서버 상태 미러, 수신 핸들러, 최소 인벤토리/재화 표시와 사용 입력, 세션 정리·씬 수명 연결. **맵 전환 완료 뒤 재조회로 이동 틈의 처치 보상 표시를 복구하는 것을 필수 완료조건과 테스트에 포함한다**(메인 판단 `msg_b554a4b55c29`, 사용자 결정 아님). 독립 Unity 테스트와 실제 플레이로 전체 완료조건을 확인한다. PR1 사용자 병합 승인 뒤 최신 main에서 이 goal의 후속 브랜치를 만든다. PR2가 미뤄지거나 빠지면 표시 복구 한계가 남는 사실을 메인에 보고한다.

각 PR은 신규 Opus 검증, 정확한 head의 CI와 원시 실행 근거를 메인에 보고한 뒤 **그 PR 병합 직전 사용자 명시 승인**을 받는다. 자동 병합하지 않는다. 중간 점검은 PR1 결과/승인 시점, 종료 점검은 전체 PR 병합·결과·Gardener 뒤다. 같은 산출물 수정이 3회를 넘으면 메인에 체크포인트를 알리며 확정 실패 집계는 별도 ORCA 정본을 따른다.

## 설계 초안과 보존 계약

이 절은 Fable 검토와 GameDev 조율에 제시할 기술 초안이며 구현 완료나 불변식 검증 결과가 아니다.

- **상태 소유**: 경제 상태는 서버 틱 흐름의 단일 소유자에 둔다. 세션 입력은 기존 gate와 큐를 거치며 다른 세션/종료된 세션/이전 수명의 요청이 새 상태를 변경하지 못해야 한다. 맵 이동 동안 보존하고 연결 수명 종료의 정리·향후 저장 인계 경계를 드러낸다.
- **획득 경로**: 클라이언트가 보상량·처치 사실을 결정하지 않는다. 실제 권위 처치 callback에서 대상 수명·수혜자·중복 반영 방지 상태를 함께 다룬다. `GameWorld.MakeMap`의 기존 quest 처리와 `GameMap`의 Hit → Death → StageClear → 제거/respawn → callback 순서를 보존한다. 보상 기본안은 처치자 지급이며 파티 공동 분배는 이번에 추가하지 않는다. **팀원 명세의 열린 질문: 처치자 지급과 파티 공동 분배 중 어느 정책인가?** 기본안은 사용자 확정 정책이 아니며 데이터·규칙의 한 지점에서 교체 가능하게 둔다(메인 `msg_763b1fa21bf5`).
- **데이터·사용**: 공유 정의는 아이템 ID·표시 데이터·상한·사용 정책을 가진다. 최소 소비성 아이템의 사용 효과/거부 조건과 자리표시 드롭은 계약 발행 전에 고정한다. 서버가 소유량·현재 상태를 검사하고 수량 차감과 효과를 한 판정으로 처리한다. 재화 증감은 음수 잔액·오버플로·잘못된 양을 거부한다.
- **원자성/실패**: 공간·수량·잔액·사용 효과·중복 방지 표식을 함께 검사하여 실패가 일부 획득/차감으로 남지 않게 한다. 정상 거부, 중복/오래된 요청, 내부 불변식 위반을 구별한다. 내부 실패를 성공 응답이나 임의 기본 상태로 바꾸지 않는다.
- **비용과 수명**: 유한 인벤토리/스택 상한, 요청/보상 중복 방지 상태의 보존·정리 기준을 명시한다. 임의 TTL로 옛 보상의 재지급을 허용하거나 처리 ID를 무제한 쌓지 않는다. 구체 수치와 키는 Fable 검토 뒤 선택하며 측정하지 않은 성능을 보장하지 않는다.
- **표시와 통신**: 서버 snapshot/사용 결과로 클라이언트를 갱신한다. Unity는 메인 스레드에서 상태를 적용하고 UI 구독을 동일 source에서 해제한다. 로컬 예상 잔액을 서버 확정으로 표시하지 않는다. 패킷·메시지 최대 길이와 수량 필드의 유효 범위를 양쪽에서 검증한다.
- **저장 접점**: 현 목표는 메모리 상태다. GameDev와 snapshot/변경 통지의 최소 값 계약을 합의하되 사용처 없는 DB 추상화·저장 성공 표시를 만들지 않는다. 게임 틱에서 DB/파일/네트워크 I/O 완료를 기다리지 않는다.

### 파트 간 소유와 패킷 합의

| 대상 | 소유·제안·상태 |
|---|---|
| `GameSession.cs`, `Loop/GameWorld.cs`, `Handlers/HandlerRegistry.cs` | GameDev 소유. `msg_58eaccfd4a15`로 Content checkout의 최소 등록·처치 callback·세션 정리 연결에 순차 쓰기를 허용받았다. 계약에 예정 심볼을 명시하고 쓰기 종료·head를 알린다. GameDev 인스턴스 구현은 해당 통합 뒤 착수한다. |
| `99_Tools/PacketGenerator/PDL.xml`, `98_Shared/Protocol/Generated/GenPackets.cs`, `ProtocolVersion.cs` | 현재 ID 1..34와 순서 자동 배정을 보존한다. 당분간 Content가 PDL·생성물·버전·등록의 단일 writer이고 Content PR1 병합 뒤 GameDev가 append한다. |
| 합의 ID | Content 35 `C_InventoryRequest`, 36 `S_InventorySnapshot`, 37 `C_ItemUse`, 38 `S_ItemUseResult`; GameDev 신규 패킷 39부터(`msg_58eaccfd4a15`). 검토로 패킷 수가 달라지면 양쪽 goal을 먼저 갱신·합의한다. 기존 번호 재사용·재배열 금지. |
| 새 Items/·Handlers/Inventory/·Shared Items·클라이언트 새 인벤토리 파일 | Content. 구체 파일 목록은 계약별로 제한하며 같은 파일 동시 쓰기 금지. |
| CURRENT | Content 행/작업 공간 한 줄만 추가. Rules는 자기 행·branch만 수정 중이며 타 파트 변경을 서로 보존하기로 회신함(`msg_3da241296f57`). |
| GameDev 합의 | 요청 `msg_47ffacf96779` → 동의 `msg_58eaccfd4a15`(2026-10-04T15:05:31Z). 새 저장 DTO/API는 아직 없으므로 임의 hook를 만들지 않고 Content 상태 소유·읽기/변경 경계만 정리한다. GameDev 다음 저장 goal에서 함께 확정한다. 회신 주소 `run:run_495ed90b4d12`(`msg_1a60ca14588c`). |

## 구현 전 Fable 검토와 TDD

검증 등급은 **강**이다. 서버 경제·프로토콜·공유 DLL·보호 집합·오류 분류가 대상이므로 실사 + 독립 테스트 + 실제 경로 1회 실행이 필요하다. 문서만의 준비 결과는 문서 실사이며 제품 검증 PASS로 바꾸지 않는다.

1. 이 범위 초안을 메인에게 확인받고 goal을 commit한다. GameDev 경계를 합의하고 읽을 경로가 실제 존재하는 계약 초안을 준비한다.
2. 신규 외부 `claude-fable-5-1`은 관련 소스를 읽기 전용으로 검토하고 **이 폴더의 `goal-review.md`만** 쓴다. 파일 첫 부분에 쓰기 전 맥락을 기록한다. 불변식과 각 전제의 `[소스]`/`[추론]`/`[미측정]`을 구분한다.
3. Astra가 각 제안의 채택·보류 이유를 이 goal에 기록하고 계약에 불변식을 반영한다. **메인이 review 원문과 보완 goal을 확인한 뒤** Sol을 발행한다.
4. 새 goal의 TDD: 구현 전에 실패하는 요구사항 테스트와 그 원시 결과를 둔다. 테스트 작성자는 신규 Opus, 제품 구현자는 새 Sol로 분리하며 같은 파일의 소유를 넘기기 전 쓰기 종료를 확인한다. 구현 뒤 독립 판정은 또 다른 신규 Opus가 보고·diff·실행 원문을 실사하고 필요한 테스트를 추가/보완/실행한다. 준비 단계의 실패를 제품 결함 확정 실패로 집계하지 않는다.
5. 구현 `gpt-6.1-sol` max, 검증 `claude-opus-5-5`. 작업 하나 뒤 정산·종료하고 수정·재검증에는 새 세션을 연다. 모델 대체나 리드 직접 제품 구현은 하지 않는다.

### Fable 결과와 채택·보완

원문: [goal-review.md](goal-review.md), SHA256 `E41A51FEAD342E469214E1D86F7221B3FD00E616B5CDF3BE7E03E711D772F550`. 고정 입력 HEAD `6d37e7a05cd0520cdda449ccfd7582f8a6d48e1c`. 신규 외부 Fable의 `task_531a88bceb1d` / `ctx_bd9b47d42fcb`는 `msg_907c58954850`(2026-10-04T15:32:01Z)로 검토 작업 succeeded를 보고했다. 원문과 규칙·파일 소유를 대조하고 release·동일 incarnation의 pane close를 완료했다. 로컬 원문 사본은 `.backups/verification/2026-10-05-items-inventory-currency/fable-goal-review-original.md`다. **설계 검토 완료이며 제품 동작 PASS가 아니다.**

| 항목 | Astra 판정·이유·최종 계약 반영 |
|---|---|
| INV-01 | 채택. World 틱의 경제 큐를 Quest 뒤·barrier 전에서 drain한다. 핸들러는 상태를 직접 읽거나 쓰지 않는다. |
| INV-02 | 수명·키 채택, 전제 보완. World 레지스트리로 세션 종료 때 정리한다. 기존 int entityId가 영원히 wrap하지 않는다고 보장하지 않으며 요청 job은 캡처한 session 참조와 entityId의 소유를 함께 검사한다. |
| INV-03 | 채택. 맵 이동 전후 같은 경제 상태를 유지하고 PlayerTransferState를 넓히지 않는다. |
| INV-04 | 채택. 실제 서버 처치 callback만 보상 입력이며 낙사 despawn 무보상과 기존 처치 순서를 보존한다. |
| INV-05 | 채택. 적용 시점의 활성 수혜자를 검사한다. 여기의 활성은 연결/소유 활성이지 HP 양수라는 새 조건이 아니다. 종료된 수혜자의 늦은 보상이 고아 상태를 만들지 못한다. |
| INV-06 | **표식 없음 채택. receipt/ring/TTL은 추가하지 않는다.** 메인 `msg_2fcb5bb91b26`의 도달 경로 대조를 반영했다. 현재 Melee는 GetEnemyById + hasLiveTarget, Dash는 ResolveImpactTargets의 살아 있는 적만 선택하고 사망 처리에서 즉시 RemoveEnemy한다. DeferredDamage도 없거나 죽은 대상은 건너뛴다. 제품 경로의 같은 틱 두 공격과 사망 뒤 지연 피해에서 보상 한 번/revision 한 번을 테스트한다. 사망 내부 메서드에 이미 제거된 객체를 임의 재주입하는 것은 현재 외부 입력 경로가 아니며 이를 위해 경제 전용 GC 상태를 추가하지 않는다. |
| INV-07 | 채택. 보상 전체 사전 검증·불변 다음 상태 계산 뒤 참조 교체를 선택한다. 이것만이 원자성을 보장하는 유일한 일반 해법이라는 원문의 표현은 채택하지 않는다. commit 전 모든 실패 가능한 계산/할당을 끝내고 전송은 commit 뒤 수행하며 전송 실패 뒤 보상을 재적용하지 않는다. |
| INV-08 | 채택. 소비량 1과 사용 효과를 같은 상태 전이로 처리한다. 실패 때 수량·재화·revision 모두 보존한다. |
| INV-09 | 채택. 음수·overflow·상한 위반은 변경 전에 거부하며 invalid catalog/내부 상태와 정상 용량 거부를 구분한다. |
| INV-10 | **보완 채택. uint wrap 미처리 가정은 보류.** 성공마다 revision +1, 거부 때 불변. uint.MaxValue에서는 새 경제 변경을 명시적으로 거부하여 0으로 돌아가지 않는다. 상한 경로를 테스트한다. |
| INV-11 | 채택. expectedRevision 비교로 사용 중복·stale을 거부하고 요청 ID 무한 집합은 두지 않는다. Stale 때 현재 snapshot도 보내 재시도가 가능한 상태를 제공한다. |
| INV-12 | 채택. 패킷에 행위자를 넣지 않고 session·entityId를 캡처해 적용 시 재검증한다. 종료/이동 중 적용 불가 요청은 변경하지 않는다. |
| INV-13 | **보완 채택.** 새 C 패킷은 고정 크기다. 기존 파서의 짧은 payload 예외 경로를 답습하지 않고 새 핸들러에서 정확한 길이·reserved/ID/범위를 Read 전에 검사한다. 관련 불량 입력은 실제 경계에서 거부되는지 테스트한다. |
| INV-14 | 채택. snapshot은 고정 8슬롯으로 제한한다. 현재 list 템플릿의 호스트 endian/NET_LEGACY 미검증 경로를 새 경제 패킷에 연결하지 않는다. 생성기 개조는 하지 않는다. |
| INV-15 | 채택. 종료 정리와 늦은 job 활성 검사를 함께 둔다. 종료/상한 거부 보상은 재시도 큐에 남기지 않는다. 내부 예외와 정상 거부의 처리는 구분한다. |
| INV-16 | 채택. 순수 값 snapshot과 상태 소유/읽기 경계만 명시한다. 저장 DTO/API·DB hook는 GameDev 후속 goal에서 확정하며 선행 구현하지 않는다. |
| INV-17 | 채택. 경제 enqueue는 quest 분기 밖이며 killerId·Kind·EntityId 값만 캡처한다. 단일 발화 근거는 INV-06의 현재 제품 경로와 회귀 테스트다. GameMap/EnemyEntity는 수정하지 않는다. |
| INV-18 | 채택. enum은 기존 Enums 관례, 카탈로그·상한·오류는 Shared Items, 드롭표는 서버 Items의 한 지점에 둔다. |
| INV-19 | 채택·순서 가정 보완. 지연된 처치 push가 새 사용 응답보다 뒤에 도착할 수 있어 같은 연결의 낮은 revision snapshot도 명시적으로 무시한다. 초기 revision 0은 별도 동기화 여부로 표현한다. 연결 교체 뒤 옛 apply를 막는 generation/기존 큐 경계는 PR2에서 검증한다. |
| INV-20 | 채택. 보상 주입 치트를 만들지 않고 실제 공격·처치 경로를 봇으로 관찰한다. |

F-1~8의 취지는 위 INV 판정으로 연결한다. 특히 F-3의 유일해 표현은 설계 선택으로 한정하고 F-5의 기존 입력 파서 위험은 새 핸들러의 정확 길이 검사로 보완한다. 실행하지 않은 list/종료/GC/순서·성능은 미측정으로 남긴다.

| 선택/정책 | 채택·보류 이유 |
|---|---|
| S-1 | 고정 8슬롯 채택. 패킷 수 4개와 ID 35..38 합의 유지. |
| S-2 | 상한 때 보상 묶음 전체 거부 채택. 부분 지급/clamp는 채택하지 않는다. |
| S-3 | 표식 없음 채택. 처음 제안한 receipt를 메인 원천 대조 뒤 철회했다(INV-06). 현재 도달 경로를 테스트하며 GC 시점에 의존한 테스트/구조를 추가하지 않는다. |
| S-4 | 요청의 직접 응답과 처치의 SendToEntity 경로 채택. 재정렬 가능성을 revision 적용 계약으로 처리한다. |
| S-5 | 연결당 경제 요청 in-flight 1개를 채택. 포화 때 상태를 바꾸지 않고 다음 요청으로 재시도 가능해야 한다. 성공/거부/예외/종료 모든 경로에서 guard를 정리한다. 이동 limiter 예산은 공유하지 않는다. |
| S-6 | 결정적 드롭 채택. 확률과 RNG 추상화는 현재 사용처가 없다. |
| P-A | 처치자 지급은 메인이 확인한 임시 기본안. 팀원 명세의 열린 질문으로 유지하고 수혜자 결정 책임 한 지점으로 제한한다. 사용처 없는 파티 분배 프레임워크는 만들지 않는다. |
| P-B | 보스도 실제 처치 보상 경로에 포함하는 임시 드롭표로 둔다. 보스 유무/수치는 팀원 명세 입력이며 최종 정책이 아니다. quest reset과 독립이다. |
| P-C | 자리표시 MaxSlots=8, MaxStack=99, MaxCurrency=1,000,000,000; 재료/재화 주머니/재화의 최소 데이터. 일반 적 재료1+재화10, 골렘 주머니1+재화10, 보스 주머니1+재료1+재화50, 주머니 사용 재화50을 테스트 가능 기본값으로 제안한다. 최종 기획·성능 실측 수치로 보고하지 않는다. |

추가 범위 밖 관찰(기존 quest 종료 정리/활성 검사, 기존 생성기 Write 할당, 기존 수신 예외 경계)은 GameDev에게 근거와 함께 전달하고 이 goal의 제품 수정으로 넓히지 않는다. ProtocolVersion의 현재 bump와 직접 관련된 낡은 list 주석은 미사용/미검증 사실로 좁게 정정할 수 있다.

메인 원문 확인: `msg_2fcb5bb91b26`(2026-10-04T15:37:43Z)은 INV-06/S-3 재판단 외의 채택/보완을 확인하고 정리 뒤 추가 확인 없이 TDD → Sol → 신규 Opus 진행을 허용했다. 메인 전달의 "Melee와 Dash가 모두 ResolveImpactTargets 사용" 표현은 실제 source와 다르므로 별도 회신에서 정정한다. MeleeAction의 GetEnemyById·hasLiveTarget 가드도 같은 단일 발화 결론을 지지한다. 원문은 `.backups/verification/2026-10-05-items-inventory-currency/main-fable-decision.json`에 보존했다.

시범 평가: 구현 전에 중복 방지 전제·revision wrap·snapshot 순서·고정/가변 패킷 경계를 드러냈다. Fable이 미측정으로 남긴 사망 경로를 메인이 원천 대조하고 Astra가 호출자 가드를 재확인하여 receipt 과잉 설계를 줄인 사례다. 구현 뒤 검증 왕복 감소 여부는 아직 평가할 수 없다.

## 검증 계획

| 경계 | 관찰할 요구사항과 원시 근거 |
|---|---|
| 경제 상태 | 최초 획득·동일 보상 반복·다른 대상 수명 구별·다른 소유자·음수/0/상한/overflow·인벤토리 용량/스택 실패 때 상태 보존. 고정 기대값 또는 독립 oracle을 사용하며 제품 계산을 복제하지 않는다. |
| 사용/수명 | 소유하지 않은 아이템, 잘못된 수량/종류/현재 상태, 동일 사용 요청 재전달, 종료/맵 이동과 지연 요청을 다룬다. 실제 gate·queue·처치 callback 진입을 확인한다. |
| 프로토콜 | 기존 ID·정의 순서 보존, 새 패킷 왕복/불량 길이·범위, 버전 불일치 gate, 서버와 Shared/ClientNet 소비자 빌드. 생성기는 실제 PDL 경로로 실행하고 diff를 확인한다. |
| 봇 실제 경로 | 해당 goal 전용 근거로 실제 서버 연결·공격/처치·획득 snapshot·재화 증가·사용 결과를 관찰한다. debug 보상 직접 주입만으로 처치 경로 완료를 주장하지 않는다. |
| Unity | 컴파일·EditMode 테스트와 실제 인벤토리 표시/아이템 사용 플레이. 새 `.meta`와 기존 GUID/직렬화/문자열 경로 보존. 실제 플레이 단계의 신규 독립 Opus가 메인에 시트를 요청한 뒤 opt-in Unity MCP를 사용한다(`msg_844ffbe24d73`). scene/prefab 저작은 PR2 범위 밖이다. .NET 통과로 Unity 완료를 대신하지 않는다. |
| 기존 동작 | 전투/사망·respawn·quest/party·맵 이동·종료의 영향 회귀. 기존 테스트 실패는 전수 분류하고 수정한 기대값은 요구사항 원천과 전후 같은 명령 결과를 남긴다. |

- 검증자의 harness는 파일로 둔다. 복합 Assert는 판단별 변수로 나누고 판정에는 `verifies=대상 task`, 결함 번호·심각도·차단 여부·귀속, `설계 관찰(비차단)`을 넣는다. 설계 결정으로 막히면 한 줄 대안을 함께 반환한다.
- SDK `10.0.301`; Windows 빌드는 Unity Plugins DLL을 복사하므로 전후 Git diff를 확인한다. SAC Off 결정은 환경 확인의 입력이며 현재 네이티브 실행 성공을 의미하지 않는다. WSL은 source별 전용 복사본·잠금을 사용한다.
- 서버 7777·Unity 프로젝트 실행은 먼저 GameDev/메인과 소유를 맞춘다. 다른 소유 프로세스를 종료하지 않는다. DB 검증은 현 goal에서 수행하지 않는다.
- 보고 수치는 원시 결과 파일에서 읽고 명령·환경·exit·성공/실패/skip·미실행을 분리한다.

## 정본 반영 전 적용 중인 사용자 결정

메인 `msg_f565ecb26653`(2026-10-05T14:18:42Z)의 사용자 원문은 「대시보드 결정 응답: 1) 모델 라우팅 - 리드 Opus 전환을 다섯 파트로 넓히기 → A 다섯 리드 모두 Opus로 (각 목표 끝날 때)」다. 이후 `msg_571418f891d9`(14:23:54Z)의 원문 「대시보드 결정 응답: 1) 모델 라우팅 - 리드 Opus 교체 시점 앞당기기 → A 작업자가 빈 시점에 바로 교체」가 **교체 시점만 대체**했다. 메인 전달이며 이 세션의 직접 사용자 입력으로 격상하지 않는다. 다음 리드는 `claude-opus-5-5` xhigh, 구현 Sol max·신규 독립 Opus 분리는 유지한다. AGENTS의 기존 Astra 문구보다 이 사용자 결정이 해당 범위에서 우선한다. 지금 Content는 결함4 Sol 정산까지만 수행했고 새 Sol·검증자·Gardener 발행 없이 아래 인계 준비 뒤 턴을 종료한다. 사용자가 잠들었으므로 병합 승인·Unity Editor/MCP 등 사용자 손이 필요한 실행은 아침까지 대기한다.

같은 메인 지시에 따라 우편함은 `orca orchestration check --wait --timeout-ms 600000`으로 바꾸고 짧은 상태/화면 조회를 줄였다. 실제600000 대기1회는 메시지 도착으로 끝났으며, 인계 준비 완료 발신 후에는 우편함을 다시 열거나 백그라운드 watch를 남기지 않는다. 원문은 근거 폴더의 `main-lead-opus-wait-decision.json`, `main-lead-immediate-handoff-decision.json`이다.

메인 `msg_16add9c27b4d`(2026-10-05T14:58:54Z)가 전달한 사용자 원문은 「PR로 체크포인트만 잘 만들어 놓으면 다음 작업 진행해도 되니까 할 수 있는 부분까지 해봐」, 앞선 원문은 「나 진짜 자러갈게, 자율적으로 할 수 있는 부분은 진행해줘」다. 메인 pane에서 Enter로 제출된 지시의 전달이며 이 세션의 직접 입력으로 격상하지 않는다. 메인 운영 해석은 범위 안 단위를 PR 체크포인트(PR·CI·독립 검증·승인 묶음)로 만들고 다음 단위로 가는 것, 병합 전 PR 위에 쌓을 수 있는 것, 병합은 PR별 사용자 승인, 범위 밖 새 goal은 범위 초안만, 사용자 손이 필요한 일은 아침, Sol은 띄울 때 메인에 한 줄 통지다. 원문 `lead-opus-inbox-4.json`.

메인 운영 판단 `msg_5e769cc95c8c`(15:00:28Z)는 Unity MCP 시트 예약을 `task_bce6064a15f5`에서 아침의 새 Opus로 옮기도록 허용했다. 조건은 현재 검증자를 batch 판정으로 좁혀 판정에 「MCP 실화면 미실행 — 아침 새 Opus로 이관」을 적게 하는 것, 정산 때 relay 종료 확인, 아침 Opus는 `msg_94ffc6cbe0f0`과 같은 조건, PR2 커밋에서 cloud3 ProjectSettings와 사용자 MinimapRT를 빼고 커밋 직전 staged 목록을 원시로 남기는 것이다. 원문 `lead-opus-inbox-5.json`.

메인 운영 지시 `msg_2b281320df53`(2026-10-05T14:34:35Z): 우편함 대기는 heartbeat만 깨움 조건에서 빼고 `--types "status,dispatch,worker_done,merge_ready,escalation,handoff,decision_gate,question" --timeout-ms 900000`으로 연다. 작업자 실행 중 빈 대기가 3번 이어지면 worker-show의 lastHeartbeatAt을 한 번 확인한다. 원문 `lead-opus-inbox-1.json`.

메인 `msg_82735bd92cff`(2026-10-05T09:37:40Z)가 전달한 사용자 원문은 「그리고 계획에 오버되는 부분은 다음 계획 편성에 포함시키고, 일단 현재 작업 목표 달성 우선」이다. 메인 pane에서 Enter로 제출된 사용자 지시의 전달이며 이 세션의 직접 입력으로 격상하지 않는다. 원문은 `current-goal-priority-decision.json`에 보존했다. 현재 goal의 완료조건과 범위 안 결함 수정을 우선하며, 이후 새 범위 밖 후보·개선 권고·비차단 지적·새 요청은 아래 다음 계획 후보에 출처와 함께 기록만 한다. 다음 계획은 메인이 사용자와 정하고, 애매한 범위는 메인에 좁게 질문한다. 이 결정은 진행 계약의 재발행·중단을 요구하지 않는다.

메인 `msg_844ffbe24d73`(2026-10-05T08:43:24Z)이 전달한 사용자 원문은 「대시보드 결정 응답: 1) Unity 시트 번갈아 쓰기 - 필요한 세션만 연결하는 방식으로 바꿀지 → A 필요한 세션만 켜기(opt-in) · 2) 목표 진척 자동 갱신 - 단계 완료를 어디서 읽을지 → A PR 자동 + goal.md 체크리스트」다. 앞선 코멘트는 「…연결 가능한 MCP의 시트가 1개인거지, MCP로 연결된 세션을 해제하고 다른 세션으로 연결하는건 가능해. 갈아 끼우면서 번갈아서 쓰는 방식으로 가도 괜찮을거 같은데…」와 「…하나의 에이전트 세션에만 시트 하나씩 할당시킬 수 있어서, Content가 전담해야 할 거 같은데」다. 출처는 메인 pane에서 Enter 제출된 대시보드 결정이며 이 세션의 사용자 직접 입력으로 격상하지 않는다. 전달 원문은 `pr2-tdd-completion-and-main-unity-decision.json`에 보존했다.

이 결정은 아래 역사 기록의 MCP 메인 전용 배정을 대체한다. Content 실제 플레이의 독립 Opus를 띄우기 전에 메인에 하나뿐인 시트를 요청하고, 현황판 소유 등록 뒤 `claude --model claude-opus-5-5 --mcp-config C:/Users/bass1/.unity/claude-mcp.json`으로 기동한다. Astra·Sol에는 MCP를 연결하지 않는다. 기존 relay 세션 두 개가 닫히기 전 Unity를 새로 열지 않는다. Opus 계약에는 시작 전 Unity.exe/relay·프로젝트 경로 확인, batch와 대화형 에디터 순차 사용, GameDev와 7777 조율, 크래시 뒤 잔존 relay 보고와 시트 획득 시점 관측을 넣는다. scene/prefab 저작은 범위 밖이다. 진척 단계는 Astra가 상태 변경 때 갱신하며 PR 번호가 정해지면 병합 단계에 실제 번호를 적는다.

별도 **메인 운영 결정** `msg_1fc59e1efddb`(2026-10-04T16:49:33Z)은 Orca1.4.217 동안 공식 blocking ask의 고정 subject Question과 공식 reply --id 자동 subject에 기존 R-3 예외를 동일 조건으로 한정 적용한다. 사용자 직접 지시로 격상하지 않는다. body 자기 태그와 현재 from_handle/Task/Dispatch 대조는 유지하고 일반 send에는 예외가 없다. 로컬 ask/reply --help의 subject 옵션 부재를 `orca217-ask-help.txt`, `orca217-reply-help.txt`에, 원문을 `orca217-policy-message.json`에 보존했다(모두 이 goal 근거 폴더). helper에1.4.218로 가장하지 않으며 실제 해당 사례는 원시를 사람이 대조해 기록한다. 이번 TDD에는 실제 ask/reply 사례가 없었고 일반 worker_done은 기존 helper에서 allowed/exit0이었다. 결정은 `msg_5b2e7e3005d0`으로 진행 작업자에게 전달했으며 고정 goal은 쓰기 종료 후 갱신했다. Orca1.4.218 이상 또는 subject 옵션 지원 때 이 한정 적용은 종료한다.

출처는 메인 `msg_53fec7129c14`와 읽기 전용 `C:/Dev/DawnHolder_Dashboard/main-notes/2026-10-04/` 아래 파일들이다. Rules의 정본 반영 전까지 이 goal에 적용한다. 최신 전달 결정과 기존 규칙이 다르면 해당 적용 범위에서 최신 결정을 따른다.

| 원문/전달 결정 | 출처 | 이 goal에 적용 |
|---|---|---|
| 「3) 게임을 두 파트로 → A 둘로 나눔」, 「2) Content 첫 goal: 아이템·인벤토리·재화 → A 승인」 | `deadline-roadmap-draft.md`, `plan-scopes-draft.md` 머리 | Content 신설, 메모리 경제·PR1/PR2. 저장은 GameDev. |
| 「OK 그렇게 가자」(범위 4항목·착수 전 확인·3회 초과 수정 체크포인트) | `HANDOFF.md` 결정 1 및 메인 메시지 공통 1 | 범위·PR 경계·메인 확인 관문 유지. |
| 「2) 설계 4범주 작업은 구현 전에 Fable이 불변식 목록 작성(시범) → A 시범 도입」 | `routing-draft.md` 머리, 메인 Fable 절 | Content 아이템 복사 방지의 보호 집합·오류 분류. 메인 원문 확인 뒤 Sol 발행. |
| 「3) Sol effort 시험은 보류하고 max 유지 → A 보류」, 「5) 검증 강도 2등급 4주 시범 → A 시범 도입」 | `routing-draft.md` 머리 | Sol max, 신규 Opus. 경제/프로토콜은 강. 10-31 재평가. |
| 「A」(새 goal부터 TDD) | `HANDOFF.md` TDD, 메인 공통 6 | 구현 전 실패 테스트·원시 결과, 구현 뒤 신규 독립 검증. |
| 「1) 작업 유형별 표와 모델 평가 권고 16개 배치 → A 승인」, 「4) 작업별 자동 기록 범위 → A 둘 다」 | `routing-draft.md` 머리, 메인 공통 2/3/5 | 계약 경로 확인·요구/판정 기준·관련 절 원문·검증 harness 파일·설계 대안·원시 수치. 기계 판독 판정 표. 기록 helper 선행 구현은 하지 않음. |
| 「A로 가자」, 「OK 그렇게 하자 … 그게 유연성이지」(명세는 입력) | `HANDOFF.md` 결정 8/9 | 팀원 명세가 없어도 교체 가능한 자리표시 데이터로 진행, PR 코멘트 비동기. |
| 「1) 필수선 → A 필수선 승인 · 2) Docker 분산 → A 마감 뒤 · … · 4) … → B 오늘 순서대로」 | `deadline-roadmap-draft.md` 머리 | 11월 첫째 주 전시·PPT/실행 녹화, 10-28 동결 목표. 다른 파트 축소·후속 goal 자동 착수 금지. |

## 재개 지점

현재 실행 지점은 아래 [결함4 후속 독립 판정](#결함4-후속-독립-판정--batch-범위-실화면은-아침)의 「다음 단위」다. 발행 경위는 [결함4 후속 독립 검증 발행](#결함4-후속-독립-검증-발행--2026-10-05)에 있다. 바로 아래 인계 블록은 새 리드 진입 시점의 기준으로 보존한다.

### 리드 교체 인계 — 2026-10-05T14:27Z

이 블록은 다음 `claude-opus-5-5` 리드가 대화 맥락 없이 읽을 현재 실행 지점이다. **새 리드가 이 브랜치의 유일한 commit/push 담당**이다. 인계 자체는 커밋하지 않았다. 이전 Content Astra는 아래 정산을 마치고 메인에 인계한 뒤 종료하며, 제품/테스트 구현·독립 검증을 직접 대신하지 않는다.

| 항목 | 현재 값·근거 |
|---|---|
| Run | `run_add8d9f825f4`. 메인이 새 pane을 연 뒤 `orca orchestration run-use`의 현 버전 help를 확인해 같은 Run을 이어받는다. 새 Run을 중복 생성하지 않는다. |
| Worktree / branch / HEAD | `C:/Users/bass1/orca/workspaces/DawnHolder_Project/content-active` / `feat/items-inventory-ui-20261005` / `8d1e8856a99e9a5ed74aa291294accd2299aaaf6`. PR2 커밋·push·PR 생성은 아직 없다. |
| 근거 root E | `.backups/verification/2026-10-05-items-inventory-currency/`. 이 블록의 짧은 근거 경로는 모두 E 아래다. `.backups`는 로컬 원문 보존이며 PR에 넣지 않는다. |
| Orca 관측 | 1.4.220, runtime `120aecfa-9f94-4533-9594-48a22f1853ba`. 현재 이전리드 handle `term_1fc1da2b-96ad-41f2-95d8-e6ee8b545c5b`는 역사 주소로만 보존하고 새 권한으로 쓰지 않는다. 메인 handle `term_072d2ee9-df16-43ce-b86c-122c9de316c0`, GameDev `run:run_6ba3f644755b`도 사용 전에 현재 identity를 확인한다. |
| 작업자 | **0**. `lead-handoff-worker-list.json`: 전체18rows/hasMorefalse/모두nextActionnone,17완료+옛BSOD폐기1. 방금 Sol의 term_1088e54e-b352-4ccf-b451-7e8991ae2180는 close 뒤 실제 부재이며 Content에는 리드 pane만 남았다. 완료 세션 재사용 금지. |
| Unity·환경 | `sol-pr2-fix2-postclose-gate.json` 14:25:36Z: Unity0/relay0/프로젝트 lock없음, AudioMasterMute DWord0, SelectedCharacterClass1. 서버/WSL/7777은 이번 Sol이 사용하지 않았다. 이전 Opus 서버 종료/7777해제 근거는 `opus-pr2-fix1/runs/server/server-2-stop.json`. 실제 재실행 전 점유를 다시 확인한다. |

**정산한 마지막 작업:** Sol `task_5f7f4e01ed5e` / `ctx_0bef8e140f29`, formal 완료 `msg_ec4ed8442cfd`(14:24:30Z). 지정/화면 `gpt-6.1-sol max`, backend unknown. report `sol-pr2-fix2/report.md` SHA256 `2D87AE26F9A991B0B9D6784A3480A5DF87C5E70ABF5C97C4593A578BF03FD9C9`와 `context-before.md`, `audit/owned.diff`, 실제 batch `runs/editmode-1`·`runs/inventory-playmode-1`을 먼저 읽는다. 제품은 `InventoryPanelView.cs`의 실제 `character_status` 하단 경계 기반 여백/남은 높이 scale만 +12/-3; 최종 제품 SHA256 `1DE7847E3D636B6FCE2D4B578A56BEE8AFC66A987C1F87C16E1CE1A3C8A6DAF8`. 자체404/404·8/8·둘exit0, 음소거 각각0→1→0. 기존 meta1144/tests34 보존. Astra의 실제 `audit-sol-pr2-fix2-settlement.ps1`은172inputs/허용제품1+자동settings1/예상밖0/XML2를 확인했으며 별도 독립 제품 검증은 아니다. completion-worker/release/preclose-identity/close/postclose-gate/completion-ack의 `sol-pr2-fix2-` receipt로 정산했다.

**아직 열린 제품 문제:** 결함4는 이전 신규 Opus의 R8 차단이다. 802×451에서 기존 HUD가 제목 첫 획을5.89×20.56px 덮었다. 첫 확정 실패1회이며 이번 Sol 자체 GREEN은 독립 해소가 아니다. 기존 scene coverage는 문제 HUD 존재를 보장하지 않아, 다음 검증은 HUD/패널 생성·활성 존재를 먼저 단정하고 교집합0을 검사해야 한다. 802×451·1920×1080/resize에서 실제 크기와 screenshot supersize를 구분하고 전체 패널/문구/8슬롯/최대값/버튼·기존 입력을 보존한다. 이전 독립 원문 `opus-pr2-fix1/report.md` SHA256 `8BF2E595E82A1FBB1BA0B7009F0612EBE700D373217EA621325F3F41FB24E494`가 원형이며 #1~3 해소, 기존29실패 a25/b4/c0/d0·404/404·scene8/8,10/10·실서버각1/1·실제 획득10/사용60은 그 당시 결과다. 이를 새 실행으로 세지 않는다. 실제 사람손/DB/CI/Player build는 미실행이다.

**미커밋 파일과 이유:** 정확한 개별 경로/상태/hash는 `lead-handoff-uncommitted.json` 및 `lead-handoff-git-status.txt`에 보존했다. 아래 그룹이 현재 의도된 산출물이며 통째로 reset/stash/clean하지 않는다.

| 경로 묶음 | 이유·처리 |
|---|---|
| FEATURE_MAP.md, domains/client.md, operations/BACKLOG.md | PR2 기능/책임 문서, 이전CI 전투 smoke 및 SENTIS drift 후속 후보. BACKLOG는 최신main과 양쪽행을 보존해 통합할 대상. |
| 이 goal.md | 현재 goal의 결과·사용자 결정·인계 기록. 이번 인계 추가도 미커밋이다. |
| Scripts/Combat/CombatBootstrap.cs, Input/LocalPlayerInput.cs, Network/MainThreadDispatcher.cs·MapEntryCoordinator.cs·NetworkService.cs·UnityClientSession.cs | 최초 PR2의 부트스트랩/수명 연결, 첫 수정의 timeout/입력 경계. 기존 산출물을 보존한다. |
| 신규 Scripts/Input/GameplayPointerInput, Network/Handlers/Inventory 및 InventoryRequestController·InventoryTimeoutScheduler, State/InventorySnapshotState·InventoryState, UI/InventoryPanel·InventoryPanelView와 새 meta | 최초 구현+첫 수정+두 번째 HUD 수정. 기존 GUID/byte 사본을 보존하고 아직 독립 최종 PASS 전이다. |
| 기존 EditMode ClientSessionGeneration·ComponentEnsureRegression·InventoryProtocolCompatibility·MapEntryBinding·PartyInviteResponseContract, PlayMode MapEntryPlayFixture; 신규 InventoryClientContract·InventoryClientTestSupport·InventoryPresentation·InventorySceneLifecycle·InventoryServerIntegration와 meta | 선행TDD/두 독립 Opus의 요구·보존 검사와 fixture 보완. 최신 Sol은 테스트를 쓰지 않았다. 실패29 분류/수정 원문은 이전 Opus report에 있다. |
| **ProjectSettings/ProjectSettings.asset** | 사용자 결정 `msg_39c94249a203`: cloudProjectId1094041a-5db2-4e67-9ca2-c413d1238be3/projectName03_Client/organizationIdroy_131은 **이PC 로컬만 유지, 커밋/옛값복원 금지**. hook 우회 금지. SENTIS 자동 제거 한 줄은 승인된 정산으로 복원 완료, 전체hash A58A3CDFB7F41C338D1542B6EF22BDAF793BBD73DFD5D0E6DEFFAEA79CBDF157(`ProjectSettings-pr2-fix2-restore.json`). |
| **Assets/Resources/MinimapRT.renderTexture** | 기존 사용자 변경. 보호hash 유지, PR2 stage/되돌리기 금지. |

**다음 행동(새 리드 배치 뒤):**
1. 현재 identity/Run·branch/HEAD·미커밋 목록·작업자0을 확인하고 이 블록/마지막 Sol 원문 및 이전 Opus 차단 원문을 읽는다. 메인에 같은 Run의 새 회신 주소를 알린다.
2. 사용자 손이 필요한 일은 아침까지 보류한다. 메인 배치/재개 지시 전 새 세션을 임의로 열지 않는다. 후속 독립 검증 초안 `opus-pr2-fix2-brief-draft.md`와 준비 스크립트 `prepare-opus-pr2-fix2.ps1`, `opus-pr2-fix2-preflight.ps1`은 **미발행·미실행**이며 Opus Task/pane/최종 계약은 아직 없다. 새 리드의 현재 권한·입력으로 최종화/실제 preflight를 수행해야 한다.
3. MCP 시트1개는 메인 `msg_94ffc6cbe0f0`가 이 후속 신규 Opus에 예약했으나 아직 미사용·소유Task없음. 새 Task 생성 후 goal에 소유를 등록하고 사용 가능 여부를 메인과 현재 상태로 확인한다. GameDev 충돌 없음 정정본 `msg_e2c1e9f2234b`는 `opus-pr2-fix2-gamedev-confirmation.json`; 직전 포트/clone writer 실측을 대체하지 않는다.
4. 새 검증은 MCP없는 batch부터, 종료/lock해제 뒤 정확프로젝트 `content-active/03_Client`·Unity6000.4.7f1을 메인에 알린다. **이번 연결의 사용자 「Editor 열림·연결 승인 준비 완료」 전달 전 첫 MCP 금지**. 첫 호출은 기존audioMasterMute관측→true, Connection revoked는 재시도/우회없이 보고한다. 열린Editor중 batch/registry harness 금지.
5. 모든 Unity 실행은 원값→mute→복원과 최종 native값 대조를 남긴다. Main `msg_833c357e684e`는 exact HKCU `Software\Unity Technologies\Unity Editor 5.x` / `AudioMasterMute_h3604209190`만 batch 전 임시 변경·finally복원 허용(다른Unity0,이미1이면쓰기없음,다른prefs/OS볼륨/추적설정불변). 원문/부록은 `sol-pr2-fix2-audio-decision-main.json`·`sol-pr2-fix2-audio-addendum-v1.1.md`. 이전 Opus의 음소거1회 누락과 메인 비차단 정산은 아래 기존기록대로 공개한다.
6. 새 독립 PASS 뒤 최신main 통합/필요CI/PR2를 진행한다. 마지막 **읽기 전용** main관측은635865038e174ee5591530f6bd83e2e698e0b077(+55commits/144files),Unity변경0·현변경교집합BACKLOG만·새architecture-tests/module-boundaries CI. fetch/merge하지 않았으며 `pr2-fix2-main-*-observed.json`은 재확인 필요하다. PR180은 이미 병합됐고 PR2 병합은 **정확 PR/head의 새 사용자 명시 승인**이 필요하다. 자동병합 금지. 전체PR결과 뒤 Gardener/종료점검; 다음goal자동착수 금지.

이전 리드의 마지막 inbox ACK는 `sol-pr2-fix2-completion-ack.json` count0이다. 실행 중인 watch/대기 명령은 없다. 다음 리드가 필요한 과거정보는 이 goal과 E의 명시 원문에서 좁게 읽으며 전체 로그/대화를 수집하지 않는다.

### 이전 착수 경계(역사)

1. 메인 범위 확인(`msg_763b1fa21bf5`)과 GameDev ID/공유 파일/저장 경계 합의(`msg_58eaccfd4a15`)를 완료했다.
2. Fable 고정 입력·검토·정산·종료와 메인 원문 확인을 완료했다. INV-06/S-3를 현재 도달 경로에 맞춰 정리했다.
3. PR1의 최종 요구/패킷 계약은 `.backups/verification/2026-10-05-items-inventory-currency/pr1-acceptance.md`다. 구현·독립 검증과 승인 병합은 아래 실제 기록을 따른다. PR2는 같은 근거 폴더의 새 계약으로 신규 Opus 선행 테스트 → 신규 Sol 구현 → 신규 Opus 실사·독립 테스트·실제 클라이언트 경로 순서로 진행한다. 이전 실행을 PR2 검증으로 재사용하지 않는다.
4. Content Run은 `run_add8d9f825f4`다. 현재 handle은 실행 전에 runtime에서 재확인하고 과거 handle을 실행 권한으로 쓰지 않는다. GameDev Run `run_6ba3f644755b`에 PR1 공유 writer 해제를 전달했고 현재 client 연결/맵 경계의 예정 쓰기 없음 회신을 받았다(`msg_6610fe6da8a3`). 실제 서버·봇·Unity 실행은 기존 소유와 프로세스를 재확인해 조율한다.

## 진척 단계

- [x] 설계 검토·범위 확정
- [x] 서버 아이템·재화 구현
- [x] 서버 독립 검증·수정
- [x] PR180 병합
- [x] 인벤토리 화면 구현
- [>] 화면 독립 검증(결함4 batch 범위 해소 관측, 16:9 실화면은 아침 새 Opus)
- [>] 실제 플레이 최종 확인(획득·사용 경로 확인, HUD 배치 수정 뒤 실화면 재확인은 아침)
- [>] 화면 PR CI·병합(PR191 생성, 아침 실화면 확인 뒤 승인 요청)
- [ ] 결과 기록·종료

### 2026-10-05 크래시 중단과 복구

메인 `msg_0f4da872f048`은 00:45 KST PC 블루스크린으로 중단됐으며 분석 뒤 지금 재개하고 재발을 지켜보는 사용자 결정을 전달했다. 중단 당시 HEAD는 `a6691d15208ef1e39e4a38a26b6494f4169a8dc3`; TDD Opus `term_688234aa-5bf4-4074-ab30-434b39e0caac`는 준비 화면까지 확인했으나 Task/Dispatch 발행 전이었다. 복구 후 Astra가 clean 상태·계약 hash 일치·테스트/부분 산출물 부재를 확인했다. 따라서 인계할 크래시 중단 부분 결과는 없으며 크래시는 확정 실패로 집계하지 않는다. 실패한 대화 복원 뒤 shell로 남은 pane `term_5108245d-780e-4879-adf3-7bfab62bdb3a`만 화면 확인 후 종료했다. 새 Content handle `term_934c3c85-da3d-439c-9b9f-101154b3c5e8`에 기존 Run `run_add8d9f825f4`를 재바인딩했고, 같은 TDD 요구를 새 `claude-opus-5-5` 세션에 발행한다. Fable 원문·채택 커밋과 이전 증거는 그대로 보존한다. 메인의 덤프 분석/무손실 보고를 Astra의 자체 검증으로 격상하지 않는다.

두 번째 중단은 메인 `msg_ee7d35eac69a`가 전달한 01:15 KST BSOD다. 중단 당시 HEAD `756c7ec76fc7fe6f6d74b6f13221588df9d40a48`, 선행 TDD Task `task_2c4281b259c6`/Dispatch `ctx_0ce465ca2b10`/Opus `term_20c0bd8b-9b83-45bf-b2d4-3dcd61b77bd1`이 조사 중이었다. 복구 때 다시 clean 상태·신규 테스트/tdd 폴더 부재를 확인하여 보존할 부분 산출물이나 유효 RED/판정은 없다. runtime은 해당 Dispatch를 terminal_missing/abandoned·failed로 정산했지만 크래시는 제품 확정 실패에 포함하지 않는다. 메인이 전달한 사용자 조치 뒤 현재 Orca1.4.217 가이드를 재확인하고 같은 Run을 Content `term_857393a1-53a9-4546-b3f2-d2f48918696f`에 연결했다. 동일 Task의 retry-of로 새 Opus를 발행하며 처음부터 테스트를 작성/실행한다. 원 TDD 요구/계약은 그대로이고 두 번째 실행 메타데이터 정본은 같은 근거 폴더 `tdd-recovery2-addendum.md`/`tdd-recovery2-input-manifest.json`이다. 앞선 복구 계약·Fable 결과·미발행 Sol 준비본은 보존한다.

## 실제 결과와 미실행

- 맥락 메모, 목적별 브랜치, goal·CURRENT, 메인 범위 확인, GameDev·Rules 경계 합의, 초기 goal commit과 외부 Fable 설계 검토·정산·종료까지 수행했다. 검토 입력/결과 원문·receipt는 로컬 근거 폴더에 보존했다.
- 신규 테스트·선행 WSL 실행과 PR1 제품 구현·자체 점검은 아래 근거대로 수행했다. 실제 서버↔봇/Unity 실행, 구현 후 독립 판정, PR/CI/병합은 아직 수행하지 않았다. 제품의 독립 통과·실제 게임 동작·DB 저장을 주장하지 않는다.
- 범위 밖 후보: 후속 던전 콘텐츠·길드/거점·마을 연출·상점/연구는 메인 승인 로드맵에 이미 있는 다음 목표이며 자동 착수하지 않는다.

### 선행 TDD 완료 — 제품 PASS 아님

신규 Opus의 `task_2c4281b259c6`/`ctx_156e286a9070`은 `msg_9f515d0665e5`(2026-10-04T17:06:48Z)로 테스트 쓰기 종료와 succeeded를 보고했다. 원문은 `.backups/verification/2026-10-05-items-inventory-currency/tdd/report.md`, 사전 메모는 `tdd/context.md`다. 새 `02_Server/GameServer.Tests/Items/InventoryWireContractTests.cs` 608줄/17메서드/26케이스의 최종 SHA256은 `B8D2CBFCD0270BBB178A3425DE58E099218257AD916974FF5C7F8D1FB2D84D5F`다. 제품/기존 테스트는 쓰지 않았고 wire 기대값은 계약의 고정 표에서 판독하며 실제 세션·공격/지연 피해·World 콜백을 실행한다.

| 실행 | SDK/범위 | 실제 결과 | 의미 |
|---|---|---|---|
| `tdd/red-run2` | WSL10.0.301·InventoryWireContractTests | 빌드 성공, 26실행/23실패/3통과/0skip, raw exit1 | 23건 모두 경제 출력 부재의 Assert.Single 실패. 제품 미구현 RED |
| `tdd/baseline-session-cleanup` | 기존 SessionCleanupTests | 15/15, exit0 | 기존 fixture 기준 |
| `tdd/baseline-enemy-death-killer` | 기존 HandleEnemyDeathKillerTests | 4/4, exit0 | 기존 처치 경로 기준 |
| `tdd/red-run1` | 첫 harness logger 인자 오류 | raw exit127 | 판정 제외, 원시 보존. 수정한 harness로 red-run2 재실행 |

Astra는 원문·전체 테스트·harness·TRX 원시 카운터/실패 메시지·Git 상태·파일 hash를 대조했다. 통과3은 두 공격/실제 지연 피해의 positive control2와 경제 미구현에서도 무응답인 종료 가드1이다. 사용 replay/in-flight·100번째 보상 거부·이동/재접속·불량 입력의 후반 단정은 첫 경제 출력 부재로 **미도달**이며 구현 후 최초 실행/독립 검증이 필요하다. currency/revision 상한, 내부 고아 상태, guard 예외, 불변 snapshot과 전송 실패도 후속 독립 검증 범위다. 전체 테스트·봇·Unity·서식 검사는 미실행이다. RED와 첫 harness 오류는 제품 확정 실패로 집계하지 않는다.

TDD 가정 D1~D6의 구현 전 정리: 3틱은 테스트 허용폭이고 제품 지연 SLA가 아니다. 결과 itemId는 요청 값을 되돌리고, 새 불량 경제 입력은 무응답 drop하며 연결을 유지한다. 성공 보상마다 **기존 경로로 snapshot 전송 시도 1회, 맵 전환 중 생략 가능, 상태 보존·이후 조회로 확인**한다. 보상 거부에는 별도 통지를 요구하지 않고 조회로 현재 상태를 확인한다. 기존의 「snapshot push1건」 표현은 acceptance:32와 공용 SendToEntity의 silent skip 보존 계약을 충분히 반영하지 못해 메인 판단 `msg_b554a4b55c29`로 바로잡았다(사용자 결정 아님). PR1에는 이 알려진 한계를 공개하고 PR2에는 맵 전환 완료 뒤 재조회·표시 복구를 필수로 둔다. 원 acceptance와 TDD 원문은 보존한다.

완료 메시지의 세 identity/태그를 기존 수신 helper로 검증(allowed/exit0)한 뒤 worker-release를 수행했다. attached external pane이므로 retained/none이었고 동일 incarnation·완료 화면·빈 prompt를 확인하여 해당 pane만 close(ptyKilled=true)했다. `tdd-completion.json`, `tdd-completion-policy.json`, `tdd-settlement.json`, `tdd-close.json`에 보존하고 Delivery를 ack했다. 재사용하지 않는다.

### PR1 서버 구현과 자체 점검 — 독립 판정 전

신규 Sol `task_7cc288fe9f80`/`ctx_1652144986fc`는 `msg_65a9cc131b56`(2026-10-04T18:04:31Z)로 구현·근거 쓰기 종료와 succeeded를 보고했다. 지정/화면은 gpt-6.1-sol max, backend actual model은 unknown이다. 구현 입력 HEAD는 TDD와 최신 main 문서를 통합한 `a44272fb0219304c8c5324bd3698edfcaeec1967`이다. 원문 `sol-pr1/report.md`의 최종 SHA256은 `AB509475377D353078CC8D8207A9C866D4F4FC14F5F1EA2BA2ADFE6BF499F3E6`이며 첫 맥락·최종 파일/hash와 원시 실행은 같은 폴더에 있다.

구현은 Shared 아이템 정의/불변 슬롯과 서버 Items의 상태·전이·처치 정책·World 큐, 두 Inventory 핸들러, 합의한 GameSession/GameWorld/HandlerRegistry 최소 연결, ID35..38 append·version17·실제 생성물·Shared.dll의 21제품파일이다. ClientNet도 빌드했지만 DLL hash가 같아 diff에는 없다. 고정 TDD 파일, 기존 전투/맵/quest 파일, .meta, 생성기 소스와 설정은 보존했다. 메모리 상태의 맵 이동 유지·종료 정리와 참조 교체 commit을 구현했으며 DB hook/receipt/TTL/GC dedup·보상 치트는 추가하지 않았다.

| 실행 | 실제 결과·원시 위치 | 범위와 한계 |
|---|---|---|
| Windows 실제 생성기, Shared/ClientNet build | 각각 exit0, `sol-pr1/generate-packets`·`windows-*-build` | DLL 복사와 .meta hash 대조. Unity 실행을 대신하지 않음 |
| 고정 wire 테스트 | 26실행/26통과/0실패/0skip, `sol-pr1/wire-run2` TRX·exit0 | 선행 RED에서 미도달인 사용/replay/busy/스택 상한/이동/종료/불량 입력 후반 단정 실행 |
| 관련 기존 회귀 | 87실행/87통과/0실패/0skip, `sol-pr1/regression-run1` TRX·exit0 | 종료·처치·이동·지연 피해·dash·handshake·quest·party의 9클래스. 전체 스위트는 미실행 |
| 공식 서식 진입점 | exit0, 제품 report0건, Debug/Release258파일 보존, 도구 테스트244/244, 원본291입력 검증 | `sol-pr1/format-evidence`, 프로세스 한정 ExecutionPolicy Bypass 사용 기록. 원본 제품 자동 서식 수정 없음 |
| 변경/프로토콜 대조 | 허용21파일·보호19입력·기존34패킷 본문 보존, `sol-pr1/final-source-audit` | 생성기 header 갱신과 새 생성물의 행 끝 공백48건을 공개. 전체 diff-check exit2, 생성물 제외 exit0 |

첫 `wire-run1`은 Sol이 작성한 다중 행 guard의 IDE0011 빌드 오류로 테스트에 도달하지 못했고 같은 작업의 중괄호 수정 뒤 같은 필터를 재실행했다. 테스트 기대값을 바꾸지 않았다. 선행 RED·이 자체 빌드 실패·크래시를 독립 검증의 제품 확정 실패로 집계하지 않는다. 재화/overflow·revision 극한, 내부 고아 상태·guard 예외·송신 실패 미재지급·snapshot 불변성은 아직 코드 경로 설명이며 신규 Opus의 독립 실행 범위로 남는다.

Astra가 최종 보고·TRX 카운터·raw exit·해시·공유 파일 diff를 직접 대조했다. 보고 형식 보충 v1(`msg_2e530f5ec365`)은 최초 메인의 [소스]/[추론]/[미측정] 요건만 전달했고 원 구현 계약은 바꾸지 않았다. 빌드 시각 대조 중 리드가 ClientNet에 Shared 참조가 있다고 잘못 추정한 지시를 보냈으나 실제 csproj/props/stdout 대조로 즉시 철회했다(`msg_85ecbdfb9770`, 메인 정정 `msg_b9f366fdf750`). 두 빌드는 서로 다른 출력 경로이며 추가 재빌드는 시작하지 않았다. 이 리드 오판·정정은 `astra-context.md`와 Sol 최종 보고에 보존하며 작업자 위반으로 집계하지 않는다.

완료 메시지는 exact handle/Task/Dispatch·태그 helper allowed/exit0으로 대조했다. worker-release의 retained/external·processAction none 뒤 동일 incarnation `34153d1c-14c7-40ab-b86e-2c639285d3c5`와 최종 idle 화면을 확인해 해당 pane만 종료했다(ptyKilled=true). 근거는 `sol-pr1-completion.json`·`sol-pr1-completion-policy.json`·`sol-pr1-preclose.json`·`sol-pr1-close.json`이다. Delivery를 ack했고 reclaimable0이며 완료 작업자는 재사용하지 않는다.

후속 실행 조율: GameDev `msg_6eca0797e9a9`는 7777 점유/계획 없음과 순차 사용에 동의했다. 메인 `msg_02cdfd027894`는 Content 자기 checkout/Library에서 설치 Unity6000.4.7f1의 revision·실행 전 프로세스·전후 자산 변경을 대조하는 batch 검증 배정을 허용했다. 두 원문은 `gamedev-port-agreement.json`, `unity-ownership-main.json`이다. 실행 직전 다시 소유를 확인하며 타 프로세스·Library를 건드리지 않는다. scene/prefab 저작과 MCP는 메인 전용이다. 현재 실제 봇·Unity·DB·독립 판정·PR/CI는 미실행이다.

### PR1 첫 독립 검증 — 차단과 수정 인계

신규 Opus `task_7d1bef8088b1` / `ctx_5f3e6866eb7d`는 `msg_b182362d946a`(2026-10-04T18:51:30Z)로 검증 쓰기 종료를 보고했다. 검증 작업의 outcome succeeded와 제품 판정은 다르며 **제품은 NOT PASS**다. 입력 HEAD `326267713f90c32dbd86b33edcef48217a026fed`, verifies `task_7cc288fe9f80`. 원문 `opus-pr1/report.md` SHA256 `D1DAC7F8F4CB47A3A1D17833FA4223DFFC76E3E932A8DBA5214C1BA636C564E1`, `verdict.json` SHA256 `D1862B94A69CEFE421A69AC32B709F4D348F7118C154A9FBA145A9102F41E569`를 로컬 근거 root에 보존했다. Astra는 최종 원문 전체·실패 표본/대조군·TRX/XML·봇 관측·파일 diff/hash·서식 raw exit를 직접 대조했다.

| 번호 - 작업내용 | 판정·근거 | 다음 처리 |
|---|---|---|
| 1 - 이동 틈의 처치 보상 누락 | 중간 심각도·차단. HuntingGround에서 처치와 Town 포탈을 같은 틱에 제출하면 실제 적 사망/도착은 성공하지만 보상 뒤 조회 revision0(기대1). InventoryRegistry의 맵 순회 수혜자/활성 판정이 두 단계 MapMigration 사이 공백을 종료로 오인한다. `InventoryLifecycleRaceTests:47`, `opus-pr1/runs/items-run2`, `full-suite-2`. 뒤 순서 맵/한 틱 간격 이동 대조군은 통과. | 신규 Sol 수정→새 Opus. 같은 요구 계약·#1의 확정 NOT PASS **1회**. 초기 RED·자체 점검 수리·도구 오류·크래시·동일 산출물 반복 실행은 추가 집계하지 않음. |
| 2 - 기능 탐색 문서와 v17 이력 보완 | 낮음·코드 판정 비차단이나 PR 전 필요. FEATURE_MAP·server/protocol 영역에 경제 흐름이 없고 ProtocolVersion 이력은 v16까지. Sol 최초 문서 허용 범위 밖이었던 계약 누락은 Astra 보조 귀속. | 같은 신규 Sol 계약에 관련 문서와 이력 주석만 포함. 별도 새 목표/정책은 만들지 않음. |

| 독립 실행 | 실제 결과 | 범위·한계 |
|---|---|---|
| 원형 wire | 26/26, exit0 | 기존 TDD 파일 불변 |
| Items 독립 | 67전체/66통과/1실패(#1), exit1 | 신규41+기존26. 상한·overflow·revision끝·불변snapshot·고아·guard·송신 실패 포함 |
| 전체 slnx 빌드·서버 테스트 | 전906전체/899통과/2실패/5skip→후906/900/1실패(#1)/5skip, exit1 | 기존 BossBehaviorTests의 version16 단정만 요구17에 맞춰 분류(a) 수정. 같은 명령의 원시 전후 보존. 기존 skip5는 미실행 |
| 실제 InventorySmoke | helper exit0, PASS1/FAIL0 | Golem 평타3/Hit3 처치→rev1/재화10/주머니1→사용 rev2/재화60/주머니0→같은 bytes 재전송 Stale/불변. CheatCommand 없음. 종료 뒤7777/listener/서버·봇 없음·lock free |
| Unity6000.4.7f1(f3c3c4248748) | EditMode356/356/0실패/0skip, exit0 | 기존348+신규호환8. 자기 checkout/새 Library. 실제 UI·PlayMode·플레이는 PR2 미실행 |
| 생성물·Shared 소비자 | 격리 재생성 CR 차이 제외 동일, Shared metadata/상수/IL 차이0 | ClientNet DLL은 hash 불변. 원본 DLL/meta 쓰기 없음 |
| 공식 format-check | exit0, 제품0/265·독립0/5·0/17·0/9, 도구 테스트244/244 | 원본 자동 수정 없음. 실행 18:37:06Z~18:49:40Z. 프로세스 한정 ExecutionPolicy Bypass 기록 |

선행 wire 첫 실행 TRX 경로 변환, 신규 테스트 using 누락, DLL 비교 스크립트 타입 문자열화는 검증 하네스 오류로 원시와 후속 정상 실행을 구분했다. 종료 manifest는 raw exit1·68/69 일치이며 유일 차이는 허용된 봇 `Program.cs` 등록이다. 이를 입력69/69나 제품 변조로 보고하지 않는다. 신규 테스트3, 기존 version 테스트 수정1, 봇 시나리오/등록2, Unity 호환 테스트/meta2의 8파일만 검증자가 썼다.

Unity 부수 변경은 ProjectSettings.asset의 Standalone 정의에서 SENTIS_ANALYTICS_ENABLED가 빠진 1줄이며 원인은 미확정이다. 메인 `msg_7691eaa0154c`에 따라 정산 뒤 사본·diff/hash 보존 후 HEAD로 복원했다(`ProjectSettings-approved-restore.json`, diff exit0). PR에 넣지 않는다. 원본 Shared/ClientNet DLL/meta·Packages·기존 Assets meta는 불변이다. Unity가 저장소 밖 기본 TestResults.xml도 썼다는 사실은 원문 E-2에 남았다.

관찰 O-1/O-4의 이동 중 요청/drop 및 지연 push 유실은 실제 관측/소스 추론을 구분하고 #1 수정의 영향과 후속 PR2 조회 경계에서 확인한다. O-2의 max revision에서 stale 요청은 이미 불일치로 거부되므로 Stale 우선순위를 유지한다. RevisionExhausted는 수락 가능한 **새 변경**의 wrap 방지 요구이며 stale 거부 코드를 바꾸는 정책은 추가하지 않는다. O-3 현 클라이언트의 새36번 경고/drop은 PR2 핸들러에서 해소한다. O-5 handler/session ID 중복 검사는 방어적 중복 관찰로 일괄 정리하지 않는다.

정산: 정확 handle/Task/Dispatch 태그 helper allowed/exit0, worker-release retained/external 뒤 동일 incarnation `65c0c264-feff-413b-9c5a-a939dfdddfba`·최종 idle·agentTerminalHandle을 대조하고 close ptyKilled=true. **close 뒤 terminal list 재확인: 남은 작업자 pane 0**(Content Astra 하나만 남음, `opus-pr1-postclose-terminals.json`). Delivery ack 완료, 작업자 재사용 없음. 메인 `msg_4416da4d9f23`의 종료 후 목록 대조를 적용했다.

GameDev `msg_1bbca7872520`은 #1 수정에 Items 내부 우선, GameWorld 경제 연결·GameSession 경제 수명 등록/해제의 최소 변경에 동의했다. 공용 IsActiveSession/SendToEntity 의미·entity 발급/소유권·맵 순회/이동 순서·Quest/Party/전투·MapMigration/GameMap은 보존한다. 등록/상태의 단일 tick 소유, closing/disconnect/늦은 queued reward/다른 owner 거부와 정리를 새 계약에 넣는다. 공유 경제 연결 밖 변경이 필요하면 쓰기 전 정확 심볼/이유를 다시 조율한다. 현재 제품 수정·재검증·PR/CI·DB는 미실행이며 승인된 goal 안의 수정 루프를 계속한다.

### PR1 첫 수정 완료 — 신규 독립 재검증 전

신규 Sol `task_8650a2393592` / `ctx_0e9661cf734f`는 `msg_352f616c04fb`(2026-10-04T19:30:47Z)로 #1·#2 수정과 쓰기 종료를 보고했다. 입력 checkpoint `0f956d08076031b98a1dc24f68b06f0912478312`, 지정/화면 gpt-6.1-sol max, backendunknown. 최종 `sol-pr1-fix1/report.md` SHA256 `CC6B5671585993BA8247897702C4ABCEF0D3F8E45A7C537F7223C0F6EE4C8313`, 쓰기 종료19:30:12.2035820Z와 원시 hash는 `evidence-hashes.json`에 있다. Astra는 보고 전체와 종료 시각 추가·실제7파일 diff·TRX/raw exit·입력hash 대조를 읽었다.

경제 등록은 실제 GameSession 입장/AddPlayer 뒤에만 생성하고 기존 World close의 Forget에서 해제한다. InventoryRegistry가 entity→session 소유를 맵 존재와 분리해 확인하며 lazy 경제 상태를 유지한다. 처치 값과 함께 당시 등록 세션 참조를 고정해 늦은 다른 수명 지급을 거부하는 선택이다. 이는 죽은 EnemyEntity를 붙잡지 않는 기존 목적을 유지하는 수명 증명 변경이며 receipt/TTL/재시도/DB 구조를 추가하지 않는다. 공용 IsActiveSession은 등록 시 입장 확인으로만 사용하고 그 본문·SendToEntity·맵/전투/Quest/Party는 보존했다. 공유 diff는 GameWorld 생성자2줄 삭제와 GameSession 경제 등록/주석4줄 추가다.

문서 #2는 FEATURE_MAP·server/protocol 영역의 실제 경제 입력/소유/수명/wire 탐색 경로와 ProtocolVersion v17 주석2줄을 보완했다. Current17·PDL/generated·실제 wire·DLL은 바꾸지 않았다. 제품/문서7파일 외 변경은 없다. 최초 메모는 구현 전에 작성했고 실제 준수 위치를 완료 뒤 갱신했다.

| 자체 실행 | 실제 결과·원시 | 한계 |
|---|---|---|
| 같은 Items 명령 전후 | 67전체/66통과/1실패→67/67/0, raw exit1→0. `runs/items-before`, `items-after` | 테스트 기대값/파일 불변. 후 실행 TRX는04_07_14 파일이며 함께 보관한 전 실행 사본과 합산하지 않음 |
| 전체 slnx 빌드/테스트 | 906전체/901실행·통과/0실패/기존skip5, raw exit0. `runs/full-suite` | skip5는 미실행. 새 등록 branch별 독립 시험은 아직 없음 |
| 공식 서식 | raw exit0, 도구244/244, report4개0건, Debug/Release262파일 보존.19:10:55Z~19:22:49Z | 프로세스한정Bypass 명령 기록. 원본 자동 수정 없음 |
| 최종 입력·보호 | 입력82 중75불변/허용7변경/예상밖0, 테스트154파일 집합/hash 불변, DLL/meta·PDL/generated 보호6불변, diff-check0 | `state-after.json`·`state-after.exit.txt`; 리드도82입력을 재대조 |

O-1의 queued 조회는 이동 틈에도 snapshot(rev1) 응답이 관측됐다. 반면 새로 들어오는 migrating 입력 gate는 기존 drop을 유지한다. **O-4는 이번 실행에서 push none/뒤 조회rev1로 관측**됐으며 보상 상태 보존을 push 도착 보장으로 확대하지 않는다. 공용 map 송신의 제한은 현재 문서에 명시했고 메인 `msg_0ec9c3ef81b1`에 보고했다. 새 Opus는 이 계약 적합성·등록의 실제 수명/owner/종료·지연 피해 경계를 판정한다. 원본 Shared 소비자 입력이 같으면 이전 HEAD3262677의 Unity356/356 근거를 대조하되 새 HEAD에서 실행한 것으로 보고하지 않는다.

정산은 exact triple/tag helper allowed/exit0, worker-list completed/succeeded·실제 pane/동일 incarnation `21aa4d61-01f1-44c5-b730-6dd9e66baabf`·최종idle 대조 후 release retained/none→close ptyKilled=true다. **close 뒤 terminal list 재확인: 남은 작업자 pane0**(`sol-pr1-fix1-postclose-terminals.json`). Delivery ack, 작업자 재사용 없음. #1의 확정 실패 횟수는1회 그대로이며 자체 GREEN을 독립 PASS로 처리하지 않는다. 전용7777/InventorySmoke·Unity·DB·CI·PR은 이 수정 작업에서 미실행이다. 다음 단계는 신규 Opus의 수정 실사·독립 테스트·실제봇 재검증이다.

### PR1 첫 수정 독립 재검증 — #1·#2 해결, #3 수정 인계

신규 Opus `task_76046842e657` / `ctx_26a909d5c2d5`는 `msg_27e1c69d0b09`(2026-10-04T20:26:20Z)로 쓰기를 종료했다. 입력 HEAD `03c4ce6d63de5932bd277b889c2995aa44890374`, verifies `task_8650a2393592`. 최종 `opus-pr1-fix1/report.md` SHA256 `90977C9DC9EF016097BC91F244B4B6BC04267DB7157039816D630CB35444E76C`, `verdict.json` SHA256 `721F9CB99C20B37D3AC90D920E8418CB7509EB42DF3547B0B9EFD149F3080ECB`를 로컬 근거 root에 보존했다. Astra는 두 원문 전체, 새 테스트 두 파일, 실제 TRX·봇/서식/입력 대조와 #3 관련 소스를 읽었다. 검증 작업은 succeeded지만 **제품은 #3으로 통과 보류**다.

| 번호 - 작업내용 | 독립 판정·근거 | 다음 처리 |
|---|---|---|
| 1 - 이동 틈의 처치 보상 누락 | 해결. 원형 재현·두 대조군 통과, 실제 Mage 지연 피해의 이동 틈 처치와 틈에서 받은 골렘 주머니 사용도 통과. 종료 전후 등록/상태/플레이어 정리, queued 입장·늦은 job·다른 owner 경계 확인 | 회귀 테스트 유지. 기존 확정 실패1회 뒤 해결이며 새 #3과 합산하지 않음 |
| 2 - 기능 탐색 문서와 v17 이력 | 해결. 실제 등록·해제·wire·소유 흐름과 문서 일치, 상대 링크51/0 및 두 새 앵커의 네 링크 확인 | 유지 |
| 3 - 수혜자 교체 지점 주석과 등록 owner 결합 | 신규 낮음·규칙 차단1회. KillRewardPolicy:5-6은 수혜자 선택의 교체 지점이 하나라고 하지만 Registry:81-82는 처치자 세션을 캡처하고 :131-132는 recipient와 그 owner의 동일성을 요구한다. 현재 killer-only 동작에는 영향 없음. 소스 대조이며 다른 수혜자 정책 실행은 하지 않음 | 새 Sol이 현재 정책/owner 결합을 설명하는 주석을 최소 수정하고 신규 Opus가 문서·동작 불변을 실사. 새 분배 정책이나 기능 확장 없음 |

| 실제 독립 실행 | 결과·원시 | 범위·한계 |
|---|---|---|
| 원형/추가 Items | 67/67 → 18경계 추가 후85/85. 최종 주석 바이트 `items-new-3`도85/85, exit0 | 신규 테스트의 두 대조군은 처음 지연 push와 조회 창을 섞은 fixture 오류가 있었으며 정착 틱과 push1건 단정으로 수정. 요구값 완화 없음 |
| 수정 전 판별 대조 | `prefix-control-1` 76/68/8, exit1 | 세 보상 동작 실패와 새 등록 필드 부재 다섯 건을 구분. fixture 전용 owner 파일은 이전 생성자와 맞지 않아 제외 |
| 전체 slnx | 924전체/919실행·통과/0실패/기존skip5, `full-suite-1` exit0 | 새 테스트 주석 정정 전 바이트, 실행 토큰은 동일. CI의 정확한 `--no-incremental` 명령은 아직 미실행 |
| 실제 InventorySmoke | PASS1/FAIL0, exit0,19:58:29Z~19:58:51Z | 실제 골렘3타/Hit3 → rev1/재화10/주머니1 → 사용rev2/60 → 동일bytes Stale·불변. 이동 틈 네트워크 타이밍 자체는 미재현 |
| 서식·입력·공유 소비 | format-check2회 exit0, 최종제품0/267·독립0/5·0/17·0/9·보존264·도구244/244. 입력95/95, 추적제품diff0. Shared IL차이0 | 최종 주석 바이트의 추가 전체 실행은 후속 한정검사 지시와 처리 관계가 확인되지 않아 아래에 별도 기록 |
| Unity | 같은 소비 입력 hash와 이전356/356을 대조 | **HEAD3262677의 이전 실행**이며 현재 재실행·UI/PlayMode/실제플레이 완료가 아님. DB·PR·CI 미실행 |

O-4 범위는 메인 `msg_b554a4b55c29`(20:08:41Z, `o4-main-scope-decision.json`)가 **현재 goal 범위 안 구체화라는 메인 판단**으로 정했다. 기존 SendToEntity와 전송 시도1회·맵 전환 중 생략 가능·상태 보존/이후 조회를 유지한다. 캡처 session 직접 송신 대안은 채택하지 않았다. PR1 알려진 한계에 「맵 전환 틈의 처치 push 없음, 표시 복구는 PR2」를 적고 PR2 맵 전환 완료 뒤 재조회·표시 복구를 필수 검증한다. 위 PR 경계와 D1~D6 문구는 검증 쓰기 종료 뒤 이 판단대로 수정했다. 이 판단은 독립 PASS나 병합 승인이 아니다.

보고 불일치: 최종 Opus 원문은 O-4 판단이 남았다고 적었지만 위 메인 결정은 이미 내려졌고20:09Z `msg_e2e93726d454`로 원문을 전달했다.20:07Z의 주석 후 한정검사 지시도 반영 확인 없이20:13Z 전체format2가 시작됐다. 최종 화면에는20:15Z의 태그 있는 메시지 확인 안내 draft가 남았다. 공개inbox 표시도 실제 회신한 앞 메시지가 read0여서 수신·처리 누락 원인은 미확정이다. 원판정을 고치지 않고 `astra-context.md`에 차이를 보존했으며 메인 `msg_2dd6954855cb`로 즉시 알렸다. 다음 계약은 현재 결정 원문을 최초 고정 입력으로 포함한다.

정산: exact triple/tag allowed0·completed/done·같은 incarnation `ed20d00f-25e0-492b-981d-e084f380988c`와 최종 idle 확인, release retained/external/none→close ptyKilledtrue. **close 뒤 terminal list 재확인: 남은 작업자 pane0**(`opus-pr1-fix1-postclose-terminals.json`), 완료 delivery ack. 태그 있는 미제출 안내 draft는 사용자 직접지시나 pane 종료 보류 사유가 아니며 완료 세션을 재사용하지 않는다.

### PR1 #3 주석 수정 완료 — 신규 독립 실사 전

신규 Sol `task_74d5ec2d4743` / `ctx_63b9fa36c09d`는 `msg_cba40bedf4d6`(2026-10-04T20:50:19Z)로 쓰기 종료를 보고했다. 입력 HEAD `626e09a7c7f62957a094739e96e5f106194b9d3c`, 최종 `sol-pr1-fix2/report.md` SHA256 `7200F71639CAD9B10EE696CF712FFFEC134495A61786821004A1E67850FDB1B5`다. KillRewardPolicy:5-7의 머리 주석만 현재 임시 killer-only 정책·드롭 원천과 Registry의 처치자 owner 캡처/recipient 동일성 검토 필요를 설명하도록 바꿨다. 실행 코드·테스트·문서·프로토콜·DLL은 불변이다.

자체 확인은 정확한 허용 치환 및 머리 주석 외 바이트 동일, UTF-8/LF/EOF/행 끝 공백, diff-check0, 보호102/102와 추가 고정입력10/10 불변이다. Astra도 최종 보고 전체·원시 명령/결과·실제 diff·103입력을 대조했다. 행동 테스트·Roslyn·빌드·봇·Unity·DB·전체서식은 이번에 실행하지 않았다. 이전85/85·919통과/skip5·botPASS·Unity356은 각각 앞선 실행 근거이며 이번 재실행이 아니다. #3 독립 해결은 새 Opus 판정 전까지 보류한다.

정산: exact triple/tag allowed0, release retained/external/none 뒤 같은 incarnation `444d9baa-775d-4b86-8f8f-1ceba2be5035`·completed/done·최종 idle 대조 후 close ptyKilledtrue. 직후 terminal list의 남은 작업자 pane0(`sol-pr1-fix2-postclose-terminals.json`), 완료 delivery ack. 새로운 Opus는 주석과 현재 계약·목표의 정합성 및 O-4 메인 판단/PR2 필수조건을 독립 실사한다. PR/CI·병합은 아직 미실행이다.

### PR1 #3 독립 실사 — 해결과 PR 관문

신규 Opus `task_a8d72f3da04a` / `ctx_11d278cb5d8e`는 `msg_96aa35e65e1f`(2026-10-04T21:10:33Z)로 **#3 해결·새 결함 없음**을 판정했다. 입력 HEAD `6b9da371fa7b94fadb56283031c81cd1887eed2a`, 최종 `opus-pr1-fix2/report.md` SHA256 `1A1C3D84A8C69729D06C5DE3354AB5B91BD36B1F1F7F58D36FD397DAEBBBFA55`, `verdict.json` SHA256 `333BE25D11C2192C826A1A25F71287625391019621856B4466D5D8EB968CD4CF`다. Astra가 두 원문 전체와 실제 diff/명령/입력·이전 실행 표본을 대조했다.

현재 임시 killer-only·드롭 위치·Registry의 처치자 owner 캡처와 recipient 검사가 새 주석에 정확히 드러난다. 주석 외 바이트는 동일하고, 고정 입력115/115·규칙6절누락0·서식바이트/diff-check를 확인했다. #1·#2의 이전 해결은 유지하며 새 기능 검증으로 확대하지 않는다. O-7은 설계 초안:34의 한 지점 교체 표현에 대한 비차단 관찰이다. 메인 원문은 「바꾸기 쉬운 자리」였고 실제 구현에서 수혜자 교체는 Registry의 owner 결합 검토도 필요하다. 현재 분배 정책을 바꾸거나 열린 기획 질문을 닫지 않았다.

O-4 메인 판단과 goal:25·:174의 PR2 필수조건 반영, 이전 보고 불일치의 원문 보존·미확정 원인도 독립 대조했다. 이번에는 행동 테스트·빌드·봇·Unity·전체서식·DB·CI를 새로 실행하지 않았다. 앞선85/85·919통과/skip5·봇PASS·Unity356은 각각 그 실행 HEAD/입력 근거이며 현재 재실행이 아니다. 정적 goal 대조의 raw exit1은 마지막 grep이 닫힌 Sol handle0건을 센 기대된 부재다. Claude 안전 검사에 한 번 거부된 검사 명령은 삭제 없는 스크립트로 수행했고 중간 사본을 보존했다.

정산: exact triple/tag allowed0, release retained/external/none, 같은 incarnation `4980ff5b-6ca6-42db-9738-4712a2d82257`·completed/done·최종 idle 확인 뒤 close ptyKilledtrue, 직후 terminal list 작업자0(`opus-pr1-fix2-postclose-terminals.json`), delivery ack. 현재 결함 계열의 열린 차단은 없으며 남은 PR 관문은 **정확한 head CI, PR 본문의 「맵 전환 틈의 처치 push 없음, 표시 복구는 PR2」 공개, 그 PR 병합 직전 사용자 명시 승인**이다. CI/PR 원격 실행 원시는 같은 로컬 근거 root에 보존하고 메인에 전달한다. PR1을 전체 goal 완료나 PR2 UI 완료로 처리하지 않는다.

### PR180 main 동기화와 검증 수행 판정 — 2026-10-05

메인 `msg_afa082de8aed`·`msg_776da3f3047e`에 따라 PR178의 main `ecca463c6e4bb8d4aa44a75aaa57c581c1c5c70c`를 받았다. 새 Sol `task_3d180a6651ab` / `ctx_b870b7e58397`가 CURRENT:8의 Content 상대 링크와 :19의 첫 문장만 해소했고 다른 행·문장은 PR178 쪽을 보존했다. 신규 Opus `task_7d72e6c4fbf1` / `ctx_02654575ca7f`의 `msg_39272b51fad0`은 문서 정적 실사 통과·내용 결함 0을 보고했다. 원문은 로컬 근거 root의 `opus-pr1-main-sync/report.md`(SHA256 `474ECAB3CAAB7904C2CE849B57AB07BA9FE65D173C44F89AFCFED196716CC5BE`)와 `verdict.json`(SHA256 `3F11F9B07A4FE02AD19514CB61E4AA03223AC5EE6D741E88951131EEBBA75C27`)이며 그대로 보존한다. 고정 입력35/35·index 내용 불변과 문서 두 구간을 Astra도 대조했다. 빌드·테스트·Unity·봇·DB·CI를 이 실사에서 실행하지 않았다.

검증 수행에는 **쓰기 경계 위반 1건**(허용 근거 폴더 밖 `%TEMP%/blob-goal.md` 생성·삭제, 자기 공개)과 **보고 정확성 결함 1건**(공개한 위반을 보고서의 「위반 없음」·`ruleViolation=false`로 판정)이 있었다. Astra는 `msg_cf79870933be`로 즉시 메인에 보고하고 커밋을 보류했다. 메인 `msg_65f045869377`(2026-10-05T06:24:40Z)은 직접 문서 diff·원문 hash·임시 파일 부재를 대조한 뒤, 영속 변경 없음·고정 입력 불변·두 구간 문서 실사라는 근거로 **내용 판정을 수용하고 새 Opus 재실사 없이 진행**하도록 결정했다. 두 결함을 면제하거나 일반 TEMP 쓰기 예외로 만들지 않는다. 첫 `git status`의 index mtime 갱신도 공개됐으나 내용 hash·2846줄은 불변이다. 관련 원문·메인 결정은 `pr1-main-sync-scope-decision.json`과 정산 근거에 보존한다.

두 작업자는 각각 정확한 완료·동일성 대조 후 release→close→실제 terminal list 작업자0→ACK로 정산했고 재사용하지 않았다. O1~O3 비차단 관찰은 메인 지시대로 이번 변경 범위 밖으로 두었다. 이 절은 메인이 명시한 goal·정산 기록이며, 독립 검증 입력에 사후 추가한 Astra 기록이다. 새 merge head의 CI와 변경 범위 대조, 그 head에 대한 사용자 재승인·병합은 별도 관문이다. 기존 ce3267a 승인을 새 head 승인으로 확대하지 않으며 PR2의 맵 전환 후 재조회·표시 복구와 전체 goal은 미완료다.

### PR180 승인 병합과 PR2 착수 — 2026-10-05

메인 `msg_a61c279bc351`(2026-10-05T07:17:31Z)이 전달한 사용자 대시보드 결정은 새 head `3ee38bf` 병합 승인이다. 메인이 전달한 사용자 결정이며 이 세션의 사용자 직접 입력으로 격상하지 않는다. 현재 head·CLEAN·두 SUCCESS를 확인하고 `gh pr merge --merge --match-head-commit 3ee38bf357f64542543fd1de0c7583d1a9b9bec6`를 실행했다. 실제 merge는 `8d1e8856a99e9a5ed74aa291294accd2299aaaf6`(2026-10-05T08:05:38Z), 부모는 `466aa025b7934ad058bb78636885cce18f0a96eb`·승인 head다. fetch된 origin/main에 포함됨을 `merge-base --is-ancestor` exit0으로 확인하고 clean 상태에서 PR2 브랜치를 만들었다. 원문은 근거 폴더의 `pr1-new-head-approval.json`·`pr1-merged.json`; 메인 보고 `msg_3eb08acaec89`다.

CI `37272468313`의 첫 실행은 924개 중 918통과·1실패·5skip, 동일 head의 한 번 재실행은 919통과·0실패·5skip이었다. 실패는 `CombatSmoke_ZeroLag_Succeeds`의 공격 거리 15초 수렴 timeout이며 원인 미확정이다. 재실행 성공을 원인 해결로 표현하지 않는다. 원시는 `pr1-main-sync-ci-dotnet.log`·`pr1-main-sync-ci-dotnet-attempt2.log`와 `pr1-main-sync-ci-audit.json`에 보존했다. 메인이 같은 테스트의 두 번째 흔들림으로 판단해 Content BACKLOG 후보 기록을 지시했다. 후보 등록은 PR2 제품 수정이나 테스트 완화를 허용하지 않는다.

PR2의 검증 등급은 **강**(연결 종료/세대·맵 전환 실패 수명, 서버 상태 표시·패킷 경계 및 제품+테스트 50줄 이상 예상)이다. 예상 규모를 실측처럼 보고하지 않으며 완료 diff의 파일/줄 수를 원시 명령으로 기록한다. 이 goal은 이미 R-7 검토와 메인 보완 승인을 받았고 `goal-review.md`의 INV-19 및 채택 내용을 고정 입력으로 유지한다. 같은 PR2 범위의 기존 검토를 새 검토 실적으로 부풀리지 않는다. UI는 기존 공유 데이터와 v17 패킷을 소비하고 scene/prefab 저작·DB·서버 정책은 변경하지 않는다.

### PR2 선행 TDD 정산과 문서 후보 — 2026-10-05

신규 Opus task_60b2e07c356a / ctx_a325a125aa44는 msg_301a774a11a7로 선행 RED 준비를 완료했다. InventoryClientContractTests·InventoryClientTestSupport와 새 meta 2개를 썼고 Unity6000.4.7f1(f3c3c4248748) EditMode filter를 한 번 실행했다. XML은 32건/1통과/31실패/0skip, exit2·error CS0이며 27건은 InventoryState 미구현, 4건은 Ready 뒤 조회 미송신이다. 통과 1건은 실패 진입의 음성 보존 단정이다. 제품 PASS나 제품 확정 실패 집계가 아니다. 입력35/35와 기존 meta1129개는 불변이었다. R5 사용 잠금/재시도·R6/R7 표시/입력·R8 UI·실제 플레이·기존 전체 회귀는 후속 독립 검증 범위다. 공개 Instance 표면 명확화 v1.1은 msg_d7097e76067b에 대한 msg_660d77c31a56 공식 reply로 고정했다.

원문은 근거 root의 opus-pr2-tdd/report.md(SHA256 44DB17A6E0C3FB09738F5764A36A63F7446CC7DF227CDB590A05B80A4F7502DD), verdict.json(183DF41DC4AD8DE1A2E2BDFF7C041535EE7C4D59B6C85D5B413D4FD29665BE00), runs/unity-editmode-red-1/editmode-results.xml(49447C19BC477D9C9E6FE602F9398E0F7D672738BCF08B18183D7A0B12E938E9)이다. Astra는 원문과 수치·실제 diff를 대조했다. 정확한 완료/태그/identity 확인→release retained/external/none→동일 incarnation idle→close ptyKilled true→실제 terminal list 작업자0 순으로 정산했고 재사용하지 않는다.

Unity는 ProjectSettings.asset의 SENTIS define 한 줄을 바꿨다. 기존 메인 msg_7691eaa0154c에 같은 변경의 반복 복원이 명시되어 있어 사본 보존 후 이 파일만 HEAD로 복원했다(934110…→C1412…·diff0, ProjectSettings-pr2-tdd-approved-restore.json). 반복 후보 한 줄은 다음 Sol에 배정한다. LocalLow TestResults.xml·GiCache 자동 쓰기는 당시 계약 경계 판정 미확정으로 메인에 msg_dd707149348e로 보고했다. 삭제하거나 위반 없음으로 바꾸지 않는다. batch는 08:37:20~08:38:33Z, 새 Unity 결정은 08:43:24Z 수신이며 이후 Unity 재실행은 없다. 다른 기존 relay 종료 확인과 이 자동 부산물 정산 판단을 기다리며 제품 구현 준비는 계속한다.

별도 Sol task_b54380de9967 / ctx_6b1ca1c2b7ab는 BACKLOG에 combat-smoke-zero-lag-flaky 한 행만 추가하고 정산·종료했다. 원문 sol-backlog/report.md(SHA256 2AF58A952CEE5E26598461C8BA8C358E5FE83877AF02AEF1E191D6B2CC25A7BD), 실제 +1/-0과 자체 점검을 확인했고 신규 독립 Opus의 문서 실사는 아직이다. 한 heartbeat의 subject/body 정책 불일치는 교정 후 올바른 alive를 확인했으며 원문·교정 내역을 보존한다. 같은 head 재실행 성공은 원인 해결이 아니고 이 후보의 조사는 PR2 제품 범위 밖이다.

메인 정산 결정 msg_5c94db82c2ab(08:54:22Z): LocalLow의 두 자동 부산물은 저장소·제품·전역 설정 영향이 없어 이번 경계 위반으로 집계하지 않으며, 이를 명시하지 않은 것은 **Astra 계약 누락**으로 귀속한다. 원본 TestResults.xml과 공유 GiCache는 삭제하지 않는다. Astra가 보존한 결과 사본은 59,838B/mtime08:38:31Z/SHA49447C…2E938E9로 명시 결과 XML과 동일했다(pr2-tdd-localLow-copy.json). 다음 실행은 결과 절대 경로·허용 LocalLow/Unity/Caches·그 밖의 추가 경로 보존/보고 및 relay0 관문을 계약에 넣는다. 다만 기존 command.json에는 이미 -testResults가 명시돼 있고 editor.log14282~14283은 두 경로 저장을 연속 기록하므로 옵션 미지정 원인/지정만으로 예방된다는 주장은 확인되지 않는다. 이 사실은 msg_6cc7f2907d06으로 즉시 정정 보고했다. 08:55:40Z 관측은 Unity.exe0/relay1(17:25 GameDev 소속 PID33908)이므로 아직 새 Unity 실행을 허용한 것으로 처리하지 않는다.

### PR2 제품 구현과 자체 점검 완료 — 독립 판정 전

신규 Sol task_4666c26b7ec7 / ctx_a14947307438의 msg_504293836734(2026-10-05T09:46:50Z)는 상태 미러·수신 검증·Ready 뒤 재조회·runtime 8슬롯 UI/사용 입력·기능 문서와 승인 SENTIS 후보 한 행의 구현 완료를 보고했다. 현재 HEAD8d1e885의 미커밋 산출물이며 자체 diff22파일(C#11·문서3·신규meta8), +878/-3줄이다. 선행 TDD4파일·Astra goal·먼저 쓴 CombatSmoke 후보 행은 이 Task 수치에 포함하지 않는다. 보고 원문 sol-pr2/report.md SHA256 615006F7D60FECD87BE1BF2337259B107E4A652D296E75C3AD53B08FD871C95F와 audit/owned.diff·output.json을 보존했다. 강 등급은 실제 변경 규모와 연결/전환 수명·패킷 경계에서도 유지된다.

자체 Unity6000.4.7f1의 InventoryClientContractTests 두 실행은 각각32통과/0실패/0skip·exit0이다. 두 번째는 패널 disable의 Canvas/입력 수명 보완 뒤09:29:18~09:29:33Z에 수행했다. 같은32개를 두 번 실행한 것이며64개 요구로 합산하지 않는다. XML SHA는 첫9A2134F1A077A5D558F141952CCC1BDDB4326FCBF8DD3ED8161DD04E589C4257, 둘째E4764A815453812885F377CD1AB8C3F98055BDCF9F2FBF1283F9E9FEF2B9BD05다. Astra는 두 XML의 실제 case수와 결과·command/result·현재 diff·최종 제품 hash를 대조했다. 마지막 실행 뒤 C# 변경0이며 신규 폴더 meta의 공백3개만 정리됐고 이 정리 뒤 Unity를 재실행하지 않았다. 이는 독립 PASS가 아니다. 전체 EditMode·PlayMode·실제 UI/클릭/resize·GameServer 경로·timeout/송신 실패/상세 결과·구독 수 검증은 후속 Opus에 남긴다.

실행 전 relay/프로젝트 Unity/unknownUnity0을 직접 확인했다. 반복 SENTIS 한 줄과 LocalLow 중복 XML/GiCache는 허용 자동 부산물로 사본·hash·시각을 보존했고 직접 설정 수리를 하지 않았다. Astra는 정산 뒤 같은 설정 파일만 기존 반복 복원 결정 msg_7691eaa0154c에 따라 HEAD로 복원했다(ProjectSettings-pr2-sol-approved-restore.json, diff0). 이후 정산 대조는 보호 입력51/51·기존meta1131/1131·tests31/31·최종 제품22/22 hash 일치다(sol-pr2-astra-settlement-audit.json). 첫 자체 audit의 no-index exit1 분류 오류와 새 폴더 meta 공백은 원시를 보존해 정정했으며, 사전 메모의 최초 시각 추정은 파일 생성09:05:50Z→첫 제품09:09:51Z metadata로 보완됐다. 최종 원문은 이 제한을 공개한다.

정산: 정확한 완료 identity/태그 allowed0→release retained/external/none→동일 incarnation6401d3a0-5311-46a9-9d77-41de14995cc2와 completed/done·최종 idle 확인→close ptyKilledtrue→실제 terminal list의 Content 작업자0→ACK. 재사용하지 않는다. 메인 msg_08bc350cce6b는 이후 신규 독립 Opus 한 세션의 Unity MCP 시트를 예약했고, 기동 직전 relay0 재확인·지정 mcp-config·handle/incarnation/Task와 시트 획득 관측 보고·종료 후 relay0 반납을 조건으로 했다. 수정 회차나 다른 세션에 승계하지 않는다. GameDev msg_9c760e2da884는 7777/Unity 사용 계획 충돌 없음에 동의했지만 Windows 관측뿐이므로 실제 실행 직전 Windows/WSL·소유를 다시 확인한다. 두 원문은 opus-pr2-seat-decision.json과 sol-pr2-heartbeat-ack-1.json에 있다.

### PR2 첫 독립 검증 — 결함 3건과 수정 인계

신규 Opus `task_387c600bca06` / `ctx_c63817055b98`는 `msg_609c3c077c4c`(2026-10-05T11:27:48Z)로 쓰기를 종료하고 **차단**을 판정했다. 원문은 근거 root의 `opus-pr2-review/report.md`·`verdict.json`, 고정 계약은 `opus-pr2-review-contract.md`와 `opus-pr2-test-scope-addendum-1.md`다. 실제 모델은 첫 실행 명령과 화면의 Opus5.5·xhigh로 구분하며 backend는 unknown이다. 제품 파일은 변경하지 않았고 테스트 3개와 meta 3개를 추가하고 기존 테스트/fixture 6개를 보완했다.

| 번호 - 작업내용 | 독립 관측과 후속 | 확정 실패 |
|---|---|---|
| 1 - 편집기 지연 입력 보존 | 5초 인벤토리 timeout이 같은 delay 전제의 FIFO와 공유되어 50ms intent가 약4.98초 지연. `SimulatedLatencyIntents_AreNotHeldBehindTheInventoryTimeout` RED. 기존 송신 순서·세대·맵 epoch·timeout 비용 상한을 보존해 수정한다. | 이번 PR2 계약 1회 |
| 2 - 인벤토리 클릭과 공격 입력 분리 | 실제 포인터 사용 클릭이 C_ItemUse1과 C_Attack1을 함께 전송. 로그 관측이며 공격 부재 단정은 아직 없다. UI 밖 마우스·키보드 공격을 유지하며 수정하고 새 Opus가 단정과 보존 대조군을 보완한다. | 이번 PR2 계약 1회 |
| 3 - 실제 화면의 인벤토리 글자 표시 | 제목·재화·새로고침 칸 높이30이 TMP 선호 높이38.18/30.54보다 작아 characterCount0. `RealTown_EveryPanelLabelIsDrawnOnScreen` RED. 문자열 일치만으로 표시 성공을 판단하지 않는다. | 이번 PR2 계약 1회 |

원시 XML은 전체 EditMode388/356통과/32실패 →400/400/0, 기존 장면10/10, 신규 인벤토리 장면5/3통과/2실패, 실제 서버 inventory1/1·기존 mapentry1/1이다. 기존 실패32건은 a26·b6·미분류0으로 전수 분류됐고 원래 generation/map epoch/party/component 요구는 보존했다. 대화형 실제 화면에서는 골렘·일반 적 처치로 재화20·재료1·주머니1, 포인터 사용 후 revision3·재화70을 관측했다. 입력은 가상 Input System 장치이며 메뉴 진입은 직접 호출했다. **사람 손 입력·플레이어 빌드·DB·CI는 미실행**이다. 현재 화면/입력 결함이 있어 데이터 경로 실행을 전체 PASS로 표현하지 않는다.

Astra는 판정 원문·메모·XML 6실행의 카운터·화면 crop/driver·규칙과 파일 경계 표본을 대조했다. `audit-opus-pr2-settlement.ps1`의 실제 실행 결과는 `opus-pr2-astra-settlement-audit.json`이며 입력106·기존meta1139·기존tests31 중 허용 테스트/보고한 설정 외 drift0이다. 새 제품 테스트 실행이나 독립 전수 감사로 합산하지 않는다. 완료 identity/태그 allowed0→release retained/external/none→동일 incarnation idle→close ptyKilledtrue→실제 terminal list 부재·Unity0/relay0→메인 시트 반환 `msg_7bfb22d99da5`→ACK를 마쳤다. 이번 시트 예약은 종료됐고 다음 Opus에 승계하지 않는다.

규칙 위반 1건: 검증자가 11:12Z scratchpad TEMP에 `patch-driver.mjs`를 직접 쓰고 11:14:05Z 삭제했다고 보고했다. 원문은 보존했고 즉시 메인 `msg_50aa1f494011`로 보고했다. 메인 운영 판단 `msg_dc98852233ed`는 차단 판정의 결함1~3을 유효한 수정 입력으로 쓰되 위반을 기록하고 다음 계약에 임시 쓰기 금지 원문과 실제 필요한 driver 경로를 명시하도록 했다. Sol 보고의 `UnityClientSession.cs:391` 짧은 버퍼 guard 설명 누락(O1)도 같은 메시지로 보고했으며 신규 Sol 보고에서 기존 변경까지 정정한다. 비차단 관찰은 아래 후보로만 둔다.

### Unity 연결·설정에 관한 적용 결정

이번 Opus의 첫 MCP 성공은10:38:47Z였고 이후 Connection revoked가 발생했다. 사용자가 시트 미준비(`msg_7397defef4aa`)→결제·시트1개 활성(`msg_ec9f594e0014`)→연결 승인 완료(`msg_3eb283d8fb90`)를 메인을 통해 알렸다. 승인 전 추가 시도를 하지 않았고 무부작용 확인 성공11:09:03Z 뒤 실제 화면 검증을 재개했다. 이는 사용자 진술·실제 호출 결과를 구분한 기록이며 거부의 기술 원인 확정이 아니다. 사용자가 이동한 보조 모니터 위치는 유지했다. 현재 작업에 추가로 켤 MCP 옵션은 관측되지 않았고, DLL 참조·Reflection 제한은 우회하지 않았다. 새 Opus는 기동 직전 메인에 시트를 요청하고 새 연결 승인 필요 여부를 확인한다.

메인 `msg_d1f7f41b5e47`(2026-10-05T11:28:08Z)이 전달한 최초 사용자 원문: 「1) 아이템·인벤토리 PR - Unity 클라우드 연결이 새 조직(roy_131)으로 바뀐 것을 유지할지 → A 새 연결 유지, 이 PR에 별도 커밋」. `ProjectSettings.asset`의 cloudProjectId=`1094041a-5db2-4e67-9ca2-c413d1238be3`, projectName=`03_Client`, organizationId=`roy_131`을 유지하라는 결정이었다. 변경 주체는 미확정이며 사용자 시트 활성화·재승인 시점과 겹친다. 원본은 `ProjectSettings-pr2-opus-cloud-observed.asset`에 보존했다. **아래 후속 사용자 결정이 별도 커밋 지시를 대체했다.**

별도 커밋은 실제 시도했으나 `.githooks/pre-commit:20-44`가 cloud-only 변경을 자동 unstage하고 exit1로 차단했다. 해당 훅은 팀원마다 다른 Cloud 식별자를 서로 덮는 것을 막는 기존 정책이다. HEAD8d1e885와 빈 index는 그대로이며, 새 연결3필드는 로컬에 보존했고 반복 SENTIS 한줄만 복원했다(`ProjectSettings-pr2-opus-sentis-restore.json`). 질문 `msg_91fad2c9d350` 뒤 메인 `msg_39c94249a203`(11:41:44Z)이 전달한 새 사용자 원문은 「2) 아이템·인벤토리 PR - 새 클라우드 연결을 커밋하지 않고 이 PC에만 둘지 (앞 결정 수정) → A 이 PC에만 유지, 커밋 안 함」이다. **3필드는 작업 파일에만 유지하고 커밋에서 제외한다. 훅 수정·우회·예외는 없다.** 앞선 메인의 「다른 작업 공간은 이 PR 병합으로 맞춰진다」 설명은 철회됐다. 머신·작업 공간마다 자기 연결값을 유지하는 정책을 따른다. 원문은 `pr2-fix1-launch-inbox.json`; 기능 수정·검증은 계속한다.

메인 `msg_dc98852233ed`는 결함1~3의 신규 Sol→신규 Opus를 범위 안 수정으로 확인했다. timer3파일은 GameDev `msg_ab490f5d317b`, LocalPlayerInput와 Input helper는 `msg_da0c9a9e5f61`로 쓰기 충돌 없음을 확인했다. 최종 파일/설계와 쓰기 종료·head를 공유한다. 최초 제품 산출물의 수정 회차는 다음 Sol부터1회이며 각 결함의 이번 첫 NOT PASS를 중복 집계하지 않는다.

### PR2 첫 수정 완료와 신규 독립 검증

Sol `task_bddf83472162` / `ctx_f0a2608585b6`는 완료 `msg_a46555948f65`(2026-10-05T12:16:21Z)로 쓰기를 종료했다. 원문은 `sol-pr2-fix1/report.md`(SHA256 `3FE3C29D631D9C3B65578EC177EFD99A1AE100514168F232C4CA89D285869C05`), 실제 diff는 `audit/output-final/owned.diff`다. 요청/화면은 gpt-6.1-sol max·Codex0.160.0, backend는 unknown이다. 변경10파일 +145/-15(C#7·문서1·meta2): 취소 가능한 인벤토리 응답 timer를 지연 송신 FIFO와 분리하고, 실제 포인터의 UI raycast로 공격 시작을 차단하며, TMP 표시 영역 높이를 늘렸다. 기존 송신 순서·키보드/외부 포인터 공격·scene 수명은 신규 독립 판정 대상이다.

자체 실행은 전체 EditMode400/371통과/29실패/skip0(exit2), InventoryScene5/5(exit0)다. 29실패의 a25/b4/c0/d0은 **Sol의 잠정 분류**이며 독립 판정이 아니다. 50ms 입력의 약0.06초 처리·UI클릭 extra attack0은 raw 로그 관측이고 특히 공격 부재 단정/보존 대조군은 새 Opus가 보완해야 한다. 이번 수정에서 실제 서버/MCP/실화면·사람 손 입력·플레이어 빌드·DB·CI는 미실행이다. O1 short-buffer guard 누락은 수정 보고에서 설명했으나 전용 테스트는 미실행이다. 기존 결함별 확정 실패는 각1회이며 자체검사 실패를 중복 집계하지 않는다.

증거 helper 오류도 공개됐다. 초기126개 hash는 기록했지만 ownership 문자열 오타 때문에 소스7개 bytes사본을 누락했다. 12:06:31.347Z에 이번 hunk만 메모리에서 역적용하고 고정 manifest SHA256과 정확히 같은7개 사본만 보충했다(`audit/baseline-copy-correction.json`). 제품은 바꾸지 않았고 사전사본이었다고 소급하지 않는다. 불완전한 이전 audit/output도 보존했다. 신규 Opus가 보고 원문·시각·방법·사본 hash를 실사한다.

Astra는 보고 전문·실제 diff·XML과 보호 hash를 대조했다. `sol-pr2-fix1-astra-settlement-audit.json`은 입력126/meta1142/tests34의 예상밖 drift0을 확인한 정산 감사이며 독립 제품 검증이 아니다. 완료 exact identity/태그 allowed0→release retained/external/none→동일 incarnation idle→close ptyKilledtrue→실제 pane 부재·Unity0/relay0→ACK를 마쳤다. 재사용하지 않는다. 반복 SENTIS 한줄은 사본 보존 뒤 승인 복원했고(`ProjectSettings-pr2-fix1-sentis-restore.json`) cloud3은 로컬에만 유지했다.

**당시 Unity MCP 시트 예약: Content 신규 Opus `task_b0289b7469b8` — 메인 배정 `msg_f1c8f2912c9f`. 현재는 아래 정산으로 반납했다.** 새 계약 `opus-pr2-fix1-contract.md`와 발행 전 manifest/preflight를 고정했다. 메인 `msg_354b8f93e257`에 따라 report.md 하나(기계 판독 표 포함), 사전메모·이름을 열거한 테스트/하네스·raw 폴더만 직접 썼다. 첫 검증의 TEMP 위반을 삭제하거나 면제하지 않는다. 메인 기존 실패 분류 원문을 새 계약에 그대로 붙였다.

먼저 MCP 없는 배치 검증을 수행한다. batch 종료·Unity/lock 해제 뒤 Astra에 공식 ask로 사용자 Editor 열기 가능 시점을 알리고, 자기 프로젝트 `content-active/03_Client`(Unity6000.4.7f1 f3c3c4248748)를 중복 없이 연다. **메인이 사용자 「Editor 열림·연결 승인 준비 완료」를 전달하기 전 첫 MCP 호출은 금지**한다. 거부/철회 시 반복·우회 없이 보고한다. GameDev `msg_d7b3af49558b`는 현재/예정7777·Unity 사용충돌 없음을 회신했으나 실제 실행 직전 Windows/WSL 소유 gate를 다시 확인한다. 검증 정산 때 시트 반납을 기록한다. 현재 예약/Task 생성은 실제 호출 성공을 뜻하지 않는다.

### PR2 첫 수정 독립 판정 — 이전 3건 해소, HUD 겹침 1건 차단

신규 Opus `task_b0289b7469b8` / `ctx_142e1d49d252`는 `msg_579110db7dd3`(2026-10-05T13:35:31Z)로 쓰기를 종료했다. 원문 `opus-pr2-fix1/report.md` SHA256은 `8BF2E595E82A1FBB1BA0B7009F0612EBE700D373217EA621325F3F41FB24E494`다. 최초 명령은 `claude --model claude-opus-5-5 --mcp-config C:/Users/bass1/.unity/claude-mcp.json`, 화면은 Claude Code2.1.289·Opus5.5 xhigh, backend는 unknown이다. 역할은 독립 검증 완료이며 **제품 판정은 차단**이다.

**4 - 기존 HUD와 인벤토리 제목의 겹침 수정**: 802×451 실제 Town·사냥터에서 HUD `character_status`(sortingOrder10)가 패널(5)의 왼쪽 위 5.89×20.56px를 덮고 제목 첫 글자 획에 닿는다. `InventoryPanelView.Resize`의 위쪽150단위 배치가 원인이고 R8의 기존 HUD 겹침 없음 요구에 걸린다. 낮음이지만 차단이다. 최초 구현 `task_4666c26b7ec7` 귀속이며 첫 수정은 이를 만들거나 키우지 않았다. **이 번호의 첫 확정 실패 1회**다. 이전 #1~#3은 해소됐고 중복 집계하지 않는다. 일시적인 퀘스트 팝업이나 O7 접기·토글 후보와 구분한다. 사각형 교차 검사로 전환 가능하다고 반환했지만 자동 RED는 미작성이다. 쓰기 종료 뒤 새 Sol의 패널 배치 수정과 새 Opus의 실제 장면 회귀 검사를 수행한다.

기존 EditMode 실패29건은 a25/b4/c0/d0으로 전수 분류됐다. 원래 요구를 보존해 fixture를 보완한 같은 명령의 결과는400/371/29→400/400/0, 추가 테스트4개 뒤404/404다. 인벤토리 장면은5/5→8/8, 기존 맵 장면10/10, 실제 서버 inventory/mapentry각1/1이다. 새 단정은 짧은 수신 버퍼 거부 뒤 정상 패킷 적용, timeout 취소·예외 격리·실제5초 간격/3회상한, UI 클릭 공격 부재와 월드/키보드 공격 대조군, 최대값/문구의 실제 글리프 표시다. 테스트9파일만 수정했고 기존meta1144와 보호파일은 보존됐다.

실화면에서는 메뉴도 가상 포인터로 클릭했고 직접 메뉴 호출 대역은0이었다. Town→일시정지/재개→포털→사냥터→골렘 처치→재화10/주머니1→사용 클릭→rev2/재화60을 확인했다. 서버 C_Attack은 Enter3+월드클릭1의4회이며 사용·새로고침 뒤 추가 공격이 없다. 사람 손·touch·1920×1080 실창·플레이어 빌드·DB·CI는 미실행이다. 1604×902 캡처는802×451 화면의2배 렌더이며 별도 창 해상도 검증이 아니다.

Astra는185줄 판정 전문·driver로그 전체·화면2장·입력 대조군/수신 guard 단정 표본을 읽었다. `audit-opus-pr2-fix1-settlement.ps1` 실제 결과는 `opus-pr2-fix1-astra-settlement-audit.json`: 입력160/meta1144/tests34, 허용테스트9와 보고된설정1 외 예상밖drift0, XML12개를 대조했다. 새로운 독립 제품 검증으로 합산하지 않는다. 완료 identity/태그allowed0→release→같은incarnation idle/done→close ptyKilledtrue→실제pane부재·Unity0/relay0/lock없음(13:39:31Z)→ACK를 마쳤다. **시트 반납 `msg_b7b613acee68`**. 다음 검증자는 새로 예약하며 이 세션을 재사용하지 않는다.

### Unity 음소거·로컬 상태 정산

메인 `msg_962ec7e0fe5a`가 전달한 사용자 원문은 「유니티 에디트모드나 실제 에디터로 플레이모드로 테스트할때, 사운드는 음소거 해달라고 전달해줘」다. **이후 Content의 모든 Unity 실행에 세션 한정 음소거와 원상 복원 증거를 적용**한다. 추적 오디오 설정·직렬화 값은 바꾸지 않는다. 원문과 적용 단계는 `opus-pr2-fix1-audio-addendum-v1.1.md`에 보존했다.

절차 편차: `after-editmode-all-2`(12:58:25Z 시작)는 명시 음소거 없이 실행됐고 실제 무음 여부는 미측정이다. 요청 전 미적용7회(Edit3/Play4)와 구분한다. 최초 회신의6회 오기와 일반 회신 태그/payload 누락은 교정 재발송 `msg_1574eb623b76`에 남겼다. 정정 지시 발송12:57:04Z와 검증자의 실제 읽기 시점 미확정을 구분한다. 메인 운영 판단 `msg_da7abd0167fe`는 이 편차를 기록하고 이후 명시 적용·복원 증거로 정산하며 제품 차단 사유로 올리지 않도록 했다. 사용자 직접 결정으로 격상하지 않는다.

PlayMode4회는 `MapEntryPlayFixture.Prepare`에서 AudioListener.volume0, Cleanup에서 이전 값을 복원했다. 메인 `msg_0070dfdd4761`이 전달한 사용자 「Editor 열림·연결 승인 준비 완료」 뒤 첫 MCP호출13:10:28.72Z가 성공했고 audioMasterMute False→True를 확인했다. 대화형 종료13:17:45Z에False로 복원, Editor 종료 뒤 `AudioMasterMute_h3604209190` DWord0도 확인했다. 자기 서버는SIGINT20초뒤SIGTERM/launcherexit143, Editor는직접Exit(0)요청뒤프로세스소멸이며 OSexit는미수집이다. 7777·lock해제를 확인했다.

메인 운영 판단 `msg_2de31caf1c5d`에 따라 Astra가 정산 후 동일SENTIS define순서 한줄만 복원했다(E030A834…→A58A3CDF…, cloud3로컬유지). Knight선택으로바뀐 제품 Editor PlayerPrefs `SelectedCharacterClass_h323074203`은 사전hash/현재DWord0을 대조해 원래1로 복원했다(13:42:03Z). Editor/MCP 재기동과 이를 위한 추적파일변경은0이다. 근거 `ProjectSettings-pr2-fix1-review-restore.json`, `opus-pr2-fix1-playerprefs-restored.json`. layouts/cache/sessionprefs는 같은 메인 판단대로 기록만 남겼다.

## 다음 계획 후보

메인 msg_82735bd92cff 이후 새 범위 밖 후보·비차단 지적·새 요청은 이 절에 출처와 함께 기록만 한다. 이전 BACKLOG의 combat-smoke-zero-lag-flaky와 unity-sentis-define-drift는 기존 후보로 유지하며 이번 PR2의 원인 조사·수리 범위로 승격하지 않는다.

- Unity 엔진 업그레이드 검토 — 출처 msg_8b8e828ec7e8, 이번 goal 착수 아님.
- 기존 HUD Gold mock의 서버 재화 연결 또는 숨김 — `opus-pr2-review/report.md` O6, 메인 msg_dc98852233ed; 현재 범위 밖.
- 인벤토리 패널 접힘·토글 등 시야 개선 — 같은 보고 O7와 메인 msg_dc98852233ed; 현재 요구 위반은 아니며 다음 제품 디자인 판단.
- 종료 경로의 중복 null 정리·게임플레이 씬 목록 중복·Ready 관찰자 오류 격리·EditMode 상태 생성 안내 — 같은 보고 O2~O5의 비차단 관찰, 다음 계획 후보만.
- 범용 dispatcher와 인벤토리 deadline 의존 방향·drainer 탐색 주석·실제 바인딩과 낡은 주석 일치·정적 scheduler 테스트 정리 — `opus-pr2-fix1/report.md` O-1/O-2/O-7와 가독성 관찰, 다음 계획 후보만.
- 일반 HUD Graphic 위 마우스 공격 차단의 UX 범위·작은 화면의 한글 폰트 판독성 — 같은 보고 O-3/O-6. 전자는 소스 추론이며 미실측이고 GameDev와 제품 디자인 판단이 필요하다. 이번 HUD 제목 겹침4번과 구분한다.
- 상시 패널 면적·퀘스트 알림의 일시 가림 — 같은 보고 O-4/O-5, 기존 O7 후속 후보에 포함한다.
- `InventoryPanelView.cs:103` `top = 150f`의 이유 주석(HUD가 없을 때의 기본 상단 여백) 복원, HUD 영역 목록으로의 일반화, `"character_status"` 이름 의존의 문서화 — `opus-pr2-fix2/report.md`의 가독성 후속 1과 O-1/O-2. 동작 영향 없음, 비차단.
- batch 640×480 고정이라 16:9 자동 회귀를 batch로 할 수 없는 공백 — 같은 보고 O-3. 아침 실화면 계약에서 다루고, 상시 자동화 수단은 다음 계획 후보로만 둔다.
- 다음 Content goal 「던전 인스턴스 콘텐츠·보상」 범위 초안 — 메인 지시 `msg_16add9c27b4d` 4항에 따라 `msg_b9768c16772e`로 보냈다(사본 근거 폴더 `next-goal-dungeon-scope-draft-sent.md`). 착수는 메인 계획 검토와 사용자 승인 뒤다.
- Unity AI Assistant 패키지 업그레이드로 MCP 연결 상한 제거 범위 초안 — 메인 요청 `msg_b30e645172e0`, 회신 `msg_b7d6727d60ab`(사본 `next-goal-unity-ai-assistant-scope-draft-sent.md`). 의존성 변경이라 사용자 승인 전 구현하지 않는다.

### PR2 두 번째 수정 발행

새 Sol Task `task_5f7f4e01ed5e`가 결함4의 `InventoryPanelView.cs` 배치만 수정한다. 제품/테스트 소유는 분리하고 원시 실제 화면 RED 및 R8 HUD 무겹침을 기준으로 한다. 계약 `sol-pr2-fix2-contract.md`, 입력 `sol-pr2-fix2-inputs.json`, 실제 경로 검사 `sol-pr2-fix2-preflight.json`; dispatch 뒤 이 goal과 보호 입력은 동결한다. 전체 PR2 강 검증은 유지하며 다음 신규 Opus가 자동 HUD 교집합 검사와 실제 표시를 재검증한다.

메인 `msg_833c357e684e`는 사용자 세션 음소거·복원 권한 안에서 기존 Editor mute 저장값 하나의 임시 변경을 허용했다. 다른 Unity0 확인, 실행 직전 원값 원시 기록, 이미1이면 쓰지 않음, 다른 preference/추적 설정/OS 볼륨 불변, finally 원값 복원·정산 재확인을 조건으로 한다. 원문과 적용 방법은 `sol-pr2-fix2-audio-decision-main.json`, 발행 전 실행 부록 `sol-pr2-fix2-audio-addendum-v1.1.md`에 고정했다.

### 결함4 후속 독립 검증 발행 — 2026-10-05

새 리드 `claude-opus-5-5`(handle `term_85786471-7462-436b-835a-ee170221fcf2`)가 메인 `msg_c563e879791f`로 진입했다. 같은 Run `run_add8d9f825f4`를 run-use로 이어받았고 READY는 `msg_7d288f29b1e0`이다. 인계 블록과 실제 상태의 차이는 없었다. 미커밋 47/47개 hash가 같고 작업자 0, Unity Editor·relay 0이다(`lead-opus-entry-uncommitted-compare.json`).

| 항목 | 값 |
|---|---|
| Task | `task_bce6064a15f5`, 신규 `[Content 검증자]` `claude-opus-5-5`. verifies `task_4666c26b7ec7`·`task_bddf83472162`·`task_5f7f4e01ed5e`·문서 `task_b54380de9967` |
| 계약 | `opus-pr2-fix2-contract.md` SHA256 `D32D9F2F8419332063DFF8284AB9D2093A798CEB5CECA2CD4B294C6F4E7DA7DD`. 이전 리드 초안에 새 리드 고지와 야간 진행 절을 더했다. 원 초안은 `opus-pr2-fix2-brief-draft-prev-lead.md`로 보존했다 |
| **Unity MCP 시트** | **`task_bce6064a15f5` 반납(MCP 호출 0) → 아침 새 Opus로 이관, 메인 `msg_5e769cc95c8c`**. 처음 배정은 `msg_94ffc6cbe0f0`이다 |
| 검증 등급 | 강. 전체 PR2의 제품+테스트 50줄 이상, 상태·실패 수명, 패킷 경계가 대상이다 |

MCP 없는 batch를 밤에 먼저 돌리는 것은 리드 판단이다. 메인이 이 판단을 맡겼고, 시트 예약이 batch 선행을 이미 조건으로 둔다. 검증자는 Codex 크레딧을 쓰지 않는다. batch 종료 ask 뒤에는 아침 사용자 「Editor 열림·연결 승인 준비 완료」 전달까지 MCP 첫 호출을 하지 않는다. batch에서 차단 결함이 확정되면 MCP 단계 없이 마감할지 리드가 회신한다.

preflight는 이 세션 PATH에 rg가 없어 `git ls-files --cached --others --exclude-standard`로 meta·test 목록을 만든다. 같은 시점 개수 1144·34가 기존 manifest와 같음을 확인했다. 이 goal은 dispatch 뒤 검증자 쓰기 종료까지 동결하고, 그 사이 기록은 리드 메모 `astra-context.md`에 둔다.

### 결함4 후속 독립 판정 — batch 범위, 실화면은 아침

신규 Opus `task_bce6064a15f5` / `ctx_72a094a4d357`는 `msg_430558ef367e`(2026-10-05T15:17:29Z)로 쓰기를 종료했다. 원문은 `opus-pr2-fix2/report.md`(SHA256 `F2F00EC0DB4A448821C83E083D7FF4FAD7767A24B34AD23D9D8C211A27E4D3EE`)다. 메인 `msg_5e769cc95c8c`에 따라 범위 축소 통지 v2(`opus-pr2-fix2-scope-reduction-v2.md`, `msg_7ed1a695c011`)로 MCP 실화면 단계를 뺐다. **판정은 batch 범위이며 PR2 전체 PASS가 아니다.**

| 항목 | 결과 |
|---|---|
| 결함4 | batch 640×480의 Town·사냥터·사용 결과 6시점(scripted 3, 실서버 3)에서 HUD 존재와 양성 대조를 먼저 단정한 뒤 HUD Graphic·패널 교집합 0, 버튼 가림 0. 802×451·1920×1080은 **MCP 실화면 미실행 — 아침 새 Opus로 이관**, 판정 보류 |
| 회귀 | 새 차단 결함 0, 기존 실패 0. EditMode 404/404, 인벤토리 장면 9/9(신규 HUD 교집합 테스트 1 포함), 맵 장면 10/10, 실서버 인벤토리 1/1 |
| 테스트 변경 | `InventorySceneLifecycleTests.cs` +260/-0(`HudOverlapProbe`, 새 테스트 1), `InventoryServerIntegrationTests.cs` +3/-0. 기존 단정 삭제·완화 0 |
| batch 화면 한계 | batch는 `-screen-width/-height`를 줘도 640×480 고정이다(`runs/screen-arg-probe-1`). 16:9 확인은 실화면 단계가 필요하다 |
| 미실행 | MCP 실화면, 리사이즈, 사람 손 입력, touch, Player build, DB, CI |

리드 정산: 판정 본문 전문과 부록의 계약 원문 동일성(SHA256 `42D7EE19…`)을 확인하고, 판정 로직 표본(`InventorySceneLifecycleTests.cs:166-241`)을 직접 읽었다. 정산 감사 `opus-pr2-fix2-astra-settlement-audit.json`은 입력 182, meta 1144, tests 34, 허용 변경 3, 예상 밖 0, XML 6개 수치·hash 일치다. 독립 제품 검증으로 세지 않는다. ProjectSettings의 SENTIS 자동 제거 한 줄만 기존 승인(`msg_7691eaa0154c`, `msg_2de31caf1c5d`)대로 복원했다(EEE969AE → A58A3CDF, cloud3 유지). release → 같은 incarnation 확인 → close ptyKilled true → relay PID 34856·Unity·lock·7777 모두 0, 음소거 0(15:19:27Z). 검증자 보고의 관찰로, 첫 batch 중 자기 relay가 batch Unity에 붙어 unity-mcp 도구 목록이 노출됐다(원인 미확인, MCP 호출 0).

다음 단위: PR2 커밋(사용자 MinimapRT·로컬 cloud3 ProjectSettings 제외, staged 목록 원시 보존) → 최신 main 통합 → push·PR·CI → 아침 새 Opus의 PR head 실화면 확인(시트 이관 `msg_5e769cc95c8c`) → 승인 묶음.

PR2 체크포인트: 커밋 `3085a20a`(제품)·`bf28954d`(테스트)·`66b02372`(기능 문서·BACKLOG)·`6852237a`(goal), main 55커밋 merge `a3136b6c`(겹친 파일은 BACKLOG.md 하나, 충돌 없음). 사용자 MinimapRT와 로컬 cloud3 ProjectSettings는 커밋에서 뺐고 커밋마다 staged 목록을 근거 폴더 `pr2-commit-1..4-staged.txt`, `pr2-merge-main-staged.txt`로 남겼다. [PR191 - 클라이언트 인벤토리 화면과 서버 동기화](https://github.com/bass131/dawnholder-server/pull/191). 이 PR의 CI는 Unity 테스트를 돌리지 않고, 로컬 code-rules도 `03_Client/` 제외로 대상 0건이었다. CI 결과를 클라이언트 검증으로 세지 않는다. 병합 승인 요청은 아침 실화면 확인 뒤에 한다.
