# REVIEW_CHECKLIST — 6축 리뷰 매핑

> **이 문서의 역할**: `reviewer` 에이전트가 매번 로드하는 *축약·매핑 체크리스트*.
> 헌법(`CLAUDE.md`), ADR, ARCHITECTURE, layer별 CLAUDE.md에 흩어진 규칙을
> 한 파일에 모아 reviewer가 *최소 토큰으로 6축 점검*할 수 있게 함.

## ⚠️ 책임 범위 (Scope)

**점검 대상 — 아키텍처·원칙 위반만**:
- 헌법 5대 절대 원칙 (Server Authority / Protocol Sacred / Trust Boundary / Shared Discipline / No Blocking)
- 채택된 ADR과의 충돌
- ARCHITECTURE의 디렉토리/의존성 구조
- 테스트 커버리지 (도메인 특성상 필수인 영역)
- 도메인 적합 패턴 (학습 포인트)

**구조/SRP는 점검한다 (ADR-028로 변경)**:
- 클래스 구조 / God class / 도메인 분리 / 패턴 오용 → **축 6 Code Convention으로 편입**. 옛 "메서드·파일 길이 등 코드 크기 = 도구 책임" 제외 조항은 ADR-028이 *구조/SRP에 한해* 뒤집음 (Phase 07 God class를 놓친 근원 봉합).

**점검 대상 *아님* — 순수 포매팅만 (도구 위임)**:
- 네이밍 컨벤션 (camelCase / PascalCase / `_field` 등)
- 들여쓰기, 줄바꿈, using 정렬, brace 위치 등 *포매팅*
- 예외 처리 스타일 (throw vs Result<T> 등 *합의 안 된 영역*)

→ *순수 포매팅*만 **`.editorconfig` + Roslyn**으로 도구 위임 (CODE_CONVENTION §4, M4.4+ 도입).
   reviewer는 포매팅은 "도구 책임"으로 두되, **구조·God class·패턴은 축 6으로 직접 점검**한다.

> **이 문서와 `/harness-review` 슬래시 커맨드의 차이** (ADR-022 정합):
> - 본 체크리스트 = Tier 2 (자동, 매 코드 변경 후, reviewer SubAgent 호출, 요약 보고)
> - `/harness-review` = Tier 3 (수동 깊은 리뷰, Phase 단위 또는 하네스 자체 점검). 본 체크리스트의 코드 점검 부분과 *내용 동일*하되 출력 폭이 다르고 *하네스 정합* 점검까지 확장.

## 유지 보수 규칙 (동기화 부담 관리)

본 체크리스트는 헌법·ADR·layer CLAUDE.md의 *복제*가 아니라 *매핑 인덱스*. 따라서 원본 갱신 시 본 체크리스트의 *해당 항목*만 같이 갱신.

**원본 변경 시 갱신 동작**:

| 원본 변경 | 본 체크리스트 갱신 동작 |
|---|---|
| 새 ADR 채택 (ADR-NNN 추가) | 코드 영역 영향이면 축 2에 1줄 추가. 코드 영향 없는 ADR(Notion/문서 정책 등)은 추가 X. |
| 헌법 절대 원칙 변경 | 축 1 갱신 (드뭄 — 헌법 350줄 임계로 갱신 자체가 신중). |
| ARCHITECTURE 디렉토리/패턴 변경 | 축 3 갱신. |
| layer별 CLAUDE.md (02_Server / 98_Shared / 03_Client) 변경 | 영향 영역의 축 갱신. |

**새 ADR 추가 절차 5단계** (`ADR.md`에도 명시 예정 — Step 3에서 반영):
1. `ADR/{카테고리}/ADR-NNN-slug.md` 생성
2. `ADR/INDEX.md` 한 줄 추가
3. `ADR.md` 후보 표에서 제거
4. `ADR_History.md` 한 줄 추가
5. **본 `REVIEW_CHECKLIST.md` 축 2 매핑 갱신** ← 새 단계

**갱신 누락 안전망 (합류 후 추가 예정)**: `.claude/hooks/check-checklist-sync.sh` — ADR 파일 mtime이 본 체크리스트 mtime보다 최근이면 다음 세션 시작 시 경고. 합류 첫 주 안정화 후 도입.

---

## 등급 정의

