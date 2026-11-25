# 1.4.5.8 锤子派生方块：编码、取证与改造方案

> 第一阶段研究文档。**第一阶段没有改 `tool/src`、没有重新编译、没有覆盖 `release/ProjectionMod.dll`、没有往游戏目录写任何东西。**
> 第二批已按第 6 节实施代码；平台 slope 3/4 的实测修正见 3.5 / 4.3。
>
> 结论先行：**平台形变不是只靠 `slope` 编码的**；`frameY` 存的是平台材质/款式，`frameX` 才是平放/端帽/楼梯/贴块等形状，`slope` 1/2 是楼梯碰撞，`halfBrick` 是半砖。抓取层其实已经保存了 `frameX/frameY`，真正缺的是 **Renderer 画错** 和 **Printer 根本不放平台、也不恢复形状**。不需要给 Blueprint 加字段。

---

## 0. 结论速览

| 问题 | 结论 | 证据等级 |
|---|---|---|
| 普通物块坡 | `slope()` 1/2/3/4 分别对应四种坡；`halfBrick()` 单独一位 | ✅ 已验证 |
| Renderer 的坡方向 | **四个方向全画反了**：1 画成 `/`、2 画成 `\`、3 画成 `\`、4 画成 `/` | ✅ 已验证（代码+像素几何） |
| 平台形变 | `frameX` 编码形状（0..468，步长 18）；`frameY = 18 × style` 编码材质（type 19；团队平台 frameY=0）；`slope` 0–4 都可能（3/4 由 block swap 保留，见 4.3）；`halfBrick` 单独一位 | ✅ 已验证 |
| 平台抓取 | `WorldUtil.CellFromTile` 第 58–60 行**已经**存了 `frameX/frameY` 并置 `hasFrame=true`；不是“没抓” | ✅ 已验证 |
| 平台渲染 | 有帧时贴图本身是对的形状；但 `DrawSlopeMark` 又叠了一条**方向相反**的斜线；半砖没有下移/裁切 | ✅ 已验证 |
| 平台打印 | `Printer` 用 `Main.tileFrameImportant` 把平台整个跳过；即使放，`PlaceTile(..., style=0)` 也只会放 0 号款式，且不写 `slope/halfBrick/frameX` | ✅ 已验证 |
| 平台放置 API | `WorldGen.PlaceTile(x,y,19,...,style)` 的平台分支只设 `frameY=18*style`；随后走 `PlaceTile` 公共尾部，会调一次 `SquareTileFrame`，但此时 `slope/halfBrick` 还是 0，所以只会得到“按当前邻居的平放帧”，不会还原蓝图里的楼梯/半砖 | ✅ 已验证 |
| 是否要加 Blueprint 字段 | **不要**。复用 `frameX/frameY/slope/halfBrick` 足够 | ✅ 方案结论 |

---

## 1. 取证方法与来源

### 1.1 本地二进制

- 游戏：`D:\steam\steamapps\common\Terraria\Terraria.exe`，FileVersion/ProductVersion 均为 `1.4.5.8`。
- 只读校验用的原版备份：`Terraria_vanilla_readonly_backup.exe`，大小与当前 `Terraria.exe` 相同（26,593,280 字节），版本相同。
- 反编译：
  - `ilspycmd 8.2.0.7535`（`C:\Users\16356\.dotnet\tools\ilspycmd.exe`）直接反编译 `Terraria_backup_original.exe` 的类型；
  - `Mono.Cecil 0.11.5` 独立读 IL，逐条核对 `Tile.slope()`、`Tile.slope(byte)`、`Tile.blockType()`、`WorldGen.PlaceTile`、`WorldGen.PoundPlatform` 等方法的指令，避免只信反编译器。
- 现有 API 摘要：`notes/api-1.4.5.8.txt`（此前已抓过 `Tile` 方法表、`Main.tileFrameImportant` 等）。

### 1.2 第三方 1.4.5.8 反编译仓库（用于对照，不作为唯一依据）

- 仓库：<https://github.com/11NU0/Terraria-1.4.5.8-decomp-recomp>
- `Tile.cs`：<https://github.com/11NU0/Terraria-1.4.5.8-decomp-recomp/blob/main/_src/Terraria/Tile.cs>
- `WorldGen.cs`：<https://github.com/11NU0/Terraria-1.4.5.8-decomp-recomp/blob/main/_src/Terraria/WorldGen.cs>
- `Framing.cs`：<https://github.com/11NU0/Terraria-1.4.5.8-decomp-recomp/blob/main/_src/Terraria/Framing.cs>
- `TileDrawing.cs`：<https://github.com/11NU0/Terraria-1.4.5.8-decomp-recomp/blob/main/_src/Terraria.GameContent.Drawing/TileDrawing.cs>
- `TileObjectData.cs`：<https://github.com/11NU0/Terraria-1.4.5.8-decomp-recomp/blob/main/_src/Terraria.ObjectData/TileObjectData.cs>

> 说明：该仓库是第三方反编译/重编译工程，不能单独当“官方源码”。本文每个关键结论都以**本地 `Terraria.exe` 的 IL/反编译**为准，仓库只用于对照方法名和代码结构；两者在关键方法上一致。

### 1.3 游戏自带贴图（独立验证 frame 布局）

- `D:\steam\steamapps\common\Terraria\Content\Images\Tiles_19.xnb`
- XNB 头是压缩的，但解压后大小字段为 **2,449,617 字节**；`486 × 1260 × 4 = 2,449,440`，加约 177 字节头，正好对上。
- 因此平台贴图是 **486 像素宽 = 27 列 × 18 像素**，**1260 像素高 = 70 行 × 18 像素**；每格 18×18，游戏绘制时取左上 16×16。
- 27 列对应 `frameX/18 = 0..26`；按尺寸推算 70 行对应 `frameY/18 = 0..69`（type 19 的款式数，见 3.1 的团队平台例外）。

### 1.4 关键 IL/代码位置（本地 `Terraria.exe`）

| 事实 | 方法 | 本地 IL/代码位置 |
|---|---|---|
| `slope()` 取 `sTileHeader & 0x7000 >> 12` | `Terraria.Tile::slope()` | `IL_0000`–`IL_0010` |
| `halfBrick()` 取 `0x0400` 位 | `Terraria.Tile::halfBrick()` | `IL_0000`–`IL_0013` |
| `blockType()` 把 slope 映射到 0–5 | `Terraria.Tile::blockType()` | `IL_0000` 起 |
| 平台 frameX 的完整分支 | `Terraria.WorldGen::TileFrameImportant` | `IL_0B62`–`IL_1120` |
| 平台 frameX 写入 | 同上 | `IL_1120` |
| `PlaceTile` 的 `type==19` 分支 | `Terraria.WorldGen::PlaceTile` | `IL_2601`–`IL_2621` |
| `PlaceTile` 公共尾部 `SquareTileFrame` | 同上 | `IL_26DF` |
| 平台锤击循环 | `Terraria.WorldGen::PoundPlatform` | `IL_0000`–`IL_019B` |
| `PlatformProperTopFrame` 的 0..7/12..16/25..26 | `Terraria.WorldGen::PlatformProperTopFrame` | `IL_0000`–`IL_002B` |

> IL 偏移来自本地 `Terraria.exe`（1.4.5.8）的 Cecil dump；不同编译/补丁版本偏移会变，方法名和逻辑不变。

---

## 2. 普通物块的锤子派生（非平台）

### 2.1 位布局

`Tile.slope()` 的 IL（本地二进制）：

```
ldfld   uint16 Tile::sTileHeader
ldc.i4  28672          // 0x7000
and
ldc.i4.s 12
shr
conv.u1
ret
```

即：

```csharp
public byte slope() {
    return (byte)((sTileHeader & 0x7000) >> 12);
}
public void slope(byte slope) {
    sTileHeader = (ushort)((sTileHeader & 0x8FFF) | ((slope & 7) << 12));
}
```

`halfBrick()` 用 `0x0400` 位：

```csharp
public bool halfBrick() { return (sTileHeader & 0x0400) == 0x0400; }
```

### 2.2 slope 0–4 的方向

`Tile.blockType()` 把“半砖/坡”统一成 0–5：

```csharp
public int blockType() {
    if (halfBrick()) return 1;
    int num = slope();
    if (num > 0) num++;
    return num;
}
```

常量：

```csharp
public const int Type_Solid = 0;
public const int Type_Halfbrick = 1;
public const int Type_SlopeDownRight = 2;
public const int Type_SlopeDownLeft = 3;
public const int Type_SlopeUpRight = 4;
public const int Type_SlopeUpLeft = 5;
```

`Framing.blockStyleLookup`（1.4.5.8 的实际表）给出了每个 `blockType()` 的实心边：

```csharp
blockStyleLookup[0] = new BlockStyle(up:true,  down:true,  left:true,  right:true ); // 整块
blockStyleLookup[1] = new BlockStyle(up:false, down:true,  left:true,  right:true ); // 半砖
blockStyleLookup[2] = new BlockStyle(up:false, down:true,  left:true,  right:false); // SlopeDownRight
blockStyleLookup[3] = new BlockStyle(up:false, down:true,  left:false, right:true ); // SlopeDownLeft
blockStyleLookup[4] = new BlockStyle(up:true,  down:false, left:true,  right:false); // SlopeUpRight
blockStyleLookup[5] = new BlockStyle(up:true,  down:false, left:false, right:true ); // SlopeUpLeft
```

对应表：

| `slope()` | `halfBrick()` | `blockType()` | 实心区域 | 对角线 | 名称 |
|---:|---:|---:|---|---|---|
| 0 | false | 0 | 全格 | 无 | 整块 |
| 0 | true | 1 | 下半（y=8..15） | 水平 | 半砖 |
| 1 | false | 2 | 左 + 下 | `\`（左上 → 右下） | SlopeDownRight |
| 2 | false | 3 | 右 + 下 | `/`（右上 → 左下） | SlopeDownLeft |
| 3 | false | 4 | 左 + 上 | `/`（右上 → 左下） | SlopeUpRight |
| 4 | false | 5 | 右 + 上 | `\`（左上 → 右下） | SlopeUpLeft |

辅助判定（`Tile.cs` 已验证）：

| 方法 | 为 true 的 slope |
|---|---|
| `topSlope()` | 1, 2（实心在下方，可站） |
| `bottomSlope()` | 3, 4（实心在上方，顶面朝下） |
| `leftSlope()` | 2, 4 |
| `rightSlope()` | 1, 3 |

### 2.3 锤击循环（普通块）

`Player.HammerTile`（本地反编译，1.4.5.8）的普通块分支：

1. 未锤过：`PoundTile` → `halfBrick=true`
2. 已半砖或已有坡：
   - 上方实心、下方不实心 → 先走上坡 3/4；否则先走下坡 1/2；
   - `slope 0 → num12 → num13 → num12+2 → num13+2 → 0`
   - 其中 `num12/num13` 为 1/2 或 2/1（按左右邻块方向交换）。
3. 即完整循环：**整块 → 半砖 → 坡1 → 坡2 → 坡3 → 坡4 → 整块**。

> 这条对“打印”很重要：不能只看 `slope` 忘了 `halfBrick`；也不能把半砖当成 `slope=1`。

### 2.4 Renderer 现在的坡方向是反的

`Renderer.DrawSlopeMark`（`tool/src/Renderer.cs:339-353`）：

```csharp
bool top = slope >= 3;
bool risingRight = (slope == 1 || slope == 3);
...
py2 = risingRight
    ? (top ? dy + 1 + i : dy + 15 - i)
    : (top ? dy + 15 - i : dy + 1 + i);
