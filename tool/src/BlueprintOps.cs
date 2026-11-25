// BlueprintOps.cs —— 蓝图文件操作：加载/锚定/循环切换/保存建筑/验证
// 兼容 .NET Framework 4.0（C# 5 语法）。

using System;
using System.Collections.Generic;
using System.IO;
using Terraria;

namespace ProjectionMod
{
    public static class BlueprintOps
    {
        private static string _currentPath;

        /// <summary>记录当前蓝图文件路径（供循环切换定位）。</summary>
        public static void SetPath(string path) { _currentPath = path; }

        /// <summary>当前蓝图文件路径（无则 null）。</summary>
        public static string CurrentPath { get { return _currentPath; } }

        /// <summary>进世界后自动加载游戏目录下的 ProjectionMod.bp。</summary>
        public static void LoadDefault()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProjectionMod.bp");
            if (!File.Exists(path))
            {
                Bootstrap.Log("未找到 ProjectionMod.bp（不显示投影）");
                return;
            }
            LoadFile(path);
        }

        /// <summary>
        /// 加载蓝图并锚定到【玩家当前位置】附近（玩家未就位时退回世界出生点）。
        /// </summary>
        public static void LoadFile(string path)
        {
            try
            {
                Blueprint bp = Blueprint.Load(path);

                int px, py;
                if (WorldUtil.TryPlayerTile(out px, out py))
                {
                    bp.OriginX = px + 2;
                    bp.OriginY = py - 3;
                }
                else
                {
                    bp.OriginX = Main.spawnTileX + 2;
                    bp.OriginY = Main.spawnTileY - 3;
                }

                Renderer.Current = bp;
                Renderer.Calibrate(bp); // 无帧数据的格子从世界采样校准
                _currentPath = path;
                Bootstrap.Log("蓝图已加载: " + Path.GetFileName(path) + " | " + bp.Cells.Count +
                    " 格 | 尺寸 " + bp.Width + "x" + bp.Height +
                    " | 锚点 " + bp.OriginX + "," + bp.OriginY);
            }
            catch (Exception e)
            {
                Bootstrap.Log("蓝图加载失败: " + e.Message);
            }
        }

        /// <summary>在游戏目录与 Blueprints 目录的 .bp 之间循环切换（跳过空蓝图）。</summary>
        public static void Cycle()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                List<string> files = new List<string>();
                string[] root = Directory.GetFiles(baseDir, "*.bp");
                string bdir = Path.Combine(baseDir, "Blueprints");
                string[] lib = Directory.Exists(bdir) ? Directory.GetFiles(bdir, "*.bp") : new string[0];
                Array.Sort(root);
                Array.Sort(lib);
                files.AddRange(root);
                files.AddRange(lib);
                if (files.Count == 0)
                {
                    Bootstrap.Log("切换蓝图: 没有 .bp 文件");
                    return;
                }

                int idx = -1;
                if (_currentPath != null)
                {
                    for (int i = 0; i < files.Count; i++)
                        if (string.Compare(files[i], _currentPath, true) == 0) { idx = i; break; }
                }

