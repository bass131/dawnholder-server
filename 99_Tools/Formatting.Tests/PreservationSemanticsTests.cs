using System.Text;
using Dawnholder.Tools.Formatting.Tests.Support;

namespace Dawnholder.Tools.Formatting.Tests;

/// <summary>
/// Requirement fixtures for the whitespace proof, evaluated with each product project's actual
/// Debug and Release parse options. Allowed layout changes must pass; any literal, comment,
/// directive or unproven inactive-region difference must fail.
/// </summary>
[Collection(ProductParseOptionsCollection.Name)]
public sealed class PreservationSemanticsTests
{
    private const string Server = "02_Server/GameServer.Tests/GameServer.Tests.csproj";
    private const string Shared = "98_Shared/Shared.csproj";
    private static readonly UTF8Encoding Utf8 = new(false);
    private readonly ProductParseOptions _options;

    public PreservationSemanticsTests(ProductParseOptions options)
    {
        _options = options;
    }

    private static readonly string[] ProjectNames =
    [
        "98_Shared/Shared.csproj",
        "04_ClientNet/Dawnholder.Client.Net.csproj",
        "02_Server/Network/Dawnholder.Server.Network.csproj",
        "02_Server/GameServer/GameServer.csproj",
        "99_Tools/PacketGenerator/PacketGenerator.csproj",
        "99_Tools/headless-bot/HeadlessBot.csproj",
        "99_Tools/BgmComposer/BgmComposer.csproj",
        "02_Server/GameServer.Tests/GameServer.Tests.csproj",
    ];

    public static TheoryData<string> AllProjects => new(ProjectNames);

