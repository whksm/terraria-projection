# gen-names.ps1 —— 把 data/tile-names.json 压成最小字段，注入 editor/blueprint-editor.html
# 只取 id / name_zh / name_en / primary 四个字段，压成一行字符串，保持单文件、零外链、零构建。
# 用法：pwsh -File editor\_selftest\gen-names.ps1
# 注意：重跑后 HTML 里只有 @tile-names-begin / @tile-names-end 之间的内容会变，其余原样保留。

$ErrorActionPreference = 'Stop'
$self = $PSScriptRoot
$root = Split-Path -Parent (Split-Path -Parent $self)
$jsonPath = Join-Path $root 'data\tile-names.json'
$editorPath = Join-Path (Split-Path -Parent $self) 'blueprint-editor.html'

foreach ($p in @($jsonPath, $editorPath)) {
  if (-not (Test-Path $p)) { throw "找不到文件：$p" }
}

Write-Host '== 读取 =='
$rows = Get-Content $jsonPath -Raw -Encoding UTF8 | ConvertFrom-Json
Write-Host ("tile-names.json  {0} 条" -f @($rows).Count)
$html = [IO.File]::ReadAllText($editorPath, [Text.Encoding]::UTF8)
Write-Host ("editor           {0:N0} 字节" -f $html.Length)

# ---------------------------------------------------------------- 1. 压成 id|zh|en|primary;
$sb = New-Object System.Text.StringBuilder
$n = 0
foreach ($r in (@($rows) | Sort-Object { [int]$_.id })) {
  $id = [int]$r.id
  $zh = ([string]$r.name_zh).Trim()
  $en = ([string]$r.name_en).Trim()
  $prim = 0
  if ($null -ne $r.primary -and "$($r.primary)" -ne '') { $prim = [int]$r.primary }
  if ("$zh$en" -match '[|;]') { throw ("名称里出现了分隔符（{0}）：{1} / {2}" -f $id, $zh, $en) }
  # 单引号字符串里只需要转义反斜杠和单引号
  $zh = $zh.Replace('\', '\\').Replace("'", "\'")
  $en = $en.Replace('\', '\\').Replace("'", "\'")
  [void]$sb.Append(("{0}|{1}|{2}|{3};" -f $id, $zh, $en, $prim))
  $n++
}
$payload = $sb.ToString().TrimEnd(';')
if (-not $payload) { throw '压出来的名称表是空的' }
Write-Host ("压缩后           {0} 条  {1:N0} 字节（UTF-8 前）" -f $n, $payload.Length)
Write-Host ("                 前 120 字符：{0}" -f $payload.Substring(0, [Math]::Min(120, $payload.Length)))

# ---------------------------------------------------------------- 2. 替换标记之间的内容
$startMark = '/* @tile-names-begin */'
$endMark = '/* @tile-names-end */'
$i = $html.IndexOf($startMark, [StringComparison]::Ordinal)
if ($i -lt 0) { throw '找不到 @tile-names-begin 标记' }
$j = $html.IndexOf($endMark, $i, [StringComparison]::Ordinal)
if ($j -lt 0) { throw '找不到 @tile-names-end 标记' }

$block = $startMark + "`nconst TILE_NAME_DATA = '" + $payload + "';`n" + $endMark
$new = $html.Substring(0, $i) + $block + $html.Substring($j + $endMark.Length)

if ($new -eq $html) { Write-Host '内容没有变化，未写回' }
else {
  [IO.File]::WriteAllText($editorPath, $new, (New-Object Text.UTF8Encoding($false)))
  Write-Host ("已写回           {0}" -f $editorPath)
  Write-Host ("editor 现在      {0:N0} 字节（+{1:N0}）" -f $new.Length, ($new.Length - $html.Length))
}
