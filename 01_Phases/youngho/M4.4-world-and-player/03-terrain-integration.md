---
owner: youngho
milestone: M4.4
phase: 03
title: 맵 데이터 파이프라인 전환(바이너리 bake) + 지형 통합 실측
status: pending
grade: 대규모
risk: unity-asset
estimated: 8~12h
domain: shared+server+client+qa
---

# Phase 03: 맵 데이터 파이프라인 전환 + 지형 통합 실측

> **상태**: pending
> **마일스톤**: M4.4
> **등급**: 대규모 (4 도메인 + 데이터 포맷 신설 비가역 + 씬 unity-asset) — 옛 정의(복잡)에서 상향
> **담당**: Coordinator 분해 — shared(포맷/loader) + client(baker/predictor) + server(GameWorld/GameMap) + qa(봇/회귀) + 본인(씬 마커 저작·발판·Play 실측)

---

## 🔁 재정의 사유 (2026-06-06 세션13 의논 박제)

옛 Phase 03 = "생성 C# 지형 데이터를 서버·클라에 연결"이었으나, 사용자 결정으로 **맵 데이터 관리 방식 자체를 전환**:

1. **C# 코드 생성 → 바이너리 파일 + 런타임 로드** — 데이터와 코드 분리. 콘텐츠 변경이 코드 재컴파일을 요구하지 않는 구조 (미래 맵 에디터 마일스톤의 기반).
2. **bake 범위 확장** — 지형뿐 아니라 오브젝트 위치(플레이어/적 스폰, kill-plane)까지. 하드코딩 좌표(`MapSpawnTable`) 은퇴.
3. **암호화 X, 무결성 헤더 O** — 서버 권위 구조에서 클라 지형 파일 변조는 본인 prediction만 깨짐(서버가 자기 파일로 판정 + reconcile). 암호화 = 난독화 비용만 실재. 대신 magic + 포맷 버전 + checksum으로 깨진/옛 파일을 **시끄러운 로드 실패**로 처리 (핸드셰이크 "관대하게 처리 금지" 정신).
4. **Claude 단독 진행** (Codex 분담 X — 직렬 파이프라인이라 병렬 이득 < 조율 비용). 멀티 AI 오케스트레이터는 본인 제작 백로그로 분리.

---

## 🎯 목표

맵 1개의 지형·오브젝트 데이터가 **씬 저작 → bake → 바이너리 파일 → 런타임 로드** 파이프라인으로 흐르고, 서버와 클라 prediction이 같은 파일에서 로드한 같은 지형 위에서 동작한다. 세 씬의 언덕·단차 위에서 이동/점프/착지가 Play로 정상이고, 봇 회귀가 통과하며, **상호작용 지형의 범위가 확정**된다.

---

## ⏪ 사전 조건

- [ ] Phase 02 — 지형 물리 + 단위 테스트 green (✅ `c5e615e` 머지됨)
- [ ] **상호작용 지형 실물 확인 선행** (유현 씬에서 어떤 오브젝트인지 — 포탈이면 기존 C_EnterPortal 재사용으로 즉결)

---

## 📐 설계 결정

### D1. 파일 포맷 v1 — 맵당 2파일 분리

| 파일 | 내용 | 소비자 |
|---|---|---|
| `map_{id}.terrain.bin` | 솔리드 AABB + one-way 발판 + killPlaneY | 서버 + 클라 + 봇 (공유) |
| `map_{id}.content.bin` | 플레이어 스폰 좌표 + 적 스폰 목록(kind+좌표) | **서버 전용** |

분리 사유 = 헌법 #1 정신: 적 스폰 정의는 서버 권위 데이터 ("클라는 S_EntitySpawn으로만 적의 존재를 안다" — `MapSpawnTable.cs` 기존 주석). 클라 빌드에는 terrain.bin만 실림.

