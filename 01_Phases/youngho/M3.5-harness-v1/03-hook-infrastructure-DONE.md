---
summary: M3.5 Phase 03 — Hook 인프라 7개 풀세트 (pin-injector + phase-gate-validator 정합 정정 / dangerous-cmd-guard + tdd-guard + circuit-breaker + risk-detector + shared-discipline-guard 신설) + settings.proposed.json 박음. 함정 3종(circuit-breaker false positive 차단 / risk-detector 자동 등급 상향 / shared-discipline-guard PDL 의무 3종 강제) 핵심. 옛 운영 영향 0.
phase: M3.5/03
status: done
owner: youngho
grade: 대규모
---

## TL;DR

새 하네스 v1의 Hook 인프라 풀세트를 박음:
- `New_Harness/hooks/` 7개 `.sh` (옛 5 → 신규 5 + 정합 정정 2 = 새 7. 옛 `check-work-envelope` + `check-server-authority` 삭제)
- `New_Harness/settings.proposed.json` (hooks 7 매핑 + Auto Mode permissions + deny 확장)
- `New_Harness/hooks/README.md` (풀세트 표 + 옛 → 새 매핑 + 우회 정책 + 실측 재조정 5항목)
- TDD 강제 영역 결정 (옛 보류 항목 해소): `02_Server/Handlers/` + `GameSession.cs` + `98_Shared/Protocol/Packets/` + `98_Shared/GameData/`

옛 `.claude/hooks/` 5개 + `.claude/settings.json`은 **그대로** → 옛 운영 100% 작동. Phase 06 전환 commit 시점에 일괄 mv 예정.

---

## 5단계 보고

> Phase 03 등급 = *대규모* — 새 헌법 v1 기준 5단계 보고 + MD/HTML 이중 박음 필수. HTML 박음은 Phase 06 전환 후 발효(`00_Document/reports/`로 mv). 본 시점은 MD만.

### 🎯 무엇을 만들었나

3 commit 누적 (`e2faae9` + `ef4682c` + 본 commit), 총 9 파일 / ~1100여줄:

**(1/3) `e2faae9` — 옛 정합 정정 + 골격 (4 파일 / 359줄)**:
- `hooks/pin-injector.sh` (60줄) — 옛 `inject-current-pin.sh` 복사 + 정책 경로 정합 (ADR-018 → `policies/pin-and-done.md`). 본문 로직 동일
- `hooks/phase-gate-validator.sh` (132줄) — 옛 `validate-phase-gate.sh` + `grade`/`owner` frontmatter 필수 추가 + 5단계 보고 섹션 = 대규모만 의무 + MD/HTML 이중 박음 경고
- `settings.proposed.json` (86줄) — pure JSON. hooks 7 매핑 + Auto Mode 정책 (5/18 ε) + deny 확장 (`.env.*` + `appsettings.Secrets.json`)
- `hooks/README.md` (88줄) — hook 7개 풀세트 표 + 옛 5 → 새 7 매핑 + 우회 정책 표 (차단 3 vs 경고 4) + 실측 재조정 5항목

**(2/3) `ef4682c` — 신규 5 hook (★ 함정 3종 + dangerous + tdd) (6 파일 / +573 -5)**:
- `hooks/dangerous-cmd-guard.sh` (101줄) — PreToolUse Bash 차단. 7 패턴: rm -rf / git reset --hard / git checkout --force / git push --force / git clean -fd / main force push / gh pr merge --admin. exit 2 우회 불가
- `hooks/tdd-guard.sh` (98줄) — PreToolUse Edit/Write 경고만. TDD 영역 4 (Handlers / GameSession / Protocol/Packets / GameData) 대응 테스트 부재 시 stderr 알림 + `.claude/state/tdd-guard-log.txt` 누적
- `hooks/circuit-breaker.sh` (95줄) ★ — PostToolUse 알림. 같은 도구 N회 반복 (Bash 제외) → 등급별 임계 차등 (단순 5 / 보통 10 / 복잡 15 / 대규모 20), 윈도우 5분. **false positive 차단 핵심**
- `hooks/risk-detector.sh` (124줄) ★ — PreToolUse Bash/Edit/Write. 3 깃발 자동 검출 (trust-boundary / irreversible / unity-asset) → stderr 알림 + `.claude/state/risk-flags.txt` 누적. **등급 자동 상향 통보 핵심**
- `hooks/shared-discipline-guard.sh` (135줄) ★ — PreToolUse Edit/Write. PDL.xml 변경 + GenPackets stale → exit 2 차단. PDL + Shared.dll commit 동반 X → exit 2. ProtocolVersion 미검토 → 경고. **"주석 약속은 가짜다" 3회 봉합 사고 봉합 핵심**
- `hooks/README.md` 갱신 — 5 hook 상태 (2/3) ✅ 반영

