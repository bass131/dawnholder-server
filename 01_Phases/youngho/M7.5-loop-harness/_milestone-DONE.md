---
summary: 하네스를 loop-driven 운영모드로 격상 (사람=방향+판단, 엔진=내장 /loop·Workflow + /engine:goal). ADR-032 6 Phase 구현 sweep, 게임 코드 0 변경. v1 attended adopt / v2 무인 defer.
phase: _milestone (M7.5 — 6 Phase 마감)
status: done
owner: youngho
grade: 대규모
---

# M7.5 — Loop-driven 하네스 격상 (마일스톤 마감)

> ADR-032(loop-driven 운영 모드) 구현 sweep · 6 Phase · 대규모 · 2026-06-18
> 시각화 페어: [`_milestone-DONE.html`](_milestone-DONE.html) (캡스톤 평가 자산)

## TL;DR

하네스를 **"사람이 매 스텝 프롬프트" → "사람=방향+판단, 엔진=구동"**으로 격상 (Addy "Loop Engineering"). 실측 결과 우리 하네스는 **"loop-ready인데 loop-less"** — done 판사(WSL2 게이트)·정지 게이트·maker-checker·메모리·refactor-sweep 半구현이 이미 다 있고, **범용 드라이버 + "무엇을 루프에 맡기나" 분류**만 빠져 있었다. 이 둘을 채웠다. 엔진은 새로 만들지 않고 **내장 `/loop`·`Workflow` 재사용** + 어긋나는 핵심(외부 done 심판)만 `/engine:goal` 신규. **v1(attended)만 adopt**, v2 무인은 별도 ADR로 defer. **게임/기술 코드 0 변경** (문서·하네스 only).

## AC 검증 결과

마일스톤 완료 조건 = ① dangling 참조 0 ② hook 정합 smoke ③ reviewer 🔴 0 ④ 게임 코드 git diff 0 (게임 WSL2 회귀 불요 — docs only).

| 항목 | 실행 | 결과 |
|---|---|---|
| 게임 코드 0 변경 | `git diff --stat main...HEAD -- 02_Server 03_Client 98_Shared 04_ClientNet` | **빈 출력 = 0 변경** ✅ |
| settings ask(pr) 보존 (P05 trust-boundary) | `git diff .claude/settings.json` → ask 블록 grep | `gh pr create/merge/--admin` 3매처 불변, allow에 git add/commit만, deny 불변 ✅ |
| 신규 링크 dangling 0 | 마크다운 링크 타깃 존재 전수 grep (P01~P06 신규/편집) | 추가 링크 전부 resolve ✅ (기존 systemic `../policies/` 깨진 링크는 P05 reviewer가 scope 밖 확정 → post-M7.5 문서정리 이월) |
| hook smoke | `bash -n circuit-breaker.sh` + `python -c json.load(settings.json)` | 문법 OK + JSON 유효 ✅ |
| ADR-032 등록 정합 | `ls ADR-032*` + INDEX/ADR.md/ADR_History grep | 파일 존재 + 3종 카탈로그 모순 0, harness 17개 번호 실측 일치 ✅ |
| reviewer 통합 | reviewer SubAgent ×3 (P02·P05·P06) | **🟢 🟢 🟢 (🔴 0)** — 헌법 §1~§5 불변, append-only, ask(pr) 보존 확인 |

**reviewer 잔여 🟡 (비차단)**: circuit halt 신호 경로 절대화(v2 무인 전 검토) — v1 attended라 진행 차단 아님, v2 항목 이월.

## 학습 일지 후보 키워드

- `loop-ready-but-loopless` — 자율 루프의 어렵고 드문 부품(done판사·정지게이트·maker-checker·메모리)은 이미 있고 범용 드라이버 + 분류만 빠진 진단. 격상 ≠ teardown.
- `builtin-reuse-over-custom` — 엔진을 새로 만들지 않고 내장 `/loop`·`Workflow` 재사용, 어긋나는 핵심(외부 done 심판)만 커스텀. 폴더 네임스페이스(`engine/`)로 내장 shadow 회피.
- `external-done-judge-vs-self-pace` — 내장 self-pace는 AI 자기판단(편향 위험), 우리는 WSL2/dangling 게이트가 객관 판정. 자기평가 편향 차단(refactor-sweep 전례).
- `append-only-supersede` — 옛 ADR 본문 rewrite 0, 상태줄 한 줄만. "무엇이 죽고 무엇이 살았나" 명시 = 결정 스냅샷 역사 자산 보존.
- `ssot-pointer-not-duplicate` — 깃발 정의는 grade-and-risk §3 단일 진실, work-judge는 매핑만. 문서판 "복붙 금지".
- `systemic-broken-relative-links` — 정책/에이전트의 `../policies/`·`../CLAUDE.md` 등이 git HEAD부터 깨진 systemic 결함(M7.5 도입 아님) → post-M7.5 문서정리 일괄 정정 후보.

## 5단계 보고

