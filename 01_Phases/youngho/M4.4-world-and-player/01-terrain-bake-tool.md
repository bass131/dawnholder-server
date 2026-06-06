---
owner: youngho
milestone: M4.4
phase: 01
title: 지형 bake 도구 — TerrainBaker 에디터 스크립트 + 98_Shared 생성 데이터
status: pending
grade: 복잡
estimated: 2~4h
domain: client+shared
summary: Unity 타일맵 솔리드 셀을 읽어 98_Shared 생성 C# 지형 데이터로 출력하는 에디터 bake 파이프라인
---

# Phase 01: 지형 bake 도구

> **상태**: pending
> **마일스톤**: M4.4
> **등급**: 복잡 (2 도메인 — 클라 에디터 도구 + shared 생성 산출물)
> **담당**: 메인 세션 직접 (에디터 스크립트 = Unity API + 코드젠 — client/shared 경계라 SubAgent 분담 비효율) + 본인 (레이어 약속 확인)

---

## 🎯 목표

에디터 메뉴 한 번으로 세 플레이 씬(Town/HuntingGround/BossRoom)의 타일맵 솔리드 셀이 `98_Shared/GameData/Generated/` 생성 C# 데이터로 박히고, 솔루션이 컴파일된다. Unity = 저작 도구, 98_Shared = 단일 진실 (헌법 #1·#4).

---

## ⏪ 사전 조건

- [x] **본인+유현 레이어 약속 확정 (첫 블로커 — plan-auditor 🟡 승격)**: 이름 규칙 **`Tilemap_Solid`(바닥·벽) / `Tilemap_Platform`(one-way 발판)** 확정 (2026-06-06 — 작업 로그 참조). 씬 분리(기존 "Tilemap" rename + 빈 Platform 레이어 추가)는 본인이 에디터에서 진행
- [x] 유현 타일맵 스테이지 세 씬 박힘 (PR #59)
- [x] M4.3 Phase 12 마감 (PR #62 → main `954e028`)

---

## 📝 작업 내용

- [x] `03_Client/Assets/Editor/TerrainBaker.cs` 신설 — 메뉴 `Tools/Dawnholder/Bake Terrain`
- [x] 씬별 Grid/Tilemap 탐색 → `Tilemap.cellBounds` 순회로 솔리드 셀 추출 (`Tilemap.CellToWorld` 절대좌표)
- [x] 같은 행 연속 셀 → AABB 병합 + 동일 x-range 연속 행 수직 병합 (Town 531타일 → AABB 3개)
- [x] one-way 발판 레이어(`Tilemap_Platform`) 분리 추출 — 윗면 세그먼트만 (현재 미저작 = 빈 배열 정상)
- [x] 생성 출력: `98_Shared/GameData/Generated/MapTerrainData.cs` + 손코딩 구조체 `98_Shared/GameData/Terrain.cs` (TerrainAabb/TerrainPlatform)
- [x] 생성 파일 헤더에 "generated — 손편집 금지 + bake 재실행 절차" 명시
- [x] `dotnet build Dawnholder.slnx` 통과 (Shared.dll 재빌드 → Plugins 복사 13:34 확인)
- [x] bake 재실행 idempotent 확인 (2회 연속 diff 0)

---

## ✅ 완료 조건

- [x] 세 씬 bake 성공 + 생성 .cs 컴파일 green (솔리드 AABB 24개: Town 3 / HuntingGround 12 / BossRoom 9)
- [x] 샘플 검증: Town 경계 스팟 체크 8곳 PASS (AABB 안쪽 셀=타일 有 / 바깥 셀=無, CellToWorld(0,0)=(0,0))
- [x] bake 2회 연속 실행 시 생성 파일 diff 0
- [x] one-way 레이어 약속이 문서(본 Phase 작업 로그)에 박힘 + sanity 테스트 13건 green (전체 362/0/4skip)

---

## 🧪 테스트

**자동**: 생성 데이터 sanity 단위 테스트 (맵별 AABB 수 > 0, 좌표 범위 정상)
**수동**: 에디터에서 bake 실행 → 콘솔 요약(맵별 AABB/세그먼트 수) → 씬 시각과 대조

---

## 📚 학습 포인트

- **저작 도구 vs 런타임 진실 분리** — 클라 씬 데이터를 서버가 직접 읽지 않고, 추출물을 공유 코드로 박는 이유 (헌법 #1: 서버가 Unity에 의존하면 안 됨)
- **코드젠 파이프라인** — PacketGenerator와 같은 패턴: 생성기 실행 → 산출물 diff 확인 → 동반 커밋 (생성기-산출물 drift 함정)
- Unity `Tilemap`/`Grid` 좌표계 (cell ↔ world)

---

## ⚠️ 함정 / 주의사항

- **생성물 동반 커밋 의무** — bake 결과 .cs + Shared.dll, 누락 시 다른 머신 pull 회귀 (PacketGenerator 사고 패턴 동일)
- Grid 오브젝트가 원점이 아닐 수 있음 — `CellToWorld` 절대좌표 사용, 씬별 offset 가정 금지
- 셀 단위 AABB 폭증 주의 — row-merge 필수 (수천 셀 → 수십~수백 AABB)
- 데모씬(Cainos SC Demo)은 bake 대상 제외 — 플레이 씬 3개만

---

## ➡️ 다음 Phase

- Phase 02 — 지형 물리 (생성 데이터를 Physics.Step이 소비)

---

## 📋 박제 (완료 후)

- **복잡 등급** — -DONE.md 박음

---

## 작업 로그

- 2026-06-06: 계획 수립 (`/work:plan M4.4`)
- 2026-06-06 (세션12, 구현): 본인 레벨 디자인 보강(타일 531/855/847) → 씬 분리는 Unity MCP RunCommand로 AI 실행(백업 `.claude/backup/m4.4-01-scene-split/` 선행, 타일 유실 0 확인) → TerrainBaker + Terrain.cs 구현 → 1차 bake(AABB 24개) → 빌드 green → 2차 bake diff 0 → Town 경계 스팟 체크 8/8 PASS → sanity 테스트 13건 green (전체 362/0/4skip). BossRoom.unity는 본인 배경 WIP 포함 commit (drift 0 우선, 사용자 GO).
- 2026-06-06 (세션12): **레이어 약속 확정 — `Tilemap_Solid` / `Tilemap_Platform`** (같은 Grid 아래 Tilemap 오브젝트 이름 규칙. baker는 이 두 이름만 읽고 그 외 타일맵은 무시 = 장식 레이어 자유). 후보였던 `Interact`는 M4.4-03 "상호작용 지형"(포탈 등 별개 개념)과 이름 충돌이라 기각, one-way 발판의 업계 통칭 Platform 채택. 실측: 세 씬 모두 현재 Grid 1 + 단일 "Tilemap"(Town 356 / HuntingGround 221 / BossRoom 309 타일) — 분리는 본인이 에디터에서 진행(BossRoom은 본인 배경 작업 중 dirty라 AI 씬 편집 회피). 유현에게 Discord 공지 시 본 약속 포함.
