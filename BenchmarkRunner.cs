using System.Diagnostics;

namespace WukongBenchmark;

public record PassResult(
    string Name,
    string Description,
    Dictionary<string, string> Settings,
    FpsStats? Fps,
    Utilization? Util,
    string? Error);

/// <summary>Один проход: применить настройки → запустить Benchmark Tool → нажать кнопки → дождаться конца → собрать FPS.</summary>
public class BenchmarkRunner
{
    private readonly AppConfig _cfg;
    private readonly string _installDir;
    private readonly string _outDir;

    public BenchmarkRunner(AppConfig cfg, string installDir, string outDir)
    {
        _cfg = cfg;
        _installDir = installDir;
        _outDir = outDir;
    }

    public string IniPath => Path.Combine(_installDir, "b1", "Saved", "Config", "Windows", "GameUserSettings.ini");

    public PassResult Run(BenchmarkProfile profile)
    {
        Log($"=== Проход «{profile.Name}»: {profile.Description}");
        var pm = new PresentMonRunner(Path.Combine(AppContext.BaseDirectory, _cfg.PresentMonPath));
        var sampler = new UtilizationSampler();
        string csv = Path.Combine(_outDir, $"presentmon_{profile.Name}.csv");
        if (File.Exists(csv)) File.Delete(csv);

        try
        {
            KillGame();
            IniEditor.Apply(IniPath, profile.IniSettings);
            Log("Настройки записаны в GameUserSettings.ini");

            LaunchGame();
            IntPtr hwnd = WaitForWindow(TimeSpan.FromSeconds(_cfg.WindowTimeoutSec));
            Log($"Окно найдено, ждём загрузки меню ({_cfg.StartupWaitSec} c)...");
            Thread.Sleep(TimeSpan.FromSeconds(_cfg.StartupWaitSec));

            if (pm.IsAvailable)
                pm.Start(csv);
            else
                Log("PresentMon не найден в папке tools, FPS измерить не получится.");
            var clock = Stopwatch.StartNew();
            Thread.Sleep(2000);

            foreach (var step in _cfg.UiSteps)
            {
                IntPtr current = FindWindow();
                if (current != IntPtr.Zero)
                    hwnd = current;
                NativeMethods.BringToFront(hwnd);
                Thread.Sleep(300);
                NativeMethods.PressKey(step.Key);
                Log($"Нажато: {step.Key}");
                Thread.Sleep(step.DelayMs);
            }

            double benchStart = clock.Elapsed.TotalSeconds;
            Log($"Бенчмарк запущен. Ждём {_cfg.BenchmarkDurationSec} c...");

            WaitWhileRunning(_cfg.WarmupTrimSec);
            sampler.Start(); // замеры загрузки начинаем после прогрева, когда сцена уже загружена
            WaitWhileRunning(Math.Max(0, _cfg.BenchmarkDurationSec - _cfg.WarmupTrimSec));

            double benchEnd = clock.Elapsed.TotalSeconds;
            var util = sampler.Stop();
            if (pm.IsAvailable) pm.Stop();
            KillGame();

            FpsStats? fps = pm.IsAvailable
                ? FpsAnalyzer.Analyze(csv, benchStart + _cfg.WarmupTrimSec, benchEnd)
                : null;

            if (fps == null)
                Log("Не удалось получить FPS (проверь PresentMon и права администратора).");

            return new PassResult(profile.Name, profile.Description, profile.Flat(), fps, util, null);
        }
        catch (Exception ex)
        {
            sampler.Stop();
            pm.Stop();
            KillGame();
            Log($"Ошибка прохода: {ex.Message}");
            return new PassResult(profile.Name, profile.Description, profile.Flat(), null, null, ex.Message);
        }
    }

    private void LaunchGame()
    {
        string exe = Path.Combine(_installDir, _cfg.ExeName);
        if (File.Exists(exe))
        {
            Process.Start(new ProcessStartInfo { FileName = exe, WorkingDirectory = _installDir, UseShellExecute = true });
            Log($"Запущен {_cfg.ExeName}");
        }
        else
        {
            // Запасной вариант: запуск средствами Steam.
            Process.Start(new ProcessStartInfo { FileName = $"steam://rungameid/{_cfg.AppId}", UseShellExecute = true });
            Log("Запуск через Steam URI");
        }
    }

    /// <summary>Процессы, запущенные из папки Benchmark Tool (лаунчер и сам игровой процесс).</summary>
    private List<Process> GameProcesses()
    {
        var list = new List<Process>();
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                string? file = p.MainModule?.FileName;
                if (file != null && file.StartsWith(_installDir, StringComparison.OrdinalIgnoreCase))
                    list.Add(p);
            }
            catch
            {
                // нет доступа к системным процессам, они нам не нужны
            }
        }
        return list;
    }

    private IntPtr FindWindow()
    {
        foreach (var p in GameProcesses())
        {
            p.Refresh();
            if (p.MainWindowHandle != IntPtr.Zero)
                return p.MainWindowHandle;
        }
        return IntPtr.Zero;
    }

    private IntPtr WaitForWindow(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            IntPtr h = FindWindow();
            if (h != IntPtr.Zero)
                return h;
            Thread.Sleep(1000);
        }
        throw new TimeoutException("Окно Benchmark Tool не появилось за отведённое время.");
    }

    /// <summary>Ждёт заданное число секунд, но выходит раньше, если игра закрылась.</summary>
    private void WaitWhileRunning(int seconds)
    {
        for (int i = 0; i < seconds; i++)
        {
            if (GameProcesses().Count == 0)
                return;
            Thread.Sleep(1000);
        }
    }

    private void KillGame()
    {
        foreach (var p in GameProcesses())
        {
            try { p.Kill(entireProcessTree: true); p.WaitForExit(5000); } catch { }
        }
        Thread.Sleep(2000);
    }

    private static void Log(string message) => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
}
