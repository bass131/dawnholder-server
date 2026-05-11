# ADR — Architecture Decision Records

> **이 문서의 역할**: "왜 이렇게 만들었는지" 결정의 기록. 6개월 뒤에
> 본인이 봐도, AI가 봐도 "왜 이 선택을 했지?"를 알 수 있게.
>
> **포맷**: 결정마다 3줄. **결정 / 이유 / 트레이드오프**. 길게 쓰지 말 것.
>
> **언제 쓰나**: 되돌리기 어려운 결정을 할 때마다. 작은 코드 결정은 ADR
> 안 씀. "이걸 바꾸려면 며칠 걸리겠다" 싶은 것만.

---

## ADR 템플릿

```markdown
### ADR-NNN: [결정 제목]
**날짜**: YYYY-MM-DD
**상태**: 채택됨 | 폐기됨 | 대체됨(ADR-NNN으로)
**결정**: [무엇을 선택했는지 한 줄]
**이유**: [왜 선택했는지 한두 줄]
**트레이드오프**: [무엇을 포기했는지 한두 줄]
```

---

## 현재까지 결정한 것들

### ADR-001: Unity 6.4 LTS + .NET 10 LTS + .NET Standard 2.1 멀티타겟
**날짜**: (Harness 셋업일) — **2026-05-06 .NET 버전 갱신**, **2026-05-09 Unity 버전 갱신**
**상태**: 채택됨 (대체: v1 ".NET 8 + Unity 2022 LTS", v2 ".NET 10 + Unity 2022 LTS")
**결정**: Unity 6.4 LTS 클라이언트 + .NET 10 LTS 권위 서버. `98_Shared/`는 .NET Standard 2.1로 빌드해 Unity가 인식 가능하게.
**이유**: C# 단일 언어 통일. .NET 10 LTS는 2028년까지 지원이라 11월 본 마감 + 시연 후 시점도 커버 (.NET 8은 2026-11-10 만료, .NET 9는 2026-05-12 만료로 부적합). .NET Standard 2.1 = Unity의 Mono/IL2CPP가 인식하는 공통 API 사양 → DLL 공유 가능. Unity 6.4 LTS 선택 이유: (a) **Unity AI MCP Server 활용 가능** — Claude Code가 Unity 에디터를 직접 조회/조작, 본 프로젝트의 Claude 중심 워크플로우와 직접 시너지. (b) Unity 6의 새 기능(GPU Resident Drawer, 향상된 2D 렌더링). (c) LTS 라이프사이클 더 김 — 2027~2028년까지.
**트레이드오프**: 웹/모바일/콘솔은 추가 작업. 기존 ServerDev 코드(.NET 9)를 클론할 때 csproj TargetFramework 마이그레이션 필요 (대부분 한 줄). .NET 10이 신규 LTS라 일부 NuGet 라이브러리는 호환성 케이스별 확인. Unity 6는 2022 대비 일부 deprecated API/내부 매개변수 변경 가능 — 학습용 ServerDev 코드는 서버/네트워크 중심이라 영향 적지만, Unity 코드 작성 시 옛 튜토리얼(2022 LTS 기준)을 그대로 옮기면 안 됨.

### ADR-002: Raw TCP + 자체 PDL + 코드 생성기
**날짜**: (Harness 셋업일) — **2026-05-06 직렬화 방식 갱신**, **2026-05-11 폴더 경로 정합**
**상태**: 채택됨 (대체: ADR-002 v1 "MessagePack")
**결정**: Mirror/FishNet 같은 HLAPI 대신 raw TCP + length-prefixed binary 사용. 직렬화는 MessagePack이 아니라 **자체 PDL(Packet Definition Language) XML + C# 코드 생성기**로. PDL.xml 단일 소스 → 양쪽(client/server)에 동일 패킷 클래스 자동 생성.
**이유**: 학습 목적이 네트워킹 깊이 이해. PDL이 단일 소스 역할을 해서 헌법 #4 ("복사-붙여넣기 금지") 강제. MessagePack 대비 wire format이 더 가볍고 메타데이터 0. 면접 임팩트: "Rookiss 강의 패턴 응용해 PDL 생성기 직접 구현". 본인이 4월에 이미 작성한 PDL 시스템(`99_Tools/PacketGenerator/`로 이주 완료)이 있음.
**트레이드오프**: 직접 짠 코드라 버그 가능 (생성기에서 발견된 하드코딩 버그 2건 ✅ Phase 06에서 정정 완료, commit `03994b0`). MessagePack의 schema evolution 같은 자동 호환성 없음 — packet ID 수동 관리 + Protocol.Version bump. JSON처럼 디버깅 쉽지 않음 (바이너리).

