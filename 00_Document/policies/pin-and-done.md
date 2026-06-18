# Pin & Done — 작업 좌표 핀(압축) + Phase 완료 박제 라이프사이클

> **헌법 참조**: 본 정책은 새 헌법 v1 "작업 좌표 + Phase 완료 박제" 섹션에서 링크됩니다.
> 충돌 시 헌법이 이깁니다.

본 문서는 *시간순으로 연결된* 3개 정책을 통합 정의합니다:

```
[작업 중]              [Phase 완료 직후]            [박제 직후]
   ↓                        ↓                          ↓
current-pin 갱신    →    -DONE.md 박제          →    두 액션 권유
(매 응답 영향)          (복잡/대규모 등급만)         (사용자 선택)
```

5/20 의논 결과 — work-pin은 *압축*(목표 30~40줄, 옛 60줄+에서 다이어트). 본인 회고 학습 트랙은 ADR-025로 은퇴 (knowledge 트랙 A만 유지).

---

## 1. 작업 좌표 핀 (current-pin.txt) — 압축본

### 위치·역할

- 파일: `.claude/state/current-pin.txt` (목표 30~40줄, `.gitignore`)
- 역할: 현재 작업 좌표를 항시 보관 → 학습 질문 끼어들어도 *다음 턴*에 작업 복원
- 주입: `UserPromptSubmit` 훅(`../hooks/pin-injector.sh`, Phase 03 산출물)이 *매 사용자 입력 직전* 핀 내용을 컨텍스트 상단에 주입

### 핀 필드 (압축 5개 + 선택: 주의할 약속 · 루프 상태)

빈 템플릿: [`../templates/pin-template.txt`](../templates/pin-template.txt) (Phase 03 산출물)

```
WORK-ID:        <Phase slug 또는 ad-hoc-YYYYMMDD-주제>
PHASE:          <마일스톤·Phase 번호> / 등급: <단순/보통/복잡/대규모, 자동 상향 표기>
현재 작업:      <지금 무엇을 하는지 한 줄>
다음 액션:      <바로 다음 한 스텝>
주의할 약속:    <빠뜨리면 안 되는 검증/제약, 없으면 생략>
루프 상태:      <버킷 a/b/c · 사람대기 여부 · pending-* 원장 참조 — loop 운영 시만 (work-judge)>
마지막 갱신:    <YYYY-MM-DD 또는 commit hash>
```

### 옛 → 새 필드 비교

| 옛 필드 | 새 처리 | 사유 |
|---|---|---|
| WORK-ID | 유지 | 그래프 키 |
| PHASE | **등급 합쳐** 한 줄 | 등급 가시화 (자동 상향 박힘) |
| 현재 작업 | 유지 | 핵심 |
| 완료 조건 | **삭제** | Phase 정의 `.md`에 박혀있음 (중복) |
| 다음 액션 | 유지 | 다음 턴 진입 좌표 |
| 주의할 약속 | **선택**(없으면 생략) | 매 작업 박힐 가치 없음 |
| 학습 보존 | **트랙 A(knowledge)로 이관** | knowledge `_index.md` (회고 트랙 B는 ADR-025 은퇴) |
| 마지막 갱신 | 유지 | 신선도 검증 |

압축 결과: 옛 8개 필드 + 본문 ~60줄 → 새 5+1개 필드 + 본문 ~30줄.

### 갱신 정책 (잘못된 핀 고착 방지)

| 시점 | 누가 | 무엇을 |
|---|---|---|
| `/work:plan` 호출 직후 (Phase 시작) | AI | 자동 생성 |
| 이미 분해된 마일스톤에서 다음 Phase 진입 시 | AI (사용자 확인 후) | 핀을 그 Phase 좌표로 갱신 |
| 코드 변경 후 work-pin 갱신 시 | AI | *변경된 항목만* (현재 작업 / 다음 액션) |
| 등급 자동 상향 시 | Hook (`../hooks/risk-detector.sh`) | PHASE 줄에 상향 사유 박음 |
| 주의할 약속 변동 시 | **사용자 수동** | 갱신 |
| 루프 스텝 경계 (loop-driven, M7.5) | 루프 엔진/드라이버 | 현재 작업/다음 액션 + 루프 상태(버킷·원장). 무인 시 `pin-injector` 미발동 → 드라이버 직접 주입 |
| Phase 완료 시 | AI | archived 또는 cleared |

**원칙**: 핀은 *잘못 박히면 가짜 좌표로 다음 응답을 오염*. *변경 항목만 최소 갱신* + *사용자 약속은 확인 후*.

---

## 2. -DONE.md 박제 — 복잡/대규모 등급 한정

### 발동 조건

- Phase 파일의 **모든 완료 조건 충족** 시
- **복잡 또는 대규모 등급일 때만** 박제 ([`grade-and-risk.md`](grade-and-risk.md))
- 단순/보통 등급은 work-pin + commit message로 박제 충분 (양식 부담 회피)

