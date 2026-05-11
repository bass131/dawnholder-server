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

## ⏸️ 현재 멈춤 지점 (2026-05-11)

**Phase 07 완료 + 회귀 테스트 보강 — M1 Foundation 정비 마지막 단계 끝. 다음 세션에서 Phase 08(PacketManager 자동 dispatch) vs M2 진입 결정 필요**.

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
- ✅ **Phase 07 완료 (2026-05-10, commit `ec2cfe5`)**: PDL 정합 + Phase 05 임시 코드 교체 + Unity 시연 재현. PacketFormat.cs 템플릿(Write byte[] + BinaryPrimitives.*LittleEndian) + Program.cs 출력 폴더 분리(`98_Shared/Protocol/Generated/`로 패킷 통합) + `--no-manager`/`--no-wait` 옵션. Phase 05 임시 PingPacket/PongPacket 삭제. **사용자 통찰로 ADR-012 진화 (책임 단위 분리/통합 정제) + AI sliding 패턴 메모리화**. 책임 단위 문서화 6군데 박제 (ADR-012 보강 + 98_Shared CLAUDE.md + SendBuffer 양쪽 주석 + PacketFormat 헤더 + Phase 07 -DONE + 학습 일지 후보 키워드).
- ✅ **Phase 07 회귀 안전망 보강 (2026-05-11, commit `2b1cc4d`)**: C_Ping/S_Pong 라운드트립 회귀 테스트 추가 (Phase 07에서 누락됐던 회수 commit). M1 Foundation 영역 테스트 커버리지 = Ping/Pong 한 군데뿐 → Phase 08 진입 시 또는 M2 코드 늘어나기 전 보강 후보.
- ✅ **문서 협업 시스템 정합 + Notion e2e 검증 (2026-05-11, commit `6333022..64a1afa`, 8 commit)**: ① 일회성 HTML 3건 정리. ② Notion 협업 분업 원칙(Claude=사실 박제 / Codex=Notion 재편집 / 본인=회고)을 CONTEXT 응축 위험에서 보호하기 위해 `.claude/templates/done-md-template.md`로 영속 이주. ③ Claude→Codex 핸드오프 절차 박음(트리거/입력 셋/책임 분담/Codex CLI cmd 형식 포함 — `codex exec` create는 `-s workspace-write`, update는 `--dangerously-bypass-approvals-and-sandbox` 필요, stdin은 `< /dev/null`로 차단). ④ Notion 출력 형식 모순 해소: STAR=출력 표준 / 8단=사고 체크리스트(매핑 표). ⑤ `-DONE.md` 템플릿에 TL;DR 섹션 박음 + Phase 07 -DONE에 소급 적용. ⑥ `/log-session` 명세는 deprecated 헤더 + Codex 환경 명세로 reposition. **e2e 검증**: 오늘 세션 자체를 노션에 1 신규 페이지 박음 + 기존 9 페이지 중 7개에 34개 용어 풀이 소급 보강 (본문 무변경 검증 OK).
- ✅ **Post-flight 게이트(② ) 도입 (2026-05-11)**: `jha0313/harness_framework` 비교 후 자동 실행은 비채택(학습 호흡 보존), 대신 `-DONE.md` 박제 시 형식 검증을 훅으로 강제. 새 훅 `validate-phase-gate.sh` + 템플릿 frontmatter(`summary` 1줄 / `phase` / `status`) + `## AC 검증 결과` 섹션 필수 + 5단계 보고 5개 항목 라벨 검사. 누락 시 `exit 2`로 차단. 헌법에 Post-flight 게이트 절 명문화. 게이트 ①(Pre-flight, Phase 시작 의식)·③(Blocked 명시화)는 게이트 ② 실전 1회 검증 후 결정. **하네스 비대화 우려 표명** — 향후는 *추가*보다 *정리* (슬래시 14개/에이전트 6개/문서 시스템 실사용 빈도 기반 가지치기 후보).
- ⏳ Phase 02·03·04·05·06·07 학습 일지는 본인 페이스에 따라 추후

**다음 작업 후보**:
- **Phase 08 (옵션)**: PacketManager + PacketHandler 자동 dispatch 도입. 새 패킷 추가 시 *PDL.xml + 핸들러 메서드*만 작성 → 자동 등록. `/new-packet` 슬래시 커맨드 활용 가능. ~1.5h.
- **M2 First Connection 진입**: 캐릭터 첫 이동(input → 패킷 → 서버 검증 → snapshot). 본격 게임 로직. M1 정비 끝났으니 진입 가능.
- **PRD.md 응축** (229줄, 220 초과) — Phase 06 후속, 다음 세션 진입 전 처리 검토.
- Hook 보강은 여전히 사례 기반으로 미룸.

---

## 보류 중

### 사례 기반 가드 결정 (보류 중)
코드가 더 들어온 뒤 *진짜 필요한 가드*가 어떤 것인지 보고 결정:

- **Hook 보강** (가드 강제):
  - ✅ `validate-phase-gate.sh` (2026-05-11 박힘) — `-DONE.md` Post-flight 게이트
  - `tdd-guard.sh` (공식·직렬화·상태머신 영역, 테스트 부재 시 차단)
  - `tick-blocking-guard.sh` (`02_Server/GameServer/Loop/`에 `Task.Delay`/`Sleep`/`await Db` 차단)
  - `check-server-authority.sh` 강화 (03_Client/에 데미지/HP/XP 키워드 차단)
  - `HOOK_MODE=warn|block` 토글 — Phase 진행 단계별 적용
