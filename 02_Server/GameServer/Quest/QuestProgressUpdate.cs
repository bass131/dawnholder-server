namespace Dawnholder.Server.GameServer.Quest;

/// <summary>One recipient's displayed progress, captured when a quest operation completes.</summary>
public readonly record struct QuestProgressUpdate(int RecipientEntityId, int CurrentCount, int TargetCount);