### 경로

```
01_Phases/<owner>/M{N}-{slug}/{NN}-{phase-name}-DONE.md
```

원본 Phase 파일과 *짝꿍 페어*. Phase 정의 `.md`의 frontmatter `owner:`가 박는 사람 식별.

### 템플릿

[`../templates/done-md-template.md`](../templates/done-md-template.md) (Phase 03 산출물 — 옛 템플릿 정합 갱신).

### 역할 분담 (가짜 학습 방지)

| 산출물 | 작성자 | 내용 |
|---|---|---|
| `-DONE.md` | **AI** | 사실·결정·증상·키워드 박제. 잊히기 전에. |
| 트랙 A 학습 (`.claude/knowledge/`) | AI 박제 + 사용자 확인 | 구조화 패턴 ([`knowledge-system.md`](knowledge-system.md)) |

(본인 회고 학습 일지 = ADR-025로 은퇴. `-DONE.md`는 AI 사실 박제로 유지.)

### Post-flight 게이트 (훅 강제 4가지)

`-DONE.md` Write/Edit 시 `../hooks/phase-gate-validator.sh`(Phase 03 산출물)가 형식 검사. 누락 시 `exit 2`로 차단:

1. **YAML frontmatter 필수 필드**: `summary` / `phase` / `status` / `owner` (신규) / `grade` (신규)
2. **필수 H2 섹션**: 단순/보통은 X. **복잡 이상** = `TL;DR` / `AC 검증 결과` / `학습 일지 후보 키워드` / `5단계 보고`(🎯/🤔/🛠️/🧪/➡️ 구조)
3. **5단계 보고 5 라벨 + HTML 시각화 페어**(복잡 이상, ADR-031) ([`reporting-format.md`](reporting-format.md))
4. **`AC 검증 결과` 섹션 비어있지 않음**: 완료조건을 *실제로 실행한* 명령어 + 결과 박제 (추측·요약 X)

Phase는 자동 진행(ADR-031 — 학습 호흡 수동 멈춤 폐기, ADR-025 정합), 박제 시 빼먹기는 훅이 물리적으로 차단.

---

## 3. Phase 완료 시 두 액션 권유

### 발동 시점

**마일스톤 마감 또는 영호 직접 확인 지점**에서만 (ADR-031 — 매 Phase 권유는 흐름 끊어 폐기). Phase 자동 진행 중에는 권유 없이 `-DONE.md`/HTML 박제만 하고 진행.

### 출력 양식

```
**📚 Phase 완료 — 다음 권유합니다**

**1. 학습 박제** (옵션, 본인 의지):
- **트랙 A (knowledge AI 캐시)**: 도메인 _index.md에 박을 키워드 있나요? — 사용자 확인 후 AI 박제
  - 패스해도 OK (시간 없을 때). (본인 회고 트랙 B는 ADR-025로 은퇴)

**2. 세션 마감** (강한 권유, 작업 박제):
- `/session:end` — commit + PR + (선택)노션 박제 + 다음 액션 결정까지 한 흐름
- 학부생 백지 팀원은 깜빡 위험 크니 잊지 말기
- `pin-injector.sh` 훅이 commit 안 된 -DONE.md 검출 시 매 입력 경고 주입 (안전망)
```

### 권유 규칙

- **권유이지 강제 X**. 패스 시 즉시 존중
- **같은 Phase에 두 번 권유 X**
- **단순/보통 등급은 권유 X** (-DONE.md 박제 자체가 X)
- **복잡/대규모 등급만 권유 발동**

---

## 4. 라이프사이클 전체 (시각화)

```
[작업 시작]
   │
   ├─ /work:plan 호출 → AI가 current-pin 생성 (압축 5+1 필드)
   │   └─ 등급 결정 → PHASE 줄에 박힘
   │
[코드 작업 반복]
   │
   ├─ Edit/Write → 양식 봉투 X (work-envelope 죽임)
   │   └─ AI가 핀의 "현재 작업 / 다음 액션"만 갱신
   │
   ├─ 위험 깃발 잡힘 → risk-detector.sh Hook이 자동 등급 상향
   │   └─ PHASE 줄에 상향 사유 박힘
   │
[Phase 완료 감지]
   │
   ├─ 복잡/대규모 등급:
   │   ├─ 복잡 이상: HTML 시각화 박제 (5단계 보고 구조 내장 — 인라인 출력 아님, ADR-031)
   │   ├─ -DONE.md 작성 (AI, 짝꿍 페어 경로) → 훅 4가지 검산
   │   ├─ commit
   │   ├─ 권유 출력 (트랙 A knowledge 박제 옵션 + 세션 마감)
   │   └─ AI가 핀 archived 또는 cleared
   │
   └─ 단순/보통 등급:
       ├─ commit message로 박제 충분
       └─ AI가 핀 cleared
```

---

## 5. work-pin = 단일 작업 좌표 (ADR-025)

