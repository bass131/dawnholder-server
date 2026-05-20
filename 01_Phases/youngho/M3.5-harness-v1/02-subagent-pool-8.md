# Phase 02: SubAgent 풀 8 정의

> **상태**: pending
> **마일스톤**: M3.5 — 새 하네스 v1 문서화
> **등급**: 대규모 (정량 4등급 중 4단계)
> **도메인**: `.claude/agents/` (새 SubAgent 정의)
> **담당**: 영호 단독
> **예상 소요**: 4~5h
> **산출물 위치**: `01_Phases/youngho/M3.5-harness-v1/New_Harness/agents/` 폴더 안 (옛 `.claude/agents/` 직접 수정 X)

---

## 🎯 목표

5/20 의논 결과 박힌 *SubAgent 풀 8개*(server/shared/client/qa + reviewer/plan-auditor/unity-bridge/coordinator) 각각의 정의 `.md`를 박는다. 옛 도메인 6 + reviewer = 7개를 8개로 확장 + 책임 재정렬.

---

## 🤔 왜 8개인가 (옛 7 → 새 8)

| # | 새 SubAgent | 옛 매핑 | 변경 |
|---|---|---|---|
| 1 | `server` | gameplay + netcode + persistence + qa-sim의 server-side | 4→1 통합 (서버 코드 도메인 단일화) |
| 2 | `shared` | (옛 없음, 묵시적으로 메인 세션이 처리) | 신설 (98_Shared/ 단독 도메인) |
| 3 | `client` | client | 그대로 |
| 4 | `qa` | qa-sim + content의 일부 | qa-sim 책임 명확화 |
| 5 | `reviewer` | reviewer | 그대로 (Tier 2 자동 리뷰) |
| 6 | `plan-auditor` | (옛 없음 — Codex γ 방식이 메인 세션 외부에서 수행) | 신설 (γ 방식 흡수) |
| 7 | `unity-bridge` | (옛 없음 — Unity MCP가 도구 수준) | 신설 (Unity Editor/MCP 전담) |
| 8 | `coordinator` | (옛 없음 — 메인 세션이 직접 분해) | 신설 (복잡/대규모 Phase 분해 + 위임) |

- **신설 4 (shared/plan-auditor/unity-bridge/coordinator)**: 5/20 의논에서 박힌 *전문화* 결정. 옛 메인 세션 부담을 분산
- **삭제/통합**: 옛 `content` SubAgent는 → server/client/qa로 흡수 (도메인 색이 흐릿했음)

---

## ⏪ 사전 조건

- [ ] Phase 01 완료 (`New_Harness/CLAUDE.md`에 SubAgent 풀 8 이름 박힘)

---

## 📝 작업 내용

### 1. 새 SubAgent 정의 파일 8개 박기 (`New_Harness/agents/`)

각 정의 파일 = 옛 `.claude/agents/*.md` 양식 + 새 등급 체계 정합. 필드:

- **name**: SubAgent 식별자
- **description**: 메인 세션이 라우팅할 때 보는 기준 (PROACTIVELY 키워드 포함 여부)
- **tools**: 허용 도구 목록 (예: Read/Edit/Write/Glob/Grep/Bash)
- **model**: 기본 모델 (Sonnet vs Opus)
- **scope**: 읽기/쓰기 가능 폴더
- **escalation**: Sonnet 2회 실패 → Opus 재호출 → 사용자

#### 1.1 Worker 4 (Sonnet 기본)

- [ ] `New_Harness/agents/server.md` — 02_Server/ + 98_Shared/ 읽기/쓰기. 도메인: 게임플레이/네트워킹/영속화/서버 게임 루프
- [ ] `New_Harness/agents/shared.md` — 98_Shared/ 단독. 도메인: 프로토콜/공식/공유 상수. *경계*: 04_ClientNet/도 읽기 가능
- [ ] `New_Harness/agents/client.md` — 03_Client/ + 04_ClientNet/ 읽기/쓰기. 98_Shared/ 읽기만. 도메인: Unity 씬/렌더링/입력/UI/prediction
- [ ] `New_Harness/agents/qa.md` — 99_Tools/ + 테스트 코드. 게임 코드는 READ-ONLY. 도메인: 헤드리스 봇/부하 테스트/퍼징/repro 스크립트

#### 1.2 Reviewer 2 (Opus 기본)

- [ ] `New_Harness/agents/reviewer.md` — 옛 reviewer.md 흡수 + 새 등급 체계 정합. 자동 호출 트리거 = 도메인 Worker 코드 변경 후 (조건 명세)
- [ ] `New_Harness/agents/plan-auditor.md` — 신설. Codex γ 방식의 *사전 검증* 패턴 흡수. 코드 박기 *전* 설계 / 시나리오 / 사전 조건 점검. `_milestone-plan.md` / Phase 정의 `.md` 검토가 핵심 사용처

#### 1.3 Specialist 2 (Sonnet 기본)

- [ ] `New_Harness/agents/unity-bridge.md` — 신설. Unity Editor MCP + asset 작업 + scene/prefab 편집 전담. 옛 메인 세션 + client SubAgent 분산 책임을 흡수
- [ ] `New_Harness/agents/coordinator.md` — 신설. 복잡/대규모 Phase 분해 + Worker 위임 + 결과 통합. Opus 기본 (분해 판단)

### 2. 라우팅 룰 박기 (`New_Harness/agents/_routing.md`)

