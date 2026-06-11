---
summary: 원격 보간을 serverTick 시간축 + clock smoothing으로 전환(창드래그 desync·stutter 봉합), broadcast 20Hz·서버 vx facing·공격 타겟 facing 스냅까지 정돈 — RemotePlayer가 Local과 같은 방향·부드러움으로 그려짐.
owner: youngho
milestone: M4.11
phase: 01-remote-interp-servertick
work-id: phase01-m4.11-sync
status: done
grade: 대규모
slug: 01-remote-interp-servertick
created: 2026-06-11
completed_at: 2026-06-11
commit: 5f928c4
domains: [shared, server, client]
prior_phases: []
depends_on: []
---

# Phase 01 — 원격 보간 serverTick 전환 + 부드러움 + facing 정돈 완료 박제

**소요 시간**: 1 세션 (compact 1회 포함)

## TL;DR

원격 엔티티(타인/몬스터) 보간이 서버 `serverTick`(권위 시각)을 버리고 클라 벽시계로 타임스탬프를 재도장해, 창 드래그로 `Update`가 멈췄다 재개되면 쌓인 스냅샷이 같은 벽시계 값으로 뭉쳐 desync(백로그 #5)가 났다. serverTick 시간축으로 전환해 봉합했고, 그 과정에서 매-snapshot 재기준이 stutter 회귀를 일으켜 clock smoothing(연속 재생 시계 + drift 흡수 + 큰 갭 snap)으로 2차 봉합했다. 이어 실측 피드백으로 broadcast 20Hz·서버 vx 기반 facing·공격 타겟 facing 스냅까지 정돈해, RemotePlayer가 Local과 동일한 방향·부드러움으로 그려진다. WSL2 556 tests passed, 영호 2클라 실측 통과, reviewer 6축 통과(blocker 0).

## 5단계 보고

- **무엇을 만들었나** — 원격 보간 4종 정돈: ①serverTick 시간축 전환(플레이어+적, 적은 `S_EntityState`에 serverTick append → ProtocolVersion 11→12) + clock smoothing, ②snapshot broadcast 10Hz→20Hz, ③RemotePlayer facing을 서버 vx 부호 기반으로, ④피격/공격 facing 가드(피격=공격자 향함, 공격=서버 타겟 방향 스냅).
- **왜 필요한가** — 발표 전 기반 정돈. 타 클라에서 보이는 캐릭터가 ⓐ창 드래그 후 천천히 회복되는 desync, ⓑ뚝뚝 끊김(stutter), ⓒ정지 중 좌우 떨림, ⓓ피격 시 잘못된 방향 플립, ⓔ공격 시 Local과 다른 방향 — 다섯 결함이 있었다. 모두 "서버 권위를 클라가 버리거나 추측"해서 생긴 것.
- **어떻게 만들었나** — 보간 버퍼 타임스탬프를 `serverTick * TickDuration`으로(클라 벽시계 폐기). 재생 시점은 `_renderTime` 연속 전진 + `targetRender`와의 drift를 `CatchupRate 0.1`로 부드럽게 추종 + `ResyncThreshold 0.5` 초과(freeze 복귀)면 즉시 snap. facing은 서버 vx 부호 + 특수 상태 가드(피격 넉백 vx 반전, 공격은 `CombatSystem`이 `FacingDir`을 타겟 방향 스냅한 값을 `S_PlayerAttack.facing`으로 latch). LocalPlayerMotion 우선순위(피격>공격>이동)를 RemotePlayerMotion에 거울 복제.
- **테스트 결과** — WSL2 `dotnet test` 556 passed / 4 skip(LongRunning). Unity 컴파일 clean(`scriptCompilationFailed=False`), BuildPlayer Succeeded(7씬, errors 0). 영호 2클라 실측: 창드래그·stutter·떨림·피격 플립·공격 방향 전부 봉합 확인. reviewer 6축 통과(🔴 0).
- **다음 스텝** — M4.11 P2~P5(force-adopt/안전망/고정스텝/재빌드)는 P1이 실제 결함을 다 해소해 contingency로 남음 — 마일스톤 마감 여부 영호 판단. 백로그: facing 스냅 회귀 테스트(`ProcessAttack` 단위) 미박음.

## AC 검증 결과

Phase 완료조건 = 창드래그 desync 봉합 + 회귀 0 + 빌드 통과. 실제 실행 결과:

```bash
# 서버 단위/통합 테스트 (WSL2, ADR-029 — SAC가 Windows dotnet test 차단)
$ wsl -d Ubuntu -- bash -lc "cd ~/dawnholder-poc && dotnet test 02_Server/GameServer.Tests/GameServer.Tests.csproj --no-build"
  Passed!  - Failed: 0, Passed: 556, Skipped: 4, Total: 560, Duration: 1 m 40 s

# 서버 빌드 (Windows — 실행만 SAC 차단, build 가능)
$ dotnet build 02_Server/GameServer/GameServer.csproj
  경고 0개  오류 0개

# 클라 컴파일 + 빌드 (Unity MCP)
  scriptCompilationFailed=False  isCompiling=False
  BUILD result=Succeeded errors=0 warnings=18(전부 기존 CS8632/CS0618) out=C:/Dev/Build/Client/03_Client.exe scenes=7

# 서버 틱 예산 (20Hz broadcast 부하 — §5 확인)
  [Tick] 1초 메트릭: p50=0.02ms p95=0.03ms p99=0.03ms max=0.30ms avg=0.02ms n=20
```

영호 2클라 실측 (육안 검증): 창드래그 desync ✅봉합 / stutter ✅사라짐 / 이동 궤적 ✅부드러움 / 정지 떨림 ✅사라짐 / 피격 시 공격자 향함 ✅ / 공격 방향 Local↔Remote ✅일치.

## 결정 흐름 (회고 참고용)

- **적 보간도 같이 고칠까 (v12 bump)** → 했음. `RemoteEntity`가 플레이어/적 공용 컴포넌트라 공용 시그니처를 바꾸면 적 경로가 깨짐. 적만 옛 시간축으로 두면 반쪽 봉합 + 컴파일 에러. append-only + 라운드트립 안전망으로 v12 안전.
- **stutter 봉합: 벽시계 복귀 vs clock smoothing** → clock smoothing. 옛 벽시계는 부드러웠지만 freeze 뭉침이 원인이었음(되돌리면 백로그 #5 재발). serverTick 유지하되 재생 시점만 연속 시계로 = 두 마리 토끼.
- **facing: 클라만 vs 서버 타겟 스냅** → 서버 타겟 스냅(영호 선택). 클라만 고치면 Local FaceToward(타겟)와 서버 broadcast(이동 방향)가 계속 어긋남. 서버가 타겟 방향으로 스냅해야 3자(Local·서버·Remote) 일치. trade-off: 이동 반대편 적 칠 때 lunge가 적 쪽으로 감(의도된 "몬스터 따라가기").
- **broadcast 20Hz 부하** → 로컬/발표 규모 무관 판정. 틱예산 p99 ~1ms 실측으로 §5 정합 확인.

## 막혔던 지점

- **BuildPlayer "scripts have compile errors" but 콘솔 CS 0** → `EnemyRegistry.cs:97` 컴파일에러를 Editor.log 역추적으로 발견(ReadConsole 빈응답 = MCP 버그 아님). `scriptCompilationFailed` 플래그가 진짜 상태.
- **SAC가 GameServer.exe 실행 차단** → WSL2 실행 표준(ADR-029). run-server.bat WSL2 전환.
- **`pkill -f 'dotnet GameServer'`가 자기 부모 bash까지 죽임** → kill과 build를 분리된 bash 호출로.

## 학습 일지 후보 키워드

- snapshot interpolation, server-authoritative time axis (serverTick vs wall-clock)
- client-side clock / playout delay buffer, interpolation clock, drift catch-up vs snap-on-discontinuity
- ProtocolVersion bump (append-only), PacketRoundTrip 안전망
- server-authoritative facing, Local↔Remote 표현 일치, knockback 방향 반전
- SAC(Smart App Control) + WSL2 실행 표준(ADR-029), pkill -f self-match 함정

---

## Commits (feature/m4.11-sync)

```
5cf33ce 플레이어 보간 serverTick 전환
3208602 적 보간 serverTick(v12) + clock smoothing
7931e91 run-server.bat WSL2 전환 (SAC 우회, ADR-029)
0c2eeb3 snapshot broadcast 10Hz→20Hz (SnapshotTickInterval 2→1)
d845e19 RemotePlayer facing 서버 vx 기반
9933740 평타 facing 타겟 방향 스냅 (서버)
5f928c4 RemotePlayer facing 우선순위 (피격/공격 가드, 클라)
```
