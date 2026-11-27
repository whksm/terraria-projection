# 泰拉瑞亚 · 投影模组（Terraria Projection Mod）

> 🎬 演示视频：[用 ai 做了一个简易的泰拉瑞亚投影 mod](https://www.bilibili.com/video/BV1kesU66ECR)

在原版泰拉瑞亚（**非 tModLoader**）里实现类似 Minecraft **Litematica / Schematica** 的建造辅助：
把蓝图以幽灵块形式投影到世界里，照着摆就行。

> 铁律：**不无中生有**。所有放置都走游戏自身的放置逻辑，消耗真实背包物品；
> 投影、放置限制、蓝图验证都是纯客户端行为，多人联机也合法。

## 写在前面

这个模组是用 AI 辅助写出来的原型，bug 肯定不少，也还做不到一步到位地处理各种半砖、半砖衍生物块。

把它开源出来，主要是想抛砖引玉：希望有能力的大佬把它改好，或者干脆做一个更完善的高质量版本。

作者不是 mod 开发者，很多东西都是边学边试，代码里能看出明显的 AI 痕迹。有 bug、有想法，欢迎开 Issue，嘴下留情就行。

## 特性

- **幻影投影**：蓝图以半透明贴图 + 状态色描边叠在世界里 —— 白=缺方块、蓝=缺墙、红=放错、黄=多余
- **自由摆放**：Ctrl + 方向键拖动蓝图
- **自动补料**：光标悬停缺料格时自动切出背包里的对应材料
- **放置限制**：建造模式下只能放蓝图相同 / 同类物块（木材族、砖族可互替），有标准/严格两档
- **蓝图验证**：一键统计正确 / 缺失 / 错放 / 多余，并列出问题坐标
- **蓝图存取**：把建筑保存成 `.bp` 文件，支持多蓝图循环切换
- **框选抓取**：手持木剑右键框两个角，把现有建筑抓成蓝图
- **材料清单 HUD**：实时显示还差哪些材料
- **投影吸附**：点投影附近，方块自动落到最近缺失格（手长内）
- **打印机**（可选，单机专用）：自动补方块、补背景墙、放**物件/家具**（火把、门、箱子、压力板、计时器、传送机…连朝向与款式）、补电线和促动器，消耗真实材料

## 环境要求

- Windows 10 / 11
- Steam 版泰拉瑞亚 **1.4.6.1**（已实测可用：游戏日志里出现 `Harmony patches applied OK` 和
  `兼容自检 11/11`；1.4.5.8 也在兼容范围内。更老的版本或以后的新版本需要重新对签名）
- NumPad 热键需要开启 NumLock
- 不需要额外运行库（单文件 exe 自带 .NET 8 运行时；`setup.cmd` 走系统自带 .NET Framework）

## 安装

> ✅ **双击运行即可**：Windows 会弹出 **UAC 权限提示**，点「**是**」允许——安装器要写游戏目录并替换
> `Terraria.exe`，所以必须有管理员权限。
> 如果双击没反应（部分系统 / 杀软会拦截），请右键 → **以管理员身份运行**。
> 用 `安装.bat` / `setup.cmd`（zip 方案）时批处理不会自己弹 UAC：**右键 → 以管理员身份运行**，
> 或在管理员命令提示符里执行。

> **不需要装 .NET**：下面方式一的 exe 自带运行时；方式二用的是系统自带的 .NET Framework。

### 方式一：单文件 exe（无需 .NET）

1. 关掉正在运行的泰拉瑞亚
2. 下载 Releases 里的 **`TerrariaProjection_Setup.exe`**（约 64 MB，把 .NET 运行时一起打包了，所以体积偏大），
   **双击运行**，在 UAC 提示框点「是」（没有弹窗就右键 → 以管理员身份运行）
3. 它会自动：找游戏目录（Steam 注册表 / 常见路径，找不到弹目录选择框）→
   按 exe 版本备份原版（`Terraria_backup_<版本>_vanilla.exe`）→ 给入口打补丁 →
   复制插件/卸载脚本/说明 → 在桌面建带图标的快捷方式
4. 从桌面「启动泰拉瑞亚投影MOD」或 Steam 正常启动，进任意世界，出生点附近会看到演示投影

命令行（脚本/自动化用）：

```bat
:: 静默安装，退出码 0=成功、非 0=失败
TerrariaProjection_Setup.exe --silent "D:\steam\steamapps\common\Terraria"

:: 只检测游戏目录，不写任何文件
TerrariaProjection_Setup.exe --detect-only

:: 快捷方式放到别的目录 / 换名字（测试或不想动桌面时）
TerrariaProjection_Setup.exe --silent --shortcut-dir "D:\tmp\lnk" --shortcut-name "投影MOD测试" "游戏目录"
```

- 日志：**exe 同目录的 `install.log`**（目录不可写时退到 `%TEMP%\install.log`），失败原因一定写在里面
  并弹窗提示；退出码 `0` 成功 / `3` 找不到游戏 / `4` 游戏正在运行 / `6` 打补丁失败 / `8` 没有同版本备份。
- 只想看状态不想写文件：`TerrariaProjection_Setup.exe --check "游戏目录"`（只读）；
  想还原：`--restore "游戏目录"`（只认同版本原版备份，见下面的「备份与还原」）。
- 快捷方式防撞名：默认名 **`启动泰拉瑞亚投影MOD`**（带「启动」前缀）。Windows 上如果桌面上同时有
  文件夹 `X` 和快捷方式 `X.lnk`（扩展名隐藏，看着同名），打开 `X` 会直接启动游戏而不是打开文件夹，
  所以安装器建之前会检查同名文件夹和同名别人的快捷方式，冲突就自动加 `(2)`、`(3)` 后缀，
  **绝不覆盖已经存在的快捷方式**。

### 方式二：zip + setup.cmd（回退方案）

1. 关掉正在运行的泰拉瑞亚
2. 下载 Releases 里的 zip，解压后**右键 `安装.bat`（或 `setup.cmd`）→ 以管理员身份运行**
3. 安装器会自动：找游戏目录 → 按 exe 版本备份原版（`Terraria_backup_<版本>_vanilla.exe`）→
   打补丁 → 复制插件 → **创建带图标的桌面快捷方式**
4. 从桌面「启动泰拉瑞亚投影MOD」或 Steam 正常启动

> 找不到游戏目录时会提示手动输入（或把游戏文件夹拖进来）。
> 安装器支持静默参数：`setup.cmd "游戏目录" --silent`（供脚本/打包使用）。
> 只检查状态：`setup.cmd --check "游戏目录"`；还原：`setup.cmd --restore "游戏目录"`。

卸载：双击游戏目录里的 `卸载.bat`（或 `setup.cmd --restore`）。**它只用与当前 exe 版本相同的
原版备份**；找不到就明确报错并提示你去 Steam 校验完整性，绝不会拿旧版本的 exe 盖上去
（详见下一节）。

## 备份与还原（重要）

备份**按版本命名**：`Terraria_backup_<版本>_vanilla.exe`，版本取自 `Terraria.exe` 的 `FileVersion`
（例如 `Terraria_backup_1.4.6.1_vanilla.exe`）。规则是：

1. **只备份未打补丁的 exe**。已经打过补丁的 exe 不会被当成原版备份（否则「还原」等于没还原）。
2. **版本变了就重新备份**。游戏更新后重跑安装器，会为新版本再生成一份带新版本号的备份，
   旧版本的备份原样留着（只对各自版本有效，不会被混用）。
3. **还原只认同版本备份**。卸载/还原时只找与当前 exe 版本相同、且自身是原版的那一份；
   找不到就报错（退出码 `8`）并提示用 Steam 校验，**不会默默盖一个旧版本上去**——
   1.4.5.8 的 exe 配 1.4.6.1 的 `Content` 会直接崩。

安装器在「当前版本没有干净备份」时（安装前、安装后汇总、卸载时都会提示）不会假装没事，
会把目录里现有备份的版本和状态列出来。

> **已经踩过的坑**：老版本安装器的逻辑是「备份已存在就跳过」，而且备份名不带版本号。
> 游戏从 1.4.5.8 更新到 1.4.6.1 后，它看到旧的 1.4.5.8 备份还在就既不备份也不提示，
> 于是「还原」会把 1.4.5.8 的 exe 盖到 1.4.6.1 的游戏上。现在这条路径已经被堵死。

### 如果你现在没有当前版本的原版备份

Steam 更新过游戏、而 mod 还在旧版本上打的补丁，就属于这种情况。补救步骤：

1. 关掉游戏，在 Steam 库里右键泰拉瑞亚 → **属性 → 已安装文件 → 验证游戏文件完整性**；
2. 验证会把 `Terraria.exe` 恢复成**当前版本的原版**——同时**补丁会被清掉**，mod 暂时失效；
3. 重新运行一次安装器（`Setup.exe` 或 `setup.cmd`），这次会生成
   `Terraria_backup_<当前版本>_vanilla.exe` 这份干净备份；
4. 从桌面快捷方式或 Steam 进游戏即可（`兼容自检 11/11`）。

不想验证完整性也行，但那样卸载时只能得到一句「没有同版本备份」，保证不会把游戏搞坏。

## 热键

| 按键 | 功能 |
|---|---|
| `NumPad0` | 投影显示 开/关 |
| `NumPad1` | 自动补料 开/关 |
| `NumPad2` | 材料清单 HUD 开/关 |
| `NumPad3` | 蓝图验证 |
| `NumPad4` | 建造模式循环：关 → 标准 → 严格 |
| `NumPad5` | 把当前区域保存为蓝图 |
| `NumPad6` | 切换蓝图 |
| `NumPad7` | 打印机 开/关（单机；方块 → 墙 → 物件/家具 → 电线 → 促动器） |
| `NumPad9` | 蓝图管理列表：↑↓ 选择 · PgUp/PgDn 翻页 · `R` 改名 · `Delete` 删除 · `/` 搜索 |
| `L` | 分层渲染循环（9 种：Y↓/Y↑/X→/X← × 单层/范围） |
| `PgUp` / `PgDn` | 层号 ±1（`Shift` 调范围厚度） |
| `Ctrl + 方向键` | 移动蓝图（`Shift` 每次 8 格） |

完整说明见 [release/使用说明.md](release/使用说明.md)，设计文档见 [DESIGN.md](DESIGN.md)。

## 目录结构

```
src/            早期源码
tool/
  src/          当前完整源码（Harmony 补丁 + 投影/打印/蓝图逻辑）
  Patcher.exe   给 Terraria.exe 打补丁的注入器
  build.ps1     构建 + 硬校验 + 同步（-Deploy 才部署）
  verify-dll.ps1 部署前硬校验（程序集名 / 依赖版本 / 图标像素）
  deploy.ps1    只打 exe 补丁 + 拷 DLL（不编译，老脚本）
  restore.ps1   还原原版 exe
  build/        构建时的游戏 exe 副本（不入库）
assets/         图标
notes/          开发笔记与截图
release/        打包好的可安装版本（setup.cmd 方式）
installer/      单文件安装器源码（.NET 8 WinForms；publish.ps1 一键发布）
dist/           发布产物（exe / zip 不进 git，RELEASE_NOTES.md 进 git）
```

## 构建

需要 .NET Framework 编译环境（`C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe`）和一份泰拉瑞亚
`Terraria.exe`（放到 `tool\build\Terraria.exe`，`build.ps1` 会校验它是 1.4.5.8 的元数据；
构建只按程序集名引用，不锁死运行时版本）。

### 构建依赖：XNA Framework 4.0（请自备）

`tool/src/` 的代码引用 **XNA Framework 4.0**，编译时需要这三个程序集放在 `tool/xna/`：

```
tool/xna/Microsoft.Xna.Framework.dll
tool/xna/Microsoft.Xna.Framework.Game.dll
tool/xna/Microsoft.Xna.Framework.Graphics.dll
```

获取方式（任选其一）：

1. **安装 XNA Framework Redistributable 4.0**（泰拉瑞亚运行时本来就依赖它）。装完程序集在 GAC：
   `%WINDIR%\Microsoft.NET\assembly\GAC_32\Microsoft.Xna.Framework*\v4.0_4.0.0.0__842cf8be1de50553\`，
   把这三个 `Microsoft.Xna.Framework*.dll` 复制到 `tool/xna/` 即可；
2. **从泰拉瑞亚游戏目录复制** —— 部分发行版/旧版本会把 `Microsoft.Xna.Framework*.dll` 直接放在游戏目录里，
   有就复制到 `tool/xna/`；没有的话用上面的方式一（1.4.6.x 的 Steam 版是从 GAC 加载的，目录里只有
   `Microsoft.Xna.Framework.Content.Pipeline.dll` / `Microsoft.Xna.Framework.Video.dll`）。

> 这三个 DLL 是 **Microsoft 的分发组件，不随本仓库分发**：`.gitignore` 已忽略 `tool/xna/`，
> 仓库里只有它们的引用（`build.ps1` 按 `tool/xna/*.dll` 通配引用），把文件放回去就能编译。
> 说明见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。

```powershell
pwsh -NoProfile -File tool\build.ps1            # 编译 + 硬校验 + 同步 tool\ 与 release\
pwsh -NoProfile -File tool\build.ps1 -Deploy    # 再备份并部署到游戏目录
```

脚本用 csc 编译 `ProjectionMod.dll`（`/out:...\obj-out\ProjectionMod.dll`），编完立刻用
`tool\verify-dll.ps1` **硬校验**程序集名（Cecil 读 `AssemblyDefinition.Name.Name` 必须为
`ProjectionMod`）、`0Harmony` / `Terraria` 引用版本、图标像素；任一项不过就报错退出，
**不复制、不部署**。`-Deploy` 才会把 `Patcher.exe` 注入 exe 副本、备份旧 DLL 并替换到游戏目录。

> csc 的**程序集名 = `/out:` 的输出文件名**。别先编成别的名字再改名——PE 里的程序集名改不掉，
> 游戏会按 `ProjectionMod` 加载失败直接崩（`System.IO.FileLoadException`）。
> 2026-11-25 就是这么崩的，详见 [notes/printer-objects.md](notes/printer-objects.md) 的「程序集名事故与修复」。

## 已知限制

- 已在 **1.4.6.1 实机验证**（`Harmony patches applied OK`、`兼容自检 11/11`）；1.4.5.8 同样可用。
  游戏以后再更新（1.4.6.2+）需要重新对签名，`tool/src/Compat.cs` 的自检会指出缺了哪一项
- 备份按版本存；更新游戏后**必须重跑一次安装器**才会生成新版本的原版备份
- 打印机在多人模式下默认禁用（防止被当成作弊）
- 蓝图目前不保存箱子内容 / 牌子文字
- 家具 / 火把等多格物块不做无支撑自动打印（避免生成非法 tile）
- 单文件安装器约 64 MB：体积主要是 **.NET 8 自包含运行时**（换了它就不用用户装 .NET），
  载荷本身只有约 3 MB；不想下大文件可以用 zip + `setup.cmd` 的回退方案

## 参与

这是一个原型，欢迎任何人接着改：

- 提 Issue：报 bug 请附上游戏版本、报错日志、复现步骤
- 提 PR：改兼容层（`tool/src/Compat.cs`）最值钱，游戏版本一变这里就要动
- 有新蓝图格式 / 功能想法，开讨论

## 免责

- 与 Re-Logic / 泰拉瑞亚官方无关
- 修改 `Terraria.exe` 属于非官方手段，**联机请只使用投影和放置限制**，不要用打印机
- 请自行判断使用场景，风险自担

## License

MIT

本项目及 `release/` 发布包里随附的第三方组件（**Lib.Harmony**、**Mono.Cecil** 等）遵循各自的许可证，
版权声明与许可证全文见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。
其中**微软 XNA Framework 4.0 不随本仓库分发**，编译前需自行安装或从游戏目录复制（见「构建」章节）。

---

## 蓝图编辑器（可视化）

发布包里带一个单文件网页编辑器 蓝图编辑器.html：

- **双击用浏览器打开即可**，不需要联网、不需要安装任何东西（画面和脚本全在内置）
- 把 .bp 蓝图拖进去就能看：方块按类型着色、**斜坡画对角三角**、半砖画下半块、墙用斜纹、电线四色、促动器、油漆都能单独开关
- 能直接编辑：画笔 / 橡皮 / 吸管、笔刷 1×1 / 3×3 / 5×5、逐格设斜坡 0-4、四色电线、漆
- **材质替换**：石头 → 大理石 这种规则可以存成映射表，也可以「点画布取源 → 一键替换全图」，先预览再应用，Ctrl+Z 能撤销
- 保存出来的 .bp 与游戏完全兼容（12 列格式，列序不变）

> 编辑器的源码、自测脚本在项目仓库 ditor/ 目录。