#nullable enable
using System;
using System.Collections.Generic;
using Dawnholder.Client.Prediction;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Input
{
    // 클라 입력측 스킬 데이터 SSOT.
    // 클래스 권위는 shared SkillCatalog. 연출은 SkillCastHandler. 새 스킬 = 여기 1행 + 핸들러 연출 1행.
    internal static class ClientSkillCatalog
    {
        internal sealed class ClientSkillEntry
        {
            internal readonly Func<LocalPlayerMovement, bool> CooldownReady;
            internal readonly Action<LocalPlayerMovement, Vector3> PredictCommit;

            internal ClientSkillEntry(
                Func<LocalPlayerMovement, bool> cooldownReady,
                Action<LocalPlayerMovement, Vector3> predictCommit)
            {
                CooldownReady = cooldownReady;
                PredictCommit = predictCommit;
            }
        }

        // 클래스별 Q/E 키 바인딩 단일 진실 (LocalPlayerInput에서 이전).
        internal static readonly Dictionary<CharacterClass, (SkillId q, SkillId e)> SkillKeyMap =
            new Dictionary<CharacterClass, (SkillId q, SkillId e)>
            {
                { CharacterClass.Mage,   (SkillId.Thunderbolt, SkillId.Teleport) },
                { CharacterClass.Knight, (SkillId.Dash,        SkillId.None)     },
            };

        static readonly Dictionary<SkillId, ClientSkillEntry> _table =
            new Dictionary<SkillId, ClientSkillEntry>
            {
                {
                    SkillId.Thunderbolt,
                    new ClientSkillEntry(
                        cooldownReady:  m => m.CanUseSkill,
                        predictCommit:  (m, _) => m.NotifyChannel())
                },
                {
                    SkillId.Dash,
                    new ClientSkillEntry(
                        cooldownReady:  m => m.CanUseDash,
                        predictCommit:  (m, _) => m.NotifyDash())
                },
                {
                    SkillId.Teleport,
                    new ClientSkillEntry(
                        cooldownReady:  m => m.CanUseTeleport,
                        predictCommit:  (m, pos) => m.NotifyTeleport(departPos: pos))
                },
            };

        internal static bool TryGet(SkillId skillId, out ClientSkillEntry? entry) =>
            _table.TryGetValue(skillId, out entry);
    }
}
