// BpPicker.cs —— 游戏内蓝图管理列表（分页 / 删除 / 改名 / 搜索）
// NumPad9 打开关闭；↑↓ 选择并即时预览；PgUp/PgDn 翻页；R 改名；Delete 删除（两次确认）；/ 搜索。
// 兼容 .NET Framework 4.0（C# 5 语法）。

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Input;

namespace ProjectionMod
{
    public static class BpPicker
    {
        public static bool Open;
        public static int Index;
        public static string Filter = "";
        public static int Page;
        public const int PageSize = 12;

        public static bool Editing;
        public static string EditText = "";

        private static readonly List<string> _all = new List<string>();
        public static readonly List<string> Files = new List<string>();

        private static bool _editForSearch;
        private static bool _confirmDelete;
        private static DateTime _confirmUntil = DateTime.MinValue;
        private static KeyboardState _prevKs;
        private static bool _hasPrev;

        // ===== 打开 / 关闭 =====
        public static void Toggle()
        {
            if (Open)
            {
                Open = false;
                Editing = false;
                Renderer.SetHint("蓝图列表已关闭");
                return;
            }

            Filter = "";
            Page = 0;
            Refresh();
            if (Files.Count == 0)
            {
                Renderer.SetHint("没有可用的 .bp 蓝图（游戏目录或 Blueprints 文件夹）");
                return;
            }

            Index = 0;
            string cur = BlueprintOps.CurrentPath;
            if (cur != null)
            {
                for (int i = 0; i < Files.Count; i++)
                    if (string.Compare(Files[i], cur, true) == 0) { Index = i; break; }
            }
            Open = true;
            Page = Index / PageSize;
            Renderer.SetHint("蓝图列表：↑↓ 选择 · PgUp/PgDn 翻页 · R 改名 · Delete 删除 · / 搜索");
        }

        // ===== 扫描文件（应用搜索过滤）=====
        public static void Refresh()
        {
            _all.Clear();
            Files.Clear();
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                string[] root = Directory.GetFiles(baseDir, "*.bp");
                string bdir = Path.Combine(baseDir, "Blueprints");
                string[] lib = Directory.Exists(bdir) ? Directory.GetFiles(bdir, "*.bp") : new string[0];
                Array.Sort(root);
                Array.Sort(lib);
                _all.AddRange(root);
                _all.AddRange(lib);
            }
            catch { }

            string f = (Filter ?? "").Trim();
            foreach (string p in _all)
            {
                if (f.Length == 0 || Path.GetFileName(p).IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0)
                    Files.Add(p);
            }
        }

        // ===== 选择 / 翻页 =====
        public static void Move(int delta)
        {
            if (!Open || Editing || Files.Count == 0) return;
            Index = (Index + delta + Files.Count) % Files.Count;
            Page = Index / PageSize;
            BlueprintOps.LoadFile(Files[Index]);
        }

        private static void PageMove(int delta)
        {
            if (!Open || Editing || Files.Count == 0) return;
            int pages = (Files.Count + PageSize - 1) / PageSize;
            Page = (Page + delta + pages) % pages;
            Index = Math.Min(Files.Count - 1, Page * PageSize);
            Renderer.SetHint("第 " + (Page + 1) + "/" + pages + " 页");
        }

        // ===== 每帧按键处理 =====
        public static void Tick()
        {
            if (!Open) return;

            KeyboardState ks = Keyboard.GetState();
            bool shift = ks.IsKeyDown(Keys.LeftShift) || ks.IsKeyDown(Keys.RightShift);

            if (Editing)
            {
                // 文本输入：逐键追加
                foreach (Keys k in ks.GetPressedKeys())
                {
                    if (_hasPrev && _prevKs.IsKeyDown(k)) continue;
                    char c = KeyToChar(k, shift);
                    if (c != '\0' && EditText.Length < 26) EditText += c;
                }
                if (Edge(ks, Keys.Back)) { if (EditText.Length > 0) EditText = EditText.Substring(0, EditText.Length - 1); }
                if (Edge(ks, Keys.Enter)) CommitEdit();
                if (Edge(ks, Keys.Escape)) { Editing = false; Renderer.SetHint("已取消输入"); }

                _prevKs = ks;
                _hasPrev = true;
                return;
            }

            if (Edge(ks, Keys.PageUp)) PageMove(1);
            if (Edge(ks, Keys.PageDown)) PageMove(-1);
            if (Edge(ks, Keys.Delete)) DeleteSelected();
            if (Edge(ks, Keys.R)) BeginEdit(false);
            if (Edge(ks, Keys.OemQuestion) || Edge(ks, Keys.Divide)) BeginEdit(true);

            _prevKs = ks;
            _hasPrev = true;
        }

        private static bool Edge(KeyboardState ks, Keys k)
        {
            return ks.IsKeyDown(k) && !(_hasPrev && _prevKs.IsKeyDown(k));
        }

