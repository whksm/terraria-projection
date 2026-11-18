// Core/PlacementRestrictor.cs
// 放置限制（蓝图建造模式）：钩放置入口，只允许放蓝图相同/同类物块。纯客户端过滤，多人合法。

using Projection.Compat;
using Projection.Data;

namespace Projection.Core;

public enum EmptyCellPolicy { Allow, Deny }

public sealed class PlacementRestrictor
{
    readonly IGameAPI api;
    readonly BlockGroups groups;
    public bool Enabled { get; set; }
    public Blueprint? Current { get; set; }
    public int OriginX { get; set; }
    public int OriginY { get; set; }
    public EmptyCellPolicy EmptyPolicy { get; set; } = EmptyCellPolicy.Allow;

    public PlacementRestrictor(IGameAPI api, BlockGroups groups)
    {
        this.api = api;
        this.groups = groups;
    }

    /// <summary>
    /// 判断：手里物品（createTile/createWall）是否允许放在蓝图格 (wx, wy)。
    /// 由“放置入口 patch”在真正放置前调用；返回 false 则取消本次放置。
    /// </summary>
    public bool AllowPlace(int wx, int wy, SlotSnapshot held)
    {
        if (!Enabled || Current is null) return true;
        if (!Current.TryGetCell(wx - OriginX, wy - OriginY, out var cell))
            return true; // 蓝图区域外不限制

        if (cell.type != 0)   // 蓝图需要“方块”
        {
            if (held.createTile == 0) return false; // 手里不是方块
            return groups.TilesCompatible((ushort)cell.type, (ushort)held.createTile);
        }

        if (cell.wall != 0)   // 蓝图需要“墙”
        {
            if (held.createWall == 0) return false;
            return groups.WallsCompatible(cell.wall, (ushort)held.createWall);
        }

        // 蓝图该格为空
        return EmptyPolicy == EmptyCellPolicy.Allow;
    }

    /// <summary>
    /// 取消本次放置（由 patch 在 AllowPlace==false 时调用）：
    /// 清 controlUseItem，可选播放提示音，不生成/不消耗任何东西。
    /// </summary>
    public void CancelPlacement()
    {
        // TODO(version): player.controlUseItem = false;（或跳过 Player.PlaceThing）
    }
}
