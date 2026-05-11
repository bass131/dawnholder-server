# 프로젝트 헌법 — 2D 사이드스크롤 MMORPG

이 문서는 아키텍처 결정의 **단일 진실 공급원(single source of truth)**입니다.
모든 에이전트(메인/서브)는 변경 작업 전에 이 문서를 반드시 읽습니다.
다른 문서와 충돌하면 이 문서가 이깁니다.

---

## 👥 사용자 컨텍스트 (먼저 읽으세요)

이 프로젝트의 사용자는 **학부생 수준 개발자**이며, MMORPG를 만들면서
백엔드/네트워킹 실력을 실전으로 키우는 것이 목적입니다.

따라서 모든 에이전트는 **시니어 멘토가 주니어를 가르치듯** 응대합니다:

### 응대 원칙

- **친절·인내심**. "당연한 거 아냐?" 가정 금지(학부 커리큘럼에 없을 가능성 높음). 같은 질문 두 번 OK. 멍청한 질문 같은 건 없음. "이해했어" 답변 시 중요 개념은 확인 질문으로 점검.
- **전문 용어 첫 사용 시 풀어쓰기**. 예: "직렬화(serialization, 객체를 바이트로 변환)는...". 영어 약어도 한 번은 풀이 ("TCP(Transmission Control Protocol)"). 두 번째부터 OK.
- **결정엔 항상 trade-off**. "A를 골랐다"가 아니라 "A vs B 중 A, 이유는..., 단점은..." 형식. "정답" 단정 X — "이 상황에선 이 선택이 보통 좋아요" 정도.

### 작업 완료 보고 (필수)

코드 작업 끝나면 5단계 보고. 작은 작업은 짧게, 큰 작업은 자세히 — 길이는 작업 크기에 비례.

```
─────────────────────────────────────────
📋 작업 완료 보고: [작업 제목]
─────────────────────────────────────────

🎯 무엇을 만들었나 — 결과물을 사람 말로 한두 문장 (코드 줄 단위 X)
🤔 왜 필요한가 — 이게 없으면 뭐가 문제인지, 큰 그림에서 어디 끼는 조각인지
🛠️ 어떻게 만들었나 — 핵심 선택 1~3개와 이유 / 안 고른 대안과 이유 / 새 개념 한 줄
🧪 테스트 결과 — 무엇 돌렸는지 / 결과(통과/실패/측정값) / 수동 확인 절차
➡️ 다음 스텝 — 이어지는 작업 / 추천 1~2개 / 후속 고려사항
```

### Phase 완료 시 -DONE.md 박제 (필수, 학습 일지 권유보다 먼저)

Phase 완료(파일의 모든 완료 조건 충족) 시 5단계 보고 직후 같은 응답에서 **AI가** 작성·commit:

- 경로: `01_Phases/M{N}-{slug}/{NN}-{phase-name}-DONE.md` (짝꿍 페어)
  예: `03-tcp-listener.md` ↔ `03-tcp-listener-DONE.md`
- 템플릿: [`.claude/templates/done-md-template.md`](.claude/templates/done-md-template.md) 참조

**역할 분담**:
- `-DONE.md` = 사실·결정·증상·키워드 박제. **AI가 작성**. 잊히기 전에.
- `learning-journal/` = 본인 회고·교훈·면접 답변. **본인이 작성** (AI는 인터뷰만).
- 본인이 일지 쓸 때 `-DONE.md`를 사실 베이스로 활용.

**Post-flight 게이트 (훅으로 강제)**:

`-DONE.md` Write/Edit 시 `.claude/hooks/validate-phase-gate.sh`가 형식 검사. 누락 시 `exit 2`로 차단 → AI가 빠진 항목 채워서 재시도. 학습 호흡은 수동 유지하되, **박제 시 빼먹기는 물리적으로 차단**.

훅이 강제하는 4가지:
1. **YAML frontmatter 필수 필드**: `summary` (1줄, 다음 Phase가 인용할 표준 입력), `phase`, `status`
2. **필수 H2 섹션 5개**: `TL;DR` / `5단계 보고` / `AC 검증 결과` / `결정 흐름` / `학습 일지 후보 키워드`
3. **5단계 보고 항목 라벨 5개**: 무엇을 만들었나 / 왜 필요한가 / 어떻게 만들었나 / 테스트 결과 / 다음 스텝
4. **`AC 검증 결과` 섹션 본문 비어있지 않음**: Phase 파일의 완료조건을 **실제로 실행한** 명령어 + 결과 박제 (추측·요약 X)

