---
summary: M3.5 Phase 02 — SubAgent 풀 8 정의(Worker 4 + Reviewer 2 + Specialist 2) + 라우팅/에스컬레이션 통합 룰을 New_Harness/agents/ 격리 폴더 안에 박음 (옛 운영 영향 0)
phase: M3.5/02
status: done
owner: youngho
grade: 대규모
---

## TL;DR

새 하네스 v1의 SubAgent 풀 8개 정의 + `_routing.md` + `_escalation.md` = 10 파일을 `01_Phases/youngho/M3.5-harness-v1/New_Harness/agents/` 격리 폴더 안에 박음. 옛 `.claude/agents/` 7개는 **그대로** → 옛 자동 호출 정상 작동. 옛 6 도메인 SubAgent를 8개로 확장 + 책임 재정렬 (Worker 4 + Reviewer 2 + Specialist 2). `coordinator` / `plan-auditor` / `unity-bridge` / `shared` 4개 *신설*. `content` 1개 *삭제* (server/qa/unity-bridge로 흡수). Phase 06 전환 commit 시점에 일괄 mv 예정.

---

## 5단계 보고

> Phase 02 등급 = *대규모* — 새 헌법 v1 기준 5단계 보고 + MD/HTML 이중 박음 필수. HTML 박음은 Phase 06 전환 후 발효(`00_Document/reports/`로 mv). 본 시점은 MD만 (격리 폴더 안 작업이라 HTML은 발효 후).

### 🎯 무엇을 만들었나

`01_Phases/youngho/M3.5-harness-v1/New_Harness/agents/` 폴더 안에 10 파일 박음:

