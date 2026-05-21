# Grade & Risk — 작업 정량 4등급 + 위험 깃발 자동 상향

> **헌법 참조**: 본 정책은 새 헌법 v1 "📊 작업 등급" 섹션에서 링크됩니다.
> 충돌 시 헌법이 이깁니다.
>
> **신선도 주의**: 본 정책은 M3.5 박힘 시점(2026-05-20) 실측 0건. M4 진입 후 1주 안에 false positive·누락·등급 기준 재조정 예정.

본 문서는 모든 작업을 *정량 4등급*으로 분류하고, *위험 깃발*이 잡히면 등급을 자동 상향하는 정책을 정의합니다. 등급이 **양식 부담**(work-pin / -DONE.md / 5단계 보고)과 **동원 패턴**(메인 직접 / Worker SubAgent / Coordinator+Team)을 결정합니다.

---

## 1. 왜 등급 체계인가 (배경)

5/20 의논 결과 — 옛 운영은 *모든 Phase를 같은 무게로* 처리했습니다. 결과:

- 단순 변경(주석 한 줄 정정, rename)에도 work-envelope + 5단계 보고가 따라붙어 양식 노이즈 폭증
- 진짜 큰 작업(헌법 변경, 프로토콜 변경)도 단순 작업과 같은 양식 → 무게 안 보임 → 사고 위험 ↑
- AI/사용자 둘 다 *양식 피로*로 본질 작업 집중력 ↓

**해결**: 작업 무게 4등급 → 양식 부담 4단계 1:1 매핑. 단순한 건 단순하게, 큰 건 크게.

PDF NDREAM 패턴(Sonnet Worker + Opus Coordinator)을 본 프로젝트에 정합 — 자세한 모델 분담은 [`subagent-routing.md`](subagent-routing.md).

---

## 2. 4등급 정의

| 등급 | 정량 기준 | 처리 패턴 | work-pin | -DONE.md | 5단계 보고 |
|---|---|---|---|---|---|
| **단순** | 1 도메인 × 1 파일 / ≤10줄 / 가역적 | 메인 세션 직접 | ✅ | ❌ | ❌ |
| **보통** | 1 도메인 × 2~3 파일 / ≤50줄 / 가역적 | Worker SubAgent 1개 | ✅ | ❌ | ❌ |
| **복잡** | 2 도메인 / ~100~200줄 / 일부 비가역 | Coordinator + Worker 1~2개 | ✅ | ✅ | ❌ |
| **대규모** | 3+ 도메인 또는 300줄+ / 비가역 | Coordinator + Team (Worker 3~4개 + Reviewer) | ✅ | ✅ | ✅ MD + HTML |

### 정량 판정의 *순서*

1. **도메인 개수** 먼저 (server/shared/client/qa)
2. **줄 수** 다음 (실질 변경, 공백/주석 제외)
3. **가역성** (`git revert` 한 줄로 복원 가능한가)

세 기준 중 *가장 높은* 등급을 채택. 예: 1 도메인 × 5줄인데 비가역(`git push` to main)이면 → 복잡으로 상향.

### 등급별 동원 패턴 디테일

- **단순**: 메인 세션이 Edit/Write 직접. SubAgent 위임 비용 > 작업 비용.
- **보통**: 도메인 Worker 1개에 위임. 메인 세션은 결과 수신 + work-pin 갱신.
- **복잡**: Coordinator가 Phase 분해 + Worker 1~2개 위임 + 결과 통합. Reviewer 자동 호출(트리거 충족 시).
- **대규모**: Coordinator + 도메인 Worker 다수 + plan-auditor 사전 검증 + Reviewer 통합 점검 + 5단계 보고 MD/HTML 이중 박음(캡스톤 평가 자산).

---

## 3. 위험 깃발 (자동 등급 상향)

다음 깃발이 잡히면 *기본 등급에서 한 단계 상향*. 두 깃발 동시 잡히면 두 단계 상향.

| 깃발 | 검출 패턴 | 사유 |
|---|---|---|
| **trust-boundary** | `02_Server/GameSession.cs`, `02_Server/Handlers/`, `02_Server/**/Validation*`, 신뢰 경계 검증 코드 | 헌법 #3 — 한 줄 실수가 보안 구멍 |
| **irreversible** | `git push` to `main`, `gh pr merge`, DB 마이그 SQL, `Protocol.Version` bump, `git reset --hard`, force push | 되돌리는 비용이 큼 |
| **unity-asset** | `03_Client/Assets/**/*.{prefab,unity,asset,mat}`, 특히 prefab | YAML 자동 머지 충돌·prefab 백업 사고 (Phase 08 BackGround 사고) |

### 상향 결과 박힘

상향이 일어나면 work-pin에 한 줄 박힘:

```
등급:           복잡 (자동 상향: 보통 + trust-boundary)
```

상향 사유가 *명시*되면 본인이 "왜 갑자기 양식 부담 늘었지?" 의문 즉시 해소.

