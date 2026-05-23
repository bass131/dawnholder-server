# Cross-Review γ 비교 — 2026-05-23 — M4.1 Phase 02·03·04

> **본 파일**: α (Claude reviewer + 메인 통합) + β (Codex 본인 별 세션 호출) γ 비교 산출물. `/cross-review` 슬래시 Step 5 정합.
>
> **입력**:
> - α (Claude): Phase 02 reviewer Tier 2-A GO + Phase 03 reviewer Tier 2-A GO + Phase 04 본인 검증 (회귀 검증 미박힘 상태)
> - β (Codex): [`2026-05-23-cross-review-m4.1-phase02-04-codex.md`](2026-05-23-cross-review-m4.1-phase02-04-codex.md)
> - prompt: [`2026-05-23-m4.1-phase02-04-codex-prompt.md`](2026-05-23-m4.1-phase02-04-codex-prompt.md) (Claude → Codex 의뢰 양식)
> - pre-review 자료: [`2026-05-23-claude-pre-review-m4.1-phase02-04.md`](2026-05-23-claude-pre-review-m4.1-phase02-04.md)

---

## 1. 변경 범위

- **마일스톤**: M4.1 Combat Integrity & Portfolio Hardening (대규모, 캡스톤 1 발표 6/10, 마감 6/4)
- **본 cross-review 점검 영역**: 본 세션 5 commit (`3f1d45c` / `0586dfe` / `fc15e77` / `21d2cfb` / `2ac1a8f`)
- **main 대비 diff**: 48 파일 / 1993 insertions / 189 deletions
- **등급**: 마일스톤 풀세트 cross-review (대규모 → 외부 시각 권장 시점)

---

## 2. α/β 정합 비교 표

| 차원 | α (Claude reviewer + 메인) | β (Codex 본인 직접 호출) | γ 결정 |
|---|---|---|---|
| 헌법 §1~§5 점검 | Phase 02/03 5축 PASS / Phase 04 reviewer 미호출 | 같은 정신 + STOP급 결함 없음 | 일치 |
| Phase 02 race elimination | 정합 | 정합 + Low 잠복 1건 (silent drop 문서 표현) | β 우위 (정밀화) |
| Phase 03 옵션 B 변형 third path | "정합 + ★★★ 어필 가치" | "부분 정합 + 어필 보강 필요" | **β 우위** — drift guard 비대칭 + contract test 필요 |
| Phase 04 deterministic build | "다른 환경 hash 보장" | "같은 환경 같은 소스 한정, cross-machine은 별 가닥" | **β 우위** — 표현 완화 의무 |
| false-promise 잠복 | 인지 X | **4건 새 발견** | β 단독 우위 (외부 시각의 진짜 가치 증명) |
| SubAgent 자율 commit | 별 시점 결정 | 정합 + prompt 계약 보강 권장 | 일치 |
| 종합 PR 머지 자신감 | GO 추정 | 조건부 GO (3건 봉합 권장) | β 채택 |

---

## 3. 발견 분류 — γ 통합

### 3.1. 양쪽 다 잡음 (=  큰 흐름 정합)

α + β 둘 다 헌법 5축 통과 + 코드 자체 STOP급 결함 0건 + Phase 02 event-based race 본질 봉합 + Phase 03 fail-closed Disconnect 정합 + Phase 04 deterministic 방향 정합. 본 영역은 PR 머지 자신감 ↑ 근거.

### 3.2. α만 잡음 (= 0건 실제 결함)

α가 β보다 더 잡은 *결함* = 0건. 하지만 α는 옵션 B 변형 third path를 "★★★ 어필 가치"로 박았는데 β가 그 평가를 부분 부정 (자기 과신 영역).

### 3.3. β만 잡음 (= 봉합 권장 5건, 즉시 처리 3건 + 별 시점 2건)

