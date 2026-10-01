using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Dawnholder.Tools.Formatting;

internal sealed record FileProof(string Path, string BeforeSha256, string AfterSha256, bool Changed, bool BeforeBom, bool AfterBom, bool BeforeFinalNewline, bool AfterFinalNewline, int Conditions, int InactiveRegionsProved);
internal sealed record PreservationResult(string CheckoutSha, string WorkTreeStatus, SdkSelection Sdk, List<ProjectInputs> Projects, List<FileProof> Files);
internal sealed record ParsedCondition(LoadedSource Source, SyntaxNode Before, SyntaxNode After, SourceText BeforeText, SourceText AfterText);
internal sealed record SemanticEvent(int Position, string Value);

internal static class Preservation
{
    public static async Task<PreservationResult> CompareAsync(string before, string after, InputManifest manifest, SdkSelection sdk)
    {
        var afterSdk = await SdkSelection.VerifyAsync(sdk.Executable, after);
        if (afterSdk.FormatterVersion != sdk.FormatterVersion) throw new InvalidDataException("Formatter version changed.");
        foreach (var file in manifest.Files.Where(file => file.Category is not ("handwritten-source" or "tool-source")))
            if (InputPaths.Hash(InputPaths.Resolve(after, file.Path)) != file.Sha256) throw new InvalidDataException($"Protected/configuration input changed: {file.Path}");
        var beforeWorkspace = await WorkspaceInputs.LoadAsync(before, sdk, manifest.Files.Select(file => file.Path));
        var afterWorkspace = await WorkspaceInputs.LoadAsync(after, afterSdk, manifest.Files.Select(file => file.Path));
        if (JsonSerializer.Serialize(beforeWorkspace.Projects) != JsonSerializer.Serialize(afterWorkspace.Projects)) throw new InvalidDataException("Compile set/parse options changed during formatting.");
        var proofs = new List<FileProof>();
        foreach (var file in manifest.Files.Where(file => file.Category is "handwritten-source" or "tool-source" or "generated-source"))
        {
            var beforeBytes = await File.ReadAllBytesAsync(InputPaths.Resolve(before, file.Path));
            var afterBytes = await File.ReadAllBytesAsync(InputPaths.Resolve(after, file.Path));
            var beforeText = Decode(beforeBytes);
            var afterText = Decode(afterBytes);
            var syntax = CompareSyntax(file.Path, beforeBytes, afterBytes, beforeWorkspace.Sources.Where(source => source.Path == file.Path).ToArray());
            var changed = !beforeBytes.SequenceEqual(afterBytes);
            if (file.Category != "generated-source" && (HasBom(afterBytes) || afterText.ToString().Contains('\r') || !afterText.ToString().EndsWith('\n'))) throw new InvalidDataException($"Output encoding/EOL/EOF violates formatting policy: {file.Path}");
            proofs.Add(new FileProof(file.Path, file.Sha256, InputPaths.Hash(InputPaths.Resolve(after, file.Path)), changed, HasBom(beforeBytes), HasBom(afterBytes), beforeText.ToString().EndsWith('\n'), afterText.ToString().EndsWith('\n'), syntax.Conditions, syntax.InactiveRegions));
        }
        return new PreservationResult(manifest.CheckoutSha, manifest.WorkTreeStatus, sdk, beforeWorkspace.Projects, proofs);
    }

