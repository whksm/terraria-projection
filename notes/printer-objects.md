# 打印机物件（frame-important）支持：火把/门/箱子/压力板/计时器/传送机

## 症状（用户实机）

蓝图里有火把、蜡烛、门、压力板、传送机、计时器，`NumPad7` 打印**一个都不放**；其余方块/墙/电线都正常。

## 根因

`tool/src/Printer.cs` 方块扫描里：

```csharp
if (Main.tileFrameImportant[c.type] && !platform) continue;   // 家具/火把交给手动；平台放行
```

所有 frame-important 物件被整体跳过。它们也**不能**用 `WorldGen.PlaceTile`：`PlaceTile` 只处理“简单方块”，
对物件的数据只在液体判定里查一次 `TileObjectData.GetTileData(num, style)`（`WorldGen.cs:59939`），
不会写物件帧，也没有支撑/空间判定。

## 反编译证据（1.4.5.8 与 1.4.6.1 逐字一致）

反编译命令（两版各跑一次）：

```powershell
C:\Users\16356\.dotnet\tools\ilspycmd.exe -t Terraria.WorldGen        "D:\steam\steamapps\common\Terraria\Terraria.exe"                      # 1.4.6.1
C:\Users\16356\.dotnet\tools\ilspycmd.exe -t Terraria.WorldGen        "D:\steam\...\Terraria_vanilla_readonly_backup.exe"                 # 1.4.5.8
C:\Users\16356\.dotnet\tools\ilspycmd.exe -t Terraria.TileObject      <两版各一次>
C:\Users\16356\.dotnet\tools\ilspycmd.exe -t Terraria.ObjectData.TileObjectData <两版各一次>
C:\Users\16356\.dotnet\tools\ilspycmd.exe -t Terraria.Chest / Terraria.Player   （1.4.6.1）
```

- `Terraria.ObjectData.TileObjectData` 两版反编译产物 **SHA256 完全相同**（161,698 字节）；
  `Terraria.TileObject` 两版也完全相同（31,542 字节）。物件系统的运行时数据表没动过。
- `WorldGen.PlaceObject` 两版**函数体反编译文本 hash 相同**（`F2FCC4580805A688`，21 行）：
  - 1.4.6.1 在第 **51862** 行，1.4.5.8 在第 **51875** 行，签名一字不差。

```csharp
// Terraria.WorldGen
public static bool PlaceObject(int x, int y, int type, bool mute = false, int style = 0,
                               int alternate = 0, int random = -1, int direction = -1)
{
    if (type >= TileID.Count) return false;
    if (!TileObject.CanPlace(x, y, type, style, direction, out var objectData)) return false;
    objectData.random = random;
    if (TileObject.Place(objectData))
    {
        SquareTileFrame(x, y);
        if (!mute) SoundEngine.PlaySound(0, x * 16, y * 16);
    }
    return true;            // ← 注意：Place 失败也返回 true
}
```

关键结论：

1. **`x,y` 不是左上角**，是“放置基准格”= 左上角 + `TileObjectData.Origin`（见 `TileObject.CanPlace`：
   `num8 = x - tileData.Origin.X`）。传送机 `Origin=(1,0)`（3 格宽的中间格），箱子 `Origin=(0,1)`（左下格）。
2. **返回值不可信**：`TileObject.Place` 走 `HookPlaceOverride` 失败时 `PlaceObject` 仍返回 `true`。
   所以必须**读回世界**（`t.active() && t.type == type`）确认。
3. 它自己会 `SquareTileFrame`，但**不会**调用 `HookPostPlaceMyPlayer`（箱子/人偶/感应器的实体数据全靠这个钩子）。
   玩家的路径在 `Player.PlaceThing_Tiles_PlaceIt`（1.4.6.1 第 40427-40489 行）：

```csharp
flag = TileObject.Place(data);
WorldGen.SquareTileFrame(tileTargetX, tileTargetY);
...
TileObjectData.CallPostPlacementPlayerHook(tileTargetX, tileTargetY, tileToCreate, style, direction,
                                           data.alternate, data);
```

   → 打印机也必须补一次 `CallPostPlacementPlayerHook`，否则箱子只是“一张箱子贴图”，打开时没有箱子数据。

## 从 frameX/frameY 反推「原点」和「style」

### 原点（`TileObjectData.TryGetTileBounds`，1.4.6.1 第 5164-5195 行）