**(3/3) 본 commit — README 매핑 갱신 + -DONE.md 박제 (예정)**:
- `New_Harness/README.md` — Phase 03 산출물 표 *완료* 상태 갱신 + 옛 hook 실제 이름 정정 (`check-authority` → `check-server-authority` / `validate-shared` → `validate-shared-changes`) + settings.proposed.json 행 추가
- 본 `-DONE.md` 박제

---

### 🤔 왜 필요한가

**핵심 동기**: 옛 운영은 헌법 문구로 약속 → "주석 약속은 가짜다" 봉합 3회 증명. Hook은 약속의 *물리적 강제*.

**함정 3종** (work-pin 강조 + Phase 정의 §함정):
1. **circuit-breaker 재귀 차단** — 옛 운영엔 없던 안전망. AI 무한 재시도 = 토큰/시간 낭비 + 잘못된 가정 누적. *동시에* 정당한 반복(테스트 fuzz 1000회 등) false positive 회피 필수 → Bash 제외 + 등급별 임계 차등
2. **risk-detector 자동 등급 상향** — 본인이 깜빡 단순 등급으로 처리하려는 변경이 `02_Server/Handlers/`였다(trust-boundary) → 헌법 #3 사고. 5/20 핵심 안전망
3. **shared-discipline-guard PDL 의무 3종** — 옛 `validate-shared-changes.sh`가 *경고만* 했다 → 5/17 정유현 pull 사고(Shared.dll commit 누락) 직접 원인. 강화 = exit 2 차단

**Phase 04와 병렬 영역 마감**: M3.5 6 Phase 중 *마지막 신규 인프라*. Phase 05/06은 정리 작업.

---

### 🛠️ 어떻게 만들었나

**옛 5 → 새 7 매핑 의사결정**:

| 옛 | 새 | 이유 |
|---|---|---|
| `check-work-envelope.sh` | (삭제) | 5/20 의논 — work-envelope 양식 자체 죽임 |
| `check-server-authority.sh` | (삭제) | grep으로 서버 권위 점검 = false positive 많음. Reviewer SubAgent가 코드 리뷰로 더 정확히 잡음 |
| `validate-shared-changes.sh` | `shared-discipline-guard.sh` (강화) | 경고만 → exit 2 + 의무 3종 자동 점검 |
| `inject-current-pin.sh` | `pin-injector.sh` | rename 정합 |
| `validate-phase-gate.sh` | `phase-gate-validator.sh` | rename 정합 + grade/owner frontmatter 필수 |
| (신설) | `dangerous-cmd-guard.sh` | settings.json deny 룰 PreToolUse 안전망 |
| (신설) | `tdd-guard.sh` | TDD 영역 점검 (경고만) |
| (신설) | `circuit-breaker.sh` | 무한 재시도 차단 (Bash 제외) |
| (신설) | `risk-detector.sh` | 위험 깃발 3종 자동 검출 |

**Hook 환경변수 추정**: 옛 hook이 사용한 `CLAUDE_TOOL_INPUT_FILE` (Edit/Write 대상 경로) 패턴 그대로. PreToolUse Bash는 `CLAUDE_TOOL_INPUT_COMMAND` 추정(공식 명세 의존) — 실측은 Phase 06 전환 후. Hook 본문은 `${CLAUDE_TOOL_INPUT_COMMAND:-${CLAUDE_TOOL_INPUT:-${1:-}}}` 패턴으로 양쪽 호환.

