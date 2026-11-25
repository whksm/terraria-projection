@echo off
chcp 65001 >nul
setlocal EnableExtensions
title 泰拉瑞亚投影MOD 安装器

rem ==================================================================
rem  泰拉瑞亚投影MOD 安装器 · setup.cmd（zip 分发方案）
rem
rem  备份策略（v1.3.0 起，与单文件安装器 SetupApp 完全一致）：
rem    * 备份名带版本：Terraria_backup_<版本>_vanilla.exe
rem      版本取自 Terraria.exe 的 FileVersion，例如 1.4.6.1
rem    * 只备份「未打补丁」的 exe；已经打过补丁的 exe 绝不当原版备份
rem    * 版本一变就必须重新备份（不再「已存在就跳过」）
rem    * 卸载/还原只认与当前 exe 版本相同的 vanilla 备份，
rem      找不到就报错并提示 Steam 校验完整性，绝不默默盖旧版本上去
rem
rem  用法：
rem    setup.cmd                       安装 · 自动找游戏目录
rem    setup.cmd "D:\...\Terraria"     安装 · 指定游戏目录
rem    setup.cmd "目录" --silent       静默安装 · 退出码 0=成功 1=失败
rem    setup.cmd --check "目录"        只检查版本/备份状态，不写任何文件
rem    setup.cmd --backup-only "目录"  只生成版本化备份，不打补丁
rem    setup.cmd --restore "目录"      用同版本 vanilla 备份还原 Terraria.exe
rem
rem  本文件必须 UTF-8（无 BOM）+ CRLF 行尾。LF 会让 cmd 解析错乱（踩过）。
rem ==================================================================

set "ROOT=%~dp0"
set "GAMEDIR="
set "MODE=install"
set "NOPAUSE="
set "SILENT="
set "BAKFAIL="
set "NOCLEANBACKUP="

:parse_args
if "%~1"=="" goto :args_done
if /i "%~1"=="--silent" ( set "SILENT=1" & set "NOPAUSE=1" & shift & goto :parse_args )
if /i "%~1"=="--no-pause" ( set "NOPAUSE=1" & shift & goto :parse_args )
if /i "%~1"=="--check" ( set "MODE=check" & shift & goto :parse_args )
if /i "%~1"=="--restore" ( set "MODE=restore" & shift & goto :parse_args )
if /i "%~1"=="--backup-only" ( set "MODE=backup" & shift & goto :parse_args )
if /i "%~1"=="--help" ( set "NOPAUSE=1" & goto :usage )
if /i "%~1"=="-h" ( set "NOPAUSE=1" & goto :usage )
if /i "%~1"=="/?" ( set "NOPAUSE=1" & goto :usage )
if not defined GAMEDIR ( set "GAMEDIR=%~1" & shift & goto :parse_args )
shift
goto :parse_args

:args_done
if not defined GAMEDIR if defined PM_DIR set "GAMEDIR=%PM_DIR%"
if defined PM_SILENT set "SILENT=1"
if defined SILENT set "NOPAUSE=1"

if /i not "%MODE%"=="install" goto :check_gamedir
echo ============================================
echo   泰拉瑞亚投影MOD  安装器
echo   备份按版本命名，只备份未打补丁的 exe
echo ============================================
echo.

:check_gamedir
if defined GAMEDIR goto :found
goto :detect

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
for %%I in ("%GAMEDIR%") do set "GAMEDIR=%%~fI"
if not exist "%GAMEDIR%\Terraria.exe" goto :badpath
set "EXE=%GAMEDIR%\Terraria.exe"
if /i "%MODE%"=="check" goto :mode_check
if /i "%MODE%"=="restore" goto :mode_restore
if /i "%MODE%"=="backup" goto :mode_backup
goto :install

rem ==================================================================
rem  安装
rem ==================================================================

:install
echo 游戏目录: %GAMEDIR%
echo.
echo [1/4] 检查版本 / 备份原版 exe
call :ensure_backup
if defined BAKFAIL goto :backupfail

echo [2/4] 打补丁
call :apply_patch
if defined PATCHFAIL goto :patchfail

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
echo   卸载：游戏目录里的 卸载.bat
if defined NOCLEANBACKUP echo.
if defined NOCLEANBACKUP echo   [重要] 本版本没有干净的原版备份，卸载时将无法还原 exe。
if defined NOCLEANBACKUP echo          请按上面的补救步骤先恢复干净原版，再重跑本安装器。
echo ============================================
echo.
if not defined NOPAUSE pause
exit /b 0

rem ==================================================================
rem  模式：只检查状态
rem ==================================================================

