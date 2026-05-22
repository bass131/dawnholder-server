---
owner: youngho
milestone: M3.6
phase: 05
title: 클라 코드 전수조사 (03_Client/ + 04_ClientNet/)
status: done
grade: 복잡
estimated: 4~6h
domain: client
---

# Phase 05: 클라 코드 전수조사 (03_Client/ + 04_ClientNet/)

> **상태**: pending
> **마일스톤**: M3.6
> **등급**: 복잡 (2 도메인 — client + clientnet / ~100~200줄 점검 / 일부 비가역 — 유현 영역 경계 보고)
> **담당**: client Worker + reviewer + unity-bridge 자문 (변경 X)

---

## 🎯 목표

**M3 응급 데모 직후 클라 측 전 코드** (Unity 씬/렌더링/입력/UI/prediction/reconciliation/네트워킹 wrapper) 헌법 #1 (서버 권위) 정합 + ADR-021 (UI Scene 분리) 정합 + 유현 영역 경계 점검. **변경은 client Worker 영역만, 유현 영역(Bootstrap/Scripts/UI/Scenes/UI.unity)은 보고만**.

**유현 영역 변경은 M3.5 마감 후 재논의 시점** — 본 Phase는 *재논의 자료 박제*까지가 책임.

---

## ⏪ 사전 조건

- [ ] Phase 02 (헌법/ADR/policies 정합 감사) 완료
- [ ] Phase 03 (하네스 v1 실측 재조정) 완료

---

## 📝 작업 내용

