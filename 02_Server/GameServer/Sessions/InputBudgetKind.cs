namespace Dawnholder.Server.GameServer.Sessions;

// 핸들러의 BudgetKind 선언이 분류의 원천이다. 수치는 GameSession 생성자의
// _inputBudgets에 모으며, 같은 종류의 핸들러는 연결별로 같은 창과 경고를 공유한다.
internal enum InputBudgetKind
{
    // MoveIntent는 SubmitMoveIntent의 별도 예산, Handshake/CheatCommand는 예산 없음.
    None,
    Attack,
    Skill,
    Menu,
    Ping,
    Other,
}
