using System.Reflection;
using Dawnholder.Server.GameServer.Accounts;
using Dawnholder.Server.GameServer.Loop;
using Xunit.Sdk;
using static Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry.RuntimeEntryApi;

namespace Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry;

/// <summary>
/// Requirement 6 (runtime-entry pretest contract; api-contract v2 「저장 대상 설정 한 곳」; goal 「메인 결정」 1): one
/// configured account name selects the persisted account. The selector reuses the AccountNameRule normalization and
/// the existing UUIDv5 numbering, and rejects an invalid, empty or mismatched configuration when it is constructed at
/// startup. Expected AccountIds are the independent Python uuid5 vectors kept in AccountIdResolverTests, so no
/// expectation is computed by the product resolver.
/// </summary>
public sealed class PersistenceAccountSelectorTests
{
    const BindingFlags Declared = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static |
        BindingFlags.Public | BindingFlags.NonPublic;

    // uuid5(uuid.UUID("d05083ec-6067-4d21-9ce9-271da508bbfd"), name), copied from AccountIdResolverTests.
    static readonly Guid Abc = new("137b1c41-ed4a-5aaf-8252-731482bcabe4");
    static readonly Guid SixteenLetters = new("71115165-620a-5a03-98d3-eacdd8a61f30");
    static readonly Guid User123 = new("18be3028-ade2-5ad0-9ffa-0fd51a36f461");
    static readonly Guid Alice01 = new("d0064115-982b-56a6-ae70-528b3cde4a09");

    public static TheoryData<string, string, Guid> ValidConfigurations => new()
    {
        { "alice_01", "alice_01", Alice01 },
        { "Alice_01", "alice_01", Alice01 },
        { "abc", "abc", Abc },
        { "ABCDEFGHIJKLMNOP", "abcdefghijklmnop", SixteenLetters },
        { "USER_123", "user_123", User123 },
    };

    public static TheoryData<string?, Guid, string> RejectedConfigurations => new()
    {
        { null, Alice01, "missing name" },
        { "", Alice01, "empty name" },
        { "ab", Abc, "shorter than 3 characters" },
        { "abcdefghijklmnopq", SixteenLetters, "longer than 16 characters" },
        { "alice-01", Alice01, "hyphen outside the name rule" },
        { "alice 01", Alice01, "space outside the name rule" },
        { "alicé_01", Alice01, "non-ASCII letter" },
        { "alice_01", User123, "AccountId is the UUIDv5 of another name" },
        { "alice_01", Guid.Empty, "empty AccountId" },
    };

    [Theory]
    [MemberData(nameof(ValidConfigurations))]
    public void ValidConfiguration_FreezesTheNormalizedNameAndItsAccountId(string configured, string normalized, Guid accountId)
    {
        object selector = NewSelector(configured, accountId);

        string actualName = NormalizedAccountName(selector);
        Guid actualId = SelectorAccountId(selector);
        bool persisted = IsPersistent(selector, accountId);

        Assert.Equal(normalized, actualName);
        Assert.Equal(accountId, actualId);
        Assert.True(persisted);
    }

    [Fact]
    public void OnlyTheConfiguredAccount_IsPersistent()
    {
        object selector = NewSelector("alice_01", Alice01);

        bool configured = IsPersistent(selector, Alice01);
        bool other = IsPersistent(selector, User123);
        bool empty = IsPersistent(selector, Guid.Empty);

        Assert.True(configured);
        Assert.False(other);
        Assert.False(empty);
    }

    // No fallback to another account or to memory play: the configuration itself is refused at startup.
    [Theory]
    [MemberData(nameof(RejectedConfigurations))]
    public void InvalidOrMismatchedConfiguration_IsRejectedAtConstruction(string? configured, Guid accountId, string reason)
    {
        Exception? thrown = Record.Exception(() => NewSelector(configured, accountId));

        Assert.False(thrown is XunitException, thrown?.Message);
        Assert.True(thrown is ArgumentException,
            $"{reason}: expected an ArgumentException-family rejection, got {thrown?.GetType().Name ?? "no exception"}");
    }

    // A changed target name in the morning edits this one property; no handler or client carries the name.
    [Fact]
    public void PersistedAccountName_IsTheOneConfiguredNamePoint()
    {
        Type options = ServerType("PersistenceOptions");
        Property(options, "PersistedAccountName", typeof(string));

        string[] declaringTypes = typeof(GameWorld).Assembly.GetTypes()
            .Where(type => type.GetMembers(Declared).Any(member => member.Name == "PersistedAccountName"))
            .Select(type => type.FullName!)
            .ToArray();

        Assert.Equal(new[] { options.FullName! }, declaringTypes);
    }

    // goal 「메인 결정」 1: the decision lives in Persistence and Accounts/AccountRegistry stay as they are. A source diff
    // is still needed to prove "unchanged"; this only shows the login side gained no dependency on the selection.
    [Fact]
    public void AccountsTypes_DoNotDependOnThePersistenceSelection()
    {
        Type selector = ServerType("PersistenceAccountSelector");
        Type options = ServerType("PersistenceOptions");
        Type[] accountTypes = typeof(AccountRegistry).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(AccountRegistry).Namespace)
            .ToArray();

        string[] references = accountTypes
            .SelectMany(type => type.GetMembers(Declared))
            .Where(member => SignatureTypes(member).Any(type => type == selector || type == options))
            .Select(member => $"{member.DeclaringType!.Name}.{member.Name}")
            .ToArray();

        Assert.NotEmpty(accountTypes);
        Assert.Empty(references);
    }

    static IEnumerable<Type> SignatureTypes(MemberInfo member) => member switch
    {
        FieldInfo field => new[] { field.FieldType },
        PropertyInfo property => new[] { property.PropertyType },
        MethodInfo method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType),
        ConstructorInfo constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType),
        _ => Array.Empty<Type>(),
    };
}
