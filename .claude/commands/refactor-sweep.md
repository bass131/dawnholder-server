---
description: 무인 자동 리팩토링 스윕 — production CODE_CONVENTION/SOLID 진단 + 안전 범위 자동 수정 + WSL2 회귀 게이트 통과분만 전용 브랜치 atomic commit (push/PR 없음). 자기 전 호출, 아침 선별 검토.
argument-hint: "[--dry-run] [--max=N] [--domains=server,shared,clientnet] - 기본: server/shared/clientnet 무인 / 03_Client 제안만 / max=8"
---

production 코드(TDD 제외)의 CODE_CONVENTION/SOLID 부합도를 *자는 동안* 점검하고, 안전 게이트를 통과한 리팩토링만 **전용 브랜치에 commit까지** 해두는 슬래시. 아침에 영호가 commit 이력을 보고 살림/재논의/폐기로 선별. M4.12 후속 신규.

모드/범위: **$ARGUMENTS** (없으면 `--domains=server,shared,clientnet --max=8`, commit 모드)

> ⚠️ **이 슬래시는 코드를 *수정하고 commit*한다** (`/harness-review`·`/cross-review`는 읽기 전용). 무인 commit의 안전은 **로컬 commit까지만 + 회귀 게이트 + 전용 브랜치 + 아침 선별 revert**에 달려 있다. push/PR은 *언제나* 아침 영호 명시 GO.

> **드라이버 프리셋 (M7.5)**: 본 슬래시는 [`engine/goal.md`](engine/goal.md)(`/engine:goal`) **범용 드라이버의 *refactor 모드 프리셋***이다. Step 0~5 골격 + done 심판(외부 기계 게이트)은 `/engine:goal`이 정의하고, 본 슬래시는 refactor 특화만 채운다 — 진단 룰북 = CODE_CONVENTION/SOLID, 도메인 = server/shared/clientnet, 안전 가드 G1~G9. 골격·G1~G9는 약화되면 안 됨.

---

### 이 커맨드의 역할

옛 운영은 리팩토링이 *낮의 대화 안에서만* 가능 → CODE_CONVENTION 부록 A 백로그(God class·DRY 등)가 쌓이기만 했다. 새 운영은 자는 동안 reviewer가 부합도를 측정하고, *되돌리기 쉬운 상태*(전용 브랜치 로컬 commit)까지 리팩토링을 시도해둔다. **판단·선별은 아침에 영호가** — commit 이력이 변경점 단일 진실.

**핵심 안전 철학** (맨 위에 박는다):
- 무인은 **로컬 commit까지만**. push/`gh pr create`/merge는 *절대* 자동 X (G4).
- **회귀 green인 것만** commit (G1). 통과 못 하면 자동 롤백.
- **전용 브랜치만** 건드림 (G2). 현재 작업 브랜치·main 미접촉.
- **trust-boundary(보안)는 영원히 무인 제외** (G7). 03_Client는 *검증 불가*라 제안만 (G3, 한시적).

**언제 호출**: 자기 전 / 큰 리팩토링 백로그가 쌓였을 때 / 발표 같은 마일스톤 사이 정비 시점.

**언제 호출 X**: 하네스 자체 점검 = [`harness-review.md`](harness-review.md) / 코드 변경 후 자동 리뷰 = reviewer (Tier 2-A) / 외부 시각 = [`cross-review.md`](cross-review.md).

---

### Scope (도메인별 처리)

| 도메인 | 경로 | 무인 commit | 진단 |
|---|---|---|---|
| `server` | `02_Server/GameServer/` (서버측, **trust-boundary 제외 G7**) + `98_Shared/` 서버측 | ✅ 저위험 + 🔶 고위험(재검증) | reviewer 축5+6 |
| `shared` | `98_Shared/` (Generated 제외) | ✅ + 🔶 | reviewer |
| `clientnet` | `04_ClientNet/` | ✅ + 🔶 | reviewer |
| `client` | `03_Client/Assets/Scripts/` | ❌ **제안만**(G3 — Unity 검증 불가) | reviewer (제안 diff까지) |
| **항상 제외** | `GameServer.Tests/`·`99_Tools/`·`Protocol/Generated/GenPackets.cs`·trust-boundary(G7) | — | — |

---

### 작업 흐름

#### Step 0. 전제 게이트 + 전용 브랜치 (G2)

