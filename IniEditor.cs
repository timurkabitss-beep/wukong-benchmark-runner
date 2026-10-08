namespace WukongBenchmark;

/// <summary>
/// Минимальный редактор UE-ini: меняет только нужные ключи, остальные строки не трогает.
/// Если ключа нет, он добавляется в конец секции; если нет секции, она создаётся.
/// </summary>
public static class IniEditor
{
    public static void Apply(string path, Dictionary<string, Dictionary<string, string>> settings)
    {
        var lines = File.Exists(path) ? File.ReadAllLines(path).ToList() : new List<string>();

        foreach (var (section, keys) in settings)
        {
            foreach (var (key, value) in keys)
                SetValue(lines, section, key, value);
        }

        File.WriteAllLines(path, lines);
    }

    private static void SetValue(List<string> lines, string section, string key, string value)
    {
        string header = $"[{section}]";
        int start = lines.FindIndex(l => l.Trim().Equals(header, StringComparison.OrdinalIgnoreCase));

        if (start < 0)
        {
            if (lines.Count > 0 && lines[^1].Trim() != "")
                lines.Add("");
            lines.Add(header);
            lines.Add($"{key}={value}");
            return;
        }

        int end = start + 1;
        while (end < lines.Count && !lines[end].TrimStart().StartsWith("["))
            end++;

        for (int i = start + 1; i < end; i++)
        {
            int eq = lines[i].IndexOf('=');
            if (eq > 0 && lines[i][..eq].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = $"{key}={value}";
                return;
            }
        }

        // Вставляем после последней непустой строки секции.
        int insertAt = end;
        while (insertAt > start + 1 && lines[insertAt - 1].Trim() == "")
            insertAt--;
        lines.Insert(insertAt, $"{key}={value}");
    }
}
