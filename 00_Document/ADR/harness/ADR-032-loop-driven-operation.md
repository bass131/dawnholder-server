# ADR-032 — Loop-driven 운영 모드 (사람=방향+판단, 엔진=/goal+Workflow)

- **상태**: accepted (영호 결정 2026-06-18 — **v1 adopt / v2 defer**). ⚠️ *결정만 박제 — 구현 sweep은 후속 `/work:plan` 대규모 마일스톤*
- **날짜**: 2026-06-18
- **결정자**: 유영호 (하네스 단독 통제)
- **관련**:
  - **확장** ADR-031 (Phase 자동 진행 + 4종 Stop) — *supersede 아님*. 본 ADR이 append-only로 일반화·확장.
  - **supersede** ADR-022 (새 하네스 v1 — *운영모드 레이어만*, 부품 정의는 보존) / ADR-019 (reviewer 정적 트리거 매트릭스 → throughput 모델) / ADR-016 (Notion 3자 분업 → Claude 단독) / ADR-023 (자동 진행 중 work-pin drift 동기 *시점* 재정의 — 발견 게이트 보존)
  - **인용·재사용** ADR-029 (WSL2 게이트 = 루프 done 판사) / ADR-028 (CODE_CONVENTION = refactor-sweep 진단 룰북) / ADR-025 (학습 트랙 은퇴 = 본 ADR 이해 게이트의 선행 정신 — §결과에서 부분 재방문) / ADR-015 (**supersede 아님** — "학습 호흡 보존" 명분은 ADR-031이 *이미* 폐기, 본 ADR은 재확인만 = supersede 이중 사망 회피)

---

## 맥락 (왜 바꾸나)

발표(1학기) 종료 후 프로젝트 점검 모드에서, Addy Osmani **"Loop Engineering"**(사람이 매 스텝 프롬프트하던 걸 *시스템이 대신*, 사람은 방향+판단만)을 우리 하네스에 적용하기로 함.

실측 결과 우리 하네스는 **"loop-ready인데 loop-less"** — 자율 루프의 어렵고 드문 부품은 이미 다 있다:

- **done 판사** = WSL2 회귀 게이트(ADR-029) — 기계가 통과/실패 판정
- **정지 게이트** = "비가역은 영호 GO"(pr-and-merge-gate)
- **maker-checker** = reviewer SubAgent(ADR-019) + cross-review
- **메모리** = work-pin(ADR-025) + knowledge + CHANGELOG
- **자동화 半구현** = refactor-sweep(무인 진단·수정·전용 브랜치 commit)
- **4종 Stop + 자동 진행** = ADR-031 (이미 loop의 절반을 선취)

빠진 건 **① 범용 루프 드라이버(엔진)와 ② "무엇을 루프에 맡기나" 명시적 분류**뿐. 그리고 동기 = AI 생산속도를 사람이 못 따라가는 **throughput 병목** — 사람이 모든 산출물을 리뷰하면 직렬 병목이 됨. 역할을 *방향+판단*으로 전환해 천장을 높인다.

**학부생 학습 목적 보존**: 사용자 목적은 "MMORPG 만들며 백엔드/네트워킹 실력 키우기". 루프가 일을 *대신*하되 사용자가 engineer로 남도록(button-pusher 아님) 설계에 이해 게이트를 박는다. (Addy: *"remain an engineer, not the person who pushes run. Verification is still yours."*)

## 결정

### A. 운영 모드 전환

| 항목 | 전 | 후 (loop-driven) |
|---|---|---|
| 작업 구동 | 사람이 매 스텝 프롬프트 | **사람 = 방향(목표·done 조건) + 판단(게이트)만**. 엔진이 매 스텝을 대신 구동 |
| 엔진 | (없음) | **`/goal`**(조건 충족까지 자율 반복, Haiku 평가자가 트랜스크립트로 done 판정) **+ Workflow**(구조적 fan-out·pipeline·예산상한). coordinator는 Workflow의 부분 구현으로 인용 |
| 기동(cadence) | 수동 세션 | **v1**(adopt): attended — 터미널 또는 `claude remote-control`(폰/웹 조종). **v2**(target, 본 ADR은 *미adopt*): Desktop scheduled task(무인). 둘 다 **PC-on 전제** — 로컬 WSL2 게이트가 done 판사라 클라우드 Routines는 탈락 |

