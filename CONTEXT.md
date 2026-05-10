# CONTEXT — 세션 핸드오프

> **이 문서를 읽는 Claude에게**: 헌법(`CLAUDE.md`)과 함께 가장 먼저 읽으세요.
> 이전 세션의 톤·결정을 잇기 위한 핸드오프 노트입니다 (헌법과 충돌 시 헌법이 이김).
>
> **사용자에게**: 새 Claude Code 세션 시작 시 가장 먼저 읽힐 파일.
>
> **유지 정책 (2026-05-09 결정)**: **누적이 아니라 응축**. ~200줄 넘으면 큰
> 마일스톤 끝날 때마다 처음부터 재작성. 옛 디테일은 `git history` +
> `00_Document/learning-journal/` + 노션 "Dawnholder 협업 히스토리"에서 찾기.

---

## TL;DR — Claude는 다음 톤으로 응답하세요

1. **학부생 멘토링 모드**. 시니어가 주니어 가르치듯 친절히. 전문 용어 첫 사용 시 풀이.
2. **trade-off 항상 설명**. "A 골랐어요"가 아니라 "A vs B 중 A, 이유는…, 단점은…".
3. **솔직함 우선**. 위험 미리 짚기. 마감 못 지킬 것 같으면 정직하게.
4. **5단계 보고**. 코드 작업 끝나면 🎯 무엇 / 🤔 왜 / 🛠️ 어떻게 / 🧪 테스트 / ➡️ 다음.
5. **Phase 완료 시 학습 일지 권유**. `/journal-phase` 등.

상세 톤 가이드: 헌법(`CLAUDE.md`) "사용자 컨텍스트" 섹션.

---

## 사용자 컨텍스트 (변하지 않는 것)

- **신분**: 학부생. 도메인 학부생 수준, 판단 본능은 시니어 진입로.
- **언어**: 한국어 (대화). 코드/식별자 영어.
- **위치**: 한국 (KST).
- **목표**: 게임 회사 백엔드 포지션 포트폴리오 + 백엔드/네트워킹 실전 학습.
- **프로젝트**: Dawnholder Project (메이플 같은 캐주얼 RPG + 길드 거점 타이쿤).
- **솔직함 패턴**: 모르는 건 모른다고, 마감 현실도 솔직히. 환영하고 같이 솔직하게.

---

## 하드 일정

- **6월** — 캡스톤 1 발표 (수업 중간, "진행 중" OK)
- **11월 19일** — 졸업작품 본 마감

→ Phase A (~6월): M1~M3 도달, 두 명 같은 맵 데모. Phase B (7~11월): M4~M8 MVP.

---

## 팀 구조 (2026-05-06 미팅 후)

| 역할 | 이름 | 영역 | 합류 시점 |
|------|------|------|-----------|
| 본인 (팀장) | 유영호 | 백엔드 코어 | 사용 중 |
| 팀원 1 | 김인규 | Unity 클라 아트 리소스 및 컨텐츠 개발 | 6월 말 학기 후 |
| 팀원 2 | 정유현 | Unity 클라 UI/입력 및 컨텐츠 개발 | 6월 말 학기 후 |
| 팀원 3 | 박정우 | 관리 시스템 (MES, **별도 레포**) | 7월 이후 (구독비 사유) |

- 팀원 전원 개발 경험 거의 백지. 온보딩은 그 수준에서.
- **캡스톤 1 시점 = 본인 단독 작업 가정.**

---

## ⏸️ 현재 멈춤 지점 (2026-05-10)

**Phase 03 완료 — Phase 04 진입 직전.**

- ✅ Phase 01 완료 (commit `2411ae0`): 솔루션 부트스트랩 + DLL 빌드 파이프라인. 학습 일지 박제.
- ✅ 폴더 prefix 정렬 + 경로 정합성 일괄 갱신 (commit `071680e`)
- ✅ Phase 02 완료 (commit `c2ea772`): ServerCore 7파일을 `02_Server/Network/`에 .NET 10 유지로 정착. nullable 21곳 청소. 빌드 경고 0 / 오류 0, 테스트 3 통과. 노션 세션 로그 박제.
- ✅ **박제 정책 결정 (2026-05-10)**: 학습 일지가 밀릴 것을 전제로 **`-DONE.md` 페어 도입**. AI가 작성하는 사실·결정·증상 박제 (5단계 보고 직후 자동). 학습 일지(본인 회고)와 역할 분리.
- ✅ **ADR-012 박힘 (2026-05-10)**: Phase 03 갈래 = Y2(분리 + 별도 클라 라이브러리). 사유: 현업 표준, socket 자체 학습 가치, 서버 변경 격리.
- ✅ **Phase 03 완료 (2026-05-10, commit `fb7a06d` + `c3f2246`)**: `04_ClientNet/` 신규 .NET Std 2.1 라이브러리 (Connector / ClientSession+PacketSession / RecvBuffer / SendBuffer / SmokeProbe). 5개 프로젝트 빌드 경고 0 / 오류 0. **Unity F12 → 원본 .cs + 한국어 주석 ReadOnly 표시 검증 통과** (ADR-010 패턴 두 번째 인스턴스). `-DONE.md` 박제 + commit.
- ⏳ Phase 02·03 학습 일지는 본인 페이스에 따라 추후 (`/journal-phase` 권유는 통과)

