using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace CursorShake.Core;

public class MouseHook
{
    private const int WhMouseLl = 14;
    private const int WmMouseMove = 0x0200;

    private IntPtr _hookId;
    private readonly LowLevelMouseProc _proc;

    public event Action<int, int>? OnMouseMove;

    public MouseHook()
    {
        _proc = HookCallback;
    }

    public void Start()
    {
        if (_hookId != IntPtr.Zero) return;

        _hookId = InstallHook(_proc);
        if (_hookId == IntPtr.Zero)
        {
            var err = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                "Could not install low-level mouse hook. Try running the built .exe (not only `dotnet run`), or run as a normal user. Error: " + err);
        }
    }

    public void Stop()
    {
        if (_hookId == IntPtr.Zero) return;
        UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
    }

    private static IntPtr InstallHook(LowLevelMouseProc proc)
    {
        // A single hMod=MainModule was unreliable under `dotnet run` (host is dotnet.exe, hook failed silently).
        var id = SetWindowsHookEx(WhMouseLl, proc, IntPtr.Zero, 0);
        if (id != IntPtr.Zero) return id;
        id = SetWindowsHookEx(WhMouseLl, proc, GetModuleHandle(null), 0);
        if (id != IntPtr.Zero) return id;
        if (Process.GetCurrentProcess().MainModule?.FileName is { } file)
        {
            id = SetWindowsHookEx(WhMouseLl, proc, GetModuleHandle(Path.GetFileName(file)), 0);
        }
        return id;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
            return CallNextHookEx(_hookId, nCode, wParam, lParam);

        if (wParam != (IntPtr)WmMouseMove)
            return CallNextHookEx(_hookId, nCode, wParam, lParam);

        var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
        OnMouseMove?.Invoke(hookStruct.pt.x, hookStruct.pt.y);

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x, y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public int mouseData;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
