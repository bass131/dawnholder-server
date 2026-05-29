---
title: Type Object
source: Game Programming Patterns
category: Behavioral Patterns
---
# [12] 타입 오브젝트 (Type Object)

> 클래스 상속 계층을 하드코딩하는 대신, 타입 정보를 런타임 데이터 객체로 빼내어 "새 타입 = 새 코드 재컴파일" 의존성을 끊는 패턴. 분류: Behavioral Patterns.

---

## 언제 참조하나 (트리거)

- "몬스터/아이템 변종이 수백 개인데 서브클래스 만들다 지침" → 바로 이 패턴
- 기획자가 코드 변경 없이 스탯·텍스트를 조정할 수 있어야 할 때
- `98_Shared/GameData/` 안의 EnemyData, ItemData 등 **데이터 정의 클래스**를 설계할 때
- JSON/XML 외부 파일에서 타입을 로드해야 할 때

---

## 핵심 내용

### 문제

수백 종류의 몬스터 품종(Breed)을 만든다고 하자. 고전적 상속이면 `DragonBreed : Monster`, `TrollBreed : Monster` 식으로 서브클래스를 수백 개 찍어야 한다. 달라지는 건 HP·공격 문자열 같은 데이터뿐인데 **컴파일 단위 타입**이 폭발적으로 늘어난다. 기획자가 스탯 하나 바꿀 때마다 재컴파일이 필요하고, 프로그래머가 매번 새 파일을 만들어야 한다.

### 해법 구조

두 클래스로 분리한다.

| 역할 | 클래스 | 담당 |
|------|--------|------|
| **Type Object** (타입 객체) | `Breed` | 품종 고유 속성 보유 (공유 데이터) |
| **Typed Object** (타입된 객체) | `Monster` | `Breed` 참조를 들고, 인스턴스별 상태 보유 |

`Monster` 인스턴스들은 같은 `Breed` 객체를 가리킨다. 언어 vtable이 하는 일을 데이터 레이어에서 흉내 낸 구조다 — "vtable이 breed 객체, vtable 포인터가 monster의 breed 참조".

### 팩토리 메서드 확장

`Breed.NewMonster()` 팩토리를 두면 `Monster` 생성자를 `private`으로 봉인할 수 있다 → 항상 Breed를 거쳐야 Monster가 만들어진다는 불변식 강제.

### 데이터 상속 (copy-down delegation)

`Breed`가 부모 `Breed`를 가질 수 있게 하면 "트롤은 기본 몬스터 스탯을 상속하되 HP만 오버라이드" 표현이 가능하다. 생성 시점에 null 필드를 부모에서 복사해 채워두면(copy-down) 런타임 조회 비용 없이 단일 포인터 조회로 끝난다.

```
Breed(parent, hp, attack):
  if parent != null:
    if hp == 0:   hp = parent.hp
    if attack == null: attack = parent.attack
```

- 단일 상속: 단순·실용적 균형점.
- 다중 상속: 데이터 공유 극대화 but 복잡도 폭발 → 거의 권장 안 함.

### 노출 vs 캡슐화

- **Breed 노출**: 외부에서 breed에 직접 접근 가능. 팩토리·풀 패턴 통합 쉬움.
- **Breed 캡슐화**: Monster가 forwarding 메서드만 제공. API 폭이 좁고, 타입별 override 로직 삽입 가능.

### 타입 변경 가능성

- **불변(immutable)**: 단순하고 디버그 쉬움. 변환 = 새 객체 생성.
- **가변(mutable)**: 변신·진화 같은 연출 효율적. 새 타입의 전제조건 검증 필요.

### 언제 쓰나

- 타입 집합이 컴파일 타임에 고정되지 않고 런타임·외부 파일에서 결정될 때
- 기획자(비프로그래머)가 타입을 추가·수정해야 할 때
- 타입 개수가 많거나 예측 불가일 때

### 언제 쓰지 않나

- 타입 수가 작고 컴파일 타임에 확정될 때 (상속이 훨씬 단순)
- 타입별 **코드 동작**이 실질적으로 다를 때 — 이 패턴은 데이터 공유 중심, 다형성 동작엔 약함
- 깊은 런타임 탐색이 성능 병목이 될 때

---

## 우리 프로젝트 적용

