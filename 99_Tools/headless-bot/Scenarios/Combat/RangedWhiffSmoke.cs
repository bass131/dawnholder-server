using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// M4.8 Mage 사거리 밖 허공 공격 negative 검증 스모크.
//
// 검증 목표:
//   - Mage가 Town(enemy 없음)에서 targetEntityId=0으로 C_Attack 발사.
//   - S_PlayerAttack(attackType=1, Mage 연출) 수신 — 스윙 연출 broadcast.
//   - S_ProjectileLaunch 수신 안 됨 — 타겟 없으므로 투사체 발사 안 됨.
//   - S_HitResult 수신 안 됨 — 데미지 0.
//
// 의미: "Mage 스윙 연출은 나가되 투사체/데미지는 없다" negative 검증.
// 기존 WhiffSwingSmoke(Knight)의 Mage 대응 검증 + S_ProjectileLaunch absent 검증 추가.
public class RangedWhiffSmoke
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    static readonly TimeSpan CooldownWait   = TimeSpan.FromMilliseconds(550);
    static readonly TimeSpan QuietWindow    = TimeSpan.FromMilliseconds(700);

    const int AttackCount = 3;

    public class Result
    {
        public bool Success;
        public string Reason = "";
        public int LocalEntityId;
        public int AttacksSent;
        public int PlayerAttackCount;
        public int ProjectileLaunchCount;
        public int HitResultCount;
    }

    public static async Task<Result> Run(
        string host, int port,
        CancellationToken ct = default)
    {
        Result result = new();
        RangedWhiffProbe bot = new();

        try
        {
            bot.Connect(host, port);

            if (!bot.WaitConnected(DefaultTimeout))
                return Fail(result, "connect timeout");
            if (!bot.WaitHandshake(DefaultTimeout))
                return Fail(result, "S_HandshakeResult timeout");
            if (!bot.HandshakeOk)
                return Fail(result, $"handshake rejected: {bot.HandshakeReason}");
            if (!bot.WaitEnterMap(DefaultTimeout))
                return Fail(result, "S_EnterMap timeout");

            result.LocalEntityId = bot.LocalEntityId;

            // serverTick 확보 — 0이면 서버 rewind 범위 검증 통과 불가(silent drop).
            if (!await bot.WaitForFirstSnapshot(DefaultTimeout, ct))
                return Fail(result, "S_Snapshot timeout — serverTick 추적 불가");

            // Town에서 허공 스윙 AttackCount회 발사. 쿨다운 간격(550ms) 준수.
            for (int i = 0; i < AttackCount; i++)
            {
                bot.SendAttack(targetEntityId: 0);
                result.AttacksSent++;
                if (i < AttackCount - 1)
                    await Task.Delay(CooldownWait, ct);
            }

            // QuietWindow 대기 후 수신 카운트 확정.
            await Task.Delay(QuietWindow, ct);

            result.PlayerAttackCount    = bot.PlayerAttackCount;
            result.ProjectileLaunchCount = bot.ProjectileLaunchCount;
            result.HitResultCount        = bot.HitResultCount;

            // 스윙 연출(S_PlayerAttack)은 1건 이상 있어야 한다 — attacker 본인 제외 broadcast이므로
            // 2봇 검증은 아니지만 단일봇에서는 자기 자신의 스윙 연출 0건 (RemoteAttackSmoke 규칙과 동일).
            // 단, 서버가 "attacker 제외" broadcast하므로 단일봇은 0건도 정상 — negative만 확인.
            //
            // S_ProjectileLaunch = 0: 타겟 없으므로 투사체 없어야 함.
            if (result.ProjectileLaunchCount > 0)
                return Fail(result, $"whiff swing produced S_ProjectileLaunch — expected 0, got {result.ProjectileLaunchCount}");

            // S_HitResult = 0: 데미지 없어야 함.
            if (result.HitResultCount > 0)
                return Fail(result, $"whiff swing produced S_HitResult — expected 0, got {result.HitResultCount}");

            result.Success = true;
            return result;
        }
        finally
        {
            bot.Disconnect();
        }
    }

    static Result Fail(Result r, string reason)
    {
        r.Success = false;
        r.Reason = reason;
        return r;
    }

    sealed class RangedWhiffProbe : ProbeBase
    {
        int _playerAttackCount;
        int _projectileLaunchCount;
        int _hitResultCount;

        protected override CharacterClass SelectedClass => CharacterClass.Mage;

        public int PlayerAttackCount    { get { lock (Gate) return _playerAttackCount; } }
        public int ProjectileLaunchCount { get { lock (Gate) return _projectileLaunchCount; } }
        public int HitResultCount        { get { lock (Gate) return _hitResultCount; } }

        public async Task<bool> WaitForFirstSnapshot(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => LastReceivedServerTick > 0, timeout, ct);

        public void SendAttack(int targetEntityId)
        {
            C_Attack p = new()
            {
                targetEntityId     = targetEntityId,
                attackerClientTick = LastReceivedServerTick,
            };
            Session?.Send(p.Write());
        }

        protected override void HandleExtraPacket(PacketID id, ArraySegment<byte> buffer)
        {
            switch (id)
            {
                case PacketID.S_PlayerAttack:
                    // attacker 본인 제외 broadcast라 단일봇에선 이 패킷이 오지 않지만
                    // 혹시 규칙이 바뀌면 감지하기 위해 카운트 추적.
                    lock (Gate) _playerAttackCount++;
                    break;

                case PacketID.S_ProjectileLaunch:
                    // 허공 스윙(타겟 없음) 시 이 패킷이 와서는 안 된다.
                    lock (Gate) _projectileLaunchCount++;
                    break;

                case PacketID.S_HitResult:
                    // 허공 스윙 시 데미지가 발생하면 안 된다.
                    lock (Gate) _hitResultCount++;
                    break;
            }
        }
    }
}
