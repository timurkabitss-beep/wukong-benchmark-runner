using System.Diagnostics;

namespace WukongBenchmark;

public record Utilization(double CpuAvg, double GpuAvg);

/// <summary>
/// Раз в секунду замеряет загрузку CPU и GPU (3D-движок). Это нужно, чтобы ДОКАЗАТЬ,
/// что CPU-тест нагружает процессор, а GPU-тест видеокарту.
/// GPU-счётчики суммируются по всем процессам системы, значение ограничено 100%.
/// </summary>
public class UtilizationSampler
{
    private readonly List<double> _cpu = new();
    private readonly List<double> _gpu = new();
    private CancellationTokenSource? _cts;
    private Task? _task;

    public void Start()
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _task = Task.Run(() => Loop(token));
    }

    public Utilization Stop()
    {
        _cts?.Cancel();
        try { _task?.Wait(3000); } catch { }
        return new Utilization(
            _cpu.Count > 0 ? _cpu.Average() : double.NaN,
            _gpu.Count > 0 ? _gpu.Average() : double.NaN);
    }

    private void Loop(CancellationToken token)
    {
        PerformanceCounter? cpu = null;
        var gpuCounters = new List<PerformanceCounter>();
        try
        {
            cpu = new PerformanceCounter("Processor Information", "% Processor Time", "_Total");
            cpu.NextValue();
        }
        catch { /* счётчики могут быть отключены */ }

        try
        {
            var cat = new PerformanceCounterCategory("GPU Engine");
            foreach (string inst in cat.GetInstanceNames().Where(n => n.EndsWith("engtype_3D")))
            {
                var c = new PerformanceCounter("GPU Engine", "Utilization Percentage", inst);
                c.NextValue();
                gpuCounters.Add(c);
            }
        }
        catch { }

        while (!token.IsCancellationRequested)
        {
            Thread.Sleep(1000);
            if (cpu != null)
                _cpu.Add(cpu.NextValue());

            if (gpuCounters.Count > 0)
            {
                double sum = 0;
                foreach (var c in gpuCounters)
                {
                    try { sum += c.NextValue(); } catch { }
                }
                _gpu.Add(Math.Min(100, sum));
            }
        }
    }
}