**격리 폴더 정신 유지**: `settings.proposed.json`은 *제안*. 옛 `.claude/settings.json`은 그대로. 새 Hook 7개도 자동 발동 X (settings.json에 박혀야 작동). Phase 06 전환 시점에 일괄 mv 후 발효.

---

### 🧪 테스트 결과

**자동 옛 운영 sanity check**:
```
dotnet build Dawnholder.slnx --nologo -v quiet
> 경고 0개 / 오류 0개 / 경과 시간 00:00:03.84
```
✅ 옛 운영 100% 작동 — 격리 폴더 안 작업, dotnet 솔루션 영향 0.

**시뮬레이션 5건** (AC 검증 결과 섹션에 상세 박힘) — 실제 발동 X, 본인 눈 통독:
1. `dangerous-cmd-guard`: `rm -rf /tmp/test-cleanup` → 차단 (exit 2) + 메시지 출력
2. `shared-discipline-guard`: PDL.xml 변경 + Shared.dll commit 미동반 → 차단
3. `risk-detector`: `02_Server/Handlers/PingHandler.cs` Edit → trust-boundary 깃발 + stderr 알림
4. `circuit-breaker`: Edit 도구 11회 반복 (보통 등급, 임계 10) → 알림 (Bash 제외 OK)
5. `tdd-guard`: 신규 Handler `.cs` 작성 + 테스트 부재 → 경고만 (exit 0)

**미검증** (Phase 06 전환 후 실측):
- PreToolUse Bash 환경변수 정확 명세 (`CLAUDE_TOOL_INPUT_COMMAND` 추정)
- Hook 실행 권한 (chmod +x) — Windows + Git Bash 환경 호환
- circuit-breaker false positive 빈도 (정당한 반복 차단 안 됨)
- risk-detector false positive 빈도 (`02_Server/Handlers/` 로깅 줄 추가 같은 케이스)

---

### ➡️ 다음 스텝

**즉시**:
- (3/3) commit + push origin (Phase 03 마감)
- work-pin 갱신 (Phase 03 ✅ + 다음 = Phase 05)

**다음 Phase 05** (~3~4h 복잡):
- 옛 슬래시 16개 → 새 10개 정리 (학습 5 + 일지 3 = 트랙 B Notion 이관 / 작업 4 + 세션 3 + 점검 2 + 셋업 1 유지)
- `/harness-review` `/cross-review` 신설 (수동 트리거)
- Phase 03 + 04 둘 다 마감 후 진입 가능 — 본 Phase 마감 후 *전제 조건 충족*

**그 후 Phase 06** (~2~3h 복잡):
- 옛 → 새 일괄 mv 전환 commit + ADR-022 박음 + CHANGELOG [H] entry
- M3.5 ↔ M4 게이트

---

## AC 검증 결과

### 완료 조건 5/5 PASS

| # | 조건 | 결과 |
|---|---|---|
| 1 | `New_Harness/hooks/` 안에 7개 `.sh` 박힘 | ✅ dangerous-cmd-guard / tdd-guard / circuit-breaker / risk-detector / shared-discipline-guard / pin-injector / phase-gate-validator |
| 2 | `New_Harness/settings.proposed.json` 박힘 (옛 settings.json 그대로) | ✅ pure JSON, hooks 7 매핑, 옛 `.claude/settings.json` 영향 0 |
| 3 | 각 Hook의 트리거 조건 + exit 정책 명세 | ✅ `hooks/README.md` 우회 정책 표 — 차단 3 (dangerous/shared-discipline/phase-gate) vs 경고 4 (tdd/risk-detector/circuit-breaker/pin-injector) |
| 4 | 옛 운영 100% 작동 (옛 hook 5개 그대로) | ✅ `dotnet build` green, 격리 폴더 안 작업 |
| 5 | 새 Hook 동작 시뮬레이션 시나리오 5건 문서화 | ✅ 본 섹션 아래 박힘 |

### 시뮬레이션 5건 (실제 호출 X — Phase 06 전환 후 실측)

#### 시뮬레이션 1 — `dangerous-cmd-guard` 차단

**시나리오**: AI가 `rm -rf 03_Client/Library` 호출 (Unity Library 폴더 정리 의도).

