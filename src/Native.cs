using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SmapiLauncher;

delegate bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam);

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
delegate IntPtr WNDPROC(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

static class Native
{
    internal const uint WmPaint = 0x000F;
    internal const uint WmTimer = 0x0113;
    internal const uint WmSetCursor = 0x0020;
    internal const uint WmMouseActivate = 0x0021;
    internal const uint WmLButtonDown = 0x0201;
    internal const uint WmClose = 0x0010;
    internal const uint WmDestroy = 0x0002;
    internal const uint WmHotkey = 0x0312;
    internal const int SwHide = 0;
    internal const int SwShowNoActivate = 4;
    internal const int SwRestore = 9;
    internal const int GwExStyle = -20;
    internal const int WsExToolWindow = 0x00000080;
    internal const int WsExAppWindow = 0x00040000;
    internal const uint SwpFrameChanged = 0x0020;
    internal const uint CreateNewConsole = 0x00000010;
    internal const int StartfUseShowWindow = 0x00000001;
    internal const int ErrorAlreadyExists = 183;
    internal const int ErrorClassAlreadyExists = 1410;
    internal const uint ModControl = 0x0002;
    internal const uint ModNoRepeat = 0x4000;
    internal const uint VkF12 = 0x7B;
    internal const uint SwpNoMoveSize = 0x0003;
    internal const uint SwpNoZorder = 0x0004;
    internal const uint SwpNoActivate = 0x0010;
    internal const int HtClient = 1;
    internal const uint LwaAlpha = 2;
    internal const uint Srccopy = 0x00CC0020;
    internal const uint DtCenterSingle = 0x25;
    internal const uint DtCalcRect = 0x0400 | 0x0008;
    internal const uint SnapProcess = 0x00000002;
    internal const uint SmapiAccess = 0x00101001;
    internal const uint ExLayeredTopmostTool = 0x00080088;
    internal const uint StylePopup = 0x80000000;
    internal const int DefaultGuiFont = 17;
    internal static readonly IntPtr HwndMessage = (IntPtr)(-3);
    internal static readonly IntPtr CursorArrow = (IntPtr)32512;
    internal static readonly IntPtr TimerId = (IntPtr)1;

    [DllImport("user32.dll")] internal static extern int GetSystemMetrics(int idx);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr CreateWindowExW(uint ex, string cls, string wnd, uint style, int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr hInst, IntPtr lpParam);
    [DllImport("user32.dll")] internal static extern bool ShowWindow(IntPtr wnd, int cmd);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr wnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] internal static extern bool DestroyWindow(IntPtr wnd);
    [DllImport("user32.dll")] internal static extern bool PostMessageW(IntPtr wnd, uint msg, IntPtr wp, IntPtr lp);
    [DllImport("user32.dll")] internal static extern IntPtr DefWindowProcW(IntPtr wnd, uint msg, IntPtr wp, IntPtr lp);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] internal static extern ushort RegisterClassW(ref WNDCLASSW cls);
    [DllImport("user32.dll", SetLastError = true)] internal static extern int GetMessageW(out MSG msg, IntPtr wnd, uint min, uint max);
    [DllImport("user32.dll")] internal static extern bool TranslateMessage(ref MSG msg);
    [DllImport("user32.dll")] internal static extern IntPtr DispatchMessageW(ref MSG msg);
    [DllImport("user32.dll")] internal static extern IntPtr SetTimer(IntPtr wnd, IntPtr id, uint ms, IntPtr timerProc);
    [DllImport("user32.dll")] internal static extern bool KillTimer(IntPtr wnd, IntPtr id);
    [DllImport("user32.dll")] internal static extern bool InvalidateRect(IntPtr wnd, IntPtr rect, bool erase);
    [DllImport("user32.dll")] internal static extern bool SetLayeredWindowAttributes(IntPtr wnd, uint crKey, byte alpha, uint flags);
    [DllImport("user32.dll")] internal static extern IntPtr BeginPaint(IntPtr wnd, out PAINTSTRUCT ps);
    [DllImport("user32.dll")] internal static extern bool EndPaint(IntPtr wnd, ref PAINTSTRUCT ps);
    [DllImport("user32.dll")] internal static extern bool GetClientRect(IntPtr wnd, ref RECT rect);
    [DllImport("user32.dll")] internal static extern bool SetWindowRgn(IntPtr wnd, IntPtr rgn, bool redraw);
    [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr wnd);
    [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr wnd, int id, uint mod, uint vk);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr wnd, int id);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr wnd);
    [DllImport("user32.dll")] internal static extern bool IsWindow(IntPtr wnd);
    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumWindowsCallback cb, IntPtr lp);
    [DllImport("user32.dll")] internal static extern int GetWindowLongW(IntPtr wnd, int index);
    [DllImport("user32.dll")] internal static extern int SetWindowLongW(IntPtr wnd, int index, int value);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr wnd, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetWindowTextW(IntPtr wnd, StringBuilder txt, int max);
    [DllImport("user32.dll")] internal static extern int GetClassNameA(IntPtr wnd, StringBuilder cls, int max);
    [DllImport("user32.dll")] internal static extern IntPtr LoadCursorW(IntPtr h, IntPtr id);
    [DllImport("user32.dll")] internal static extern IntPtr SetCursor(IntPtr cur);
    [DllImport("user32.dll")] internal static extern void PostQuitMessage(int code);
    [DllImport("kernel32.dll")] internal static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")] internal static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] internal static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
    [DllImport("user32.dll")] internal static extern bool FillRect(IntPtr hdc, ref RECT rect, IntPtr br);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int DrawTextW(IntPtr hdc, string txt, int len, ref RECT rect, uint fmt);

    [DllImport("kernel32.dll")] internal static extern bool AttachConsole(uint pid);
    [DllImport("kernel32.dll")] internal static extern bool FreeConsole();
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern bool CreateProcessW(string exe, string cmd, IntPtr procAttr, IntPtr threadAttr, bool inherit, uint flags, IntPtr env, string dir, ref STARTUPINFOW si, ref PROCESS_INFORMATION pi);
    [DllImport("kernel32.dll")] internal static extern bool TerminateProcess(IntPtr proc, uint code);
    [DllImport("kernel32.dll")] internal static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("kernel32.dll")] internal static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll")] internal static extern bool GetExitCodeProcess(IntPtr proc, out uint code);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] internal static extern IntPtr CreateMutexW(IntPtr attr, bool initial, string name);
    [DllImport("kernel32.dll")] internal static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint pid);
    [DllImport("kernel32.dll")] internal static extern bool Process32FirstW(IntPtr snap, ref PROCESSENTRY32W pe);
    [DllImport("kernel32.dll")] internal static extern bool Process32NextW(IntPtr snap, ref PROCESSENTRY32W pe);
    [DllImport("kernel32.dll")] internal static extern uint WaitForSingleObject(IntPtr handle, uint ms);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr GetModuleHandleW(string mod);
    [DllImport("kernel32.dll")] internal static extern ulong GetTickCount64();

    [DllImport("gdi32.dll")] internal static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] internal static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] internal static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);
    [DllImport("gdi32.dll")] internal static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
    [DllImport("gdi32.dll")] internal static extern IntPtr GetStockObject(int id);
    [DllImport("gdi32.dll")] internal static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] internal static extern IntPtr CreateSolidBrush(uint color);
    [DllImport("gdi32.dll")] internal static extern bool BitBlt(IntPtr dst, int dx, int dy, int w, int h, IntPtr src, int sx, int sy, uint rop);
    [DllImport("gdi32.dll")] internal static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int w, int h);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] internal static extern bool GetTextExtentPoint32W(IntPtr hdc, string txt, int len, out SIZE size);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr CreateFontW(int h, int w, int esc, int orient, int weight, bool italic, bool underline, bool strike, uint charset, uint outPrec, uint clipPrec, uint quality, uint pitch, string face);
    [DllImport("gdi32.dll")] internal static extern int SetBkMode(IntPtr hdc, int mode);
    [DllImport("gdi32.dll")] internal static extern uint SetTextColor(IntPtr hdc, uint color);

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG { public IntPtr hwnd; public uint message; public IntPtr wParam; public IntPtr lParam; public uint time; public POINT pt; }
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int x, y; }
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int left, top, right, bottom; }
    [StructLayout(LayoutKind.Sequential)]
    public struct PAINTSTRUCT
    {
        public IntPtr hdc;
        public bool fErase;
        public RECT rcPaint;
        public bool fRestore;
        public bool fIncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] rgbReserved;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WNDCLASSW { public uint style; public IntPtr lpfnWndProc; public int cbClsExtra; public int cbWndExtra; public IntPtr hInstance; public IntPtr hIcon; public IntPtr hCursor; public IntPtr hbrBackground; public string lpszMenuName; public string lpszClassName; }
    [StructLayout(LayoutKind.Sequential)]
    public struct SIZE { public int cx, cy; }
    [StructLayout(LayoutKind.Sequential)]
    public struct STARTUPINFOW
    {
        public int cb; public IntPtr lpReserved, lpDesktop, lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2; public IntPtr lpReserved2;
        public IntPtr hStdInput, hStdOutput, hStdError;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION { public IntPtr hProcess, hThread; public int dwProcessId, dwThreadId; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct PROCESSENTRY32W
    {
        public int dwSize, cntUsage, th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public int th32ModuleID, cntThreads, th32ParentProcessID, pcPriClassBase, dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
        public string ExeName => szExeFile ?? "";
    }

    public static int ScreenW() => GetSystemMetrics(0);
    public static int ScreenH() => GetSystemMetrics(1);
    public static uint RGB(byte r, byte g, byte b) => (uint)(r | (g << 8) | (b << 16));

    public static bool CreateProcess(string exe, string cmd, uint flags, string dir, short show, out PROCESS_INFORMATION pi)
    {
        var si = new STARTUPINFOW { cb = Marshal.SizeOf<STARTUPINFOW>() };
        si.dwFlags |= StartfUseShowWindow;
        si.wShowWindow = show;
        pi = default;
        return CreateProcessW(exe, cmd, IntPtr.Zero, IntPtr.Zero, false, flags, IntPtr.Zero, dir, ref si, ref pi);
    }

    public static string ClassNameOf(IntPtr wnd)
    {
        if (wnd == IntPtr.Zero || !IsWindow(wnd)) return "";
        var sb = new StringBuilder(128);
        return GetClassNameA(wnd, sb, 128) > 0 ? sb.ToString() : "";
    }

    public static string WindowTitleOf(IntPtr wnd)
    {
        if (wnd == IntPtr.Zero || !IsWindow(wnd)) return "";
        var sb = new StringBuilder(256);
        return GetWindowTextW(wnd, sb, 256) > 0 ? sb.ToString() : "";
    }

    public static int WindowProcessId(IntPtr wnd)
    {
        return GetWindowThreadProcessId(wnd, out uint pid) == 0 ? 0 : (int)pid;
    }

    public static int SnapshotTerminalWindows(IntPtr[] buffer)
    {
        int count = 0;
        bool overflow = false;
        EnumWindows((wnd, _) =>
        {
            if (ClassNameOf(wnd) != "CASCADIA_HOSTING_WINDOW_CLASS") return true;
            if (count >= buffer.Length)
            {
                overflow = true;
                return true;
            }
            buffer[count++] = wnd;
            return true;
        }, IntPtr.Zero);
        return overflow ? -1 : count;
    }

    public static IntPtr FindTerminalWindowByTitle(string title)
    {
        IntPtr found = IntPtr.Zero;
        EnumWindows((wnd, _) =>
        {
            if (ClassNameOf(wnd) != "CASCADIA_HOSTING_WINDOW_CLASS" || WindowTitleOf(wnd) != title)
                return true;
            found = wnd;
            return false;
        }, IntPtr.Zero);
        return found;
    }

    public static int FindDescendantProcess(int rootPid, string exeName)
    {
        if (rootPid == 0) return 0;
        IntPtr snap = CreateToolhelp32Snapshot(SnapProcess, 0);
        if (snap == IntPtr.Zero || snap == (IntPtr)(-1)) return 0;

        var parents = new Dictionary<int, int>();
        var candidates = new List<int>();
        var pe = new PROCESSENTRY32W();
        pe.dwSize = Marshal.SizeOf<PROCESSENTRY32W>();
        if (Process32FirstW(snap, ref pe))
        {
            do
            {
                parents[pe.th32ProcessID] = pe.th32ParentProcessID;
                if (string.Equals(pe.ExeName, exeName, StringComparison.OrdinalIgnoreCase))
                    candidates.Add(pe.th32ProcessID);
            } while (Process32NextW(snap, ref pe));
        }
        CloseHandle(snap);

        int found = 0;
        foreach (int pid in candidates)
        {
            int parent = pid;
            for (int depth = 0; depth < 32 && parent != 0; depth++)
            {
                if (parent == rootPid && pid != rootPid)
                {
                    if (found != 0) return 0;
                    found = pid;
                    break;
                }
                if (!parents.TryGetValue(parent, out parent)) break;
            }
        }
        return found;
    }

    public static bool HasProcess(string exeName)
    {
        IntPtr snap = CreateToolhelp32Snapshot(SnapProcess, 0);
        if (snap == IntPtr.Zero || snap == (IntPtr)(-1)) return true;
        var pe = new PROCESSENTRY32W();
        pe.dwSize = Marshal.SizeOf<PROCESSENTRY32W>();
        bool found = false;
        if (Process32FirstW(snap, ref pe))
        {
            do
            {
                if (string.Equals(pe.ExeName, exeName, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            } while (Process32NextW(snap, ref pe));
        }
        CloseHandle(snap);
        return found;
    }

    public static bool FindGameWindow(int targetPid, IntPtr excludeWnd)
    {
        bool found = false;
        EnumWindows((wnd, _) =>
        {
            if (wnd == excludeWnd) return true;
            if (!IsWindowVisible(wnd)) return true;
            GetWindowThreadProcessId(wnd, out uint pid);
            if ((int)pid != targetPid) return true;
            var clsSb = new StringBuilder(128);
            GetClassNameA(wnd, clsSb, 128);
            string clsName = clsSb.ToString();
            if (clsName == "ConsoleWindowClass" || clsName == "CASCADIA_HOSTING_WINDOW_CLASS" || clsName == "PseudoConsoleWindow") return true;
            var titleSb = new StringBuilder(64);
            if (GetWindowTextW(wnd, titleSb, 64) <= 0) return true;
            if (titleSb.ToString().StartsWith("Stardew Valley"))
            {
                found = true;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    public static bool ResolveDirectWindow(int pid, out IntPtr wnd)
    {
        wnd = IntPtr.Zero;
        if (pid == 0) return false;
        if (!AttachConsole((uint)pid)) return false;
        wnd = GetConsoleWindow();
        FreeConsole();
        return wnd != IntPtr.Zero && IsWindow(wnd);
    }

    public static bool IsEffectivelyHidden(IntPtr wnd) => !IsWindowVisible(wnd);

    public static void ApplyHidden(IntPtr wnd)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            ShowWindow(wnd, SwHide);
            System.Threading.Thread.Sleep(100);
            if (!IsEffectivelyHidden(wnd)) continue;
            int ex = GetWindowLongW(wnd, GwExStyle);
            SetWindowLongW(wnd, GwExStyle, (ex | WsExToolWindow) & ~WsExAppWindow);
            SetWindowPos(wnd, IntPtr.Zero, 0, 0, 0, 0, SwpNoMoveSize | SwpNoZorder | SwpFrameChanged);
            return;
        }
    }

    public static void ApplyShown(IntPtr wnd)
    {
        int ex = GetWindowLongW(wnd, GwExStyle);
        SetWindowLongW(wnd, GwExStyle, (ex & ~WsExToolWindow) | WsExAppWindow);
        SetWindowPos(wnd, IntPtr.Zero, 0, 0, 0, 0, SwpNoMoveSize | SwpNoZorder | SwpFrameChanged);
        ShowWindow(wnd, SwRestore);
        SetForegroundWindow(wnd);
    }
}
