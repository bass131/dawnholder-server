# SubAgent Routing — 풀 9 라우팅 + 자동 호출 + 에스컬레이션

> **헌법 참조**: 본 정책은 새 헌법 v1 "🤖 SubAgent 풀" 섹션에서 링크됩니다.
> 충돌 시 헌법이 이깁니다.
>
> **신선도 주의**: 본 정책은 M3.5 박힘 시점(2026-05-20) 실측 0건. **M3.6 Phase 03-A 첫 실측 사이클 (2026-05-22)** — 5 항목 모두 0건 또는 간접 증명 박힘 (위임 false hit 0 / 재귀 마찰 0 — 단 재귀 차단 Hook 부재 발견 = work-pin 별 시점 / 에스컬레이션 0 / plan-auditor 가치 M3.6 plan 통과로 증명 / unity-bridge 영역 효과 간접 증명). 본문 수정 항목 없음.

본 문서는 SubAgent 풀 9개의 *라우팅 룰*과 *자동 호출 트리거*, 그리고 *에스컬레이션*(Sonnet 2회 실패 → Opus → 사용자)을 정의합니다. SubAgent 정의 자체는 Phase 02/04 산출물(`../agents/<name>.md`).

---

## 1. SubAgent 풀 9 (요약)

| # | 이름 | 역할 | 모델 | 권한 |
|---|---|---|---|---|
| 1 | `server` | 02_Server/ + 98_Shared/ 서버측 (게임플레이/네트워킹/영속화) | Sonnet | 02_Server/ + 98_Shared/ R/W |
| 2 | `shared` | 98_Shared/ 단독 (Protocol/공식/공유 상수) | Sonnet | 98_Shared/ R/W, 04_ClientNet/ R |
| 3 | `client` | 03_Client/ + 04_ClientNet/ (Unity 씬/렌더링/입력/UI/prediction) | Sonnet | 03_Client/ + 04_ClientNet/ R/W, 98_Shared/ R |
| 4 | `qa` | 99_Tools/ + 테스트 코드 (헤드리스 봇/부하/퍼징) | Sonnet | 99_Tools/ + 테스트 R/W, 게임 코드 R only |
| 5 | `reviewer` | Tier 2 자동 리뷰 (헌법/ADR/도메인 패턴 점검) | Opus | 전체 R only |
| 6 | `plan-auditor` | _milestone-plan.md / Phase 정의 사전 검증 (Codex γ 흡수) | Opus | 전체 R only |
| 7 | `unity-bridge` | Unity Editor MCP + asset + scene/prefab 작업 전담 | Sonnet | 03_Client/ + Unity MCP |
| 8 | `coordinator` | 복잡/대규모 Phase 분해 + Worker 위임 + 결과 통합 | Opus | 전체 R only, 위임 권한 |
| 9 | `knowledge-gc` | `.claude/knowledge/` 캐시 정리 (비활성화/응축/승격 후보/분해) — *수동 트리거만* | Sonnet | `.claude/knowledge/` R/W, 다른 영역 R only |

옛 6 도메인(`netcode`/`gameplay`/`client`/`content`/`persistence`/`qa-sim`) → 새 9 SubAgent 매핑은 `../README.md` 옛 → 새 매핑 표 참조 (`knowledge-gc`는 옛 대응 없음, Phase 04 신설).

각 SubAgent 디테일(입력/출력/툴 권한) = [`../agents/<name>.md`](../agents/) (Phase 02 산출물).

---

## 2. 라우팅 — 도메인 → SubAgent

작업이 들어오면 메인 세션(or coordinator)이 *도메인 → SubAgent* 매핑으로 위임:

| 도메인 / 작업 | 위임 대상 | 비고 |
|---|---|---|
| 패킷 모양 / 직렬화 / 프레이밍 / 연결 라이프사이클 | `shared` + `server` | PDL은 `shared`, 핸들러는 `server` |
| 전투 / 스킬 / 스탯 / 공식 / AI / 영속화 | `server` | 게임플레이 + 네트워킹 + DB 통합 |
| Unity 씬 / 렌더링 / 입력 / UI / prediction | `client` | |
| Unity prefab / asset / scene YAML | `unity-bridge` | MCP 도구 전담 |
| 헤드리스 봇 / 부하 / 퍼징 / 테스트 | `qa` | 게임 코드 R only |
| ComfyUI 자산 / 2D 스프라이트 import | `unity-bridge` (인규 영역 보조) | unity-asset 깃발 발동 |
| 헌법 / ADR / policies / 하네스 자체 | (위임 X, 영호 단독) | M3.5 약속 |

### 여러 도메인 작업

