# 泰拉瑞亚投影MOD 安装器（单文件 exe）

.NET 8 WinForms 安装器，`dotnet publish` 自包含单文件发布，**不依赖目标机器的 .NET**。
它把 `release/` 里的全部载荷内嵌进 exe，运行时解压到临时目录再调用，行为与
`release/setup.cmd`（zip 回退方案，两份脚本逻辑同步维护）保持一致。

> ✅ **双击运行即可**：Windows 会弹出 **UAC 权限提示**，点「**是**」允许——安装器要写游戏目录并替换
> `Terraria.exe`，所以必须有管理员权限。
> 如果双击没反应（部分系统 / 杀软会拦截），请右键 → **以管理员身份运行**。
> 用 `安装.bat` / `setup.cmd`（zip 方案）时批处理不会自己弹 UAC：**右键 → 以管理员身份运行**，
> 或在管理员命令提示符里执行。

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

> **`app.manifest` 必须是 `requireAdministrator`**（`SetupApp.csproj` 的 `ApplicationManifest` 已指向它）：
> 安装器要写游戏目录、替换 `Terraria.exe`。写成 `asInvoker` 时，普通权限下双击会**静默失败/没反应**。
> 改完 manifest 要重跑 `publish.ps1`，并用字节搜索确认成品里是 `requireAdministrator`：
> ```powershell
> $t = [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes('dist\TerrariaProjection_Setup.exe'))
> $t.Contains('level="requireAdministrator"')   # 必须为 True
> ```

## 用法

| 命令行 | 说明 |
| --- | --- |
| `Setup.exe` | 打开图形界面，自动找游戏目录 |
| `Setup.exe "D:\path\to\Terraria"` | 打开界面并预填目录 |
| `Setup.exe --silent "D:\path\to\Terraria"` | 静默安装，退出码 0=成功、非 0=失败 |
| `Setup.exe --silent --no-shortcut "目录"` | 静默安装但跳过桌面快捷方式 |
| `Setup.exe --shortcut-name "名字"` | 改快捷方式文件名（不带 `.lnk`），默认 `启动泰拉瑞亚投影MOD` |
| `Setup.exe --shortcut-dir "目录"` | 快捷方式放到指定目录而不是桌面（测试用） |
| `Setup.exe --detect-only` | 只检测游戏目录，不写任何文件 |
| `Setup.exe --check "目录"` | 只打印版本 / 补丁状态 / 备份清单（只读） |
| `Setup.exe --restore "目录"` | 用与当前版本相同的原版备份还原 `Terraria.exe` |
| `Setup.exe --selftest` | 环境自检（内嵌资源 / WinForms / COM / 代码页），不碰游戏 |
| `Setup.exe --help` / `--version` | 帮助 / 版本 |

退出码：`0` 成功 · `1` 取消 · `2` 参数错误 · `3` 找不到游戏 · `4` 游戏正在运行 · `5` 备份失败 ·
`6` 打补丁失败 · `7` 复制文件失败 · `8` 没有同版本原版备份（还原拒绝） · `9` 未知异常。

日志：**exe 同目录的 `install.log`**（该目录不可写时自动退到 `%TEMP%\install.log`），
UTF-8、带毫秒时间戳，开头写启动环境、结尾写结果汇总。任何异常都会写进日志并在界面模式弹窗。

## 安装流程（6 步）

1. 定位游戏目录：`HKCU\Software\Valve\Steam` → `HKLM\...\WOW6432Node\Valve\Steam` 的 `SteamPath`，
   再解析 `steamapps\libraryfolders.vdf` 里的其他 Steam 库；然后扫 `D:\steam`、
   `Program Files (x86)\Steam`、各盘符的 `SteamLibrary` 等常见路径；都没有就弹目录选择框。
2. 检查游戏是否正在运行（按 Terraria.exe 的完整路径比对 PID），正在运行直接报错退出，不碰任何文件。
3. 读 `Terraria.exe` 的版本（`FileVersion`）和补丁状态，按版本备份
   （`BackupStore.cs`，规则见下一节）。**只备份未打补丁的 exe；版本变了重新备份**；
4. 把 `Patcher.exe` / `Mono.Cecil.dll` / `ProjectionMod.dll` 解压到临时目录，**工作目录也设成该目录**后
   执行 `Patcher.exe patch <游戏目录> <临时目录>` —— 让 Patcher **只把成品写到临时目录**
   （不碰游戏目录，也不让它自己建没有版本号的 `Terraria_backup_projection.exe`）。
   之后校验成品里真出现 `ProjectionMod` 标记，再经「游戏目录里的 `.new` → 原子改名」替换 `Terraria.exe`。
   已经打过补丁的 exe 直接跳过注入（幂等，不会把 `Bootstrap.Init` 注入两次）。
