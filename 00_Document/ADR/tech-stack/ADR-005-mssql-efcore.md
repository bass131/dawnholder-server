### ADR-005: MSSQL (SQL Server) + EF Core 10
**날짜**: (Harness 셋업일) — **2026-05-11 EF Core 8 → 10 정합** — **2026-05-14 PostgreSQL → MSSQL 정정**
**상태**: 채택됨 (v2)
**결정**: 영속화 DB로 **Microsoft SQL Server**(개발 환경 = LocalDB,
인증 방식 = Windows 통합 인증), ORM은 Entity Framework Core 10.
**이유**:
- **포트폴리오 정합성**: 한국 게임 업계(넥슨/엔씨/스마일게이트 등)
  백엔드는 MSSQL/SQL Server가 압도적. 게임 회사 백엔드 포지션
  포트폴리오 목적(ADR-009)에 직결.
- **학습 자료 정합성**: 본인 학습 베이스인 Rookiss(ADR-011·ADR-012의
  한국 MMO 백엔드 카테고리) 후속편들이 MSSQL + EF Core 기반.
- **.NET 1군 정합**: .NET 10 LTS + EF Core 10 + SQL Server 2022가
  MS 1군 조합. 도구(SSMS, Azure Data Studio), 문서, 에러 메시지
  생태계가 가장 매끄러움.
- **온보딩 비용**: 본인·팀원 전원 Windows. LocalDB는 Visual Studio
  설치 시 같이 깔림 → 학부생 백지 팀원 셋업 비용 최소화 (ADR-017
  ASCII 경로 결정과 같은 결: "팀원 셋업 마찰 줄이기").
- **인증 방식 = Windows 통합 인증**: 연결 문자열에 비밀번호 0개.
  `.gitignore` secret 격리 실수가 물리적으로 불가능. 협업 환경
  보호. 운영 진입 시점에 SQL 인증 전환은 별도 ADR 후보.
**트레이드오프**:
- **Linux 운영 비용**: SQL Server on Linux는 가능하지만 PostgreSQL
  on Linux보다 어색. Developer Edition은 무료지만 운영 라이선스는
  유료. 본 프로젝트는 학습/포트폴리오라 운영 진입 시점에 재검토.
- **Docker 이미지 무게**: PostgreSQL ~150MB vs SQL Server ~1.5GB.
  CI/CD 빌드 시간 영향 있으나 본 단계에서는 LocalDB 사용으로 회피.
- **JSONB 같은 NoSQL 친화 기능**: PostgreSQL JSONB가 SQL Server JSON
  컬럼보다 강력. 다만 캐릭터 데이터를 한 JSON 컬럼에 박는 건 학습
  가치 낮음 → 정규화 쪽이 백엔드 포트폴리오에 더 부합. 영향 작음.
- **Windows 인증의 운영 호환성**: 컨테이너/Linux 환경 안 됨. 운영
  진입 시점에 SQL 인증 + secret 관리 전략 ADR로 박제 예정.

**후속 ADR 후보**:
- 운영 진입 시 SQL 인증 + secret 관리(User Secrets / 환경 변수 /
  Azure Key Vault) 전환 결정
- ADR-021 캐릭터 데이터 스키마 (정규화 vs JSON 컬럼)

**Supersedes**: v1 (PostgreSQL + EF Core). v1은 학습 친화성·도커
용이성을 들었으나, 한국 게임 업계 표준 정합 + Windows 1군 조합
가치가 본 프로젝트 목적에 더 부합한다는 재평가.
