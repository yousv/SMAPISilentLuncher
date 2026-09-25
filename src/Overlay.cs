using System;
using System.Runtime.InteropServices;

namespace SmapiLauncher;

enum OverlayState { Launching, Warning, Crashed }

static class OverlayStyle
{
    internal const int MinWidth = 280;
    internal const int MaxWidth = 480;
    internal const int MinHeight = 56;
    internal const int CornerRadius = 8;
    internal const int TopOffsetPercent = 10;
    internal const int PaddingX = 18;
    internal const int PaddingY = 14;
    internal const int Gap = 7;
    internal const int StatusFontSize = 17;
    internal const int DetailFontSize = 14;
    internal const int FontWeight = 400;
    internal const string StatusFont = "Segoe UI";
    internal const string DetailFont = "Cascadia Mono";
    internal static uint BackgroundColor => Native.RGB(28, 28, 30);
    internal static uint HintColor => Native.RGB(150, 150, 155);

    internal static uint AccentColor(OverlayState state)
    {
        return state switch
        {
            OverlayState.Warning => Native.RGB(240, 200, 70),
            OverlayState.Crashed => Native.RGB(240, 110, 100),
            _ => Native.RGB(100, 160, 240)
        };
    }
}

static partial class Launcher
{
    const string ToggleHint = "[Ctrl+F12] toggle console";

    struct OverlayLayout
    {
        internal int StatusH;
        internal int HintH;
        internal int TimerH;
    }

    static OverlayLayout overlayLayout;

    static IntPtr OverlayWndProc(IntPtr wnd, uint msg, IntPtr wp, IntPtr lp)
    {
        switch (msg)
        {
            case Native.WmPaint:
            {
                Native.BeginPaint(wnd, out Native.PAINTSTRUCT ps);
                Native.RECT ca = default;
                Native.GetClientRect(wnd, ref ca);
                EnsureBackBuffer(ps.hdc, ca.right, ca.bottom);
                if (memoryDC != IntPtr.Zero)
                {
                    IntPtr br = Native.CreateSolidBrush(OverlayStyle.BackgroundColor);
                    Native.FillRect(memoryDC, ref ca, br);
                    Native.DeleteObject(br);
                    DrawContent(memoryDC, ca.right);
                    Native.BitBlt(ps.hdc, 0, 0, ca.right, ca.bottom, memoryDC, 0, 0, Native.Srccopy);
                }
                Native.EndPaint(wnd, ref ps);
                return IntPtr.Zero;
            }
            case Native.WmTimer:
            {
                ulong now = Native.GetTickCount64();
                if (now >= overlayDeadlineMs)
                {
                    Native.KillTimer(wnd, Native.TimerId);
                    Native.DestroyWindow(wnd);
                    return IntPtr.Zero;
                }
                if (!UpdateFade(wnd, now)) return IntPtr.Zero;
                if (!timerSlowed && alpha == FadeAlphaMax)
                {
                    timerSlowed = true;
                    if (Native.SetTimer(wnd, Native.TimerId, PollIntervalMs, IntPtr.Zero) == IntPtr.Zero)
                    {
                        overlayFailed = true;
                        Native.DestroyWindow(wnd);
                        return IntPtr.Zero;
                    }
                }
                elapsedMs = now - stateStartMs;
                if (overlayState == OverlayState.Launching && now >= nextPollMs)
                {
                    nextPollMs = now + (ulong)PollIntervalMs;
                    if (!PollLaunch(wnd)) return IntPtr.Zero;
                }
                else if (overlayState != OverlayState.Launching && !fadingOut && elapsedMs >= (ulong)currentDurationMs)
                {
                    fadingOut = true;
                    fadeStartMs = now;
                }
                Native.InvalidateRect(wnd, IntPtr.Zero, false);
                return IntPtr.Zero;
            }
            case Native.WmSetCursor:
                if ((lp & 0xFFFF) == (IntPtr)Native.HtClient)
                {
                    Native.SetCursor(Native.LoadCursorW(IntPtr.Zero, Native.CursorArrow));
                    return (IntPtr)1;
                }
                break;
            case Native.WmMouseActivate:
                return (IntPtr)1;
            case Native.WmLButtonDown:
            {
                if (overlayState != OverlayState.Launching)
                {
                    if (!fadingOut) { fadingOut = true; fadeStartMs = Native.GetTickCount64(); }
                    return IntPtr.Zero;
                }
                launchAborted = true;
                TerminateSMAPI();
                SetOverlayState(wnd, OverlayState.Crashed, "Launch aborted", PopupDisplayMs);
                return IntPtr.Zero;
            }
            case Native.WmClose:
                TerminateSMAPI();
                Native.DestroyWindow(wnd);
                return IntPtr.Zero;
            case Native.WmDestroy:
            {
                overlayWindow = IntPtr.Zero;
                ReleaseBackBuffer();
                ReleaseFonts();
                if (stayResident) return IntPtr.Zero;
                if (hotkeyWindow != IntPtr.Zero) { Native.DestroyWindow(hotkeyWindow); hotkeyWindow = IntPtr.Zero; }
                Native.PostQuitMessage(0);
                return IntPtr.Zero;
            }
        }
        return Native.DefWindowProcW(wnd, msg, wp, lp);
    }

