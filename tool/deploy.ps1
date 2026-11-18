# 部署脚本：把投影补丁写入游戏目录（自动备份原版，同时覆盖 Terraria.exe 与 Terraria_MultiRod.exe）
$ErrorActionPreference = 'Stop'
$tool = $PSScriptRoot
$game = 'd:\steam\steamapps\common\Terraria'
$build = Join-Path $tool 'build'

foreach ($exeName in @('Terraria.exe', 'Terraria_MultiRod.exe')) {
  $srcFile = Join-Path $game $exeName
  if (-not (Test-Path $srcFile)) { Write-Host "跳过（不存在）: $exeName"; continue }

  $srcDir = Join-Path $build ('src-' + $exeName)
  $outDir = Join-Path $build ('out-' + $exeName)
  New-Item -ItemType Directory -Force -Path $srcDir, $outDir | Out-Null
  Copy-Item $srcFile (Join-Path $srcDir 'Terraria.exe') -Force

  & (Join-Path $tool 'Patcher.exe') patch $srcDir $outDir
  if ($LASTEXITCODE -ne 0) { throw "Patcher 失败: $exeName" }
  Rename-Item (Join-Path $outDir 'Terraria.exe.tmp') 'Terraria.exe' -ErrorAction SilentlyContinue

  $backup = Join-Path $game ($exeName -replace '\.exe$', '_backup_projection.exe')
  if (-not (Test-Path $backup)) {
    Copy-Item $srcFile $backup
    Write-Host "已备份 -> $backup"
  }
  Copy-Item (Join-Path $outDir 'Terraria.exe') $srcFile -Force
  Write-Host "已写入补丁版 -> $exeName"
}

Copy-Item (Join-Path $tool 'ProjectionMod.dll') (Join-Path $game 'ProjectionMod.dll') -Force
Copy-Item (Join-Path $tool '0Harmony.dll') (Join-Path $game '0Harmony.dll') -Force
Write-Host '部署完成。启动游戏后查看游戏目录 ProjectionMod.log 验证心跳。'
