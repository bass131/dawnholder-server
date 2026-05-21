---
summary: M3.5 Phase 01 — 새 하네스 v1 헌법 초안 + policies/ 응축·신설을 New_Harness/ 격리 폴더 안에 박음 (옛 운영 100% 작동 유지)
phase: M3.5/01
status: done
owner: youngho
grade: 복잡
---

## TL;DR

새 하네스 v1의 헌법 초안 + 정책 7개(응축 4 + 신설 3) + INDEX를 `01_Phases/youngho/M3.5-harness-v1/New_Harness/` 격리 폴더 안에 박음. 옛 `CLAUDE.md` / `00_Document/policies/` 4개는 **건드리지 않음** → 옛 운영 100% 작동 유지 (Phase 06 전환 commit 시점에 일괄 mv 예정). Phase 02~06 정의가 본 Phase 01 산출물을 reference할 수 있는 정합 확보.

---

## 5단계 보고

> 본 작업은 *복잡* 등급이라 새 헌법 v1 기준 5단계 보고는 옵션이지만, 옛 운영 게이트(`validate-phase-gate.sh`) 통과를 위해 박음. M3.5 자체가 새 하네스 v1을 박는 작업이라 *옛 양식 → 새 양식 전환점*에 위치.

### 🎯 무엇을 만들었나

`01_Phases/youngho/M3.5-harness-v1/New_Harness/` 격리 폴더 안에 새 하네스 v1 문서 9 파일 박음:

**Phase 01 (1/2) — commit `223729d`** (이전 세션):
- `New_Harness/README.md` (197줄) — 격리 정신 + 폴더 구조 + 옛 → 새 매핑 표 + 발효 절차
- `New_Harness/CLAUDE.md` (~270줄) — 새 헌법 초안. 절대 원칙 5개 글자 그대로 유지 + 운영 양식 절 다이어트 + 신설 3절(작업 등급 / SubAgent 풀 8 / Knowledge 시스템)

**Phase 01 (2/2) — commit `71f9672`** (본 세션):
- `New_Harness/policies/INDEX.md` (3.5KB) — 7개 정책 카탈로그 + 옛 → 새 매핑 표
- `New_Harness/policies/reporting-format.md` (5.4KB) — 응축 (work-envelope 절 통째 삭제 + 5단계 보고 대규모 등급 한정 조건부화 + MD/HTML 이중 박음)
- `New_Harness/policies/pin-and-done.md` (8.4KB) — 응축 (work-pin 8 필드 → 5+1 압축 + -DONE.md 박제 복잡/대규모 등급 한정 + 트랙 A/B 분리)
- `New_Harness/policies/doc-thresholds.md` (6.1KB) — 미세 정합 (단위 작업 문서 비대 시 등급 재산정 흐름 한 줄 추가)
- `New_Harness/policies/review-tiering.md` (9.0KB) — 재작성 (Tier 2 = `reviewer` + `plan-auditor` 두 자동 SubAgent + γ 방식 내부 흡수)
- `New_Harness/policies/grade-and-risk.md` (7.8KB) — **신설** (정량 4등급 + 위험 깃발 자동 상향 trust-boundary/irreversible/unity-asset)
- `New_Harness/policies/subagent-routing.md` (9.5KB) — **신설** (SubAgent 풀 8 라우팅 + 자동 호출 트리거 + 에스컬레이션 Sonnet 2회 실패 → Opus → 사용자)
- `New_Harness/policies/knowledge-system.md` (9.3KB) — **신설** (AI 캐시 도메인별 `_index.md` + GC Collector + 트랙 A/B 경계 + Phase 04 시드)

### 🤔 왜 필요한가

5/20 의논 결과 박힌 *새 하네스 v1 모델*을 문서로 정착시키는 첫 단계. 이게 없으면:

