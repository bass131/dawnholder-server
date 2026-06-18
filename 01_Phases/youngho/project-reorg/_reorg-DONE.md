---
summary: 전체 repo 문서 정리 — UltraCode 실측 결과 파편화 원인은 폴더위치 아니라 깨진링크+INDEX부재. 깨진링크 ~60건 수정 + 마스터/reviews INDEX 신설 + 미사용(learning-journal 삭제·M4-backlog archive·state 백업) 정리. 게임 코드 0 변경.
phase: project-reorg (M7.5 후속 문서 정리)
status: done
owner: youngho
grade: 복잡
---

# project-reorg — 전체 repo 문서 정리 (마감)

> UltraCode 워크플로우 실측 + plan-auditor GO · 복잡 · 2026-06-19
> 시각화 페어: [`_reorg-DONE.html`](_reorg-DONE.html)

## TL;DR

영호 "이왕 하는 김에 모든 프로젝트 UltraCode 실측해 깔끔하게 정리" 지시 → 7-에이전트 워크플로우(`project-structure-audit`)로 전체 repo 실측. **핵심 발견: "정보 파편화"의 진짜 원인은 폴더 위치가 아니라 (1) 깨진 상대경로 링크 ~60건 + (2) 마스터 INDEX 부재.** 폴더는 이미 카테고리별로 잘 나뉘어 있고, 오히려 *이동하면 frozen 문서(ADR/-DONE/정책)가 참조하던 경로가 깨진다*. 따라서 **"이동 최소화 + 비파괴 최대화"** — 깨진 링크 depth별 일괄 수정 + 마스터/reviews INDEX 신설 + 미사용 정리(learning-journal 삭제·M4-backlog archive·state 백업 제거). **게임/기술 코드 0 변경.**

## AC 검증 결과

완료 조건 = ① 건드린 영역 dangling 0 ② 게임 코드 0 변경 ③ plan-auditor 위반 0 ④ append-only 보존.

| 항목 | 실행 | 결과 |
|---|---|---|
| 게임 코드 0 변경 | `git diff --stat main...HEAD -- 02_Server 03_Client 98_Shared 04_ClientNet` | 빈 출력 = 0 ✅ |
| 01_Phases 미변경 | `git diff --stat -- 01_Phases` (이 -DONE 제외) | 빈 출력 — 잔여 8 dangling은 frozen pre-existing ✅ |
| 건드린 영역 dangling 0 | `check_links` 00_Document·.claude·루트 | BROKEN 0 ✅ |
| plan-auditor 검증 | 워크플로우 audit | **verdict=GO, violations=0** ✅ |
| append-only 보존 | ADR-025 본문 rewrite 0 + 상태줄 1줄 append | ✅ |
| settings/hook 무변경 | 권한·강제 층 미터치 | trust-boundary 무관 ✅ |

**잔여 (범위 밖)**: `01_Phases/` frozen `-DONE.md` 8개의 깨진 링크(옛 `New_Harness/` 경로·삭제된 `journal/phase.md`·산문 오인). pre-existing이고 append-only라 미수정 — 동결 역사 보존.

## 결정 흐름 (영호 게이트)

- **스코프 전환**: 영호 처음 "넓게(폴더 이동)" 선택 → 실측 결과 루트 코어 이동 = **92참조 + 헌법 본문 4곳** 수정 + frozen 충돌 → **비파괴로 전환**(이동 11후보 중 안전한 건 gitignore 백업 4건뿐, refChurn 0).
- **learning-journal**: 영호 "날리자" → **삭제**. ADR-025가 명시한 "보존" 결정을 철회하는 것이라 ADR-025 **상태줄에 append-only로 철회 기록**(본문 33·39줄 '보존'은 당시 결정 역사로 보존).
- **M4-backlog**: 삭제 아닌 **archive 이동** (기술부채 스냅샷 보존).
- **reviews `.tmp` 3개 + state 백업/로그/씬백업**: 쓰레기 → 삭제.

## 학습 일지 후보 키워드

