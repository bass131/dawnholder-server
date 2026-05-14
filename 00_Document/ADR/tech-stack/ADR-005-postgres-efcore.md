### ADR-005: PostgreSQL + EF Core 10
**날짜**: (Harness 셋업일) — **2026-05-11 EF Core 8 → 10 정합**
**상태**: 채택됨
**결정**: 영속화 DB로 PostgreSQL, ORM은 Entity Framework Core 10.
**이유**: 학습 친화적(SQL 표준 잘 따름), 도커 띄우기 쉬움, EF Core는
.NET 표준 ORM. 마이그레이션 도구 잘 되어있음. EF Core 10은 .NET 10 LTS와
정합된 같은 세대 (ADR-001).
**트레이드오프**: NoSQL(예: Redis) 대비 복잡한 인덱스 설계 필요.
EF Core는 raw query 대비 추상화 비용이 약간 있음. 캐싱 레이어는 추후 추가.
