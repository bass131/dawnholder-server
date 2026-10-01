# P2 / D1a — 캐릭터 영속성 기술 계약

상태: **goal 초안 커밋 후 메인 확인 대기**. D0·P0·현재 서버의 읽기 전용 조사를 마쳤다. 이 goal은 기술 명세 작업의 범위이며 GameServer 연동·신규 migration·SQL 시험이 완료됐다는 뜻이 아니다.

## 착수 근거와 기준

- 메인 지시 `msg_40867e846fd7`: DB 설계가 큰 목표이고 R-2 검사와 Management 규칙 적용은 병행한다. C 사전 조사 결과와 초안·질문은 `msg_29b12d4b2d87`로 전달했다.
- 메인 `msg_deb8cb0b630f`: R-2 판정·PR 후 최신 main의 별도 branch에서 D1a goal을 작성하고 커밋 뒤 메인 확인을 받는다. R-2는 독립 실사 PASS·제품 변경 없이 [PR155](https://github.com/bass131/dawnholder-server/pull/155)로 분리됐으며 사용자 병합 승인은 별도다.
- branch `bass131/persistence-technical-design`, base `cb6f717de0fc0eea6d1295d3c8c47da7454125a6`(branch 생성 직전 `git fetch origin main` 결과). GameDev checkout의 R-2 쓰기를 종료한 뒤 전환했고 다른 checkout을 만들지 않았다.
- 계약 출처: [D0 결정](../2026-09-29-persistence-design/goal.md), [D0 설계](../2026-09-29-persistence-design/design.md), [D1–D4 분할](../2026-09-29-persistence-design/implementation-plan.md), [P0 DB·소유 계약](../2026-09-30-contracts-baseline/contracts.md), [P2 로드맵](../../milestones/2026-09-30-contracts-persistence/roadmap.md). D0 제품 결정을 다시 열지 않는다.

## 확정 결정과 사용자 판단 경계

개발 고정 계정 1개·캐릭터 1개, 최초 생성 클래스 유지, 재접속 Town 안전 spawn·기본 풀HP, quest/보스 해금 세션 한정을 유지한다. 영속 대상은 identity/class와 안전 checkpoint다. 주기 저장·매 logout 동일 게임 데이터 UPDATE·전투 위치/rawHP/quest 저장·정식 인증·경제 데이터는 추가하지 않는다.

- 사용자 결정은 메인 `msg_04bde9ea9023`으로 전달됐다: **신뢰 복구 입구는 로컬 Windows 관리자가 명시 실행하는 제한된 관리 도구**로 설계한다. 게임 앱 principal에는 recovery 권한을 주지 않는다. Management 복구 command와 운영툴 권한/경계는 후속 브레인스토밍 후보이며 D1a 범위 밖이다.
- SQL 시험 대상은 **D1b 실행 직전에 결정**한다. 새 `Dawnholder_Dev_<suffix>` 격리 DB가 추천안이나 아직 선택·실행 승인으로 보지 않는다.
- 사용자 확인을 받은 메인 `msg_deb8cb0b630f`에 따라 `Northwind`·`BaseballGame`은 연습용이며 참조·재사용·변경하지 않는다. 게임용 사전 schema는 `Dawnholder_Dev`의 `dh` 테이블이다. **001 migration을 고정하고 authority/operation 변경은 002 이후 신규 migration으로 설계**한다.
- 사용자는 게임용 사전 테이블을 아직 직접 보지 않았으므로 아래 역할·제약을 보고에 포함한다. 보고 후 변경을 원하면 D0 재결정 여부를 메인에 올린다. 이 설명을 기존 데이터의 실제 현황 재검증으로 보지 않는다.

## 사용자가 볼 사전 테이블 요약

근거는 [001 SQL](../../../99_Tools/database/migrations/001_initial.sql)과 [MSSQL 안내](../../../00_Document/operations/MSSQL.md)다. 001은 게임 테이블 3개를 만들고 `SchemaVersion`은 설치 도구가 관리하는 이력 테이블이다. 현재 DB에 접속해 행이나 설치 상태를 조회하지 않았다.

| 테이블 | 맡는 일 | 핵심 제약·이번 설계의 의미 |
|---|---|---|
| `dh.Account` | 게임 계정의 고정 식별자 | GUID 기본키·생성 UTC. 로그인 인증이나 다른 연습 DB 계정을 뜻하지 않는다 |
| `dh.Character` | 캐릭터 소유 계정과 최초 클래스 | GUID 기본키·Account 외래키·Class 0/1·rowversion. 기존 schema는 계정당 여러 행을 허용하므로 MVP의 고정 논리 키와 최초 생성 직렬화를 별도 계약으로 정한다 |
| `dh.CharacterProgress` | 캐릭터당 최대 한 checkpoint | Character 기본키/외래키·맵 0~3·좌표·HP/MaxHP·해금·rowversion. `MaxHp > 0`, `0 <= Hp <= MaxHp`; 이번에는 Town 안전값만 다루고 전투 상태/해금을 복원하지 않는다 |
| `dh.SchemaVersion` | 적용한 migration의 이력과 무결성 | 번호·파일명·정규화 SQL checksum·적용 시각. 적용된 001을 고치지 않고 새 migration을 추가한다 |

## 허용 범위와 산출물

1. .NET SQL driver와 고정 version, Windows/WSL 설정·비밀 주입·연결 수명·권한 경계를 공식 근거와 함께 정한다. 기존 ODBC probe 성공을 .NET repository 성공으로 쓰지 않는다.
2. authority/operation의 필드·키·제약·보존과 신규 migration 명세를 작성한다. 미존재 고정 키의 최초 생성부터 acquire+일관 load, mutation, release, 관리 recovery까지 같은 DB 직렬화 경계를 구체화한다.
3. fence·읽기 의존 CharacterVersion·ProgressVersion·operation ID/payload·증빙의 원자성, immutable token 복사, unknown 조정·conflict·timeout·cancellation·재시도 금지를 구체화한다. 게임 데이터 UPDATE가 없는 `NoWriteNeeded`와 권위 release transaction을 구분한다.
4. D1b 저장소 API/결과와 D2 bounded worker·owner/종료 인계, D3 Loading/권위 class 입장의 소비 경계를 명세한다. D2 전체 deadline 수치 튜닝이나 D3 packet ID·Unity 구현을 앞당기지 않는다.
5. 격리 SQL 두 연결 시험의 old/new 순서·장애 주입 지점·관측 증거·권한·정리 절차를 설계한다. 정확한 실행 DB·시험 principal/GUID·cleanup 권한은 D1b 실행 gate로 남긴다.

산출물은 이 `goal.md`(기준·상태·결정·결과), 같은 폴더의 `technical-spec.md`(DDL/API/transaction/설정·권한 계약), `verification-plan.md`(반증 행렬·실행 gate·정리)다. 메인 확인 전에는 후자의 두 문서를 구현 명세로 확정하지 않는다. CURRENT와 로드맵은 goal 링크만 유지한다.

## 현재 코드와 후속 연결점

- `GameSession.HasSelectedClass`는 `_stats != null`이며 CharacterSelect 뒤 즉시 기존 입장 큐로 이어진다. 현재 저장 GUID·DB repository·권위 acquire가 없다. D3에서 Loading과 Active gate를 연결해야 한다.
- `GameWorld.DrainSessionCloses`는 틱 앞뒤에 entity·party를 정리한다. `MapMigration.Execute`는 source 제거 뒤 destination 등록 전 0-owner 구간을 가진다. D2 safe DTO/진행 작업 인계는 살아 있는 entity를 뒤늦게 재조회하는 방식에 의존하면 안 된다.
- `ServerHost.StopCore`는 listener·disconnect·world flush·world stop에 단계별 timeout을 사용한다. D0의 단일 절대 deadline과 DB 결과/조건부 release 종결은 D2 구현 범위다.
- `PlayerEntity`의 snapshot/transfer와 DB checkpoint는 별개다. 기존 전투 rawHP와 위치 계약·EnterMap→HP→roster 순서·tick 비차단·session generation을 보존한다.

## 완료조건·소유·검증

- D0 불변조건마다 구체 필드/transaction/결과/반증 시나리오를 대응시킨다. driver/version·잠금·권한·증빙·저장소 반환 계약을 구현자가 추측하지 않게 정하고 실제 SQL로만 확인할 가정을 표시한다.
- old-first/new-first·원자 최초 생성·create/commit/release unknown·같은 ID의 다른 payload·stale fence/두 token·alias 변경·NoWriteNeeded·무권한 recovery와 정상 recovery를 시험 계획에 포함한다.
- GameDev Astra가 조사·설계 문서·결과 정리·Git을 소유한다. Sol은 발행하지 않는다. 신규 외부 Opus 한 명이 문서와 출처·보존 계약·반증 계획을 독립 실사하며 저장소 쓰기는 하지 않고 `.backups/verification/2026-10-01-persistence-technical-design/`에 원문을 남긴다.
- 지정 검증 모델은 `claude-opus-5-5`, 요청/기동/화면/backend를 구분하고 확인 불가 backend는 `unknown`이다. 담당 Astra 아래 신규 pane→최초 attach, 작업 하나 뒤 정산·종료를 따른다. 기동 불가 시 메인에게 요청한다.
- main 확인→기술 명세 작성→쓰기 종료→독립 실사→필요한 수정과 새 세션 재검증→결과/PR 순서다. 제품 결함·범위/운영 정책 분기는 메인에 올린다. PASS 후 push·PR은 허용하되 병합은 해당 PR에 대한 사용자 명시 승인 대상이다.

제품/프로토콜/테스트/SQL script·001·계정/서비스·전역 설정·비밀 파일은 수정하지 않는다. DB 접속·비밀 읽기·DDL/데이터 쓰기·restore/장애 시험·빌드/Unity/서버 실행도 이 설계 목표에서 수행하지 않는다. 문서 실사와 실제 저장/복구 성공을 구분한다. `CLAUDE.md`·Management 문서는 각 소유자에게 남긴다.

branch 전환 후 manifest·packages-lock·ProjectSettings의 skip-worktree `S`와 SHA256은 R-2 검증 끝 상태와 동일했다. stash 2개·archive를 보존하고 제품 변경은 없다. 설계 문서와 독립 실사 결과는 아직 작성·실행 전이다.
