# Knowledge System — AI 캐시 도메인별 _index + GC

> **헌법 참조**: 본 정책은 새 헌법 v1 "📚 Knowledge 시스템" 섹션에서 링크됩니다.
> 충돌 시 헌법이 이깁니다.
>
> **신선도 주의**: 본 정책은 M3.5 박힘 시점(2026-05-20) 시드 단계. 실제 입출력 패턴 + GC 정책 정합은 Phase 04 산출물에서 정착. 본 정책은 *원칙·경계*만 박음.

본 문서는 SubAgent가 도메인별 학습을 *축적·조회*하는 Knowledge 시스템(트랙 A)과 *GC Collector* 정책을 정의합니다. (본인 회고 학습 일지 = 옛 트랙 B는 ADR-025로 은퇴.) 디테일은 Phase 04 산출물 ([`../knowledge/`](../../.claude/knowledge/)).

---

## 1. 왜 Knowledge 시스템인가 (배경)

옛 운영의 함정 — AI는 매 세션 *백지*에서 시작합니다:

- 같은 함정(예: PacketGenerator `noManager` 기본값 트랩)을 매 마일스톤마다 다시 학습
- 도메인 패턴(예: Phase 10 broadcast race deterministic 재현)이 코드 박힌 후 잊혀짐
- `~/.claude/memory/`(개인 메모리)는 *MEMORY.md 자동 로드* 영역 — 도메인 SubAgent가 직접 조회하진 않음

**해결**: `.claude/knowledge/<domain>/_index.md` = SubAgent가 작업 시작 시 자기 도메인 캐시 조회 → 백지 비용 ↓. *git에 박힌 공유 캐시* → 현재 영호 단독, 미래 합류자 합류 시 동일 캐시 활용.

5/20 의논 결과 — KPI 전환("학습 박제 중심" → "Planning → 구현 → 보고"). AI 활용 캐시(트랙 A)만 유지, 본인 회고 일지(옛 트랙 B)는 **ADR-025로 은퇴**:

| 트랙 | 위치 | 용도 | 작성자 |
|---|---|---|---|
| **트랙 A — Knowledge** | `.claude/knowledge/<domain>/_index.md` | AI가 직접 조회·활용 (구조화 패턴) | AI 박제 (사용자 확인 후) |

**경계 명확화**: knowledge(트랙 A)는 *코드 패턴·도메인 결함·재현 시나리오*만. *왜 그렇게 결정했나*는 ADR. 가짜 학습 방지를 위해 회고는 박지 않음 (ADR-013 정신 + ADR-025 트랙 B 은퇴).

---

## 2. 폴더 구조

```
.claude/knowledge/
├── _usage.md                    AI/사용자가 어떻게 활용하는지 가이드
├── server/_index.md             02_Server/ + 98_Shared/ 서버측 학습 캐시
├── shared/_index.md             98_Shared/ 단독 (Protocol/공식) 캐시
├── client/_index.md             03_Client/ + 04_ClientNet/ 캐시
├── qa/_index.md                 99_Tools/ + 테스트 + 시뮬레이션 캐시
└── cross-cutting/_index.md      도메인 횡단 (보안/성능/툴 함정/마이그 패턴 등)
```

### 도메인별 _index.md 구조 (Phase 04 산출물 시드)

```markdown
# {Domain} Knowledge — _index.md

## 활성 항목 (최근 ~3개월)

| 키워드 | 한 줄 요약 | 트리거 | 검증 |
|---|---|---|---|
| `false-promise-pattern` | 주석 약속은 가짜다 — 코드+문서 동시 봉합 | 헌법 #4 또는 README 약속 변경 시 | Rule of Three (실측 3건) |
| `broadcast-race-pattern` | N-1 fan-out lifecycle race deterministic 재현 | broadcast 패킷 신설 시 | Phase 10 + Phase 04 |
| ... | ... | ... | ... |

## 디테일 본문 (키워드별 ~50줄)

### `false-promise-pattern`

(증상 / 패턴 / 봉합 방법 / 검증된 사례 / 키워드 link)

---

## 비활성 / GC 대기 (3개월+ 무참조)

| 키워드 | 마지막 참조 | GC 후보 사유 |
|---|---|---|
| ... | ... | ... |
```

---

## 3. 입력 — 새 학습 박는 시점

다음 시점에 *사용자 확인 후* `_index.md`에 박힘:

| 시점 | 트리거 | 책임 |
|---|---|---|
| **`-DONE.md` 박제 직후** | `학습 일지 후보 키워드` 섹션 검토 → 트랙 A 후보 추출 | AI 제안 + 사용자 결정 |
| **CHANGELOG [H] 또는 [M] 박힘 직후** | 행동 변경 또는 결정 뒤집기 = 트랙 A 후보 | AI 제안 + 사용자 결정 |
| **사용자 명시 요청** | "이거 knowledge에 박아줘" | AI 즉시 박제 |

**AI 자율 박제 금지** — 사용자 확인 없이 박힌 학습은 *AI의 자기 강화 편향* 위험 (자기가 박은 패턴을 자기가 인용 → 검증 없는 순환). 사용자 확인 = 사실 검증 게이트.

### 박는 양식

```markdown
### `<keyword-kebab-case>`

**증상**: <한 줄, 무엇이 일어났는지>
**패턴**: <한 줄, 왜 일어나는지>
**봉합**: <한 줄, 어떻게 막는지>
**사례**: <Phase NN 또는 commit hash 1~3개>
**확신도**: 실측 N건 (Rule of Three 통과 = 3건 이상)
**관련 키워드**: [[다른 키워드 1]], [[다른 키워드 2]]
```

