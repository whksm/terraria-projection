# 泰拉瑞亚原版「投影 + 打印机」设计文档

> 目标：在原版 Terraria（非 tModLoader）里实现类似 Minecraft **Litematica / Schematica** 的功能——
> ① 投影（全息显示待建蓝图）；② 放置限制（只允许放蓝图相同 / 同类物块）；③ 打印机（自动放置，可选）。
> 铁律：**不无中生有**。打印机必须走游戏自身放置逻辑、消耗真实背包物品、遵守放置范围。

---

## 1. 总体原则

1. **投影 / 限制 / 打印机 三个模块彻底解耦**，各自独立开关。
2. **投影 + 放置限制 = 纯客户端、多人合法**；打印机默认在多人禁用。
3. **兼容性优先**：薄钩面 + 兼容层，版本更新只改映射表，不改业务逻辑。

---

## 2. 兼容性策略（本项目核心）

### 2.1 为什么版本更新不该是灾难
- Terraria 是 .NET / FNA，**未混淆**，公共字段/方法名在官方反编译中稳定多年。
- 我们需要的东西（`Main.tile`、`Main.screenPosition`、`Main.LocalPlayer`、
  `Main.GameViewMatrix`、`Player.PlaceThing`）都属于长期稳定的公共 API。

### 2.2 薄钩面
只用 Harmony 打下面几个**公共**方法：

| 钩子 | 用途 | 稳定性 |
|---|---|---|
| `Main.Draw` / `Main.DoDraw` | 投影渲染 | 高 |
| `Main.DoUpdate` / `Main.Update` | 打印机 tick、放置限制 | 高 |
| 放置入口（`Player.PlaceThing` 或输入层） | 放置限制 / 打印机 | 高（入口名偶有微调） |

> 不钩任何私有方法、不碰 IL 偏移（Harmony 按“方法签名”打补丁，不是按地址）。

### 2.3 兼容层 IGameAPI
所有对游戏内部的读写收敛到 `IGameAPI`（见 `src/IGameAPI.cs`），业务层只依赖中立结构。
游戏更新时，绝大多数改动发生在这一层：
- 字段改名 → 改字段名映射表；
- 方法签名变 → 改对应 patch 的签名；
- 其余（投影、蓝图、分组、打印逻辑）**不动**。

