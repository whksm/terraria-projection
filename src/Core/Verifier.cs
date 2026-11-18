// Core/Verifier.cs
// 蓝图验证：建造后核对是否放对。只读，不写世界 → 多人合法。

using Projection.Compat;
using Projection.Data;

namespace Projection.Core;

public sealed class VerifyReport
{
    public int Total { get; set; }
    public int Correct { get; set; }
    public int Missing { get; set; }
    public int Wrong { get; set; }
    public int Extra { get; set; }
    public List<Diff> Diffs { get; } = new();
    public bool IsPerfect => Wrong == 0 && Missing == 0 && Extra == 0;

    public override string ToString() =>
        $"正确 {Correct}/{Total}，缺失 {Missing}，错放 {Wrong}，多余 {Extra}" +
        (IsPerfect ? " —— 完美 ✓" : "");
}

public sealed class Verifier
{
    readonly IGameAPI api;
    public Verifier(IGameAPI api) => this.api = api;

    /// <summary>全区域校验（不限视口）。</summary>
    public VerifyReport Verify(Blueprint bp, int originX, int originY)
    {
        var r = new VerifyReport();

        // 蓝图内的格子：缺失 / 正确 / 错放
        foreach (var ((kx, ky), want) in bp.cells)
        {
            int wx = originX + kx, wy = originY + ky;
            r.Total++;
            var got = api.ReadTile(wx, wy);
            if (got.IsEmpty)                          { r.Missing++; r.Diffs.Add(new Diff(wx, wy, DiffKind.Missing, want, got)); }
            else if (got.SameBlockAs(want))            r.Correct++;
            else                                        { r.Wrong++;   r.Diffs.Add(new Diff(wx, wy, DiffKind.Wrong,   want, got)); }
        }

        // 蓝图区域内的“多余”块（蓝图为空、世界里却有东西）
        for (int y = 0; y < bp.height; y++)
        for (int x = 0; x < bp.width; x++)
        {
            if (bp.TryGetCell(x, y, out _)) continue;
            var got = api.ReadTile(originX + x, originY + y);
            if (!got.IsEmpty) { r.Extra++; r.Diffs.Add(new Diff(originX + x, originY + y, DiffKind.Extra, default, got)); }
        }
        return r;
    }

    /// <summary>把错放/缺失/多余的格子在世界里高亮（调用渲染层的 DrawGhost）。</summary>
    public void Highlight(VerifyReport report, ProjectionRenderer renderer)
    {
        foreach (var d in report.Diffs) renderer.DrawGhost(d);
    }
}
