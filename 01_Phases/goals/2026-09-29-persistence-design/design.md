# 작은 범위의 캐릭터 영속화 설계

상태·결정 출처·승인·결과는 [goal](goal.md), 후속 순서는 [implementation-plan](implementation-plan.md)이 소유한다. 아래는 선택된 MVP의 계약이며 SQL/게임 실행으로 검증한 구현이 아니다.

## 기존 schema와 선택 범위

[MSSQL handoff](../2026-09-29-mssql-setup/handoff.md)는 당시 설치·Windows/WSL 접속·rollback CRUD/제약/token 검증을 기록한다. 오늘 DB 상태 재검증은 없으며 .NET repository/저장/로드/인증은 아직 없다. ODBC 성공을 GameServer 연동 성공으로 해석하지 않는다.

| 기존 001 | 이번 의미 | 변경/추가 경계 |
|---|---|---|
| Account GUID/생성 UTC | 서버 설정의 개발용 고정 주체 | 정식 사용자 인증 아님. 다른 프로젝트 accounts 재사용 없음 |
| Character GUID/Account FK/Class0·1/Version | 고정 캐릭터1, 최초 commit의 클래스 유지 | 메뉴 class는 생성 의도. 기존 row 소유 검사 필수, runtime EntityId와 분리 |
| Progress 0..1행, Map/X/Y/HP/MaxHP/해금/Version | **Town + 서버 content 안전 spawn + 저장 클래스 기본 풀HP/MaxHP + 해금false** | 전투 위치/rawHP/quest를 저장·복원하지 않음. load에서는 현재 content/클래스 정의로 안전 projection |
| HP CHECK·두 rowversion | 제약과 낙관적 경합 검출 | token은 DB 권위/fence나 작업 순서를 대신하지 않음 |
| authority/operation 증빙 없음 | acquire/load/create/release의 수명에 필요 | 새로운 migration 후보. 구체 DDL/driver/잠금은 D1a에서 고정·검증 |

[001_initial.sql](../../../99_Tools/database/migrations/001_initial.sql)은 고정한다. 기존 데이터 일괄 덮어쓰기·삭제·앱 DDL/DELETE 권한 확대는 없다. 과거 Progress의 전투값은 복원하지 않고 진단만 남기는 기본안이며 실제 데이터 전환 필요는 D1에서 별도 검토한다. 잘못된 class/소유·필수 identity는 입장 오류다. Progress 없는 기존 Character는 저장 class를 유지한 safe 초기화로 수렴하도록 검증한다.

M2b의 불변 PlayerStats·snapshot.CurrentHp·transfer rawHP/default는 메모리 계약이다. DB projection과 혼동하지 않는다. 영속 대상은 identity/class와 안전 checkpoint뿐이며 party/초대/킬 수/해금·적·입력/FSM/cooldown/위치 이력은 복원하지 않는다.

## 필요한 작업과 DB 권위

필요한 작업은 최초 idempotent Account/Character/초기Progress 생성, 권위 acquire+load, 미확정 결과 조정, 조건부 release다. 주기 저장·매 logout 같은 row UPDATE·클래스 변경은 없다. 변경할 durable 필드와 진행 중 작업이 없으면 `NoWriteNeeded → release`이며 새 저장 성공으로 세지 않는다. actor는 I/O를 기다리지 않고 한 캐릭터용 bounded 작업 lane에 immutable 값만 인계한다.

### transaction 불변조건

1. **AcquireAndLoad와 모든 mutation/release는 같은 캐릭터의 DB 직렬화 경계**를 쓴다. admission/소유 검사→fence 갱신→Character+Progress 일관 load를 한 transaction에서 수행하고 commit 후에만 성공한다. fence는 이전 수명과 다르고 재사용하지 않는다.
2. 각 mutation은 current fence·서버가 확인한 AccountId/CharacterId·읽기 의존 CharacterVersion·ExpectedProgressVersion·operation 순서를 검사하고 검사부터 commit까지 경계를 유지한다. release도 fence 조건부다. 검사 후 별도 transaction UPDATE는 불가하다.
3. old 작업이 먼저 경계를 잡았으면 commit/rollback 확정 후 new acquire가 그 결과를 읽는다. new acquire가 먼저 commit됐으면 old mutation/release는 거부된다. 프로세스 종료·메모리 lease·고정 대기·cancel 요청은 DB 작업 완료 증거가 아니다.
4. Character가 아직 없어도 고정 논리 키의 생성/acquire가 직렬화돼야 한다. Account/Character/필요한 초기Progress와 성공 증빙은 원자적이다. 중복 생성 의도가 새 GUID로 두 캐릭터를 만들지 않는다.
5. Progress-only projection도 읽은 Class/Account에 의존하면 CharacterVersion을 검사한다. 불필요 Character UPDATE는 하지 않되 검사와 commit 사이 일관성을 유지한다. 두 행 변경은 양쪽 token과 전체 commit 후 token만 ack한다. token byte[]는 복사해 mutable alias를 남기지 않는다. 관리 writer도 같은 규약을 따라야 한다.