**헤더 (공통)**: magic `DWMP`(4B) + formatVersion u16 + mapId u16 + payloadLength u32 + CRC32(payload) u32. 본문 직렬화는 `BinaryPrimitives` LittleEndian 명시 (GenPackets wire format 관례 정합, ADR-002 정신). float는 바이너리 round-trip = 비트 단위 보존이라 결정론에 텍스트보다 유리.

**fail-closed**: magic/버전/길이/CRC 불일치 → 명확한 메시지의 예외 (silent fallback 금지).

**스탯은 데이터에 안 박음**: content.bin은 위치+kind만. MaxHp 등은 서버 `EnemyStats` kind 기본값 해석 (위치=저작 데이터 / 수치=서버 권위 코드 분리).

### D2. 파일 배치 + 배포 경로

- **repo 단일 진실**: `98_Shared/GameData/Maps/*.bin` (baker 출력지)
- **서버/봇**: csproj `Content` + `CopyToOutputDirectory` → `AppContext.BaseDirectory/Maps/`에서 로드
- **클라**: baker가 terrain.bin을 `03_Client/Assets/StreamingAssets/Maps/`에 **동시 출력** (byte-identical 2벌 — 한 번의 bake 안에서 같이 쓰므로 drift 창 없음. ADR-010 Shared.dll → Plugins 복사 선례와 같은 "산출물 배포 복사" 패턴). content.bin은 StreamingAssets에 **출력하지 않음** (D1).

### D3. Reader/Writer 모두 98_Shared

`MapDataFile`(가칭): `WriteTerrain/ReadTerrain`, `WriteContent/ReadContent` + CRC32 구현(netstandard2.1에 System.IO.Hashing 없음 — 테이블 방식 ~20줄). **baker(Unity 에디터)가 Shared.dll의 writer를 직접 사용** → 쓰기/읽기 대칭이 한 코드에 있어 포맷 drift 원천 차단 + round-trip 테스트 가능.

### D4. 기존 코드 은퇴/교체

