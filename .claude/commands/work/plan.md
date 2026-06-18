---
description: 큰 목표를 학습 가능한 Phase들로 쪼개서 01_Phases/<owner>/M{N}-{slug}/ 폴더에 생성
argument-hint: <마일스톤 또는 목표 설명>
---

사용자가 다음 목표에 대한 Phase 계획을 요청했습니다:
**$ARGUMENTS**

다음 절차를 따르세요:

### 1. 컨텍스트 수집

다음 문서들을 읽어서 큰 그림을 잡으세요:
- `CLAUDE.md` (헌법) — 특히 "📊 작업 등급" 섹션
- `00_Document/PRD.md` (무엇을 만들지)
- `00_Document/ARCHITECTURE.md` (어떻게 만들지)
- `00_Document/ADR/INDEX.md` (왜 이렇게 결정했는지)
- `00_Document/policies/grade-and-risk.md` (4등급 분류 정책)
- `00_Document/policies/subagent-routing.md` (SubAgent 9 풀)
- 이미 있는 `01_Phases/<owner>/` 폴더 (중복 방지)

이 중 비어있거나 채워져 있지 않은 게 있으면 STOP하고 사용자에게
"이 문서를 먼저 채우는 게 좋겠어요"라고 안내.

### 2. 목표 검증

사용자의 목표가:
- 너무 추상적인가? (예: "서버 만들기") → 마일스톤으로 잡고 그 안에 Phase 5~7개로 쪼갬
- 너무 작은가? (예: "패킷 ID 하나 추가") → 단순/보통 등급 = Phase 분해 X. 직접 진행 권유
- 적절한가? → Phase **5~7개** 사이로 쪼개기 (M3.5 학습: 옛 M3 9개는 과했음 — 양식 부담 ↑ + 학습 호흡 ↓)

목표가 모호하면 사용자에게 1~2개 명확화 질문 후 진행.

### 3. Phase 분해

다음 원칙을 따라 쪼개세요:

- **한 Phase = 1~3시간 작업**. 더 크면 다시 쪼갬
- **앞 Phase 끝나면 뭔가 데모할 수 있어야 함** — 6개월짜리 모델 같은 거 만드는 Phase 금지
- **의존성 순서대로 정렬** — Phase N은 Phase N-1, N-2가 끝나야만 가능
- **각 Phase는 한 명의 SubAgent 영역에 가급적 들어맞게** — 한 Phase가 server + client + qa 모두 건드리면 너무 큰 거 (등급 *대규모*로 상향)
- **첫 Phase는 매우 작게** — "Hello World 수준" 추천. 환경 검증용
- **병렬 가능한 Phase 식별** — 의존성 없으면 명시 (예: Phase 03·04 병렬). plan-auditor 점검 기준에 박힘

### 4. Phase 파일 생성 (frontmatter 필수)

`01_Phases/<본인 owner>/M{N}-{milestone-slug}/` 폴더 만들고, 각 Phase를 파일로 생성 (사람별 namespace).

각 파일은 `01_Phases/_template.md`를 베이스로 채우되, **frontmatter 필수**:

```yaml
---
owner: youngho | yuhyeon | ingyu  # M3.5 신규 필수 필드
phase: NN
status: pending  # pending | in_progress | done
grade: 단순 | 보통 | 복잡 | 대규모  # M3.5 신규 — 4등급 명시
loop_track: auto-gate | human-visual | human-gate  # M7.5 — 루프 버킷(a 기계 / b 취향·육안 / c 비가역). work-judge.md
summary: <한 줄 요약>
---
```

> **loop_track (M7.5)**: 루프 드라이버가 이 Phase를 *어떻게 다룰지* — `auto-gate`(버킷 a, 기계 게이트 통과 시 자율) / `human-visual`(버킷 b, 아트·Unity 외관 = 사람 트랙) / `human-gate`(버킷 c, 비가역·설계 분기 = 사람 GO 정지). 매핑 = [`../../../00_Document/policies/work-judge.md`](../../../00_Document/policies/work-judge.md).

본문에 다음을 반드시 채울 것:

- 🎯 목표 (구체적, 측정 가능)
- ⏪ 사전 조건 (다른 Phase 의존성)
- 📝 작업 내용 (체크리스트)
- ✅ 완료 조건 (객관적, 정량적 — "잘 작동한다" 같은 모호 표현 X)
- 📚 학습 포인트 (학부생 시각에서 새로운 개념)
- ⚠️ 함정 (이 영역에서 자주 하는 실수)
- 담당 SubAgent 명시 (server / shared / client / qa / unity-bridge 또는 메인 직접)

