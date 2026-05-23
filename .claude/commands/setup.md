---
description: 협업 셋업 — clone 후 첫 호출. 자기소개 → 환경 검증 → 역할별 셋업 → 첫 작업 안내까지 차근차근 진행
---

사용자가 ClaudeDev 레포에 처음 합류해 협업 셋업을 시작했습니다.

---

### 이 커맨드의 역할

`/setup` 한 번 호출하면 차근차근 단계별로 진행. 학부생 백지 팀원 가정 — **한 번에 한 단계씩 떠먹이듯** 안내. 막히면 STOP하고 도움 요청.

---

### 진행 흐름 (5단계)

```
01-intro         자기소개 + 역할 분기
02-common        모두 거치는 공통 환경 검증 (8 단계)
03-backend       백엔드 추가 셋업 (5 단계)         ─┐
                                                  ├── 역할에 따라 분기
03-unity-client  Unity 클라 추가 셋업 (8 단계)    ─┘
04-finalize      개인 자산 초기화 + 다음 액션 안내 (7 단계)
```

---

### 진행 절차

다음 순서로 단계별 .md 파일을 읽어 그대로 진행:

#### 1. 단계 01 진행

`.claude/setup-steps/01-intro.md` 읽고 안내대로.

종료 시 `name_kr`, `slug`, `role` 변수 결정됨.

**M3.5 팀 namespace 정합**:

- 영호 (팀장, server/shared) → `slug = "youngho"` / `role = "backend"`
- 정유현 (Unity 클라 UI/입력) → `slug = "yuhyeon"` / `role = "unity-client"`
- 김인규 (Unity 클라 컨텐츠 + ComfyUI 2D 자산) → `slug = "ingyu"` / `role = "unity-client"`
- 박정우 (관리 시스템 MES, 별도 레포) → 본 레포 셋업 X, 7월 이후 별도 진입

#### 2. 단계 02 진행

`.claude/setup-steps/02-common.md` 읽고 안내대로. 공통 환경 검증 8 단계. 막히면 STOP, 사용자 도움 요청 후 재진행.

#### 3. 단계 03 분기

`role` 변수에 따라:

- `role == "backend"` → `.claude/setup-steps/03-backend.md` 진행
- `role == "unity-client"` → `.claude/setup-steps/03-unity-client.md` 진행
- 다른 값 → 어느 쪽으로 진행할지 묻기

#### 4. 단계 04 진행

`.claude/setup-steps/04-finalize.md` 읽고 안내대로. 끝나면 셋업 완료. 첫 작업 안내까지.

---

### 중요 원칙

- **학습 모드 톤 유지** — 학부생 백지 팀원 가정. 전문 용어 풀어 설명
- **한 번에 한 단계씩** — 한꺼번에 여러 단계 안내 X
- **막히면 STOP** — 검증 실패 시 다음 단계로 X, 사용자 도움 요청
- **trade-off는 짧게** — 셋업은 결정 영역 아님. "이렇게 하면 됨" 톤
- **5단계 보고는 04 끝에 한 번만** — 셋업 자체는 환경 준비라 각 단계마다 보고 X

---

### 단계 간 변수 전달

- 단계 01 결정 변수:
  - `name_kr` — 한글 이름 (예: "김인규")
  - `slug` — 영문 식별자 (예: "ingyu")
  - `role` — "backend" 또는 "unity-client"
- 단계 02 종료 시: 모든 환경 검증 통과
- 단계 03 종료 시: 역할별 도구 검증 통과
- 단계 04 종료 시: 개인 자산 초기화 완료, 첫 작업 안내됨

---

### 진행 중 사용자 응답 패턴

- "✓" 또는 "OK" — 통과, 다음으로
- "막혔어요" / 에러 메시지 — 진단 + 도움 안내, 같은 단계 재시도
- "건너뛸게요" — 명시적 SKIP 요청. 위험 짚고 진행 (예: 한글 경로 검증 SKIP은 비추천 — 48시간 silent fail 함정)

---

### M3.5 새 하네스 변경 (옛 대비)

- **본 절차 변경 X** — 옛 협업 셋업 인프라 그대로 유지 (2026-05-14 박힘)
- **namespace 정합 명시**: 옛 `<본인 네임스페이스>` placeholder → 새 영호/유현/인규 실제 slug 박힘 (Phase 06 전환 시 setup-steps/*.md 안 placeholder도 정정)
- **`/learn:*`, `/journal:*` 안내 제거**: 옛 첫 작업 안내의 학습/일지 슬래시 제거. 본인 회고 학습 트랙은 ADR-025로 은퇴 (work-pin 단일 핸드오프 + knowledge 트랙 A만).
