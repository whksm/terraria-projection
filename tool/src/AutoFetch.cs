// AutoFetch.cs —— 自动补料：把背包里匹配的材料换到当前选中槽
// 守卫：不打断物品使用、不碰“注定放不上”的格子（例如无支撑火把）。
// 兼容 .NET Framework 4.0（C# 5 语法）。

using System;
using Terraria;
using Terraria.DataStructures;

namespace ProjectionMod
{
    public static class AutoFetch
    {
        public static bool Enabled = true;

        private static int _cooldown;
        private static DateTime _logTime = DateTime.MinValue;

        public static void Tick()
        {
            if (!Enabled) return;
            if (_cooldown > 0) { _cooldown--; return; }
            _cooldown = 8; // 约 7 次/秒
            try { DoFetch(); }
            catch { /* 补料失败不影响游戏 */ }
        }

        private static void DoFetch()
        {
            if (Renderer.Current == null || Main.gameMenu) return;
            if (Main.playerInventory || Main.drawingPlayerChat) return;

            Player pl = WorldUtil.LocalPlayer;
            if (pl == null || pl.dead) return;
            // 正在使用/挥动物品时不换物品，避免打断或污染使用状态
            if (pl.itemAnimation > 0 || pl.controlUseItem) return;

            int hx, hy;
            WorldUtil.MouseTile(out hx, out hy);
            if (!WorldUtil.InWorld(hx, hy)) return;

            BpCell cell;
            if (!Renderer.Current.TryGetWorld(hx, hy, out cell) || cell.IsEmpty) return;

            Tile t;
            if (!WorldUtil.TryTile(hx, hy, out t)) return;
            bool missing = (cell.type != 0 && !t.active()) || (cell.wall != 0 && t.wall == 0);
            if (!missing) return;

            Item held = WorldUtil.HeldItem(pl);
            if (held == null) return;

            // 手持工具（扳手/剪线钳/油漆/促动器/镐子等不铺方块的物品）时绝不抢换：
            // 否则工具会被换成方块，导致电线/油漆这类“需要特殊道具”的操作用不了。
            if (held.createTile <= 0 && held.createWall <= 0)
            {
                LogToolSkip(held);
                return;
            }

            // 手长判定交给游戏原生规则（含装备加成）
            int boost = held.tileBoost + pl.blockRange;
            if (!pl.IsInTileInteractionRange(hx, hy, TileReachCheckSettings.Simple, boost))
            {
                Renderer.SetHint("超出放置距离：走近一点（装备可加手长）");
                return;
            }

            if (!WorldUtil.CanLikelyPlace(hx, hy, cell))
            {
                Renderer.SetHint("该格缺支撑：先放方块/背景墙，再放这里");
                return;
            }

            SwapIn(pl, cell);
        }

        private static int _lastToolType = -1;
        private static DateTime _toolLogTime = DateTime.MinValue;

        /// <summary>记录“因手持工具而跳过换料”，同一物品 10 秒最多一条。</summary>
        private static void LogToolSkip(Item held)
        {
            if (held.type == _lastToolType &&
                (DateTime.UtcNow - _toolLogTime).TotalSeconds < 10) return;
            _lastToolType = held.type;
            _toolLogTime = DateTime.UtcNow;
            Bootstrap.Log("自动补料: 手持工具(type=" + held.type +
                " createTile=" + held.createTile + ")，不抢换物品（电线/油漆等需要原工具）");
        }

        private static void SwapIn(Player pl, BpCell cell)
        {
            int sel = pl.selectedItem;
            if (sel < 0 || sel >= pl.inventory.Length) return;
            if (WorldUtil.ItemMatches(pl.inventory[sel], cell)) return; // 手里已是所需材料

            int n = Math.Min(58, pl.inventory.Length);
            for (int i = 0; i < n; i++)
            {
                if (i == sel) continue;
                Item it = pl.inventory[i];
                if (!WorldUtil.ItemMatches(it, cell)) continue;

                // 交换引用（Item 是引用类型，槽位互换等价于游戏内拖动物品）
                Item tmp = pl.inventory[sel];
                pl.inventory[sel] = pl.inventory[i];
                pl.inventory[i] = tmp;

                if ((DateTime.UtcNow - _logTime).TotalSeconds >= 1)
                {
                    _logTime = DateTime.UtcNow;
                    Bootstrap.Log("自动补料: 已把所需材料换到当前槽 (" + i + " -> " + sel + ")");
                }
                return;
            }

            Renderer.SetHint("背包里没有该材料");
        }
    }
}