**기대 동작**:
- PreToolUse Bash Hook 발동
- `BLOCKED="rm -rf (재귀 강제 삭제)"` 매칭
- exit 2 + stderr:
  ```
  ❌ 파괴 명령 차단: rm -rf (재귀 강제 삭제)
    명령: rm -rf 03_Client/Library
    사유: 작업물 유실 위험. 특정 파일만 지우려면 'rm <파일>' 또는 'git checkout -- <파일>' 사용.
  ```
- Claude Code 도구 호출 차단 → AI가 별 접근 시도

**미검증 가정**: PreToolUse Bash 환경변수 명세. 입력이 `CLAUDE_TOOL_INPUT_COMMAND`로 들어오는지 또는 `$1` arg로 들어오는지. Hook 본문은 양쪽 호환.

---

#### 시뮬레이션 2 — `shared-discipline-guard` 차단 (PDL 의무 3종 위반)

**시나리오**: 본인이 `98_Shared/Protocol/PDL.xml`에 새 패킷 추가 후, PacketGenerator 재생성 잊고 `02_Server/Handlers/NewHandler.cs` 박으려 함.

**기대 동작**:
- PreToolUse Edit/Write Hook 발동 (98_Shared/ 외 파일이지만 *git status*에 PDL.xml dirty)
- `PDL_DIRTY` non-empty + `GEN_DIRTY` empty → PDL stale 검출
- `PDL_DIRTY` non-empty + `DLL_DIRTY` empty → Shared.dll 미동반 검출
- exit 2 + stderr 의무 3종 명세 출력

**옛 사고 봉합**: 5/17 정유현 pull 사고 (PacketGenerator 재생성 + Shared.dll commit 누락 → 정유현 빌드 깨짐). 옛 hook은 경고만 → 본인 진행해버림.

---

#### 시뮬레이션 3 — `risk-detector` 자동 등급 상향 통보

**시나리오**: 메인 세션이 *단순 등급*으로 인식한 작업 = `02_Server/Handlers/PingHandler.cs` 한 줄 로깅 추가.

**기대 동작**:
- PreToolUse Edit Hook 발동
- `TOOL_INPUT_FILE = 02_Server/Handlers/PingHandler.cs` → trust-boundary 매칭
- `FLAGS=(trust-boundary)` → 1 깃발
- stderr 알림 + `.claude/state/risk-flags.txt`에 한 줄 누적
- 메인 세션이 등급 *단순 → 보통* 인지 후 진행

**미검증 가정**: PreToolUse 환경변수가 *변경 전* 파일 경로를 정확히 노출하는지 (Write 신규 파일 vs Edit 기존 파일 양쪽 동작).

---

#### 시뮬레이션 4 — `circuit-breaker` 알림 (false positive 차단 확인)

**시나리오 A** — 정당한 반복:
- AI가 `Bash` 도구로 `dotnet test` 15회 호출 (fuzz 시나리오)
- 본 Hook: `Bash 도구 제외` → 즉시 exit 0
- 알림 X ✅

**시나리오 B** — 무한 재시도 의심:
- AI가 `Edit` 도구로 같은 파일 11회 호출 (work-pin 등급 = 보통, 임계 10)
- 본 Hook: 윈도우 5분 안 카운트 = 11 ≥ 10 → 알림
- stderr: `같은 도구 반복 호출 임계 도달 ... 본인이 판단:`
- 사용자가 멈춤·다른 접근 결정

**미검증 가정**: `CLAUDE_TOOL_NAME` 환경변수 노출 명세.

---

#### 시뮬레이션 5 — `tdd-guard` 경고 (학부생 학습 호흡)

**시나리오**: 본인이 `02_Server/Handlers/AttackHandler.cs` 신규 작성 (대응 `AttackHandlerTests.cs` 없음).

**기대 동작**:
- PreToolUse Write Hook 발동
- TDD 영역 매칭 (`02_Server/Handlers/`) + 테스트 파일 부재
- stderr 경고만 + `.claude/state/tdd-guard-log.txt`에 한 줄 누적
- exit 0 → Claude Code 작업 진행

