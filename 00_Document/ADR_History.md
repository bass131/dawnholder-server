# ADR 변경 이력

> [`ADR.md`](ADR.md)의 변경 이력 외부화 (헌법: 문서 세분화 정책 — 누적 섹션 외부화).
>
> 새 ADR 추가/갱신 시 본 파일에 한 줄씩 추가. ADR.md 본문은 *현재 결정*만 담음.

| 날짜 | 변경 | 이유 |
|------|------|------|
| (Harness 셋업일) | 최초 작성 + ADR-001~005 시드 | Harness 셋업에서 결정한 것들 박제 |
| (PRD 1차 작성일) | ADR-006~009 추가 | PRD 1차 작성 과정에서 결정된 것들 박제 |
| 2026-05-06 | ADR-001 갱신 (.NET 8 → .NET 10 LTS + .NET Standard 2.1) | .NET 9 STS 만료 임박, .NET 8 LTS도 본 마감 직전 만료. .NET 10 LTS가 시연 후 시점도 커버. shared/는 Unity 호환 위해 .NET Standard 2.1. |
| 2026-05-06 | ADR-002 갱신 (외부 직렬화안 → 자체 PDL + 코드 생성기) | 본인이 4월에 작성한 PDL 시스템 채택. 면접 임팩트 + 학습 가치. |
| 2026-05-06 | ADR-010 신규 (DLL + Embedded PDB) | 헌법 #4 (복사-붙여넣기 금지) 물리적 강제. 비개발자 팀원 보호. |
| 2026-05-06 | ADR-011 신규 (기존 ServerDev 코드 부분 채택, 시나리오 B) | 6월 캡스톤 옵션 C(2인 movement) 6주 일정 확보. 게임 로직은 헌법 적용 위해 새로 작성. |
| 2026-05-09 | ADR-001 갱신 (Unity 2022 LTS → Unity 6.4 LTS) | Unity AI MCP Server 활용 + Unity 6 새 기능 + LTS 라이프사이클이 더 김. |
| 2026-05-10 | ADR-012 신규 (Unity 클라 socket = Y2 분리 모델) | 현업 표준 + socket 자체 학습 가치 + 서버 변경이 클라 빌드 안 깸. 마이그 갈래(X) 실측에서도 가능했지만 학습 임팩트 우세 판단. |
| 2026-05-10 | ADR-012 보강 (Phase 07 책임 단위 정제 + 카테고리 맥락) | "현업 표준" = 한국 MMO 백엔드 카테고리(Rookiss/NCSoft/Nexon)라고 명시. Mirror/FishNet은 Unity 인디 멀티 카테고리라 본 프로젝트와 다름을 분명히. 책임 단위 분리/통합 표 추가. |
| 2026-05-11 | ADR-002 outdated 정합 (`tools/` → `99_Tools/`) | 2026-05-09 폴더 prefix 변경 시점에 누락됐던 잔존물 정정. |
| 2026-05-11 | ADR-005 outdated 정합 (EF Core 8 → 10) | 2026-05-10 ARCHITECTURE 일괄 정합 시점에 ADR.md만 누락. |
| 2026-05-11 | ADR-010 outdated 정합 (`shared/`, `client/Assets/Plugins/` → `98_Shared/`, `03_Client/Assets/Plugins/`) | 폴더 prefix 변경 누락 정합. |
| 2026-05-11 | ADR-011 트레이드오프 후속 박음 (PacketGenerator 버그 ✅ 처리됨, commit `03994b0`) | Phase 06에서 이미 정정된 결과 박제. |
| 2026-05-11 | ADR-013 신규 (-DONE.md 페어 박제 정책) | 학습 일지 미루기 전제 → AI=사실/본인=회고 분업으로 박제 누락 방지 (2026-05-10 결정). |
| 2026-05-11 | ADR-014 신규 (문서 세분화 정책 — 220줄 임계 + 헌법 350줄 예외) | 사전형 문서 비대화 방지 + 헌법은 자기참조 무한 루프 차단 위해 예외 (2026-05-10 결정). |
| 2026-05-11 | ADR-015 신규 (Post-flight 게이트 = validate-phase-gate.sh) | 자동 실행 비채택(학습 호흡), 형식 강제만 훅으로. `jha0313/harness_framework` 비교 후 결정 (2026-05-11). |
| 2026-05-11 | ADR-016 신규 (Notion 협업 3자 분업 — Claude/Codex/본인) | 사실 박제·재편집·회고 역할 분리. 자세한 원칙은 `.claude/templates/done-md-template.md`에 영속화 (2026-05-11). |
| 2026-05-11 | ADR-017 신규 (프로젝트 폴더 ASCII 경로 이동 — 한글 경로 영구 해결) | Phase 03·04에서 한글 경로 도구 호환성 사건 반복. ASCII 경로 이동 후 build/test/PacketGenerator 직접 실행 검증 완료. WDAC 차단(error 4551)은 별도 사건으로 명시 분리. |
| 2026-05-11 | 변경 이력 외부화 (ADR.md → ADR_History.md) | ADR.md 220줄 임계 대응. CONTEXT_History 패턴 동일. |
| 2026-05-12 | ADR-018 신규 (하네스 망각 안전망 — 봉투 + 핀 + WORK-ID) | LLM context decay 진단 후 입구·출구 한 짝 안전망 + WORK-ID 합류 지점 도입. Codex 3라운드 자문 반영. 헌법 5요구 중 4번(컨텍스트 분기 망각)/5번(이식성) 직접 해소. |
| 2026-05-14 | ADR 본문 카테고리 외부화 (`00_Document/ADR/{tech-stack,gameplay,harness}/` + `ADR/INDEX.md`) | ADR-014 정책 (b) 카테고리 분리 패턴 발화. ADR.md가 ADR-020 추가 시 220줄 초과 확정 → 채택된 18개 ADR을 카테고리 폴더로 분할. ADR.md는 thin landing(템플릿 + 후보 표 + INDEX 링크)으로 응축. 카테고리: tech-stack(9), gameplay(4), harness(5). |
| 2026-05-14 | ADR-020 신규 (훅 실행 환경 의존성 — Git Bash on Windows + 검증 패턴) | ADR-018 사후 검증 48시간 silent fail 사건의 박제. Windows에서 bash 훅 실행 = Git for Windows + 시스템 PATH 전제 + Claude Code가 PostToolUse/Stop exit-0 stderr를 silent 처리한다는 부수 발견. 부록 A에 재사용 가능 검증 패턴(`echo "[HOOK-MARKER] ..." >> log`) 박음. 후보 표 번호 shift — 캐릭터 데이터 스키마는 ADR-020 → ADR-021. |
| 2026-05-14 | ADR-005 v2 (PostgreSQL → MSSQL/LocalDB, Windows 통합 인증) | 협업 셋업 논의 중 DB 결정 오류 발견. 코드 진입 전 시점이라 변경 비용 최소. 이유 4개: 한국 게임 업계 표준 정합(포트폴리오 목적, ADR-009) + Rookiss 학습 자료 정합(ADR-011·012) + .NET 1군 조합 + Windows 팀원 온보딩 비용. 인증 Windows 통합—비밀번호 0개로 협업 환경에서 secret 격리 실수 불가능. 운영 진입 시 SQL 인증 전환 후속 ADR 후보. |
| 2026-05-15 | ADR-019 신규 (Reviewer 에이전트 도입 — Tier 2 자동 리뷰) | 6년차 시니어 피드백("검증 부실 — 리뷰어 에이전트 1차 검증") 대응. 3-Tier 구조(도메인 셀프/자동 통합/수동 깊은) + 스마트 트리거(30~50% 호출)로 비용 관리. REVIEW_CHECKLIST.md가 reviewer 기준 자료. 새 ADR 추가 절차 5단계(체크리스트 매핑 갱신) 수령. 코드 스타일 검증은 의도적 Scope 제외 — Roslyn analyzer 접근 후속 ADR 후보로 미루(길 D, 합류 직전 1명+추후 1명 현실에서 premature abstraction 회피). 후보 표 번호 shift — 인증 방식은 ADR-019 → ADR-021. 헌법 350줄 임계 경계선상 도달(약 350줄) → 다음 서브에이전트 주제 확장 시 외부화 필수. |
| 2026-05-17 | ADR-021 신규 (클라이언트 UI는 별도 Additive Scene으로 분리) | 정유현 Phase 03 HUD 마감 직후 *영역 분리* 가치 자각 → 팀장 합의 후 ad-hoc 작업(`ad-hoc-20260517-ui-scene-split`)으로 박음. Unity .unity 파일 YAML 머지 충돌 위험 차단 + 한국 게임 회사 정석 패턴(씬 하나당 한 명). `Assets/Scenes/UI.unity` 신설 + `Scripts/Bootstrap/SceneBootstrap` Awake에서 Additive 로드. CODEOWNERS에 UI 씬 + Scripts/Bootstrap/ + Scripts/UI/ 3 경로를 `@yuhyeon` 단독 영역으로 박음. `Scripts/Bootstrap/` 폴더 신설로 `03_Client/CLAUDE.md` Layout 표 갱신 + CHANGELOG [M] 한 줄은 팀장 영역이라 PR 동반 갱신 권유. |
| 2026-05-21 | ADR-022 신규 (새 하네스 v1 — M3.5 마일스톤 / 5/20 의논 + NDREAM 패턴 + KPI 전환) | M3 마감 직후 *바이브 도메인 시대 인식 + 팀 합류 흡수* 흐름에서 옛 하네스 4 모순 표면화 (SubAgent 도메인 모호 / Codex γ 외부 의존 / 약속 가짜화 3회 봉합 / 학습-일지 슬래시 vs KPI 전환). M3.5 Phase 01~06 6 분해 + 격리 폴더 `New_Harness/` 누적 → Phase 06 전환 commit atomic 발효. 6 핵심 변경 = (1) KPI 전환 (Planning→구현→보고) (2) 정량 4등급 + 위험 깃발 자동 상향 (3) SubAgent 풀 9 (Worker 4/Reviewer 2/Specialist 3 = Codex γ → plan-auditor 흡수) (4) Hook 7 풀세트 (rename 3 + 신설 4) (5) Knowledge 시스템 신설 (5 도메인 + GC Collector + 트랙 A/B 분리) (6) 슬래시 17 → 10 (학습 5+일지 3 = 트랙 B 이관) + 양식 다이어트 (work-envelope 죽임 / 5단계 보고 = 대규모만). 헌법 200줄 → 239줄 (절대 원칙 5개 100% 보존, 새 모델 본체 추가만). PDF NDREAM 참조 + 5/20 면담 의논 결과 + 18+ ★★★ 학습 자산 누적 위에 박힘. work-pin ↔ CONTEXT 정합 게이트 옵션 C 동반 (`/session:end` 단일 게이트 단방향 동기). 옛 운영 100% → 새 운영 100% atomic 발효 정신 = `isolation-folder-migration-pattern` (DB dual-write phase). 면접 자산화 = *방향성 결정/리팩터링 의사결정* 한국 게임 회사 백엔드 어필 결정타. 모든 팀원 영향 = CHANGELOG [H] entry + 슬랙/디스코드 동반 안내 의무. |
| 2026-05-25 | ADR-026 신규 (entity id 전역 풀 — 맵 간 id 유지) | M4.2 Phase 02 진입 전 결정 (plan-auditor 🟡 — PDL `S_MapTransition` 필드 모양이 정책 종속이라 선결). 맵 이동 시 player entity id 유지(GameWorld 단일 `Interlocked.Increment` 발급) vs 맵별 재배정(B안) 중 전자 채택. 이유 = 클라 단순(맵 전환 id 교체 0) + M5 영속화/M4.3 cheat 추적 일관성 + 전역 카운터 race는 단일 지점 발급으로 해결(id 발급 ≠ 게임 mutation, Map=Actor 위배 X). 결과 = `S_MapTransition`에 entityId 필드 불포함. 후보 표 ADR-026(헤드리스 봇 자동화)은 미채택 예시였으므로 본 채택이 실제 ADR-026. |
| 2026-05-29 | ADR-028 신규 (Code Convention 수립 — 2 권위서 참고서 + 우리 규칙 + 강제 4중) | M4.3 Phase 07에서 `GameMap` 665줄 God class 발견 + reviewer가 "코드 크기/구조 = 도구 책임"으로 명시 제외해 못 잡음(REVIEW_CHECKLIST 옛 조항) + SOLID는 🎓로만 강제력 0. 사용자 핵심 = "선언만으론 안 지켜짐(신뢰 문제)" + "기반 애매하면 후속 다 부채" → *결정적 규칙 + 자동 강제 메커니즘 + 완성형*. 추상 SOLID 대신 이름 있는 패턴 카탈로그(GPP 19) + 게임서버 교과서(10) 정독(워크플로우 32 agent/118만 토큰) → `00_Document/conventions/` 33파일(refs/CODE_CONVENTION/INDEX 3층). 책 이론(refs) vs 우리 규칙(CODE_CONVENTION) 분리. God class 분리 §2.2 결정 기준 + 과분할 경계(§0.3). 강제 4중(agent 참조 의무 + reviewer 축6 + 줄수 hook + 본 ADR) — ADR-019 "코드 스타일 Scope 제외"를 구조/SRP에 한해 부분 뒤집음(.editorconfig 포매팅은 §4로 M4.4 이월). 발표 Phase 08~12 보류하고 기반 투자(사용자 결정). ※ ADR_History 누락분(023/024/025/027)은 M4.4 하네스 sweep 이월. |
| 2026-06-07 | ADR-029 신규 (SAC dotnet 실행 차단 — WSL2 실행 표준, 로컬 테스트 부활) | SAC가 세션16 dotnet test 차단에 이어 세션17 풀 리빌드 후 서버 실행(fresh Shared.dll 로드)까지 차단 — 차단 대상 비결정("빌드 룰렛") + per-file 예외 부재 + Off 비가역. AskUserQuestion 3안(WSL2/SAC Off/신뢰바이트 도구화) 중 WSL2 표준 채택. PoC 5항목 게이트 통과(빌드 5.68s / 봇 desync 0 / test 392/0/4 = CI 동일 / Windows→wslrelay TCP 검증, Vanguard 간섭 기우 확인). 세션16 "SAC 게이트 은퇴 = CI 단독" 부분 supersede — CI 유지 + 로컬 피드백 부활. 옛 후보 "WDAC 미서명 DLL 차단 정책 정리" 채택분. LocalDB Linux 부재는 M5 영속화 진입 시 결정 명시 이월. |
