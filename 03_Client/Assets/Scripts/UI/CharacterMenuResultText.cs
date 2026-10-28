#nullable enable
namespace Dawnholder.Client.UI
{
    /// <summary>
    /// Player text for a refused character menu reply. The bytes are World's transport contract values
    /// (packet-contract-v1 result and invalidField); the wording is the character menu goal's approved table,
    /// so a wording change starts in that table. Server diagnostics never reach this type.
    /// </summary>
    internal static class CharacterMenuResultText
    {
        // Shared.Protocol does not carry these enums on this branch yet, so the wire values are named here.
        enum Result : byte
        {
            Success = 0,
            InvalidInput = 1,
            RequestInProgress = 2,
            InvalidState = 3,
            SlotOccupied = 4,
            SlotsFull = 5,
            NameTaken = 6,
            CharacterUnavailable = 7,
            Busy = 8,
            RecoveryRequired = 9,
            CancelledBeforeDispatch = 10,
            Unavailable = 11,
            OutcomeUnknown = 12,
            SchemaMismatch = 13,
            IntegrityFailure = 14,
            OperationConflict = 15,
            LegacyFlowOnly = 16,
        }

        enum InvalidField : byte { None = 0, Slot = 1, Name = 2, Class = 3, CharacterId = 4 }

        /// <summary>
        /// False for Success and LegacyFlowOnly, which the caller handles without an error text.
        /// invalidField is read only for InvalidInput; the contract sends 0 for every other result.
        /// </summary>
        internal static bool TryGetFailureText(byte result, byte invalidField, out string text)
        {
            switch ((Result)result)
            {
                case Result.Success:
                case Result.LegacyFlowOnly:
                    text = string.Empty;
                    return false;
                case Result.InvalidInput:
                    text = InvalidInputText(result, (InvalidField)invalidField);
                    break;
                case Result.RequestInProgress:
                    text = "앞의 요청을 처리하는 중입니다. 잠시 기다려 주세요.";
                    break;
                case Result.InvalidState:
                    text = "지금은 이 요청을 할 수 없습니다. 메뉴로 돌아가 다시 접속해 주세요.";
                    break;
                case Result.SlotOccupied:
                    text = "이미 캐릭터가 있는 슬롯입니다. 목록을 새로고침해 주세요.";
                    break;
                case Result.SlotsFull:
                    text = "캐릭터 슬롯 3칸이 모두 찼습니다.";
                    break;
                case Result.NameTaken:
                    text = "이미 쓰는 이름입니다. 다른 이름을 입력해 주세요.";
                    break;
                case Result.CharacterUnavailable:
                    text = "이 캐릭터를 찾을 수 없습니다. 목록을 새로고침해 주세요.";
                    break;
                case Result.Busy:
                    text = "이 캐릭터가 다른 곳에서 접속 중입니다. 잠시 뒤 다시 시도해 주세요.";
                    break;
                case Result.RecoveryRequired:
                    text = "캐릭터 상태를 확인하는 중이라 지금은 입장할 수 없습니다.";
                    break;
                case Result.CancelledBeforeDispatch:
                case Result.Unavailable:
                    text = "서버가 잠시 응답하지 못했습니다. 다시 시도해 주세요.";
                    break;
                case Result.OutcomeUnknown:
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

        static string InvalidInputText(byte result, InvalidField field) => field switch
        {
            InvalidField.Name => "이름은 2~12자의 한글·영문·숫자만 쓸 수 있습니다.",
            InvalidField.Slot or InvalidField.Class or InvalidField.CharacterId => "선택을 다시 확인해 주세요. 목록을 새로고침합니다.",
            // None or an unknown field does not say what was wrong, so it is not presumed to be the name.
            _ => ServerErrorText(result),
        };

        static string ServerErrorText(byte result) => $"서버 오류로 처리하지 못했습니다(코드 {result}).";
    }
}
