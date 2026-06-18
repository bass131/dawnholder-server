---
name: knowledge-shared-index
description: Shared 도메인 (98_Shared/ Protocol/공식/공유 상수) 학습 캐시 인덱스
domain: shared
maintainer: youngho
last_updated: 2026-05-24
---

# Shared Knowledge — _index.md

> **누가 통독**: `shared` SubAgent (필수) + `server` (함께 통독) + `coordinator` / `reviewer` / `plan-auditor` (R only)
> **함께 통독**: 본 캐시 + [`../cross-cutting/_index.md`](../cross-cutting/_index.md)
> **박는 시점**: `-DONE.md` 박제 직후 / CHANGELOG [M]/[H] 직후 / 사용자 명시 요청. **AI 자율 박제 금지**.
> **양식·박는 방법**: [`../_usage.md`](../_usage.md) 4번 섹션

---

## 활성 항목 (최근 3개월)

| 키워드 | 한 줄 요약 | 트리거 | 검증 |
|---|---|---|---|
| `false-promise-pattern` | 주석/문서/CLAUDE.md에 박힌 약속이 코드에 없음 — 코드+문서 동시 commit으로 봉합 | 헌법 / 도메인 README / Layout 표 갱신 시 / "약속 박기"라는 문구 | M3 Phase 02/03/04 (Rule of Three 통과 ★★★) |
| `shared-code-discipline-relocation-pattern` | 서버 권위 ≠ 서버 전용 코드 *위치* — 양쪽이 읽는 타입(PlayerStats 등)은 98_Shared/ 소속 | 양쪽 공유될 타입 위치 결정 시 / "이거 02_Server에 둘까 98_Shared에 둘까" | M4.1 Phase 05 (실측 1건, ★★) |
| `init-setter-net-standard-2-1-trap` | .NET Standard 2.1엔 IsExternalInit 부재 → `init` setter CS0518 → private ctor + factory로 회피 | 98_Shared에 `init`/`record`/위치 지정 setter 추가 시 | M4.1 Phase 05 (실측 1건, ★★) |

---

## 디테일 본문

### `false-promise-pattern`

**증상**: 헌법 #4 ("Shared Code Discipline — 복사-붙여넣기 금지") 또는 `02_Server/CLAUDE.md` Layout에 박힌 약속(`Handlers/` 폴더 등)이 *실재로는 없음*. AI가 "여기 있을 거예요" 답변 → 실측해보면 가짜.

**패턴**: 문서 약속은 *작성 시점*의 의도. 코드 변경이 동시 commit되지 않으면 *약속만 박히고 코드는 안 박혀* 시간 지나면 가짜화. AI가 다음 세션 그 약속을 *사실*로 인용 → 검증 없는 순환.

**봉합 (코드 + 문서 동시 commit 정신)**:
- 약속 박는 commit이 *반드시* 코드 + 문서 *동시* 변경
- 코드만 박고 문서 미갱신 = 다음 세션 헷갈림
- 문서만 박고 코드 미구현 = 가짜 약속 누적
- 후속 ad-hoc 감사로 "약속 vs 코드" 격차 점검

**Rule of Three 누적 사례**:
- M3 Phase 02 (commit `e91XXX`) — 헌법 #2 "ProtocolVersion 핸드셰이크" 가짜 약속 1번째 봉합 (PDL 신설 + 코드 동시)
- M3 Phase 03 (commit `4065616`) — 헌법 #4 "Handlers/ 폴더" 가짜 약속 2번째 봉합 (02_Server/CLAUDE.md Layout *동시* commit)
- M3 Phase 04 (commit `5ea1123`) — 헌법 #4 3번째 봉합, Rule of Three 통과 → 패턴 정착

**사례**: M3 Phase 02 (1번째) → Phase 03 (2번째) → Phase 04 (3번째). Rule of Three 통과 후 M3.5에서 본 캐시에 박힘.
**확신도**: 실측 26건+ (★★★). M3 3건 → M4.1 누적 26번째까지 (Codex 슬래시 23 + CLAUDE.md:38 stale + Codex β cross-review 4건 + 98_Shared/CLAUDE.md Layout stale 등). ADR-024 false-promise cadence로 제도화됨. 한국 게임 회사 면접 *문서/코드 정합* 어필 결정타. 헌법 변경 시 *반드시* 코드+문서 동시 commit 강제.
**관련 키워드**: [[gamma-pre-validation-pattern]] (사전 검증으로 가짜 약속 차단 가능), [[prefab-overwrite-untracked-disaster]] (절차 박혀도 안 지키면 가짜)

### `shared-code-discipline-relocation-pattern`

