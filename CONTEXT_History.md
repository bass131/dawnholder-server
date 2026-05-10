# CONTEXT_History — 갱신 이력

> `CONTEXT.md`의 누적 섹션 외부화 (헌법: 문서 세분화 정책, Level 1).
>
> 본 파일이 220줄을 넘으면 주제별 카테고리(`CONTEXT_History/{topic}.md`)로
> 분리. 후보 카테고리: phases / policies / structure / meetings.

---

## 갱신 이력

| 날짜 | 변경 |
|------|------|
| 2026-05-06 | 미팅 결과 + ServerDev 시나리오 B 결정 + ADR-001/002/010/011 박제 |
| 2026-05-09 (5번째 세션) | Phase 01 완료 + ADR-001 v3 (Unity 6.4 LTS) + 응축 정책 박제 |
| 2026-05-09 (응축 재작성) | 200줄 내외로 처음부터 재작성. 옛 디테일은 git history + 노션 + learning-journal로 이관. START_HERE.md / harness-structure.html 삭제. |
| 2026-05-09 (폴더 prefix) | 모든 최상위 폴더에 정렬용 숫자 prefix 부여 (`00_Document/`, `01_Phases/`, `02_Server/`, `03_Client/`, `98_Shared/`, `99_Tools/`). `Dawnholder.slnx` + `global.json` 신설. .gitignore / csproj / 헌법 / 서브-CLAUDE.md 경로 정합성 일괄 갱신. |
| 2026-05-09 (Phase 02 재정의) | ServerCore 마이그 함정 실측 (nullable 13개뿐). 그 결과 *마이그 가능*하지만 *현업 표준 + 학습 가치*는 분리 모델 우세 판단. **Phase 02 = 서버측 ServerCore 정착(.NET 10 그대로)으로 축소. 클라측 socket 전략은 Phase 03에서 X/Y 결정.** Hook 보강도 Phase 02 후 사례 기반으로 미룸. |
| 2026-05-09 (Phase 02 완료) | ServerCore 7파일을 `02_Server/Network/`에 정착(.NET 10 유지, namespace `Dawnholder.Server.Network`). nullable 21곳 청소(실측 13 + .NET 10 추가 8). JobQueueTests 2개 추가. 빌드 경고 0 / 오류 0, 테스트 3 통과. commit `c2ea772`. 노션 세션 로그 박제. |
| 2026-05-10 (박제 정책) | 학습 일지가 밀릴 것을 전제로 **`-DONE.md` 페어 박제 정책 도입**. AI가 5단계 보고 직후 사실/결정/증상/키워드를 `01_Phases/{milestone}/{phase}-DONE.md`에 박고 commit. 학습 일지(본인 회고)와 역할 분리. 헌법 + `/journal-phase` 스킬 갱신, Phase 01·02 소급 작성. **Phase 03 갈래 = Y2(분리 + 별도 클라 라이브러리) 확정**, ADR + Phase 03 파일 재작성 직전. |
| 2026-05-10 (Phase 03 완료) | `04_ClientNet/` 신규 라이브러리 (Connector / ClientSession+PacketSession / Recv·SendBuffer / SmokeProbe). 5개 프로젝트 빌드 경고 0 / 오류 0. Unity F12 → 원본 한국어 주석 ReadOnly 표시 검증 통과 (ADR-010 두 번째 인스턴스). commit `fb7a06d` + `c3f2246`. 다음 = Phase 04 (Listener wire-up + connect 스모크). |
| 2026-05-10 (문서 세분화 정책) | 헌법에 **220줄 세분화 정책** 박음 (누적 섹션 외부화 / 응축 / 자르지 않음의 3분류 + 분리된 파일 재귀 세분화 시 주제별 카테고리 우선). 시범으로 `CONTEXT.md`의 갱신 이력을 본 파일(`CONTEXT_History.md`)로 외부화. |
| 2026-05-10 (헌법 응축) | 자기 정책 위반 해결 — `CLAUDE.md` 348→264줄. 사용자 컨텍스트 섹션 5개(톤·용어·결정 / 5단계 보고 템플릿 / -DONE.md 박제 / 학습 일지 권유 / 도구 카탈로그) 응축. `-DONE.md` 템플릿은 `.claude/templates/done-md-template.md`로 외부화. 절대 원칙·세분화 정책·구조 섹션은 본질이라 보존. commit `d01624c`. |
| 2026-05-10 (헌법 350줄 예외) | 헌법(`CLAUDE.md`)의 220줄 임계 자기참조 무한 제안 루프 차단. 헌법만 **350줄 임계**로 예외 처리(현재 264줄 → 86줄 성장 여유). 다른 사전형 문서(ARCHITECTURE.md 등)는 220 그대로 — 필요 시 별도 ADR. |
| 2026-05-10 (harness outdated 정정) | `.claude/` 18개 파일(서브에이전트 6 + 슬래시 커맨드 10 + hooks 2)에 박혀있던 옛 폴더 prefix(2026-05-09 정렬 변경 시 누락)를 sed로 일괄 정정. 추가로 Agent 점검 후 잔존 2건 정정 (log-session.md circular ref / done-md-template.md 참고 사례 mismatch). 핵심 통찰: 헌법 응축으로 인한 outdated = 0건. 폴더 prefix만 진짜 주범. commit `54838a5` + `15bbeb7`. |
| 2026-05-10 (Phase 04 완료) | 서버 `Program.cs` Listener wire-up(0.0.0.0:7777) + `GameSession.cs` 신작 + Unity 측 3파일(`MainThreadDispatcher` / `UnityClientSession` / `NetworkBootstrap`). 양쪽 OnConnected 로그 동시 + UnityException 없음 → main thread queue 패턴 작동 입증. ADR-012(Y2 분리) 분업이 *진짜 작동*함을 시연. SampleScene에 Network GameObject 박힘. Phase 04 파일은 이름 유지(framing 가정 outdated였지만 본문 재작성으로 정합). commit `a798479`. 다음 = Phase 05 (framing + Ping/Pong). |
| 2026-05-10 (전체 outdated 점검 + 정합) | Phase 05 진입 전 Explore agent로 `00_Document/` + Phase 파일 일괄 점검. 주요 발견: ARCHITECTURE.md가 2026-05-06/09/10 세 차례 변경(ADR-002 v2 / 폴더 prefix / ADR-012)에 *전부 누락*. 디렉토리 구조 diagram 통째 재작성 + MessagePack 의존성 제거 + EF Core 8→10 + Y2 socket 분리 모델 명시 + 변경 이력 placeholder 박제. PRD.md 1줄(`phases/` → `01_Phases/`) 정정. PRD.md 229줄(220 초과)은 별도 작업으로 미룸. Phase 05 파일 신설(`05-framing-ping.md`, BitConverter 임시 + Phase 06에서 PDL 이주 plan). commit `d5b8677` + `6494896`. |
| 2026-05-10 (Phase 05 완료 + ★ M1 Foundation 마일스톤 완료) | framing(`[size(2)][packetId(2)][payload]`) + 첫 Ping/Pong 양방향 시연. `98_Shared/Protocol/` 신설(PacketId enum + PingPacket + PongPacket, BinaryPrimitives.*LittleEndian 명시 + ToBytes/Read 페어). 서버 GameSession + Unity UnityClientSession 모두 PacketSession 상속으로 교체. Unity Update에서 1초마다 Ping → 서버 Pong → 클라 RTT 출력. 5개 프로젝트 빌드 경고 0/오류 0, 1분 안정성 통과. 직렬화는 BitConverter 임시(Phase 06에서 PDL 이주 예정). commit `5174573`. ★ M1 Foundation 완료 = 영상 시연 가능한 첫 데모. |
| 2026-05-10 (Phase 06 완료) | PacketGenerator 4월 ServerDev → `99_Tools/PacketGenerator/` 이주. **하드코딩 버그 2개 정정**: ADR-002 v2가 명시한 L178 `C_Chat`(자리 표시자 누락) + grep으로 발견한 추가 L194 `chatLen` → `count`. Program.cs nullable 정합(static string =""+`string?`+`return null`→throw). PDL.xml=`C_Ping`/`S_Pong` 두 패킷만(4월 게임용 통째 제거). csproj 정리(ApplicationIcon/PackageOutputPath 등 잔재 제거). 생성기 실행 → 3개 .cs(GenPackets 214줄+ClientPacketManager 79줄+ServerPacketManager 79줄) 정상 출력. 6개 프로젝트 빌드 경고 0/오류 0. 본 Phase 한계: 양쪽(서버+클라) 정합 + Phase 05 임시 BitConverter 코드 교체는 Phase 07로 분리. commit `03994b0`. |
