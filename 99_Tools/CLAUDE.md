# Tools — 도구체 (PacketGenerator + 헤드리스 봇)

## Layout

```
99_Tools/
├── PacketGenerator/    PDL.xml → C# 코드 생성기 (ADR-002 v2)
│   ├── PDL.xml         단일 진실: 모든 패킷 정의 (수동 작성 금지 영역)
│   ├── Program.cs      XML 파싱 + 출력 파일 생성
│   ├── PacketFormat.cs 출력 템플릿 (C# 코드 raw string)
│   └── PacketGenerator.csproj
└── headless-bot/       서버 부하/회귀 자동화 (Phase 08 박힘)
    ├── HeadlessBot.csproj
    ├── Program.cs      메인 진입점 (시나리오 디스패치)
    ├── BotSession.cs   04_ClientNet 재사용 (Connector 자리잡이 활용 — ADR-011)
    └── Scenarios/      M2BasicMovement.cs 등 결정론 시나리오
```

## PacketGenerator — PDL 수정 후 후속 작업 *의무*

**헌법 #2 (Protocol Sacred) + #4 (Shared Code Discipline) 정합**. CHANGELOG 2026-05-17 [M] 박제 — `cee8775` 사고로 표면화. 본인이 PDL.xml 수정 시 다음 3종 의무:

1. **PacketGenerator 즉시 재생성** — `dotnet run --project 99_Tools/PacketGenerator/` (또는 IDE 단축키).
2. **`dotnet build Dawnholder.slnx`로 `Shared.dll` 갱신** — PostBuild가 `03_Client/Assets/Plugins/Shared/`로 자동 복사.
3. **세 산출물 동반 commit**: `PDL.xml` + `98_Shared/Protocol/Generated/GenPackets.cs` + `Shared.dll`. 누락 시 다른 머신 pull 직후 빌드 회귀 (정유현 PR #19 사고 패턴).

## PacketGenerator 알려진 결함 (M2.5 후 또는 새 패킷 추가 직전 fix 대상)

Codex pre-M3 감사 발견 (2026-05-18, `00_Document/reviews/2026-05-18-pre-m3-codex-review.md`):

- **기본값 `noManager = false`인데 manager 인프라 부재** — `--no-manager` 없이 실행하면 *컴파일 깨지는* `ServerPacketManager.cs` 생성됨 (현재 `ServerCore` namespace + `PacketHandler` 타입이 존재하지 않음). **대처**: PDL 변경 시 `--no-manager` 옵션 의무 또는 기본값 반전 fix.
- **PDL schema validation 약함** — `<unit name="x"/>` 같은 *오타가 silent하게 누락됨*. `bool`/`string` 타입은 *broken code* 생성 (`BinaryPrimitives.ReadBooleanLittleEndian` 부재 / `Segment` 변수 누락). **대처**: 현재 PDL이 두 타입을 안 쓰니 즉시 영향 X. 새 패킷에 `bool`/`string` 추가 시점 *직전* 본 결함 fix 의무.

## headless-bot — 서버 회귀 안전망 (Phase 08 박힘)

xUnit 통합 테스트(`02_Server/GameServer.Tests/Integration/M2BasicMovementIntegrationTests.cs`)에서 사용. ServerFixture (port 0 bind) + 봇 결정론 시나리오 + p99 자동 assert. LongRunning (100회 반복) Skip 처리.

- **04_ClientNet 재사용 의존성** — `Dawnholder.Client.Net.csproj` 프로젝트 참조 (ADR-012 Y2 분리 정합). Connector.cs:21~25 "헤드리스 봇 재사용" 의도 박힘 활용 — *자리잡이 패턴 효과 3번째 증명* (CONTEXT.md 응축본 메모).
- **98_Shared 재사용** — 패킷 정의 공유 (헌법 #4 정합).

## 새 시나리오 추가 시

1. `Scenarios/{ScenarioName}.cs` 신설 — 결정론 input list + 봇 자체 Physics.Step 시뮬 + 서버 snapshot 일치 검증.
2. `BotSession.cs` 또는 `Program.cs`에서 dispatch 연결.
3. xUnit 통합 테스트로 박을 가치 있으면 `02_Server/GameServer.Tests/Integration/`에 fixture 추가.

## 변경 머지 전

`dotnet build Dawnholder.slnx`로 양쪽(PacketGenerator + headless-bot) 컴파일 통과 검증. PDL 수정 후엔 위 *후속 작업 의무* 3종 동반.
