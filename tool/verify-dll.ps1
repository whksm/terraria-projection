# ============================================================================
# ProjectionMod.dll 部署前硬校验（Assembly name / 依赖版本 / 图标像素）
#
#   为什么需要：csc 的「程序集名」= /out: 指定的输出文件名。一旦输出成
#   build_obj.dll，程序集名就是 build_obj，游戏按 ProjectionMod 去加载会抛
#   System.IO.FileLoadException（0xe0434352 崩溃）。本脚本在编译后、复制前
#   用 Mono.Cecil 读回真正的程序集名，不对就 throw → 不复制、不部署。
#
#   用法（单独跑，也可以校验已经部署到游戏目录的 DLL）：
#     pwsh -NoProfile -File tool\verify-dll.ps1 -Path .\tool\build\obj-out\ProjectionMod.dll
#     pwsh -NoProfile -File tool\verify-dll.ps1 -Path 'D:\steam\steamapps\common\Terraria\ProjectionMod.dll'
#
#   失败时退出码 1；成功时打印全部校验项。
# ============================================================================
[CmdletBinding()]
param(
  [Parameter(Mandatory = $true)][string]$Path,
  [string]$IconPath,
  [string]$CecilPath,
  [string]$GameDir = 'D:\steam\steamapps\common\Terraria',
  [string]$ExpectHarmony,
  [string]$ExpectTerraria = '1.4.5.8'
)

$ErrorActionPreference = 'Stop'
$toolDir = $PSScriptRoot
$repoDir = Split-Path $toolDir -Parent
if (-not $IconPath)  { $IconPath  = Join-Path $repoDir 'assets\icon_32.ico' }
if (-not $CecilPath) { $CecilPath = Join-Path $toolDir 'Mono.Cecil.dll' }

$problems = New-Object System.Collections.Generic.List[string]
function Fail([string]$msg) { $script:problems.Add($msg) | Out-Null; Write-Host ("[校验][失败] " + $msg) -ForegroundColor Red }
function Ok([string]$msg)   { Write-Host ("[校验][通过] " + $msg) -ForegroundColor Green }
function Note([string]$msg) { Write-Host ("[校验]       " + $msg) }

if (-not (Test-Path -LiteralPath $Path)) { throw "找不到 DLL: $Path" }
if (-not (Test-Path -LiteralPath $CecilPath)) { throw "找不到 Mono.Cecil: $CecilPath" }
$file = Get-Item -LiteralPath $Path
$sha = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
Note ("文件: {0}" -f $file.FullName)
Note ("大小: {0:N0} 字节   SHA256: {1}" -f $file.Length, $sha)

# ---------------------------------------------------------------- 1) 程序集名
if (-not ('Mono.Cecil.AssemblyDefinition' -as [type])) { Add-Type -Path $CecilPath }
$asm = $null
try {
  $asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($Path)
} catch {
  Fail ("Mono.Cecil 无法读取该文件（不是合法托管程序集？）: " + $_.Exception.Message)
}
if ($asm) {
  $asmName = $asm.Name.Name
  $asmVer = $asm.Name.Version
  if ($asmName -eq 'ProjectionMod') {
    Ok ("程序集名 AssemblyDefinition.Name.Name = '{0}'（要求 ProjectionMod）" -f $asmName)
  } else {
    Fail ("程序集名 = '{0}'，不是 ProjectionMod —— 输出文件名错了，游戏会 FileLoadException，禁止部署" -f $asmName)
  }
  Note ("程序集版本: {0}" -f $asmVer)

  # ------------------------------------------------------------ 2) 依赖版本
  $refs = @{}
  foreach ($r in $asm.MainModule.AssemblyReferences) { $refs[$r.Name] = $r.Version }

  # 0Harmony：必须与游戏目录里的 0Harmony.dll 一致（运行时按版本绑）
  $gameHarmony = Join-Path $GameDir '0Harmony.dll'
  if (Test-Path -LiteralPath $gameHarmony) {
    $h = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($gameHarmony)
    $harmonyVer = $h.Name.Version
    $h.Dispose()
    $ExpectHarmony = $harmonyVer.ToString()
  }
  if (-not $ExpectHarmony) { $ExpectHarmony = '2.4.3.0' }

  if (-not $refs.ContainsKey('0Harmony')) {
    Fail "DLL 没有引用 0Harmony（Harmony 补丁不会生效）"
  } elseif ($refs['0Harmony'].ToString() -eq $ExpectHarmony) {
    Ok ("引用 0Harmony {0} == 游戏目录 0Harmony {1}" -f $refs['0Harmony'], $ExpectHarmony)
  } else {
    Fail ("引用 0Harmony {0} != 游戏目录 {1}（版本不一致会加载失败）" -f $refs['0Harmony'], $ExpectHarmony)
  }

  # Terraria：固定引用 1.4.5.8（运行在 1.4.6.1 上一直可用，保持）
  if (-not $refs.ContainsKey('Terraria')) {
    Fail "DLL 没有引用 Terraria"
  } elseif ($refs['Terraria'].ToString() -eq $ExpectTerraria) {
    $gameExe = Join-Path $GameDir 'Terraria.exe'
    $info = ''
    if (Test-Path -LiteralPath $gameExe) { $info = (Get-Item -LiteralPath $gameExe).VersionInfo.FileVersion }
    Ok ("引用 Terraria {0}（要求 {1}；游戏实际 {2}，属预期差异）" -f $refs['Terraria'], $ExpectTerraria, $info)
  } else {
    Fail ("引用 Terraria {0} != 期望 {1}（引用版本变了，需人工确认）" -f $refs['Terraria'], $ExpectTerraria)
  }

  # 记录 XNA 引用，方便对照
  $xna = ($refs.Keys | Where-Object { $_ -like 'Microsoft.Xna*' } | Sort-Object) -join ', '
  Note ("XNA 引用: {0}" -f $xna)
  $asm.Dispose()
}

