using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace Dawnholder.Client.Tests.PlayMode
{
    // The kill reward text as a player sees it, without a product type name (kill reward goal 「시험 층」): every active
    // world-space TextMeshPro whose tag-free text has a line starting with "+". These readings are the observation
    // contract (.backups/verification/2026-10-30-kill-reward-float-text/pretest-01/observation-contract.md 「2. 관측 가정」);
    // an implementation that conflicts with them asks the Effect lead instead of changing them.
    internal static class KillRewardFloatTextProbe
    {
        static readonly Regex LineBreakTags = new Regex(@"<br\s*/?>", RegexOptions.IgnoreCase);
        static readonly Regex Tags = new Regex("<[^<>]*>");

        // TextMeshProUGUI is a sibling type of TextMeshPro, so screen UI labels are never returned.
        internal static TextMeshPro[] Find() => UnityEngine.Object.FindObjectsByType<TextMeshPro>()
            .Where(text => text != null && text.isActiveAndEnabled && TextLines(text).Any(line => line.StartsWith("+", StringComparison.Ordinal)))
            .ToArray();

        // Top first (world y descending), then left to right; each component keeps its own line order.
        internal static string[] Lines(TextMeshPro[] texts) => Ordered(texts).SelectMany(TextLines).ToArray();

        internal static Vector3 Center(TextMeshPro[] texts)
        {
            if (texts.Length == 0) return Vector3.zero;
            Vector3 sum = Vector3.zero;
            foreach (TextMeshPro text in texts) sum += text.transform.position;
            return sum / texts.Length;
        }

        internal static float Opacity(TextMeshPro[] texts) => texts.Length == 0 ? 0f : texts.Max(text => text.alpha * text.color.a);

        internal static string Describe(TextMeshPro[] texts) => texts.Length == 0
            ? "none"
            : string.Join(" | ", Ordered(texts).Select(text =>
                $"'{string.Join("\\n", TextLines(text))}' at ({text.transform.position.x:F2}, {text.transform.position.y:F2}) " +
                $"opacity {text.alpha * text.color.a:F2} font {(text.font != null ? text.font.name : "null")}"));

        // Every active world-space TextMeshPro, kill text or not, so a failed check shows what else was drawn.
        internal static string DescribeWorldTexts() => string.Join(" | ", UnityEngine.Object.FindObjectsByType<TextMeshPro>()
            .Where(text => text != null && text.isActiveAndEnabled)
            .Select(text => $"{text.name}='{string.Join("\\n", TextLines(text))}'"));

        static IEnumerable<TextMeshPro> Ordered(TextMeshPro[] texts) => texts
            .OrderByDescending(text => text.transform.position.y)
            .ThenBy(text => text.transform.position.x);

        static string[] TextLines(TMP_Text text)
        {
            string plain = Tags.Replace(LineBreakTags.Replace(text.text ?? string.Empty, "\n"), string.Empty);
            return plain.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).ToArray();
        }
    }
}