---

## 4. 출력 — SubAgent가 조회하는 시점

각 SubAgent는 작업 시작 시 *자기 도메인 + cross-cutting _index.md*를 자동 통독:

| SubAgent | 통독 대상 |
|---|---|
| `server` | `server/_index.md` + `shared/_index.md` + `cross-cutting/_index.md` |
| `shared` | `shared/_index.md` + `cross-cutting/_index.md` |
| `client` | `client/_index.md` + `cross-cutting/_index.md` |
| `qa` | `qa/_index.md` + 작업 도메인 _index + `cross-cutting/_index.md` |
| `reviewer` | 전체 _index.md (R only) |
| `plan-auditor` | 전체 _index.md (R only) |
| `unity-bridge` | `client/_index.md` + `cross-cutting/_index.md` |
| `coordinator` | 전체 _index.md (R only, 분해 시 활용) |

**적용 방식**: SubAgent 정의(`../agents/<name>.md`) 시스템 프롬프트에 *작업 시작 시 자기 도메인 _index.md를 먼저 통독한다*고 박음. 디테일은 Phase 02·04 산출물.

---

## 5. GC Collector — 오래된 / 중복 / 결함 정리

캐시는 *비대해지면 가치 ↓* — 매 작업마다 통독 부담 ↑ + 잘못된 패턴이 살아남음. GC Collector 정책:

### 5-1. 자동 비활성화 (3개월 무참조)

- `_index.md`에서 *마지막 참조 날짜* 추적
- 3개월 무참조 → "비활성 / GC 대기" 섹션으로 이동
- 6개월 무참조 → 사용자 확인 후 *완전 삭제* (git history 잔존)

### 5-2. 중복 정리

- Rule of Three 미달(실측 1~2건) 항목이 6개월 이상 1건 머무름 → 비활성화 후보
- 두 키워드가 *같은 패턴*임 발견 시 → 한 곳 흡수 + 별칭 표기

### 5-3. 결함 발견 정리

- 박힌 학습이 *후속 실측에서 틀린* 것 판명 → 즉시 정정 또는 삭제
- 예: γ 4회차에서 발견된 옛 패턴이 5회차에서 false 판명 시

### 5-4. GC 실행 시점

- **수동**: 사용자가 `/harness-review` 슬래시 호출 시 (Phase 05 산출물)
- **자동**: 매 마일스톤 마감 후 `/session:end` 흐름 안에 GC 점검 단계 추가 (Phase 04 결정)

---

## 6. 트랙 A 경계 (가짜 학습 방지)

**knowledge(트랙 A)** = AI가 직접 활용. 구조화·재현 가능·검증된 패턴만. (옛 트랙 B "본인 회고 학습 일지"는 ADR-025로 은퇴.)

### knowledge에 박을 것 / 안 박을 것

- ✅ 박음: *어떻게 검출하나 / 어떻게 막나* (패턴 구조) — 예: `PacketGenerator noManager 기본값`
- ❌ 안 박음: *왜 그렇게 결정했나* (회고·결정 이유) → 그건 ADR

---

## 7. 함정 / 주의사항

- **AI 자율 박제 금지** — 사용자 확인 없이 박힌 캐시는 *자기 강화 편향* 위험
- **knowledge에 회고/결정 이유 박지 마라** — 트랙 A는 *구조화 패턴*만. 결정 이유는 ADR (회고 트랙 B는 ADR-025 은퇴)
- **GC는 *완전 삭제 금지*가 기본** — 비활성화 후 6개월 후에야 사용자 확인 + 완전 삭제. git history는 잔존
- **AI 캐시 ≠ ADR** — ADR은 *결정의 기록*(왜 이 선택을 했는지). 캐시는 *작업 시점 활용 가능한 패턴*. 같은 사건이 ADR + 캐시 양쪽에 박힐 수 있음

---

## 8. 변경 시 동기화 책임

본 정책 수정 시 *반드시* 함께 갱신:

- [`../CLAUDE.md`](../../CLAUDE.md) "📚 Knowledge 시스템" 섹션
- [`../agents/`](../../.claude/agents/) (SubAgent 정의 — 각자 _index.md 통독 약속 박힘)
- [`../knowledge/_usage.md`](../../.claude/knowledge/_usage.md) (Phase 04 산출물 — AI/사용자 활용 가이드)
- [`subagent-routing.md`](subagent-routing.md) (SubAgent 정의 → 통독 대상 매핑)

---

## 9. 실측 후 재조정 항목

본 정책은 *시드*. Phase 04 산출물 + M4 진입 후 첫 1주 안에 다음 관찰 → 명세 갱신:

- [ ] **_index.md 통독 부담** — SubAgent 작업 시작 시 매번 통독이 토큰 부담 큰가, 적절한가
- [ ] **사용자 확인 마찰** — AI가 박제 제안 빈도 vs 사용자 승인 비율
- [ ] **GC 발동 적정성** — 3개월/6개월 임계가 너무 짧은지 긴지
- [ ] **트랙 A ↔ B 경계 혼란** — "이건 트랙 A인가 B인가?" 판단 어려움 빈도
- [ ] **자기 강화 편향** — AI가 자기가 박은 패턴을 자기가 인용 검증 없이 → 실제 발생 사례

재조정 결과는 본 정책 직접 수정 또는 ADR-024 신설(변경 폭에 따라).

---

## 갱신 이력

- 2026-05-20 — M3.5 Phase 01 (2/2)에서 신설(시드). 5/20 의논 결과(Knowledge 시스템 풀세트 + GC Collector + 트랙 A/B 분리) 박힘. 디테일은 Phase 04 산출물에서 정착.