### ADR-003: 모노레포
**날짜**: (Harness 셋업일)
**상태**: 채택됨 (단, MES는 별도 레포 — ADR-011 참고)
**결정**: client/, server/, shared/를 한 git 레포에 둠.
**이유**: shared/Protocol을 패키지로 분리하면 1인 개발에서 오버헤드만 큼.
한 PR로 양쪽 변경이 일관됨. AI가 컨텍스트 잡기 쉬움.
**트레이드오프**: 레포가 커지면 clone 시간 증가. CI에서 client/server를
선택적으로 빌드하는 설정이 필요해질 수 있음.

### ADR-004: 20 TPS 서버 틱
**날짜**: (Harness 셋업일)
**상태**: 채택됨
**결정**: 서버 시뮬레이션 틱을 50ms 간격(20 TPS)으로 고정.
**이유**: 사이드스크롤 RPG는 격투 게임 수준의 정밀도 불필요. 20 TPS면
체감 반응성 충분하면서 서버 부하 적당. 클라 60fps는 보간으로 부드럽게.
**트레이드오프**: 정밀 격투 액션에는 부족. 추후 특정 시스템(예: 패링)에서
틱 레이트가 부족하면 서버 측 부분 sub-tick 처리 필요해질 수 있음.

### ADR-005: PostgreSQL + EF Core 10
**날짜**: (Harness 셋업일) — **2026-05-11 EF Core 8 → 10 정합**
**상태**: 채택됨
**결정**: 영속화 DB로 PostgreSQL, ORM은 Entity Framework Core 10.
**이유**: 학습 친화적(SQL 표준 잘 따름), 도커 띄우기 쉬움, EF Core는
.NET 표준 ORM. 마이그레이션 도구 잘 되어있음. EF Core 10은 .NET 10 LTS와
정합된 같은 세대 (ADR-001).
**트레이드오프**: NoSQL(예: Redis) 대비 복잡한 인덱스 설계 필요.
EF Core는 raw query 대비 추상화 비용이 약간 있음. 캐싱 레이어는 추후 추가.

---

### ADR-006: 두 장르 결합을 MVP 핵심으로 유지
**날짜**: (PRD 1차 작성일)
**상태**: 채택됨
**결정**: Dawnholder는 캐주얼 액션 RPG + 길드 거점 타이쿤을 **MVP 단계부터** 결합한
형태로 만든다. 둘 중 하나를 빼고 시작하지 않는다.
**이유**: 두 장르가 섞이는 게 프로젝트 정체성의 핵심. 분리하면 기존 RPG/타이쿤
대비 차별점이 사라짐. 게임 회사 포트폴리오 관점에서도 "두 도메인을 통합한 백엔드"
가 더 강한 어필.
**트레이드오프**: MVP scope가 단순 액션 RPG 대비 1.5~2배. 마일스톤 수가 8개로 늘어남.
완성까지 시간 더 걸림. 위험 1(scope 폭주)이 상시 존재.

### ADR-007: 거점 시설 = "구매 → 기능 제공" 모델
**날짜**: (PRD 1차 작성일)
**상태**: 채택됨
**결정**: 길드 거점 시설은 "길드 자금으로 구매 → 길드원이 사용 시 기능 제공" 모델만
구현한다. 자원 생산, NPC 고용, 자원 변환 같은 풀 타이쿤 요소는 제외.
**이유**: 풀 타이쿤은 게임 디자인 시간이 폭증. 학습 목표(백엔드)에서 멀어짐.
"구매 모델"만으로도 동시성, 트랜잭션, 영속화의 핵심 학습 포인트는 다 커버됨.
**트레이드오프**: 타이쿤의 깊은 재미는 제한됨. 시설 종류가 적으면 금방 콘텐츠
소진. MVP 후 "Maybe Later"로 자원 흐름 추가 가능성 열어둠.

