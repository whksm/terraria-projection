// Core.cs —— 每帧编排：心跳 / 锚定 / 热键 / 精确放置 / 移动 / 自动补料
// 兼容 .NET Framework 4.0（C# 5 语法）。

using System;
using Microsoft.Xna.Framework.Input;
using Terraria;

namespace ProjectionMod
{
    public static class Core
    {
        private static DateTime _lastHeartbeat = DateTime.MinValue;
        private static int _worldTicks;
        private static bool _bpAttempted;
        private static int _moveCooldown;
        private static DateTime _moveLogTime = DateTime.MinValue;

        /// <summary>最近一次主线程 Tick 的时间（看门狗用）。</summary>
        public static DateTime LastTickUtc = DateTime.MinValue;

        public static void Tick()
        {
            LastTickUtc = DateTime.UtcNow;
            DateTime now = LastTickUtc;
            Heartbeat(now);
            AnchorTick();
            Hotkeys.Tick();
            BpPicker.Tick();
            Selector.Tick();
            MoveTick();
            AutoFetch.Tick();
            Printer.Tick();
        }

        private static void Heartbeat(DateTime now)
        {
            if ((now - _lastHeartbeat).TotalSeconds < 15) return;
            _lastHeartbeat = now;
            string tiles = Main.tile != null
                ? Main.tile.GetLength(0) + "x" + Main.tile.GetLength(1) : "null";
            Bootstrap.Log("heartbeat OK | tile=" + tiles +
                " | screenPos=" + Main.screenPosition.X.ToString("F0") + "," +
                Main.screenPosition.Y.ToString("F0") +
                " | zoom=" + Main.GameViewMatrix.Zoom.X.ToString("F2"));
        }

        /// <summary>
        /// 进世界后加载默认蓝图。等玩家真正落位（Entity.position 有效）再锚定，
        /// 保证蓝图出现在玩家身边而不是出生点。
        /// </summary>
        private static void AnchorTick()
        {
            if (Main.gameMenu)
            {
                _worldTicks = 0;
                _bpAttempted = false;   // 退到菜单后再进世界会重新加载默认蓝图
                return;
            }
            _worldTicks++;
            if (_bpAttempted) return;
            if (WorldUtil.LocalPlayer == null) return;

            int px, py;
            bool playerReady = WorldUtil.TryPlayerTile(out px, out py);
            // 世界加载可能十几秒：最多等 30 秒（1800 tick）让玩家真正落位
            if (!playerReady && _worldTicks < 1800) return;

            _bpAttempted = true;
            Player pl = WorldUtil.LocalPlayer;
            Bootstrap.Log(string.Format("锚点诊断: 就绪={0} 玩家 {1},{2} | active={3} pos={4:F0},{5:F0}",
                playerReady, px, py,
                pl != null && pl.active,
                pl != null ? pl.position.X : -1f,
                pl != null ? pl.position.Y : -1f));
            BlueprintOps.LoadDefault();
        }

        /// <summary>Ctrl + 方向键：移动蓝图锚点（Shift 每次 8 格）。</summary>
        private static void MoveTick()
        {
            if (Main.gameMenu) return;
            if (BpPicker.Open) return;   // 列表打开时方向键归列表用
            if (!Hotkeys.CtrlDown || Renderer.Current == null) return;
            if (_moveCooldown > 0) { _moveCooldown--; return; }

            KeyboardState ks = Hotkeys.State;
            bool shift = ks.IsKeyDown(Keys.LeftShift) || ks.IsKeyDown(Keys.RightShift);
            int step = shift ? 8 : 1;
            bool moved = false;
            if (ks.IsKeyDown(Keys.Left)) { Renderer.Current.OriginX -= step; moved = true; }
            if (ks.IsKeyDown(Keys.Right)) { Renderer.Current.OriginX += step; moved = true; }
            if (ks.IsKeyDown(Keys.Up)) { Renderer.Current.OriginY -= step; moved = true; }
            if (ks.IsKeyDown(Keys.Down)) { Renderer.Current.OriginY += step; moved = true; }
            if (!moved) return;

            _moveCooldown = 6; // 约 10 格/秒
            if ((DateTime.UtcNow - _moveLogTime).TotalSeconds >= 1)
            {
                _moveLogTime = DateTime.UtcNow;
                Bootstrap.Log("锚点移至 " + Renderer.Current.OriginX + "," + Renderer.Current.OriginY);
            }
        }
    }
}
