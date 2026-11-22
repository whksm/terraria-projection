using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace ProjectionSetup
{
    internal enum ExitCodes
    {
        Ok = 0,
        Cancelled = 1,
        BadArgs = 2,
        GameNotFound = 3,
        GameRunning = 4,
        BackupFailed = 5,
        PatchFailed = 6,
        CopyFailed = 7,
        Unexpected = 9,
    }

    internal sealed class InstallOptions
    {
        /// <summary>命令行/界面指定的游戏目录；为空则自动检测。</summary>
        public string GameDir;
        public bool Silent;
        public bool NoShortcut;
        /// <summary>自定义快捷方式名（不带 .lnk）；null/空=泰拉瑞亚投影MOD。</summary>
        public string ShortcutName;
        /// <summary>快捷方式目录；null/空=当前用户桌面。测试时指到临时目录，绝不碰真桌面。</summary>
        public string ShortcutDir;
        /// <summary>未找到目录时由界面弹选择框；静默模式为 null。</summary>
        public Func<string> AskGameDir;
        /// <summary>进度回调：百分比 0-100、当前步骤描述。可能在工作线程上调用。</summary>
        public Action<int, string> Progress;
    }

    internal sealed class InstallResult
    {
        public bool Ok;
        public int ExitCode;
        public string Message = "";
        public string GameDir;
        public string BackupPath;
        public string ShortcutPath;
        public long ExeSizeBefore;
        public long ExeSizeAfter;
        public int MarkerBefore;
        public int MarkerAfter;
        public int PatcherExitCode = -1;
        public readonly List<string> CopiedFiles = new List<string>();
        public readonly List<string> Warnings = new List<string>();
    }

    internal static class Installer
    {
        public const string AppName = "泰拉瑞亚投影MOD 安装器";
        public const string Version = "1.2.0";
        private const int TotalSteps = 6;

        private sealed class PayloadFile
        {
            public readonly string Resource;
            public readonly string FileName;
            public readonly bool OnlyIfMissing;
            public PayloadFile(string resource, string fileName, bool onlyIfMissing)
            {
                Resource = resource;
                FileName = fileName;
                OnlyIfMissing = onlyIfMissing;
            }
        }

        /// <summary>给 Patcher.exe 用的临时目录文件（Patcher 用 Assembly.Location 找 ProjectionMod.dll）。</summary>
        private static readonly PayloadFile[] TempFiles =
        {
            new PayloadFile("pl.Patcher.exe", "Patcher.exe", false),
            new PayloadFile("pl.Mono.Cecil.dll", "Mono.Cecil.dll", false),
            new PayloadFile("pl.ProjectionMod.dll", "ProjectionMod.dll", false),
        };

        /// <summary>要复制进游戏目录的文件（顺序、覆盖规则复刻 release/setup.cmd）。</summary>
        private static readonly PayloadFile[] GameFiles =
        {
            new PayloadFile("pl.ProjectionMod.dll", "ProjectionMod.dll", false),
            new PayloadFile("pl.0Harmony.dll", "0Harmony.dll", false),
            new PayloadFile("pl.ProjectionMod.bp", "ProjectionMod.bp", true),
            new PayloadFile("pl.icon.ico", "ProjectionMod.ico", false),
            new PayloadFile("pl.Uninstall.bat", "卸载.bat", false),
            new PayloadFile("pl.Readme.md", "投影MOD_使用说明.md", false),
        };

        // ------------------------------------------------------------------
        //  对外入口
        // ------------------------------------------------------------------

        public static InstallResult Run(InstallOptions opt)
        {
            InstallResult r = new InstallResult();
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                Log.Header();
                Log.Line("开始安装（静默=" + opt.Silent + "，指定目录=" + (string.IsNullOrEmpty(opt.GameDir) ? "(无，自动检测)" : opt.GameDir) + "）");

                // ---------- 1/6 定位游戏目录 ----------
                BeginStep(opt, 1, "定位游戏目录");
                string how;
                string gameDir = ResolveGameDir(opt.GameDir, out how);
                if (string.IsNullOrEmpty(gameDir) && opt.AskGameDir != null && !opt.Silent)
                {
                    Log.Line("      没有自动找到，弹出目录选择框…");
                    gameDir = opt.AskGameDir();
                    how = "手动选择";
                }
                if (string.IsNullOrEmpty(gameDir))
                {
                    r.ExitCode = (int)ExitCodes.GameNotFound;
                    r.Message = "没有找到泰拉瑞亚游戏目录（需要包含 Terraria.exe 的文件夹）。";
                    Log.Line("      [失败] " + r.Message);
                    return Finish(r, opt, sw);
                }

                gameDir = NormalizeDir(gameDir);
                r.GameDir = gameDir;
                string exePath = Path.Combine(gameDir, "Terraria.exe");
                if (!File.Exists(exePath))
                {
                    r.ExitCode = (int)ExitCodes.GameNotFound;
                    r.Message = "这个目录里没有 Terraria.exe：" + gameDir;
                    Log.Line("      [失败] " + r.Message);
                    return Finish(r, opt, sw);
                }
                Log.Line("      游戏目录: " + gameDir + "  （来源: " + how + "）");
                Log.Line("      Terraria.exe: " + new FileInfo(exePath).Length + " 字节, 版本 " + FileVersion(exePath));

                // ---------- 2/6 检查游戏是否在运行 ----------
                BeginStep(opt, 2, "检查游戏是否正在运行");
                List<int> pids = RunningGamePids(gameDir);
                if (pids.Count > 0)
                {
                    r.ExitCode = (int)ExitCodes.GameRunning;
                    r.Message = "检测到游戏正在运行（PID " + string.Join(", ", pids) + "），请先完全退出泰拉瑞亚再安装。";
                    Log.Line("      [失败] " + r.Message);
                    return Finish(r, opt, sw);
                }
                Log.Line("      没有检测到正在运行的 Terraria.exe");

                // ---------- 3/6 备份 ----------
                BeginStep(opt, 3, "备份原版 Terraria.exe");
                string backup = Path.Combine(gameDir, "Terraria_backup_projection.exe");
                r.BackupPath = backup;
                if (File.Exists(backup))
                {
                    Log.Line("      备份已存在，按规则不覆盖: Terraria_backup_projection.exe (" + new FileInfo(backup).Length + " 字节)");
                }
                else
                {
                    try
                    {
                        File.Copy(exePath, backup, false);
                    }
                    catch (Exception e)
                    {
                        Log.Exception("备份失败", e);
                        r.ExitCode = (int)ExitCodes.BackupFailed;
                        r.Message = "备份 Terraria.exe 失败：" + e.Message;
                        return Finish(r, opt, sw);
                    }
                    long bl = new FileInfo(backup).Length;
                    Log.Line("      已备份: Terraria_backup_projection.exe (" + bl + " 字节)");
                    if (bl != new FileInfo(exePath).Length)
                    {
                        r.ExitCode = (int)ExitCodes.BackupFailed;
                        r.Message = "备份文件大小和原文件不一致（" + bl + " vs " + new FileInfo(exePath).Length + "）。";
                        Log.Line("      [失败] " + r.Message);
                        return Finish(r, opt, sw);
                    }
                }

                // ---------- 4/6 打补丁 ----------
                BeginStep(opt, 4, "给 Terraria.exe 打补丁");
                r.ExeSizeBefore = new FileInfo(exePath).Length;
                int firstOffset;
                r.MarkerBefore = CountMarker(exePath, out firstOffset);
                Log.Line("      打补丁前: " + r.ExeSizeBefore + " 字节, ProjectionMod 标记 x" + r.MarkerBefore);

                string tempDir = Path.Combine(Path.GetTempPath(), "ProjectionModSetup_" + Guid.NewGuid().ToString("N").Substring(0, 8));
                try
                {
                    Directory.CreateDirectory(tempDir);
                    foreach (PayloadFile f in TempFiles) ExtractResource(f.Resource, Path.Combine(tempDir, f.FileName));
                    Log.Line("      载荷已解压到临时目录: " + tempDir);
                    Log.Line("      （Patcher.exe 依赖同目录的 Mono.Cecil.dll / ProjectionMod.dll，工作目录也设为该目录）");

                    string patcher = Path.Combine(tempDir, "Patcher.exe");
                    Proc.Result pr = Proc.Run(patcher, "patch \"" + gameDir + "\"", tempDir, 180000);
                    r.PatcherExitCode = pr.ExitCode;
                    Log.Line("      Patcher.exe 退出码: " + pr.ExitCode + "  用时 " + pr.ElapsedMs + " ms");
                    if (!string.IsNullOrWhiteSpace(pr.StdOut))
                        Log.Line("      Patcher 输出: " + Flatten(pr.StdOut));
                    if (!string.IsNullOrWhiteSpace(pr.StdErr))
                        Log.Line("      Patcher 错误输出: " + Flatten(pr.StdErr));
                    if (!string.IsNullOrEmpty(pr.Error))
                        Log.Line("      Patcher 执行异常: " + pr.Error);
                }
                finally
                {
                    TryDeleteDir(tempDir);
                }

                r.ExeSizeAfter = new FileInfo(exePath).Length;
                r.MarkerAfter = CountMarker(exePath, out firstOffset);
                Log.Line("      打补丁后: " + r.ExeSizeAfter + " 字节（变化 " + (r.ExeSizeAfter - r.ExeSizeBefore).ToString("+0;-0;0") +
                        "）, ProjectionMod 标记 x" + r.MarkerAfter + (r.MarkerAfter > 0 ? "（首个偏移 0x" + firstOffset.ToString("X") + "）" : ""));

                if (r.MarkerAfter == 0)
                {
                    r.ExitCode = (int)ExitCodes.PatchFailed;
                    r.Message = "打补丁失败：Terraria.exe 里没有出现 ProjectionMod 引用（Patcher 退出码 " + r.PatcherExitCode +
                                "，文件 " + r.ExeSizeBefore + " -> " + r.ExeSizeAfter + " 字节）。常见原因：游戏正在运行 / 文件只读 / 游戏版本不是 1.4.5.8。";
                    Log.Line("      [失败] " + r.Message);
                    string leftover = exePath + ".tmp";
                    if (File.Exists(leftover))
                    {
                        try { File.Delete(leftover); Log.Line("      已清理残留: " + leftover); }
                        catch { Log.Line("      残留文件删不掉: " + leftover); }
                    }
                    return Finish(r, opt, sw);
                }
                if (r.MarkerBefore > 0)
                    Log.Line("      说明：安装前就已经是打好补丁的状态，本次属于重复安装");
                else
                    Log.Line("      补丁成功：入口已注入 ProjectionMod.Bootstrap.Init");
                if (r.PatcherExitCode != 0)
                {
                    r.Warnings.Add("Patcher 退出码非 0（" + r.PatcherExitCode + "），但补丁标记已存在");
                    Log.Line("      [警告] Patcher 退出码非 0，不过补丁标记已存在，按成功处理");
                }

                // ---------- 5/6 复制文件 ----------
                BeginStep(opt, 5, "复制 Mod 文件到游戏目录");
                foreach (PayloadFile f in GameFiles)
                {
                    string dst = Path.Combine(gameDir, f.FileName);
                    if (f.OnlyIfMissing && File.Exists(dst))
                    {
                        Log.Line("      跳过（已存在，不覆盖）: " + f.FileName + " (" + new FileInfo(dst).Length + " 字节)");
                        continue;
                    }
                    try
                    {
                        byte[] data = ReadResource(f.Resource);
                        File.WriteAllBytes(dst, data);
                        long len = new FileInfo(dst).Length;
                        if (len != data.Length) throw new IOException("写入后大小不符: " + len + " != " + data.Length);
                        Log.Line("      写入: " + f.FileName + " (" + len + " 字节)");
                        r.CopiedFiles.Add(f.FileName);
                    }
                    catch (Exception e)
                    {
                        Log.Exception("复制失败 " + f.FileName, e);
                        r.ExitCode = (int)ExitCodes.CopyFailed;
                        r.Message = "复制 " + f.FileName + " 到游戏目录失败：" + e.Message;
                        return Finish(r, opt, sw);
                    }
                }

                Log.Line("      游戏目录关键文件:");
                string[] keyFiles =
                {
                    "Terraria.exe", "Terraria_backup_projection.exe", "ProjectionMod.dll", "0Harmony.dll",
                    "ProjectionMod.bp", "ProjectionMod.ico", "卸载.bat", "投影MOD_使用说明.md",
                };
                foreach (string name in keyFiles)
                {
                    string p = Path.Combine(gameDir, name);
                    Log.Line("        " + (File.Exists(p) ? (new FileInfo(p).Length).ToString().PadLeft(9) + "  " + name : "   (缺失)      " + name));
                }

                // ---------- 6/6 快捷方式 ----------
                BeginStep(opt, 6, "创建桌面快捷方式");
                if (opt.NoShortcut)
                {
                    Log.Line("      按参数 --no-shortcut 跳过");
                }
                else
                {
                    CreateShortcut(opt, gameDir, exePath, r);
                }

                r.ExitCode = (int)ExitCodes.Ok;
                r.Message = "安装完成";
                return Finish(r, opt, sw);
            }
            catch (Exception e)
            {
                Log.Exception("安装过程中出现未捕获异常", e);
                r.ExitCode = (int)ExitCodes.Unexpected;
                r.Message = "安装过程中出现异常：" + e.GetType().Name + ": " + e.Message;
                try { return Finish(r, opt, sw); }
                catch { return r; }
            }
        }

        /// <summary>只做目录检测（不写任何文件），给 --detect-only 用。</summary>
        public static string DetectOnly(string forcedDir, out string how)
        {
            if (!string.IsNullOrWhiteSpace(forcedDir))
            {
                how = "命令行指定";
                return NormalizeDir(forcedDir);
            }
            return ResolveGameDir(null, out how);
        }

        // ------------------------------------------------------------------
        //  步骤 / 日志
        // ------------------------------------------------------------------

        private static void BeginStep(InstallOptions opt, int step, string title)
        {
            int pct = (int)Math.Round(100.0 * (step - 1) / TotalSteps);
            Log.Line("[" + step + "/" + TotalSteps + "] " + title);
            Action<int, string> cb = opt == null ? null : opt.Progress;
            if (cb != null)
            {
                try { cb(pct, "[" + step + "/" + TotalSteps + "] " + title); } catch { }
            }
        }

        private static InstallResult Finish(InstallResult r, InstallOptions opt, Stopwatch sw)
        {
            r.Ok = r.ExitCode == (int)ExitCodes.Ok;
            Log.Line();
            Log.Line("---------- 安装结果 ----------");
            Log.Line("结果        : " + (r.Ok ? "成功" : "失败（退出码 " + r.ExitCode + "）"));
            if (!r.Ok) Log.Line("失败原因    : " + r.Message);
            if (!string.IsNullOrEmpty(r.GameDir)) Log.Line("游戏目录    : " + r.GameDir);
            if (r.ExeSizeBefore > 0 || r.ExeSizeAfter > 0)
                Log.Line("Terraria.exe: " + r.ExeSizeBefore + " -> " + r.ExeSizeAfter + " 字节（" + (r.ExeSizeAfter - r.ExeSizeBefore).ToString("+0;-0;0") + "）");
            Log.Line("补丁标记    : 安装前 x" + r.MarkerBefore + " -> 安装后 x" + r.MarkerAfter);
            Log.Line("Patcher     : 退出码 " + r.PatcherExitCode);
            Log.Line("复制文件    : " + r.CopiedFiles.Count + " 个" + (r.CopiedFiles.Count > 0 ? ": " + string.Join(", ", r.CopiedFiles) : ""));
            Log.Line("快捷方式    : " + (string.IsNullOrEmpty(r.ShortcutPath) ? "(未创建)" : r.ShortcutPath));
            foreach (string w in r.Warnings) Log.Line("警告        : " + w);
            Log.Line("耗时        : " + sw.ElapsedMilliseconds + " ms");
            Log.Line("日志文件    : " + (Log.Path ?? "(无法写入)"));
            Log.Line("---------- 结束 ----------");
            Log.Line();

            if (opt != null && opt.Progress != null)
            {
                try { opt.Progress(100, r.Ok ? "完成" : "失败"); } catch { }
            }
            return r;
        }

        private static string Flatten(string s)
        {
            return s.Trim().Replace("\r\n", " / ").Replace("\n", " / ").Replace("\r", " ");
        }

        // ------------------------------------------------------------------
        //  游戏目录定位
        // ------------------------------------------------------------------

        private static string ResolveGameDir(string forcedDir, out string how)
        {
            how = null;
            if (!string.IsNullOrWhiteSpace(forcedDir))
            {
                how = "命令行参数";
                return NormalizeDir(forcedDir);
            }

            string env = Environment.GetEnvironmentVariable("PM_DIR");
            if (!string.IsNullOrWhiteSpace(env))
            {
                how = "环境变量 PM_DIR";
                return NormalizeDir(env);
            }

            List<string> steamRoots = SteamRoots();
            List<string> candidates = new List<string>();
            List<string> sources = new List<string>();

            foreach (string steam in steamRoots)
            {
                candidates.Add(Path.Combine(steam, "steamapps", "common", "Terraria"));
                sources.Add("Steam 安装路径 " + steam);
                foreach (string lib in SteamLibraries(steam))
                {
                    candidates.Add(Path.Combine(lib, "steamapps", "common", "Terraria"));
                    sources.Add("Steam 库 " + lib);
                }
            }

            foreach (string c in CommonCandidates())
            {
                candidates.Add(c);
                sources.Add("常见路径");
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < candidates.Count; i++)
            {
                string dir = candidates[i];
                if (string.IsNullOrWhiteSpace(dir)) continue;
                string key;
                try { key = Path.GetFullPath(dir); } catch { continue; }
                if (!seen.Add(key)) continue;

                bool ok = File.Exists(Path.Combine(dir, "Terraria.exe"));
                if (ok)
                {
                    how = sources[i];
                    Log.Line("      命中: " + dir + "  （" + how + "）");
                    return dir;
                }
                Log.Line("      未命中: " + dir);
            }

            Log.Line("      所有候选路径都没有 Terraria.exe");
            how = null;
            return null;
        }

        private static List<string> SteamRoots()
        {
            List<string> found = new List<string>();
            ReadSteamPath(Registry.CurrentUser, @"Software\Valve\Steam", found);
            ReadSteamPath(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", found);
            ReadSteamPath(Registry.LocalMachine, @"SOFTWARE\Valve\Steam", found);

            List<string> distinct = new List<string>();
            foreach (string s in found)
            {
                bool dup = false;
                foreach (string d in distinct) if (string.Equals(d, s, StringComparison.OrdinalIgnoreCase)) { dup = true; break; }
                if (!dup) distinct.Add(s);
            }
            return distinct;
        }

        private static void ReadSteamPath(RegistryKey root, string subKey, List<string> found)
        {
            try
            {
                using (RegistryKey k = root.OpenSubKey(subKey))
                {
                    object v = k == null ? null : k.GetValue("SteamPath");
                    string s = v as string;
                    if (string.IsNullOrWhiteSpace(s)) return;
                    s = s.Replace('/', '\\').TrimEnd('\\');
                    Log.Line("      注册表 " + (root == Registry.CurrentUser ? "HKCU" : "HKLM") + "\\" + subKey + " SteamPath=" + s);
                    found.Add(s);
                }
            }
            catch (Exception e)
            {
                Log.Line("      读取注册表失败 " + subKey + ": " + e.Message);
            }
        }

        /// <summary>解析 steamapps\libraryfolders.vdf 里的 "path"（Steam 库可能在其他盘）。</summary>
        private static IEnumerable<string> SteamLibraries(string steamRoot)
        {
            List<string> libs = new List<string>();
            try
            {
                string vdf = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
                if (File.Exists(vdf))
                {
                    string text = File.ReadAllText(vdf);
                    MatchCollection ms = Regex.Matches(text, "\"path\"\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
                    foreach (Match m in ms)
                    {
                        string p = m.Groups[1].Value.Replace("\\\\", "\\").TrimEnd('\\');
                        if (!string.IsNullOrWhiteSpace(p)) libs.Add(p);
                    }
                    if (libs.Count > 0) Log.Line("      libraryfolders.vdf 里找到 " + libs.Count + " 个 Steam 库");
                }
            }
            catch (Exception e)
            {
                Log.Line("      解析 libraryfolders.vdf 失败: " + e.Message);
            }
            return libs;
        }

        private static IEnumerable<string> CommonCandidates()
        {
            List<string> list = new List<string>();
            list.Add(@"D:\steam\steamapps\common\Terraria");
            list.Add(@"C:\Program Files (x86)\Steam\steamapps\common\Terraria");
            list.Add(@"E:\steam\steamapps\common\Terraria");
            list.Add(@"C:\Program Files\Steam\steamapps\common\Terraria");

            foreach (string pf in new[] { Environment.GetEnvironmentVariable("ProgramFiles(x86)"), Environment.GetEnvironmentVariable("ProgramFiles") })
            {
                if (!string.IsNullOrWhiteSpace(pf))
                    list.Add(Path.Combine(pf, "Steam", "steamapps", "common", "Terraria"));
            }

            try
            {
                foreach (DriveInfo d in DriveInfo.GetDrives())
                {
                    if (!d.IsReady) continue;
                    if (d.DriveType != DriveType.Fixed && d.DriveType != DriveType.Removable) continue;
                    string root = d.RootDirectory.FullName;
                    list.Add(Path.Combine(root, "steam", "steamapps", "common", "Terraria"));
                    list.Add(Path.Combine(root, "Steam", "steamapps", "common", "Terraria"));
                    list.Add(Path.Combine(root, "SteamLibrary", "steamapps", "common", "Terraria"));
                    list.Add(Path.Combine(root, "Games", "SteamLibrary", "steamapps", "common", "Terraria"));
                    list.Add(Path.Combine(root, "Program Files (x86)", "Steam", "steamapps", "common", "Terraria"));
                }
            }
            catch (Exception e)
            {
                Log.Line("      枚举盘符失败: " + e.Message);
            }
            return list;
        }

        private static string NormalizeDir(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir)) return dir;
            dir = dir.Trim().Trim('"');
            try { dir = Path.GetFullPath(dir); }
            catch { }
            return dir.TrimEnd('\\', '/');
        }

        private static List<int> RunningGamePids(string gameDir)
        {
            List<int> pids = new List<int>();
            string target;
            try { target = Path.GetFullPath(Path.Combine(gameDir, "Terraria.exe")); }
            catch { return pids; }

            Process[] procs;
            try { procs = Process.GetProcessesByName("Terraria"); }
            catch (Exception e) { Log.Line("      枚举进程失败: " + e.Message); return pids; }

            foreach (Process p in procs)
            {
                try
                {
                    string path = null;
                    try { path = p.MainModule == null ? null : p.MainModule.FileName; }
                    catch { }
                    if (!string.IsNullOrEmpty(path) && string.Equals(Path.GetFullPath(path), target, StringComparison.OrdinalIgnoreCase))
                        pids.Add(p.Id);
                }
                catch { }
                finally { try { p.Dispose(); } catch { } }
            }
            return pids;
        }

        // ------------------------------------------------------------------
        //  载荷 / 补丁校验
        // ------------------------------------------------------------------

        private static byte[] ReadResource(string name)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            using (Stream s = asm.GetManifestResourceStream(name))
            {
                if (s == null)
                {
                    string all = string.Join(", ", asm.GetManifestResourceNames());
                    throw new InvalidOperationException("安装包缺少内嵌资源 " + name + "（现有: " + all + "）");
                }
                using (MemoryStream ms = new MemoryStream())
                {
                    s.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        private static void ExtractResource(string resource, string destination)
        {
            byte[] data = ReadResource(resource);
            File.WriteAllBytes(destination, data);
            long len = new FileInfo(destination).Length;
            if (len != data.Length) throw new IOException("解压 " + Path.GetFileName(destination) + " 大小不符: " + len + " != " + data.Length);
        }

        /// <summary>
        /// 统计文件里 ASCII "ProjectionMod" 出现次数（补丁成功会给 Terraria.exe 增加一条 AssemblyRef）。
        /// 原版文件是 0 次，打过补丁是 1 次以上。
        /// </summary>
        private static int CountMarker(string path, out int firstOffset)
        {
            firstOffset = -1;
            try
            {
                byte[] data = File.ReadAllBytes(path);
                byte[] needle = Encoding.ASCII.GetBytes("ProjectionMod");
                int count = 0;
                int pos = 0;
                ReadOnlySpan<byte> span = data;
                while (pos <= span.Length - needle.Length)
                {
                    int hit = span.Slice(pos).IndexOf(needle);
                    if (hit < 0) break;
                    if (count == 0) firstOffset = pos + hit;
                    count++;
                    pos += hit + needle.Length;
                }
                return count;
            }
            catch (Exception e)
            {
                Log.Line("      读取 " + Path.GetFileName(path) + " 失败: " + e.Message);
                return 0;
            }
        }

        private static string FileVersion(string path)
        {
            try
            {
                FileVersionInfo vi = FileVersionInfo.GetVersionInfo(path);
                return vi.FileVersion ?? "(无)";
            }
            catch
            {
                return "(读取失败)";
            }
        }

        // ------------------------------------------------------------------
        //  快捷方式
        // ------------------------------------------------------------------

        /// <summary>
        /// 快捷方式文件名默认值。注意不要用「泰拉瑞亚投影MOD」这种会和桌面文件夹撞名的名字：
        /// 桌面上同时有同名文件夹和 .lnk 时，打开那个路径会解析到 .lnk（直接启动游戏）而不是文件夹。
        /// </summary>
        public const string DefaultShortcutBaseName = "启动泰拉瑞亚投影MOD";

        private static string SanitizeShortcutName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return DefaultShortcutBaseName;
            name = name.Trim();
            if (name.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - 4);
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            name = name.Trim().TrimEnd('.');
            return string.IsNullOrWhiteSpace(name) ? DefaultShortcutBaseName : name;
        }

        /// <summary>
        /// 挑一个不会和「同名文件夹 / 别人的同名快捷方式」冲突的文件名。
        /// 规则：
        ///   * 目录里已有同名文件夹（大小写不敏感）→ 该名字作废，加 (2) (3)… 后缀；
        ///   * 已有同名 .lnk 且指向本安装的 exe  → 直接复用（重复安装幂等，不再加后缀）；
        ///   * 已有同名 .lnk 但指向别处          → 该名字作废，加后缀，绝不覆盖。
        /// </summary>
        private static string PickShortcutPath(string dir, string baseName, string exePath, out bool reused, out string note)
        {
            reused = false;
            note = null;
            for (int i = 1; i <= 50; i++)
            {
                string name = i == 1 ? baseName : baseName + " (" + i + ")";
                string lnk = Path.Combine(dir, name + ".lnk");
                string plain = Path.Combine(dir, name);

                // 同名文件夹（或同名无扩展名文件）存在时，打开这个路径会跑到别的东西上，必须换名
                if (Directory.Exists(plain) || File.Exists(plain))
                {
                    note = "「" + name + "」已被同名文件夹占用";
                    Log.Line("      跳过名字「" + name + "」：目录里已有同名文件夹/文件 " + plain);
                    continue;
                }
                // 少见但可能：一个叫 "名字.lnk" 的文件夹
                if (Directory.Exists(lnk))
                {
                    note = "「" + name + ".lnk」被同名文件夹占用";
                    Log.Line("      跳过名字「" + name + "」：已存在同名文件夹 " + lnk);
                    continue;
                }

                if (File.Exists(lnk))
                {
                    string target = TryReadShortcutTarget(lnk);
                    if (!string.IsNullOrEmpty(target) && SamePath(target, exePath))
                    {
                        reused = true;
                        return lnk;
                    }
                    note = "「" + name + "」已有快捷方式指向 " + (string.IsNullOrEmpty(target) ? "(读不出目标)" : target);
                    Log.Line("      跳过名字「" + name + "」：已有同名快捷方式指向 " +
                            (string.IsNullOrEmpty(target) ? "(读不出目标)" : target) + "，不覆盖");
                    continue;
                }

                return lnk;
            }

            // 兜底：极端情况下加时间戳
            string fallback = baseName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            note = "同名冲突太多，改用 " + fallback;
            Log.Line("      [警告] 同名冲突太多，改用 " + fallback);
            return Path.Combine(dir, fallback + ".lnk");
        }

        private static bool SamePath(string a, string b)
        {
            try { return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }

        private static void CreateShortcut(InstallOptions opt, string gameDir, string exePath, InstallResult r)
        {
            string ico = Path.Combine(gameDir, "ProjectionMod.ico");

            // 目标目录：默认桌面；--shortcut-dir 指定时用指定目录（测试用，绝不碰真桌面）
            bool customDir = !string.IsNullOrWhiteSpace(opt.ShortcutDir);
            bool customName = !string.IsNullOrWhiteSpace(opt.ShortcutName);
            string dir;
            if (customDir)
            {
                try { dir = Path.GetFullPath(opt.ShortcutDir.Trim().Trim('"')); }
                catch (Exception e)
                {
                    Log.Line("      [警告] --shortcut-dir 路径无效: " + e.Message + "，改用桌面");
                    dir = DesktopDir();
                    customDir = false;
                }
            }
            else
            {
                dir = DesktopDir();
            }
            if (string.IsNullOrEmpty(dir))
            {
                Log.Line("      [警告] 拿不到桌面路径，跳过快捷方式");
                r.Warnings.Add("拿不到桌面路径，快捷方式未创建");
                return;
            }
            if (customDir)
            {
                try { Directory.CreateDirectory(dir); }
                catch (Exception e)
                {
                    r.Warnings.Add("快捷方式目录建不出来: " + e.Message);
                    Log.Line("      [警告] 快捷方式目录建不出来: " + e.Message);
                    return;
                }
            }

            string baseName = SanitizeShortcutName(opt.ShortcutName);
            bool reused;
            string note;
            string lnk = PickShortcutPath(dir, baseName, exePath, out reused, out note);
            if (customDir || customName)
                Log.Line("      快捷方式使用自定义位置（测试用，不影响桌面真名快捷方式）: " + lnk);
            if (!string.Equals(Path.GetFileNameWithoutExtension(lnk), baseName, StringComparison.OrdinalIgnoreCase))
            {
                r.Warnings.Add("快捷方式最终名为「" + Path.GetFileName(lnk) + "」（" + (note ?? "原名冲突") + "）");
                Log.Line("      最终快捷方式名: " + Path.GetFileName(lnk) + "  ← " + (note ?? "原名冲突"));
            }
            if (reused)
                Log.Line("      同名快捷方式已存在且指向本安装的 exe，直接更新它: " + lnk);
            else if (File.Exists(lnk))
            {
                string old = TryReadShortcutTarget(lnk);
                if (!string.IsNullOrEmpty(old)) Log.Line("      同名快捷方式已存在，当前指向: " + old);
            }

            // 方式一：IShellLink COM（不依赖 PowerShell，能指定任意路径）
            try
            {
                ShellLink.Create(lnk, exePath, gameDir, File.Exists(ico) ? ico : null, "泰拉瑞亚投影MOD（原版 1.4.5.8）");
                if (File.Exists(lnk))
                {
                    r.ShortcutPath = lnk;
                    Log.Line("      已创建快捷方式（COM）: " + lnk + " (" + new FileInfo(lnk).Length + " 字节) -> " + exePath);
                    return;
                }
                Log.Line("      COM 方式返回了但 .lnk 不存在，尝试 PowerShell 回退");
            }
            catch (Exception e)
            {
                Log.Line("      COM 方式失败: " + e.GetType().Name + ": " + e.Message);
            }

            // 方式二（默认位置）：直接用 release/shortcut.ps1（和 setup.cmd 同一条已验证路径）
            if (!customDir && !customName)
            {
                string tempDir1 = null;
                try
                {
                    tempDir1 = Path.Combine(Path.GetTempPath(), "ProjectionModSetup_" + Guid.NewGuid().ToString("N").Substring(0, 8));
                    Directory.CreateDirectory(tempDir1);
                    string script = Path.Combine(tempDir1, "shortcut.ps1");
                    ExtractResource("pl.shortcut.ps1", script);
                    Proc.Result pr = Proc.Run("powershell.exe",
                        "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\" \"" + gameDir + "\"",
                        tempDir1, 120000);
                    Log.Line("      shortcut.ps1 退出码: " + pr.ExitCode + (string.IsNullOrWhiteSpace(pr.StdOut) ? "" : "  输出: " + Flatten(pr.StdOut)));
                    if (File.Exists(lnk))
                    {
                        r.ShortcutPath = lnk;
                        Log.Line("      已创建快捷方式（PowerShell）: " + lnk + " (" + new FileInfo(lnk).Length + " 字节)");
                        return;
                    }
                    Log.Line("      [警告] PowerShell 回退也没生成 .lnk");
                }
                catch (Exception e)
                {
                    Log.Line("      PowerShell 回退失败: " + e.GetType().Name + ": " + e.Message);
                }
                finally
                {
                    TryDeleteDir(tempDir1);
                }
            }

            // 方式三：生成一个能指定任意路径的 PowerShell 脚本再试一次
            if (TryCreateShortcutWithPowerShell(lnk, exePath, gameDir, File.Exists(ico) ? ico : null))
            {
                r.ShortcutPath = lnk;
                Log.Line("      已创建快捷方式（PowerShell 自定义路径）: " + lnk + " (" + new FileInfo(lnk).Length + " 字节)");
                return;
            }

            r.Warnings.Add("桌面快捷方式创建失败（不影响游戏，可手动把 Terraria.exe 发到桌面）");
            Log.Line("      [警告] 快捷方式创建失败，安装其余步骤不受影响");
        }

        private static string DesktopDir()
        {
            string d = null;
            try { d = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); }
            catch { }
            if (string.IsNullOrEmpty(d))
            {
                try { d = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); }
                catch { }
            }
            return d;
        }

        private static string TryReadShortcutTarget(string lnk)
        {
            try { return ShellLink.ReadTarget(lnk); }
            catch (Exception e)
            {
                Log.Line("      读取已有快捷方式目标失败: " + e.GetType().Name + ": " + e.Message);
                return null;
            }
        }

        private static bool TryCreateShortcutWithPowerShell(string lnk, string exePath, string workDir, string icoPath)
        {
            string tempDir = null;
            try
            {
                tempDir = Path.Combine(Path.GetTempPath(), "ProjectionModSetup_" + Guid.NewGuid().ToString("N").Substring(0, 8));
                Directory.CreateDirectory(tempDir);
                string script = Path.Combine(tempDir, "mkshortcut.ps1");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("param([string]$LnkPath, [string]$Target, [string]$WorkDir, [string]$Icon, [string]$Desc)");
                sb.AppendLine("$ErrorActionPreference = 'Stop'");
                sb.AppendLine("$sh = New-Object -ComObject WScript.Shell");
                sb.AppendLine("$s = $sh.CreateShortcut($LnkPath)");
                sb.AppendLine("$s.TargetPath = $Target");
                sb.AppendLine("$s.WorkingDirectory = $WorkDir");
                sb.AppendLine("if ($Icon -and (Test-Path $Icon)) { $s.IconLocation = $Icon }");
                sb.AppendLine("$s.Description = $Desc");
                sb.AppendLine("$s.Save()");
                sb.AppendLine("if (-not (Test-Path $LnkPath)) { throw 'Save() 之后 .lnk 仍不存在' }");
                // Windows PowerShell 5.1 读无 BOM 的 UTF-8 脚本会乱码，这里写成带 BOM 的 UTF-8
                File.WriteAllText(script, sb.ToString(), new UTF8Encoding(true));

                Proc.Result pr = Proc.Run("powershell.exe",
                    "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\"" +
                    " -LnkPath \"" + lnk + "\" -Target \"" + exePath + "\" -WorkDir \"" + workDir + "\"" +
                    " -Icon \"" + (icoPath ?? "") + "\" -Desc \"泰拉瑞亚投影MOD（原版 1.4.5.8）\"",
                    tempDir, 120000);
                Log.Line("      mkshortcut.ps1 退出码: " + pr.ExitCode +
                        (string.IsNullOrWhiteSpace(pr.StdOut) ? "" : "  输出: " + Flatten(pr.StdOut)) +
                        (string.IsNullOrWhiteSpace(pr.StdErr) ? "" : "  错误: " + Flatten(pr.StdErr)));
                return File.Exists(lnk);
            }
            catch (Exception e)
            {
                Log.Line("      自定义路径 PowerShell 回退失败: " + e.GetType().Name + ": " + e.Message);
                return false;
            }
            finally
            {
                TryDeleteDir(tempDir);
            }
        }

        private static void TryDeleteDir(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return;
            try
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
            catch (Exception e)
            {
                Log.Line("      临时目录清理失败（可忽略）: " + e.Message);
            }
        }
    }
}
