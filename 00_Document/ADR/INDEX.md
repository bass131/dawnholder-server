# ADR INDEX — 카테고리별 결정 목록

> 본 폴더는 [`../ADR.md`](../ADR.md)의 본문이 220줄 임계 도달로 카테고리 외부화된 결과입니다 (ADR-014 정책 (b) 패턴).
>
> 새 ADR 추가 시: (1) 적절한 카테고리 폴더에 `ADR-NNN-slug.md` 생성 → (2) 본 INDEX에 한 줄 추가 → (3) `../ADR.md`의 후보 표 갱신 → (4) `../ADR_History.md`에 변경 이력 한 줄.

---

## tech-stack/ — 스택·도구체인·환경

| 번호 | 결정 | 파일 |
|------|------|------|
| ADR-001 | Unity 6.4 LTS + .NET 10 LTS + .NET Std 2.1 멀티타겟 | [tech-stack/ADR-001-unity-dotnet-versions.md](tech-stack/ADR-001-unity-dotnet-versions.md) |
| ADR-002 | Raw TCP + 자체 PDL + 코드 생성기 | [tech-stack/ADR-002-tcp-pdl.md](tech-stack/ADR-002-tcp-pdl.md) |
| ADR-003 | 모노레포 (MES만 별도 레포) | [tech-stack/ADR-003-monorepo.md](tech-stack/ADR-003-monorepo.md) |
| ADR-004 | 20 TPS 서버 틱 | [tech-stack/ADR-004-tickrate.md](tech-stack/ADR-004-tickrate.md) |
| ADR-005 | **MSSQL (SQL Server) + EF Core 10** (v2: PostgreSQL→MSSQL 정정) | [tech-stack/ADR-005-mssql-efcore.md](tech-stack/ADR-005-mssql-efcore.md) |
| ADR-010 | Shared 코드 공유 = DLL + Embedded PDB | [tech-stack/ADR-010-shared-dll.md](tech-stack/ADR-010-shared-dll.md) |
| ADR-011 | 기존 ServerDev 코드 부분 채택 (시나리오 B) | [tech-stack/ADR-011-serverdev-scenario-b.md](tech-stack/ADR-011-serverdev-scenario-b.md) |
| ADR-012 | Unity 클라 socket 분리 클라용 라이브러리 (Y2) | [tech-stack/ADR-012-socket-y2.md](tech-stack/ADR-012-socket-y2.md) |
| ADR-017 | 프로젝트 폴더 ASCII 경로 이동 | [tech-stack/ADR-017-ascii-path.md](tech-stack/ADR-017-ascii-path.md) |

## gameplay/ — 게임 디자인·스코프

| 번호 | 결정 | 파일 |
|------|------|------|
| ADR-006 | 두 장르 결합을 MVP 핵심으로 유지 | [gameplay/ADR-006-genre-mix.md](gameplay/ADR-006-genre-mix.md) |
| ADR-007 | 거점 시설 = "구매 → 기능 제공" 모델 | [gameplay/ADR-007-stronghold-model.md](gameplay/ADR-007-stronghold-model.md) |
| ADR-008 | 단일 서버 프로세스 (분산/샤딩 없음) | [gameplay/ADR-008-single-process.md](gameplay/ADR-008-single-process.md) |
| ADR-009 | 포트폴리오 타겟 = 게임 회사 백엔드 | [gameplay/ADR-009-portfolio-target.md](gameplay/ADR-009-portfolio-target.md) |

## harness/ — 작업 흐름·문서·훅

| 번호 | 결정 | 파일 |
|------|------|------|
| ADR-013 | -DONE.md 페어 박제 정책 (AI=사실 / 본인=회고 분업) | [harness/ADR-013-done-md-pair.md](harness/ADR-013-done-md-pair.md) |
| ADR-014 | 문서 세분화 정책 (220줄 임계 + 헌법 350줄 예외) | [harness/ADR-014-doc-length-thresholds.md](harness/ADR-014-doc-length-thresholds.md) |
| ADR-015 | Post-flight 게이트 (validate-phase-gate.sh 훅) | [harness/ADR-015-postflight-gate.md](harness/ADR-015-postflight-gate.md) |
| ADR-016 | Notion 협업 3자 분업 (Claude / Codex / 본인) | [harness/ADR-016-notion-3way.md](harness/ADR-016-notion-3way.md) |
| ADR-018 | 하네스 망각 안전망 — 작업 봉투 + 핀 + WORK-ID *(부분 superseded — ADR-022)* | [harness/ADR-018-forgetting-safety-net.md](harness/ADR-018-forgetting-safety-net.md) |
| ADR-019 | Reviewer 에이전트 도입 (Tier 2 자동 리뷰) *(부분 갱신 — ADR-022)* | [harness/ADR-019-reviewer-agent.md](harness/ADR-019-reviewer-agent.md) |
| ADR-020 | 훅 실행 환경 의존성 (Git Bash on Windows) + 검증 패턴 | [harness/ADR-020-hook-env-deps.md](harness/ADR-020-hook-env-deps.md) |
| ADR-021 | 클라이언트 UI는 별도 Additive Scene으로 분리 | [harness/ADR-021-client-ui-additive-scene.md](harness/ADR-021-client-ui-additive-scene.md) |
| ADR-022 | 새 하네스 v1 (M3.5 — 5/20 의논 + NDREAM 패턴 흡수 + KPI 전환) | [harness/ADR-022-new-harness-v1.md](harness/ADR-022-new-harness-v1.md) |

---

## 후보 ADR (아직 채택 안 됨)

본문 [`../ADR.md`](../ADR.md#채워질-adr-후보들-예시) 참조. 후보 번호는 채택 순서대로 부여되어 변동될 수 있음.
