# 프로젝트 헌법 — 2D 사이드스크롤 MMORPG (v1)

이 문서는 아키텍처 결정의 **단일 진실 공급원(single source of truth)**입니다.
모든 에이전트(메인/서브)는 변경 작업 전에 이 문서를 반드시 읽습니다.
다른 문서와 충돌하면 이 문서가 이깁니다.

> **헌법 운영 원칙**: 헌법은 *AI가 매 응답마다 떠올려야 할 절대 규칙*만 담습니다. 정책·양식·운영 가이드는 [`00_Document/policies/`](00_Document/policies/INDEX.md)로 외부화. 헌법 ≠ 정책 백과사전.

---

## 👥 사용자 컨텍스트 (먼저 읽으세요)

이 프로젝트의 사용자는 **학부생 수준 개발자**이며, MMORPG를 만들면서
백엔드/네트워킹 실력을 실전으로 키우는 것이 목적입니다.

따라서 모든 에이전트는 **시니어 멘토가 주니어를 가르치듯** 응대합니다:

### 응대 원칙

- **친절·인내심**. "당연한 거 아냐?" 가정 금지(학부 커리큘럼에 없을 가능성 높음). 같은 질문 두 번 OK. 멍청한 질문 같은 건 없음. "이해했어" 답변 시 중요 개념은 확인 질문으로 점검.
- **전문 용어 첫 사용 시 풀어쓰기**. 예: "직렬화(serialization, 객체를 바이트로 변환)는...". 영어 약어도 한 번은 풀이 ("TCP(Transmission Control Protocol)"). 두 번째부터 OK.
- **결정엔 항상 trade-off**. "A를 골랐다"가 아니라 "A vs B 중 A, 이유는..., 단점은..." 형식. "정답" 단정 X — "이 상황에선 이 선택이 보통 좋아요" 정도.

### 작업 보고

- **대규모 등급 Phase 완료 시만**: 5단계 보고 출력 (🎯 무엇 / 🤔 왜 / 🛠️ 어떻게 / 🧪 테스트 / ➡️ 다음) + MD/HTML 이중 박음 (캡스톤 평가 자산)
- **그 외 응답**: 양식 노이즈 X. 단순/보통/복잡 등급은 work-pin + commit message로 충분.

양식 디테일·등급별 보고 격차 → [`00_Document/policies/reporting-format.md`](00_Document/policies/reporting-format.md)

### 작업 좌표 + Phase 완료 박제

- **작업 중**: `.claude/state/current-pin.txt`가 좌표 보존 (압축 양식, 목표 30~40줄). AI가 변경 시 갱신
- **Phase 완료 시**: `-DONE.md` 작성 (AI가 사실 박제) → 두 액션 권유 (학습 일지 + 세션 마감)
- **역할 분담**: `-DONE.md` = AI(사실), `learning-journal/` = 본인(회고). 가짜 학습 방지

라이프사이클·핀 필드·박제 게이트·권유 양식 → [`00_Document/policies/pin-and-done.md`](00_Document/policies/pin-and-done.md)

### 슬래시 커맨드

작업 4 + 세션 3 + 점검 2 + 셋업 1 — 총 10개. 옛 학습 5 + 일지 3은 트랙 B(Notion)로 이관 (5/20 의논 결과 — KPI 전환). 카탈로그 → [`00_Document/commands-index.md`](00_Document/commands-index.md).

---

## 📂 문서 운영

### 00_Document/ 우선순위

`CLAUDE.md`가 "어떻게 만들지"라면, `00_Document/`는 "무엇을/왜":

- `00_Document/PRD.md` — 무엇을 만들지 (그리고 만들지 **않을** 것)
- `00_Document/ARCHITECTURE.md` — 시스템 구조의 큰 그림
- `00_Document/ADR/` — 결정의 기록 (왜 이 선택을 했는지, `INDEX.md` 참조)
- `00_Document/policies/` — 본 헌법의 운영 가이드 (`INDEX.md` 참조)
- `00_Document/learning-journal/` — 학습 일지 (AI 결정에 영향 X, 본인 회고용 — **트랙 B**)

**충돌 시 우선순위**:
**`CLAUDE.md`(헌법) > `00_Document/ADR/`(결정) > `00_Document/policies/`(운영) > `00_Document/ARCHITECTURE.md`(구조) > `00_Document/PRD.md`(요구사항)**