| ID | 위치 | 박힌 결함 | γ 분류 | 처리 |
|---|---|---|---|---|
| **B1** | Phase 03 옵션 B 변형 contract test 부재 | drift guard가 서버 측만, 클라 측 자동 검출 X = "동등 보호" 약속의 마지막 10% 남음 | **🔴 즉시 봉합** | ✅ 본 cross-review 후속에서 `FrameValidatorSymmetryTests` 신설 (이번 commit) |
| **B2** | `04_ClientNet/FrameValidator.cs:5` 주석 | 서버 counterpart를 `Session.cs`로 잘못 가리킴 (실재 `02_Server/Network/FrameValidator.cs:1`) | 🟡 보조 봉합 | 별 시점 (cross-reference 주석 정정, ~5분) |
| **B3** | 4건 false-promise 잠복 | `98_Shared/CLAUDE.md:19` Current=3 stale / `CLAUDE.md:101` slnx 본문 / `02_Server/CLAUDE.md:11` ServerCore 위치 / `04_ClientNet/CLAUDE.md:12` Layout FrameValidator 누락 | **🔴 즉시 봉합 (cadence 정신)** | ✅ 본 cross-review 후속에서 4건 sweep (이번 commit) |
| **B4** | Phase 04 deterministic 주석 "다른 환경 보장" | 과한 표현 — cross-machine은 별 가닥 | **🔴 즉시 봉합 (주석 1줄)** | ✅ 본 cross-review 후속 (이번 commit) |
| **B5** | Phase 04 CI `ContinuousIntegrationBuild=true` 옵션 부재 | CI 환경 진입 시 정석 옵션 | 🟢 backlog | 별 시점 (CI 환경 아직 없음, M5+ 또는 LAN/EC2 진입 시 박음) |
| **B6** | Phase 02 silent drop 문서 표현 | "잘못된 순서 패킷은 전부 disconnect" 박힌 곳이 실재와 다름 (character select 전 MoveIntent/Attack는 silent drop) | 🟡 보조 봉합 | 별 시점 (Phase 02 -DONE.md 또는 02_Server/CLAUDE.md 문서 표현 정밀화, ~10분) |
| **B7** | SubAgent 자율 commit prompt 계약 명시 | 다음 SubAgent 호출 시 "commit 금지/허용" 명시 의무 | 🟢 backlog | 별 시점 (이미 work-pin 박힘, 다음 마일스톤 시점에 적용) |

### 3.4. α 자기 과신 영역 (β 보정)

옵션 B 변형 third path = α가 ★★★ 자산화 박은 패턴에 β가 "부분 정합 + 어필 표현 보강 필요" 박음. 봉합 가닥:

- **α가 박은 학습 자산 명**: `option-b-variant-third-path-with-drift-guard`
- **β 보정 후 본질화**: drift guard 클라 측 부재 = "동등 보호" 미완. `FrameValidatorSymmetryTests` 신설(B1 봉합)이 본 학습 자산을 *진짜화*.
- **면접 어필 표현 보강**: "공유 못 해서 복붙" → "production dependency는 분리하되 contract test로 wire invariant를 고정" (β 인용)

본 학습 자산은 이번 commit 후 *진짜 ★★★*로 격상 정합 (드리프트 검출의 진짜 동등 보호 박힘).

---

## 4. 본 cross-review 후속 봉합 (즉시 처리 가닥 B 채택)

### 4.1. 즉시 봉합 3 가닥 (사용자 결정 가닥 B)

