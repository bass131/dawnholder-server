namespace Shared.Protocol;

/// <summary>
/// 캐릭터 클래스 enum (데모용 기본 2종).
///
/// **PDL 패킷 필드와 정합**: `C_CharacterSelect.characterClass`가 본 enum의 byte cast로 전송.
/// 클라가 *선택 의도만* 보냄 (헌법 #1 Server Authority) — 서버가 본 값 검증 후 PlayerStats 박음.
/// </summary>
public enum CharacterClass : byte
{
    Knight = 0,
    Mage = 1,
}
