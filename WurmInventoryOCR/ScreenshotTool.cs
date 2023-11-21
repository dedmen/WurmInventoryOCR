using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;


namespace WurmInventoryOCR
{
    using System.Runtime.InteropServices;
    using HWND = System.IntPtr;

    /// <summary>Contains functionality to get all the open windows.</summary>
    public static class OpenWindowGetter
    {
        /// <summary>Returns a dictionary that contains the handle and title of all the open windows.</summary>
        /// <returns>A dictionary that contains the handle and title of all the open windows.</returns>
        public static IDictionary<HWND, string> GetOpenWindows()
        {
            HWND shellWindow = GetShellWindow();
            Dictionary<HWND, string> windows = new Dictionary<HWND, string>();

            EnumWindows(delegate (HWND hWnd, int lParam)
            {
                if (hWnd == shellWindow) return true;
                if (!IsWindowVisible(hWnd)) return true;

                int length = GetWindowTextLength(hWnd);
                if (length == 0) return true;

                StringBuilder builder = new StringBuilder(length);
                GetWindowText(hWnd, builder, length + 1);

                windows[hWnd] = builder.ToString();
                return true;

            }, 0);

            return windows;
        }

        private delegate bool EnumWindowsProc(HWND hWnd, int lParam);

        [DllImport("USER32.DLL")]
        private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

        [DllImport("USER32.DLL")]
        private static extern int GetWindowText(HWND hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("USER32.DLL")]
        private static extern int GetWindowTextLength(HWND hWnd);

        [DllImport("USER32.DLL")]
        private static extern bool IsWindowVisible(HWND hWnd);

        [DllImport("USER32.DLL")]
        private static extern IntPtr GetShellWindow();
    }

    public static class ScreenshotTool
    {

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // Captures a screenshot of the specified window region (the region, not the window! We could catch other windows that are overlapping the region that aren't part of the window itself)
        public static System.Drawing.Bitmap CaptureWindowScreenshot(HWND targetWindowHandle)
        {
            if (targetWindowHandle != IntPtr.Zero)
            {
                RECT windowRect;
                GetWindowRect(targetWindowHandle, out windowRect);

                Bitmap playerPicture = new Bitmap(windowRect.Right - windowRect.Left, windowRect.Bottom - windowRect.Top);
                using (Graphics graphics = Graphics.FromImage(playerPicture))
                {
                    graphics.CopyFromScreen(
                        windowRect.Left,
                        windowRect.Top,
                        0,
                        0,
                        playerPicture.Size);
                }

                return playerPicture;
            }

            return null;
        }

        // 
        public static HWND FindWurmWindow()
        {
            // Find the window handle to screenshot, by iterating through all processes

            var openWindows = OpenWindowGetter.GetOpenWindows();

            // Process code below is alternative if window title isn't good enough.

            var result = openWindows.FirstOrDefault((x) => x.Value.Contains("jackd23"), new KeyValuePair<HWND, string>());

            if (string.IsNullOrEmpty(result.Value))
            {
                //#TODO failed, handle it
                Debugger.Break();
            }

            return result.Key;

            //IntPtr targetWindowHandle = IntPtr.Zero;
            //Process[] processes = Process.GetProcesses();
            //foreach (Process process in processes)
            //{
            //    if (process.ProcessName.Contains("wurm", StringComparison.OrdinalIgnoreCase) || process.MainWindowTitle.Contains("Wurm", StringComparison.OrdinalIgnoreCase))
            //    {
            //        targetWindowHandle = process.MainWindowHandle;
            //        break;
            //    }
            //}
        }


    }
}