### ADR-008: 단일 서버 프로세스 (분산/샤딩 없음)
**날짜**: (PRD 1차 작성일)
**상태**: 채택됨
**결정**: MVP는 단일 .NET 서버 프로세스로 100~500명 동접 처리. 맵 서버 분리, 샤딩,
로드밸런서 같은 분산 시스템 요소는 도입하지 않는다.
**이유**: 동접 100~500명은 단일 프로세스로 충분 가능. 분산 시스템은 학습 곡선이
급격히 가팔라지고 "배보다 배꼽이 큰" 상황 발생. 내부 구조(맵을 actor로 분리)는
나중에 분산화하기 좋게 설계해두지만, 실제 분산 인프라는 다음 프로젝트로.
**트레이드오프**: 1000명+ 가면 못 버팀. 한 프로세스 죽으면 전체 다운. 면접에서
"수평 확장은 어떻게?" 질문 시 "다음 프로젝트에서"라고 답해야 함 (정직하게).

### ADR-009: 포트폴리오 타겟 = 게임 회사 백엔드
**날짜**: (PRD 1차 작성일)
**상태**: 채택됨
**결정**: 이 프로젝트의 결과물은 **게임 회사 백엔드 포지션 지원용 포트폴리오**로
최적화한다. 따라서 README는 기술 블로그 형식, 부하 테스트 결과 그래프 필수,
데모 영상 2분 이내 필수.
**이유**: 일반 백엔드 vs 게임 백엔드는 강조점이 다름. 게임 백엔드는 "실시간 동기화,
권위 서버, 동시성, 부하 테스트"가 핵심. 일반 백엔드용으로 최적화하면 게임 회사
어필이 약해짐.
**트레이드오프**: 일반 백엔드 회사 지원 시 README 톤 조정 필요할 수 있음.
게임 회사 외 진로로 갈 경우 일부 어필 포인트 재가공 필요.

---

### ADR-010: Shared 코드 공유 방식 = DLL + Embedded PDB
**날짜**: 2026-05-06 — **2026-05-11 폴더 경로 정합**
**상태**: 채택됨
**결정**: `98_Shared/`는 .NET Standard 2.1 라이브러리로 빌드. 빌드 산출물(`.dll` + `.pdb`)을 `03_Client/Assets/Plugins/`에 자동 복사해서 Unity가 참조. PDB는 `EmbedAllSources=true`로 원본 `.cs` 통째로 임베드 → IDE가 F12 시 원본 코드(주석 포함) 그대로 표시.
**이유**: 헌법 #4 ("동일 어셈블리 참조, 복사-붙여넣기 금지")의 **물리적 강제**. Unity 측에선 임베드된 소스가 ReadOnly로 떠서 수정 자체가 불가능. F12 + step into는 정상 동작 → C++의 헤더+구현 분리 모델보다 풍부 (모든 함수 바디 보임). 비개발자 팀원(유현)이 클라 작업 중 실수로 shared 코드 건드릴 가능성 0%.
**트레이드오프**: shared 수정 시 "빌드 → 복사 → Unity 새로고침" 사이클 1~2초 추가 (`dotnet watch` 자동화 가능). symlink/Unity Local Package 대비 빌드 단계 1개 더. `.dll`/`.pdb`는 빌드 산출물이라 `.gitignore` (커밋 금지).

---

### ADR-011: 기존 ServerDev 코드 부분 채택 (시나리오 B)
**날짜**: 2026-05-06 — **2026-05-11 PacketGenerator 후속 박음**
**상태**: 채택됨
**결정**: 본인이 4월에 학습 목적으로 작성한 `C:\Users\bass1\바탕 화면\ServerDev\Dawnholder_Server`의 코드 일부를 채택. **채택**: ServerCore (Listener/Session/RecvBuffer/SendBuffer/JobQueue), PacketGenerator + PDL.xml, DummyClient. **새로 작성**: Server 게임 로직 (GameRoom/ClientSession 등), Unity 클라이언트 전체 (기존 코드는 3D였고 본 프로젝트는 2D).
**이유**: ServerCore는 4월에 디버깅 끝낸 검증된 SocketAsyncEventArgs 패턴 → 시간 절약. PDL 시스템은 면접 임팩트 큰 자체 구현물. 게임 로직(GameRoom 등)은 헌법 #1(Server Authority) 위반 — 클라가 보낸 좌표 무검증 적용 — 이라 새로 짜야 함. 6월 캡스톤 C 옵션(2인 movement) 6주 안에 가려면 시간 절약 필요.
**트레이드오프**: 본인 코드 빚 일부 안고 시작. 발견된 PacketGenerator 버그(`PacketFormat.cs` 178번 줄 하드코딩 `C_Chat`, `chatLen` 2건) ✅ Phase 06에서 정정 완료 (commit `03994b0`). 기존 코드의 한글 주석/네이밍 컨벤션이 새로 짜는 부분과 미묘하게 안 맞을 수 있음 → 이주 시 정리. **참고**: 기존 `GameRoom.Move()`의 Server Authority 위반은 **학습 일지 1호 후보** — "처음엔 이렇게 짰다 → 헌법 적용해 이렇게 진화" 면접 서사로 활용.

