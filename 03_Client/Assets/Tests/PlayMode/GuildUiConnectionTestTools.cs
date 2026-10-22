using System;
using System.Buffers.Binary;
using System.Collections;
using System.Linq;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using NUnit.Framework;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Only finite server input and observations of existing product views. No display model,
    // request callback, price calculation or replacement bootstrap is installed by this fixture.
    internal static class GuildUiConnectionTestTools
    {
        internal static IEnumerator EnterTown(MapEntryPlayFixture fixture, bool ready = true)
        {
            yield return fixture.Prepare(false);
            yield return MapEntryPlayFixture.Wait(() => fixture.Session.CharacterLoad.IsSuccess,
                "fixture: actual 48 character authority");
            fixture.Peer.Send(new S_EnterMap { entityId = 700, spawnX = 2, spawnY = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => fixture.Session.Entry.State == MapEntryState.Entering &&
                fixture.Session.Entry.SceneReady && fixture.Player != null, "fixture: actual Town scene binding");
            Assert.IsNotNull(UnityEngine.Object.FindAnyObjectByType<CombatBootstrap>(), "fixture: real scene bootstrap");
            fixture.PairInput();
            if (ready)
            {
                fixture.Hp();
                yield return fixture.WaitMap(0);
                Assert.IsTrue(fixture.Session.CanSendGameplay, "fixture: actual Ready gate");
            }
            yield return fixture.Frames(4);
            Debug.Log("[guild live boundary] real bootstrap/48/scene reached; ready=" + ready);
        }

        internal static T Window<T>() where T : Component
        {
            Scene scene = SceneManager.GetActiveScene();
            T[] windows = Resources.FindObjectsOfTypeAll<T>()
                .Where(window => window.gameObject.scene == scene).ToArray();
            Debug.Log("[guild live boundary] " + typeof(T).Name + " count=" + windows.Length + " scene=" + scene.name);
            Assert.AreEqual(1, windows.Length,
                "RED missing live UI: actual CombatBootstrap gameplay entry must create one scene-owned " + typeof(T).Name);
            return windows[0];
        }

        internal static string Label(Component window, string name)
        {
            TMP_Text[] labels = window.GetComponentsInChildren<TMP_Text>(true).Where(label => label.name == name).ToArray();
            Assert.AreEqual(1, labels.Length, "existing view label " + name);
            return labels[0].text;
        }

        internal static Component Button(Component window, string name)
        {
            Component[] buttons = window.GetComponentsInChildren<Component>(true)
                .Where(component => component != null && component.name == name &&
                    component.GetType().FullName == "UnityEngine.UI.Button").ToArray();
            Assert.AreEqual(1, buttons.Length, "existing view action button " + name);
            return buttons[0];
        }

        internal static void HasNumbers(Component window, string name, string expected)
        {
            string text = Label(window, name);
            string numbers = new string(text.Where(char.IsDigit).ToArray());
            Assert.AreEqual(expected, numbers, "server literal in " + name + ": " + text);
        }

        internal static void Send(EntryWirePeer peer, byte[] wire) => peer.Send(new ArraySegment<byte>(wire));

        internal static int Requests(EntryWirePeer peer) => peer.Count(PacketID.C_GuildRequest) +
            peer.Count(PacketID.C_StrongholdUpgrade) + peer.Count(PacketID.C_WeaponEnhance);

        // Only called before any purchase input. Opening a view may query, but need not do so.
        // Answer that actual request with one prescribed Query44, never force a query policy.
        internal static IEnumerator AnswerOptionalQuery(MapEntryPlayFixture fixture, byte[] query44)
        {
            yield return fixture.Frames(4);
            if (!fixture.Session.Guild.IsPending) yield break;
            yield return MapEntryPlayFixture.Wait(() => fixture.Peer.Count(PacketID.C_GuildRequest) > 0,
                "fixture: optional view query reaches peer", 5);
            Assert.AreEqual(0, fixture.Peer.Last<C_GuildRequest>(PacketID.C_GuildRequest).reserved);
            Send(fixture.Peer, query44);
            yield return MapEntryPlayFixture.Wait(() => !fixture.Session.Guild.IsPending, "optional view Query44", 5);
            yield return fixture.Frames(3);
        }

        internal static IEnumerator Synchronize(MapEntryPlayFixture fixture, bool unlocked = false)
        {
            fixture.Peer.Send(new S_InventorySnapshot { revision = 3, currency = 71,
                slot0ItemId = 1, slot0Count = 4 }.Write());
            // A Query44 satisfies any optional initial query. No query policy is required by this test.
            Send(fixture.Peer, Snapshot(sequence: 10, unlocked: unlocked));
            yield return MapEntryPlayFixture.Wait(() => fixture.Session.Guild.State.SnapshotSequence == 10,
                "fixture: TCP 44 through OnRecv/handler/main queue", 5);
            yield return fixture.Frames(4);
            if (fixture.Session.Guild.IsPending)
            {
                Assert.Greater(fixture.Peer.Count(PacketID.C_GuildRequest), 0, "only an actual query is answered");
                Send(fixture.Peer, Snapshot(sequence: 11, unlocked: unlocked));
                yield return MapEntryPlayFixture.Wait(() => !fixture.Session.Guild.IsPending, "Query44 completion", 5);
            }
            Assert.IsTrue(fixture.Session.Guild.IsSynchronized);
            Assert.IsTrue(InventoryState.Instance.HasSnapshot);
            yield return fixture.Frames(3);
            Debug.Log("[guild live boundary] received finite inventory36/guild44; current state synchronized");
        }

        // Independent feature-table 44 literal: '새벽 길드', members7/online3, treasury40,
        // level1/2, revision7, personal revision3, weapon0/1, costs30/20/material1.
        // Named writes change input fields only; assertions remain literal in the test cases.
        internal static byte[] Snapshot(long sequence = 10, uint guildRevision = 7, uint inventoryRevision = 3,
            byte kind = 0, int treasury = 40, bool unlocked = false, bool enhanced = false)
        {
            byte[] wire =
            {
                76, 0, 44, 0,
                1, 0, 0, 0, 0, 0, 0, 0, 0,
                1, 0, 0, 0, 7, 0, 0, 0, 40, 0, 0, 0,
                1, 2, 0, 7, 0, 0, 0, 3, 0, 0, 0,
                3, 0, 0, 0, 0, 1, 0, 0, 0, 0,
                30, 0, 0, 0, 20, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0,
                0, 5, 10, 0, 200, 192, 189, 188, 32, 0, 56, 174, 220, 180,
            };
            BinaryPrimitives.WriteInt64LittleEndian(wire.AsSpan(4), sequence);
            wire[12] = kind;
            BinaryPrimitives.WriteUInt32LittleEndian(wire.AsSpan(17), guildRevision);
            BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(21), treasury);
            BinaryPrimitives.WriteUInt32LittleEndian(wire.AsSpan(36), inventoryRevision);
            if (unlocked)
            {
                wire[25] = 2;
                wire[27] = 1;
                BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(46), 0);
                wire[62] = 6;
                wire[63] = 0;
            }
            if (enhanced)
            {
                wire[40] = 1;
                BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(42), 5);
                BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(50), 0);
                BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(58), 0);
                wire[63] = 6;
            }
            return wire;
        }

        internal static byte[] Result(byte action, uint requested, uint guildRevision, uint inventoryRevision, long sequence)
        {
            byte[] wire = { 26, 0, 47, 0, action, 0, 7, 0, 0, 0, 8, 0, 0, 0, 3, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
            BinaryPrimitives.WriteUInt32LittleEndian(wire.AsSpan(6), requested);
            BinaryPrimitives.WriteUInt32LittleEndian(wire.AsSpan(10), guildRevision);
            BinaryPrimitives.WriteUInt32LittleEndian(wire.AsSpan(14), inventoryRevision);
            BinaryPrimitives.WriteInt64LittleEndian(wire.AsSpan(18), sequence);
            return wire;
        }
    }
}
