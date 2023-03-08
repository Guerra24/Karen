using System;
using System.Runtime.InteropServices;

namespace Karen.Interop
{
    public static class Kernel32
    {

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        public static extern int GetProcessId(IntPtr handle);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hHandle);

        public const int STD_INPUT_HANDLE = -10;
        public const int STD_OUTPUT_HANDLE = -11;
        public const int STD_ERROR_HANDLE = -12;

        public static void ShowConsole()
        {
            User32.ShowWindow(GetConsoleWindow(), User32.SW_SHOW);
            User32.ShowWindow(GetConsoleWindow(), User32.SW_RESTORE);
        }

        public static void HideConsole()
        {
            User32.ShowWindow(GetConsoleWindow(), User32.SW_HIDE);
        }
    }
}