```

逐方向算出来：

| `slope` | Renderer 实际画的线 | 正确线 | 结论 |
|---:|---|---|---|
| 1 | `/`（左下 → 右上） | `\`（左上 → 右下） | ❌ 反了 |
| 2 | `\` | `/` | ❌ 反了 |
| 3 | `\` | `/` | ❌ 反了 |
| 4 | `/` | `\` | ❌ 反了 |

也就是说 **1↔2、3↔4 的对角线正好互换**。这个结论同时由 `blockStyleLookup` 和 `TileDrawing.DrawBasicTile` 的 8 条竖条裁剪算法交叉验证。

---

## 3. 平台的锤子派生（重点）

### 3.1 哪些 tile 是平台

1.4.5.8 的 `TileID.Sets.Platforms`（`TileID.Sets` 静态表）：

```csharp
public static bool[] Platforms = Factory.CreateBoolSet(19, 427, 435, 436, 437, 438, 439);
```

| TileID | 名称 |
|---:|---|
| 19 | Platforms（木平台，款式最多） |
| 427 | TeamBlockRedPlatform |
| 435 | TeamBlockGreenPlatform |
| 436 | TeamBlockBluePlatform |
| 437 | TeamBlockYellowPlatform |
| 438 | TeamBlockPinkPlatform |
| 439 | TeamBlockWhitePlatform |

注意：`380` 是 **PlanterBox（种植盒）**，不是平台；它也有 `frameY=18*style` 的放置分支，但不要按平台 frame 表处理。

另外，`PlaceTile` 只对 `type == 19`（和 380）显式写 `frameY = 18*style`；团队平台 427/435–439 走 `default` 分支，不设 `frameY`，它们实际上只有 1 个款式（`frameY=0`，style 0）。所以“70 种款式”只适用于 type 19，不要套到团队平台上。

### 3.2 三个字段各管什么

| 字段 | 平台上的含义 | 证据 |
|---|---|---|
| `frameY` | **材质/款式**，`frameY = 18 × style` | `WorldGen.PlaceTile` 的 `type == 19` 分支；`ReplaceTile_DoActualReplacement_Single` |
| `frameX` | **形状/连接变体**，0..468 步长 18 | `WorldGen.TileFrameImportant` 平台分支 |
| `slope()` | 楼梯碰撞方向：0 平放、1 右下坡、2 左下坡；3/4 可由 block swap 保留 | `WorldGen.PoundPlatform`；平台分支只按 1/2 选 frameX；3/4 见 4.3 实测 |
| `halfBrick()` | 半砖平台 | `WorldGen.PoundPlatform`；`TileDrawing` 的 `halfBrickHeight=8` |

平台 `slope` 1/2 与普通块 `slope` 1/2 的**方向语义相同**（1 = 右下坡，2 = 左下坡），只是平台的 `frameX` 编码和普通块不同；**单纯用锤子敲平台不会产生 3/4，但平台格可以因 block swap 带 3/4（实测 85/150，见 4.3）**。

> **关键纠正**：`frameY` **不是**形状。平台被锤成楼梯时，`frameY` 仍然是 `18 × style`；形状在 `frameX`，碰撞在 `slope`。这跟“frameX/frameY 组合表示平放/端帽/楼梯”的直觉不完全一样。

### 3.3 平台贴图是 27 列 × 18 像素

- 贴图：`Tiles_19.xnb`，解压尺寸约 2,449,617 字节。
- 486 宽 = 27 × 18；1260 高 = 70 × 18。
- 每格 18×18；游戏实际绘制 `new Rectangle(frameX, frameY, 16, 16)`，只取左上 16×16。
- `TileObjectData.PlatformFrameWidth()` 的实现是 `return _data[19].CoordinateFullWidth;`，配合上面的 18px 格宽；`WorldGen.PlatformProperTopFrame` 也直接用这个宽度做除法。
- 27 列中 **只有 23 列是正常 tile.frameX**；`162/198/306/324` 这 4 列是斜坡边缘的 **额外背面补片**（`TileDrawing` 用同一张贴图在 `Layer_BehindTiles` 再画一块 16×16，不是 glowmask），不是正常框定会写出的值。

`TileDrawing` 里的证据：

```csharp
// 平台 slope==1 且右下有实心块时，额外画一层：
Rectangle sourceRectangle = new Rectangle(198, drawData.tileFrameY, 16, 16);
if (TileID.Sets.Platforms[tile3.type] && tile3.slope() == 0)
    sourceRectangle.X = 324;
