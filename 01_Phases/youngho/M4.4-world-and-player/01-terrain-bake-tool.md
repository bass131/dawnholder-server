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

- [ ] **본인+유현 레이어 약속 확정 (첫 블로커 — plan-auditor 🟡 승격)**: 솔리드 지형 Tilemap과 one-way 플랫폼 Tilemap을 *어떻게 구분*하는지 (별도 Tilemap 오브젝트 이름 규칙 추천 — 예: `Tilemap_Solid` / `Tilemap_OneWay`. 현재 씬은 단일 "Tilemap"이라 **분리 씬 작업이 본 Phase 착수 전 선행** — 본인/유현)
- [x] 유현 타일맵 스테이지 세 씬 박힘 (PR #59)
- [ ] M4.3 Phase 12 마감 (현 브랜치 main 머지 — 깨끗한 출발점)

---

## 📝 작업 내용

- [ ] `03_Client/Assets/Editor/TerrainBaker.cs` 신설 — 메뉴 아이템 (예: `Tools/Dawnholder/Bake Terrain`)
- [ ] 씬별 Grid/Tilemap 탐색 → `Tilemap.cellBounds` 순회로 솔리드 셀 추출 (cell → world 좌표 변환: `Grid.CellToWorld` + cellSize/origin 반영)
- [ ] 같은 행 연속 셀 → AABB로 병합 (row-merge — 데이터 양 축소)
- [ ] one-way 플랫폼 레이어 분리 추출 (위 레이어 약속 기반)
- [ ] 생성 출력: `98_Shared/GameData/Generated/MapTerrainData.cs` — MapId → 솔리드 AABB 배열 + one-way 세그먼트 배열 (static readonly, 파일 IO/JSON 의존 0 — DLL에 박힘)
- [ ] 생성 파일 헤더에 "generated — 손편집 금지 + bake 재실행 절차" 명시
- [ ] `dotnet build Dawnholder.slnx` 통과 (Shared.dll 재빌드 → 클라 Plugins 자동 복사 확인)
- [ ] bake 재실행 idempotent 확인 (같은 씬 → 같은 출력, diff 0)

---

## ✅ 완료 조건

- [ ] 세 씬 bake 성공 + 생성 .cs 컴파일 green
- [ ] 샘플 검증: 한 씬에서 수동으로 센 솔리드 영역 수/경계 좌표와 생성 데이터 일치 (스팟 체크 3곳+)
- [ ] bake 2회 연속 실행 시 생성 파일 diff 0
- [ ] one-way 레이어 약속이 문서(본 Phase 작업 로그)에 박힘

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
