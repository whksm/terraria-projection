# 第三方组件声明（THIRD-PARTY-NOTICES）

本文件列出**泰拉瑞亚投影模组**（下称「本项目」）随源码仓库、以及随 `release/` 发布包一起分发的
第三方组件，以及每个组件的许可证、版权归属和用途。

本项目自身以 MIT 许可证发布，见 [LICENSE](LICENSE)。第三方组件分别适用各自的许可证，
**不因本项目而改变授权**。

## 一览

| 组件 | 文件位置 | 许可证 | 版权 | 用途 |
|---|---|---|---|---|
| [Lib.Harmony](#1-libharmony0harmonydll) 2.4.3 | `tool/0Harmony.dll`、`release/0Harmony.dll` | MIT | Copyright (c) 2017 Andreas Pardeike | 运行时方法钩子（Harmony patch） |
| [Mono.Cecil](#2-monocecilmonocecildll) 0.11.5 | `tool/Mono.Cecil.dll`、`release/Mono.Cecil.dll` | MIT | Copyright (c) 2008 - 2015 Jb Evain；Copyright (c) 2008 - 2011 Novell, Inc. | Patcher 读取并改写 `Terraria.exe` |
| [Microsoft XNA Framework](#3-microsoft-xna-framework-40不随本仓库分发) 4.0 | **不随本仓库分发**（编译期引用 `tool/xna/*.dll`） | Microsoft 专有许可 | Microsoft Corporation | 仅编译期引用游戏使用的图形/框架程序集 |

> 两个 MIT 组件（Lib.Harmony、Mono.Cecil）的许可证均**允许随本仓库和发布包原样再分发**，
> 只要保留下面的版权声明和许可证全文；本项目已原样保留，未做任何修改。

---

## 1. Lib.Harmony（`0Harmony.dll`）

- **组件**：Lib.Harmony（运行时补丁库，运行时程序集名 `0Harmony`）
- **版本**：2.4.3（程序集版本 `2.4.3.0`）
- **上游**：https://github.com/pardeike/Harmony
- **版权**：Copyright (c) 2017 Andreas Pardeike
- **许可证**：MIT
- **本项目用途**：在游戏运行时给 `Terraria.exe` 里的方法打补丁（投影渲染、放置限制、打印机等），
  本项目不修改 Harmony 的二进制文件。
- **分发位置**：`tool/0Harmony.dll`（编译引用）、`release/0Harmony.dll`（发布包运行时依赖）

### 许可证全文

```
MIT License

Copyright (c) 2017 Andreas Pardeike

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## 2. Mono.Cecil（`Mono.Cecil.dll`）

- **组件**：Mono.Cecil（IL 读写库）
- **版本**：0.11.5（程序集版本 `0.11.5.0`）
- **上游**：https://github.com/jbevain/cecil
- **版权**：Copyright (c) 2008 - 2015 Jb Evain；Copyright (c) 2008 - 2011 Novell, Inc.
- **许可证**：MIT（MIT/X11）
- **本项目用途**：`Patcher.exe` 用它在安装/卸载时读取并改写 `Terraria.exe` 的入口点，
  把 `ProjectionMod.Bootstrap.Init()` 插进游戏启动流程；本项目不修改 Cecil 的二进制文件。
- **分发位置**：`tool/Mono.Cecil.dll`（构建/打补丁用）、`release/Mono.Cecil.dll`（发布包运行时依赖）

### 许可证全文

```
Copyright (c) 2008 - 2015 Jb Evain
Copyright (c) 2008 - 2011 Novell, Inc.

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
```

---

## 3. Microsoft XNA Framework 4.0（**不随本仓库分发**）

- **组件**：Microsoft XNA Framework 4.0
  （`Microsoft.Xna.Framework.dll`、`Microsoft.Xna.Framework.Game.dll`、`Microsoft.Xna.Framework.Graphics.dll`）
- **版权**：Copyright (c) Microsoft Corporation
- **许可证**：Microsoft 专有许可（随 **XNA Framework Redistributable 4.0** 分发），**不是**开源许可证
- **本项目用途**：**仅编译期引用**。`tool/src/` 的代码用到 XNA 的类型（矩阵、纹理等），
  编译时需要这三个程序集做引用；运行时的这三个程序集由用户机器上已安装的 XNA 4.0 / 游戏本身提供。

### 为什么不在仓库里

这三个 DLL 是微软的分发组件，**本仓库不包含、也不再跟踪它们**：

- `.gitignore` 已忽略 `tool/xna/`；
- `tool/build.ps1` 仍按 `tool/xna/*.dll` 通配引用，所以本地只要把文件放进去就能编译，路径不需要改；
- 历史提交里可能仍留有它们的痕迹，属历史记录，当前分支不再包含。

### 怎么自备

任选其一：

1. 安装微软的 **XNA Framework Redistributable 4.0**（泰拉瑞亚运行时本来就依赖它）。装完程序集位于 GAC：
   `%WINDIR%\Microsoft.NET\assembly\GAC_32\Microsoft.Xna.Framework*\v4.0_4.0.0.0__842cf8be1de50553\`，
   把三个 `Microsoft.Xna.Framework*.dll` 复制到 `tool/xna/`；
2. 从**泰拉瑞亚游戏目录**复制（部分发行版/旧版本把 `Microsoft.Xna.Framework*.dll` 直接放在游戏目录里）；
3. 从 XNA Game Studio 的 References 目录复制同版本文件。

需要的是程序集版本 **4.0**（本项目编译时用的文件版本为 `4.0.20823.0`）。使用这四个 DLL 需遵守
Microsoft 的许可条款（该许可**不允许**把 XNA Framework 组件再分发给第三方）。

---

## 4. `data/tile-names.json`（方块名数据）

- **来源**：由**用户本机**的正版 `Terraria.exe` 中**内嵌的本地化数据**提取的方块/墙名称表
  （id → 名称），不是从游戏资源文件里复制出来的内容。
- **用途**：仅用于投影模组的**编辑器（蓝图编辑器.html）显示**与蓝图里方块 id 的可读名称，
  目的是**提高互操作性**（让玩家看懂蓝图里是什么方块），不含任何贴图、音效、音乐、地图或存档数据。
- **范围**：仓库**不包含**任何泰拉瑞亚游戏资源文件（贴图 / 音效 / 音乐 / 存档 / 地图 / Content 目录），
  仅包含这份名称索引（`data/tile-names.json`，以及由它生成的 `data/tile-names.md`）。
- **权利**：泰拉瑞亚的程序、素材及其本地化文本版权归 **Re-Logic** 所有。若权利方认为这份名称索引
  不宜分发，请开 Issue，我们会移除。

---

## 5. 关于泰拉瑞亚（Re-Logic）

- 本项目是**非官方的第三方作品**，与 **Re-Logic** 及泰拉瑞亚官方**没有任何关联、赞助或背书**。
- 本项目不含、也不分发泰拉瑞亚的任何游戏资源；**使用时需要自备正版泰拉瑞亚**
  （Steam 版，已实测 1.4.5.8 / 1.4.6.1）。
- 安装模组会修改你自己机器上的 `Terraria.exe`（安装器会自动按版本备份原版，可还原）。
  请自行判断使用场景，风险自担；联机时请遵守服务器规则。

---

## 变更记录

| 日期 | 内容 |
|---|---|
| 2026-11 | 初次建立：Lib.Harmony / Mono.Cecil（MIT，随包分发）、Microsoft XNA Framework 4.0（不随仓库分发）、`data/tile-names.json` 说明 |