...
// slope==2 且左下有实心块：
Rectangle sourceRectangle2 = new Rectangle(162, drawData.tileFrameY, 16, 16);
if (TileID.Sets.Platforms[tile2.type] && tile2.slope() == 0)
    sourceRectangle2.X = 306;
```

这解释了为什么 `162/198/306/324` 不在 `TileFrameImportant` 的正常 frameX 输出里。

### 3.4 平台 frameX 完整表（正常 tile.frameX，23 个值）

按数值排序的 23 个正常值：

```text
0, 18, 36, 54, 72, 90, 108, 126,
144, 180,
216, 234, 252, 270, 288,
342, 360, 378, 396, 414, 432, 450, 468
```

> 用户实测蓝图里 150 个平台格只出现了 `0,18,36,54,72,90,144,180,360`，全部在这个表内，等于给这张表做了真数据背书。
>
> 当平台 `slope()==3` 或 `4` 时，`TileFrameImportant` 的平台分支只判断 `==1` / `==2`，3/4 会落到“平放”分支；所以 **3/4 的平台 frameX 取平放族（0/18/36/.../288）**，不会出现 342..468 那组楼梯帧。游戏绘制也把 3/4 当平放 frame 画（半砖则再裁 8px）。详见 3.5 与 4.3。

下表中：

- `L` / `R`：左/右邻是**平台**（任意平台类型；代码会把其它平台类型映射成当前 `type`，所以混合平台也算连接）。
- `BL` / `BR`：左/右邻是**可连接的实体块**（非平台，`Main.tileSolid`，通过 cull）。
- `UL1`：左上角是**同型**平台且 `slope()==1`；`UR2`：右上角是**同型**平台且 `slope()==2`。
- “连接”指 `tile.active() && !CullLeft/Right`；“cull”是 1.4.5 的 `GetTileMergeCulling` 结果。
- “上方禁止斜坡”指 `ForbidsSloping(x, y-1)` 为 true（例如上方是树/家具等）。
- 对 `L/R` 还要求“半砖状态相同”：代码里如果邻居平台的 `halfBrick()` 与当前 tile 不同，会把该侧 `num9/num8` 置 -1，归到“不连接”的情况。

#### 3.4.1 平放 `slope()==0`（13 个值）

| `frameX` | 条件（简化） | 用途 |
|---:|---|---|
| 0 | L 且 R，且两侧都没有“斜坡邻接”特例 | 平放中段 |
| 18 | L，R 空；L 不是 slope2 | 右端帽 |
| 36 | R，L 空；R 不是 slope1 | 左端帽 |
| 54 | BL + R | 左边贴实体块、右边接平台 |
| 72 | L + BR | 左边接平台、右边贴实体块 |
| 90 | 孤立 | 单格平台 |
| 108 | BL，R 空 | 左边贴实体块、右边空 |
| 126 | BR，L 空 | 右边贴实体块、左边空 |
| 216 | L + R；L 是 slope2，R 不是 slope1 | 平放，左接楼梯 |
| 234 | L + R；R 是 slope1，L 不是 slope2 | 平放，右接楼梯 |
| 252 | L + R；L 是 slope2 且 R 是 slope1 | 平放，两侧接楼梯 |
| 270 | L，R 空；L 是 slope2 | 右端接楼梯 |
| 288 | R，L 空；R 是 slope1 | 左端接楼梯 |

#### 3.4.2 当前 tile 是斜坡 1（`slope()==1`，5 个值）

| `frameX` | 条件（简化） | 用途 |
|---:|---|---|
| 468 | R 是平放平台（任意平台类型，slope0、非半砖、未 cull），或上方禁止斜坡 | 斜坡 1 接右侧平放 |
| 360 | 右下空/被 cull，或右下是平台且 slope2（未 cull）；且左侧有块或 UL1 | 斜坡 1，左侧有连接 |
| 432 | 同上，但左侧无块/无 UL1 | 斜坡 1，左侧无连接 |
| 180 | 右下非空且不是“平台 slope2”；且左侧有块或 UL1 | 斜坡 1，右下有阻挡、左连接 |
| 396 | 同上，但左侧无块/无 UL1 | 斜坡 1，右下有阻挡、左无连接 |

#### 3.4.3 当前 tile 是斜坡 2（`slope()==2`，5 个值）

| `frameX` | 条件（简化） | 用途 |
|---:|---|---|
| 450 | L 是平放平台（任意平台类型，slope0、非半砖、未 cull），或上方禁止斜坡 | 斜坡 2 接左侧平放 |
| 342 | 左下空/被 cull，或左下是平台且 slope1（未 cull）；且右侧有块或 UR2 | 斜坡 2，右侧有连接 |
| 414 | 同上，但右侧无块/无 UR2 | 斜坡 2，右侧无连接 |
| 144 | 左下非空且不是“平台 slope1”；且右侧有块或 UR2 | 斜坡 2，左下阻挡、右连接 |
| 378 | 同上，但右侧无块/无 UR2 | 斜坡 2，左下阻挡、右无连接 |

> 备注：`360/432/180/396` 和 `342/414/144/378` 的区分点是“左侧/右侧连接”和“右下/左下是否有阻挡”。这是**代码条件**的准确翻译；具体像素外观建议实机截图逐值核对（见第 8 节）。

### 3.5 平台锤击循环

`WorldGen.PoundPlatform`（本地 IL + 反编译一致）：

1. `halfBrick()==true` → `PoundTile` → 回到平放。
2. `slope()==0` → `SlopeTile(x,y,num)`，`num` 默认 1；若右上有平台、或左下是平台、或右实心且左不实心，则 `num=2`。
3. `slope()==num` → `SlopeTile(x,y,other)`（1↔2）。
4. 其它（即已经是另一个坡）→ `SlopeTile(x,y,0)` 再 `PoundTile` → `halfBrick=true`。

所以平台锤击循环是：

**平放 → 坡1/2（按邻接自动选方向）→ 另一个坡 → 半砖 → 平放**

**注意**：这里说的是“用锤子敲平台”这条路径，它只产生 0/1/2。平台格**确实可以**带 `slope 3/4`，来源不是锤平台，而是 **block swap**：实体块先被锤成上坡 3/4，再用平台替换，`slope` 被保留下来；此时 frameX 仍按平放族算（3/4 走平放分支）。实测 150 个平台格里有 85 个是 3/4，见 4.3。

游戏对这种“平台 + slope 3/4 + 平放 frameX”的画法：`TileDrawing.DrawBasicTile` 的平台分支只要 `slope()>0` 就画 `normalTileRect`（frameX/frameY 的 16×16；半砖则取 8px 并下移），只有 slope 1/2 才会额外画楼梯边缘补片；3/4 **不会裁切、不会旋转**，视觉上就是平放平台。也就是说碰撞坡向和视觉不一致，这是游戏自身行为。

### 3.6 `PlatformProperTopFrame`（碰撞/附着用）

```csharp
public static bool PlatformProperTopFrame(short frameX) {
    int num = frameX / TileObjectData.PlatformFrameWidth(); // 18
    if ((num < 0 || num > 7) && (num < 12 || num > 16)) {
        if (num >= 25) return num <= 26;
        return false;
    }
    return true;
}
```

`frameX/18` 为 **0..7、12..16、25..26** 时返回 true。它用于“这个平台顶面是否在格子上沿”的碰撞/附着判断；**不要**把它当成“平放”的完整定义（450/468 这类接平放的斜坡帧也会返回 true）。

### 3.7 半砖平台怎么画

`TileDrawing.GetTileDrawData` 对任何 `halfBrick()` 都设 `halfBrickHeight = 8`；绘制时基础逻辑是：

- 源矩形高度变成 `16 - 8 = 8`（取 frame 顶部 8 像素）；
- 目标 Y 加 `tileTop + 8`（下移 8 像素）。

对**平台**，`DrawBasicTile` 直接画 `normalTileRect`（就是这 8px）到 `normalTilePosition`，所以可以简单理解为“平台半砖 = frame 顶部 8px 下移 8px”。

对**普通块**，游戏在“下方没有实心块/下方也是半砖”时还会把下半段拆成两段 4px（源 `frameY+66` 附近）来拼，以处理平滑边缘；不是所有情况下都只是简单 8px 裁切。平台没有这个普通块拼接分支。

```text
平台半砖 = frameX 仍是平放族（0/18/36/...）；
           frameY 仍是 18*style；
           slope=0, halfBrick=1；
           绘制时取 (frameX, frameY, 16, 8) 画到 (x, y+8)。
