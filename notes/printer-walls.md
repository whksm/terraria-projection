# 打印机补墙：墙从“整类跳过”改成“方块之后的第二道工序”

## 症状（用户实机）

蓝图里有墙（玻璃墙等），NumPad7 打印只补方块，**墙一面都不补**；HUD 一直显示“缺墙”计数，`render: 差异 ... 缺墙 N` 也不会下降。

## 根因

`tool/src/Printer.cs` 的方块扫描第一行：

```csharp
if (c.type == 0) continue;   // 墙暂不打印
```

于是**所有“只有墙没有方块”的格子被整体跳过**；而“有方块又有墙”的格子即使方块已放好，循环也只处理方块，从不看 `c.wall`。全项目也没有任何 `WorldGen.PlaceWall` 调用 —— 打印机的墙从来没实现过。

其他模块其实早就认墙：`Renderer` 的 `_wallToItem`（缺墙=蓝光）、`BlueprintOps.Verify` 的缺墙统计、`Restrictor._wallGroups`、`AutoFetch` 的 `createWall` 分支、`WorldUtil.ItemMatches` 的 `createWall` 判断。差的只是打印机这一步。

## 1.4.5.8 签名确认（反编译，不凭记忆）

```csharp
// Terraria.WorldGen
public static void PlaceWall(int i, int j, int type, bool mute = false)
```

- 参数：`i,j` 图格坐标；`type` WallID；`mute` 静音（不发放置音效）。
- **返回 `void`，不是 bool** —— 不能靠返回值判断成功。
- 函数体：`i<=1 || j<=1 || i>=maxTilesX-2 || j>=maxTilesY-2` 直接 return；只有 `Main.tile[i,j].wall == 0` 时写入 `wall = type` 并自动 `SquareWallFrame(i,j)`；不扣物品、不判手长。
- 所以：**成功 = 调用后读回 `Main.tile[i,j].wall == type`**；读不到就当作失败、不扣材料。
- 已核对两版一致：1.4.5.8（`Terraria_MultiRod.exe` / `Terraria_backup_original.exe`）与 1.4.6.1（现装 `Terraria.exe`）签名、参数名、函数体都相同。

## 改法（`tool/src/Printer.cs`）

1. **流水线顺序**：方块 → **墙** → 电线 → 促动器。
   `Tick()` 里 `TryPlaceBlock` 之后插 `if (TryPlaceWall(pl, bp)) return;`，电线/促动器仍在后面；
   方块/电线的既有分支一行没动（顺手把形状收尾修复保持原位置）。
2. **候选扫描**照搬方块那套（独立函数 `TryPlaceWall`）：
   - 目标格：`c.wall != 0` 且 `t.wall == 0`（已有墙不覆盖，含别的墙）；
   - `Renderer.PassesLayer` 分层过滤、`Player.IsInTileInteractionRange` 手长判定；
   - **不因 `cell.type == 0` 跳过纯墙格**，也不看方块是否已放；
   - 同时维护“材料有的最近格”（真正要放）和“所有缺墙格最近格”（只用于缺料提示）；
   - 手长外的缺墙格单独计数 `pending`，只在 `pending > 0` 时提示“够不着”，避免蓝图墙补齐后每 2 秒刷屏。
3. **材料**：`BuildWallSlots` 建 `createWall → 第一个非空槽位` 的 `Dictionary<int,int>`（与 `BuildTileSlots` 同款，`Item.createWall`）。
4. **放置**：`WorldGen.PlaceWall(x, y, wall, true)`（静音）→ 读回确认 → `Consume(pl, slot)` 扣 1 → `Sync(x, y)`。
   失败（边界/被占用）不扣材料，直接返回等下个 tick。
5. **日志**：
   ```text
   打印机: 墙 (3247,217) wall=52 剩余=1234
   打印机: 缺少墙材料，范围内无法放置: wall=52 ×12, wall=270 ×3
   打印机: 墙未放置 7 面（不在手长/交互范围内）
   ```
   成功日志限频 2 秒（`CanLogWall`），缺料/够不着聚合 5 秒（`CanLogWallMissing`），与方块/电线各自独立。
6. `BuildMissingSummary` 加了 `prefix` 参数（`tile=` / `wall=`），方块提示文本与原来完全一致。
7. `BlueprintOps.Verify`：新增 `missWall` 计数并写进日志
   `验证: 正确 x/y | 缺失 N（缺墙 M） | 错误 K（形状/帧 S） | 多余 E`；
   计入规则 = 该格蓝图有墙、世界 `t.wall == 0`（越界格按有墙计）；`缺失` 总数语义不变。

未做：墙漆（`wallPaint`）暂不刷 —— 与方块漆同样交给手动；墙的 `frameX/frameY` 由游戏 `SquareWallFrame` 自动框，不需要蓝图帧。

## 涉及文件

- `tool/src/Printer.cs`（新增 `TryPlaceWall` / `BuildWallSlots` / `CanLogWall` / `CanLogWallMissing`，改顺序与日志）
- `tool/src/BlueprintOps.cs`（Verify 加缺墙计数）
- `release/使用说明.md`、`README.md`（打印机说明加“墙”）

## 构建 / 部署（2026-11-25 19:25）

- 编译：`csc /target:library /platform:x86 /optimize+ /debug- /langversion:5 /win32icon:assets\icon_32.ico`，
  15 个源文件（`tool/src/*.cs` 去掉 `Patcher.cs`），引用 `Terraria.exe`(1.4.5.8) + `tool/xna` 三个 XNA dll + `0Harmony.dll`。
- 结果：**0 error / 0 warning**；DLL 78,848 字节（上一版 77,312）。
- 引用仍是 `Terraria 1.4.5.8`（与历次构建一致；运行时按简单名绑定，1.4.6.1 实机可用）。
- 图标：`.rsrc` 里 4 个 RT_ICON 帧（16/32/48/256）与 `assets/icon_32.ico` 的 DIB 像素 SHA256 完全一致。
- 三处 DLL 同步（游戏目录 / `tool/` / `release/`），SHA256
  `B724EE4B8CD6B24879A583B08EAE2CB6B46DC1F4EA676412ADB285D871BC0E25`。
- 部署前备份：`ProjectionMod.dll.bak-墙-192515`（77,312 字节）。
- 未动 `Terraria.exe`、`Content`、桌面快捷方式；未启动游戏。

## 回滚

```powershell
Copy-Item 'D:\steam\steamapps\common\Terraria\ProjectionMod.dll.bak-墙-192515' 'D:\steam\steamapps\common\Terraria\ProjectionMod.dll' -Force
```

## 验证

1. 备好含玻璃墙的蓝图（`type` 可以全是 0、只有 `wall`），站到手长内，NumPad7。
2. 看墙从近到远补上，日志出现 `打印机: 墙 (x,y) wall=<id> 剩余=<n>`；背包对应墙物品 -1。
3. 材料不够时 5 秒一条 `打印机: 缺少墙材料，范围内无法放置: wall=52 ×N`，不刷屏。
4. NumPad3 验证：`缺失 ... （缺墙 M）`，M 归零即墙补齐。
5. 边界：站得太远时只提示“墙未放置 N 面（不在手长/交互范围内）”，走近自动继续。
