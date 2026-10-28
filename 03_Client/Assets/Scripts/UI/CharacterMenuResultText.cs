#nullable enable
using Shared.Protocol;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// Player text for a refused character menu reply. The bytes are Shared.Protocol's CharacterMenuResult and
    /// CharacterMenuInvalidField (World's packet-contract-v1); the wording is the character menu goal's approved
    /// table, so a wording change starts in that table. Server diagnostics never reach this type.
    /// </summary>
    internal static class CharacterMenuResultText
    {
        /// <summary>
        /// False for Success and LegacyFlowOnly, which the caller handles without an error text.
        /// invalidField is read only for InvalidInput; the contract sends 0 for every other result.
        /// </summary>
        internal static bool TryGetFailureText(byte result, byte invalidField, out string text)
        {
            switch ((CharacterMenuResult)result)
            {
                case CharacterMenuResult.Success:
                case CharacterMenuResult.LegacyFlowOnly:
                    text = string.Empty;
                    return false;
                case CharacterMenuResult.InvalidInput:
                    text = InvalidInputText(result, (CharacterMenuInvalidField)invalidField);
                    break;
                case CharacterMenuResult.RequestInProgress:
                    text = "앞의 요청을 처리하는 중입니다. 잠시 기다려 주세요.";
                    break;
                case CharacterMenuResult.InvalidState:
                    text = "지금은 이 요청을 할 수 없습니다. 메뉴로 돌아가 다시 접속해 주세요.";
                    break;
                case CharacterMenuResult.SlotOccupied:
                    text = "이미 캐릭터가 있는 슬롯입니다. 목록을 새로고침해 주세요.";
                    break;
                case CharacterMenuResult.SlotsFull:
                    text = "캐릭터 슬롯 3칸이 모두 찼습니다.";
                    break;
                case CharacterMenuResult.NameTaken:
                    text = "이미 쓰는 이름입니다. 다른 이름을 입력해 주세요.";
                    break;
                case CharacterMenuResult.CharacterUnavailable:
                    text = "이 캐릭터를 찾을 수 없습니다. 목록을 새로고침해 주세요.";
                    break;
                case CharacterMenuResult.Busy:
                    text = "이 캐릭터가 다른 곳에서 접속 중입니다. 잠시 뒤 다시 시도해 주세요.";
                    break;
                case CharacterMenuResult.RecoveryRequired:
                    text = "캐릭터 상태를 확인하는 중이라 지금은 입장할 수 없습니다.";
                    break;
                case CharacterMenuResult.CancelledBeforeDispatch:
                case CharacterMenuResult.Unavailable:
                    text = "서버가 잠시 응답하지 못했습니다. 다시 시도해 주세요.";
                    break;
                case CharacterMenuResult.OutcomeUnknown:
                    // The character may already exist, so the text sends the player to the list, never to a retry.
                    text = "결과를 확인하지 못해 연결을 끊습니다. 다시 접속해 목록을 먼저 확인해 주세요.";
                    break;
                default:
                    // SchemaMismatch, IntegrityFailure, OperationConflict and values past the contract.
                    text = ServerErrorText(result);
                    break;
            }
            return true;
        }

        static string InvalidInputText(byte result, CharacterMenuInvalidField field) => field switch
        {
            CharacterMenuInvalidField.Name => "이름은 2~12자의 한글·영문·숫자만 쓸 수 있습니다.",
            CharacterMenuInvalidField.Slot or CharacterMenuInvalidField.Class or CharacterMenuInvalidField.CharacterId =>
                "선택을 다시 확인해 주세요. 목록을 새로고침합니다.",
            // None or an unknown field does not say what was wrong, so it is not presumed to be the name.
            _ => ServerErrorText(result),
        };

        static string ServerErrorText(byte result) => $"서버 오류로 처리하지 못했습니다(코드 {result}).";
    }
}
