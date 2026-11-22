using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ProjectionSetup
{
    /// <summary>
    /// WinExe 默认没有控制台。静默模式下尝试挂到父进程控制台，
    /// 这样 `Setup.exe --silent ...` 在终端里能看到进度（管道捕获也能拿到）。
    /// 挂不上就静默失败，不影响日志与退出码。
    /// </summary>
    internal static class ConOut
    {
        private const uint ATTACH_PARENT_PROCESS = 0xFFFFFFFF;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);

        private static bool _init;

        public static void Init()
        {
            if (_init) return;
            _init = true;
            try { AttachConsole(ATTACH_PARENT_PROCESS); }
            catch { }
            try { Console.OutputEncoding = new UTF8Encoding(false); }
            catch { }
            try
            {
                StreamWriter w = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
                Console.SetOut(w);
            }
            catch { }
        }

        public static void Write(string text)
        {
            try { Console.Out.WriteLine(text ?? ""); }
            catch { }
        }

        public static void WriteError(string text)
        {
            try { Console.Error.WriteLine(text ?? ""); }
            catch { }
        }
    }
}
