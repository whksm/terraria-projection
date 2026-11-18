// Printer.cs —— 打印机（自动放置蓝图）
// NumPad7 开关，默认关闭；多人客户端自动禁用（自动化行为请遵守服务器规则）。
// 打印顺序：① 简单方块 → ② 电线（四色）→ ③ 促动器。
// 全程消耗真实背包材料：方块=对应物品、电线=「电线」530、促动器=「促动器」849。
// 受手长（含手持物加成）与分层（L）限制。
// 兼容 .NET Framework 4.0（C# 5 语法）。

using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;

namespace ProjectionMod
{
    public static class Printer
    {
        public static bool Enabled;

        /// <summary>是否连电线/促动器一起打印（默认开）。</summary>
        public static bool PrintWiring = true;

        private const int WireItem = 530;      // 电线（四色共用）
        private const int ActuatorItem = 849;  // 促动器

        private static int _tick;
        private static DateTime _logTime = DateTime.MinValue;
        private static DateTime _wireLogTime = DateTime.MinValue;

        public static void Tick()
        {
            try
            {
                if (!Enabled || Main.gameMenu) return;

                if (Main.netMode == 1)
                {
                    Enabled = false;
                    Bootstrap.Log("打印机: 多人客户端默认禁用");
                    Renderer.SetHint("打印机在多人模式默认禁用");
                    return;
                }

                if (++_tick < 6) return;   // 约 10 次/秒
                _tick = 0;

                Blueprint bp = Renderer.Current;
                if (bp == null) return;
                Player pl = WorldUtil.LocalPlayer;
                if (pl == null || pl.dead) return;

                if (TryPlaceBlock(pl, bp)) return;
                if (PrintWiring) TryPlaceWire(pl, bp);
            }
            catch { /* 打印失败不影响游戏 */ }
        }

        // ---------------- ① 方块 ----------------
        private static bool TryPlaceBlock(Player pl, Blueprint bp)
        {
            int ptx = (int)((pl.position.X + pl.width * 0.5f) / 16f);
            int pty = (int)((pl.position.Y + pl.height * 0.5f) / 16f);
            Rectangle body = new Rectangle((int)pl.position.X, (int)pl.position.Y, pl.width, pl.height);
            int reachBoost = pl.blockRange + HeldBoost(pl);

            int bestX = -1, bestY = -1, bestD = int.MaxValue;
            BpCell want = new BpCell();
            foreach (BpEntry e in bp.Entries)
            {
                BpCell c = e.cell;
                if (c.type == 0) continue;                              // 墙暂不打印
                if (!Renderer.PassesLayer(e.bx, e.by)) continue;
                if (c.type >= Main.tileFrameImportant.Length) continue;
                if (Main.tileFrameImportant[c.type]) continue;           // 家具/火把交给手动

                int x = bp.OriginX + e.bx;
                int y = bp.OriginY + e.by;
                Tile t;
                if (!WorldUtil.TryTile(x, y, out t)) continue;
                if (t.active()) continue;
                if (body.Intersects(new Rectangle(x * 16, y * 16, 16, 16))) continue;
                if (!pl.IsInTileInteractionRange(x, y, TileReachCheckSettings.Simple, reachBoost)) continue;

                int dx = x - ptx, dy = y - pty;
                int d = dx * dx + dy * dy;
                if (d >= bestD) continue;
                bestD = d;
                bestX = x;
                bestY = y;
                want = c;
            }
            if (bestX < 0)
            {
                if (CanLog()) Bootstrap.Log("打印机: 范围内没有可放的方块空位（占用/超距/分层过滤）");
                return false;
            }

            int slot = FindItem(pl, want.type);
            if (slot < 0)
            {
                Renderer.SetHint("打印机：背包里没有该方块");
                if (CanLog()) Bootstrap.Log("打印机: 缺方块材料 tile=" + want.type + " 目标=(" + bestX + "," + bestY + ")");
                return false;
            }

            Item it = pl.inventory[slot];
            int type = it.createTile;
            if (type <= 0 || it.stack <= 0) return false;
            if (!WorldGen.PlaceTile(bestX, bestY, type, false, false, pl.whoAmI, 0)) return false;

            Consume(pl, slot);
            Sync(bestX, bestY);
            if (CanLog())
            {
                Item after = pl.inventory[slot];
                Bootstrap.Log("打印机: 方块 (" + bestX + "," + bestY + ") type=" + type +
                    " 剩余=" + (after != null && after.stack > 0 ? after.stack.ToString() : "0"));
            }
            return true;
        }

