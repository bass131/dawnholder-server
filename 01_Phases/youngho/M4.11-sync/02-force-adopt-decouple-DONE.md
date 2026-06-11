---
summary: 서버 임펄스 클램프 임계(VelocityEpsilon 0.05f, 서버 전용)와 클라 force-adopt 게이트(0.0001f 리터럴)의 암묵 결합을 Constants.ExternalImpulseEpsilon(0.05f, 98_Shared) 하나로 통일 — 클라 게이트는 >= ε(서버 <ε→0 클램프의 정확한 보색)로 보정. wire 무변경(v12 유지)·값 동일이라 동작 불변, silent break 위험만 명시 계약으로 승격.
owner: youngho
milestone: M4.11
phase: 02-force-adopt-decouple
work-id: phase02-m4.11-sync
status: done
grade: 복잡
slug: 02-force-adopt-decouple
created: 2026-06-11
completed_at: 2026-06-11
commit: 981259b
domains: [shared, server, client]
prior_phases: [01-remote-interp-servertick]
depends_on: [01-remote-interp-servertick]
---

# Phase 02 — force-adopt 덤불 정리 (서버 임펄스 클램프 ↔ 클라 게이트 결합 끊기) 완료 박제

**소요 시간**: 1 세션

## TL;DR

서버 임펄스 클램프 임계(`VelocityEpsilon = 0.05f`, 서버 전용)와 클라 force-adopt 게이트(`0.0001f` 리터럴)가 *공유 상수 없이* 암묵 결합돼 있었다 — 한쪽 숫자만 바꾸면 다른 쪽이 조용히 깨진다(서버 임계를 낮추면 평타에서 rubber-band 재발, 클라 게이트를 올리면 lunge 중 위치 발산). `Constants.ExternalImpulseEpsilon = 0.05f`(98_Shared) 하나로 통일하고, 클라 게이트를 `> 0.0001f` → `>= ε`로 보정했다(서버 `< ε → 0` 클램프의 정확한 보색). wire 무변경(ProtocolVersion v12 유지)·값 동일(0.05f)이라 동작은 불변이고, silent break 위험만 *명시 계약*으로 승격된다. WSL2 556 passed, Unity EditMode 116 passed(신규 ε 경계 4종 포함), reviewer 6축 통과, 영호 실측 동작 불변 확인.

## AC 검증 결과

Phase 완료조건 = 게이트 리터럴 제거 + 양쪽 공유 상수 참조 + 기존 테스트 green + 신규 경계 테스트 green + Unity 컴파일 clean + 영호 실측 동작 불변. 실제 실행 결과:

```bash
# 서버 단위/통합 테스트 (WSL2, ADR-029 — SAC가 Windows dotnet test 차단)
$ wsl -d Ubuntu -- bash -lc "cd ~/dawnholder-poc && dotnet test 02_Server/GameServer.Tests/GameServer.Tests.csproj --no-build"
  Passed!  - Failed: 0, Passed: 556, Skipped: 4, Total: 560, Duration: 1 m 40 s

# 서버/공유 빌드 (Windows — 실행만 SAC 차단, build 가능). 3종 전부 0 error 0 warning
$ dotnet build 98_Shared / GameServer / GameServer.Tests
  경고 0개  오류 0개  (×3)

# 클라 컴파일 + EditMode 전체 (Unity MCP TestRunnerApi 실행)
  scriptCompilationFailed=False  isCompiling=False
  EditMode: passed=116 failed=0 skipped=0   (신규 ε 경계 4종 포함)

# 신규 ε 경계 테스트 (전부 Constants.ExternalImpulseEpsilon 참조로 작성)
  serverVx=0.049f → false  (서버 클램프 구간, 게이트 미발동)
  serverVx=ε(0.05f) → true (살아남은 최소 임펄스, 게이트 발동)
  serverVx=-ε → true       (Abs 대칭)
  serverVx=0f → false      (완전 소멸)

# 게이트 리터럴 제거 확인 (AC: 게이트 줄 한정 grep — 전역 아님, plan-auditor D1)
$ grep '0.0001f' LocalPlayerMovement.cs  # force-adopt 게이트 줄에서 0건
  (다른 의미 0.0001f는 무변경: Physics.cs:98 ground 임계 / PlayerPredictor.cs:66,68 금지구역 / 테스트 tolerance)
```

영호 실측(2026-06-11, 육안 검증): 평타 lunge ✅이상 무 / Dash ✅이상 무 / 피격 넉백 ✅이상 무 — P1 마감 시점과 동일(게이트 = 동작 불변 증명).

