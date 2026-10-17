using System.Data;
using Microsoft.Data.SqlClient;

namespace Dawnholder.Server.GameServer.Persistence.Sql;

/// <summary>원 typed payload를 mutation과 resolver에 같은 값으로 전달한다.</summary>
internal static class SqlRuntimeParameters
{
    internal static void Identity(SqlCommand command, PersistenceIdentity identity)
    {
        Add(command, "@SlotId", SqlDbType.Int, (int)identity.SlotId);
        Add(command, "@AccountId", SqlDbType.UniqueIdentifier, identity.AccountId);
        Add(command, "@CharacterId", SqlDbType.UniqueIdentifier, identity.CharacterId);
    }

    internal static void Operation(SqlCommand command, OperationIdentity operation, bool resolving)
    {
        Identity(command, operation.Identity);
        Add(command, "@OperationId", SqlDbType.UniqueIdentifier, operation.OperationId);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, operation.OwnerId);
        Add(command, "@ExpectedFence", SqlDbType.BigInt, operation.ExpectedFence);
        Add(command, "@Sequence", SqlDbType.BigInt, operation.Sequence);
        if (resolving)
            Add(command, "@Kind", SqlDbType.Int, (int)operation.Kind);

        if (operation.Kind == PersistenceOperationKind.Acquire)
        {
            SafeDefaults safe = operation.SafeDefaults!;
            Add(command, "@Class", SqlDbType.Int, (int)operation.IntendedClass!.Value);
            Add(command, "@TownX", SqlDbType.Real, safe.TownX);
            Add(command, "@TownY", SqlDbType.Real, safe.TownY);
            Add(command, "@KnightMaxHp", SqlDbType.Int, safe.KnightMaxHp);
            Add(command, "@MageMaxHp", SqlDbType.Int, safe.MageMaxHp);
        }
        else if (operation.Kind == PersistenceOperationKind.Checkpoint)
        {
            SafeCheckpoint safe = operation.Safe!;
            Add(command, "@ExpectedCharacterVersion", SqlDbType.VarBinary,
                operation.ExpectedCharacterToken!.ToArray(), 8);
            Add(command, "@ExpectedProgressVersion", SqlDbType.VarBinary,
                operation.ExpectedProgressToken?.ToArray(), 8);
            Add(command, "@Class", SqlDbType.Int, (int)safe.CharacterClass);
            Add(command, "@TownX", SqlDbType.Real, safe.TownX);
            Add(command, "@TownY", SqlDbType.Real, safe.TownY);
            Add(command, "@MaxHp", SqlDbType.Int, safe.MaxHp);
        }
        else if (operation.Kind != PersistenceOperationKind.ReleaseRuntime)
        {
            throw new ArgumentException("Only runtime operations are supported.", nameof(operation));
        }
        // Resolver's remaining kind-specific arguments use the procedure's explicit NULL defaults.
        // No opaque payload, replacement operation ID or retry is constructed here.
    }

    internal static void Add(SqlCommand command, string name, SqlDbType type, object? value, int size = 0)
    {
        SqlParameter parameter = size == 0
            ? new SqlParameter(name, type)
            : new SqlParameter(name, type, size);
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
