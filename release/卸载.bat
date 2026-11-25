@echo off
chcp 65001 >nul
setlocal EnableExtensions
title 泰拉瑞亚投影 Mod 卸载

rem ==================================================================
rem  卸载 / 还原：只认同版本的干净备份
rem
rem  * 只接受 Terraria_backup_<当前版本>_vanilla.exe
rem    （也接受多带 .0 的老名字，如 1.4.6.1.0）
rem  * 找不到同版本备份时【明确报错】，绝不拿旧版本 exe 盖上去
rem    （1.4.5.8 的 exe 配 1.4.6.1 的 Content 必崩）
rem  * 备份必须自己也是原版；拿打过补丁的 exe 当备份同样拒绝
rem  * 版本/状态判定逻辑与 setup.cmd 的对应子过程一致，改动请两边同步
rem
rem  本文件必须 UTF-8（无 BOM）+ CRLF 行尾。
rem ==================================================================

rem 卸载.bat 就装在游戏目录里，优先用它自己的位置
set "GAMEDIR=%~dp0"
for %%I in ("%GAMEDIR%.") do set "GAMEDIR=%%~fI"
if exist "%GAMEDIR%\Terraria.exe" goto :found

rem 回退：注册表 / 常见路径（从 zip 里直接跑卸载.bat 时会走到这里）
set "GAMEDIR="
for /f "tokens=2*" %%A in ('reg query "HKCU\Software\Valve\Steam" /v SteamPath 2^>nul ^| findstr SteamPath') do set "STEAMPATH=%%B"
if defined STEAMPATH if exist "%STEAMPATH%\steamapps\common\Terraria\Terraria.exe" set "GAMEDIR=%STEAMPATH%\steamapps\common\Terraria"
if not defined GAMEDIR if exist "D:\steam\steamapps\common\Terraria\Terraria.exe" set "GAMEDIR=D:\steam\steamapps\common\Terraria"
if not defined GAMEDIR if exist "C:\Program Files (x86)\Steam\steamapps\common\Terraria\Terraria.exe" set "GAMEDIR=C:\Program Files (x86)\Steam\steamapps\common\Terraria"
if not defined GAMEDIR if exist "E:\steam\steamapps\common\Terraria\Terraria.exe" set "GAMEDIR=E:\steam\steamapps\common\Terraria"
if defined GAMEDIR goto :found
echo 自动找不到游戏，请手动输入游戏目录（包含 Terraria.exe 的文件夹）后回车：
set /p GAMEDIR=
if not defined GAMEDIR goto :cancelled
for %%I in ("%GAMEDIR%") do set "GAMEDIR=%%~fI"

:found
if not exist "%GAMEDIR%\Terraria.exe" goto :badpath
set "EXE=%GAMEDIR%\Terraria.exe"
call :read_exe_info

echo ============================================
echo   泰拉瑞亚投影 Mod 卸载 / 还原
echo ============================================
echo 游戏目录: %GAMEDIR%
if defined EXE_VER echo 当前版本: %EXE_VER%   %EXE_SIZE% 字节
if not defined EXE_VER echo 当前版本: 读不出   %EXE_SIZE% 字节
if /i "%IS_PATCHED%"=="PATCHED" echo 当前状态: 已打补丁
if /i "%IS_PATCHED%"=="CLEAN" echo 当前状态: 未打补丁（原版）
if /i "%IS_PATCHED%"=="UNKNOWN" echo 当前状态: 无法判断是否已打补丁
echo.

if /i "%IS_PATCHED%"=="CLEAN" goto :already_clean
if /i "%IS_PATCHED%"=="PATCHED" goto :do_restore
goto :unknown_state

rem ---------------- 已经是原版 ----------------
:already_clean
echo 当前 Terraria.exe 已经是原版，不需要还原 exe。
echo.
call :cleanup_hint
echo.
pause
exit /b 0

