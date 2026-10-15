using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Persistence.Admission;

internal readonly record struct CharacterAdmissionResult(CharacterLoadStatus Status, CharacterClass? LoadedClass = null);