        // ---------------- ② 电线 / ③ 促动器 ----------------
        private static void TryPlaceWire(Player pl, Blueprint bp)
        {
            int ptx = (int)((pl.position.X + pl.width * 0.5f) / 16f);
            int pty = (int)((pl.position.Y + pl.height * 0.5f) / 16f);
            int reachBoost = pl.blockRange + HeldBoost(pl);

            int bestX = -1, bestY = -1, bestD = int.MaxValue;
            byte missingBits = 0;
            bool missingActuator = false;

            foreach (BpEntry e in bp.Entries)
            {
                BpCell c = e.cell;
                if (c.wire == 0 && !c.actuator) continue;
                if (!Renderer.PassesLayer(e.bx, e.by)) continue;

                int x = bp.OriginX + e.bx;
                int y = bp.OriginY + e.by;
                Tile t;
                if (!WorldUtil.TryTile(x, y, out t)) continue;

                byte have = WorldUtil.PackWires(t);
                byte miss = (byte)(c.wire & ~have);
                bool missAct = c.actuator && !t.actuator();
                if (miss == 0 && !missAct) continue;
                if (!pl.IsInTileInteractionRange(x, y, TileReachCheckSettings.Simple, reachBoost)) continue;

                int dx = x - ptx, dy = y - pty;
                int d = dx * dx + dy * dy;
                if (d >= bestD) continue;
                bestD = d;
                bestX = x;
                bestY = y;
                missingBits = miss;
                missingActuator = missAct;
            }
            if (bestX < 0) return;

            // 逐色补线：每放一段消耗 1 个「电线」
            for (int bit = 0; bit < 4; bit++)
            {
                if ((missingBits & (1 << bit)) == 0) continue;
                int slot = FindItemByType(pl, WireItem);
                if (slot < 0)
                {
                    Renderer.SetHint("打印机：背包里没有电线");
                    if (CanLogWire())
                        Bootstrap.Log("打印机: 缺电线材料 (item=530) 目标=(" + bestX + "," + bestY + ")");
                    return;
                }
                bool ok;
                switch (bit)
                {
                    case 0: ok = WorldGen.PlaceWire(bestX, bestY); break;
                    case 1: ok = WorldGen.PlaceWire2(bestX, bestY); break;
                    case 2: ok = WorldGen.PlaceWire3(bestX, bestY); break;
                    default: ok = WorldGen.PlaceWire4(bestX, bestY); break;
                }
                if (!ok) return;
                Consume(pl, slot);
                Sync(bestX, bestY);
                if (CanLogWire())
                    Bootstrap.Log("打印机: 电线 (" + bestX + "," + bestY + ") 颜色=" + WireName(bit));
            }

            if (!missingActuator) return;

            int aslot = FindItemByType(pl, ActuatorItem);
            if (aslot < 0)
            {
                Renderer.SetHint("打印机：背包里没有促动器");
                if (CanLogWire())
                    Bootstrap.Log("打印机: 缺促动器材料 (item=849) 目标=(" + bestX + "," + bestY + ")");
                return;
            }
            if (!WorldGen.PlaceActuator(bestX, bestY)) return;
            Consume(pl, aslot);
            Sync(bestX, bestY);
            if (CanLogWire())
                Bootstrap.Log("打印机: 促动器 (" + bestX + "," + bestY + ")");
        }

        // ---------------- 工具 ----------------
        private static int HeldBoost(Player pl)
        {
            Item h = WorldUtil.HeldItem(pl);
            return h != null ? h.tileBoost : 0;
        }

        private static int FindItem(Player pl, int tileType)
        {
            if (pl.inventory == null) return -1;
            int n = Math.Min(58, pl.inventory.Length);
            for (int i = 0; i < n; i++)
            {
                Item it = pl.inventory[i];
                if (it == null || it.stack <= 0) continue;
                if (it.createTile == tileType) return i;
            }
            return -1;
        }

        private static int FindItemByType(Player pl, int type)
        {
            if (pl.inventory == null) return -1;
            int n = Math.Min(58, pl.inventory.Length);
            for (int i = 0; i < n; i++)
            {
                Item it = pl.inventory[i];
                if (it == null || it.stack <= 0) continue;
                if (it.type == type) return i;
            }
            return -1;
        }

        private static void Consume(Player pl, int slot)
        {
            Item it = pl.inventory[slot];
            if (it == null) return;
            it.stack--;
            if (it.stack <= 0) it.SetDefaults(0);
        }

        private static void Sync(int x, int y)
        {
            if (Main.netMode != 1) return;
            try { NetMessage.SendTileSquare(-1, x, y, Terraria.ID.TileChangeType.None); } catch { }
        }

        /// <summary>方块日志限频：2 秒一条。</summary>
        private static bool CanLog()
        {
            if ((DateTime.UtcNow - _logTime).TotalSeconds < 2) return false;
            _logTime = DateTime.UtcNow;
            return true;
        }

        /// <summary>电线日志限频：3 秒一条（与方块日志各自独立）。</summary>
        private static bool CanLogWire()
        {
            if ((DateTime.UtcNow - _wireLogTime).TotalSeconds < 3) return false;
            _wireLogTime = DateTime.UtcNow;
            return true;
        }

        private static string WireName(int bit)
        {
            switch (bit)
            {
                case 0: return "红";
                case 1: return "蓝";
                case 2: return "绿";
                default: return "黄";
            }
        }
    }
}
