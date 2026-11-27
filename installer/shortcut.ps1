param([string]$gameDir)
$ErrorActionPreference = 'Stop'
try {
    if ([string]::IsNullOrWhiteSpace($gameDir)) { throw "缺少游戏目录参数" }
    $exe = Join-Path $gameDir 'Terraria.exe'
    if (-not (Test-Path $exe)) { throw "找不到 $exe" }

    $ico = Join-Path $gameDir 'ProjectionMod.ico'
    $desk = [Environment]::GetFolderPath('Desktop')

    # 快捷方式名必须和安装器 Installer.DefaultShortcutBaseName 一致（文档里也写这个名字）。
    # 桌面上如果同时有同名文件夹，点这个名字会变成"打开文件夹"而不是启动游戏，所以撞名就换一个。
    $base = '启动泰拉瑞亚投影MOD'
    $lnk = Join-Path $desk ($base + '.lnk')
    if (Test-Path (Join-Path $desk $base)) {
        for ($i = 2; $i -le 20; $i++) {
            $name = $base + ' (' + $i + ')'
            if (-not (Test-Path (Join-Path $desk $name))) { $lnk = Join-Path $desk ($name + '.lnk'); break }
        }
    }

    $sh = New-Object -ComObject WScript.Shell
    $s = $sh.CreateShortcut($lnk)
    $s.TargetPath = $exe
    $s.WorkingDirectory = $gameDir
    if (Test-Path $ico) { $s.IconLocation = $ico }
    $s.Description = '泰拉瑞亚投影MOD（投影插件）'
    $s.Save()

    Write-Host ("      已创建快捷方式: " + $lnk)
    exit 0
}
catch {
    Write-Host ("      快捷方式创建失败: " + $_.Exception.Message)
    exit 1
}