```csharp
int num  = tile.frameX / tileData.CoordinateFullWidth;    // 第几列块
int num2 = tile.frameY / tileData.CoordinateFullHeight;   // 第几行块
int num3 = tile.frameX - num * tileData.CoordinateFullWidth;
int num4 = tile.frameY - num2 * tileData.CoordinateFullHeight;
int coordinateWidth = tileData.CoordinateWidth;
while (num3 >= coordinateWidth) { num3 -= coordinateWidth; bounds.X--; }
int[] coordinateHeights = tileData.CoordinateHeights;
for (int i = 0; i < coordinateHeights.Length; i++)
    if (num4 >= coordinateHeights[i]) { num4 -= coordinateHeights[i]; bounds.Y--; }
```

即：**从本格向左/向上按 `CoordinateWidth` / `CoordinateHeights` 逐格回退**，直到余量 < 一格。
（不是“退到 frameX<18&&frameY<18”那么粗：门/箱子的列宽是 18、坐标宽 16，18 格要退 1 格、16 格不退。）

`CoordinateFullWidth/Height` 的来源（`Calculate()`，第 1805-1832 行）：

```csharp
styleWidth  = (CoordinateWidth + CoordinatePadding) * Width + paddingFix.X;
styleHeight = Σ(CoordinateHeights[i] + CoordinatePadding) + paddingFix.Y;
```

### style（`TileObjectData.GetTileData(Tile)`，第 5197-5262 行）

```csharp
int num  = frameX / CoordinateFullWidth;      // 列块
int num2 = frameY / CoordinateFullHeight;     // 行块
int num3 = StyleWrapLimit == 0 ? 1 : StyleWrapLimit;
int num4 = StyleHorizontal ? (num2 * num3 + num) : (num * num3 + num2);
int num5 = num4 / StyleMultiplier;            // ← style
int num6 = num4 %  StyleMultiplier;
if (StyleLineSkip > 1) {                      // 门就是这一类（StyleLineSkip=3）
    num5 = StyleHorizontal ? (num2 / StyleLineSkip * num3 + num)
                           : (num /  StyleLineSkip * num3 + num2);
    num6 = StyleHorizontal ? (num2 % StyleLineSkip) : (num % StyleLineSkip);
}
```

这就是“从帧求 style 的官方逆运算”。`StyleLineSkip` 那一段非常关键：**门的 frameX 是每行随机的 0/18/36**，
靠 `num / StyleLineSkip` 把它压成 0，否则会算出 style 92 这种鬼值。

只有 3 个类型挂了 `GetStyleOverride` 特例（`Detritus` / `SmallPiles` / `Stalactite`，第 4969/4980/4994 行），
其余全部走上面的通用公式。

## 实测反推（`Blueprints\building_07.bp`，6767 行）

把 `TileObjectData.Initialize()` 里这 9 个类型的数据抄出来，跑一遍上面的算法：

| type | 物件 | W×H | Origin | CoordW/Pad | CoordHeights | StyleHorizontal | Wrap | LineSkip | Mult | 反推结果 | style |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 4 火把 | 38 | 1×1 | (0,0) | 20/2 | [20] | true | 6 | 1 | 6 | 38 个物件，格数全对 | **5** |
| 10 门 | 2 | 1×3 | (0,0) | 16/2 | [16,16,16] | false | 36 | 3 | 1 | 2 个物件，各 3 格 | **20** |
| 21 箱子 | 40 | 2×2 | (0,1) | 16/2 | [16,18] | true | 0 | 1 | 1 | 40 个物件，各 4 格 | **47** |
| 49 蜡烛 | 1 | 1×1 | (0,0) | 16/2 | [20] | false | 0 | 1 | 1 | 1 个 | 0 |
| 97 保险箱 | 1 | 2×2 | (0,1) | 16/2 | [16,16] | false | 0 | 1 | 1 | 1 个 | 0 |
| 135 压力板 | 1 | 1×1 | (0,0) | 16/0 | [18] | false | 0 | 1 | 1 | 1 个 | **1** |
| 144 计时器 | 7 | 1×1 | (0,0) | 16/2 | [16] | true | 0 | 1 | 1 | 7 个 | 0 / **4** |
| 235 传送机 | 4 | 3×1 | (1,0) | 16/2 | [16] | false | 0 | 1 | 1 | 4 个，各 3 格 | 0 |
| 428 测重板 | 1 | 1×1 | (0,0) | 16/0 | [18] | false | 0 | 1 | 1 | 1 个 | 0 |