    [Fact]
    public void ActualOptions_DistinguishDebugAndRelease()
    {
        foreach (var project in ProjectNames)
        {
            var debug = _options.Get(project, "Debug").PreprocessorSymbolNames.ToHashSet();
            var release = _options.Get(project, "Release").PreprocessorSymbolNames.ToHashSet();
            Assert.Contains("DEBUG", debug);
            Assert.DoesNotContain("DEBUG", release);
            Assert.Contains("RELEASE", release);
        }
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public void LayoutOnlyChanges_Pass(string project)
    {
        var before = Lines(
            "namespace Fixture;",
            "public static class Sample",
            "{",
            "  public static string Run(int value)",
            "  {",
            "      var text=\"a  b\";",
            "      var point = new Point { X = 1, Y = 2 };",
            "      var hole = $\"v={ value }\";",
            "",
            "",
            "      return text+hole + point;",
            "  }",
            "}");
        var after = Lines(
            "namespace Fixture;",
            "public static class Sample",
            "{",
            "    public static string Run(int value)",
            "    {",
            "        var text = \"a  b\";",
            "        var point = new Point",
            "        {",
            "            X = 1,",
            "            Y = 2",
            "        };",
            "        var hole = $\"v={value}\";",
            "",
            "        return text + hole + point;",
            "    }",
            "}");
        var result = Compare(project, before, after);
        Assert.Equal(2, result.Conditions);
        Assert.Equal(0, result.InactiveRegions);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public void BomCrLfAndMissingFinalNewline_RemovedByFormatter_Pass(string project)
    {
        var body = "namespace Fixture;\r\npublic static class Sample\r\n{\r\n    public const string Name = \"x\";\r\n}";
        var before = new byte[] { 0xef, 0xbb, 0xbf }.Concat(Utf8.GetBytes(body)).ToArray();
        var after = Utf8.GetBytes(body.Replace("\r\n", "\n", StringComparison.Ordinal) + "\n");
        Assert.Equal(2, CompareBytes(project, before, after).Conditions);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public void DebugOnlyBranchWhitespace_ProvedByActualDebugCondition(string project)
    {
        // Mirrors the real CheatBuildGateTests change: the Debug branch is disabled text in Release.
        var before = Gate("        Check(registered,  \"debug\");", "        Check(registered, \"release\");");
        var after = Gate("        Check(registered, \"debug\");", "        Check(registered, \"release\");");
        var result = Compare(project, before, after);
        Assert.True(result.InactiveRegions >= 1, $"Expected an inactive-region proof, got {result.InactiveRegions}.");
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public void ReleaseOnlyBranchWhitespace_ProvedByActualReleaseCondition(string project)
    {
        var before = Gate("        Check(registered, \"debug\");", "        Check(  registered, \"release\");");
        var after = Gate("        Check(registered, \"debug\");", "        Check(registered, \"release\");");
        Assert.True(Compare(project, before, after).InactiveRegions >= 1);
    }

    [Fact]
    public void DirectiveSpacingAndDocumentationIndentation_Pass()
    {
        var before = Lines(
            "namespace Fixture;",
            "public static class Sample",
            "{",
            "  /// <summary>Keeps text.</summary>",
            "#if  DEBUG",
            "  public const int Mode = 1;",
            "#else",
            "  public const int Mode = 2;",
            "#endif",
            "}");
        var after = Lines(
            "namespace Fixture;",
            "public static class Sample",
            "{",
            "    /// <summary>Keeps text.</summary>",
            "#if DEBUG",
            "    public const int Mode = 1;",
            "#else",
            "    public const int Mode = 2;",
            "#endif",
            "}");
        Compare(Server, before, after);
        Compare(Shared, before, after);
    }

    public static TheoryData<string, string, string> Violations => new()
    {
        { "string literal", Member("var s = \"a  b\";"), Member("var s = \"a b\";") },
        { "verbatim trailing whitespace", Member("var s = @\"x   \nline2\";"), Member("var s = @\"x\nline2\";") },
        { "verbatim CRLF value", Member("var s = @\"x\r\nline2\";"), Member("var s = @\"x\nline2\";") },
        { "raw string indentation", Member("var s = \"\"\"\n          indented\n        \"\"\";"), Member("var s = \"\"\"\n        indented\n        \"\"\";") },
        { "raw string trailing whitespace", Member("var s = \"\"\"\n        kept   \n        \"\"\";"), Member("var s = \"\"\"\n        kept\n        \"\"\";") },
        { "interpolated text", Member("var s = $\"a  {Value}\";"), Member("var s = $\"a {Value}\";") },
        { "raw interpolated text", Member("var s = $\"\"\"\n        a  {Value}\n        \"\"\";"), Member("var s = $\"\"\"\n        a {Value}\n        \"\"\";") },
        { "char literal", Member("var c = ' ';"), Member("var c = '\t';") },
        { "utf8 literal", Member("var u = \"a  b\"u8;"), Member("var u = \"a b\"u8;") },
        { "single-line comment", Member("// keep  two\n        var x = 1;"), Member("// keep two\n        var x = 1;") },
        { "multi-line comment", Member("/* a  b */ var x = 1;"), Member("/* a b */ var x = 1;") },
        { "block comment body indentation", Member("/*\n           body\n        */ var x = 1;"), Member("/*\n        body\n        */ var x = 1;") },
        { "documentation text", Doc("/// <summary>a  b</summary>"), Doc("/// <summary>a b</summary>") },
        { "documentation attribute", Doc("/// <param name=\"value\">v</param>"), Doc("/// <param name=\"other\">v</param>") },
        { "directive symbol", Directive("#if DEBUG"), Directive("#if RELEASE") },
        { "directive expression", Directive("#if DEBUG || TRACE"), Directive("#if DEBUG && TRACE") },
        { "region text", Region("#region Combat  rules"), Region("#region Combat rules") },
        { "pragma code", Member("#pragma warning disable CS0168\n        var x = 1;"), Member("#pragma warning disable CS0169\n        var x = 1;") },
        { "token kind", Member("var x = Value + 1;"), Member("var x = Value - 1;") },
        { "identifier", Member("var x = Value;"), Member("var y = Value;") },
        { "statement order", Member("var x = 1;\n        var y = 2;"), Member("var y = 2;\n        var x = 1;") },
        { "release branch literal", Gate("        Check(registered, \"debug\");", "        Check(registered, \"release\");"), Gate("        Check(registered, \"debug\");", "        Check(registered, \"release \");") },
        { "never-active region", Never("        var  x = 1;"), Never("        var x = 1;") },
        { "never-active blank line", Never("        var x = 1;\n\n"), Never("        var x = 1;\n") },
        { "nested never-active in debug", Nested("          x  "), Nested("          x") },
        { "parse error introduced", Member("var x = 1;"), Member("var x = ;") },
    };

    [Theory]
    [MemberData(nameof(Violations))]
    public void SemanticDifferences_FailInServerProject(string name, string before, string after)
    {
        var error = Assert.ThrowsAny<Exception>(() => Compare(Server, before, after));
        Assert.True(error is InvalidDataException, $"{name}: expected a reported difference, got {error.GetType().Name}: {error.Message}");
    }

    [Theory]
    [MemberData(nameof(Violations))]
    public void SemanticDifferences_FailInUnityPluginProject(string name, string before, string after)
    {
        var error = Assert.ThrowsAny<Exception>(() => Compare(Shared, before, after));
        Assert.True(error is InvalidDataException, $"{name}: expected a reported difference, got {error.GetType().Name}: {error.Message}");
    }

    [Fact]
    public void MissingReleaseCondition_IsNotAProof()
    {
        var text = Member("var x = 1;");
        var sources = new[] { new LoadedSource(Server, "Debug", "Fixture.cs", _options.Get(Server, "Debug")) };
        Assert.Throws<InvalidDataException>(() => Preservation.CompareSyntax("Fixture.cs", Utf8.GetBytes(text), Utf8.GetBytes(text), sources));
    }

    [Fact]
    public void InvalidUtf8_IsRejected()
    {
        var valid = Utf8.GetBytes(Member("var x = 1;"));
        var invalid = valid.Concat(new byte[] { 0xc3, 0x28 }).ToArray();
        Assert.ThrowsAny<Exception>(() => CompareBytes(Server, valid, invalid));
    }

    private static string Lines(params string[] lines) => string.Join("\n", lines) + "\n";

    private static string Member(string statements) => Lines(
        "namespace Fixture;",
        "public static class Sample",
        "{",
        "    public static int Value => 1;",
        "    public static void Run()",
        "    {",
        "        " + statements,
        "    }",
        "}");

    private static string Doc(string comment) => Lines(
        "namespace Fixture;",
        "public static class Sample",
        "{",
        "    " + comment,
        "    public static int Run(int value) => value;",
        "}");

    private static string Directive(string condition) => Lines(
        "namespace Fixture;",
        "public static class Sample",
        "{",
        condition,
        "    public const int Mode = 1;",
        "#endif",
        "}");

    private static string Region(string region) => Lines(
        "namespace Fixture;",
        "public static class Sample",
        "{",
        "    " + region,
        "    public const int Mode = 1;",
        "    #endregion",
        "}");

    private static string Gate(string debugLine, string releaseLine) => Lines(
        "namespace Fixture;",
        "public static class Sample",
        "{",
        "    public static void Run(bool registered)",
        "    {",
        "#if DEBUG",
        debugLine,
        "#else",
        releaseLine,
        "#endif",
        "    }",
        "    private static void Check(bool value, string message) { }",
        "}");

    private static string Never(string body) => Lines(
        "namespace Fixture;",
        "public static class Sample",
        "{",
        "    public static void Run()",
        "    {",
        "#if DAWNHOLDER_NEVER_DEFINED",
        body,
        "#endif",
        "    }",
        "}");

    private static string Nested(string body) => Lines(
        "namespace Fixture;",
        "public static class Sample",
        "{",
        "    public static void Run()",
        "    {",
        "#if DEBUG",
        "#if DAWNHOLDER_NEVER_DEFINED",
        body,
        "#endif",
        "#endif",
        "    }",
        "}");

    private (int Conditions, int InactiveRegions) Compare(string project, string before, string after) =>
        CompareBytes(project, Utf8.GetBytes(before), Utf8.GetBytes(after));

    private (int Conditions, int InactiveRegions) CompareBytes(string project, byte[] before, byte[] after)
    {
        var sources = new[] { "Debug", "Release" }.Select(configuration => new LoadedSource(project, configuration, "Fixture.cs", _options.Get(project, configuration))).ToArray();
        return Preservation.CompareSyntax("Fixture.cs", before, after, sources);
    }
}

[CollectionDefinition(Name)]
public sealed class ProductParseOptionsCollection : ICollectionFixture<ProductParseOptions>
{
    public const string Name = "Product parse options";
}