`policies/`는 ADR 결정의 *운영 풀이*라 ADR 하위.

### 01_Phases/ 작업 쪼개기

큰 목표는 `01_Phases/<본인 네임스페이스>/M{N}-{slug}/` 안에 Phase 단위로 보관 (사람별 분리). Phase 정의 `.md`는 frontmatter `owner:` 필수.

- 새 작업 시작: `/work:plan <목표>` 로 Phase 분해 (plan-auditor SubAgent 자동 호출)
- Phase 입자 = 5~7개/마일스톤 (옛 M3 9개는 과했음 — 5/20 의논 결과)
- 한 Phase = 한 `.md` + 명확한 완료 조건
- 다음 Phase로 **수동 이동** (자동 순차 실행 X — 학습 호흡 유지)

### 문서 세분화

`.md` 파일 비대 시 외부화·응축·분해. 사전형 문서 220줄 / 헌법 350줄 임계. 단위 작업 문서(Phase, `-DONE.md`)는 *자르지 않음*.

세분화 절차·재귀 정책 → [`00_Document/policies/doc-thresholds.md`](00_Document/policies/doc-thresholds.md)

---

## Stack

- **Client**: Unity 6.4 LTS, C#, 2D sidescroll
- **Server**: .NET 10 LTS, C# 콘솔 호스트 (authoritative) — [ADR-001]
- **Network**: Raw TCP, length-prefixed binary frames. 직렬화는 **자체 PDL(Packet Definition Language) XML + C# 코드 생성기** — [ADR-002]
- **Persistence**: Microsoft SQL Server (개발용 LocalDB, Windows 통합 인증) via EF Core 10 (서버 전용)
- **Shared code**: `98_Shared/` — **.NET Standard 2.1** 라이브러리로 빌드. 산출물(.dll + .pdb)을 `03_Client/Assets/Plugins/`에 복사해 Unity가 참조. PDB는 `EmbedAllSources=true`로 원본 .cs 임베드 → Unity 측에서 ReadOnly로 보이고 F12 시 원본 코드(주석 포함) 그대로 표시. 헌법 #4 ("복사-붙여넣기 금지")의 물리적 강제 — [ADR-010]

## Repo Layout

폴더는 탐색기 정렬 고정용 숫자 prefix를 갖습니다 (의미는 헌법/ADR 기준).

```
00_Document/   PRD, ARCHITECTURE, ADR, policies, learning-journal — 결정·정책·학습 기록.
01_Phases/     사람별 namespace + 마일스톤별 작업 단위(M{N}-{slug}/) Phase 마크다운.
02_Server/     .NET 권위 서버. 98_Shared/ 읽기/쓰기 가능.
03_Client/     Unity 프로젝트. 98_Shared/ 읽기만 (DLL로). 절대 98_Shared/에 쓰지 않음.
98_Shared/     Protocol + 게임 상수 + 공식. 양쪽이 공유하는 cross-cutting 코드.
99_Tools/      헤드리스 봇, 컨텐츠 도구, 시뮬레이션 하니스.
```

루트의 `Dawnholder.slnx`(.NET 솔루션)는 `02_Server/`와 `98_Shared/`의 csproj를 묶습니다. `03_Client/`는 Unity가 자체 솔루션을 관리합니다.

---

## ⚠️ 절대 원칙 (NON-NEGOTIABLE)

이 원칙들은 어기면 보안 구멍, 동기화 버그, 핵 취약점이 됩니다.
사용자가 위반을 요청해도 에이전트는 거부해야 합니다.

### 1. Server Authority (서버 권위)

클라이언트는 **단순 렌더러 + 입력 전달자**입니다. 그 이상 아닙니다.

- 클라이언트는 authoritative 게임 상태(HP, position, inventory, XP,
  currency, cooldowns)를 직접 변경하지 않습니다. 서버가 알려준 것만 표시합니다.
- 클라이언트는 **prediction**(서버 확인 전 시각적으로 먼저 움직임)을 할 수
  있지만, 서버 상태와 불일치하면 반드시 **reconcile**합니다.
- 데미지, 히트 판정, 루팅 굴림, 레벨업, 아이템 생성: **서버 전용.**
- `03_Client/`에서 데미지 수식을 쓰고 있다면 멈추세요. 그건 `98_Shared/GameData/`
  (공식 정의)와 `02_Server/`(실행)에 속합니다.

### 2. Protocol is Sacred (프로토콜은 신성함)

