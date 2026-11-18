# 工具说明（泰拉瑞亚 1.4.5.8 投影 Mod 最小闭环）

## 目录
- `Patcher.exe`        —— 打补丁工具（Mono.Cecil，把 ProjectionMod.Bootstrap.Init() 插进游戏入口）
- `ProjectionMod.dll`  —— 插件本体（目前只含“心跳日志”冒烟补丁，验证链路）
- `0Harmony.dll`       —— Harmony 2.4.3（运行时补丁库，net472）
- `deploy.ps1`         —— 一键部署（自动备份 + 替换 + 拷贝依赖）
- `restore.ps1`        —— 一键还原原版
- `src\`               —— 源码（Patcher.cs / ProjectionMod.cs，csc 直接编译，无需 dotnet SDK）

## 使用
1. 部署：`powershell -ExecutionPolicy Bypass -File deploy.ps1`
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
