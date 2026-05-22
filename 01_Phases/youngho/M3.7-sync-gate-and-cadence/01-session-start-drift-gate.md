---
owner: youngho
milestone: M3.7
phase: 01
title: /session:start drift 발견 게이트 신설
status: done
grade: 보통
estimated: 30~60min
domain: harness
---

# Phase 01: `/session:start` drift 발견 게이트 신설

> **상태**: pending
> **마일스톤**: M3.7
> **등급**: 보통 (1 도메인 × 2 파일 / ~30~50줄)
> **담당**: 메인 직접 (영호)

---

## 🎯 목표

`/session:start` 슬래시 본문에 **drift 발견 게이트** 신설 — 세션 시작 시점에 git/gh 명령으로 실제 진행 단계를 조회하고 work-pin "현재 작업/다음 액션" 줄과 비교 → 차이 발견 시 STOP + 본인 수동 갱신 안내.

**핵심 정신**: 자동 갱신 박지 않음. *발견*만 자동, *갱신은 본인 인지 게이트* (헌법 정신 = `pin-and-done.md` §1 "갱신은 본인 수동" 정합 / Hook is for alert, not action).

**왜 본 Phase가 필요한가**:

- 본 세션 시작 시점에 발견: work-pin "commit + push + PR 게이트 대기"라 박혔지만 실제 4단계 모두 박힘 (PR #44 MERGED). `/session:start` 0단계(git 안전 점검)는 *git status + 브랜치*만 점검 → *진행 단계* 비교 안 함
- 5번째 실측 = Rule of Three 통과 → 발견을 자동화 해야 함. 옛 패턴 = 본인이 수동 PR 리스트 체크 → 사람 인지 게이트 의존이라 빈도 ↓
- 본 게이트가 박히면 다음 세션 시작 시 stale 자동 발견 (본 세션 같은 발견 = 자동 STOP)

---

## ⏪ 사전 조건

- [x] M3.7 _milestone-plan.md 박힘
- [x] work-pin clear → M3.7 진입 좌표 박힘 (본 마일스톤 자체가 stale hole 봉합 시범)

---

## 📝 작업 내용

### `.claude/commands/session/start.md` 본문 보강

기존 0단계(git 안전 점검) 통과 후 **새 단계 (0.5 또는 1.5) 신설**:

- `git log -3 --oneline` 자동 호출 → 최근 commit 3건
- `gh pr list --state all --head $(git branch --show-current) --limit 3` 자동 호출 → 본 브랜치 최근 PR 3건
- work-pin "현재 작업" / "다음 액션" 줄에 박힌 단계 키워드 vs 실제 비교:
  - "commit 박을 예정" 박혔는데 본 브랜치 최근 commit가 *그 작업 commit*이면 stale
  - "push 대기" 박혔는데 git status가 origin과 sync면 stale
  - "PR 생성 대기" 박혔는데 `gh pr list`에 본 브랜치 PR 박혀있으면 stale
  - "PR 머지 대기" 박혔는데 PR state == MERGED면 stale
- 차이 발견 시 STOP 메시지 + 본인 수동 갱신 안내 (자동 갱신 X):

```
⚠️ STOP — work-pin이 실제 진행 단계와 어긋났어요 (drift 발견).

work-pin 박힌 단계: <키워드>
실제 git/gh 상태: <실제>

다음 중 본인이 결정:
  1) work-pin 갱신 (.claude/state/current-pin.txt) — 실제 상태 반영
  2) CONTEXT.md "⏸️ 현재 멈춤 지점" 갱신 (다음 세션 동기)
  3) 둘 다 갱신

해결 후 /session:start 다시 호출해주세요.

(자동 갱신 안 함: Hook은 알림 전용 정신 — pin-and-done.md §1)
```

- 차이 없으면 무음 통과 → 기존 1단계 (CONTEXT.md 통독) 진행

### `00_Document/team-guide.html` "막혔을 때" 표 한 행 박제

기존 표(2026-05-15 STOP 안내 박힘 시 추가)에 한 행 추가:

| 증상 | 원인 | 해결 |
|---|---|---|
| `/session:start`가 "work-pin drift" STOP을 띄움 | 옛 work-pin 박힌 진행 단계가 실제 git/gh 상태와 어긋남 | work-pin 또는 CONTEXT.md "⏸️ 현재 멈춤 지점" 갱신 후 재호출 |

학부생이 처음 본 STOP 만났을 때 대응 방향 박힘.

---

## ✅ 완료 조건

- [ ] `.claude/commands/session/start.md` 새 단계 박힘 (0단계와 1단계 사이 또는 1단계 안 분기)
- [ ] git log + gh pr list 호출 명령 박힘 (PowerShell + Bash 양쪽 호환, 또는 도구 무관)
- [ ] STOP 메시지 양식 박힘 (위 예시대로 또는 비슷한 본인 인지 게이트 안내)
- [ ] 자동 갱신 X 명시 (Hook은 알림 전용 정신)
- [ ] `team-guide.html` "막혔을 때" 표에 한 행 추가
- [ ] **본 Phase 자체가 *작동 시연*** — 본 Phase 작성 직후 시뮬 호출 (다음 세션이 아니라 본 세션 안에서 "만약 호출하면 어떻게 작동할지" 명시 박음)

---

## 🧪 테스트

**수동 시뮬**:
- 본 세션 work-pin (M3.7 진입 박힌 상태) vs 실제 git/gh 상태 비교 → 차이 없음 (정합) 확인
- 만약 work-pin이 옛 M3.6 본문 그대로 박혔다면 어떻게 STOP 떴을지 시뮬 결과 박음 (Phase 01 -DONE.md 또는 본 정의 `학습 포인트` 섹션)

**자동**:
- 별 시점 (다음 세션 시작 시점) 실측 → Phase 01 마감 후 별 시점 박음

---

## 📚 학습 포인트

- **자동 발견 + 본인 갱신** — Hook 자동 갱신 박지 않음. 학부생 인지 게이트 보호 정신
- **5번째 실측 후 봉합 = Rule of Three 정합** — 옛 운영 *발견은 본인 수동*이라 빈도 ↓. 본 Phase = 자동 발견으로 빈도 ↑, 갱신은 여전히 본인
- **세션 시작 시점 게이트의 가치** — 옵션 C 게이트(세션 마감 시 동기) + 새 발견 게이트(세션 시작 시 비교) = 양방향 안전망

---

## ⚠️ 함정 / 주의사항

- **gh CLI 의존성** — 본 게이트는 `gh` CLI 박힌 환경 전제. 미박힘 환경(인규 합류 시점)은 setup-steps에 박혀있는지 확인 필요 (별 시점 점검)
- **git log + gh pr list 비용** — 매 세션 시작 시 2 호출 추가 = ~1~2초. 학부생 호흡 영향 X (체감 미미)
- **stale 키워드 매칭의 false positive** — work-pin "commit 박을 예정" 자유 양식이라 키워드 매칭 깨질 수 있음. 본 Phase는 *대략 매칭*만 (정확 매칭 X), 본인 인지 게이트가 최종 판단

---

## ➡️ 다음 Phase

- Phase 02 (ADR 묶음 신설) — 본 Phase 산출물(/session:start 새 단계)이 ADR-023 *새 발견 게이트* 항목에 인용됨

---

## 📋 박제 (완료 후)

- 등급 보통 → **`-DONE.md` 박지 않음** (work-pin + commit message로 충분)
- 5단계 보고 X
- 본 Phase 완료 = work-pin "다음 액션" → Phase 02 진입 좌표로 갱신