**학부생 학습 호흡**: 차단이 아닌 *알림*. /work:plan 단계에서 테스트 페어 박을지 결정. 누적 로그가 "테스트 부재 코드" 추적.

---

## 결정 흐름

### 1. TDD 강제 영역 결정 (옛 보류 항목 해소)

Phase 03 정의 §함정 박혀있던 미해소 항목. 본 Phase 안에서 *4개 영역* 확정:

| 영역 | 이유 |
|---|---|
| `02_Server/Handlers/` | 헌법 #3 신뢰 경계 검증 — 한 줄 실수가 보안 구멍 |
| `02_Server/GameSession.cs` | 세션 lifecycle + first-packet 강제 — race condition 위험 |
| `98_Shared/Protocol/Packets/` | PDL 자동 생성 코드 — 변경 = breaking change |
| `98_Shared/GameData/` | 공식·상수 — 재현 가능 의무 |

**제외**: `02_Server/Network/` (네트워킹 인프라, 회귀 안전망은 통합 테스트 측에서), `02_Server/Loop/` (틱 루프, 통합 테스트 측에서), `03_Client/` (Unity 클라 — TDD 강제 영역 X, 학습 호흡 우선).

### 2. Hook 우회·차단 정책

| Hook | 우회? | 사유 |
|---|---|---|
| `dangerous-cmd-guard` | ❌ 차단 (exit 2) | 파괴 명령 보호 — 정말 필요하면 외부 셸 |
| `shared-discipline-guard` | ❌ 차단 (exit 2) | "주석 약속은 가짜다" 3회 봉합 — 강제 |
| `phase-gate-validator` | ❌ 차단 (exit 2) | -DONE.md 형식 강제 (.githooks/pre-commit이 commit 시점 이중 안전망) |
| `risk-detector` | ⚠️ 알림만 | 등급 상향 통보 — 본인이 인지하면 진행 가능 |
| `tdd-guard` | ⚠️ 경고만 | 학부생 학습 호흡 (테스트 강제 부담 ↓) |
| `circuit-breaker` | ⚠️ 알림만 | false positive 위험 (정당한 반복) — 사용자 판단 |
| `pin-injector` | ⚠️ 출력만 | UserPromptSubmit, 차단 X |

차단 3개 vs 경고 4개. 차단은 헌법/사고 봉합 핵심만. 경고는 정보 제공.

### 3. circuit-breaker false positive 회피 패턴

함정 §"circuit-breaker false positive 위험 — 정당한 반복(테스트 fuzz 1000회 등) 차단 X" 봉합:

1. **Bash 도구 제외**: 테스트 명령 빈도 ↑ 정상. 차단 시 작업 불가
2. **등급별 임계 차등**: 단순 5 / 보통 10 / 복잡 15 / 대규모 20. 큰 작업일수록 도구 호출 ↑ 정상
3. **윈도우 5분**: 짧으면 false positive ↑, 길면 무한 재시도 인지 늦음. 5분 = 사용자 한 번 응답 사이클 추정
4. **알림만, 차단 X**: 사용자 판단 = 정당 vs 막힘. AI가 결정 불가

### 4. settings.proposed.json `ask` 절 빈 결정

5/18 ε 결정 (CHANGELOG [H]) — Auto Mode 정책. Edit(**)/Write(**)/Bash(git commit*) ask 룰 *제거*. 대신:

- `dangerous-cmd-guard` PreToolUse가 *진짜 위험 명령* 차단 (ask보다 강한 안전망)
- `risk-detector` PreToolUse가 *위험 영역* 통보 (자동 등급 상향)
- 사용자가 무서우면 Shift+Tab으로 `acceptEdits` → `default` 모드 전환 (UI 측 안전망)

`ask`로 모든 Edit 차단 = 자동화 효율 ↓ 비용이 안전망 효익보다 큼.

---

## 학습 일지 후보 키워드

본 섹션은 *키워드만* — 디테일은 본인이 [`/journal:phase`](.claude/commands/journal/phase.md) 또는 노션 트랙 B에서 박음 (가짜 학습 방지 정신).

