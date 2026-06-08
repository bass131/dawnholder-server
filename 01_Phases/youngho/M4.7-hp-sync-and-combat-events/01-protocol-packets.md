---
owner: youngho
milestone: M4.7
phase: 01
title: 프로토콜 신설 — S_PlayerHp + S_PlayerAttack + ProtocolVersion 9→10
status: done
grade: 보통
risk: irreversible(ProtocolVersion bump)
estimated: 1~2h
domain: shared
---

# Phase 01: 프로토콜 신설 (v10 토대)

> 상세 설계 = `_milestone-plan.md` "프로토콜 모양" + plan-mode `delegated-prancing-gray.md`.

## 목표
HP갈래·공격갈래가 둘 다 의존하는 v10 프로토콜 토대를 깐다. PDL append-only, 기존 enum 시프트 0.

## 작업
1. `99_Tools/PacketGenerator/PDL.xml` 맨 끝(S_EnemyAttack 다음)에 append:
   - `S_PlayerHp`(int entityId, int currentHp, int maxHp) — 플레이어 HP 권위 동기화 전용 이벤트.
   - `S_PlayerAttack`(int attackerEntityId, byte attackType, int targetEntityId, byte facing) — 원격 공격 발동 이벤트(허공 스윙 포함).
2. PacketGenerator 재생성: `~/.dotnet/dotnet run --project 99_Tools/PacketGenerator/ -- 99_Tools/PacketGenerator/PDL.xml --no-wait` (PDL 경로 인자 필수).
3. `98_Shared/Protocol/ProtocolVersion.cs` `Current = 9 → 10` + v10 이력 주석.
4. `dotnet build Dawnholder.slnx`로 Shared.dll 재빌드(PostBuild → 03_Client/Plugins 복사). ClientNet.dll drift는 복원.
5. `C_Attack`(ID 11) **모양 불변** — targetEntityId 의미만 "필수 타겟"→"선택 힌트(0=없음)"로 후속 Phase에서 사용.

## 완료 조건 (정량)
- [x] PDL append-only — S_PlayerHp/S_PlayerAttack 2패킷 추가, 기존 정의 무수정
- [x] **enum 시프트 0** — 검증: `grep -E "S_PlayerHp|S_PlayerAttack|S_EnemyAttack" GenPackets.cs`로 `S_EnemyAttack = 20`(불변) + `S_PlayerHp = 21` + `S_PlayerAttack = 22` 확인 (plan-auditor 🟡 흡수)
- [x] `ProtocolVersion.Current == 10`
- [x] `dotnet build Dawnholder.slnx` 0 Warning / 0 Error (server+shared+봇)
- [x] PDL.xml + GenPackets.cs + Shared.dll 3산출물 동반 commit

## 결과 (commit e1606b9)
✅ enum 시프트 0 실측(S_EnterMap=3·C_Attack=11·S_EnemyAttack=20 불변, 신규 21/22). ProtocolVersion 10. 빌드 0W/0E. Shared.dll 65536→67072. ClientNet drift 복원. _milestone-plan.md(plan-auditor GO) 동반.
