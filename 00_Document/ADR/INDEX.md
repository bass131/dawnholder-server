# 기술 결정 색인

ADR은 선택 당시의 이유와 비용을 보존한다. 현재 운영 권한은 [AGENTS](../../AGENTS.md), 실제 구현은 [ARCHITECTURE](../ARCHITECTURE.md)를 확인한다. 채택된 설계가 모두 구현되었다는 뜻은 아니다. 특히 SQL 영속화와 거점 기능은 구현 상태를 따로 확인한다.

## 기술・제품 결정

| 번호 | 결정 |
|---|---|
| ADR-001 | [Unity・.NET・공유 런타임 선택](tech-stack/ADR-001-unity-dotnet-versions.md) |
| ADR-002 | [TCP・PDL・코드 생성](tech-stack/ADR-002-tcp-pdl.md) |
| ADR-003 | [모노레포와 별도 MES 저장소](tech-stack/ADR-003-monorepo.md) |
| ADR-004 | [20 TPS 서버 틱](tech-stack/ADR-004-tickrate.md) |
| ADR-005 | [SQL Server・EF Core 선택(영속화 설계)](tech-stack/ADR-005-mssql-efcore.md) |
| ADR-006 | [사냥과 거점 성장의 제품 방향](gameplay/ADR-006-genre-mix.md) |
| ADR-007 | [거점 시설의 구매・기능 제공 모델](gameplay/ADR-007-stronghold-model.md) |
| ADR-008 | [단일 서버 프로세스](gameplay/ADR-008-single-process.md) |
| ADR-009 | [게임 백엔드 포트폴리오 범위](gameplay/ADR-009-portfolio-target.md) |
| ADR-010 | [공유 DLL과 디버그 정보](tech-stack/ADR-010-shared-dll.md) |
| ADR-011 | [기존 ServerDev 코드의 부분 채택](tech-stack/ADR-011-serverdev-scenario-b.md) |
| ADR-012 | [클라이언트 전송 라이브러리 분리](tech-stack/ADR-012-socket-y2.md) |
| ADR-017 | [ASCII 프로젝트 경로](tech-stack/ADR-017-ascii-path.md) |
| ADR-021 | [Unity UI Additive Scene 분리](harness/ADR-021-client-ui-additive-scene.md) |
| ADR-026 | [맵 이동 시 entity ID 유지](tech-stack/ADR-026-entity-id-global-pool.md) |
| ADR-027 | [클라이언트 bootstrap과 연결 수명주기](harness/ADR-027-client-bootstrap-persistent-services.md) |
| ADR-028 | [코드 규칙과 참고자료 분리](harness/ADR-028-code-convention.md) |
| ADR-029 | [WSL에서 .NET 실행](harness/ADR-029-wsl2-dotnet-execution-standard.md) |
| ADR-030 | [행동 상태의 서버 권위](gameplay/ADR-030-server-authoritative-action-rules.md) |
| ADR-033 | [구조 이름과 책임 경계](tech-stack/ADR-033-structure-naming-boundaries.md) |

ADR-021・027・028・029는 기존 harness 경로에 있지만 기술 결정도 담고 있어 원문을 유지했다. 본문의 옛 도구・필수 절차는 당시 운영 기록이며 현재 지침을 덮어쓰지 않는다.

## 종료된 운영 결정

다음 결정은 영역별 보관에 요약과 원문을 남겼다. 현재 작업 절차로 적용하지 않는다.

| 번호 | 과거 주제 |
|---|---|
| ADR-013 | [작업 결과・회고 문서](../archive/workflow/legacy-decisions.md#source-35a469c35ce5) |
| ADR-014 | [문서 길이 기준](../archive/workflow/legacy-decisions.md#source-3d1e41694cde) |
| ADR-015 | [작업 후 검사](../archive/workflow/legacy-decisions.md#source-389ab65637f2) |
| ADR-016 | [Notion 협업 분담](../archive/workflow/legacy-decisions.md#source-a762dc07ff8e) |
| ADR-018 | [작업 상태 보존](../archive/workflow/legacy-decisions.md#source-75630e314736) |
| ADR-019 | [리뷰 에이전트](../archive/workflow/legacy-decisions.md#source-2f318205b5c4) |
| ADR-020 | [Claude 훅 실행 환경](../archive/workflow/legacy-decisions.md#source-b2b65307970b) |
| ADR-022 | [Claude 운영 체계](../archive/workflow/legacy-decisions.md#source-0401e39b8a8e) |
| ADR-023 | [작업 상태 동기화](../archive/workflow/legacy-decisions.md#source-25b81bb57514) |
| ADR-024 | [문서와 구현의 정기 대조](../archive/workflow/legacy-decisions.md#source-89f44ecfc98f) |
| ADR-025 | [이전 학습 기록 절차 종료](../archive/workflow/legacy-decisions.md#source-7a20d3b9c6e6) |
| ADR-031 | [작업 진행・보고 방식](../archive/workflow/legacy-decisions.md#source-a794c5a66fc0) |
| ADR-032 | [Claude 목표 루프](../archive/workflow/legacy-decisions.md#source-9f27ca064173) |

[변경 이력](../ADR_History.md) · [새 결정 기록 방법](../ADR.md)