동일 operation ID/동일 payload 재요청은 확정 결과 조회, 같은 ID/다른 payload는 거부한다. 데이터/fence와 operation 증빙의 원자 기록을 권장한다. commit 뒤 응답 유실은 `CommitUnknown`으로 lane을 멈추고 증빙을 조회한다. 같은 값/SavedUtc/rowversion 증가만으로 요청 성공을 추정하지 않는다. conflict에 token만 바꿔 stale DTO를 재시도하지 않는다. 재시도 가능한 미해결 operation의 증빙을 자동 삭제하지 않는다.

### 정상 접속·중복·재시작

활성 owner가 있으면 신규 동일캐릭터 접속을 거부하고 기존 Ready 세션은 유지한다. 정상 close의 release commit 후에는 다음 접속/정상 서버 재시작이 자동 acquire/load할 수 있다. crash·commit/release 미확정·옛 owner 잔존은 `RecoveryRequired`로 닫는다. 자동 takeover/TTL 회수·무손실 crash 복구는 도입하지 않는다.

신뢰된 관리 복구가 허용한 요청만 같은 DB transaction에서 fence 교체+load를 한다. 클라이언트 GUID/timeout/프로세스 부재만으로 복구 권한을 주지 않는다. old process 부재 확인은 허용 판단의 일부일 뿐 old SQL 종료 증거가 아니며 보호는 DB fencing이 제공해야 한다. 복구 principal/도구/권한·operation 조회 방식은 D1a에서 고정한다.

| 결과 | 의미·금지 |
|---|---|
| Accepted / Durable | worker 책임 인수 / commit 증빙. 서로 다른 상태 |
| Acquiring / Acquired | 진행 중 / commit 확인된 fence+일관 load+복사 token |
| Conflict / Rejected | token/fence/소유·상태 오류 또는 접수 거부. blind retry 금지 |
| AcquireUnknown / CommitUnknown | 결과 미확정. Ready·후속 mutation·자유 release 성공으로 진행 금지 |
| NoWriteNeeded / Released | 쓸 게임 데이터 없음 / 조건부 권위 해제 commit 확인 |
| Unresolved / RecoveryRequired | 예산 내 확정 실패 또는 신뢰 복구 필요. 성공으로 표시하지 않음 |

## 서버 입장과 입력 gate

`HandshakePending → Selecting → Loading → Resolved → Entering → Active`를 분리하고 언제든 Closing으로 갈 수 있다. gameplay 입력(move/attack/skill/portal/party/quest 변경/개발 치트)은 **Active+현재 owner+session generation**에서만 허용한다. `_stats != null`, client Ready, 우연한 owner-null drop은 load 완료 gate가 아니다. handshake/selection·disconnect·필요한 ping은 별도 허용 목록이다.

connection당 선택/load는 한 번이다. Loading 중 동일/다른 선택은 새 create나 두 번째 입장을 만들지 않는다. 실패 후 재시도는 새 연결이다. 최초 유효 class0/1은 생성 의도이며, 기존 class가 다르면 저장 class를 안내한 뒤 그 값으로 입장한다. DB load 실패는 새 입장 거부이고 기존 Ready 세션은 유지한다.

늦은 load 결과는 actor에서 generation/closing/authority를 재확인한다. 닫힌 세션에는 적용하지 않아도 persistence owner가 생성/acquire/operation·release를 종결한다. **create 결과 폐기≠rollback**이다. 이미 생성된 행을 임의 삭제하거나 재시도에 새 Character를 만들지 않는다. UI에는 간단한 로딩·연결 실패/재시도 안내·복구 필요 안내만 두고 SQL/fence/epoch·비밀을 노출하지 않는다.

## 권위 class 확인 후 Unity 생성

현행 S_EnterMap에는 class가 없고 self S_PlayerJoin은 버린다. EnterMapHandler는 Town(0) entry다. Town 선로드 뒤 spawner가 메뉴 class로 생성하고 Movement.Awake에서 predictor를 고정하므로 cache만 늦게 바꾸는 안은 부족하다.

