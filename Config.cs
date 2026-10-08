using System.Text.Json;

namespace WukongBenchmark;

public class UiStep
{
    public string Key { get; set; } = "Enter";
    public int DelayMs { get; set; } = 1000;
}

public class AppConfig
{
    public string? InstallDir { get; set; }
    public int AppId { get; set; } = 3132990;
    public string ExeName { get; set; } = "b1_benchmark.exe";
    public string PresentMonPath { get; set; } = @"tools\PresentMon.exe";

    public int StartupWaitSec { get; set; } = 25;
    public int WindowTimeoutSec { get; set; } = 120;
    public int BenchmarkDurationSec { get; set; } = 170;
    public int WarmupTrimSec { get; set; } = 15;

    public List<UiStep> UiSteps { get; set; } = new()
    {
        new UiStep { Key = "Enter", DelayMs = 4000 },
        new UiStep { Key = "Enter", DelayMs = 1000 },
    };

    public static AppConfig Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "config.json");
        if (!File.Exists(path))
            return new AppConfig();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
        return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path), options) ?? new AppConfig();
    }
}
