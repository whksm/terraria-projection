@echo off
chcp 65001 >nul
title 泰拉瑞亚投影 Mod 卸载
cd /d "%~dp0"

set "GAMEDIR="
for /f "tokens=2*" %%A in ('reg query "HKCU\Software\Valve\Steam" /v SteamPath 2^>nul ^| findstr SteamPath') do set "STEAMPATH=%%B"
if defined STEAMPATH if exist "%STEAMPATH%\steamapps\common\Terraria\Terraria.exe" set "GAMEDIR=%STEAMPATH%\steamapps\common\Terraria"
if not defined GAMEDIR if exist "D:\steam\steamapps\common\Terraria\Terraria.exe" set "GAMEDIR=D:\steam\steamapps\common\Terraria"
if not defined GAMEDIR if exist "C:\Program Files (x86)\Steam\steamapps\common\Terraria\Terraria.exe" set "GAMEDIR=C:\Program Files (x86)\Steam\steamapps\common\Terraria"
if not defined GAMEDIR (
  echo 未自动找到游戏，请手动输入游戏目录（包含 Terraria.exe 的文件夹）后回车:
  set /p GAMEDIR=
)

if not exist "%GAMEDIR%\Terraria_backup_projection.exe" (
  echo.
  echo [错误] 没有找到备份文件 Terraria_backup_projection.exe，无法还原。
  pause
  exit /b 1
)

copy /y "%GAMEDIR%\Terraria_backup_projection.exe" "%GAMEDIR%\Terraria.exe" >nul
echo 已还原原版 Terraria.exe。
echo.
echo 可选清理（删除以下文件可完全移除，不影响游戏）:
echo   %GAMEDIR%\ProjectionMod.dll
echo   %GAMEDIR%\0Harmony.dll
echo   %GAMEDIR%\ProjectionMod.bp
echo   %GAMEDIR%\ProjectionMod.log
echo   %GAMEDIR%\Blueprints  （你保存的蓝图，建议保留）
echo.
pause