- **게이트 ①·③ (보류)**: 게이트 ②(Post-flight) 실전 1회 검증 후 ① Pre-flight(Phase 시작 의식) → ③ Blocked 명시화 순으로 도입 검토.
- **TDD 강제 영역 결정**: 헌법 6번째 원칙으로 박을지, ADR로 박을지. "엄격 vs 미루기" 갈등 인지 중.
- **하네스 정리 점검** (비대화 우려 후속): 슬래시 14개/서브에이전트 6개 중 실사용 빈도 낮은 것 가지치기. `CONTEXT.md` vs `CONTEXT_History.md` vs `-DONE.md` 역할 중복 점검.

### 학습 마라톤 시작 전 (~6월 말)
- `00_Document/TEAM.md` (미팅 결과 박제)
- `.claude/settings.json` 권한 분리 (영역별 쓰기)
- `00_Document/onboarding/` (인규/정우/유현 용, Git/터미널 백지 가정) - 팀장(본인) : 유영호
- `mentor` 서브에이전트
- MES 별도 레포 헌법 골격 (정우 합류 직전)
- 정우 Anthropic 학생 할인 알아보기

---

## 다음 세션 결정 사항 (Phase 08 vs M2 진입)

**M1 Foundation 정비 끝 → 갈래 선택 필요**. 다음 세션 시작 시 사용자 결정 후 진행.

### 옵션 A: Phase 08 — PacketManager + PacketHandler 자동 dispatch
- **범위**: 새 패킷 추가 시 `PDL.xml + 핸들러 메서드`만 작성하면 자동 등록되는 dispatch 레이어 도입.
- **소요**: ~1.5h.
- **사유**: M2의 캐릭터 이동 패킷부터는 양이 빠르게 늘어남 → 미리 깔아두면 M2 진입 후 가속.
- **위험**: 아직 패킷이 Ping/Pong 둘뿐이라 추상화의 *진짜 형태*가 안 보임 (premature abstraction 위험). `/new-packet` 슬래시 커맨드도 같이 정합 필요.

### 옵션 B: M2 First Connection 진입
- **범위**: 캐릭터 첫 이동 (Unity input → 패킷 → 서버 검증 → snapshot → 클라 reconcile). 본격 게임 로직 시작점.
- **사유**: M1 정비가 충분히 끝났으니 (framing/PDL/책임 단위/회귀 테스트) 더 미루지 말고 게임 본질로.
- **위험**: 패킷 수 늘어날 때 dispatch 수동 wire-up 부담 → Phase 08을 *나중에* 끼울 수밖에 없음. 그때 기존 패킷 retrofit 필요.

### 추천 (사용자 결정 전, 참고용)
- **B (M2 진입) 추천**. 이유:
  - Phase 08은 패킷 종류가 5~10개쯤 되면 *진짜 형태*가 보임. 지금은 추측 기반.
  - M2가 게임 본질(권위/예측/reconcile)이라 캡스톤 1 시점 데모 가치도 큼.
  - 단점은 명확: M2 중간에 dispatch 추상화 갈증 생김. 그때 Phase 08을 사이에 끼우는 흐름.

### 그 외 사이드 작업 (다음 세션 진입 *전*에 처리 검토)
- **PRD.md 응축** — 229줄(220 초과). Phase 06부터 미뤄진 항목. 5분 작업.
- **Phase 02·03·04·05·06·07 학습 일지** — 본인 페이스. `/journal-phase` 또는 `/journal-concept <키워드>`. 미뤄도 되지만 디테일 잊기 전에 권장.

### 시작 흐름 (다음 세션)
1. 사용자가 "이어서 가자" / "Phase 08 가자" / "M2 진입" 등으로 신호
2. Claude는 헌법 + CONTEXT + Phase 07 -DONE + ADR-012 통독
3. (옵션 미정 시) 짧게 1~2문항으로 결정 묻기
4. 결정된 방향에 따라 Phase 파일 신설 또는 기존 활용 → 코드 작업 → 5단계 보고 → `-DONE.md` 박제 + commit → 학습 일지 권유

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

### Notion 협업 히스토리 문서 분업
- **정책 위치**: [`.claude/templates/done-md-template.md`](.claude/templates/done-md-template.md) "Notion 협업 분업 원칙" 섹션 (영속). CONTEXT 응축 시 유실 방지를 위해 템플릿으로 이주됨 (2026-05-11).
- **요지**: Claude=사실 박제 / Codex=Notion 재편집·면접 답변 / 본인=회고·학습 일지. 자세한 8단 구조·용어 처리·원칙은 위 링크 참조.

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
.claude/hooks/                                   ← 3개 (validate-shared / check-authority / validate-phase-gate)
Dawnholder.slnx                                  ← .NET 솔루션 (02_Server + 98_Shared)
global.json                                      ← .NET SDK 핀 (10.0.203)
노션 "Dawnholder 협업 히스토리" DB               ← 세션 STAR 박제
```

---

## 미해결 질문 (남은 ADR 후보)

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
