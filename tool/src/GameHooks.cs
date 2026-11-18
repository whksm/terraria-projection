// GameHooks.cs —— Harmony 钩子与懒加载安装
//   Update        : 每帧主循环（热键/锚定/补料/远程放置）
//   DrawInterface : prefix 画投影、postfix 画 HUD
//   PlaceThing    : prefix 做放置限制
// 绘制/放置钩子在游戏完全启动后才懒加载（启动期打会破坏游戏的初始化时序）。
// 兼容 .NET Framework 4.0（C# 5 语法）。

using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;

namespace ProjectionMod
{
    public static class GameHooks
    {
        private static Harmony _harmony;
        private static bool _patched;
        private static DateTime _failLogTime = DateTime.MinValue;
        private static DateTime _tilePlaceLogTime = DateTime.MinValue;

        /// <summary>WorldGen.PlaceTile 结果诊断（限频 1 秒；参数名必须与游戏一致：i, j, Type）。</summary>
        private static void PlaceTileDiag(int i, int j, int Type, bool __result)
        {
            try
            {
                if (Renderer.Current == null) return;
                if ((DateTime.UtcNow - _tilePlaceLogTime).TotalSeconds < 1) return;
                _tilePlaceLogTime = DateTime.UtcNow;
                Bootstrap.Log("WorldGen.PlaceTile: (" + i + "," + j + ") type=" + Type +
                    " 结果=" + (__result ? "成功" : "失败"));
            }
            catch { }
        }

        public static void SetHarmony(Harmony h) { _harmony = h; }

        [HarmonyPatch(typeof(Main), "Update", new Type[] { typeof(GameTime) })]
        private static class UpdatePatch
        {
            private static void Postfix()
            {
                try
                {
                    if (!_patched && _harmony != null) LazilyInstall();
                    Core.Tick();
                }
                catch (Exception e) { Bootstrap.Log("update hook error: " + e.Message); }
            }
        }

