namespace WukongBenchmark;

/// <summary>Набор настроек для одного прохода бенчмарка.</summary>
public class BenchmarkProfile
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public Dictionary<string, Dictionary<string, string>> IniSettings { get; init; } = new();

    // Для отчёта: те же настройки в плоском виде.
    public Dictionary<string, string> Flat() =>
        IniSettings.SelectMany(s => s.Value).ToDictionary(kv => kv.Key, kv => kv.Value);

    private const string Display = "/Script/Engine.GameUserSettings";
    private const string Scalability = "ScalabilityGroups";

    /// <summary>
    /// CPU-тест: низкое разрешение + минимальное внутреннее разрешение рендера + всё на минимум,
    /// без трассировки лучей и апскейлеров. Дальность прорисовки оставлена повышенной:
    /// она увеличивает число объектов, которые процессор должен обработать (culling, draw calls),
    /// почти не добавляя нагрузки на GPU на таком разрешении.
    /// </summary>
    public static BenchmarkProfile Cpu() => new()
    {
        Name = "CPU",
        Description = "720p, render scale 50%, все качества на минимум, RT выкл., дальность прорисовки повышена",
        IniSettings = new()
        {
            [Display] = Resolution(1280, 720, fullscreenMode: "1"),
            [Scalability] = new()
            {
                ["sg.ResolutionQuality"] = "50",
                ["sg.ViewDistanceQuality"] = "3",
                ["sg.AntiAliasingQuality"] = "0",
                ["sg.ShadowQuality"] = "0",
                ["sg.GlobalIlluminationQuality"] = "0",
                ["sg.ReflectionQuality"] = "0",
                ["sg.PostProcessQuality"] = "0",
                ["sg.TextureQuality"] = "0",
                ["sg.EffectsQuality"] = "0",
                ["sg.FoliageQuality"] = "0",
                ["sg.ShadingQuality"] = "0",
                ["sg.RayTracingQuality"] = "0",
            },
        },
    };

    /// <summary>
    /// GPU-тест: родное разрешение монитора, внутреннее разрешение 100%, все качества на максимум,
    /// трассировка лучей на максимум. Апскейлеры не включаем, чтобы рендер шёл в полном разрешении.
    /// </summary>
    public static BenchmarkProfile Gpu()
    {
        (int w, int h) = NativeMethods.GetScreenSize();
        return new()
        {
            Name = "GPU",
            Description = $"{w}x{h} (родное), render scale 100%, все качества на максимум, RT на максимум",
            IniSettings = new()
            {
                [Display] = Resolution(w, h, fullscreenMode: "0"),
                [Scalability] = new()
                {
                    ["sg.ResolutionQuality"] = "100",
                    ["sg.ViewDistanceQuality"] = "4",
                    ["sg.AntiAliasingQuality"] = "4",
                    ["sg.ShadowQuality"] = "4",
                    ["sg.GlobalIlluminationQuality"] = "4",
                    ["sg.ReflectionQuality"] = "4",
                    ["sg.PostProcessQuality"] = "4",
                    ["sg.TextureQuality"] = "4",
                    ["sg.EffectsQuality"] = "4",
                    ["sg.FoliageQuality"] = "4",
                    ["sg.ShadingQuality"] = "4",
                    ["sg.RayTracingQuality"] = "4",
                },
            },
        };
    }

    private static Dictionary<string, string> Resolution(int w, int h, string fullscreenMode) => new()
    {
        ["ResolutionSizeX"] = w.ToString(),
        ["ResolutionSizeY"] = h.ToString(),
        ["LastUserConfirmedResolutionSizeX"] = w.ToString(),
        ["LastUserConfirmedResolutionSizeY"] = h.ToString(),
        ["FullscreenMode"] = fullscreenMode,
        ["LastConfirmedFullscreenMode"] = fullscreenMode,
        ["bUseVSync"] = "False",
    };
}