### 2.4 注入底座选择
- **推荐：运行时 Harmony 加载器**（模型参考 [tPlainModLoader](https://github.com/github-user-64/tPlainModLoader)，
  自写约几百行，或 fork 更新到当前版本）。游戏更新只需重跑 patch 签名检查脚本。
- 不推荐：TerraAngel 式“反编译 + 重编译 + diff”——功能强但每次更新要重新反编译，迭代重。

### 2.5 蓝图格式自带版本
JSON 顶层带 `formatVersion`，字段可选/可扩展，旧蓝图可升级。

---

## 3. 模块划分

```
ProjectionMod.dll
├─ Compat/IGameAPI.cs        # 兼容层（唯一碰游戏内部的地方）
├─ Data/Blueprint.cs         # 蓝图数据模型 + 序列化 + 从世界扫描
├─ Core/ProjectionRenderer.cs# 差异计算 + 幽灵块渲染 + HUD
├─ Core/PlacementRestrictor.cs# 放置限制（蓝图建造模式）
├─ Core/Verifier.cs          # 蓝图验证（只读）
├─ Core/Printer.cs           # 打印机（可选、多人默认关）
├─ Core/BuildOptions.cs      # 放置开关 + 分层 LayerRange
└─ Core/BlockGroups.cs       # 相似物块分组
```

---

## 4. 蓝图数据模型

见 `src/Blueprint.cs`。要点：

- 稀疏存储：只存非空格，`Dictionary<(int x,int y), TileCell>`。
- `TileCell` 覆盖：`type / active / frameX / frameY / wall / paint / wallPaint / slope /
  halfBrick / actuated / liquid / liquidAmount / wire`。
- 扩展字段（箱子内容、牌子文字等）走可选 `extras`，避免破坏旧格式。

### 相似组（放置限制的关键）
- `TileGroups : Dictionary<ushort, string>`，把 tileId 归到语义组（如 `stone_brick`、`wood_any`）。
- 未分组的 tile 自己成一组（只能放自己）。
- 组表可配置、可由用户增改（放 `Config/groups.json`）。

---

## 5. 放置限制规则

蓝图建造模式下，在格子 `(x,y)` 放置时：

1. 蓝图该格**需要方块**（`type != 0`）：
   - 手里必须是方块（`createTile != 0`）；
   - 放行条件：`createTile == cell.type` **或** `GroupOf(createTile) == GroupOf(cell.type)`。
2. 蓝图该格**需要墙**（`type == 0 && wall != 0`）：
   - 手里必须是墙（`createWall != 0`）；
   - 同上用 `WallGroupOf` 判断。
3. 蓝图该格**为空**：默认放行（`PlacementPolicy` 可配成“禁止”）。
4. 蓝图区域之外：不限制。

> 机制：钩放置入口，判定不通过 → 清 `player.controlUseItem`（跳过本次放置）+ 提示音。
> 不生成、不消耗任何东西，纯“管住手”，多人亦合法。

---

## 6. 投影渲染要点

- 世界→屏幕：`screen = (world - Main.screenPosition) * zoom`，`zoom = Main.GameViewMatrix.Zoom`。
- 只画**视口内**的格子（按 `screenPosition` + 屏幕尺寸做裁剪）。
- 三态差异：缺（画半透明幽灵块）/ 错（画红描边）/ 多（可选，画黄描边）。
- 幽灵块纹理走 `TextureAssets.Tile[...]` + `Main.tileFrame`（可降级为纯色半透明方块，不依赖纹理则更稳）。
- HUD：剩余材料清单、坐标、当前蓝图名、开关状态。

---

## 7. 打印机要点（可选模块）

- 每个 tick：找“距离最近、在放置范围内、且已满足支撑”的待放格 →
  从热键栏/背包选对应物品 → 设 `tileTarget` → 调 `Player.PlaceThing()` →
  校验“物品确实被消耗/格子确实变化”，失败则重试或跳过。
- 流水线顺序：**方块 → 背景墙 → 物件（frame-important）→ 电线 → 促动器**。
  物件走 `WorldGen.PlaceObject(x,y,type,mute,style,...)`，不是 `PlaceTile`：帧-重要类型的
  左上角与 style 由蓝图帧反推（`TileObjectData.TryGetTileBounds` / `GetTileData(Tile)` 的逆运算），
  放完回写蓝图帧并补 `CallPostPlacementPlayerHook`（箱子等实体数据）。详见 `notes/printer-objects.md`。
- **合法性由游戏自身保证**（范围、支撑、液体、空间、物品消耗）。
- 材料来源：背包 → 猪猪存钱罐 → 附近箱子（可配置）。
- 多人默认禁用（检测 `Main.netMode == NetmodeID.MultiplayerClient` 时弹确认，且需手动开）。

---

## 8. 多人立场

| 功能 | 多人是否可用 | 说明 |
|---|---|---|
| 投影（显示） | ✅ 合法 | 纯客户端渲染 |
| 放置限制 | ✅ 合法 | 纯客户端过滤，不发额外数据 |
| 打印机 | ⚠️ 默认禁用 | 自动放置多数服视为宏/外挂，单机/自建服可用 |

---

## 8.5 附加功能：验证 / 流体 / 分层

### 8.5.1 蓝图验证（Verifier，只读）
- 在蓝图区域做**全区域**差异对比（不仅视口）：统计 `total / correct / missing / wrong / extra`。
- 错放/缺失/多余的格子出可滚动清单（坐标 + 应放什么 + 实际是什么），并在世界里高亮。
- 只读不写 → 多人合法，可做“一键验收”。

### 8.5.2 流体不放置（及电线）
- 打印机通过 `BuildOptions` 控制：`PlaceTiles / PlaceWalls / PlaceLiquids(默认关) / PlaceWires(默认关)`。
- 纯液体格（`type==0 && wall==0 && liquidAmount>0`）默认跳过，不尝试放置。
- 投影仍可显示液体（`ShowLiquids`，默认开）作为提示，但打印机不碰。

### 8.5.3 分层放置（LayerRange）
- 用 `LayerRange`（y 区间，可选 x 区间）限制“显示/可打印”范围，实现逐层/逐带建造。
- `MoveUp()/MoveDown()` 按 `Step`（层高）推进；投影与打印机共享同一 `LayerRange`。

## 9. 路线图（MVP）

| 阶段 | 内容 | 预估 |
|---|---|---|
| P1 | 兼容层 + 蓝图模型 + 从世界扫描生成蓝图 + 投影渲染 + 差异高亮 | 1–3 周 |
| P2 | 放置限制（蓝图建造模式）+ 相似组配置 + HUD | +1–2 周 |
| P3 | 打印机（材料校验 + 合法放置 + 失败重试） | +1–2 月 |
| P4 | 多人适配、UI、蓝图导入导出（兼容 `.TEditSch`）、版本跟进 | 持续 |

---

## 10. 风险与对策

| 风险 | 对策 |
|---|---|
| 游戏小版本更新破坏钩子 | 薄钩面 + IGameAPI 映射表 + patch 签名检查脚本 |
| 多人反作弊误判 | 投影/限制不发任何额外包；打印机默认禁用 |
| UI 缩放 / 全屏对齐 | 渲染走游戏自身 `GameViewMatrix`，不自己算窗口坐标 |
| 家具/多格物体 frame 匹配 | 比较 `type + frameX/frameY`；单格物块忽略 frame |

---

## 附：现有生态速查

- 投影/限制/打印机二合一：**无公开成品**。
- 最接近的现成物：[TEdit](https://github.com/TEdit/Terraria-Map-Editor)（外部改图，无中生有）、
  [tPlainModLoader](https://github.com/github-user-64/tPlainModLoader)（原版加载器，可二开）、
  [TerraAngel](https://github.com/UnrealMultiple/TerraAngel)（原版工具客户端，插件系统可用）、
  [tML Cheat Sheet](https://github.com/javidpack/cheatsheet)（非蓝图）。
