---
owner: youngho
milestone: M4.4
phase: 03
title: 맵 데이터 파이프라인 전환(바이너리 bake) + 지형 통합
status: done
completed: 2026-06-06
grade: 대규모
summary: M4.4 Phase 03 완료 (10 commits, 71파일 +3030/−620, 세션13~14). 맵의 진실(지형+스폰)이 생성 C#/하드코딩 테이블에서 씬 저작→bake→바이너리(terrain.bin 공유 / content.bin 서버 전용)→런타임 로드 파이프라인으로 이사. MapDataFile(DWMP+CRC32 fail-closed) 신설, TerrainBaker 바이너리 재작성, 서버 MapDataLoader+kill-plane, 클라/봇 지형 주입 3경로 대칭, MapSpawnTable·MapTerrainData 은퇴. E 검증에서 봇 시나리오를 bake 배치 적응형으로 전환하며 결함 2건 발본(EmergencyCombat stale-좌표 race / EditMode stale 단언). 검증 = test 388/0/4skip + 봇 6/6 + EditMode 36/0 + reviewer 4회 🔴0 + plan-auditor GO. Play 실측은 사용자 결정으로 Phase 05 묶음 이월. 5단계 보고 시각판 = 03-terrain-integration-DONE.html.
---

# Phase 03 박제: 맵 데이터 바이너리 파이프라인 전환

**소요**: 세션13 (재정의+A~D 풀세트) + 세션14 (E 잔여 ③⑤ + 마감) — Coordinator 분해 + 도메인 SubAgent 분담 + 메인 검수
**시각 보고서**: [`03-terrain-integration-DONE.html`](03-terrain-integration-DONE.html) — bin 실데이터 디코드 지형 렌더링 포함 (대규모 5단계 보고 HTML 박제)

## 5단계 보고

