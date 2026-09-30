using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Maps;

[Collection("ConsoleSerial")]
public sealed class ImmediateEnemyHitTests : IDisposable
{
    private readonly TextWriter _originalOut = Console.Out;
    private readonly StringWriter _output = new();

    public ImmediateEnemyHitTests() => Console.SetOut(_output);

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        _output.Dispose();
    }

    [Theory]
    [InlineData(false, EnemyKind.Normal)]
    [InlineData(true, EnemyKind.Normal)]
    [InlineData(false, EnemyKind.Boss)]
    [InlineData(true, EnemyKind.Boss)]
    public void PacketAction_Overkill_PreservesWireOrderAndKillAttribution(bool dash, EnemyKind kind)
    {
        var scene = new Scene(kind);
        scene.Enemy.Hp = 1;

        scene.Attack(dash);

        S_HitResult hit = scene.Hit();
        Assert.True(hit.currentHp < 0);
        Assert.Equal(1 - hit.damage, hit.currentHp);
        Assert.Equal(scene.Enemy.Hp, hit.currentHp);
        Assert.Equal(scene.Enemy.MaxHp, hit.maxHp);
        Assert.Equal(scene.Player.EntityId, hit.attackerEntityId);
        Assert.Equal(scene.Enemy.EntityId, hit.targetEntityId);
        Assert.Equal((byte)(dash ? HitEffect.Dash : HitEffect.Melee), hit.hitEffect);
        Assert.Equal(scene.Player.EntityId, scene.Enemy.TargetEntityId);
        Assert.Equal(new[] { scene.Player.EntityId }, scene.Killers);
        Assert.True(scene.RemovedAtCallback);
        Assert.False(scene.Map.Enemies.ContainsKey(scene.Enemy.EntityId));
        Assert.Equal(0f, scene.Enemy.KnockbackVx);
        var expected = new List<string>();
        if (!dash) expected.Add(nameof(PacketID.S_PlayerAttack));
        expected.Add(nameof(PacketID.S_HitResult));
        expected.Add(nameof(PacketID.S_EntityDeath));
        if (kind == EnemyKind.Boss) expected.Add(nameof(PacketID.S_StageClear));
        expected.Add("Killed");
        if (dash) expected.Add(nameof(PacketID.S_SkillCast));
        Assert.Equal(expected, scene.Events);
        Assert.Equal(kind == EnemyKind.Boss, scene.Map.IsStageCleared);
    }

    [Theory]
    [InlineData(false, -1)]
    [InlineData(true, 1)]
    public void PacketAction_LiveTarget_KnockbackUsesActionSpecificDirection(bool dash, int direction)
    {
        // Rewind hitbox touches the enemy slightly behind the player. Melee pushes
        // away from the player; Dash pushes forward even for this overlap.
        var scene = new Scene(EnemyKind.Normal, enemyX: -0.25f);
        scene.Enemy.Hp = 100;

        scene.Attack(dash);

        S_HitResult hit = scene.Hit();
        Assert.Equal(100 - hit.damage, scene.Enemy.Hp);
        Assert.Equal(scene.Player.EntityId, scene.Enemy.TargetEntityId);
        Assert.Equal(direction, Math.Sign(scene.Enemy.KnockbackVx));
        Assert.Empty(scene.Killers);
        Assert.True(scene.Map.Enemies.ContainsKey(scene.Enemy.EntityId));
        Assert.DoesNotContain(nameof(PacketID.S_EntityDeath), scene.Events);
        Assert.Equal(dash ? new[] { nameof(PacketID.S_HitResult), nameof(PacketID.S_SkillCast) }
            : new[] { nameof(PacketID.S_PlayerAttack), nameof(PacketID.S_HitResult) }, scene.Events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PacketAction_OutOfRange_EmitsActionWithoutHit(bool dash)
    {
        var scene = new Scene(EnemyKind.Normal, enemyX: 20f);
        int hp = scene.Enemy.Hp;

        scene.Attack(dash);

        Assert.Equal(hp, scene.Enemy.Hp);
        Assert.Empty(scene.Killers);
        Assert.Equal(new[] { dash ? nameof(PacketID.S_SkillCast) : nameof(PacketID.S_PlayerAttack) }, scene.Events);
    }

    [Fact]
    public void MageAttack_RemainsDeferredAfterLaunch()
    {
        var scene = new Scene(EnemyKind.Normal, enemyX: 8f, characterClass: CharacterClass.Mage);
        int hp = scene.Enemy.Hp;

        scene.Attack(dash: false);

        Assert.Equal(hp, scene.Enemy.Hp);
        Assert.DoesNotContain(nameof(PacketID.S_HitResult), scene.Events);
        S_ProjectileLaunch launch = new();
        launch.Read(new ArraySegment<byte>(Assert.Single(scene.Observer.Packets, p => Id(p) == PacketID.S_ProjectileLaunch)));
        for (long tick = 3; tick < 2 + launch.travelTicks; tick++) scene.Map.Tick(tick);
        Assert.Equal(hp, scene.Enemy.Hp);
        scene.Map.Tick(2 + launch.travelTicks);
        Assert.Equal((byte)HitEffect.Projectile, scene.Hit().hitEffect);
    }

    [Theory]
    [InlineData(11, 10, true)]
    [InlineData(10, 10, false)]
    [InlineData(1, 10, false)]
    public void CommonEntryPoint_ReturnsSurvivalAndPublishesUnclampedHp(int hp, int damage, bool survives)
    {
        var scene = new Scene(EnemyKind.Normal);
        scene.Enemy.Hp = hp;
        bool? result = null;
        scene.Map.EnqueueJob(() => result = scene.Map.ApplyImmediateEnemyHit(
            scene.Enemy, scene.Player.EntityId, damage, HitEffect.Dash));

        scene.Map.Tick(2);

        Assert.Equal(survives, result);
        S_HitResult hit = scene.Hit();
        Assert.Equal(hp - damage, hit.currentHp);
        Assert.Equal(damage, hit.damage);
        Assert.Equal((byte)HitEffect.Dash, hit.hitEffect);
        Assert.Equal(scene.Player.EntityId, scene.Enemy.TargetEntityId);
        Assert.Equal(survives, scene.Map.Enemies.ContainsKey(scene.Enemy.EntityId));
        Assert.Equal(survives ? 0 : 1, scene.Killers.Count);
        if (!survives)
        {
            Assert.True(scene.RemovedAtCallback);
            Assert.Equal(new[] { nameof(PacketID.S_HitResult), nameof(PacketID.S_EntityDeath), "Killed" }, scene.Events);
        }
    }

    private static PacketID Id(byte[] packet) => (PacketID)BitConverter.ToUInt16(packet, 2);

    private sealed class Scene
    {
        public Scene(EnemyKind kind, float enemyX = 1f, CharacterClass characterClass = CharacterClass.Knight)
        {
            var content = new MapContent(0f, 0f, new[] { new EnemySpawnPoint((byte)kind, enemyX, 0f) });
            Map = new GameMap(MapId.HuntingGround, content: content, onEnemyKilled: (killer, target) =>
            {
                Killers.Add(killer);
                RemovedAtCallback = !Map!.Enemies.ContainsKey(target.EntityId);
                Events.Add("Killed");
            });
            Actor = new CapturingSession(Map, null);
            Actor.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
            Actor.Enter(characterClass);
            Map.Tick(1);
            Player = Map.Players.Single();
            Player.Position = Vector2.Zero;
            Player.FacingDir = 1;
            Player.RecordPosition(1, Vector2.Zero);
            Enemy = Map.Enemies.Values.Single();
            Enemy.X = enemyX;
            Enemy.Y = 0f;
            Observer = new CapturingSession(Map, Events);
            Map.AddPlayer(Observer, new Vector2(100f, 0f));
            Events.Clear();
            Observer.Packets.Clear();
        }

        public GameMap Map { get; }
        public PlayerEntity Player { get; }
        public EnemyEntity Enemy { get; }
        public CapturingSession Actor { get; }
        public CapturingSession Observer { get; }
        public List<string> Events { get; } = new();
        public List<int> Killers { get; } = new();
        public bool RemovedAtCallback { get; private set; }

        public void Attack(bool dash)
        {
            if (dash)
            {
                Actor.OnRecvPacket(new C_SkillUse { skillId = (byte)SkillId.Dash, attackerClientTick = 1, facing = 1 }.Write());
            }
            else
            {
                Actor.OnRecvPacket(new C_Attack { targetEntityId = Enemy.EntityId, attackerClientTick = 1 }.Write());
            }
            Map.Tick(2);
        }

        public S_HitResult Hit()
        {
            S_HitResult hit = new();
            hit.Read(new ArraySegment<byte>(Assert.Single(Observer.Packets, p => Id(p) == PacketID.S_HitResult)));
            return hit;
        }
    }

    private sealed class CapturingSession(GameMap map, List<string>? events) : GameSession
    {
        public List<byte[]> Packets { get; } = new();

        public void Enter(CharacterClass characterClass)
        {
            CompleteHandshakeAndEnter();
            SetCharacterClass((byte)characterClass);
            EnterGameWorldIfReady();
        }

        public override void Send(ArraySegment<byte> packet)
        {
            byte[] copy = packet.ToArray();
            Packets.Add(copy);
            PacketID id = Id(copy);
            if (id is PacketID.S_PlayerAttack or PacketID.S_HitResult or PacketID.S_EntityDeath
                or PacketID.S_StageClear or PacketID.S_SkillCast)
            {
                events?.Add(id.ToString());
            }
        }

        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { }
        protected override GameMap GetMap() => map;
    }
}
