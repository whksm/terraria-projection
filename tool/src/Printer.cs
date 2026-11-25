// Printer.cs —— 打印机（自动放置蓝图）
// NumPad7 开关，默认关闭；多人客户端自动禁用（自动化行为请遵守服务器规则）。
// 打印顺序：① 简单方块 → ② 电线（四色）→ ③ 促动器。
// 全程消耗真实背包材料：方块=对应物品、电线=「电线」530、促动器=「促动器」849。
// 受手长（含手持物加成）与分层（L）限制。
// 兼容 .NET Framework 4.0（C# 5 语法）。

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

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
        private static DateTime _missingLogTime = DateTime.MinValue;
        private static DateTime _wireMissingLogTime = DateTime.MinValue;

        // 收尾修复：SquareTileFrame 会框 3x3，后放的邻居会把先放的平台 frameX 重算掉。
        // 所有可放格都放完后，再统一按蓝图把 slope/halfBrick/frameX/frameY 写回一次。
        private static bool _shapeDirty;
        private static Blueprint _shapeBp;

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
                if (_shapeDirty && _shapeBp != bp)
                {
                    _shapeDirty = false;
                    _shapeBp = null;
                }
                Player pl = WorldUtil.LocalPlayer;
                if (pl == null || pl.dead) return;

                bool hasPlaceable;
                if (TryPlaceBlock(pl, bp, out hasPlaceable)) return;
                if (!hasPlaceable && _shapeDirty)
                {
                    RepairShapes(bp);
                    _shapeDirty = false;
                    _shapeBp = null;
                }
                if (PrintWiring) TryPlaceWire(pl, bp);
            }
            catch { /* 打印失败不影响游戏 */ }
        }

        // ---------------- ① 方块 ----------------
        private static bool TryPlaceBlock(Player pl, Blueprint bp, out bool hasPlaceable)
        {
            hasPlaceable = false;
            int ptx = (int)((pl.position.X + pl.width * 0.5f) / 16f);
            int pty = (int)((pl.position.Y + pl.height * 0.5f) / 16f);
            Rectangle body = new Rectangle((int)pl.position.X, (int)pl.position.Y, pl.width, pl.height);
            int reachBoost = pl.blockRange + HeldBoost(pl);

            // 背包只建一次 createTile -> slot 表；扫描循环里只查字典，不逐格 FindItem。
            Dictionary<int, int> tileSlots = BuildTileSlots(pl);

            int bestX = -1, bestY = -1, bestD = int.MaxValue;
            BpCell want = new BpCell();
            int anyX = -1, anyD = int.MaxValue;
            Dictionary<int, int> missing = new Dictionary<int, int>();

            foreach (BpEntry e in bp.Entries)
            {
                BpCell c = e.cell;
                if (c.type == 0) continue;                              // 墙暂不打印
                if (!Renderer.PassesLayer(e.bx, e.by)) continue;
                if (c.type >= Main.tileFrameImportant.Length) continue;
                bool platform = WorldUtil.IsPlatform(c.type);
                if (Main.tileFrameImportant[c.type] && !platform) continue; // 家具/火把交给手动；平台放行

                int x = bp.OriginX + e.bx;
                int y = bp.OriginY + e.by;
                Tile t;
                if (!WorldUtil.TryTile(x, y, out t)) continue;
                if (t.active()) continue;
                if (body.Intersects(new Rectangle(x * 16, y * 16, 16, 16))) continue;
                if (!pl.IsInTileInteractionRange(x, y, TileReachCheckSettings.Simple, reachBoost)) continue;

                int dx = x - ptx, dy = y - pty;
                int d = dx * dx + dy * dy;
                hasPlaceable = true;

                // 所有空格中最近的一格：只用于“完全没料”时提示
                if (d < anyD)
                {
                    anyD = d;
                    anyX = x;
                }

                int slot;
                if (tileSlots.TryGetValue(c.type, out slot))
                {
                    // 材料有的格子里最近的一格：真正要放的那个
                    if (d < bestD)
                    {
                        bestD = d;
                        bestX = x;
                        bestY = y;
                        want = c;
                    }
                }
                else
                {
                    int n;
                    missing.TryGetValue(c.type, out n);
                    missing[c.type] = n + 1;
                }
            }

            if (bestX < 0)
            {
                if (anyX >= 0)
                {
                    // 有空格但所有待放类型的材料都没有
                    string hintTypes = BuildMissingTypeList(missing, 5);
                    Renderer.SetHint("打印机：范围内缺少材料：" + hintTypes);
                    if (CanLogMissing())
                        Bootstrap.Log("打印机: 缺少材料，范围内无法放置: " + BuildMissingSummary(missing, 8));
                }
                else if (CanLog())
                {
                    Bootstrap.Log("打印机: 范围内没有可放的方块空位（占用/超距/分层过滤）");
                }
                return false;
            }

            int useSlot = tileSlots[want.type];
            Item it = pl.inventory[useSlot];
            int type = it.createTile;
            if (type <= 0 || it.stack <= 0) return false;
            if (type != want.type) return false;

            bool isPlatform = WorldUtil.IsPlatform((ushort)type);
            int style = isPlatform ? PlatformStyle(want) : 0;
            if (!WorldGen.PlaceTile(bestX, bestY, type, false, false, pl.whoAmI, style)) return false;

            ApplyCellShape(bestX, bestY, want, isPlatform, style);
            _shapeDirty = true;
            _shapeBp = bp;

            Consume(pl, useSlot);
            Sync(bestX, bestY);
            if (CanLog())
            {
                Item after = pl.inventory[useSlot];
                Bootstrap.Log("打印机: 方块 (" + bestX + "," + bestY + ") type=" + type +
                    " 剩余=" + (after != null && after.stack > 0 ? after.stack.ToString() : "0"));
                if (isPlatform)
                {
                    Bootstrap.Log("打印机: 平台 (" + bestX + "," + bestY + ") type=" + type +
                        " style=" + style +
                        " frameX=" + want.frameX +
                        " frameY=" + want.frameY +
                        " slope=" + want.slope +
                        " half=" + (want.halfBrick ? 1 : 0));
                }
            }
            return true;
        }

        /// <summary>背包 createTile → 第一个非空槽位；与旧 FindItem 的“按 type 精确匹配”语义一致。</summary>
        private static Dictionary<int, int> BuildTileSlots(Player pl)
        {
            Dictionary<int, int> map = new Dictionary<int, int>();
            if (pl.inventory == null) return map;
            int n = Math.Min(58, pl.inventory.Length);
            for (int i = 0; i < n; i++)
            {
                Item it = pl.inventory[i];
                if (it == null || it.stack <= 0 || it.createTile <= 0) continue;
                if (!map.ContainsKey(it.createTile)) map[it.createTile] = i;
            }
            return map;
        }

        /// <summary>缺料日志：按待放格数降序，最多 8 种。</summary>
        private static string BuildMissingSummary(Dictionary<int, int> missing, int maxTypes)
        {
            List<KeyValuePair<int, int>> list = new List<KeyValuePair<int, int>>(missing);
            list.Sort(delegate(KeyValuePair<int, int> a, KeyValuePair<int, int> b)
            {
                int cmp = b.Value.CompareTo(a.Value);
                if (cmp != 0) return cmp;
                return a.Key.CompareTo(b.Key);
            });

            StringBuilder sb = new StringBuilder();
            int count = 0;
            foreach (KeyValuePair<int, int> kv in list)
            {
                if (count >= maxTypes) break;
                if (count > 0) sb.Append(", ");
                sb.Append("tile=").Append(kv.Key).Append(" ×").Append(kv.Value);
                count++;
            }
            return sb.ToString();
        }

        /// <summary>提示里只列类型，最多 maxTypes 种。</summary>
        private static string BuildMissingTypeList(Dictionary<int, int> missing, int maxTypes)
        {
            List<KeyValuePair<int, int>> list = new List<KeyValuePair<int, int>>(missing);
            list.Sort(delegate(KeyValuePair<int, int> a, KeyValuePair<int, int> b)
            {
                int cmp = b.Value.CompareTo(a.Value);
                if (cmp != 0) return cmp;
                return a.Key.CompareTo(b.Key);
            });

            StringBuilder sb = new StringBuilder();
            int count = 0;
            foreach (KeyValuePair<int, int> kv in list)
            {
                if (count >= maxTypes) break;
                if (count > 0) sb.Append(", ");
                sb.Append(kv.Key);
                count++;
            }
            return sb.ToString();
        }

        /// <summary>平台款式：frameY / 18；旧蓝图没有帧时退回 0。</summary>
        private static int PlatformStyle(BpCell c)
        {
            if (!c.hasFrame || c.frameY < 0) return 0;
            int style = c.frameY / 18;
            return style < 0 ? 0 : style;
        }

        /// <summary>放完立即写 slope/halfBrick，重新框一次，再精确覆盖平台的 frameX/frameY。</summary>
        private static void ApplyCellShape(int x, int y, BpCell c, bool platform, int style)
        {
            Tile t;
            if (!WorldUtil.TryTile(x, y, out t) || t == null) return;

            byte sl = c.slope;
            if (sl > 4) sl = 0;
            bool half = c.halfBrick;
            // 平台可能合法地带 slope 3/4（block swap 保留），不能按普通块规则清零。
            if (!platform && half) sl = 0;
            t.slope(sl);
            t.halfBrick(half);

            // 先按当前邻居 + 当前 slope 框一次（普通块靠这一步更新纹理连接）
            WorldGen.SquareTileFrame(x, y, true);

            if (!platform) return;
            if (c.hasFrame && c.frameY >= 0)
            {
                t.frameY = c.frameY;             // 精确复制优先：不改写成 style*18
                if (c.frameX >= 0) t.frameX = c.frameX;
            }
            else
            {
                t.frameY = (short)(style * 18);
            }
        }

        /// <summary>
        /// 收尾修复：先把所有已放格的 slope/halfBrick 写回并按当前邻居框一次，
        /// 再统一覆盖平台的精确 frameX/frameY。第二遍之后绝不能再调 SquareTileFrame。
        /// </summary>
        private static void RepairShapes(Blueprint bp)
        {
            foreach (BpEntry e in bp.Entries)
            {
                if (e.cell.type == 0) continue;
                int x = bp.OriginX + e.bx;
                int y = bp.OriginY + e.by;
                Tile t;
                if (!WorldUtil.TryTile(x, y, out t) || t == null) continue;
                if (!t.active() || t.type != e.cell.type) continue;

                byte sl = e.cell.slope;
                if (sl > 4) sl = 0;
                bool half = e.cell.halfBrick;
                bool isPlatform = WorldUtil.IsPlatform(e.cell.type);
                if (!isPlatform && half) sl = 0;
                t.slope(sl);
                t.halfBrick(half);

                if (isPlatform)
                    WorldGen.SquareTileFrame(x, y, true);
            }

            foreach (BpEntry e in bp.Entries)
            {
                if (!WorldUtil.IsPlatform(e.cell.type)) continue;
                if (!e.cell.hasFrame || e.cell.frameY < 0) continue;
                int x = bp.OriginX + e.bx;
                int y = bp.OriginY + e.by;
                Tile t;
                if (!WorldUtil.TryTile(x, y, out t) || t == null) continue;
                if (!t.active() || t.type != e.cell.type) continue;

                t.frameY = e.cell.frameY;
                if (e.cell.frameX >= 0) t.frameX = e.cell.frameX;
            }
        }

        // ---------------- ② 电线 / ③ 促动器 ----------------
        private static void TryPlaceWire(Player pl, Blueprint bp)
        {
            int ptx = (int)((pl.position.X + pl.width * 0.5f) / 16f);
            int pty = (int)((pl.position.Y + pl.height * 0.5f) / 16f);
            int reachBoost = pl.blockRange + HeldBoost(pl);

            int wireStack = CountItemByType(pl, WireItem);
            int actStack = CountItemByType(pl, ActuatorItem);

            int bestWireX = -1, bestWireY = -1, bestWireD = int.MaxValue;
            byte bestWireBits = 0;
            int anyWireX = -1, anyWireD = int.MaxValue;
            int wireCells = 0;

            int bestActX = -1, bestActY = -1, bestActD = int.MaxValue;
            int anyActX = -1, anyActD = int.MaxValue;
            int actCells = 0;

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

                if (miss != 0)
                {
                    wireCells++;
                    if (d < anyWireD)
                    {
                        anyWireD = d;
                        anyWireX = x;
                    }
                    // 本格所有缺色都要有足够的电线（同一物品 530，放一段消耗 1 个）
                    if (wireStack >= CountBits(miss) && d < bestWireD)
                    {
                        bestWireD = d;
                        bestWireX = x;
                        bestWireY = y;
                        bestWireBits = miss;
                    }
                }

                if (missAct)
                {
                    actCells++;
                    if (d < anyActD)
                    {
                        anyActD = d;
                        anyActX = x;
                    }
                    if (actStack > 0 && d < bestActD)
                    {
                        bestActD = d;
                        bestActX = x;
                        bestActY = y;
                    }
                }
            }

            // 电线：优先最近的“材料够本格所有缺色”的格子
            if (bestWireX >= 0)
            {
                for (int bit = 0; bit < 4; bit++)
                {
                    if ((bestWireBits & (1 << bit)) == 0) continue;
                    if (wireStack <= 0) break;
                    int slot = FindItemByType(pl, WireItem);
                    if (slot < 0) break;

                    bool ok;
                    switch (bit)
                    {
                        case 0: ok = WorldGen.PlaceWire(bestWireX, bestWireY); break;
                        case 1: ok = WorldGen.PlaceWire2(bestWireX, bestWireY); break;
                        case 2: ok = WorldGen.PlaceWire3(bestWireX, bestWireY); break;
                        default: ok = WorldGen.PlaceWire4(bestWireX, bestWireY); break;
                    }
                    if (!ok) return;
                    Consume(pl, slot);
                    wireStack--;
                    Sync(bestWireX, bestWireY);
                    if (CanLogWire())
                        Bootstrap.Log("打印机: 电线 (" + bestWireX + "," + bestWireY + ") 颜色=" + WireName(bit));
                }
            }
            else if (anyWireX >= 0)
            {
                Renderer.SetHint("打印机：范围内缺少电线材料（" + wireCells + " 格）");
                if (CanLogWireMissing())
                    Bootstrap.Log("打印机: 缺少电线材料，范围内无法补线: " + wireCells + " 格");
            }

            // 促动器：独立按最近且 849 有货的格子
            if (bestActX >= 0)
            {
                int aslot = FindItemByType(pl, ActuatorItem);
                if (aslot < 0) return;
                if (!WorldGen.PlaceActuator(bestActX, bestActY)) return;
                Consume(pl, aslot);
                Sync(bestActX, bestActY);
                if (CanLogWire())
                    Bootstrap.Log("打印机: 促动器 (" + bestActX + "," + bestActY + ")");
            }
            else if (anyActX >= 0)
            {
                Renderer.SetHint("打印机：范围内缺少促动器（" + actCells + " 格）");
                if (CanLogWireMissing())
                    Bootstrap.Log("打印机: 缺少促动器，范围内无法放置: " + actCells + " 格");
            }
        }

        /// <summary>一个字节里 1 的个数（缺色数量）。</summary>
        private static int CountBits(byte b)
        {
            int n = 0;
            for (int i = 0; i < 8; i++)
                if ((b & (1 << i)) != 0) n++;
            return n;
        }

        // ---------------- 工具 ----------------
        private static int HeldBoost(Player pl)
        {
            Item h = WorldUtil.HeldItem(pl);
            return h != null ? h.tileBoost : 0;
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

        /// <summary>某 item 类型在所有背包槽里的总数量（电线/促动器可能分多堆）。</summary>
        private static int CountItemByType(Player pl, int type)
        {
            if (pl.inventory == null) return 0;
            int n = Math.Min(58, pl.inventory.Length);
            int total = 0;
            for (int i = 0; i < n; i++)
            {
                Item it = pl.inventory[i];
                if (it == null || it.stack <= 0) continue;
                if (it.type == type) total += it.stack;
            }
            return total;
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

        /// <summary>缺料聚合日志：5 秒最多一条。</summary>
        private static bool CanLogMissing()
        {
            if ((DateTime.UtcNow - _missingLogTime).TotalSeconds < 5) return false;
            _missingLogTime = DateTime.UtcNow;
            return true;
        }

        /// <summary>电线/促动器缺料聚合日志：5 秒最多一条。</summary>
        private static bool CanLogWireMissing()
        {
            if ((DateTime.UtcNow - _wireMissingLogTime).TotalSeconds < 5) return false;
            _wireMissingLogTime = DateTime.UtcNow;
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