2 도메인 이상 = **복잡 등급** 이상 → `coordinator`에게 위임:

1. `coordinator`가 Phase 분해 (또는 받은 분해본 검증)
2. 도메인별 Worker 위임 (1단계만)
3. Worker 결과 수집 + 통합
4. (조건 충족 시) `reviewer` 자동 호출

**재귀 차단**: Worker가 다른 Worker를 *직접 호출 X*. 분해는 coordinator 책임.

---

## 3. 등급 → 처리 패턴 (재확인)

[`grade-and-risk.md`](grade-and-risk.md) 등급 정의에서 처리 패턴이 결정됩니다:

| 등급 | 처리 패턴 |
|---|---|
| **단순** | 메인 세션이 Edit/Write 직접. SubAgent 위임 X (위임 비용 > 작업 비용) |
| **보통** | 도메인 Worker 1개에 위임 (예: `server` 단독) |
| **복잡** | `coordinator` + Worker 1~2개 + `reviewer`(조건부) |
| **대규모** | `coordinator` + Worker 3~4개 + `plan-auditor`(사전) + `reviewer`(통합) + 5단계 보고 MD/HTML |

---

## 4. 자동 호출 트리거

다음 SubAgent는 *메인 세션 판단 없이* 자동 발동:

### 4-1. `reviewer` (Tier 2 자동 리뷰)

도메인 Worker 코드 변경 후 메인 세션이 다음 평가:

- **무조건 호출**: `98_Shared/` 변경 / 새 핸들러·패킷·공식 / 사용자 "리뷰 돌려줘" 명시
- **조건부 호출**: 실질 변경 ≥10줄 → 호출, <10줄 → 스킵
- **무조건 스킵**: 테스트 파일만 / 주석·rename만 / 사용자 "리뷰 스킵해줘 + 사유"

트리거 디테일 = [`review-tiering.md`](review-tiering.md).

### 4-2. `plan-auditor` (Phase 정의 사전 검증)

- `_milestone-plan.md` Write/Edit → 자동 호출
- `01_Phases/**/NN-{slug}.md` Write/Edit (Phase 정의) → 자동 호출
- 출력: 결함 발견 시 사용자에게 리스트 + 옵션 B(즉시 봉합) / 옵션 A(스킵 후 진행)

Codex γ 방식(4~7회 실측)에서 *코드 박기 전 설계 검증* 패턴 흡수. γ 6/7회차 HIGH 2 + MEDIUM 3 봉합 시간 절감이 가치 증명.

### 4-3. `knowledge-gc` (*자동 호출 X — 수동 트리거만*)

- AI 자율 박제 차단 (`ai-self-reinforcement-bias-prevention` 학습 정합) — Knowledge 캐시 변경은 *사용자 확인 게이트* 의무
- 발동 경로:
  - `/harness-review` 슬래시 (수동) — 본인이 점검 중 권유 시
  - `/session:end` 마감 시 권유 (사용자가 GO 줄 때만)
  - 사용자 명시 요청 ("knowledge 정리 부탁")
- 작업: 비활성화 / 응축 / 승격 후보 식별 / 분해 (Phase 04 산출물 명세 = `../agents/knowledge-gc.md` + `../knowledge/_usage.md`)

### 4-4. 그 외 (수동 위임)

- `server`/`shared`/`client`/`qa`/`unity-bridge`: 메인 세션 또는 coordinator가 명시 위임
- `coordinator`: 복잡/대규모 등급 자동 호출 (등급 결정 직후)

---

## 5. 에스컬레이션 룰

Worker가 2번 실패하면 *모델 상향*:

```
[Worker 1차 시도 — Sonnet]
   │
   ├─ 성공 → 결과 반환
   │
   ├─ 실패 (빌드 깨짐 / 테스트 0건 / 명세 미달)
   │
   ├─ [Worker 2차 시도 — Sonnet, 같은 SubAgent]
   │   ├─ 성공 → 결과 반환 + work-pin에 "에스컬레이션: Sonnet 2회" 박힘
   │   │
   │   ├─ 실패 → [3차 시도 — Opus, 같은 SubAgent 재호출 또는 coordinator로 승격]
   │   │   ├─ 성공 → 결과 반환 + work-pin에 "에스컬레이션: Opus" 박힘
   │   │   └─ 실패 → 사용자에게 escalate
```

### 에스컬레이션 박힘 = 비용 인식

work-pin에 박히는 이유 — Opus 호출 비용이 Sonnet 대비 크므로 *얼마나 자주 발동되는지* 가시화. M4 진입 후 빈도가 잦으면 본 정책 재조정(예: 처음부터 Opus 위임 등급 추가).

