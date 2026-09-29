# 영역 간 경계와 알려진 함정

## 입력 시점과 예측

입력을 받았다는 ack와 효과가 서버 상태에 적용되는 시점은 다를 수 있다. 지연 적용된 입력을 클라이언트가 history에서 먼저 지우면 재조정 시 재생할 근거가 없어진다. jump buffer를 바꿀 때 `InputHistory`·서버 ack·`PlayerPredictor`를 함께 확인한다. 과거 `324dfb3` 사례에서는 Unity 측 재조정 snap이 4회에서 0회로 줄어 추가 변경을 하지 않았으며, 이를 모든 지연 입력의 안전성으로 일반화하지 않는다.

자신이 시작한 대시처럼 시점·방향을 아는 동작과 서버가 결정하는 피격·넉백은 예측 근거가 다르다. 서버 적용·판정 권위는 유지하면서 클라이언트가 보낸 방향을 검증·정규화할 수 있다. 방향전환 입력보다 대시 요청이 먼저 처리되던 사례에서는 `C_SkillUse.facing`을 추가했다(프로토콜 v12→v13, `52e5042`). [ActionGate](../../02_Server/GameServer/Maps/Systems/ActionGate.cs)와 [PlayerPredictor](../../03_Client/Assets/Scripts/Prediction/PlayerPredictor.cs)를 함께 본다.

과거 임펄스 예측 결정은 `2e1b85e`, `dcf3b12`, 넉백 채택 결정은 `6ad70da`에 기록되어 있다. 현재 동작은 해당 코드와 실제 스냅샷으로 확인한다.

## 개발 환경

- Windows에서 빌드는 되지만 DLL 로드가 차단된 과거 사례가 있다: `FileLoadException 0x800711C7`. 현재 권장 실행 경로는 [WSL helper](../operations/DEVELOPMENT.md)다. 오류를 확인하지 않고 시스템 보안 설정을 변경하지 않는다.
- 과거 C# Dev Kit의 `spawn UNKNOWN`을 Vanguard와 연관해 기록했지만 단일 머신 관찰이다. 같은 오류의 원인을 보안 소프트웨어로 단정하지 않는다.
- Unity Cloud 설정은 머신별 값이 섞일 수 있다. [.githooks/pre-commit](../../.githooks/pre-commit)이 알려진 Cloud 필드 변경을 검사하며, 모든 에셋·설정을 보호하는 장치는 아니다.

옛 자동 리뷰·knowledge 승격 절차는 현재 운영 규칙이 아니다. [이관 전 원문](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/.claude/knowledge/cross-cutting/_index.md)에서 당시 관찰·커밋·판단을 확인할 수 있다.
