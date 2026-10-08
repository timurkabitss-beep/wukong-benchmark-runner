using System.Diagnostics;

namespace WukongBenchmark;

/// <summary>
/// Запускает PresentMon 1.x (консольная версия) в фоне: он пишет в CSV время каждого кадра.
/// Положи PresentMon.exe в папку tools (см. README).
/// </summary>
public class PresentMonRunner
{
    private const string SessionName = "WukongBenchSession";
    private readonly string _exe;
    private Process? _process;

    public PresentMonRunner(string exePath) => _exe = exePath;

    public bool IsAvailable => File.Exists(_exe);

    public void Start(string csvPath)
    {
        _process = Process.Start(new ProcessStartInfo
        {
            FileName = _exe,
            Arguments = $"-session_name {SessionName} -output_file \"{csvPath}\" -stop_existing_session -no_top -dont_restart_as_admin",
            UseShellExecute = false,
            CreateNoWindow = true,
        });
    }

    public void Stop()
    {
        if (_process == null)
            return;

        try
        {
            // Штатная остановка: PresentMon сам дозаписывает и закрывает CSV.
            Process.Start(new ProcessStartInfo
            {
                FileName = _exe,
                Arguments = $"-session_name {SessionName} -terminate_existing",
                UseShellExecute = false,
                CreateNoWindow = true,
            })?.WaitForExit(5000);

            if (!_process.WaitForExit(10000))
                _process.Kill();
        }
        catch
        {
            // процесс уже завершился
        }
    }
}