**다음 작업**: Phase 04 진입 — 서버 `Program.cs`에 Listener 인스턴스화(포트 7777) + Unity 측 MonoBehaviour로 ClientNet의 Connector 호출 → 양쪽 connect 스모크 → main thread queue 첫 도입. **Phase 04 파일(`04-framing-and-pingpong.md`)은 outdated** — 진입 시점에 "Listener wire-up + connect 스모크" 기준으로 재작성 필요. 첫 framing 코드는 Phase 05로 이동. Hook 보강은 여전히 사례 기반으로 미룸.

---

## 보류 중

### Phase 03 후 판단 (사례 기반)
코드가 더 들어온 뒤 *진짜 필요한 가드*가 어떤 것인지 보고 결정:

- **Hook 보강** (가드 강제):
  - `tdd-guard.sh` (공식·직렬화·상태머신 영역, 테스트 부재 시 차단)
  - `tick-blocking-guard.sh` (`02_Server/GameServer/Loop/`에 `Task.Delay`/`Sleep`/`await Db` 차단)
  - `check-server-authority.sh` 강화 (03_Client/에 데미지/HP/XP 키워드 차단)
  - `HOOK_MODE=warn|block` 토글 — Phase 진행 단계별 적용
- **TDD 강제 영역 결정**: 헌법 6번째 원칙으로 박을지, ADR로 박을지. "엄격 vs 미루기" 갈등 인지 중.

### 학습 마라톤 시작 전 (~6월 말)
- `00_Document/TEAM.md` (미팅 결과 박제)
- `.claude/settings.json` 권한 분리 (영역별 쓰기)
- `00_Document/onboarding/` (인규/정우/유현 용, Git/터미널 백지 가정) - 팀장(본인) : 유영호
- `mentor` 서브에이전트
- MES 별도 레포 헌법 골격 (정우 합류 직전)
- 정우 Anthropic 학생 할인 알아보기

---

## 다음 Phase = Phase 04 (서버 Listener wire-up + 첫 connect 스모크)

**범위**: 서버에서 처음으로 포트를 열고, 클라가 connect까지 가서 양쪽 로그가 뜨는지 확인.

**핵심 작업**:
- 서버 `02_Server/GameServer/Program.cs`: `Listener` 인스턴스화 + 포트 7777 listen (현재는 `02_Server/Network/`에 코드만 있고 main에서 안 부름)
- Unity 측 `MonoBehaviour` 1개로 `ClientNet`의 `Connector` 호출 → 서버에 connect → 양쪽 콘솔 로그 확인
- **Unity main thread marshalling 첫 도입** — `Update()`에서 main thread queue drain 패턴 (Phase 03에서 자리만 박아둔 그것)
- 첫 framing 코드(보내고 받기)는 **Phase 05**로 이동 — 이번엔 raw connect 한 번 시연만

**위험 / 함정**:
- Phase 04 파일(`04-framing-and-pingpong.md`)이 **outdated** — 진입 첫 단계로 재작성
- Unity main thread queue 첫 시연이라 디버깅 폭발 가능성. queue 패턴을 너무 정교하게 짜지 말고 *최소 동작*으로 박을 것
- `Listener`는 서버측 `02_Server/Network/Dawnholder.Server.Network.csproj`에 이미 존재 — `GameServer.csproj`가 이걸 참조하는지 확인 필요

**시작 흐름**:
1. 사용자 "Phase 04 시작하자"
2. Claude는 헌법 + CONTEXT + ADR-001/002/010/012 + Phase 03 -DONE.md + Phase 04 파일(재작성 필요) 통독
3. Phase 04 파일 재작성 → Listener wire-up + connect 스모크 기준
4. 코드 작업 → 빌드 + 양쪽 connect 로그 확인 → 5단계 보고 → `-DONE.md` 박제 + commit → 학습 일지 권유

---

## 핵심 결정 요약 (ADR 박혀있음, 빠른 참조)

