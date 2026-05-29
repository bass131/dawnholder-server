---
title: NoSQL 기초 — 캐시·분산 저장·MongoDB 실전
source: 게임 서버 프로그래밍 교과서
category: 08장 NoSQL 기초
---
# [GSP-08] NoSQL 기초 (NoSQL Fundamentals)
> 관계형 DB의 스키마 경직성·수평 확장 한계를 NoSQL로 보완. 게임 로그/통계/빠른 프로토타입에 활용. MongoDB CRUD + 수평 샤딩 실습 중심.

---

## 언제 참조하나 (트리거)
- DB 스키마가 자주 바뀌어서 ALTER TABLE 비용이 걱정될 때.
- 플레이어 로그·행동 통계·이벤트 이력처럼 write-heavy + 구조 이질적 데이터를 저장해야 할 때.
- DB 수평 확장(샤딩)이나 고가용성(레플리카) 설계를 논의할 때.
- EF Core(RDBMS) 외에 MongoDB 드라이버 wiring을 서버에 추가할 때.

---

## 핵심 내용

### 8.1 관계형 DB와 NoSQL — 차이점 (접근 가능)
- RDBMS는 모든 레코드가 동일 필드 구조를 가져야 함. 새 필드 추가 = 1억 행 전체에 ALTER 필요.
  - 실용 우회책: null 허용 컬럼 추가 — 기존 레코드는 null, 신규 레코드만 값 가짐.
- NoSQL(문서형)은 도큐먼트마다 다른 필드 구조를 허용 — 스키마 유연성이 핵심 강점.
- NoSQL은 "특정 전통적 우선순위(원자성·일관성·복잡 쿼리)를 희생하고 다른 이점(수평 확장·유연 스키마·가용성)을 얻는" 트레이드오프.
- 요약 비교:

| 항목 | RDBMS | NoSQL(MongoDB) |
|------|-------|---------------|
| 레코드 구조 | 동일 필드 강제 | 도큐먼트별 자유 |
| 우선순위 | 원자성·일관성 | 가용성·수평 확장 |
| 쿼리 기능 | 풍부·표준화(SQL) | 제한적·독자 문법 |
| 스키마 변경 비용 | 높음 | 낮음 |

### 8.2 수직 분산 vs. 수평 분산(샤딩) (접근 가능)
- **수직 분산(Vertical Sharding)**: 테이블 단위로 다른 DB 서버에 배치. 테이블 수 한계 = 분산 한계.
- **수평 분산(Horizontal Sharding)**: 같은 테이블의 레코드를 여러 서버에 쪼개 저장.
  - 예: 1억 행 → 100대에 100만 행씩. 각 서버 부하 균등화.
  - 분산된 데이터 조각 = **샤드(shard)**. RDBMS에서는 직접 구현, MongoDB에는 내장.

### 8.3 고가용성 — 미러링 복제 (접근 가능)
- 고가용성: 하드웨어 장애 시에도 서비스 연속성 유지.
- 기법: **Active-Passive 미러링**.
  - DB 1(액티브/마스터): 실운영 쓰기·읽기 담당.
  - DB 2(패시브/슬레이브): 실시간 데이터 복제(replication) 수신.
  - 마스터 장애 시 슬레이브가 자동 승격(failover) → 서비스 무중단.
- 이 이중화(Redundancy) 구조가 MongoDB Replica Set의 이론적 토대.

### 8.4 JSON 이해 (접근 가능)
- MongoDB 데이터 포맷 = JSON/BSON.
- `{}` = 객체, 내부는 `"키": 값` 쌍. 값은 문자열·숫자·배열·중첩 객체 가능.
- 예: `{ "playerId": 42, "hp": 100, "items": ["sword", "shield"] }`

### 8.5 MongoDB 기본 구조 (접근 가능)
- RDBMS 개념 대응:
  - DB 인스턴스 → DB 인스턴스 (동일)
  - 테이블 → **컬렉션(Collection)**
  - 레코드 → **도큐먼트(Document)**
- 컬렉션은 미리 생성할 필요 없음 — 첫 insert 시 자동 생성.

### 8.6 CRUD (접근 가능)
- **Create** `db.<COLL>.insert(<JSON_OBJECT>)` — 컬렉션 없으면 자동 생성.
- **Read** `db.<COLL>.find(<COND>)` — `{}` 조건이면 전체 반환. `findOne()`은 첫 결과만.
  - SQL 대응: `SELECT * FROM <COLL> WHERE <COND>`
- **Update** `db.<COLL>.update({K:V}, {$set:{K2:V2}})` — 첫 인자로 대상 도큐먼트 지정, `$set`으로 필드 갱신.
- **Delete** `db.<COLL>.remove({K:V})` — find와 같은 조건 문법으로 대상 지정.

