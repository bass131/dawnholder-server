#nullable enable
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Client.UI
{
    /// <summary>Projects the connection's confirmed values; never derives prices, eligibility or revisions.</summary>
    internal static class GuildDisplayProjection
    {
        internal static void Apply(GuildDisplayModel model, GuildRequestController? requests, CharacterClass? characterClass)
        {
            GuildClientState? state = requests?.State;
            bool pending = requests != null && requests.IsPending;
            GuildActionResult result = requests?.LastResult == null ? GuildActionResult.None
                : requests.LastResult == GuildResult.Success ? GuildActionResult.Succeeded : GuildActionResult.Refused;
            model.SetGuild(state == null || !state.HasSnapshot ? null : new GuildStatusDisplay(
                state.GuildName, state.MemberCount, state.OnlineMemberCount, state.Treasury,
                state.StrongholdLevel, state.MaxStrongholdLevel, state.BlacksmithUnlocked,
                state.StrongholdCost, Availability(state.StrongholdAvailability), pending, result));
            // GuildClientState retains the last accepted personal 44 when a newer 36 precedes it.
            model.SetBlacksmith(state == null || !state.HasPersonalSnapshot || !characterClass.HasValue
                ? null : new BlacksmithDisplay(characterClass.Value, state.WeaponLevel, state.MaxWeaponLevel,
                    state.AttackBonus, state.WeaponCurrencyCost, state.WeaponMaterialCount,
                    Availability(state.WeaponAvailability), pending, result));
        }

        internal static string Status(GuildRequestController? requests, bool ready, bool inTown, bool alive, bool personal)
        {
            if (!ready) return "입장 준비 중 · 현재 연결을 확인하고 있습니다.";
            if (requests == null || !requests.State.HasSnapshot) return "서버 동기화 대기 중 · 새로고침으로 조회하세요.";
            if (requests.LastSendError != null) return "전송 오류 · 서버 확인 대기 중입니다.";
            if (requests.IsPending) return "길드 요청 처리 중 · 서버 확인을 기다립니다.";
            if (!inTown) return "마을에서 이용할 수 있습니다.";
            if (!alive) return "생존 상태에서 이용할 수 있습니다.";
            if (personal && !requests.State.HasPersonalSnapshot) return "개인 강화 정보 동기화 대기 중";
            return Reason(personal ? requests.State.WeaponAvailability : requests.State.StrongholdAvailability);
        }

        internal static string Result(GuildRequestController? requests)
        {
            if (requests?.LastResult == null) return string.Empty;
            return requests.LastResult == GuildResult.Success ? "최근 길드 요청 완료 · 서버 확정"
                : "최근 길드 요청 거부 · " + Reason(requests.LastResult.Value);
        }

        internal static string Balance(InventoryState? inventory, GuildClientState? guild)
        {
            if (inventory == null || !inventory.HasSnapshot) return "보유 골드 · 재료 확인 중";
            if (guild == null || !guild.HasPersonalSnapshot)
                return "보유 골드 " + inventory.Currency.ToString("N0") + " · 재료 확인 중";
            int material = 0;
            for (int index = 0; index < InventoryLimits.MaxSlots; index++)
            {
                InventorySlot slot = inventory.GetSlot(index);
                if ((int)slot.ItemId == guild.WeaponMaterialId) material = slot.Count;
            }
            return "보유 골드 " + inventory.Currency.ToString("N0") + " · 재료 " + material.ToString("N0");
        }

        static GuildActionAvailability Availability(GuildResult value)
        {
            switch (value)
            {
                case GuildResult.Success: return GuildActionAvailability.Available;
                case GuildResult.MaxLevel: return GuildActionAvailability.MaxLevel;
                case GuildResult.InsufficientCurrency:
                case GuildResult.InsufficientMaterial: return GuildActionAvailability.Insufficient;
                default: return GuildActionAvailability.Locked;
            }
        }

        static string Reason(GuildResult value)
        {
            switch (value)
            {
                case GuildResult.Success: return "진행할 수 있습니다.";
                case GuildResult.Stale: return "상태가 변경되었습니다. 최신 정보를 확인하세요.";
                case GuildResult.NotMember: return "길드 소속을 확인할 수 없습니다.";
                case GuildResult.NotInTown: return "마을에서 이용할 수 있습니다.";
                case GuildResult.Dead: return "생존 상태에서 이용할 수 있습니다.";
                case GuildResult.FacilityLocked: return "대장간 잠김 · 거점을 성장시켜 해금하세요.";
                case GuildResult.MaxLevel: return "최고 단계에 도달했습니다.";
                case GuildResult.InsufficientCurrency: return "골드 또는 공동 금고가 부족합니다.";
                case GuildResult.InsufficientMaterial: return "재료가 부족합니다.";
                case GuildResult.RevisionExhausted: return "상태 변경 한도에 도달했습니다.";
                default: return "현재 이용할 수 없습니다.";
            }
        }
    }
}