# ---------------------------------------------------------------- 3) 图标像素
if (-not ('Win32.Res' -as [type])) {
  Add-Type -Namespace Win32 -Name Res -MemberDefinition @'
[DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
public static extern IntPtr LoadLibraryExW(string lpLibFileName, IntPtr hFile, uint dwFlags);
[DllImport("kernel32.dll", SetLastError=true)]
public static extern bool FreeLibrary(IntPtr hModule);
public delegate bool EnumResNameProc(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam);
[DllImport("kernel32.dll", SetLastError=true)]
public static extern bool EnumResourceNamesW(IntPtr hModule, IntPtr lpszType, EnumResNameProc lpEnumFunc, IntPtr lParam);
[DllImport("kernel32.dll", SetLastError=true)]
public static extern IntPtr FindResourceW(IntPtr hModule, IntPtr lpName, IntPtr lpType);
[DllImport("kernel32.dll", SetLastError=true)]
public static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);
[DllImport("kernel32.dll", SetLastError=true)]
public static extern IntPtr LockResource(IntPtr hResData);
[DllImport("kernel32.dll", SetLastError=true)]
public static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);
'@
}

function Get-IconBlobSha([string]$imagePath) {
  $mod = [Win32.Res]::LoadLibraryExW($imagePath, [IntPtr]::Zero, 0x22)  # AS_DATAFILE | AS_IMAGE_RESOURCE
  if ($mod -eq [IntPtr]::Zero) { throw "LoadLibraryEx 失败: $imagePath" }
  $script:iconBlobs = New-Object System.Collections.Generic.List[object]
  $cb = [Win32.Res+EnumResNameProc]{
    param($hm, $type, $name, $lp)
    $info = [Win32.Res]::FindResourceW($hm, $name, [IntPtr]3)   # RT_ICON
    if ($info -ne [IntPtr]::Zero) {
      $size = [int][Win32.Res]::SizeofResource($hm, $info)
      $ptr = [Win32.Res]::LockResource([Win32.Res]::LoadResource($hm, $info))
      $buf = New-Object byte[] $size
      [System.Runtime.InteropServices.Marshal]::Copy($ptr, $buf, 0, $size)
      $h = [System.Security.Cryptography.SHA256]::Create()
      $script:iconBlobs.Add([pscustomobject]@{
        Id   = [int]$name
        Size = $size
        Sha  = [BitConverter]::ToString($h.ComputeHash($buf)).Replace('-', '')
      }) | Out-Null
    }
    return $true
  }
  [void][Win32.Res]::EnumResourceNamesW($mod, [IntPtr]3, $cb, [IntPtr]::Zero)
  [void][Win32.Res]::FreeLibrary($mod)
  return @($script:iconBlobs | Sort-Object Id)
}

function Get-IcoFrameSha([string]$icoPath) {
  $bytes = [System.IO.File]::ReadAllBytes($icoPath)
  $count = [BitConverter]::ToUInt16($bytes, 4)
  $list = New-Object System.Collections.Generic.List[object]
  for ($i = 0; $i -lt $count; $i++) {
    $o = 6 + 16 * $i
    $len = [int][BitConverter]::ToUInt32($bytes, $o + 8)
    $off = [int][BitConverter]::ToUInt32($bytes, $o + 12)
    $h = [System.Security.Cryptography.SHA256]::Create()
    $list.Add([pscustomobject]@{
      Index = $i
      Size  = $len
      Sha   = [BitConverter]::ToString($h.ComputeHash($bytes[$off..($off + $len - 1)])).Replace('-', '')
    }) | Out-Null
  }
  return $list.ToArray()   # 注：PowerShell 7.7 里对 List[object] 用 @() 会抛 ArgumentException
}

if (Test-Path -LiteralPath $IconPath) {
  $icoFrames = Get-IcoFrameSha $IconPath
  $dllIcons = Get-IconBlobSha $Path
  $icoSet = @($icoFrames | ForEach-Object { $_.Sha } | Sort-Object)
  $dllSet = @($dllIcons | ForEach-Object { $_.Sha } | Sort-Object)
  $missing = @($icoSet | Where-Object { $dllSet -notcontains $_ })
  if ($dllIcons.Count -eq 0) {
    Fail "DLL 里没有 RT_ICON 资源（忘了 /win32icon:assets\icon_32.ico？）"
  } elseif ($missing.Count -eq 0 -and (@($dllSet | Where-Object { $icoSet -notcontains $_ }).Count -eq 0)) {
    Ok ("图标 {0} 帧像素 SHA256 全部命中 {1}（{2}）" -f $dllIcons.Count, (Split-Path $IconPath -Leaf), (($dllIcons | ForEach-Object { $_.Size }) -join '/'))
  } else {
    Fail ("图标像素与 {0} 不一致（缺 {1} 帧）" -f (Split-Path $IconPath -Leaf), $missing.Count)
  }
} else {
  Note ("图标源不存在，跳过图标校验: " + $IconPath)
}

# ---------------------------------------------------------------- 4) 结论
if ($problems.Count -gt 0) {
  Write-Host ""
  Write-Host ("[校验] 未通过，共 {0} 项问题 —— 禁止复制 / 禁止部署。" -f $problems.Count) -ForegroundColor Red
  throw ("ProjectionMod.dll 校验失败: " + ($problems -join ' | '))
}
Write-Host ""
Write-Host "[校验] 全部通过：程序集名 / 依赖版本 / 图标像素 均正确。" -ForegroundColor Green