### 8.7 성능 분석 (접근 가능)
- 쿼리 뒤에 `.explain("executionStats")` 체이닝 → 실행 통계(스캔 수·소요 시간) 출력.
- DB 과부하 원인 파악의 1차 진단 도구. 인덱스 미설정 컬렉션 full-scan 탐지에 유효.
- SQL Server의 실행 계획(Execution Plan)과 동일한 역할.

### 8.8 MongoDB 수평 확장 — 샤딩 (접근 가능)
- 단일 서버 한계 도달 시 수직 증설보다 **수평 확장(샤딩)** 현실적.
- MongoDB 내장 샤딩: 데이터를 조각(shard)으로 나눠 여러 인스턴스에 분산.
- 9장(분산 서버 아키텍처)에서 수직 vs. 수평 확장 심화.
- Replica Set = 고가용성(8.3) + 샤딩 = 수평 확장. 운영 MongoDB는 둘을 조합.

### 8.9 게임 서버에서 MongoDB 명령 실행 (접근 가능)
- MongoDB는 C++·C#·Java·Python 등 공식 클라이언트 드라이버 제공.
- C# 기준 의사코드 3단계 wiring:
  ```
  client = new MongoClient("mongodb://localhost:27017");
  db     = client["mydb"];
  coll   = db["mycollection"];
  // 이후 coll.Insert / Find / Update / Remove 호출
  ```
- 게임 서버 내에서 동기 드라이버 호출은 tick 루프 밖(큐드 라이터)에서만.

### 8.10 요약 — 실전 활용 포지셔닝 (접근 가능)
- NoSQL은 RDBMS를 완전 대체하지 않음. 게임 서버에서는 **혼용**이 표준.
- NoSQL 적합 용도: 로그·통계·이벤트 이력, 스키마 미확정 초기 프로토타입, 빠른 write-heavy 저장.
- RDBMS 유지 용도: 캐릭터/인벤토리/거래 등 원자성·정합성 필요한 핵심 게임 데이터.

---

## 우리 프로젝트 적용

### 현재 상태: RDBMS(SQL Server + EF Core) 단독 사용
- `02_Server/` — EF Core 10 + SQL Server LocalDB. 현재 영속화 대상: 플레이어 스냅샷 30초 cadence + 로그아웃 + 중요 이벤트 (헌법 §Gameplay Pillars).
- NoSQL은 아직 미채택. 현재 구조에서 RDBMS로 충분한 단계.

### 채택 후보 — 두 가지 유력 용도
1. **이벤트/행동 로그 (write-heavy, 스키마 이질)**: 전투 로그·cheat-flag 로그(M4.4 예정)·아이템 획득 이력. 레코드마다 필드가 다르고 볼륨이 큼 → NoSQL 적합.
2. **치트 탐지 + 통계 분석 파이프라인**: MongoDB의 집계(aggregation) 파이프라인으로 패턴 분석 가능. SQL JOIN 없이 플레이어별 행동 통계 빠르게 조회.

### 현재 무관
- 캐릭터 HP·위치·인벤토리·레벨업 — 원자성 필요 → RDBMS 유지.
- PDL 프로토콜 패킷 정의 — RDBMS와 무관.

---

## 함정 / 과용 경계
- **NoSQL = "빠르다" 오해**: 인덱스 없으면 full-scan. RDBMS와 마찬가지로 `.explain()` + 인덱스 설계 필수.
- **트랜잭션 맹신 금지**: MongoDB 4.0+ 다중 도큐먼트 트랜잭션 지원하지만 RDBMS보다 제약 많음. 원자성 필요한 인벤토리·거래는 RDBMS에 남겨야 함.
- **tick 루프 내 동기 드라이버 호출 금지**: 헌법 원칙 #5 — DB 호출은 큐드 라이터 경유. MongoDB C# 드라이버 async 버전(`InsertOneAsync`, `FindAsync`) 사용.
- **샤딩 조기 도입 경계**: 학습 단계 프로젝트에서 샤딩은 과분할. 단일 인스턴스 + Replica Set 수준에서 충분히 학습 가능.
- **RDBMS 완전 교체 금지**: 로그 저장에 NoSQL 도입하더라도 캐릭터/거래 데이터는 SQL Server 유지. 혼용이 정석.

---

## 관련
- [[07-database]] — EF Core + SQL Server 설계
- 헌법 원칙 #5 — tick 루프 블로킹 금지 (큐드 라이터 패턴)
- `00_Document/ADR/` — 영속화 스택 결정 기록 (ADR-001, EF Core 채택 근거)
