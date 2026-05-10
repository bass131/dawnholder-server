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

**Phase 06 완료 — Phase 07(생성 코드 양쪽 정합 + Phase 05 코드 교체) 진입 직전**.

- ✅ Phase 01 완료 (commit `2411ae0`): 솔루션 부트스트랩 + DLL 빌드 파이프라인. 학습 일지 박제.
- ✅ 폴더 prefix 정렬 + 경로 정합성 일괄 갱신 (commit `071680e`)
- ✅ Phase 02 완료 (commit `c2ea772`): ServerCore 7파일을 `02_Server/Network/`에 .NET 10 유지로 정착. nullable 21곳 청소. 빌드 경고 0 / 오류 0, 테스트 3 통과. 노션 세션 로그 박제.
- ✅ **박제 정책 결정 (2026-05-10)**: 학습 일지가 밀릴 것을 전제로 **`-DONE.md` 페어 도입**. AI가 작성하는 사실·결정·증상 박제 (5단계 보고 직후 자동). 학습 일지(본인 회고)와 역할 분리.
- ✅ **ADR-012 박힘 (2026-05-10)**: Phase 03 갈래 = Y2(분리 + 별도 클라 라이브러리). 사유: 현업 표준, socket 자체 학습 가치, 서버 변경 격리.
- ✅ **Phase 03 완료 (2026-05-10, commit `fb7a06d` + `c3f2246`)**: `04_ClientNet/` 신규 .NET Std 2.1 라이브러리 (Connector / ClientSession+PacketSession / RecvBuffer / SendBuffer / SmokeProbe). 5개 프로젝트 빌드 경고 0 / 오류 0. **Unity F12 → 원본 .cs + 한국어 주석 ReadOnly 표시 검증 통과** (ADR-010 패턴 두 번째 인스턴스). `-DONE.md` 박제 + commit.
- ✅ **Harness 정합성 일제 정비 (2026-05-10)**: 220줄 세분화 정책 + CONTEXT_History 외부화 + 헌법 응축(348→264) + 헌법 350줄 예외 + 슬래시 커맨드/서브에이전트/hooks 18개 폴더 prefix 일괄 정정 + Agent 점검으로 잔존 outdated 2건 정정. 다음 세션부터 헌법·CONTEXT·harness 모두 정합 상태로 작동.
- ✅ **Phase 04 완료 (2026-05-10, commit `a798479`)**: 서버 `Program.cs` Listener wire-up(0.0.0.0:7777) + `GameSession.cs` 신작 + Unity 측 3파일(`MainThreadDispatcher` / `UnityClientSession` / `NetworkBootstrap`) 첫 시연. **양쪽 OnConnected 로그 동시 확인 + UnityException 없음** → main thread queue 패턴 작동 입증. SampleScene에 Network GameObject 박힘(다음 세션 시연 재현 가능).
- ✅ **전체 outdated 점검 + 정합 (2026-05-10, commit `d5b8677` + `6494896`)**: Explore agent로 ARCHITECTURE.md가 2026-05-06/09/10 세 차례 변경에 누락됐던 것 일괄 정합. 디렉토리 구조 통째 재작성 + MessagePack 의존성 제거 + EF Core 8→10 + Y2 socket 분리 모델 명시. Phase 05 파일 신설.
- ✅ **Phase 05 완료 (2026-05-10, commit `5174573`)**: framing(`[size(2)][packetId(2)][payload]`) + 첫 Ping/Pong 양방향 시연. `98_Shared/Protocol/` 신설(PacketId enum + PingPacket + PongPacket, BitConverter 임시). 서버/클라 PacketSession 상속 교체. Unity 1초마다 Ping → 서버 Pong → 클라 RTT 출력. **★ M1 Foundation 마일스톤 완료** — 영상 시연 가능한 첫 데모.
- ✅ **Phase 06 완료 (2026-05-10, commit `03994b0`)**: PacketGenerator 4월 ServerDev → `99_Tools/PacketGenerator/` 이주 + 하드코딩 버그 2개 정정(`C_Chat`/`chatLen`) + Program.cs nullable 정합 + PDL.xml=`C_Ping`/`S_Pong`. 생성기 실행 → 3개 .cs(GenPackets+ClientPacketManager+ServerPacketManager) 정상 출력 + eyeball 검증 통과. 6개 프로젝트 빌드 경고 0/오류 0.
- ⏳ Phase 02·03·04·05·06 학습 일지는 본인 페이스에 따라 추후