- [ ] 등급별 처리 패턴 (PDF NDREAM 그대로):
  - 단순 = 메인 세션 직접
  - 보통 = Worker 1개
  - 복잡 = Coordinator + Worker 1~2개
  - 대규모 = Coordinator + Team (Worker 3~4개 + Reviewer)
- [ ] 도메인 → Worker 매핑 표 (옛 헌법의 6 도메인 표를 8개로 확장)
- [ ] 자동 호출 트리거 명세 (reviewer = 코드 변경 후 자동 / plan-auditor = `_milestone-plan.md` Write 후 자동 / 나머지는 수동 위임)

### 3. 에스컬레이션 + 모델 분담 명세 (`New_Harness/agents/_escalation.md`)

- [ ] Sonnet Worker 2회 실패 → Opus Worker 재호출 → 그래도 실패 → 사용자 통보
- [ ] Reviewer 발견 위반 → Worker에 재작업 위임 (1 라운드만, 그 후 사용자 통보)
- [ ] Coordinator의 Worker 위임 실패 → 분해 잘못 가정 → 사용자 통보 + 재분해 권유

### 4. 옛 → 새 매핑 표 갱신 (`New_Harness/README.md`)

- [ ] Phase 01에서 박은 매핑 표에 옛 7 SubAgent → 새 8 SubAgent 매핑 행 추가

---

## ✅ 완료 조건

- [ ] `New_Harness/agents/` 안에 8개 `.md` + `_routing.md` + `_escalation.md` = 10개 파일 박힘
- [ ] 각 SubAgent의 *경계* (읽기/쓰기 폴더) 명확 — 중첩 없음
- [ ] 자동 호출 트리거가 *어느 도구 호출 후 어느 SubAgent가 호출되는지* 명세 (Phase 03 Hook 인프라가 강제)
- [ ] 옛 `.claude/agents/` 그대로 작동 (옛 운영 깨뜨림 없음 — Phase 06 전환까지)

---

## 🧪 테스트

**자동**: 옛 운영 sanity check
- 옛 reviewer SubAgent 호출 시 정상 응답
- 옛 client/gameplay/netcode/content/persistence/qa-sim SubAgent 호출 시 정상 응답

**수동**:
- 본인 눈으로 새 정의 통독 — 도메인 경계 중첩 / 누락 점검
- 옛 7 → 새 8 매핑 표 reverse check (옛 책임이 새 어느 SubAgent로 갔는지 1:1 추적)
- *가상 시나리오* 5건으로 라우팅 시뮬레이션 (예: "새 패킷 추가" → shared + server + client 위임, "Unity prefab 작업" → unity-bridge + client)

---

## 📚 학습 포인트

- **SubAgent = 도메인 + 권한 + 모델의 3축 정의**: 옛 운영은 도메인만 박았고 권한·모델은 묵시적 → 새 운영은 3축 명세
- **shared SubAgent 신설 의미**: `98_Shared/` 가 *프로토콜 신성*(헌법 #2) 영역이라 server/client 누구든 함부로 못 만짐 → 전담 SubAgent가 게이트
- **plan-auditor 흡수의 정신**: Codex γ 방식이 *외부 도구*에 의존 → 새 운영은 *내부 SubAgent*로 흡수 → 외부 의존 X (단 Codex β 검증은 후속 옵션으로 유지)
- **unity-bridge 분리 이유**: Unity MCP가 코드 vs asset vs scene/prefab을 *섞어서 만져야* → 전담 SubAgent가 컨텍스트 보존 비용 ↓
- **coordinator는 분해 책임**: 메인 세션이 모든 분해를 직접 하면 컨텍스트 부담 ↑ + 일관성 X → Coordinator SubAgent가 *대규모 Phase 전담*

---

## ⚠️ 함정 / 주의사항

- **새 SubAgent 정의가 옛 .claude/agents/ 자동 로드 충돌 X 확인**: `New_Harness/agents/`는 Claude Code 자동 로드 경로 *아님* — 안전. 단 Phase 06 전환 시 `.claude/agents/`로 mv하면 그때 자동 로드됨
- **권한 중첩 금지**: server SubAgent가 03_Client/에 쓰기 금지, client SubAgent가 02_Server/에 쓰기 금지. shared만 양쪽 읽기 가능 (쓰기는 98_Shared/만)
- **plan-auditor 자동 호출 조건 명세 부족 = Phase 03 Hook 인프라 의존**: 본 Phase는 *정의*만, *강제*는 Phase 03이 책임
- **coordinator의 위임 재귀 X**: Coordinator → Worker 1단계만. Worker가 다시 Coordinator 호출 = 무한 재귀 위험. 헌법에 박을 것

---

## ➡️ 다음 Phase

- **Phase 03 — Hook 인프라** (등급:대규모, 병렬 가능)
- **Phase 04 — Knowledge 시스템 + GC** (등급:대규모, 병렬 가능)
- 의존성: 본 Phase 02의 SubAgent 정의가 Hook의 강제 대상 + Knowledge 캐시 입출력 주체

---

## 📋 박제 (완료 후 -DONE.md)

- 옛 7 → 새 8 SubAgent 매핑 표 최종본
- 각 SubAgent의 권한/모델/도메인 1:1 표
- 라우팅 시뮬레이션 5건 결과
- 학습 키워드 후보 (SubAgent 3축 정의 / plan-auditor 흡수 / unity-bridge 전담 etc)