**反向对照**：用算出来的 style 按 `TileObject.Place` 的公式重算每一格的帧：

```
num4 = style*Multiplier; num5 = 0;
if (Wrap > 0) { num5 = num4/Wrap*LineSkip; num4 %= Wrap; }
StyleHorizontal ? (frameX = FullW*num4, frameY = FullH*num5)
                : (frameX = FullW*num5, frameY = FullH*num4)
再按格偏移：frameX += i*(CoordW+Pad); frameY += Σ(heights[0..j-1]+Pad)
```

结果：**9 个类型的 frameY 全部 0 处不符**（style 推对了）；frameX 只有两类不符：

- **门 4 格**：`PlaceDoor`（`WorldGen.cs:32052`）里 `frameX = 54*(style/36) + genRand.Next(3)*18`，
  闭门每行的 frameX 是**随机 0/18/36 的贴图变体**（`CloseDoor` `WorldGen.cs:32165` 同款）。属装饰差异。
- **火把 7 格**：frameX = 22 / 44。火把 `CoordinateFullWidth` 正好 22，所以 22/44 是**朝向**（侧挂/倒挂）而不是 style。

→ 结论：**放置后用蓝图帧回写 frameX/frameY 是必须的**（火把朝向、门的随机列、任意朝向类物件都靠它），
而且因为回写的是“真实世界抓下来的帧”，结果一定和原物一致。

## 门：闭门是 1×3，不是 2×3

`addTile(10)` 的数据（`TileObjectData.cs:2224-2248`）：`Width=1, Height=3, Origin=(0,0)`，
`AnchorTop=(SolidTile,1,0)`、`AnchorBottom=(SolidTile,1,0)`，`StyleWrapLimit=36, StyleLineSkip=3`。

- 闭门帧：`frameY = 54*(style%36) + 18*row`，`frameX = 54*(style/36) + 0/18/36(随机)`。
  → 蓝图 1080 / 1098 / 1116 三行 = style 20 的第 0/1/2 行。
- 开门是**另一个 tile**（type 11，`WorldGen.OpenDoor` 把 10 改写成 11），所以蓝图里出现 11 时按 11 的数据反推。
- `PlaceObject(10)` 的 `Place` 算出来：`frameY = 54*(style%36)`、`frameX = 54*(style/36)`，与 `PlaceDoor` 一致
  （差一个随机装饰列），**所以门可以直接走 PlaceObject，不需要 `PlaceDoor`**。
- 门还需要上下都有实体块（AnchorTop/AnchorBottom），缺支撑会 `CanPlace` 失败——这正是要记日志的原因。

## 箱子这类“有实体”的物件

`TileObjectData.cs:3462-3476`：

```csharp
newTile.HookCheckIfCanPlace  = new PlacementHook(Chest.FindEmptyChest, -1, 0, true);
newTile.HookPostPlaceMyPlayer = new PlacementHook(Chest.AfterPlacement_Hook, -1, 0, false);
```

- `Chest.FindEmptyChest`（`Chest.cs:518`）只读：找第一个空的箱子槽，找不到返回 -1 → `CanPlace` 直接判失败（箱子满 8000 个时）。
- `Chest.AfterPlacement_Hook`（`Chest.cs:555`）先 `TileObjectData.OriginToTopLeft(type, style, ref base)`，
  再 `CreateWorldChest(...)` —— **不调它，箱子里没有数据（打不开/掉落异常）**。
- 挂 `HookPostPlaceMyPlayer` 的类型（`Initialize()` 里全量）：21、88、467（容器）、520、698、470、475、597、
  423、395、471、723、724、378 等。打印机统一调用 `CallPostPlacementPlayerHook`，箱子/梳妆台/人偶/感应器都能正常落地。

## 实现（`tool/src/Printer.cs`）

### 流水线顺序

`① 方块 → ② 墙 → ③ 物件 → ④ 电线 → ⑤ 促动器`

物件放在**墙之后**：挂墙的火把（`AnchorWall` 备选）和挂画/挂旗必须等背景墙先存在；
物件在**电线之前**：电线/促动器不依赖物件，反过来先铺线再放物件会让 `SquareTileFrame` 重算刚放的物件帧。
方块必须在物件之前（物件的 `AnchorBottom/AnchorTop` 判的是实体块）。

