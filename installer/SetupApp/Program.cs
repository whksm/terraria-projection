using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProjectionSetup
{
    internal sealed class CliArgs
    {
        public bool Silent;
        public bool NoShortcut;
        public bool SelfTest;
        public bool ShowHelp;
        public bool ShowVersion;
        public bool DetectOnly;
        /// <summary>还原模式：用同版本 vanilla 备份还原 Terraria.exe。</summary>
        public bool Restore;
        /// <summary>只检查版本/备份状态，不写任何文件。</summary>
        public bool Check;
        public string GameDir;
        /// <summary>自定义快捷方式名（不带 .lnk）；为空=泰拉瑞亚投影MOD。测试时用测试名，绝不覆盖真名。</summary>
        public string ShortcutName;
        /// <summary>快捷方式放哪个目录；为空=当前用户桌面。测试时指到临时目录。</summary>
        public string ShortcutDir;

        public static CliArgs Parse(string[] args)
        {
            CliArgs c = new CliArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i] ?? "";
                string low = a.ToLowerInvariant();
                if (low == "--silent" || low == "-silent" || low == "/silent") c.Silent = true;
                else if (low == "--no-shortcut") c.NoShortcut = true;
                else if (low == "--selftest" || low == "--self-test") c.SelfTest = true;
                else if (low == "--detect-only") c.DetectOnly = true;
                else if (low == "--restore") c.Restore = true;
                else if (low == "--check") c.Check = true;
                else if (low == "--help" || low == "-h" || low == "/?" || low == "-?") c.ShowHelp = true;
                else if (low == "--version" || low == "-v") c.ShowVersion = true;
                else if (low == "--shortcut-name" || low == "-n")
                {
                    if (i + 1 >= args.Length) throw new ArgumentException("--shortcut-name 后面缺少名字");
                    c.ShortcutName = args[++i];
                }
                else if (low.StartsWith("--shortcut-name="))
                {
                    c.ShortcutName = a.Substring("--shortcut-name=".Length);
                }
                else if (low == "--shortcut-dir")
                {
                    if (i + 1 >= args.Length) throw new ArgumentException("--shortcut-dir 后面缺少路径");
                    c.ShortcutDir = args[++i];
                }
                else if (low.StartsWith("--shortcut-dir="))
                {
                    c.ShortcutDir = a.Substring("--shortcut-dir=".Length);
                }
                else if (low == "--dir" || low == "-d")
                {
                    if (i + 1 >= args.Length) throw new ArgumentException("--dir 后面缺少路径");
                    c.GameDir = args[++i];
                }
                else if (low.StartsWith("--dir="))
                {
                    c.GameDir = a.Substring(6);
                }
                else if (a.StartsWith("-"))
                {
                    throw new ArgumentException("未知参数: " + a);
                }
                else if (string.IsNullOrEmpty(c.GameDir))
                {
                    c.GameDir = a; // 位置参数 = 游戏目录
                }
                else
                {
                    throw new ArgumentException("多余的参数: " + a);
                }
            }
            return c;
        }

        public string Describe()
        {
            List<string> parts = new List<string>();
            if (Silent) parts.Add("--silent");
            if (NoShortcut) parts.Add("--no-shortcut");
            if (SelfTest) parts.Add("--selftest");
            if (DetectOnly) parts.Add("--detect-only");
            if (Restore) parts.Add("--restore");
            if (Check) parts.Add("--check");
            if (ShowHelp) parts.Add("--help");
            if (ShowVersion) parts.Add("--version");
            if (!string.IsNullOrWhiteSpace(ShortcutName)) parts.Add("--shortcut-name=" + ShortcutName);
            if (!string.IsNullOrWhiteSpace(ShortcutDir)) parts.Add("--shortcut-dir=" + ShortcutDir);
            parts.Add("游戏目录=" + (string.IsNullOrEmpty(GameDir) ? "(自动检测)" : GameDir));
            return string.Join(" ", parts);
        }
    }

    internal static class Program
    {
        private static bool _silent;

        [STAThread]
        private static int Main(string[] args)
        {
            // 第一件事：把“我启动了”写进日志。任何无声退出都能定位到断点。
            Log.Init();
            Log.Line("================================");
            Log.Line(Installer.AppName + " v" + Installer.Version);
            Log.Line("启动时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Log.Line("程序路径: " + SafeProcessPath());
            Log.Line("进程 ID : " + Environment.ProcessId + "，64 位进程=" + Environment.Is64BitProcess);
            Log.Line("运行时  : .NET " + Environment.Version + "，OS " + Environment.OSVersion.VersionString);
            Log.Line("命令行  : " + Environment.CommandLine);
            Log.Line("日志文件: " + (Log.Path ?? "(无法写日志文件!)"));
            Log.Line("内嵌资源: " + string.Join(", ", ResourceNames()));

            AppDomain.CurrentDomain.UnhandledException += delegate (object s, UnhandledExceptionEventArgs e)
            {
                HandleFatal("未处理异常（AppDomain）", e.ExceptionObject as Exception);
            };
            Application.ThreadException += delegate (object s, ThreadExceptionEventArgs e)
            {
                HandleFatal("界面线程异常", e.Exception);
            };
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            TaskScheduler.UnobservedTaskException += delegate (object s, UnobservedTaskExceptionEventArgs e)
            {
                Log.Exception("后台任务异常", e.Exception);
                e.SetObserved();
            };

            CliArgs cli;
            try
            {
                cli = CliArgs.Parse(args);
            }
            catch (Exception e)
            {
                Log.Exception("解析命令行失败", e);
                Console.Error.WriteLine("参数错误: " + e.Message);
                Console.Error.WriteLine(HelpText());
                return (int)ExitCodes.BadArgs;
            }

            _silent = cli.Silent;
            Log.Line("参数解析: " + cli.Describe());

            if (cli.ShowHelp)
            {
                ConOut.Init();
                ConOut.Write(HelpText());
                return 0;
            }
            if (cli.ShowVersion)
            {
                ConOut.Init();
                ConOut.Write(Installer.AppName + " v" + Installer.Version + "（日志: " + (Log.Path ?? "不可写") + "）");
                return 0;
            }
            if (cli.SelfTest)
            {
                ConOut.Init();
                return SelfTest();
            }
            if (cli.DetectOnly)
            {
                ConOut.Init();
                string how;
                string dir = Installer.DetectOnly(cli.GameDir, out how);
                ConOut.Write(dir == null ? "未找到游戏目录" : "找到游戏目录: " + dir + "（" + how + "）");
                Log.Line("检测结果: " + (dir ?? "(未找到)") + "  来源=" + (how ?? "-"));
                return string.IsNullOrEmpty(dir) || !File.Exists(Path.Combine(dir, "Terraria.exe"))
                    ? (int)ExitCodes.GameNotFound
                    : 0;
            }

            if (cli.Check)
            {
                ConOut.Init();
                return RunCheck(cli);
            }
            if (cli.Restore)
            {
                ConOut.Init();
                return RunRestore(cli);
            }

            if (cli.Silent) return RunSilent(cli);
            return RunUi(cli);
        }

        // ------------------------------------------------------------------

        private static int RunSilent(CliArgs cli)
        {
            ConOut.Init();
            ConOut.Write(Installer.AppName + " v" + Installer.Version + "  [静默模式]");
            ConOut.Write("日志: " + (Log.Path ?? "(不可写)"));

            InstallOptions opt = new InstallOptions
            {
                GameDir = cli.GameDir,
                Silent = true,
                NoShortcut = cli.NoShortcut,
                ShortcutName = cli.ShortcutName,
                ShortcutDir = cli.ShortcutDir,
                AskGameDir = null,
                Progress = delegate (int pct, string msg) { ConOut.Write("  " + pct.ToString().PadLeft(3) + "%  " + msg); },
            };

            InstallResult r = Installer.Run(opt);
            ConOut.Write("");
            ConOut.Write(r.Ok ? "安装完成（退出码 0）" : "安装失败（退出码 " + r.ExitCode + "）：" + r.Message);
            if (r.CopiedFiles.Count > 0) ConOut.Write("已复制: " + string.Join(", ", r.CopiedFiles));
            if (!string.IsNullOrEmpty(r.ShortcutPath)) ConOut.Write("快捷方式: " + r.ShortcutPath);
            if (!string.IsNullOrEmpty(r.BackupPath)) ConOut.Write("原版备份: " + r.BackupPath);
            if (r.NoCleanBackup)
            {
                ConOut.Write("");
                ConOut.Write("[重要] 本版本没有干净的原版备份，卸载时将无法还原 exe。");
                foreach (string line in BackupStore.SteamVerifyHints(r.VersionTag)) ConOut.Write(line);
            }
            ConOut.Write("日志: " + (Log.Path ?? "(不可写)"));
            Log.Close();
            return r.Ok ? (int)ExitCodes.Ok : r.ExitCode;
        }

        /// <summary>--restore：用同版本 vanilla 备份还原（找不到就报错，绝不盖旧版本）。</summary>
        private static int RunRestore(CliArgs cli)
        {
            ConOut.Init();
            ConOut.Write(Installer.AppName + " v" + Installer.Version + "  [还原模式]");
            ConOut.Write("日志: " + (Log.Path ?? "(不可写)"));

            InstallOptions opt = new InstallOptions
            {
                GameDir = cli.GameDir,
                Silent = true,
                NoShortcut = true,
                AskGameDir = null,
                Progress = delegate (int pct, string msg) { ConOut.Write("  " + pct.ToString().PadLeft(3) + "%  " + msg); },
            };

            InstallResult r = Installer.Restore(opt);
            ConOut.Write("");
            ConOut.Write(r.Ok ? "还原完成（退出码 0）" : "还原失败（退出码 " + r.ExitCode + "）：" + r.Message);
            ConOut.Write("日志: " + (Log.Path ?? "(不可写)"));
            Log.Close();
            return r.Ok ? (int)ExitCodes.Ok : r.ExitCode;
        }

        /// <summary>--check：只打印版本/备份状态，不写任何文件。</summary>
        private static int RunCheck(CliArgs cli)
        {
            ConOut.Write(Installer.AppName + " v" + Installer.Version + "  [检查模式 · 只读]");
            string how;
            string dir = Installer.DetectOnly(cli.GameDir, out how);
            if (string.IsNullOrEmpty(dir) || !File.Exists(Path.Combine(dir, "Terraria.exe")))
            {
                ConOut.Write("没有找到游戏目录" + (string.IsNullOrEmpty(dir) ? "" : "（" + dir + " 里没有 Terraria.exe）"));
                ConOut.Write("日志: " + (Log.Path ?? "(不可写)"));
                Log.Close();
                return (int)ExitCodes.GameNotFound;
            }

            ExeProbe p = BackupStore.Probe(Path.Combine(dir, "Terraria.exe"));
            ConOut.Write("游戏目录: " + dir + "  （" + how + "）");
            ConOut.Write("Terraria.exe: 版本 " + (p.VersionTag ?? "(读不出)") + "， " + p.Size + " 字节，" + (p.Patched ? "已打补丁" : "未打补丁"));
            BackupInfo m = BackupStore.FindMatchingBackup(dir, p.VersionTag);
            if (m == null) ConOut.Write("本版本原版备份: 没有");
            else if (m.Patched) ConOut.Write("本版本原版备份: " + m.FileName + "  （但它本身是打过补丁的，不可用）");
            else ConOut.Write("本版本原版备份: " + m.Path);
            ConOut.Write("");
            ConOut.Write("目录里现有的备份文件:");
            ConOut.Write("  （只读扫描，下面这些都不会被旧版本拿去还原）");
            foreach (BackupInfo b in BackupStore.ListBackups(dir))
            {
                ConOut.Write("  " + b.FileName + "   版本 " + (b.VersionTag ?? "名字里没有版本") + "   " + b.Size + " 字节   " +
                             (b.Patched ? "已打补丁" : "原版") + (b.Versioned ? "" : "（老命名）"));
            }
            ConOut.Write("");
            if (!p.Patched)
                ConOut.Write("结论: 当前 exe 是原版，无需还原。");
            else if (m != null && !m.Patched)
                ConOut.Write("结论: 已打补丁，且本版本有干净备份，可随时 --restore 还原。");
            else
            {
                ConOut.Write("结论: 已打补丁，但本版本没有干净备份，现在不能还原 exe。");
                foreach (string line in BackupStore.SteamVerifyHints(p.VersionTag)) ConOut.Write(line);
            }
            ConOut.Write("日志: " + (Log.Path ?? "(不可写)"));
            Log.Close();
            return 0;
        }

        private static int RunUi(CliArgs cli)
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                try { Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); } catch { }

                using (MainForm form = new MainForm(cli))
                {
                    Application.Run(form);
                    Log.Close();
                    return form.ExitCode;
                }
            }
            catch (Exception e)
            {
                HandleFatal("界面启动失败", e);
                return (int)ExitCodes.Unexpected;
            }
        }

        /// <summary>
        /// 自检：证明「单文件 exe 能跑起来 + WinForms 能加载 + 内嵌载荷齐全」。
        /// 不碰游戏目录、不写任何游戏文件。退出码 0=环境正常。
        /// </summary>
        private static int SelfTest()
        {
            ConOut.Write(Installer.AppName + " v" + Installer.Version + "  [自检 --selftest]");
            ConOut.Write("程序路径: " + SafeProcessPath());
            ConOut.Write("日志文件: " + (Log.Path ?? "(不可写)"));
            bool ok = true;

            Log.Line("--- 自检开始 ---");
            Log.Line("日志可写: " + (Log.Path != null));
            if (Log.Path == null) ok = false;

            // 内嵌载荷
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                string[] names = asm.GetManifestResourceNames();
                Log.Line("资源数量: " + names.Length);
                ConOut.Write("内嵌资源: " + names.Length + " 个");
                foreach (string n in names)
                {
                    using (Stream s = asm.GetManifestResourceStream(n))
                    {
                        long len = s == null ? -1 : s.Length;
                        bool good = len > 0;
                        if (!good) ok = false;
                        Log.Line("  资源 " + n + " = " + len + " 字节" + (good ? "" : "  [异常]"));
                    }
                }
                if (names.Length < 9) ok = false;
            }
            catch (Exception e)
            {
                Log.Exception("自检读取资源失败", e);
                ConOut.Write("读取资源失败: " + e.Message);
                ok = false;
            }

            // 代码页 936（Patcher 输出解码用）
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                Encoding gbk = Encoding.GetEncoding(936);
                Log.Line("GBK 代码页可用: " + gbk.WebName);
                ConOut.Write("GBK 代码页: OK (" + gbk.WebName + ")");
            }
            catch (Exception e)
            {
                Log.Line("GBK 代码页不可用（将回退 UTF-8）: " + e.Message);
                ConOut.Write("GBK 代码页: 不可用（回退 UTF-8）");
            }

            // WinForms 能否真正加载（创建窗口但绝不显示）
            try
            {
                using (Form probe = new Form())
                {
                    probe.Text = "selftest";
                    Log.Line("WinForms 窗口创建: OK，客户区 " + probe.ClientSize);
                    ConOut.Write("WinForms: OK");
                }
            }
            catch (Exception e)
            {
                Log.Exception("WinForms 创建失败", e);
                ConOut.Write("WinForms 失败: " + e.Message);
                ok = false;
            }

            // 快捷方式 COM 可用性（只建一个临时 lnk，然后删掉）
            try
            {
                string tmpLnk = Path.Combine(Path.GetTempPath(), "ProjectionSetupSelftest_" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".lnk");
                ShellLink.Create(tmpLnk, Path.Combine(Environment.SystemDirectory, "notepad.exe"), Environment.SystemDirectory, null, "selftest");
                bool exists = File.Exists(tmpLnk);
                if (exists) File.Delete(tmpLnk);
                Log.Line("IShellLink COM: " + (exists ? "OK" : "失败"));
                ConOut.Write("IShellLink COM: " + (exists ? "OK" : "失败"));
                if (!exists) ok = false;
            }
            catch (Exception e)
            {
                Log.Exception("IShellLink 自检失败（安装时会回退 PowerShell）", e);
                ConOut.Write("IShellLink COM: 失败（安装时回退 PowerShell）");
            }

            Log.Line("--- 自检结果: " + (ok ? "通过" : "有问题") + " ---");
            ConOut.Write(ok ? "自检通过（退出码 0）" : "自检有问题（退出码 9）");
            Log.Close();
            return ok ? 0 : (int)ExitCodes.Unexpected;
        }

        // ------------------------------------------------------------------

        private static void HandleFatal(string context, Exception e)
        {
            try { Log.Exception(context, e); } catch { }
            string text = Installer.AppName + " 遇到错误：\r\n\r\n" +
                          (e == null ? "(未知错误)" : e.GetType().Name + ": " + e.Message) +
                          "\r\n\r\n日志：" + (Log.Path ?? "(无法写入日志)");
            if (_silent)
            {
                ConOut.WriteError(text);
                return;
            }
            try
            {
                MessageBox.Show(text, Installer.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }

        private static string[] ResourceNames()
        {
            try { return Assembly.GetExecutingAssembly().GetManifestResourceNames(); }
            catch { return Array.Empty<string>(); }
        }

        private static string SafeProcessPath()
        {
            try { return Environment.ProcessPath ?? "(未知)"; }
            catch { return "(未知)"; }
        }

        private static string HelpText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Installer.AppName + " v" + Installer.Version);
            sb.AppendLine();
            sb.AppendLine("用法：");
            sb.AppendLine("  Setup.exe                                    打开图形界面（自动找游戏目录）");
            sb.AppendLine("  Setup.exe \"D:\\path\\to\\Terraria\"            打开界面并预填游戏目录");
            sb.AppendLine("  Setup.exe --silent \"D:\\path\\to\\Terraria\"   静默安装，退出码 0=成功、非 0=失败");
            sb.AppendLine("  Setup.exe --silent --no-shortcut \"目录\"      静默安装但不建桌面快捷方式");
            sb.AppendLine("  Setup.exe --shortcut-name \"自定义名字\"        改快捷方式名（不带 .lnk，默认 " + Installer.DefaultShortcutBaseName + "）");
            sb.AppendLine("  Setup.exe --shortcut-dir \"目录\"              快捷方式放到指定目录而不是桌面");
            sb.AppendLine("  Setup.exe --detect-only                      只检测游戏目录，不安装");
            sb.AppendLine("  Setup.exe --check \"目录\"                    只打印版本/备份状态（只读，不写文件）");
            sb.AppendLine("  Setup.exe --restore \"目录\"                  用手头同版本的原版备份还原 Terraria.exe");
            sb.AppendLine("  Setup.exe --selftest                         环境自检（不碰游戏）");
            sb.AppendLine("  Setup.exe --version / --help                 版本 / 帮助");
            sb.AppendLine();
            sb.AppendLine("备份规则：备份名带版本 Terraria_backup_<版本>_vanilla.exe（版本取 exe 的 FileVersion）。");
            sb.AppendLine("  只备份未打补丁的 exe；版本一变就重新备份；还原只认同版本备份，");
            sb.AppendLine("  找不到就报错并让你去 Steam 校验完整性（绝不默默盖旧版本上去）。");
            sb.AppendLine();
            sb.AppendLine("退出码：0 成功 / 1 取消 / 2 参数错误 / 3 找不到游戏 / 4 游戏正在运行");
            sb.AppendLine("        5 备份失败 / 6 打补丁失败 / 7 复制文件失败 / 8 没有同版本备份（还原拒绝） / 9 未知异常");
            sb.AppendLine();
            sb.AppendLine("日志：exe 同目录 install.log（不可写时用 %TEMP%\\install.log）");
            sb.AppendLine();
            sb.AppendLine("测试建议：用 --shortcut-name 测试名 或 --shortcut-dir 临时目录，避免覆盖桌面上的真快捷方式；");
            sb.AppendLine("          只验证补丁/复制/幂等，不要试图启动游戏副本（缺 steam_api.dll 与 Content）。");
            sb.AppendLine();
            sb.AppendLine("快捷方式命名：默认「" + Installer.DefaultShortcutBaseName + "」。");
            sb.AppendLine("  桌面上若有同名文件夹（大小写不敏感），打开该路径会被解析成 .lnk 直接启动游戏，");
            sb.AppendLine("  所以安装器会先查同名文件夹/同名别人的快捷方式，冲突就自动加 (2)(3) 后缀，绝不覆盖别人的。");
            return sb.ToString();
        }
    }
}
