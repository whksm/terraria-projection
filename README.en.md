# Terraria Projection Mod

[中文](README.md) | [English](README.en.md)

> 🎬 Demo: [I built a simple Terraria projection mod with AI](https://www.bilibili.com/video/BV1kesU66ECR)

## What is this

A build assistant for vanilla Terraria (**not tModLoader**), built around the idea behind the "projection /
blueprint" mods of the Minecraft ecosystem (such as Litematica and Schematica): the design is laid over the world
as ghost blocks, and you build along with it. This project is an independent implementation and has no affiliation
with, or licence from, those projects — see "Credits and inspiration" at the end.

> Hard rule: **nothing appears out of thin air.** Every placement goes through the game's own placement logic and
> consumes real items from your inventory. Projection, placement restriction and blueprint verification are
> client-side only, so they are legitimate in multiplayer as well.

## Before you start

This is an AI-assisted prototype. It certainly has bugs, and it still can't handle every half-block and
half-block derivative in a single pass.

It's open-sourced to hand the idea to better hands: improve it, or write a cleaner, higher-quality version from
scratch.

The author is not a mod developer — most of this was learned by trial and error, and the AI fingerprints in the
code are hard to miss. Bug reports and ideas are welcome as Issues; please be kind.

## Features

- **Ghost projection** — the blueprint is drawn over the world as semi-transparent sprites with state-coloured
  outlines: white = missing block, blue = missing wall, red = wrong block, yellow = extra block
- **Free placement** — `Ctrl` + arrow keys moves the blueprint
- **Auto-pick** — hovering a missing slot swaps in the matching item from your inventory
- **Placement restriction** — in build mode you may only place blocks that match the blueprint or belong to the
  same family (woods substitute for woods, bricks for bricks); standard and strict levels
- **Blueprint verification** — one key tallies correct / missing / wrong / extra and lists the offending coordinates
- **Save and load** — save a build to a `.bp` file and cycle through several blueprints
- **Box capture** — right-click two corners with a wooden sword to capture an existing build as a blueprint
- **Material HUD** — a live list of the materials you're still short of
- **Snap to projection** — click near a projection and the block drops into the nearest missing slot (within reach)
- **Printer** (optional, single-player only) — fills in blocks, background walls and **objects/furniture** (torches,
  doors, chests, pressure plates, timers, teleporters… facing and style included), plus wires and actuators;
  consumes real materials

## Requirements

- Windows 10 / 11
- Steam version of Terraria **1.4.6.1** (verified in-game: the log shows `Harmony patches applied OK` and
  `compat self-check 11/11`; 1.4.5.8 works too. Older builds, and any future update, need the signatures re-mapped)
- NumLock on, for the NumPad hotkeys
- No extra runtime: the single-file exe bundles the .NET 8 runtime, and `setup.cmd` uses the .NET Framework that
  ships with Windows

## Installation

> ⚠️ **The detailed install guides are Chinese-only.** `安装前必读.txt` (a one-page read-me-first) and
> `使用说明.md` (the full manual, installed into the game folder as `投影MOD_使用说明.md`) ship inside the release
> and are written in Chinese; there is no English translation yet. The sections below cover the whole procedure in
> English, so you don't need them to install.

### Three steps

1. **Quit Terraria** (make sure Steam no longer shows it as running either);
2. **Double-click `TerrariaProjection_Setup_v1.3.0.exe`** → when Windows asks whether to allow the app to make
   changes to your device, click **Yes** → check that the detected game folder is right → click **Start install** →
   when **Install complete!** appears, click OK, then Close;
3. Launch **"启动泰拉瑞亚投影MOD"** from the desktop shortcut and press **NumPad0** to toggle the projection
   (NumLock must be on).

If the installer won't run, nothing happens on double-click, antivirus flags it, or the game looks unchanged after
installing, see the FAQ in [release/使用说明.md](release/使用说明.md) (Chinese).

### Option A: single-file exe (recommended, no .NET install needed)

1. Close Terraria if it is running;
2. Download **`TerrariaProjection_Setup_v1.3.0.exe`** from Releases (about 64 MB — it carries the .NET runtime with
   it, hence the size), **double-click it**, and confirm the UAC prompt with **Yes** (if no prompt appears,
   right-click → **Run as administrator**);
3. It will: locate the game folder (Steam registry, `libraryfolders.vdf`, common drive letters; if that fails it
   shows a folder picker — click **Browse…** to pick it manually) → back up the vanilla exe under the current version
   (`Terraria_backup_<version>_vanilla.exe`) → patch the entry point → copy the plugin, the uninstaller,
   `安装前必读.txt`, `投影MOD_使用说明.md` and the blueprint editor → create a desktop shortcut with an icon;
4. Start the game from the desktop shortcut or from Steam, enter any world, and a demo projection appears near the
   spawn point.

Command line (for scripts and automation):

```bat
:: silent install; exit code 0 = success, anything else = failure
TerrariaProjection_Setup.exe --silent "D:\steam\steamapps\common\Terraria"

:: detect the game folder only, write nothing
TerrariaProjection_Setup.exe --detect-only

:: shortcut somewhere else / under another name (for testing, or to leave the desktop alone)
TerrariaProjection_Setup.exe --silent --shortcut-dir "D:\tmp\lnk" --shortcut-name "Projection Test" "game dir"
```

- Log: **`install.log` next to the exe** (falls back to `%TEMP%\install.log` if that folder isn't writable). Every
  failure is written there and also shown in a dialog. Exit codes: `0` success / `3` game not found / `4` game is
  running / `6` patch failed / `8` no same-version backup.
- Status only, no writes: `TerrariaProjection_Setup.exe --check "game dir"` (read-only). Restore:
  `TerrariaProjection_Setup.exe --restore "game dir"` (see "Backup and restore" below).
- Shortcut names never collide: the default is **`启动泰拉瑞亚投影MOD`**. On Windows a desktop folder `X` and a
  shortcut `X.lnk` (extension hidden) look identical, and opening `X` would launch the game instead of the folder.
  The installer therefore checks for an existing folder and for someone else's shortcut of the same name, and adds a
  `(2)`, `(3)` suffix if needed — **it never overwrites an existing shortcut**.

### Option B: zip + 安装.bat (fallback, if the exe won't launch)

1. Close Terraria if it is running;
2. Download **`TerrariaProjection_v1.3.0.zip`** and **extract it fully** to any folder (⚠️ don't double-click files
   from inside the zip preview window — that won't install anything);
3. In the extracted folder, **right-click `安装.bat` → Run as administrator** (`安装.bat` is a batch file and **does
   not raise UAC by itself** — you have to elevate it; a plain double-click may just flash and close);
4. It will: locate the game folder → back up the vanilla exe under the current version → patch it → copy the plugin
   and docs → **create the desktop shortcut**; it finishes by printing "安装完成！";
5. Start the game from the desktop shortcut or from Steam.

> If the game folder can't be found you'll be asked to type it in (or drag the folder into the console window and
> press Enter). Silent mode: `setup.cmd "game dir" --silent`. Status only: `setup.cmd --check "game dir"`.
> Restore: `setup.cmd --restore "game dir"`.

### What a successful install looks like

- **Desktop**: a **`启动泰拉瑞亚投影MOD`** shortcut (renamed to `… (2)` if the name is taken — nothing is overwritten);
- **Game folder**: `ProjectionMod.dll`, `0Harmony.dll`, `卸载.bat`, `投影MOD_使用说明.md`, `安装前必读.txt`,
  `蓝图编辑器.html`, plus the vanilla backup `Terraria_backup_<version>_vanilla.exe` (full list in section 4 of the manual);
- **Verify**: press `NumPad0` in game and see the projection toggle. Or check the first line of `ProjectionMod.log` in
  the game folder for `=== ProjectionMod init ===`, with `compat self-check: 11/11 passed` nearby;
- **Logs**: plugin log `ProjectionMod.log` in the game folder; installer log `install.log` next to the exe (or
  `%TEMP%\install.log`). Those two are enough for almost any problem.

Uninstall: double-click `卸载.bat` in the game folder (or `setup.cmd --restore` /
`TerrariaProjection_Setup_v1.3.0.exe --restore "game dir"`). **It only restores a vanilla backup matching the current
exe version**; if there isn't one it fails loudly and tells you to verify the game in Steam, and it will never put an
older exe in place (see the next section).

## Backup and restore (important)

Backups are **named by version**: `Terraria_backup_<version>_vanilla.exe`, with the version taken from
`Terraria.exe`'s `FileVersion` (for example `Terraria_backup_1.4.6.1_vanilla.exe`). The rules:

1. **Only unpatched exes get backed up.** An already-patched exe is never treated as a vanilla backup — otherwise
   "restore" would restore nothing.
2. **A new game version means a new backup.** After a game update, run the installer again; it writes a fresh backup
   with the new version number and leaves the old one alone (each backup is only good for its own version).
3. **Restore only accepts a same-version backup.** Uninstall/restore looks for a backup whose version matches the
   current exe and which is genuinely vanilla; if there isn't one it errors out (exit code `8`) and points you at
   Steam's file verification. It **will not quietly drop an older exe in place** — a 1.4.5.8 exe against 1.4.6.1
   `Content` crashes outright.

When the current version has no clean backup (checked before installing, in the post-install summary and on
uninstall), the installer says so instead of pretending everything is fine, and lists the backups it did find with
their versions and state.

> **A real incident**: the old installer skipped the backup whenever one already existed, and didn't put the version
> in the file name. After the game went from 1.4.5.8 to 1.4.6.1, it saw the stale 1.4.5.8 backup, backed up nothing
> and said nothing — so "restore" would have put the 1.4.5.8 exe onto a 1.4.6.1 install. That path is closed now.

### If you have no vanilla backup for the current version

This is what happens when Steam updated the game while the mod was still patched on the old build. To recover:

1. Close the game, then in Steam right-click Terraria → **Properties → Installed Files → Verify integrity of game files**;
2. Verification restores `Terraria.exe` to the **current vanilla build** — and **wipes the patch**, so the mod stops
   working for now;
3. Run the installer again (`TerrariaProjection_Setup_v1.3.0.exe`, or `安装.bat` from the zip); this time it writes
   `Terraria_backup_<current version>_vanilla.exe`;
4. Launch from the desktop shortcut or from Steam — `compat self-check 11/11`.

You can skip the verification, but then uninstall will only tell you there's no same-version backup — it will never
damage the game.

## Hotkeys

| Key | Action |
|---|---|
| `NumPad0` | toggle projection |
| `NumPad1` | toggle auto-pick |
| `NumPad2` | toggle material HUD |
| `NumPad3` | verify blueprint |
| `NumPad4` | cycle build mode: off → standard → strict |
| `NumPad5` | save the current area as a blueprint |
| `NumPad6` | switch blueprint |
| `NumPad7` | toggle printer (single-player; blocks → walls → objects/furniture → wires → actuators) |
| `NumPad9` | blueprint list: ↑↓ select · PgUp/PgDn page · `R` rename · `Delete` delete · `/` search |
| `L` | cycle layer filter (9 modes: Y↓/Y↑/X→/X← × single layer/range) |
| `PgUp` / `PgDn` | layer ±1 (`Shift` sets the range thickness) |
| `Ctrl + arrow keys` | move blueprint (`Shift` = 8 blocks per step) |

Full details in [release/使用说明.md](release/使用说明.md) (Chinese); design notes in [DESIGN.md](DESIGN.md) (Chinese).

## Blueprint editor

The release ships a single-file browser editor, `蓝图编辑器.html`:

- **Double-click to open it in a browser** — no network, nothing to install (markup and script are all inline)
- Drop a `.bp` blueprint on it: blocks are coloured by type, **slopes are drawn as diagonal triangles**, half-blocks
  as their lower half, walls hatched, wires in four colours; actuators and paint toggle independently
- Edit directly: brush / eraser / eyedropper, 1×1 / 3×3 / 5×5 brushes, per-tile slope 0-4, four wire colours, paint
- **Material replacement**: rules like stone → marble can be saved as a mapping table, or pick a source on the canvas
  and "replace everywhere" — preview first, then apply, `Ctrl+Z` to undo
- The saved `.bp` is fully compatible with the game (12-column format, same column order)

> The editor's source and self-test script live in the `editor/` folder of the repository.

## Directory layout

```
src/            early source
tool/
  src/          current full source (Harmony patches + projection/printer/blueprint logic)
  Patcher.exe   injector that patches Terraria.exe
  build.ps1     build + hard verification + sync (deploys only with -Deploy)
  verify-dll.ps1 pre-deploy hard checks (assembly name / dependency versions / icon pixels)
  deploy.ps1    patch the exe + copy the DLL only (no build, legacy)
  restore.ps1   restore the vanilla exe
  build/        game exe copy used for building (not in git)
assets/         icons
notes/          development notes and screenshots
release/        packaged installable version (setup.cmd route)
installer/      single-file installer source (.NET 8 WinForms; publish.ps1 publishes in one go)
dist/           release artifacts (exe / zip not in git, RELEASE_NOTES.md is)
```

## Edit and build from source

You need a .NET Framework compiler environment (`C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe`) and a
Terraria `Terraria.exe` at `tool\build\Terraria.exe`. **You have to supply your own 1.4.5.8** — `build.ps1` checks
the metadata is 1.4.5.8. The build references assemblies by name only, so the runtime version isn't pinned.

### Build dependency: XNA Framework 4.0 (bring your own)

`tool/src/` references **XNA Framework 4.0**, so the build needs these three assemblies in `tool/xna/`:

```
tool/xna/Microsoft.Xna.Framework.dll
tool/xna/Microsoft.Xna.Framework.Game.dll
tool/xna/Microsoft.Xna.Framework.Graphics.dll
```

Either route works:

1. **Install XNA Framework Redistributable 4.0** (Terraria depends on it at runtime anyway). The assemblies land in
   the GAC under `%WINDIR%\Microsoft.NET\assembly\GAC_32\Microsoft.Xna.Framework*\v4.0_4.0.0.0__842cf8be1de50553\`;
   copy the three `Microsoft.Xna.Framework*.dll` files into `tool/xna/`;
2. **Copy them from the Terraria game folder** — some distributions and older builds ship `Microsoft.Xna.Framework*.dll`
   next to `Terraria.exe`. If they're there, copy them to `tool/xna/`; if not, use route 1 (1.4.6.x on Steam loads
   them from the GAC and only has `Microsoft.Xna.Framework.Content.Pipeline.dll` / `Microsoft.Xna.Framework.Video.dll`).

> These three DLLs are **Microsoft redistributables and are not distributed with this repository**: `.gitignore`
> already excludes `tool/xna/`, and the repo only carries references to them (`build.ps1` globs `tool/xna/*.dll`).
> Put the files back and it builds. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

```powershell
pwsh -NoProfile -File tool\build.ps1            # build + hard verify + sync tool\ and release\
pwsh -NoProfile -File tool\build.ps1 -Deploy    # also back up and deploy into the game folder
```

The script compiles `ProjectionMod.dll` with csc (`/out:...\obj-out\ProjectionMod.dll`) and immediately runs
`tool\verify-dll.ps1`, which **hard-checks** the assembly name (Cecil reads `AssemblyDefinition.Name.Name`, which has
to be `ProjectionMod`), the `0Harmony` / `Terraria` reference versions and the icon pixels. Any failure exits with an
error — **nothing is copied or deployed**. Only `-Deploy` injects `Patcher.exe` into a copy of the exe, backs up the
old DLL and replaces it in the game folder.

> With csc, the **assembly name is the `/out:` file name.** Don't compile to one name and rename later — the assembly
> name is baked into the PE header and can't be changed, and the game then fails to load `ProjectionMod` and crashes
> with `System.IO.FileLoadException`. That is exactly what happened on 2026-11-25; see the assembly-name incident in
> [notes/printer-objects.md](notes/printer-objects.md).

## Compatibility

- Verified on **1.4.6.1** on a real install (`Harmony patches applied OK`, `compat self-check 11/11`); **1.4.5.8 also
  works**. The plugin is still compiled against 1.4.5.8 assembly references and binds by simple name at runtime.
- A future update (1.4.6.2+) needs the signatures re-mapped. The self-check in `tool/src/Compat.cs` names the item that
  broke, so the log tells you where to look.

## Known limitations

- Backups are per version: after a game update you **must re-run the installer** to get a backup for the new build
- The printer is disabled in multiplayer by default (it would look like cheating)
- Blueprints don't store chest contents or sign text
- Multi-tile furniture and torches aren't auto-printed without support (that would create invalid tiles)
- The single-file installer is about 64 MB, mostly the **self-contained .NET 8 runtime** (which is what saves you from
  installing .NET); the payload itself is only ~3 MB. If you'd rather not download that much, use the zip +
  `setup.cmd` route

## Contributing

This is a prototype, and anyone is welcome to take it further:

- Issues: for bugs, please include the game version, the error log and the steps to reproduce
- PRs: changes to the compatibility layer (`tool/src/Compat.cs`) are the most valuable — it needs attention every time
  the game updates
- New blueprint formats or feature ideas: open a discussion

## Disclaimer

- Not affiliated with Re-Logic or with Terraria in any way
- Modifying `Terraria.exe` is an unofficial approach. **In multiplayer, use projection and placement restriction only,
  not the printer**
- Judge for yourself whether a use case is appropriate; use at your own risk

## Credits and inspiration

This project was **inspired by the ideas** behind the "projection / blueprint" mods of the Minecraft ecosystem (such
as Litematica and Schematica) — laying a design over the world as a translucent overlay and building along with it.
Credit to those projects and their authors.

To be clear: the code, assets and implementation here are **entirely our own**. No code or assets from any of those
projects were used, and there is no affiliation, endorsement or licensing relationship with them. Vanilla Terraria
has no such feature; what you see here was designed from scratch for Terraria.

If any of those authors feels this section misrepresents their project, please open an issue and we'll adjust it.

## License

MIT

Third-party components bundled with this project and with the release packages in `release/` (**Lib.Harmony**,
**Mono.Cecil** and others) are under their own licenses; copyright notices and full license texts are in
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). The **Microsoft XNA Framework 4.0 is not distributed with this
repository** — install it or copy it from the game folder before building (see "Edit and build from source").

---

Repository: <https://github.com/whksm/terraria-projection>