---

### ADR-012: Unity 클라 socket 레이어 = 분리 클라용 라이브러리 (갈래 Y2)
**날짜**: 2026-05-10 · **2026-05-10 보강** (Phase 07: 책임 단위 분리/통합 표 + 카테고리 맥락)
**상태**: 채택됨
**결정**: Unity 클라이언트의 socket 레이어를 **서버측 ServerCore와 별개로** 신작한다. 새 csproj `04_ClientNet/Dawnholder.Client.Net.csproj` (.NET Standard 2.1)로 작성하고, 빌드 산출물을 `03_Client/Assets/Plugins/ClientNet/`에 자동 복사 (ADR-010 패턴 재사용). 갈래 X(서버 ServerCore를 `98_Shared/Net/`로 마이그해 양쪽이 같은 DLL 참조)는 채택하지 않음.
**이유**: ① **현업 표준 (한국 MMO 백엔드 카테고리)** — Rookiss 강의 패턴 = NCSoft/Nexon/Smilegate/Pearl Abyss 실무 패턴 (전용 서버 + 클라 socket layer 분리). ⚠️ Mirror/FishNet/Unity Netcode 같은 *Unity 인디 멀티플레이어* 카테고리는 통합 패턴이지만 *본 프로젝트와 다른 카테고리* (그쪽은 Unity 안에서 클라+서버 모두 호스팅). gRPC도 일반 RPC 영역. ② **socket 자체 학습 가치** — 클라 입장의 connect/recv/send를 한 번 직접 짜는 것이 면접 임팩트. ③ **변경 내성** — 서버측 nullable·인터페이스 변경이 클라 빌드를 즉시 깨지 않음. ④ 마이그 함정 실측(2026-05-09)에서 X도 무리 없음(~1시간, nullable 13개)이 확인됐지만, 위 세 이유로 Y2 우세.
**트레이드오프**: ① 코드 두 벌 — 같은 SocketAsyncEventArgs 패턴을 클라용으로 한 번 더. ~200~300줄 추가. ② 양쪽 socket 버그가 따로 발생할 수 있음 (서버는 잘 도는데 클라만 꺼짐 등). ③ Plugins 복사 파이프라인 한 번 더 셋업. 다만 Phase 01에서 `Shared.dll` 파이프라인 검증되어 패턴 그대로. ④ 추후 "클라/서버 양쪽이 진짜 같은 framing 로직을 써야겠다"가 되면 framing 부분만 `98_Shared/`로 떼어낼 수 있음 (열어둠).

**Phase 07 책임 단위 정제** (2026-05-10): 분리/통합은 *책임마다 따로* 결정. *전부 분리* 또는 *전부 통합*은 단순화. 각 책임의 *환경 의존성*을 기준으로:

| 책임 | 분리 vs 통합 | 위치 | 이유 |
|---|---|---|---|
| socket 라이프사이클 (Connector/Listener/Session) | 분리 | `02_Server/Network/` + `04_ClientNet/` | 환경별 GC + 변경 내성 (현재 코드는 거의 동일 — 미래 환경별 최적화 자유 보존) |
| 버퍼 관리 (SendBuffer/RecvBuffer) | 분리 | 위와 동일 | socket 인프라 부속 |
| **패킷 데이터 정의** (C_Ping/S_Pong) | **통합** | `98_Shared/Protocol/Generated/` | 와이어 포맷은 환경 무관. 양쪽 동기 필수 |
| PacketManager (dispatch table) | 분리 (예정) | 양쪽 (Phase 08+) | 서버=C_*만, 클라=S_*만 — 받는 데이터 다름 |
| 핸들러 함수 (HandlePing 등) | 분리 | GameSession / UnityClientSession | 서버=게임 로직, 클라=Unity API. 환경 진짜 다름 |

