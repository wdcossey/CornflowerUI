// Win32 helpers for driving the emulator (XDE) where UI Automation can't: its toolbar draws its buttons
// itself, and walking a modal file dialog's tree through UI Automation is slow and can hang.
// Compiled by grab-loop.ps1 with Add-Type.
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

public static class SaveDialog
{
    delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc f, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr p, EnumProc f, IntPtr l);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern int GetDlgCtrlID(IntPtr h);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, string l);
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);

    const uint WM_SETTEXT = 0x000C, BM_CLICK = 0x00F5;

    static string Cls(IntPtr h) { var s = new StringBuilder(256); GetClassName(h, s, 256); return s.ToString(); }

    /// <summary>The first visible #32770 window of the process, or zero.</summary>
    public static IntPtr Find(int processId)
    {
        var found = IntPtr.Zero;
        EnumWindows((h, l) =>
        {
            uint pid;
            GetWindowThreadProcessId(h, out pid);
            if (found == IntPtr.Zero && pid == processId && IsWindowVisible(h) && Cls(h) == "#32770") found = h;
            return true;
        }, IntPtr.Zero);
        return found;
    }

    /// <summary>Waits up to timeoutMs for the process's dialog.</summary>
    public static IntPtr Wait(int processId, int timeoutMs)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < timeoutMs)
        {
            var h = Find(processId);
            if (h != IntPtr.Zero) return h;
            Thread.Sleep(100);
        }
        return IntPtr.Zero;
    }

    static IntPtr Child(IntPtr dialog, string cls, int id)
    {
        var found = IntPtr.Zero;
        EnumChildWindows(dialog, (h, l) =>
        {
            if (found == IntPtr.Zero && Cls(h) == cls && GetDlgCtrlID(h) == id) found = h;
            return true;
        }, IntPtr.Zero);
        return found;
    }

    /// <summary>Types path into the file name box (Edit 1001) and presses Save (Button 1). Returns an error, or null.</summary>
    public static string Save(IntPtr dialog, string path)
    {
        var name = Child(dialog, "Edit", 1001);
        if (name == IntPtr.Zero) return "no file name box (Edit 1001)";
        SendMessage(name, WM_SETTEXT, IntPtr.Zero, path);
        var save = Child(dialog, "Button", 1);
        if (save == IntPtr.Zero) return "no Save button (Button 1)";
        PostMessage(save, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
        return null;
    }

    /// <summary>Presses Cancel (Button 2).</summary>
    public static void Cancel(IntPtr dialog)
    {
        var cancel = Child(dialog, "Button", 2);
        if (cancel != IntPtr.Zero) PostMessage(cancel, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
    }
}

public static class XdeToolbar
{
    delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc f, IntPtr l);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [StructLayout(LayoutKind.Sequential)] struct RECT { public int L, T, R, B; }
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);

    const uint WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202;

    /// <summary>The toolbar: the process's visible top-level window 38 pixels wide (read 2026-11-08, XDE 8.1).</summary>
    public static IntPtr Find(int processId)
    {
        var found = IntPtr.Zero;
        EnumWindows((h, l) =>
        {
            uint pid;
            GetWindowThreadProcessId(h, out pid);
            RECT r;
            if (found == IntPtr.Zero && pid == processId && IsWindowVisible(h) && GetWindowRect(h, out r) && r.R - r.L == 38) found = h;
            return true;
        }, IntPtr.Zero);
        return found;
    }

    /// <summary>Clicks the toolbar's last button, &gt;&gt; (Additional Tools), 20 pixels above its bottom.</summary>
    public static bool OpenTools(int processId)
    {
        var bar = Find(processId);
        if (bar == IntPtr.Zero) return false;
        RECT r;
        GetWindowRect(bar, out r);
        var point = (IntPtr)(((r.B - r.T - 20) << 16) | 19);
        PostMessage(bar, WM_LBUTTONDOWN, (IntPtr)1, point);
        PostMessage(bar, WM_LBUTTONUP, IntPtr.Zero, point);
        return true;
    }
}
