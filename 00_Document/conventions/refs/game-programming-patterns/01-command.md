---
title: Command 패턴
source: Game Programming Patterns — Robert Nystrom
category: Design Patterns Revisited
---
# [GPP-01] Command 패턴 (Command)
> 메서드 호출을 **객체로 캡슐화**해 저장·지연·역전·전송을 가능하게 한다. "Reified method call(구체화된 메서드 호출)". — 디자인 패턴 재조명

## 언제 참조하나 (트리거)
- 키 입력/버튼을 함수 직호출이 아니라 **런타임 리매핑**할 수 있게 만들고 싶을 때.
- **Undo/Redo**(예: 레벨 에디터, 턴제 전투 취소)가 필요한 작업을 설계할 때.
- 서버가 클라이언트 행동을 **직렬화해 재생(replay)** 하거나 네트워크로 전달하는 구조를 짤 때.
- AI/플레이어/봇이 동일한 행동 인터페이스를 통해 **동일한 Actor를 제어**해야 할 때.

## 핵심 내용

### 문제
버튼 A → `jump()` 직접 호출하면 코드가 단단히 묶인다(tight coupling).
키 리매핑, 기록·재생, Undo, AI 제어를 추가하려면 호출부를 전부 뜯어야 한다.

### 해법 구조
인터페이스 하나:
```
interface ICommand { void Execute(); }
```
각 행동을 구현체로:
```
class JumpCommand : ICommand {
    public void Execute() { actor.Jump(); }
}
```
입력 핸들러는 `ICommand` 참조만 들고, 버튼 이벤트마다 `command.Execute()` 한 줄.
버튼-커맨드 바인딩을 교체하는 것만으로 키 리매핑이 완성된다.

### 변형 1 — Actor 주입 (actor-parameterized command)
커맨드 생성 시 Actor를 고정하지 않고 `Execute(GameActor actor)` 시그니처로 넘긴다.
같은 `JumpCommand` 인스턴스가 플레이어·AI·봇 어떤 Actor든 제어할 수 있다.
AI 시스템은 커맨드 스트림을 생성하고, 어느 Actor가 실행하는지 알 필요가 없다.

### 변형 2 — 커맨드 큐 (command queue)
생산자(입력핸들러·AI)와 소비자(Actor·서버 틱)를 큐로 분리한다.
- 네트워크 전송: 커맨드 스트림 직렬화 → 원격 수신 후 재실행 → 멀티플레이어/리플레이.
- 틱 분리: 틱마다 큐에서 꺼내 처리 → 프레임 시간과 실행을 디커플.

### 변형 3 — Undo/Redo (undoable command)
커맨드가 **이전 상태를 직접 저장**:
```
class MoveUnitCommand : ICommand {
    int prevX, prevY;
    public void Execute() { prevX=unit.X; prevY=unit.Y; unit.MoveTo(x, y); }
    public void Undo()    { unit.MoveTo(prevX, prevY); }
}
```
히스토리 리스트 + 현재 포인터로 Undo는 포인터를 뒤로, Redo는 앞으로.
Undo 후 새 커맨드 실행 → 포인터 이후 히스토리를 버린다.
Memento 패턴(전체 스냅샷) 대비 **필요한 delta만** 저장하므로 메모리 효율이 좋다.

### 재사용 vs. 1회용
- 무상태(stateless) 커맨드: 싱글턴 또는 Flyweight처럼 재사용.
- 상태 보유 커맨드(예: MoveUnitCommand): 실행마다 새 인스턴스, 생성자에서 대상·좌표를 바인딩.

### 언제 쓰고, 언제 안 쓰나
**써야 할 때**: 키 리매핑, Undo/Redo, 리플레이·로깅, AI-플레이어 동일 인터페이스, 지연 실행, 직렬화 전송.
**쓰지 말아야 할 때**: 바인딩이 고정이고 절대 변하지 않을 간단한 입력, 단순 직접 호출로 충분한 경우 — 커맨드 객체 생성 오버헤드와 간접층이 오히려 노이즈.

