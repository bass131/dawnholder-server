namespace Shared.Protocol;

/// <summary>
/// InvalidInput 결과에서만 사용하는 공개 사용자 입력 필드. 다른 결과는 None이다.
/// 서버가 만든 문맥의 오류는 IntegrityFailure/None이며 사용자 필드 오류로 추정하지 않는다.
/// </summary>
public enum CharacterMenuInvalidField : byte
{
    None = 0,
    Slot = 1,
    Name = 2,
    Class = 3,
    CharacterId = 4
}
