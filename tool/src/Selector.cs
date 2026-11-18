// Selector.cs —— 蓝图选取工具（木剑 + 右键，参考投影模组的“小木棒”）
// NumPad7 开关。开启后手持木剑(ItemID 24)：右键点第一角 A，再右键点对角 B，
// 把 A-B 矩形区域抓成新蓝图（含贴图帧），保存到 Blueprints\selection_*.bp 并加载。
// 兼容 .NET Framework 4.0（C# 5 语法）。

using System;
using System.IO;
using Terraria;
using Terraria.ID;

namespace ProjectionMod
{
    public static class Selector
    {
        // 无总开关：手持木剑右键即选取（木剑是少见道具，误触概率很低）
        public static bool HasA = false;
        public static int ax, ay, bx, by;
        public static string Info = "";

        private static bool _prevRight;

        public static void Tick()
        {
            if (Main.gameMenu) return;
            bool right = Main.mouseRight;
            bool clicked = right && !_prevRight;   // 先取状态，保证任何提前 return 都不丢边沿
            _prevRight = right;
            if (!clicked) return;

            Player pl = WorldUtil.LocalPlayer;
            if (pl == null || pl.dead) return;
            if (Main.playerInventory || Main.drawingPlayerChat) return;

            // 选取工具 = 木剑（其他物品不响应右键，避免误触）
            Item held = WorldUtil.HeldItem(pl);
            if (held == null || held.type != ItemID.WoodenSword) return;

            int tx, ty;
            WorldUtil.MouseTile(out tx, out ty);
            if (!WorldUtil.InWorld(tx, ty)) return;

            if (!HasA)
            {
                ax = tx;
                ay = ty;
                HasA = true;
                Info = string.Format("选取: A({0},{1}) 已标记，右键选对角 B", ax, ay);
                Bootstrap.Log(Info);
                Renderer.SetHint(Info);
                return;
            }

            bx = tx;
            by = ty;
            Complete();
        }

        private static void Complete()
        {
            int x0 = Math.Min(ax, bx);
            int y0 = Math.Min(ay, by);
            int w = Math.Abs(ax - bx) + 1;
            int h = Math.Abs(ay - by) + 1;
            if (w > 300 || h > 300)
            {
                Cancel();
                Bootstrap.Log("选取: 区域过大（>300 格），已取消");
                Renderer.SetHint("选取区域过大（>300 格），已取消");
                return;
            }

            Blueprint bp = new Blueprint();
            bp.OriginX = x0;
            bp.OriginY = y0;
            int n = 0;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Tile t;
                    if (!WorldUtil.TryTile(x0 + x, y0 + y, out t)) continue;
                    if (!WorldUtil.HasContent(t)) continue;
                    bp.Add(x, y, WorldUtil.CellFromTile(t));
                    n++;
                }
            }
            if (n == 0)
            {
                Cancel();
                Bootstrap.Log("选取: 区域内没有建筑，已取消");
                Renderer.SetHint("选取区域里没有建筑");
                return;
            }

            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Blueprints");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            // 固定文件名：每次选取覆盖上一张，避免堆积大量无用蓝图
            string path = Path.Combine(dir, "selection.bp");
            bp.Save(path);

            Renderer.Current = bp;
            BlueprintOps.SetPath(path);
            Renderer.Calibrate(bp);

            float dist = (float)Math.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));
            Info = string.Format("选取完成: 尺寸 {0}x{1} | 距离 {2:F1} 格 | {3} 格方块", w, h, dist, n);
            Bootstrap.Log("已保存选取蓝图: " + path + " | " + Info);
            Renderer.SetHint("已抓取蓝图：" + Path.GetFileName(path));
            HasA = false;
        }

        /// <summary>关闭选取模式或取消当前选点。</summary>
        public static void Cancel()
        {
            HasA = false;
            Info = "";
        }
    }
}
