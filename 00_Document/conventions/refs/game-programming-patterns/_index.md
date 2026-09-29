# Game Programming Patterns — Robert Nystrom 참고 색인

작업에 필요한 항목만 읽는다. 개념 설명과 현재 구현을 구분하며 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md), 실제 코드 위치는 [FEATURE_MAP](../../../FEATURE_MAP.md)에 있다.

| 항목 | 읽을 때 |
|---|---|
| [Command](01-command.md) | 입력·행동을 큐에 저장하거나 다시 실행할 때 |
| [Flyweight](02-flyweight.md) | 여러 엔티티가 같은 정의 데이터를 반복 보유할 때 |
| [Observer](03-observer.md) | 상태 변경을 여러 소비자에게 알릴 때 |
| [Prototype](04-prototype.md) | 설정이 비슷한 객체를 기준 객체에서 만들 때 |
| [Singleton](05-singleton.md) | 하나의 서비스에 어디서든 접근하려는 설계를 검토할 때 |
| [State](06-state.md) | 행동 조건이 여러 플래그와 분기로 얽힐 때 |
| [Double Buffer](07-double-buffer.md) | 읽는 도중 중간 갱신 상태가 보이면 안 될 때 |
| [Game Loop](08-game-loop.md) | 입력·시뮬레이션·렌더링의 시간 간격을 정할 때 |
| [Update Method](09-update-method.md) | 여러 엔티티를 매 틱 갱신할 때 |
| [Bytecode](10-bytecode.md) | 콘텐츠 동작을 코드 재빌드 없이 구성할 필요가 있을 때 |
| [Subclass Sandbox](11-subclass-sandbox.md) | 여러 행동이 제한된 공통 연산을 사용해야 할 때 |
| [Type Object](12-type-object.md) | 유사한 게임 종류를 데이터 차이로 표현할 때 |
| [Component](13-component.md) | 하나의 객체에 여러 도메인 책임이 섞일 때 |
| [Event Queue](14-event-queue.md) | 요청을 다른 실행 흐름이나 나중 시점에 처리할 때 |
| [Service Locator](15-service-locator.md) | 구현 교체가 필요한 서비스를 공통 경로로 찾을 때 |
| [Data Locality](16-data-locality.md) | 프로파일링에서 순회·메모리 접근 비용이 확인됐을 때 |
| [Dirty Flag](17-dirty-flag.md) | 파생값을 매 변경마다 다시 계산하는 비용이 클 때 |
| [Object Pool](18-object-pool.md) | 반복 생성·해제 비용이나 GC가 측정된 병목일 때 |
| [Spatial Partition](19-spatial-partition.md) | 근처 대상 검색이 실제 병목일 때 |