**증상**: "데미지·HP·스탯은 서버 권위(헌법 #1)니까 `02_Server/`에 둔다"고 *위치*까지 서버 전용으로 오해. 실제로 클라가 *표시*를 위해 같은 타입(예: `PlayerStats`)을 읽어야 하면, 코드를 복붙하거나 클라에서 재정의 → 헌법 #4(복사-붙여넣기 금지) 위반.

**패턴**: 헌법 #1(Server Authority)은 *실행·판정 권위*를 서버에 둔다는 뜻이지 *코드 파일 위치*를 서버 전용으로 못박는 게 아니다. 데미지 *공식*과 그 입력 타입(스탯)은 양쪽이 합의해야 할 *계약* → `98_Shared/GameData/` 소속. 실행(데미지 적용)만 `02_Server/`.

**봉합 (M4.1 Phase 05 실측)**:
- `PlayerStats`를 `02_Server/` → `98_Shared/GameData/`로 이동. `Formulas.ComputeDamage(...)` 순수 함수도 98_Shared에 신설.
- 서버는 *호출·적용*만 (`GameMap.ProcessAttack`이 `Formulas`에 위임). 권위는 그대로 서버.
- 판단 기준: "클라가 이 타입을 *읽기라도* 하나?" → Yes면 98_Shared. "클라가 이 *값을 바꾸나?*" → 그건 서버 전용 실행 (≠ 타입 위치).

**확신도**: 실측 1건 (★★, Rule of Three 미달이나 방지 비용 낮음). 미래 shared/server Worker가 공유 타입을 서버 전용으로 잘못 위치시키는 사고 예방.
**관련 키워드**: [[false-promise-pattern]] (헌법 약속을 코드로 정합화), [[init-setter-net-standard-2-1-trap]] (이동 시 동반 함정)

### `init-setter-net-standard-2-1-trap`

**증상**: `02_Server/`(.NET 10)에서 잘 컴파일되던 `init` setter(또는 `record`)를 `98_Shared/`(.NET Standard 2.1)로 옮기면 **CS0518 `IsExternalInit` 미정의** 컴파일 오류. .NET Standard 2.1엔 `System.Runtime.CompilerServices.IsExternalInit` 타입이 없어서.

**패턴**: `init`/`record`/일부 위치 지정 setter는 컴파일러가 `IsExternalInit` shim 타입을 요구. .NET 5+는 기본 제공하나 .NET Standard 2.1은 부재. 98_Shared는 헌법상 .NET Standard 2.1 빌드라(ADR-010, Unity 참조 위해) cross-runtime 함정.

**봉합 (두 길)**:
- (택1) `IsExternalInit` shim을 98_Shared에 직접 정의 (internal static class)
- (택2, Phase 05 채택) `init` setter 포기 → **private ctor + 정적 factory 메서드**로 불변성 확보. shim 의존 없이 양쪽 런타임 안전.

**확신도**: 실측 1건 (★★). 98_Shared에 `init`/`record` 추가하는 모든 미래 작업에 트리거. shared SubAgent 통독 가치 ↑.
**관련 키워드**: [[shared-code-discipline-relocation-pattern]] (이동 작업의 동반 함정)

---

## 비활성 / GC 대기 (3개월+ 무참조)

_(없음 — 본 캐시는 2026-05-20 신설)_

---

## 도메인 경계

이 캐시는 *98_Shared/ Protocol + 공식 + 공유 상수* 패턴을 담습니다:

- **포함**: PDL 정의·생성기 / Protocol.Version bump / 공유 enum / 공식 (데미지 계산 등) / cross-platform 직렬화 / Shared.dll 동기화
- **제외**:
  - 서버측 실행 로직 (handler / lifecycle) → [`../server/`](../server/_index.md)
  - 클라측 dispatch / prediction → [`../client/`](../client/_index.md)
  - 환경 사고 / 툴 함정 → [`../cross-cutting/`](../cross-cutting/_index.md)

---

## 관련 자산

- 헌법: [`../../CLAUDE.md`](../../../CLAUDE.md) "Protocol is Sacred" + "Shared Code Discipline" 절대 원칙
- 정책: [`../../policies/knowledge-system.md`](../../../00_Document/policies/knowledge-system.md)
- SubAgent 정의: [`../../agents/shared.md`](../../agents/shared.md)

---

## 갱신 이력

- 2026-05-20 — M3.5 Phase 04 (1/3) 골격 박힘. 시드 항목은 (2/3)에서 채움.
- 2026-05-24 — `/harness-review all` knowledge-gc 후속. `shared-code-discipline-relocation-pattern` + `init-setter-net-standard-2-1-trap` 신규 박제 (M4.1 Phase 05 ★★). `false-promise-pattern` 확신도 3건 → 26건+ 갱신.
