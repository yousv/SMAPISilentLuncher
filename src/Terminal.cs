using System;

namespace SmapiLauncher;

static partial class Launcher
{
    static void ResolveLaunchedConsole(int pid)
    {
        IntPtr wnd = FindConsoleWindow(pid);
        if (wnd == IntPtr.Zero) return;
        consoleWindowHandle = wnd;
    }

    static IntPtr FindConsoleWindow(int pid)
    {
        if (shellIsWt) return FindNewTerminalWindow();
        return Native.ResolveDirectWindow(pid, out IntPtr wnd) ? wnd : IntPtr.Zero;
    }

    static IntPtr FindNewTerminalWindow()
    {
        IntPtr titled = Native.FindTerminalWindowByTitle(terminalTitle);
        if (titled != IntPtr.Zero) return titled;

        int count = Native.SnapshotTerminalWindows(currentWindows);
        if (count < 0) return IntPtr.Zero;
        IntPtr found = IntPtr.Zero;
        bool multiple = false;
        for (int i = 0; i < count; i++)
        {
            bool known = false;
            for (int k = 0; k < knownWindowCount; k++)
            {
                if (currentWindows[i] == knownWindows[k])
                {
                    known = true;
                    break;
                }
            }
            if (known) continue;
            if (found != IntPtr.Zero) multiple = true;
            found = currentWindows[i];
        }
        if (multiple || found == IntPtr.Zero) return IntPtr.Zero;
        return Native.FindDescendantProcess(Native.WindowProcessId(found), SmapiExeName) != 0 ? found : IntPtr.Zero;
    }

    static void CloseLaunchedConsole()
    {
        if (!shellIsWt) return;
        for (int i = 0; i < 20; i++)
        {
            if (!Native.IsWindow(consoleWindowHandle))
                ResolveLaunchedConsole(0);
            if (Native.IsWindow(consoleWindowHandle) && Native.PostMessageW(consoleWindowHandle, Native.WmClose, IntPtr.Zero, IntPtr.Zero))
                return;
            System.Threading.Thread.Sleep(100);
        }
    }

    static void ToggleConsole()
    {
        if (launchAborted || overlayState != OverlayState.Launching) return;
        IntPtr wnd = consoleWindowHandle;
        if (wnd == IntPtr.Zero || !Native.IsWindow(wnd))
        {
            wnd = FindConsoleWindow(smapiProcessId != 0 ? smapiProcessId : cmdPid);
            if (wnd == IntPtr.Zero) return;
            consoleWindowHandle = wnd;
        }
        if (Native.IsEffectivelyHidden(wnd))
            Native.ApplyShown(wnd);
        else
            Native.ApplyHidden(wnd);
    }
}
