# Pin & Done — 작업 좌표 핀 + Phase 완료 박제 라이프사이클

> **헌법 참조**: 본 정책은 헌법 "응대 원칙"에서 링크됩니다.
> 충돌 시 헌법이 이깁니다.

본 문서는 *시간순으로 연결된* 3개 정책을 통합 정의합니다:

```
[작업 중]              [Phase 완료 직후]         [박제 직후]
   ↓                        ↓                       ↓
current-pin 갱신    →    -DONE.md 박제      →    두 액션 권유
(매 응답 영향)          (1회성, AI 작성)        (사용자 선택)
```

라이프사이클 한 그림: 작업하는 동안 핀이 좌표 보존 → Phase 완료 시 AI가 사실 박제 → 박제 후 학습 일지·세션 마감 권유.

---

## 1. 작업 좌표 핀 (current-pin.txt)

### 위치·역할

- 파일: `.claude/state/current-pin.txt` (8~12줄, `.gitignore`)
- 역할: 현재 작업 좌표를 항시 보관 → 학습 질문 끼어들어도 *다음 턴*에 작업 복원
- 주입: `UserPromptSubmit` 훅(`.claude/hooks/inject-current-pin.sh`)이 *매 사용자 입력 직전* 핀 내용을 컨텍스트 상단에 주입

### 핀 필드 (8개)

빈 템플릿: [`.claude/templates/pin-template.txt`]

```
WORK-ID:        <Phase slug 또는 ad-hoc-YYYYMMDD-주제>
PHASE:          <마일스톤·Phase 번호>
현재 작업:      <지금 무엇을 하는지 한 줄>
완료 조건:      <어떻게 끝났다고 판단하는지>
다음 액션:      <바로 다음 한 스텝>
주의할 약속:    <빠뜨리면 안 되는 검증/제약>
학습 보존:      <이 작업에서 잊지 말아야 할 개념>
마지막 갱신:    <YYYY-MM-DD 또는 commit hash>
```

### 갱신 정책 (잘못된 핀 고착 방지 — Codex R3 권고)

| 시점 | 누가 | 무엇을 |
|---|---|---|
| `/work:plan` 호출 직후 (Phase 시작) | AI | 자동 생성 |
| 이미 분해된 마일스톤에서 다음 Phase 진입 시 | AI (사용자 확인 후) | 핀을 그 Phase 좌표로 갱신 — 사용자 진입 의지 표현 시점 ("Phase NN 시작하자" 같은). `/work:plan` 갭 메우기 |
| 코드 변경 후 work-envelope 작성 시 | AI | *변경된 항목만* 갱신 (현재 작업 / 다음 액션) |
| 완료 조건 진척 시 | AI (사용자 확인 후) | 갱신 |
| 주의할 약속 · 학습 보존 변동 시 | **사용자 수동** | 갱신 |
| Phase 완료 시 | AI | archived 또는 cleared |

**원칙**: 핀은 *잘못 박히면 가짜 좌표로 다음 응답을 오염시킴*. 그래서 *변경 항목만 최소 갱신* + *완료 조건/사용자 약속은 확인 후*.

---

## 2. -DONE.md 박제 (Phase 완료 시 필수)

### 발동 조건

- Phase 파일의 **모든 완료 조건 충족** 시
- 5단계 보고(→ [`reporting-format.md`](reporting-format.md))와 **같은 응답에서** AI가 작성·commit

### 경로

```
01_Phases/M{N}-{slug}/{NN}-{phase-name}-DONE.md
```

원본 Phase 파일과 *짝꿍 페어*:

```
03-tcp-listener.md           ← Phase 정의
03-tcp-listener-DONE.md      ← Phase 박제 (AI 작성)
```

### 템플릿

[`.claude/templates/done-md-template.md`](.claude/templates/done-md-template.md) 참조.

### 역할 분담 (가짜 학습 방지)

| 산출물 | 작성자 | 내용 |
|---|---|---|
| `-DONE.md` | **AI** | 사실·결정·증상·키워드 박제. 잊히기 전에. |
| `learning-journal/` | **본인** | 회고·교훈·면접 답변. AI는 인터뷰만. |

본인이 학습 일지 쓸 때 `-DONE.md`를 *사실 베이스*로 활용.

