// Compat/IGameAPI.cs
// 兼容层：唯一“碰游戏内部”的地方。业务层（投影/限制/打印机）只依赖本接口与中立结构。
// 目标：游戏更新时，绝大多数改动只发生在这里的“名称映射”，不动业务逻辑。
//
// 注意：本文件为参考骨架，未针对具体游戏版本编译。
//       标记 TODO(version) 的地方是版本敏感点，集中在实现类 HarmonyGameAPI 中。

namespace Projection.Compat;

/// <summary>与游戏内部对话的最小、版本无关接口。</summary>
public interface IGameAPI
{
    // ---------- 世界数据 ----------
    int TileCountX { get; }
    int TileCountY { get; }

    /// <summary>把游戏内的 Tile 转成中立结构（业务层不依赖 Terraria 的 Tile 类型）。</summary>
    TileCell ReadTile(int x, int y);

    // ---------- 相机 ----------
    /// <summary>Main.screenPosition —— 屏幕左上角对应的世界坐标（像素）。</summary>
    Vector2 ScreenPosition { get; }

    /// <summary>Main.GameViewMatrix.Zoom —— 缩放（UI 缩放另算）。</summary>
    Vector2 Zoom { get; }

    /// <summary>屏幕坐标 -> 图格坐标（裁剪/命中用）。</summary>
    (int x, int y) ScreenToTile(Vector2 screen);

    /// <summary>屏幕可见尺寸（像素），用于投影裁剪。</summary>
    Vector2 ScreenSize { get; }

    // ---------- 玩家 ----------
    /// <summary>Main.LocalPlayer.position（像素）。</summary>
    Vector2 PlayerPosition { get; }

    /// <summary>热键栏快照（物品 type / createTile / createWall / stack）。</summary>
    IReadOnlyList<SlotSnapshot> Hotbar { get; }
    int SelectedHotbarIndex { get; set; }

    /// <summary>设置 Main.mouseWorld（打印机用来指定放置目标）。</summary>
    void SetMouseWorld(Vector2 world);

    // ---------- 放置（打印机走游戏自身逻辑 = 合法消耗）----------
    /// <summary>
    /// 在 (x,y) 放置当前手持物品：设 tileTarget 后调 Player.PlaceThing。
    /// 返回是否“本次放置被接受”（成功消耗/格子变化）。
    /// </summary>
    bool TryPlaceAt(int x, int y);

    /// <summary>是否处于多人客户端（打印机默认禁用的依据）。</summary>
    bool IsMultiplayerClient { get; }

    // ---------- 版本 ----------
    string GameVersion { get; }
}

/// <summary>热键栏一格的中立快照。</summary>
public readonly record struct SlotSnapshot(
    int itemType,
    int createTile,   // Item.createTile
    int createWall,   // Item.createWall
    int stack,
    int tileWand);    // Item.tileWand，-1 表示无魔杖

/// <summary>轻量二维向量（避免业务层依赖 XNA/FNA）。</summary>
public readonly record struct Vector2(float X, float Y)
{
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator *(Vector2 a, float s) => new(a.X * s, a.Y * s);
}

/*
 * 实现类参考（HarmonyGameAPI）—— TODO(version)：以下字段名是版本敏感点，集中维护。

    public sealed class HarmonyGameAPI : IGameAPI
    {
        // 用反射按“名称”拿字段，缓存 FieldInfo；字段改名只改这里的映射。
        static readonly FieldInfo fTile   = typeof(Main).GetField("tile");
        static readonly FieldInfo fScreen = typeof(Main).GetField("screenPosition");
        static readonly FieldInfo fLocal  = typeof(Main).GetField("LocalPlayer");
        static readonly PropertyInfo pView = typeof(Main).GetProperty("GameViewMatrix");

        public TileCell ReadTile(int x, int y) {
            var tiles = (Tile[,])fTile.GetValue(null);
            var t = tiles[x, y];
            return TileCell.FromTile(t);
        }
        public Vector2 ScreenPosition { get {
            var v = (Microsoft.Xna.Framework.Vector2)fScreen.GetValue(null);
            return new Vector2(v.X, v.Y);
        } }
        public bool TryPlaceAt(int x, int y) {
            // TODO(version): 设置 Player.tileTargetX/Y + Main.mouseWorld，再调 Player.PlaceThing()
            // 走游戏自身逻辑 → 范围/支撑/消耗都被校验，非“无中生有”。
            throw new NotImplementedException();
        }
        // ...
    }
 */