옛 운영은 작업 좌표를 두 곳(work-pin + `CONTEXT.md` "현재 멈춤 지점")에 두고 `/session:end`에서 동기화했다. **ADR-025로 CONTEXT 3종 은퇴** → work-pin(`.claude/state/current-pin.txt`, 매 응답 자동 주입)이 *유일한* 세션 간 핸드오프 표면. 안 변하는 사용자 컨텍스트(신분/목표/일정)는 memory(`~/.claude/projects/.../memory/`)가 보유.

→ 이중 좌표 동기 비용 + 세션 시작 컨텍스트 적재 낭비 소멸. 단 **work-pin 자체 비대**가 새 위험 → 30~40줄 목표 유지 + 마감 commit 이력·완료 Phase 상세는 CHANGELOG/`-DONE.md`로 위임 (핀에 누적 X).

### 5.1 진행 단계 stale hole 발견 게이트 (M3.7 ADR-023; ADR-025로 work-pin 단독화)

**한계**: work-pin "현재 작업/다음 액션"이 실제 git/gh 진행 단계(commit / push / PR 생성 / PR 머지)와 어긋난 채 박힐 수 있음.

**게이트**: `/session:start` 0-부수 단계가 `git log -3` + `gh pr list --head $(branch)` + `git status -sb` 자동 호출 → work-pin 키워드 vs 실제 상태 대략 매칭 → 차이 발견 시 STOP + 본인 수동 갱신 안내.

**핵심 정신**: 발견만 자동, 갱신은 본인 수동 (Hook is for alert, not action / §1 "갱신은 본인 수동" 확장). 사용자 명시 위임("drift 봉합해줘") 시 예외. 디테일 = [ADR-023](../ADR/harness/ADR-023-sync-gate-progress-stale-hole.md) — CONTEXT 동기 절반은 ADR-025로 무효, drift 게이트는 work-pin 단독으로 유지.

---

## 6. 변경 시 동기화 책임

본 정책 수정 시 *반드시* 함께 갱신: [`pin-injector.sh`](../hooks/pin-injector.sh) (핀 주입) / [`phase-gate-validator.sh`](../hooks/phase-gate-validator.sh) (-DONE.md 게이트) / [`pin-template.txt`](../templates/pin-template.txt) (압축 필드 + 루프 상태) / [`done-md-template.md`](../templates/done-md-template.md) (등급별 필수 섹션) / [`reporting-format.md`](reporting-format.md) (5단계 라벨 정합) / [`grade-and-risk.md`](grade-and-risk.md) (등급 박제 조건) / [`loop-driver.md`](loop-driver.md) · [`work-judge.md`](work-judge.md) (루프 상태 필드·갱신 주체).

---

## 갱신 이력

- 2026-05-24 — ADR-025 정합. §5 "work-pin ↔ CONTEXT 정합"을 "work-pin = 단일 작업 좌표"로 재작성 (CONTEXT 3종 은퇴, memory가 사용자 컨텍스트 보유). §1·§2·§3 학습 일지 트랙 B 참조 제거 (knowledge 트랙 A만 유지). drift 발견 게이트는 work-pin 단독으로 유지.
- 2026-05-22 — M3.7 Phase 02에서 §5.1 신설 (진행 단계 stale hole 봉합, ADR-023). 옵션 C 게이트 한계 명시 (세션 마감 시점만 동기 → 세션 도중 진행 단계 X) + 새 발견 게이트 인용 (`/session:start` 0-부수, Hook is for alert 정신). 5번 실측 누적 → Rule of Three 통과 후 박힘. 동기화 룰 표 한 행 추가 (진행 단계 stale 세 시점).
- 2026-05-22 — M3.6 Phase 02-D에서 §5 응축 (254 → ~219줄, doc-thresholds 220 임계 위반 봉합). 정합 약속 + 동기화 룰 표 + 함정 핵심만 유지, 풀 절차는 [`session/end.md §7.5`](../../.claude/commands/session/end.md) 참조로 위임. 정신 변경 X, 중복 제거만.
- 2026-05-21 — M3.5 Phase 05에서 §5 신설 (work-pin ↔ CONTEXT 정합, 옵션 C `/session:end` 단일 게이트). 등급 무관 "⏸️ 현재 멈춤 지점" 항상 동기 / 학습 후보는 콘텐츠 유무 분기. 양식 부담 ↓보다 Claude 혼선 방지 우선 정신 박힘. 옛 §5 → 새 §6 (변경 동기화 책임).
- 2026-05-20 — M3.5 Phase 01 (2/2)에서 압축. work-pin 8 필드 → 5+1, 본문 응축. -DONE.md 박제 = 복잡/대규모 등급 한정 조건부화. 학습 일지 권유 = 트랙 A/B 분리 정합. 옛 178줄 → ~140줄.
- 2026-05-15 — 헌법에서 외부화 (Action 1, 3단계). 핀·박제·권유를 *시간순 라이프사이클*로 통합.