### B. work 3버킷 판정자(judge) — ADR-031의 4종 Stop을 분류 축으로 명시화

| 버킷 | 판정자 | 처리 | risk-detector 깃발 |
|---|---|---|---|
| **(a) 기계 판정** | 빌드·테스트·WSL2 회귀(ADR-029)·정적분석 | **루프 자율** | 무깃발 |
| **(b) 취향·육안** | 사람(아트·사운드·Unity 외관) | **사람 트랙(병행, 안 막음)** | unity-asset |
| **(c) 판단·비가역** | 사람(설계 분기·push/PR/merge·`Protocol.Version`·trust-boundary) | **사람 게이트(Stop)** | irreversible / trust-boundary |

> (a)=ADR-031 "공학 게이트 자동 진행", (c)=ADR-031 "4종 Stop". 즉 본 분류는 *새 규칙이 아니라 ADR-031을 판정자 축으로 재서술*. risk-detector 3깃발이 1차 분류기.
>
> ⚠️ **버킷 (c) *물리적* 강제는 v1(attended)에서 사람 게이트로 성립**. risk-detector가 advisory(차단 X)라, **v2(무인)는 깃발→사람게이트 자동 적재 hook이 선결**(§한계·미결1) — 그전까지 버킷은 v2에서 *서류상 분류*라 v2 미adopt. (plan-auditor 축1 🟡)

### C. 아트 두 트랙 (버킷 b의 이음새)

루프는 아트를 하드코딩하지 않고 **"이름 붙은 아트 슬롯 + 스펙(크기·pivot·프레임·포맷)"에 placeholder**를 꽂는다. 사람이 *같은 슬롯에 실아트*를 교체(코드 무변경). 아트 *구조·wiring*=루프(기계 검사), *내용·취향*=사람. **pending-art manifest**(원장)로 placeholder rot 방지 — ADR-024 false-promise cadence의 감사 대상.

### D. 이해 게이트 (학습 목적 보존)

- **게이트-이해**(책임지고 GO할 만큼)는 루프에 *남는다*. **깊은 학습**(어떻게/왜 구현)은 *별도 pull 세션*으로 분리 — 루프는 구현 처리량 단일 목표.
- **pending-comprehension 원장**: 사용자가 아직 깊게 안 판 항목의 가시 목록(인지적 항복을 숨기지 않음). ⚠️ ADR-025가 학습 트랙 B를 죽인 이유("쌓이기만 하고 pull 안 됨")를 본 ADR이 정면 인용 — *이번엔 pull-분리 + 가시 원장*이라 다르다는 논증 필요(트랙 B 부활 아님).

### E. 리뷰 throughput 모델 (병목 해소)

사람이 *모든* 산출물을 리뷰하지 않는다:
- **예외 기반**: 고위험·신규·flagged + 샘플만 사람. 나머지는 기계 게이트 + AI 리뷰가 통과(일부 산출물은 *의식적으로* 완전 리뷰 포기 = 잔여 위험 수용).
- **통합 고도**: PR 100개가 아니라 *통합 이야기 하나*를 리뷰. 루프는 coherent 단위로 묶음.
- **신뢰 졸업**: 안전 증명된 카테고리는 개별 정독 없이 배치 GO.
- **시선 배분 = `max(위험, 학습가치)`**: 보일러플레이트는 빠르게, 새 아키텍처·까다로운 네트워킹은 깊게.

### F. refactor-sweep = 범용 드라이버의 첫 검증된 인스턴스

