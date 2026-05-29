---
summary: GameSession(700줄, trust-boundary)에서 IntentRateLimiter + MapMigration 추출 → 566줄. handshake state 잔류(§0.3). 헌법 #3 검증 invariant byte-for-byte 보존(reviewer 🔴0), test 322/0/4(+7).
phase: 04-gamesession-trust-boundary-extract
work-id: m4.3r-phase04-gamesession-tb
status: done
grade: 복잡
owner: youngho
completed_at: 2026-05-29
commit: c24d26e
---

# Phase 04 — GameSession trust-boundary 추출 완료 박제

**소요 시간**: ~1.5h (server Worker + trust-boundary reviewer + 🟡 봉합)
**위험 깃발**: trust-boundary (헌법 #3 — rate-limit/handshake/portal 근접 검증)

## TL;DR

마지막 God class였던 `GameSession`(700줄)에서 분리 이득이 명확한 메커니즘 둘 — rate-limit 윈도우 로직(`IntentRateLimiter`)과 160줄짜리 migration 람다(`MapMigration.Execute`) — 을 추출해 566줄로 줄였다. trust-boundary 코드(헌법 #3)라 검증 invariant 보존이 최우선이었고, reviewer가 rate-limit off-by-one·portal 근접·handshake 게이트·lifecycle race 4항목을 byte-for-byte 동일로 확인(🔴 0). handshake state는 socket lifecycle 강결합이라 §0.3에 따라 컨테이너 잔류. 기존 trust-boundary 테스트 22개 무수정 통과 + rate-limit 단독 테스트 7개 신설.

## 5단계 보고

- **무엇을 만들었나** — `IntentRateLimiter.cs`(rate-limit 윈도우 + `TryConsume(out firstWarn)`), `MapMigration.cs`(Execute = portal 검증+transfer), `IntentRateLimiterTests.cs`(단독 7개). GameSession 700→566줄.
- **왜 필요한가** — GameSession이 lifecycle+rate-limit+migration+socket dispatch 4책임을 떠안아 700줄(§2.2 부분 위반). rate-limit 로직이 socket과 묶여 단독 테스트 불가했고, 160줄 단일 migration 람다는 검증/transfer 경로가 섞여 읽기 어려웠다.
- **어떻게 만들었나** — IntentRateLimiter로 윈도우 3필드+로직 이전(SubmitMoveIntent는 TryConsume 호출만). MapMigration.Execute로 람다 본문 이전, 검증(portal lookup→존재→근접) 3단계를 **한 곳에 모아** split-validation 안티패턴 회피. GameSession은 `SetMigrating`/`ReadClosing`/`SetCurrentMapId` internal 래퍼로 캡슐화 유지(ref 직접 접근 대신 명명 메서드 = 오히려 강화). handshake state 잔류(§0.3).
- **테스트 결과** — build 0/0, test 322/0/4(315+7). 기존 GameSessionRateLimitTests(4)/HandshakeHandlerTests(4)/SessionStateMachineTests(6)/EnterPortalHandlerTests(5)/GameSessionLifecycleTests(7) 무수정 통과.
- **다음 스텝** — Phase 05(클라 기회성: EnemyViewFactory + PlayerPredictorTests) → 06(클라 네이밍) → 07(네트워크 prefix).

## AC 검증 결과

```bash
$ dotnet build Dawnholder.slnx --no-incremental
  빌드했습니다. 경고 0개 / 오류 0개

$ dotnet test Dawnholder.slnx --no-build
  통과! - 실패: 0, 통과: 322, 건너뜀: 4, 전체: 326   # 315 + 신규 IntentRateLimiter 7

$ wc -l 02_Server/GameServer/Network/GameSession.cs
  566 ...GameSession.cs   # < 600 (size-guard 해소, 700→566)
```

reviewer(Tier 2-A, trust-boundary 정밀): 🔴 0건. invariant 4항목 byte-for-byte 보존 실측(기존 trust-boundary 테스트 22개 무수정 통과). 🟡 1건(ProximityThreshold false-promise) → 봉합 완료(internal→private, 주석 정정).

## 결정 흐름 (회고 참고용)

- **handshake 분리 vs 잔류** → 잔류 채택(§0.3 overSplitWarning). socket lifecycle과 강결합이라 빼면 socket 콜백과 두 파일을 동시에 열어야 흐름이 보임. rate-limit/migration만 추출.
- **migration state 접근: ref 캡처 vs internal 래퍼** → `SetMigrating`/`ReadClosing`/`SetCurrentMapId` 명명 메서드 채택. 옛 `Volatile.Write(ref self._migrating,...)`보다 캡슐화 강화 + 호출자 추적 가능(유일 호출자 = MapMigration.Execute).
- **검증을 한 곳에 모음** → portal lookup·존재·근접 3단계 전부 Execute에. 여러 메서드로 쪼개면 한쪽 호출 누락 시 우회 구멍(split-validation 안티패턴). GameSession은 EnqueueJob(tick thread)으로 감싸는 책임만.

## 막혔던 지점

- **C# private 필드 ref 외부 접근 불가** → 증상: MapMigration이 GameSession의 `_migrating`/`_closing`/`_currentMapId`를 직접 못 바꿈. 원인: private 필드는 다른 클래스가 ref로 접근 불가. 해결: internal 래퍼 메서드 3개 추가(캡슐화 유지하며 위임).

## 학습 일지 후보 키워드

- trust-boundary 리팩토링 invariant 보존(헌법 #3), split-validation 안티패턴(검증 한 곳에 모으기), §2.2 컨테이너+System이 trust-boundary와 충돌 안 하는 예시, 추출의 이득=테스트 가능성(socket 없이 rate-limit 검증), §0.3 handshake 잔류, internal 래퍼로 캡슐화 강화
