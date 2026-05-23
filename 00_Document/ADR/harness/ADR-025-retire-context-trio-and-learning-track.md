# ADR-025 — CONTEXT 3종 + 학습 일지 트랙 B 은퇴, work-pin 단일 핸드오프

- **상태**: accepted
- **날짜**: 2026-05-24
- **결정자**: 유영호 (하네스 단독 통제)
- **관련**: ADR-013 (-DONE.md 페어 — 회고 절반 supersede) / ADR-022 (하네스 v1 — 학습 트랙 분리 정신 supersede) / ADR-023 (work-pin↔CONTEXT 동기 — CONTEXT 절반 supersede) / ADR-024 (false-promise cadence — 유지)

---

## 맥락 (왜 바꾸나)

새 하네스 v1(ADR-022)은 *학부생이 학습하며 만든다*는 전제로 설계됐다. 그 전제 위에 세 가지 부속이 박혔다:

1. **CONTEXT 3종** — `CONTEXT.md`(세션 핸드오프) + `CONTEXT_History.md`(세션 일지) + `CONTEXT_LearningJournalCandidates.md`(★★ 후보 intake)
2. **학습 일지 트랙 B** — `00_Document/learning-journal/` + `/session:end` 일지 권유 + work-pin "★★ 후보 누적" 추적
3. **`/session:start`의 CONTEXT 통독** — 매 세션 진입 시 `CONTEXT.md`를 컨텍스트 창에 적재

실측 결과 이 부속들이 *가치보다 비용*이 커졌다:

- **컨텍스트 낭비**: work-pin이 이미 매 턴 자동 주입(현재 작업/다음 액션)되는데, `/session:start`가 `CONTEXT.md`를 *또* 적재 → 같은 핸드오프 정보가 이중으로 컨텍스트를 먹음. 사용자 컨텍스트(신분/목표)는 memory(`~/.claude/.../memory/`)가 이미 보유.
- **회고 안 함**: ★★ 후보가 60건+ 쌓였으나 실제 학습 일지로 전환된 건 거의 없음 → "언젠가 회고" TODO가 work-pin·History를 비대화(harness-review 2026-05-24 양식 비용 평가: work-pin 104줄 = 목표 2.6배). 학습 추적이 *가치 있는 척하는 오버헤드*로 전락.
- **"학습 호흡" 전제 소멸**: Phase 단위 수동 멈춤의 *학습* 명분이 사용자에게 더는 의미 없음 (목표가 "배우기"→"밀기"로 이동). 체크포인트의 *공학적* 명분(드리프트·비가역 게이트)은 별개로 유지.

## 결정

세 부속을 **은퇴**한다.

| 항목 | 전 | 후 |
|---|---|---|
| 세션 간 핸드오프 (지금 어디 + 다음) | CONTEXT.md + work-pin (이중) | **work-pin 단일** (`.claude/state/current-pin.txt`, 매 턴 자동 주입) |
| 안 변하는 사용자 컨텍스트 (신분/목표/일정) | CONTEXT.md "사용자 컨텍스트" 섹션 | **memory** (`~/.claude/projects/.../memory/`) |
| 세션 일지 | CONTEXT_History.md | git history + CHANGELOG + (선택) Notion |
| 학습 일지 트랙 B | learning-journal/ + CONTEXT_LearningJournalCandidates.md + ★★ 추적 | **은퇴** (기존 `learning-journal/{본인}/` 잔존분은 *각자 작업물*이라 보존, 신규 안 함) |
| `/session:start` | CONTEXT.md 통독 | CLAUDE.md(자동) + work-pin + 최근 git/CHANGELOG만 |
| `/session:end` | commit/PR/Notion + CONTEXT/History 갱신 + 일지 권유 | commit/PR/(선택)Notion + work-pin 갱신만 |

**삭제 파일**: `CONTEXT.md`, `CONTEXT_History.md`, `CONTEXT_LearningJournalCandidates.md`.

**보존**: `00_Document/learning-journal/{youngho,yuhyeon}/` 기존 잔존분 (남의 작업물 포함 — 단독 삭제 X). 신규 작성/권유만 중단.

## 결과

- **얻음**: 세션 시작 컨텍스트 적재 ↓ + work-pin 다이어트 가능(★★ 추적 줄 제거) + 핸드오프 단일 진실 → drift 표면 ↓.
- **잃음**: 서사형 개발 일지(History)·회고 자산. → 캡스톤이 *과정 기록*을 평가 자료로 요구하지 않음을 확인함(2026-05-24). 필요 시 git/CHANGELOG/Notion에서 복원.
- **supersede**:
  - ADR-013 (-DONE.md 페어)의 *본인 회고 절반* 무효 (AI 사실 박제 `-DONE.md`는 유지).
  - ADR-023 (work-pin↔CONTEXT 동기 게이트)의 *CONTEXT 절반* 무효 → drift 발견 게이트는 work-pin 단독 기준으로 유지.
  - ADR-022의 *학습 트랙 A/B 분리* 중 트랙 B 무효. 트랙 A(knowledge AI 캐시)는 유지.
- **헌법/정책 sweep 동반**: CLAUDE.md "학습 일지/트랙 B" 절 + `policies/{pin-and-done,knowledge-system,doc-thresholds,pr-and-merge-gate,reporting-format}.md` + `commands/session/{start,end,log}.md` + `commands/{setup,_mapping}.md` + `agents/{knowledge-gc,shared,_routing}.md` 참조 제거. dangling 참조 0 = false-promise 신규 생성 방지(ADR-024 정신 정합).

## 한계 / 모니터링

- work-pin이 단일 핸드오프가 되므로 *work-pin 자체 비대*가 다시 위험. pin-and-done.md 30~40줄 목표 + 마감 commit 이력은 CHANGELOG 위임 규율 강화 동반.
- knowledge(트랙 A)는 유지하므로 "AI 자율 박제 금지" 게이트는 그대로.