| 등급 | 의미 | reviewer 행동 |
|------|------|--------------|
| 🔴 | 헌법/ADR/구조 **위반**. 고쳐야 함. | 메인 세션에 강하게 보고. 사용자에게 "고칠까요?" 유도. |
| 🟡 | 위반 아니지만 *개선 가능*. 취향/패턴/학습 포인트. | 보고하되 통과. 학습 기회로 짚음. |
| 🟢 | 잘 된 부분. 강점 인지. | 위반 0개일 때 짧게 칭찬. 자신감 빌드용. |
| 🎓 | 학습 포인트. 위반 아니지만 *개념을 짚어두면 좋은* 자리. | 1~2개 골라 학부생 톤으로 설명. |

---

## 축 1: 헌법 (CLAUDE.md 5대 절대 원칙)

루트 [`CLAUDE.md`](../CLAUDE.md) 의 절대 원칙. 위반 시 보안 구멍 / 동기화 버그 / 핵 취약점.

| # | 점검 항목 | 출처 | 등급 |
|---|----------|------|------|
| 1.1 | 클라가 authoritative 상태(HP/XP/inventory/currency/cooldown) 직접 변경 | 헌법 §1 Server Authority | 🔴 |
| 1.2 | 데미지·히트 판정·루팅·레벨업·아이템 생성을 클라에서 수행 | 헌법 §1 | 🔴 |
| 1.3 | 클라 prediction 결과를 서버 reconcile 없이 commit | 헌법 §1 + `03_Client/CLAUDE.md` "Authoritative 경계선" | 🔴 |
| 2.1 | 은퇴 PacketId 재사용 / 기존 ID 값 변경 | 헌법 §2 Protocol Sacred + `98_Shared/CLAUDE.md` | 🔴 |
| 2.2 | PDL.xml이 아닌 수동 패킷 struct 작성 또는 PacketID/필드 순서 임의 지정 | 헌법 §2 + ADR-002 v2 | 🔴 |
| 2.3 | 기존 패킷에 필드 *재정렬* 또는 *중간 삽입* (끝 추가는 OK) | 헌법 §2 + `98_Shared/CLAUDE.md` "PacketId" | 🔴 |
| 2.4 | 클라/서버가 서로 다르게 컴파일된 protocol을 참조 (복사-붙여넣기) | 헌법 §2 + ADR-010 | 🔴 |
| 3.1 | 클라 소켓 입력 사용 전 *범위 검증* 누락 (위치 델타, 수량 등) | 헌법 §3 Trust Boundary | 🔴 |
| 3.2 | 클라 소켓 입력 사용 전 *소유권 확인* 누락 | 헌법 §3 | 🔴 |
| 3.3 | 클라 소켓 입력 사용 전 *rate-limit* 누락 (적용 가능한 경우) | 헌법 §3 | 🔴 |
| 3.4 | length-check 전에 buffer 할당 | 헌법 §3 + `netcode.md` Hard rule #4 | 🔴 |
| 4.1 | `98_Shared/` 변경 후 양쪽 빌드 검증 미수행 | 헌법 §4 Shared Discipline | 🔴 |
| 4.2 | 패킷 모양 breaking change인데 `Protocol.Version` bump 누락 | 헌법 §4 | 🔴 |
| 4.3 | `02_Server/` 또는 `03_Client/`에서 `98_Shared/` 코드 *복사* (DLL 참조 X) | 헌법 §4 + ADR-010 | 🔴 |
| 5.1 | 틱 루프(`02_Server/GameServer/Loop/`)에서 `await DB` / `await Task.Delay` | 헌법 §5 No Blocking + `02_Server/CLAUDE.md` "금지 사항" | 🔴 |
| 5.2 | 틱 루프에서 `Thread.Sleep` | 헌법 §5 | 🔴 |
| 5.3 | 틱 루프에서 동기 DB 호출 (write queue 우회) | 헌법 §5 + ARCHITECTURE "Persistence Write Queue" | 🔴 |
| 5.4 | 틱 루프 안에서 `Task.Run` | `02_Server/CLAUDE.md` "금지 사항" | 🔴 |

---

## 축 2: ADR (00_Document/ADR/)

채택된 ADR과 충돌하는 코드. INDEX 출처: [`00_Document/ADR/INDEX.md`](ADR/INDEX.md).

### 2-A. Tech-stack ADR (직렬화 / DB / 구조)

