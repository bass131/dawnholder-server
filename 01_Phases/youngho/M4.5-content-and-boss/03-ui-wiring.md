---
owner: youngho
milestone: M4.5
phase: 03
title: UI 연결 — 미니맵 HUD 이동 + 맵 이름 + MP 훅
status: done
grade: 보통
risk: unity-asset
estimated: 1.5~2h
domain: client
---

# Phase 03: UI 연결 — 유현 핸드오프 소화 (확정 범위)

> **상태**: pending
> **마일스톤**: M4.5
> **등급**: 보통 (client 단일 + UI.unity 씬 — unity-asset 인지하되 입자 작음)
> **담당**: client SubAgent + unity-bridge (UI.unity 씬) + 메인 검수

---

## 🎯 목표

유현 핸드오프(`_handoff-ui-art-20260605.html`) 중 **세션18 사용자 확정 범위**를 연결한다: 미니맵을 일시정지 메뉴에서 HUD로 옮겨 상시 표시 + 현재 맵 이름 실시간 갱신, MP 바 normalized 훅 추가. 끝나면 ESC 없이 미니맵 프레임이 보이고 맵 전환 시 이름이 바뀐다.

**범위 밖 (사용자 확정 이월)**: EXP/퀘스트/스킬/다이얼로그/인벤토리 = M5~M6, 미니맵 RenderTexture 카메라 = 이월. HP 실연결은 Phase 05 (S_EnemyAttack 필요).

---

## ⏪ 사전 조건

- [x] 유현 UI.unity 아트 배치 (PR #59/#61)
- [x] `S_MapTransition` 패킷 (M4.2 — 맵 이름 출처)
- (Phase 01/02/04와 의존성 0 — **병렬 가능**)

---

## 📝 작업 내용

- [ ] 미니맵 프레임을 `PauseMenuCanvas > ... > MinimapPlaceholder` → HUD 캔버스로 이동 (unity-bridge, 앵커 재설정 — 유현 가이드 "앵커만 다시 잡으면 됨")
- [ ] `map_name` TMP ← `S_MapTransition` 수신 시 맵 이름 갱신 (MapId → 표시명 매핑, 첫 입장 맵도 초기화) — HudController 또는 소형 전용 컨트롤러 (SRP 판단)
- [ ] `HudController.UpdateMP(int current, int max)` 추가 — UpdateHP와 동일 normalized 패턴(`value = current/max`, 현 width 기준 비율) + 인스펙터 `MpSlider` 연결 + Fill 색 지정(파랑) + M4.5 동안 풀 바(1.0) 초기화
- [ ] 유현 권고 정리 2건(기능 전 정돈): `skill_slot1 (n)` 복제 네이밍 → `skill_slot_1~5`, `entitiy_name` 오타 → `entity_name`
- [ ] UI.unity 씬 백업 후 작업 (`.claude/state/scene-backups/`)

---

## ✅ 완료 조건

- [ ] Play: ESC 없이 미니맵 프레임 상시 표시 + Town→HG→BR 전환마다 맵 이름 갱신
- [ ] MP 바 풀 바(1.0) 표시 + `UpdateMP` 단위 호출 시 비율 반영 (EditMode 또는 Play 확인)
- [ ] 클라 UI 코드에 HP/MP *계산* 로직 0 (표시만 — 헌법 #1)
- [ ] 기존 HUD(HP mock/Gold)·일시정지 메뉴 회귀 0
- [ ] EditMode 테스트 green

---

## 🧪 테스트

**자동**: EditMode — UpdateMP normalized 경계(0, max, max+1 클램프)
**수동**: Play — 미니맵 상시 표시 + 맵 3종 전환 이름 갱신 + ESC 메뉴 회귀

---

## 📚 학습 포인트

- **normalized UI 계약** — 서버는 절대값(current/max)을 주고, 클라는 비율(0~1)로 변환해 *현재 width 안에서* 채움. 아트 크기와 데이터가 분리되는 지점
- **표시 전용 API 설계** — `Update*(server값)` 패턴: UI가 서버 데이터의 종착지일 뿐 출발지가 아니게 만드는 관례 (유현 HudController가 본보기)

---

## ⚠️ 함정 / 주의사항

- **UI.unity = 유현 영역** (ADR-021) — 구조 변경(미니맵 이동/네이밍 정리)은 핸드오프 문서가 위임한 범위 안에서만. 아트 재배치/스타일 변경 금지
- **씬 백업 의무** — UI.unity 수정 전 백업 (unity-asset 깃발)
- 맵 이름을 클라 하드코딩 테이블로 박을 때 MapId enum과 drift 주의 — MapId 옆에 두거나 switch 한 곳으로 봉인
- `Time.time` 금지 (03_Client/CLAUDE.md) — 본 Phase는 타이밍 코드 없음이 정상

---

## ➡️ 다음 Phase

- Phase 04 — 보스 프로토콜 + 서버 행동 (본 Phase와 병렬 가능)

---

## 📋 박제 (완료 후)

- **보통 등급** — -DONE.md 없음, work-pin + commit message로 박제

---

## 작업 로그

- 2026-06-07: 계획 수립 (`/work:plan M4.5`, 세션18 — UI 범위 사용자 의논 4항목 확정 반영)
- 2026-06-07: 완료 (세션20) — **미니맵은 실측 결과 이미 HUD 직속** (정의의 "PauseMenu→HUD 이동" 전제 stale, 이동 0건 + Play 상시 표시 확인). UpdateMP + MapIdToDisplayName(영어, SceneRouter 한 파일 봉인) + MapNameDisplay 신규(static 저장 + Start 복원 — UI 씬 재로드 생존) + 핸들러 훅 2곳. 씬 = _mpSlider 연결 + map_name 폰트 Pretendard 할당(NULL이었음, 기능 필수) + skill_slot_1~5/entity_name 리네임. 예정 외 = Tests.EditMode asmdef에 Unity.TextMeshPro 참조 추가(TMPro 미해결 → 컴파일 실패 → 도메인 리로드 차단 봉합). 검증 = EditMode 75/75(신규 12) + Play 4항목 + reviewer 🔴0/🟡1(null label 분기 테스트 — 이월)