```

### 3.8 `PlaceTile` 的平台分支（打印必须知道）

`WorldGen.PlaceTile` 里 `type == 19` 的分支反编译为：

```csharp
case 19:
    tile.frameY = (short)(18 * style);
    tile.active(true);
    tile.type = 19;
    break;   // 跳出 switch，走 PlaceTile 的公共尾部
...
// 公共尾部（所有类型共用）：
if (tile.active()) {
    SquareTileFrame(i, j);   // ← 平台也会在这里被框定
    result = true;
}
```

也就是说：**平台分支本身不写 frameX，但 `PlaceTile` 末尾会统一调 `SquareTileFrame(i,j)`**，按当前邻居算一个合法的 frameX。这个 frame 是“平放”的（因为此时 `slope()==0`），不是蓝图里的楼梯帧。

（`380` 种植盒也有类似 `frameY=18*style` 分支，但它是另一类。）

这直接说明：

- 打印机必须把 `style = frameY/18` 传进去，否则所有平台都会变成 0 号款式；
- `PlaceTile` 末尾的 `SquareTileFrame` 只按“当前邻居 + 当前 slope”框一次；此时 slope 还是 0，所以得到的是平放帧。要还原楼梯/半砖，必须自己先写 `slope/halfBrick`，再重新 `SquareTileFrame`，最后覆盖蓝图 `frameX/frameY`。

---

## 4. “锤过的平台并用物块替代”到底是什么

这是用户原话里最模糊的一条。按 1.4.5.8 的代码，最可能是 **block swap（方块替换）** 这一路径。相关代码：

`Player.PlaceThing_TryReplacingTiles` → `WorldGen.ReplaceTile` → `ReplaceTile_DoActualReplacement_Single`。

关键逻辑（简写）：

```csharp
int type = t.type;          // 旧类型
t.type = (ushort)targetType; // 新类型
if (TileID.Sets.Platforms[t.type] || t.type == 380)
    t.frameY = (short)(targetStyle * 18);