    static IntPtr HotkeyWndProc(IntPtr wnd, uint msg, IntPtr wp, IntPtr lp)
    {
        switch (msg)
        {
            case Native.WmHotkey:
                ToggleConsole();
                return IntPtr.Zero;
            case Native.WmTimer:
            {
                bool dead = smapiProcess == IntPtr.Zero || Native.WaitForSingleObject(smapiProcess, 0) == 0;
                if (dead && overlayWindow == IntPtr.Zero)
                {
                    Cleanup();
                    Native.KillTimer(wnd, Native.TimerId);
                    Native.DestroyWindow(wnd);
                    return IntPtr.Zero;
                }
                return IntPtr.Zero;
            }
            case Native.WmDestroy:
                Native.UnregisterHotKey(wnd, HotkeyId);
                hotkeyRegistered = false;
                hotkeyWindow = IntPtr.Zero;
                Native.PostQuitMessage(0);
                return IntPtr.Zero;
        }
        return Native.DefWindowProcW(wnd, msg, wp, lp);
    }

    static void EnsureBackBuffer(IntPtr hdc, int w, int h)
    {
        if (memoryDC != IntPtr.Zero && memoryWidth == w && memoryHeight == h) return;
        ReleaseBackBuffer();
        memoryDC = Native.CreateCompatibleDC(hdc);
        if (memoryDC == IntPtr.Zero) return;
        memoryBitmap = Native.CreateCompatibleBitmap(hdc, w, h);
        if (memoryBitmap == IntPtr.Zero)
        {
            Native.DeleteDC(memoryDC);
            memoryDC = IntPtr.Zero;
            return;
        }
        memoryWidth = w;
        memoryHeight = h;
        memoryPreviousBitmap = Native.SelectObject(memoryDC, memoryBitmap);
    }

    static void ReleaseBackBuffer()
    {
        if (memoryDC == IntPtr.Zero) return;
        Native.SelectObject(memoryDC, Native.GetStockObject(Native.DefaultGuiFont));
        if (memoryPreviousBitmap != IntPtr.Zero)
            Native.SelectObject(memoryDC, memoryPreviousBitmap);
        if (memoryBitmap != IntPtr.Zero)
        {
            Native.DeleteObject(memoryBitmap);
            memoryBitmap = IntPtr.Zero;
        }
        Native.DeleteDC(memoryDC);
        memoryDC = IntPtr.Zero;
        memoryPreviousBitmap = IntPtr.Zero;
        memoryWidth = 0;
        memoryHeight = 0;
    }

    static void DrawContent(IntPtr hdc, int w)
    {
        Native.SetBkMode(hdc, 1);
        uint accent = OverlayStyle.AccentColor(overlayState);
        int cursorY = OverlayStyle.PaddingY;

        DrawTextLine(hdc, w, cursorY, overlayLayout.StatusH, statusFont, accent, overlayMessage);
        cursorY += overlayLayout.StatusH + OverlayStyle.Gap;

        if (overlayLayout.HintH > 0)
        {
            DrawTextLine(hdc, w, cursorY, overlayLayout.HintH, detailFont, OverlayStyle.HintColor, ToggleHint);
            cursorY += overlayLayout.HintH + OverlayStyle.Gap;
        }

        if (overlayLayout.TimerH > 0)
        {
            string timerText = $"{elapsedMs / 1000}s";
            DrawTextLine(hdc, w, cursorY, overlayLayout.TimerH, detailFont, accent, timerText);
        }
    }

