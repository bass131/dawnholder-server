# Phase 11: M2.5 정리 (헌법 우선순위 표 + CONTEXT 응축 + UnitTest1 삭제)

> **상태**: pending
> **마일스톤**: M2.5 Hardening
> **예상 소요**: 1시간
> **담당 에이전트**: (메인 세션 직접 — 문서 작업, 도메인 에이전트 위임 X)

---

## 🎯 목표

M2.5 audit (γ 방식)에서 발견된 작은 정리 항목 3건을 일괄 정리. M3 진입 시 토큰 비용 최소화 + 합류 팀원 첫 혼란 차단. 코드 변경 1줄(파일 삭제) + 문서 3건.

본 Phase는 *선택적*. Phase 09 + 10 완료 후 바로 M3 진입해도 OK. 단, CONTEXT.md 296줄 응축은 M3 진입 *직전*이 적기 (다음 세션 시작 토큰 비용 -50%).

---

## ⏪ 사전 조건

- [ ] Phase 09 완료 (Trust-boundary fail-closed)
- [ ] Phase 10 완료 (Session lifecycle race 제거)
- [ ] M2.5 학습 일지 후보 박혀있음 (응축 시 옛 후보 누락 방지)

---

## 📝 작업 내용

### 1. 헌법 우선순위 표 — `policies/` 박기

- [ ] `CLAUDE.md` "충돌 시 우선순위" 표 (현재 L67 부근) 갱신:

  **현재**:
  ```
  **`CLAUDE.md`(헌법) > `00_Document/ADR/`(결정) > `00_Document/ARCHITECTURE.md`(구조) > `00_Document/PRD.md`(요구사항)**
  ```

  **변경 후**:
  ```
  **`CLAUDE.md`(헌법) > `00_Document/ADR/`(결정) > `00_Document/policies/`(운영) > `00_Document/ARCHITECTURE.md`(구조) > `00_Document/PRD.md`(요구사항)**
  ```

- [ ] policies가 ADR 하위인 근거 한 줄 추가: "policies는 ADR 결정의 *운영 풀이*라 ADR 하위."

### 2. CONTEXT.md 응축 (296줄 → ~200줄)

- [ ] 현재 296줄. 본문 L8 "~200줄 넘으면 큰 마일스톤 끝날 때마다 처음부터 재작성" 박혀 있음. M2 + M2.5 마감 = "큰 마일스톤 끝".
- [ ] 응축 대상:
  - "현재 멈춤 지점" 섹션의 *옛* M2 Phase 07/06/05 디테일 → `CONTEXT_History.md`로 이전 (요약 1줄만 유지)
  - "M2 진행 현황" 표는 *완료 체크* 형식으로 압축
  - "합류 직전 세팅 요약" 5파트 — 완료 사항이므로 *한 단락*으로 응축
  - "보류 중" 11개 항목 — 실제 살아있는 것만 남기고 폐기 결정
- [ ] 새 상단: M2 + M2.5 완료 + M3 진입 직전 컨텍스트만.
- [ ] 옛 디테일은 `CONTEXT_History.md` 한 줄 박제 + git history 위임.

### 3. UnitTest1.cs 삭제

- [ ] `02_Server/GameServer.Tests/UnitTest1.cs` 통째 삭제 (`git rm`).
- [ ] `dotnet test` 카운트가 110→109로 줄어드는지 확증 (의미 없는 통과 1건 제거).

### 4. CHANGELOG 박제

- [ ] `.claude/CHANGELOG.md` 최상단에 [L] 한 줄:
  ```
  | 2026-05-XX | **M2.5 정리** — 헌법 우선순위 표에 `00_Document/policies/` 한 줄 보강 + CONTEXT.md 296→200줄 응축 + UnitTest1.cs 빈 placeholder 삭제. Phase 11 마감. | [L] |
  ```

---

## ✅ 완료 조건

- [ ] `CLAUDE.md` 표에 `00_Document/policies/` 줄 존재 (grep 확증):
  ```bash
  grep -E "policies.*ARCHITECTURE" CLAUDE.md
  ```