- **★★★ `hook-as-policy-physical-enforcement`** — Hook이 헌법 정책의 *물리적 강제*. 옛 운영 "주석 약속은 가짜다" 3회 봉합 (헌법 #2 ProtocolVersion handshake 7 Phase째 가짜 / 헌법 #4 Shared Code Discipline / Handlers/ 폴더 7 Phase째 가짜) → 새 운영은 Hook이 강제. 면접 *AI 자동화 의사결정* 어필 결정타
- **★★★ `circuit-breaker-false-positive-prevention`** — Bash 제외 + 등급별 임계 차등 + 알림만(차단 X). 정당한 반복 보호 + AI 무한 재시도 차단 균형. 면접 *false positive vs false negative 균형* 어필
- **★★★ `risk-flag-auto-elevation`** — 본인이 깜빡 단순 등급으로 처리하려는 trust-boundary 변경을 자동 검출 + 양식 부담 자동 상향. 5/20 의논 핵심 안전망. *사람 판단 깜빡 + 시스템 보강* 사고 패턴
- **★★★ `shared-discipline-from-warning-to-block`** — 옛 경고만 → 새 exit 2 차단. 5/17 정유현 pull 사고 직접 원인 봉합. "주석 약속은 가짜다" 3회 봉합 시리즈 마지막
- **★★ `pretool-vs-posttool-tradeoff`** — Pre = 차단 / Post = 경고. 옛 `check-work-envelope.sh`가 Post였던 게 양식 노이즈 비용 (5/20 의논). 차단할 거면 Pre, 정보 제공만이면 Post
- **★★ `tdd-warning-not-block-for-learning-rhythm`** — 학부생 학습 호흡 유지 위해 차단 X 경고만 + 누적 로그. *학습 호흡* vs *엄격함* 균형. 학부생 → 시니어 전환 시점에 차단으로 승격 가능
- **★ `permission-deny-vs-pretool-hook-defense-in-depth`** — settings.json `deny` 룰 + `dangerous-cmd-guard` PreToolUse Hook 이중 안전망. deny는 매처 단순, Hook은 정규식 깊이 — 보완 관계

본 디테일은 본 Phase 마감 후 본인이 회고체로 박음. AI 자율 박제 X (Phase 04 `ai-self-reinforcement-bias-prevention` 게이트 정신 정합).

---

## Phase 06 전환 시 추가 작업

본 Phase 마감 시점엔 *격리 폴더 안 정의*. Phase 06 전환 commit에서 다음 보강 필요:

1. **실행 권한** — `chmod +x .claude/hooks/*.sh` (Windows + Git Bash 호환 점검)
2. **옛 hook 5개 삭제** — `check-work-envelope.sh` + `check-server-authority.sh` + `validate-shared-changes.sh` + `inject-current-pin.sh` + `validate-phase-gate.sh`
3. **`.claude/settings.json` 교체** — `New_Harness/settings.proposed.json` 내용으로
4. **시뮬레이션 5건 실측** — 본 -DONE.md AC 섹션의 5 시나리오 실제 발동 검증. 결과를 ADR-022 또는 -DONE.md 후속 박음
5. **PreToolUse 환경변수 명세 확정** — `CLAUDE_TOOL_INPUT_COMMAND` 노출 여부 확인. 안 노출되면 Hook 본문 fallback 패턴 조정

---

## 참조

- Phase 정의: [`03-hook-infrastructure.md`](03-hook-infrastructure.md)
- 옛 hook 5개: [`.claude/hooks/`](../../../.claude/hooks/)
- 새 헌법: [`New_Harness/CLAUDE.md`](New_Harness/CLAUDE.md) "📊 작업 등급" + "⚠️ 절대 원칙" 섹션
- 정책: [`New_Harness/policies/grade-and-risk.md`](New_Harness/policies/grade-and-risk.md) + [`pin-and-done.md`](New_Harness/policies/pin-and-done.md)
- 옛 사고 봉합 이력: `.claude/CHANGELOG.md` 5/17 (정유현 pull 사고) + 5/18 (M3 Phase 02/03 핸들러 layer 분리)
- 다음 Phase 정의: [`05-slash-cleanup.md`](05-slash-cleanup.md) (옛 슬래시 16 → 새 10 정리)