    static void DrawTextLine(IntPtr hdc, int width, int top, int height, IntPtr font, uint color, string text)
    {
        Native.SelectObject(hdc, font);
        Native.SetTextColor(hdc, color);
        Native.RECT area = new Native.RECT
        {
            left = OverlayStyle.PaddingX,
            top = top,
            right = width - OverlayStyle.PaddingX,
            bottom = top + height
        };
        Native.DrawTextW(hdc, text, -1, ref area, Native.DtCenterSingle);
    }

    static bool UpdateFade(IntPtr wnd, ulong now)
    {
        if (fadingOut)
        {
            int fadeElapsed = (int)(now - fadeStartMs);
            int a = FadeAlphaMax - (FadeAlphaMax * fadeElapsed / FadeDurationMs);
            if (a <= 0) { Native.KillTimer(wnd, Native.TimerId); Native.DestroyWindow(wnd); return false; }
            Native.SetLayeredWindowAttributes(wnd, 0, (byte)a, Native.LwaAlpha);
            return true;
        }
        if (alpha < FadeAlphaMax)
        {
            int fadeElapsed = (int)(now - fadeStartMs);
            int a = FadeAlphaMax * fadeElapsed / FadeDurationMs;
            alpha = a > FadeAlphaMax ? FadeAlphaMax : a;
            Native.SetLayeredWindowAttributes(wnd, 0, (byte)alpha, Native.LwaAlpha);
        }
        return true;
    }

    static void SetOverlayState(IntPtr wnd, OverlayState next, string message, int durationMs)
    {
        overlayState = next;
        overlayMessage = message;
        elapsedMs = 0;
        currentDurationMs = durationMs;
        fadingOut = false;
        stateStartMs = Native.GetTickCount64();
        overlayDeadlineMs = stateStartMs + (ulong)(durationMs + FadeDurationMs + 2000);
        nextPollMs = stateStartMs + (ulong)PollIntervalMs;
        if (wnd == IntPtr.Zero) return;
        MeasureContentSize(out int w, out int h);
        if (w != overlayWidth || h != overlayHeight)
        {
            overlayWidth = w;
            overlayHeight = h;
            int x = (Native.ScreenW() - w) / 2;
            int y = Native.ScreenH() * OverlayStyle.TopOffsetPercent / 100;
            Native.SetWindowPos(wnd, IntPtr.Zero, x, y, w, h, Native.SwpNoZorder | Native.SwpNoActivate);
            IntPtr rgn = Native.CreateRoundRectRgn(0, 0, w + 1, h + 1, OverlayStyle.CornerRadius, OverlayStyle.CornerRadius);
            if (rgn != IntPtr.Zero && !Native.SetWindowRgn(wnd, rgn, false))
                Native.DeleteObject(rgn);
        }
        Native.InvalidateRect(wnd, IntPtr.Zero, true);
    }

    static void ReleaseFonts()
    {
        if (statusFont != IntPtr.Zero)
        {
            Native.DeleteObject(statusFont);
            statusFont = IntPtr.Zero;
        }
        if (detailFont != IntPtr.Zero)
        {
            Native.DeleteObject(detailFont);
            detailFont = IntPtr.Zero;
        }
    }