### 反推与去重

1. `TryObjectLayout`：用上面的 `TryGetTileBounds` + `GetTileData(Tile)` 逆运算，得到
   蓝图内**物件左上角 (ox,oy)**、宽高 `W×H`、`style`。
2. 物件格必须**整块都在蓝图内**（`bp.TryGet` 全中且 `type` 相同），否则记日志跳过——
   绝不让打印机在蓝图边界外多放半扇门。
3. 同一次扫描里用 `origin+type` 去重：一个 2×2 箱子只算一个候选，不会放 4 次。
4. 已放（左上角已是同类型）→ 跳过；任一格被别的方块占住 → 计 `blocked` 并跳过（不重试、不刷屏）。
5. 目标格还要过：分层 `Renderer.PassesLayer`、手长 `IsInTileInteractionRange`、玩家身体不重叠。

### 放置

```csharp
int px = wx + data.Origin.X;              // 放置基准格，不是左上角
int py = wy + data.Origin.Y;
if (!TileObject.CanPlace(px, py, type, style, dir, out objData))   // dir 先试 1，再试 -1
    → 记日志失败，返回
WorldGen.PlaceObject(px, py, type, /*mute*/ true, style, 0, -1, dir);
读回 (wx,wy) 校验 active + type
TileObjectData.CallPostPlacementPlayerHook(px, py, type, style, dir, objData.alternate, objData);
WriteObjectFrames(...)                    // 蓝图帧回写，立刻生效
```

`CanPlace` 先调一次只为拿 `objData.alternate`（后置钩子要它），它本身**只读世界、不改世界**。

- 材质：先按 `(createTile, placeStyle)` 精确找，再退化为任意 `createTile == type` 的物品；成功后扣 1。
- 失败日志（2 秒限频，避免刷屏但不静默）：

```text
打印机: 物件放置失败 type=4 目标=(3247,217) style=5 原因=CanPlace 拒绝（缺支撑/被占用/空间不够）
```

  失败后该物件 **5 秒内不再重试**（`_objFail` 表），避免每 tick 撞同一面墙。
- 缺料聚合日志（5 秒限频）：`打印机: 缺少物件材料，范围内无法放置: item=4 ×38, item=21 ×40`。

### 收尾修复

`RepairShapes` 第三遍：所有 frame-important 非平台格，世界格 `type` 对上就写回蓝图的 `frameX/frameY`。
这一遍在**所有物件放完之后**跑，且之后只放电线/促动器（它们不 `SquareTileFrame`），
所以平台帧 + 物件帧都不会再被邻居的重框覆盖。

### 验证（`NumPad3`）

`BlueprintOps.Verify` 只比 `type`/`wall`（帧只对平台比），物件多格同 type、帧由打印机回写，
所以物件格不会被算成“多余/错放”，不需要改（已确认）。

## 仍然可能放不了的物件（如实）

1. **没有 `TileObjectData` 的 frame-important 类型**：`GetTileData` 返回 null → 无宽高/锚点可用，
   `PlaceObject` 也一定失败。打印机只记一条 `打印机: 物件无 TileObjectData type=N，跳过` 并跳过。
2. **蓝图帧被截断/多格物件跨蓝图边界**：直接跳过（不补半扇门）。
3. **需要额外条件的物件**：
   - 床/椅子/马桶等有朝向的家具：`CanPlace` 的 `Direction` 可能正好反向 → 打印机两个方向都试；
     仍失败就是“这一格原版也不让放”。
   - 箱子/梳妆台：世界箱子槽满（8000）时必失败。
   - 人偶/武器架/物品框/训练假人/传送晶塔/逻辑感应器：要 `HookPostPlaceMyPlayer` 建实体数据（已调），
     但晶塔还要求同类型唯一 + 距离规则，可能因“已有同款晶塔”失败。
   - 标牌/绘画/告示牌：绘画需要墙 + `AnchorWall`，标牌要写文字（蓝图不存文字，只能放空标牌）。
   - 植物类（`PlantCheck_CanPlaceHook`）：要求土/墙条件，原版不放的打印机也不放。
4. **`RandomStyleRange` 类型**（部分地毯/雕像）：`Place` 会随机选子样式，打印机回写蓝图帧后外观正确，
   但**扣的物品**可能不是同一 variant（`placeStyle` 已优先匹配，匹配不上就用同 `createTile` 的第一堆）。
