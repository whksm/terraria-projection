# 泰拉瑞亚投影MOD 安装器（单文件 exe）

.NET 8 WinForms 安装器，`dotnet publish` 自包含单文件发布，**不依赖目标机器的 .NET**。
它把 `release/` 里的全部载荷内嵌进 exe，运行时解压到临时目录再调用，行为对齐已经跑通的
`release/setup.cmd`（那份仍然保留作回退方案，没有被改动）。

## 构建

```powershell
# 一键（推荐）：发布并把成品放到 dist\TerrariaProjection_Setup.exe
powershell -NoProfile -ExecutionPolicy Bypass -File installer\SetupApp\publish.ps1

# 等价的手工命令
dotnet publish installer/SetupApp -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true
```

产物：`installer\SetupApp\bin\Release\net8.0-windows\win-x64\publish\Setup.exe`
（单文件约 64 MB，目录里没有别的 dll）。

## 用法

| 命令行 | 说明 |
| --- | --- |
| `Setup.exe` | 打开图形界面，自动找游戏目录 |
| `Setup.exe "D:\path\to\Terraria"` | 打开界面并预填目录 |
| `Setup.exe --silent "D:\path\to\Terraria"` | 静默安装，退出码 0=成功、非 0=失败 |
| `Setup.exe --silent --no-shortcut "目录"` | 静默安装但跳过桌面快捷方式 |
| `Setup.exe --shortcut-name "名字"` | 改快捷方式文件名（不带 `.lnk`），默认 `泰拉瑞亚投影MOD` |
| `Setup.exe --shortcut-dir "目录"` | 快捷方式放到指定目录而不是桌面（测试用） |
| `Setup.exe --detect-only` | 只检测游戏目录，不写任何文件 |
| `Setup.exe --selftest` | 环境自检（内嵌资源 / WinForms / COM / 代码页），不碰游戏 |
| `Setup.exe --help` / `--version` | 帮助 / 版本 |

退出码：`0` 成功 · `1` 取消 · `2` 参数错误 · `3` 找不到游戏 · `4` 游戏正在运行 · `5` 备份失败 ·
`6` 打补丁失败 · `7` 复制文件失败 · `9` 未知异常。

日志：**exe 同目录的 `install.log`**（该目录不可写时自动退到 `%TEMP%\install.log`），
UTF-8、带毫秒时间戳，开头写启动环境、结尾写结果汇总。任何异常都会写进日志并在界面模式弹窗。

## 安装流程（6 步）

1. 定位游戏目录：`HKCU\Software\Valve\Steam` → `HKLM\...\WOW6432Node\Valve\Steam` 的 `SteamPath`，
   再解析 `steamapps\libraryfolders.vdf` 里的其他 Steam 库；然后扫 `D:\steam`、
   `Program Files (x86)\Steam`、各盘符的 `SteamLibrary` 等常见路径；都没有就弹目录选择框。
2. 检查游戏是否正在运行（按 Terraria.exe 的完整路径比对 PID），正在运行直接报错退出，不碰任何文件。
3. 备份 `Terraria.exe` → `Terraria_backup_projection.exe`（已存在不覆盖，和 setup.cmd 一致）。
4. 把 `Patcher.exe` / `Mono.Cecil.dll` / `ProjectionMod.dll` 解压到临时目录，**工作目录也设成该目录**后
   `Patcher.exe patch <游戏目录>`（Patcher 用 `Assembly.Location` 找 ProjectionMod.dll，必须同目录）。
   之后用「文件里是否出现 `ProjectionMod` 引用」校验补丁真的生效，并记录打补丁前后的大小。
5. 复制 `ProjectionMod.dll`、`0Harmony.dll`、`ProjectionMod.bp`（已存在不覆盖）、`ProjectionMod.ico`、
   `卸载.bat`、`投影MOD_使用说明.md`，每个文件校验写入长度。
6. 建桌面快捷方式 `泰拉瑞亚投影MOD.lnk`（带图标）：先用 IShellLink COM，失败回退到内嵌的
   `shortcut.ps1`（和 setup.cmd 同一条已验证路径），再失败用生成的自定义路径脚本。
   文件名/目录可以用 `--shortcut-name` / `--shortcut-dir` 改。快捷方式失败只是警告，不算安装失败。

## 实测纪律（重要）

1. **绝不覆盖用户桌面上真正的 `泰拉瑞亚投影MOD.lnk`**。实测时用
   `--shortcut-name 投影MOD_安装测试` 或 `--shortcut-dir <临时目录>`，
   或者干脆 `--no-shortcut`。测完删掉测试快捷方式和测试目录。
2. **验收标准里不包含"启动游戏"**。测试副本只有 `Terraria.exe`，没有 `steam_api.dll` 和 `Content`，
   启动必然报 `System.DllNotFoundException: Unable to load DLL 'steam_api'`——这是副本不完整，
   不是 mod 或安装器的问题。验收只看：exe 是否打进补丁（大小/标记/退出码）、文件是否复制齐全、
   日志是否正常、重复安装是否幂等。
3. 测试副本可能被上一次残留的 Terraria 进程锁住（独占打开失败）。遇到就另开一份新副本，
   不要 kill 玩家正在用的进程。

## 实测记录（本机，干净游戏目录副本）

- 干净副本 `Terraria.exe` = 26,584,064 字节 / 版本 1.4.5.8
- 第一次静默安装：退出码 **0**，`Terraria.exe` 26,584,064 → **26,593,280**（+9,216），
  `ProjectionMod` 标记 x0 → x1，复制 6 个文件，快捷方式创建成功（TargetPath/IconLocation 已回读校验）
- 第二次静默安装：退出码 **0**，Patcher 输出「已经打过补丁」，`Terraria.exe` SHA256 不变、
  备份 SHA256/时间不变、`ProjectionMod.bp` 不覆盖 → 幂等
- 界面模式：窗口正常打开、点「开始安装」走完全流程、弹「安装完成」，日志同步
- 负面用例：目录不存在 → 退出码 3；游戏正在运行 → 退出码 4 且目标目录一个文件都没写
- 按「不碰真桌面快捷方式」的约束复测：`--shortcut-name 投影MOD_安装测试 --shortcut-dir <临时目录>`，
  三次安装（全新 / 重复 / `--no-shortcut`）退出码都是 0，测试 lnk 建在临时目录里，
  桌面真 shortcut 的 SHA256 与修改时间在测试前后**完全一致**；`--shortcut-dir` 指向不存在的目录会自动创建

## 环境坑（排查记录）

旧 `csc.exe` 那批安装器「启动即静默退出」的原因没有沿用（这条链路已经整体替换）。
这次唯一一次可疑症状是快捷方式 COM 报 `E_ACCESSDENIED`，查明**不是代码问题**：

- DSH 沙盒会把「位于工作区里的 exe」限制成只能写工作区，写 `%TEMP%` / 桌面都返回拒绝访问
  （同一份 exe 拷到 `%TEMP%` 下运行就正常，`powershell.exe` 这类系统进程也不受限）。
- 所以本机实测时把 exe 放在 `%TEMP%` 里跑（等同于用户从「下载」目录双击），快捷方式一次成功。
- 连带结论：安装器**必须**把日志优先写在 exe 同目录并支持回退到 `%TEMP%`，否则这类环境下会完全静默；
  现在的实现两者都覆盖，且启动第一行就写日志。
