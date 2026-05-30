---
owner: youngho
milestone: M4.3X
title: 코드베이스 정리 — §6 주석 self-documenting + Convention 갭 정합
status: in_progress
grade: 복잡
risk: irreversible
estimated: 6~10h (실제)
domain: server+shared+client+tools+문서
---

# M4.3X — 코드베이스 정리 (Code Cleanup)

> **상태**: in_progress — 2026-05-30
> **사용자 명시 결정**: 발표(2026-06-10)보다 기반 정리 우선 (ADR-028 "기반 부채 > 발표 데모" 연장)
> **관계**: M4.3 애니 상태머신 보류 (08a 커밋됨 47b59d5, 정리 후 08b~ 재개)

> ⚠️ **재범위화 (2026-05-30)**: 원래 plan은 주석 + **God class 분리** + naming 3축이었으나, plan-auditor + Codex 실측에서 **God class는 M4.3R(PR #56)에서 이미 완료** 확인 — GameMap 665→320, UnityClientSession 665→231, GameSession 700→565. 옛 ADR-028 부록 A 줄 수는 *리팩토링 이전* 스냅샷이었음. 실제 남은 부채 = **주석 과다 + Codex 감사 갭**. → God class 축(03/04/05 Phase) 제거, 3 Phase로 압축, 대규모→복잡 격하.

---

## 🎯 마일스톤 목표

코드베이스 부채는 **주석 과다**였다 (EnemyEntity 코드 40 / 주석 70 — 자명 재진술·역사 Phase 박제·폐기 사고과정). 이를 §6 self-documenting 기준으로 정리하고, Codex read-only 감사가 짚은 Convention 갭(PacketGenerator prefix / SceneTransition SerializeField / 네트워크 상속 예외 / 문서 stale)을 정합한다.

(God class는 M4.3R에서 이미 컨테이너+System으로 분리 — 본 마일스톤 대상 아님.)

---

## ⚖️ 절대 대원칙 (지킴)

1. **동작 보존** — 주석=코드 0변경 / naming=rename. 322(+) 테스트가 동작 사수.
2. **빌드/테스트 가드** — build 0 error + test green. 생성기(PacketGenerator)는 재생성→diff→build 한 묶음.
3. **발표 안전** — 주석/naming=코드0변경이라 발표 위협 없음 (위험한 구조 변경 0).

---

## 📋 Phase (실제 3개)

| # | Phase | 등급 | 도메인 | 상태 |
|---|---|---|---|---|
| 01 | CODE_CONVENTION 보강 — §6 주석 정책 + §2.4 네트워크 상속 예외 + 제목 v3→v5 (+ §5 강제 스테이징) | 복잡 | 문서 | ✅ 완료 |
| 02 | Codex 갭 4종 + 전체 주석 전수 §6 정리 (workflow 7도메인) | 복잡 | server+shared+client+tools | ✅ 완료 |
| 06 | 회귀 검증 + 마감 (build/test green · reviewer 🔴0 · PR) | 보통 | qa+메타 | 진행 |

**총 등급 = 복잡** (코드 0변경 주석 + 문서 + 소량 naming. God class 구조 변경 없음).

### Phase 02 상세 (실제 한 것)
- **Codex 갭 4종**: ① PacketGenerator §3.3 prefix(`_r`/`m_MakeFunc`/`_Segment` 정합) + 재생성 ② SceneTransition `_fadeGroup`/`_fadeDuration` rename + `FormerlySerializedAs`(prefab 값 보존) ③ 네트워크 상속 §2.4 예외 명문화 ④ 문서 stale(CODE_CONVENTION v5 / REVIEW_CHECKLIST 6축)
- **전체 주석 전수**: workflow 7도메인 병렬 → **124 .cs §6 정리 (−2313 / +949, 삭제 위주)**. 노이즈(역사 Phase 박제·폐기 사고과정·backlog·자명 재진술) 제거, 5% 안전 예외(보안/프로토콜/헌법) 보존. reviewer 🔴0 확인.
- **GenPackets drift 봉합**: 생성기 수정분이 산출물에 안 반영된 drift 발견 → 재생성(build 후 segment 유지 확인).

---

## ✅ 마일스톤 완료 조건

- [x] CODE_CONVENTION §6 주석 정책 + §2.4 네트워크 상속 예외 + 버전 정정
- [x] 전체 코드베이스 주석 §6 정리 (124 .cs, 노이즈 제거 + 5% 안전 예외 보존)
- [x] Codex 갭 4종 정합 (PacketGenerator/SceneTransition/상속예외/문서)
- [x] build 0 error + test 338 green (동작 보존) + Unity Play 실측(맵전환 3번 + 에러 0)
- [x] reviewer 🔴0 (안전 주석 보존 확인)
- [ ] §5 강제(reviewer 축 6 + SubAgent §6 규칙) 스테이징 → 본인 적용
- [ ] PR 머지 (사용자 GO) → M4.3 애니 08b 재개

---

## 🚫 명시적으로 안 한 것

- **God class 분리** — M4.3R에서 이미 완료 (대상 아님).
- **포매팅(.editorconfig/Roslyn)** — M4.4 이월.
- **PDL.xml 주석** — 보안 근거 多, 신중 영역이라 이번 .cs 전수에서 제외.
- **GameSession roster/spawn 추가 추출** — §0.3 균형상 잔여 후보(M4.4 선택).

---

## ➡️ 정리 후

- **M4.3 애니 상태머신 재개** — 08b(클라 구조) → 09 → 10 → 11 → 12.

---

## 갱신 이력

- 2026-05-30 — `/work:plan` 6 Phase 분해 (주석+God class+naming).
- 2026-05-30 — **재범위화**: plan-auditor가 God class(04/05) 중복 발견(M4.3R 이미 완료, 옛 부록 A 줄 수=리팩토링 전 스냅샷). Codex read-only 감사로 실제 갭 확정. God class 축 제거 → 03/04/05 Phase 삭제, 3 Phase 압축, 대규모→복잡.
- 2026-05-30 — **실행 완료**: Codex 갭 4종 + §6 정책 신설 + workflow 7도메인 전체 주석 전수(124 .cs, −2313/+949) + GenPackets drift 봉합. build 0/test 338 green, reviewer 🔴0. 한 묶음 커밋.
