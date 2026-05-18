---
summary: M3 Phase 01 선행 체크 4건 박음 — 98_Shared/CLAUDE.md:19 옛 문구 정정 (M2.5 Phase 09 → M3 Phase 02) + PacketGenerator Program.cs:21 noManager 기본값 false→true 반전 (Codex β 발견 #3 fix) + dotnet test baseline 132/0/1 green + Asset smoke는 정유현 M2-client-visuals Phase 01 DONE으로 갈음. 다음 = M3 Phase 02 ProtocolVersion 핸드셰이크.
phase: 01-pre-flight-smoke
work-id: phase01-pre-flight-smoke
status: done
completed_at: 2026-05-18
commit: TBD
---

# Phase 01 — Pre-flight Smoke Check 완료 박제

**작업 시간**: ~10분 (응급 모드 minimum)

## TL;DR

M3 본 작업 진입 전 함정 3개 사전 봉합. Codex β pre-M3 감사 발견 #3(PacketGenerator manager 인프라 부재로 `--no-manager` 잊으면 컴파일 깨짐) + #6(98_Shared/CLAUDE.md:19 옛 문구 stale) 봉합. Asset smoke는 정유현 M2-client-visuals Phase 01 DONE(Knight_player_1.4 36 PNG 컨벤션 통일 + GameplayTest 씬 박힘)으로 갈음. baseline 132 통과 / 0 실패 / 1 skip 유지.

## 5단계 보고

- **무엇을 만들었나** —
  - `98_Shared/CLAUDE.md:19` "M2.5 Phase 09 처리 예정" → "M3 Phase 02 처리 예정" 정정 (Codex β 발견 #6 봉합)
  - `99_Tools/PacketGenerator/Program.cs:21` `bool noManager = false;` → `bool noManager = true;` + 주석 2줄 (Codex β 발견 #3 fix)
  - `dotnet test Dawnholder.slnx --nologo` baseline 확인 (132 통과 / 0 실패 / 1 skip — Codex β 검증 결과 그대로, 46s)
  - Asset smoke = 정유현 M2-client-visuals Phase 01 DONE으로 갈음 (응급 시간 단축)
- **왜 필요한가** —
  - Phase 02에서 헌법 #2(Protocol is Sacred) 가짜 약속 봉합 *전에* 도구·문서 정합 박는 게 안전 (헌법 #4 정합)
  - `noManager = false` 기본값으로 `--no-manager` 잊으면 *manager 인프라 부재(ServerCore namespace + PacketHandler 타입 미존재)*에서 컴파일 깨지는 ServerPacketManager.cs 생성 risk. 원인 fix
  - 옛 Shared 문구가 향후 Codex/Claude 검토 시 *현재 상태와 충돌*로 혼동시킴 (γ 방식 3회차에서 Codex가 직접 짚음)
- **어떻게 만들었나** —
  - PacketGenerator fix 옵션 비교: (A) 기본값만 반전 vs (B) `--with-manager` 인자 신설 vs (C) 변수명 통째 반전. 응급 모드 minimum + over-engineer 회피로 **(A)** 채택. `--no-manager` 인자(라인 25)는 redundant지만 호환성 유지
  - Asset smoke 갈음 근거: 정유현 Phase 01 DONE에서 *Knight 36 PNG 컨벤션 통일 + drift 5건 자동 fix + GameplayTest 씬 박음*이 이미 끝남. 본인 클라 통합은 M3 Phase 08(Asset 통합) 시점에 자연스럽게
- **테스트 결과** —
  - `dotnet test Dawnholder.slnx --nologo`: **132 통과 / 0 실패 / 1 skip / 46s** (Codex β 검증 baseline 그대로, `Hundred_runs_all_succeed` LongRunning skip 유지)
  - `dotnet build 99_Tools/PacketGenerator/PacketGenerator.csproj --nologo`: 경고 0 / 오류 0 (기본값 반전 후 compile OK)
- **다음 스텝** — M3 Phase 02 — ProtocolVersion 핸드셰이크 (첫 PDL 변경 진입, PacketGenerator 실제 실행이 그 시점에 처음 박힘 — 기본값 `true`라 manager 파일 생성 X = 안전)

## AC 검증 결과

```bash
# 1. Shared 문서 정정 확인
$ grep -n "M3 Phase 02 처리 예정" 98_Shared/CLAUDE.md
   19:│   └── ProtocolVersion.cs  Current=2 정의 (Phase 07 박힘) — 핸드셰이크 코드 미구현, M3 Phase 02 처리 예정

# 2. PacketGenerator 기본값 반전 확인 (line 23 = 주석 2줄 추가 후 위치 shift)
$ grep -n "bool noManager" 99_Tools/PacketGenerator/Program.cs
   23:            bool noManager = true;

# 3. dotnet test baseline (Codex β 검증 결과 그대로)
$ dotnet test Dawnholder.slnx --nologo 2>&1 | tail -3
   통과!  - 실패:     0, 통과:   132, 건너뜀:     1, 전체:   133, 기간: 46 s - GameServer.Tests.dll (net10.0)

# 4. PacketGenerator build (기본값 반전 후 compile OK)
$ dotnet build 99_Tools/PacketGenerator/PacketGenerator.csproj --nologo 2>&1 | tail -5
   PacketGenerator -> C:\Dev\ClaudeDev\99_Tools\PacketGenerator\bin\Debug\net10.0\Dawnholder.Tools.PacketGenerator.dll
   빌드했습니다.
       경고 0개
       오류 0개
```

## 결정 흐름

- **PacketGenerator fix 방식**: (A) 기본값만 반전 vs (B) `--with-manager` 인자 신설 vs (C) 변수명 통째 반전 → **(A) 채택**. 이유: 응급 모드 minimum + over-engineer 회피 + 기존 `--no-manager` 인자 호환성. 단점 = 인자 redundant이지만 호환 우선.
- **Asset smoke 처리**: 본인이 직접 sample import vs 정유현 Phase 01 DONE 갈음 → **갈음**. 이유: 정유현이 *Knight 36 PNG 컨벤션 통일 + drift 0 검증 + GameplayTest 씬 박음*까지 끝냄. 본인이 같은 작업 다시 X (cross-team leverage 첫 실증, 응급 시간 단축).
- **Phase 01 commit 범위**: PDL.xml 변경 X라 Shared.dll commit 의무 적용 X여도 *dotnet test 솔루션 빌드 결과 Shared.dll timestamp 갱신*은 동반 commit (헌법 #4 정합 + CHANGELOG 2026-05-17 룰 — 일관성 우선).

## 학습 일지 후보 키워드

- `tool-default-value-trap` — `noManager = false`가 manager 인프라 부재 사이에 *조용히 박혀있던* 패턴. Codex β가 짚지 않았으면 Phase 02 진입 시 폭발했을 risk. *위험한 기본값* 클래스의 학습 가치 ★★
- `cross-team-leverage-first-instance` — 정유현 M2-client-visuals Phase 01(Asset 컨벤션)이 본인 M3 Phase 01 부담 *직접 ↓*. 팀원 작업이 *본인 마일스톤 효율*에 미친 첫 실증. `/journal:concept` 후보
- `pre-flight-smoke-pattern` — 본 작업 진입 *전* 작은 정합 작업이 *진짜 작업의 시간 폭발*을 차단. M2.5 Phase 11 정리 정신 정합