- 🎯 **무엇을 만들었나** — 맵 지형·스폰 데이터의 "씬 저작 → bake → 바이너리(terrain.bin/content.bin) → 런타임 로드" 파이프라인. 서버·클라·봇 3경로가 같은 파일을 읽는다. 상세 = TL;DR + 박제 사실.
- 🤔 **왜 필요한가** — 옛 구조(생성 C# + 하드코딩 테이블)는 진실이 두 군데라 씬 수정마다 drift 위험. 콘텐츠가 늘어나는 M4.4부터 못 버티는 구조 → 데이터 주도 전환 + 파일 경계로 신뢰 경계(헌법 #1) 강제.
- 🛠️ **어떻게 만들었나** — 스테이지 A(포맷)→B(baker)→C(서버)→bake(마커 저작)→D(클라+봇)→E(검증). 상세 = 박제 사실 표 + HTML §3~6.
- 🧪 **테스트 결과** — dotnet 388/0/4skip + 봇 6시나리오 신선 서버 일괄 PASS + Unity EditMode 36/0 + reviewer 4회 🔴0. 상세 = AC 검증 결과.
- ➡️ **다음 스텝** — Phase 04 직업 이동 분리 진입. Play 실측·발판 저작은 Phase 05 묶음(사용자 확정). 상세 = 막혔던 지점 / 이월.

## TL;DR (🎯 무엇 / 🤔 왜)

맵 1개의 지형·오브젝트 데이터가 **씬 저작 → bake → 바이너리 파일 → 런타임 로드** 한 줄기로 흐르게 됐다.
옛 구조는 지형=생성 C#(`MapTerrainData`) + 스폰=하드코딩(`MapSpawnTable`)로 진실이 두 군데라 씬 수정마다 코드 동기가 사람 손에 달려 있었다(drift 위험). M4.4부터 언덕·단차·발판·적 배치가 늘어나 못 버티는 구조 → 데이터 주도 전환.

핵심 갈래 셋:
1. **파일 경계 = 신뢰 경계 (헌법 #1)** — `terrain.bin`(솔리드/발판/kill-plane)은 서버·클라·봇 공유, `content.bin`(플레이어/적 스폰)은 서버 전용·클라 빌드 미배포. 클라는 적의 존재를 S_EntitySpawn으로만 안다.
2. **무결성은 넣고 암호화는 뺌** — CRC32 fail-closed(조용한 오독 < 시끄러운 즉사). 암호화는 서버 권위라 변조=본인 prediction만 깨짐 → 보호 가치 없는 비용 컷.
3. **3경로 대칭** — 서버 `MapDataLoader` / 클라 `ClientTerrainStore`(StreamingAssets) / 봇 `BotTerrainLoader`가 같은 bin + 같은 `Physics.Step`. 다른 지형으로 예측하면 reconcile이 매 걸음 어긋난다.

## 박제 사실 (🛠️ 어떻게)

| 스테이지 | 산출 | commit |
|---|---|---|
| 재정의 | 사용자 의논 4건 반영 + plan-auditor 조건부 GO→GO (P0 kill-plane 정책 사용자 확정) | `370a160` |
| A 포맷 | `98_Shared/GameData/MapDataFile.cs` — DWMP+version+kind+mapId+payloadLen+CRC32, 5중 검증 fail-closed. writer/reader 한 클래스 = 포맷 drift 원천 차단. β P3 봉합(`MapTerrain` ReadOnlySpan 캡슐화) | `9f6fd53` |
| B baker | `TerrainBaker` 바이너리 재작성 — 마커(`Spawn_Player`/`Spawn_Enemy_*`) 추출 + kill-plane 자동(min MinY−10) + terrain.bin 두 벌 동시 출력(98_Shared+StreamingAssets, drift 창 0) | `3a35f08` |
| C 서버 | `MapDataLoader`(플레이 맵 부재=startup hard error, Ending=의도된 빈 맵 명시) + GameMap terrain/content 주입 + kill-plane(스폰 재배치 HP 무변화) + `MapSpawnTable`·`MapTerrainData`·`ForMap` 삭제. reviewer 🟡 채택(kindId 검증 단일화) | `fa58fdd` `741a89c` |
| bake | 첫 실산출 bin 9개 + 세 씬 마커 저작. 가드 의미론 실저작 3회 반복 확정 — 받침 면="bake 중력" / Spawn_Player 공중 허용(저작 y 보존) / Spawn_Enemy faceY 스냅 / 복제 suffix `(n)` 정규화 | `28a0d2c` |
| D 클라+봇 | `ClientTerrainStore` + `PlayerPredictor.SetTerrain`/`IsGroundedAt`(reconcile 접지) + `BotTerrainLoader` + 맵 전환 시 terrain 갱신 의무 | `52ebdea` |
| E 검증 | 봇 시나리오 bake 배치 적응형 전환 + 실행 + IsGroundedAt EditMode 4분기 (아래 상세) | `7cff593` |

**E 스테이지 상세 (세션14)**:
- `EnemyAiSmoke` — 하드코딩 상수(`AggroEntryX=7`, "SpawnX=10" 가정) 은퇴 → S_EntitySpawn 수신 목록에서 **같은 높이(±1.5) + aggro 밖 중 |dx| 최소** 타겟을 런타임 선정. aggro 상수는 `EnemyStats.NormalDefault()` 단일 출처(헌법 #4). 재bake 내성 — 봇은 content.bin을 안 읽고 패킷 관측만(헌법 #1).
- `EmergencyCombatSmoke` — **실행에서 race 발본**: 스폰 패킷의 stale patrol 좌표로 접근 직후 즉시 첫 공격 → 새 배치에선 순찰 위상 따라 적이 aggro 밖이면 AABB 미스. live S_EntityState 근접 수렴 대기(15s 상한) 추가로 봉합. 옛 배치(x=10)에선 순찰대 전체가 항상 aggro 안이라 우연히 통과했던 것.
- `PlayerPredictorTests` — IsGroundedAt 4분기(평지 null/솔리드 윗면/발판/상승 vy>0) 8테스트를 OnSnapshot 공개 표면으로 추가 + **스테이지 D stale 단언 발본**(SetInitialPosition 항상 공중 출발로 바뀐 걸 테스트 미반영 — EditMode가 dotnet test에 안 잡혀 숨어 있었음).
- `MapId.cs` 주석 stale 정정 + destSpawn/playerSpawn 사용처 실측: 첫 입장=content(`GameSession`) / 포탈 이동=PortalTable destSpawn(`MapMigration`) / kill-plane 재배치=content(`GameMap`).

## AC 검증 결과

- `dotnet build --no-incremental` 경고 0/오류 0 + `dotnet test --no-build` → **388 통과 / 0 실패 / 4 skip**
- 헤드리스 봇 **6 시나리오 신선 서버 일괄 PASS** (smoke/M2/MultiRoster/EmergencyCombat/BossStageClear/EnemyAi). EmergencyCombat 누적 4회 PASS — 리스폰 적 타겟 케이스 포함. 보스는 StageClear 1회성 설계라 같은 부팅 재실행 시 스폰 timeout = 정상
- Unity EditMode **36 통과 / 0 실패** — TestRunnerApi를 Unity MCP RunCommand로 실행하는 패턴 검증됨 (Refresh→Run→콘솔 수확)
- bake idempotent: 재bake 시 bin byte-identical(git diff 0) / bin 9개 실디코드로 좌표 전수 검증(pin 박제값 일치)
- bake 산출물 = 바이너리만(생성 C# 0건) / 클라 빌드에 content.bin 미포함 / `MapSpawnTable`·`MapTerrainData` 참조 0
- plan-auditor GO(착수 전) + reviewer 4회(세션13 3 + 세션14 1) 누적 **🔴 0** / ProtocolVersion 8 불변(파일 formatVersion v1은 별개 축)
- 메인 검수 발본 누적 10건 — 세션13 SubAgent 산출물 8건(B 2/C 5/D 1, 허위 보고 신변종 포함) + 세션14 실행 검증 2건

## 결정 흐름 (회고 참고용)

- **바이너리 vs 생성 C# 유지** — C# 코드젠은 컴파일 필요 + Unity/서버 별도 경로 + 콘텐츠 수정=코드 diff. 바이너리는 재컴파일 불필요 + 한 로더 공유 + 포맷 버전 축 분리. 단점 = diff 불가 산출물 → CRC32+실디코드 검증으로 흡수.
- **GameWorld provider 필수 인자** — 옵셔널이면 빈 월드가 조용히 뜬다(silent 실패). 필수로 박아 부재=기동 실패.
- **kill-plane 처리 = 스폰 재배치 HP 무변화** — 낙사 데미지/사망 정책은 전투 밸런스(M4.5)와 함께. 지금은 "구멍에 빠져도 게임이 계속된다"만 보장.
- **PortalTable bake 이월(M4.5+)** — M4.2 race 검증까지 끝난 코드를 다시 굽는 위험 > 이득.
- **봇 시나리오 적응형 전환** — 좌표 상수 재보정(1회용)이 아니라 수신 데이터 기반 선정(재bake 내성)을 택함. Phase 05 발판 저작+재bake가 예정돼 있어 1회용 보정은 즉시 부채가 됐을 것.
- **도구 가드는 저작자 편** — 공중 스폰 에러 처리였다면 저작 흐름이 매번 끊겼을 것. 실저작 3회 반복으로 "에러는 깊은 매몰/받침 없음만" 확정.

## 막혔던 지점 / 이월 (➡️ 다음)

- **Play 실측 = Phase 05 묶음 이월 (사용자 확정)** — 플레이어 prefab+직업 분기 세팅 후 03~05 일괄: 언덕/단차/발판/공중 스폰 착지/낙하 kill-plane 재배치/맵 전환/dt reconcile 흡수. 본인 잔여 = 발판 저작+재bake(현재 세 씬 발판 0개)
- **reviewer 🟡 이월 2건(선택)** — 봇 probe 좌표 lock 비대칭(_entityX는 lock, SpawnX 비보호) / EmergencyCombatSmoke 첫 마리+공간 전제(다음 작업 시 EnemyAiSmoke식 선정으로 대칭화)
- **Ending 클라-봇 fail-loud 비대칭 인지** — "지형 없는 맵" 분기가 봇 코드에만 있음. Ending에 플레이어 배치하는 날 클라 FileNotFound — 세 번째 호출자 등장 시 `MapId.HasTerrain` Shared 추출 검토
- 세션14 환경 메모: Bash 분류기 장애 ~30분(읽기/편집으로 우회 작업) + PowerShell deny 룰 확인(bash가 현 경로 — python/dotnet으로 디코드)

## 학습 일지 후보 키워드

파일 경계=신뢰 경계 설계 / fail-closed 무결성(암호화 없는 이유 논증) / 산출물 두 벌 동시 출력=drift 창 제거 / 적응형 시나리오(상수 보정 vs 데이터 적응) / "커버리지가 있다 ≠ 돌아간다"(EditMode 사각) / stale 관측 좌표 race(스폰 패킷=관측 순간의 진실) / Unity TestRunnerApi MCP 실행 패턴

## 다음 Phase

- **Phase 04 — 직업 이동 분리** (Warrior 4 / Ranger 6, β10 의도된 정정. 세 번째 유사 순회 시 Aabb helper Rule of Three)