폐기 아님. refactor-sweep Step0~5 골격(전제게이트→진단 fan-out→Worker→회귀 게이트→재검증→리포트 + G1~G9 안전 가드)을 **도메인 무관 드라이버로 추출**, refactor-sweep는 그 드라이버의 *"refactor 모드 프리셋"*으로 재정의.

### G. 재사용 부품 (죽이지 않음)

SubAgent 9(Worker/checker) · WSL2 게이트(ADR-029) · GO 게이트(pr-and-merge-gate) · knowledge 캐시 · hooks · work-pin. **ADR-022가 이들의 정의처라, 그 운영모드만 supersede하고 부품 정의는 보존.**

## 결과

- **얻음**: 사람이 매 스텝에서 빠짐(throughput 천장 ↑), 자율 진행 가능분은 게이트까지 무중단, 학습은 pull로 분리해 흐름 안 끊김.
- **잃음**: 산출물 전수 리뷰의 세밀함 → 예외기반 + 신뢰졸업 + `max(위험,학습가치)` 시선 배분이 대체(잔여 위험 의식적 수용).

### supersede / 갱신 (전부 append-only — 옛 ADR 파일 rewrite 0, 상태줄 한 줄만)

| ADR | 무엇이 덮이나 | 보존 |
|---|---|---|
| ADR-022 | KPI 운영모드(Planning→구현→보고), Phase 수동 진행 명분 | 등급4·SubAgent9·Hook·Knowledge·work-pin **정의 전부 보존** |
| ADR-019 | 정적 트리거 매트릭스(무조건/조건부/스킵) → 예외기반·신뢰졸업 | 5축 reviewer 본체·읽기전용 권한 보존 |
| ADR-016 | 본인 회고 축(ADR-025) + Codex 재편집 축(memory) → Claude 단독 | (사실상 1자만 생존) |
| ADR-023 | 자동 진행 중 work-pin drift 동기 *시점* | `/session:start` drift 발견 게이트 본체 보존 |
| **ADR-031** | (supersede 아님 — **확장**) | 4종 Stop=버킷(c), 자동진행=버킷(a) 그대로 |
| **ADR-015** | (supersede 아님 — **재확인**) | "학습 호흡" 명분은 ADR-031이 *이미* 폐기 — 이중 사망 회피, 인용만 |

### sweep 동반 (dangling 0 — 한 묶음 atomic)

- **신규 파일**: `ADR-032`(본 파일) · `policies/loop-driver.md` · `policies/work-judge.md`(or grade-and-risk §3 흡수) · `policies/review-throughput.md`(or review-tiering 확장) · `.claude/commands/{loop,goal}.md`(드라이버 슬래시) · `.claude/state/{pending-art,pending-comprehension,pending-knowledge}.md`(원장 3종) · (선택) 신규 hook(`loop-done-gate`/manifest guard — 일부는 risk-detector 확장으로 대체 가능)
- **REVISE(in-place)**: `CLAUDE.md` 6절(작업 보고/작업 좌표/Phase 진행/작업 등급/SubAgent 풀/Knowledge) · `policies/{reporting-format,pin-and-done,review-tiering,subagent-routing,grade-and-risk}.md`(5개 **강결합 atomic**) · `pr-and-merge-gate.md`(settings 권한 승격 ↔ §5 ask 매처 정합 확인 — plan-auditor 축3 🟡) · `agents/{coordinator,reviewer,plan-auditor,knowledge-gc,_routing,_escalation}.md` · `commands/{refactor-sweep,session/start,session/end,work/plan}.md` · `hooks/{circuit-breaker.sh,README.md}` · `settings.json`+`settings.local.json`(권한 승격) · `knowledge/{README,_usage}.md`
- **카탈로그 3종 atomic**: `policies/INDEX.md` · `ADR/INDEX.md`(harness 행 추가 **+ ADR-016/023 supersede 표기**) · `commands-index.md`(이미 10 vs 11 drift) — 셋이 한 묶음
- **옛 ADR 본문 상태줄**(append-only — rewrite 아닌 한 줄): `ADR-022/019/016/023` 본문 끝에 "(부분 superseded — ADR-032)" 표기 (ADR-013/018 전례)
- **stale drift sweep**: `.claude/templates/{done-md-template.md,pin-template.txt}`(ADR-031/025 잔재) · `.claude/setup-steps/04-finalize.md`(옛 work-pin 시드) · `ADR.md:18`(tech-stack **ADR-026 누락** → 10개) · `ADR.md:20`(harness "8개:013~021" → **17개, 번호 나열**: 013·014·015·016·018·019·020·021·022·023·024·025·027·028·029·031·032) · `ADR_History.md`(본 ADR 이력 한 줄)