    static IntPtr CreateOverlayWindow()
    {
        Native.WNDCLASSW wc = default;
        wc.lpfnWndProc = overlayWndProcPtr;
        wc.hInstance = Native.GetModuleHandleW(null);
        wc.lpszClassName = "SmapiLauncherOverlay";
        ushort cls = Native.RegisterClassW(ref wc);
        if (cls == 0 && Marshal.GetLastWin32Error() != Native.ErrorClassAlreadyExists) return IntPtr.Zero;

        statusFont = Native.CreateFontW(OverlayStyle.StatusFontSize, 0, 0, 0, OverlayStyle.FontWeight, false, false, false, 1, 0, 0, 5, 0, OverlayStyle.StatusFont);
        detailFont = Native.CreateFontW(OverlayStyle.DetailFontSize, 0, 0, 0, OverlayStyle.FontWeight, false, false, false, 1, 0, 0, 5, 0, OverlayStyle.DetailFont);
        if (statusFont == IntPtr.Zero || detailFont == IntPtr.Zero)
        {
            ReleaseFonts();
            return IntPtr.Zero;
        }

        MeasureContentSize(out overlayWidth, out overlayHeight);

        int screenW = Native.ScreenW();
        int screenH = Native.ScreenH();
        int x = (screenW - overlayWidth) / 2;
        int y = screenH * OverlayStyle.TopOffsetPercent / 100;

        IntPtr wnd = Native.CreateWindowExW(Native.ExLayeredTopmostTool, "SmapiLauncherOverlay", "", Native.StylePopup,
            x, y, overlayWidth, overlayHeight, IntPtr.Zero, IntPtr.Zero, Native.GetModuleHandleW(null), IntPtr.Zero);
        if (wnd == IntPtr.Zero)
        {
            ReleaseFonts();
            return IntPtr.Zero;
        }
        overlayWindow = wnd;

        IntPtr rgn = Native.CreateRoundRectRgn(0, 0, overlayWidth + 1, overlayHeight + 1, OverlayStyle.CornerRadius, OverlayStyle.CornerRadius);
        if (rgn != IntPtr.Zero && !Native.SetWindowRgn(wnd, rgn, false))
            Native.DeleteObject(rgn);

        Native.SetLayeredWindowAttributes(wnd, 0, 0, Native.LwaAlpha);
        Native.ShowWindow(wnd, Native.SwShowNoActivate);
        if (Native.SetTimer(wnd, Native.TimerId, OverlayTickMs, IntPtr.Zero) == IntPtr.Zero)
        {
            Native.DestroyWindow(wnd);
            return IntPtr.Zero;
        }

        Native.MSG msg;
        int result;
        while ((result = Native.GetMessageW(out msg, IntPtr.Zero, 0, 0)) > 0)
        {
            Native.TranslateMessage(ref msg);
            Native.DispatchMessageW(ref msg);
        }
        if (result < 0)
        {
            overlayFailed = true;
            Native.DestroyWindow(wnd);
            return IntPtr.Zero;
        }
        return overlayFailed ? IntPtr.Zero : wnd;
    }

    static void SetMinimumContentSize(out int width, out int height)
    {
        width = OverlayStyle.MinWidth;
        height = OverlayStyle.MinHeight;
    }