5. 物件上的**漆**（`paint`）暂不刷，和方块/墙一致（交给手动/自动补料）。

## 涉及文件

- `tool/src/Printer.cs`：新增物件阶段（`TryPlaceObject` / `TryObjectLayout` / `GetObjectData` /
  `BuildObjectSlots` / `WriteObjectFrames` / `IsObjectType` / 失败限频表 `_objFail`），`RepairShapes` 加第三遍，
  `Tick` 顺序改为 方块→墙→物件→电线。
- `notes/printer-objects.md`（本文）
- `release/使用说明.md`、`README.md`、`DESIGN.md`：打印机说明加“物件/家具”。

## 构建 / 部署（2026-11-25 19:53）

- 编译：`csc.exe /target:library /platform:x86 /optimize+ /debug- /langversion:5 /win32icon:assets\icon_32.ico`，
  15 个源文件（`tool/src/*.cs` 去掉 `Patcher.cs`），引用 1.4.5.8 的 `Terraria.exe` +
  `tool/xna` 三个 XNA dll + `0Harmony.dll`。
- 结果：**0 error / 0 warning**；DLL **84,480** 字节（上一版 78,848）。
- 图标：4 帧（16/32/48/256）的 DIB 像素在 DLL 的 `.rsrc` 里逐字节命中 `assets/icon_32.ico`。
- `tool/ProjectionMod.dll` 与 `release/ProjectionMod.dll` SHA256
  `0719B3458DB169EC238E16D4C3E1B883CE4CBB4B94455652ABFAED6F50774F2A`。
  （中间还有一版 `1B3566…`：跑完后发现“物件缺料时 `hasObject` 一直为真，会把收尾的帧修复永远挡住”，
  改成**有料才算还能放**，平台帧修复不受物件缺料影响。）
- 部署前备份：`ProjectionMod.dll.bak-物件-195312`（78,848 字节）。
- **游戏当时正在运行（Terraria pid 32656），DLL 被占用无法覆盖** → 起了一个延迟部署守护进程：
  等 Terraria 进程退出后自动 `Copy-Item` 到游戏目录并写 `notes/deploy-objects.log`（含 SHA256）。
  三处 SHA256 一致需等它跑完再核。
- 未动 `Terraria.exe`、`Content`、快捷方式；未启动游戏。

## 程序集名事故与修复（2026-11-25 21:40）

**事故**：上面「构建 / 部署」里那版 84,480 的 DLL，程序集名是 **`build_obj`** 而不是 `ProjectionMod`。
根因是那一轮 csc 直接在命令行打了 `build_obj` 的壳（输出落在 `%TEMP%\build_obj.dll` 再改名/拷贝），
而 **csc 的程序集名 = `/out:` 输出文件名**，改名不会改 PE 里的 `AssemblyDefinition.Name`。
游戏 `Terraria.exe`（1.4.6.1，已打补丁）启动时按 `ProjectionMod` 找程序集 → 名字对不上 →
`System.IO.FileLoadException` → 崩溃（事件日志 `Application Error` 异常码 `0xe0434352`，`.NET Runtime` 指向 `Terraria.WindowsLaunch.Main`）。

**另一个坑**：上一轮那个“延迟部署守护进程”（PID 42152）在用户把 DLL 回滚成 `bak-物件-195312` 之后、
Terraria 一退出，又把 `tool/ProjectionMod.dll`（坏版本）覆盖回游戏目录了，所以用户回滚后“又能进游戏”的状态
其实很快被冲掉。已手动 `Stop-Process -Id 42152 -Force` 终止，不再有进程会覆盖游戏目录的 DLL。

**修复**（不再靠手打 csc 命令行）：

- `tool/build.ps1` —— 唯一构建入口。固定
  `/out:<repo>\tool\build\obj-out\ProjectionMod.dll`（程序集名即 `ProjectionMod`），
  `/target:library /platform:x86 /optimize+ /debug- /langversion:5 /win32icon:assets\icon_32.ico`，
  15 个源文件（不含 `Patcher.cs`），引用 `tool/build/Terraria.exe`(1.4.5.8) + `tool/xna` 三个 + `tool/0Harmony.dll`。
  编译后要求 **0 error / 0 warning**（有 warning 也直接失败），然后才做校验、才复制。
