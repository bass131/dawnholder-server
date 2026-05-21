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

5/20 의논 결과 — work-pin은 *압축*(목표 30~40줄, 옛 60줄+에서 다이어트), 학습 일지 권유는 *트랙 B* (Notion + 잔존 learning-journal/)로 이관.

---

## 1. 작업 좌표 핀 (current-pin.txt) — 압축본

### 위치·역할

- 파일: `.claude/state/current-pin.txt` (목표 30~40줄, `.gitignore`)
- 역할: 현재 작업 좌표를 항시 보관 → 학습 질문 끼어들어도 *다음 턴*에 작업 복원
- 주입: `UserPromptSubmit` 훅(`../hooks/pin-injector.sh`, Phase 03 산출물)이 *매 사용자 입력 직전* 핀 내용을 컨텍스트 상단에 주입

### 핀 필드 (압축 5개 + 선택 1개)

빈 템플릿: [`../templates/pin-template.txt`](../templates/pin-template.txt) (Phase 03 산출물)

```
WORK-ID:        <Phase slug 또는 ad-hoc-YYYYMMDD-주제>
PHASE:          <마일스톤·Phase 번호> / 등급: <단순/보통/복잡/대규모, 자동 상향 표기>
현재 작업:      <지금 무엇을 하는지 한 줄>
다음 액션:      <바로 다음 한 스텝>
주의할 약속:    <빠뜨리면 안 되는 검증/제약, 없으면 생략>
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
| 학습 보존 | **트랙 A/B로 이관** | knowledge `_index.md` 또는 Notion |
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
| 트랙 B 학습 일지 (Notion + 잔존 learning-journal/) | **본인** | 회고·교훈·면접 답변. AI는 인터뷰만. |

본인이 학습 일지 쓸 때 `-DONE.md`를 *사실 베이스*로 활용.

### Post-flight 게이트 (훅 강제 4가지)

`-DONE.md` Write/Edit 시 `../hooks/phase-gate-validator.sh`(Phase 03 산출물)가 형식 검사. 누락 시 `exit 2`로 차단:

1. **YAML frontmatter 필수 필드**: `summary` / `phase` / `status` / `owner` (신규) / `grade` (신규)
2. **필수 H2 섹션**: 단순/보통은 X. 복잡 = `TL;DR` / `AC 검증 결과` / `학습 일지 후보 키워드`. 대규모 = 거기에 `5단계 보고` 추가
3. **5단계 보고 5 라벨**(대규모만) ([`reporting-format.md`](reporting-format.md))
4. **`AC 검증 결과` 섹션 비어있지 않음**: 완료조건을 *실제로 실행한* 명령어 + 결과 박제 (추측·요약 X)

학습 호흡은 수동 유지, 박제 시 빼먹기는 물리적으로 차단.

---

## 3. Phase 완료 시 두 액션 권유

### 발동 시점

`-DONE.md` commit 직후 같은 응답.

### 출력 양식 (트랙 A/B 분리 정합)

```
**📚 Phase 완료 — 다음 두 액션 권유합니다**

**1. 학습 박제** (옵션, 본인 의지):
- **트랙 A (AI 캐시)**: 도메인 _index.md에 박을 키워드 있나요? — 사용자 확인 후 AI 박제
- **트랙 B (회고)**: Notion "Dawnholder 협업 히스토리" 또는 learning-journal/ — 본인 작성
  - 패스해도 OK (시간 없을 때). 단 큰 학습은 잊기 전에 박는 게 가치 ↑

**2. 세션 마감** (강한 권유, 작업 박제):
- `/session:end` — commit + PR + 노션 박제 + 다음 액션 결정까지 한 흐름
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
   │   ├─ 대규모만: 5단계 보고 출력 (MD + HTML 이중)
   │   ├─ -DONE.md 작성 (AI, 짝꿍 페어 경로) → 훅 4가지 검산
   │   ├─ commit
   │   ├─ 두 액션 권유 출력 (트랙 A 박제 + 트랙 B 회고 + 세션 마감)
   │   └─ AI가 핀 archived 또는 cleared
   │
   └─ 단순/보통 등급:
       ├─ commit message로 박제 충분
       └─ AI가 핀 cleared
