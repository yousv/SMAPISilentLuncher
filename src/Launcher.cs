using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace SmapiLauncher;

[SupportedOSPlatform("windows")]
static partial class Launcher
{
    const string MutexName = @"Local\SMAPI Silent Launcher";
    const string SmapiExeName = "StardewModdingAPI.exe";
    const string GameExeName = "Stardew Valley.exe";
    const int FadeAlphaMax = 230;
    const int FadeDurationMs = 300;
    const int OverlayTickMs = 16;
    const int PollIntervalMs = 250;
    const int PopupDisplayMs = 5000;
    const int ResidentPollMs = 1000;
    const int ResolveBudgetMs = 15000;
    const int WindowSnapCap = 64;
    const int HotkeyId = 1;

    static string launcherDir = "";
    static string modsPath = DefaultModsDir;
    static string smapiExePath = "";
    static IntPtr smapiProcess;
    static int smapiProcessId;
    static IntPtr cmdProcess;
    static int cmdPid;
    static bool shellIsWt;
    static bool launchAborted;
    static string terminalTitle;
    static IntPtr launchMutex;
    static OverlayState overlayState = OverlayState.Launching;
    static string overlayMessage = "Launching SMAPI...";
    static int currentDurationMs;
    static ulong elapsedMs;
    static int alpha;
    static bool fadingOut;
    static ulong stateStartMs;
    static ulong fadeStartMs;
    static ulong overlayDeadlineMs;
    static ulong nextPollMs;
    static IntPtr overlayWindow;
    static IntPtr hotkeyWindow;
    static bool hotkeyRegistered;
    static bool timerSlowed;
    static bool overlayFailed;
    static bool stayResident;
    static IntPtr consoleWindowHandle;
    static IntPtr[] knownWindows = new IntPtr[WindowSnapCap];
    static IntPtr[] currentWindows = new IntPtr[WindowSnapCap];
    static int knownWindowCount;
    static ulong resolveDeadlineMs;
    static IntPtr statusFont;
    static IntPtr detailFont;
    static IntPtr memoryDC;
    static IntPtr memoryBitmap;
    static IntPtr memoryPreviousBitmap;
    static int memoryWidth;
    static int memoryHeight;
    static int overlayWidth;
    static int overlayHeight;

    static IntPtr overlayWndProcPtr;
    static IntPtr hotkeyWndProcPtr;
    static WNDPROC overlayWndProcDelegate;
    static WNDPROC hotkeyWndProcDelegate;

