# 도구와 검증

게임 시나리오의 [ProbeBase](../../99_Tools/headless-bot/Scenarios/ProbeBase.cs)는 봇마다 `bot_<실행별 4자리 hex>_<일련번호>` 계정을 만들고 handshake → 로그인 → 직업 선택 순으로 입장한다. 수동 접속인 `M2BasicMovement`도 같은 이름 생성기와 로그인 단계를 사용한다. [MultiAccountLoginSmoke](../../99_Tools/headless-bot/Scenarios/Roster/MultiAccountLoginSmoke.cs)는 서로 다른 두 계정 입장, 이중 로그인 거부와 끊김, 기존 연결의 계속되는 스냅샷, 퇴장 통지 뒤 같은 이름 재입장, 규칙 위반 거부를 클라이언트 수신 패킷으로 확인한다. `Program.cs`와 `run_bot_regression.sh`에 등록되어 있다. 시나리오의 작성·빌드와 실제 실행 결과는 구분한다.

[GameServer.Tests](../../02_Server/GameServer.Tests/)는 xUnit 단위·통합 검증을, [headless-bot](../../99_Tools/headless-bot/)은 ClientNet·Shared를 사용한 통신 시나리오를 제공한다. 실행 명령과 환경 전제는 [DEVELOPMENT](../operations/DEVELOPMENT.md)를 따른다.

| 변경 | 검증 출발점 |
|---|---|
| 패킷·입력 검증 | 패킷 왕복, 길이·범위·권한 거부 테스트와 해당 봇 |
| 이동·전투 | 물리·판정 단위 테스트, 관련 Integration/시나리오 |
| 맵·파티·퀘스트 | 상태 이전·ID·진행도 단위 검사와 해당 봇 |
| Unity 표현 | Unity 컴파일·플레이 확인; .NET 결과로 대체하지 않음 |
| 문서 | 경로·심볼·권한 일치, 링크와 원문 근거, HTML 렌더 |

테스트가 성공한 분기·실패한 조건·Skip 지정·검증하지 않은 영역을 구분한다. 봇 이름만으로 검증 범위를 확대하지 않는다. 예를 들어 PartyQuest standalone의 파티 결성·맵 이동·해산 성공은 공동 처치 카운트를 확인했다는 뜻이 아니다.

WSL 실행은 원본 경로별 복사 공간과 잠금을 사용한다. 7777 포트가 다른 작업에 속하면 종료하지 않고 충돌을 보고한다. 시나리오와 원본 로그 위치를 결과에 남긴다.

과거 QA knowledge는 전용 검증 사례가 없는 색인이었다. 이 문서는 존재하지 않았던 실적을 추가하지 않고 현재 테스트·도구 진입점으로 대체했다. [원문](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/.claude/knowledge/qa/_index.md).

## 두 봇 길드 성장 순환

`bash 99_Tools/sync-wsl.sh bot GuildGrowthSmoke`는 새 DEBUG 서버에서 [GuildGrowthSmoke](../../99_Tools/headless-bot/Scenarios/Guild/GuildGrowthSmoke.cs)를 실행한다. 별도 두 계정이 같은 파티·보스방에 들어가 실제 공격으로 클리어하고, 39→36→자기44의 개인50/재료1/주머니1과 공동 금고10→30을 검사한다. DEBUG cheatType0은 퀘스트 게이트 준비만이며 실제 처치 실적이 아니다. Town 복귀·중복확인 뒤 같은 공동revision의 구매 둘 중 하나만 금고30을 소비하고, 한 명의 개인20/재료1 강화와 동등한 새 보스에 대한 실제 근접 Damage 차이+5/상대0, 같은 계정 재접속의 시드 중복0·공동 유지·개인 초기화를 확인한다.

[GuildGrowthProbe](../../99_Tools/headless-bot/Scenarios/Guild/GuildGrowthProbe.cs)는 ProbeBase의 접속·이동을 재사용하며, 44의 Read 전 길이/이름·enum/범위·접속별 sequence와 47의 action/requestedRevision/target44를 검사한다. 일반 방송44는 구매 pending을 끝내지 않는다. 관측은 `[Guild wire]`/`[Guild observation]`에 기록하고 누락·파싱 오류·취소는 실패/exit1이다. 내부110초 취소와 finally의 자기 연결 종료를 쓰며 원본 runner의120초 제한을 유지한다.

`run_bot_regression.sh`는 기존 목록을 보존하고 PartyQuest·InventorySmoke·DungeonClearSmoke·BossGate·GuildGrowthSmoke를 추가한다. 목록 등록과 전체 실행은 별개다. 길드 봇은 전사 근접 피해 비교이며 마법사·Dash·Thunderbolt·지연 피해·리스폰과 모든 실패 조합을 전수 검증하지 않는다. 실제 회귀/최종 독립 판정·Unity 양소비자·CI/PR은 해당 실행 근거로 별도 확인한다. WSL의 Shared/ClientNet DLL2를 별도 배포해도 Unity 컴파일 성공은 아니다.
