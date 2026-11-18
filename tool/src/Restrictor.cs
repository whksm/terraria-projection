// Restrictor.cs —— 放置限制（蓝图建造模式，NumPad4 三档）
// 开启后：在蓝图区域内放置方块/墙时，只允许放蓝图相同或“同类组”物块；
// 严格档连空位也禁止。其余放置被静默拦截（不消耗物品、不写世界）。
// 纯客户端过滤，多人联机不发送任何额外数据。
// 兼容 .NET Framework 4.0（C# 5 语法）。

using System;
using System.Collections.Generic;
using Terraria;

namespace ProjectionMod
{
    public static class Restrictor
    {
        /// <summary>0 = 关闭；1 = 标准（空位放行）；2 = 严格（蓝图内空位也禁止放置）。</summary>
        public static int Mode = 0;

        private static Dictionary<ushort, string> _tileGroups = new Dictionary<ushort, string>();
        private static Dictionary<ushort, string> _wallGroups = new Dictionary<ushort, string>();
        private static bool _init;
        private static DateTime _lastLog = DateTime.MinValue;

        private static void EnsureInit()
        {
            if (_init) return;
            _init = true;
            // 同类组预设（ID 从游戏文件 TileID/WallID 常量提取，可继续扩展）
            Group("wood",
                new ushort[] { 30, 157, 159, 208, 253, 311, 321, 322, 635, 734, 741 },
                new ushort[] { 4, 27, 41, 43, 85, 115, 142, 143, 149, 151, 316, 351, 358 });
            Group("brick",
                new ushort[] { 38, 39, 45, 46, 47, 75, 76, 118, 121, 122, 140, 148, 151, 152,
                    175, 176, 177, 206, 226, 346, 347, 370, 472, 473, 478, 500, 501, 502, 503, 735, 737 },
                new ushort[] { 5, 6, 10, 11, 12, 20, 22, 25, 26, 31, 33, 34, 35, 44, 45, 46, 47,
                    84, 112, 173, 174, 177, 182, 231, 232, 234, 237, 238, 239, 240, 352, 355 });
        }

        private static void Group(string name, ushort[] tiles, ushort[] walls)
        {
            foreach (ushort t in tiles) _tileGroups[t] = name;
            foreach (ushort w in walls) _wallGroups[w] = name;
        }

        /// <summary>PlaceThing 前置钩子：返回 false = 取消本次放置。</summary>
        public static bool AllowPlace()
        {
            try
            {
                EnsureInit();
                if (Mode == 0 || Renderer.Current == null) return true;

                Player pl = WorldUtil.LocalPlayer;
                if (pl == null) return true;
                Item held = WorldUtil.HeldItem(pl);
                if (held == null || held.stack <= 0) return true;
                if (held.createTile <= 0 && held.createWall <= 0) return true; // 油漆/魔杖等不拦

                // 用实际放置目标（SnapPlace 可能已把目标吸附到投影格）
                int tx = Player.tileTargetX;
                int ty = Player.tileTargetY;
                if (!WorldUtil.InWorld(tx, ty)) return true;

                BpCell cell;
                if (!Renderer.Current.TryGetWorld(tx, ty, out cell)) return true; // 蓝图外不拦

                if (cell.IsEmpty)
                {
                    if (Mode < 2) return true; // 标准档放行
                    LogThrottled("蓝图限制: 严格模式下空位禁止放置");
                    return false;
                }

                bool ok = false;
                if (cell.type != 0 &&
                    Compat(_tileGroups, (ushort)held.createTile, cell.type)) ok = true;
                if (!ok && cell.wall != 0 &&
                    Compat(_wallGroups, (ushort)held.createWall, cell.wall)) ok = true;

                if (!ok)
                    LogThrottled("蓝图限制: 该格需要 方块" + cell.type + "/墙" + cell.wall +
                        "，手持 " + held.createTile + "/" + held.createWall + " 已拦截");
                return ok;
            }
            catch
            {
                return true; // 任何异常都放行，绝不影响正常游戏
            }
        }

        private static void LogThrottled(string msg)
        {
            if ((DateTime.UtcNow - _lastLog).TotalSeconds < 1) return;
            _lastLog = DateTime.UtcNow;
            Bootstrap.Log(msg);
        }

        private static bool Compat(Dictionary<ushort, string> g, ushort a, ushort b)
        {
            if (a == b) return true;
            string ga, gb;
            if (g.TryGetValue(a, out ga) && g.TryGetValue(b, out gb)) return ga == gb;
            return false;
        }
    }
}