| # | 점검 항목 | 위반 ADR | 등급 |
|---|----------|----------|------|
| 2A.1 | TCP 외 통신 프로토콜 (UDP/WebSocket/gRPC) 도입 | ADR-002 (Raw TCP + 자체 PDL) | 🔴 |
| 2A.2 | 자체 PDL XML + 코드 생성기 외 *대체 직렬화* 사용 | ADR-002 v2 (자체 PDL만) | 🔴 |
| 2A.3 | `98_Shared/Protocol/Generated/GenPackets.cs` 외부에서 패킷 *수동 작성* | ADR-002 v2 + `98_Shared/CLAUDE.md` | 🔴 |
| 2A.4 | MSSQL/EF Core 외 DB 또는 ORM 도입 (개발 단계) | ADR-005 v2 | 🔴 |
| 2A.5 | `98_Shared/` 외부에 패킷 정의 (서버/클라 어느 쪽이든) | ADR-003 모노레포 + ADR-010 | 🔴 |
| 2A.6 | 클라용 socket 코드를 `98_Shared/`에 넣음 (Y2 분리 위반) | ADR-012 | 🔴 |
| 2A.7 | 서버용 socket 코드를 `98_Shared/`에 넣음 (Y2 분리 위반) | ADR-012 | 🔴 |
| 2A.8 | Unity 6.4 LTS / .NET 10 LTS 아닌 SDK 사용 (`global.json` 무시) | ADR-001 | 🔴 |
| 2A.9 | 20 TPS 외 틱 레이트 하드코딩 (`98_Shared/GameData/Constants.cs` 우회) | ADR-004 | 🟡 |
| 2A.10 | 분산/샤딩 가정 코드 (현재는 단일 프로세스) | ADR-008 | 🟡 |

### 2-B. Harness ADR (하네스 운영)

| # | 점검 항목 | 위반 ADR | 등급 |
|---|----------|----------|------|
| 2B.1 | Phase 완료인데 `-DONE.md` 페어 작성 누락 | ADR-013 | 🔴 |
| 2B.2 | `-DONE.md` 필수 섹션(TL;DR / 5단계 보고 / AC 검증 / 결정 흐름 / 학습 키워드) 누락 | ADR-015 post-flight 게이트 | 🔴 |
| 2B.3 | `.md` 파일 220줄 초과 (헌법 350줄 예외) — 단위 작업 문서 제외 | ADR-014 | 🟡 |
| 2B.4 | (폐기됨, M3.5 Phase 06 — ADR-022) 옛 `work-envelope` 봉투 양식이 죽음. 새 모델 = 5단계 보고는 *대규모 등급 Phase 완료 시만*. 단순/보통/복잡은 work-pin + commit message로 충분 | ADR-022 | — |

---

## 축 3: 구조 (ARCHITECTURE.md + 디렉토리 의존성)

| # | 점검 항목 | 출처 | 등급 |
|---|----------|------|------|
| 3.1 | TCP/세션 코드가 `02_Server/GameServer/Network/` 외부에 위치 | ARCHITECTURE "디렉토리" + `netcode.md` turf | 🔴 |
| 3.2 | 데미지/전투 코드가 `02_Server/GameServer/Combat/` 외부에 위치 | ARCHITECTURE + `gameplay.md` turf | 🔴 |
| 3.3 | DB 쓰기 코드가 `Persistence/` 외부 + write queue 우회 | ARCHITECTURE "Persistence Write Queue" + `persistence.md` | 🔴 |
| 3.4 | 한 Map 안에서 `lock` / `Monitor` 사용 (actor 모델 위반) | ARCHITECTURE "Map = Actor" + `02_Server/CLAUDE.md` | 🔴 |
| 3.5 | 맵 간 직접 메서드 호출 (message channel 우회) | ARCHITECTURE "Map = Actor" | 🔴 |
| 3.6 | `03_Client/`에서 `98_Shared/` *쓰기* (읽기만 허용) | 헌법 Repo Layout + ADR-010 | 🔴 |
| 3.7 | 정적 mutable 게임 상태 (싱글톤 mutable 필드) | `02_Server/CLAUDE.md` "금지 사항" | 🔴 |
| 3.8 | 클라에서 레거시 `Input.GetKey` 사용 (새 Input System 의무) | `03_Client/CLAUDE.md` | 🟡 |
| 3.9 | 클라 게임플레이 타이밍에 `Time.time` 사용 (서버 tick 의무) | `03_Client/CLAUDE.md` "금지 사항" | 🔴 |
| 3.10 | 클라에 게임 밸런스 숫자 하드코딩 (`98_Shared/GameData/` 우회) | `03_Client/CLAUDE.md` "금지 사항" | 🔴 |
| 3.11 | 클라 네트워크 코드가 메인 스레드에서 read | `03_Client/CLAUDE.md` "컨벤션" | 🟡 |
| 3.12 | 한 `MonoBehaviour`가 여러 개념 담음 (갓-오브젝트) | `03_Client/CLAUDE.md` | 🟡 |