**다음 작업 = Phase 07: 생성 코드 양쪽 정합 + Phase 05 코드 교체 + 시연**

진입 전 결정 필요 (Y2 정합 갈래):
- ① **Shared에 SendBufferHelper 두기** (코드 중복 0, 생성기 단순)
- ② **생성기가 양쪽에 별도 GenPackets.cs 출력** (Y2 분리 갈래 일관, 코드 두 벌)
- ③ 다른 방식

작업:
- 위 갈래 결정 → PacketFormat.cs 템플릿 수정 (`using` + `SendBufferHelper` 분담)
- BinaryPrimitives.*LittleEndian 정합 (현재 BitConverter 호스트 endian)
- Phase 05 `PingPacket.cs`/`PongPacket.cs` 삭제 → 생성 `C_Ping`/`S_Pong` 사용
- 양쪽 GameSession/UnityClientSession dispatch 정합
- Unity 시연 (Phase 05와 동일 RTT 로그, 단 *생성 코드*로)

**기타 후보** (Phase 07 후):
- PRD.md 응축 (229줄→220 안)
- M2 First Connection 진입 (캐릭터 첫 이동, 본격 게임 로직)
- Hook 보강은 여전히 사례 기반으로 미룸.

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

## 다음 Phase = Phase 05 (Length-prefixed framing + 첫 Ping/Pong)

**범위**: TCP byte stream을 패킷 단위로 자르는 framing 도입 + 첫 양방향 패킷 왕복 시연.

**핵심 작업**:
- 와이어 포맷: `[size(2)][packetId(2)][payload...]` — `04_ClientNet/PacketSession.cs`(이미 작성됨)와 서버측 `02_Server/Network/Session.cs`의 PacketSession 패턴 사용
- 첫 패킷 정의: `Ping` (클라→서버, 클라 timestamp) / `Pong` (서버→클라, 클라 timestamp echo + 서버 timestamp)
- Unity Update()에서 1초마다 `Connector` 보유 세션을 통해 Ping 송신 → 서버 GameSession이 받아서 Pong 응답 → 클라 RTT 계산 출력
- 서버측 GameSession을 PacketSession 상속으로 교체 (현재는 raw Session 상속)

**판단 필요**:
- **직렬화 방식**: ① 자체 PDL(ADR-002 채택, 4월 코드 재활용 + 코드 생성기 — 인프라 셋업 비용 ~1~2시간 추가) ② 단순 `BitConverter.WriteBytes` 직접(Ping/Pong은 필드 2개라 PDL 없이도 OK, Phase 06+에 PDL 도입). Phase 05 진입 시 사용자 결정.

**위험 / 함정**:
- Phase 05 파일은 **없음** — `/plan` 또는 직접 `01_Phases/M1-foundation/05-*.md` 신설부터
- Unity Update에서 1초 간격은 `Time.time` 누적 또는 `InvokeRepeating` — 둘 다 OK
- 서버측 GameSession을 PacketSession 상속으로 *교체* 시 OnRecv 시그니처 변경 — 컴파일 깨질 수 있음. 한 번에 처리.

**시작 흐름**:
1. 사용자 "Phase 05 시작하자"
2. Claude는 헌법 + CONTEXT + Phase 04 -DONE + ADR-002 통독
3. 직렬화 방식 결정 (PDL vs BitConverter)
4. Phase 05 파일 신설 → 작업 단계 박음
5. 코드 작업 → 빌드 + 시연(클라 RTT 로그 + 서버 Ping 받음 로그) → 5단계 보고 → `-DONE.md` 박제 + commit → 학습 일지 권유

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
