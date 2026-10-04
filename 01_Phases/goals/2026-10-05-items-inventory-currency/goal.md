# 아이템·인벤토리·재화

상태: **선행 TDD와 PR1 서버 구현·자체 점검 완료. 신규 Opus 독립 검증 준비. PR/병합 미실행.**

- 담당: Content Astra. 시작 기준 `origin/main` = `955002a932925ff2c4ac81f4a5a99f2024a4b9b2`.
- 작업 공간: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/content-active`, branch `feat/items-inventory-currency-20261005`.
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
2. **클라이언트 인벤토리 UI**: 서버 상태 미러, 수신 핸들러, 최소 인벤토리/재화 표시와 사용 입력, 세션 정리·씬 수명 연결. 독립 Unity 테스트와 실제 플레이로 전체 완료조건을 확인한다. PR1 사용자 병합 승인 뒤 최신 main에서 이 goal의 후속 브랜치를 만든다.

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
| Unity | 컴파일·EditMode 테스트와 실제 인벤토리 표시/아이템 사용 플레이. 새 `.meta`와 기존 GUID/직렬화/문자열 경로 보존. scene/prefab 작업·MCP는 메인 소유. .NET 통과로 Unity 완료를 대신하지 않는다. |
| 기존 동작 | 전투/사망·respawn·quest/party·맵 이동·종료의 영향 회귀. 기존 테스트 실패는 전수 분류하고 수정한 기대값은 요구사항 원천과 전후 같은 명령 결과를 남긴다. |

- 검증자의 harness는 파일로 둔다. 복합 Assert는 판단별 변수로 나누고 판정에는 `verifies=대상 task`, 결함 번호·심각도·차단 여부·귀속, `설계 관찰(비차단)`을 넣는다. 설계 결정으로 막히면 한 줄 대안을 함께 반환한다.
- SDK `10.0.301`; Windows 빌드는 Unity Plugins DLL을 복사하므로 전후 Git diff를 확인한다. SAC Off 결정은 환경 확인의 입력이며 현재 네이티브 실행 성공을 의미하지 않는다. WSL은 source별 전용 복사본·잠금을 사용한다.
- 서버 7777·Unity 프로젝트 실행은 먼저 GameDev/메인과 소유를 맞춘다. 다른 소유 프로세스를 종료하지 않는다. DB 검증은 현 goal에서 수행하지 않는다.
- 보고 수치는 원시 결과 파일에서 읽고 명령·환경·exit·성공/실패/skip·미실행을 분리한다.

## 정본 반영 전 적용 중인 사용자 결정

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

1. 메인 범위 확인(`msg_763b1fa21bf5`)과 GameDev ID/공유 파일/저장 경계 합의(`msg_58eaccfd4a15`)를 완료했다.
2. Fable 고정 입력·검토·정산·종료와 메인 원문 확인을 완료했다. INV-06/S-3를 현재 도달 경로에 맞춰 정리했다.
3. 최종 요구사항/패킷 계약은 `.backups/verification/2026-10-05-items-inventory-currency/pr1-acceptance.md`, 선행 TDD 계약은 같은 폴더 `tdd-contract.md`(SHA256 `8E2968D6E7EC1C5008D304F4535A1695BFB8429274DFF013ABAC180F5168B23E`)다. 복구 시점/기준 SHA 보충은 recovery/recovery2 파일에 보존한다. 선행 TDD와 `sol-pr1-contract.md`의 구현은 아래와 같이 완료했다. 다음은 신규 Opus의 보고·diff 실사, 독립 테스트, 실제 서버↔봇 및 Unity 소비자 검증이다.
4. 현재 Content Run은 `run_add8d9f825f4`, coordinator `term_857393a1-53a9-4546-b3f2-d2f48918696f`다. GameDev의 새 Run `run_6ba3f644755b`에 공유 파일 쓰기 종료 head/diff와 실제 봇 실행 시작·종료를 알린다. PR1 병합 전 사용자 승인을 기다리며 PR2를 먼저 착수하지 않는다.

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

TDD 가정 D1~D6의 구현 전 정리: 3틱은 테스트 허용폭이고 제품 지연 SLA가 아니다. 결과 itemId는 요청 값을 되돌리고, 새 불량 경제 입력은 무응답 drop하며 연결을 유지한다. 성공 보상마다 snapshot push1건, 보상 거부에는 별도 통지를 요구하지 않고 조회로 현재 상태를 확인한다. 이는 현재 핸들러/전송 관례에 맞춘 작은 구현 선택이며 기존 임시 데이터/범위를 바꾸지 않는다. 원 acceptance와 TDD 원문은 보존하고 구현 계약에 명시한다.

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