...
bool flag = !CanPoundTile(topLeftX, topLeftY);
if (TileID.Sets.Platforms[type] && TileID.Sets.Platforms[t.type])
    flag = false; // 平台换平台：不强制清坡
if (flag) {
    t.slope(0);
    t.halfBrick(false);
}
SquareTileFrame(topLeftX, topLeftY);
```

### 4.1 平台 → 另一种平台

- `frameY = 18 × 新 style`；
- `frameX` 由 `SquareTileFrame` 重算；
- `slope/halfBrick` **保留**（平台换平台时 `flag` 被强制为 false）。
- 结果：同形状、换材质。抓取时 `type/frameX/frameY/slope/halfBrick` 都在，可还原。

### 4.2 平台 → 实体块

- `type` 变成实体块；
- `frameY` 不再按平台设置；`SquareTileFrame` 对非 frame-important 方块会重算 `frameX/frameY`（普通方块纹理帧，不是平台形状）；
- `slope/halfBrick` 在 `CanPoundTile` 为 true 时**保留**。
- 结果是一个“带坡/半砖的普通实体块”，这本身就是合法的锤子状态；`slope`/`halfBrick` 已经能记录，不需要额外字段。
- 这也解释了用户看到的“平台被物块替代后还是斜的/半的”：不是平台 frame 残留，而是 **slope/halfBrick 被 block swap 保留**。

### 4.3 实体块 → 平台：slope 3/4 会保留（✅ 用户蓝图实测）

- `frameY = 18 × style`；
- `slope/halfBrick` 同样在 `CanPoundTile` 为 true 时保留；
- 如果旧实体块是 slope 3/4（上坡），平台格会带着 3/4 碰撞；但 `TileFrameImportant` 的平台分支只处理 1/2，3/4 走平放分支，`frameX` 变成平放族（0/18/36/.../288），视觉上游戏也照平放 frame 画。
- 这就是“平台 + slope 3/4 + 平放 frameX”的怪状态，也正是用户说的“锤过的平台并用物块替代”的一类。

**实测证据（用户真实蓝图）**：

- 文件：`D:\steam\steamapps\common\Terraria\Blueprints\building_07.bp`，同目录 `text.bp`；两份统计一致。
- 共 150 个 `type=19` 平台格；`frameY` 只有 `252`（style 14，145 格）和 `900`（style 50，5 格），再次印证 `frameY=18×style` 是材质。
- `frameX` 只出现 `0,18,36,54,72,90,144,180,360`，全部在 3.4 的 23 个正常值内。
- 其中 30 个 `halfBrick=1`。
- **85 个平台格的 `slope` 是 3 或 4**：

| slope | 格数 | 实际 frameX 分布 |
|---:|---:|---|
| 3 | 65 | `0`×59, `18`×2, `36`×1, `54`×2, `72`×1 |
| 4 | 20 | `0`×16, `54`×2, `72`×2 |

- 这 85 格全部 `halfBrick=0`，`frameX` 绝大多数是 `0`（平放）。
- 用户原统计与本次逐行解析一致，已从“🟡 推测”升级为“✅ 已验证（至少‘会保留’这一半）”。

> changelog 有“Block swapping a platform onto a sloped block will now clear the slope”的说法，但 1.4.5.8 的实际 `ReplaceTile` 代码和用户蓝图都显示会保留；可能 changelog 指别的方向/版本，别拿它覆盖实测。

### 4.4 结论

- 用户说的“锤过的平台并用物块替代”大概率就是 **block swap**；其中平台→实体块保留 `slope/halfBrick`，实体块→平台也会保留 slope 3/4，不是新的编码。
- 真正需要修的不是“再多存一个平台形状字段”，而是 **打印时把 slope/halfBrick 写回去**（平台 3/4 原样写回，不能 clamp），以及 **平台 frameX/frameY 的恢复**。
- 如果有人看到“物块替代后仍有平台边缘贴图”，那更可能是 `SquareTileFrame` 没跑/旧 frame 残留；正常 `ReplaceTile` 最后会 `SquareTileFrame`。

---

## 5. 现状诊断（逐文件）

### 5.1 `WorldUtil.cs`：抓取层其实已经存了 frame

`CellFromTile`（第 53–68 行）：

```csharp
c.type = t.active() ? t.type : (ushort)0;
c.wall = t.wall;
c.frameX = t.frameX;   // ← 已经存了
c.frameY = t.frameY;   // ← 已经存了
c.hasFrame = true;
...
c.slope = t.slope();
c.halfBrick = t.halfBrick();
```

`Blueprint.Save` 也写 `frameX,frameY`，`Load` 在字段数 ≥6 时读回并置 `hasFrame=true`。所以：

- **平台 frameX/frameY 不是“没抓到”**；新选/新保存的蓝图里是有帧的。
- 真正的问题是：`Printer` 不用这些帧；`Renderer` 画的时候又叠了错误提示线；半砖没有按游戏方式下移。

抓取层唯一值得补的，是**语义/校验**（不是新字段）：

- 增加 `IsPlatform(ushort type)` 辅助；
- 对 `slope` 做合法性检查（只把 >4 归 0；平台 3/4 合法，不收敛到 0..2）；
- 记录/跳过 `slope` 与 `halfBrick` 同时为真的异常状态（正常锤击不会产生，block swap 边界可能）；
- 可选：在日志里打印平台 `type/frameX/frameY/slope/halfBrick`，便于实机核对。

### 5.2 `Blueprint.cs`：不需要加字段

现有字段：

```csharp
public ushort type, wall;
public short frameX, frameY;
public bool hasFrame;
public byte wire;
public bool actuator;
public byte slope;
public bool halfBrick;
public byte paint, wallPaint;
```

平台还原所需信息已经齐全：

| 需要的信息 | 现有字段 |
|---|---|
| 平台类型（19/427/435-439） | `type` |
| 材质/款式 | `frameY`（`frameY/18`） |
| 形状/连接变体 | `frameX` |
| 楼梯碰撞 | `slope` |
| 半砖 | `halfBrick` |

**不建议**再加 `platformStyle`/`shape`/`frame` 之类字段：会破坏旧蓝图兼容，且 `frameX/frameY` 已经是官方编码。

兼容性注意：

- 旧 4 字段蓝图：`hasFrame=false`，平台无法知道款式/形状，只能画外框；这是数据缺失，不是 bug。
- 旧 6 字段蓝图：有 `frameX/frameY`，但没有 `slope/halfBrick`（默认 0/false），楼梯/半砖形状会丢；只能重新抓取。
- 新 12 字段蓝图：完整。
- 建议顺手修 `Load`：`hasFrame = frameX >= 0 && frameY >= 0`。现在只要字段数 ≥6 就置 true，遇到旧文件里的 `-1` 会走“有帧”分支画出错误款式；改成上面的判断后会退回 `FrameOf`/外框。

### 5.3 `Renderer.cs`：平台贴图本身是对的，但叠加提示线是错的

`DrawCellTexture` 已经用 `cell.frameX/frameY` 画 16×16：

```csharp
src = new Rectangle(fr.X, fr.Y, 16, 16);
...
sb.Draw(tex, dst, src, tint);
```

问题：

1. **平台坡**：贴图 frame 已经是楼梯形状；后面又调用 `DrawSlopeMark`，画了一条**方向相反**的虚线，视觉上像“错了”。平台 slope 3/4 则是游戏照平放 frame 画，投影应照画（可加小角标）。
2. **普通块坡**：贴图是整块，游戏通常靠 8 条 2px 竖条裁出坡（例外：`TileID.Sets.HasSlopeFrames` 的 421/422 用自带坡帧）；当前只画整块 + 一条反向虚线，所以看起来不像坡。
3. **半砖**：游戏是“取 frame 顶部 8px，画到 y+8”；当前画完整 16×16 再画一条横线，位置不对。
4. **平台半砖**：同上，而且平台 frame 的 16×16 贴图直接画在格子上沿，看起来像平放平台，不像半砖。
5. **无帧平台**：`noStyle` 只画白框，这个策略是对的（避免把史莱姆平台画成木平台），保留。

### 5.4 `Printer.cs`：平台根本没被打印

候选过滤（第 69–74 行）：

```csharp
if (c.type >= Main.tileFrameImportant.Length) continue;
if (Main.tileFrameImportant[c.type]) continue; // 家具/火把交给手动
```

平台是 frame-important（`TileFrame` 会走 `TileFrameImportant`，平台分支就在里面），所以**平台在第一行就被全部跳过**。

即使放开，当前放置：

```csharp
WorldGen.PlaceTile(bestX, bestY, type, false, false, pl.whoAmI, 0);
```

- style 固定 0 → 所有平台都是 0 号材质；
- 没有写 `slope/halfBrick`；
- 没有写 `frameX`；
- `PlaceTile` 末尾会调 `SquareTileFrame`，但那时 `slope/halfBrick` 仍是 0，所以算出来的是平放帧；不是蓝图里的楼梯帧。

结果：平台要么不放，要么放成“0 号材质的默认帧”，锤子形状全丢。

### 5.5 `BlueprintOps.Verify`：只验证 type/wall，不验证形状

`Verify` 只比较 `t.type`/`t.wall`，不比较 `slope/halfBrick/frameX/frameY`。所以打印出来的平台形状错了，验证仍可能显示“完美”。这也是为什么问题一直没被发现。

### 5.6 `SnapPlace` / 手动放置：也不会恢复形状

`SnapPlace.Apply` 只改 `Player.tileTargetX/Y`，真正放置仍走游戏原生 `Player.PlaceThing`，用的是**手持物品的 placeStyle**，不会读蓝图里的 `frameY/frameX/slope/halfBrick`。所以即使用户靠“投影吸附”手动放平台，平台也只会是当前物品的款式/平放形状。这个问题不在 `Printer` 路径内，但属于同一类“蓝图形状没有参与放置”的缺口；如果下一阶段要彻底解决，手动放置也需要一个“放完后按蓝图校正”的钩子（或至少提示用户平台请用打印机）。

---

## 6. 改造方案（下一阶段建议）

### 6.1 总原则

1. **Blueprint 不加字段**；`frameX/frameY/slope/halfBrick` 就是平台形状的完整描述。
2. **打印顺序**：先按普通方块/平台把格子放出来；所有格子放完后做一次“形状/帧收尾”，避免后放的邻居把先放的 frameX 重算掉。
3. **平台特殊**：`style = frameY/18`；`frameY` 最终要写回；`frameX` 最终按蓝图覆盖；`slope/halfBrick` 先写再 `SquareTileFrame`。
4. **普通块特殊**：`slope/halfBrick` 写回即可；普通块不依赖 `frameX` 表达坡（坡是绘制时裁的），但 `SquareTileFrame` 仍要跑，保证纹理连接正确。
5. **渲染**：平台直接用 frame 贴图；普通块坡按游戏算法裁 8 条；半砖统一裁 8px 下移；删除/修正错误提示线。

### 6.2 `WorldUtil`：只加辅助，不改数据结构

建议加：

```csharp
public static bool IsPlatform(ushort type) {
    return type < TileID.Sets.Platforms.Length && TileID.Sets.Platforms[type];
}
```

`CellFromTile` 保持现有字段；可选加：

```csharp
byte sl = t.slope();
if (sl > 4) sl = 0;                       // 平台 3/4 合法，不能收敛到 0..2
if (!IsPlatform(c.type) && c.halfBrick)   // 普通块半砖优先；平台保留原 slope
    sl = 0;