### 강제 발동 = Hook

위 깃발은 사용자/AI 판단에 *의존하지 않음*. `.claude/hooks/risk-detector.sh`(Phase 03 산출물)가 PreToolUse/PostToolUse에서 변경 파일 경로 grep으로 자동 검출 → 등급 상향 + work-pin 갱신 강제.

Hook 명세 = [`../hooks/risk-detector.sh`](../hooks/risk-detector.sh) (Phase 03 reference).

---

## 4. 등급 판정 흐름 (시각화)

```
[사용자 요청]
   │
   ├─ 메인 세션: "이 작업 등급 뭐냐?"
   │   ├─ 도메인 개수 셈
   │   ├─ 줄 수 추정 (Phase 정의 또는 변경 범위)
   │   └─ 가역성 판정 (push/merge/migration?)
   │
   ├─ 기본 등급 결정 (단순/보통/복잡/대규모)
   │
   ├─ risk-detector.sh Hook 자동 발동
   │   ├─ 깃발 0개 → 기본 등급 유지
   │   ├─ 깃발 1개 → 1단계 상향
   │   └─ 깃발 2개+ → 2단계 상향
   │
   ├─ 최종 등급 → work-pin에 박힘
   │
   ├─ 처리 패턴 결정 (메인 직접 / Worker / Coordinator+Team)
   │   └─ [subagent-routing.md] 참조
   │
   └─ 작업 진행
```

---

## 5. 등급별 보고 양식 격차 (요약)

| 양식 | 단순 | 보통 | 복잡 | 대규모 |
|---|---|---|---|---|
| work-pin 갱신 | ✅ | ✅ | ✅ | ✅ |
| commit message | ✅ | ✅ | ✅ | ✅ |
| `-DONE.md` 박제 | ❌ | ❌ | ✅ | ✅ |
| 5단계 보고 | ❌ | ❌ | ❌ | ✅ MD + HTML |
| Reviewer 자동 호출 | ❌ | 조건부 | ✅ | ✅ |
| plan-auditor 사전 검증 | ❌ | ❌ | ✅ | ✅ |

양식 디테일 — [`reporting-format.md`](reporting-format.md) + [`pin-and-done.md`](pin-and-done.md).

---

## 6. 함정 / 주의사항

- **등급은 *예상*이 아니라 *측정*** — Phase 시작 시점에 추정한 등급이 작업 도중 정량 기준 넘으면 *상향 후 work-pin 갱신*. 등급 고착으로 양식 부담 회피 X
- **위험 깃발은 *우회 금지*** — `risk-detector.sh`는 PreToolUse Hook이라 사용자가 "그냥 진행해" 해도 양식 부담 자동 적용. 헌법 절대 원칙 보호
- **단순 등급의 함정** — 단순 1줄 변경이지만 `02_Server/Handlers/`에 박히면 trust-boundary 깃발 발동 → 보통으로 상향. 위치가 기준의 일부

---

## 7. 변경 시 동기화 책임

본 정책 수정 시 *반드시* 함께 갱신:

- [`../CLAUDE.md`](../CLAUDE.md) "📊 작업 등급" 섹션 (헌법 본문 표와 정합)
- [`subagent-routing.md`](subagent-routing.md) (등급 → 처리 패턴 매핑)
- [`reporting-format.md`](reporting-format.md) (등급별 5단계 보고 조건부화)
- [`pin-and-done.md`](pin-and-done.md) (등급별 -DONE.md 박제 조건)
- [`../hooks/risk-detector.sh`](../hooks/risk-detector.sh) (Phase 03 산출물 — 깃발 검출 패턴)

---

## 8. 실측 후 재조정 항목

본 정책은 *추측 기반*. M4 진입 후 첫 1주 안에 다음 관찰 → 명세 갱신:

- [ ] **줄 수 임계 적정성** — 50줄/200줄/300줄이 너무 빡빡한지 너무 느슨한지
- [ ] **위험 깃발 false positive** — `02_Server/`에 박혔지만 핸들러 로직 X (예: 로깅 줄 추가)인데 trust-boundary 발동되는 빈도
- [ ] **위험 깃발 누락** — `98_Shared/` 변경 자체는 깃발 아님(헌법 #4가 잡음). 추가 깃발 후보 발견 빈도
- [ ] **등급 상향 사용자 마찰** — 본인이 단순 작업이라 인식했는데 자동 상향으로 양식 부담 ↑ 시 불만 누적 빈도
- [ ] **처리 패턴 효율** — 보통에 Worker 위임이 메인 직접보다 빠른가, 느린가

재조정 결과는 본 정책 직접 수정 또는 ADR-023 신설(변경 폭에 따라).

---

## 갱신 이력

- 2026-05-20 — M3.5 Phase 01 (2/2)에서 신설. 5/20 의논 결과(KPI 전환 + 정량 4등급 + 위험 Hook 자동 상향) 박힘. 실측 0건, M4 진입 후 재조정 예정.
