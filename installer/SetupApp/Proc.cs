using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ProjectionSetup
{
    /// <summary>子进程执行器：捕获原始 stdout/stderr 字节，退出码和时间都很清楚。</summary>
    internal static class Proc
    {
        public sealed class Result
        {
            public int ExitCode = -1;
            public bool TimedOut;
            public string StdOut = "";
            public string StdErr = "";
            public string Error;
            public long ElapsedMs;
        }

        public static Result Run(string fileName, string arguments, string workingDir, int timeoutMs)
        {
            Result r = new Result();
            Stopwatch sw = Stopwatch.StartNew();
            Process p = null;
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);
                psi.WorkingDirectory = workingDir;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                p = new Process();
                p.StartInfo = psi;
                if (!p.Start())
                {
                    r.Error = "进程启动失败（Start 返回 false）";
                    return r;
                }

                Stream outStream = p.StandardOutput.BaseStream;
                Stream errStream = p.StandardError.BaseStream;
                Task<byte[]> tOut = Task.Run(() => ReadAll(outStream));
                Task<byte[]> tErr = Task.Run(() => ReadAll(errStream));

                bool exited = p.WaitForExit(timeoutMs);
                if (!exited)
                {
                    r.TimedOut = true;
                    try { p.Kill(true); } catch { }
                    try { p.WaitForExit(5000); } catch { }
                }

                byte[] bo = Finish(tOut);
                byte[] be = Finish(tErr);
                r.StdOut = Decode(bo);
                r.StdErr = Decode(be);
                try { r.ExitCode = p.ExitCode; } catch { r.ExitCode = -1; }
                if (r.TimedOut) r.Error = "超时（" + timeoutMs + " ms）";
            }
            catch (Exception e)
            {
                r.Error = e.GetType().Name + ": " + e.Message;
            }
            finally
            {
                if (p != null)
                {
                    try { p.Dispose(); } catch { }
                }
            }
            sw.Stop();
            r.ElapsedMs = sw.ElapsedMilliseconds;
            return r;
        }

        private static byte[] ReadAll(Stream s)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] buf = new byte[8192];
                    int n;
                    while ((n = s.Read(buf, 0, buf.Length)) > 0) ms.Write(buf, 0, n);
                    return ms.ToArray();
                }
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        private static byte[] Finish(Task<byte[]> t)
        {
            try
            {
                if (t.Wait(5000) && t.Status == TaskStatus.RanToCompletion) return t.Result;
            }
            catch { }
            return Array.Empty<byte>();
        }

        private static Encoding _gbk;
        private static bool _gbkTried;

        private static Encoding Gbk()
        {
            if (!_gbkTried)
            {
                _gbkTried = true;
                try
                {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    _gbk = Encoding.GetEncoding(936);
                }
                catch { _gbk = null; }
            }
            return _gbk;
        }

        /// <summary>
        /// Patcher.exe 是 .NET Framework 控制台程序：重定向时它按系统 ANSI 代码页（简中=936）输出。
        /// 这里 UTF-8 / GBK 各解一次，选替换字符更少的那个。
        /// </summary>
        private static string Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return "";
            string utf8 = Encoding.UTF8.GetString(bytes);
            Encoding gbk = Gbk();
            if (gbk == null) return utf8;
            string g = gbk.GetString(bytes);
            return Count(utf8, '\uFFFD') <= Count(g, '\uFFFD') ? utf8 : g;
        }

        private static int Count(string s, char c)
        {
            int n = 0;
            for (int i = 0; i < s.Length; i++) if (s[i] == c) n++;
            return n;
        }
    }
}
