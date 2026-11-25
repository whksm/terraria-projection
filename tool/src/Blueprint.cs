// Blueprint.cs —— 蓝图数据模型（预解析结构，避免每帧字符串解析）
// 文本格式: x,y,type,wall,frameX,frameY[,wire,actuator,slope,halfBrick,paint,wallPaint]
//   前 6 字段兼容旧蓝图；后 6 字段记录电线/促动器/斜坡/半砖/油漆（新保存的建筑都会带上）。
//   type=TileID（0=无方块） wall=WallID（0=无墙）
// 兼容 .NET Framework 4.0（C# 5 语法）。

using System;
using System.Collections.Generic;
using System.IO;

namespace ProjectionMod
{
    public struct BpCell
    {
        public ushort type;   // TileID，0 = 无方块
        public ushort wall;   // WallID，0 = 无墙
        public short frameX;  // 贴图帧（默认 -1 = 运行时校准）
        public short frameY;
        public bool hasFrame;

        public byte wire;      // 电线位 bit0..3 = 红/蓝/绿/黄
        public bool actuator;  // 促动器
        public byte slope;     // 0=无 1..4
        public bool halfBrick; // 半砖
        public byte paint;     // 方块漆
        public byte wallPaint; // 墙漆

        public bool IsEmpty { get { return type == 0 && wall == 0; } }
    }

    /// <summary>预解析条目：渲染/统计热路径直接遍历，不再 Split 字符串。</summary>
    public struct BpEntry
    {
        public int bx;
        public int by;
        public BpCell cell;
    }

    public sealed class Blueprint
    {
        public int Width;
        public int Height;
        public int OriginX;   // 世界坐标锚点（蓝图左上角）
        public int OriginY;

        /// <summary>按 "x,y" 键索引（用于随机查询）。</summary>
        public Dictionary<string, BpCell> Cells = new Dictionary<string, BpCell>();

        /// <summary>顺序条目表（渲染 / 统计遍历用）。</summary>
        public List<BpEntry> Entries = new List<BpEntry>();

        public static string Key(int x, int y)
        {
            return x.ToString() + "," + y.ToString();
        }

        /// <summary>加入一个格子（自动维护尺寸；重复键以先者为准）。</summary>
        public void Add(int bx, int by, BpCell cell)
        {
            string k = Key(bx, by);
            if (Cells.ContainsKey(k)) return;
            Cells[k] = cell;
            BpEntry e;
            e.bx = bx;
            e.by = by;
            e.cell = cell;
            Entries.Add(e);
            if (bx + 1 > Width) Width = bx + 1;
            if (by + 1 > Height) Height = by + 1;
        }

        /// <summary>按蓝图内坐标查询。</summary>
        public bool TryGet(int bx, int by, out BpCell cell)
        {
            return Cells.TryGetValue(Key(bx, by), out cell);
        }

        /// <summary>按世界坐标查询（自动做锚点偏移）。</summary>
        public bool TryGetWorld(int wx, int wy, out BpCell cell)
        {
            return TryGet(wx - OriginX, wy - OriginY, out cell);
        }

        /// <summary>从文本文件加载。</summary>
        public static Blueprint Load(string path)
        {
            Blueprint bp = new Blueprint();
            string[] lines = File.ReadAllLines(path);
            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                string[] p = line.Split(',');
                if (p.Length < 4) continue;
                try
                {
                    int x = int.Parse(p[0].Trim());
                    int y = int.Parse(p[1].Trim());
                    BpCell c;
                    c.type = ushort.Parse(p[2].Trim());
                    c.wall = ushort.Parse(p[3].Trim());
                    c.hasFrame = false;
                    c.frameX = -1;
                    c.frameY = -1;
                    c.wire = 0;
                    c.actuator = false;
                    c.slope = 0;
                    c.halfBrick = false;
                    c.paint = 0;
                    c.wallPaint = 0;
                    if (p.Length >= 6)
                    {
                        c.frameX = short.Parse(p[4].Trim());
                        c.frameY = short.Parse(p[5].Trim());
                        c.hasFrame = c.frameX >= 0 && c.frameY >= 0;
                    }
                    if (p.Length >= 12)
                    {
                        c.wire = byte.Parse(p[6].Trim());
                        c.actuator = p[7].Trim() != "0";
                        c.slope = byte.Parse(p[8].Trim());
                        c.halfBrick = p[9].Trim() != "0";
                        c.paint = byte.Parse(p[10].Trim());
                        c.wallPaint = byte.Parse(p[11].Trim());
                    }
                    bp.Add(x, y, c);
                }
                catch { /* 忽略坏行 */ }
            }
            return bp;
        }

        /// <summary>保存为文本文件（含帧与电线/促动器/斜坡/油漆）。</summary>
        public void Save(string path)
        {
            List<string> lines = new List<string>();
            foreach (BpEntry e in Entries)
            {
                BpCell c = e.cell;
                lines.Add(Key(e.bx, e.by) + "," + c.type + "," + c.wall +
                    "," + c.frameX + "," + c.frameY +
                    "," + c.wire + "," + (c.actuator ? 1 : 0) + "," + c.slope +
                    "," + (c.halfBrick ? 1 : 0) + "," + c.paint + "," + c.wallPaint);
            }
            File.WriteAllLines(path, lines.ToArray());
        }
    }
}
