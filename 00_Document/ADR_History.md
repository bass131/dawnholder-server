# ADR 변경 이력

> [`ADR.md`](ADR.md)의 변경 이력 외부화 (헌법: 문서 세분화 정책 — 누적 섹션 외부화).
>
> 새 ADR 추가/갱신 시 본 파일에 한 줄씩 추가. ADR.md 본문은 *현재 결정*만 담음.

| 날짜 | 변경 | 이유 |
|------|------|------|
| (Harness 셋업일) | 최초 작성 + ADR-001~005 시드 | Harness 셋업에서 결정한 것들 박제 |
| (PRD 1차 작성일) | ADR-006~009 추가 | PRD 1차 작성 과정에서 결정된 것들 박제 |
| 2026-05-06 | ADR-001 갱신 (.NET 8 → .NET 10 LTS + .NET Standard 2.1) | .NET 9 STS 만료 임박, .NET 8 LTS도 본 마감 직전 만료. .NET 10 LTS가 시연 후 시점도 커버. shared/는 Unity 호환 위해 .NET Standard 2.1. |
| 2026-05-06 | ADR-002 갱신 (MessagePack → 자체 PDL + 코드 생성기) | 본인이 4월에 작성한 PDL 시스템 채택. 면접 임팩트 + 학습 가치. |
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
| 2026-05-11 | 변경 이력 외부화 (ADR.md → ADR_History.md) | ADR.md 220줄 임계 대응. CONTEXT_History 패턴 동일. |