### 사용자 escalate 양식

```
⚠️ Worker 에스컬레이션 — 3차 시도 후에도 실패
  SubAgent: <name>
  작업: <한 줄>
  실패 사유: <마지막 시도 에러 한 줄>
  옵션:
    1) 본인이 직접 코드 들어가기
    2) 다른 SubAgent에 재위임 (예: server → coordinator로 분해 요청)
    3) Phase 분해 재검토 (작업 단위가 너무 컸을 가능성)
```

---

## 6. 위임 경계 — 약속

### Coordinator → Worker 1단계만

- Worker는 *다른 Worker를 호출 X*. 분해 필요하면 결과에 "분해 요청" 표기 → coordinator가 받아서 재분해
- 재귀 차단으로 무한 호출 사고 예방 (한국 게임 회사 표준 패턴)

### Worker 권한 범위 외 작업

- Worker가 권한 범위 외 파일 수정 시도 → 즉시 거부 + coordinator에게 보고
- 예: `client` Worker가 `02_Server/` 파일 수정 시도 → 권한 부재 → coordinator에게 "server Worker 필요" 보고

### Reviewer/plan-auditor R only

- 두 Opus SubAgent는 *읽기만*. 수정 권고는 메인 세션 또는 도메인 Worker 책임
- 권고 → 사용자 확인 → 도메인 Worker 재위임 흐름

---

## 7. 함정 / 주의사항

- **단순 등급에 위임하지 마라** — 위임 비용 > 작업 비용. 메인 세션 직접이 더 빠름
- **여러 도메인 = 무조건 coordinator** — 메인 세션이 직접 분해 시 문맥 손실 사고 발생. coordinator는 *분해 전문가*
- **Sonnet/Opus 모델 비용 인식** — Opus는 비싸다. 에스컬레이션 발동 시 work-pin에 박힘으로 *왜 비용 커졌는지* 즉시 가시화
- **unity-bridge 단독 영역** — Unity prefab/asset 사고(Phase 08 BackGround 사고)는 일반 Worker가 다루면 위험 ↑. MCP 도구 전담

---

## 8. 변경 시 동기화 책임

본 정책 수정 시 *반드시* 함께 갱신:

- [`../CLAUDE.md`](../CLAUDE.md) "🤖 SubAgent 풀" 섹션 (헌법 본문 표와 정합)
- [`../agents/`](../agents/) (SubAgent 정의 9개 — Phase 02 산출물 8 + Phase 04 신설 `knowledge-gc` 1)
- [`grade-and-risk.md`](grade-and-risk.md) (등급 → 처리 패턴 매핑)
- [`review-tiering.md`](review-tiering.md) (reviewer 자동 호출 트리거)
- [`../hooks/circuit-breaker.sh`](../hooks/circuit-breaker.sh) (Phase 03 산출물 — *반복 도구 사용 알림* advisory. 재귀 차단은 hook 아닌 coordinator 단독 위임 구조로 강제 — line 6 신선도 주석 정합)

---

## 9. 실측 후 재조정 항목

본 정책은 *추측 기반*. M4 진입 후 첫 1주 안에 다음 관찰 → 명세 갱신:

- [ ] **위임 false hit** — 메인 직접이 더 빠른데 SubAgent 위임된 빈도
- [ ] **재귀 차단 마찰** — Worker가 다른 도메인 작업 필요 발견 시 coordinator 거치는 비용
- [ ] **에스컬레이션 빈도** — Sonnet 2회 실패 → Opus 발동 주간 카운트. 잦으면 처음부터 Opus 등급 후보
- [ ] **plan-auditor 가치** — 사전 검증으로 봉합한 결함 vs 후속 사고 비율 (γ 6/7회차 가치 정량화)
- [ ] **unity-bridge 단독 영역 효과** — prefab 사고 0건 유지 검증

재조정 결과는 본 정책 직접 수정 또는 ADR-023 신설(변경 폭에 따라).

---

## 갱신 이력

- 2026-05-20 — M3.5 Phase 01 (2/2)에서 신설. 5/20 의논 결과(SubAgent 풀 8 + 모델 분담 + 에스컬레이션 룰) 박힘. 옛 헌법 6 도메인 라우팅 표를 본 정책으로 외부화 + 신설 4개(`shared`/`plan-auditor`/`unity-bridge`/`coordinator`) 흡수.
- 2026-05-22 — M3.5 후속 봉합 (β cross-review #3). 풀 8 → 풀 9 갱신 (`knowledge-gc` Phase 04 신설 항목 누락 봉합) + §4-3 수동 트리거 명세 박음.