메인이 `Bash`로:
1. `git status --porcelain` — **dirty면 즉시 중단**(미커밋 변경 위 작업 금지). 단 `03_Client/ProjectSettings/ProjectSettings.asset` cloud 라인만 dirty면 무음 통과(session:start C-1 정합).
2. 현재 브랜치 기록(`git rev-parse --abbrev-ref HEAD`) — 아침 복귀 좌표.
3. `git checkout -b refactor/auto-YYYYMMDD`(이미 있으면 `-NN` suffix). **이후 모든 commit은 이 브랜치에만**.
4. **baseline 회귀 1회** (아래 "회귀 게이트" 명령 전체) → 빨강이면 *시작 자체 중단*(깨진 baseline 위 리팩 금지). 측정한 `test passed` 수를 **baseline**으로 박음.

#### Step 1. 진단 fan-out (병렬) — reviewer×N

도메인별로 [`../agents/reviewer.md`](../agents/reviewer.md)를 **병렬 호출**(R-only라 상태 충돌 0 — Workflow fan-out 또는 Agent 동시 호출). reviewer 입력 4항목:
- `range` = `refactor-sweep-YYYYMMDD-{domain}`
- `files` = 그 도메인 production 파일 목록(Glob 수집 + 위 "항상 제외" 필터)
- `diff_summary` = `"리팩토링 전 진단 — 변경 없음. 축5(SOLID·패턴) + 축6(Code Convention) 부합도 측정"`
- `grade` = `보통`

4결과를 합쳐 **개선 백로그** 생성 + 아래 위험도표로 각 항목에 `✅/🔶/⛔/📋` 라벨 + 우선순위.

#### Step 2. 리팩 위임 — Worker 도메인 직렬

`--dry-run`이면 **스킵 → Step 5**(진단+제안 리포트만).

아니면 **✅ 저위험 + 🔶 고위험** 항목을 `server` → `shared` → `clientnet` 순차로 도메인 Worker에 위임(병렬 X — "어느 변경이 무엇을 깼나" 추적 위해). 위임 입력:
- 작업: 해당 §조항 위반 해소 (구체 file:line)
- 완료조건: **거동 불변 + wire(패킷/시그니처) 불변 + 빌드 green**
- 제외 명시: **⛔ trust-boundary 파일 + 📋 03_Client 절대 손대지 말 것**
- 🔶 고위험은 **"한 리팩토링 = 한 commit" 단위**로(아침 선별 용이)
- 출력: commit 메시지 후보(§조항/파일/왜)

> Worker는 *수정만*. commit은 최상단(메인)이 Step 3에서. (헌법 "Worker commit 금지" 정합)

#### Step 3. 회귀 게이트 + atomic commit (G1·G6)

도메인 한 묶음 변경이 모이면:
1. **회귀 게이트** 1회(아래 명령). **green(baseline 비감소 + 신규 fail 0)이면** 그 도메인 변경을 **항목별 atomic commit**으로 분할:
   - 메시지: `refactor(scope): <한 줄> [auto-sweep]`
   - 본문: `§조항 / 파일:줄 / 왜(어떤 위반 해소)` — **이 메시지가 아침 선별·재논의의 입력**이므로 의도를 충분히.
2. **red면 이분 격리**: 변경을 반씩 적용→게이트 반복으로 범인 항목 특정 → 통과분만 commit, 범인은 `git restore`(미적용) + 리포트 "실패로 미적용" 기록.

#### Step 4. 검증 fan-out (병렬) — reviewer 재점검

변경된 도메인만 reviewer **병렬 재호출**(`diff_summary` = Step 3 commit 요약). "거동 불변 의도 정합 / 새 위반 유발 X" 확인.
- **🔶 고위험 변경은 재검증 필수**(추가 게이트).
- reviewer 🔴 → 그 commit을 **revert 후보**로 표시 + 리포트 강조(아침 우선검토 대상).

#### Step 5. 종합 + 산출물 (commit까지만, G4)

`00_Document/reviews/YYYY-MM-DD-refactor-sweep.md` Write (아래 스키마). **push/PR 안 함** — "아침 영호 GO 시 push/PR" 명시. 사용자 보고(아래).

---

### 위험도 분류표 (CODE_CONVENTION 조항별)

