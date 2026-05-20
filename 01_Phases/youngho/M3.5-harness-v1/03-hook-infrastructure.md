# Phase 03: Hook 인프라

> **상태**: pending
> **마일스톤**: M3.5 — 새 하네스 v1 문서화
> **등급**: 대규모 (정량 4등급 중 4단계)
> **도메인**: `.claude/hooks/` + `settings.json` (새 Hook 정의)
> **담당**: 영호 단독
> **예상 소요**: 4~6h
> **산출물 위치**: `01_Phases/youngho/M3.5-harness-v1/New_Harness/hooks/` + `New_Harness/settings.proposed.json`
> **병렬 가능**: Phase 04 (Knowledge)와 의존성 X — 병렬 진행 OK

---

## 🎯 목표

5/20 의논 결과 박힌 *위험 Hook 자동 상향 + SubAgent 행동 강제* 인프라를 박는다. 옛 hook 5개(`validate-shared` / `check-authority` / `validate-phase-gate` / `check-work-envelope` / `inject-current-pin`) 다이어트 + 신규 hook 5개 박음.

---

## 🤔 왜 Hook 재설계 (옛 5 → 새 5~7)

| # | 새 Hook | 옛 대응 | 변경 |
|---|---|---|---|
| 1 | `dangerous-cmd-guard` | (옛 없음, settings.json deny 룰만) | 신설 (rm -rf / git reset --hard / force push 등 PreToolUse 차단) |
| 2 | `tdd-guard` | (옛 없음) | 신설 (코드 변경 전 테스트 존재 점검 — TDD 강제 영역 결정 따라 적용) |
| 3 | `circuit-breaker` | (옛 없음) | 신설 (Worker 무한 재시도 차단 — 2회 실패 후 Stop) |
| 4 | `risk-detector` | (옛 없음) | 신설 (trust-boundary/irreversible/unity-asset 자동 등급 상향) |
| 5 | `shared-discipline-guard` | `validate-shared.sh` | 강화 (PDL 수정 시 PacketGenerator 재생성 + Shared.dll commit 의무 자동 점검) |
| 6 | `pin-injector` | `inject-current-pin.sh` | 유지 (work-pin UserPromptSubmit 주입) |
| 7 | `phase-gate-validator` | `validate-phase-gate.sh` | 유지 (`-DONE.md` 형식 강제, core.hooksPath pre-commit과 이중) |

- **삭제**: `check-work-envelope` (양식 자체가 5/20 의논에서 죽임 결정)
- **삭제**: `check-authority` (server 권위 = 코드 리뷰가 잡음, hook은 false positive 많음)
- **유지**: `validate-shared` → `shared-discipline-guard`로 강화
- **유지**: `inject-current-pin` → `pin-injector` (이름 정합)
- **유지**: `validate-phase-gate` → `phase-gate-validator` (이름 정합)

---

## ⏪ 사전 조건

- [ ] Phase 01 완료 (`New_Harness/CLAUDE.md`에 위험 깃발 3종 박힘)
- [ ] Phase 02 완료 (SubAgent 정의 — Hook이 강제할 대상)
- [ ] (Phase 04와 병렬 OK)

---

## 📝 작업 내용

### 1. 신규 Hook 5개 박기 (`New_Harness/hooks/`)

#### 1.1 `dangerous-cmd-guard.sh` (PreToolUse — Bash)

- [ ] 차단 패턴 grep:
  - `rm -rf` / `rm -fr`
  - `git reset --hard` / `git checkout --force`
  - `git push --force` / `git push -f` (main 대상은 더 엄격)
  - `git clean -fd`
  - `gh pr merge` (main 대상 force / 본인 외 PR — 영호만 main 머지 정책 강제)
- [ ] exit 2 + 사용자 친화 메시지 (왜 차단됐는지 + 대안 안내)
- [ ] 우회 = `--no-verify` 같은 옵션 X (PreToolUse는 우회 불가, 의도)

