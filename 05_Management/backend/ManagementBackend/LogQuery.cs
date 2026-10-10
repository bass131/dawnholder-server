using System.Globalization;

namespace Dawnholder.Management.Backend;

internal sealed record LogQuery(int Minutes, string[] Contains, int Limit, string? RunId)
{
    public static LogQuery Parse(IQueryCollection query, ServerRecordStore records)
    {
        int minutes = Integer(query, "minutes", 10, 1, 1440);
        int limit = Integer(query, "limit", 1000, 1, 5000);
        string[] contains = query["contains"].Select(value => value ?? "").ToArray();
        if (contains.Length > 5 || contains.Any(word => word.Length is < 1 or > 64)) throw Invalid();
        string? runId = null;
        if (query.TryGetValue("runId", out var values))
        {
            if (values.Count != 1 || string.IsNullOrEmpty(values[0]) || !records.HasRun(values[0]!)) throw Invalid();
            runId = values[0];
        }

        return new(minutes, contains, limit, runId);
    }

    private static ApiFailure Invalid() => new(400, "invalidQuery", "로그 조회 조건이 허용 범위를 벗어났습니다.");

    private static int Integer(IQueryCollection query, string key, int fallback, int minimum, int maximum)
    {
        if (!query.TryGetValue(key, out var values)) return fallback;
        if (values.Count != 1 || !int.TryParse(values[0], NumberStyles.None, CultureInfo.InvariantCulture, out int number)
            || number < minimum || number > maximum)
        {
            throw Invalid();
        }
        return number;
    }
}
