// Core/BuildOptions.cs
// 打印机放置开关 + 分层 LayerRange。投影与打印机共享 LayerRange。

namespace Projection.Core;

/// <summary>
/// 分层范围：限制“显示/可打印”的区域。2D 下默认按 y（水平带）分层，可加 x 区间做子选区。
/// </summary>
public sealed class LayerRange
{
    public int YMin { get; set; }
    public int YMax { get; set; }
    public int? XMin { get; set; }
    public int? XMax { get; set; }
    public int Step { get; set; } = 1;   // 层高/步进

    public bool Contains(int x, int y) =>
        y >= YMin && y <= YMax &&
        (!XMin.HasValue || x >= XMin.Value) &&
        (!XMax.HasValue || x <= XMax.Value);

    public void MoveUp()   { YMin += Step; YMax += Step; }
    public void MoveDown() { YMin -= Step; YMax -= Step; }
}

/// <summary>打印机放置开关（对应“流体不放置”“电线不处理”等）。</summary>
public sealed class BuildOptions
{
    public bool PlaceTiles { get; set; } = true;
    public bool PlaceWalls { get; set; } = true;
    public bool PlaceLiquids { get; set; } = false; // 流体不放置（默认）
    public bool PlaceWires { get; set; } = false;   // 电线需扳手，v1 默认不处理
    public LayerRange? Layer { get; set; }          // 分层放置
}
