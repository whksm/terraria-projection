// WorldUtil.cs —— 世界读取 / 物品辅助 / 放置可行性预判
// 所有越界、空引用、异常都在这里兜底，业务代码不再直接碰 Main.tile。
// 兼容 .NET Framework 4.0（C# 5 语法）。

using System;
using Terraria;
using Terraria.ID;

namespace ProjectionMod
{
    public static class WorldUtil
    {
        /// <summary>坐标是否在世界内（含边界格）。</summary>
        public static bool InWorld(int x, int y)
        {
            return Main.tile != null && x >= 0 && y >= 0 && x <= Main.maxTilesX && y <= Main.maxTilesY;
        }

        /// <summary>安全读格：越界返回 null。</summary>
        public static Tile TileAt(int x, int y)
        {
            if (!InWorld(x, y)) return null;
            return Main.tile[x, y];
        }

        public static bool TryTile(int x, int y, out Tile t)
        {
            if (!InWorld(x, y)) { t = null; return false; }
            t = Main.tile[x, y];
            return true;
        }

        /// <summary>当前本地玩家（菜单/未加载时为 null）。</summary>
        public static Player LocalPlayer
        {
            get
            {
                if (Main.gameMenu) return null;
                return Main.LocalPlayer;
            }
        }

        /// <summary>手持物品（越界/空时返回 null）。</summary>
        public static Item HeldItem(Player pl)
        {
            if (pl == null || pl.inventory == null) return null;
            int sel = pl.selectedItem;
            if (sel < 0 || sel >= pl.inventory.Length) return null;
            return pl.inventory[sel];
        }

        /// <summary>从世界 Tile 生成蓝图格（含方块/墙/帧/电线/促动器/斜坡/半砖/油漆）。</summary>
        public static BpCell CellFromTile(Tile t)
        {
            BpCell c;
            c.type = t.active() ? t.type : (ushort)0;
            c.wall = t.wall;
            c.frameX = t.frameX;
            c.frameY = t.frameY;
            c.hasFrame = true;
            c.wire = PackWires(t);
            c.actuator = t.actuator();
            c.slope = t.slope();
            c.halfBrick = t.halfBrick();
            c.paint = t.color();
            c.wallPaint = t.wallColor();
            return c;
        }

        /// <summary>电线四位打包成 bitmask（bit0 红 / bit1 蓝 / bit2 绿 / bit3 黄）。</summary>
        public static byte PackWires(Tile t)
        {
            byte w = 0;
            if (t.wire()) w |= 1;
            if (t.wire2()) w |= 2;
            if (t.wire3()) w |= 4;
            if (t.wire4()) w |= 8;
            return w;
        }

        /// <summary>格子是否“有内容”（方块或墙）。电线/漆单独存在也算保存对象，这里只判主内容。</summary>
        public static bool HasContent(Tile t)
        {
            return t.active() || t.wall != 0;
        }

        /// <summary>物品能否满足该蓝图格（方块或墙）。</summary>
        public static bool ItemMatches(Item it, BpCell cell)
        {
            if (it == null || it.stack <= 0) return false;
            if (cell.type != 0 && it.createTile == cell.type) return true;
            if (cell.wall != 0 && it.createWall == cell.wall) return true;
            return false;
        }

        /// <summary>格子相邻是否有实体方块（用于火把等需要支撑的物块）。</summary>
        public static bool SolidNeighbor(int x, int y)
        {
            return SolidAt(x - 1, y) || SolidAt(x + 1, y) ||
                   SolidAt(x, y - 1) || SolidAt(x, y + 1);
        }

        private static bool SolidAt(int x, int y)
        {
            if (!InWorld(x, y)) return false;
            Tile t = Main.tile[x, y];
            return t.active() && Main.tileSolid[t.type];
        }

        /// <summary>
        /// 粗略预判该格能否放上去（避免自动补料在“注定失败”的格上反复换物品）。
        /// 目前只处理火把类：需要背景墙或相邻实体块支撑。其他物块交给游戏自身判定。
        /// </summary>
        public static bool CanLikelyPlace(int x, int y, BpCell cell)
        {
            if (!InWorld(x, y)) return false;
            if (cell.type != 0 && TileID.Sets.Torches[cell.type])
            {
                Tile t = Main.tile[x, y];
                if (t.wall != 0) return true;          // 有背景墙可挂
                if (t.active()) return true;           // 该格已有方块（可能替换/挂接）
                return SolidNeighbor(x, y);            // 相邻实体块支撑
            }
            return true;
        }

        /// <summary>玩家所在图格（玩家尚未落位/越界时返回 false）。</summary>
        public static bool TryPlayerTile(out int x, out int y)
        {
            x = 0;
            y = 0;
            Player pl = LocalPlayer;
            if (pl == null || !pl.active) return false;
            // 玩家坐标在基类 Entity.position（Player.position 不存在）；
            // 取碰撞箱中心作为所在格
            Microsoft.Xna.Framework.Vector2 pos = pl.position;
            if (pos.X == 0f && pos.Y == 0f) return false; // 世界加载中
            x = (int)((pos.X + pl.width * 0.5f) / 16f);
            y = (int)((pos.Y + pl.height * 0.5f) / 16f);
            return InWorld(x, y);
        }

        /// <summary>屏幕坐标 → 图格坐标。</summary>
        public static void MouseTile(out int tx, out int ty)
        {
            Microsoft.Xna.Framework.Vector2 mw = Main.MouseWorld;
            tx = (int)(mw.X / 16f);
            ty = (int)(mw.Y / 16f);
        }
    }
}