✅ **(1) CLAUDE.md 4건 false-promise sweep** (B3)
- `98_Shared/CLAUDE.md:19` Current=3 → Current=4 정정 (M3.8 PR #49 bump 박힌 후 sweep 누락)
- `CLAUDE.md:101` slnx 본문 = `02_Server/`+`98_Shared/`만 → `02_Server/`+`04_ClientNet/`+`98_Shared/`+`99_Tools/` 정정
- `02_Server/CLAUDE.md:11` Layout = `GameServer/Network/ ServerCore + GameSession` → `02_Server/Network/` 별 영역으로 ServerCore 분리 (실재 구조 정합)
- `04_ClientNet/CLAUDE.md:12` Layout = `FrameValidator.cs` 한 줄 추가 (Phase 03 신설 박힘 정합)

✅ **(2) `FrameValidatorSymmetryTests` 신설** (B1)
- 위치: `02_Server/GameServer.Tests/Network/FrameValidatorSymmetryTests.cs`
- 박힘: 상수 동기화 2건 (`MinFrameSize` / `MaxFrameSize`) + Theory 9건 (`zero / below-min × 3 / valid × 3 / above-max × 3` 양쪽 helper 결과 비교)
- `GameServer.Tests.csproj`에 `04_ClientNet` ProjectReference 추가 (contract test 전용, production code 흐름 변경 X 주석 명시)
- 테스트 결과: **24/24 통과** (회귀 0, 신규 11건)

✅ **(3) deterministic 주석 완화 (B4) + ContinuousIntegrationBuild backlog (B5 별 시점)**
- `98_Shared/Shared.csproj` + `04_ClientNet/Dawnholder.Client.Net.csproj` 양쪽 주석 보강:
  - 옛: "같은 소스 + 다른 환경 = 같은 hash 보장"
  - 새: "같은 환경에서 같은 소스 재빌드 시 같은 hash 보장. cross-machine reproducible은 별 가닥, CI 환경 진입 시 `ContinuousIntegrationBuild=true` 옵션 추가 정석"

### 4.2. 별 시점 봉합 (work-pin 박힘)

- B2: `04_ClientNet/FrameValidator.cs:5` cross-reference 주석 정정 (~5분)
- B5: `ContinuousIntegrationBuild=true` CI 환경 진입 시
- B6: Phase 02 silent drop 문서 표현 정밀화 (~10분)
- B7: SubAgent prompt 계약 검증 게이트 (다음 마일스톤)

---

## 5. 결정 — M4.1 Phase 02-04 풀세트 PR 머지 자신감

**β 판정**: 조건부 GO  
**즉시 봉합 3건 처리 후 γ 판정**: **GO** — 본 commit 후 M4.1 풀세트 마일스톤 PR 머지 자신감 풀세트 확보. B2/B5/B6/B7 별 시점 봉합은 PR 머지 차단 사유 X.

---

## 6. 옛 학습 정합 + 본 cross-review 학습 자산

### 6.1. 옛 학습 정합

- **false-promise cadence (ADR-024)**: 본 마일스톤 23+24번째 변종 발본 후 *또 4건* 외부 시각으로 잡힘 = 자기 점검 한계 + 외부 시각 cadence 가치 ★★★ 재증명. ADR-024 cadence 자체가 "본인이 자기 약속을 다 못 잡는다"는 전제로 박힌 정신과 정합.
- **γ 방식 학습 정합 (옛 4~7회차 패턴)**: 본 cross-review가 γ 방식 8회차. β가 "코드 자체 STOP급 결함 X + 약속/안전망 마지막 10%" 잡는 패턴 = γ 방식의 *정상 작동* 케이스 (큰 결함 X 시점에 외부 시각의 보강 가치).

### 6.2. 본 cross-review 새 학습 자산 (★★★ 후보)

1. **`drift-guard-asymmetry-is-incomplete-equivalence`** ★★★ — 옵션 B 변형(공유 vs 분리 third path)이 진짜 헌법 #4 동등 보호이려면 *양쪽 모두 검증하는 contract test*가 필수. 한쪽만 검출하는 drift guard는 "동등 보호"가 아니라 "부분 보호". 자기 과신 영역 외부 시각이 정확히 잡음.
2. **`self-overconfidence-on-design-decisions-by-ai-assistant`** ★★ — AI 보조 도구가 사용자 결정을 사후 정당화하면서 "★★★ 어필 가치" 박는 패턴 = 자기 과신 위험. 외부 시각(β) 없으면 가짜 학습 자산이 잠복.
3. **`deterministic-build-precise-claim-vs-overpromise`** ★★ — "deterministic + PathMap = 다른 환경 같은 hash" = overpromise. 실재는 compiler 버전 + SDK feature band + 참조 assembly 등 입력 정합이 추가 의무. cross-machine 정신은 별 가닥(CI `ContinuousIntegrationBuild`).

본 ★★★ 자산 1번이 본 마일스톤의 진짜 학습 코어 — 한국 게임 회사 백엔드 면접에서 "헌법 #4 vs 모듈 재사용성 갈등 어떻게 풀었나" 답변 시 *옵션 B 변형 + contract test로 wire invariant 고정* 박는 가닥이 정합 어필.

---

## 7. 작업 로그

- 2026-05-23: γ 비교 산출물 작성. 즉시 봉합 3건(false-promise sweep 4건 + FrameValidatorSymmetryTests + deterministic 주석 완화) 본 commit에 묶음. B2/B5/B6/B7 별 시점.