    static void MeasureContentSize(out int width, out int height)
    {
        overlayLayout = default;
        IntPtr hdc = Native.GetDC(IntPtr.Zero);
        if (hdc == IntPtr.Zero)
        {
            SetMinimumContentSize(out width, out height);
            return;
        }

        IntPtr hMem = Native.CreateCompatibleDC(hdc);
        if (hMem == IntPtr.Zero)
        {
            Native.ReleaseDC(IntPtr.Zero, hdc);
            SetMinimumContentSize(out width, out height);
            return;
        }
        IntPtr hBmp = Native.CreateCompatibleBitmap(hdc, 1, 1);
        if (hBmp == IntPtr.Zero)
        {
            Native.DeleteDC(hMem);
            Native.ReleaseDC(IntPtr.Zero, hdc);
            SetMinimumContentSize(out width, out height);
            return;
        }
        IntPtr oldBmp = Native.SelectObject(hMem, hBmp);
        if (oldBmp == IntPtr.Zero)
        {
            Native.DeleteObject(hBmp);
            Native.DeleteDC(hMem);
            Native.ReleaseDC(IntPtr.Zero, hdc);
            SetMinimumContentSize(out width, out height);
            return;
        }

        Native.SelectObject(hMem, statusFont);
        Native.RECT rc = new Native.RECT { left = 0, top = 0, right = 9999, bottom = 9999 };
        Native.DrawTextW(hMem, overlayMessage, -1, ref rc, Native.DtCalcRect);
        int statusW = rc.right - rc.left;
        overlayLayout.StatusH = rc.bottom - rc.top;

        int contentW = statusW + OverlayStyle.PaddingX * 2;
        int contentH = OverlayStyle.PaddingY * 2 + overlayLayout.StatusH;

        if (overlayState == OverlayState.Launching)
        {
            Native.SelectObject(hMem, detailFont);

            Native.GetTextExtentPoint32W(hMem, ToggleHint, ToggleHint.Length, out Native.SIZE hintSize);
            overlayLayout.HintH = hintSize.cy;
            if (hintSize.cx + OverlayStyle.PaddingX * 2 > contentW) contentW = hintSize.cx + OverlayStyle.PaddingX * 2;
            contentH += OverlayStyle.Gap + overlayLayout.HintH;

            string timer = "0s";
            Native.DrawTextW(hMem, timer, -1, ref rc, Native.DtCalcRect);
            overlayLayout.TimerH = rc.bottom - rc.top;
            contentH += OverlayStyle.Gap + overlayLayout.TimerH;
        }

        width = contentW < OverlayStyle.MinWidth ? OverlayStyle.MinWidth : contentW > OverlayStyle.MaxWidth ? OverlayStyle.MaxWidth : contentW;
        height = contentH < OverlayStyle.MinHeight ? OverlayStyle.MinHeight : contentH;

        Native.SelectObject(hMem, oldBmp);
        Native.DeleteObject(hBmp);
        Native.DeleteDC(hMem);
        Native.ReleaseDC(IntPtr.Zero, hdc);
    }

    static IntPtr CreateHotkeyWindow()
    {
        hotkeyRegistered = false;
        Native.WNDCLASSW wc = default;
        wc.lpfnWndProc = hotkeyWndProcPtr;
        wc.hInstance = Native.GetModuleHandleW(null);
        wc.lpszClassName = "SmapiLauncherHotkey";
        if (Native.RegisterClassW(ref wc) == 0 && Marshal.GetLastWin32Error() != Native.ErrorClassAlreadyExists) return IntPtr.Zero;

        IntPtr wnd = Native.CreateWindowExW(0, "SmapiLauncherHotkey", "", 0,
            0, 0, 0, 0, Native.HwndMessage, IntPtr.Zero, Native.GetModuleHandleW(null), IntPtr.Zero);
        if (wnd == IntPtr.Zero) return IntPtr.Zero;

        if (!Native.RegisterHotKey(wnd, HotkeyId, Native.ModControl | Native.ModNoRepeat, Native.VkF12)) return wnd;
        if (Native.SetTimer(wnd, Native.TimerId, ResidentPollMs, IntPtr.Zero) == IntPtr.Zero)
        {
            Native.UnregisterHotKey(wnd, HotkeyId);
            return wnd;
        }
        hotkeyRegistered = true;
        return wnd;
    }

    static void RunOverlay(OverlayState state, string message, int durationMs)
    {
        overlayState = state;
        if (state != OverlayState.Launching)
            overlayMessage = message;
        currentDurationMs = state == OverlayState.Launching ? 0 : durationMs;
        elapsedMs = 0;
        fadingOut = false;
        stateStartMs = Native.GetTickCount64();
        overlayDeadlineMs = state == OverlayState.Launching
            ? ulong.MaxValue
            : stateStartMs + (ulong)(durationMs + FadeDurationMs + 2000);
        alpha = 0;
        fadeStartMs = Native.GetTickCount64();
        timerSlowed = false;
        nextPollMs = stateStartMs + (ulong)PollIntervalMs;
        stayResident = false;
        launchAborted = false;
        overlayFailed = false;

        IntPtr result = CreateOverlayWindow();
        if (state == OverlayState.Launching && (result == IntPtr.Zero || overlayFailed))
            TerminateSMAPI();
        overlayWindow = IntPtr.Zero;

        if (hotkeyWindow != IntPtr.Zero)
        {
            Native.DestroyWindow(hotkeyWindow);
            hotkeyWindow = IntPtr.Zero;
            hotkeyRegistered = false;
        }
    }
}