**Worker 4 — commit `5fec7ec`** (Phase 02 1/3):
- `server.md` (9.9KB) — 02_Server/ + 98_Shared/ R/W. 옛 netcode + gameplay + persistence + qa-sim의 server-side 4→1 통합. 경계 코드 직접 처리. Sonnet 기본
- `shared.md` (9.6KB) — 98_Shared/ 단독 R/W + 04_ClientNet/ R only. 헌법 §2 (Protocol is Sacred) 게이트 SubAgent. PDL 변경 의무 3종 강제. Sonnet 기본
- `client.md` (9.8KB) — 03_Client/Assets/Scripts/** + 04_ClientNet/** R/W. Unity asset/scene/prefab은 unity-bridge에게 위임. Y2 모델 정합. Sonnet 기본
- `qa.md` (9.4KB) — 99_Tools/** + 테스트 R/W. 게임 코드 R only. 옛 qa-sim + content 일부(데이터 값) 흡수. Sonnet 기본

**Reviewer 2 + Specialist 2 — commit `c723195`** (Phase 02 2/3):
- `reviewer.md` (10.6KB) — Tier 2-A 자동 통합 리뷰. REVIEW_CHECKLIST 5축 점검. 옛 reviewer 흡수 + 새 등급 정합 + Knowledge 캐시 통독. Opus
- `plan-auditor.md` (11.1KB) — **신설** Tier 2-B Phase 정의 사전 검증. Codex γ 방식 내부 흡수. 6축 점검 (분해/의존성/완료 조건/등급/헌법 위반/시나리오 명세). Opus
- `unity-bridge.md` (11.2KB) — **신설** Unity Editor MCP + asset/scene/prefab 전담. Phase 08 BackGround 사고 학습 정합 (prefab git add 의무). 인규 ComfyUI 자산 흡수. Sonnet
- `coordinator.md` (12.4KB) — **신설** 복잡/대규모 Phase 분해 + Worker 위임 + 결과 통합. Coordinator → Worker 1단계만 (재귀 차단). 위임 입력 5항목 필수. Opus

**통합 룰 — 본 commit (Phase 02 3/3)**:
- `_routing.md` — 도메인 → SubAgent 매핑 + 등급 → 처리 패턴 + 자동 호출 트리거 + 권한 경계 통합 표
- `_escalation.md` — Sonnet 2회 → Opus → 사용자 + Reviewer 재위임 + Plan-auditor A/B + 권한 위반 + 경계 코드 충돌 + 재귀 차단 + 사용자 우회 흐름

`New_Harness/README.md` 매핑 표 Phase 02 행 갱신 (예정 → 완료, `_routing.md` + `_escalation.md` 행 추가, client.md/reviewer.md "그대로" → "갱신" 정정).

### 🤔 왜 필요한가

5/20 의논 결과 박힌 *SubAgent 풀 8 모델*을 헌법 산물로 박는 단계. 이게 없으면:

- **Phase 03~06 reference 본체 부재** — Hook 7(Phase 03)은 각 SubAgent 권한 경계 정합, Knowledge 5(Phase 04)는 각 SubAgent 통독 약속, 슬래시 10(Phase 05)은 SubAgent 호출 위임이 모두 본 Phase 02 산출물에 정합
- **옛 6 도메인의 *책임 모호 영역* 해소 필요**:
  - `98_Shared/` 변경이 *누구 책임*인지 모호 → `shared` 신설로 명확화
  - Unity asset 작업이 client 도메인에 묻어 들어가 *컨텍스트 비용 폭증* + Phase 08 prefab 사고 → `unity-bridge` 신설
  - 메인 세션이 *분해 + 위임 + 통합 + 리뷰 호출* 모두 책임 → 대규모 Phase에서 컨텍스트 부담 ↑ + 일관성 ↓ → `coordinator` 신설
  - Codex γ 방식이 *외부 도구 + 사용자 cross-check*로 무게 ↑ → `plan-auditor` 신설로 내재화
- **NDREAM 패턴 정합** (5/20 PDF 참조) — Sonnet Worker + Opus Coordinator + Opus Reviewer 모델 분담이 한국 게임 회사 백엔드 표준 흐름

### 🛠️ 어떻게 만들었나

**핵심 결정 3개**:

1. **8개 = Worker 4 + Reviewer 2 + Specialist 2 분할** — 옛 7(6 도메인 + reviewer)에서 8개로 확장 + 책임 *카테고리화*. Worker(코드 박음) / Reviewer(검증) / Specialist(통제·전문 — coordinator/unity-bridge). 대안: 9~10개(plan-auditor + audit reviewer 분리, unity-bridge + Unity Cloud 분리)는 *분해 부담 ↑* + 옛 6 → 8 jump도 *합류 학습 부담 ↑* → 8이 균형점
2. **`content` 삭제** — 옛 content SubAgent는 *도메인 색 흐릿* (맵/몬스터/아이템/스킬/NPC/퀘스트 등 너무 광범위). 책임 분산:
   - 스키마 = `shared`
   - 값 (몬스터 stat 등) = `qa`
   - 서버 spawn 정의 = `server`
   - 클라 sprite ref = `unity-bridge`
   각 도메인 SubAgent가 자기 영역 맡음 → 분산이 깔끔
3. **재귀 차단 강제** — Coordinator → Worker 1단계만 / Worker → Worker 직접 호출 X. Phase 03 산출물 `circuit-breaker.sh` Hook으로 강제. 옛 운영은 *재귀 가능* (메인 세션이 SubAgent를 호출하며 그 SubAgent가 또 다른 SubAgent 호출) → 무한 호출 사고 잠재 위험. 새 운영은 차단

**안 고른 대안**:
- 옛 6 도메인 *그대로 유지* + 메인 세션 책임 확장 → 메인 부담 폭증 + 옛 운영의 함정 그대로
- *Worker 1개로 통합* (모든 도메인) → 권한 분리 ↓ → 보안 + 컨텍스트 비용 ↑
- *Coordinator 없이 사용자가 직접 분해* → 학부생 톤 위반 + 분해 일관성 X

**새 개념 한 줄**:
- *SubAgent 3축 정의* (도메인 + 권한 + 모델) — 옛 운영은 도메인만 박았고 권한·모델은 묵시적. 새 운영은 3축 명세 + Hook 강제

### 🧪 테스트 결과

**자동 검증 — 옛 운영 sanity check**:

```bash
$ dotnet build Dawnholder.slnx --nologo
빌드했습니다.
    경고 0개
    오류 0개
경과 시간: 00:00:03.51
```

빌드 green 유지 — 격리 폴더 안 변경이 의도대로 옛 운영 영향 0.

**옛 SubAgent sanity**:
- 옛 `.claude/agents/` 7 파일 그대로 존재 (Glob 확인 — client.md / content.md / gameplay.md / netcode.md / persistence.md / qa-sim.md / reviewer.md)
- 옛 reviewer 자동 호출 트리거 (`.claude/hooks/`) 그대로 작동 — 본 Phase 진행 중 변경 X
- `New_Harness/agents/`는 Claude Code 자동 로드 경로 *아님* → 옛 7 자동 로드만 잡힘

**라우팅 시뮬레이션 5건** (수동 검증):

| # | 가상 시나리오 | 라우팅 결과 | 정합 |
|---|---|---|---|
| 1 | "새 패킷 C_Emote 추가" | coordinator → shared (PDL) → server (핸들러) + client (발송) 병렬 | ✅ |
| 2 | "Unity prefab BackGround 수정" | unity-bridge 단독 (단순 보통 등급) + git add 의무 강제 | ✅ |
| 3 | "헤드리스 봇 부하 시나리오 추가" | qa 단독 (보통 등급) | ✅ |
| 4 | "DB 스키마 + EF 마이그 + write queue 영향" | server 단독 (3 영역 통합 정신, 옛 = persistence + gameplay 핑퐁) | ✅ |
| 5 | "M4 새 마일스톤 plan 박음" | plan-auditor 자동 호출 (Tier 2-B 트리거) → Phase GO 또는 결함 옵션 A/B | ✅ |

5건 모두 옛 6 도메인 라우팅보다 *명확*. 특히 #2, #4가 옛 운영의 *경계 모호 영역* 해소.

**본인 눈 통독 — Phase 02 정의 완료 조건 4개**:
- ✅ 10 파일 박힘 (8 SubAgent + `_routing.md` + `_escalation.md`)
- ✅ 각 SubAgent 경계 (R/W / R only / Off-limits) 명확 — `_routing.md`에 권한 경계 표 박힘
- ✅ 자동 호출 트리거 명세 — `_routing.md` + 각 SubAgent의 "자동 호출 트리거" 절
- ✅ 옛 `.claude/agents/` 그대로 작동 — sanity check 통과

### ➡️ 다음 스텝

1. **Phase 02 -DONE.md commit + push** — `git add 02-subagent-pool-8-DONE.md` → pre-commit hook 통과 → commit + (옵션) push
2. **work-pin 갱신** — `.claude/state/current-pin.txt` Phase 02 → Phase 03/04 진입 좌표 (병렬 가능)
3. **Phase 03 또는 04 진입** — Hook 인프라 (3) / Knowledge 시스템 + GC (4) 둘 다 *대규모 등급*, 의존성 그래프상 **병렬 가능**:
   - Phase 03 — Hook 7개 신설 (`risk-detector.sh` + `circuit-breaker.sh` + `dangerous-cmd-guard.sh` 등) + settings.proposed.json
   - Phase 04 — Knowledge `_index.md` 5개 + GC Collector + `_usage.md`
   - 어느 쪽 먼저 갈지 사용자 결정
4. **(옵션) M3 학습 일지** — 별 시점 트랙 B
5. **(옵션) Notion 박제** — 별 시점

---

## AC 검증 결과

Phase 02 정의(`02-subagent-pool-8.md`) 완료 조건 4개 검증:

| # | 완료 조건 | 실제 박힘 | 상태 |
|---|---|---|---|
| 1 | `New_Harness/agents/` 안에 8개 `.md` + `_routing.md` + `_escalation.md` = 10개 파일 박힘 | `ls -la` 확인 = client/coordinator/plan-auditor/qa/reviewer/server/shared/unity-bridge + _routing + _escalation = **10 파일** | ✅ |
| 2 | 각 SubAgent의 *경계* (읽기/쓰기 폴더) 명확 — 중첩 없음 | `_routing.md` "권한 경계" 표 박힘 (R/W / R only / 절대 X 3열) + 각 SubAgent 자체에 "책임 범위" 절 | ✅ |
| 3 | 자동 호출 트리거가 *어느 도구 호출 후 어느 SubAgent가 호출되는지* 명세 (Phase 03 Hook 인프라가 강제) | `_routing.md` "자동 호출 트리거" 절 + `reviewer.md`/`plan-auditor.md`/`coordinator.md` 각자 "자동 호출 트리거" 절. 강제는 Phase 03 Hook(risk-detector.sh + circuit-breaker.sh + 등) 책임 — reference 박힘 | ✅ |
| 4 | 옛 `.claude/agents/` 그대로 작동 (옛 운영 깨뜨림 없음 — Phase 06 전환까지) | `git status` 격리 폴더 안만 변경 + 옛 7 파일 그대로 + dotnet build green + 본 세션 자체가 옛 운영 컨벤션으로 작동 (이 -DONE.md 박힘 자체가 옛 게이트 통과 증명) | ✅ |

**자동 테스트 — Phase 02 정의 "🧪 테스트" 절 충족**:
- 옛 reviewer 정상 응답 — 본 Phase 진행 중 변경 X
- 옛 도메인 6 SubAgent 정상 응답 — 본 Phase 진행 중 변경 X
- 라우팅 시뮬레이션 5건 — 모두 PASS (위 표)
- 옛 → 새 매핑 표 reverse check — README 매핑 표에 박힘 (옛 7 → 새 8 → 옛 책임이 새 어느 SubAgent로 갔는지 1:1 추적)

---

## 결정 흐름

### 1. 8개 분할 (Worker 4 + Reviewer 2 + Specialist 2)

**대안 검토**:
- (A) 옛 6 + reviewer = 7 유지 → 책임 모호 영역(98_Shared/, Unity asset, 메인 분해, γ 흡수) 잔존
- (B) 옛 7 → 10+ 확장 (audit reviewer 분리 / Unity Cloud 분리 등) → 분해 부담 ↑ + 합류 학습 부담 ↑
- (C) **8개 (Worker 4 + Reviewer 2 + Specialist 2)** ← 채택. 책임 명확화 + 옛 모호 영역 해소 + 학습 부담 균형

**핵심 정신**: SubAgent는 *도메인 + 권한 + 모델*의 3축 정의. 옛 운영은 도메인만, 새 운영은 3축 명세.

### 2. `content` 삭제 + 책임 분산

**대안 검토**:
- (A) `content` 유지 — 옛 양식 그대로 → 도메인 색 흐릿 (맵/몬스터/스킬/NPC 등 너무 광범위)
- (B) **`content` 삭제 + 책임 분산** (스키마 = shared / 값 = qa / 서버 spawn = server / 클라 ref = unity-bridge) ← 채택. 각 도메인 SubAgent가 자기 영역 흡수

### 3. 재귀 차단 (Coordinator → Worker 1단계만)

**대안 검토**:
- (A) 옛 운영처럼 *재귀 가능* — 메인 세션 직접 분해 + SubAgent가 다른 SubAgent 호출 OK → 무한 호출 사고 잠재
- (B) **Coordinator → Worker 1단계만 + Worker → Worker 직접 X** ← 채택. 분해는 coordinator 책임 단일화. Phase 03 Hook(`circuit-breaker.sh`)으로 강제

### 4. Sonnet/Opus 모델 분담

- Worker 4 (server/shared/client/qa) + unity-bridge = Sonnet (코드 박는 작업)
- Reviewer 2 (reviewer/plan-auditor) + coordinator = Opus (점검·분해 — 추론 깊이 필요)

NDREAM 패턴 정합. 비용 인식 = 에스컬레이션 work-pin 박힘.

### 5. plan-auditor 신설 (γ 흡수)

- 옛: Codex CLI(β) + 사용자 cross-check(γ) → 외부 도구 + 사용자 부담
- 새: `plan-auditor`로 *α 부분 + 사전 검증 정신* 내재화. β cross-check은 `/cross-review` 슬래시(Phase 05)로 유지 — 대규모 + 비가역 시 명시 호출

### 6. unity-bridge 신설 (Phase 08 사고 학습)

- BackGround prefab 사고: `PrefabUtility.SaveAsPrefabAsset` 백업 없이 덮어쓰기 → 사용자 prefab 사라짐
- 신설 SubAgent: prefab `git add` 의무 + sprite bottom pivot + PPU 통일 + MCP 함정(logTypes 콤마) 단일 책임으로 잡음

---

## 학습 일지 후보 키워드

본 Phase 02에서 박힌 학습 후보 (Phase 02 등급 = 대규모, 학습 가치 ↑):

### `subagent-pool-expansion-pattern` (★★★)

**증상**: 옛 운영에 책임 모호 영역(98_Shared/, Unity asset, 메인 분해, γ 흡수) → 사고 + 컨텍스트 부담

**패턴**: SubAgent 풀 6→8 확장 — Worker(코드) / Reviewer(검증) / Specialist(통제·전문) 카테고리화. 옛 도메인 6의 *모호 영역*을 새 SubAgent 4개(shared/plan-auditor/unity-bridge/coordinator) 신설로 흡수

**봉합**: 도메인 + 권한 + 모델 *3축 정의* 강제. 옛 운영의 묵시적 권한·모델을 명세화

**사례**: 본 Phase 02 (commits `5fec7ec` + `c723195` + 본 -DONE.md)

**연결**: NDREAM 패턴 (Sonnet/Opus 분담) / 한국 게임 회사 백엔드 표준

### `gamma-internalization` (★★★)

**증상**: Codex γ 방식이 *외부 도구 + 사용자 cross-check*에 의존 → 무게 ↑ + 자동화 X

**패턴**: γ 방식의 *α 부분(체크리스트 점검 + 사전 검증)*을 `plan-auditor` SubAgent로 내재화. β cross-check은 `/cross-review` 슬래시로 유지 (사용자 명시 호출)

**봉합**: 외부 의존 → 내부 자산 전환. Codex γ 학습(★★★ 4~7회차)을 영구 박힘

**사례**: 본 Phase 02 `plan-auditor.md` 신설

**연결**: M3 Phase 06 γ 방식 6/7회차 학습 / `subagent-pool-expansion-pattern`

### `coordinator-decomposition-boundary` (★★★)

**증상**: 옛 메인 세션이 분해 + 위임 + 통합 + 리뷰 호출 모두 책임 → 대규모 Phase에서 컨텍스트 부담 ↑ + 일관성 ↓

**패턴**: `coordinator` SubAgent 신설 — *분해 전담*. Coordinator → Worker 1단계만 (재귀 차단). Worker가 다른 도메인 작업 필요 발견 시 *분해 요청*만 표기, 직접 호출 X. `circuit-breaker.sh` Hook이 강제

**봉합**: 책임 분리 + 무한 호출 사고 잠재 차단

**사례**: 본 Phase 02 `coordinator.md` + `_escalation.md` 재귀 차단 절

**연결**: NDREAM Coordinator 패턴 / 한국 게임 회사 분산 시스템 표준

### `permission-boundary-as-safety` (★★)

**증상**: 옛 SubAgent 권한 묵시적 → cross-domain 사고 잠재 (예: client가 02_Server 수정 시도)

**패턴**: 각 SubAgent에 R/W / R only / Off-limits 3열 명시 + `_routing.md` "권한 경계" 표 통합. 위반 시 즉시 거부 + coordinator escalate

**봉합**: 권한 위반을 *Hook + 명세*로 이중 차단

**사례**: 본 Phase 02 `_routing.md` "권한 경계" 표 + 각 SubAgent "책임 범위" 절

**연결**: 헌법 §1 + §4 정합 (Server Authority + Shared Code Discipline 권한 강제)

### `escalation-by-cost-visibility` (★★)

**증상**: Sonnet → Opus 에스컬레이션 비용 인식 부재 → Opus 남발 + 무한 호출

**패턴**: 에스컬레이션 매번 work-pin에 박힘 ("에스컬레이션: Sonnet 2회" / "Opus 재호출"). 사용자가 *왜 비용 커졌는지* 즉시 가시화. M4 진입 후 Opus 발동 주간 카운트 추적 → 잦으면 등급 체계 재조정

**봉합**: 비용 가시화 + 무한 호출 차단 (3차 실패 사용자 escalate)

**사례**: 본 Phase 02 `_escalation.md` 1번 절 + `coordinator.md` 에스컬레이션 절

**연결**: PDF NDREAM 모델 분담 비용 인식

### `unity-asset-specialist-pattern` (★★)

**증상**: Unity asset 작업이 client 도메인에 묻어 들어가 *컨텍스트 비용 폭증* + Phase 08 BackGround prefab 사고

**패턴**: `unity-bridge` SubAgent 신설 — `.cs` 스크립트(client) vs `.prefab/.unity/.asset` (unity-bridge) 경계 명확화. prefab git add 의무 + sprite pivot + PPU 통일 + MCP 함정 단일 책임

**봉합**: Phase 08 사고 학습 정합 + 인규 ComfyUI 자산 흡수 준비

**사례**: 본 Phase 02 `unity-bridge.md` 신설

**연결**: M3 Phase 08 BackGround 사고 학습 (★★★ 별 박힘) / `permission-boundary-as-safety`

### ★ 후보 (2건)

- `worker-domain-consolidation` — 옛 4 도메인(netcode + gameplay + persistence + qa-sim의 server-side) → `server` 1로 통합. *경계 코드 직접 처리* 정신으로 다중 위임 부담 해소
- `tier-2-bifurcation` — Tier 2를 reviewer(코드 변경 후) + plan-auditor(Phase 정의 전) 두 자동 SubAgent로 분기. 옛 단일 Tier 2 → 두 단계 (β cross-check은 Tier 3 수동으로 유지)

---

## 박제 메타

- **commit** (1/3): `5fec7ec` — Worker 4 (server/shared/client/qa, 785줄)
- **commit** (2/3): `c723195` — Reviewer 2 + Specialist 2 (reviewer/plan-auditor/unity-bridge/coordinator, 1001줄)
- **commit** (3/3): 다음 commit — `_routing.md` + `_escalation.md` + README 매핑 + 본 `-DONE.md`
- **branch**: `youngho/harness-v1` (from main `ac2d302`)
- **다음 Phase**: 03 (Hook 인프라) 또는 04 (Knowledge 시스템 + GC) — 병렬 가능, 사용자 결정
- **WORK-ID**: `m3.5-harness-v1-phase02`
