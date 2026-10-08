using System.Text;
using System.Text.Json;

namespace WukongBenchmark;

public static class Report
{
    public static string BuildText(HardwareInfo hw, IReadOnlyList<PassResult> passes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("==================== BLACK MYTH: WUKONG BENCHMARK ====================");
        sb.AppendLine($"Дата: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("--- Характеристики ПК ---");
        sb.AppendLine($"CPU : {hw.Cpu} ({hw.Cores} ядер / {hw.Threads} потоков)");
        foreach (var gpu in hw.Gpus)
            sb.AppendLine($"GPU : {gpu}");
        sb.AppendLine($"RAM : {hw.RamGb} ГБ, {hw.RamSpeed}");
        sb.AppendLine($"ОС  : {hw.Os}");
        sb.AppendLine($"Экран: {hw.Resolution}");

        foreach (var p in passes)
        {
            sb.AppendLine();
            sb.AppendLine($"--- {p.Name}-тест ---");
            sb.AppendLine($"Профиль: {p.Description}");

            if (p.Error != null)
            {
                sb.AppendLine($"ОШИБКА: {p.Error}");
                continue;
            }

            if (p.Fps != null)
            {
                sb.AppendLine($"Кадров измерено: {p.Fps.Frames} (процесс {p.Fps.Process})");
                sb.AppendLine($"FPS: avg {p.Fps.Avg:F1} | min {p.Fps.Min:F1} | max {p.Fps.Max:F1} | 1% low {p.Fps.Low1:F1} | 5% low {p.Fps.Low5:F1}");
            }
            else
            {
                sb.AppendLine("FPS: не получен");
            }

            if (p.Util != null)
                sb.AppendLine($"Средняя загрузка: CPU {Fmt(p.Util.CpuAvg)}% | GPU (3D) {Fmt(p.Util.GpuAvg)}%");

            sb.AppendLine("Применённые настройки:");
            foreach (var (k, v) in p.Settings)
                sb.AppendLine($"  {k} = {v}");
        }
        return sb.ToString();
    }

    public static void Save(string dir, HardwareInfo hw, IReadOnlyList<PassResult> passes)
    {
        File.WriteAllText(Path.Combine(dir, "report.txt"), BuildText(hw, passes), Encoding.UTF8);

        var json = new
        {
            date = DateTime.Now,
            hardware = hw,
            passes = passes.Select(p => new
            {
                p.Name,
                p.Description,
                p.Settings,
                p.Error,
                fps = p.Fps == null ? null : new
                {
                    p.Fps.Frames,
                    avg = Math.Round(p.Fps.Avg, 1),
                    min = Math.Round(p.Fps.Min, 1),
                    max = Math.Round(p.Fps.Max, 1),
                    low1 = Math.Round(p.Fps.Low1, 1),
                    low5 = Math.Round(p.Fps.Low5, 1),
                },
                utilization = p.Util == null ? null : new
                {
                    cpuAvgPercent = N(p.Util.CpuAvg),
                    gpuAvgPercent = N(p.Util.GpuAvg),
                },
            }),
        };

        var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        File.WriteAllText(Path.Combine(dir, "report.json"), JsonSerializer.Serialize(json, options), Encoding.UTF8);
    }

    private static string Fmt(double v) => double.IsNaN(v) ? "n/a" : v.ToString("F0");
    private static double? N(double v) => double.IsNaN(v) ? null : Math.Round(v, 1);
}