### 기술 스택
- Unity 6.4 LTS + .NET 10 LTS 권위 서버 (ADR-001 v3 — Unity AI MCP Server 시너지)
- Raw TCP + length-prefixed binary + **자체 PDL** (ADR-002)
- 모노레포 (단, MES는 별도 레포 — ADR-011)
- 20 TPS 서버 틱 (ADR-004)
- PostgreSQL + EF Core (ADR-005)
- `98_Shared/` = .NET Standard 2.1 + DLL + Embedded PDB (ADR-010)
- ServerDev 4월 코드 부분 채택 — **시나리오 B** (ADR-011)

### 게임/스코프
- 두 장르 결합 MVP (RPG + 길드 타이쿤) — ADR-006
- 거점 시설 = 구매/기능 모델만 — ADR-007
- 단일 서버 프로세스 — ADR-008
- 게임 회사 백엔드 포트폴리오 — ADR-009
- 6월 캡스톤 1 = 옵션 C(2인 movement) Stretch / 옵션 B(1인) Fallback

### Harness 작동 원칙
- 학부생 멘토링 + 5단계 보고 + `-DONE.md` 박제 + 학습 일지 권유 (헌법)
- 자동 실행 안 함 — Phase 끝 → 보고 → 사용자 확인 → 다음 Phase 수동
- **박제 분업**: `-DONE.md` = AI 작성(사실/결정/증상). `learning-journal/` = 본인 작성(회고/면접 답변). Phase 폴더에 짝꿍으로.
- 학습 일지: 본인이 쓰고 AI는 인터뷰만 (가짜 학습 방지). `-DONE.md`를 사실 베이스로 활용.

---

## 자산 위치 (빠른 참조)

폴더는 탐색기 정렬 고정용 숫자 prefix를 갖습니다.

```
00_Document/PRD.md, ARCHITECTURE.md, ADR.md     ← 결정·구조
00_Document/commands-index.md                    ← 14개 슬래시 커맨드 카탈로그
00_Document/learning-journal/M{N}-{slug}/        ← Phase 학습 일지
  └── concepts/                                  ← 개념 일지
01_Phases/M{N}-{slug}/                           ← 작업 단위 ({NN}-*.md 정의 + {NN}-*-DONE.md 박제 페어)
02_Server/, 03_Client/, 98_Shared/, 99_Tools/    ← 게임 코드
.claude/agents/                                  ← 6개 서브에이전트
.claude/commands/                                ← 14개 슬래시 커맨드
.claude/hooks/                                   ← 2개 (코드 더 들어온 뒤 사례 기반 보강 예정)
Dawnholder.slnx                                  ← .NET 솔루션 (02_Server + 98_Shared)
global.json                                      ← .NET SDK 핀 (10.0.203)
노션 "Dawnholder 협업 히스토리" DB               ← 세션 STAR 박제
```

---

## 미해결 질문 (남은 ADR 후보)

- **ADR 후보 (시급)**: Unity 클라 socket 전략 — 갈래 X(ServerCore .NET Std 2.1 마이그 + DLL 공유) vs 갈래 Y(자체 작성, 현업 표준). Phase 03 시작 시 결정.
- **ADR 후보**: 인증 방식 (단순 닉네임 → JWT? 세션?)
- **ADR 후보**: 캐릭터 데이터 스키마 (정규화 vs JSONB)
- **ADR 후보**: 채팅 시스템 (TCP 전송 vs 별도 채널) — MVP 후
- **ADR 후보**: 로그 저장 (로컬 파일 vs 외부 sink)
- **ADR 후보**: 헤드리스 봇 자동화 방식
- 캡스톤 1 발표 정확한 날짜 (6월 중순 가정)

---

## 다음 Claude를 위한 마지막 안내

1. **이 문서 + 헌법 통독 후 짧게 인지 확인**: "CONTEXT 잘 읽었어요. {현재 멈춤 지점} 이어서 가는 거 맞죠?"
2. **5단계 보고는 코드 작업 후에만** (대화/의논엔 안 씀).
3. **사용자가 던지는 짧은 메시지**엔 짧게 공감 + 다음 액션 가볍게.
4. **새 정보**(미팅 결과, Unity 업데이트 등)가 들어오면 즉시 재정렬.
5. **이 문서는 살아있는 응축본**. 큰 변화 시 갱신하되 누적 X — 정책은 맨 위 참조.

---

## 갱신 이력

> 이력은 [`CONTEXT_History.md`](CONTEXT_History.md) 참조 (헌법: 문서 세분화 정책 — 누적 섹션 외부화).
>
> 새 갱신 발생 시 본 파일이 아니라 `CONTEXT_History.md`에 한 줄씩 추가.
