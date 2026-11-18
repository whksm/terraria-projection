// Core/BlockGroups.cs
// 相似物块分组：决定“放置限制”里哪些物块可以互相替换。

namespace Projection.Core;

/// <summary>
/// 相似物块组表。未分组的物块自成一个单元素组（只能放自己）。
/// </summary>
public sealed class BlockGroups
{
    readonly Dictionary<ushort, string> tileToGroup = new();
    readonly Dictionary<ushort, string> wallToGroup = new();
    readonly Dictionary<string, HashSet<ushort>> groupToTiles = new();

    /// <summary>注册一个“相似组”，例如 ("wood_any", 30, 240, ...) 表示这些木材可互替。</summary>
    public void RegisterTileGroup(string group, params ushort[] tileIds)
    {
        var set = groupToTiles.GetValueOrDefault(group) ?? new HashSet<ushort>();
        foreach (var id in tileIds)
        {
            tileToGroup[id] = group;
            set.Add(id);
        }
        groupToTiles[group] = set;
    }

    public void RegisterWallGroup(string group, params ushort[] wallIds)
    {
        foreach (var id in wallIds) wallToGroup[id] = group;
    }

    public string TileGroupOf(ushort tileId) =>
        tileToGroup.TryGetValue(tileId, out var g) ? g : $"tile:{tileId}";

    public string WallGroupOf(ushort wallId) =>
        wallToGroup.TryGetValue(wallId, out var g) ? g : $"wall:{wallId}";

    /// <summary>两个方块是否属于同一可互替组（含“完全相同”）。</summary>
    public bool TilesCompatible(ushort a, ushort b) => a == b || TileGroupOf(a) == TileGroupOf(b);
    public bool WallsCompatible(ushort a, ushort b) => a == b || WallGroupOf(a) == WallGroupOf(b);

    /// <summary>从 JSON 配置加载（Config/groups.json），让用户可自行增改分组。</summary>
    public static BlockGroups Load(string path) => new(); // TODO: 实现 JSON 反序列化
}

/*
 * 预置分组示例（可后续按方块 ID 补全，集中在 Config/groups.json，不硬编码进逻辑）：
 *
 *   wood_any    : 各类原木/木材
 *   stone_brick : 石砖/灰砖/红砖…
 *   platform_any: 各种平台
 *   door_any    : 各种门
 */
