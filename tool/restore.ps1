# 还原脚本：恢复原版 Terraria.exe（从 Terraria_backup_projection.exe）
$ErrorActionPreference = 'Stop'
$game = 'd:\steam\steamapps\common\Terraria'
$backup = Join-Path $game 'Terraria_backup_projection.exe'
if (-not (Test-Path $backup)) { Write-Host '没有备份，无法还原'; exit 1 }
Copy-Item $backup (Join-Path $game 'Terraria.exe') -Force
Write-Host '已还原原版 Terraria.exe（ProjectionMod.dll / 0Harmony.dll 可留可删，不影响原版运行）'