## 한계 / 모니터링 (critic 발견 위험)

- **이해 부채 (학습 pull 미발생)**: "나중에 물어봐야지"가 안 일어나면 부채가 조용히 쌓임 = ADR-025가 죽인 그 함정. pending-comprehension 원장의 가시성이 유일 방어 → 작동 안 하면 재조정.
- **정책 5개 강결합**: grade-and-risk↔subagent-routing↔reporting-format↔pin-and-done↔review-tiering + 헌법 6절 + agents가 "동기화 책임"으로 묶임 → **반드시 한 묶음 atomic REVISE**(하나만 고치면 drift).
- **circuit-breaker halt 권한 부재**: 훅은 루프를 직접 못 죽임 → halt 신호 파일을 드라이버가 폴링해야 멈춤. 폴링 누락 시 무인 토큰/시간 폭주 미차단(예산 상한 공백).
- **pin-injector 무인 미발동**: UserPromptSubmit 트리거라 scheduled task(사람 프롬프트 0)에선 발동 안 함 → 드라이버가 스텝 경계에서 work-pin 직접 주입 필요.
- **trust-boundary 자율 침범**: risk-detector는 advisory(차단 X) → 무인 루프가 깃발 산출물을 자동으로 사람게이트로 적재하는 후속 hook 없으면 trust-boundary 자율 commit 구멍.
- **권한 승격 보안 면적**: settings.local의 commit/checkout 권한을 무인 allow로 올리면 baseline 오염 위험. **ask(pr merge/create) 게이트는 절대 보존.**
- **Unity MCP 메인세션 전용**: 무인 v2에서 실아트 import/검증 불가 → 아트는 강제 사람 트랙(placeholder rot는 manifest 운영에 의존).

## 미결 — #1 결정됨, 나머지 2~6은 구현(`/work:plan`) 단계 이월

1. **✅ v2 채택 범위 — 결정 (2026-06-18, 영호): v1(attended)만 adopt.** v2(무인 Desktop scheduled)는 target으로 *문서화만* — v1 검증 + 3대 위험(권한 승격·circuit halt·trust-boundary 자율침범) 방어 hook 선결 후 *별도 ADR*. (엔진 동일, 기동층만 교체라 v1 시작에 손해 0 = 보조바퀴 졸업 패턴)
2. **신규 정책 파일 vs 흡수**: work-judge/review-throughput을 독립 파일로 둘지, grade-and-risk §3 / review-tiering 확장으로 흡수할지(220줄 임계 판단).
3. **`/goal`+`/loop` 둘 다 vs 통합**: 인터벌 반복형과 목표 도달형 둘 다 슬래시로 만들지.
4. **settings 권한 승격 범위**: commit/add/test/checkout 어디까지 무인 allow로? (ask(pr)만 남기면 충분한가)
5. **원장 위치**: pending-* 3종을 `.claude/state`(라이브) vs `00_Document/policies`(명세) 어디에, `.gitignore` 여부.
6. **신뢰 졸업 N**: 어떤 산출물 유형이 N회 위반0이면 샘플링으로 강등되는지의 N·분류 기준.

> **1번 결정 완료(v1)** → ADR 스코프 확정. 나머지 2~6은 `/work:plan` sweep 단계에서 하나씩 (auditor: "정책/구현 이월 가능").
