// Renderer.cs —— 投影渲染 v9：方块贴图（深色衬底+85%半透明）+ 外围发光高亮，画在 UI 之下。
//   缺方块：贴图虚化 + 白外光     缺墙：贴图虚化 + 蓝外光
//   放错：  贴图虚化 + 红外光     多余：世界贴图虚化 + 黄外光
// 纯客户端渲染，不写世界、不发网络包，多人合法。
// 兼容 .NET Framework 4.0（C# 5 语法）。

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace ProjectionMod
{
    public static class Renderer
    {
        public static Blueprint Current;
        public static bool Enabled = true;

        private static Texture2D _pixel;
        private static DateTime _lastLog = DateTime.MinValue;
        private static DateTime _lastDrawErr = DateTime.MinValue;

        // 贴图缓存（避免每格每帧反射取值）
        private static Dictionary<int, Texture2D> _tileTexCache = new Dictionary<int, Texture2D>();
        private static Dictionary<int, Texture2D> _wallTexCache = new Dictionary<int, Texture2D>();

        // 贴图帧校准：从世界里的真实同类方块/墙采样其 frameX/frameY（解决拼接贴图取帧错误）
        private static Dictionary<int, Point> _tileFrames = new Dictionary<int, Point>();
        private static Dictionary<int, Point> _wallFrames = new Dictionary<int, Point>();

        // 材料清单 HUD（F5 开关）
        public static bool HudEnabled = true;

        /// <summary>临时提示（如“该格缺支撑”），2.5 秒后自动消失。</summary>
        public static string Hint;
        private static DateTime _hintUntil = DateTime.MinValue;

        // —— 分层渲染（L 切换模式，PgUp/PgDn 调层号，Shift+PgUp/PgDn 调范围厚度）——
        // 分层模式：Y/X 各支持“单层/范围”，并且方向可选（Y：自上而下/自下而上；X：自左向右/自右向左）
        public enum LayerKind
        {
            All = 0,
            YSingleDown = 1,   // Y 单层，从上往下数
            YSingleUp = 2,     // Y 单层，从下往上数
            YRangeDown = 3,    // Y 范围，上→下
            YRangeUp = 4,      // Y 范围，下→上
            XSingleRight = 5,  // X 单层，从左往右数
            XSingleLeft = 6,   // X 单层，从右往左数
            XRangeRight = 7,   // X 范围，左→右
            XRangeLeft = 8     // X 范围，右→左
        }
        public static LayerKind Layer = LayerKind.All;
        public static int LayerIndex;
        public static int LayerThickness = 2;

        private static bool IsY
        {
            get { return Layer >= LayerKind.YSingleDown && Layer <= LayerKind.YRangeUp; }
        }
        private static bool IsRange
        {
            get { return Layer == LayerKind.YRangeDown || Layer == LayerKind.YRangeUp || Layer == LayerKind.XRangeRight || Layer == LayerKind.XRangeLeft; }
        }
        private static bool IsReversed
        {
            get
            {
                return Layer == LayerKind.YSingleUp || Layer == LayerKind.YRangeUp ||
                       Layer == LayerKind.XSingleLeft || Layer == LayerKind.XRangeLeft;
            }
        }

        public static bool PassesLayer(int bx, int by)
        {
            if (Layer == LayerKind.All) return true;

            int total = 0;
            if (Current != null) total = IsY ? Current.Height : Current.Width;

            // 把“从另一侧数”的层号换算成蓝图内的相对坐标
            int coord = IsY ? by : bx;
            if (IsReversed && total > 0) coord = (total - 1) - coord;

            int diff = coord - LayerIndex;
            if (diff < 0) diff = -diff;
            return IsRange ? diff <= LayerThickness : diff == 0;
        }

        public static string LayerText
        {
            get
            {
                if (Layer == LayerKind.All) return "分层: 全部";
                string axis = IsY ? "Y" : "X";
                string dir;
                switch (Layer)
                {
                    case LayerKind.YSingleDown:
                    case LayerKind.YRangeDown: dir = "↓"; break;
                    case LayerKind.YSingleUp:
                    case LayerKind.YRangeUp: dir = "↑"; break;
                    case LayerKind.XSingleRight:
                    case LayerKind.XRangeRight: dir = "→"; break;
                    default: dir = "←"; break;
                }
                string kind = IsRange ? ("±" + LayerThickness) : "单层";
                int total = 0;
                if (Current != null) total = IsY ? Current.Height : Current.Width;
                return "分层: " + axis + dir + " " + kind + " " + (LayerIndex + 1) + "/" + Math.Max(1, total);
            }
        }

        public static void CycleLayer()
        {
            Layer = (LayerKind)(((int)Layer + 1) % 9);
            LayerIndex = 0;
            Bootstrap.Log(LayerText + "（层号从所选方向的第 1 层开始）");
            SetHint(LayerText);
        }

        public static void MoveLayer(int delta)
        {
            if (Layer == LayerKind.All) { CycleLayer(); return; }
            int max = 0;
            if (Current != null) max = IsY ? Current.Height : Current.Width;
            LayerIndex += delta;
            if (LayerIndex < 0) LayerIndex = 0;
            if (max > 0 && LayerIndex > max - 1) LayerIndex = max - 1;
            Bootstrap.Log(LayerText);
        }

        public static void ChangeThickness(int delta)
        {
            LayerThickness += delta;
            if (LayerThickness < 0) LayerThickness = 0;
            if (LayerThickness > 32) LayerThickness = 32;
            Bootstrap.Log("分层范围: ±" + LayerThickness);
            SetHint("分层范围 ±" + LayerThickness);
        }

        public static void SetHint(string msg)
        {
            Hint = msg;
            _hintUntil = DateTime.UtcNow.AddSeconds(2.5);
        }
        private static Dictionary<int, int> _tileToItem;
        private static Dictionary<int, int> _wallToItem;
        private static List<string> _matLines = new List<string>();
        private static int _matTick = 0;

        // 贴图资源（TextureAssets.Tile/Wall 是 Asset<Texture2D>[]，Asset<T> 定义在游戏内嵌的
        // ReLogic 程序集里，编译期拿不到 → 用反射读取并缓存 PropertyInfo）
        private static Array _tileAssets;
        private static Array _wallAssets;
        private static Array _itemAssets;
        private static PropertyInfo _assetValueProp;
        private static Dictionary<int, Texture2D> _itemTexCache = new Dictionary<int, Texture2D>();

        // 虚化贴图 + 深色衬底（保证灰系方块在任何背景下都清晰可辨）
        private static readonly Color C_GHOST     = Color.White * 0.85f;
        private static readonly Color C_BACKDROP  = Color.Black * 0.40f;
        private static readonly Color B_MISS_TILE  = new Color(255, 255, 255, 230);   // 缺方块：白
        private static readonly Color B_MISS_WALL  = new Color(120, 180, 255, 230);   // 缺墙：蓝
        private static readonly Color B_WRONG      = new Color(255, 70, 70, 240);     // 放错：红
        private static readonly Color B_EXTRA      = new Color(255, 215, 60, 240);    // 多余：黄

        /// <summary>图格坐标打包成 64 位键（轮廓判定用，避免字符串分配）。</summary>
        private static long Pack(int x, int y)
        {
            return ((long)x << 32) ^ (uint)y;
        }

        /// <summary>电线/促动器提示：蓝图有、世界缺时叠画在对应格上。</summary>
        private static void DrawWireOverlays(SpriteBatch sb)
        {
            try
            {
                if (Current == null || Main.tile == null) return;

                Vector2 zoom = Main.GameViewMatrix.Zoom;
                float zx = zoom.X > 0.01f ? zoom.X : 1f;
                float zy = zoom.Y > 0.01f ? zoom.Y : 1f;
                int vx0 = (int)(Main.screenPosition.X / 16f) - 1;
                int vy0 = (int)(Main.screenPosition.Y / 16f) - 1;
                int vx1 = (int)((Main.screenPosition.X + Main.screenWidth / zx) / 16f) + 1;
                int vy1 = (int)((Main.screenPosition.Y + Main.screenHeight / zy) / 16f) + 1;

                List<WireOp> list = new List<WireOp>();
                foreach (BpEntry e in Current.Entries)
                {
                    BpCell cell = e.cell;
                    if (cell.wire == 0 && !cell.actuator) continue;
                    int tx = Current.OriginX + e.bx;
                    int ty = Current.OriginY + e.by;
                    if (tx < vx0 || tx > vx1 || ty < vy0 || ty > vy1) continue;
                    Tile t;
                    if (!WorldUtil.TryTile(tx, ty, out t)) continue;

                    byte missingWires = (byte)(cell.wire & ~WorldUtil.PackWires(t));
                    bool missingActuator = cell.actuator && !t.actuator();
                    if (missingWires == 0 && !missingActuator) continue;

                    WireOp w;
                    w.x = tx;
                    w.y = ty;
                    w.wire = missingWires;
                    w.actuator = missingActuator;
                    list.Add(w);
                }
                if (list.Count == 0) return;

                Texture2D px = Pixel(sb);
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                    DepthStencilState.None, RasterizerState.CullNone, null,
                    Main.GameViewMatrix.TransformationMatrix);

                foreach (WireOp w in list)
                {
                    int dx = w.x * 16 - (int)Main.screenPosition.X;
                    int dy = w.y * 16 - (int)Main.screenPosition.Y;

                    // 电线：画成“会连到相邻同色电线”的细线（模拟原版电线的走向）
                    for (int bit = 0; bit < 4; bit++)
                    {
                        if ((w.wire & (1 << bit)) == 0) continue;
                        Color c = WireColor(bit);
                        bool left = HasBlueprintWire(w.x - 1, w.y, bit);
                        bool right = HasBlueprintWire(w.x + 1, w.y, bit);
                        bool up = HasBlueprintWire(w.x, w.y - 1, bit);
                        bool down = HasBlueprintWire(w.x, w.y + 1, bit);

                        int cx = dx + 7;
                        int cy = dy + 7;
                        int x0 = left ? dx : cx;
                        int x1 = right ? dx + 16 : cx + 2;
                        int y0 = up ? dy : cy;
                        int y1 = down ? dy + 16 : cy + 2;
                        sb.Draw(px, new Rectangle(x0, cy, x1 - x0, 2), c);
                        sb.Draw(px, new Rectangle(cx, y0, 2, y1 - y0), c);
                    }

                    // 促动器：细方框 + 中心点
                    if (w.actuator)
                    {
                        Color ac = new Color(210, 225, 255) * 0.9f;
                        sb.Draw(px, new Rectangle(dx + 4, dy + 4, 9, 1), ac);
                        sb.Draw(px, new Rectangle(dx + 4, dy + 12, 9, 1), ac);
                        sb.Draw(px, new Rectangle(dx + 4, dy + 4, 1, 9), ac);
                        sb.Draw(px, new Rectangle(dx + 12, dy + 4, 1, 9), ac);
                    }
                }
                sb.End();
            }
            catch { /* 电线提示失败静默 */ }
        }

        /// <summary>画单格幽灵贴图（含深色衬底与取不到贴图时的兜底色块）。</summary>
        private static void DrawCellTexture(SpriteBatch sb, Texture2D px, DrawOp op)
        {
            // 已有别的方块/墙的格子：只由“外框高光”那一层描边，绝不覆盖原贴图
            if (op.outlineOnly) return;

            int dx = op.x * 16 - (int)Main.screenPosition.X;
            int dy = op.y * 16 - (int)Main.screenPosition.Y;
            Rectangle dst = new Rectangle(dx, dy, 16, 16);

            bool isWall = op.texKind == 1;
            bool platform = !isWall && op.texType >= 0 && op.texType <= 65535
                && WorldUtil.IsPlatform((ushort)op.texType);

            DrawCellBackdrop(sb, px, dx, dy, op, isWall, platform);

            Texture2D tex = null;
            Rectangle src = new Rectangle(0, 0, 16, 16);
            bool wholeTex = false;

            if (op.texKind == 0)
            {
                // 方块图集 + 帧：保存帧 > 世界校准帧 > (0,0) 兜底
                tex = TileTexture(op.texType);
                Point fr = op.frX >= 0
                    ? new Point(op.frX, op.frY)
                    : FrameOf(op.texKind, op.texType);
                if (tex != null)
                    src = new Rectangle(fr.X >= 0 ? fr.X : 0, fr.Y >= 0 ? fr.Y : 0, 16, 16);
                else
                {
                    int item = MapItem(_tileToItem, (ushort)op.texType);
                    if (item > 0)
                    {
                        tex = ItemTexture(item);
                        wholeTex = tex != null;
                    }
                }
            }
            else
            {
                tex = WallTexture(op.texType);
                Point fr = op.frX >= 0
                    ? new Point(op.frX, op.frY)
                    : FrameOf(op.texKind, op.texType);
                if (tex != null)
                    src = new Rectangle(fr.X >= 0 ? fr.X : 0, fr.Y >= 0 ? fr.Y : 0, 16, 16);
                else
                {
                    int witem = MapItem(_wallToItem, (ushort)op.texType);
                    if (witem > 0)
                    {
                        tex = ItemTexture(witem);
                        wholeTex = tex != null;
                    }
                }
            }

            Color tint = isWall ? C_GHOST * 0.6f : C_GHOST;
            if (op.paint != 0)
            {
                Color pc = PaintColor(op.paint);
                tint = new Color(tint.R * pc.R / 255, tint.G * pc.G / 255, tint.B * pc.B / 255, tint.A);
            }

            if (tex != null)
            {
                if (wholeTex)
                {
                    // 物品图标没有方块帧，整张画，形状用兜底线提示
                    sb.Draw(tex, dst, null, tint);
                }
                else if (!isWall && op.halfBrick)
                {
                    // 半砖：取 frame 顶部 8px，画到下半格
                    sb.Draw(tex, new Rectangle(dx, dy + 8, 16, 8),
                        new Rectangle(src.X, src.Y, 16, 8), tint);
                }
                else if (!isWall && !platform && op.slope != 0 && !HasSlopeFrames(op.texType))
                {
                    // 普通块坡：按游戏算法用 8 条 2px 竖条裁出坡
                    DrawSlopeStrips(sb, tex, src.X, src.Y, dx, dy, op.slope, tint, true);
                }
                else
                {
                    // 平台/整块/自带坡帧：直接画 frame（平台的 frameX 已经编码了形状）
                    sb.Draw(tex, dst, src, tint);
                }
            }
            else
            {
                DrawFallbackShape(sb, px, dx, dy, op, isWall, platform);
            }

            // 平台 slope 3/4（block swap 遗留）：游戏照画平放帧，投影也照画；
            // 额外加一个 3x3 半透明角标提示隐藏坡向，不改变形状判断。
            if (!isWall && platform && op.slope >= 3 && tex != null && !wholeTex)
                DrawPlatformSlopeCorner(sb, px, dx, dy, op.slope, op.halfBrick);

            // 只在“没有真实方块贴图”或“只有物品图标”时画兜底线，避免盖住真实 frame
            if (tex == null || wholeTex)
                DrawShapeFallbackLine(sb, px, dx, dy, op.slope, op.halfBrick);
        }

        /// <summary>衬底也按形状裁：半砖只画下半格，普通块坡画同样 8 条，平台坡不画整格。</summary>
        private static void DrawCellBackdrop(SpriteBatch sb, Texture2D px, int dx, int dy,
            DrawOp op, bool isWall, bool platform)
        {
            if (isWall)
            {
                sb.Draw(px, new Rectangle(dx, dy, 16, 16), C_BACKDROP * 0.6f);
                return;
            }
            if (op.halfBrick)
            {
                sb.Draw(px, new Rectangle(dx, dy + 8, 16, 8), C_BACKDROP);
                return;
            }
            if (op.slope != 0)
            {
                if (!platform && !HasSlopeFrames(op.texType))
                    DrawSlopeStrips(sb, px, 0, 0, dx, dy, op.slope, C_BACKDROP, false);
                // 平台坡/自带坡帧：形状由 frame 决定，画整格衬底会露出空白半边
                return;
            }
            sb.Draw(px, new Rectangle(dx, dy, 16, 16), C_BACKDROP);
        }

        /// <summary>取不到方块贴图时的兜底：按形状铺色块（坡用 8 条，半砖用下半格）。</summary>
        private static void DrawFallbackShape(SpriteBatch sb, Texture2D px, int dx, int dy,
            DrawOp op, bool isWall, bool platform)
        {
            Color fc = FallbackColor(op.texKind, op.texType) * 0.55f;
            if (isWall)
            {
                sb.Draw(px, new Rectangle(dx, dy, 16, 16), fc);
                return;
            }
            if (op.halfBrick)
            {
                sb.Draw(px, new Rectangle(dx, dy + 8, 16, 8), fc);
                return;
            }
            if (op.slope != 0 && !platform && !HasSlopeFrames(op.texType))
            {
                DrawSlopeStrips(sb, px, 0, 0, dx, dy, op.slope, fc, false);
                return;
            }
            sb.Draw(px, new Rectangle(dx, dy, 16, 16), fc);
        }

        private static bool HasSlopeFrames(int type)
        {
            return type >= 0 && type < TileID.Sets.HasSlopeFrames.Length && TileID.Sets.HasSlopeFrames[type];
        }

        /// <summary>
        /// 普通块坡的 8 条竖条裁剪（与 1.4.5.8 TileDrawing.DrawBasicTile 的算法一致）。
        /// useSource=false 时用 1x1 像素纹理铺色块（衬底/兜底）。
        /// </summary>
        private static void DrawSlopeStrips(SpriteBatch sb, Texture2D tex, int srcX, int srcY,
            int dx, int dy, byte slope, Color tint, bool useSource)
        {
            for (int i = 0; i < 8; i++)
            {
                int offX;
                int offY;
                int h;
                int srcYOff;
                switch (slope)
                {
                    case 1:
                        offX = i * 2;
                        offY = i * 2;
                        h = 14 - i * 2;
                        srcYOff = 0;
                        break;
                    case 2:
                        offX = 16 - i * 2 - 2;
                        offY = i * 2;
                        h = 14 - i * 2;
                        srcYOff = 0;
                        break;
                    case 3:
                        offX = i * 2;
                        offY = 0;
                        h = 16 - i * 2;
                        srcYOff = i * 2;
                        break;
                    default: // 4
                        offX = 16 - i * 2 - 2;
                        offY = 0;
                        h = 16 - i * 2;
                        srcYOff = i * 2;
                        break;
                }
                if (h <= 0) continue;
                Rectangle d = new Rectangle(dx + offX, dy + offY, 2, h);
                if (useSource)
                    sb.Draw(tex, d, new Rectangle(srcX + offX, srcY + srcYOff, 2, h), tint);
                else
                    sb.Draw(tex, d, tint);
            }
        }

        /// <summary>平台 slope 3/4 的隐藏坡向角标（左上/右上小 L，不覆盖形状主体）。</summary>
        private static void DrawPlatformSlopeCorner(SpriteBatch sb, Texture2D px, int dx, int dy,
            byte slope, bool halfBrick)
        {
            Color c = new Color(255, 245, 200) * 0.45f;
            int y = dy + (halfBrick ? 9 : 1);
            int x = slope == 3 ? dx + 1 : dx + 12;
            sb.Draw(px, new Rectangle(x, y, 3, 1), c);
            sb.Draw(px, new Rectangle(x, y, 1, 3), c);
        }

        /// <summary>贴图缺失时的方向提示：1→\、2→/、3→/、4→\；半砖画 y+8 横线。</summary>
        private static void DrawShapeFallbackLine(SpriteBatch sb, Texture2D px, int dx, int dy,
            byte slope, bool halfBrick)
        {
            Color c = new Color(255, 245, 200) * 0.85f;
            if (halfBrick)
            {
                for (int i = 0; i < 4; i++)
                    sb.Draw(px, new Rectangle(dx + i * 4, dy + 8, 3, 2), c);
                return;
            }
            if (slope == 0) return;

            bool downRight = (slope == 1 || slope == 4);
            for (int i = 0; i < 8; i++)
            {
                int x = dx + i * 2;
                int y = downRight ? dy + i * 2 : dy + 15 - i * 2;
                sb.Draw(px, new Rectangle(x, y, 2, 2), c);
            }
        }

        /// <summary>漆色（近似原版油漆调色板，用于幽灵着色提示）。</summary>
        private static Color PaintColor(byte paint)
        {
            switch (paint)
            {
                case 1: return new Color(255, 60, 60);     // 红
                case 2: return new Color(255, 140, 40);    // 橙
                case 3: return new Color(255, 235, 70);    // 黄
                case 4: return new Color(170, 255, 70);    // 黄绿
                case 5: return new Color(70, 220, 70);     // 绿
                case 6: return new Color(60, 210, 190);    // 青绿
                case 7: return new Color(70, 220, 255);    // 青
                case 8: return new Color(110, 170, 255);   // 天蓝
                case 9: return new Color(70, 90, 255);     // 蓝
                case 10: return new Color(160, 70, 255);   // 紫
                case 11: return new Color(210, 80, 255);   // 紫罗兰
                case 12: return new Color(255, 120, 200);  // 粉
                case 13: return new Color(180, 30, 30);    // 深红
                case 14: return new Color(190, 100, 30);   // 深橙
                case 15: return new Color(190, 175, 50);   // 深黄
                case 16: return new Color(120, 190, 50);   // 深黄绿
                case 17: return new Color(40, 150, 40);    // 深绿
                case 18: return new Color(40, 150, 140);   // 深青绿
                case 19: return new Color(40, 160, 190);   // 深青
                case 20: return new Color(70, 120, 190);   // 深天蓝
                case 21: return new Color(40, 50, 170);    // 深蓝
                case 22: return new Color(110, 40, 170);   // 深紫
                case 23: return new Color(150, 50, 190);   // 深紫罗兰
                case 24: return new Color(190, 80, 150);   // 深粉
                case 25: return new Color(40, 40, 40);     // 黑
                case 26: return new Color(245, 245, 245);  // 白
                case 27: return new Color(140, 140, 140);  // 灰
                case 28: return new Color(140, 100, 60);   // 棕
                case 29: return new Color(70, 60, 90);     // 影漆
                default: return new Color(255, 255, 255);  // 其它（负漆/夜光）当白色提示
            }
        }

        /// <summary>取不到贴图时的兜底颜色（按类型散列，同一类固定色，便于辨认占位）。</summary>
        private static Color FallbackColor(int texKind, int texType)
        {
            int h = texType * 2654435761u.GetHashCode() + texKind * 97;
            int r = 90 + (Math.Abs(h) % 90);
            int g = 90 + (Math.Abs(h / 7) % 90);
            int b = 90 + (Math.Abs(h / 31) % 90);
            return new Color(r, g, b);
        }

        /// <summary>蓝图某格是否有指定颜色的电线（位 0-3）。</summary>
        private static bool HasBlueprintWire(int x, int y, int bit)
        {
            try
            {
                Blueprint bp = Current;
                if (bp == null) return false;
                BpCell cell;
                if (!bp.TryGetWorld(x, y, out cell)) return false;
                return (cell.wire & (1 << bit)) != 0;
            }
            catch { return false; }
        }

        private static Color WireColor(int bit)
        {
            switch (bit)
            {
                case 0: return new Color(255, 70, 70);    // 红
                case 1: return new Color(80, 150, 255);   // 蓝
                case 2: return new Color(90, 220, 100);   // 绿
                default: return new Color(255, 215, 70);  // 黄
            }
        }

        private struct WireOp
        {
            public int x;
            public int y;
            public byte wire;
            public bool actuator;
        }

        private static Texture2D Pixel(SpriteBatch sb)
        {
            if (_pixel == null || _pixel.GraphicsDevice != sb.GraphicsDevice)
            {
                if (_pixel != null) { try { _pixel.Dispose(); } catch { } }
                _pixel = new Texture2D(sb.GraphicsDevice, 1, 1);
                _pixel.SetData<Color>(new Color[] { Color.White });
            }
            return _pixel;
        }

        private static Texture2D AssetValue(object asset)
        {
            if (asset == null) return null;
            if (_assetValueProp == null)
                _assetValueProp = Compat.Property(asset.GetType(), "Value");
            if (_assetValueProp == null) return null;
            try { return _assetValueProp.GetValue(asset, null) as Texture2D; }
            catch { return null; }
        }

        /// <summary>
        /// 物品图标贴图。幽灵方块优先用“物品图标”而不是方块图集：
        /// 图标本身就是可辨认的单个方块/火把/家具，彻底避开图集帧布局问题。
        /// </summary>
        private static Texture2D ItemTexture(int itemType)
        {
            Texture2D cached;
            if (_itemTexCache.TryGetValue(itemType, out cached) && cached != null && !cached.IsDisposed)
                return cached;
            try
            {
                if (_itemAssets == null)
                {
                    FieldInfo f = Compat.Field(typeof(TextureAssets), "Item");
                    if (f == null) return null;
                    _itemAssets = (Array)f.GetValue(null);
                }
                if (itemType <= 0 || itemType >= _itemAssets.Length) return null;
                object asset = _itemAssets.GetValue(itemType);
                if (asset == null) return null;
                Texture2D t = AssetValue(asset);
                if (t == null)
                {
                    try { Main.instance.LoadItem(itemType); } catch { }
                    t = AssetValue(asset);
                }
                if (t != null) _itemTexCache[itemType] = t;
                return t;
            }
            catch { return null; }
        }

        private static Texture2D TileTexture(int type)
        {
            Texture2D cached;
            if (_tileTexCache.TryGetValue(type, out cached) && cached != null && !cached.IsDisposed)
                return cached;
            Texture2D t = LoadTileTexture(type);
            if (t != null) _tileTexCache[type] = t;
            return t;
        }

        private static Texture2D LoadTileTexture(int type)
        {
            try
            {
                if (_tileAssets == null)
                {
                    FieldInfo f = Compat.Field(typeof(TextureAssets), "Tile");
                    _tileAssets = (Array)f.GetValue(null);
                }
                if (type <= 0 || type >= _tileAssets.Length) return null;
                object asset = _tileAssets.GetValue(type);
                if (asset == null) return null;
                Texture2D t = AssetValue(asset);
                if (t == null)
                {
                    // 贴图懒加载：出生点附近没渲染过的方块其贴图未加载 → 调游戏自身加载器
                    try { Main.instance.LoadTiles(type); } catch { }
                    t = AssetValue(asset);
                }
                return t;
            }
            catch { return null; }
        }

        private static Texture2D WallTexture(int wall)
        {
            Texture2D cached;
            if (_wallTexCache.TryGetValue(wall, out cached) && cached != null && !cached.IsDisposed)
                return cached;
            Texture2D t = LoadWallTexture(wall);
            if (t != null) _wallTexCache[wall] = t;
            return t;
        }

        private static Texture2D LoadWallTexture(int wall)
        {
            try
            {
                if (_wallAssets == null)
                {
                    FieldInfo f = Compat.Field(typeof(TextureAssets), "Wall");
                    _wallAssets = (Array)f.GetValue(null);
                }
                if (wall <= 0 || wall >= _wallAssets.Length) return null;
                object asset = _wallAssets.GetValue(wall);
                if (asset == null) return null;
                Texture2D t = AssetValue(asset);
                if (t == null)
                {
                    try { Main.instance.LoadWall(wall); } catch { }
                    t = AssetValue(asset);
                }
                return t;
            }
            catch { return null; }
        }

        /// <summary>从世界采样校准每种方块/墙的“整面”贴图帧（优先四周同类的完整面）。</summary>
        public static void Calibrate(Blueprint bp)
        {
            try
            {
                _tileFrames.Clear();
                _wallFrames.Clear();
                HashSet<int> tts = new HashSet<int>();
                HashSet<int> wts = new HashSet<int>();
                foreach (BpEntry e in bp.Entries)
                {
                    if (e.cell.type != 0) tts.Add(e.cell.type);
                    if (e.cell.wall != 0) wts.Add(e.cell.wall);
                }
                int cx = Main.spawnTileX;
                int cy = Main.spawnTileY;
                EnsureItemMaps();
                foreach (int t in tts)
                {
                    Point f = FindFrame(t, true, cx, cy);
                    if (f.X >= 0) _tileFrames[t] = f;
                    Texture2D tex = TileTexture(t);
                    int item = MapItem(_tileToItem, (ushort)t);
                    Texture2D icon = item > 0 ? ItemTexture(item) : null;
                    Bootstrap.Log(string.Format(
                        "自检 tile {0}: item={1} icon={2} | 图集帧 {3},{4} 图集 {5}x{6}",
                        t, item, icon != null ? "OK" : "缺",
                        f.X, f.Y,
                        tex != null ? tex.Width.ToString() : "?", tex != null ? tex.Height.ToString() : "?"));
                }
                foreach (int w in wts)
                {
                    Point f = FindFrame(w, false, cx, cy);
                    if (f.X >= 0) _wallFrames[w] = f;
                    Texture2D tex = WallTexture(w);
                    Bootstrap.Log(string.Format("自检 wall {0}: frame {1},{2} | 贴图 {3}x{4}",
                        w, f.X, f.Y,
                        tex != null ? tex.Width.ToString() : "?", tex != null ? tex.Height.ToString() : "?"));
                }
            }
            catch (Exception e)
            {
                Bootstrap.Log("校准失败: " + e.Message);
            }
        }

        private static Point FindFrame(int type, bool isTile, int cx, int cy)
        {
            Point any = new Point(-1, -1);
            Point surrounded = new Point(-1, -1);
            int rx = 350, ry = 120;
            int x0 = Math.Max(0, cx - rx);
            int x1 = Math.Min(Main.maxTilesX, cx + rx);
            int y0 = Math.Max(10, cy - ry);
            int y1 = Math.Min(Main.maxTilesY, cy + ry);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    Tile t = Main.tile[x, y];
                    bool match = isTile ? (t.active() && t.type == type) : (t.wall == type);
                    if (!match) continue;
                    if (any.X < 0) any = new Point(t.frameX, t.frameY);
                    int same = SameNeighbors(x, y, type, isTile);
                    if (same == 0)
                        return new Point(t.frameX, t.frameY);          // 孤立整块（自带边缘光影，最像方块）
                    if (same == 4 && surrounded.X < 0)
                        surrounded = new Point(t.frameX, t.frameY);    // 四周同块的“整面”
                }
            }
            if (surrounded.X >= 0) return surrounded;
            return any;
        }

        private static int SameNeighbors(int x, int y, int type, bool isTile)
        {
            int[] dxs = { 1, -1, 0, 0 };
            int[] dys = { 0, 0, 1, -1 };
            int n = 0;
            for (int i = 0; i < 4; i++)
            {
                int nx = x + dxs[i];
                int ny = y + dys[i];
                if (nx < 0 || ny < 0 || nx > Main.maxTilesX || ny > Main.maxTilesY) continue;
                Tile t = Main.tile[nx, ny];
                bool same = isTile ? (t.active() && t.type == type) : (t.wall == type);
                if (same) n++;
            }
            return n;
        }

        private static Point FrameOf(int texKind, int texType)
        {
            Point f;
            if (texKind == 0 && _tileFrames.TryGetValue(texType, out f)) return f;
            if (texKind == 1 && _wallFrames.TryGetValue(texType, out f)) return f;
            return new Point(-1, -1);
        }

        /// <summary>在 Main.DrawInterface 的 Prefix 中调用：世界之上、所有 UI 之下。</summary>
        public static void Draw()
        {
            if (!Enabled || Current == null) return;
            if (Main.gameMenu) return;
            Player p = Main.LocalPlayer;
            if (p == null) return;
            SpriteBatch sb = Main.spriteBatch;
            if (sb == null || sb.GraphicsDevice == null) return;
            if (Main.tile == null) return;

            EnsureItemMaps();   // 物品图标贴图需要 方块→物品 反查表

            Vector2 zoom = Main.GameViewMatrix.Zoom;
            float zx = zoom.X > 0.01f ? zoom.X : 1f;
            float zy = zoom.Y > 0.01f ? zoom.Y : 1f;

            int vx0 = (int)(Main.screenPosition.X / 16f) - 2;
            int vy0 = (int)(Main.screenPosition.Y / 16f) - 2;
            int vx1 = (int)((Main.screenPosition.X + Main.screenWidth / zx) / 16f) + 2;
            int vy1 = (int)((Main.screenPosition.Y + Main.screenHeight / zy) / 16f) + 2;

            int maxX = Main.maxTilesX;
            int maxY = Main.maxTilesY;

            List<DrawOp> ops = new List<DrawOp>();
            int missTile = 0, missWall = 0, wrong = 0;

            foreach (BpEntry entry in Current.Entries)
            {
                BpCell cell = entry.cell;
                if (cell.IsEmpty) continue;
                if (!PassesLayer(entry.bx, entry.by)) continue;   // 分层渲染：只画当前层
                int tx = Current.OriginX + entry.bx;
                int ty = Current.OriginY + entry.by;
                if (tx < 0 || ty < 0 || tx > maxX || ty > maxY) continue;
                if (tx < vx0 || tx > vx1 || ty < vy0 || ty > vy1) continue;

                Tile t = Main.tile[tx, ty];
                bool wActive = t.active();
                bool wWall = t.wall != 0;
                bool wantTile = cell.type != 0;
                bool wantWall = cell.wall != 0;

                if (wantTile)
                {
                    if (!wActive)
                    {
                        // 平台这类“同 ID 多款式”的物块：蓝图没记录帧就不要猜款式
                        //（否则史莱姆平台会被画成木平台），只画白色外框提示。
                        bool noStyle = !cell.hasFrame
                            && cell.type < TileID.Sets.Platforms.Length
                            && TileID.Sets.Platforms[cell.type];
                        ops.Add(new DrawOp(tx, ty, B_MISS_TILE, 0, cell.type,
                            cell.hasFrame ? cell.frameX : -1, cell.hasFrame ? cell.frameY : -1, noStyle,
                            cell.slope, cell.halfBrick, cell.paint));
                        missTile++;
                    }
                    else if (t.type != cell.type)
                    {
                        // 该格已有别的方块：只画高光外框，不覆盖原方块贴图
                        ops.Add(new DrawOp(tx, ty, B_WRONG, 0, cell.type,
                            cell.hasFrame ? cell.frameX : -1, cell.hasFrame ? cell.frameY : -1, true,
                            cell.slope, cell.halfBrick, cell.paint));
                        wrong++;
                    }
                }
                if (wantWall)
                {
                    if (!wWall)
                    {
                        if (wActive) { ops.Add(new DrawOp(tx, ty, B_WRONG, 1, cell.wall,
                            cell.hasFrame ? cell.frameX : -1, cell.hasFrame ? cell.frameY : -1, true)); wrong++; }
                        else { ops.Add(new DrawOp(tx, ty, B_MISS_WALL, 1, cell.wall,
                            cell.hasFrame ? cell.frameX : -1, cell.hasFrame ? cell.frameY : -1, false,
                            0, false, cell.wallPaint)); missWall++; }
                    }
                    else if (t.wall != cell.wall)
                    {
                        ops.Add(new DrawOp(tx, ty, B_WRONG, 1, cell.wall,
                            cell.hasFrame ? cell.frameX : -1, cell.hasFrame ? cell.frameY : -1, true));
                        wrong++;
                    }
                }
            }

            // 多余扫描：蓝图区域 ∩ 视口 内，蓝图为空但世界有东西
            int extra = 0;
            if (Layer == LayerKind.All && Current.Width <= 128 && Current.Height <= 128)
            {
                int ex0 = Math.Max(vx0, Current.OriginX);
                int ey0 = Math.Max(vy0, Current.OriginY);
                int ex1 = Math.Min(vx1, Current.OriginX + Current.Width);
                int ey1 = Math.Min(vy1, Current.OriginY + Current.Height);
                for (int ty = ey0; ty <= ey1; ty++)
                {
                    if (ty < 0 || ty > maxY) continue;
                    for (int tx = ex0; tx <= ex1; tx++)
                    {
                        if (tx < 0 || tx > maxX) continue;
                        BpCell cell;
                        if (Current.TryGet(tx - Current.OriginX, ty - Current.OriginY, out cell) && !cell.IsEmpty)
                            continue;
                        Tile t = Main.tile[tx, ty];
                        if (t.active()) { ops.Add(new DrawOp(tx, ty, B_EXTRA, 0, t.type, -1, -1)); extra++; }
                        else if (t.wall != 0) { ops.Add(new DrawOp(tx, ty, B_EXTRA, 1, t.wall, -1, -1)); extra++; }
                    }
                }
            }

            if (ops.Count > 0)
            {
                try
                {
                    Texture2D px = Pixel(sb);

                    // 幽灵位置集合（用于判断轮廓：相邻格也是幽灵则不画共用边）
                    HashSet<long> ghostKeys = new HashSet<long>();
                    foreach (DrawOp op in ops) ghostKeys.Add(Pack(op.x, op.y));

                    // 第一层：方块贴图（85% 半透明）+ 深色衬底
                    sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                        DepthStencilState.None, RasterizerState.CullNone, null,
                        Main.GameViewMatrix.TransformationMatrix);

                    // 先画墙（背景层），再画方块（前景层）——顺序反了会把方块整个盖住
                    foreach (DrawOp op in ops) { if (op.texKind == 1) DrawCellTexture(sb, px, op); }
                    foreach (DrawOp op in ops) { if (op.texKind == 0) DrawCellTexture(sb, px, op); }
                    sb.End();

                    // 第二层：外围高光（加色混合 = 真发光感，只画外侧边）
                    sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp,
                        DepthStencilState.None, RasterizerState.CullNone, null,
                        Main.GameViewMatrix.TransformationMatrix);
                    foreach (DrawOp op in ops)
                    {
                        int dx = op.x * 16 - (int)Main.screenPosition.X;
                        int dy = op.y * 16 - (int)Main.screenPosition.Y;
                        Color core = new Color(op.border.R, op.border.G, op.border.B, 255);
                        Color glow = op.border * 0.32f;

                        // 错放格没有填充，必须画完整细框才看得见（且不覆盖原方块贴图）
                        if (op.outlineOnly)
                        {
                            sb.Draw(px, new Rectangle(dx, dy, 16, 2), core);
                            sb.Draw(px, new Rectangle(dx, dy + 14, 16, 2), core);
                            sb.Draw(px, new Rectangle(dx, dy, 2, 16), core);
                            sb.Draw(px, new Rectangle(dx + 14, dy, 2, 16), core);
                            continue;
                        }

                        bool hasUp    = ghostKeys.Contains(Pack(op.x, op.y - 1));
                        bool hasDown  = ghostKeys.Contains(Pack(op.x, op.y + 1));
                        bool hasLeft  = ghostKeys.Contains(Pack(op.x - 1, op.y));
                        bool hasRight = ghostKeys.Contains(Pack(op.x + 1, op.y));

                        if (!hasUp)
                        {
                            // 光晕与亮线都在方块【外侧】，不照进方块内部
                            sb.Draw(px, new Rectangle(dx - 3, dy - 6, 22, 6), glow);
                            sb.Draw(px, new Rectangle(dx, dy - 2, 16, 2), core);
                        }
                        if (!hasDown)
                        {
                            sb.Draw(px, new Rectangle(dx - 3, dy + 16, 22, 6), glow);
                            sb.Draw(px, new Rectangle(dx, dy + 16, 16, 2), core);
                        }
                        if (!hasLeft)
                        {
                            sb.Draw(px, new Rectangle(dx - 6, dy - 3, 6, 22), glow);
                            sb.Draw(px, new Rectangle(dx - 2, dy, 2, 16), core);
                        }
                        if (!hasRight)
                        {
                            sb.Draw(px, new Rectangle(dx + 16, dy - 3, 6, 22), glow);
                            sb.Draw(px, new Rectangle(dx + 16, dy, 2, 16), core);
                        }
                    }
                    sb.End();
                }
                catch (Exception e)
                {
                    // 绘制失败限频记录（失败多半是 spriteBatch 状态问题，不影响游戏）
                    if ((DateTime.UtcNow - _lastDrawErr).TotalSeconds >= 30)
                    {
                        _lastDrawErr = DateTime.UtcNow;
                        Bootstrap.Log("draw error: " + e.Message);
                    }
                }
            }

            // 电线 / 促动器缺失提示（叠画在方块之上）
            DrawWireOverlays(sb);

            // 选取标记点（木剑选取模式）
            DrawSelection();

            DateTime now = DateTime.UtcNow;
            if ((now - _lastLog).TotalSeconds >= 5)
            {
                _lastLog = now;
                Bootstrap.Log(string.Format(
                    "render: 差异 {0} 处（缺块 {1} / 缺墙 {2} / 错 {3} / 多 {4}）锚点 {5},{6}",
                    ops.Count, missTile, missWall, wrong, extra, Current.OriginX, Current.OriginY));
            }
        }

        /// <summary>材料清单 HUD（DrawInterface 的 postfix 调用，画在游戏 UI 之上）。</summary>
        public static void DrawHudOverlay()
        {
            if (Main.gameMenu) return;
            // 材料清单受 HUD 开关控制；提示与蓝图列表不受影响
            if (!HudEnabled && !BpPicker.Open && Layer == LayerKind.All &&
                !(Hint != null && DateTime.UtcNow < _hintUntil) && Selector.Info.Length == 0)
                return;
            try
            {
                _matTick++;
                if (_matTick >= 30) { _matTick = 0; RebuildMaterials(); }
                DrawHud(Main.spriteBatch);
            }
            catch (Exception e)
            {
                HudDiag("overlay 异常: " + e.GetType().Name + ": " + e.Message);
            }
        }

        /// <summary>选取标记：A 点青色高亮 + 光标十字准星（手持木剑或开启选取模式时显示）。</summary>
        private static void DrawSelection()
        {
            try
            {
                if (Main.gameMenu || Main.tile == null) return;

                Player pl = WorldUtil.LocalPlayer;
                Item held = WorldUtil.HeldItem(pl);
                bool wand = held != null && held.type == ItemID.WoodenSword;
                if (!wand) return;   // 仅木剑显示选取标记

                SpriteBatch sb = Main.spriteBatch;
                if (sb == null || sb.GraphicsDevice == null) return;
                Texture2D px = Pixel(sb);

                sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp,
                    DepthStencilState.None, RasterizerState.CullNone, null,
                    Main.GameViewMatrix.TransformationMatrix);

                if (Selector.HasA)
                    DrawPoint(sb, px, Selector.ax, Selector.ay, new Color(0, 255, 230));

                // 光标十字准星（帮助对准第二点）
                if (wand)
                {
                    Vector2 mw = Main.MouseWorld;
                    int mx = (int)(mw.X / 16f);
                    int my = (int)(mw.Y / 16f);
                    if (mx >= 0 && my >= 0 && mx <= Main.maxTilesX && my <= Main.maxTilesY)
                        DrawPoint(sb, px, mx, my, new Color(255, 255, 255));
                }
                sb.End();
            }
            catch { /* 标记失败静默 */ }
        }

        private static void DrawPoint(SpriteBatch sb, Texture2D px, int tx, int ty, Color c)
        {
            int dx = tx * 16 - (int)Main.screenPosition.X;
            int dy = ty * 16 - (int)Main.screenPosition.Y;
            Color core = new Color(c.R, c.G, c.B, 255);
            Color glow = c * 0.3f;
            sb.Draw(px, new Rectangle(dx - 8, dy - 8, 32, 32), glow);
            sb.Draw(px, new Rectangle(dx - 2, dy - 2, 20, 20), glow);
            sb.Draw(px, new Rectangle(dx, dy, 16, 2), core);
            sb.Draw(px, new Rectangle(dx, dy + 14, 16, 2), core);
            sb.Draw(px, new Rectangle(dx, dy, 2, 16), core);
            sb.Draw(px, new Rectangle(dx + 14, dy, 2, 16), core);
        }

        /// <summary>重建材料清单：按蓝图缺失格统计所需物品（约每 0.5 秒一次）。</summary>
        private static void RebuildMaterials()
        {
            try
            {
                EnsureItemMaps();
                _matLines.Clear();
                if (Current == null) return;
                Dictionary<int, int> counts = new Dictionary<int, int>();
                foreach (BpEntry e in Current.Entries)
                {
                    BpCell c = e.cell;
                    if (c.IsEmpty) continue;
                    Tile t;
                    if (!WorldUtil.TryTile(Current.OriginX + e.bx, Current.OriginY + e.by, out t)) continue;
                    int item = -1;
                    if (c.type != 0 && !t.active()) item = MapItem(_tileToItem, c.type);
                    else if (c.wall != 0 && t.wall == 0) item = MapItem(_wallToItem, c.wall);
                    if (item < 0) continue;
                    int n;
                    counts.TryGetValue(item, out n);
                    counts[item] = n + 1;
                }
                List<KeyValuePair<int, int>> sorted = new List<KeyValuePair<int, int>>(counts);

                // 扣掉背包 + 快捷栏已有数量（Item 是引用类型，直接读 stack）
                Player pl = WorldUtil.LocalPlayer;
                if (pl != null && pl.inventory != null)
                {
                    int inv = Math.Min(58, pl.inventory.Length);
                    for (int i = 0; i < inv; i++)
                    {
                        Item it = pl.inventory[i];
                        if (it == null || it.stack <= 0) continue;
                        int have;
                        if (!counts.TryGetValue(it.type, out have)) continue;
                        counts[it.type] = have - it.stack;
                    }
                }

                // 只保留仍缺的
                sorted.Clear();
                foreach (KeyValuePair<int, int> kv in counts)
                    if (kv.Value > 0) sorted.Add(kv);
                sorted.Sort(delegate(KeyValuePair<int, int> a, KeyValuePair<int, int> b)
                { return b.Value.CompareTo(a.Value); });
                for (int i = 0; i < sorted.Count && i < 6; i++)
                {
                    string name;
                    try { name = Lang.GetItemNameValue(sorted[i].Key); }
                    catch { name = "#" + sorted[i].Key; }
                    _matLines.Add(name + " x" + sorted[i].Value);
                }
                if (_matLines.Count == 0) _matLines.Add("材料齐全 ✓");
            }
            catch { _matLines.Clear(); }
        }

        /// <summary>构建 方块ID→物品ID / 墙ID→物品ID 反查表（用游戏内容表）。</summary>
        private static void EnsureItemMaps()
        {
            if (_tileToItem != null) return;
            _tileToItem = new Dictionary<int, int>();
            _wallToItem = new Dictionary<int, int>();
            try
            {
                FieldInfo f = Compat.Field(typeof(ContentSamples), "ItemsByType");
                IDictionary d = (IDictionary)f.GetValue(null);
                foreach (DictionaryEntry de in d)
                {
                    Item it = de.Value as Item;
                    if (it == null) continue;
                    if (it.createTile > 0 && !_tileToItem.ContainsKey(it.createTile))
                        _tileToItem[it.createTile] = it.type;
                    if (it.createWall > 0 && !_wallToItem.ContainsKey(it.createWall))
                        _wallToItem[it.createWall] = it.type;
                }
            }
            catch { /* 失败则 HUD 静默无内容 */ }
        }

        private static int MapItem(Dictionary<int, int> map, ushort key)
        {
            int item;
            if (map.TryGetValue(key, out item)) return item;
            return -1;
        }

        /// <summary>左上角绘制材料清单（用游戏自身的 Utils.DrawBorderString 文字绘制）。</summary>
        private static bool _hudDiagDone = false;

        private static void HudDiag(string msg)
        {
            if (_hudDiagDone) return;
            _hudDiagDone = true;
            Bootstrap.Log("HUD 诊断: " + msg);
        }

        private static void DrawHud(SpriteBatch sb)
        {
            try
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                    DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

                // 先算行数，画半透明底板（避免和游戏左上角的物品名文字糊在一起）
                int lines = 0;
                if (HudEnabled) lines += 1 + _matLines.Count;
                if (Selector.Info.Length > 0) lines++;
                bool showHint = Hint != null && DateTime.UtcNow < _hintUntil;
                if (showHint) lines++;
                if (Layer != LayerKind.All) lines++;
                if (BpPicker.Open) lines += 2 + BpPicker.Render().Length;

                int y = Math.Max(100, Main.screenHeight - (lines * 22 + 40));   // 左下角，避开快捷栏与小地图
                if (lines > 0)
                {
                    Texture2D px = Pixel(sb);
                    sb.Draw(px, new Rectangle(8, y - 8, 400, lines * 22 + 16), new Color(0, 0, 0, 130));
                }

                // 分层状态（不属于材料清单，始终显示）
                if (Layer != LayerKind.All)
                {
                    Utils.DrawBorderString(sb, LayerText, new Vector2(16, y), Color.Cyan, 1f, 0f, 0f, -1);
                    y += 22;
                }

                // 材料清单只在 HUD 开启时绘制；提示与蓝图列表始终可见
                if (HudEnabled)
                {
                    Utils.DrawBorderString(sb, "材料清单（还差·已扣背包）", new Vector2(16, y), Color.Yellow, 1f, 0f, 0f, -1);
                    y += 22;
                    foreach (string line in _matLines)
                    {
                        Utils.DrawBorderString(sb, line, new Vector2(16, y), Color.White, 1f, 0f, 0f, -1);
                        y += 22;
                    }
                }
                if (Selector.Info.Length > 0)
                {
                    Utils.DrawBorderString(sb, Selector.Info, new Vector2(16, y), Color.Cyan, 1f, 0f, 0f, -1);
                    y += 22;
                }
                if (showHint)
                {
                    Utils.DrawBorderString(sb, Hint, new Vector2(16, y), Color.Orange, 1f, 0f, 0f, -1);
                    y += 22;
                }

                // 蓝图管理列表（分页/改名/删除/搜索）
                if (BpPicker.Open)
                {
                    foreach (string line in BpPicker.Render())
                    {
                        Color c = Color.White;
                        if (line.StartsWith("▶")) c = Color.Yellow;
                        else if (line.StartsWith("蓝图列表")) c = Color.LightGreen;
                        else if (line.StartsWith("↑↓")) c = new Color(180, 220, 180);
                        else if (line.StartsWith("[改名]") || line.StartsWith("[搜索]")) c = Color.Cyan;
                        Utils.DrawBorderString(sb, line, new Vector2(16, y), c, 1f, 0f, 0f, -1);
                        y += 20;
                    }
                }
                sb.End();
                HudDiag("绘制成功, 行数=" + _matLines.Count);
            }
            catch (Exception e)
            {
                HudDiag("异常: " + e.GetType().Name + ": " + e.Message);
            }
        }

        private struct DrawOp
        {
            public int x;
            public int y;
            public Color border;   // 状态描边色
            public int texKind;    // 0 = 方块贴图, 1 = 墙贴图
            public int texType;
            public int frX;        // 蓝图记录的帧（-1 = 用校准帧）
            public int frY;
            public bool outlineOnly;   // 只描边不覆盖贴图（该格已有别的方块/墙）
            public byte slope;         // 0 无，1-4 斜坡
            public bool halfBrick;     // 半砖
            public byte paint;         // 漆色（0 = 无）

            public DrawOp(int x, int y, Color border, int texKind, int texType, int frX, int frY)
                : this(x, y, border, texKind, texType, frX, frY, false, 0, false, 0)
            {
            }

            public DrawOp(int x, int y, Color border, int texKind, int texType, int frX, int frY, bool outlineOnly)
                : this(x, y, border, texKind, texType, frX, frY, outlineOnly, 0, false, 0)
            {
            }

            public DrawOp(int x, int y, Color border, int texKind, int texType, int frX, int frY,
                bool outlineOnly, byte slope, bool halfBrick, byte paint)
            {
                this.x = x;
                this.y = y;
                this.border = border;
                this.texKind = texKind;
                this.texType = texType;
                this.frX = frX;
                this.frY = frY;
                this.outlineOnly = outlineOnly;
                this.slope = slope;
                this.halfBrick = halfBrick;
                this.paint = paint;
            }
        }
    }
}
