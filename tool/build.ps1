# ============================================================================
# ProjectionMod.dll 一键构建（csc / .NET Framework 4.0 / x86）+ 硬校验 + 同步 + 可选部署
#
#   事故教训（2026-11-25）：csc 的「程序集名」由 /out: 的输出文件名决定。
#   上一版输出成 build_obj.dll，程序集名就成了 build_obj，游戏按 ProjectionMod
#   加载失败 → FileLoadException → 0xe0434352 崩溃。所以本脚本：
#     1) 固定 /out:ProjectionMod.dll（输出到临时目录的对象名就叫 ProjectionMod.dll）；
#     2) 编完立刻调 tool\verify-dll.ps1 用 Mono.Cecil 读回程序集名，
#        不叫 ProjectionMod 就 throw → 不复制、不部署；
#     3) 顺带校验 0Harmony / Terraria 引用版本、图标像素。
#
#   用法：
#     pwsh -NoProfile -File tool\build.ps1              # 只构建 + 校验 + 同步 tool/ release/
#     pwsh -NoProfile -File tool\build.ps1 -Deploy      # 再备份并部署到游戏目录
# ============================================================================
[CmdletBinding()]
param(
  [switch]$Deploy,
  [string]$GameDir = 'D:\steam\steamapps\common\Terraria',
  [string]$BackupTag = '构建'      # 备份名后缀：ProjectionMod.dll.bak-<BackupTag>-<HHmmss>
)

$ErrorActionPreference = 'Stop'
$tool   = $PSScriptRoot
$repo   = Split-Path $tool -Parent
$srcDir = Join-Path $tool 'src'
$icon   = Join-Path $repo 'assets\icon_32.ico'
$refTerraria = Join-Path $tool 'build\Terraria.exe'   # 1.4.5.8 参考程序集（不是游戏目录那份 1.4.6.1）
$harmony      = Join-Path $tool '0Harmony.dll'
$xnaDir       = Join-Path $tool 'xna'
$outDir  = Join-Path $tool 'build\obj-out'
$outDll  = Join-Path $outDir 'ProjectionMod.dll'
$verify  = Join-Path $tool 'verify-dll.ps1'

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $csc)) { throw "找不到 csc.exe: $csc" }
if (-not (Test-Path -LiteralPath $verify)) { throw "找不到校验脚本: $verify" }
if ((Split-Path $outDll -Leaf) -ne 'ProjectionMod.dll') { throw "输出文件名必须是 ProjectionMod.dll（程序集名由它决定）" }

# 参考 Terraria 必须是 1.4.5.8，否则程序集引用版本会变
$refVer = (Get-Item -LiteralPath $refTerraria).VersionInfo.ProductVersion
if ($refVer -ne '1.4.5.8') { throw "参考 Terraria.exe 版本是 $refVer，要求 1.4.5.8: $refTerraria" }

# 源文件：tool/src/*.cs 去掉独立入口 Patcher.cs（15 个）
$sources = @(Get-ChildItem -LiteralPath $srcDir -Filter '*.cs' | Where-Object { $_.Name -ne 'Patcher.cs' } | Sort-Object Name)
if ($sources.Count -lt 15) { throw ("源文件只有 {0} 个，期望 15 个（不含 Patcher.cs）" -f $sources.Count) }