핵심 원칙: **환경 차이가 *코드에 박힐 만한* 곳만 분리**. 패킷 정의는 byte[] ↔ struct 변환이라 *환경 무관* → 통합. PDL.xml + 코드 생성기가 양쪽 동기 자동화.

---

### ADR-013: -DONE.md 페어 박제 정책 (AI=사실 / 본인=회고 분업)
**날짜**: 2026-05-10
**상태**: 채택됨
**결정**: Phase 완료 시 `01_Phases/M{N}-{slug}/{NN}-{phase-name}-DONE.md`를 **AI가 작성**해 사실·결정·증상·키워드를 박제한다 (5단계 보고 직후 같은 응답에서). 학습 일지(`00_Document/learning-journal/`)는 본인이 작성. AI는 인터뷰만.
**이유**: 학습 일지가 본인 페이스에 따라 *밀릴 것*을 전제. 그동안 사실/결정 디테일이 잊힘 → 면접 무기 손실. AI가 사실 베이스만 잊히기 전에 박아두면, 본인이 회고 쓸 때 베이스로 활용 가능. 역할 분리로 가짜 학습(AI가 다 써준 일지) 방지.
**트레이드오프**: Phase당 파일 수 2배 (정의 + DONE 페어). AI가 회고 톤으로 흐를 위험 → ADR-015(Post-flight 게이트)로 형식 강제. 본인이 일지 안 쓰면 면접 답변 부분은 누적 안 됨 (의도된 분업).

### ADR-014: 문서 세분화 정책 (220줄 임계 + 헌법 350줄 예외)
**날짜**: 2026-05-10
**상태**: 채택됨
**결정**: 모든 사전형 `.md`는 220줄 임계. 초과 시 (a) 누적 섹션 외부화, (b) 응축 재작성, (c) 단위 작업 문서(Phase/-DONE)는 자르지 않음(잘 쪼개진 단위면 자연스레 220 이하). 분리된 파일도 220 초과 시 카테고리화/사건별로 재귀. **헌법(`CLAUDE.md`)만 350줄 예외**.
**이유**: 220줄 ≈ LLM이 한 호흡에 읽기 적정선. 외부화 패턴(`CONTEXT_History.md`, `ADR_History.md`)으로 *현재 결정/상태*만 본문에 유지. 헌법 350 예외 사유: (a) 자기참조 무한 제안 루프 차단(헌법이 임계의 주체이자 대상), (b) 절대 원칙·세분화 정책·사용자 컨텍스트 같은 본질적으로 큰 정책 모음을 220 강제 시 핵심 룰 손상 위험.
**트레이드오프**: 임계 위반 시 응축 작업 부담. 외부화 시 파일 수 증가 + 분리 후 카테고리 기준 모호 시 추가 단계. 헌법 예외는 350줄 비대 시 다시 응축 발화점 필요.

### ADR-015: Post-flight 게이트 (validate-phase-gate.sh 훅)
**날짜**: 2026-05-11
**상태**: 채택됨 (Pre-flight ①·Blocked ③ 게이트는 ② 실전 1회 검증 후 결정 — 보류)
**결정**: `-DONE.md` Write/Edit 시 형식 검증 훅 `.claude/hooks/validate-phase-gate.sh` 자동 실행. 누락 시 `exit 2`로 차단. 강제 4가지: (1) frontmatter `summary`/`phase`/`status`, (2) 필수 H2 섹션 5개, (3) 5단계 보고 항목 라벨 5개, (4) `## AC 검증 결과` 본문 비어있지 않음. **자동 실행(Phase 자동 진행)은 비채택** — 학습 호흡 보존.
**이유**: `jha0313/harness_framework` 비교 결과, 자동 진행은 학습 깊이 손상 위험이라 비채택. 그러나 박제 자체를 빼먹는 것은 물리적으로 차단해야 면접 무기 누락 방지 → 형식 강제만 훅으로. 사용자 페이스는 보존, AI 빠뜨리기는 차단.
**트레이드오프**: 훅 차단 시 AI 재시도 사이클 발생(시간 소모). 게이트 ①·③ 보류 → 향후 추가 시 하네스 비대화 우려. 형식 통과만 검사 — *내용의 질*은 검증 못 함.