---

## 축 4: 테스트 커버리지

`/harness-review` 슬래시 커맨드의 "점검 4" 그대로 (ADR-022 정합). 게임 서버 도메인에서 테스트 없으면 늦게 폭발하는 영역은 🔴, 일반 영역은 🟡.

| # | 점검 항목 | 등급 |
|---|----------|------|
| 4.1 | 새 핸들러에 happy path 테스트 누락 | 🔴 |
| 4.2 | 새 핸들러에 invalid input 거부 테스트 누락 | 🔴 |
| 4.3 | 새 핸들러에 auth 테스트 누락 (적용 가능한 경우) | 🔴 |
| 4.4 | 새 공식(damage / stat / xp)에 단위 테스트 누락 | 🔴 (입력→출력 명확한데 안 쓰면 회귀 시 무성하게 깨짐) |
| 4.5 | PDL ↔ 패킷 직렬화 *라운드트립* 테스트 누락 | 🔴 (호환성 깨짐이 production에서 발견됨) |
| 4.6 | 상태 머신(연결/세션/캐릭터 lifecycle) 엣지 케이스 미커버 | 🔴 (머릿속 추론 불가) |
| 4.7 | 그 외 영역(렌더링, UI, 도구 등) 테스트 누락 | 🟡 |
| 4.8 | 테스트 이름이 의도를 표현하지 못함 (예: `Test1`, `TestHandler`) | 🟡 |

**테스트 부재 시 위 4.1~4.6은 🔴**, 그 외는 🟡로 짚되 학습 포인트 첨부.

---

## 축 5: 도메인 적합 패턴 (학습 포인트)

위반 아니지만 게임 서버 도메인에서 학습 가치 큰 패턴들. 🟡 또는 🎓 학습 포인트로 짚음.

| # | 점검 항목 | 등급 |
|---|----------|------|
| 5.1 | Composition over inheritance — 상속 깊이 ≥2 단계 또는 "X도 되고 Y도 된다" 식 클래스 | 🎓 |
| 5.2 | Hot path 알로케이션 최소화 — 틱 루프 / 패킷 처리 경로에서 매 호출 `new` / `ToList()` / LINQ 체인 / 박싱 | 🟡 |
| 5.3 | Rule of three — 비슷한 로직 *2번째* 등장에서 추상화 (premature abstraction) | 🟡 |
| 5.4 | 명시적 상태 머신 — implicit `if (state == X && other != Y && ...)` 대신 enum + transition 함수 | 🎓 |
| 5.5 | YAGNI / scope creep — `00_Document/PRD.md`의 "MVP 제외" 항목이 슬그머니 들어옴 | 🟡 |
| 5.6 | 미래 확장성 hook인데 현재 호출자가 0개 | 🟡 |
| 5.7 | **명시된 안전망의 코드 동작 일치** — 주석/상수/문서로 박힌 약속 (`rate-limit`, `ProtocolVersion`, cooldown 등)이 실제 코드에서 *차단/검증을 수행*하는지 점검. 약속만 박히고 호출/적용 누락 시 *silent 우회* 위험. 출처: 2026-05-18 ad-hoc 감사 (Codex 발견 rate-limit "기록만" + Claude 발견 ProtocolVersion 호출처 0건). | 🟡 (헌법 §3 정신 위반에 가까움) |

이 축은 *위반이 아니라 학습 기회*. 학부생이 SOLID 같은 추상 원칙을 외우는 것보다 실제 코드에서 만나는 게 효과적이라는 학습 철학을 따름 (`/harness-review` 슬래시 커맨드 정신 유지 — ADR-022 정합).

---

## 축 6: Code Convention (ADR-028)