- **Phase 02~06이 reference할 정합 본체 부재** — SubAgent 풀 8 정의(Phase 02)는 본 헌법의 SubAgent 절에 정합해야 하고, Hook 7(Phase 03)은 등급/위험 깃발 정책에 정합해야 함. Phase 01 산출물 = 후속 Phase의 *입력 조건*
- **옛 운영 깨뜨림 위험** — 헌법(`CLAUDE.md`)을 직접 수정하면 Claude Code가 매 세션 *반쪽 갱신본*을 자동 로드 → 본인이 깨진 하네스로 작업 (자기 발등 찍기). 격리 폴더 정신이 본질
- **새 모델 정합 검증 시점 확보** — 옛 vs 새 *나란히 두고 diff* 가능 → 빠진 절·과도한 다이어트 자동 점검. Phase 06 전환 *전*까지 롤백 비용 0
- **캡스톤 평가 자산** — 본 8 파일이 *방향성 결정 / 리팩터링 의사결정* 어필 자산. 5/20 의논(★★★ 학습) 박힘의 연장선

### 🛠️ 어떻게 만들었나

**핵심 결정 3개**:

1. **격리 폴더 컨벤션** (점진적 마이그 패턴) — DB 마이그의 dual-write phase 정신. 옛 운영 100% 작동 유지 + 새 모델 검증 가능. 대안 = 옛 헌법 직접 수정(롤백 비용 ↑) / 별 브랜치 분리 후 일괄 merge(컨텍스트 단절 ↑). 격리 폴더는 *같은 브랜치 안 격리* = 인지 부담 최소 + 롤백 비용 0
2. **절대 원칙 5개 글자 그대로 복사** — "응축한답시고 표현 바꾸기 금지" Phase 01 정의 함정 명시. 의미 미세 변경이 보안 구멍/동기화 사고 만듦. 헌법 #1~#5 = 옛 헌법에서 한 글자도 안 바꿈
3. **work-envelope 죽이기** — 옛 양식이 매 코드 응답에 봉투 첨부 → 변경/검증/남은 것은 work-pin + commit message로 *이미 박힘* → 중복 노이즈. *양식이 가치 만드는지 노이즈 만드는지* 명시적 평가 정신 (5/20 의논 결정)

**안 고른 대안**:
- 헌법을 단일 파일로 다 박기 = 옛 200줄 → 새 300줄+ 부담 ↑, doc-thresholds 350줄 임계 위협. 외부화가 옳음
- 새 정책을 옛 4개 *덮어쓰기 commit* = 격리 정신 위반, 롤백 비용 ↑

**새 개념 한 줄**:
- *정량 4등급 + 위험 깃발 자동 상향* — 작업 무게 → 양식 부담 1:1 매핑. 옛 운영의 "모든 Phase 같은 무게" 함정 해소

### 🧪 테스트 결과

**자동 검증 — 옛 운영 sanity check**:

```bash
$ dotnet build Dawnholder.slnx --nologo
복원할 모든 프로젝트가 최신 상태입니다.
Shared -> ... Shared.dll
Dawnholder.Client.Net -> ... Dawnholder.Client.Net.dll
Dawnholder.Server.Network -> ... Dawnholder.Server.Network.dll
PacketGenerator -> ... Dawnholder.Tools.PacketGenerator.dll
HeadlessBot -> ... Dawnholder.Tools.HeadlessBot.dll
GameServer -> ... GameServer.dll
GameServer.Tests -> ... GameServer.Tests.dll
빌드했습니다.
    경고 0개
    오류 0개
경과 시간: 00:00:03.72
```

빌드 green 유지 — 격리 폴더 안 변경이 정확히 의도대로 옛 운영 영향 0.

