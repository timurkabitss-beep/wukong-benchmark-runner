using System.Security.Principal;
using System.Text;
using WukongBenchmark;

Console.OutputEncoding = Encoding.UTF8;

if (!OperatingSystem.IsWindows())
{
    Console.WriteLine("Инструмент работает только в Windows.");
    return 1;
}

using (var identity = WindowsIdentity.GetCurrent())
{
    if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
        Console.WriteLine("ВНИМАНИЕ: запущено без прав администратора, PresentMon может не измерить FPS.");
}

var cfg = AppConfig.Load();
string? installDir = cfg.InstallDir ?? SteamLocator.FindInstallDir(cfg.AppId);
if (installDir == null || !Directory.Exists(installDir))
{
    Console.WriteLine($"Black Myth: Wukong Benchmark Tool (AppID {cfg.AppId}) не найден. " +
                      "Установите его в Steam или укажите InstallDir в config.json.");
    return 1;
}
Console.WriteLine($"Найден Benchmark Tool: {installDir}");

string outDir = Path.Combine(AppContext.BaseDirectory, "Results", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
Directory.CreateDirectory(outDir);

var runner = new BenchmarkRunner(cfg, installDir, outDir);
if (!File.Exists(runner.IniPath))
{
    Console.WriteLine($"Не найден {runner.IniPath}\n" +
                      "Запустите Benchmark Tool один раз вручную до главного меню, чтобы он создал файл настроек.");
    return 1;
}

// Резервная копия настроек пользователя: восстановим после тестов.
string backup = Path.Combine(outDir, "GameUserSettings.original.ini");
File.Copy(runner.IniPath, backup, overwrite: true);

var hardware = HardwareCollector.Collect();
var results = new List<PassResult>();

try
{
    results.Add(runner.Run(BenchmarkProfile.Cpu()));
    results.Add(runner.Run(BenchmarkProfile.Gpu()));
}
finally
{
    File.Copy(backup, runner.IniPath, overwrite: true);
    Console.WriteLine("Исходные настройки Benchmark Tool восстановлены.");
}

Report.Save(outDir, hardware, results);
Console.WriteLine();
Console.WriteLine(Report.BuildText(hardware, results));
Console.WriteLine($"Отчёты сохранены в: {outDir}");
return results.All(r => r.Error == null) ? 0 : 2;