#### 1.2 `tdd-guard.sh` (PreToolUse — Edit/Write)

- [ ] 트리거 영역 = `02_Server/`, `98_Shared/Protocol/`, `98_Shared/GameData/`
- [ ] 점검: Write/Edit 대상 코드 파일의 *대응 테스트 파일 존재 여부*
- [ ] 테스트 없으면 경고 (exit 0 + stderr 메시지) — 차단 아님 (학부생 학습 호흡)
- [ ] 사용자 응답 기록 = `.claude/state/tdd-guard-log.txt` (어느 파일이 테스트 없이 박혔는지 추적)

#### 1.3 `circuit-breaker.sh` (PostToolUse — 모든 도구)

- [ ] 직전 N분 동안 *같은 SubAgent*가 *같은 도구*로 *N회 호출*했는지 점검
- [ ] 임계 (단순/보통/복잡/대규모 등급별로 차등) — 단순 = 5회 / 대규모 = 20회
- [ ] 초과 시 사용자 통보 (Stop 아님, 알림만 — 사용자가 판단)

#### 1.4 `risk-detector.sh` (PreToolUse — Edit/Write/Bash)

- [ ] 트리거 영역 자동 인식:
  - `trust-boundary`: `02_Server/GameSession.cs`, `Handlers/`, `validate-shared.sh` 변경
  - `irreversible`: `git push` to main, `gh pr merge`, DB 마이그 SQL 변경, `Protocol.Version` bump
  - `unity-asset`: `03_Client/Assets/**/*.{prefab,unity,asset}` 변경 (특히 prefab)
- [ ] 자동 등급 상향: 단순 → 보통, 보통 → 복잡, 복잡 → 대규모
- [ ] 상향 결과를 work-pin에 한 줄 박음 (사용자가 인지)

#### 1.5 `shared-discipline-guard.sh` (PreToolUse — Edit/Write)

