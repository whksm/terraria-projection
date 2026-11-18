// Core/ProjectionRenderer.cs
// 投影渲染：差异计算 + 幽灵块绘制 + HUD。纯客户端，多人合法。
// 支持：液体显示开关（ShowLiquids）、分层（LayerRange，与打印机共享）。

using Projection.Compat;
using Projection.Data;

namespace Projection.Core;

public enum DiffKind { Missing, Wrong, Extra }

/// <summary>一格差异。</summary>
public readonly record struct Diff(int x, int y, DiffKind kind, TileCell want, TileCell got);

public sealed class ProjectionRenderer
{
    readonly IGameAPI api;
    public Blueprint? Current { get; set; }
    public int OriginX { get; set; }   // 蓝图原点对应的世界坐标
    public int OriginY { get; set; }

    /// <summary>是否显示液体格（默认开：只作提示，打印机仍不放置）。</summary>
    public bool ShowLiquids { get; set; } = true;

    /// <summary>分层显示（与打印机共享同一个 LayerRange）。null = 显示全部。</summary>
    public LayerRange? Layer { get; set; }

    public ProjectionRenderer(IGameAPI api) => this.api = api;

    /// <summary>判断格子是否“纯液体”（无方块无墙，只有液位）。</summary>
    public static bool IsLiquidOnly(in TileCell c) => c.type == 0 && c.wall == 0 && c.liquidAmount > 0;

    /// <summary>
    /// 计算蓝图区域与当前世界的差异（只对视口附近格子）。
    /// </summary>
    public IEnumerable<Diff> ComputeDiff()
    {
        if (Current is null) yield break;

        var (x0, y0, x1, y1) = VisibleRange();
        foreach (var ((kx, ky), want) in Current.cells)
        {
            int wx = OriginX + kx, wy = OriginY + ky;
            if (wx < x0 || wx > x1 || wy < y0 || wy > y1) continue;
            if (Layer is not null && !Layer.Contains(wx, wy)) continue;   // 分层：范围外不显示
            if (want.IsEmpty) continue;
            if (IsLiquidOnly(want) && !ShowLiquids) continue;             // 液体不显示开关

            var got = api.ReadTile(wx, wy);
            if (got.IsEmpty) { yield return new Diff(wx, wy, DiffKind.Missing, want, got); continue; }
            if (!got.SameBlockAs(want)) yield return new Diff(wx, wy, DiffKind.Wrong, want, got);
        }
    }

    /// <summary>视口范围（图格），用于裁剪。</summary>
    (int x0, int y0, int x1, int y1) VisibleRange()
    {
        var sp = api.ScreenPosition;
        var size = api.ScreenSize;
        var zoom = api.Zoom;
        // TODO(version): 精确换算依赖 GameViewMatrix，简化版：按 16px/格 + zoom 估算。
        int x0 = (int)(sp.X / 16f) - 2, y0 = (int)(sp.Y / 16f) - 2;
        int x1 = (int)((sp.X + size.X / zoom.X) / 16f) + 2;
        int y1 = (int)((sp.Y + size.Y / zoom.Y) / 16f) + 2;
        return (x0, y0, x1, y1);
    }

    /// <summary>
    /// 每帧在 Main.Draw 后调用（Harmony postfix on Main.DoDraw）。
    /// 用游戏自身的 spriteBatch + GameViewMatrix 画半透明幽灵块，天然对齐。
    /// </summary>
    public void Draw()
    {
        if (Current is null) return;
        foreach (var d in ComputeDiff())
            DrawGhost(d);
    }

    public void DrawGhost(Diff d)
    {
        // TODO(version): 世界坐标 → 屏幕坐标 → spriteBatch.Draw。
        //   screen = (worldPx - Main.screenPosition) * GameViewMatrix.Zoom
        //   缺：半透明白色方块（或 TextureAssets.Tile[want.type] 的实际纹理 + 半透明）
        //   错：红色描边    多：黄色描边    液体：蓝色半透明（ShowLiquids 时）
        // 纹理版本敏感，可选“纯色半透明矩形”作为无纹理降级方案（更稳）。
    }
}