### 4.5. work-pin 자동 시드 (ADR-018 입구 안전망)

Phase 파일 생성 직후 `.claude/state/current-pin.txt`를 마일스톤의 **첫 Phase 좌표**로 시드 ([`../../policies/pin-and-done.md`](../../policies/pin-and-done.md) "1번 절: pin 라이프사이클" 참조). 시드 필드 (M3.5 압축 양식, 목표 30~40줄):

```
WORK-ID: m{N}-{milestone-slug}
PHASE: 01/{전체}
등급: <첫 Phase의 grade>
현재 작업: <첫 Phase의 🎯 목표 한 줄>
다음 액션: <첫 Phase의 📝 첫 체크리스트 항목>
주의할 약속: <헌법 절대 원칙 중 충돌 가능한 것 최대 3개>
마지막 갱신: {YYYY-MM-DD}
```

→ `pin-injector.sh` Hook이 다음 입력부터 자동 주입.

### 4.6. plan-auditor SubAgent 자동 호출 (Tier 2-B)

Phase 파일 생성 직후 **plan-auditor SubAgent 자동 호출** ([`../../agents/plan-auditor.md`](../../agents/plan-auditor.md)):

- 입력: `plan_files` (생성된 Phase `.md` + `_milestone-plan.md` 경로) + `milestone_context` + `prior_phases` (옛 마일스톤 -DONE.md 경로)
- 6축 점검 출력 (Phase 분해 적정성 / 의존성 그래프 / 완료 조건 정량성 / 등급 산정 / 헌법 위반 위험 / 시나리오 명세)
- 🔴 결함 발견 시 → 옵션 A (즉시 봉합) / 옵션 B (현 상태 진행 + 별 Phase 봉합) 두 갈래 제시
- 🔴 0개 = GO

### 5. 사용자에게 보고

Phase 분해 + plan-auditor 결과를 다음 형식으로 요약:

```
─────────────────────────────────────────
📋 Phase 계획 완료
─────────────────────────────────────────

🎯 목표: [사용자 입력]

📂 생성된 마일스톤: M{N}-{slug} (owner: <slug>)
   총 N개 Phase (5~7 범위 권장)

순서 (등급 + 담당 SubAgent 표기):
  1. [Phase 01 제목] (예상 1.5시간, 등급: 보통, 담당: server)
     → 끝나면: 무엇을 데모할 수 있는지
  2. [Phase 02 제목] (예상 2시간, 등급: 복잡, 담당: shared+server)
     → 끝나면: ...
  ...

병렬 가능: Phase NN ↔ Phase MM (의존성 없음)

📚 이번 마일스톤에서 배우게 될 핵심 개념:
   - 개념 1
   - ...

🔬 plan-auditor 결과: <✅ GO / 🔴 N개 결함 / 🟡 N개 제안>

📌 work-pin 시드 완료: WORK-ID=`m{N}-{milestone-slug}` 박힘. 다음 입력부터 자동 주입.

➡️ 추천 시작점:
   "01_Phases/<owner>/M{N}-{slug}/01-{first-phase}.md 부터 시작하자"

⚠️ 주의: Phase 진행하다 막히거나 scope가 늘면, 새 Phase로 떼어내세요.
   현재 Phase에 끼워 넣지 마세요.
```

### 6. 추가 학습 안내

Phase 계획을 본 사용자가 "이게 왜 이 순서?"라고 물을 가능성이 큼.
요약 마지막에 한 줄 추가:

> 각 Phase의 순서·범위 궁금하면 "이 Phase 왜 여기에 있어?"라고 물어보세요.

---

**중요 원칙**:

- **학습 모드** — 학부생이 따라갈 수 있게
- 각 Phase에 대해 "이건 너무 빨리 가는 건 아닌가?" 자문 — 의심되면 더 작게
- **Phase 입자 5~7개/마일스톤 = 권장** — 8+ = plan-auditor 결함 가능성 ↑
- **plan-auditor 자동 호출 = 의무** — 우회 X. 사용자가 "스킵" 명시하면 work-pin에 사유 박음
- **frontmatter 필수** — `owner` / `phase` / `status` / `grade` / `summary` 없으면 phase-gate-validator.sh가 -DONE.md 박을 때 차단