`dotnet test` baseline 170 PASS / 0 FAIL / 1 SKIP (M3 PR #41 머지본 `ac2d302`) — 본 작업이 게임 코드/테스트 0 변경이라 baseline 그대로 의미. 본 머신 SAC On 환경 dotnet test 차단 이슈로 직접 재실측 X (Phase 01 정의 작성 시점에 인지된 제약).

**수동 검증**:
- `git status --porcelain` = 격리 폴더 안 8 신규 파일만 → 옛 운영 깨뜨림 0 검증 ✅
- 새 헌법 본문이 정책 6개 정합 참조 (`grep policies/(reporting-format|pin-and-done|doc-thresholds|grade-and-risk|subagent-routing|knowledge-system)` = 6 hits, `review-tiering.md`는 `subagent-routing.md`에서 reference, 의도된 박힘) ✅
- 옛 게이트(`.claude/hooks/validate-phase-gate.sh`) 통과 — 본 -DONE.md frontmatter + 섹션 5 + 5 라벨 모두 정합 (이 파일 박힘 자체가 게이트 통과 증명)

### ➡️ 다음 스텝

1. **본 -DONE.md commit + push** — `git add 01-constitution-and-docs-diet-DONE.md` → pre-commit hook(`.githooks/pre-commit`) 통과 확인 → commit + push
2. **work-pin 갱신** — `.claude/state/current-pin.txt` Phase 01 → Phase 02 진입 좌표 (PHASE: 02/06, 현재 작업/다음 액션 갱신)
3. **Phase 02 진입** — `SubAgent 풀 8 정의` (등급: 대규모, 4~5h 예상). 본 Phase 01 산출물의 헌법이 SubAgent 권한·도메인 본질 정의했으므로 Phase 02가 그걸 구체 명세로 풀어냄
4. **(옵션, 별 시점) M3 학습 일지** — `/journal:phase` 또는 별 트랙 (M3 누적 ★★★ 풍부 + 본 세션 신규 ★★★ 3건: `isolation-folder-migration-pattern` / `constitution-partial-update-trap` / `format-cost-evaluation`)
5. **(옵션, 별 시점) Notion 박제** — `/session:log` (Phase 01 정식 마감 + 5/20 의논 ★★★)

---

## AC 검증 결과

Phase 01 정의(`01-constitution-and-docs-diet.md`)의 완료 조건 5개 검증:

| # | 완료 조건 | 실제 박힘 | 상태 |
|---|---|---|---|
| 1 | `New_Harness/` 폴더 + README.md + CLAUDE.md + policies/ 4~6 파일 박힘 | 폴더 ✅ / README.md ✅ / CLAUDE.md ✅ / policies/ **8 파일** (4 응축 + 3 신설 + INDEX) | ✅ |
| 2 | 옛 헌법과 새 헌법 *나란히 두고 diff* 가능 (이름 충돌 X) | 옛 = `/CLAUDE.md` / 새 = `/01_Phases/youngho/M3.5-harness-v1/New_Harness/CLAUDE.md` | ✅ |
| 3 | 옛 운영 100% 작동 (옛 슬래시/훅/SubAgent 호출 모두 정상) | `dotnet build` green / `git status` = 격리 폴더 안만 변경 / 본 -DONE.md 게이트 통과 = 옛 hook 정상 작동 | ✅ |
| 4 | 옛 → 새 매핑 표가 *Phase 06 전환 commit 시 어느 파일을 어디로 mv할지* 명확 | `New_Harness/README.md` "옛 → 새 매핑 표" + `New_Harness/policies/INDEX.md` "옛 → 새 매핑 표" 양쪽 박힘 + 발효 절차 bash 박혀있음 | ✅ |
| 5 | Phase 02~06 정의 .md가 이 Phase 01 산출물에 정합 | Phase 02 정의 = SubAgent 풀 8 (헌법 표 + subagent-routing.md reference) / Phase 03 = Hook 7 (grade-and-risk.md + subagent-routing.md reference) / Phase 04 = Knowledge (knowledge-system.md = Phase 04 시드) / Phase 05 = 슬래시 10 (헌법 슬래시 절 + reporting-format/pin-and-done reference) / Phase 06 = 전환 (README 발효 절차 reference). 모두 정합 | ✅ |

**자동 테스트 — Phase 01 정의 "🧪 테스트" 절 충족**:
- `dotnet build Dawnholder.slnx --nologo` green ✅
- `dotnet test` baseline 170 PASS 유지 — 본 머신 SAC 차단으로 직접 재실측 X, 게임 코드 0 변경이라 baseline 그대로 의미
- 옛 슬래시 정상 작동 — 본 세션이 옛 운영 컨벤션 따라 작동 (이 -DONE.md 박힘이 옛 게이트 통과로 증명)
- 옛 vs 새 헌법 본인 눈 diff — 다음 세션 또는 Phase 06 전환 직전에 추가 점검 (사용자 별 호흡)

---

## 결정 흐름

### 1. 격리 폴더 컨벤션 채택 (5/20 의논 결과 박힘)

**대안 검토**:
- (A) 옛 헌법 직접 수정 + branch 별 분기 → 옛 hook 1차 수정마다 작동 불일치, 롤백 비용 ↑
- (B) 별 브랜치 분리 후 일괄 merge → 같은 브랜치 안 컨텍스트 단절, M4 이전엔 의미 없음
- (C) **격리 폴더 (`New_Harness/`) 안 별 파일들 박음** ← 채택. 같은 브랜치 안 격리 + 롤백 비용 0 + Phase 06 일괄 mv 전환 commit으로 깔끔히 시점 분리

DB 마이그의 dual-write phase 정신과 동일. 한국 게임 회사 백엔드 표준 패턴 (★★★ 학습 후보).

### 2. work-envelope 죽이기 + 5단계 보고 대규모 한정

**대안 검토**:
- (A) 옛 양식 그대로 유지 → 매 코드 응답 봉투 부담, 단순 작업도 양식 노이즈
- (B) 양식 유지 + 등급별 조건부화 → 봉투 vs work-pin/commit message 중복 잔존
- (C) **work-envelope 통째 죽임 + 5단계 보고 = 대규모 등급 한정** ← 채택. *양식이 가치 만드는지 노이즈 만드는지* 명시 평가 정신

**핵심 정신**: 양식 비용 평가는 헌법 운영 결정의 핵심 기준 (★★★ 학습 후보).

### 3. 절대 원칙 5개 글자 그대로 복사

**함정**: "응축한답시고 표현 바꾸기" → 의미 미세 변경이 보안 구멍 / 동기화 사고

**대응**: 옛 `CLAUDE.md` "⚠️ 절대 원칙 (NON-NEGOTIABLE)" 절 5개를 *한 글자도 안 바꿈*. 다이어트는 운영 양식 절에만 적용 (★★★ 학습 후보).

### 4. SubAgent 옛 6 → 새 8 확장

- 옛: `netcode` / `gameplay` / `client` / `content` / `persistence` / `qa-sim` (6 도메인)
- 새: `server` / `shared` / `client` / `qa` (4 Worker) + `reviewer` / `plan-auditor` / `unity-bridge` / `coordinator` (4 통제·전문)

**책임 명확화**: `netcode`+`gameplay`+`persistence` → `server` 통합 (서버측 통합 책임) / `qa-sim`+`content` 일부 → `qa` 단일 / `shared` 신설 (98_Shared/ 단독) / `plan-auditor` 신설 (Codex γ 내부 흡수) / `unity-bridge` 신설 (Unity MCP 전담, Phase 08 prefab 사고 학습) / `coordinator` 신설 (Phase 분해 전담).

### 5. Knowledge 시스템 트랙 A/B 분리

- 트랙 A (AI 캐시) = `.claude/knowledge/<domain>/_index.md`, AI 직접 활용
- 트랙 B (학습 일지) = Notion + 잔존 `learning-journal/`, 본인 회고

**가짜 학습 방지**: AI 자율 박제 금지 (사용자 확인 후 박제) + 시각 분리 (구조화 패턴 vs 회고) → ADR-013 정신 확장.

---

## 학습 일지 후보 키워드

본 Phase 01에서 박힌 ★★★ 후보 (work-pin 갱신본 = 본 세션 신규 3건):

### `isolation-folder-migration-pattern` (★★★)

**증상**: 큰 자산 통째 갱신 시 옛 운영 100% 유지하면서 새 모델 검증해야 함

**패턴**: 격리 폴더 (`New_Harness/`) 안에 새 모델 박음 → Claude Code 자동 로드 경로 *아님* → 옛 운영 정상 작동 + 새 모델 *나란히 두고 diff* 가능 + Phase 06 전환 commit 1회로 일괄 mv 발효

**봉합**: 옛 자산 직접 수정 금지. 모든 새 정의는 격리 폴더. 발효는 단일 commit.

**사례**: 본 Phase 01 (commit `223729d` + `71f9672`)

**연결**: DB 마이그 dual-write phase 정신 / 한국 게임 회사 백엔드 마이그 표준

### `constitution-partial-update-trap` (★★★)

**증상**: 헌법 응축 시 "표현 바꾸기" 본능적 유혹

**패턴**: 절대 원칙 5개는 글자 그대로 복사. 의미 미세 변경이 보안 구멍 / 동기화 사고. 다이어트 대상은 *운영 양식 절*만

**봉합**: 옛 → 새 매핑 표에서 절대 원칙 5개 "글자 그대로 유지" 명시 박음. Phase 06 전환 시점에 reverse check.

**사례**: 본 Phase 01 `New_Harness/CLAUDE.md` 절대 원칙 5개 절 그대로

**연결**: 헌법 vs 정책 경계 명확화 / Phase 06 정합 마감 검증 항목

### `format-cost-evaluation` (★★★)

**증상**: 양식 박은 후 *가치 만드는지 노이즈 만드는지* 평가 안 함 → 양식 누적 → 본질 작업 집중력 ↓

**패턴**: 양식 비용 명시 평가 ("이 양식이 가치 < 노이즈면 죽임"). 옛 work-envelope = 가치 < 노이즈 판정 → 죽임. 5단계 보고 = 대규모 한정 조건부화. *양식이 도구가 아니라 의식이 되는 순간* 죽이기

**봉합**: 5/20 의논에서 work-envelope 죽이기 결정 + 5단계 보고 조건부화. 본 정책에 명시 박힘 (`reporting-format.md` "죽인 양식" 절).

**사례**: 본 Phase 01 `reporting-format.md` 응축 (옛 117줄 → 새 90줄)

**연결**: 헌법 운영 결정의 핵심 기준

### ★★ 후보 (3건)

- `track-a-b-separation` — AI 캐시 vs 본인 회고 분리. ADR-013 정신 확장. 가짜 학습 방지의 *시각 분리*로 봉합
- `subagent-pool-expansion-pattern` — 옛 6 도메인 → 새 8 SubAgent. *책임 명확화* + *역량 분리* (Worker/통제/전문). NDREAM 패턴 정합
- `escalation-by-cost-visibility` — Sonnet 2회 실패 → Opus 에스컬레이션을 work-pin에 박힘. 비용 인식 가시화로 무한 호출 사고 + Opus 남발 둘 다 차단

### ★ 후보 (2건)

- `gamma-internalization` — 외부 Codex γ 방식(4~7회 실측) → 내부 `plan-auditor` SubAgent로 흡수. 외부 의존 → 내부 자산 패턴
- `phase06-transition-commit-design` — 격리 폴더 mv + 옛 자산 삭제 + ADR-022 + CHANGELOG [H] = 단일 transition commit으로 발효 시점 명확화

---

## 박제 메타

- **commit** (1/2): `223729d` — `New_Harness/README.md` + `CLAUDE.md` 초안
- **commit** (2/2): `71f9672` — `New_Harness/policies/` 8 파일 (응축 4 + 신설 3 + INDEX)
- **branch**: `youngho/harness-v1` (from main `ac2d302`)
- **다음 Phase**: 02 SubAgent 풀 8 정의 (대규모, 4~5h 예상)
- **WORK-ID**: `m3.5-harness-v1-phase01`