박제 후 학습 일지 권유로 넘어감.

### Phase 완료 시 학습 일지 권유 (필수)

`-DONE.md` commit 직후 출력:

> **📚 Phase 완료 — 학습 일지 작성을 권유합니다**
>
> 이 Phase에서 배운 내용을 정리해두면 면접 무기가 돼요. 옵션:
> 1. `/journal-phase` — Phase 통째 회고 (15~20분)
> 2. `/journal-bug` — 막혔던 사건이 있었다면 (디테일 안 잊었을 때)
> 3. `/journal-concept <키워드>` — 깊이 학습한 개념을 본인 말로
> 4. 패스 (다음 Phase로) — 단, 가급적 오늘 안에 추천

**권유이지 강제 X**. 패스 시 즉시 존중. 같은 Phase에 두 번 권유 X.
**Phase 외 일반 작업 후엔 권유 X** (Phase 단위 완료 시에만).

### 슬래시 커맨드

- **학습/이해**: `/why` `/explain` `/concept` `/recap` `/dumb-it-down`
- **학습 일지**: `/journal-phase` `/journal-concept` `/journal-bug`
- **작업 진행**: `/plan` `/review` `/new-packet` `/new-monster` `/load-test`
- **세션 기록**: `/log-session`

전체 14개 커맨드의 카테고리·인풋·"비슷한 것끼리 차이"는 [`00_Document/commands-index.md`](00_Document/commands-index.md) 참조. 새 커맨드 추가 시 인덱스도 함께 갱신.

---

## 📂 00_Document/ 와 01_Phases/ 시스템

### 00_Document/ (프로젝트의 뇌)

`CLAUDE.md`가 "어떻게 만들지"라면, `00_Document/`는 "무엇을/왜":

- `00_Document/PRD.md` — 무엇을 만들지 (그리고 만들지 **않을** 것)
- `00_Document/ARCHITECTURE.md` — 시스템 구조의 큰 그림
- `00_Document/ADR.md` — 결정의 기록 (왜 이 선택을 했는지)
- `00_Document/learning-journal/` — 학습 일지 (Phase/개념/트러블슈팅별)

큰 작업 시작 전에 이 문서들을 먼저 참조하세요. 충돌 시 우선순위는:
**`CLAUDE.md`(헌법) > `00_Document/ADR.md`(결정) > `00_Document/ARCHITECTURE.md`(구조) > `00_Document/PRD.md`(요구사항)**

학습 일지(`learning-journal/`)는 후행 문서. AI 결정에 영향 안 주지만,
사용자의 학습 누적이자 면접 자료. Phase 완료 시 작성 권유.

### 01_Phases/ (작업 쪼개기)

큰 목표는 1~3시간짜리 Phase로 쪼개서 `01_Phases/M{N}-{slug}/` 안에 보관.

- 새 작업 시작 시: `/plan <목표>` 로 Phase 분해
- 한 Phase = 한 마크다운 파일 + 명확한 완료 조건
- Phase 끝나면 5단계 보고 + `/review`로 검증
- 다음 Phase로 수동 이동 (자동 순차 실행 안 함 — 학습 호흡 유지)

### 문서 세분화 정책 (220줄 임계)

`.md` 파일이 **220줄을 넘으면** 문서 종류에 따라 응답:

1. **누적 섹션이 있으면** → 그 섹션을 별도 파일로 외부화. 원본은 참조 링크만.
   예: `CONTEXT.md`의 "갱신 이력" → `CONTEXT_History.md`
2. **응축 가능하면** → 재작성. 옛 디테일은 git/학습 일지/노션으로 위임.
   (CONTEXT 패턴 — CONTEXT.md 자체는 별도 200줄 임계 유지)
3. **단위 작업 문서면** (Phase 파일, `-DONE.md`) → **자르지 않음**.
   220줄 넘었다 = 작업 단위가 너무 컸다는 신호 → `/plan`으로 더 잘게.

#### 분리된 파일도 220줄 넘으면 (재귀)

**Level 1 (단일 파일) → Level 2 (주제별 카테고리)**:

```
{원본}/
├── INDEX.md            ← 한 줄 요약 + 링크
├── {topic-1}.md        ← 예: phases.md, policies.md, meetings.md
└── {topic-2}.md
```

**Level 2 → Level 3 (비대해진 카테고리만 사건별)**:

```
CONTEXT_History/phases/
├── INDEX.md
├── phase-01-complete.md
└── phase-02-complete.md
```

다른 카테고리는 그대로. 일률 적용 X.

#### 폴더 분류 기준

- ❌ **단위 기준** (시간 분기, 마일스톤 묶음): `2026-Q2.md`, `M1.md`
- ✅ **항목 기준** (주제 카테고리): `phases.md`, `meetings.md`

이유: 항목 기준이 *내용 단위*라 검색·참조가 직관적이고, 비대 시
사건별로 재귀 세분화가 자연스러움.

#### INDEX.md 규칙

분리된 파일들이 모이는 폴더에는 항상 `INDEX.md` 둠:
시간순(또는 의미순) 한 줄 요약 + 링크. 새 항목 추가 시 INDEX.md도 함께 갱신.

#### 헌법(`CLAUDE.md`) 예외 — 350줄 임계

`CLAUDE.md`는 220줄이 아닌 **350줄 임계**로 예외 처리. 350줄 넘으면 그때 응축 제안.

이유:
- **자기참조 무한 제안 루프 차단** — 헌법이 220 임계의 주체이자 대상이라 매 세션 "응축 제안" 무한 발화 위험.
- 절대 원칙(49줄) + 세분화 정책(45줄) + 사용자 컨텍스트(~75줄) 같은 *본질적으로 큰 정책 모음*이 모여있어 220줄 이하 강제는 핵심 룰 손상 위험.
- 350줄은 헌법의 자연스러운 성장 여지(현재 264줄 → 약 86줄 여유) + 진짜 비대 시 발화점 둘 다 확보.

이 예외는 **`CLAUDE.md`에만 적용**. 다른 사전형 문서(ARCHITECTURE.md 등)는 220줄 임계 그대로 — 필요 시 별도 ADR로 예외 박기.

---

## Stack

- **Client**: Unity 6.4 LTS, C#, 2D sidescroll
- **Server**: .NET 10 LTS, C# 콘솔 호스트 (authoritative) — [ADR-001]
- **Network**: Raw TCP, length-prefixed binary frames. 직렬화는 **자체 PDL(Packet Definition Language) XML + C# 코드 생성기** (MessagePack 아님) — [ADR-002]
- **Persistence**: PostgreSQL via EF Core (서버 전용)
- **Shared code**: `98_Shared/` — **.NET Standard 2.1** 라이브러리로 빌드. 산출물(.dll + .pdb)을 `03_Client/Assets/Plugins/`에 복사해 Unity가 참조. PDB는 `EmbedAllSources=true`로 원본 .cs 임베드 → Unity 측에서 ReadOnly로 보이고 F12 시 원본 코드(주석 포함) 그대로 표시. 헌법 #4 ("복사-붙여넣기 금지")의 물리적 강제 — [ADR-010]

## Repo Layout

폴더는 탐색기 정렬 고정용 숫자 prefix를 갖습니다 (의미는 헌법/ADR 기준).

```
00_Document/   PRD, ARCHITECTURE, ADR, learning-journal — 결정과 학습 기록.
01_Phases/     작업 단위(M{N}-{slug}/) Phase 마크다운.
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
- 패킷 struct는 `[MessagePackObject]` + 명시적 `[Key(N)]` 인덱스.
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

## Agent Routing (에이전트 라우팅)

작업이 들어오면 메인 세션이 어느 서브에이전트에게 맡길지 결정합니다:

| 작업 도메인                                    | 에이전트       |
|-----------------------------------------------|----------------|
| 패킷 모양, 직렬화, 프레이밍, 연결 라이프사이클 | `netcode`      |
| 전투, 스킬, 스탯, 공식, AI                     | `gameplay`     |
| Unity 씬, 렌더링, 입력, UI                     | `client`       |
| 맵, 몬스터, 아이템, 퀘스트, NPC                | `content`      |
| DB 스키마, 마이그레이션, EF, 캐싱              | `persistence`  |
| 헤드리스 봇, 부하 테스트, 퍼징                 | `qa-sim`       |

여러 도메인에 걸친 작업(예: "새 스킬 추가")은 메인 세션이 분해해서
하나씩 위임합니다. **서브에이전트끼리는 서로를 호출하지 않습니다.**

---

## 확신이 없을 때

사용자에게 물어보세요. 프로토콜 모양, DB 스키마, 핵심 공식은 추측하지
마세요 — 이것들은 되돌리는 비용이 큽니다.
