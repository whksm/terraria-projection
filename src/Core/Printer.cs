// Core/Printer.cs
// 打印机（可选模块）：自动放置。走游戏自身放置逻辑 → 消耗真实物品、遵守范围，非“无中生有”。
// 支持：放置开关（流体不放置/电线不处理）、分层放置（LayerRange）。多人默认禁用。

using Projection.Compat;
using Projection.Data;

namespace Projection.Core;

public sealed class Printer
{
    readonly IGameAPI api;
    readonly BlockGroups groups;
    public Blueprint? Current { get; set; }
    public int OriginX { get; set; }
    public int OriginY { get; set; }
    public bool Enabled { get; set; }

    /// <summary>放置开关 + 分层（流体不放置等）。</summary>
    public BuildOptions Options { get; set; } = new();

    /// <summary>是否允许在多人客户端运行（默认关）。</summary>
    public bool AllowInMultiplayer { get; set; }

    public Printer(IGameAPI api, BlockGroups groups)
    {
        this.api = api;
        this.groups = groups;
    }

    bool IsBlocked => api.IsMultiplayerClient && !AllowInMultiplayer;

    /// <summary>每帧在 Main.Update 后调用一次。</summary>
    public void Tick()
    {
        if (!Enabled || IsBlocked || Current is null) return;

        // 1. 找“最近、在放置范围内、已满足支撑”的待放格（受 BuildOptions / Layer 约束）
        var (wx, wy, cell) = FindNext();
        if (cell.IsEmpty) return;

        // 2. 从背包选匹配物品（含相似组替换）
        if (!SelectItem(cell, out _)) return; // 缺料 → 停，可提示

        // 3. 走到放置范围（可选：仅提示玩家移动；自动移动更接近“脚本”，先不做）
        // 4. 放置并验证消耗
        if (api.TryPlaceAt(wx, wy))
        {
            // 成功：格子变化/物品消耗，继续下一格
        }
        else
        {
            // 失败：重试有限次后跳过，标记该格“待人工”
        }
    }

    /// <summary>该格是否在当前 BuildOptions 下可被打印机处理。</summary>
    bool ShouldPlace(in TileCell c)
    {
        if (c.IsEmpty) return false;
        if (ProjectionRenderer.IsLiquidOnly(c) && !Options.PlaceLiquids) return false; // 流体不放置
        if (c.wire != 0 && !Options.PlaceWires) return false;                          // 电线需扳手
        if (c.type != 0 && !Options.PlaceTiles) return false;
        if (c.wall != 0 && !Options.PlaceWalls) return false;
        return true;
    }

    (int x, int y, TileCell cell) FindNext()
    {
        if (Current is null) return (0, 0, default);
        // TODO: 按“自下而上、先支撑后悬挂”的顺序遍历，
        //       取距离玩家最近且在放置范围内的格子；
        //       跳过 !ShouldPlace(cell) 的格子；Layer 不为空时跳过 Layer.Contains 之外的格子。
        return (0, 0, default);
    }

    bool SelectItem(TileCell cell, out int slot)
    {
        slot = -1;
        // 在热键栏（其次背包）找 createTile/createWall 与 cell 兼容的格子
        foreach (var s in api.Hotbar)
        {
            if (cell.type != 0 && s.createTile != 0 &&
                groups.TilesCompatible((ushort)cell.type, (ushort)s.createTile))
            { slot = s.itemType; return true; }
            if (cell.wall != 0 && s.createWall != 0 &&
                groups.WallsCompatible(cell.wall, (ushort)s.createWall))
            { slot = s.itemType; return true; }
        }
        return false;
    }
}
