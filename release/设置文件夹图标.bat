@echo off
chcp 65001 >nul
cd /d "%~dp0"
if not exist "icon.ico" (
  echo 缺少 icon.ico，无法设置文件夹图标。
  pause
  exit /b 1
)
if not exist "desktop.ini" (
  > "desktop.ini" echo [.ShellClassInfo]
  >> "desktop.ini" echo IconResource=icon.ico,0
)
attrib +r "%~dp0."
attrib +h +s "desktop.ini"
echo 已设置文件夹图标（资源管理器若未刷新，按 F5 或重开窗口）。
pause