> **모든 항목 진단·발견은 함.** 처리 4분류 — *무인이 자동으로 고치느냐* + *얼마나 강한 게이트로*:
> - **✅ 무인 (저위험)** — 거동 불변·가역·국소. 회귀 게이트 통과 시 commit.
> - **🔶 무인 (고위험)** — 구조 변경. 회귀 게이트 **+ Step 4 reviewer 재검증 필수** + 리포트 별도 강조.
> - **⛔ 영구 제외 (보안)** — trust-boundary. 테스트로 못 잡는 신뢰경계 구멍(헌법 §3) → 영원히 사람 트랙. 제안만.
> - **📋 제안만 (검증 불가, 한시적)** — 03_Client. Unity 무인 검증 인프라 전까지 commit 불가. 진단·제안 diff까지.

| 조항 | 위반 | 처리 |
|---|---|---|
| §6.2 (a~e) | 금지 주석 제거(역사·Phase 박제/자명 재진술/폐기 사고/TODO/internal XML doc) | ✅ 무인 (저위험) |
| §6.5 | public 클래스 1줄 책임 헤더 추가 | ✅ 무인 (저위험) |
| §7.1 | 멤버 정렬(SA1201/1202) | ✅ 무인 (production / 03_Client는 📋) |
| §7.2(b) | 파일 흐름 1줄 헤더 추가 | ✅ 무인 (저위험) |
| §3.3 | 네이밍 prefix(지역/private field) | ✅ 무인 (`[SerializeField]`은 03_Client=📋) |
| §2.5 | DRY 순수 헬퍼 추출(facingByte 류, 시그니처 동일, 3회+ & 우연중복 아님) | ✅ 무인 (저위험) |
| §2.5 | DRY 데이터소유 mutator 추출(적 사망 류, 호출순서 계약) | 🔶 무인 (고위험 — 재검증 필수) |
| §2.2/2.3 | God class 분리, 600줄+ | 🔶 무인 (고위험 — 재검증 필수) |
| §1.2 | 콘텐츠/엔진 혼재 | 🔶 무인 (고위험 — 재검증 필수) |
| **위치: `GameSession.cs`·`Handlers/**`·`*Validation*`** | 모든 항목 | ⛔ 영구 제외 (보안 §3, G7) |
| **위치: 03_Client** | 모든 항목 | 📋 제안만 (Unity 검증 불가, G3 한시적) |

**판정 순서(위치 우선)**: (1) trust-boundary 위치 → ⛔ (2) 03_Client 위치 → 📋 (3) 구조변경 조항(§2.2/2.3/1.2/2.5-mutator) → 🔶 무인+재검증 (4) 저위험 조항 → ✅ 무인 (5) reviewer 🟡 *진짜 모호* → 📋(다음 라운드로 미룸).

---

### 회귀 게이트 (ADR-029 WSL2 — 실측 baseline)

Bash 도구(Git Bash)에서 `wsl` 경유. **sync→build→test 한 묶음**(rsync 누락 = stale 실행 = 1순위 위험):

```
1) sync:  wsl -d Ubuntu -- bash -lc "cd /mnt/c/Dev/ClaudeDev && rsync -a --delete --exclude 'bin/' --exclude 'obj/' Dawnholder.slnx global.json 02_Server 98_Shared 99_Tools 04_ClientNet ~/dawnholder-poc/"
2) build: wsl -d Ubuntu -- bash -lc "cd ~/dawnholder-poc && ~/.dotnet/dotnet build Dawnholder.slnx"
3) test:  wsl -d Ubuntu -- bash -lc "cd ~/dawnholder-poc && ~/.dotnet/dotnet test Dawnholder.slnx --no-build"
4) 봇:    99_Tools/run_bot_regression.sh (연속) + 의심 시 run_bot_fresh_recheck.sh (fresh 단독). 핵심 시나리오 우선.
```

- **baseline = 하드코딩 숫자 아님**. Step 0가 측정한 값 기준 **"비감소 + 신규 fail 0"**으로 판정(숫자 drift 견고 — work-pin/CHANGELOG 숫자가 달라도 무관).
- **봇 연속 FAIL ≠ 회귀**: 서버 상태 누적(entity=0)일 수 있음 → fresh 단독 재검 PASS가 판정.

---

### 아침 검토 — 선별 처리 (핵심 운영 정신)

영호가 아침에 리포트 + `git log refactor/auto-YYYYMMDD`(각 commit에 §조항/파일/왜)를 보고 **3분기 선별**. **commit 이력 = 변경점 단일 진실, atomic commit = 선별 단위.**

