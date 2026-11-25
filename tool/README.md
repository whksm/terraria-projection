# 工具说明（泰拉瑞亚 1.4.5.8 投影 Mod 最小闭环）

## 目录
- `Patcher.exe`        —— 打补丁工具（Mono.Cecil，把 ProjectionMod.Bootstrap.Init() 插进游戏入口）
- `ProjectionMod.dll`  —— 插件本体（目前只含“心跳日志”冒烟补丁，验证链路）
- `0Harmony.dll`       —— Harmony 2.4.3（运行时补丁库，net472）
- `build.ps1`          —— **唯一正确的构建入口**（csc 编译 + 硬校验 + 同步 tool/release，`-Deploy` 再部署）
- `verify-dll.ps1`     —— 部署前硬校验（程序集名 / 依赖版本 / 图标像素），`build.ps1` 内部会自动调
- `deploy.ps1`         —— 老的一键部署（只打 exe 补丁 + 拷 DLL，不编译）
- `restore.ps1`        —— 一键还原原版
- `src\`               —— 源码（`Patcher.cs` 是独立工具，其余 15 个文件编成 ProjectionMod.dll）

## 构建（唯一正确做法）

```powershell
pwsh -NoProfile -File tool\build.ps1            # 编译 + 校验 + 同步 tool\ 与 release\
pwsh -NoProfile -File tool\build.ps1 -Deploy    # 再备份并部署到 D:\steam\steamapps\common\Terraria
```

**规则（踩过事故，别再犯）**：csc 的**程序集名 = `/out:` 的输出文件名**，不是项目名、不是源码里的名字。
脚本固定 `/out:...\obj-out\ProjectionMod.dll`，**不允许**先编成 `build_obj.dll` 再改名——
改名改不掉 PE 里已经写死的 `AssemblyDefinition.Name`，游戏按 `ProjectionMod` 加载会直接
`System.IO.FileLoadException`（崩溃码 `0xe0434352`）。

**硬校验**（`tool\verify-dll.ps1`，编译后、复制前跑，任一项不过就 `throw`，不复制、不部署）：

| 校验项 | 判定 | 实现 |
|---|---|---|
| 程序集名 | `AssemblyDefinition.Name.Name` 必须 `== 'ProjectionMod'` | Mono.Cecil `ReadAssembly` |
| 0Harmony 版本 | DLL 引用的 `0Harmony` 版本 == 游戏目录 `0Harmony.dll` 的版本（当前 2.4.3.0） | Cecil 读双方 `AssemblyReferences` |
| Terraria 版本 | 引用必须 `1.4.5.8`（游戏实际 1.4.6.1，属预期差异，一直可用） | Cecil `AssemblyReferences` + `Terraria.exe` FileVersion |
| 图标像素 | DLL 里 `RT_ICON` 每帧 SHA256 必须与 `assets\icon_32.ico` 的帧逐一命中（4 帧：905/2985/5746/3810 字节） | `LoadLibraryEx(AS_DATAFILE\|AS_IMAGE_RESOURCE)` + `EnumResourceNames(RT_ICON)` 取原始 DIB 字节算哈希 |

单独校验一份 DLL（例如已经部署到游戏目录的那份）：

```powershell
pwsh -NoProfile -File tool\verify-dll.ps1 -Path 'D:\steam\steamapps\common\Terraria\ProjectionMod.dll'
```

## 使用
1. 部署：`pwsh -NoProfile -File tool\build.ps1 -Deploy`（会自动备份成 `ProjectionMod.dll.bak-物件修复-<HHmmss>`）
2. 通过 **Steam 正常启动** 泰拉瑞亚（不是 Terraria_MultiRod 快捷方式），进游戏玩一会儿。
3. 查看游戏目录下 `ProjectionMod.log`，出现 `heartbeat OK | tile=8400x2400 ...` 即整条链路成功。

## 安全
- 原版 exe 备份在 `Terraria_backup_projection.exe`，出问题跑 `restore.ps1` 即还原。
- Steam「验证游戏文件完整性」会把 exe 还原成原版（补丁失效，重跑 deploy.ps1 即可）。
- Terraria_MultiRod.exe 是另一个独立文件，本补丁不影响它；反之用 MultiRod 启动也不会加载本插件。

## 已知版本事实（1.4.5.8 侦察记录，见 notes/api-1.4.5.8.txt）
- 运行时：.NET Framework 4.0 Client Profile + XNA 4.0（不是 FNA/.NET 8）
- 入口点：`Terraria.WindowsLaunch::Main(String[])`
- `Main.tile / screenPosition / spriteBatch / GameViewMatrix / tileFrame` 均在
- `Player.PlaceThing(bool, ref Player/ItemCheckContext)`（1.4.5 新签名）
- `Main.LocalPlayer / Main.MouseWorld / Player.selectedItem` 已是属性
