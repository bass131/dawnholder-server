# CHANGELOG — 하네스 변경 이력

> **이 파일의 역할**: 팀장(유영호)이 헌법/ADR/하네스/공유 파일을 변경할 때마다
> 한 줄씩 박제. 팀원이 매일 작업 시작 시 `/session:start`가 이 파일의 최근 줄을
> 자동으로 보여줘서 본인이 옛 결정 기반으로 작업하는 사고 예방.
>
> **갱신 주체**: 팀장(유영호) 단독. 팀원은 읽기만.
>
> **갱신 규칙**:
> - 변경 commit 직후 한 줄 추가
> - 형식: `YYYY-MM-DD — 한 줄 요약 (영향 범위 / 위험도)`
> - 위험도 표기: `[L]` (저위험, 추가만) / `[M]` (중간, 행동 변경) / `[H]` (고위험, 결정 뒤집기)
> - 슬랙(또는 디스코드)에도 같은 한 줄 알림 박기

---

## 갱신 룰 짧게

**저위험 [L]** — 새 슬래시 커맨드, 새 서브에이전트, 새 ADR 박제 (기존 결정 변경 X), 학습 일지 템플릿 보강
**중간 [M]** — 슬래시 커맨드 동작 변경, 기존 ADR 사후 보강, 헌법 일부 섹션 추가
**고위험 [H]** — 헌법 절대 원칙 수정, 기존 ADR 뒤집기 (PostgreSQL→MSSQL 같은 결정 정정), 파일 권한 분리, 영역 분리 변경

[H]는 팀장이 슬랙/디스코드에 추가 안내 + 팀원 작업 일시 중단 요청 권장.

---

## 이력 (최신이 위)

| 날짜 | 변경 | 위험도 |
|------|------|--------|
| 2026-05-15 | ADR-019 박제: Reviewer 서브에이전트 도입 (Tier 2 자동 리뷰). 신규 `.claude/agents/reviewer.md` + `00_Document/REVIEW_CHECKLIST.md` (5축 점검 매핑) + 헌법 `## Tier 2 자동 리뷰` 섹션 추가. 도메인 에이전트 코드 변경 후 메인 세션이 트리거 조건(새 핸들러/패킷/공식/≥10줄/`98_Shared/` 포함) 충족 시 reviewer 자동 호출. 코드 스타일 검증은 의도적 Scope 제외 (Roslyn analyzer 도입 후보로 미루). 서브에이전트 6→7. | [M] |
| 2026-05-14 | `/session:end` 슬래시 커맨드 신설 (Phase 마감 절차 8단계) + `/session:log` Codex 분기 추가 (Codex 없는 팀원은 Claude 단독 박제) + 헌법 Phase 완료 권유 섹션 통합 (15 → 16 슬래시) + inject-current-pin.sh 훅 확장 (commit 안 된 -DONE.md 경고 주입) | [M] |
| 2026-05-14 | 협업 셋업 인프라 박제: `/setup` 슬래시 커맨드 + setup-steps 5개 + CHANGELOG 시스템 + CODEOWNERS + `/session:start` CHANGELOG 자동 확인 | [M] |
| 2026-05-14 | `.vscode/settings.json` 화이트리스트로 공유 (통합 터미널 Git Bash + 자동 저장 + LF + 저장 시 포맷) | [M] |
| 2026-05-12 | ADR-005 v2: PostgreSQL → MSSQL LocalDB + Windows 통합 인증 정정 | [H] |
| 2026-05-14 | ADR-020 박제: 훅 실행 환경 의존성 (Git Bash + PATH 셋업 필수) | [L] |
| 2026-05-14 | 협업 베이스라인 박제: CONTEXT 시스템 각자 보유 (.gitignore) + learning-journal 사람별 네임스페이스 + `.vscode/extensions.json` 화이트리스트 | [M] |

---

## 이력 외부화 정책

220줄 넘으면 `CHANGELOG_History.md`로 분리 (헌법 ADR-014 — 문서 세분화 정책).
또는 분기별로 자연 분리: `CHANGELOG_2026Q2.md` 등.