[`00_Document/conventions/`](conventions/INDEX.md)(CODE_CONVENTION + refs) 위반 점검. **ADR-028이 옛 "코드 크기/구조 = 도구 책임" 제외 조항을 구조/SRP에 한해 뒤집음** — God class·패턴 위반은 reviewer가 직접 본다(순수 포매팅만 도구 위임).

| # | 점검 항목 | 출처 | 등급 |
|---|----------|------|------|
| 6.1 | 한 클래스가 2+ 도메인(전투+AI+네트워크 등) — God class | CODE_CONVENTION §2.2 | 🔴 |
| 6.2 | 게임 로직이 컨테이너(`GameMap`/`GameSession`/MonoBehaviour)에 직접 박힘 — System 미분리 | §2.2 | 🟡 |
| 6.3 | 콘텐츠(게임 규칙)가 엔진(`02_Server/Network/` 인프라)에 혼재 | §1.2 | 🔴 |
| 6.4 | 과한 추상화 — 호출자 0개 확장 hook / 단일 도메인 억지 분할 (과분할도 부채) | §0.3 | 🟡 |
| 6.5 | 채택 패턴 오용 (Singleton mutable 상태 / 보간 대신 extrapolation 등) | refs 해당 패턴 | 🟡 |
| 6.6 | 핵심 파일(`GameMap`/`GameSession`/`UnityClientSession`) 600줄+ 비대 (size-guard hook 연동) | §2.3 | 🟡 |

위반 시 `CODE_CONVENTION` 해당 § + `refs/` 파일을 인용해 보고. 작업 유형별 라우팅 = [`conventions/INDEX.md`](conventions/INDEX.md).

---

## reviewer 에이전트 출력 포맷

reviewer 에이전트는 본 체크리스트를 기준으로 6축 점검 후 다음 포맷으로 *메인 세션에만* 요약 반환:

```
🔍 Tier 2 자동 리뷰 결과
─────────────────────────
범위: <변경 파일 목록 또는 phase slug>

🔴 위반 N개:
  - [축X.Y] <파일:줄> <한 줄 설명> — 수정 방향: <한 줄>
  - ...

🟡 개선 제안 N개:
  - [축X.Y] <파일:줄> <한 줄> — <한 줄 이유>
  - ...

🎓 학습 포인트 (있으면 1~2개):
  - <한 문단, 학부생 톤>

🟢 잘 된 점 (위반 0개일 때만):
  - <한두 줄>

➡️ 권장 액션:
  - <위반 있으면: 사용자 확인 후 수정>
  - <없으면: 통과>
```

**중요**: reviewer는 *짚기만* 함. 코드 직접 수정 X (Read/Glob/Grep/Bash만 허용).
실제 수정은 메인 세션이 사용자 확인 후 도메인 에이전트에 위임.

---

## 변경 이력

| 날짜 | 변경 | 이유 |
|------|------|------|
| 2026-05-15 | 최초 작성 | ADR-019 (시니어 피드백: 리뷰어 에이전트 도입) 결과물. Tier 2 자동 리뷰 기반 자료. 책임 범위(아키텍처만, 코드 스타일 제외) 명시 + 옵션 4 동기화 절차 박음. analyzer 도입은 ADR 후보로 미룸. |
| 2026-05-18 | 축 5에 5.7 추가 (명시된 안전망의 코드 동작 일치 검증, 🟡) | Pre-M3 ad-hoc 전체 감사 (γ 방식)에서 Codex가 잡은 rate-limit "기록만" 패턴 + Claude가 잡은 ProtocolVersion 호출처 0건 패턴이 동형 — *약속이 코드 동작까지 박혀야 안전망 진짜*. 본인 헌법 §3 정신 위반에 가까움 (Trust Boundary), 학습 가치 큼 (`CONTEXT_LearningJournalCandidates.md` ★★★ 신규 항목과 정합). Rule of Three까지 ADR 신설은 보류. |
| 2026-05-29 | 축 6 (Code Convention, ADR-028) 추가 | God class·구조/SRP 점검을 reviewer 직접 책임으로 편입 (옛 "코드 크기=도구" 제외 조항을 구조/SRP에 한해 뒤집음). M4.3R. |
| 2026-05-30 | 제목·설명 "5축"→"6축" stale 정정 | 축 6 추가 후 표현 미갱신 (Codex read-only 감사 발견). |
