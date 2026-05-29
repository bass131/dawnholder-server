---
title: Prototype
source: Game Programming Patterns
category: Design Patterns Revisited
---
# [GPP-04] Prototype (프로토타입)
> 기존 객체를 복제(clone)해 새 객체를 만드는 패턴 — Design Patterns 재방문 챕터, 실제 게임에선 '데이터 위임(delegation)'으로 살아있다.

## 언제 참조하나 (트리거)
- "몬스터 종류별로 Spawner 클래스를 따로 만들어야 하나?" 라는 고민이 생길 때.
- JSON/XML 데이터에서 "이 몬스터는 기본 고블린에서 HP만 바꾼 것" 같은 계층형 상속을 표현하고 싶을 때.
- `clone()` 메서드 또는 Object.create() 스타일의 위임 구조가 코드에 등장했을 때.
- 타입별 팩토리·스포너 클래스 수가 몬스터 종류만큼 불어나고 있을 때.

## 핵심 내용
- **문제**: Gauntlet 류 게임에서 Ghost, Demon, Sorcerer … 종류마다 GhostSpawner, DemonSpawner … 를 별도 클래스로 만들면 클래스 수가 두 배로 늘고 유지보수가 힘들다.
- **해법**: 각 몬스터 클래스에 `clone()` 메서드를 추가. Spawner는 *프로토타입 인스턴스 하나*를 들고 있다가, spawn 요청 시 `prototype->clone()`을 호출. Spawner 클래스 하나로 모든 종류를 처리.
- **복사 = 클래스 + 상태**: "speed가 두 배인 Ghost"용 스포너가 필요하면 속도값을 조정한 Ghost 인스턴스를 프로토타입으로 넘기면 끝. 새 클래스 불필요.
- **변형 1 — 함수 포인터/람다**: SpawnCallback typedef로 클래스 대신 콜백을 저장. 보일러플레이트가 클래스보다 적다.
- **변형 2 — 제너릭/템플릿**: `SpawnerFor<Ghost>` 처럼 타입 파라미터화. clone() 구현 없이도 동일 효과.
- **변형 3 — 일급 타입(First-class types)**: Python·Ruby·JavaScript 같은 동적 언어에서는 클래스 자체가 객체이므로 래퍼 없이 타입을 그냥 넘길 수 있다.
- **가장 실용적인 변형 — 데이터 위임(Data Modeling)**: JSON/YAML 데이터 엔트리에 `"prototype": "goblin grunt"` 필드를 두고, 정의되지 않은 속성은 원형 엔트리에서 폴백 조회. 클래스 계층 없이 데이터로 상속 표현. JavaScript의 프로토타입 체인과 같은 아이디어.
- **저자의 솔직한 평가**: 디자인 패턴 자체("clone()을 모든 서브클래스에 구현")는 "실제로 권장할 만한 케이스를 찾지 못했다"고 인정. 대신 데이터 위임 변형은 게임 콘텐츠 정의에서 진짜 유용하다고 평가.

## 우리 프로젝트 적용
- **현재 무관 — 코드 패턴으로서의 Prototype**: 서버 `EnemyEntity`는 `EnemyType enum` + 팩토리 메서드 방식을 쓰고 있으며, clone() 계층은 없다. 몬스터 종류가 적어 불필요.
- **채택 후보 — 데이터 위임 변형**: 현재 `98_Shared/GameData/`(Constants/Formulas/PlayerStats 등) 및 서버 `02_Server/GameServer/Combat/EnemyKind.cs`의 몬스터 정의가 하드코딩 상수/enum 수준이다. "고블린 마법사 = 고블린 grunt + spells 추가" 패턴의 JSON 정의가 생기면 `prototype` 필드 위임이 자연스러운 해법. 미래 맵 에디터 + 데이터 주도 마일스톤(memory: future-map-editor-data-driven-milestone) 시 재검토.
- **현재 무관 — 스포너**: `GameMap.UpdateEnemies` / `RespawnEnemy`에서 몬스터 생성은 switch/enum 기반. 종류가 3~5개 수준이면 교체 실익 없음.

## 함정 / 과용 경계
- **clone() 지옥**: 모든 서브클래스에 deep-copy clone()을 직접 구현하면, 필드 추가마다 clone()도 동기화해야 해서 유지보수 비용이 오히려 늘어난다. 저자가 지적한 "패턴이 해결하려는 보일러플레이트만큼의 보일러플레이트"가 생긴다.
- **데이터 위임 순환 참조**: JSON prototype 체인이 순환되면 조회 루프가 무한 루프로 전환된다. 로더 단에서 방문 집합(visited set) 체크 필수.
- **얕은 복사 vs 깊은 복사 혼동**: 참조 타입 필드(리스트, 중첩 객체)가 있으면 shallow clone은 원형과 상태를 공유한다. 서버에서 몬스터 스탯 객체를 복제할 때 이 함정이 터질 수 있다.
- **조기 채택**: 몬스터 종류가 5개 미만이면 단순 팩토리 메서드가 낫다. 종류가 10~20개를 넘어서고 변형(엘리트/보스/데이터 커스텀)이 폭발할 때 데이터 위임을 고려.
- **Component / Type Object 패턴이 더 나은 경우**: 몬스터의 동작 자체가 달라야 할 때(AI FSM, 스킬 세트)는 Prototype보다 Component나 Type Object가 맞다. Prototype은 *데이터 변형*에 강하지, *행동 조합*에는 약하다.

## 관련
- [[05-singleton]] — 스포너를 싱글턴으로 만들고 싶은 충동이 생기면 먼저 읽기.
- [[game-server-programming/04-server-and-client]] — 서버 엔티티 팩토리 패턴 (위임 없는 단순 팩토리와의 비교).
