# 发布单文件安装器：installer\SetupApp\publish.ps1 [-OutDir <目录>]
# 产出：<OutDir>\TerrariaProjection_Setup.exe（默认 OutDir = 仓库 dist\）
param(
    [string]$OutDir = ""
)

$ErrorActionPreference = 'Stop'

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = (Resolve-Path (Join-Path $here "..\..")).Path
if ([string]::IsNullOrWhiteSpace($OutDir)) { $OutDir = Join-Path $repo "dist" }

$stage = Join-Path $here "bin\publish"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }

Write-Host "[1/3] dotnet publish ..."
dotnet publish $here -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $stage
if ($LASTEXITCODE -ne 0) { throw "dotnet publish 失败（退出码 $LASTEXITCODE）" }

$exe = Join-Path $stage "Setup.exe"
if (-not (Test-Path $exe)) { throw "没有产出 Setup.exe: $exe" }

$extra = Get-ChildItem $stage -File | Where-Object { $_.Name -ne "Setup.exe" }
if ($extra) {
    Write-Warning ("发布目录里还有额外文件（可能不是单文件）: " + (($extra | ForEach-Object Name) -join ", "))
}

Write-Host "[2/3] 复制到 $OutDir ..."
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$dest = Join-Path $OutDir "TerrariaProjection_Setup.exe"
Copy-Item $exe $dest -Force

Write-Host "[3/3] 完成"
$fi = Get-Item $dest
Write-Host ("  {0}" -f $fi.FullName)
Write-Host ("  {0:N0} 字节 ({1:N2} MB)  版本 {2}" -f $fi.Length, ($fi.Length / 1MB), $fi.VersionInfo.FileVersion)
