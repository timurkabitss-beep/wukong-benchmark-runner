using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace WukongBenchmark;

/// <summary>Находит папку установки Benchmark Tool через реестр и файлы Steam.</summary>
public static class SteamLocator
{
    public static string? FindInstallDir(int appId)
    {
        string? steamPath = GetSteamPath();
        if (steamPath == null)
            return null;

        foreach (string library in GetLibraries(steamPath))
        {
            string manifest = Path.Combine(library, "steamapps", $"appmanifest_{appId}.acf");
            if (!File.Exists(manifest))
                continue;

            Match m = Regex.Match(File.ReadAllText(manifest), "\"installdir\"\\s+\"([^\"]+)\"");
            if (!m.Success)
                continue;

            string dir = Path.Combine(library, "steamapps", "common", m.Groups[1].Value);
            if (Directory.Exists(dir))
                return dir;
        }
        return null;
    }

    private static string? GetSteamPath()
    {
        string? path = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
        path ??= Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null) as string;
        return path == null ? null : Path.GetFullPath(path);
    }

    private static IEnumerable<string> GetLibraries(string steamPath)
    {
        // Основная библиотека всегда внутри папки Steam.
        yield return steamPath;

        string vdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(vdf))
            yield break;

        foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
            yield return m.Groups[1].Value.Replace(@"\\", @"\");
    }
}
