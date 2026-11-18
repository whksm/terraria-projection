// Hotkeys.cs —— 热键表（小键盘 NumPad，避开游戏调试键）
// 只在按下瞬间触发一次（边缘检测）。
// 兼容 .NET Framework 4.0（C# 5 语法）。

using System;
using Microsoft.Xna.Framework.Input;
using Terraria;

namespace ProjectionMod
{
    public static class Hotkeys
    {
        private static KeyboardState _ks;
        private static bool _ctrl;

        private static bool _p0, _p1, _p2, _p3, _p4, _p5, _p6, _p7, _p9;
        private static bool _pL, _pPgUp, _pPgDn;
        private static bool _pUp, _pDown;

        public static KeyboardState State { get { return _ks; } }
        public static bool CtrlDown { get { return _ctrl; } }

        public static void Tick()
        {
            _ks = Keyboard.GetState();
            _ctrl = _ks.IsKeyDown(Keys.LeftControl) || _ks.IsKeyDown(Keys.RightControl);
            if (Main.gameMenu) return;

            Edge(Keys.NumPad0, ref _p0, ToggleProjection);
            Edge(Keys.NumPad1, ref _p1, ToggleAutoFetch);
            Edge(Keys.NumPad2, ref _p2, ToggleHud);
            Edge(Keys.NumPad3, ref _p3, BlueprintOps.Verify);
            Edge(Keys.NumPad4, ref _p4, CycleRestrictMode);
            Edge(Keys.NumPad5, ref _p5, BlueprintOps.Capture);
            Edge(Keys.NumPad6, ref _p6, BlueprintOps.Cycle);
            Edge(Keys.NumPad7, ref _p7, TogglePrinter);
            Edge(Keys.NumPad9, ref _p9, BpPicker.Toggle);

            // 分层渲染：L 切模式，PgUp/PgDn 调层，Shift+PgUp/PgDn 调范围
            // （蓝图列表打开/输入中时，这些键交给列表用，避免翻层与翻页打架）
            if (!BpPicker.Open)
            {
                Edge(Keys.L, ref _pL, Renderer.CycleLayer);
                if (_ks.IsKeyDown(Keys.LeftShift) || _ks.IsKeyDown(Keys.RightShift))
                {
                    Edge(Keys.PageUp, ref _pPgUp, delegate { Renderer.ChangeThickness(1); });
                    Edge(Keys.PageDown, ref _pPgDn, delegate { Renderer.ChangeThickness(-1); });
                }
                else
                {
                    Edge(Keys.PageUp, ref _pPgUp, delegate { Renderer.MoveLayer(1); });
                    Edge(Keys.PageDown, ref _pPgDn, delegate { Renderer.MoveLayer(-1); });
                }
            }
            else
            {
                _pL = _ks.IsKeyDown(Keys.L);
                _pPgUp = _ks.IsKeyDown(Keys.PageUp);
                _pPgDn = _ks.IsKeyDown(Keys.PageDown);
            }

            // 蓝图列表打开时，↑↓ 切换蓝图（优先于 Ctrl+方向键移动）
            if (BpPicker.Open)
            {
                Edge(Keys.Up, ref _pUp, delegate { BpPicker.Move(-1); });
                Edge(Keys.Down, ref _pDown, delegate { BpPicker.Move(1); });
            }
            else
            {
                _pUp = _ks.IsKeyDown(Keys.Up);
                _pDown = _ks.IsKeyDown(Keys.Down);
            }
        }

        private static void Edge(Keys key, ref bool prev, Action action)
        {
            bool down = _ks.IsKeyDown(key);
            if (down && !prev) action();
            prev = down;
        }

        private static void ToggleProjection()
        {
            Renderer.Enabled = !Renderer.Enabled;
            Bootstrap.Log("投影显示: " + (Renderer.Enabled ? "开启" : "关闭"));
        }

        private static void ToggleAutoFetch()
        {
            AutoFetch.Enabled = !AutoFetch.Enabled;
            Bootstrap.Log("自动补料: " + (AutoFetch.Enabled ? "开启" : "关闭"));
        }

        private static void ToggleHud()
        {
            Renderer.HudEnabled = !Renderer.HudEnabled;
            Bootstrap.Log("材料清单 HUD: " + (Renderer.HudEnabled ? "开启" : "关闭"));
        }

        private static void CycleRestrictMode()
        {
            Restrictor.Mode = (Restrictor.Mode + 1) % 3;
            string name = Restrictor.Mode == 0 ? "关闭" :
                (Restrictor.Mode == 1 ? "标准（空位放行）" : "严格（空位也禁止）");
            Bootstrap.Log("蓝图建造模式: " + name);
            Renderer.SetHint("蓝图建造模式: " + name);
        }
        private static void TogglePrinter()
        {
            Printer.Enabled = !Printer.Enabled;
            Bootstrap.Log("打印机: " + (Printer.Enabled ? "开启（单机自动放置）" : "关闭"));
            Renderer.SetHint("打印机: " + (Printer.Enabled ? "开启" : "关闭"));
        }
    }
}
