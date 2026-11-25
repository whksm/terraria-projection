// Printer.cs —— 打印机（自动放置蓝图）
// NumPad7 开关，默认关闭；多人客户端自动禁用（自动化行为请遵守服务器规则）。
// 打印顺序：① 简单方块 → ② 背景墙 → ③ 物件（frame-important）→ ④ 电线（四色）→ ⑤ 促动器。
// 全程消耗真实背包材料：方块=createTile 对应物品、墙=createWall 对应物品、
// 物件=createTile 对应物品（placeStyle 优先匹配）、电线=「电线」530、促动器=「促动器」849。
// 受手长（含手持物加成）与分层（L）限制。
// 兼容 .NET Framework 4.0（C# 5 语法）。

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ObjectData;

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
        private static DateTime _wallLogTime = DateTime.MinValue;
        private static DateTime _wallMissingLogTime = DateTime.MinValue;
        private static DateTime _objLogTime = DateTime.MinValue;
        private static DateTime _objMissingLogTime = DateTime.MinValue;
        private static DateTime _nonConsumeLogTime = DateTime.MinValue;   // 用非消耗物品（魔棒）放置的日志限频

        // 物件放置失败黑名单：同一物件 5 秒内不重试（避免每 tick 撞同一面缺支撑的墙）
        private static Dictionary<long, DateTime> _objFail = new Dictionary<long, DateTime>();
        private static Blueprint _objFailBp;

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
                if (_objFailBp != bp)
                {
                    _objFail.Clear();
                    _objFailBp = bp;
                }
                Player pl = WorldUtil.LocalPlayer;
                if (pl == null || pl.dead) return;

                bool hasPlaceable;
                if (TryPlaceBlock(pl, bp, out hasPlaceable)) return;
                bool hasObject;
                // ② 墙：方块放完才轮到墙（挂墙物件要靠背景墙）
                if (TryPlaceWall(pl, bp)) return;
                // ③ 物件：方块和墙都补齐才放（物件的锚点判的就是实体块/墙）
                if (TryPlaceObject(pl, bp, out hasObject)) return;
                if (!hasPlaceable && !hasObject && _shapeDirty)
                {
                    RepairShapes(bp);
                    _shapeDirty = false;
                    _shapeBp = null;
                }
                // ④ 电线 / ⑤ 促动器：放在物件之后，避免重框覆盖刚放好的物件帧
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
                        Bootstrap.Log("打印机: 缺少材料，范围内无法放置: " + BuildMissingSummary(missing, 8, "tile="));
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

            Consume(pl, useSlot, type, "");           // 法杖类（蜂巢魔棒等）不扣数量；普通方块照常 -1
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

        // ---------------- ② 墙 ----------------
        /// <summary>
        /// 每 tick 最多补一面背景墙。选格与方块同一套：蓝图有墙、世界无墙、在手长内、过 L 分层；
        /// 纯墙格（type=0）也算目标，不再被跳过。背包建 createWall → slot 表，
        /// 同时维护“材料有的最近格”和“所有最近格”，缺料按墙类型聚合（5 秒限频）。
        /// </summary>
        private static bool TryPlaceWall(Player pl, Blueprint bp)
        {
            int ptx = (int)((pl.position.X + pl.width * 0.5f) / 16f);
            int pty = (int)((pl.position.Y + pl.height * 0.5f) / 16f);
            int reachBoost = pl.blockRange + HeldBoost(pl);

            Dictionary<int, int> wallSlots = BuildWallSlots(pl);

            int bestX = -1, bestY = -1, bestD = int.MaxValue;
            int wantWall = 0;
            int anyX = -1, anyD = int.MaxValue;
            int pending = 0;                       // 蓝图范围内还缺墙的格数（只用于“够不着”提示，避免刷屏）
            Dictionary<int, int> missing = new Dictionary<int, int>();

            foreach (BpEntry e in bp.Entries)
            {
                BpCell c = e.cell;
                if (c.wall == 0) continue;                          // 该格蓝图没有墙
                if (!Renderer.PassesLayer(e.bx, e.by)) continue;

                int x = bp.OriginX + e.bx;
                int y = bp.OriginY + e.by;
                Tile t;
                if (!WorldUtil.TryTile(x, y, out t)) continue;
                if (t.wall != 0) continue;                          // 已有墙就不覆盖
                pending++;
                if (!pl.IsInTileInteractionRange(x, y, TileReachCheckSettings.Simple, reachBoost)) continue;

                int dx = x - ptx, dy = y - pty;
                int d = dx * dx + dy * dy;

                // 所有缺墙格中最近的一格：只用于“完全没料”提示
                if (d < anyD)
                {
                    anyD = d;
                    anyX = x;
                }

                int slot;
                if (wallSlots.TryGetValue(c.wall, out slot))
                {
                    // 材料有的格子里最近的一格：真正要放的那个
                    if (d < bestD)
                    {
                        bestD = d;
                        bestX = x;
                        bestY = y;
                        wantWall = c.wall;
                    }
                }
                else
                {
                    int n;
                    missing.TryGetValue(c.wall, out n);
                    missing[c.wall] = n + 1;
                }
            }

            if (bestX < 0)
            {
                if (anyX >= 0)
                {
                    // 有缺墙格但所有待放墙类型的材料都没有
                    Renderer.SetHint("打印机：范围内缺少墙材料：" + BuildMissingTypeList(missing, 5));
                    if (CanLogWallMissing())
                        Bootstrap.Log("打印机: 缺少墙材料，范围内无法放置: " +
                            BuildMissingSummary(missing, 8, "wall="));
                }
                else if (pending > 0 && CanLogWallMissing())
                {
                    Bootstrap.Log("打印机: 墙未放置 " + pending + " 面（不在手长/交互范围内）");
                }
                // pending == 0：蓝图墙已补齐（或蓝图里根本没墙）→ 不打扰日志
                return false;
            }

            int useSlot = wallSlots[wantWall];
            Item it = pl.inventory[useSlot];
            int wall = it.createWall;
            if (wall <= 0 || it.stack <= 0) return false;
            if (wall != wantWall) return false;

            // 1.4.5.8 实测签名：WorldGen.PlaceWall(int i, int j, int type, bool mute = false) 返回 void。
            // 它只在 t.wall == 0 时写入并自动 SquareWallFrame，不扣物品、不判手长 ——
            // 所以成功与否只能读回世界确认，材料由这里扣。
            WorldGen.PlaceWall(bestX, bestY, wall, true);

            Tile placed;
            if (!WorldUtil.TryTile(bestX, bestY, out placed) || placed == null || placed.wall != wantWall)
                return false;                                       // 放失败（边界/被占用）：不扣材料

            Consume(pl, useSlot, wall, "墙");          // 墙纸/墙类物品都是消耗品，正常 -1
            Sync(bestX, bestY);
            if (CanLogWall())
            {
                Item after = pl.inventory[useSlot];
                Bootstrap.Log("打印机: 墙 (" + bestX + "," + bestY + ") wall=" + wall +
                    " 剩余=" + (after != null && after.stack > 0 ? after.stack.ToString() : "0"));
            }
            return true;
        }

        /// <summary>背包 createWall → 第一个非空槽位；语义同 BuildTileSlots。</summary>
        private static Dictionary<int, int> BuildWallSlots(Player pl)
        {
            Dictionary<int, int> map = new Dictionary<int, int>();
            if (pl.inventory == null) return map;
            int n = Math.Min(58, pl.inventory.Length);
            for (int i = 0; i < n; i++)
            {
                Item it = pl.inventory[i];
                if (it == null || it.stack <= 0 || it.createWall <= 0) continue;
                if (!map.ContainsKey(it.createWall)) map[it.createWall] = i;
            }
            return map;
        }

        // ---------------- ③ 物件（frame-important：火把/门/箱子/压力板/计时器/传送机…） ----------------
        // 这些类型不能走 PlaceTile（PlaceTile 只处理简单方块），必须 WorldGen.PlaceObject。
        // 关键点（反编译见 notes/printer-objects.md）：
        //   · PlaceObject 的 x,y 是“放置基准格”= 左上角 + TileObjectData.Origin，不是左上角；
        //   · PlaceObject 返回值不可信（Place 失败也返回 true）→ 必须读回世界确认；
        //   · 它不调 HookPostPlaceMyPlayer → 箱子/人偶等要自己补一次 CallPostPlacementPlayerHook。
        private static bool TryPlaceObject(Player pl, Blueprint bp, out bool hasPlaceable)
        {
            hasPlaceable = false;
            int ptx = (int)((pl.position.X + pl.width * 0.5f) / 16f);
            int pty = (int)((pl.position.Y + pl.height * 0.5f) / 16f);
            Rectangle body = new Rectangle((int)pl.position.X, (int)pl.position.Y, pl.width, pl.height);
            int reachBoost = pl.blockRange + HeldBoost(pl);

            Dictionary<long, int> objSlots = BuildObjectSlots(pl);

            int bestX = -1, bestY = -1, bestD = int.MaxValue;
            int bestType = 0, bestStyle = 0, bestW = 1, bestH = 1, bestOffX = 0, bestOffY = 0, bestSlot = -1;
            int anyX = -1, anyD = int.MaxValue;
            int blocked = 0;
            Dictionary<int, int> missing = new Dictionary<int, int>();
            HashSet<long> seen = new HashSet<long>();

            foreach (BpEntry e in bp.Entries)
            {
                BpCell c = e.cell;
                if (!IsObjectType(c.type)) continue;
                if (!Renderer.PassesLayer(e.bx, e.by)) continue;

                int ox, oy, w, h, style;
                if (!TryObjectLayout(c, e.bx, e.by, out ox, out oy, out w, out h, out style))
                {
                    // 没有 TileObjectData（本来就不该出现）：只记一条，别每 tick 刷
                    if (CanLogObjMissing())
                        Bootstrap.Log("打印机: 物件无 TileObjectData type=" + c.type + "，跳过");
                    continue;
                }

                // 同一个物件的多格只算一个候选（origin + type 去重）
                long seenKey = ((long)c.type << 40) ^ ((long)ox << 20) ^ (uint)oy;
                if (seen.Contains(seenKey)) continue;
                seen.Add(seenKey);

                // 分层只打印当前层：多格物件以“左上角”所在的层为准，不跨层拆放
                if (!Renderer.PassesLayer(ox, oy)) continue;

                int offX = 0, offY = 0;
                TileObjectData od = GetObjectData(c.type);
                if (od != null) { offX = od.Origin.X; offY = od.Origin.Y; }

                // 物件格必须在蓝图内（不在蓝图外补半扇门）
                bool complete = true;
                for (int i = 0; i < w && complete; i++)
                {
                    for (int j = 0; j < h; j++)
                    {
                        BpCell cc;
                        if (!bp.TryGet(ox + i, oy + j, out cc) || cc.type != c.type) { complete = false; break; }
                    }
                }
                if (!complete)
                {
                    if (CanLogObjMissing())
                        Bootstrap.Log("打印机: 物件跨蓝图边界，跳过 type=" + c.type + " 蓝图格=(" + e.bx + "," + e.by + ")");
                    continue;
                }

                int wx = bp.OriginX + ox;
                int wy = bp.OriginY + oy;

                long failKey = ((long)c.type << 40) ^ ((long)wx << 20) ^ (uint)wy;
                DateTime failUntil;
                if (_objFail.TryGetValue(failKey, out failUntil) && DateTime.UtcNow < failUntil) continue;

                // 已放？左上角已是同类型 → 当成已完成
                Tile t0;
                if (WorldUtil.TryTile(wx, wy, out t0) && t0 != null && t0.active() && t0.type == c.type) continue;

                // 其它格被占（包括同类型但不完整）→ 记占用，跳过
                bool occupied = false;
                for (int i = 0; i < w && !occupied; i++)
                {
                    for (int j = 0; j < h; j++)
                    {
                        Tile tt;
                        if (!WorldUtil.TryTile(wx + i, wy + j, out tt)) { occupied = true; break; }
                        if (tt.active()) { occupied = true; break; }
                        if (body.Intersects(new Rectangle((wx + i) * 16, (wy + j) * 16, 16, 16))) { occupied = true; break; }
                    }
                }
                if (occupied) { blocked++; continue; }

                int px = wx + offX;
                int py = wy + offY;
                if (!pl.IsInTileInteractionRange(px, py, TileReachCheckSettings.Simple, reachBoost)) continue;

                int dx = px - ptx, dy = py - pty;
                int d = dx * dx + dy * dy;

                if (d < anyD)
                {
                    anyD = d;
                    anyX = px;
                }

                int slot;
                if (objSlots.TryGetValue(ObjSlotKey(c.type, style), out slot))
                {
                    // 有料才算“还能放” —— 否则缺物件料时会把收尾帧修复一直挡住
                    hasPlaceable = true;
                    if (d < bestD)
                    {
                        bestD = d;
                        bestX = wx;
                        bestY = wy;
                        bestType = c.type;
                        bestStyle = style;
                        bestW = w;
                        bestH = h;
                        bestOffX = offX;
                        bestOffY = offY;
                        bestSlot = slot;
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
                    Renderer.SetHint("打印机：范围内缺少物件材料：" + BuildMissingTypeList(missing, 5));
                    if (CanLogObjMissing())
                        Bootstrap.Log("打印机: 缺少物件材料，范围内无法放置: " +
                            BuildMissingSummary(missing, 8, "item="));
                }
                else if (blocked > 0 && CanLogObjMissing())
                {
                    Bootstrap.Log("打印机: 物件有 " + blocked + " 个待放位置被占用/超距，跳过");
                }
                return false;
            }

            Item it = pl.inventory[bestSlot];
            if (it == null || it.stack <= 0 || it.createTile != bestType) return false;

            int px2 = bestX + bestOffX;
            int py2 = bestY + bestOffY;

            // dir 两个方向都试：有朝向的家具（床/椅/马桶）只用其中一个方向能通过锚点判定
            TileObject objData;
            int dir = 1;
            if (!TileObject.CanPlace(px2, py2, bestType, bestStyle, dir, out objData))
            {
                dir = -1;
                if (!TileObject.CanPlace(px2, py2, bestType, bestStyle, dir, out objData))
                {
                    FailObject(bestType, bestX, bestY, bestStyle, dir, "CanPlace 拒绝（缺支撑/被占用/空间不够）", bestW, bestH);
                    return false;
                }
            }

            // mute=true：不每放一个响一次
            bool ok = WorldGen.PlaceObject(px2, py2, bestType, true, bestStyle, 0, -1, dir);
            if (!ok)
            {
                FailObject(bestType, bestX, bestY, bestStyle, dir, "PlaceObject 返回 false", bestW, bestH);
                return false;
            }

            Tile placed;
            if (!WorldUtil.TryTile(bestX, bestY, out placed) || placed == null ||
                !placed.active() || placed.type != bestType)
            {
                FailObject(bestType, bestX, bestY, bestStyle, dir, "放置后读回不符", bestW, bestH);
                return false;
            }

            // 箱子/梳妆台/人偶/感应器等需要建立实体数据（PlaceObject 不会调这个钩子）
            try
            {
                TileObjectData.CallPostPlacementPlayerHook(px2, py2, bestType, bestStyle, dir,
                    objData.alternate, objData);
            }
            catch { /* 钩子异常不影响已经放好的方块 */ }

            // 立刻把蓝图帧写回：火把朝向（frameX 22/44）、门的随机列都要靠这个
            WriteObjectFrames(bp, bestX, bestY, bestW, bestH);
            _shapeDirty = true;
            _shapeBp = bp;

            Consume(pl, bestSlot, bestType, "物件");   // 火把/家具等是消耗品；魔棒类不扣
            Sync(bestX, bestY);
            if (CanLogObj())
            {
                Item after = pl.inventory[bestSlot];
                Bootstrap.Log("打印机: 物件 (" + bestX + "," + bestY + ") type=" + bestType +
                    " style=" + bestStyle + " 尺寸=" + bestW + "x" + bestH +
                    " 剩余=" + (after != null && after.stack > 0 ? after.stack.ToString() : "0"));
            }
            return true;
        }

        /// <summary>frame-important 且非平台 = 物件（平台已由 ① 处理）。</summary>
        private static bool IsObjectType(int type)
        {
            if (type <= 0 || Main.tileFrameImportant == null) return false;
            if (type >= Main.tileFrameImportant.Length) return false;
            if (!Main.tileFrameImportant[type]) return false;
            return !WorldUtil.IsPlatform((ushort)type);
        }

        /// <summary>取物件数据；越界/异常一律 null（类型没有物件数据时不能放）。</summary>
        private static TileObjectData GetObjectData(int type)
        {
            try { return TileObjectData.GetTileData(type, 0); }
            catch { return null; }
        }

        /// <summary>
        /// 从蓝图帧反推物件：蓝图内左上角 (ox,oy)、宽高、style。
        /// 算法照抄 TileObjectData.TryGetTileBounds（回溯）与 GetTileData(Tile)（style 逆运算）。
        /// 返回 false = 该类型没有 TileObjectData。
        /// </summary>
        private static bool TryObjectLayout(BpCell c, int bx, int by,
            out int ox, out int oy, out int w, out int h, out int style)
        {
            ox = bx;
            oy = by;
            w = 1;
            h = 1;
            style = 0;

            TileObjectData data = GetObjectData(c.type);
            if (data == null) return false;

            int fw = data.CoordinateFullWidth; if (fw <= 0) fw = 1;
            int fh = data.CoordinateFullHeight; if (fh <= 0) fh = 1;
            int cw = data.CoordinateWidth; if (cw <= 0) cw = 1;
            w = data.Width; if (w <= 0) w = 1;
            h = data.Height; if (h <= 0) h = 1;

            int fx = c.hasFrame ? c.frameX : 0;
            int fy = c.hasFrame ? c.frameY : 0;
            if (fx < 0) fx = 0;
            if (fy < 0) fy = 0;

            // ① 回溯原点：按一格的坐标宽/高累计回退，余量不足一格即停
            int remX = fx - (fx / fw) * fw;
            int backX = 0;
            while (remX >= cw && backX < w) { remX -= cw; backX++; }
            int remY = fy - (fy / fh) * fh;
            int backY = 0;
            int[] hs = data.CoordinateHeights;
            if (hs != null)
            {
                for (int i = 0; i < hs.Length && backY < h; i++)
                    if (remY >= hs[i]) { remY -= hs[i]; backY++; }
            }
            ox = bx - backX;
            oy = by - backY;

            // ② style：GetTileData(Tile) 的逆运算
            int unitX = fx / fw;
            int unitY = fy / fh;
            int wrap = data.StyleWrapLimit > 0 ? data.StyleWrapLimit : 1;
            int mult = data.StyleMultiplier > 0 ? data.StyleMultiplier : 1;
            int unit = data.StyleHorizontal ? (unitY * wrap + unitX) : (unitX * wrap + unitY);
            style = unit / mult;
            int skip = data.StyleLineSkip;
            if (skip > 1)
            {
                // 门/栅栏这类：一个 style 占 skip 条帧线，frameX/frameY 里含随行列号，必须除回去
                style = data.StyleHorizontal
                    ? (unitY / skip) * wrap + unitX
                    : (unitX / skip) * wrap + unitY;
            }
            if (style < 0) style = 0;
            return true;
        }

        /// <summary>背包 createTile(+placeStyle) → 槽位：先精确 (type,placeStyle)，再退化同 type。</summary>
        private static Dictionary<long, int> BuildObjectSlots(Player pl)
        {
            Dictionary<long, int> map = new Dictionary<long, int>();
            if (pl.inventory == null) return map;
            int n = Math.Min(58, pl.inventory.Length);
            for (int i = 0; i < n; i++)
            {
                Item it = pl.inventory[i];
                if (it == null || it.stack <= 0 || it.createTile <= 0) continue;
                long exact = ObjSlotKey(it.createTile, it.placeStyle);
                if (!map.ContainsKey(exact)) map[exact] = i;
                long any = ObjSlotKey(it.createTile, -1);
                if (!map.ContainsKey(any)) map[any] = i;
            }
            return map;
        }

        /// <summary>(type, style) 打包成 long；style=-1 表示“同类型任意款式”。</summary>
        private static long ObjSlotKey(int type, int style)
        {
            long s = style < 0 ? 0xFFFFFL : (long)(style & 0xFFFFF);
            return ((long)type << 20) | s;
        }

        /// <summary>把蓝图帧写回物件所有格（火把朝向 / 门的随机列 / 床的方向都靠它）。</summary>
        private static void WriteObjectFrames(Blueprint bp, int wx, int wy, int w, int h)
        {
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    BpCell cc;
                    if (!bp.TryGetWorld(wx + i, wy + j, out cc)) continue;
                    if (!cc.hasFrame) continue;
                    Tile t;
                    if (!WorldUtil.TryTile(wx + i, wy + j, out t) || t == null) continue;
                    if (!t.active() || t.type != cc.type) continue;
                    t.frameX = cc.frameX;
                    t.frameY = cc.frameY;
                }
            }
        }

        /// <summary>物件放置失败：日志限频 + 5 秒内不再重试同一物件。</summary>
        private static void FailObject(int type, int wx, int wy, int style, int dir, string reason, int w, int h)
        {
            long key = ((long)type << 40) ^ ((long)wx << 20) ^ (uint)wy;
            _objFail[key] = DateTime.UtcNow.AddSeconds(5);
            if (_objFail.Count > 256)
            {
                DateTime now = DateTime.UtcNow;
                List<long> dead = new List<long>();
                foreach (KeyValuePair<long, DateTime> kv in _objFail)
                    if (kv.Value <= now) dead.Add(kv.Key);
                for (int i = 0; i < dead.Count; i++) _objFail.Remove(dead[i]);
            }
            if (CanLogObjMissing())
            {
                Bootstrap.Log("打印机: 物件放置失败 type=" + type + " 目标=(" + wx + "," + wy + ")" +
                    " style=" + style + " 尺寸=" + w + "x" + h + " dir=" + dir + " 原因=" + reason);
            }
        }

        /// <summary>缺料日志：按待放格数降序，最多 8 种；prefix 区分 tile= / wall= / item=。</summary>
        private static string BuildMissingSummary(Dictionary<int, int> missing, int maxTypes, string prefix)
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
                sb.Append(prefix).Append(kv.Key).Append(" ×").Append(kv.Value);
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

            // 第三遍：物件（frame-important 非平台）。放物件时 PlaceObject 会 SquareTileFrame 3x3，
            // 邻居重框可能把火把朝向/门的随机列改掉；所有物件放完后统一按蓝图帧再写一次。
            // 这一遍之后只放电线/促动器（它们不重框），所以帧不会再被覆盖。
            foreach (BpEntry e in bp.Entries)
            {
                if (!IsObjectType(e.cell.type)) continue;
                if (!e.cell.hasFrame) continue;
                int x = bp.OriginX + e.bx;
                int y = bp.OriginY + e.by;
                Tile t;
                if (!WorldUtil.TryTile(x, y, out t) || t == null) continue;
                if (!t.active() || t.type != e.cell.type) continue;

                t.frameX = e.cell.frameX;
                t.frameY = e.cell.frameY;
            }
        }

        // ---------------- ④ 电线 / ⑤ 促动器 ----------------
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
                    // 电线是消耗品（consumable=true）：必须真扣；万一没扣（改坏/魔改物品）就停，别无限放
                    if (!Consume(pl, slot, 0, "电线")) break;
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
                Consume(pl, aslot, 0, "促动器");        // 促动器是消耗品，正常 -1
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

        /// <summary>
        /// 从背包扣 1 个材料，返回是否真的扣了。
        ///
        /// ★ 不消耗物品（法杖类：蜂巢魔棒 1129 / 骨棒 29 / 树叶魔棒 740 / 生命木魔棒 832 …，
        ///   原版 `Item.consumable == false`）放方块时不扣数量 —— 原版放蜂巢块就不消耗魔棒。
        ///   这里以前无条件 `stack--`，魔棒 stack=1 会被 `SetDefaults(0)` 直接吞掉（用户实测的丢物品 bug）。
        ///
        /// ★ 反向保证：`consumable == true` 的普通材料必须照常 -1（末尾 Debug.Assert 兜住），
        ///   不要一刀切“都不扣” —— 那会变成无限材料。
        /// </summary>
        private static bool Consume(Player pl, int slot, int placeType, string what)
        {
            if (pl.inventory == null || slot < 0 || slot >= pl.inventory.Length) return false;
            Item it = pl.inventory[slot];
            if (it == null || it.stack <= 0) return false;

            if (!it.consumable)
            {
                // 放置照样成功，只是不扣数量；限频记一条，方便以后确认行为
                if (CanLogNonConsume())
                {
                    Bootstrap.Log("打印机: 使用非消耗物品放置 " +
                        (string.IsNullOrEmpty(what) ? "" : what + " ") +
                        "type=" + placeType + " item=" + it.type + "（不扣数量）");
                }
                return false;
            }

            int before = it.stack;
            it.stack--;
            if (it.stack <= 0) it.SetDefaults(0);

            // 断言：可消耗材料必须真的少了 1 个（或整格被清空）。Debug 版才有；release 编译成空。
            Debug.Assert(it.type == 0 || it.stack == before - 1,
                "Consume: consumable=true 的物品没有扣数量，type=" + it.type);
            return true;
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

        /// <summary>墙日志限频：2 秒一条（与方块/电线各自独立）。</summary>
        private static bool CanLogWall()
        {
            if ((DateTime.UtcNow - _wallLogTime).TotalSeconds < 2) return false;
            _wallLogTime = DateTime.UtcNow;
            return true;
        }

        /// <summary>缺墙材料聚合日志：5 秒最多一条。</summary>
        private static bool CanLogWallMissing()
        {
            if ((DateTime.UtcNow - _wallMissingLogTime).TotalSeconds < 5) return false;
            _wallMissingLogTime = DateTime.UtcNow;
            return true;
        }

        /// <summary>物件日志限频：2 秒一条（与方块/墙/电线各自独立）。</summary>
        private static bool CanLogObj()
        {
            if ((DateTime.UtcNow - _objLogTime).TotalSeconds < 2) return false;
            _objLogTime = DateTime.UtcNow;
            return true;
        }

        /// <summary>物件缺料/失败聚合日志：5 秒最多一条。</summary>
        private static bool CanLogObjMissing()
        {
            if ((DateTime.UtcNow - _objMissingLogTime).TotalSeconds < 5) return false;
            _objMissingLogTime = DateTime.UtcNow;
            return true;
        }

        /// <summary>“用了非消耗物品”日志限频：5 秒一条（蜂巢魔棒/骨棒这类，避免每格刷屏）。</summary>
        private static bool CanLogNonConsume()
        {
            if ((DateTime.UtcNow - _nonConsumeLogTime).TotalSeconds < 5) return false;
            _nonConsumeLogTime = DateTime.UtcNow;
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
