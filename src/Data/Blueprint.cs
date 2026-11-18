// Data/Blueprint.cs
// 蓝图数据模型：中立、可序列化、带版本号。业务层统一用这个结构，不依赖游戏类型。

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Projection.Data;

/// <summary>一个格子的中立描述（覆盖方块/墙/漆/斜坡/促动器/液体/电线）。</summary>
public struct TileCell : IEquatable<TileCell>
{
    public ushort type;        // TileID，0 = 无实体方块
    public bool active;        // 是否有实体方块
    public short frameX;       // 家具/多格物体的 frame
    public short frameY;
    public ushort wall;        // WallID
    public byte paint;         // 方块漆（Tile.color()）
    public byte wallPaint;     // 墙漆
    public byte slope;         // 0 = 无，1..4 = 斜坡
    public bool halfBrick;
    public bool actuated;      // inActive()
    public byte liquid;        // liquidType：0 水 / 1 熔岩 / 2 蜂蜜
    public byte liquidAmount;
    public byte wire;          // 电线位标志（1/2/3/4）

    public bool IsEmpty => type == 0 && wall == 0 && liquidAmount == 0 && wire == 0;

    /// <summary>“同一物块”比较。单格物块忽略 frame（frame 无意义），多格物块才比较。</summary>
    public bool SameBlockAs(in TileCell o)
    {
        if (type != o.type || wall != o.wall) return false;
        // 只有 frame 有意义的物块（家具/多格）才比较 frame；这里保守处理为“类型相同即可”。
        return true;
    }

    public bool Equals(TileCell o) =>
        type == o.type && active == o.active && frameX == o.frameX && frameY == o.frameY &&
        wall == o.wall && paint == o.paint && wallPaint == o.wallPaint &&
        slope == o.slope && halfBrick == o.halfBrick && actuated == o.actuated &&
        liquid == o.liquid && liquidAmount == o.liquidAmount && wire == o.wire;
}

/// <summary>蓝图：稀疏存储非空格子，带版本号与相似组。</summary>
public sealed class Blueprint
{
    public const int CurrentFormatVersion = 1;

    public int formatVersion { get; set; } = CurrentFormatVersion;
    public int width { get; set; }
    public int height { get; set; }

    /// <summary>非空格子表（世界坐标或相对原点的偏移均可，由 origin 约定）。</summary>
    public Dictionary<string, TileCell> cells { get; set; } = new();

    /// <summary>相似物块组：tileId -> 组名（如 "stone_brick" / "wood_any"）。</summary>
    public Dictionary<ushort, string> tileGroups { get; set; } = new();

    /// <summary>相似墙组。</summary>
    public Dictionary<ushort, string> wallGroups { get; set; } = new();

    // ---- 序列化 ----
    public static Blueprint Load(string path)
    {
        var json = File.ReadAllText(path);
        var bp = JsonSerializer.Deserialize<Blueprint>(json)
                 ?? throw new InvalidDataException("蓝图解析失败");
        if (bp.formatVersion > CurrentFormatVersion)
            throw new InvalidDataException($"蓝图版本 {bp.formatVersion} 高于当前支持版本");
        return bp;
    }

    public void Save(string path)
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    // ---- 查询 ----
    static string Key(int x, int y) => $"{x},{y}";

    public bool TryGetCell(int x, int y, out TileCell cell) =>
        cells.TryGetValue(Key(x, y), out cell);
}

/*
 * 从当前世界扫描生成蓝图（“复制建筑”），放在实现层（依赖 IGameAPI.ReadTile）：
 *
 *   public static Blueprint FromWorld(IGameAPI api, int x0, int y0, int w, int h)
 *   {
 *       var bp = new Blueprint { width = w, height = h };
 *       for (int y = 0; y < h; y++)
 *       for (int x = 0; x < w; x++)
 *       {
 *           var c = api.ReadTile(x0 + x, y0 + y);
 *           if (!c.IsEmpty) bp.cells[$"{x},{y}"] = c;
 *       }
 *       return bp;
 *   }
 */