c.slope = sl;
```

但**不要**在这里把平台帧“归一化”掉；原样保存，打印时再决定是否按邻居重算。

### 6.3 `Renderer`：按类型分三条绘制路径

#### A. 平台（`TileID.Sets.Platforms[type]`）

```csharp
bool platform = WorldUtil.IsPlatform((ushort)op.texType);
Rectangle src = new Rectangle(fr.X, fr.Y, 16, 16);
Rectangle dst = new Rectangle(dx, dy, 16, 16);

if (platform && op.halfBrick) {
    src.Height = 8;   // 取 frame 顶部 8px
    dst.Y += 8;       // 下移 8px
    dst.Height = 8;
}
// 平台 slope 1/2：frameX 已经是楼梯形状，直接画。
// 平台 slope 3/4：游戏照平放 frame 画（不裁不转），投影也照画。
// 额外加一个 3x3 半透明角标提示 3/4 的隐藏坡向，不改变形状主体。
```

- 有帧：直接画。
- 无帧：保留现在的 `noStyle` 白框。
- 平台半砖：按上面裁切；不要再画 `DrawHalfBrickMark`。
- 平台 slope 3/4：**不要**按普通块 8 条竖条裁（游戏不裁）；照 frame 画，再加 `DrawPlatformSlopeCorner` 的 3px L 角标（slope3 左上、slope4 右上）。
- 衬底（`C_BACKDROP`）也应使用同一个裁剪矩形，否则空白半边仍会有一块深色底，看起来还是整格。

#### B. 普通块坡（`op.slope != 0`）

先判断 `TileID.Sets.HasSlopeFrames[type]`：1.4.5.8 里只有 421/422 这类“自带坡帧”的方块，它们直接画 frame，不要裁条。其余普通块按 `TileDrawing.DrawBasicTile` 的算法画 8 条 2px 竖条：

```csharp
for (int i = 0; i < 8; i++) {
    int offX, offY, h, srcY;
    switch (op.slope) {
    case 1:
        offX = i * 2; offY = i * 2; h = 14 - i * 2;
        srcY = 0; break;
    case 2:
        offX = 16 - i * 2 - 2; offY = i * 2; h = 14 - i * 2;
        srcY = 0; break;
    case 3:
        offX = i * 2; offY = 0; h = 16 - i * 2;
        srcY = i * 2; break;
    default: // 4
        offX = 16 - i * 2 - 2; offY = 0; h = 16 - i * 2;
        srcY = i * 2; break;
    }
    sb.Draw(tex,
        new Rectangle(dx + offX, dy + offY, 2, h),
        new Rectangle(fr.X + offX, fr.Y + srcY, 2, h),
        tint);
}
```

（上面是 `TileDrawing` 的等价写法；`case 1/2` 的 `srcY=0`、`h=14-2i`，`case 3/4` 的 `srcY=2i`、`h=16-2i`。）

画坡时**不要先画完整 16×16**，否则空白半边会被填满；直接用这 8 条竖条组成坡。

- 这样普通块的坡会显示成真正的三角形/斜边，而不是整块 + 一条线。
- 不需要 `DrawSlopeMark`。

#### C. 普通块半砖

```csharp
if (!platform && op.halfBrick) {
    src.Height = 8;
    dst.Y += 8;
    dst.Height = 8;
}
```

简单做法是这样裁 8px 下移，视觉上已经接近；要**完全一致**，普通块还要按 `TileDrawing` 在“下方无实心块/下方也是半砖”时的两段 4px 拼接分支画（源第二段在 `frameY + 66` 附近）。平台不需要这个拼接分支。

#### D. 删除/保留提示线

- 建议**删除** `DrawSlopeMark` / `DrawHalfBrickMark` 的调用。
- 如果担心贴图缺失时没有形状提示，可以只在 `tex == null` 的兜底路径画一个**方向正确**的线：
  - slope 1 → `\`；2 → `/`；3 → `/`；4 → `\`。
- 平台坡永远不要叠线；平台 frame 已经是正确形状。

### 6.4 `Printer`：允许平台 + 写回形状 + 收尾

> 实际第二批实现把候选选择从“单候选”改成了 `bestAvailable`/`bestAny` 双候选，避免最近格缺料时整机卡住；详见 `notes/printer-selection.md`。下面保留最初方案，形状/帧处理部分仍然适用。

#### 1）候选过滤

```csharp
bool platform = WorldUtil.IsPlatform(c.type);
if (c.type >= Main.tileFrameImportant.Length) continue;
if (Main.tileFrameImportant[c.type] && !platform) continue; // 家具/火把仍交给手动
```

#### 2）放置时传 style

```csharp
int style = 0;
if (platform && c.hasFrame && c.frameY >= 0)
    style = c.frameY / 18;      // 平台材质