```

---

## 5. work-pin ↔ CONTEXT.md 정합 (옵션 C: `/session:end` 단일 게이트)

### 정합 약속

본 프로젝트에는 *현재 작업 좌표*를 담는 두 자산이 공존 — **work-pin** (`.claude/state/current-pin.txt`, 매 응답 영향, AI 자동 주입) + **CONTEXT.md "⏸️ 현재 멈춤 지점"** (세션 진입 `/session:start` 통독). 두 곳 어긋나면 AI가 *어느 게 진짜*인지 못 잡음 → 옛 결정 기반 작업 위험.

**정합 게이트 = `/session:end` 단일 박힘 (단방향 동기, work-pin → CONTEXT)**. 작업 중엔 work-pin만 갱신, CONTEXT는 마감 시점만.

### 동기화 룰 (요약)

| CONTEXT 항목 | 동기 분기 |
|---|---|
| **⏸️ 현재 멈춤 지점** | ✅ 등급 무관 항상 동기 (work-pin 압축본 → 풀어쓴 한 문단) |
| **학습 일지 후보** | ✅ 콘텐츠 있으면 추가 (★ 평가). 없으면 스킵 |
| **CONTEXT_History.md** | ✅ 매 마감 한 줄 자동 |
| **응축 평가** | ⚠️ 250줄+ 알림만, 자동 응축 X (사용자 결정) |
| **다른 섹션** (하드 일정 / 팀 구조 / 보류 중 / 핵심 결정) | ❌ 자동 갱신 X (사용자 의도 보호) |

**핵심 정신**: 옛 운영 "단순/보통 등급 = CONTEXT 갱신 스킵" 분기 고려했으나, work-pin 정합 깨짐 비용 > 양식 부담 ↓. **"현재 멈춤 지점" = 등급 무관 항상 동기**. 양식 부담은 *기본 OK 디폴트*로 줄임.

**디테일 절차** (자산 확인 / 미리보기 / 응축 알림): [`../../.claude/commands/session/end.md`](../../.claude/commands/session/end.md) §7.5 풀세트 박힘. 본 정책은 *정신*만, 절차는 슬래시 본문.

### 함정

- **work-pin 갱신 시 CONTEXT 동시 갱신 X** — 작업 중엔 work-pin만, 정합은 `/session:end`에서만 (양식 부담 ↑↑ 회피)
- **단순 복사 X** — work-pin은 *압축 좌표*, CONTEXT는 *맥락 포함 한 문단*
- **마일스톤 끝엔 응축 평가 의무** — 250줄+ 알림만, 본인 결정 (옛 자동 응축 사고 위험)

---

## 6. 변경 시 동기화 책임

본 정책 수정 시 *반드시* 함께 갱신: [`pin-injector.sh`](../hooks/pin-injector.sh) (핀 주입) / [`phase-gate-validator.sh`](../hooks/phase-gate-validator.sh) (-DONE.md 게이트) / [`pin-template.txt`](../templates/pin-template.txt) (압축 필드 5+1) / [`done-md-template.md`](../templates/done-md-template.md) (등급별 필수 섹션) / [`reporting-format.md`](reporting-format.md) (5단계 라벨 정합) / [`grade-and-risk.md`](grade-and-risk.md) (등급 박제 조건).

---

## 갱신 이력

- 2026-05-22 — M3.6 Phase 02-D에서 §5 응축 (254 → ~219줄, doc-thresholds 220 임계 위반 봉합). 정합 약속 + 동기화 룰 표 + 함정 핵심만 유지, 풀 절차는 [`session/end.md §7.5`](../../.claude/commands/session/end.md) 참조로 위임. 정신 변경 X, 중복 제거만.
- 2026-05-21 — M3.5 Phase 05에서 §5 신설 (work-pin ↔ CONTEXT 정합, 옵션 C `/session:end` 단일 게이트). 등급 무관 "⏸️ 현재 멈춤 지점" 항상 동기 / 학습 후보는 콘텐츠 유무 분기. 양식 부담 ↓보다 Claude 혼선 방지 우선 정신 박힘. 옛 §5 → 새 §6 (변경 동기화 책임).
- 2026-05-20 — M3.5 Phase 01 (2/2)에서 압축. work-pin 8 필드 → 5+1, 본문 응축. -DONE.md 박제 = 복잡/대규모 등급 한정 조건부화. 학습 일지 권유 = 트랙 A/B 분리 정합. 옛 178줄 → ~140줄.
- 2026-05-15 — 헌법에서 외부화 (Action 1, 3단계). 핀·박제·권유를 *시간순 라이프사이클*로 통합.