:mode_check
call :read_exe_info
echo ============================================
echo   备份状态检查（只读，不写任何文件）
echo ============================================
echo 游戏目录: %GAMEDIR%
if defined EXE_VER echo Terraria.exe 版本: %EXE_VER%   %EXE_SIZE% 字节
if not defined EXE_VER echo Terraria.exe 版本: 读不出   %EXE_SIZE% 字节
if /i "%IS_PATCHED%"=="PATCHED" echo Terraria.exe 状态: 已打补丁
if /i "%IS_PATCHED%"=="CLEAN" echo Terraria.exe 状态: 未打补丁（原版）
if /i "%IS_PATCHED%"=="UNKNOWN" echo Terraria.exe 状态: 无法判断是否已打补丁
echo.
call :find_vanilla
set "CLEANBK="
if not defined VANILLA goto :ck_bk_done
call :read_ver_patched "%VANILLA%"
if /i "%OF_PATCHED%"=="PATCHED" goto :ck_bk_bad
echo 本版本原版备份: %VANILLA%
set "CLEANBK=1"
goto :ck_bk_done

:ck_bk_bad
echo 本版本备份: %VANILLA%
echo       但它本身是打过补丁的，不能当原版用。

:ck_bk_done
if defined VER_TAG if not defined VANILLA echo 本版本原版备份: 没有
echo.
echo 目录里现有的备份文件:
call :scan_backups
echo.
if not defined VER_TAG goto :ck_nover
if /i "%IS_PATCHED%"=="PATCHED" goto :ck_patched
if /i "%IS_PATCHED%"=="CLEAN" goto :ck_clean
goto :ck_unknown

:ck_patched
if defined CLEANBK echo 结论: 已打补丁，且本版本有干净备份，可随时用 --restore 还原。
if not defined CLEANBK echo 结论: 已打补丁，但本版本【没有】干净备份，现在不能还原 exe。
if not defined CLEANBK call :steam_hint
goto :ck_end

:ck_clean
echo 结论: 当前 Terraria.exe 是原版（未打补丁），无需还原。
goto :ck_end

:ck_nover
echo 结论: 读不出版本号，无法判断备份是否匹配。
goto :ck_end

:ck_unknown
echo 结论: 判断不出补丁状态，建议关掉游戏后再跑一次。
:ck_end
echo.
if not defined NOPAUSE pause
exit /b 0

rem ==================================================================
rem  模式：只备份
rem ==================================================================

:mode_backup
echo ============================================
echo   只创建版本化备份（不打补丁）
echo ============================================
echo 游戏目录: %GAMEDIR%
echo.
call :ensure_backup
if defined BAKFAIL goto :backupfail
echo.
if defined NOCLEANBACKUP echo 注意: 本版本没有生成干净备份，原因见上面。
if not defined NOCLEANBACKUP echo 备份就绪。
echo.
if not defined NOPAUSE pause
exit /b 0

rem ==================================================================
rem  模式：还原（按版本选备份）
rem ==================================================================

:mode_restore
call :read_exe_info
echo ============================================
echo   还原原版 Terraria.exe（按版本选备份）
echo ============================================
echo 游戏目录: %GAMEDIR%
if defined EXE_VER echo 当前版本: %EXE_VER%   %EXE_SIZE% 字节
if not defined EXE_VER echo 当前版本: 读不出   %EXE_SIZE% 字节
if /i "%IS_PATCHED%"=="PATCHED" echo 当前状态: 已打补丁
if /i "%IS_PATCHED%"=="CLEAN" echo 当前状态: 未打补丁（原版）
if /i "%IS_PATCHED%"=="UNKNOWN" echo 当前状态: 无法判断是否已打补丁
echo.
if /i "%IS_PATCHED%"=="CLEAN" goto :rs_already
if /i "%IS_PATCHED%"=="PATCHED" goto :rs_do
goto :rs_unknown

:rs_already
echo 当前 Terraria.exe 已经是原版，不需要还原 exe。
echo.
call :cleanup_hint
echo.
if not defined NOPAUSE pause
exit /b 0