- [ ] `98_Shared/Protocol/PDL.xml` 변경 감지 시 의무 3종 점검 (옛 헌법 #4 + 5/17 운영 룰 그대로):
  - PacketGenerator 재생성 실행됐는지 (변경 시각 비교)
  - `03_Client/Assets/Plugins/Shared/Shared.dll` commit 동반인지
  - `Protocol.Version` bump 필요 여부 자동 판단 (필드 추가 = breaking change 검사)
- [ ] 미준수 시 exit 2 + 의무 3종 명세 안내

### 2. 유지 Hook 3개 이름 정정 + 강화 (`New_Harness/hooks/`)

- [ ] `pin-injector.sh` = 옛 `inject-current-pin.sh` 복사 + 새 work-pin 압축 양식 반영
- [ ] `phase-gate-validator.sh` = 옛 `validate-phase-gate.sh` 복사 + 새 등급 frontmatter 점검 추가
- [ ] `shared-discipline-guard.sh` 안에 옛 `validate-shared.sh` 로직 통합

### 3. 새 `settings.proposed.json` 박기

- [ ] `hooks` 절 = 새 Hook 8개 매핑 (PreToolUse / PostToolUse / Stop / UserPromptSubmit)
- [ ] `permissions.deny` 절 = 옛 deny 룰 유지 (curl/wget secrets / .env read)
- [ ] `permissions.ask` 절 = Auto Mode 정책 그대로 (5/18 ε 결정)
- [ ] 새 등급 → 자동 권한 매핑 (위험 깃발 발동 시 deny 추가 등)

### 4. 옛 → 새 매핑 표 갱신 (`New_Harness/README.md`)

- [ ] 옛 hook 5개 → 새 hook 7개 매핑 행 추가
- [ ] settings.json 절 매핑

---

## ✅ 완료 조건

- [ ] `New_Harness/hooks/` 안에 7개 `.sh` 박힘 (실행 권한 검토는 Phase 06에서)
- [ ] `New_Harness/settings.proposed.json` 박힘 (옛 settings.json은 그대로)
- [ ] 각 Hook의 *트리거 조건* + *exit 코드 정책* (차단 vs 경고) 명세
- [ ] 옛 운영 100% 작동 (옛 hook 5개 그대로)
- [ ] 새 Hook *동작 시뮬레이션* 시나리오 5건 문서화 (실제 호출 X, Phase 06 전환 후 실측)

---

## 🧪 테스트

**자동**: 옛 운영 sanity check
- 옛 hook 5개 작동 (work-pin 주입 / -DONE.md 형식 검증 / shared validate)
- `dotnet test` 통과 유지

**수동**:
- 새 Hook 7개 *각각의 차단 시나리오* 본인 눈으로 통독 (실제 발동 X)
- `dangerous-cmd-guard`의 차단 패턴이 *과도하지 않은지* 점검 (false positive 위험)
- `tdd-guard`가 *학부생 학습 호흡 안 깨뜨리는지* 점검 (경고만, 차단 X)

---

## 📚 학습 포인트

- **Hook = 정책의 *물리적 강제***: 옛 운영은 헌법 문구로 약속 → "주석 약속은 가짜다" 봉합 3회 증명. 새 운영은 Hook이 약속을 *강제*
- **PreToolUse vs PostToolUse**: 차단할 거면 Pre / 경고만 할 거면 Post. 옛 운영의 `check-work-envelope`가 Post였던 게 양식 노이즈 비용
- **circuit-breaker의 가치**: AI 무한 재시도 = 토큰/시간 낭비 + 잘못된 가정 누적. 임계 도달 시 사용자 호출
- **risk-detector의 자동 등급 상향**: 본인이 깜빡 단순 등급으로 처리하려는 변경이 trust-boundary였다 = 5/20 의논에서 핵심 안전망 박힘
- **shared-discipline-guard의 강화**: 옛 `validate-shared.sh`가 *경고만* 했다 → "주석 약속은 가짜다" 3회 봉합 사고 원인. 새 운영은 의무 3종 자동 점검 + 차단

---

## ⚠️ 함정 / 주의사항

- **Hook 우회 = 본인이 깜빡 `--no-verify` 같은 옵션**: 본 Phase에서 박는 룰은 *PreToolUse*라 Claude Code 도구 호출에서 우회 불가. 단 외부 에디터 직접 편집은 우회 가능 → `.githooks/pre-commit`이 commit 시점 안전망 (5/18 박힘 그대로 유지)
- **새 Hook이 옛 `.claude/settings.json` 의 hooks 절과 충돌 X**: `New_Harness/`는 자동 로드 경로 아님. 단 Phase 06 전환 시 일괄 mv → 그때 충돌 점검
- **TDD 강제 영역 결정 미완**: `tdd-guard`의 트리거 영역(02_Server/ / 98_Shared/Protocol/Tools/ 등) 최종 결정은 본 Phase 안에서. 옛 보류 항목 해소
- **circuit-breaker false positive 위험**: 정당한 반복 (테스트 fuzz 1000회 등)을 차단하면 안 됨. 임계는 *등급별 차등* + Bash 도구는 제외 (테스트 실행 빈도 ↑)

---

## ➡️ 다음 Phase

- **Phase 05 — 슬래시 정리 + 신규 2개** (Hook 호출하는 슬래시 정합)
- 의존성: 본 Phase 03의 Hook이 슬래시 동작 강제

---

## 📋 박제 (완료 후 -DONE.md)

- 옛 5 → 새 7 Hook 매핑 표 최종본
- 각 Hook의 트리거/exit 정책 1:1 표
- 시뮬레이션 5건 결과
- TDD 강제 영역 최종 결정 (옛 보류 항목 해소)
- 학습 키워드 후보 (Hook = 약속의 물리 강제 / risk-detector 자동 상향 / circuit-breaker etc)