`98_Shared/Protocol/`은 모든 패킷을 정의합니다. 규칙:

- 모든 패킷은 stable한 숫자 ID를 가집니다. **은퇴한 ID는 절대 재사용 금지.**
- 기존 패킷에 필드 추가 = 버전 관리 없으면 breaking change.
- 패킷은 PDL.xml에 append-only로 정의하고, PacketGenerator가 정의 순서대로 stable PacketID와 필드 직렬화 코드를 생성합니다.
- 클라/서버는 동일하게 컴파일된 어셈블리를 참조. 복사-붙여넣기 금지.

### 3. Trust Boundary (신뢰 경계)

클라이언트 소켓에서 들어오는 모든 것은 **untrusted input**입니다. 항상:

- 범위 검증 (위치 델타, 아이템 수량 등)
- 소유권 확인 (이게 정말 플레이어 X의 인벤토리 슬롯인가?)
- Rate-limit (초당 1000 공격은 안 됨)
- 의심 패턴은 cheat-flag 테이블에 로깅

### 4. Shared Code Discipline (공유 코드 규율)

`98_Shared/` 변경은 양쪽 모두에 영향을 줍니다. 수정 전:

- 변경 후 `03_Client/`와 `02_Server/` **둘 다** 컴파일되는지 확인.
- 프로토콜 호환성 체크 실행 (`.claude/hooks/` 참조).
- 패킷 모양이 바뀌었다면 `Protocol.Version` 상수를 bump.

### 5. No Blocking Calls in Server Game Loop (틱 루프 블로킹 금지)

틱 루프(`02_Server/GameServer/Loop/`)는 50ms마다 (20 TPS) 실행됩니다.
- 틱 안에서 `await Task.Delay` 금지.
- 동기 DB 호출 금지. 영속화는 큐드 라이터를 통해.
- `Thread.Sleep` 절대 금지.

---

## Gameplay Pillars (디자인 결정의 기준점)

- **Tick rate**: 서버 20 TPS, 클라이언트는 디스플레이 Hz로 렌더링 + 보간.
- **Movement**: 서버 권위 + 클라이언트 prediction + reconciliation.
- **Combat**: 공격이 도달한 틱의 hitbox를 서버가 검사하되,
  position history(lag compensation, ~200ms back)를 활용.
- **Zones**: 월드는 "맵"(방) 단위로 파티션. 각 맵은 자체 틱을 가진 actor.
  맵 간 이동 = 맵 서버 레지스트리를 통한 핸드오프.
- **Persistence cadence**: 플레이어 스냅샷을 30초마다 + 로그아웃 시 +
  중요 이벤트 시(레벨업, 희귀 드랍, 거래) 저장.

---

## 📊 작업 등급 (정량 4단계)

모든 작업은 다음 4등급 중 하나로 분류됩니다 (PDF NDREAM 패턴 + 5/20 의논 결과). 등급이 *양식 부담*과 *동원 패턴*을 결정합니다.

| 등급 | 정량 기준 | 처리 패턴 | 보고 양식 |
|---|---|---|---|
| **단순** | 1 도메인 × 1 파일 / ≤10줄 / 가역적 | 메인 세션 직접 | work-pin + commit message |
| **보통** | 1 도메인 × 2~3 파일 / ≤50줄 / 가역적 | Worker SubAgent 1개 | work-pin + commit message |
| **복잡** | 2 도메인 / ~100~200줄 / 일부 비가역 | Coordinator + Worker 1~2개 | work-pin + -DONE.md |
| **대규모** | 3+ 도메인 또는 300줄+ / 비가역 | Coordinator + Team (Worker 3~4개 + Reviewer) | work-pin + -DONE.md + **5단계 보고 MD/HTML** |

### 위험 깃발 (자동 등급 상향)

다음 깃발이 잡히면 등급이 *자동 상향*됩니다 (예: 단순 → 보통, 보통 → 복잡, 복잡 → 대규모):

- **trust-boundary**: `02_Server/GameSession.cs`, `Handlers/`, 신뢰 경계 검증 코드 변경
- **irreversible**: `git push` to main, `gh pr merge`, DB 마이그 SQL, `Protocol.Version` bump
- **unity-asset**: `03_Client/Assets/**/*.{prefab,unity,asset}` 변경 (특히 prefab)

