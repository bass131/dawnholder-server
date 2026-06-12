---
owner: youngho
milestone: M4.9
phase: 06
title: Mage Teleport 클라 — 보간 끊기(force-adopt 스냅 + 원격 버퍼 reset) + 연출
status: pending
grade: 복잡
risk: (prediction/reconcile 함정)
estimated: 2.5h
domain: client
---

# Phase 06: Mage Teleport 클라 (보간 끊기 + 연출)

> **상태**: pending
> **마일스톤**: M4.9
> **등급**: 복잡 (보간/reconcile 함정 — 단순 에셋 연결 아님)
> **담당**: client Worker(Sonnet) + 영호(이펙트)
> **의존**: Phase 05 (Teleport 서버 로직 + S_SkillCast(skillId=Teleport) 신호)

---

## 🎯 목표

서버가 점프시킨 텔레포트를 **클라가 진짜 순간이동으로 보이게** 한다. 핵심은 **보간 끊기** — 로컬은 force-adopt 즉시 스냅(슬라이드 금지), 원격은 S_SkillCast(Teleport) 수신 시 보간 버퍼를 reset 후 스냅. 거기에 출발/도착 지점 텔레포트 이펙트를 얹는다. 이 Phase가 끝나면 2클라에서 양쪽 화면 모두 캐릭터가 미끄러지지 않고 *순간*이동한다.

---

## ⏪ 사전 조건

- [ ] Phase 05 완료 — 서버가 위치 점프 + S_SkillCast(skillId=Teleport) broadcast
- [ ] 영호 제작 에셋: `03_Client/Assets/Art/.../Mage/Skill_Effect/Teleport`
- [ ] 원격 엔티티 보간 버퍼 구조 확인 (`RemoteEntityRegistry` 또는 스냅샷 보간 — reset 진입점 파악)
- [ ] 로컬 force-adopt(reconcile snap) 경로 확인 (큰 위치 차 발생 시 스냅하는 기존 로직)

---

## 📝 작업 내용

**로컬 (시전자 본인)**:
- [ ] 텔레포트 후 서버 위치를 **force-adopt 즉시 스냅** — 일반 reconcile의 부드러운 보정이 아니라 **즉시 스냅**. **S_SkillCast(skillId=Teleport) 신호 기반 명시적 스냅으로 확정** — reconcile 임계 우연 의존 금지(임계를 넘어 자연 스냅될 *수도* 있다는 가정에 기대지 않음).

**원격 (다른 클라가 보는 시전자)**:
- [ ] S_SkillCast(skillId=Teleport) 수신 시 해당 엔티티 **보간 버퍼 reset** 후 다음 스냅샷 위치로 즉시 스냅 — reset 안 하면 옛 위치→새 위치를 보간으로 미끄러뜨림(슬라이드 = 실패)
- [ ] SkillCastHandler에 skillId=Teleport 분기 추가 (현재 Thunderbolt/Dash 외)

**이펙트**:
- [ ] `Teleport` 이펙트 prefab 포장 + `EffectLifetime`
- [ ] **출발 지점 + 도착 지점 양쪽 재생** — 텔레포트 직전 위치와 직후 위치 둘 다 이펙트

**qa/검증**:
- [ ] 2클라 보간 끊기 실측 (자동 테스트 어려움 — 시각 검증)

---

## ✅ 완료 조건 (정량)

- [ ] **2클라 실측** — 시전자 화면: 순간이동(슬라이드 0) + 출발/도착 양 지점 이펙트
- [ ] **2클라 실측** — 원격 화면: 같은 엔티티가 순간이동(보간 슬라이드 0) + 양 지점 이펙트
- [ ] **보간 슬라이드 0** — 어느 화면에서도 캐릭터가 옛→새 위치를 미끄러져 가지 않음
- [ ] **회귀 가드** — 텔레포트 거리를 reconcile 임계 아래로 줄여도 스냅 동작 유지(임계 의존 아님을 실측)
- [ ] Unity 콘솔 error CS 0 + Teleport 관련 prefab 미존재 warn 0

---

## 🧪 테스트

**자동**:
- 보간 버퍼 reset 단위 테스트 가능하면 추가(EditMode) — reset 후 buffer가 새 위치만 갖는지
- 시각 연출은 수동 검증

**수동**:
- 2클라: A(Mage)가 Teleport → A·B 화면 모두 순간이동 + 양 지점 이펙트. 특히 B 화면에서 **슬라이드 없는지** 집중 확인.

---

## 📚 학습 포인트

- **보간 끊기 vs force-adopt — 이번 마일스톤 최대 함정**: 원격 엔티티는 평소 스냅샷 사이를 **보간**해 부드럽게 움직인다. 그런데 텔레포트는 "옛 위치"와 "새 위치"가 멀리 떨어져 있어, 보간이 이 둘을 **부드럽게 이어버리면 순간이동이 슬라이드로 뭉개진다.** 해법 = S_SkillCast(Teleport)를 "보간 끊어"라는 신호로 써서 **보간 버퍼를 reset**한다. 로컬은 force-adopt로 즉시 스냅. 둘 다 "보간을 의도적으로 건너뛰는" 처리.
- **신호 패킷의 이중 용도**: S_SkillCast는 원래 "캐스팅 연출 시작" 신호지만, Teleport에선 **"여기서 보간을 끊어라"는 클라 reconcile 트리거**로도 쓰인다. 하나의 패킷이 연출 + 동기화 신호를 겸한다.
- **prediction과 순간이동의 충돌**: 로컬 플레이어를 예측 이동시키는 평소 로직이 텔레포트 좌표와 안 맞으면 rubber-band가 난다. 텔레포트는 예측 대상이 아니라 서버 권위 결과를 받아 스냅하는 이벤트로 다뤄야 한다.

---

## ⚠️ 함정 / 주의사항

- **보간 버퍼 reset을 빼먹으면** 데미지/위치는 맞는데 *시각적으로만* 슬라이드 → "동작은 하는데 텔레포트로 안 보임"의 전형. 완료 조건의 "슬라이드 0"이 이 함정을 잡는 게이트.
- 로컬 force-adopt를 일반 reconcile 임계에 의존하면 안 됨(임계 우연 의존 금지) — S_SkillCast(Teleport) 신호로 **명시적 스냅**을 건다(임계 의존 X, 확정).
- 이펙트를 도착 지점에만 재생하면 출발 지점이 허전하다 — 양쪽 재생이 완료 조건.

---

## ➡️ 다음 Phase

- Phase 07 (스킬 슬롯 쿨다운 UI + 전체 회귀/마감) — 모든 스킬이 박힌 뒤 슬롯 UI 확정

---

## 📋 박제 (완료 후 -DONE.md)

- 복잡 등급 → `-DONE.md` 박음 (보간 끊기 vs force-adopt + 신호 패킷 이중 용도 키워드).

---

## 작업 로그

- 2026-06-10: 계획 작성 (보간 끊기 = M4.9 최대 함정으로 식별)