새 결과1종(가칭 `S_CharacterLoadResult`)에 공개 status와 성공 시 authoritative class를 두는 기본안이다. GUID/fence/token/SQL 오류는 보내지 않는다. 정확 ID/status/version은 D3에서 기존 ID를 재사용하지 않고 확정한다. 성공 순서는 **LoadResult(success,class) → EnterMap → full PlayerHp → 기존 roster**다. 결과 성공은 DB 준비 완료이며 gameplayReady가 아니다. FIFO와 기존 scene/player/terrain/registry/spawn/첫HP barrier를 유지한다. Town 복귀에는 새 mapId가 필요 없다.

- 메뉴 생성 의도와 session 소유 nullable 권위 class를 분리한다. class 전 local spawn을 보류하고 sceneLoaded/class-ready 양쪽에서 멱등 spawn 조정을 한다.
- 최초 Awake/predictor·visual·평타 전략/전진 예측·Q/E·SkillHUD·class별 SFX 선택이 같은 class를 쓴다. SkillHUD는 late bind/rebind하며 Knight에서 숨긴 E 슬롯의 복원도 검증한다. HP HUD late bind와 Ready 독립성은 유지한다.
- 같은 Town에 남은 옛 player를 새 Ready player로 채택하지 않는다. 이전 객체/구독 정리와 Editor direct-entry를 포함한다. 중복 success는1회 spawn이다. 현재 generation의 상충/invalid class·성공 없는 EnterMap은 해당 현재 입장을 실패 처리·정리하며 Knight fallback으로 숨기지 않는다. old generation 결과는 현재 연결을 닫지 않고 조용히 폐기하며 옛 자원만 정리한다. 새 class/entry/mirror에는 영향을 주지 않는다.
- 실패 packet 직후 Disconnect가 전달을 보장하지 않으므로 송신 완료/제한 대기 계약을 정한다. 응답 없이 EOF/timeout이어도 UI가 끝나고 새 연결로 재시도할 수 있어야 한다. 무한 Loading은 없다.

## EOF·Host·migration의 safe DTO 인계

공통 순서는 **quiesce → immutable safe DTO/진행 요청 인계 → entity/party cleanup → persistence owner의 결과·조건부 release 종결**이다. close가 새 gameplay mutation을 막고 기존 queued closing/owner 규칙은 유지한다. DTO는 고정 identity/저장class/Town projection/fence/operation만 담으며 위치/rawHP/Quest capture가 필요 없다.

persistence owner가 ticket 책임 또는 명시적 FinalRejected를 인수한 뒤 owner cleanup을 한다. 한 캐릭터의 bounded final 인계 공간을 두고 기존 create/acquire 결과를 인계한다. 진행 작업·변경 필드가 없으면 NoWriteNeeded다. full/timeout을 accepted/durable로 숨기거나 cleanup을 무한 대기시키지 않는다. session/world 제거 후에도 DTO/ledger/결과 책임은 persistence owner에 남고 actor notification과 terminal 결과 기록을 분리한다.

migration source 제거 뒤 0 owner 또는 dest close skip에도 context의 safe DTO로 유한 인계한다. 도착 owner를 기다리거나 새 동적 checkpoint를 만들지 않는다. 장래 위치 복원은 source capture/도착 commit/fallback, quest 영속화는 별도 phase가 필요하지만 이번 작업/필수 시험에서 제외한다.

Host는 하나의 monotonic **절대 deadline**을 두고 accept 차단→handoff→cleanup barrier→pending 조정→조건부 release→자원 정리에 남은 예산만 전달한다. world는 safe 인계 후 정지할 수 있고 이후 DB 종결은 persistence owner가 맡는다. 값은 D2 설정/측정 대상이며 단계마다 전체 timeout을 재시작하지 않는다. 초과는 ShutdownIncomplete/미확정 목록이다. cancellation은 rollback 증거가 아니고 진행 I/O 아래 자원을 먼저 dispose하거나 추적을 잃지 않는다. 기존 Host 소유를 우회하는 finally World.Stop도 추가하지 않는다.

## 구현 전 기술 명세

D1a에서 driver/version·새 authority/operation schema·동일 DB 잠금/transaction·복구 권한/증빙 조회·timeout을 고정하고 격리 DB의 old/new interleaving 반증을 준비한다. D3에서 정확 packet/등록/생성물/Loading UI 접점을, D2에서 종료 예산을 확정한다. 이는 사용자 제품 범위 미합의가 아니라 구현 전 기술 작업이다. D0 리뷰는 계약/범위/소유/검증계획의 일관성을 판정하며 실제 SQL/게임 성공과 구분한다.
