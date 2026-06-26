# 하네스 자체 점검 — 2026-06-26 — scope=all

> `/harness-review all`. reviewer(Tier 2-A) + plan-auditor(Tier 2-B) + knowledge-gc 독립 동원 + 양식 비용 실측.
> 읽기 전용 점검 — 본 문서가 유일 산출. 결함은 *제안*만, 봉합은 영호 결정.

## TL;DR
- 🔴 결함 **2** / 🟡 제안 **12** / 🟢 정합 **9축** + knowledge 정리 후보 **8**
- **핵심 진단**: 위험·보안 구멍 **0**. 헌법 절대원칙 5개는 실제로 강제됨(reviewer 축1 🟢). 발견은 전부 **문서-현실 drift** — 두 원인: ① **solo 전환(2026-06-18) 미반영**, ② **M7.5 신규 자산(커맨드 3개·정책 3개·깃발 1종) 헌법 본문 미반영**.
- **양식 비용**: 정합. work-pin 31줄(목표 30~40 ✅), CLAUDE.md 266줄(<350 ✅).

---

## reviewer (Tier 2-A) — 헌법·정책·ADR·훅·커맨드 (🔴1 / 🟡6 / 🟢5)

### 🔴
- **`CLAUDE.md:44` 슬래시 수 stale** — 헌법 "총 10개" vs 실제 **13개**(work4·session4·engine1·점검3·setup1). `commands-index.md`("13개")와 정면 충돌. 헌법=SSOT인데 M7.5 신규(`/engine:goal`·`/session:review`·`/refactor-sweep`) 미반영.

### 🟡
- `hooks/README.md:17` phase-gate 명세 "대규모"인데 훅 실제는 "복잡 이상"(ADR-031 반영). 훅이 더 엄격 = 안전구멍 아님, 문서 lag.
- `shared-discipline-guard.sh:49-71` PDL.xml **단독** 편집은 exit 0 통과(차단은 후속 98_Shared 편집 시만). README는 "비우회 차단"으로 단정 → 약간 과장. `.githooks/pre-commit` 2차망 존재.
- `ADR/INDEX.md:44` ADR-019 superseded 마커에 ADR-032 누락(파일 본문엔 있음).
- `policies/INDEX.md:28-30` loop-driver·work-judge·review-throughput "헌법 참조=P02 추가 예정" stale (헌법 링크 이미 완료됨).
- **(load-bearing) reviewer 6축↔5축 drift** — `REVIEW_CHECKLIST.md`는 6축(축6=God class/SRP, ADR-028)인데 `reviewer.md`는 "5축"으로 박혀 reviewer가 **축6 점검 누락** 소지. ADR-028이 봉합한 Phase 07 God class 누락이 reviewer 프롬프트 단에서 재발 가능.
- `REVIEW_CHECKLIST.md:116-117` -DONE/5단계 누락=🔴이 모든 Phase 적용처럼 기술 → 단순/보통(ADR-031: -DONE 자체 없음)에서 false-positive.

### 🟢
축1 절대원칙 5개 실재 강제 / 축2 정책11 INDEX↔파일 일치 / 축3 ADR superseded 마커 / 축4 훅 배선 9종 일치 / 축5+신선도(6개월 초과 0건).

---

## plan-auditor (Tier 2-B) — SubAgent 풀·라우팅 (🔴1 / 🟡6 / 🟢4)

### 🔴
- **`unity-bridge` MCP 권능 허위** — 4개 문서(CLAUDE.md row7·`subagent-routing.md`·`_routing.md`·`unity-bridge.md`)가 "Unity MCP 전담" 선언하나 frontmatter `tools`에 `mcp__unity__*` **미보유** → 호출 불가. memory `unity-mcp-tools-in-main-session-not-unity-bridge`로 **이미 실측된 갭**(손YAML fallback = prefab 사고 위험). "메인 세션이 MCP 실행, unity-bridge는 asset 손편집만"으로 정정 필요.