:rs_do
if not defined VER_TAG goto :rs_nover
call :find_vanilla
if not defined VANILLA goto :rs_nobackup
call :read_ver_patched "%VANILLA%"
if /i "%OF_PATCHED%"=="PATCHED" goto :rs_nobackup
echo 找到同版本原版备份: %VANILLA%
set "RESTORE_FROM=%VANILLA%"
set "RESTORE_VER=%EXE_VER%"
copy /y "%RESTORE_FROM%" "%EXE%" >nul
if errorlevel 1 goto :rs_copyfail
call :read_exe_info
set "STATE_CN=%IS_PATCHED%"
if /i "%STATE_CN%"=="CLEAN" set "STATE_CN=原版"
if /i "%STATE_CN%"=="PATCHED" set "STATE_CN=已打补丁"
if /i "%STATE_CN%"=="UNKNOWN" set "STATE_CN=状态未知"
echo 已还原: %EXE%   版本 %EXE_VER%   状态 %STATE_CN%
if /i not "%EXE_VER%"=="%RESTORE_VER%" goto :rs_mismatch
if /i not "%IS_PATCHED%"=="CLEAN" goto :rs_stillpatched
echo.
echo 还原成功。
call :cleanup_hint
echo.
if not defined NOPAUSE pause
exit /b 0

:rs_nobackup
echo [错误] 没有找到与当前版本 %EXE_VER% 相同、且内部是原版的备份。
echo        为了不把旧版本 exe 盖到这个版本的游戏上（版本错配会崩），本次拒绝还原。
echo.
echo        只接受这种名字的备份: Terraria_backup_%EXE_VER%_vanilla.exe
echo.
call :steam_hint
echo.
echo        目录里现有的备份（都不会被采用）:
call :scan_backups
echo.
if not defined NOPAUSE pause
exit /b 1

:rs_nover
echo [错误] 读不出当前 Terraria.exe 的版本号，无法确认该用哪一份备份。
echo        请关掉游戏后重试；仍读不出就用 Steam 校验完整性。
echo.
if not defined NOPAUSE pause
exit /b 1

:rs_unknown
echo [错误] 判断不出当前 Terraria.exe 有没有打过补丁，为安全起见不做任何改动。
echo        请确认游戏已完全退出后重试。
echo.
if not defined NOPAUSE pause
exit /b 1

:rs_copyfail
echo [错误] 复制备份到 Terraria.exe 失败。常见原因：游戏还在运行、文件只读。
echo.
if not defined NOPAUSE pause
exit /b 1

:rs_mismatch
echo [错误] 还原后版本 %EXE_VER% 与目标版本 %RESTORE_VER% 不一致，请 Steam 校验完整性。
echo.
if not defined NOPAUSE pause
exit /b 1

:rs_stillpatched
echo [错误] 还原后 Terraria.exe 里仍有补丁标记，请 Steam 校验完整性。
echo.
if not defined NOPAUSE pause
exit /b 1

rem ==================================================================
rem  公共子过程
rem ==================================================================

:read_exe_info
rem  读取 %EXE% 的版本 / 大小 / 补丁状态，写 EXE_VER VER_TAG EXE_SIZE IS_PATCHED
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
rem  %1 = exe 路径。 输出 OF_VER（规范化版本，读不出写 ?）和 OF_PATCHED（PATCHED/CLEAN/UNKNOWN）
set "OF_VER=?"
set "OF_PATCHED=UNKNOWN"
if not exist "%~1" goto :eof
set "OF_PATH=%~1"
for /f "tokens=1,2" %%A in ('powershell -NoProfile -ExecutionPolicy Bypass -Command "$f='%OF_PATH%'; $s=''; try{$i=(Get-Item -LiteralPath $f).VersionInfo; $s=$i.FileVersion; if(-not $s){$s=$i.ProductVersion}}catch{}; $s=($s -replace '^[^0-9]*','') -replace '[^0-9.].*$',''; $a=$s.Split('.'); while($a.Count -gt 3 -and $a[$a.Count-1] -eq '0'){ $a=$a[0..($a.Count-2)] }; $s=$a -join '.'; if(-not $s){$s='?'}; $p='UNKNOWN'; try{ $b=[IO.File]::ReadAllBytes($f); $t=[Text.Encoding]::GetEncoding(28591).GetString($b); if($t.Contains('ProjectionMod')){$p='PATCHED'}else{$p='CLEAN'} }catch{}; '{0} {1}' -f $s, $p"') do ( set "OF_VER=%%A" & set "OF_PATCHED=%%B" )
goto :eof

:find_vanilla
rem  找与当前 %VER_TAG% 同版本的 Terraria_backup_*_vanilla.exe，写 VANILLA
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
rem  打印目录里所有 *backup*.exe 的名字 / 版本 / 补丁状态（只读）
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

