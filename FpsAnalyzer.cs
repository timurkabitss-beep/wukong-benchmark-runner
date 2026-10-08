using System.Globalization;

namespace WukongBenchmark;

public record FpsStats(int Frames, double Avg, double Min, double Max, double Low1, double Low5, string Process);

/// <summary>Считает FPS-метрики из CSV, который пишет PresentMon.</summary>
public static class FpsAnalyzer
{
    public static FpsStats? Analyze(string csvPath, double fromSec, double toSec)
    {
        if (!File.Exists(csvPath))
            return null;

        string[] lines = File.ReadAllLines(csvPath);
        if (lines.Length < 2)
            return null;

        string[] header = lines[0].Split(',');
        int iApp = Array.IndexOf(header, "Application");
        int iTime = Array.IndexOf(header, "TimeInSeconds");
        int iMs = Array.IndexOf(header, "MsBetweenPresents");
        if (iMs < 0) iMs = Array.IndexOf(header, "FrameTime");
        if (iApp < 0 || iTime < 0 || iMs < 0)
            return null;

        // PresentMon пишет кадры всех программ, поэтому берём процесс игры (b1*) с наибольшим числом кадров.
        var frames = new Dictionary<string, List<double>>();
        foreach (string line in lines.Skip(1))
        {
            string[] c = line.Split(',');
            if (c.Length <= Math.Max(iApp, Math.Max(iTime, iMs)))
                continue;
            if (!c[iApp].StartsWith("b1", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!double.TryParse(c[iTime], NumberStyles.Float, CultureInfo.InvariantCulture, out double t))
                continue;
            if (t < fromSec || t > toSec)
                continue;
            if (!double.TryParse(c[iMs], NumberStyles.Float, CultureInfo.InvariantCulture, out double ms) || ms <= 0)
                continue;

            if (!frames.TryGetValue(c[iApp], out var list))
                frames[c[iApp]] = list = new List<double>();
            list.Add(ms);
        }

        if (frames.Count == 0)
            return null;

        var best = frames.OrderByDescending(kv => kv.Value.Count).First();
        var ft = best.Value.OrderBy(x => x).ToList(); // кадровое время по возрастанию

        double Fps(double ms) => 1000.0 / ms;
        double Percentile(double p) => ft[Math.Min(ft.Count - 1, (int)Math.Ceiling(p * ft.Count) - 1)];

        return new FpsStats(
            Frames: ft.Count,
            Avg: Fps(ft.Average()),
            Min: Fps(ft[^1]),
            Max: Fps(ft[0]),
            Low1: Fps(Percentile(0.99)),
            Low5: Fps(Percentile(0.95)),
            Process: best.Key);
    }
}