- `tool/verify-dll.ps1` —— **硬校验**，`build.ps1` 编译完立刻调用；任一项不过就 `throw`，
  **不复制 `tool/`、不复制 `release/`、不部署**：
  - 程序集名：Mono.Cecil 读 `AssemblyDefinition.Name.Name`，必须 `== 'ProjectionMod'`；
  - `0Harmony` 引用版本 == 游戏目录 `0Harmony.dll` 的版本（当前 2.4.3.0）；
  - `Terraria` 引用版本 == `1.4.5.8`（与游戏 1.4.6.1 的差异是预期的）；
  - 图标像素：DLL 内 `RT_ICON` 每帧 SHA256 与 `assets/icon_32.ico` 的 4 帧（905/2985/5746/3810 字节）逐一命中。
  - 单独用：`pwsh -NoProfile -File tool\verify-dll.ps1 -Path '<dll>'`。
  - 负向验证过：拿坏版本 `%TEMP%\build_obj.dll` 跑，会报
    `程序集名 = 'build_obj'，不是 ProjectionMod —— 输出文件名错了…禁止部署` 并退出码 1。
- `README.md` / `tool/README.md` 的构建章节都改成了 `tool\build.ps1`，并写明“程序集名 = /out 文件名，不许改名”。

**本次产物**：DLL **84,480** 字节，`AssemblyDefinition.Name.Name = 'ProjectionMod'`（Cecil 读回原文），
版本 0.0.0.0，19 个类型 / 187 个方法（与坏版本的方法清单逐条一致，只差 csc 每次编译随机生成的
`<PrivateImplementationDetails>{GUID}` 类型名），物件相关方法 `TryPlaceObject` / `TryObjectLayout` /
`GetObjectData` / `BuildObjectSlots` / `WriteObjectFrames` / `IsObjectType` 都在。
引用：`Terraria 1.4.5.8`、`0Harmony 2.4.3.0`、`Microsoft.Xna.Framework(.Game/.Graphics) 4.0.0.0`。
SHA256 **`7E45A1E2975400749E22720BB0E73FE91E96AFE4118E53A1025F210428590FB6`**。

**三处一致**（同一 SHA256 / 84,480 字节）：

| 位置 | 路径 |
|---|---|
| 构建输出 | `tool\build\obj-out\ProjectionMod.dll` |
| 工具目录 | `tool\ProjectionMod.dll` |
| 发布目录 | `release\ProjectionMod.dll` |
| 游戏目录 | `D:\steam\steamapps\common\Terraria\ProjectionMod.dll` |

**部署前备份**：`ProjectionMod.dll.bak-物件修复-214054`（78,848 字节，即用户确认能进游戏的
`bak-物件-195312` 那一版；部署前我先把被守护进程冲掉的这份恢复回游戏目录，再备份，所以备份是能用的版本）。

**未验证**：这次**没有启动游戏**（本会话键盘进不去），需要用户自己启动确认；程序集名与依赖版本、
图标都已用 Cecil / 资源 API 校验通过。

## 回滚

```powershell
# 回滚到“物品消耗修正”之前（84,480 字节，物件支持版，蜂巢魔棒会被吞的那个）
Copy-Item 'D:\steam\steamapps\common\Terraria\ProjectionMod.dll.bak-消耗-215317' `
          'D:\steam\steamapps\common\Terraria\ProjectionMod.dll' -Force
# 再往前：78,848 字节（无物件支持的旧版，用户确认能进游戏）
Copy-Item 'D:\steam\steamapps\common\Terraria\ProjectionMod.dll.bak-物件修复-214054' `
          'D:\steam\steamapps\common\Terraria\ProjectionMod.dll' -Force
```

## 验证（NumPad3 / 看日志）

1. **火把**：蓝图里立着的火把、挂侧墙的火把各一个。关游戏重开（新 DLL）、进世界、NumPad7。
   期望日志 `打印机: 物件 (x,y) type=4 style=<款式> 尺寸=1x1 剩余=N`；
   实物的 **朝向和款式（颜色）** 与蓝图一致（不是全变立式/全变同色）。
