---
title: 데이터베이스 기초
source: 게임 서버 프로그래밍 교과서
category: 7장 데이터베이스 기초
---
# [GSP-07] 데이터베이스 기초 (Database Fundamentals)
> 게임 서버에서 플레이어 데이터를 영속화하기 위한 관계형 DB 연동 기초 — M5 영속화 마일스톤의 직접 근거

## 언제 참조하나 (트리거)
- M5 영속화 작업 시작 전 설계 기준으로
- `PlayerEntity` / `UserAccount` / `Character` / `Item` DB 테이블 스키마를 정의할 때
- EF Core 도입 전 원시 SQL 패턴이나 연결 문자열 구조를 확인할 때
- 트랜잭션(아이템 거래, 인벤토리 이전) 구현 시 All-or-Nothing 보장 설계를 검토할 때

---

## 핵심 내용

### 7.1 플레이어 정보 저장 — 로컬 vs 서버
- 싱글플레이어: 세이브 파일을 로컬 PC에 저장(텍스트/바이너리/암호화). 클라이언트 신뢰 가능.
- 온라인 MMORPG: 클라이언트는 신뢰할 수 없음(헌법 #3 Trust Boundary). 플레이어 데이터를 서버 측 DB에 중앙 집중 보관해야 함.
- 핵심 전환: "클라이언트는 렌더러, 서버가 유일한 진실" 원칙이 영속화에도 동일 적용됨.

### 7.2 어떤 DB를 쓸 것인가
- 저자 권장: Microsoft SQL Server(Express 무료) 또는 Oracle MySQL.
- 관리 도구: SQL Server Management Studio(SSMS). Visual Studio 내장 DB 도구보다 기능이 풍부함.
- 이 책의 예제는 SQL Server 기반. 우리 프로젝트는 동일 계열(LocalDB/SQL Server) + EF Core 채택(ADR 기준).

### 7.3 데이터 구성 단위
계층 구조: **DB 인스턴스 → 테이블(표) → 레코드(행) → 필드(열)**

| 단위 | 설명 |
|---|---|
| 인스턴스 | 하나의 DB 서비스 공간. 여러 테이블 포함. |
| 테이블 | 단일 주제의 행/열 집합 (플레이어 계정, 캐릭터, 아이템 등). |
| 레코드 | 테이블의 한 행. 한 플레이어 또는 아이템 하나. |
| 필드 | 열. 이름 + 데이터 타입(정수/실수/문자열/날짜/이진 등) 지정. |

### 7.4 DB 시작 — SSMS 실습
- SSMS 실행 → DB 인스턴스 우클릭 → 새 DB 생성 → 테이블 우클릭 → 필드 정의 후 저장.
- 프로덕션 기준으로는 마이그레이션 스크립트(EF Core Migrations) 사용 권장; SSMS GUI는 로컬 탐색용.

### 7.5 SQL 기본 CRUD
필수 4개 연산:

```sql
-- 추가 (INSERT)
INSERT INTO UserAccount (ID, PasswordHash, Name) VALUES ('hongild', 'abc123hash', '홍길동')

-- 조회 (SELECT)
SELECT ID, Name FROM UserAccount WHERE ID = 'hongild'
SELECT * FROM Character WHERE OwnerUserAccountID = 'hongild'

-- 수정 (UPDATE)
UPDATE Character SET Level = 10 WHERE ID = 'LittleElf'

-- 삭제 (DELETE)
DELETE FROM Item WHERE OwnerCharacterID = 'LittleElf'
```

책은 게임 서버 영속화에 필요한 최소 연산 범위로 SQL을 의도적으로 한정함.

### 7.6 인덱스와 키
- **인덱스**: 책의 색인처럼, 전체 테이블을 순차 스캔(Full Scan)하지 않고 특정 값을 빠르게 찾는 자료구조.
- 인덱스 없이 `WHERE ID='hongild'`를 실행하면 레코드 수에 비례하는 O(n) 스캔 발생 → 플레이어 수 증가 시 성능 급락.
- **Primary Key(기본 키)**: 테이블 내 레코드를 유일하게 식별. 자동으로 인덱스 생성. `UserAccount.ID` 가 예시.
- **Foreign Key(외래 키)**: 다른 테이블의 PK를 참조. `Character.OwnerUserAccountID → UserAccount.ID`. 참조 무결성 강제.
- **Unique Key**: PK 외 추가적인 유일성 제약(예: 이메일). (일반 지식 보완)
- 게임 서버에서 자주 사용하는 인덱스 대상: 계정 ID, 캐릭터 ID, 아이템 소유자 ID — 로그인·로딩 쿼리의 WHERE 조건.

### 7.7 플레이어 데이터 계층 구조 (방법 1 — 개념 정의)
플레이어 데이터는 **계층 트리**: 계정(1) → 캐릭터(N) → 아이템(N).

```
UserAccount
  └── Character (OwnerUserAccountID FK)
        └── Item (OwnerCharacterID FK)
```

각 노드가 복수의 필드를 가지므로, 노드 하나 = DB 레코드 하나가 기본 매핑.

### 7.8 플레이어 데이터 저장 방법 2 — JSON/XML vs 관계형 분산
- **방법 1(단순 직렬화)**: 플레이어 전체를 JSON/XML으로 직렬화해 BLOB 컬럼 하나에 저장.
  - 장점: 구현 단순, 스키마 변경이 쉬움.
  - 단점: "레벨 50 이상 캐릭터 전체 조회" 같은 조건 검색에 Full Scan 불가피. 리포팅·운영 쿼리 불편.
- **방법 2(관계형 분산)**: 계층의 각 노드를 별도 테이블의 레코드로 저장.
  - 장점: SQL WHERE로 임의 조건 검색 가능. JOIN으로 관계 탐색. 인덱스 효과 최대.
  - 단점: 로드 시 JOIN 쿼리 여러 개 필요. 스키마 변경 비용이 더 큼.
- 저자 권장: 관계형 분산(방법 2). MMORPG 운영 쿼리 빈도와 규모를 고려하면 방법 2가 표준.

### 7.9 실용 쿼리 패턴 — 로그인 / 캐릭터 로드 / 인벤토리 로드

```sql
-- 로그인 인증: 계정 + 해시 검증
SELECT ID, PasswordHash FROM UserAccount WHERE ID = 'hongild'

-- 해당 계정의 캐릭터 목록
SELECT ID FROM Character WHERE OwnerUserAccountID = 'hongild'

-- 특정 캐릭터 상세 정보
SELECT * FROM Character WHERE ID = 'LittleElf'

-- 캐릭터 인벤토리 전체 로드
SELECT * FROM Item WHERE OwnerCharacterID = 'LittleElf'
```

관계 계층을 ID 체인으로 추적: 계정 ID → 캐릭터 ID → 아이템 목록. EF Core 사용 시에도 같은 계층으로 Include/ThenInclude 작성.

### 7.9.1 트랜잭션 (Transaction)
- **정의**: 여러 SQL 문을 **하나의 논리 단위**로 묶어 All-or-Nothing 실행을 보장하는 메커니즘.
- **예시**: 플레이어 A가 B에게 골드 100 이전 시:
  - `UPDATE Wallet SET Gold = Gold - 100 WHERE CharID = 'A'`
  - `UPDATE Wallet SET Gold = Gold + 100 WHERE CharID = 'B'`
  - 두 문 중 하나만 실행되면 골드가 증발/복제됨 → 트랜잭션이 둘 다 성공 OR 둘 다 롤백 보장.
- **SQL 기본 구문** (일반 지식 보완):

```sql
BEGIN TRANSACTION
  UPDATE Wallet SET Gold = Gold - 100 WHERE CharID = 'A'
  UPDATE Wallet SET Gold = Gold + 100 WHERE CharID = 'B'
COMMIT          -- 이상 없으면 확정
-- 오류 발생 시: ROLLBACK  -- 전부 취소
```

- **ACID 속성** (일반 지식 보완):
  - Atomicity(원자성): 전부 or 없음.
  - Consistency(일관성): 제약 조건 항상 유지.
  - Isolation(격리성): 동시 트랜잭션 간 간섭 없음.
  - Durability(지속성): COMMIT 후 서버 재시작해도 보존.
- **게임 서버 적용 대상**: 아이템 거래, 재화 이전, 인벤토리 슬롯 이동 — 항상 트랜잭션 묶기.
- EF Core에서는 `context.Database.BeginTransactionAsync()` 또는 `SaveChangesAsync()`의 암묵적 트랜잭션 활용.

### 7.10 게임 서버에서 쿼리 실행
- 게임 서버 → DB 연결: 네트워크 TCP 연결과 동일한 개념. 연결 객체 생성 후 Open → 쿼리 실행 → Close(또는 Connection Pool 반납).
- 연결 정보 필수 항목: DB 서버 주소, 인스턴스 이름, 서비스 계정 ID/PW, DB 이름.
- 게임 서버는 플레이어 개인 계정이 아닌 **전용 서비스 계정**으로 DB 접속 (예: `serverbot`).
- 원시 ADO.NET 예시 (일반 지식 보완):

```csharp
// 연결 열기
using var conn = new SqlConnection(connectionString);
await conn.OpenAsync();

// 쿼리 실행
using var cmd = new SqlCommand(
    "SELECT * FROM Character WHERE OwnerUserAccountID=@accountId", conn);
cmd.Parameters.AddWithValue("@accountId", accountId);

using var reader = await cmd.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    // 레코드 읽기
}
```

- **비동기 필수**: `OpenAsync` / `ExecuteReaderAsync` — 틱 루프(20TPS)에서 동기 블로킹 DB 호출은 헌법 #5 위반.
- **Connection Pool**: SqlConnection은 기본적으로 풀링됨. `Close()`는 풀 반납이지 실제 소켓 종료 아님.

### 7.11 보안 주의사항
- **관리자 계정 격리**: DBA(관리자)와 서버 서비스 계정 분리. 서비스 계정에는 필요한 테이블 접근 권한만 부여(최소 권한 원칙).
- **네트워크 격리**: DB는 게임 서버 전용 내부 네트워크에서만 접근 가능하도록 방화벽/VPN 설정. 외부 인터넷 직접 노출 금지.
- **SQL Injection 방지**: 클라이언트 입력을 쿼리 문자열에 직접 연결(문자열 concatenation) 절대 금지 → Parameterized Query 필수.

```csharp
// 취약 (절대 하면 안 됨)
string query = "SELECT * FROM UserAccount WHERE ID='" + userInput + "'";

// 안전 (Parameterized)
cmd.CommandText = "SELECT * FROM UserAccount WHERE ID=@id";
cmd.Parameters.AddWithValue("@id", userInput);
```

---

## 우리 프로젝트 적용

**[채택 후보 — M5 영속화 마일스톤 대상]**

| 책의 개념 | Dawnholder 매핑 |
|---|---|
| UserAccount 테이블 | `02_Server/GameServer/DB/` 또는 EF Core Entity `UserAccount.cs` |
| Character 테이블 | `CharacterEntity.cs` — Level/HP/Position 컬럼 포함 |
| Item 테이블 | `ItemEntity.cs` — OwnerCharacterID FK, ItemType/Quantity |
| 트랜잭션 (골드이전) | 인벤토리 이전·재화 소모 로직 — `DbContext.Database.BeginTransactionAsync()` |
| Parameterized Query | EF Core LINQ → 자동 파라미터화. 원시 SQL 쓸 때 `FromSqlRaw`에 파라미터 필수 |
| 비동기 DB 호출 | `02_Server/GameServer/Loop/` 틱 루프 바깥에서 실행 + `QueuedWriter` 패턴으로 tick 비차단 |
| 서비스 계정 | LocalDB 개발 시 Windows 통합 인증, 프로덕션 시 전용 SQL 계정 |

현재 상태: `PlayerEntity`는 메모리 휘발 상태(`02_Server/GameServer/Maps/PlayerEntity.cs`). DB Entity 클래스 및 EF Core DbContext 미존재. M5 시작 시 이 장이 직접 근거.

**영속화 저장 주기** (헌법 Gameplay Pillars):
- 30초 주기 스냅샷 저장
- 로그아웃 시 즉시 저장
- 중요 이벤트 시 즉시 저장 (레벨업, 희귀 드랍, 거래)

→ `SnapshotWriter`(큐드 라이터 패턴)가 tick 루프 외부에서 비동기 Upsert 실행하는 구조 권장.

---

## 함정 / 과용 경계

- **tick 루프 안 동기 DB 호출 금지** — `SqlConnection.Open()` / EF Core `SaveChanges()` 는 수십~수백 ms 블로킹. 20TPS(50ms 틱) 루프 안에서 직접 호출하면 전체 맵이 멈춤. 헌법 #5 직접 위반.
- **N+1 쿼리 함정** — 캐릭터 N명 각각의 아이템을 개별 SELECT → DB 왕복 N회. 로그인 시 캐릭터+아이템 JOIN 또는 `Include(c => c.Items)` 한 번에 로드.
- **방법 1(JSON/XML BLOB) 초기 편의 함정** — 초반엔 구현이 쉽지만, 운영 쿼리("레벨 50 이상 캐릭터 전체 조회")나 경쟁 조건(두 서버가 같은 BLOB 동시 수정) 발생 시 대규모 리팩토링 필요. M5부터 방법 2(관계형 분산)로 시작 권장.
- **트랜잭션 범위 과도 확장** — 트랜잭션이 길수록 DB 락 점유 시간 증가. 게임 서버의 트랜잭션은 단일 비즈니스 단위(골드 이전 1건)로 최소화.
- **Connection Pool 고갈** — 비동기 호출 후 `conn.Close()`를 `finally`나 `using` 블록으로 반드시 반납. 누수 시 동시 접속자 증가하면 연결 대기 큐잉.
- **SQL Injection** — 클라이언트 입력(캐릭터 이름, 채팅) 직접 쿼리 문자열 삽입 절대 금지. EF Core LINQ는 자동 파라미터화지만 `FromSqlRaw` 사용 시 주의.

---

## 관련
- [[game-server-programming/08-nosql]] — 8장: NoSQL DB 비교(MongoDB 등), BLOB 직렬화 방식의 대안
- [[game-programming-patterns/09-update-method]] — Entity 틱 업데이트 패턴 (tick 루프 내 DB 호출 금지와 연계)
