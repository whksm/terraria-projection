using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ProjectionSetup
{
    /// <summary>一个 exe 的版本 / 补丁状态（只读探测，不改任何文件）。</summary>
    internal sealed class ExeProbe
    {
        public string Path;
        public long Size;
        /// <summary>规范化版本，如 1.4.6.1；读不出为 null。</summary>
        public string VersionTag;
        /// <summary>文件里是否存在 ProjectionMod 标记（补丁注入的 AssemblyRef / 类型名）。</summary>
        public bool Patched;
        /// <summary>文件是否读完过（读不了时 Patched 不可信）。</summary>
        public bool Readable;
    }

    /// <summary>目录里的一份备份文件。</summary>
    internal sealed class BackupInfo
    {
        public string Path;
        public string FileName;
        public long Size;
        /// <summary>文件名里的版本号（规范化后）；不符合命名规则时为 null。</summary>
        public string VersionTag;
        /// <summary>备份内部是否已含补丁标记（true = 这份备份不能当原版用）。</summary>
        public bool Patched;
        /// <summary>是否符合 Terraria_backup_&lt;版本&gt;_vanilla.exe 命名。</summary>
        public bool Versioned;
    }

    /// <summary>
    /// 备份策略（安装器与 setup.cmd / 卸载.bat 一致，改动请三处同步）：
    ///
    ///   * 备份名带版本：Terraria_backup_&lt;版本&gt;_vanilla.exe，版本取 exe 的 FileVersion；
    ///   * 只备份「未打补丁」的 exe —— 已经打过补丁的 exe 绝不能当原版备份；
    ///   * 版本变了就必须重新备份（不是「已存在就跳过」），旧版本备份原样留着；
    ///   * 还原只认与当前 exe 版本相同的 vanilla 备份，找不到就报错并让用户去
    ///     Steam 校验完整性，绝不默默盖一个旧版本上去（1.4.5.8 的 exe 配 1.4.6.1 的
    ///     Content 必崩）。
    /// </summary>
    internal static class BackupStore
    {
        public const string Prefix = "Terraria_backup_";
        public const string Suffix = "_vanilla.exe";
        public const string Marker = "ProjectionMod";

        // ------------------------------------------------------------------
        //  版本号
        // ------------------------------------------------------------------

        /// <summary>
        /// 规范化版本号，和 setup.cmd / 卸载.bat 里的 PowerShell 逻辑一致：
        /// 取开头连续的数字与点；超过 3 段时去掉结尾的 ".0"（1.4.6.1.0 → 1.4.6.1）。
        /// 读不出返回 null。
        /// </summary>
        public static string NormalizeVersion(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            int i = 0;
            while (i < raw.Length && !IsDigit(raw[i])) i++;
            int j = i;
            while (j < raw.Length && (IsDigit(raw[j]) || raw[j] == '.')) j++;
            string core = raw.Substring(i, j - i).Trim('.');
            if (core.Length == 0) return null;

            List<string> parts = new List<string>(core.Split('.'));
            while (parts.Count > 3 && parts[parts.Count - 1] == "0") parts.RemoveAt(parts.Count - 1);
            return string.Join(".", parts.ToArray());
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        /// <summary>读 exe 的 FileVersion（回退 ProductVersion），返回规范化版本。</summary>
        public static string ReadVersionTag(string path)
        {
            try
            {
                FileVersionInfo vi = FileVersionInfo.GetVersionInfo(path);
                string tag = NormalizeVersion(vi.FileVersion);
                if (tag == null) tag = NormalizeVersion(vi.ProductVersion);
                return tag;
            }
            catch
            {
                return null;
            }
        }

        // ------------------------------------------------------------------
        //  补丁标记
        // ------------------------------------------------------------------

        /// <summary>
        /// 统计文件里 ASCII "ProjectionMod" 出现次数（注入会写入 AssemblyRef/类型名）。
        /// 原版 0 次，打过补丁 ≥1 次。
        /// </summary>
        public static int CountMarker(string path, out int firstOffset, out bool readable)
        {
            firstOffset = -1;
            readable = false;
            try
            {
                byte[] data = File.ReadAllBytes(path);
                readable = true;
                byte[] needle = Encoding.ASCII.GetBytes(Marker);
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

        public static int CountMarker(string path, out int firstOffset)
        {
            bool readable;
            return CountMarker(path, out firstOffset, out readable);
        }

        /// <summary>探测一个 exe：大小、版本、是否已打补丁。</summary>
        public static ExeProbe Probe(string path)
        {
            ExeProbe p = new ExeProbe();
            p.Path = path;
            try { p.Size = new FileInfo(path).Length; }
            catch { p.Size = 0; }
            p.VersionTag = ReadVersionTag(path);
            int off;
            bool readable;
            int n = CountMarker(path, out off, out readable);
            p.Readable = readable;
            p.Patched = n > 0;
            return p;
        }

        // ------------------------------------------------------------------
        //  备份文件
        // ------------------------------------------------------------------

        public static string BackupFileName(string versionTag)
        {
            return Prefix + versionTag + Suffix;
        }

        public static string BackupPath(string gameDir, string versionTag)
        {
            return Path.Combine(gameDir, BackupFileName(versionTag));
        }

        /// <summary>从 Terraria_backup_&lt;版本&gt;_vanilla.exe 里取出并规范化版本号。</summary>
        private static string TagFromFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            string low = fileName.ToLowerInvariant();
            if (!low.StartsWith(Prefix.ToLowerInvariant())) return null;
            if (!low.EndsWith(Suffix.ToLowerInvariant())) return null;
            int start = Prefix.Length;
            int len = fileName.Length - Prefix.Length - Suffix.Length;
            if (len <= 0) return null;
            return NormalizeVersion(fileName.Substring(start, len));
        }

        /// <summary>列目录里所有 *backup*.exe（含不符合新命名的老备份），只读。</summary>
        public static List<BackupInfo> ListBackups(string gameDir)
        {
            List<BackupInfo> list = new List<BackupInfo>();
            string[] files;
            try { files = Directory.GetFiles(gameDir, "Terraria*backup*.exe", SearchOption.TopDirectoryOnly); }
            catch (Exception e)
            {
                Log.Line("      枚举备份文件失败: " + e.Message);
                return list;
            }
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            foreach (string f in files)
            {
                BackupInfo b = Describe(f);
                if (b != null) list.Add(b);
            }
            return list;
        }

        private static BackupInfo Describe(string path)
        {
            BackupInfo b = new BackupInfo();
            b.Path = path;
            b.FileName = Path.GetFileName(path);
            b.VersionTag = TagFromFileName(b.FileName);
            b.Versioned = b.VersionTag != null;
            try { b.Size = new FileInfo(path).Length; }
            catch { b.Size = 0; }
            int off;
            bool readable;
            b.Patched = CountMarker(path, out off, out readable) > 0;
            return b;
        }

        /// <summary>
        /// 找与版本相同的 versioned 备份（名字匹配即返回，是否干净看 Patched）。
        /// 找不到返回 null。
        /// </summary>
        public static BackupInfo FindMatchingBackup(string gameDir, string versionTag)
        {
            if (string.IsNullOrEmpty(versionTag)) return null;
            string[] files;
            try { files = Directory.GetFiles(gameDir, Prefix + "*" + Suffix, SearchOption.TopDirectoryOnly); }
            catch (Exception e)
            {
                Log.Line("      枚举同版本备份失败: " + e.Message);
                return null;
            }
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            BackupInfo patchedMatch = null;
            foreach (string f in files)
            {
                string tag = TagFromFileName(Path.GetFileName(f));
                if (tag == null) continue;
                if (!string.Equals(tag, versionTag, StringComparison.OrdinalIgnoreCase)) continue;
                BackupInfo b = Describe(f);
                if (!b.Patched) return b;      // 优先用干净的那份
                if (patchedMatch == null) patchedMatch = b;
            }
            return patchedMatch;               // 只有坏的也返回，好让界面说清「这份本身是打过补丁的」
        }

        // ------------------------------------------------------------------
        //  人话化的提示
        // ------------------------------------------------------------------

        /// <summary>Steam 校验完整性的补救步骤（安装器/还原失败时共用）。</summary>
        public static string[] SteamVerifyHints(string versionTag)
        {
            string want = string.IsNullOrEmpty(versionTag) ? "Terraria_backup_<版本>_vanilla.exe" : BackupFileName(versionTag);
            return new string[]
            {
                "补救办法（会让补丁失效，之后需要重装一次 mod）：",
                "  1. 关掉游戏，Steam 库里右键泰拉瑞亚 - 属性 - 已安装文件 - 验证游戏文件完整性",
                "  2. Steam 会把 Terraria.exe 还原成当前版本的原版 exe（补丁同时被清掉）",
                "  3. 重新运行一次安装器 / setup.cmd，这次会生成 " + want + " 这份干净备份",
                "  4. 装完再进游戏",
            };
        }

        /// <summary>把备份清单格式化成几行日志，方便用户看清「哪份是哪版、哪份是打过补丁的」。</summary>
        public static void LogBackupList(string gameDir)
        {
            List<BackupInfo> list = ListBackups(gameDir);
            if (list.Count == 0)
            {
                Log.Line("        目录里没有任何 *backup*.exe");
                return;
            }
            foreach (BackupInfo b in list)
            {
                string ver = b.VersionTag ?? "名字里没有版本";
                string state = b.Patched ? "已打补丁（不能当原版）" : "原版";
                string kind = b.Versioned ? "" : "（老命名）";
                Log.Line("        " + b.FileName + "   版本 " + ver + "   " + b.Size + " 字节   " + state + kind);
            }
        }
    }
}
