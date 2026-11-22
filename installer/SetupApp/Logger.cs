using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ProjectionSetup
{
    /// <summary>
    /// 全程可见的日志。第一次启动就写文件，任何“无声退出”都能在日志里看到断在哪一步。
    /// 位置优先 exe 同目录 install.log（不可写时回退 %TEMP%\install.log）。
    /// </summary>
    internal static class Log
    {
        private static readonly object Gate = new object();
        private static StreamWriter _writer;
        private static string _path;
        private static bool _fallback;

        public static string Path { get { return _path; } }

        /// <summary>每写一行日志都会触发（界面用它实时显示）。</summary>
        public static event Action<string> Written;

        /// <summary>日志没能写到 exe 同目录，退到了临时目录。</summary>
        public static bool FellBackToTemp { get { return _fallback; } }

        public static void Init()
        {
            List<string> candidates = new List<string>();
            try { candidates.Add(System.IO.Path.Combine(AppContext.BaseDirectory, "install.log")); }
            catch { }
            try { candidates.Add(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "install.log")); }
            catch { }

            for (int i = 0; i < candidates.Count; i++)
            {
                string c = candidates[i];
                try
                {
                    FileStream fs = new FileStream(c, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    _writer = new StreamWriter(fs, new UTF8Encoding(false)) { AutoFlush = true };
                    _path = c;
                    _fallback = i > 0;
                    return;
                }
                catch
                {
                    // 换下一个位置
                }
            }
            _path = null;
        }

        /// <summary>写一行带时间戳的日志。</summary>
        public static void Line(string message)
        {
            string text = DateTime.Now.ToString("HH:mm:ss.fff") + "  " + (message ?? "");
            lock (Gate)
            {
                try { if (_writer != null) _writer.WriteLine(text); }
                catch { }
            }
            Action<string> handler = Written;
            if (handler != null)
            {
                try { handler(message ?? ""); }
                catch { }
            }
        }

        public static void Line()
        {
            Line("");
        }

        /// <summary>日志分隔头（每次安装开始时打一条，方便多次安装的证据并存）。</summary>
        public static void Header()
        {
            lock (Gate)
            {
                try
                {
                    if (_writer != null)
                    {
                        _writer.WriteLine();
                        _writer.WriteLine("============================================================");
                        _writer.WriteLine("  泰拉瑞亚投影MOD 安装器  ·  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        _writer.WriteLine("============================================================");
                    }
                }
                catch { }
            }
        }

        public static void Exception(string context, Exception ex)
        {
            Line("[异常] " + context + ": " + (ex == null ? "(null)" : ex.GetType().FullName + ": " + ex.Message));
            if (ex != null)
            {
                foreach (string l in ex.ToString().Split('\n'))
                    Line("        " + l.TrimEnd('\r'));
            }
        }

        public static void Close()
        {
            lock (Gate)
            {
                try { if (_writer != null) { _writer.Flush(); _writer.Dispose(); } }
                catch { }
                _writer = null;
            }
        }
    }
}
