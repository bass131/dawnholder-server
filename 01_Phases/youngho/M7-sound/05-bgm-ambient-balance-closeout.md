---
owner: youngho
milestone: M7
phase: 05
title: BGM/Ambient + 볼륨 밸런싱 + 클로즈아웃
status: pending
grade: 복잡
risk: irreversible
estimated: 3~5h
domain: client
summary: 마을/전투/보스 BGM 전환 + ambient + 볼륨 밸런싱 + 설정 + -DONE 박제 + (영호 GO) 머지
---

# Phase 05: BGM/Ambient + 볼륨 밸런싱 + 클로즈아웃

> **상태**: pending
> **마일스톤**: M7
> **등급**: 복잡 — irreversible 깃발은 영호 GO 게이트로 흡수. 도메인은 client 단일(서버/프로토콜 무변경)이고 변경 표면적이 좁아 *대규모* 미상향 (M5 closeout이 대규모였던 이유는 3+ 도메인 + 8패킷 + ProtocolVersion bump). Opus plan-auditor 권고대로 등급 근거 명시.
> **담당**: client + 영호 청음

---

## 🎯 목표

상황별 BGM(마을/전투/보스/메뉴)이 부드럽게 전환되고 ambient가 깔리며,
전체 볼륨 밸런스를 맞춘 뒤 M7을 박제하고 영호 GO 시 main 머지한다.

---

## ⏪ 사전 조건

- [ ] Phase 04 — SFX wiring 완료

---

## 📝 작업 내용

- [ ] 상황별 BGM 전환 연결: 마을 / 일반 전투 / 보스전 / 메인메뉴 (크로스페이드)
- [ ] ambient(마을 환경음 등) 적용 (선택 항목)
- [ ] 볼륨 밸런싱: SFX vs BGM 상대 음량, 마스터 기본값 조정 (영호 청음)
- [ ] (선택) 설정 UI에서 볼륨 슬라이더 노출
- [ ] 전체 사운드 통합 청음 (전투→보스→마을 순회)
- [ ] WSL2 회귀 게이트 (baseline 644/0/5)
- [ ] `_milestone-DONE.md` + HTML 박제
- [ ] **영호 명시 GO 후**: push → PR → (코드 PR이면 admin 예외) → main 머지
- [ ] 머지 후 work-pin 다음 마일스톤 전환

---

## ✅ 완료 조건

- [ ] 상황별 BGM이 의도대로 전환됨 (영호 청음)
- [ ] 볼륨 밸런스 OK (SFX가 BGM에 묻히거나 과하지 않음)
- [ ] WSL2 회귀 게이트 green
- [ ] -DONE.md/HTML 박제 완료
- [ ] (GO 시) main 머지 완료

---

## 🧪 테스트

**수동 (영호 청음)**: 마을→전투→보스→복귀 전체 순회, 볼륨 밸런스
**자동**: WSL2 회귀 644/0/5

---

## 📚 학습 포인트

- BGM 상태 전환을 게임 상태(존/전투/보스)에 어떻게 매핑하는가.
- 볼륨 밸런싱은 정답이 없는 청음 작업 — 기준 레퍼런스를 두고 상대 조정.

---

## ⚠️ 함정 / 주의사항

- push/PR/머지는 비가역 — **영호 명시 GO 의무**. PR 본문 bypass 키워드 literal 금지.
- 사운드는 순수 클라 — ProtocolVersion 무변경이어야 함(머지 메시지 명시). 변경됐다면 어딘가 잘못된 것.
- "전면 사운드"가 Phase 01 인벤토리를 초과 확장하지 않게 — 초과분은 M8+로 분리.

---

## ➡️ 다음 마일스톤

- (미정 — M6/M7 완료 후 영호와 결정. 미래 후보: SOLID 리팩토링 울트라코드 / 맵 에디터·데이터 직렬화)