if (!WorldGen.PlaceTile(bestX, bestY, type, false, false, pl.whoAmI, style))
    return false;
```

#### 3）放置后立即写形状

```csharp
Tile t = Main.tile[bestX, bestY];
t.slope(c.slope);
t.halfBrick(c.halfBrick);
WorldGen.SquareTileFrame(bestX, bestY, true);   // 按当前邻居先框一次

if (platform) {
    if (c.hasFrame && c.frameY >= 0) {
        t.frameY = c.frameY;   // 原样恢复（兼容非 18 倍数的旧值）
        if (c.frameX >= 0) t.frameX = c.frameX;
    } else {
        t.frameY = (short)(style * 18);
    }
}
```

注意顺序：

1. 先 `slope/halfBrick`；
2. 再 `SquareTileFrame`（让 frameX 至少是当前邻居下的合理值）；
3. 最后覆盖 `frameY/frameX`（精确还原）。

如果先写 frameX 再 `SquareTileFrame`，会被重算掉。

平台 `slope=3/4` 必须原样写回；只有普通块才执行“半砖时 slope 归 0”。不要在这里把平台 slope 收敛到 0..2。

#### 4）收尾修复（关键）

因为 `SquareTileFrame` 会框 3×3，后放的平台/方块会把先放好的邻居 frameX 重算掉。建议加一个“收尾”阶段：

```csharp
// 伪代码
bool hasPlaceable;
if (TryPlaceBlock(pl, bp, out hasPlaceable)) {
    _shapeDirty = true;
    return;
}
if (!hasPlaceable && _shapeDirty) {
    RepairShapes(bp);
    _shapeDirty = false;
}
```

`RepairShapes` 分两遍：

**第一遍：写 slope/halfBrick + 让游戏按当前邻居框一次**

```csharp
foreach (BpEntry e in bp.Entries) {
    if (e.cell.type == 0) continue;
    int x = bp.OriginX + e.bx;
    int y = bp.OriginY + e.by;
    Tile t;
    if (!WorldUtil.TryTile(x, y, out t)) continue;
    if (!t.active() || t.type != e.cell.type) continue;

    t.slope(e.cell.slope);
    t.halfBrick(e.cell.halfBrick);

    if (WorldUtil.IsPlatform(e.cell.type))
        WorldGen.SquareTileFrame(x, y, true);
}
```

**第二遍：平台精确覆盖 frameX/frameY**

```csharp
foreach (BpEntry e in bp.Entries) {
    if (!WorldUtil.IsPlatform(e.cell.type)) continue;
    int x = bp.OriginX + e.bx;
    int y = bp.OriginY + e.by;
    Tile t;
    if (!WorldUtil.TryTile(x, y, out t)) continue;
    if (!t.active() || t.type != e.cell.type) continue;

    int style = e.cell.hasFrame && e.cell.frameY >= 0 ? e.cell.frameY / 18 : 0;
    t.frameY = e.cell.hasFrame && e.cell.frameY >= 0
        ? e.cell.frameY
        : (short)(style * 18);
    if (e.cell.hasFrame && e.cell.frameX >= 0)
        t.frameX = e.cell.frameX;
}
```

- 第二遍之后**不要**再调用 `SquareTileFrame`，否则精确帧又被改掉。
- 对于 `!hasFrame` 的平台，保留第一遍算出的 frameX。
- 对于非平台方块，不需要第二遍；普通块的坡是绘制时裁的，`frameX/frameY` 只影响纹理连接。
- 如果打印过程中玩家手动改了某个格子（type 不匹配），跳过，不强行覆盖。

#### 5）`WorldGen.PlaceObject` 不需要

平台不是 `TileObject`，`PlaceObject` 不适用；`PlaceTile` 的 `type==19` 分支就是官方平台放置路径。不要为了平台去走 `PlaceObject`。

#### 6）验证日志

打印成功后记录：

```text
platform type=19 style=3 frameX=468 frameY=54 slope=1 half=0
```

方便实机对比。

### 6.5 `BlueprintOps.Verify`：建议补形状比对

现在只比 `type/wall`。建议：

```csharp
bool shapeMismatch = t.slope() != cell.slope || t.halfBrick() != cell.halfBrick;
bool frameMismatch = WorldUtil.IsPlatform(cell.type)
    && cell.hasFrame
    && (t.frameX != cell.frameX || t.frameY != cell.frameY);
