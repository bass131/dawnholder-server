### ADR-026: entity id 전역 풀 (맵 간 id 유지)
**날짜**: 2026-05-25
**상태**: 채택됨
**결정**: player/enemy의 entity id를 `GameWorld` 단일 **전역 풀**(`Interlocked.Increment`)에서 발급한다.
맵 이동 시 player의 entity id를 **유지**(재배정 X). 그 결과 `S_MapTransition` 패킷에 `entityId`
필드를 **포함하지 않는다** (클라가 기존 id를 그대로 보유하므로 redundant).
**이유**: (1) 클라 단순 — 맵 전환 시 id 교체 로직이 0 (Phase 04 클라 부담 ↓). (2) M5 영속화
(캐릭터 DB id) + M4.3 cheat-flag 추적이 맵을 넘나들어도 같은 id로 일관. (3) 전역 카운터의
유일한 우려인 멀티스레드 race는 발급을 GameWorld 단일 지점 `Interlocked.Increment`로만 처리해
해결 — id 발급은 "번호 뽑기"일 뿐 게임 상태 mutation이 아니므로 헌법 Map=Actor(맵별 격리)를
위배하지 않는다.
**트레이드오프**: 맵별 완전 독립 풀(B안) 대비 GameWorld에 공유 카운터 1개가 추가된다. 단 발급
외의 모든 게임 로직(이동/전투/spawn)은 여전히 맵별 단일 스레드로 격리된다. 미래에 분산 서버로
가면(ADR-008은 현재 단일 프로세스라 무관) 맵이 다른 프로세스로 분리될 때 전역 id 발급을
globally-unique 전략으로 재설계해야 하며, 그 시점에 별도 ADR로 박는다.

> **맥락**: M4.2 Phase 02 진입 전 결정 (plan-auditor 2026-05-25 🟡 — PDL `S_MapTransition` 필드
> 모양이 본 정책에 종속되므로 패킷 박기 전 선결). Phase 01(`dad760b`) 현행은 맵별 독립 풀이었고,
> Phase 02에서 GameWorld 전역 발급기로 전환한다.
</content>
