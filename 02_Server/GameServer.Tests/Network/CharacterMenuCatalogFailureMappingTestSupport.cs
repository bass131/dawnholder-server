using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Network;

// Only this test adapter late-binds Core names. The product seam must take the real Core enums.
// No Core ordinal or local result mapper supplies an answer when the World product is absent.
internal sealed class CharacterMenuCatalogFailureMappingProbe
{
    internal const string UndefinedField = "<undefined-field>";

    const string CatalogNamespace = "Dawnholder.Server.GameServer.Persistence.Catalog.";
    readonly Type _status;
    readonly Type _invalidField;
    readonly MethodInfo _mapFailure;

    internal CharacterMenuCatalogFailureMappingProbe()
    {
        Assembly server = typeof(GameSession).Assembly;
        Type? mapper = server.GetType(
            "Dawnholder.Server.GameServer.Sessions.CharacterMenuCatalogResultMapper");
        Assert.True(mapper is not null,
            "catalog-failure-mapping missing-product: CharacterMenuCatalogResultMapper is absent; later semantics not reached");
        Assert.True(mapper!.IsNotPublic && mapper.IsAbstract && mapper.IsSealed && !mapper.ContainsGenericParameters,
            "catalog-failure-mapping product-shape: the World mapper must be internal static and non-generic");
        const BindingFlags methods = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public
            | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        Assert.True(mapper.GetMethods(methods).Any(method => method.Name == "MapFailure"),
            "catalog-failure-mapping missing-method: MapFailure is absent; later semantics not reached");

        // Check the product first so a missing mapper cannot be hidden by a missing Core dependency.
        _status = RequireCoreEnum(server, "CatalogStatus");
        _invalidField = RequireCoreEnum(server, "InvalidField");
        MethodInfo? method = mapper.GetMethod("MapFailure", methods, null, new[] { _status, _invalidField }, null);
        Assert.True(method is not null,
            "catalog-failure-mapping signature: exact MapFailure(CatalogStatus, InvalidField) is absent; later semantics not reached");
        Assert.True(method!.IsStatic && method.IsAssembly && !method.IsGenericMethod,
            "catalog-failure-mapping signature: MapFailure must be internal static and non-generic");
        Assert.Equal(typeof((CharacterMenuResult, CharacterMenuInvalidField)), method.ReturnType);
        Assert.Equal(new[] { _status, _invalidField }, method.GetParameters().Select(parameter => parameter.ParameterType));
        _mapFailure = method;
    }

    internal (CharacterMenuResult Result, CharacterMenuInvalidField InvalidField) MapFailure(
        string statusName, string invalidFieldName)
        => Invoke(NamedValue(_status, statusName), FieldValue(invalidFieldName));

    internal (CharacterMenuResult Result, CharacterMenuInvalidField InvalidField) MapUndefinedStatus(
        string invalidFieldName)
        => Invoke(UndefinedValue(_status), FieldValue(invalidFieldName));

    static Type RequireCoreEnum(Assembly server, string name)
    {
        Type? type = server.GetType(CatalogNamespace + name);
        Assert.True(type is not null,
            $"catalog-failure-mapping missing-dependency: actual Core {name} is absent; later semantics not reached");
        Assert.True(type!.IsEnum && type.IsPublic && type.Assembly == server,
            $"catalog-failure-mapping dependency-shape: {name} must be the real public Core enum in the server assembly");
        return type;
    }

    static object NamedValue(Type type, string name)
    {
        Assert.True(Enum.GetNames(type).Contains(name, StringComparer.Ordinal),
            $"catalog-failure-mapping dependency-member: {type.FullName}.{name} is absent; this semantic case not reached");
        return Enum.Parse(type, name, ignoreCase: false);
    }

    static object UndefinedValue(Type enumType)
    {
        Type underlying = Enum.GetUnderlyingType(enumType);
        (decimal minimum, decimal maximum) = Type.GetTypeCode(underlying) switch
        {
            TypeCode.SByte => ((decimal)sbyte.MinValue, (decimal)sbyte.MaxValue),
            TypeCode.Byte => ((decimal)byte.MinValue, (decimal)byte.MaxValue),
            TypeCode.Int16 => ((decimal)short.MinValue, (decimal)short.MaxValue),
            TypeCode.UInt16 => ((decimal)ushort.MinValue, (decimal)ushort.MaxValue),
            TypeCode.Int32 => ((decimal)int.MinValue, (decimal)int.MaxValue),
            TypeCode.UInt32 => ((decimal)uint.MinValue, (decimal)uint.MaxValue),
            TypeCode.Int64 => ((decimal)long.MinValue, (decimal)long.MaxValue),
            TypeCode.UInt64 => ((decimal)ulong.MinValue, (decimal)ulong.MaxValue),
            _ => throw new InvalidOperationException($"Unsupported enum underlying type: {underlying}"),
        };

        // At most the number of declared values can occupy the candidates before an unused value.
        // Conversion is bounded by actual metadata; it assumes no Core ordinal or enum width.
        int declaredValues = Enum.GetValues(enumType).Length;
        for (int offset = 0; offset <= declaredValues; offset++)
        {
            decimal candidate = maximum - offset;
            if (candidate < minimum) break;
            object raw = Convert.ChangeType(candidate, underlying, CultureInfo.InvariantCulture);
            object value = Enum.ToObject(enumType, raw);
            if (!Enum.IsDefined(enumType, value)) return value;
        }

        Assert.Fail($"catalog-failure-mapping fixture: {enumType.FullName} has no representable undefined value");
        throw new InvalidOperationException("No undefined enum value available");
    }

    object FieldValue(string name)
        => name == UndefinedField ? UndefinedValue(_invalidField) : NamedValue(_invalidField, name);

    (CharacterMenuResult Result, CharacterMenuInvalidField InvalidField) Invoke(object status, object invalidField)
    {
        try
        {
            return Assert.IsType<(CharacterMenuResult Result, CharacterMenuInvalidField InvalidField)>(
                _mapFailure.Invoke(null, new[] { status, invalidField }));
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}
