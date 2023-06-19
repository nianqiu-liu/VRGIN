using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRGIN.Core;

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
                    _ = Process.GetCurrentProcess().ProcessName;
                    var rootWindowsOfProcess = GetRootWindowsOfProcess(Process.GetCurrentProcess().Id);
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
            var lpRect = default(WindowsInterop.RECT);
            WindowsInterop.GetClientRect(Handle, ref lpRect);
            var lpPoint = default(WindowsInterop.POINT);
            WindowsInterop.ClientToScreen(Handle, ref lpPoint);
            lpRect.Left = lpPoint.X;
            lpRect.Top = lpPoint.Y;
            lpRect.Right += lpPoint.X;
            lpRect.Bottom += lpPoint.Y;
            return lpRect;
        }

        public static void Activate()
        {
            WindowsInterop.SetForegroundWindow(Handle);
        }
    }
}
