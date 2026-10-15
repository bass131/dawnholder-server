namespace Dawnholder.Server.GameServer.Persistence;

public enum ReadAdmissionStatus : byte
{
    Probe = 1,
    ReadFailed = 2,
    NotDispatched = 3
}
