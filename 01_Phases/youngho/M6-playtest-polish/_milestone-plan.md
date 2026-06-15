---
owner: youngho
milestone: M6
title: 플레이테스트 폴리시 (UI 재배치 / Animator ExitTime / 렌더 소팅 / NPC 대화패널)
status: pending
created: 2026-06-15
---

# M6 — 플레이테스트 폴리시

## 배경

M5(파티/퀘스트) main 머지(#111, ProtocolVersion v16) 직후 인터랙티브 2차 플레이테스트에서
영호가 직접 발견한 폴리시 피드백 6건을 한 마일스톤으로 묶음. (7번째 = 전면 사운드 적용은
워낙 커서 **M7-sound 마일스톤으로 분리** — 별도 plan.)

이 마일스톤은 *신규 기능이 아니라 기존 기능의 체감 품질 보정*이 핵심.
대부분 Unity 에셋(Animator·SortingLayer·Scene) 비중이 높아 **영호 육안 검증이 게이트**.

## 피드백 → Phase 매핑

| 피드백 (2차 플레이테스트) | Phase | 비중 |
| --- | --- | --- |
| C) 보스 피격 복귀 후 공격이 너무 빨리 끝남 | 01 | Unity 에셋 80% |
| D) 일반 몹이 공격 모션 끝나기 전에 IDLE 복귀 | 01 | Unity 에셋 85% |
| E) 렌더 소팅 (BG→마을지형→세부지형→NPC→Player→UI) | 02 | Unity 에셋 80% |
| A) 파티 멤버 HUD를 퀘스트 mockup 위치로 재배치 (mockup 제거) | 03 | 혼합 50% |
| B) 퀘스트 이름+목표 텍스트 + 뒷 패널 확장 | 03 | 코드 80% |
| F) NPC 대화 패널 재구축 (Menu_Button.png 배경 + 초상화 + 텍스트) | 04 | 코드 70% |

## Phase 순서 (5개)

1. **Phase 01 — Animator ExitTime 보정** (등급: 복잡, risk: unity-asset, 담당: client+영호 육안)
   - 끝나면: 보스가 피격 복귀 후 공격 모션을 끝까지 재생, 일반몹이 공격 모션 완주 후 IDLE 복귀
2. **Phase 02 — 렌더 소팅 레이어 정립** (등급: 복잡, risk: unity-asset, 담당: client+영호 Scene)
   - 끝나면: BG→마을지형→세부지형→NPC→Player→UI 순서로 항상 올바르게 겹침
3. **Phase 03 — 상단 HUD 재정비** (등급: 복잡, 담당: client+영호 육안)
   - 끝나면: 파티 HUD가 퀘스트 위치로 이동(mockup 제거) + 퀘스트 이름/목표/카운트 3줄 표시
4. **Phase 04 — NPC 대화 패널 재구축** (등급: 복잡, risk: unity-asset, 담당: client+영호)
   - 끝나면: E키 상호작용 시 Menu_Button.png 배경 + NPC 초상화 + 대사가 런타임 생성되어 표시
5. **Phase 05 — 통합 플레이테스트 + 클로즈아웃** (등급: 보통, 담당: cross+영호 육안)
   - 끝나면: 6개 항목 통합 검증 + WSL2 회귀 게이트 green + -DONE.md/HTML 박제

## 의존성 그래프

```
01 (Animator)  ─┐
02 (Sorting)   ─┼─····→ 03·04 (명세 의존: SortingLayer 이름 상수)
02 (Sorting)   ─┤
03 (HUD)       ─┼─→ 05 (통합 테스트 + 클로즈아웃)
04 (Dialog)    ─┘
```

- **01·02·03·04는 작업 파일군 독립** → 병렬 가능.
- **02 → 03·04 점선 명세 의존** (Opus plan-auditor 지적): Phase 02가 박는 SortingLayer 이름 상수(예: `RenderLayers.cs`)를 Phase 03·04 BuildRuntime UI가 *알아야* 정합. UI Canvas는 ScreenSpace-Overlay라 월드 SortingLayer와 별개 체계이지만, 이름 상수 위치는 공유. 실제 충돌 없는 약한 의존이라 병렬 진행 가능하나 03·04 진입 전 02의 상수 위치 합의 필요.
- **05는 01~04 전부 완료 후**.

## 설계 분기 (Phase 진입 시 영호 확인)

- **Phase 01·02 분담**: Animator·SortingLayer는 Unity 에셋 영역 → AI가 MCP로 1차 조정 vs 영호 직접.
  메모리 정책(Unity 외관 = 먼저 분담 물어봄)대로 Phase 시작 시 질문.
- **Phase 03 — 퀘스트 이름/목표 출처**: 클라 로컬 content(권장, 프로토콜 변경 X) vs 서버 패킷 확장(버전 bump).
  S_QuestUpdate가 현재 count 2개만 + 퀘스트 ID 미포함 + 단일 퀘스트 → 클라 로컬이 정합.

## 이번 마일스톤 핵심 개념 (학부생 시각)

- Unity Animator State Machine의 Exit Time / Has Exit Time / Transition Duration의 의미
- SortingLayer vs Order in Layer — 2D 렌더 순서 결정 2축
- BuildRuntime 패턴(코드로 UI 생성) vs Scene 수동 배치 — NpcDialogPanel이 후자라 NRE 났던 원인
- 표시용 콘텐츠(퀘스트 이름)는 왜 서버 권위 대상이 아닌가 (헌법 #1 경계)

## 위험 깃발

- **unity-asset**: Phase 01(Animator), 02(prefab/scene + TagManager), 04(prefab/scene) — 등급 자동 상향 반영됨.
- **비가역 없음**: 프로토콜 bump 회피(Phase 03 클라 로컬 선택 시). DB 마이그 없음.
