---
owner: youngho
milestone: M4.8
phase: 01
title: 프로토콜 신설 — S_ProjectileLaunch/C_SkillUse/S_SkillCast + S_HitResult hitEffect + ProtocolVersion 11
status: pending
grade: 보통
risk: irreversible(ProtocolVersion bump)
estimated: 1~2h
domain: shared
---

# Phase 01: 프로토콜 신설 (v11)

> 의존 = 없음 (마일스톤 시작점). 후속 P2~P5 전부가 본 Phase 산출물 의존.

## 목표
원거리 평타 + 최소 스킬 + 썬더볼트 AoE에 필요한 패킷을 PDL append-only로 신설하고 ProtocolVersion을 11로 bump.

## 작업
1. **PDL.xml 맨 아래 append** (`99_Tools/PacketGenerator/PDL.xml`):
   - `S_ProjectileLaunch`(23): `int attackerEntityId`, `int targetEntityId`, `byte projectileType`, `int travelTicks`.
   - `C_SkillUse`(24): `byte skillId`, `int attackerClientTick`.
   - `S_SkillCast`(25): `int casterEntityId`, `byte skillId`, `int strikeDelayTicks`, `byte facing`. **목록 없음**(PDL list 미지원 — 적별 S_HitResult로 회피).
   - 각 패킷에 의도 주석(기존 패턴 — 왜/필드 의미).
2. **S_HitResult(13) 끝에 `byte hitEffect` append** — 0=기본/근접, 1=투사체 도착, 2=낙뢰. 클라 VFX 분기 키. *모양 변경이지만 append-only*.
3. **PacketGenerator 재생성** — `dotnet run --project 99_Tools/PacketGenerator/`. `GenPackets.cs` enum 23/24/25 확인 + **기존 22 이하 시프트 0**(전체 회귀 차단).
4. **`SkillId` 상수** (`98_Shared/GameData/`): `Thunderbolt = 1`(0 예약/None). 서버·클라 공유.
5. **ProtocolVersion.cs** `Current = 10 → 11` + 이력 주석 1줄(v11: 원거리 투사체+스킬 시스템+썬더볼트, 신기능이 신규 패킷 의존 → 옛 클라 cutoff).
6. **빌드 + Shared.dll** — `dotnet build Dawnholder.slnx` 통과 → PostBuild가 `03_Client/Assets/Plugins/Shared/`로 복사.
7. **PacketRoundTrip 테스트 4건** (`GameServer.Tests/`): S_ProjectileLaunch / C_SkillUse / S_SkillCast / S_HitResult(hitEffect 포함) Write→Read 왕복 일치.

## 완료 조건 (정량)
- [ ] `dotnet build Dawnholder.slnx` 0 error
- [ ] GenPackets enum: S_ProjectileLaunch=23, C_SkillUse=24, S_SkillCast=25, 기존 ID(≤22) 불변
- [ ] S_HitResult에 hitEffect 필드 직렬화 (Write/Read 둘 다)
- [ ] `ProtocolVersion.Current == 11` + 이력 주석
- [ ] `SkillId.Thunderbolt == 1`
- [ ] PacketRoundTrip 4건 green
- [ ] **3산출물 동반 commit**: PDL.xml + GenPackets.cs + Shared.dll (+ ClientNet.dll drift는 정식 변경이므로 동반)

## 주의
- PDL 수정 후속 3종 의무(99_Tools/CLAUDE.md): 재생성 + Shared.dll 빌드 + 3산출물 commit.
- ID 시프트는 전체 프로토콜 회귀 — 재생성 후 enum 번호 *반드시* 눈으로 확인.
