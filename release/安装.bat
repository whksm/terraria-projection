@echo off
chcp 65001 >nul
title 泰拉瑞亚投影 Mod 一键安装
cd /d "%~dp0"

echo ============================================
echo   泰拉瑞亚投影 Mod 安装器（原版 1.4.5.8）
echo ============================================
echo.

set "GAMEDIR="
for /f "tokens=2*" %%A in ('reg query "HKCU\Software\Valve\Steam" /v SteamPath 2^>nul ^| findstr SteamPath') do set "STEAMPATH=%%B"
if defined STEAMPATH if exist "%STEAMPATH%\steamapps\common\Terraria\Terraria.exe" set "GAMEDIR=%STEAMPATH%\steamapps\common\Terraria"
if not defined GAMEDIR if exist "D:\steam\steamapps\common\Terraria\Terraria.exe" set "GAMEDIR=D:\steam\steamapps\common\Terraria"
if not defined GAMEDIR if exist "C:\Program Files (x86)\Steam\steamapps\common\Terraria\Terraria.exe" set "GAMEDIR=C:\Program Files (x86)\Steam\steamapps\common\Terraria"
if not defined GAMEDIR (
  echo 未自动找到游戏，请手动输入游戏目录（包含 Terraria.exe 的文件夹）后回车:
  set /p GAMEDIR=
)

if not exist "%GAMEDIR%\Terraria.exe" (
  echo.
  echo [错误] 该目录下没有 Terraria.exe，请检查后重试。
  pause
  exit /b 1
)

echo 游戏目录: %GAMEDIR%
echo.
echo [1/4] 打补丁（自动备份原版 Terraria.exe）...
Patcher.exe patch "%GAMEDIR%"
if errorlevel 1 (
  echo.
  echo [错误] 打补丁失败。可能原因：游戏正在运行 / 版本不是 1.4.5.8。
  pause
  exit /b 1
)

echo [2/4] 复制运行库与演示蓝图...
copy /y ProjectionMod.dll "%GAMEDIR%\" >nul
copy /y 0Harmony.dll "%GAMEDIR%\" >nul
if not exist "%GAMEDIR%\ProjectionMod.bp" copy /y ProjectionMod.bp "%GAMEDIR%\" >nul
if exist "icon.ico" copy /y icon.ico "%GAMEDIR%\ProjectionMod.ico" >nul

echo [3/4] 生成本模组图标与桌面快捷方式...
if exist "icon.ico" (
  powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$ico='%GAMEDIR%\ProjectionMod.ico'; if (Test-Path $ico) {" ^
    "  $desk=[Environment]::GetFolderPath('Desktop');" ^
    "  $lnk=Join-Path $desk '泰拉瑞亚投影MOD.lnk';" ^
    "  $s=(New-Object -ComObject WScript.Shell).CreateShortcut($lnk);" ^
    "  $s.TargetPath='%GAMEDIR%\Terraria.exe';" ^
    "  $s.WorkingDirectory='%GAMEDIR%';" ^
    "  $s.IconLocation=$ico;" ^
    "  $s.Description='泰拉瑞亚投影MOD（原版 1.4.5.8）';" ^
    "  $s.Save(); Write-Host ('  已创建桌面快捷方式: ' + $lnk) }"
)

echo [4/4] 完成！
echo.
echo 原版备份: %GAMEDIR%\Terraria_backup_projection.exe
echo 启动方式: 桌面「泰拉瑞亚投影MOD」快捷方式，或通过 Steam 正常启动游戏。
echo           支持 MultiRod 等自行打补丁的 exe，如需在那个 exe 上也生效，
echo           请参考使用说明里的手动步骤。
echo 卸载方式: 运行 卸载.bat（桌面快捷方式可手动删除）
echo.
pause