        private static char KeyToChar(Keys k, bool shift)
        {
            if (k >= Keys.A && k <= Keys.Z)
            {
                char c = (char)('a' + (k - Keys.A));
                return shift ? char.ToUpper(c) : c;
            }
            if (k >= Keys.D0 && k <= Keys.D9) return (char)('0' + (k - Keys.D0));
            if (k >= Keys.NumPad0 && k <= Keys.NumPad9) return (char)('0' + (k - Keys.NumPad0));
            if (k == Keys.Space) return ' ';
            if (k == Keys.OemMinus) return shift ? '_' : '-';
            if (k == Keys.OemPeriod) return '.';
            return '\0';
        }

        // ===== 改名 =====
        private static void BeginEdit(bool forSearch)
        {
            if (Files.Count == 0 && !forSearch) return;
            Editing = true;
            _editForSearch = forSearch;
            EditText = forSearch ? Filter : Path.GetFileNameWithoutExtension(Files[Index]);
            Renderer.SetHint(forSearch ? "输入搜索词，回车确认，Esc 取消" : "输入新名字，回车确认，Esc 取消");
        }

        private static void CommitEdit()
        {
            Editing = false;
            if (_editForSearch)
            {
                Filter = EditText;
                Refresh();
                Page = 0;
                Index = 0;
                Renderer.SetHint(Filter.Length == 0 ? "已清除搜索" : ("搜索: " + Filter + "（" + Files.Count + " 个）"));
                return;
            }

            string name = Sanitize(EditText);
            if (name.Length == 0) { Renderer.SetHint("名字为空，已取消"); return; }
            if (Files.Count == 0) return;

            string oldPath = Files[Index];
            string dir = Path.GetDirectoryName(oldPath);
            string newPath = Path.Combine(dir, name + ".bp");
            if (string.Compare(oldPath, newPath, true) == 0) { Renderer.SetHint("名字未变"); return; }
            if (File.Exists(newPath))
            {
                Renderer.SetHint("已存在同名蓝图，未改名");
                return;
            }
            try
            {
                File.Move(oldPath, newPath);
                if (BlueprintOps.CurrentPath == oldPath) BlueprintOps.SetPath(newPath);
                Refresh();
                for (int i = 0; i < Files.Count; i++)
                    if (string.Compare(Files[i], newPath, true) == 0) { Index = i; break; }
                Page = Index / PageSize;
                Bootstrap.Log("蓝图改名: " + Path.GetFileName(oldPath) + " -> " + Path.GetFileName(newPath));
                Renderer.SetHint("已改名: " + Path.GetFileName(newPath));
            }
            catch (Exception e)
            {
                Bootstrap.Log("蓝图改名失败: " + e.Message);
                Renderer.SetHint("改名失败: " + e.Message);
            }
        }

        private static string Sanitize(string s)
        {
            if (s == null) return "";
            string bad = "\\/:*?\"<>|";
            string outp = "";
            foreach (char c in s.Trim())
            {
                if (bad.IndexOf(c) >= 0) continue;
                outp += c;
            }
            return outp.Trim();
        }

        // ===== 删除（两次确认）=====
        private static void DeleteSelected()
        {
            if (Files.Count == 0) return;
            string path = Files[Index];
            string name = Path.GetFileName(path);

            if (!_confirmDelete || DateTime.UtcNow > _confirmUntil)
            {
                _confirmDelete = true;
                _confirmUntil = DateTime.UtcNow.AddSeconds(4);
                Renderer.SetHint("再按一次 Delete 确认删除 " + name);
                return;
            }

            _confirmDelete = false;
            try
            {
                File.Delete(path);
                Bootstrap.Log("已删除蓝图: " + name);
                Refresh();
                if (Index >= Files.Count) Index = Math.Max(0, Files.Count - 1);
                Page = Files.Count == 0 ? 0 : Index / PageSize;
                Renderer.SetHint("已删除: " + name);
            }
            catch (Exception e)
            {
                Bootstrap.Log("删除蓝图失败: " + e.Message);
                Renderer.SetHint("删除失败: " + e.Message);
            }
        }

        // ===== 渲染文本行 =====
        public static string[] Render()
        {
            List<string> lines = new List<string>();
            int pages = Files.Count == 0 ? 1 : (Files.Count + PageSize - 1) / PageSize;
            string head = "蓝图列表 " + (Page + 1) + "/" + pages + " 共" + Files.Count + "个";
            if (Filter.Length > 0) head += " 搜索:" + Filter;
            lines.Add(head);
            lines.Add("↑↓ 选择 · PgUp/PgDn 翻页 · R 改名 · Del 删除 · / 搜索 · NumPad9 关闭");

            int start = Page * PageSize;
            int shown = Math.Min(PageSize, Math.Max(0, Files.Count - start));
            if (Files.Count == 0) lines.Add("（没有匹配的蓝图）");
            for (int i = 0; i < shown; i++)
            {
                int k = start + i;
                string mark = k == Index ? "▶ " : "  ";
                lines.Add(mark + Path.GetFileName(Files[k]));
            }

            if (Editing)
                lines.Add((_editForSearch ? "[搜索] " : "[改名] ") + EditText + "_");

            return lines.ToArray();
        }
    }
}
