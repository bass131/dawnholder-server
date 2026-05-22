namespace Shared.Protocol;

/// <summary>
/// 캐릭터 클래스 enum (M3.8 Phase 03 — 캡스톤 1 데모용 기본 2종).
///
/// **PDL 패킷 필드와 정합**: `C_CharacterSelect.characterClass`가 본 enum의 byte cast로 전송.
/// 클라가 *선택 의도만* 보냄 (헌법 #1 Server Authority) — 서버가 본 값 검증 후 PlayerStats 박음.
///
/// **byte 타입**: 0/1 두 값만 + future-proof. ushort 과잉, sbyte 음수 불필요.
///
/// **확장 정책**: M6 이후 길드 진입 시 정식 직업 체계 도입 가능. 본 enum은 *데모용 한정*
/// (PRD MVP 제외 항목 정정 정합, M3.8 Phase 01 박힘). 본 마감 후 정식 직업 도입 시 enum 갱신
/// 또는 별 enum 신설 결정 박음.
/// </summary>
public enum CharacterClass : byte
{
    Warrior = 0,
    Ranger = 1,
}