### ADR-016: Notion 협업 3자 분업 (Claude / Codex / 본인)
**날짜**: 2026-05-11
**상태**: 채택됨
**결정**: Notion "Dawnholder 협업 히스토리" DB는 3자 분업으로 운영. **Claude** = 사실 박제 (1차 페이지 생성 + 용어 풀이). **Codex** = Notion 재편집 + 면접 답변 보강 (`codex exec` CLI, create는 `-s workspace-write`, update는 `--dangerously-bypass-approvals-and-sandbox`, stdin은 `< /dev/null`로 차단). **본인** = 회고/학습 일지. 자세한 원칙(8단 구조·STAR 출력·용어 처리·핸드오프 트리거)은 [`.claude/templates/done-md-template.md`](../.claude/templates/done-md-template.md) "Notion 협업 분업 원칙" 섹션 영속화.
**이유**: AI 단일 작성은 사실/회고/면접 답변 톤이 섞여 어느 것도 강하지 않음. 3자 분업으로 각 역할 톤 명확화 + 본인 학습/면접 무기 분리. CONTEXT 응축 시 원칙 유실 위험은 template 영속화로 해소.
**트레이드오프**: 협업 도구 추가(Codex) → 환경 의존성 증가. Codex CLI 옵션이 create/update에 따라 다름(혼동 위험 → 핸드오프 절차에 박힘). 본인이 회고 안 쓰면 3자 중 1자가 비어있음 (의도된 분업).

### ADR-017: 프로젝트 폴더 ASCII 경로 이동 (한글 경로 영구 해결)
**날짜**: 2026-05-11
**상태**: 채택됨
**결정**: 프로젝트 루트를 한글 포함 경로에서 ASCII 전용 경로 `C:\Dev\ClaudeDev`로 이동. 본 결정의 범위는 **한글 경로 호환성 해결만**이며, Burst Enable 시 발생하는 WDAC(Windows Defender Application Control) 미서명 DLL 차단 (error code 4551)은 **별도 사건**으로 본 ADR 범위 밖. 본 프로젝트는 Burst 비활성 상태로 진행 — Burst가 진짜 필요한 복잡도 시스템 도입 시 별도 ADR로 WDAC 정책 정리.
**이유**: Phase 03·04에서 한글 절대경로가 Burst JIT 컴파일러·PacketGenerator `dotnet run` 등 .NET/Unity 도구 체인에서 hang·경로 파싱 실패를 반복 유발. 우회책(publish 산출물 직접 실행, Burst Disable)으로 진행은 했으나 매 Phase 도구 신뢰 비용 누적. ASCII 경로 이동 후 검증(`dotnet build` 0 error / `dotnet test` 25/25 / `dotnet run` PacketGenerator 직접 실행 / Burst Enable 시 hang 없이 즉시 4551 에러로 떨어짐 = 컴파일러 경로 파싱은 해결됨)으로 도구 호환성 근본 회복.
**트레이드오프**: 절대경로 박힌 옛 문서·일지·노션 페이지 참조 깨짐(`-DONE.md` 등에 옛 경로 잔존 가능 — 발견 시 정정). 폴더 이동 자체가 1회성 수작업(PowerShell `Move-Item`) — 다른 머신에 클론할 땐 무관. Burst·WDAC 사건은 미해결로 남아 향후 복잡도 시스템 도입 시 재발 가능.

---

## 채워질 ADR 후보들 (예시)

> 본인이 진행하다가 다음 결정들을 할 때 ADR로 기록하세요. 번호는 채택 순서대로 부여 — 아래는 가이드일 뿐.

- **ADR-018**: 인증 방식 (단순 닉네임 → JWT? 세션?)
- **ADR-019**: 캐릭터 데이터 스키마 (정규화 vs JSONB)
- **ADR-020**: 채팅 시스템 (TCP로 전송 vs 별도 채널)
- **ADR-021**: 로그 저장 (로컬 파일 vs 외부 sink)
- **ADR-022**: 헤드리스 봇의 자동화 방식
- **ADR 후보**: WDAC 미서명 DLL 차단 정책 정리 (Burst 등 도구 활성화 필요 시점에)
- ...

---

## 변경 이력

> 이력은 [`ADR_History.md`](ADR_History.md) 참조 (헌법: 문서 세분화 정책 — 누적 섹션 외부화).
>
> 새 ADR 추가/갱신 발생 시 본 파일이 아니라 `ADR_History.md`에 한 줄씩 추가.