2. **门**：一扇门。期望 3 格一次成形（不是 2 格宽/3 格高的错位），门能正常开关；日志一条 `type=10 style=<款式>`。
3. **压力板 / 测重压力板**：一格。日志 `type=135` / `type=428`，实物颜色对应（135 蓝图 style=1）。
4. **传送机**：3 格一次成形（`尺寸=3x1`），右键能打开传送机界面；日志一条 `type=235 style=0`。
5. **计时器**：`type=144 style=0` 与 `style=4` 各一个，实物是不同时长的计时器。
6. **箱子**：`type=21 style=47`，2×2 一次成形，**右键能打开箱子**（能开 = 后置钩子生效）。
7. NumPad3 验证：物件格不计入“多余/错放”，`缺失` 应归零；`错误` 里的“形状/帧”只可能来自平台。
8. 失败路径：把门下面的方块拆掉再打印，应出现
   `打印机: 物件放置失败 type=10 目标=(x,y) style=… 原因=CanPlace 拒绝（缺支撑/被占用/空间不够）`，
   且 5 秒内不刷屏。

## 物品消耗修正：魔棒被吞（2026-11-25 21:53）

**症状**：打印机放蜂巢块（tile 225）时把**蜂巢魔棒**（item 1129）吃掉了；原版放蜂巢块不消耗魔棒。
骨棒（29）/ 树叶魔棒（740）/ 生命木魔棒（832）这类“法杖类放置物”同理。

**根因**：`Printer.Consume` 无条件 `it.stack--`，没看 `Item.consumable`；魔棒 stack=1 →
扣一次就 `SetDefaults(0)`，直接从背包消失。`GameHooks.TryDirectPlace`（无支撑放置，手持放置路径）
里也有同一个无条件的 `held.stack--`。

**改动**：

- `tool/src/Printer.cs`
  - `Consume(Player pl, int slot, int placeType, string what)` 改为返回 `bool`；
    `if (!it.consumable)` → **不扣数量**，放置照常成功，只记一条 5 秒限频日志：
    `打印机: 使用非消耗物品放置 type=225 item=1129（不扣数量）`（方块无前缀，墙/物件带 `墙`/`物件` 前缀）。
  - 5 个调用点全部带类型：方块 `type`、墙 `wall`、物件 `bestType`、电线/促动器 `0`。
  - 电线分支改用返回值：`if (!Consume(...)) break;` 之后才 `wireStack--` —— 电线是消耗品，真没扣就停，避免无限放。
  - `Debug.Assert`（只在 DEBUG 编译；release `/debug-` 下不生成指令）：`consumable=true` 的普通材料
    必须真的 `-1`，**不要一刀切“都不扣”**（否则变无限材料）。
- `tool/src/GameHooks.cs`（无支撑放置 `TryDirectPlace`）：同样先看 `held.consumable`；
  日志变成 `无支撑放置: (x,y) type=225 非消耗物品 item=1129（不扣数量）`（可消耗时仍打 `剩余材料=N`）。

**排查结论**：全项目只有这两处改物品数量（`grep '\.stack'` 其余全是只读判断）；
`AutoFetch` 只换槽位、`BlueprintOps` / `SnapPlace` / `Selector` / `Renderer` 都不扣物品。

**编译 / 校验**：`tool/build.ps1` → **0 error / 0 warning**；`tool/verify-dll.ps1` 全通过
（程序集名 `ProjectionMod`、`0Harmony 2.4.3.0`、`Terraria 1.4.5.8`、图标 4 帧命中）。
DLL **84,992** 字节（上一版 84,480），SHA256
**`8398D2D886EFB9521F42B6509034CD6AF70705C0D9ECC3E7491706680EB9C5E6`**。

**四处一致**：`tool/build/obj-out`、`tool/`、`release/`、游戏目录，同一 SHA256。

**部署**：备份 `ProjectionMod.dll.bak-消耗-215317`（84,480，修复前那版）；部署前确认没有轮询守护进程，
部署后 10 秒内文件未被外部改动。
**注意**：`dist/TerrariaProjection_最新版_20261125_2145.zip` 里还是旧 DLL（84,480 / `7E45A1E2…`），需要重打。

**已知显示问题（不影响功能）**：HUD 材料清单按“方块物品”统计（tile 225 → 蜂巢块物品），
背包里只有魔棒时仍显示“缺蜂巢块”；打印机照样能放（不扣数量），只是显示上不认魔棒。

**验证方法**：蓝图里放 3~5 个蜂巢块 → NumPad7 → 背包里魔棒还在、日志出现
`打印机: 使用非消耗物品放置 type=225 item=1129（不扣数量）`；再用普通方块（土块）打印，
日志 `剩余=` 必须每个 -1。