### 🎯 무엇을 만들었나
하네스의 **운영 모드**를 loop-driven으로 격상. 루프 엔진(`/engine:goal` + 내장 `/loop`·`Workflow`), 판정자 3버킷(`work-judge.md`), 리뷰 처리량 모델(`review-throughput.md`), 엔진 SSOT(`loop-driver.md`), 세션 2종(작업 `/session:start` / 리뷰 `/session:review`), 미룸 장부 3종(`00_Document/ledgers/pending-*`)을 추가하고, 헌법·정책 6개·에이전트 6개·commands·hooks·settings가 이를 가리키게 했다.

### 🤔 왜 필요한가
AI 생산 속도를 사람이 못 따라가는 **throughput 병목**. 사람이 모든 산출물을 리뷰하면 직렬 병목이 된다. 역할을 *방향+판단*으로 전환해 천장을 높인다. 단 학부생 학습 목적 보존을 위해 **이해 게이트**를 박았다 — 깊은 학습은 별도 pull 세션(`/session:review`)으로 분리해 "engineer로 남되 button-pusher는 안 됨"(Addy).

### 🛠️ 어떻게 만들었나
- **엔진을 새로 안 만듦** — 내장 `/loop`(반복)·`Workflow`(병렬)를 몸통으로 재사용, 어긋나는 핵심(**외부 done 심판** = WSL2/dangling 게이트, AI 자기판단 X)만 `/engine:goal`로 글루. `refactor-sweep`은 그 첫 검증된 프리셋(Step0~5 골격 추출).
- **3버킷 판정**(work-judge) = ADR-031의 4종 Stop을 판정자 축으로 재서술. risk-detector 깃발이 1차 분류기 (깃발 정의는 grade-and-risk §3 단일 진실 참조 = 중복 0).
- **세션 2종** — 구현(`/session:start`)과 학습(`/session:review` pull)을 분리. pending-comprehension 장부가 다리.
- **append-only** — 옛 ADR(022 운영모드/019 정적매트릭스/016 3자분업/023 동기시점) 본문 rewrite 0, 상태줄 한 줄만. ADR-031은 *확장*(supersede 아님), ADR-015는 *재확인*(이중 사망 회피).
- **안 고른 대안**: 커스텀 `/loop` 재구현(내장 shadow 위험) / 무인 v2(권한승격·circuit halt·trust-boundary 자율침범 3위험 동시 유입 → defer) / work-judge·review-throughput 흡수(220 임계 초과).

### 🧪 테스트 결과
위 **AC 검증 결과** 표 참조. 핵심: 게임 코드 git diff **0** / 신규 dangling **0** / reviewer **🟢×3 (🔴 0)** / settings ask(pr) 게이트 git diff 불변 보존 / hook smoke + JSON 유효. docs-only라 WSL2 게임 회귀는 불요(done 판사 = dangling 0 + hook smoke + reviewer 🔴0).

### ➡️ 다음 스텝
- **PR 생성·머지 = 영호 명시 GO** (비가역 게이트 — 자동 진행 X). 팀 유지 안 됨 = admin 예외.
- **실측 후 조정**: #6 신뢰졸업 N=3 / 사람 게이트 빈도(영호 "행동 잠금 자주" 우려) / pending 장부 작동 여부(이해 부채 방어).
- **v2 항목**: circuit halt 경로 절대화(reviewer 🟡) · 무인 Desktop scheduled(별도 ADR).
- **post-M7.5 후보**: `00_Document` 분류 정리(systemic 깨진 링크 일괄, 루프 dogfood 첫 후보) · 팀 전제 하네스(CODEOWNERS·co-review·아트 트랙·owner 필드) 정리.

## Phase 진행 + commits

| Phase | 내용 | commit | reviewer |
|---|---|---|---|
| P01 | 신규 정책 3종 토대 (#2=둘 다 독립) | `70533b7` | — |
| P02 | 헌법 + 정책 5 atomic (키스톤) | `6fcb466` | 🟢 |
| P03 | agents 6 REVISE (권한·모델 표 불변) | `7c94e12` | — |
| P04 | `/engine:goal` + `/session:review` (#3) | `a2e094d` | — |
| P05 | hooks halt + settings #4 + 원장 #5 [trust-boundary] | `cea1e4f` | 🟢 |
| P06 | 카탈로그 + ADR-032 등록 + 마감 | (본 commit) | 🟢 |

## 결정 흐름 (미결 #2~#6, 영호 게이트)

- **#2** work-judge·review-throughput = **둘 다 독립** (흡수 시 220 임계 초과 + SRP)
- **#3** 내장 `/loop` 재사용 + `/engine:goal` 신규 (`engine/` 네임스페이스)
- **#4** settings allow에 `git add/commit` 추가, **`ask(pr)` 불변 보존**, 무인 commit 전면=v2
- **#5** 원장 3종 = `00_Document/ledgers/` committed (영호 영속·감사, 팀 유지 안 됨)
- **#6** 신뢰졸업 **N=3 초안** (Rule of Three) — 실측 후 영호 확정 대기
