using System;
using Dawnholder.Client.Network;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests
{
    // Consumer check of the committed Shared.dll and Dawnholder.Client.Net.dll for the guild packets of goal
    // 2026-10-14-guild-server (G7, technical contract v1 「패킷 필드와 결과」). Expected bytes are written out by hand
    // from that table: LittleEndian, [ushort size incl. header][ushort id], 43 = 5, 45 = 46 = 8, 44 = 66 + name bytes
    // with the UInt16 name byte length at offset 64, 47 = 26, guild name 「새벽 길드」 as UTF-16LE. IDs 1..42 are the
    // order of PDL.xml at main 5c16194; guild 43..47 and admission 48 stay fixed, and menu 49..54 adds v22. Nothing uses the generator
    // or the server. The final test also checks Content's consumer without weakening the raw framing contract.
    public sealed class GuildProtocolCompatibilityTests
    {
        static readonly string[] ContractOrder =
        {
            "C_Ping", "S_Pong", "S_EnterMap", "S_LeaveMap", "C_MoveIntent", "S_Snapshot", "C_Handshake",
            "S_HandshakeResult", "S_PlayerJoin", "S_PlayerLeave", "C_Attack", "S_EntitySpawn", "S_HitResult",
            "S_EntityDeath", "S_StageClear", "C_CharacterSelect", "C_EnterPortal", "S_MapTransition",
            "S_EntityState", "S_EnemyAttack", "S_PlayerHp", "S_PlayerAttack", "S_ProjectileLaunch", "C_SkillUse",
            "S_SkillCast", "C_PartyInvite", "C_PartyRespond", "C_PartyLeave", "S_PartyInviteRecv", "S_PartyUpdate",
            "S_PartyError", "S_QuestUpdate", "S_PortalLocked", "C_CheatCommand", "C_InventoryRequest",
            "S_InventorySnapshot", "C_ItemUse", "S_ItemUseResult", "S_DungeonClearResult", "C_DungeonClearConfirm",
            "C_Login", "S_LoginResult",
            "C_GuildRequest", "S_GuildSnapshot", "C_StrongholdUpgrade", "C_WeaponEnhance", "S_GuildActionResult",
            "S_CharacterLoadResult",
            "C_CharacterListRequest", "S_CharacterListResult", "C_CharacterCreateRequest", "S_CharacterCreateResult",
            "C_CharacterSelectRequest", "S_CharacterSelectResult",
        };

        UnityClientSession _session;

        [TearDown]
        public void Teardown()
        {
            _session?.Cleanup();
            _session = null;
        }

        [Test]
        public void ProtocolVersion_IsTwentyTwo()
        {
            Assert.AreEqual(22, ProtocolVersion.Current);
        }

        [Test]
        public void PacketIds_OneToFiftyFour_FollowTheContractOrder()
        {
            for (int i = 0; i < ContractOrder.Length; i++)
            {
                Assert.IsTrue(Enum.TryParse(ContractOrder[i], out PacketID id), ContractOrder[i] + " must exist");
                Assert.AreEqual(i + 1, (int)(ushort)id, ContractOrder[i]);
            }
            Assert.AreEqual(ContractOrder.Length, Enum.GetValues(typeof(PacketID)).Length, "no other packet IDs");
        }

        [Test]
        public void GuildCodes_MatchTheContractTable()
        {
            byte[] results =
            {
                (byte)GuildResult.Success, (byte)GuildResult.Stale, (byte)GuildResult.NotMember,
                (byte)GuildResult.NotInTown, (byte)GuildResult.Dead, (byte)GuildResult.FacilityLocked,
                (byte)GuildResult.MaxLevel, (byte)GuildResult.InsufficientCurrency,
                (byte)GuildResult.InsufficientMaterial, (byte)GuildResult.RevisionExhausted,
                (byte)GuildResult.Unavailable,
            };
            byte[] kinds =
            {
                (byte)GuildChangeKind.Query, (byte)GuildChangeKind.Membership, (byte)GuildChangeKind.TreasuryCredit,
                (byte)GuildChangeKind.StrongholdUpgrade, (byte)GuildChangeKind.WeaponEnhance,
                (byte)GuildChangeKind.Presence,
            };

            CollectionAssert.AreEqual(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, results);
            Assert.AreEqual(results.Length, Enum.GetValues(typeof(GuildResult)).Length);
            CollectionAssert.AreEqual(new byte[] { 0, 1, 2, 3, 4, 5 }, kinds);
            Assert.AreEqual(kinds.Length, Enum.GetValues(typeof(GuildChangeKind)).Length);
            Assert.AreEqual(1, (byte)GuildAction.StrongholdUpgrade);
            Assert.AreEqual(2, (byte)GuildAction.WeaponEnhance);
            Assert.AreEqual(2, Enum.GetValues(typeof(GuildAction)).Length);
        }

        [Test]
        public void GuildRequests_WriteTheContractFrames()
        {
            byte[] query = { 0x05, 0x00, 0x2B, 0x00, 0x00 };
            byte[] upgrade = { 0x08, 0x00, 0x2D, 0x00, 0x04, 0x03, 0x02, 0x01 };
            byte[] enhance = { 0x08, 0x00, 0x2E, 0x00, 0xFF, 0xFF, 0xFF, 0xFF };

            CollectionAssert.AreEqual(query, Bytes(new C_GuildRequest { reserved = 0 }.Write()));
            CollectionAssert.AreEqual(
                upgrade, Bytes(new C_StrongholdUpgrade { expectedGuildRevision = 0x01020304u }.Write()));
            CollectionAssert.AreEqual(
                enhance, Bytes(new C_WeaponEnhance { expectedInventoryRevision = uint.MaxValue }.Write()));
        }

        [Test]
        public void GuildSnapshot_ReadsAndWritesTheSeventySixByteContractFrame()
        {
            byte[] wire = SnapshotFrame();

            var decoded = new S_GuildSnapshot();
            decoded.Read(new ArraySegment<byte>(wire));
            byte[] encoded = Bytes(new S_GuildSnapshot
            {
                snapshotSequence = 0x0102030405060708L,
                changeKind = 4,
                guildId = 1,
                guildRevision = 0xA1B2C3D4u,
                treasury = 1_000_000_000,
                strongholdLevel = 2,
                maxStrongholdLevel = 2,
                blacksmithUnlocked = 1,
                memberCount = 7,
                onlineMemberCount = 3,
                inventoryRevision = 0x11223344u,
                weaponLevel = 1,
                maxWeaponLevel = 1,
                attackBonus = 5,
                strongholdCost = 0,
                weaponCurrencyCost = 0,
                weaponMaterialId = 1,
                weaponMaterialCount = 0,
                strongholdAvailability = 6,
                weaponAvailability = 6,
                guildName = "새벽 길드",
            }.Write());

            Assert.AreEqual(0x0102030405060708L, decoded.snapshotSequence);
            Assert.AreEqual((byte)GuildChangeKind.WeaponEnhance, decoded.changeKind);
            Assert.AreEqual(1u, decoded.guildId);
            Assert.AreEqual(0xA1B2C3D4u, decoded.guildRevision);
            Assert.AreEqual(1_000_000_000, decoded.treasury);
            Assert.AreEqual(2, decoded.strongholdLevel);
            Assert.AreEqual(2, decoded.maxStrongholdLevel);
            Assert.AreEqual(1, decoded.blacksmithUnlocked);
            Assert.AreEqual(7, decoded.memberCount);
            Assert.AreEqual(3, decoded.onlineMemberCount);
            Assert.AreEqual(0x11223344u, decoded.inventoryRevision);
            Assert.AreEqual(1, decoded.weaponLevel);
            Assert.AreEqual(1, decoded.maxWeaponLevel);
            Assert.AreEqual(5, decoded.attackBonus);
            Assert.AreEqual(0, decoded.strongholdCost);
            Assert.AreEqual(0, decoded.weaponCurrencyCost);
            Assert.AreEqual(1, decoded.weaponMaterialId);
            Assert.AreEqual(0, decoded.weaponMaterialCount);
            Assert.AreEqual((byte)GuildResult.MaxLevel, decoded.strongholdAvailability);
            Assert.AreEqual((byte)GuildResult.MaxLevel, decoded.weaponAvailability);
            Assert.AreEqual("새벽 길드", decoded.guildName);
            CollectionAssert.AreEqual(wire, encoded);
        }

        [Test]
        public void GuildActionResult_ReadsAndWritesTheTwentySixByteContractFrame()
        {
            byte[] wire = ResultFrame();

            var decoded = new S_GuildActionResult();
            decoded.Read(new ArraySegment<byte>(wire));
            byte[] encoded = Bytes(new S_GuildActionResult
            {
                action = 2,
                result = 1,
                requestedRevision = 0x01020304u,
                guildRevision = 0xA1B2C3D4u,
                inventoryRevision = 0x11223344u,
                snapshotSequence = 0x0102030405060708L,
            }.Write());

            Assert.AreEqual((byte)GuildAction.WeaponEnhance, decoded.action);
            Assert.AreEqual((byte)GuildResult.Stale, decoded.result);
            Assert.AreEqual(0x01020304u, decoded.requestedRevision);
            Assert.AreEqual(0xA1B2C3D4u, decoded.guildRevision);
            Assert.AreEqual(0x11223344u, decoded.inventoryRevision);
            Assert.AreEqual(0x0102030405060708L, decoded.snapshotSequence);
            CollectionAssert.AreEqual(wire, encoded);
        }

        // One receive chunk carries a whole 44, a whole 47 and the first 3 bytes of the next 44. The committed
        // ClientNet framing must cut the two complete frames and wait for the rest. Registered consumers apply
        // only on main; an unsolicited result does not create a pending purchase or grant gameplay authority.
        [Test]
        public void CurrentClient_FramesGuildPushesThroughClientNet_ConsumesRegisteredIds_AndStaysOpen()
        {
            var queue = new ManualConnectionQueue();
            _session = new UnityClientSession(() => true, queue.Post);
            byte[] snapshot = SnapshotFrame();
            byte[] result = ResultFrame();
            byte[] chunk = new byte[snapshot.Length + result.Length + 3];
            Buffer.BlockCopy(snapshot, 0, chunk, 0, snapshot.Length);
            Buffer.BlockCopy(result, 0, chunk, snapshot.Length, result.Length);
            Buffer.BlockCopy(snapshot, 0, chunk, snapshot.Length + result.Length, 3);
            object guild = GuildClientProduct.State(_session);

            int consumed = _session.OnRecv(new ArraySegment<byte>(chunk));
            Assert.IsFalse(GuildClientProduct.Read<bool>(guild, "HasSnapshot"));
            queue.Drain();

            Assert.AreEqual(76 + 26, consumed, "two complete frames are cut; the partial third waits");
            Assert.IsFalse(_session.IsClosed);
            Assert.IsTrue(GuildClientProduct.Read<bool>(guild, "HasSnapshot"));
            Assert.AreEqual(0x0102030405060708L, GuildClientProduct.Read<long>(guild, "SnapshotSequence"));
            Assert.IsFalse(GuildClientProduct.Pending(_session));
            LogAssert.NoUnexpectedReceived();
        }

        // A valid contract 44: stronghold and weapon at their maximum, so both prices are 0 and both show MaxLevel.
        static byte[] SnapshotFrame()
        {
            return new byte[]
            {
                0x4C, 0x00, 0x2C, 0x00,                         // size 76 = 66 + 10, id 44
                0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01, // snapshotSequence 0x0102030405060708
                0x04,                                           // changeKind WeaponEnhance
                0x01, 0x00, 0x00, 0x00,                         // guildId 1
                0xD4, 0xC3, 0xB2, 0xA1,                         // guildRevision 0xA1B2C3D4
                0x00, 0xCA, 0x9A, 0x3B,                         // treasury 1,000,000,000
                0x02, 0x02, 0x01,                               // stronghold 2 of 2, blacksmith unlocked
                0x07, 0x00, 0x00, 0x00,                         // memberCount 7
                0x03, 0x00, 0x00, 0x00,                         // onlineMemberCount 3
                0x44, 0x33, 0x22, 0x11,                         // inventoryRevision 0x11223344
                0x01, 0x01,                                     // weapon 1 of 1
                0x05, 0x00, 0x00, 0x00,                         // attackBonus 5
                0x00, 0x00, 0x00, 0x00,                         // strongholdCost 0 at max
                0x00, 0x00, 0x00, 0x00,                         // weaponCurrencyCost 0 at max
                0x01, 0x00, 0x00, 0x00,                         // weaponMaterialId 1 (Material)
                0x00, 0x00, 0x00, 0x00,                         // weaponMaterialCount 0 at max
                0x06, 0x06,                                     // availability MaxLevel, MaxLevel
                0x0A, 0x00,                                     // name byte length 10 at offset 64
                0xC8, 0xC0, 0xBD, 0xBC, 0x20, 0x00, 0x38, 0xAE, 0xDC, 0xB4, // 새 벽 ' ' 길 드
            };
        }

        static byte[] ResultFrame()
        {
            return new byte[]
            {
                0x1A, 0x00, 0x2F, 0x00,                         // size 26, id 47
                0x02, 0x01,                                     // WeaponEnhance, Stale
                0x04, 0x03, 0x02, 0x01,                         // requestedRevision 0x01020304
                0xD4, 0xC3, 0xB2, 0xA1,                         // guildRevision 0xA1B2C3D4
                0x44, 0x33, 0x22, 0x11,                         // inventoryRevision 0x11223344
                0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01, // snapshotSequence 0x0102030405060708
            };
        }

        static byte[] Bytes(ArraySegment<byte> segment)
        {
            byte[] bytes = new byte[segment.Count];
            Buffer.BlockCopy(segment.Array, segment.Offset, bytes, 0, segment.Count);
            return bytes;
        }
    }
}