| 판단 | 처리 |
|---|---|
| **전체 OK** | 그대로 살림 → push/PR은 영호 명시 GO (G4) |
| **전체 NG** | 전체 폐기: `git checkout <원브랜치> && git branch -D refactor/auto-YYYYMMDD` |
| **일부 NG** ★ | 버리지 말고 선별 (아래) |

**일부 NG일 때** — 문제 commit이 *폐기가 기본이 아님*:
- **OK commit** → 그대로 살림(선별 보존).
- **방향은 맞는데 방식이 별로** → **재논의 대상**. commit 메시지의 `§조항/파일/왜`가 의도를 보존하므로, 다음 대화에서 "이 위반을 *어떻게* 고칠지" 다시 논의해 구체화·재작업(원 의도 살림).
- **의도 자체가 틀림** → `git revert <hash>`로 그것만 폐기(atomic이라 선별 가능).

→ "전체 날림"은 *전체 NG일 때만*. 부분 문제는 commit 이력을 입력 삼아 다듬는다.

---

### 산출물 스키마

`00_Document/reviews/YYYY-MM-DD-refactor-sweep.md`:

```markdown
# 무인 리팩토링 스윕 — YYYY-MM-DD
## TL;DR
- 브랜치: refactor/auto-YYYYMMDD (출발: <원 브랜치>)
- baseline: test <N>/0 (시작) → <M>/0 (종료, 비감소 ✅/❌)
- 적용: <K> commit (✅저위험 <a> / 🔶고위험 <b>) / 제안만: <J> / 실패 미적용: <F>
- ⚠️ reviewer 재검증 🔴: <r>건 (우선검토)
- 아침: 전체폐기 = git checkout <원브랜치> && git branch -D <브랜치>

## 1. 부합도 점수 (도메인별 축5/축6 🔴N🟡M)
## 2. 적용 commit (# | hash | 도메인 | §조항 | ✅/🔶 | 한 줄 | 게이트)
## 3. 🔶 고위험 변경 — 우선검토 (commit + 무엇을 어떻게 바꿨나 + reviewer 재검증 결과)
## 4. 제안만 (⛔보안 / 📋 03_Client — §조항 | 파일:줄 | 제안 diff | 왜 제외)
## 5. 테스트/봇 (baseline→최종 / 봇 PASS·FAIL / reviewer 재검증)
## 6. 실패 미적용 (항목 | 사유 | 롤백 방식)
## 7. 선별 가이드 (전체폐기 / 부분: git revert <hash> — atomic)
```

---

### 사용자 보고 (아침)

```
─────────────────────────────────────────
🧹 무인 리팩토링 스윕 완료 — YYYY-MM-DD
─────────────────────────────────────────

브랜치: refactor/auto-YYYYMMDD (출발: <원 브랜치>)
baseline: test <N> → <M> (비감소 ✅)
산출물: 00_Document/reviews/YYYY-MM-DD-refactor-sweep.md

✅ 저위험 자동수정: <a> commit
🔶 고위험 자동수정: <b> commit  ← 우선 검토 권장
📋 제안만(보안·03_Client): <J>건
❌ 실패 미적용: <F>건

⚠️ reviewer 재검증 🔴: <r>건 (revert 후보)

➡️ 선별:
  - 전체 OK → push/PR (영호 GO)
  - 일부 NG → 그 commit만 git revert <hash> 또는 재논의
  - 전체 NG → git checkout <원브랜치> && git branch -D <브랜치>
```

---

### Hard rules (G1~G7)

1. **회귀 green만 commit (G1)** — baseline 비감소 + 신규 fail 0. 실패는 항목 롤백 + 리포트.
2. **전용 브랜치만 (G2)** — `refactor/auto-YYYYMMDD`. 현재 브랜치·main 절대 미접촉.
3. **03_Client 제안만 (G3)** — Unity 무인 검증 불가. commit X, 제안 diff까지. (인프라 생기면 해제)
4. **push/PR 절대 금지 (G4)** — 무인은 로컬 commit까지만. push/merge는 아침 영호 명시 GO.
5. **고위험도 무인 시도하되 재검증 필수 (G5)** — 🔶는 Step 4 reviewer 재검증 + 리포트 강조.
6. **atomic commit + 리포트 (G6)** — 항목별 commit(§조항/파일/왜) + 아침 선별 리포트.
7. **trust-boundary 영구 제외 (G7)** — `GameSession.cs`·`Handlers/`·`*Validation*` = 보안 §3, 영원히 사람 트랙.
8. **Worker는 수정만, commit은 최상단** — 헌법 "Worker commit 금지" 정합.
9. **reviewer 입력 4항목 누락 금지 / WSL2 sync→build→test 한 묶음** (stale 방지).