rem ---------------- 真正还原 ----------------
:do_restore
if not defined VER_TAG goto :no_version
call :find_vanilla
if not defined VANILLA goto :no_backup
call :read_ver_patched "%VANILLA%"
if /i "%OF_PATCHED%"=="PATCHED" goto :no_backup
echo 找到同版本原版备份: %VANILLA%
set "RESTORE_VER=%EXE_VER%"
copy /y "%VANILLA%" "%EXE%" >nul
if errorlevel 1 goto :copyfail
call :read_exe_info
echo 已还原: %EXE%
echo 版本 %EXE_VER%
if /i not "%EXE_VER%"=="%RESTORE_VER%" goto :mismatch
if /i not "%IS_PATCHED%"=="CLEAN" goto :still_patched
echo.
echo 还原成功。
call :cleanup_hint
echo.
pause
exit /b 0

rem ---------------- 各类失败 ----------------
:no_backup
echo [错误] 没有找到与当前版本 %EXE_VER% 相同、且自身是原版的备份。
echo        为了不把旧版本的 exe 盖到这个版本的游戏上（版本错配会崩），本次拒绝还原。
echo.
echo        只接受这种名字的备份: Terraria_backup_%EXE_VER%_vanilla.exe
echo.
call :steam_hint
echo.
echo        目录里现有的备份（都不会被采用）:
call :scan_backups
echo.
pause
exit /b 1

:no_version
echo [错误] 读不出当前 Terraria.exe 的版本号，无法确认该用哪一份备份。
echo        请关掉游戏后重试；仍读不出就用 Steam 校验游戏文件完整性。
echo.
pause
exit /b 1

:unknown_state
echo [错误] 判断不出当前 Terraria.exe 有没有打过补丁，为安全起见不做任何改动。
echo        请确认游戏已完全退出后重试。
echo.
pause
exit /b 1

:copyfail
echo [错误] 复制备份到 Terraria.exe 失败。常见原因：游戏还在运行、文件只读。
echo.
pause
exit /b 1

:mismatch
echo [错误] 还原后版本 %EXE_VER% 与目标版本 %RESTORE_VER% 不一致。
echo        请用 Steam 校验游戏文件完整性后重装。
echo.
pause
exit /b 1

:still_patched
echo [错误] 还原后 Terraria.exe 里仍有补丁标记。
echo        请用 Steam 校验游戏文件完整性后重装。
echo.
pause
exit /b 1

:badpath
echo.
echo [错误] 这个目录里没有 Terraria.exe：%GAMEDIR%
echo.
pause
exit /b 1

:cancelled
echo.
echo 已取消。
pause
exit /b 1

rem ==================================================================
rem  公共子过程（与 setup.cmd 保持一致）
rem ==================================================================

:read_exe_info
set "EXE_VER="
set "VER_TAG="
set "EXE_SIZE=0"
set "IS_PATCHED="
for %%S in ("%EXE%") do set "EXE_SIZE=%%~zS"
call :read_ver_patched "%EXE%"
set "EXE_VER=%OF_VER%"
set "VER_TAG=%OF_VER%"
set "IS_PATCHED=%OF_PATCHED%"
if "%EXE_VER%"=="?" set "EXE_VER="
if "%VER_TAG%"=="?" set "VER_TAG="
if not defined IS_PATCHED set "IS_PATCHED=UNKNOWN"
goto :eof

