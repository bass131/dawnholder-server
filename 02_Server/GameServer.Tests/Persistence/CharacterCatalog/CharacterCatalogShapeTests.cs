using System.Reflection;
using Dawnholder.Server.GameServer.Persistence;
using static Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog.CharacterCatalogApi;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>
/// Observable shape fixed by API v1 「공개 작업과 결과」 and 「선택·취소·종료 상태 계약」: CharacterSummary is the immutable
/// (CharacterId, Slot, Name, Class) snapshot without AccountId, NameKey, fence, owner or SQL data; Class is Knight=0 and
/// Mage=1 only; every operation takes a PersistenceDeadline and a CancellationToken; BeginSelectCharacter returns its
/// handle synchronously while the other three are asynchronous; and no operation takes fence, sequence or owner input,
/// which stays Core-owned and never arrives from a client or packet.
/// </summary>
public sealed class CharacterCatalogShapeTests
{
    const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    static readonly string[] Operations = { ListOperation, CreateOperation, SelectOperation, ResolveOperation };

    [Fact]
    public void Summary_CarriesOnlyTheClientListValues()
    {
        Type summary = ServerType("CharacterSummary");
        string[] names = summary.GetProperties(Members).Select(property => property.Name).ToArray();
        string[] hidden = names.Where(name =>
            name.Contains("Account", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("NameKey", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Fence", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Owner", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Sequence", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Sql", StringComparison.OrdinalIgnoreCase)).ToArray();

        Assert.Subset(names.ToHashSet(), new HashSet<string> { "CharacterId", "Slot", "Name", "Class" });
        Assert.Empty(hidden);
    }

    [Fact]
    public void Summary_IsImmutable()
    {
        Type summary = ServerType("CharacterSummary");
        string[] mutable = summary.GetProperties(Members)
            .Where(property => property.SetMethod is { IsPublic: true } setter && !IsInitOnly(setter))
            .Select(property => property.Name)
            .ToArray();
        string[] writableFields = summary.GetFields(BindingFlags.Instance | BindingFlags.Public)
            .Where(field => !field.IsInitOnly)
            .Select(field => field.Name)
            .ToArray();

        Assert.Empty(mutable);
        Assert.Empty(writableFields);
    }

    [Fact]
    public void SummaryClass_IsKnightZeroAndMageOneOnly()
    {
        PropertyInfo? classProperty = ServerType("CharacterSummary").GetProperty("Class", Members);
        Assert.True(classProperty is not null, $"{CharacterCatalogApi.Missing}CharacterSummary.Class does not exist yet");
        Type type = Nullable.GetUnderlyingType(classProperty!.PropertyType) ?? classProperty.PropertyType;

        Dictionary<string, long> members = type.IsEnum
            ? Enum.GetNames(type).ToDictionary(name => name, name => Convert.ToInt64(Enum.Parse(type, name)))
            : new Dictionary<string, long>();

        Assert.True(type.IsEnum, $"CharacterSummary.Class is {type.Name}, not an enum");
        Assert.Equal(new Dictionary<string, long> { ["Knight"] = 0, ["Mage"] = 1 }, members);
    }

    [Theory]
    [InlineData(ListOperation)]
    [InlineData(CreateOperation)]
    [InlineData(SelectOperation)]
    [InlineData(ResolveOperation)]
    public void Operation_TakesADeadlineAndACancellationToken(string operation)
    {
        Type[] parameters = Operation(operation).GetParameters().Select(parameter => parameter.ParameterType).ToArray();

        Assert.Contains(typeof(PersistenceDeadline), parameters);
        Assert.Contains(typeof(CancellationToken), parameters);
    }

    [Fact]
    public void OnlyBeginSelect_ReturnsItsHandleSynchronously()
    {
        Dictionary<string, bool> asynchronous = Operations.ToDictionary(
            name => name,
            name => typeof(Task).IsAssignableFrom(Operation(name).ReturnType));

        Assert.True(asynchronous[ListOperation], "ListCharactersAsync must return a Task");
        Assert.True(asynchronous[CreateOperation], "CreateCharacterAsync must return a Task");
        Assert.True(asynchronous[ResolveOperation], "ResolveCreateAsync must return a Task");
        Assert.False(asynchronous[SelectOperation], "BeginSelectCharacter returns the handle before storage work");
    }

    [Fact]
    public void NoOperation_TakesFenceSequenceOrOwnerInput()
    {
        string[] leaked = Operations
            .SelectMany(name => Operation(name).GetParameters().Select(parameter => (Operation: name, Parameter: parameter)))
            .Where(pair => pair.Parameter.ParameterType == typeof(long) ||
                pair.Parameter.ParameterType == typeof(PersistenceAuthority) ||
                pair.Parameter.ParameterType == typeof(PersistenceToken) ||
                (pair.Parameter.Name ?? "").Contains("fence", StringComparison.OrdinalIgnoreCase) ||
                (pair.Parameter.Name ?? "").Contains("sequence", StringComparison.OrdinalIgnoreCase) ||
                (pair.Parameter.Name ?? "").Contains("owner", StringComparison.OrdinalIgnoreCase))
            .Select(pair => $"{pair.Operation}({pair.Parameter.ParameterType.Name} {pair.Parameter.Name})")
            .ToArray();

        Assert.Empty(leaked);
    }

    static bool IsInitOnly(MethodInfo setter)
        => setter.ReturnParameter.GetRequiredCustomModifiers()
            .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit");
}