- [ ] `CONTEXT.md` ≤200줄:
  ```bash
  wc -l CONTEXT.md  # 결과 ≤ 200
  ```
- [ ] `02_Server/GameServer.Tests/UnitTest1.cs` 없음:
  ```bash
  test ! -f 02_Server/GameServer.Tests/UnitTest1.cs
  ```
- [ ] `dotnet test` 통과 (테스트 수 110→109 또는 +new Phase 09/10 테스트 만큼 증가)
- [ ] `.claude/CHANGELOG.md` 신규 줄 박혀 있음
- [ ] `11-m25-cleanup-DONE.md` 작성

---

## 🧪 테스트

**자동:**
- `dotnet test` (전체 회귀)
- 위 grep/wc/test 명령 3건

**수동:**
- 다음 세션 `/session:start` 시 CONTEXT.md 로드가 명확히 응축본인지 시각 확증.
- `CLAUDE.md` 표 가시성 확증.

---

## 📚 학습 포인트

- **응축의 자가-검증** — CONTEXT.md 본문이 "200줄 한도" 박혀있는데 296줄로 자가-위반. 자기참조 정책의 함정. 정책은 강제 메커니즘(훅, CI)이 없으면 *지키지 않는다*는 일반 통찰.
- **빈 테스트의 정직성 비용** — UnitTest1.cs는 통과 카운트만 올림. CI 시그널 약화의 미시 사례. "통과 N건"이 *의미 있는 N건*이어야 신호.
- **헌법 자기일관성** — `policies/`가 본문엔 박혔는데 우선순위 표에 누락. 헌법 자체가 합류 팀원 첫 혼란점. 헌법 작성자도 자기 글의 *내부 참조*를 다 못 잡는다는 일반 통찰.
- **Codex β와 Claude α 우선순위 자체가 뒤집힐 수 있는 시각 차이** — 학습 일지 ★★★ "AI 리뷰어 두 명의 시각 보완성" 후속 실증.

---

## ⚠️ 함정 / 주의사항

- **CONTEXT.md 응축 시 *현재 멈춤 지점*과 *다음 액션*은 반드시 갱신** — 옛 M2 갱신 그대로 두면 다음 세션 핸드오프 깨짐. 새 상단은 "M2 + M2.5 완료 / M3 진입 직전 / 다음 액션 = /work:plan M3" 형식.
- **응축본은 *누적 X*** — 옛 디테일은 `CONTEXT_History.md`로 이전. `CONTEXT_History.md`는 누적 OK.
- **`CLAUDE.md` 표 변경 위험도 [M]** — 절대 원칙 *수정*은 아니지만 헌법 변경. CHANGELOG 박제 필수. 본 Phase는 *추가*만 (제거/수정 X)이라 [L] 분류 가능 — 본인 판단.
- **`git rm UnitTest1.cs` 후 `.csproj` 참조 확인** — `<Compile>` 명시 참조면 별도 제거. SDK default glob이면 자동 정리.
- **응축 후 *학습 일지 후보*는 외부 파일 (`CONTEXT_LearningJournalCandidates.md`, gitignored)에 박혀있음 확증** — CONTEXT.md 본문 응축이 외부 후보 누락으로 이어지지 않도록.

---

## ➡️ 다음 Phase

- **M3 진입** — `/work:plan M3` 호출 → 첫 Phase 분해. M3 핵심 후보:
  - ProtocolVersion 핸드셰이크 (C_Handshake/S_HandshakeAck 한 쌍)
  - 핸들러 layer 분리 (`02_Server/GameServer/Handlers/` 신설 + `02_Server/CLAUDE.md` Layout 동시 갱신)
  - 두 명 같은 맵 broadcast (M3 핵심 목표)
  - Handler 단위 invalid/auth 테스트 (γ 감사 관찰 발견 후속)

---

## 작업 로그

- 2026-05-18: Phase 분해 완료. Phase 09 + 10 마감 후 진입. *선택적* Phase — 바로 M3 진입도 가능.