### Post-flight 게이트 (훅 강제 4가지)

`-DONE.md` Write/Edit 시 `.claude/hooks/validate-phase-gate.sh`가 형식 검사. 누락 시 `exit 2`로 차단 → AI가 빠진 항목 채워서 재시도.

1. **YAML frontmatter 필수 필드**: `summary` (1줄, 다음 Phase가 인용할 표준 입력), `phase`, `status`
2. **필수 H2 섹션 5개**: `TL;DR` / `5단계 보고` / `AC 검증 결과` / `결정 흐름` / `학습 일지 후보 키워드`
3. **5단계 보고 항목 라벨 5개** (→ [`reporting-format.md`](reporting-format.md) 1번 절): 무엇을 만들었나 / 왜 필요한가 / 어떻게 만들었나 / 테스트 결과 / 다음 스텝
4. **`AC 검증 결과` 섹션 본문 비어있지 않음**: Phase 파일의 완료조건을 *실제로 실행한* 명령어 + 결과 박제 (추측·요약 X)

학습 호흡은 수동 유지하되, **박제 시 빼먹기는 물리적으로 차단**.

---

## 3. Phase 완료 시 두 액션 권유 (필수)

### 발동 시점

`-DONE.md` commit 직후 같은 응답에서 다음 출력.

### 출력 양식

```
**📚 Phase 완료 — 다음 두 액션 권유합니다**

**1. 학습 일지 작성** (선택, 면접 무기로 누적):
- `/journal:phase` — Phase 통째 회고 (15~20분)
- `/journal:bug` — 막혔던 사건이 있었다면 (디테일 안 잊었을 때)
- `/journal:concept <키워드>` — 깊이 학습한 개념을 본인 말로
- 패스 (다음 Phase로) — 단, 가급적 오늘 안에 추천

**2. 세션 마감** (강한 권유, 작업 박제):
- `/session:end` — commit + PR + 노션 박제 + 다음 액션 결정까지 한 흐름
- 학부생 백지 팀원은 깜빡 위험 크니 잊지 말기
- `inject-current-pin.sh` 훅이 commit 안 된 -DONE.md 검출 시 매 입력 경고 주입 (안전망)
```

### 권유 규칙

- **권유이지 강제 X**. 패스 시 즉시 존중.
- **같은 Phase에 두 번 권유 X.**
- **Phase 외 일반 작업 후엔 권유 X** (Phase 단위 완료 시에만).

---

## 4. 라이프사이클 전체 (시각화)

```
[작업 시작]
   │
   ├─ /work:plan 호출 → AI가 current-pin 생성 (필드 8개)
   │
[코드 작업 반복]
   │
   ├─ Edit/Write → work-envelope 첨부 (→ reporting-format.md)
   │   └─ AI가 핀의 "현재 작업 / 다음 액션"만 갱신
   │
   ├─ 완료 조건 진척 → 사용자 확인 후 AI가 갱신
   │
[Phase 완료 감지]
   │
   ├─ 5단계 보고 출력 (→ reporting-format.md)
   ├─ -DONE.md 작성 (AI, 짝꿍 페어 경로) → 훅 4가지 검산
   ├─ commit
   │
   ├─ 두 액션 권유 출력 (학습 일지 + 세션 마감)
   │   ├─ 학습 일지: 본인 선택
   │   └─ /session:end: 강한 권유
   │
   └─ AI가 핀 archived 또는 cleared
```

---

## 5. 변경 시 동기화 책임

본 정책 수정 시 *반드시* 함께 갱신:

- `.claude/hooks/inject-current-pin.sh` (핀 주입 동작)
- `.claude/hooks/validate-phase-gate.sh` (-DONE.md 게이트)
- `.claude/templates/pin-template.txt` (필드 8개)
- `.claude/templates/done-md-template.md` (필수 섹션 5개)
- [`reporting-format.md`](reporting-format.md) (5단계 라벨 5개와 일관성)

---

## 갱신 이력

- 2026-05-15 — 헌법에서 외부화 (Action 1, 3단계). 핀·박제·권유를 *시간순 라이프사이클*로 통합. ADR-013(박제 분업) + ADR-015(Post-flight 게이트) + Codex R3(핀 갱신) 박제 한 곳에 모음.
