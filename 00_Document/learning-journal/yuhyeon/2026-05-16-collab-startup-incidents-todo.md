# 학습 대기: 협업 셋업 초기 두 사건 (2026-05-16)

> **작성일**: 2026-05-16
> **work-id**: (셋업 단계, WORK-ID 미할당)
> **상태**: 🟡 stub — 사건 발생/해결까지만 박제. 본격 학습 회고는 나중에 `/journal:bug`로 확장.
> **소요 시간 추정 (학습)**: 사건당 30~45분

본 파일은 "나중에 깊이 학습할 사건 목록"입니다. 손에 따끈할 때 키워드·링크만 박아두고, 후일 시간 잡고 트러블슈팅 일지(`_template-troubleshoot.md`)로 확장합니다.

---

## 사건 1 — Unity Cloud 식별자 자동 덮어쓰기

**증상 (한 줄)**: 어제 Unity Hub로 프로젝트 열었더니 `ProjectSettings.asset`이 단독 M으로 떴는데, diff가 `cloudProjectId` / `organizationId` 두 줄뿐.

**잘못된 첫 추측**: "ProjectVersion.txt와 같은 결의 환경 파일이니 .gitignore에 박자" — 팀장이 즉시 막음. 그 라인은 Unity AI 토큰 분리와 묶여있어서 단순 ignore로 풀 수 없음.

**진단**: Unity Hub가 각자 계정의 Cloud Project ID를 자동 주입. 팀원마다 토큰 다름 → 매 세션 충돌 위험.

**해결**: 팀장이 PR #6 ((B+) 게이트) — `/session:start`가 "ProjectSettings.asset 단독 + cloud 라인만 변경" 조건을 정확히 감지하면 `git checkout`으로 자동 정리. 그 외 변경은 (C-2)/(C-3)으로 빠져 사용자 결정.

**관련 PR**: https://github.com/bass131/dawnholder-server/pull/6

**학습 후보 키워드** (검색용):
- Unity Cloud Project ID, organizationId
- `.gitignore` vs 게이트 자동 정리 — 언제 어느 쪽?
- 안전 게이트 설계의 (B+) 정책: "단독 + 특정 라인만" 정확 매칭 후에만 자동 행동
- 환경 차이 함정: "내 환경에 안 떴다 = 다른 사람 환경에도 안 뜬다" *아님*

**STAR 박제 후보 (면접 무기)**:
- S: 협업 첫 PR 받는데 ProjectSettings.asset 충돌 위험 발견
- T: 매 세션 같은 잡음 안 뜨게 영구 해결
- A: `.gitignore` 추천 → 팀장 거부 → 게이트 자동 정리로 선회
- R: PR #6 머지, 게이트 동작. (C-1) 시연 경로 B로 직접 트리거 → 자동 정리 메시지 정상 출력 확인 (2026-05-16 세션 마감 직전)

---

## 사건 2 — Git autocrlf phantom diff

**증상 (한 줄)**: Unity 6.4.7f1 업그레이드 후 `EditorBuildSettings.asset` + `ShaderGraphSettings.asset`이 M으로 떴는데, `git diff`는 비어있고 "LF will be replaced by CRLF" 경고만.

**진단 (팀장)**:
- Git for Windows 기본값 `core.autocrlf=true` (체크아웃 시 LF→CRLF, 커밋 시 CRLF→LF)
- 우리 레포에 `.gitattributes` 부재 → OS·git config 차이가 곧 phantom diff
- Unity가 같은 내용 다시 저장 → Git stat이 touch 감지 → autocrlf가 phantom diff 트리거
- 실제 byte 변경 0

**해결**: PR #8 — Unity 표준 `.gitattributes` 박음. 161줄, 레포 차원 EOL 강제. OS/개인 config 무관하게 모두 동일 규칙.

**관련 PR**: https://github.com/bass131/dawnholder-server/pull/8

**학습 후보 키워드**:
- `core.autocrlf` (true / false / input 차이)
- `.gitattributes` 구조: `* text=auto eol=lf`, `*.cs text eol=lf` 등
- LF / CRLF / EOL 정책
- Unity 표준 `.gitattributes` (Unity Asset Store / GitHub 검색)
- diff 0인데 M 뜸 — 진단 사고법

**STAR 박제 후보**:
- S: Unity 업그레이드 후 두 .asset 파일이 가짜 변경 (phantom diff)으로 떠 협업 흐름 막힘
- T: 본인뿐 아니라 다음 합류자(인규)도 같은 함정 안 만나게 영구 차단
- A: 본인은 진단 못 함 → 팀장 진단 받아 `.gitattributes` 정책 학습 → PR #9로 첫 PR (lscache .gitignore) 후속 박음
- R: PR #8/#9 머지, working tree clean. 향후 OS 섞인 팀 환경에서도 동일 함정 안 폭발

---

## 메모리 저장 결정 대기 (작업 핀에서 이월)

본격 학습 후 결정할 두 메모리 후보:

1. **(이미 핀에 박힘)** "ProjectVersion.txt .gitignore 잘못 추천 사건" → `feedback` 메모리
   - 내용 요지: Unity 버전·환경 파일은 .gitignore 회피 추천 금지. 진짜 원인(설치 revision 불일치 등)부터 찾기.

2. **✅ 저장 완료 (2026-05-17)** "환경 차이로 보이는 git stat 변경은 본인 환경에 안 떠도 다른 사람 환경에서 폭발" → `feedback_env_vs_repo_fixes.md`
   - 내용 요지: phantom diff·캐시·OS별 EOL 등은 *레포 차원*(`.gitattributes`, `.gitignore`) 해결이 *개인 환경* 해결보다 우선.

---

## 향후 작업

- [ ] 사건 1 → `_template-troubleshoot.md`로 확장 (`/journal:bug`)
- [ ] 사건 2 → 같은 방식으로 확장
- [x] 두 메모리 후보 박을지 결정 후 AI에게 위임 (1번 이미 박힘 / 2번 2026-05-17 박힘)
- [ ] (선택) ADR/policies 문서에 학습 내용 일반화할 부분 있는지 검토

확장 시 본 파일은 "index" 역할로 남겨두고, 본문은 새 troubleshoot 파일로.
