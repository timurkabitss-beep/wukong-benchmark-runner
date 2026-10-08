using System.Management;

namespace WukongBenchmark;

public record HardwareInfo(
    string Cpu, int Cores, int Threads,
    List<string> Gpus,
    double RamGb, string RamSpeed,
    string Os, string Resolution);

public static class HardwareCollector
{
    public static HardwareInfo Collect()
    {
        string cpu = "?";
        int cores = 0, threads = 0;
        foreach (var o in Query("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor"))
        {
            cpu = o["Name"]?.ToString()?.Trim() ?? "?";
            cores += Convert.ToInt32(o["NumberOfCores"]);
            threads += Convert.ToInt32(o["NumberOfLogicalProcessors"]);
        }

        var gpus = Query("SELECT Name, DriverVersion FROM Win32_VideoController")
            .Select(o => $"{o["Name"]} (драйвер {o["DriverVersion"]})")
            .ToList();

        double ramGb = 0;
        foreach (var o in Query("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
            ramGb = Convert.ToDouble(o["TotalPhysicalMemory"]) / (1024.0 * 1024 * 1024);

        var speeds = Query("SELECT Speed FROM Win32_PhysicalMemory")
            .Select(o => Convert.ToInt32(o["Speed"])).Distinct().ToList();
        string ramSpeed = speeds.Count > 0 ? string.Join("/", speeds) + " MHz" : "n/a";

        string os = "?";
        foreach (var o in Query("SELECT Caption, Version FROM Win32_OperatingSystem"))
            os = $"{o["Caption"]} {o["Version"]}".Trim();

        var (w, h) = NativeMethods.GetScreenSize();
        return new HardwareInfo(cpu, cores, threads, gpus, Math.Round(ramGb, 1), ramSpeed, os, $"{w}x{h}");
    }

    private static List<ManagementBaseObject> Query(string wql)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(wql);
            return searcher.Get().Cast<ManagementBaseObject>().ToList();
        }
        catch
        {
            return new List<ManagementBaseObject>();
        }
    }
}
