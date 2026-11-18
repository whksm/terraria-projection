// SnapPlace.cs —— 投影吸附放置（常开，无开关）
// 点击放置时，把目标吸附到光标附近最近的“投影缺失格”（半径 3 格），
// 前提：手持物品正好是该格需要的方块/墙（避免干扰普通建造）。
// 放置距离仍由游戏原生规则（手长/装备加成）判定，不修改任何距离字段。
// 兼容 .NET Framework 4.0（C# 5 语法）。

using System;
using Terraria;
using Terraria.DataStructures;

namespace ProjectionMod
{
    public static class SnapPlace
    {
        /// <summary>吸附搜索半径（格）。只吸附近格，避免把原本想放别处的方块抢走。</summary>
        public const int Radius = 2;

        private static DateTime _logTime = DateTime.MinValue;

        /// <summary>放置前调用：把放置目标吸附到最近的缺失投影格。</summary>
        public static void Apply()
        {
            try
            {
                Blueprint bp = Renderer.Current;
                if (bp == null) return;
                Player pl = WorldUtil.LocalPlayer;
                if (pl == null) return;
                Item held = WorldUtil.HeldItem(pl);
                if (held == null) return;

                int hx, hy;
                WorldUtil.MouseTile(out hx, out hy);

                // 光标已经对准一个“手持材料可满足”的缺失格 → 无需吸附
                BpCell cur;
                if (IsMissing(bp, hx, hy, out cur) && WorldUtil.ItemMatches(held, cur)) return;

                // 光标所在格已有方块 → 不吸附（此时是在对已有结构操作，别抢目标）
                Tile cursorTile;
                if (WorldUtil.TryTile(hx, hy, out cursorTile) && cursorTile.active()) return;

                int bestX = -1, bestY = -1, bestD = int.MaxValue;
                for (int dy = -Radius; dy <= Radius; dy++)
                {
                    for (int dx = -Radius; dx <= Radius; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int x = hx + dx;
                        int y = hy + dy;
                        BpCell c;
                        if (!IsMissing(bp, x, y, out c)) continue;
                        if (!Renderer.PassesLayer(x - bp.OriginX, y - bp.OriginY)) continue;  // 只吸附当前可见层
                        if (!WorldUtil.ItemMatches(held, c)) continue;
                        int d = dx * dx + dy * dy;
                        if (d < bestD) { bestD = d; bestX = x; bestY = y; }
                    }
                }
                if (bestX < 0) return;

                // 手长判定：超出手长就不改目标，直接给提示（避免“点了没反应”）
                int boost = held.tileBoost + pl.blockRange;
                if (!pl.IsInTileInteractionRange(bestX, bestY, TileReachCheckSettings.Simple, boost))
                {
                    Renderer.SetHint("投影格超出放置距离（约 20 格上限），走近一点");
                    if ((DateTime.UtcNow - _logTime).TotalSeconds >= 1)
                    {
                        _logTime = DateTime.UtcNow;
                        Bootstrap.Log("吸附放置: 目标 (" + bestX + "," + bestY + ") 超出放置距离，未吸附");
                    }
                    return;
                }

                // PlaceThing 不会重算该字段，改它即可让本次放置落到投影格上
                Player.tileTargetX = bestX;
                Player.tileTargetY = bestY;

                if ((DateTime.UtcNow - _logTime).TotalSeconds >= 1)
                {
                    _logTime = DateTime.UtcNow;
                    Bootstrap.Log("吸附放置: 目标 (" + hx + "," + hy + ") -> (" + bestX + "," + bestY + ")");
                }
            }
            catch { /* 吸附失败不影响原放置流程 */ }
        }

        /// <summary>该格是否为“投影需要、但尚未放置”的缺失格。</summary>
        private static bool IsMissing(Blueprint bp, int x, int y, out BpCell cell)
        {
            cell = new BpCell();
            if (!WorldUtil.InWorld(x, y)) return false;
            if (!bp.TryGetWorld(x, y, out cell) || cell.IsEmpty) return false;
            Tile t = Main.tile[x, y];
            return (cell.type != 0 && !t.active()) || (cell.wall != 0 && t.wall == 0);
        }
    }
}