## 우리 프로젝트 적용

### 이미 사용 중 (암묵적 패턴)
- `98_Shared/Protocol/` 의 패킷 자체가 **직렬화된 커맨드 스트림**이다. 클라이언트가 C_Move 패킷을 서버로 보내는 구조는 "커맨드를 직렬화해 원격 실행"의 네트워크 변형.
- `02_Server/GameServer/Handlers/` 의 각 핸들러(`PlayerMoveHandler`, `AttackHandler` 등)는 수신 패킷(커맨드)을 해석해 게임 상태에 적용하는 소비자 역할.

### 채택 후보 — UnityClientSession God class 분리
`03_Client/Assets/Scripts/Network/UnityClientSession.cs` (665줄) 안에 패킷핸들러 12개가 인라인 박혀 있다.
이를 서버 `Handlers/` 패턴처럼 `IPacketHandler` 인터페이스 + 핸들러 클래스로 분리하면 Command 패턴 구조가 된다.
각 패킷 타입 → 각 Handler 객체가 `Execute(session, packet)` 형태로 처리.

### 채택 후보 — 클라이언트 Undo/Prediction reconcile
`InputHistory` + `PlayerPredictor`에서 클라이언트 prediction은 이미 입력 히스토리를 기록하고 있다.
각 입력을 `ICommand` + `Undo()`로 만들면 서버 권위 상태와 reconcile할 때 "history 끝부터 현재까지 Undo → 서버 상태 적용 → 미확인 입력 Replay"를 깔끔하게 구현할 수 있다.
현재는 수동 배열 관리인데, Command 패턴 적용 시 코드가 단순해짐.

### 채택 후보 — 봇/AI 커맨드 스트림
`99_Tools/` 헤드리스 봇이 서버와 같은 ICommand 인터페이스를 쓰면 봇 행동이 실제 플레이어 행동과 동일 경로를 탄다 → 통합 부하 테스트 + 리플레이 검증에 유리.

## 함정 / 과용 경계
- **커맨드 폭발**: 행동 종류가 많으면 클래스 수가 폭발한다. 람다/델리게이트(`Action` / `Func`)로 간단한 커맨드를 인라인하면 클래스 수를 줄일 수 있다. C#에서는 `() => actor.Jump()` 델리게이트를 `ICommand.Execute`에 래핑하는 경량 어댑터가 현실적.
- **상태 캡처 버그**: Undo용으로 이전 상태를 캡처할 때 값 복사가 아닌 참조 복사를 하면 상태가 이미 바뀐 뒤라 Undo가 틀린 값으로 되돌린다. C# struct vs. class 주의.
- **틱 루프 안 커맨드 할당**: 매 틱 new 커맨드를 남발하면 GC 압박. 자주 생성되는 커맨드는 오브젝트 풀 or 구조체 값 타입으로 교체.
- **조기 적용 경계**: 입력 바인딩이 고정이고 Undo/리플레이가 없다면 직접 호출이 더 읽기 쉽다. "언젠가 필요할 것 같아서" 미리 커맨드화하지 말 것.
- **직렬화 복잡도**: 커맨드를 네트워크 전송할 때 버전 관리가 없으면 클라/서버 커맨드 형식 불일치가 생긴다. PDL 원칙(#2 Protocol is Sacred)과 동일 문제 — 커맨드 스키마도 append-only + stable ID 필요.

## 관련
- [[02-flyweight]] — 무상태 커맨드 재사용 전략
- [[09-update-method]] — 틱 루프 안에서 커맨드 큐를 소비하는 패턴
- `00_Document/ADR/` ADR-002 (PDL 직렬화) — 패킷이 직렬화된 커맨드 스트림임을 선언한 결정
- `00_Document/policies/subagent-routing.md` — Handler 분리 작업 시 server SubAgent 라우팅 기준
