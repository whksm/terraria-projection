@echo off
chcp 65001 >nul
setlocal EnableExtensions
title 泰拉瑞亚投影MOD 安装器

set "ROOT=%~dp0"
set "GAMEDIR=%~1"
if not defined GAMEDIR if defined PM_DIR set "GAMEDIR=%PM_DIR%"
if defined PM_SILENT set "NOPAUSE=1"
if /i "%~2"=="--silent" set "NOPAUSE=1"   & rem SILENTFLAG

echo ============================================
echo   泰拉瑞亚投影MOD  安装器（原版 1.4.5.8）
echo ============================================
echo.

if not defined GAMEDIR goto :detect
goto :found

:detect
for /f "tokens=2*" %%A in ('reg query "HKCU\Software\Valve\Steam" /v SteamPath 2^>nul ^| findstr SteamPath') do set "STEAMPATH=%%B"
if not defined STEAMPATH goto :detect2
if exist "%STEAMPATH%\steamapps\common\Terraria\Terraria.exe" set "GAMEDIR=%STEAMPATH%\steamapps\common\Terraria"
if defined GAMEDIR goto :found

:detect2
if exist "D:\steam\steamapps\common\Terraria\Terraria.exe" set "GAMEDIR=D:\steam\steamapps\common\Terraria"
if defined GAMEDIR goto :found
if exist "C:\Program Files (x86)\Steam\steamapps\common\Terraria\Terraria.exe" set "GAMEDIR=C:\Program Files (x86)\Steam\steamapps\common\Terraria"
if defined GAMEDIR goto :found
if exist "E:\steam\steamapps\common\Terraria\Terraria.exe" set "GAMEDIR=E:\steam\steamapps\common\Terraria"
if defined GAMEDIR goto :found

echo 没自动找到泰拉瑞亚。
echo 请把游戏目录（包含 Terraria.exe 的文件夹）粘贴进来后回车，直接回车=取消：
set /p GAMEDIR=
if not defined GAMEDIR goto :cancelled

:found
if not exist "%GAMEDIR%\Terraria.exe" goto :badpath

echo 游戏目录: %GAMEDIR%
echo.

echo [1/4] 备份原版 exe
if exist "%GAMEDIR%\Terraria_backup_projection.exe" goto :backup_ok
copy /y "%GAMEDIR%\Terraria.exe" "%GAMEDIR%\Terraria_backup_projection.exe" >nul
echo       已备份为 Terraria_backup_projection.exe
goto :patch_step

:backup_ok
echo       已存在备份，跳过

:patch_step
echo [2/4] 打补丁
"%ROOT%Patcher.exe" patch "%GAMEDIR%"
if errorlevel 1 goto :patchfail

echo [3/4] 复制文件
copy /y "%ROOT%ProjectionMod.dll" "%GAMEDIR%\" >nul
copy /y "%ROOT%0Harmony.dll" "%GAMEDIR%\" >nul
if exist "%GAMEDIR%\ProjectionMod.bp" goto :skip_bp
copy /y "%ROOT%ProjectionMod.bp" "%GAMEDIR%\" >nul
:skip_bp
if not exist "%ROOT%icon.ico" goto :skip_icon
copy /y "%ROOT%icon.ico" "%GAMEDIR%\ProjectionMod.ico" >nul
:skip_icon
if not exist "%ROOT%卸载.bat" goto :skip_uninst
copy /y "%ROOT%卸载.bat" "%GAMEDIR%\卸载.bat" >nul
:skip_uninst
if not exist "%ROOT%使用说明.md" goto :skip_doc
copy /y "%ROOT%使用说明.md" "%GAMEDIR%\投影MOD_使用说明.md" >nul
:skip_doc

echo [4/4] 创建桌面快捷方式
if not exist "%ROOT%shortcut.ps1" goto :no_ps
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%shortcut.ps1" "%GAMEDIR%"
goto :done

:no_ps
echo       安装包缺少 shortcut.ps1，跳过

:done
echo.
echo ============================================
echo   安装完成！
echo   启动：桌面「泰拉瑞亚投影MOD」或 Steam 正常启动
echo   卸载：重跑本安装包里的卸载.bat
echo ============================================
echo.
if not defined NOPAUSE pause
exit /b 0

:patchfail
echo.
echo [失败] 打补丁没成功。
echo        常见原因：游戏正在运行 / 版本不是 1.4.5.8。
echo        请完全退出游戏后再试。
echo.
if not defined NOPAUSE pause
exit /b 1

:badpath
echo.
echo [失败] 这个目录里没有 Terraria.exe。
echo.
if not defined NOPAUSE pause
exit /b 1

:cancelled
echo.
echo 已取消。
if not defined NOPAUSE pause
exit /b 1