                for (int tries = 0; tries < files.Count; tries++)
                {
                    idx = (idx + 1) % files.Count;
                    Blueprint probe = Blueprint.Load(files[idx]);
                    if (probe.Cells.Count == 0)
                    {
                        Bootstrap.Log("切换蓝图: 跳过空蓝图 " + Path.GetFileName(files[idx]));
                        continue;
                    }
                    LoadFile(files[idx]);
                    return;
                }
                Bootstrap.Log("切换蓝图: 没有可用的非空蓝图");
            }
            catch (Exception e)
            {
                Bootstrap.Log("切换蓝图失败: " + e.Message);
            }
        }

        /// <summary>把当前蓝图区域内的真实建筑保存为新蓝图（含贴图帧）。</summary>
        public static void Capture()
        {
            try
            {
                Blueprint bp = Renderer.Current;
                if (bp == null) { Bootstrap.Log("保存建筑: 未加载蓝图"); return; }

                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Blueprints");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                // 自动编号：building_01.bp、building_02.bp…（可积累多张）
                string path = null;
                for (int i = 1; i <= 99; i++)
                {
                    string candidate = Path.Combine(dir, "building_" + i.ToString("00") + ".bp");
                    if (!File.Exists(candidate)) { path = candidate; break; }
                }
                if (path == null)
                    path = Path.Combine(dir, "building_" + DateTime.Now.ToString("HHmmss") + ".bp");

                Blueprint outBp = new Blueprint();
                int saved = 0;
                for (int y = 0; y < bp.Height; y++)
                {
                    for (int x = 0; x < bp.Width; x++)
                    {
                        Tile t;
                        if (!WorldUtil.TryTile(bp.OriginX + x, bp.OriginY + y, out t)) continue;
                        if (!WorldUtil.HasContent(t)) continue;
                        outBp.Add(x, y, WorldUtil.CellFromTile(t));
                        saved++;
                    }
                }
                if (saved == 0)
                {
                    Bootstrap.Log("保存建筑: 该区域还没有建筑，未保存");
                    Renderer.SetHint("保存建筑：该区域没有建筑");
                    return;
                }
                outBp.Save(path);
                Bootstrap.Log("已保存建筑蓝图: " + path + " (" + saved + " 格)");
                Renderer.SetHint("已保存建筑蓝图：" + Path.GetFileName(path) + "（" + saved + " 格）");
            }
            catch (Exception e)
            {
                Bootstrap.Log("保存建筑失败: " + e.Message);
            }
        }

        /// <summary>全区域验证：正确/缺失/错放/多余 + 前 10 个问题坐标。</summary>
        public static void Verify()
        {
            try
            {
                Blueprint bp = Renderer.Current;
                if (bp == null) { Bootstrap.Log("验证: 未加载蓝图"); return; }

                int total = 0, ok = 0, miss = 0, wrong = 0, extra = 0, shapeWrong = 0, missWall = 0;
                List<string> issues = new List<string>();

                foreach (BpEntry e in bp.Entries)
                {
                    BpCell cell = e.cell;
                    if (cell.IsEmpty) continue;
                    total++;
                    int wx = bp.OriginX + e.bx;
                    int wy = bp.OriginY + e.by;
                    Tile t;
                    if (!WorldUtil.TryTile(wx, wy, out t)) { miss++; if (cell.wall != 0) missWall++; continue; }
                    bool missBlock = cell.type != 0 && !t.active();
                    bool missWallCell = cell.wall != 0 && t.wall == 0;
                    if (missWallCell) missWall++;
                    if (missBlock || missWallCell) { miss++; continue; }
                    if ((cell.type != 0 && t.type != cell.type) ||
                        (cell.wall != 0 && t.wall != cell.wall))
                    {
                        wrong++;
                        if (issues.Count < 10)
                            issues.Add(string.Format("({0},{1}) 应 块{2}/墙{3} 实际 块{4}/墙{5}",
                                wx, wy, cell.type, cell.wall,
                                t.active() ? t.type.ToString() : "空", t.wall));
                        continue;
                    }

                    bool shapeBad = t.slope() != cell.slope || t.halfBrick() != cell.halfBrick;
                    bool frameBad = WorldUtil.IsPlatform(cell.type) && cell.hasFrame &&
                        (t.frameX != cell.frameX || t.frameY != cell.frameY);
                    if (shapeBad || frameBad)
                    {
                        wrong++;
                        shapeWrong++;
                        if (issues.Count < 10)
                            issues.Add(string.Format(
                                "({0},{1}) 形状/帧不符: slope {2}->{3}, half {4}->{5}, frame ({6},{7})->({8},{9})",
                                wx, wy, cell.slope, t.slope(),
                                cell.halfBrick ? 1 : 0, t.halfBrick() ? 1 : 0,
                                cell.frameX, cell.frameY, t.frameX, t.frameY));
                    }
                    else ok++;
                }

                // 多余：蓝图区域内、蓝图为空但世界有东西
                if (bp.Width <= 128 && bp.Height <= 128)
                {
                    for (int y = 0; y < bp.Height; y++)
                    {
                        for (int x = 0; x < bp.Width; x++)
                        {
                            BpCell c;
                            if (bp.TryGet(x, y, out c) && !c.IsEmpty) continue;
                            Tile t;
                            if (!WorldUtil.TryTile(bp.OriginX + x, bp.OriginY + y, out t)) continue;
                            if (t.active() || t.wall != 0) extra++;
                        }
                    }
                }

                Bootstrap.Log(string.Format("验证: 正确 {0}/{1} | 缺失 {2}（缺墙 {6}） | 错误 {3}（形状/帧 {4}） | 多余 {5}",
                    ok, total, miss, wrong, shapeWrong, extra, missWall));
                foreach (string s in issues) Bootstrap.Log("  问题: " + s);
                Bootstrap.Log(wrong == 0 && miss == 0 && extra == 0 ? "验证: 完美 ✓" : "验证: 存在问题，见上");
            }
            catch (Exception e)
            {
                Bootstrap.Log("验证失败: " + e.Message);
            }
        }
    }
}