## 결정 흐름 (회고 참고용)

- **옵션 A/B/C 중 A 선택(영호 확정)** → 공유 상수 `Constants.ExternalImpulseEpsilon` 신설, 서버 클램프 + 클라 게이트 둘 다 참조. B(v13 명시 임펄스 플래그) 기각 = breaking change + P4 force-adopt 재설계 시 프로토콜 churn 2번 위험. C(본인 공격 `S_PlayerAttack` latch를 게이트 출처로) 기각 = 본인 공격 `S_PlayerAttack`은 `except: attacker.Owner`로 본인에게 미전송 → 로컬 게이트 출처로 사용 불가.
- **부등호 경계 귀속: `>=` vs `>`** → 클라 `>=`(ε 포함)가 서버 `<`(ε 미포함 → ε는 생존)의 정확한 보색. `>`로 썼다면 vx=ε인 *최소 생존 임펄스*를 한 틱 놓친다.
- **`VelocityEpsilon` 정의 삭제** → 사용처 3곳(`PlayerCombatStates.cs:40,76` AttackState lunge·HitState 넉백 / `EnemyStates.cs:174` EnemyHitState 넉백)이 전부 동일 의미(임펄스 감쇠 near-zero 클램프)임을 grep으로 재확인 후 삭제. 다른 의미 사용처 없음.
- **Worker STOP 검증 2건 PASS** → ① 상태 전이 시 넉백 잔류 합산 시나리오 불성립: Hit→Attack 전이 시 `HitState.Exit`이 `KnockbackVx=0` 정리(`PlayerCombatStates.cs:86`) → `0 < |vx| < ε`인 Attack 스냅샷 안 생김. ② Attack 중 이동 성분 혼입 없음: Attack 상태 동안 서버 `inputX` 강제 0(`GameMap.cs:233-238`) → vx에 이동 성분 미혼입.
- **plan-auditor 조건부 GO** → D1(완료조건 AC가 전역 `0.0001f` grep 0건 → 도달 불가, 게이트 줄 한정으로 수정)·D2(`PlayerCombatStates` 경로 오기) 봉합 후 착수.

## 막혔던 지점

- **Unity MCP RunCommand 중첩 private 클래스 → CS1527** → RunCommand 래퍼가 중첩 private 클래스를 namespace 레벨로 복제해 CS1527(namespace에 동일 멤버 중복). 콜백 클래스를 최상위 internal로 분리해 해결 → TestRunnerApi EditMode 실행 성공(passed=116).
- **98_Shared 빌드 시 ClientNet.dll binary drift** → csproj `CopyToUnityPlugins` 타겟이 소스 무변경인 ClientNet.dll까지 갱신. `git checkout`으로 ClientNet.dll만 되돌림(Shared.dll은 소스 실변경이라 정당한 갱신 — 커밋에 포함).
- **WSL2 서버 종료 self-match** → `pkill -f '[d]otnet GameServer.dll'` 브래킷 트릭으로 pkill 자기 명령줄 매치 회피.

## 학습 일지 후보 키워드

- shared constant contract — complement gating(보색 게이팅), 두 곳이 하나의 약속을 독립 매직넘버로 박을 때의 silent break
- 부등호 경계 귀속(`>=` vs `>`) — 클램프 `< ε → 0`의 정확한 보색은 `>= ε`, 최소 생존값 ε 누락 방지
- dead-zone(도달 불가 구간)에서의 동작 보존 동치 논증 — 값 불변 + 보색 보정 = 동작 불변 증명
- plan-auditor AC falsifiability — 전역 grep 함정(도달 불가 완료조건), 검증 가능한 범위로 좁히기
- Unity MCP RunCommand 중첩 클래스 CS1527 함정, 98_Shared 빌드 ClientNet.dll binary drift, pkill -f 브래킷 self-match 회피

---

## Commits (feature/m4.11-sync)

```
981259b refactor(sync): M4.11 P2 임펄스 ε 공유 상수로 force-adopt 결합 끊기
```

## 다음 스텝

- **P3** (reconcile/보간 회귀 안전망, qa) — P4 착수 전 필수 그물.
- **P4** (고정스텝, 고위험 심장부 — 가변 dt → SnapThreshold 1.5f dead-zone 뿌리).
- **P5** (재빌드).
- 백로그: facing 스냅 회귀 테스트(`ProcessAttack` 단위) 미박음 / `reviewer.md` 체크리스트 경로 drift.