- `fragmentation-is-links-not-folders` — "정보가 흩어졌다" 체감의 원인이 폴더 배치가 아니라 깨진 링크 + 길찾기(INDEX) 부재였던 진단. 이동 충동을 실측이 기각.
- `frozen-reference-blocks-move` — non-frozen 파일이어도 frozen 문서(ADR/-DONE/정책)가 그 경로를 참조하면 사실상 이동 불가 (frozen은 못 고쳐 dangling 발생). 이동 가능성 = 대상이 아니라 *참조자*가 결정.
- `ultracode-measure-before-reorg` — 7 에이전트 병렬 실측 + plan-auditor 검증이 "92참조+헌법본문" 비용을 숫자로 드러내 헛된 대공사를 막음. 감 아닌 실측이 스코프를 정함.
- `non-destructive-over-move` — 발견성 문제는 폴더 이동(고비용·위험)보다 제자리 INDEX/배너(refChurn 0)로 더 싸고 안전하게 해결.
- `append-only-decision-reversal` — 과거 ADR 결정(보존)을 뒤집을 때 본문 rewrite 0, 상태줄 한 줄로 "결정이 바뀌었다" 기록. 역사 왜곡 없이 현재 상태 정합.

## 5단계 보고

### 🎯 무엇을 만들었나
흩어진 문서 길찾기를 복구. `.claude/`의 깨진 상대링크 ~60건을 depth별로 일괄 수정하고, `00_Document/INDEX.md` 마스터 네비게이션 + `reviews/INDEX.md`(31개 분류) + `archive/README.md`를 신설. 미사용 자산(learning-journal/ 16파일 삭제, M4-backlog.md → archive/, `.claude/state` stale 백업·로그·씬백업, reviews `.tmp` 3개)을 정리. ADR-025에 learning-journal 보존 철회를 상태줄로 박음.

### 🤔 왜 필요한가
영호가 "정보가 파편화·분리돼 실측이 불편할 정도"라 호소. 직관은 "폴더를 옮겨 깔끔하게"였으나, UltraCode 실측이 **진짜 병목은 위치가 아니라 깨진 링크 + 마스터 목차 부재**임을 드러냄. 이동은 frozen 역사 참조를 깨뜨려 오히려 손해.

### 🛠️ 어떻게 만들었나
- **실측 우선** — 7 에이전트 워크플로우(00_Document·01_Phases·.claude·루트·참조그래프 5영역 병렬) → 정리안 설계 → plan-auditor 검증(GO).
- **비파괴 전략** — 이동 11후보 중 안전(gitignore·refChurn 0)한 4건만, 나머지는 제자리 + INDEX/배너.
- **depth별 링크 수정** — `agents/commands`(`../../`)·`knowledge 하위`(`../../../`) 깊이를 구분, 수정 후 `ls`-resolve 전수 검증(단일 find/replace 금지 — audit 조건).
- **append-only** — ADR-025 본문 무변경, 상태줄 1줄.
- **안 한 것** — 루트 코어 파일 이동(헌법 앵커, 92참조)·yuhyeon frontmatter·slug 오타(frozen)·게임 .cs(SOLID 별건).

### 🧪 테스트 결과
게임 코드 git diff **0** / 01_Phases 미변경 / 건드린 영역 dangling **0** / plan-auditor **GO·위반 0** / append-only 보존. (docs-only라 WSL2 게임 회귀 불요.)

### ➡️ 다음 스텝
- **PR 생성·머지 = 영호 GO** (비가역). CODEOWNERS = `00_Document`·`.claude`만 → @bass131 단독 추정 → normal merge.
- **post-reorg 후보**: 01_Phases frozen dangling(옛 경로) 정리는 append-only 완화 결정 선행 시 / 팀 전제 하네스(CODEOWNERS·co-review·아트 트랙) 정리.

## commits

| commit | 내용 |
|---|---|
| `1ba27d5` | (dogfood) 00_Document/policies 깨진 링크 20→0 |
| `913b05d` | 전체 repo 정리 — 링크 ~60 + 마스터 INDEX + 미사용 정리 |
| (이번 commit) | `_reorg-DONE.md` + `_reorg-DONE.html` 박제 |