    internal static (int Conditions, int InactiveRegions) CompareSyntax(string path, byte[] before, byte[] after, IReadOnlyList<LoadedSource> sources)
    {
        var beforeText = Decode(before);
        var afterText = Decode(after);
        var conditions = new List<ParsedCondition>();
        foreach (var source in sources)
        {
            var oldTree = CSharpSyntaxTree.ParseText(beforeText, source.Options, path);
            var newTree = CSharpSyntaxTree.ParseText(afterText, source.Options, path);
            foreach (var tree in new[] { oldTree, newTree })
                if (tree.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)) throw new InvalidDataException($"Parse error in {path}/{source.Configuration}: {string.Join("; ", tree.GetDiagnostics())}");
            var oldRoot = oldTree.GetRoot();
            var newRoot = newTree.GetRoot();
            var oldEvents = Events(oldRoot);
            var newEvents = Events(newRoot);
            if (!oldEvents.Select(item => item.Value).SequenceEqual(newEvents.Select(item => item.Value), StringComparer.Ordinal))
            {
                var index = Enumerable.Range(0, Math.Min(oldEvents.Count, newEvents.Count)).FirstOrDefault(index => oldEvents[index].Value != newEvents[index].Value, -1);
                throw new InvalidDataException($"Token/literal/comment/directive difference: {path}/{source.Configuration}, event {index}: {(index < 0 ? "length mismatch" : oldEvents[index].Value + " -> " + newEvents[index].Value)}");
            }
            conditions.Add(new ParsedCondition(source, oldRoot, newRoot, beforeText, afterText));
        }
        if (!sources.Any(source => source.Configuration == "Debug") || !sources.Any(source => source.Configuration == "Release")) throw new InvalidDataException($"Missing Debug/Release proof: {path}");
        return (conditions.Count, ProveInactive(path, conditions));
    }

    private static SourceText Decode(byte[] bytes)
    {
        var offset = HasBom(bytes) ? 3 : 0;
        var text = new UTF8Encoding(false, true).GetString(bytes, offset, bytes.Length - offset);
        return SourceText.From(text, new UTF8Encoding(false));
    }

    private static bool HasBom(byte[] bytes) => bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf;
    private static string Lines(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    private static string Encode(params object?[] fields) => JsonSerializer.Serialize(fields);

    private static List<SemanticEvent> Events(SyntaxNode root)
    {
        var events = root.DescendantTokens().Select(token => new SemanticEvent(token.SpanStart, Encode("token", token.RawKind, token.Text, token.Value?.GetType().FullName, token.ValueText, token.IsMissing))).ToList();
        foreach (var trivia in root.DescendantTrivia())
        {
            switch (trivia.Kind())
            {
                case SyntaxKind.WhitespaceTrivia:
                case SyntaxKind.EndOfLineTrivia:
                case SyntaxKind.DisabledTextTrivia:
                    break;
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                    events.Add(new SemanticEvent(trivia.SpanStart, Encode("comment", trivia.RawKind, Lines(trivia.ToString()))));
                    break;
                case SyntaxKind.SingleLineDocumentationCommentTrivia:
                case SyntaxKind.MultiLineDocumentationCommentTrivia:
                    var structure = trivia.GetStructure() ?? throw new InvalidDataException("Documentation structure missing.");
                    var docTokens = structure.DescendantTokens().Select(token => Encode(token.RawKind, Lines(token.Text), Lines(token.ValueText), token.IsMissing)).ToArray();
                    var docTrivia = structure.DescendantTrivia().Where(item => !item.IsKind(SyntaxKind.WhitespaceTrivia) && !item.IsKind(SyntaxKind.EndOfLineTrivia)).Select(item => item.IsKind(SyntaxKind.DocumentationCommentExteriorTrivia) ? Encode(item.RawKind, item.ToString().TrimStart(' ', '\t')) : Encode(item.RawKind, Lines(item.ToString()))).ToArray();
                    events.Add(new SemanticEvent(trivia.SpanStart, Encode("documentation", trivia.RawKind, docTokens, docTrivia)));
                    break;
                default:
                    if (trivia.GetStructure() is DirectiveTriviaSyntax directive)
                    {
                        var tokens = directive.DescendantTokens().Select(token => Encode(token.RawKind, token.Text, token.ValueText, token.IsMissing)).ToArray();
                        var contents = directive.DescendantTrivia().Where(item => !item.IsKind(SyntaxKind.WhitespaceTrivia) && !item.IsKind(SyntaxKind.EndOfLineTrivia)).Select(item => Encode(item.RawKind, Lines(item.ToString()))).ToArray();
                        bool? taken = directive switch { IfDirectiveTriviaSyntax item => item.BranchTaken, ElifDirectiveTriviaSyntax item => item.BranchTaken, ElseDirectiveTriviaSyntax item => item.BranchTaken, _ => null };
                        events.Add(new SemanticEvent(trivia.SpanStart, Encode("directive", directive.RawKind, directive.IsActive, taken, tokens, contents)));
                    }
                    else
                    {
                        // Unknown/skipped trivia must retain its exact text; never erase it as whitespace.
                        events.Add(new SemanticEvent(trivia.SpanStart, Encode("other", trivia.RawKind, trivia.ToFullString())));
                    }
                    break;
            }
        }
        return events.OrderBy(item => item.Position).ToList();
    }

    private static List<TextSpan> Blocks(SyntaxNode root, SourceText text)
    {
        var result = new List<TextSpan>();
        var start = 0;
        foreach (var directive in root.DescendantTrivia().Where(trivia => trivia.GetStructure() is DirectiveTriviaSyntax))
        {
            var line = text.Lines.GetLineFromPosition(directive.SpanStart);
            result.Add(TextSpan.FromBounds(start, line.Start));
            start = text.Lines.GetLineFromPosition(Math.Max(directive.SpanStart, directive.Span.End - 1)).EndIncludingLineBreak;
        }
        result.Add(TextSpan.FromBounds(start, text.Length));
        return result;
    }

    private static bool Disabled(SyntaxNode root, TextSpan span) => root.DescendantTrivia().Any(trivia => trivia.IsKind(SyntaxKind.DisabledTextTrivia) && trivia.Span.OverlapsWith(span));

    private static int ProveInactive(string path, List<ParsedCondition> conditions)
    {
        var proved = new HashSet<int>();
        var oldBlocks = conditions.Select(condition => Blocks(condition.Before, condition.BeforeText)).ToArray();
        var newBlocks = conditions.Select(condition => Blocks(condition.After, condition.AfterText)).ToArray();
        if (oldBlocks.Concat(newBlocks).Any(blocks => blocks.Count != oldBlocks[0].Count)) throw new InvalidDataException($"Directive regions changed: {path}");
        for (var index = 0; index < oldBlocks[0].Count; index++)
        {
            for (var variant = 0; variant < conditions.Count; variant++)
            {
                var condition = conditions[variant];
                var oldSpan = oldBlocks[variant][index];
                var newSpan = newBlocks[variant][index];
                if (!Disabled(condition.Before, oldSpan) && !Disabled(condition.After, newSpan)) continue;
                if (condition.BeforeText.ToString(oldSpan) == condition.AfterText.ToString(newSpan)) continue;
                var active = Enumerable.Range(0, conditions.Count).Any(candidate =>
                {
                    var matching = conditions[candidate];
                    var left = oldBlocks[candidate][index];
                    var right = newBlocks[candidate][index];
                    if (Disabled(matching.Before, left) || Disabled(matching.After, right)) return false;
                    var beforeEvents = Events(matching.Before).Where(item => left.Contains(item.Position)).Select(item => item.Value);
                    var afterEvents = Events(matching.After).Where(item => right.Contains(item.Position)).Select(item => item.Value);
                    return beforeEvents.SequenceEqual(afterEvents, StringComparer.Ordinal);
                });
                if (!active) throw new InvalidDataException($"Changed inactive region lacks an actual active configuration: {path}, block {index}.");
                proved.Add(index);
            }
        }
        return proved.Count;
    }
}