:read_ver_patched
rem  %1 = exe 路径。输出 OF_VER（规范化版本，读不出写 ?）和 OF_PATCHED（PATCHED/CLEAN/UNKNOWN）
set "OF_VER=?"
set "OF_PATCHED=UNKNOWN"
if not exist "%~1" goto :eof
set "OF_PATH=%~1"
for /f "tokens=1,2" %%A in ('powershell -NoProfile -ExecutionPolicy Bypass -Command "$f='%OF_PATH%'; $s=''; try{$i=(Get-Item -LiteralPath $f).VersionInfo; $s=$i.FileVersion; if(-not $s){$s=$i.ProductVersion}}catch{}; $s=($s -replace '^[^0-9]*','') -replace '[^0-9.].*$',''; $a=$s.Split('.'); while($a.Count -gt 3 -and $a[$a.Count-1] -eq '0'){ $a=$a[0..($a.Count-2)] }; $s=$a -join '.'; if(-not $s){$s='?'}; $p='UNKNOWN'; try{ $b=[IO.File]::ReadAllBytes($f); $t=[Text.Encoding]::GetEncoding(28591).GetString($b); if($t.Contains('ProjectionMod')){$p='PATCHED'}else{$p='CLEAN'} }catch{}; '{0} {1}' -f $s, $p"') do ( set "OF_VER=%%A" & set "OF_PATCHED=%%B" )
goto :eof

:find_vanilla
set "VANILLA="
if not defined VER_TAG goto :eof
for /f "delims=" %%B in ('dir /b /a-d "%GAMEDIR%\Terraria_backup_*_vanilla.exe" 2^>nul') do call :match_vanilla "%%B"
goto :eof

:match_vanilla
if defined VANILLA goto :eof
set "MNAME=%~1"
set "MTAG=%MNAME:Terraria_backup_=%"
set "MTAG=%MTAG:_vanilla.exe=%"
if /i "%MTAG%"=="%VER_TAG%" set "VANILLA=%GAMEDIR%\%MNAME%"
if defined VANILLA goto :eof
echo %MTAG%| findstr /r /c:"^%VER_TAG%\.0*$" >nul
if not errorlevel 1 set "VANILLA=%GAMEDIR%\%MNAME%"
goto :eof

:scan_backups
set "FOUNDBK="
for /f "delims=" %%B in ('dir /b /a-d "%GAMEDIR%\Terraria*backup*.exe" 2^>nul') do call :scan_one "%%B"
if not defined FOUNDBK echo         （目录里没有 *backup*.exe）
goto :eof

:scan_one
set "FOUNDBK=1"
set "SBNAME=%~1"
for %%S in ("%GAMEDIR%\%SBNAME%") do set "SBSIZE=%%~zS"
call :read_ver_patched "%GAMEDIR%\%SBNAME%"
set "SBVER=%OF_VER%"
set "SBSTATE=%OF_PATCHED%"
if "%SBVER%"=="?" set "SBVER=读不出"
if /i "%SBSTATE%"=="PATCHED" set "SBSTATE=已打补丁"
if /i "%SBSTATE%"=="CLEAN" set "SBSTATE=原版"
if /i "%SBSTATE%"=="UNKNOWN" set "SBSTATE=状态未知"
echo         %SBNAME%   版本 %SBVER%   %SBSIZE% 字节   %SBSTATE%
goto :eof

:steam_hint
echo       补救办法（会让补丁失效，之后需要重装一次 mod）:
echo         1. 关掉游戏，Steam 库里右键泰拉瑞亚 - 属性 - 已安装文件 - 验证游戏文件完整性
echo         2. Steam 会把 Terraria.exe 还原成当前版本的原版 exe（补丁同时被清掉）
echo         3. 重新运行一次 setup.cmd 或 安装.bat
echo            这次会生成 Terraria_backup_^<版本^>_vanilla.exe 这份干净备份
echo         4. 装完再进游戏
goto :eof

:cleanup_hint
echo 可选清理（删掉这些就完全移除 mod，不影响游戏）:
echo   %GAMEDIR%\ProjectionMod.dll
echo   %GAMEDIR%\0Harmony.dll
echo   %GAMEDIR%\ProjectionMod.ico
echo   %GAMEDIR%\ProjectionMod.log
echo   %GAMEDIR%\卸载.bat
echo   %GAMEDIR%\投影MOD_使用说明.md
echo   %GAMEDIR%\Blueprints    你保存的蓝图，建议保留
echo 备份文件建议留着，下次重装或还原还要用。
goto :eof