- `MapTerrainData.cs`(생성 C#) **삭제**, `MapTerrain.ForMap` **은퇴** → 로드 API로 교체
- `MapSpawnTable.cs` **은퇴** → content.bin (테스트는 인라인 `MapContent` 구성으로 이행)
- **PortalTable은 이번 범위 밖** — M4.2 race 검증이 끝난 코드라 보존, 포탈 bake 이월은 M4.5+ 명시
- **β P3 봉합**: `MapTerrain` public 배열 필드 → private + `ReadOnlySpan<T>` 프로퍼티 노출 (요소 변조 차단, 핫루프 비용 0). Phase 02 M-2(ReferenceEquals zero-alloc) 테스트는 로더 1회 로드 + 틱 무할당 검증으로 교체

### D5. 씬 저작 약속 (마커)

타일맵 레이어 약속(`Tilemap_Solid`/`Tilemap_Platform` — 이름으로만 읽음)과 같은 정신으로, 빈 GameObject 이름 규약:

- `Spawn_Player` (맵당 1개 의무 — 없으면 bake 에러)
- `Spawn_Enemy_Normal` / `Spawn_Enemy_Boss` (0개+ — 이름 suffix = EnemyKind 매핑)

**kill-plane은 마커 없이 자동 유도**: `min(솔리드 MinY) - 10f` → terrain.bin에 박음 (저작 부담 0, 구멍 낙하 = 맵 밖 추락만 처리).

**bake 가드 (plan-auditor P1-①)**: 스폰 마커 Y가 솔리드 윗면 ±eps 밖이면 **bake 에러** — 공중스폰/지형 매몰 저작 실수를 도구가 강제 차단 (`Spawn_Player` 부재 에러와 같은 정신).

### D6. 런타임 배선

- **서버**: `GameWorld` ctor에서 맵별 terrain/content 1회 로드 → `GameMap` ctor 주입 (오버로드 — null이면 평지+빈 콘텐츠, 기존 테스트 호출부 변경 최소화 = Phase 02 오버로드 선례). 플레이 맵 3개는 파일 부재 시 **startup hard error**, `Ending`은 의도된 빈 맵 명시 등록.
- **kill-plane 처리 (서버 tick)**: `y < killPlaneY` → 해당 맵 `Spawn_Player` 좌표로 재배치, HP 무변화 (**2026-06-06 사용자 확정** — 낙사 데미지/사망 정책은 M4.5 검토. plan-auditor P0 봉합).
- **클라**: 초기 진입 + `S_MapTransition` 수신 시 StreamingAssets에서 해당 맵 terrain 로드 → `PlayerPredictor`에 주입 (`Predict`가 지형 오버로드 호출). 맵 전환 시 갱신 누락 = 드리프트 폭증 함정.
- **봇**: csproj copy로 같은 bin 로드 → 자체 시뮬에 지형 주입 (= reconcile drift 검증).

---

## 📝 작업 내용 (스테이지 직렬)

- [ ] **A. 포맷+Reader/Writer (shared)**: `MapDataFile` + CRC32 + 무결성 round-trip/변조/버전 mismatch 테스트. β P3 `ReadOnlySpan` 전환 동반
- [ ] **B. baker 재작성 (client 에디터)**: 코드젠 → terrain.bin(2벌)+content.bin 출력 + 마커 추출 + `Spawn_Player` 부재 에러. 본인: 세 씬에 마커 저작(+발판 저작 가능 시) → **재bake → bin 동반 commit**
- [ ] **C. 서버 배선 (server)**: GameWorld 로드 + GameMap 주입(오버로드) + player tick `Physics.Step(state, input, terrain)` + kill-plane 처리(정책 = 작업 로그 박제 결정) + `MapSpawnTable` 은퇴/테스트 이행 + 적 스폰 Y 정합(평탄 구간 전제 유지). **착수 게이트(plan-auditor P1-②)**: `Grep MapSpawnTable·new GameMap`으로 이행 대상 테스트 N파일 실측 확정 후 진행 — enemy 존재 전제 테스트만 인라인 content 주입, 비-enemy 테스트는 오버로드로 0변경 보장
- [ ] **D. 클라+봇 배선 (client/qa)**: predictor 지형 주입(전환 갱신 포함) + 봇 지형 주입 + 이동 시나리오 지형 맵 PASS
- [ ] **E. 실측+회귀**: Play 체크리스트(언덕/단차/발판 통과·착지/맵 전환/낙하 재배치) + 서버 단독 진실 확인 + dt mispredict reconcile 흡수 실측 + `dotnet test` 회귀 0
- [ ] **CHANGELOG [M] entry를 첫 commit에 묶음** (M4.4 01+02 누적분 + 본 Phase 파이프라인 전환 — M4.3 Phase 12 선례)
- [ ] 본인+유현: **상호작용 지형 실물 확인 → 범위 확정** (포탈이면 C_EnterPortal 재사용 / 그 외면 M4.5+ 이월 명시 — 결정 박제)

---

## ✅ 완료 조건

- [ ] bake 산출물 = 바이너리 파일만 (생성 C# 0건) + 재실행 idempotent + StreamingAssets/98_Shared 두 벌 byte-identical
- [ ] 무결성 테스트: 정상 round-trip + CRC 변조 + 버전 mismatch + magic 오염 전부 명시 실패 (silent 통과 0)
- [ ] 클라 빌드 산출물에 content.bin 미포함 (terrain.bin만)
- [ ] 세 씬 Play 실측 체크리스트 전 항목 통과 (멈춤/벽끼임/공중부양 0) + 구멍 낙하 시 spawn 재배치
- [ ] 봇 이동 시나리오 PASS — SnapCount가 평지 baseline 동일 수준 (임계값은 스테이지 D 착수 시 baseline 실측으로 박제 — "0건" 요구는 가변 dt 간헐 snap 특성상 false-fail 위험, plan-auditor P2-①)
- [ ] 서버 단독 진실 확인: 클라 prediction을 죽여도(스냅만) 서버 위치가 지형 위 정상
- [ ] 상호작용 지형 범위 결정이 본 문서 작업 로그에 박힘
- [ ] `MapSpawnTable.cs`/`MapTerrainData.cs` 삭제 + 참조 0 + `dotnet test` 회귀 0
- [ ] ProtocolVersion == 8 유지 (파이프라인 전환은 패킷 무관 — bump 0)

---

## 🧪 테스트

**자동**: 포맷 round-trip/무결성 4종(A) · GameMap 로드 통합(스폰 → 이동 → 단차 착지 좌표 1건+) · kill-plane 재배치 · 봇 이동 시나리오(지형 맵)
**수동**: Play 실측 체크리스트(E) + bake idempotent 확인(본인)

---

## 📚 학습 포인트

- **데이터-코드 분리의 실전 비용**: "파일 하나 읽기"가 포맷 버저닝·무결성·배포 경로·실패 처리 책임을 데려옴 — codegen이 공짜로 주던 것들의 가격표
- **prediction-서버 대칭의 가치**: 같은 코드 + 같은 파일이면 지형이 복잡해져도 reconcile은 평지와 동일
- **신뢰 경계와 데이터 배치**: 클라에 주는 파일(terrain)과 안 주는 파일(content)의 분리 기준 = 헌법 #1
- **스폰 좌표도 자산**: 지형이 바뀌면 스폰도 같이 움직여야 함 — 마커 bake가 그 결합을 도구로 강제

---

## ⚠️ 함정 / 주의사항

- **생성기-산출물 drift 확장판**: 씬 수정 → 재bake → **bin 두 벌 + 씬 동반 commit** (PacketGenerator 관례). bake 없이 씬만 commit = 서버/클라 지형 불일치
- **씬 수정 = unity-asset 깃발** — 마커 저작 전 씬 백업 (Phase 08 BackGround 사고 학습)
- 맵 전환 시 클라 지형 갱신 누락 → 이전 맵 지형으로 예측해 드리프트 폭증 (S_MapTransition 핸들러 경로 확인)
- `GameMap` ctor 시그니처 — 테스트 호출부 12+ 파일이라 **오버로드 필수** (Phase 02 선례). 단 `MapSpawnTable` 의존 테스트는 인라인 content로 이행 필요
- Unity StreamingAssets는 에디터/Windows standalone에서 직접 File IO 가능 — 단 `.meta` 생성 주의 (bin과 meta 동반 commit)
- 서버 빌드 후 `Maps/` copy 누락 시 startup hard error가 *의도된 동작* — 봇 csproj도 같은 copy 필요
- **콘솔 경고는 타임스탬프 먼저** (세션8 학습 — stale 로그 헛다리)
- dotnet.exe 잔존으로 GameServer.dll 잠기면 사용자에게 종료 요청 (강제 kill X)

---

## ➡️ 다음 Phase

- Phase 04 — 직업 이동 분리 (같은 Physics.Step 위 작업이라 본 Phase 머지 후 착수)

---

## 📋 박제 (완료 후)

- **대규모 등급** — -DONE.md + 5단계 보고 MD/HTML

---

## 작업 로그

- 2026-06-06: 계획 수립 (`/work:plan M4.4`)
- 2026-06-06 (세션13): **재정의** — 사용자 의논 결정 4건 반영 (바이너리 파이프라인 전환 + bake 범위 확장 + 무결성 헤더/암호화 X + 단독 진행). 등급 복잡 → 대규모 상향. 오케스트레이터 도구는 본인 제작 백로그 분리
- 2026-06-06 (세션13): **plan-auditor 조건부 GO → GO** — P0(kill-plane 정책) 사용자 확정 = 스폰 재배치(HP 무변화) / P1 2건 문서 봉합(마커 Y bake 가드 + 스테이지 C 착수 게이트) / P2 임계 정량화 반영. 구현 착수