        /// <summary>
        /// 绘制/放置钩子懒加载：等游戏完全启动（Update 循环在跑）后再打，
        /// 避免启动阶段干扰 CaptureManager 初始化。
        /// </summary>
        private static void LazilyInstall()
        {
            try
            {
                MethodInfo draw = AccessTools.Method(typeof(Main), "DrawInterface",
                    new Type[] { typeof(GameTime) });
                MethodInfo before = typeof(GameHooks).GetMethod("DrawBeforeUI",
                    BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo after = typeof(GameHooks).GetMethod("DrawAfterUI",
                    BindingFlags.NonPublic | BindingFlags.Static);
                if (draw == null || before == null || after == null)
                {
                    LogFail("绘制钩子未找到 draw=" + (draw != null) +
                        " before=" + (before != null) + " after=" + (after != null));
                    return;
                }
                _harmony.Patch(draw,
                    prefix: new HarmonyMethod(before),
                    postfix: new HarmonyMethod(after));

                // PlaceThing 的参数含内部类型 ItemCheckContext，无法在属性里引用，
                // 这里用无参 prefix 按签名查找。
                Type icc = typeof(Player).GetNestedType("ItemCheckContext",
                    BindingFlags.Public | BindingFlags.NonPublic);
                MethodInfo place = icc == null ? null : AccessTools.Method(typeof(Player), "PlaceThing",
                    new Type[] { typeof(bool), icc.MakeByRefType() });
                MethodInfo placePrefix = typeof(GameHooks).GetMethod("PlacePrefix",
                    BindingFlags.NonPublic | BindingFlags.Static);
                if (place != null && placePrefix != null)
                {
                    _harmony.Patch(place, prefix: new HarmonyMethod(placePrefix));
                    Bootstrap.Log("放置限制钩子已懒加载");
                }
                else
                {
                    Bootstrap.Log("放置限制钩子未挂上: place=" + (place != null));
                }

                // 诊断用：WorldGen.PlaceTile 的调用与结果（定位“点了没放上”的原因）
                MethodInfo tp = AccessTools.Method(typeof(Terraria.WorldGen), "PlaceTile", new Type[]
                {
                    typeof(int), typeof(int), typeof(int), typeof(bool), typeof(bool), typeof(int), typeof(int)
                });
                MethodInfo tpp = typeof(GameHooks).GetMethod("PlaceTileDiag",
                    BindingFlags.NonPublic | BindingFlags.Static);
                if (tp != null && tpp != null)
                {
                    _harmony.Patch(tp, postfix: new HarmonyMethod(tpp));
                    Bootstrap.Log("PlaceTile 诊断钩子已挂载");
                }

                _patched = true;
                Bootstrap.Log("绘制钩子已懒加载（DrawInterface prefix+postfix）");
                Compat.SelfCheck();   // 依赖成员自检，游戏更新后能从日志定位缺失项
            }
            catch (Exception e)
            {
                LogFail("钩子懒加载失败(下帧重试): " + e.Message);
            }
        }

        private static void LogFail(string msg)
        {
            if ((DateTime.UtcNow - _failLogTime).TotalSeconds >= 30)
            {
                _failLogTime = DateTime.UtcNow;
                Bootstrap.Log(msg);
            }
        }

        private static void DrawBeforeUI()
        {
            try { Renderer.Draw(); }
            catch (Exception e) { Bootstrap.Log("draw hook error: " + e.Message); }
        }

        private static void DrawAfterUI()
        {
            try { Renderer.DrawHudOverlay(); }
            catch (Exception e) { Bootstrap.Log("hud overlay error: " + e.Message); }
        }

        private static bool PlacePrefix()
        {
            try
            {
                LogPlaceGate();
                SnapPlace.Apply();                      // 吸附到投影缺失格
                if (!Restrictor.AllowPlace()) return false;

                // 无支撑放置：只解除“需要相邻支撑”的限制，材料照常消耗 1 个，节奏照常
                if (TryDirectPlace()) return false;
                return true;
            }
            catch { return true; }
        }

        private static DateTime _gateLogTime = DateTime.MinValue;

        /// <summary>放置钩子入口诊断（限频 1 秒；只记录“目标为空、本该放置”的情况，避免刷日志）。</summary>
        private static void LogPlaceGate()
        {
            if (Renderer.Current == null) return;
            if ((DateTime.UtcNow - _gateLogTime).TotalSeconds < 2) return;

            Player pl = WorldUtil.LocalPlayer;
            if (pl == null || !Main.mouseLeft) return;      // 只在按住左键时记录
            Tile st = WorldUtil.TileAt(Player.tileTargetX, Player.tileTargetY);
            if (st == null || st.active()) return;          // 只关心空目标格

            _gateLogTime = DateTime.UtcNow;
            Item held = WorldUtil.HeldItem(pl);
            Bootstrap.Log(string.Format(
                "放置判定: 目标=({0},{1}) 空 手持createTile={2} 动画={3}",
                Player.tileTargetX, Player.tileTargetY,
                held != null ? held.createTile : -1,
                pl.itemAnimation));
        }

        private static DateTime _directLogTime = DateTime.MinValue;

        /// <summary>
        /// 无支撑放置（消耗真实材料）：
        ///   按住左键（原版就是按住连续放）+ 物品冷却已好 + 目标为空 + 手长内
        ///   + 不压玩家 + 手持简单方块且 stack&gt;0 → 调 WorldGen.PlaceTile，扣 1 材料。
        /// 松开左键即停止；不操作时不会自发放置。
        /// </summary>
        private static bool TryDirectPlace()
        {
            // 输入闸门：必须按住左键
            if (!Main.mouseLeft) return false;

            Player pl = WorldUtil.LocalPlayer;
            if (pl == null || pl.dead) return false;
            // 注意：不要在动画/冷却期间提前返回——原版正是在“挥动中”调用 PlaceThing，
            // 之前按 itemAnimation 拦，会把每一次真实点击全部挡掉。
            // 连续放置的节奏由我们放置后写入的 itemAnimation 控制。

            Item held = WorldUtil.HeldItem(pl);
            if (held == null || held.createTile <= 0 || held.stack <= 0) return false;

            int tileType = held.createTile;
            if (tileType <= 0 || tileType >= Main.tileFrameImportant.Length) return false;

            // 多格带帧物块（火把/家具/平台…）不接管：
            // 无支撑强放会生成非法 tile，游戏的框架系统可能卡死（历史事故）。
            // 只对“简单方块”放宽支撑要求。
            if (Main.tileFrameImportant[tileType]) return false;

            int tx = Player.tileTargetX;
            int ty = Player.tileTargetY;
            Tile t;
            if (!WorldUtil.TryTile(tx, ty, out t)) return false;
            if (t.active()) return false;                          // 已占用 → 交给原版

            int boost = held.tileBoost + pl.blockRange;
            if (!pl.IsInTileInteractionRange(tx, ty, TileReachCheckSettings.Simple, boost)) return false;

            Rectangle body = new Rectangle((int)pl.position.X, (int)pl.position.Y, pl.width, pl.height);
            if (body.Intersects(new Rectangle(tx * 16, ty * 16, 16, 16))) return false;

            int useTime = held.useTime;
            int useAnim = held.useAnimation;

            bool placed = WorldGen.PlaceTile(tx, ty, tileType, false, false, pl.whoAmI, 0);
            if (!placed) return false;

            // —— 正常消耗与节奏 ——
            held.stack--;
            if (held.stack <= 0) held.SetDefaults(0);
            pl.itemTime = useTime;
            pl.itemAnimation = useAnim;

            if (Main.netMode == 1)
            {
                try { NetMessage.SendTileSquare(-1, tx, ty, Terraria.ID.TileChangeType.None); } catch { }
            }

            if ((DateTime.UtcNow - _directLogTime).TotalSeconds >= 1)
            {
                _directLogTime = DateTime.UtcNow;
                Bootstrap.Log("无支撑放置: (" + tx + "," + ty + ") type=" + tileType +
                    " 剩余材料=" + (held.stack > 0 ? held.stack.ToString() : "0"));
            }
            return true;
        }
    }
}