### 이미 사용 중 (유사 형태)

현재 미적용 — `98_Shared/GameData/`에 EnemyData 클래스는 없고, 서버 `02_Server/GameServer/Combat/EnemyKind.cs`의 enum이 적 종류를 나열하는 초보 형태. Type Object 패턴의 정식 골격은 아직 미존재.

### 채택 후보 (M4.x 이후 ~ M5 콘텐츠 확장)

**EnemyBreed 클래스 정식화**

```csharp
// 98_Shared/GameData/EnemyBreed.cs  (Type Object — 미래 채택 후보)
public class EnemyBreed {
    public string BreedId   { get; init; }
    public int    MaxHp     { get; init; }
    public int    AtkDamage { get; init; }
    public float  PatrolRange { get; init; }
    // ... FSM 파라미터
    public EnemyBreed? Parent { get; init; }  // copy-down 상속

    public int ResolvedMaxHp =>
        MaxHp > 0 ? MaxHp : (Parent?.ResolvedMaxHp ?? 0);
}

// 02_Server/GameServer/Combat/EnemyEntity.cs  (Typed Object — 현재)
public class EnemyEntity {
    // 현재는 EnemyKind enum + 하드코딩 스탯. Breed 분리는 채택 후보.
    public EnemyKind Kind { get; }
    public int CurrentHp;
    // ... 인스턴스 상태
}
```

**적용 이유**: 현재 `02_Server/GameServer/Maps/GameMap.cs`에 몬스터 스탯이 하드코딩되어 있다. Breed를 분리하면 Map 코드 없이도 기획자가 JSON으로 몬스터 종류를 추가·조정할 수 있다.

**팩토리 패턴 연계**:

```csharp
// EnemyBreed가 생성 담당 → 오브젝트 풀과 자연스럽게 통합
public EnemyEntity Spawn(Vector2Int pos) => new EnemyEntity(this, pos);
```

**State 패턴 연계** (`EnemyState` FSM과 조합):
- `EnemyBreed` = 불변 타입 속성 (HP, 공격력, 패트롤 범위)
- `EnemyState` = 가변 행동 상태 (Idle/Patrol/Chase)
- 두 클래스가 서로 다른 축을 담당 → 독립적으로 변경 가능

### 현재 무관

Unity 클라이언트 측 `03_Client/`는 렌더링·보간 전용이므로 이 패턴 적용 대상 아님 (데미지/스탯 연산은 서버 전용 — 헌법 원칙 #1).

---

## 함정 / 과용 경계

- **메모리 책임이 수동 이전**: 언어 타입 시스템이 Breed 생명주기를 보장해주지 않는다. Breed 먼저 해제되면 dangling reference. → C#이면 GC가 처리하지만, 풀 패턴 통합 시 풀 비워지는 타이밍 주의.
- **타입별 코드 동작 표현 한계**: "드래곤은 패턴 3단계 콤보 공격" 같은 행동 다양성은 데이터로 못 담는다. 이럴 때는 Strategy나 상속이 더 적합.
- **컴파일 타임 안전성 손실**: 잘못된 BreedId를 참조해도 컴파일 오류가 아닌 런타임 오류. → 로드 타임 검증 단계 필수.
- **조기 도입 금지**: 몬스터 종류가 5~6개일 때는 그냥 enum + switch로 충분. 수십 종 이상이 실제로 생기는 시점에 리팩토링.
- **Breed 상속 깊이 제한**: copy-down 체인이 3단계 이상이면 어떤 값이 실제로 적용되는지 추적하기 어려워진다. 2단계까지만 권장.

---

## 관련

- [[02-flyweight]] — 메모리 공유 목적은 비슷하나, Flyweight는 "타입" 개념 없이 인스턴스 간 불변 상태만 공유.
- [[06-state]] — State는 동일 객체의 시간적 행동 변화(가변), Type Object는 타입 정체성(불변). `02_Server/GameServer/Combat/EnemyState.cs` FSM과 EnemyBreed 분리가 정확히 이 관계.
- [[04-prototype]] — 타입 대신 기존 인스턴스 복사로 변형 생성. Type Object의 대안.
- [[18-object-pool]] — Breed 팩토리 메서드가 풀에서 Monster를 꺼낼 수 있게 하면 자연스럽게 통합.
