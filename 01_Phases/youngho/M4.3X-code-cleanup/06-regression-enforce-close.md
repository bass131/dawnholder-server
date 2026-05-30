---
owner: youngho
phase: 06
status: pending
grade: 보통
summary: 정리 전체(01~05) 통합 회귀 검증 + ADR-028 §5 강제(reviewer 축/SubAgent 주석 규칙) 본인 적용 확인 + PR 머지 → M4.3 애니 08b 재개 좌표 복원
---

# Phase 06: 회귀 검증 + 강제 적용 + 마감

> **등급**: 보통 (qa + 마감. PR 머지 시 irreversible)
> **담당**: qa SubAgent + 메인 세션
> **선행**: Phase 01~05 전부 완료

---

## 🎯 목표

코드베이스 정리 전체(주석 + God class + naming)를 **통합 회귀 검증**하고, ADR-028 §5 강제 메커니즘(reviewer 축 + SubAgent 주석 규칙)이 **본인 적용**됐는지 확인한 뒤, PR 머지하고 **M4.3 애니 상태머신 재개 좌표를 복원**한다.

---

## ⏪ 사전 조건

- [ ] **Phase 01~05 전부 완료** (Convention + 주석 + God class)

---

## 📝 작업 내용

- [ ] **전체 회귀** — `dotnet build Dawnholder.slnx --no-incremental` 0 error + `dotnet test --no-build` 322(+) green + 헤드리스 봇 전 시나리오
- [ ] **Unity Play 풀 회귀** — 마을→사냥터→보스방→엔딩 (이동/보간/전투/맵전환 + prefab 값 보존)
- [ ] **§5 강제 적용 확인** — 01에서 스테이징한 reviewer REVIEW_CHECKLIST 주석 축 + SubAgent 주석 규칙이 본인 `! cp`로 적용됐나. 미적용 시 안내(미적용 = 강제 무력, ADR-028 갭)
- [ ] **before/after 요약** — God class 줄 수(GameMap 665→?, UnityClientSession 665→?), 주석 밀도 샘플(EnemyEntity 등). 정리 효과 정량화
- [ ] CHANGELOG entry ([M] — 코드베이스 정리, 모든 팀원 영향: naming/구조 변경)
- [ ] **PR 생성** — 사용자 명시 GO 게이트(irreversible). diff 대형(정리)이라 리뷰 부담 — "주석/추출/rename, 동작 보존" 명시
- [ ] **work-pin 복원** — M4.3X MERGED + **M4.3 애니 08b 재개 좌표** (보류했던 08b~12 복원)

---

## ✅ 완료 조건

- [ ] `dotnet test --no-incremental` 전 green (회귀 0 — 정리가 동작 안 바꿈)
- [ ] 헤드리스 봇 + Unity Play 풀 시나리오 회귀 0
- [ ] §5 강제(reviewer 축 + SubAgent) 본인 적용 확인 (또는 미적용 사유 work-pin)
- [ ] before/after 정량 요약 (God class 줄 수 + 주석 밀도)
- [ ] CHANGELOG + PR 머지 (사용자 GO)
- [ ] work-pin = M4.3 애니 08b 재개 좌표 복원

---

## 🧪 테스트

**자동**: 전 dotnet test + 헤드리스 봇
**수동**: Unity Play 풀 루프 (정리 후 회귀 0 최종 확인)

---

## 📚 학습 포인트

- **대형 정리 PR의 리뷰**: 주석/추출 위주 대형 diff는 "로직 변경 0"을 *테스트 green + diff 성격*으로 증명. 리뷰어가 줄 단위 안 봐도 안전 보장.
- **강제 적용의 마지막 1마일**: ADR-028 핵심 — 스테이징만 하고 본인이 `! cp` 안 하면 강제 무력. 이 Phase가 그 적용을 확인하는 게이트.

---

## ⚠️ 함정 / 주의사항

- **PR 머지 = irreversible + 사용자 GO** (헌법/pr-and-merge-gate): AI 자율 X. admin bypass 시 사유 박음.
- **대형 diff 회귀 사각**: 파일 多 변경 → 한 곳 미묘한 회귀를 테스트가 못 잡을 수도. Play 풀 회귀로 보완.
- **work-pin 복원 누락 주의**: 정리 끝나고 애니 08b 좌표 복원 안 하면 다음 세션이 길 잃음.

---

## ➡️ 다음 마일스톤

- **M4.3 애니 상태머신 재개** — 08b(클라 구조) → 09 → 10 → 11 → 12. 깨끗한 베이스 위에서.

---

## 📋 박제 (완료 후)

- **보통 등급** — work-pin + commit. 단 마일스톤 마감이라 가벼운 요약(God class 정리 효과).

---

## 작업 로그

- 2026-05-30: 계획 수립 (`/work:plan` 코드베이스 정리)
