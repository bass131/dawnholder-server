using System;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Network.Handlers.Session
{
    internal sealed class LoginResultHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            if (buffer.Count != 5)
            {
                session.EnqueueApply(() =>
                {
                    Debug.LogWarning("[Unity] Invalid S_LoginResult length — disconnecting.");
                    session.Disconnect();
                });
                return;
            }

            var packet = new S_LoginResult();
            packet.Read(buffer);
            byte result = packet.result;
            if (result == (byte)LoginResult.InvalidName || result == (byte)LoginResult.AlreadyOnline)
            {
                session.EnqueueLoginRejection((LoginResult)result);
                return;
            }
            session.EnqueueApply(() =>
            {
                if (result > (byte)LoginResult.AlreadyOnline)
                {
                    Debug.LogWarning($"[Unity] Invalid login result {result} — disconnecting.");
                    session.Disconnect();
                    return;
                }
                session.RaiseLoginResult((LoginResult)result);
            });
        }
    }
}