5. 复制 `ProjectionMod.dll`、`0Harmony.dll`、`ProjectionMod.bp`（已存在不覆盖）、`ProjectionMod.ico`、
   `卸载.bat`、`投影MOD_使用说明.md`、`蓝图编辑器.html`，每个文件校验写入长度。
6. 建桌面快捷方式（带图标，默认名 `启动泰拉瑞亚投影MOD`）：先用 IShellLink COM，失败回退到内嵌的
   `shortcut.ps1`（和 setup.cmd 同一条已验证路径），再失败用生成的自定义路径脚本。
   文件名/目录可以用 `--shortcut-name` / `--shortcut-dir` 改。快捷方式失败只是警告，不算安装失败。

## 快捷方式命名规则（避免和文件夹撞名）

Windows 上如果同一个目录里既有文件夹 `X` 又有快捷方式 `X.lnk`（资源管理器默认隐藏扩展名，看起来同名），
用 `Start-Process "…\X"` 或资源管理器打开那个路径时，会**解析到 `X.lnk` 并直接启动它的目标程序**，
而不是打开文件夹。实测确认：`目标\` + `目标.lnk`(notepad) → 打开 `目标` 启动记事本；删掉 .lnk 才正常。

所以安装器的规则是：

- 默认名 `启动泰拉瑞亚投影MOD`（前缀「启动」就是为了和常见的 `泰拉瑞亚投影MOD` 文件夹区分开）。
- 建之前先检查目标目录（默认桌面）：**同名文件夹**（大小写不敏感）、同名无扩展名文件、
  `名字.lnk` 是同名文件夹 —— 任一命中就换名，依次试 `名字 (2)`、`名字 (3)`…，最多 50 次。
- 已有同名 `.lnk`：
  - 指向本次安装的 `Terraria.exe` → 直接复用更新（重复安装幂等，不会一直加后缀）；
  - 指向别的地方 → **不覆盖**，换名下一位，并在日志里写明原来指向哪里。
- 卸载：`卸载.bat` 只还原 `Terraria.exe` 并列出可选清理项，**不删除任何快捷方式**，
  因此不可能误删用户的其他快捷方式；需要清理时手动删安装日志里记录的那个 `.lnk`。
- 还原只认同版本原版备份，找不到就报错退出（详见下一节）。

## 备份策略（v1.3.0 起，与 `setup.cmd` / `卸载.bat` 完全一致）

`BackupStore.cs` 是这一块的唯一实现，`setup.cmd` / `卸载.bat` 里的对应子过程逻辑相同，
**改规则要三处一起改**：

- 备份名：`Terraria_backup_<版本>_vanilla.exe`，版本取 `FileVersion`（回退 `ProductVersion`），
  规范化规则 = 取开头连续的数字与点、超过 3 段时去掉结尾 `.0`（`1.4.6.1.0` → `1.4.6.1`）；
- **只备份未打补丁的 exe**：判据是文件里有没有 ASCII `ProjectionMod`（补丁注入了 AssemblyRef /
  类型名）。已打补丁的 exe 一旦被当原版备份，卸装就等于没卸；
- **版本变了必须重新备份**：不再「存在就跳过」；旧版本备份不删，但要指明「只对各自版本有效」；
- 同名备份本身是打过补丁的 → 用当前原版 exe 覆盖它；
- **还原只认与当前 exe 版本相同的 vanilla 备份**，找不到/只有坏的 → `ExitCodes.NoMatchingBackup`(8)，
  报错并给 Steam 校验完整性的步骤，**绝不默默盖旧版本**（1.4.5.8 的 exe + 1.4.6.1 的 Content 必崩）；
- 没有干净备份时，安装流程不会假装没事：结果里带 `NoCleanBackup`，界面弹警告、静默模式打印补救步骤、
  日志写「本版本没有干净的原版备份」。

## 实测纪律（重要）

1. **绝不覆盖用户桌面上真正的快捷方式**（用户现在用的是 `启动泰拉瑞亚投影MOD.lnk`）。
   实测时用 `--shortcut-name 投影MOD_安装测试` 或 `--shortcut-dir <临时目录>`，
   或者干脆 `--no-shortcut`。测完删掉测试快捷方式和测试目录。
2. **验收标准里不包含"启动游戏"**。测试副本只有 `Terraria.exe`，没有 `steam_api.dll` 和 `Content`，
   启动必然报 `System.DllNotFoundException: Unable to load DLL 'steam_api'`——这是副本不完整，
   不是 mod 或安装器的问题。验收只看：exe 是否打进补丁（大小/标记/退出码）、文件是否复制齐全、
   日志是否正常、重复安装是否幂等。
3. 测试副本可能被上一次残留的 Terraria 进程锁住（独占打开失败）。遇到就另开一份新副本，
   不要 kill 玩家正在用的进程。

## 实测记录（本机，干净游戏目录副本）

### v1.3.0 备份策略（1.4.6.1 时代的收尾修复）

用的是临时目录里的假 exe（`csc` 编译出 `FileVersion` 分别为 1.4.6.1 / 1.4.6.2 / 1.4.5.8 的小程序，
「已打补丁」= 在文件尾部追加 ASCII `ProjectionMod`），以及一份真 1.4.5.8 `Terraria.exe` 的副本：

| 场景 | 期望 | 实测 |
| --- | --- | --- |
| 干净 1.4.6.1，首次安装/备份 | 生成 `Terraria_backup_1.4.6.1_vanilla.exe` | ✅ 生成，大小与原文件一致 |
| 再跑一次（同版本、未变） | 幂等，不再备份 | ✅ 「本版本备份已存在且是原版，跳过」 |
| exe 换成 1.4.6.2（干净） | 按新版本**重新**备份，旧备份保留 | ✅ 生成 `…_1.4.6.2_vanilla.exe`，1.4.6.1 那份还在 |
| 已打补丁 + 本版本有干净备份 | 不备份、不重复注入 | ✅ 跳过注入，exe 的 SHA256 不变 |
| 已打补丁 + 本版本**没有**干净备份 | 不备份、明确警告 + 补救步骤 + 列出旧备份 | ✅ 界面对应 `NoCleanBackup`（静默模式打印 4 步补救） |
| 还原：只有旧版本备份 | **拒绝**，退出码 8 + Steam 校验提示 | ✅ `exit 8`，报错文案含「只接受 Terraria_backup_1.4.6.2_vanilla.exe」 |
| 还原：同名备份本身被打过补丁 | 同样拒绝 | ✅ `exit 8` |
| 还原：同版本、干净 | 覆盖成功并回读校验 | ✅ 版本一致、无 `ProjectionMod` 标记 |
| 还原：当前已是原版 | 不动文件，只列清理项 | ✅ 「已经是原版，不需要还原」 |
| `--check` | 只读，不写任何文件 | ✅ 打印版本/状态/备份清单/结论 |

真 1.4.5.8 副本上跑完整安装：`Terraria_backup_1.4.5.8_vanilla.exe` 生成、
补丁标记 x0 → x1、6 个文件复制、`--silent` 退出码 0；第二次安装补丁跳过、exe 哈希不变；
**没有再产生 `Terraria_backup_projection.exe`**。
`setup.cmd` 的同款场景在 `installer/setup.cmd` 上跑了一遍，结果一致（含 `--check` / `--backup-only` /
`--restore` 三个模式）。

### 早先的记录（v1.2.0，1.4.5.8 时代）

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
- 快捷方式命名矩阵实测（全部 `--shortcut-dir` 到临时目录，退出码都是 0）：
  空目录 → `启动泰拉瑞亚投影MOD.lnk`；预置同名文件夹 → `启动泰拉瑞亚投影MOD (2).lnk`；
  预置**仅大小写不同**的同名文件夹（`…投影mod`）→ 同样 `(2)`；预置同名但指向 notepad 的 `.lnk` →
  `(2)` 且那个 notepad 快捷方式一个字节都没改；重复安装 → 复用原名字，不会越加越多后缀

## 环境坑（排查记录）

旧 `csc.exe` 那批安装器「启动即静默退出」的原因没有沿用（这条链路已经整体替换）。
这次唯一一次可疑症状是快捷方式 COM 报 `E_ACCESSDENIED`，查明**不是代码问题**：

- DSH 沙盒会把「位于工作区里的 exe」限制成只能写工作区，写 `%TEMP%` / 桌面都返回拒绝访问
  （同一份 exe 拷到 `%TEMP%` 下运行就正常，`powershell.exe` 这类系统进程也不受限）。
- 所以本机实测时把 exe 放在 `%TEMP%` 里跑（等同于用户从「下载」目录双击），快捷方式一次成功。
- 连带结论：安装器**必须**把日志优先写在 exe 同目录并支持回退到 `%TEMP%`，否则这类环境下会完全静默；
  现在的实现两者都覆盖，且启动第一行就写日志。
