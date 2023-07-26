using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using VRGIN.Core;
using IntPtr = System.IntPtr;

namespace VRGIN.Native
{
    public class WindowManager
    {
        private static IntPtr? _Handle;

        public static IntPtr Handle
        {
            get
            {
                if (!_Handle.HasValue)
                {
                    var num = 0;
                    var lpRect = default(WindowsInterop.RECT);
                    var currentProcess = Process.GetCurrentProcess();
                    var rootWindowsOfProcess = GetRootWindowsOfProcess(currentProcess.Id);

                    if (rootWindowsOfProcess.Count == 0)
                    {
                        // Looks like on some Linux systems no windows are reported
                        // These workarounds are far from perfect but it's better than not working at all
                        try
                        {
                            _Handle = currentProcess.MainWindowHandle;
                            VRLog.Warn("Assuming MainWindowHandle is the main window. Cursor and GUI might have issues!");
                        }
                        catch (Exception e)
                        {
                            VRLog.Error(e);
                        }

                        if (!_Handle.HasValue || _Handle.Value == IntPtr.Zero)
                        {
                            VRLog.Warn("No window handles found! Cursor and GUI are going to have issues!");
                            _Handle = IntPtr.Zero;
                        }
                    }
                    else
                    {
                        foreach (var item in rootWindowsOfProcess)
                        {
                            if (WindowsInterop.GetWindowRect(item, ref lpRect) && lpRect.Right - lpRect.Left > num)
                            {
                                num = lpRect.Right - lpRect.Left;
                                _Handle = item;
                            }
                        }

                        if (!_Handle.HasValue)
                        {
                            VRLog.Warn("Fall back to first handle!");
                            _Handle = rootWindowsOfProcess.First();
                        }
                    }
                }

                return _Handle.Value;
            }
        }

        private static List<IntPtr> GetRootWindowsOfProcess(int pid)
        {
            var childWindows = GetChildWindows(IntPtr.Zero);
            var list = new List<IntPtr>();
            foreach (var item in childWindows)
            {
                WindowsInterop.GetWindowThreadProcessId(item, out var lpdwProcessId);
                if (lpdwProcessId == pid) list.Add(item);
            }

            return list;
        }

        private static List<IntPtr> GetChildWindows(IntPtr parent)
        {
            var list = new List<IntPtr>();
            var value = GCHandle.Alloc(list);
            try
            {
                WindowsInterop.Win32Callback callback = EnumWindow;
                WindowsInterop.EnumChildWindows(parent, callback, GCHandle.ToIntPtr(value));
                return list;
            }
            finally
            {
                if (value.IsAllocated) value.Free();
            }
        }

        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            (GCHandle.FromIntPtr(pointer).Target as List<IntPtr> ?? throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>")).Add(handle);
            return true;
        }

        public static string GetWindowText(IntPtr hWnd)
        {
            var stringBuilder = new StringBuilder(WindowsInterop.GetWindowTextLength(hWnd) + 1);
            WindowsInterop.GetWindowText(hWnd, stringBuilder, stringBuilder.Capacity);
            return stringBuilder.ToString();
        }

        public static void ConfineCursor()
        {
            var rcClip = GetClientRect();
            WindowsInterop.ClipCursor(ref rcClip);
        }

        public static WindowsInterop.RECT GetClientRect()
        {
            var handle = Handle;
            if (handle == IntPtr.Zero)
                return new WindowsInterop.RECT(0, 0, Screen.width, Screen.height);

            var lpRect = default(WindowsInterop.RECT);
            WindowsInterop.GetClientRect(handle, ref lpRect);
            var lpPoint = default(WindowsInterop.POINT);
            WindowsInterop.ClientToScreen(handle, ref lpPoint);
            lpRect.Left = lpPoint.X;
            lpRect.Top = lpPoint.Y;
            lpRect.Right += lpPoint.X;
            lpRect.Bottom += lpPoint.Y;
            return lpRect;
        }

        public static void Activate()
        {
            var handle = Handle;
            if (handle != IntPtr.Zero)
                WindowsInterop.SetForegroundWindow(handle);
        }
    }
}