if (Test-Path -LiteralPath $outDir) { Remove-Item -LiteralPath $outDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# ---------------------------------------------------------------- 1) 编译
$cscArgs = @(
  '/nologo', '/target:library', '/platform:x86', '/optimize+', '/debug-', '/langversion:5',
  "/win32icon:$icon",
  "/out:$outDll",                       # ★ 程序集名来源，固定 ProjectionMod.dll
  "/reference:$refTerraria",
  "/reference:$harmony"
)
foreach ($x in (Get-ChildItem -LiteralPath $xnaDir -Filter '*.dll' | Sort-Object Name)) {
  $cscArgs += "/reference:$($x.FullName)"
}
foreach ($s in $sources) { $cscArgs += $s.FullName }

Write-Host ("[构建] csc {0}" -f ($cscArgs -join ' ')) -ForegroundColor DarkGray
$cscLog = & $csc @cscArgs 2>&1
$code = $LASTEXITCODE
$cscLog | ForEach-Object { Write-Host $_ }

$errCount  = @($cscLog | Where-Object { "$_" -match 'error CS\d+' }).Count
$warnCount = @($cscLog | Where-Object { "$_" -match 'warning CS\d+' }).Count
if ($code -ne 0 -or $errCount -gt 0) {
  throw ("编译失败：exit={0} error={1} warning={2}" -f $code, $errCount, $warnCount)
}
if ($warnCount -gt 0) {
  throw ("编译有 {0} 个 warning，要求 0 warning" -f $warnCount)
}
Write-Host ("[构建] 编译成功：0 error / 0 warning，{0} 个源文件 -> {1}" -f $sources.Count, $outDll) -ForegroundColor Green

# ------------------------------------------------- 2) 硬校验（程序集名等）
# verify-dll.ps1 内部只要有一项不对就 throw，后面的复制/部署都不会执行
& $verify -Path $outDll -GameDir $GameDir -IconPath $icon

# ---------------------------------------------------------------- 3) 同步
$newSize = (Get-Item -LiteralPath $outDll).Length
$newSha  = (Get-FileHash -LiteralPath $outDll -Algorithm SHA256).Hash

$copies = @(
  (Join-Path $tool 'ProjectionMod.dll'),
  (Join-Path $repo 'release\ProjectionMod.dll')
)
foreach ($c in $copies) { Copy-Item -LiteralPath $outDll -Destination $c -Force }

$hashes = @{}
foreach ($p in (@($outDll) + $copies)) {
  $hashes[$p] = (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash
}
$distinct = @($hashes.Values | Sort-Object -Unique)
if ($distinct.Count -ne 1) { throw "复制后 SHA256 不一致: $($hashes.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" })" }
Write-Host ("[同步] tool / release 已更新，SHA256 = {0}" -f $newSha) -ForegroundColor Green

# ---------------------------------------------------------------- 4) 部署
if ($Deploy) {
  if (Get-Process -Name Terraria -ErrorAction SilentlyContinue) {
    throw "游戏正在运行，先退出 Terraria 再部署（DLL 被占用）"
  }
  $target = Join-Path $GameDir 'ProjectionMod.dll'
  if (-not (Test-Path -LiteralPath $GameDir)) { throw "游戏目录不存在: $GameDir" }
  $stamp  = (Get-Date).ToString('HHmmss')
  $backup = "$target.bak-$BackupTag-$stamp"
  if (Test-Path -LiteralPath $target) {
    Copy-Item -LiteralPath $target -Destination $backup -Force
    Write-Host ("[部署] 已备份旧 DLL -> {0}（{1:N0} 字节）" -f $backup, (Get-Item -LiteralPath $backup).Length) -ForegroundColor Yellow
  }
  Copy-Item -LiteralPath $outDll -Destination $target -Force

  # 部署后再校验一次落盘的那份，并核对三处 SHA256
  & $verify -Path $target -GameDir $GameDir -IconPath $icon
  $gameSha = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
  if ($gameSha -ne $newSha) { throw ("部署后 SHA256 不一致: game={0} build={1}" -f $gameSha, $newSha) }

  if (-not ('Mono.Cecil.AssemblyDefinition' -as [type])) { Add-Type -Path (Join-Path $tool 'Mono.Cecil.dll') }
  $a = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($target)
  $asmLine = $a.Name.Name
  $a.Dispose()

  Write-Host ""
  Write-Host "[部署] 完成" -ForegroundColor Green
  Write-Host ("  文件     : {0}" -f $target)
  Write-Host ("  大小     : {0:N0} 字节" -f (Get-Item -LiteralPath $target).Length)
  Write-Host ("  SHA256   : {0}" -f $gameSha)
  Write-Host ("  程序集名 : {0}" -f $asmLine)
  Write-Host ("  备份     : {0}" -f $backup)
  Write-Host ("  回滚命令 : Copy-Item '{0}' '{1}' -Force" -f $backup, $target)
}
