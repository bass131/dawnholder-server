---
owner: youngho
milestone: M4.4
phase: 01
title: 지형 bake 도구 — TerrainBaker + 98_Shared 생성 지형 데이터
status: done
completed: 2026-06-06
grade: 복잡
summary: M4.4 Phase 01 완료 (commit 7fd8dc0). 레이어 약속 Tilemap_Solid/Tilemap_Platform 확정(Interact 후보는 상호작용 지형과 충돌이라 기각) → 씬 3개 분리(Unity MCP, 타일 유실 0) → TerrainBaker 에디터 도구(행 run+수직 병합, CellToWorld, R+InvariantCulture) → MapTerrainData.cs 생성(솔리드 AABB 24개) + Terrain.cs 구조체 + Shared.dll 동반 commit. 검증 = 빌드 0/0 + 2차 bake diff 0 + Town 경계 스팟 8/8 + sanity 테스트 13건(전체 362/0/4skip). reviewer 🔴0/🟡2(Phase 02 소비 시점 자연 봉합).
---

# Phase 01 박제: 지형 bake 도구

**소요**: 세션12 후반 (M4.3 마감 직후 연속 진입)

## TL;DR

Unity 씬에 그려진 타일맵(저작물)을 서버·클라가 공유하는 충돌 데이터(단일 진실)로 변환하는 bake 파이프라인을 박았다. 본인이 레벨 디자인을 보강(타일 531/855/847)하고, AI가 씬 분리(기존 "Tilemap"→`Tilemap_Solid` rename + 빈 `Tilemap_Platform` 추가)를 Unity MCP RunCommand로 실행, TerrainBaker가 세 씬을 열어 셀을 긁어 행 run + 동일 x-range 수직 병합으로 AABB를 만들고 `98_Shared/GameData/Generated/MapTerrainData.cs`로 출력한다 (Town 531타일 → AABB 3개). PacketGenerator와 같은 generated-artifact 패턴 — 생성 .cs + Shared.dll 동반 commit, bake 2회 diff 0(idempotent).

## 박제 사실

- **레이어 약속 (본인+유현)**: `Tilemap_Solid`(바닥·벽) / `Tilemap_Platform`(one-way 발판). baker는 이 두 이름만 읽음 — 그 외 타일맵(장식)은 자유. 사용자 제안 "Interact"는 M4.4-03 "상호작용 지형"(포탈 등 별개 개념)과 이름 충돌이라 의논 후 기각, 업계 통칭 Platform 채택. 유현 Discord 공지 의무에 포함.
- **씬 분리**: 세 씬 백업(`.claude/backup/m4.4-01-scene-split/`) 선행 → MCP RunCommand(에디터 API)로 rename+추가+저장 → 디스크 grep 재검증(이름 2종 각 1, 타일 수 보존 531/855/847). Phase 08 YAML 손편집 사고 학습 정합.
- **구현**: `03_Client/Assets/Editor/TerrainBaker.cs`(메뉴 Tools/Dawnholder/Bake Terrain) + `98_Shared/GameData/Terrain.cs`(TerrainAabb/TerrainPlatform readonly struct) + 생성 산출물 `MapTerrainData.cs`(GetSolids/GetPlatforms(int mapId) switch). 발판은 윗면 세그먼트만(두께 없는 착지 면).
- **commit**: `7fd8dc0` (11파일 — 씬 3 + 코드 3 + 생성 1 + 테스트 1 + 문서 1 + Shared.dll). BossRoom.unity는 본인 배경 WIP 포함 commit (생성 데이터↔씬 drift 0 우선, 사용자 GO).

## AC 검증 결과

- `dotnet build Dawnholder.slnx --no-incremental` → 경고 0 / 오류 0
- 1차 bake: Town(mapId 0) solids=3 / HuntingGround(1) solids=12 / BossRoom(2) solids=9, platforms 전부 0(미저작 정상)
- 2차 bake → `diff` 결과 0줄 (idempotent — 정렬 순회 + "R"+InvariantCulture)
- Town 경계 스팟 체크 8/8 PASS: AABB 안쪽 마지막 셀=타일 有 / 바로 바깥=無 (off-by-one 0), `CellToWorld(0,0)=(0,0)` 원점·cellSize 확인
- `MapTerrainDataTests` 13건 green (불변식만 — Min<Max, sane bounds, unknown map empty). 전체 suite **362 통과 / 0 실패 / 4 skip**
- Shared.dll → `03_Client/Assets/Plugins/Shared/` 자동 복사 확인 (13:34)
- reviewer Tier 2-A: **🔴 0 / 🟡 2** — ① mapId↔MapId enum 정합이 주석 약속(Phase 02에서 `GetSolids((int)MapId)` 캐스팅 경유로 코드화) ② enum-키 정합 어서션 부재(①의 안전망으로 Phase 02에 1건 추가 권고)

## 결정 흐름 (회고 참고용)

- **이름 규칙 vs 타일 종류 구분** — 타일 에셋 목록 기반 구분은 baker가 에셋을 알아야 해서 깨지기 쉬움. 오브젝트 이름 규칙은 구현 단순 + 에디터 시각 확인 쉬움, 비용은 저작 시 레이어 선택 주의.
- **"안 헷갈리게" 이름이 오히려 미래 충돌** — Interact 후보는 마일스톤 내 예약 개념(상호작용 지형)과 부딪힘. 이름 결정은 현재 직관 + *계획된 미래 개념*까지 대조.
- **씬 편집은 MCP 에디터 API + 백업 선행** — YAML 손편집 회피(세션11 학습 재적용), idempotent 가드(이미 분리된 씬 통과) 포함.
- **생성 데이터 키 = int (enum 비참조)** — MapId enum은 서버 소유, Shared가 역참조하면 의존 방향이 뒤집힘. 주석 약속 + Phase 02 캐스팅 소비로 정합 강제 (reviewer 정당 판정).
- **BossRoom WIP 포함 commit** — 씬↔생성 데이터 drift 0이 본인 커밋 타이밍 자유보다 우선 (생성기-산출물 한 묶음 원칙).

## 막혔던 지점 / 이월

- MCP GetConsoleLogs가 bake 로그 대신 옛 컴파일 경고만 반환(정렬 이슈 추정) → 디스크 실물 검증으로 우회 (memory `mcp-unity-console-empty-diagnosis` 동형).
- **Phase 02 이월**: ① `MapTerrain` 조회 타입 + Physics.Step 소비 ② `GetSolids((int)MapId)` 캐스팅 경유 + enum-키 정합 어서션 1건 (reviewer 🟡 2 봉합) ③ 발판 실제 저작(본인) 후 재bake.

## 학습 일지 후보 키워드

저작 도구 vs 런타임 진실 분리(헌법 #1) / generated-artifact 동반 commit 패턴(PacketGenerator 동형) / 빌드타임 코드 생성은 "클라→Shared 쓰기 금지"의 예외인 이유(런타임/빌드타임 경계) / 이름 결정 시 예약 개념 대조 / row-merge+수직 병합 사각형 분해

## 다음 Phase

- **Phase 02 — 지형 물리** (Physics.Step 솔리드 AABB + one-way, 기술 난도 최고 — 머지 전 /cross-review 권장)
