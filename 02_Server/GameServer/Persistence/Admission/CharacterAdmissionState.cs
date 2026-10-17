namespace Dawnholder.Server.GameServer.Persistence.Admission;

internal enum CharacterAdmissionState
{
    HandshakePending,
    Selecting,
    Loading,
    Resolved,
    Entering,
    Active,
    Rejected,
    Closed
}
