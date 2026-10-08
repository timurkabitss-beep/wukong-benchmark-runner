using System.Runtime.InteropServices;

namespace WukongBenchmark;

public static class NativeMethods
{
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const int SW_RESTORE = 9;
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_SCANCODE = 0x0008;

    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int cmd);
    [DllImport("user32.dll")] private static extern uint MapVirtualKey(uint code, uint mapType);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, INPUT[] inputs, int size);
    [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx, dy;
        public uint mouseData, dwFlags, time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk, wScan;
        public uint dwFlags, time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    private static readonly Dictionary<string, ushort> VirtualKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Enter"] = 0x0D, ["Esc"] = 0x1B, ["Space"] = 0x20, ["Tab"] = 0x09,
        ["Left"] = 0x25, ["Up"] = 0x26, ["Right"] = 0x27, ["Down"] = 0x28,
    };

    public static (int Width, int Height) GetScreenSize()
    {
        SetProcessDPIAware(); // иначе Windows вернёт «виртуальное» разрешение при масштабировании
        return (GetSystemMetrics(SM_CXSCREEN), GetSystemMetrics(SM_CYSCREEN));
    }

    public static void BringToFront(IntPtr hWnd)
    {
        ShowWindow(hWnd, SW_RESTORE);
        SetForegroundWindow(hWnd);
    }

    /// <summary>Нажимает клавишу через SendInput со скан-кодом (игры часто игнорируют «виртуальные» коды).</summary>
    public static void PressKey(string key)
    {
        if (!VirtualKeys.TryGetValue(key, out ushort vk))
            throw new ArgumentException($"Неизвестная клавиша в config.json: {key}");

        ushort scan = (ushort)MapVirtualKey(vk, 0);
        var down = MakeKey(vk, scan, KEYEVENTF_SCANCODE);
        var up = MakeKey(vk, scan, KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP);

        SendInput(1, new[] { down }, Marshal.SizeOf<INPUT>());
        Thread.Sleep(80);
        SendInput(1, new[] { up }, Marshal.SizeOf<INPUT>());
    }

    private static INPUT MakeKey(ushort vk, ushort scan, uint flags) => new()
    {
        type = INPUT_KEYBOARD,
        u = new InputUnion { ki = new KEYBDINPUT { wVk = vk, wScan = scan, dwFlags = flags } },
    };
}