    static void Main()
    {
        try
        {
            Run();
        }
        catch (Exception ex)
        {
            try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "launcher-crash.txt"), ex.ToString()); }
            catch { }
        }
    }

    static void Run()
    {
        overlayWndProcDelegate = OverlayWndProc;
        hotkeyWndProcDelegate = HotkeyWndProc;
        overlayWndProcPtr = Marshal.GetFunctionPointerForDelegate(overlayWndProcDelegate);
        hotkeyWndProcPtr = Marshal.GetFunctionPointerForDelegate(hotkeyWndProcDelegate);

        launcherDir = AppContext.BaseDirectory.TrimEnd('\\', '/');

        string cleanedArgs = ForwardedArgs();
        ParseModsPath(cleanedArgs);

        string gameExe = Path.Combine(launcherDir, GameExeName);
        if (!File.Exists(gameExe))
        {
            RunOverlay(OverlayState.Warning, "Wrong folder: Stardew Valley.exe not found here", PopupDisplayMs);
            Cleanup();
            return;
        }

        smapiExePath = Path.Combine(launcherDir, SmapiExeName);
        if (!File.Exists(smapiExePath))
        {
            RunOverlay(OverlayState.Warning, "SMAPI is not installed", PopupDisplayMs);
            Cleanup();
            return;
        }

        if (IsProcessRunning(SmapiExeName) || IsProcessRunning(GameExeName))
        {
            RunOverlay(OverlayState.Warning, "SMAPI or the game is already running", PopupDisplayMs);
            Cleanup();
            return;
        }

        launchMutex = Native.CreateMutexW(IntPtr.Zero, false, MutexName);
        if (launchMutex == IntPtr.Zero || Marshal.GetLastWin32Error() == Native.ErrorAlreadyExists)
        {
            if (launchMutex != IntPtr.Zero)
            {
                Native.CloseHandle(launchMutex);
                launchMutex = IntPtr.Zero;
            }
            RunOverlay(OverlayState.Warning, "SMAPI Silent Launcher is already running", PopupDisplayMs);
            Cleanup();
            return;
        }

        string smapiArgs = $"\"{smapiExePath}\" {cleanedArgs}";
        if (smapiArgs.Length >= MaxLaunchArgs)
        {
            RunOverlay(OverlayState.Warning, "Launch arguments are too long", PopupDisplayMs);
            Cleanup();
            return;
        }

        hotkeyWindow = CreateHotkeyWindow();
        if (!hotkeyRegistered)
        {
            RunOverlay(OverlayState.Warning, "Could not register Ctrl+F12", PopupDisplayMs);
            Cleanup();
            return;
        }

        overlayState = OverlayState.Launching;
        CountMods();

        if (!StartSMAPI(smapiArgs))
        {
            RunOverlay(OverlayState.Warning, "Failed to launch SMAPI", PopupDisplayMs);
            Cleanup();
            return;
        }

        RunOverlay(OverlayState.Launching, "Launching SMAPI...", 0);
        Cleanup();
    }

    static bool StartSMAPI(string smapiArgs)
    {
        string shellCmd;
        terminalTitle = $"SMAPI Silent Launcher {Guid.NewGuid():N}";
        shellIsWt = WtCommand(smapiArgs, out shellCmd);
        knownWindowCount = 0;
        if (shellIsWt)
        {
            knownWindowCount = Native.SnapshotTerminalWindows(knownWindows);
            if (knownWindowCount < 0) knownWindowCount = 0;
        }
        else
            shellCmd = smapiArgs;
        if (shellCmd.Length >= MaxLaunchArgs)
        {
            shellIsWt = false;
            return false;
        }

        if (!Native.CreateProcess(null, shellCmd, Native.CreateNewConsole, launcherDir, Native.SwShowNoActivate, out Native.PROCESS_INFORMATION pi))
        {
            if (!shellIsWt) return false;
            shellIsWt = false;
            shellCmd = smapiArgs;
            if (shellCmd.Length >= MaxLaunchArgs || !Native.CreateProcess(null, shellCmd, Native.CreateNewConsole, launcherDir, Native.SwShowNoActivate, out pi))
                return false;
        }

        Native.CloseHandle(pi.hThread);
        cmdProcess = pi.hProcess;
        cmdPid = pi.dwProcessId;
        resolveDeadlineMs = Native.GetTickCount64() + (ulong)ResolveBudgetMs;
        return true;
    }

    static bool WtCommand(string smapiArgs, out string shellCmd)
    {
        shellCmd = null;
        if (smapiArgs.Contains(';')) return false;
        string wt = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\WindowsApps\wt.exe");
        if (!File.Exists(wt)) return false;
        shellCmd = $"\"{wt}\" -w -1 nt --title \"{terminalTitle}\" --suppressApplicationTitle -d \"{launcherDir}\" {smapiArgs}";
        return shellCmd.Length < MaxLaunchArgs;
    }

    static bool PollResolve(IntPtr wnd)
    {
        if (launchAborted) return false;
        if (Native.GetTickCount64() >= resolveDeadlineMs)
        {
            TerminateSMAPI();
            SetOverlayState(wnd, OverlayState.Crashed, "SMAPI did not start", PopupDisplayMs);
            return false;
        }
        int pid = 0;
        if (shellIsWt)
        {
            if (!Native.IsWindow(consoleWindowHandle))
                ResolveLaunchedConsole(0);
            if (Native.IsWindow(consoleWindowHandle))
                pid = Native.FindDescendantProcess(Native.WindowProcessId(consoleWindowHandle), SmapiExeName);
        }
        else
        {
            pid = cmdPid;
        }
        if (pid != 0)
        {
            IntPtr h = Native.OpenProcess(Native.SmapiAccess, false, (uint)pid);
            if (h != IntPtr.Zero)
            {
                smapiProcess = h;
                smapiProcessId = pid;
                if (!Native.IsWindow(consoleWindowHandle)) ResolveLaunchedConsole(smapiProcessId);
                return true;
            }
        }
        return true;
    }

    static void TerminateSMAPI()
    {
        if (smapiProcess != IntPtr.Zero)
        {
            Native.TerminateProcess(smapiProcess, 0);
            Native.CloseHandle(smapiProcess);
            smapiProcess = IntPtr.Zero;
        }
        if (cmdProcess != IntPtr.Zero)
        {
            if (!shellIsWt && Native.WaitForSingleObject(cmdProcess, 0) != 0)
                Native.TerminateProcess(cmdProcess, 0);
            if (shellIsWt)
                CloseLaunchedConsole();
            Native.CloseHandle(cmdProcess);
            cmdProcess = IntPtr.Zero;
        }
    }

    static bool PollLaunch(IntPtr wnd)
    {
        if (smapiProcess == IntPtr.Zero && !PollResolve(wnd)) return false;
        if (!Native.IsWindow(consoleWindowHandle)) ResolveLaunchedConsole(smapiProcessId);

        bool gameWindowFound = Native.FindGameWindow(smapiProcessId, consoleWindowHandle);
        if (gameWindowFound)
        {
            if (hotkeyWindow != IntPtr.Zero)
                stayResident = true;
            Native.KillTimer(wnd, Native.TimerId);
            Native.DestroyWindow(wnd);
            return false;
        }

        if (smapiProcess != IntPtr.Zero && Native.WaitForSingleObject(smapiProcess, 0) == 0)
        {
            string msg = Native.GetExitCodeProcess(smapiProcess, out uint code)
                ? $"SMAPI closed (code {code}) before the game started"
                : "SMAPI closed before the game started";
            TerminateSMAPI();
            SetOverlayState(wnd, OverlayState.Crashed, msg, PopupDisplayMs);
            return false;
        }
        return true;
    }

    static void Cleanup()
    {
        if (smapiProcess != IntPtr.Zero) { Native.CloseHandle(smapiProcess); smapiProcess = IntPtr.Zero; }
        if (cmdProcess != IntPtr.Zero) { Native.CloseHandle(cmdProcess); cmdProcess = IntPtr.Zero; }
        if (launchMutex != IntPtr.Zero) { Native.CloseHandle(launchMutex); launchMutex = IntPtr.Zero; }
    }

}