:ensure_backup
rem  安装/只备份模式共用：按版本决定要不要（重新）备份
set "BAKFAIL="
call :read_exe_info
if defined EXE_VER echo       版本 %EXE_VER%   %EXE_SIZE% 字节
if not defined EXE_VER echo       版本 读不出   %EXE_SIZE% 字节
if /i "%IS_PATCHED%"=="PATCHED" echo       状态 已打补丁
if /i "%IS_PATCHED%"=="CLEAN" echo       状态 未打补丁（原版）
if /i "%IS_PATCHED%"=="UNKNOWN" echo       状态 无法判断是否已打补丁
if not defined VER_TAG goto :eb_nover
call :find_vanilla
if /i "%IS_PATCHED%"=="PATCHED" goto :eb_patched
if /i "%IS_PATCHED%"=="CLEAN" goto :eb_clean
goto :eb_unknown

:eb_patched
if not defined VANILLA goto :eb_patched_nobk
call :read_ver_patched "%VANILLA%"
if /i "%OF_PATCHED%"=="PATCHED" goto :eb_patched_badbk
echo       本版本已有干净备份，跳过备份: %VANILLA%
echo       当前 exe 已打过补丁，不会拿它当原版备份。
goto :eof

:eb_patched_badbk
echo       [警告] %VANILLA% 本身是打过补丁的，不能当原版备份。
goto :eb_patched_nobk

:eb_patched_nobk
echo.
echo       [重要] 当前 Terraria.exe 已经打过补丁，不能当作原版备份；
echo              而本版本 %EXE_VER% 又没有一份干净的原版备份。
echo              一旦卸载，就没有可还原的原版 exe 了。
echo.
call :steam_hint
echo.
echo       现有备份（都不会被当作 %EXE_VER% 的原版）:
call :scan_backups
set "NOCLEANBACKUP=1"
goto :eof

:eb_clean
if not defined VANILLA goto :eb_clean_new
call :read_ver_patched "%VANILLA%"
if /i "%OF_PATCHED%"=="PATCHED" goto :eb_clean_badbk
if /i "%OF_PATCHED%"=="CLEAN" goto :eb_clean_ok
echo       [警告] 判断不出已有备份 %VANILLA% 是不是原版，保持不动。
echo              以后还原若出问题，请 Steam 校验完整性后重装。
goto :eof

:eb_clean_ok
echo       本版本备份已存在且是原版，跳过: %VANILLA%
goto :eof

:eb_clean_badbk
echo       [警告] 已有备份 %VANILLA% 内部是打过补丁的，用当前原版覆盖它。
set "BK=%VANILLA%"
call :copy_backup
goto :eof

:eb_clean_new
echo       本版本还没有备份，现在创建: Terraria_backup_%VER_TAG%_vanilla.exe
set "BK=%GAMEDIR%\Terraria_backup_%VER_TAG%_vanilla.exe"
call :copy_backup
if defined BAKFAIL goto :eof
echo       注意：目录里其他版本的备份只对各自版本有效，卸载时只认同版本：
call :scan_backups
goto :eof

:eb_unknown
echo       [警告] 判断不出当前 exe 有没有打过补丁（文件被占用或权限不足），
echo              为了安全这次不创建备份。
if defined VANILLA echo              本版本已有的原版备份继续可用: %VANILLA%
if defined VANILLA goto :eof
echo.
call :steam_hint
set "NOCLEANBACKUP=1"
goto :eof

:eb_nover
echo       [警告] 读不出 Terraria.exe 的版本号，无法按版本命名备份，跳过备份。
echo              请关掉游戏重试；读不出版本就先别装，先确认游戏是原版。
set "NOCLEANBACKUP=1"
goto :eof

:copy_backup
rem  把 %EXE% 复制成 %BK%，校验大小
copy /y "%EXE%" "%BK%" >nul
if errorlevel 1 goto :cb_fail
for %%S in ("%EXE%") do set "S1=%%~zS"
for %%S in ("%BK%") do set "S2=%%~zS"
if not "%S1%"=="%S2%" goto :cb_size
echo       已创建备份: %BK%   %S2% 字节
goto :eof

:cb_fail
echo       [错误] 备份失败。常见原因：游戏还在运行、文件只读。
set "BAKFAIL=1"
goto :eof

:cb_size
echo       [错误] 备份大小和原文件不一致（%S2% 与 %S1% 字节）。
set "BAKFAIL=1"
goto :eof