### 🟡
- `coordinator` "위임 권한" 선언 vs tools에 위임도구 없음 — 실행모델("메인이 Worker 호출")과 prose 불일치.
- **harness 위험깃발이 CLAUDE.md 깃발 목록 누락** — grade-and-risk·risk-detector는 4종(+harness), CLAUDE.md·plan-auditor는 3종.
- `reviewer-auto-trigger.sh`가 unity-asset(`.prefab/.unity/.asset`) 깃발 미커버 — "무조건 호출"인데 Hard hook 밖.
- plan-auditor 자동호출 hook 부재 → reviewer Hard hook과 비대칭(Phase 정의 Write가 어떤 훅도 발동 안 함).
- 다수 공동소유 R/W 경계(98_Shared=server+shared, PacketGenerator=shared+qa 등)가 `_routing.md` 표에 미반영.
- CLAUDE.md "qa 게임코드 R only" vs `qa.md` 데이터값(GameData/Tables 등) R/W 불일치 — §4 의무 nuance 누락.

### 🟢
축2 재귀차단 4중 명문화 / 축5 절대원칙 권한위반 0(client 98_Shared=R only ✓) / 축1 풀9 완전일치 / 축4 등급·Opus 룰 4문서 일관.

---

## knowledge-gc — 정리 후보 8 (전부 제안, 자동정리 X)

도메인: server1·shared3·client2·qa0·cross-cutting7 = 활성 13항목. 크기 문제 없음.

- **결함 정정 4 ← 가장 시급**: solo 전환이 knowledge 미반영. `unity-version-hash-pinning`(정유현·인규 언급), `projectsettings-cloud-ping-pong`(유현 Cloud), `riot-vanguard-spawn-unknown`(인규/정우 합류 전제), cross-cutting 스테일 플래닝 노트(Phase 04 흡수계획).
- **응축 2**: cross-cutting prediction 3항목 우산화 / memory 중복(hash-pinning·sac-block) 경량 응축.
- **승격 2**: `false-promise-pattern`→헌법 §4 본문 한 줄 / `gamma-pre-validation`→"승격완료(plan-auditor)" 표기.
- **구조 1**: qa `_index.md` 플레이스홀더 텍스트 stale.

---

## 양식 비용 평가 (정량)

| 지표 | 실측 | 목표 | 판정 |
|---|---|---|---|
| work-pin 줄수 | 31 | 30~40 | 🟢 (단 라인 밀도 높음) |
| CLAUDE.md 줄수 | 266 | <350 | 🟢 |
| 정책 수 | 11(+INDEX) | — | 🟢 |
| -DONE.md | 121건 (최대 370줄) | 단위문서=자르지않음 | 🟢 정책 정합 |
| HTML 시각화 | 56건 | 복잡이상 박제 | 🟢 |

양식 과부하 징후 없음. work-pin 31줄로 압축 목표 유지 중(M3.5 다이어트 정신 정합).

---

## 결정 권유

위험 0이라 즉시 봉합 의무 없음. 그러나 두 원인(solo 미반영 / M7.5 미반영)이 명확해 **한 번의 doc-sync sweep로 다수 동시 봉합 가능**:

- 🟡 **번들 A — 하네스 doc-sync sweep** (단순/보통, 브랜치): CLAUDE.md 커맨드수(10→13)+harness 깃발 추가 / README phase-gate·shared-discipline 갭 명시 / INDEX 마커 2건(ADR-019·"P02") / reviewer.md 5축→6축. → 헌법·문서 정합 일괄.
- 🟡 **번들 B — knowledge solo-reconcile** (단순, knowledge-gc 위임): 결함정정 4 + qa 플레이스홀더. 팀원 언급 일반화.
- 🟠 **별건 surface — unity-bridge MCP 재정의** (설계 결정): "MCP=메인 전담, unity-bridge=손편집 asset" 명문화 or MCP 워크플로우 메인 이관. memory와 이미 일치 = 결정만 남음.
- 🟢 그 외(공동소유 표·plan-auditor hook 비대칭·승격 후보) = 하네스 v2 백로그.

> 헌법/문서 수정 = harness 깃발(영호 단독 통제). 본 점검은 제안까지 — 봉합 GO는 영호.