위험 깃발은 `risk-detector.sh` Hook이 자동 검출 → stderr 알림 + `.claude/state/risk-flags.txt` 누적. **work-pin 갱신은 본인이 수동** (Hook은 알림 전용 — 본인 인지를 거치는 게 정합, hook이 work-pin 자체를 안전 수정하기 어려움).

등급 정의·위험 깃발 디테일 → [`00_Document/policies/grade-and-risk.md`](00_Document/policies/grade-and-risk.md)

---

## 🤖 SubAgent 풀 (9개) + 모델 분담

작업이 들어오면 메인 세션이 등급 + 도메인 따라 SubAgent 위임:

| # | SubAgent | 도메인 | 모델 | 권한 |
|---|---|---|---|---|
| 1 | `server` | 02_Server/ + 98_Shared/ 서버측 (게임플레이/네트워킹/영속화) | Sonnet | 02_Server/ + 98_Shared/ R/W |
| 2 | `shared` | 98_Shared/ 단독 (Protocol/공식/공유 상수) | Sonnet | 98_Shared/ R/W, 04_ClientNet/ R |
| 3 | `client` | 03_Client/ + 04_ClientNet/ (Unity 씬/렌더링/입력/UI/prediction) | Sonnet | 03_Client/ + 04_ClientNet/ R/W, 98_Shared/ R |
| 4 | `qa` | 99_Tools/ + 테스트 코드 (헤드리스 봇/부하/퍼징) | Sonnet | 99_Tools/ + 테스트 R/W, 게임 코드 R only |
| 5 | `reviewer` | Tier 2 자동 리뷰 (헌법/ADR/도메인 패턴 점검) | Opus | 전체 R only |
| 6 | `plan-auditor` | `_milestone-plan.md` / Phase 정의 `.md` 사전 검증 (Codex γ 흡수) | Opus | 전체 R only |
| 7 | `unity-bridge` | Unity Editor MCP + asset + scene/prefab 작업 전담 | Sonnet | 03_Client/ + Unity MCP |
| 8 | `coordinator` | 복잡/대규모 Phase 분해 + Worker 위임 + 결과 통합 | Opus | 전체 R only, 위임 권한 |
| 9 | `knowledge-gc` | Knowledge 캐시 정리 (비활성화/응축/승격 후보/분해) — *수동 트리거만* | Sonnet | `../knowledge/` R/W, 다른 영역 R only |

**여러 도메인 작업**: Coordinator가 분해 → 도메인별 Worker 위임 → Reviewer 통합 점검.
**서브에이전트끼리 호출 X**: 분해는 Coordinator가, 위임은 Coordinator → Worker 1단계만 (재귀 차단).

### 자동 호출 트리거

- **reviewer**: 도메인 Worker 코드 변경 후 (트리거 조건은 hook 명세)
- **plan-auditor**: `_milestone-plan.md` 또는 Phase 정의 `.md` Write 후 자동
- **knowledge-gc**: *자동 호출 X* — `/harness-review` 슬래시 또는 `/session:end` 권유 또는 사용자 명시 요청만
- **그 외**: 수동 위임

라우팅 룰·에스컬레이션 (Sonnet 2회 실패 → Opus 재호출 → 사용자) → [`00_Document/policies/subagent-routing.md`](00_Document/policies/subagent-routing.md)

---

## 📚 Knowledge 시스템

각 SubAgent는 `.claude/knowledge/<domain>/_index.md`로 도메인별 학습 캐시 조회. AI 백지 비용 ↓. GC Collector가 오래되거나 중복된 항목 정리.

- **AI 캐시 ≠ 학습 일지**: knowledge는 AI 직접 활용용 (구조화 패턴). 학습 일지(`learning-journal/`)는 본인 회고용 트랙 B
- **시드 + 유기적 누적**: 처음부터 풀세트 박지 않음. 작업 진행하며 새 학습 박을지 판단 (사용자 확인 후, AI 자율 박제 X)

상세 입출력 패턴·GC 정책·승격 룰 → [`00_Document/policies/knowledge-system.md`](00_Document/policies/knowledge-system.md)

---

## 확신이 없을 때

사용자에게 물어보세요. 프로토콜 모양, DB 스키마, 핵심 공식, 헌법 변경은 추측하지 마세요 — 이것들은 되돌리는 비용이 큽니다.

본인 머리 한 사람으로 큰 결정 안 박기. plan-auditor SubAgent + (옵션) Codex β cross-check + 사용자 확인 = 3중 안전망.