:apply_patch
rem  只让 Patcher 把成品写到临时目录（不碰游戏目录、不让它自己乱建备份），
rem  校验成品里真有补丁标记后，再原子替换 Terraria.exe。
rem  注意：Patcher 自带的 IsPatched 判定不可靠（它只认 AssemblyRef 名字），
rem  所以这里先用本脚本自己的标记扫描结果跳过，避免重复注入 Bootstrap.Init。
set "PATCHFAIL="
if /i "%IS_PATCHED%"=="PATCHED" goto :ap_already
if not exist "%ROOT%Patcher.exe" goto :ap_nopatcher
set "PATCHDIR=%TEMP%\ProjectionModPatch_%RANDOM%_%RANDOM%"
mkdir "%PATCHDIR%" 2>nul
copy /y "%ROOT%Patcher.exe" "%PATCHDIR%\" >nul
copy /y "%ROOT%Mono.Cecil.dll" "%PATCHDIR%\" >nul
copy /y "%ROOT%ProjectionMod.dll" "%PATCHDIR%\" >nul
if not exist "%PATCHDIR%\ProjectionMod.dll" goto :ap_nopayload
pushd "%PATCHDIR%"
"%PATCHDIR%\Patcher.exe" patch "%GAMEDIR%" "%PATCHDIR%"
set "PRC=%ERRORLEVEL%"
popd
if not exist "%PATCHDIR%\Terraria.exe.tmp" goto :ap_noout
call :read_ver_patched "%PATCHDIR%\Terraria.exe.tmp"
if /i not "%OF_PATCHED%"=="PATCHED" goto :ap_badtmp
copy /y "%PATCHDIR%\Terraria.exe.tmp" "%EXE%.new" >nul
if errorlevel 1 goto :ap_copyfail
move /y "%EXE%.new" "%EXE%" >nul
if errorlevel 1 goto :ap_movefail
call :read_exe_info
echo       补丁完成: %EXE%   %EXE_SIZE% 字节   版本 %EXE_VER%
goto :ap_end

:ap_noout
rem  Patcher 没产出成品：要么它自己判断已经打过补丁（幂等），要么就是失败了
call :read_exe_info
if /i "%IS_PATCHED%"=="PATCHED" goto :ap_already
echo       [错误] Patcher 没有产出补丁文件（退出码 %PRC%）
set "PATCHFAIL=1"
goto :ap_end

:ap_already
echo       当前 exe 已经是打过补丁的状态，跳过（重复安装是幂等的）
goto :ap_end

:ap_badtmp
echo       [错误] 补丁成品里没有 ProjectionMod 标记，已放弃写入。
set "PATCHFAIL=1"
goto :ap_end

:ap_copyfail
echo       [错误] 写补丁成品失败（%EXE%.new）。游戏可能还在运行。
set "PATCHFAIL=1"
goto :ap_end

:ap_movefail
echo       [错误] 替换 Terraria.exe 失败。请确认游戏已完全退出。
if exist "%EXE%.new" del /f /q "%EXE%.new" >nul 2>nul
set "PATCHFAIL=1"
goto :ap_end

:ap_nopatcher
echo       [错误] 安装包里没有 Patcher.exe，无法打补丁。
set "PATCHFAIL=1"
goto :ap_end

:ap_nopayload
echo       [错误] 解压补丁工具失败（%PATCHDIR%）。
set "PATCHFAIL=1"
goto :ap_end

:ap_end
if defined PATCHDIR if exist "%PATCHDIR%" rd /s /q "%PATCHDIR%" >nul 2>nul
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

rem ==================================================================
rem  错误出口 / 帮助
rem ==================================================================

:backupfail
echo.
echo [失败] 备份原版 Terraria.exe 没成功，安装已中止。
echo        没有干净备份就装上，以后卸载就没有可还原的原版了。
echo.
if not defined NOPAUSE pause
exit /b 1

:patchfail
echo.
echo [失败] 打补丁没成功。
echo        常见原因：游戏正在运行 / Terraria.exe 只读 / exe 结构异常。
echo        请完全退出游戏后再试；仍失败请把 ProjectionMod.log 和本窗口内容发给作者。
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

:usage
echo 泰拉瑞亚投影MOD 安装器 · setup.cmd
echo.
echo 用法:
echo   setup.cmd                       安装 · 自动找游戏目录
echo   setup.cmd "D:\...\Terraria"     安装 · 指定游戏目录
echo   setup.cmd "目录" --silent       静默安装 · 退出码 0=成功 1=失败
echo   setup.cmd --check "目录"        只检查版本与备份状态，不写任何文件
echo   setup.cmd --backup-only "目录"  只创建版本化备份，不打补丁
echo   setup.cmd --restore "目录"      用同版本 vanilla 备份还原 Terraria.exe
echo.
echo 备份命名: Terraria_backup_^<版本^>_vanilla.exe
echo           例 Terraria_backup_1.4.6.1_vanilla.exe
echo 规则: 只备份未打补丁的 exe；版本变了重新备份；还原只认同版本备份。
echo 退出码: 0 成功 · 1 失败
echo.
if not defined NOPAUSE pause
exit /b 0
