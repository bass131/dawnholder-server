---
owner: youngho
milestone: M4.15
phase: 01
title: Baseline 회귀 그린 + 시작값 표 영호 확정
status: done
grade: 단순
domain: qa
summary: 변경 전 WSL2 회귀 green 스냅샷 + 박스/투사체 시작값 영호 승인 게이트
---

# Phase 01: Baseline 회귀 그린 + 시작값 표 영호 확정

> **상태**: done (2026-06-14, commit `5beda1f`)
> **마일스톤**: M4.15
> **등급**: 단순
> **담당**: qa (메인 세션 직접 가능)

---

## 🎯 목표

코드 변경 *전* 안전망을 확보한다: WSL2(ADR-029) 전체 회귀가 green인 baseline 숫자를 박제하고(이후 Phase의 "회귀 0" 비교 기준), 박스 X/Y·투사체 속도의 **시작값 표를 영호가 승인**해 Phase 02~05의 정량 목표를 고정한다.

---

## ⏪ 사전 조건

- [ ] `feature/m4.15-energybolt-skill-range` 브랜치 (main 동기 후 분기 — 완료).
- [ ] ADR-029 WSL2 실행 환경(`~/dawnholder-poc` rsync) 가용.

---

## 📝 작업 내용

- [ ] WSL2 `dotnet build Dawnholder.slnx` 0/0 확인.
- [ ] WSL2 `dotnet test` green 카운트 박제 (M4.14 마감 = 서버 **568/0/5** 기대).
- [ ] Unity 컴파일 0err + EditMode 카운트 박제 (M4.14 = **147**) — 메인 세션 MCP.
- [ ] 현재 거동 수치 박제: Mage 박스 정사각 ±8, freeze 0.5~0.9s, 투사체 속도 곡선(거리>20 폭증).
- [ ] **시작값 표 영호 확정 게이트** (아래 표 — 영호가 Play 보며 조절/승인). 영호가 Unity 맵 실제 발판 Δ 알려주면 Y 정밀화.

### 시작값 표 (영호 승인 대상 — 구조만 코드, 숫자는 영호)

| 항목 | 현재 | 시작 제안 | 비고 |
|---|---|---|---|
| Mage 평타 박스 | 정사각 ±8 | X ±10~12 / **Y ±1.0~1.5** | Y 대폭 ↓, X는 사거리 |
| Knight 평타 박스 | 정사각 ±1.5 | X ±1.5~2.0 / Y ±1.0 | 약간 납작 |
| Thunderbolt 박스 | X13 / Y3 | X 유지 / **Y ±1.5~2.0** | 광역 가로 유지, 세로 ↓ |
| Dash 박스 | X2.5 / Y1.5 | 유지 / Y ±1.0~1.5 | 거의 OK |
| 투사체 속도 | 가변(폭증) | 고정 ~40 u/s (2.0 u/tick) | 일정 속도 |

---

## ✅ 완료 조건

- [ ] WSL2 build 0/0 + test green 카운트 박제 (숫자 명시).
- [ ] Unity 컴파일 0err + EditMode 카운트 박제.
- [ ] 시작값 표 영호 승인 (또는 "구조 먼저, Play 튜닝 후속" 명시 결정) — work-pin 박제.

---

## 🧪 테스트

**자동**: WSL2 full `dotnet test` (회귀 기준선).
**수동**: 없음 (측정 Phase).

---

## 📚 학습 포인트

- **baseline-first 정신** — 거동 바꾸기 전에 green 스냅샷 박으면 이후 회귀가 "내가 깬 건지 원래 그랬는지" 즉시 판정 가능 (carry-over: 봇 연속 FAIL ≠ 회귀).
- **숫자 vs 구조 분리** — AI는 *구조*(X/Y 분리)를 깔고, 플레이 *느낌* 숫자는 게임 디자이너(영호)가 Play로 튜닝. 역할 경계.

---

## ⚠️ 함정 / 주의사항

- WSL2 빌드 신선도 = Managed DLL mtime 확인 (carry-over). `git checkout`으로 Shared.dll 비결정 재복사 복원 가능.
- 시작값을 AI가 *감으로 박지 않기* — 영호 승인 게이트 필수 (헌법: 핵심 공식/수치 추측 금지).

---

## ➡️ 다음 Phase

- Phase 02 — 히트박스 X/Y 분리 + 전 스킬 Y 재튜닝.

---

## 📋 박제 (완료 후)

- 단순 등급 → work-pin + commit message만 (`-DONE.md` 불요). baseline 숫자 + 시작값 승인은 work-pin에 박음.

---

## 작업 로그

- 2026-06-14: 생성.
- 2026-06-14: 완료. WSL2 build 0/0 + test **568/0/5**(M4.14 마감값 정확 일치 — 브랜치 깨끗) + EditMode 147 inherited(클라 무변경). 시작값 표 영호 승인("그렇게 진행하자"): Mage X=11/Y=1.0, Knight X=1.5/Y=1.0, Thunderbolt X=13/Y=1.5, Dash X=2.5/Y=1.0, 투사체속도 2.0u/tick 유지(+P04 상한제거). 숫자는 Play 튜닝 후속. commit `5beda1f`.