### client Worker — 본인 영역 점검
- [ ] **Network/** — UnityClientSession / MainThreadDispatcher / NetworkBootstrap 정합 (ADR-012 Y2 + ADR-021 정합)
- [ ] **Prediction/** — M2 Phase 06 input replay reconcile 정합 / Phase 07 점프 prediction (Y-axis mispredict 후속 봉합 후보 = M4 backlog 정합 확인)
- [ ] **Rendering/** — sprite bottom pivot foot alignment (M3 Phase 08 학습 정합)
- [ ] **Input/** — 입력 → intent packet + HandshakeOk gate (M3 Phase 02 race window 차단)
- [ ] **State/** — RemoteEntityRegistry (M3 Phase 05 client-remote-entity-registry) 정합 / local-vs-remote entity branch 정합
- [ ] **Prefabs/Characters/** — LocalPlayer / RemotePlayer / PlayerBase variant 체인 (M3 Phase 08a 정합)

### 04_ClientNet/ — Y2 socket 라이브러리
- [ ] Connector / ClientSession / RecvBuffer / SendBuffer (.NET Standard 2.1 정합)
- [ ] DLL 자동 복사 파이프라인 (ADR-010 + ADR-012) 정합 확인

### 유현 영역 경계 점검 (보고만, 변경 X)
- [ ] **Bootstrap/SceneBootstrap.cs** — ADR-021 additive load 패턴 정합
- [ ] **Scripts/UI/** — HudController + TMP_Text + Korean font asset (yuhyeon M2 Phase 06) 정합
- [ ] **Scenes/UI.unity** — YAML 보지 않고 commit history만 확인 (prefab 사고 학습 정합, BackGround 사건)
- [ ] **CODEOWNERS** — 3 경로 (Scenes/UI.unity, Scripts/Bootstrap/, Scripts/UI/) @jungyoohyun0105 단독 정합

### 유현 영역 재논의 자료 박제
- [ ] 발견된 영역 경계 모호 케이스 리스트 (예: 클라 prediction이 UI 영역 침범하는 케이스)
- [ ] 별 시점 본인 + 유현 의논 권유 자료 박음 (변경 X)

### unity-bridge 자문 (변경 X)
- [ ] prefab/scene/asset 영역 점검 시 *읽기 자문*만 (M3.6은 unity-asset 변경 X)
- [ ] BackGround prefab 사고 학습 (`prefab-overwrite-untracked-disaster`) 재발 차단 정합 확인

### reviewer 자동 호출 (Tier 2-A)
- [ ] 5축 점검 (헌법 / ADR / ARCHITECTURE / 테스트 / 도메인 패턴)
- [ ] 발견 사항 P0/P1/P2 분류

---

## ✅ 완료 조건

- [ ] client Worker 점검 결과 박힘 (본인 영역)
- [ ] 04_ClientNet/ Y2 정합 확인
- [ ] 유현 영역 경계 보고 자료 박힘 (변경 0건, 재논의 자료만)
- [ ] reviewer 5축 점검 결과 박힘 (P0/P1/P2 분류)
- [ ] P0 발견 0건 또는 본 Phase 즉시 봉합 commit 박힘
- [ ] `dotnet build` green 유지 (04_ClientNet/ 영역)
- [ ] `-DONE.md` 박음 (복잡 등급)

---

## 🧪 테스트

**자동**:
- 04_ClientNet 빌드 (.NET Standard 2.1) green
- DLL 자동 복사 파이프라인 작동 확인
- **Unity batchmode compile required** (β3 봉합) — `Unity.exe -batchmode -nographics -quit -projectPath 03_Client/ -executeMethod ...` 명령 실행. *dotnet build로 안 덮이는 Unity scope* 검증 (Unity 스크립트 컴파일 / asmdef 참조 / EditMode 테스트). 미실행 시 *Phase 06 종합 보고에 "Unity 미검증 리스크" 명시 의무*
- **EditMode test 실행 검토** — `03_Client/Assets/Tests/EditMode/Dawnholder.Client.Tests.EditMode.asmdef` 실재. Unity CLI test runner 호출 가능 시 실측, 본 머신 환경 차단 시 Cloud Codex 위탁 또는 별 환경 위탁

**수동**:
- Unity 에디터에서 03_Client/ 열어 컴파일 오류 0건 확인 (batchmode 실패 시 fallback)
- M3 데모 시나리오 1회 수동 실행 (옵션, 별 시점)

---

## 📚 학습 포인트

- **영역 경계 점검의 정합** — 유현 영역 *변경 X 보고만* 패턴 = 한국 게임 회사 *팀 작업 경계* 어필. CODEOWNERS + 본 점검 정합
- **유현 영역 재논의 자료 박제** — *변경 결정*은 본인 + 유현 의논 시점. 본 Phase는 *근거 박제*까지 = 비대칭 의사결정 정합
- **unity-bridge 읽기 자문 패턴** — Specialist SubAgent의 *읽기만* 활용. prefab 사고 차단 정신 정합

---

## ⚠️ 함정 / 주의사항

- **유현 영역 *절대 변경 X*** — Bootstrap/ / UI/ / UI.unity 변경 시도 = CODEOWNERS 차단 + 본 Phase 정합 위반. 보고만
- **prefab/scene 변경 X** — BackGround 사고 학습 정합. M3.6에서 unity-asset 깃발 발동 = 본 Phase 등급 자동 상향 위험. 점검은 git history + 정의 파일까지만
- **04_ClientNet/ 변경 시 Y2 정합 의무** — 한쪽 변경이 다른 쪽 빌드 안 깸 정신. 본 Phase는 점검 위주라 변경 최소

---

## ➡️ 다음 Phase

- Phase 06 (외부 리뷰 4건 흡수 + 종합 마감) — Phase 04 + 05 둘 다 끝나야 진입

---

## 📋 박제 (완료 후)

- 등급 복잡 → **`-DONE.md` 박음**
- 5단계 보고 X (복잡 등급 면제)
- 유현 영역 재논의 자료 = 별 `_yuhyeon-area-review-2026-MM-DD.md` 박음 (Phase 06 종합 보고에서 인용)
- 학습 키워드 후보:
  - `region-boundary-audit-without-change` (변경 X 보고만 패턴)
  - `asymmetric-decision-evidence-staging` (재논의 자료 박제 정합)