---

### 함정

- **stale 실행** (ADR-029 1순위) — 게이트는 *매번* sync→build→test 한 묶음. rsync 빼면 옛 코드 테스트.
- **baseline drift** — 시작 baseline이 빨강이면 시작 중단(깨진 위에 리팩 금지). 숫자는 *실측*(work-pin 561 vs CHANGELOG 541 같은 drift 무시).
- **reviewer false positive 증폭** — 진단 🟡는 무인 수정 X(위험도표 *명시 조항만*). 모호하면 📋로.
- **commit 폭주** — `--max=N`(기본 8) 상한. 한 번에 다 갈아엎지 않음.
- **봇 비결정** — BossFight/HpSync 연속 실패는 기존 한계 → fresh 서버 단독 + 핵심 시나리오만.
- **🔶 고위험의 진짜 안전망은 테스트 커버리지** — 테스트가 약한 영역의 큰 리팩토링은 미묘한 버그 가능 → 리포트 강조 + 아침 우선검토가 2차망.
- **★진단 힌트는 진단 대상 브랜치에서 실측** — Step 0에서 브랜치 확정 *후* 그 트리에서만 줄수·좌표 측정. **전환 전 브랜치의 1차 스캔 값을 reviewer 힌트로 주입 금지**(stale). 2026-06-12 첫 dry-run에서 feature/m4.12 줄수(LocalPlayerMovement 410)가 main 기준 진단(393)에 섞여 Codex가 적발. carry-over "박제/추천 전 file:line 실측" 정합.
- **★self-assessment bias = 고위험 cross-check 게이트** — Claude reviewer가 *자기 슬래시로 자기 코드*를 진단하면 후하게 볼 수 있음(첫 dry-run에서 `LocalPlayerMovement.Update()` 책임 과다를 reviewer가 "🟢 분리X"로 놓침 → Codex가 🟡로 적발). **🔶 고위험 무인 자동수정 전, 특히 commit 모드 *첫 회차*는 외부 시각 cross-check(`/cross-review` 또는 Codex β) 1회 권장** — reviewer 단독 진단을 무인 commit의 유일 게이트로 삼지 말 것.

---

### 첫 도입 → 확장 로드맵

| 단계 | 범위 | 무인 commit |
|---|---|---|
| **v0** (권장 첫 1~2회) | `--dry-run`: 전체(✅+🔶) 진단 + 고위험 제안 diff까지 리포트만 → 영호가 "AI가 God class를 *이렇게* 분리하려는구나" 품질·라벨 육안 확인 | ❌ |
| **v1** | server/shared/clientnet: ✅ 저위험 + 🔶 고위험(재검증). `--max` 상한 | ✅ |
| **v2** | 03_Client 포함 | ✅ Unity 무인 검증 인프라(EditMode/콘솔 MCP 무인) 선결 후 G3 해제 |
| **영구** | ⛔ trust-boundary | ❌ 자동화 안 함 (보안 §3) |

확장 게이트: v0 dry-run 1~2회로 고위험 제안 품질 확인 후 v1. v1→v2는 Unity 무인 검증 인프라 선결.

---

### 옛/다른 슬래시와 차이

- **`/harness-review`** — *하네스 메타* 점검. **읽기 전용**(코드 미수정).
- **`/cross-review`** — *외부 시각* 재검증. **읽기 전용**.
- **`/engine:goal`** — *범용* 목표 도달형 드라이버 (Step 0~5 골격 + 외부 done 심판). 본 슬래시는 그 **refactor 프리셋**.
- **`/refactor-sweep`** (본 슬래시) — production *코드를 수정하고 commit*. 무인 자동화. 안전 = 전용 브랜치 + 회귀 게이트 + 아침 선별. **프로젝트 첫 무인 코드 변경 패턴 = `/engine:goal`의 첫 검증된 인스턴스.**

---

### 발동 시점 권유

| 시점 | 모드 |
|---|---|
| 처음 써보는 첫 1~2회 | `--dry-run` (품질 확인) |
| 자기 전 정기 정비 | 기본(commit 모드) |
| 부록 A 백로그 쌓였을 때 | 기본 |
| 마일스톤 사이 정비 | 기본 |
| trust-boundary/Unity 작업 직후 | 호출 X (그 영역은 제안만/제외 — 가치 낮음) |
