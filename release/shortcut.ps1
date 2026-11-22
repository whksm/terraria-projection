param([string]$gameDir)
$ErrorActionPreference = 'Stop'
try {
    if ([string]::IsNullOrWhiteSpace($gameDir)) { throw "缺少游戏目录参数" }
    $exe = Join-Path $gameDir 'Terraria.exe'
    if (-not (Test-Path $exe)) { throw "找不到 $exe" }

    $ico = Join-Path $gameDir 'ProjectionMod.ico'
    $desk = [Environment]::GetFolderPath('Desktop')
    $lnk = Join-Path $desk '泰拉瑞亚投影MOD.lnk'

    $sh = New-Object -ComObject WScript.Shell
    $s = $sh.CreateShortcut($lnk)
    $s.TargetPath = $exe
    $s.WorkingDirectory = $gameDir
    if (Test-Path $ico) { $s.IconLocation = $ico }
    $s.Description = '泰拉瑞亚投影MOD（原版 1.4.5.8）'
    $s.Save()

    Write-Host ("      已创建快捷方式: " + $lnk)
    exit 0
}
catch {
    Write-Host ("      快捷方式创建失败: " + $_.Exception.Message)
    exit 1
}
