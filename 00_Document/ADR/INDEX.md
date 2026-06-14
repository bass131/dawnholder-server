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
| ADR-026 | entity id 전역 풀 (맵 간 id 유지) | [tech-stack/ADR-026-entity-id-global-pool.md](tech-stack/ADR-026-entity-id-global-pool.md) |

## gameplay/ — 게임 디자인·스코프

| 번호 | 결정 | 파일 |
|------|------|------|
| ADR-006 | 두 장르 결합을 MVP 핵심으로 유지 | [gameplay/ADR-006-genre-mix.md](gameplay/ADR-006-genre-mix.md) |
| ADR-007 | 거점 시설 = "구매 → 기능 제공" 모델 | [gameplay/ADR-007-stronghold-model.md](gameplay/ADR-007-stronghold-model.md) |
| ADR-008 | 단일 서버 프로세스 (분산/샤딩 없음) | [gameplay/ADR-008-single-process.md](gameplay/ADR-008-single-process.md) |
| ADR-009 | 포트폴리오 타겟 = 게임 회사 백엔드 | [gameplay/ADR-009-portfolio-target.md](gameplay/ADR-009-portfolio-target.md) |
| ADR-030 | 행동 상태 규칙 = 서버 권위 (클라 Animator Exit Time = 시각 거울) | [gameplay/ADR-030-server-authoritative-action-rules.md](gameplay/ADR-030-server-authoritative-action-rules.md) |

## harness/ — 작업 흐름·문서·훅

| 번호 | 결정 | 파일 |
|------|------|------|
| ADR-013 | -DONE.md 페어 박제 정책 (AI=사실 / 본인=회고 분업) *(회고 절반 superseded — ADR-025)* | [harness/ADR-013-done-md-pair.md](harness/ADR-013-done-md-pair.md) |
| ADR-014 | 문서 세분화 정책 (220줄 임계 + 헌법 350줄 예외) | [harness/ADR-014-doc-length-thresholds.md](harness/ADR-014-doc-length-thresholds.md) |
| ADR-015 | Post-flight 게이트 (validate-phase-gate.sh 훅) | [harness/ADR-015-postflight-gate.md](harness/ADR-015-postflight-gate.md) |
| ADR-016 | Notion 협업 3자 분업 (Claude / Codex / 본인) | [harness/ADR-016-notion-3way.md](harness/ADR-016-notion-3way.md) |
| ADR-018 | 하네스 망각 안전망 — 작업 봉투 + 핀 + WORK-ID *(부분 superseded — ADR-022)* | [harness/ADR-018-forgetting-safety-net.md](harness/ADR-018-forgetting-safety-net.md) |
| ADR-019 | Reviewer 에이전트 도입 (Tier 2 자동 리뷰) *(부분 갱신 — ADR-022)* | [harness/ADR-019-reviewer-agent.md](harness/ADR-019-reviewer-agent.md) |
| ADR-020 | 훅 실행 환경 의존성 (Git Bash on Windows) + 검증 패턴 | [harness/ADR-020-hook-env-deps.md](harness/ADR-020-hook-env-deps.md) |
| ADR-021 | 클라이언트 UI는 별도 Additive Scene으로 분리 | [harness/ADR-021-client-ui-additive-scene.md](harness/ADR-021-client-ui-additive-scene.md) |
| ADR-022 | 새 하네스 v1 (M3.5 — 5/20 의논 + NDREAM 패턴 흡수 + KPI 전환) | [harness/ADR-022-new-harness-v1.md](harness/ADR-022-new-harness-v1.md) |
| ADR-023 | work-pin/CONTEXT 동기화 결함 — 진행 단계 stale hole 봉합 (M3.7 — 옵션 C 게이트 보강, `/session:start` drift 발견 단계 신설) *(CONTEXT 절반 superseded — ADR-025, drift 게이트는 work-pin 단독으로 유지)* | [harness/ADR-023-sync-gate-progress-stale-hole.md](harness/ADR-023-sync-gate-progress-stale-hole.md) |
| ADR-024 | false-promise 주기적 감사 cadence (M3.7 — 누적 12건+ Rule of Three 3회 통과, 마일스톤 마감 + ad-hoc X건 트리거) | [harness/ADR-024-false-promise-cadence.md](harness/ADR-024-false-promise-cadence.md) |
| ADR-025 | CONTEXT 3종 + 학습 일지 트랙 B 은퇴, work-pin 단일 핸드오프 (M4.1 — ADR-013 회고/ADR-023 CONTEXT/ADR-022 트랙 B 부분 supersede) | [harness/ADR-025-retire-context-trio-and-learning-track.md](harness/ADR-025-retire-context-trio-and-learning-track.md) |
| ADR-027 | 클라 Bootstrap(코드 주도 RuntimeInitialize) + Persistent Services + 연결 생명주기 A안 (M4.2 — ADR-021 scene-lifecycle 확장, ① DontDestroyOnLoad-per-service WIP supersede, B/로그인은 M5 이월) | [harness/ADR-027-client-bootstrap-persistent-services.md](harness/ADR-027-client-bootstrap-persistent-services.md) |
| ADR-028 | Code Convention 수립 (GPP 19 + 게임서버 교과서 10 참고서 + 우리 규칙 + 강제 4중) — God class 분리 결정 기준, refs/CODE_CONVENTION/INDEX 3층 | [harness/ADR-028-code-convention.md](harness/ADR-028-code-convention.md) |
| ADR-029 | SAC dotnet 실행 차단 — WSL2 실행 표준 (로컬 테스트 부활, 세션16 "SAC 게이트 은퇴 = CI 단독" 부분 supersede, PoC 5항목 게이트) | [harness/ADR-029-wsl2-dotnet-execution-standard.md](harness/ADR-029-wsl2-dotnet-execution-standard.md) |
| ADR-031 | Phase 자동 진행 + 보고 비동기 문서화 (학습 호흡 수동 멈춤 폐기 = ADR-025 드리프트 봉합, HTML 임계 대규모→복잡, Stop=영호 직접확인 4종) *(ADR-015 "학습 호흡 보존"·ADR-022 "5단계 대규모 인라인" supersede)* | [harness/ADR-031-auto-phase-progression-async-reporting.md](harness/ADR-031-auto-phase-progression-async-reporting.md) |

---

## 후보 ADR (아직 채택 안 됨)

본문 [`../ADR.md`](../ADR.md#채워질-adr-후보들-예시) 참조. 후보 번호는 채택 순서대로 부여되어 변동될 수 있음.