```

把 `shapeMismatch/frameMismatch` 计入 `wrong` 或单独统计。否则打印错了也显示“完美”。

---

## 7. 验证矩阵（下一阶段实机）

> 当前阶段没动代码，也没进游戏实测；下面是给改造后用的验收清单。

### 7.1 抓取（只读）

在测试世界建一小片，逐项选取并保存，检查 `.bp` 文本：

| 测试对象 | 期望字段 |
|---|---|
| 整块 | `slope=0, halfBrick=0` |
| 半砖 | `slope=0, halfBrick=1` |
| 坡 1/2/3/4 | `slope=1/2/3/4, halfBrick=0` |
| 平台平放（中间/左端/右端/孤立） | `frameX=0/18/36/90...`, `frameY=18*style`, `slope=0` |
| 平台坡 1 | `slope=1`, `frameX` ∈ {180,360,396,432,468} |
| 平台坡 2 | `slope=2`, `frameX` ∈ {144,342,378,414,450} |
| 平台 slope 3/4（block swap） | `slope=3/4`, `halfBrick=0`, `frameX` 平放族（常见 `0`） |
| 平台半砖 | `halfBrick=1`, `slope=0`, `frameX` 平放族 |
| 平台贴实体块 | `frameX` ∈ {54,72,108,126} |
| 平台接楼梯 | `frameX` ∈ {216,234,252,270,288} |

### 7.2 打印（改造后）

1. 清空一片区域，加载蓝图，开打印机。
2. 打完后逐格读 `Tile` 字段，和 `.bp` 对比：
   - type/wall 一致；
   - `slope/halfBrick` 一致；
   - 平台 `frameX/frameY` 一致。
3. 截图对比“投影幽灵”和“真实方块”：
   - 平台楼梯不能被额外斜线覆盖；
   - 平台 slope 3/4 的幽灵应像平放平台，只有一个小角标；真实方块也是平放外观；
   - 普通块坡应是三角形，不是整块；
   - 半砖应下移 8px。
4. 测试相邻关系：
   - 平台左右端帽；
   - 平台坡接平放（468/450）；
   - 平台坡接实体块（360/342 等）；
   - 平台半砖旁边有平放平台。
5. 测试 block swap：
   - 平台锤成坡 → 用方块替换 → 抓取/打印，确认 slope 是否保留；
   - 实体块锤成上坡 3/4 → 用平台替换 → 抓取/打印，确认是否出现 platform slope 3/4 的异常。

### 7.3 回归

- 普通建筑（无坡/无平台）打印不变。
- 旧 4 字段/6 字段蓝图不崩；平台只画外框/无法恢复形状时给提示。
- 电线/促动器/油漆不受影响。
- 多人模式仍禁用打印机（现有逻辑）。

---

## 8. 没查清 / 推测未验证

| 项 | 状态 | 说明 |
|---|---|---|
| 每个 `frameX` 的精确像素外观 | 🟡 推测 | 条件和数值是代码精确的；具体哪一列长什么样，建议实机截图逐值核对（尤其 360/432/180/396 与 342/414/144/378 的连接差异） |
| 平台 frameX 复制到不同邻居环境是否“好看” | ✅ 已决策 | 默认精确复制（忠于蓝图），不按新邻居重算；端帽/连接的视觉差异属于已知取舍 |
| 实体块 → 平台时 slope 3/4 是否保留 | ✅ 已验证 | 用户蓝图 `building_07.bp` / `text.bp`：150 个平台格中 85 个 slope 3/4，frameX 走平放族；代码 `ReplaceTile` 也保留 |
| 平台 slope 3/4 的游戏绘制 | ✅ 已验证 | `TileDrawing.DrawBasicTile` 平台分支只对 1/2 做楼梯补片；3/4 照画 `normalTileRect`（平放 frame），不裁不转；投影照画 + 3px 角标 |
| “锤过的平台并用物块替代”是否还有别的含义 | ❌ 没查清 | 本文按 block swap 解释；如果用户指的是别的操作（例如平台被物块覆盖、或平台被锤后再用物块替换的某种特殊组合），需要用户补充截图/步骤 |
| 平台 frame 的 162/198/306/324 | ✅ 已验证 | 1.4.5.8 的正常框定不会写出这四个值；`TileDrawing` 把它们当额外背面补片。老世界是否保留旧版本 frameX 未逐世界检查，遇到旧值建议原样保存/绘制，不要重算 |
| 平台 70 种款式 | ✅ 资产推算 | 486×1260 的 XNB 尺寸；type 19 的 `frameY/18` 0..69 |
| 1.4.5.8 与旧版/ tModLoader 的差异 | ✅ 只查 1.4.5.8 | 本文没有套用 1.4.4/tModLoader 结论；平台 frameX 表是 1.4.5.8 的 |
| 多人同步 | 🟡 未测 | 打印机在 `Main.netMode == 1` 时禁用；单机恢复不涉及 `NetMessage` |

---

## 9. 一句话建议

**不要加字段，也不要重做蓝图格式。** 下一阶段只做三件事：

1. `Printer` 放行平台、传 `style=frameY/18`、放完写 `slope/halfBrick`（平台 3/4 原样保留）+ `SquareTileFrame` + 精确覆盖 `frameX/frameY`，最后跑一次收尾修复；
2. `Renderer` 平台直接画 frame（半砖裁 8px 下移；slope 3/4 照画平放帧 + 小角标），普通块坡按游戏 8 条竖条裁，删掉/修正反向提示线；
3. `Verify` 补 `slope/halfBrick/frameX/frameY` 比对，避免“形状错了还显示完美”。

按这个方案，抓取层基本不用动；风险集中在打印的收尾顺序和“精确 frame vs 按新邻居重算”的产品取舍。
