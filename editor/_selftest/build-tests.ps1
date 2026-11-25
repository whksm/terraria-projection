# build-tests.ps1 —— 生成自测用的测试数据 / 测试页，并用无头 Chrome 跑验证与截图
# 只读写 editor/ 目录内的文件（release/ProjectionMod.bp 只读）。
# 用法：pwsh -File editor\_selftest\build-tests.ps1

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)   # ...\terraria-projection
$self = $PSScriptRoot
$fixDir = Join-Path $self 'fixtures'
$shotDir = Join-Path $self 'shots'
New-Item -ItemType Directory -Force -Path $fixDir, $shotDir | Out-Null

$editorPath = Join-Path (Split-Path -Parent $self) 'blueprint-editor.html'
$releasePath = Join-Path $root 'release\ProjectionMod.bp'
$chrome = 'C:\Program Files\Google\Chrome\Application\chrome.exe'

Write-Host '== 读取 =='
$html = [IO.File]::ReadAllText($editorPath, [Text.Encoding]::UTF8)
$releaseText = [IO.File]::ReadAllText($releasePath, [Text.Encoding]::UTF8)
Write-Host ("editor   {0} 字节" -f $html.Length)
Write-Host ("release  {0} 字节" -f $releaseText.Length)

# ---------------------------------------------------------------- 1. 大测试蓝图
$script:cells = [ordered]@{}
function AddCell {
  $line = $args -join ','
  $k = "$($args[0]),$($args[1])"
  if (-not $script:cells.Contains($k)) { $script:cells[$k] = $line }
}

# (1) 斜坡演示：4 块 3x3，分别 slope 1..4
foreach ($i in 0..3) {
  $sx = 2 + $i * 4
  foreach ($dy in 0..2) { foreach ($dx in 0..2) {
    AddCell ($sx + $dx) (2 + $dy) 38 0 -1 -1 0 0 ($i + 1) 0 0 0
  } }
}
# (2) 半砖（下面留空，便于一眼看出只有下半块）
foreach ($x in 2..9) { AddCell $x 6 38 0 -1 -1 0 0 0 1 0 0 }
# 整块参照行（放在 y=8）
foreach ($x in 2..9) { AddCell $x 8 38 0 -1 -1 0 0 0 0 0 0 }
# (3) 四色电线横线 + 竖总线（每格 4 色）
foreach ($b in 0..3) {
  $row = 9 + $b
  $bit = [int][math]::Pow(2, $b)
  foreach ($x in 2..24) { AddCell $x $row 30 0 -1 -1 $bit 0 0 0 0 0 }
}
foreach ($y in 9..12) { AddCell 25 $y 30 0 -1 -1 15 0 0 0 0 0 }
# (4) 促动器阵列
foreach ($y in 14..19) { foreach ($x in 2..9) { AddCell $x $y 45 0 -1 -1 0 1 0 0 0 0 } }
# (5) 油漆 1..13
for ($i = 1; $i -le 13; $i++) { AddCell (11 + $i) 14 45 0 -1 -1 0 0 0 0 $i 0 }
# (6) 墙：带方块的墙 + 纯墙格
foreach ($y in 14..22) {
  foreach ($x in 27..36) { AddCell $x $y 30 1 -1 -1 0 0 0 0 0 0 }
  foreach ($x in 38..46) { AddCell $x $y 0 5 -1 -1 0 0 0 0 0 0 }
}
# (7) 混装格：方块的墙 + 双色线 + 促动器 + 方块漆 + 墙漆
foreach ($x in 2..22) { AddCell $x 24 38 4 -1 -1 3 1 0 0 4 2 }
# (8) 建筑：地板 + 天花 + 平台 + 立柱
foreach ($x in 2..40) {
  AddCell $x 30 30 0 -1 -1 0 0 0 0 0 0
  AddCell $x 44 30 0 -1 -1 0 0 0 0 0 0
  AddCell $x 37 19 0 -1 -1 0 0 0 0 0 0
}
foreach ($y in 31..36) { AddCell 2 $y 30 0 -1 -1 0 0 0 0 0 0; AddCell 40 $y 30 0 -1 -1 0 0 0 0 0 0 }
# (9) 带帧的家具（frameX/frameY 非 -1）
AddCell 50 24 18 0 0 0 0 0 0 0 0 0
AddCell 51 24 18 0 16 0 0 0 0 0 0 0
AddCell 52 24 21 0 0 0 0 0 0 0 0 0
# (10) 大片地形：地表 + 地下，把蓝图撑到 120x80
foreach ($x in 0..119) {
  AddCell $x 60 1 0 -1 -1 0 0 0 0 0 0
  AddCell $x 61 1 0 -1 -1 0 0 0 0 0 0
  AddCell $x 79 1 0 -1 -1 0 0 0 0 0 0
}
foreach ($y in 62..77) { foreach ($x in 0..119) { AddCell $x $y 1 0 -1 -1 0 0 0 0 0 0 } }
# 地表起伏（斜坡 + 半砖）
foreach ($i in 0..19) {
  $x = 10 + $i * 5
  AddCell $x 59 1 0 -1 -1 0 0 ($(if ($i % 2 -eq 0) { 1 } else { 2 })) 0 0 0
  AddCell ($x + 1) 59 1 0 -1 -1 0 0 0 ($(if ($i % 3 -eq 0) { 1 } else { 0 })) 0 0
}
# 地下石室 + 墙
foreach ($y in 66..73) { foreach ($x in 60..100) { AddCell $x $y 38 1 -1 -1 0 0 0 0 0 0 } }
# 电线主干
foreach ($x in 0..119) { AddCell $x 78 1 0 -1 -1 4 0 0 0 0 0 }
# 右侧边界与右下角
foreach ($y in 0..79) { AddCell 119 $y 1 0 -1 -1 0 0 0 0 0 0 }

$stressLines = ($script:cells.Values | ForEach-Object { $_ }) -join "`n"
$stressText = $stressLines + "`n"
$stressPath = Join-Path $fixDir 'stress-120x80.bp'
[IO.File]::WriteAllText($stressPath, $stressText, (New-Object Text.UTF8Encoding($false)))
Write-Host ("stress   {0} 行" -f $script:cells.Count)

# ---------------------------------------------------------------- 2. 混合列数 / 坏行
$mixedText = @'
# 混合列数 + 坏行测试文件
# 4 列 / 6 列 / 12 列 / 各种坏行

0,0,1,0
1,0,1,21
0,0,1,0
2,0,18,0,0,0
3,0,19,0,0,0,1,0,0,0,0,0
这一行是垃圾数据
4,0,abc,0
5,0,70000,0
6,0
7,0,1,0,0
8,0,1,0,0,0,1,1,2,1,3,4
9,0,1,0,-5,-7,15,1,0,0,0,0
'@
$mixedPath = Join-Path $fixDir 'mixed.bp'
[IO.File]::WriteAllText($mixedPath, $mixedText, (New-Object Text.UTF8Encoding($false)))
Write-Host 'mixed    已生成'

# ---------------------------------------------------------------- 3. 核心代码提取
$m = [regex]::Match($html, '(?s)/\* @core-begin \*/(.*?)/\* @core-end \*/')
if (-not $m.Success) { throw '找不到 @core-begin / @core-end 标记' }
$core = $m.Groups[1].Value
Write-Host ("core     {0} 字节" -f $core.Length)

$relJson = ConvertTo-Json $releaseText -Compress
$stressJson = ConvertTo-Json $stressText -Compress
$mixedJson = ConvertTo-Json $mixedText -Compress

# ---------------------------------------------------------------- 4. 逻辑测试页
$testJs = @'
const out = []; let fail = 0;
function hdr(t) { out.push(''); out.push('=== ' + t + ' ==='); }
function ok(n, c, extra) { out.push((c ? '  PASS  ' : '  FAIL  ') + n + (extra ? '   [' + extra + ']' : '')); if (!c) fail++; }

/* ---- A. release/ProjectionMod.bp：读入 → 保存 → 逐行比对 ---- */
hdr('A  release/ProjectionMod.bp  读入→保存 逐行比对');
const pa = parseBlueprint(FIX.release);
const origA = FIX.release.split(/\r?\n/).map(s => s.trim()).filter(s => s && s[0] !== '#');
ok('解析行数 = 数据行数(' + origA.length + ')', pa.cells.size === origA.length, 'cells=' + pa.cells.size + ' bad=' + pa.bad + ' dup=' + pa.dup);
ok('没有坏行', pa.bad === 0, 'bad=' + pa.bad);
ok('尺寸 5x5', pa.width === 5 && pa.height === 5, pa.width + 'x' + pa.height);
const outA = serializeCells(pa.cells).split('\n').filter(Boolean);
ok('保存行数 = 原始数据行数', outA.length === origA.length, origA.length + ' vs ' + outA.length);
const DEF = [null, null, null, null, '-1', '-1', '0', '0', '0', '0', '0', '0'];
let diff = 0, filled = 0, badCols = 0;
for (let i = 0; i < Math.min(origA.length, outA.length); i++) {
  const a = origA[i].split(','), b = outA[i].split(',');
  if (b.length !== 12) { badCols++; continue; }
  for (let j = 0; j < a.length; j++) if (String(a[j]).trim() !== b[j]) diff++;
  for (let j = a.length; j < 12; j++) { if (b[j] === DEF[j]) filled++; else diff++; }
}
ok('每行 12 列', badCols === 0, 'badCols=' + badCols);
ok('已有字段逐字节相同，差值 = 0', diff === 0, 'diff=' + diff);
out.push('  仅做默认值补齐：' + filled + ' 个字段（frameX/Y=-1, wire/actuator/slope/halfBrick/paint/wallPaint=0）');
out.push('  首行: ' + origA[0] + '  →  ' + outA[0]);

/* ---- B. 120x80 大蓝图：逐字节往返 ---- */
hdr('B  stress-120x80.bp  读入→保存 逐字节比对');
const pb = parseBlueprint(FIX.stress);
ok('无坏行', pb.bad === 0, 'bad=' + pb.bad);
ok('源文件全部是 12 列', FIX.stress.split('\n').filter(Boolean).every(l => l.split(',').length === 12));
ok('尺寸 120x80', pb.width === 120 && pb.height === 80, pb.width + 'x' + pb.height + ' cells=' + pb.cells.size);
const sb = serializeCells(pb.cells);
const norm = t => t.replace(/\r\n/g, '\n').replace(/\n+$/, '');
ok('序列化结果与原文件逐字节相同', norm(sb) === norm(FIX.stress),
   'len ' + norm(sb).length + ' vs ' + norm(FIX.stress).length);
if (norm(sb) !== norm(FIX.stress)) {
  const a = norm(FIX.stress).split('\n'), c = norm(sb).split('\n');
  for (let i = 0; i < Math.max(a.length, c.length); i++) {
    if (a[i] !== c[i]) { out.push('  首个差异行 #' + (i + 1)); out.push('   orig: ' + a[i]); out.push('   ours: ' + c[i]); break; }
  }
}
// 特性覆盖统计
let s1 = 0, s2 = 0, s3 = 0, s4 = 0, hb = 0, act = 0, w4 = 0, wAny = 0, paint = 0, wpaint = 0, pureWall = 0, frames = 0;
for (const c of pb.cells.values()) {
  if (c.slope === 1) s1++; else if (c.slope === 2) s2++; else if (c.slope === 3) s3++; else if (c.slope === 4) s4++;
  if (c.halfBrick) hb++;
  if (c.actuator) act++;
  if ((c.wire & 15) === 15) w4++;
  if (c.wire & 15) wAny++;
  if (c.paint) paint++;
  if (c.wallPaint) wpaint++;
  if (!c.type && c.wall) pureWall++;
  if (c.frameX !== -1 || c.frameY !== -1) frames++;
}
ok('覆盖 slope 1/2/3/4', s1 && s2 && s3 && s4, 'slope1=' + s1 + ' slope2=' + s2 + ' slope3=' + s3 + ' slope4=' + s4);
ok('覆盖半砖', hb > 0, 'halfBrick=' + hb);
ok('覆盖促动器', act > 0, 'actuator=' + act);
ok('覆盖 4 色电线（含 15）', w4 > 0, 'wire=15 的格子 ' + w4 + '，任一线 ' + wAny);
ok('覆盖方块漆 + 墙漆', paint > 0 && wpaint > 0, 'paint=' + paint + ' wallPaint=' + wpaint);
ok('覆盖纯墙格', pureWall > 0, 'pureWall=' + pureWall);
ok('覆盖非 -1 帧', frames > 0, 'frames=' + frames);

/* ---- C. 混合列数 / 坏行 ---- */
hdr('C  mixed.bp  4/6/12 列 + 坏行');
const pc = parseBlueprint(FIX.mixed);
ok('有效格子 7 个', pc.cells.size === 7, 'cells=' + pc.cells.size);
ok('坏行 4 行被跳过', pc.bad === 4, 'bad=' + pc.bad);
ok('重复坐标 1 行被忽略', pc.dup === 1, 'dup=' + pc.dup);
const c20 = pc.cells.get('2,0');
ok('6 列行读到 frameX/frameY', c20 && c20.frameX === 0 && c20.frameY === 0, c20 ? c20.frameX + ',' + c20.frameY : 'missing');
const c70 = pc.cells.get('7,0');
ok('5 列行按 4 列处理，帧 = -1', c70 && c70.frameX === -1 && c70.frameY === -1, c70 ? c70.frameX + ',' + c70.frameY : 'missing');
const c80 = pc.cells.get('8,0');
ok('12 列行读到全部字段', c80 && c80.wire === 1 && c80.actuator === 1 && c80.slope === 2 && c80.halfBrick === 1 && c80.paint === 3 && c80.wallPaint === 4,
   c80 ? [c80.wire, c80.actuator, c80.slope, c80.halfBrick, c80.paint, c80.wallPaint].join('/') : 'missing');
const c90 = pc.cells.get('9,0');
ok('负 frameX/frameY 保留', c90 && c90.frameX === -5 && c90.frameY === -7, c90 ? c90.frameX + ',' + c90.frameY : 'missing');
const expectC = [
  '0,0,1,0,-1,-1,0,0,0,0,0,0',
  '1,0,1,21,-1,-1,0,0,0,0,0,0',
  '2,0,18,0,0,0,0,0,0,0,0,0',
  '3,0,19,0,0,0,1,0,0,0,0,0',
  '7,0,1,0,-1,-1,0,0,0,0,0,0',
  '8,0,1,0,0,0,1,1,2,1,3,4',
  '9,0,1,0,-5,-7,15,1,0,0,0,0'
].join('\n');
ok('保存结果完全符合预期', norm(serializeCells(pc.cells)) === expectC);
if (norm(serializeCells(pc.cells)) !== expectC) {
  out.push('  --- got ---'); out.push(norm(serializeCells(pc.cells)));
  out.push('  --- want ---'); out.push(expectC);
}
out.push('  覆盖: ' + pa.bad + '/' + pa.dup + ' | ' + pc.bad + '/' + pc.dup + '  (bad/dup)');

/* ---- D. 模拟游戏端的严格读取（int/ushort/short/byte） ---- */
hdr('D  模拟游戏端逐列解析（int/ushort/short/byte 取值范围）');
function gameRead(text) {
  const bad = [];
  let n = 0;
  for (const raw of text.split(/\r?\n/)) {
    const line = raw.trim();
    if (!line || line[0] === '#') continue;
    const p = line.split(',');
    if (p.length < 4) { bad.push(['列数不足', line]); continue; }
    const rng = (s, lo, hi) => {
      const t = s.trim();
      if (!/^[+-]?[0-9]+$/.test(t)) return false;
      const v = Number(t);
      return Number.isSafeInteger(v) && v >= lo && v <= hi;
    };
    if (!rng(p[0], -2147483648, 2147483647) || !rng(p[1], -2147483648, 2147483647) ||
        !rng(p[2], 0, 65535) || !rng(p[3], 0, 65535)) { bad.push(['int/ushort 越界', line]); continue; }
    if (p.length >= 6 && (!rng(p[4], -32768, 32767) || !rng(p[5], -32768, 32767))) { bad.push(['short 越界', line]); continue; }
    if (p.length >= 12) {
      for (const i of [6, 8, 10, 11]) {
        if (!rng(p[i], 0, 255)) { bad.push(['byte 越界 col' + i, line]); continue; }
      }
    }
    n++;
  }
  return { n, bad };
}
const dA = gameRead(outA.join('\n'));
const dB = gameRead(sb);
ok('release 输出全部可被游戏端解析', dA.bad.length === 0, dA.n + ' 行, 失败 ' + dA.bad.length);
ok('stress 输出全部可被游戏端解析', dB.bad.length === 0, dB.n + ' 行, 失败 ' + dB.bad.length);
ok('stress 输出 12 列', sb.split('\n').filter(Boolean).every(l => l.split(',').length === 12));

/* ---- E. 幂等 ---- */
hdr('E  保存→读入→再保存 幂等');
const sb2 = serializeCells(parseBlueprint(sb).cells);
ok('第二次保存与第一次相同', norm(sb2) === norm(sb));
const sa2 = serializeCells(parseBlueprint(outA.join('\n')).cells);
ok('release 二次保存稳定', norm(sa2) === norm(outA.join('\n')));

/* ---- F. 映射表 ---- */
hdr('F  映射表 预览/应用');
const mp = parseMappingText('1=477\n38=39');
ok('解析 2 条映射', mp.mappings.length === 2 && mp.mappings[0].from === 1 && mp.mappings[0].to === 477);
const hit = collectMappingHits(pb.cells, mp.mappings);
let manual = 0;
for (const c of pb.cells.values()) if (c.type === 1 || c.type === 38) manual++;
ok('预览命中数与遍历统计一致', hit.count === manual, 'hits=' + hit.count + ' manual=' + manual);
// 真正应用（模拟 UI 的 apply）
const copy = new Map();
for (const [k, c] of pb.cells) copy.set(k, cloneCell(c));
let before1 = 0, before38 = 0;
for (const c of copy.values()) { if (c.type === 1) before1++; else if (c.type === 38) before38++; }
const fromTo = new Map([[1, 477], [38, 39]]);
let changed = 0;
for (const [k, c] of copy) { if (fromTo.has(c.type)) { c.type = fromTo.get(c.type); changed++; } }
ok('应用后替换格数 = 预览数', changed === hit.count, changed + ' vs ' + hit.count);
let after477 = 0, after39 = 0;
for (const c of copy.values()) { if (c.type === 477) after477++; else if (c.type === 39) after39++; }
ok('1 → 477 数量正确', after477 === before1, '477=' + after477 + ' 原 type1=' + before1);
ok('38 → 39 数量正确', after39 === before38, '39=' + after39 + ' 原 type38=' + before38);
ok('未命中的格子一个都没动', pb.cells.size === copy.size);

/* ---- G. 边界情况 ---- */
hdr('G  边界情况');
ok('空文本 → 0 格', parseBlueprint('').cells.size === 0);
ok('只有注释 → 0 格', parseBlueprint('# a\n# b\n\n').cells.size === 0);
ok('CRLF 换行正常', parseBlueprint('0,0,1,0\r\n1,0,2,0\r\n').cells.size === 2);
ok('BOM 正常', parseBlueprint('\uFEFF0,0,1,0\n').cells.size === 1);
const sp = parseBlueprint(' 1 , 2 , 3 , 4 \n');
ok('字段两端空格正常', sp.cells.get('1,2') && sp.cells.get('1,2').type === 3);
ok('type 65536 越界被跳过', parseBlueprint('0,0,65536,0\n').bad === 1);
ok('type -1 被跳过', parseBlueprint('0,0,-1,0\n').bad === 1);
ok('"1.0" 不是合法整数', parseBlueprint('0,0,1.0,0\n').bad === 1);
const ac = parseBlueprint('0,0,1,0,0,0,0,abc,0,0,0,0');
ok('促动器列非 0 即真（同 C#）', ac.bad === 0 && ac.cells.get('0,0').actuator === 1);
ok('空文件序列化为空串', serializeCells(new Map()) === '');
const emptyCellTest = parseBlueprint('0,0,0,0\n').cells.get('0,0');
ok('type=0/wall=0 的格子仍保留（行数据不丢）', !!emptyCellTest && emptyCellTest.type === 0);

/* ---- H. 一键替换（纯逻辑） ---- */
hdr('H  一键替换 collectReplaceHits / applyReplaceInPlace');
let n1 = 0;
for (const c of pb.cells.values()) if (c.type === 1) n1++;
const hitsR = collectReplaceHits(pb.cells, 1, 477, null);
ok('命中数 = type 1 的格子数', hitsR.length === n1, hitsR.length + ' vs ' + n1);
ok('命中的确实都是 type 1', hitsR.every(k => pb.cells.get(k).type === 1));
ok('源 == 目标 → 0 命中', collectReplaceHits(pb.cells, 1, 1, null).length === 0);
ok('不存在的源 → 0 命中', collectReplaceHits(pb.cells, 60000, 477, null).length === 0);
ok('源/目标非数字 → 0 命中',
   collectReplaceHits(pb.cells, NaN, 477, null).length === 0 &&
   collectReplaceHits(pb.cells, 1, null, null).length === 0);

const cp = new Map();
for (const [k, c] of pb.cells) cp.set(k, cloneCell(c));
const snap = new Map();
for (const k of hitsR) snap.set(k, cloneCell(pb.cells.get(k)));
const changedR = applyReplaceInPlace(cp, 1, 477, null);
ok('替换格数 = 命中数', changedR === hitsR.length, changedR + ' vs ' + hitsR.length);
ok('总格数不变', cp.size === pb.cells.size, cp.size + ' vs ' + pb.cells.size);

let badType = 0, badField = 0;
for (const k of hitsR) {
  const a = snap.get(k), b = cp.get(k);
  if (b.type !== 477) badType++;
  if (b.wall !== a.wall || b.frameX !== a.frameX || b.frameY !== a.frameY ||
      b.wire !== a.wire || b.actuator !== a.actuator || b.slope !== a.slope ||
      b.halfBrick !== a.halfBrick || b.paint !== a.paint || b.wallPaint !== a.wallPaint) badField++;
}
ok('每格 type 都变成 477', badType === 0, 'bad=' + badType);
ok('wall/frame/slope/wire/paint 等字段逐格未变', badField === 0, 'bad=' + badField);

let cnt477 = 0;
for (const c of cp.values()) if (c.type === 477) cnt477++;
ok('477 总数 = 替换前的 1 的数量', cnt477 === n1, cnt477 + ' vs ' + n1);

const hitSet = new Set(hitsR);
let untouched = true;
for (const [k, c] of pb.cells) {
  if (hitSet.has(k)) continue;
  if (!cellsEqual(c, cp.get(k))) untouched = false;
}
ok('未命中的格子一格没动', untouched);

// 选区范围
const selRect = { x0: 0, y0: 60, x1: 9, y1: 64 };
const selHits = collectReplaceHits(pb.cells, 1, 477, selRect);
ok('选区内只命中 10x5 = 50 格', selHits.length === 50, 'hits=' + selHits.length);
const cp2 = new Map();
for (const [k, c] of pb.cells) cp2.set(k, cloneCell(c));
applyReplaceInPlace(cp2, 1, 477, selRect);
let inSel = 0, outSel = 0;
for (const [k, c] of cp2) {
  if (c.type !== 477) continue;
  const i = k.indexOf(',');
  const x = +k.slice(0, i), y = +k.slice(i + 1);
  if (x >= 0 && x <= 9 && y >= 60 && y <= 64) inSel++; else outSel++;
}
ok('选区内替换 50 格', inSel === 50, 'in=' + inSel);
ok('选区外没有 477', outSel === 0, 'out=' + outSel);

// 替换后仍然符合 .bp 格式
const txtR = serializeCells(cp);
ok('替换后仍是 12 列', txtR.split('\n').filter(Boolean).every(l => l.split(',').length === 12));
const dR = gameRead(txtR);
ok('替换后可被游戏端解析', dR.bad.length === 0, dR.n + ' 行，失败 ' + dR.bad.length);

out.push('');
out.push('=========================================');
out.push(fail === 0 ? 'RESULT: ALL PASS (' + (out.filter(l => l.indexOf('PASS') >= 0).length) + ' checks)' : 'RESULT: ' + fail + ' FAILED');
out.push('=========================================');
document.getElementById('out').textContent = out.join('\n');
document.title = fail === 0 ? 'SELFTEST-OK' : 'SELFTEST-FAIL';
'@

$harness = @"
<!doctype html>
<html lang="zh-CN"><head><meta charset="utf-8"><title>SELFTEST</title>
<style>body{background:#0b0e12;color:#cfe;font:12px/1.5 Consolas,monospace;padding:14px}pre{white-space:pre-wrap}</style>
</head><body><pre id="out">running...</pre>
<script>
$core
</script>
<script>
const FIX = { release: $relJson, stress: $stressJson, mixed: $mixedJson };
window.onerror = function (msg, src, line, col, err) {
  var pre = document.getElementById('out');
  pre.textContent = 'HARNESS ERROR: ' + msg + ' @line ' + line + (err && err.stack ? ('\n' + err.stack) : '');
  document.title = 'SELFTEST-ERROR';
};
$testJs
</script>
</body></html>
"@
$harnessPath = Join-Path $self 'harness.html'
[IO.File]::WriteAllText($harnessPath, $harness, (New-Object Text.UTF8Encoding($false)))

# ---------------------------------------------------------------- 5. 渲染测试页
#   视图参数走 URL query：?v=x,y,zoom
$renderJs = @"
function applyView() {
  try {
    BP.loadFromText(FIXTURE, 'stress-120x80.bp');
    var q = new URLSearchParams(location.search);
    var v = q.get('v');
    if (v) {
      var p = v.split(',').map(Number);
      BP.setView(p[0], p[1], p[2]);
      var ly = q.get('layers');
      if (ly) {
        var L = BP.state.layers;
        if (ly.indexOf('no-frame') >= 0) L.frame = false;
      }
      document.title = 'RENDER-OK ' + v;
    } else {
      BP.fitView();
      document.title = 'RENDER-OK fit';
    }
  } catch (e) { document.title = 'RENDER-ERR ' + e.message; }
}
applyView();
window.addEventListener('load', applyView);
"@
$renderData = "const FIXTURE = $stressJson;`n$renderJs"
$renderHtml = $html.Replace('</body>', "<script>`n$renderData`n</script>`n</body>")
$renderPath = Join-Path $self 'render-test.html'
[IO.File]::WriteAllText($renderPath, $renderHtml, (New-Object Text.UTF8Encoding($false)))

# ---------------------------------------------------------------- 5.5 UI 交互测试页
$uiJs = @'
(function () {
const R = []; let fail = 0;
function ok(n, c, extra) { R.push((c ? '  PASS  ' : '  FAIL  ') + n + (extra ? '   [' + extra + ']' : '')); if (!c) fail++; }
function hdr(t) { R.push(''); R.push('=== ' + t + ' ==='); }
const S = BP.state;
const stage = document.getElementById('stage');
const rect = stage.getBoundingClientRect();
const cvEl = document.getElementById('cv');
function toClient(x, y) {
  const z = S.cam.zoom;
  return {
    x: rect.left + 26 + (x - S.cam.x + 0.5) * z,
    y: rect.top + 26 + (y - S.cam.y + 0.5) * z
  };
}
function pev(type, x, y, button) {
  const p = toClient(x, y);
  return new PointerEvent(type, {
    clientX: p.x, clientY: p.y, button: button || 0,
    buttons: button === 2 ? 2 : 1, pointerId: 1, isPrimary: true, bubbles: true, cancelable: true
  });
}
function clickCell(x, y) { cvEl.dispatchEvent(pev('pointerdown', x, y)); cvEl.dispatchEvent(pev('pointerup', x, y)); }
function key(k, mods) {
  window.dispatchEvent(new KeyboardEvent('keydown', Object.assign({ key: k, bubbles: true, cancelable: true }, mods || {})));
}

hdr('UI 交互（合成 PointerEvent 走完整事件链）');
ok('初始为空', S.cells.size === 0, 'cells=' + S.cells.size);

// --- 画笔 + 笔刷 3x3
document.querySelector('.sizes .btn[data-size="3"]').click();
ok('笔刷切到 3x3', S.size === 3, 'size=' + S.size);
const fType = document.getElementById('fType');
fType.value = '38';
fType.dispatchEvent(new Event('input', { bubbles: true }));
ok('方块设置为 38', S.brush.type === 38, 'type=' + S.brush.type);
clickCell(10, 10);
ok('画笔落了 3x3 = 9 格', S.cells.size === 9, 'cells=' + S.cells.size);
const anyCell = S.cells.get('10,10');
ok('新格子 frameX/Y = -1', anyCell && anyCell.frameX === -1 && anyCell.frameY === -1);

// --- 撤销 / 重做
key('z', { ctrlKey: true });
ok('Ctrl+Z 撤销后回到 0 格', S.cells.size === 0, 'cells=' + S.cells.size);
key('z', { ctrlKey: true, shiftKey: true });
ok('Ctrl+Shift+Z 重做后 9 格', S.cells.size === 9, 'cells=' + S.cells.size);
key('z', { ctrlKey: true });
key('y', { ctrlKey: true });
ok('Ctrl+Y 也能重做', S.cells.size === 9, 'cells=' + S.cells.size);

// --- 橡皮
document.querySelector('.tool[data-tool="eraser"]').click();
ok('工具切到橡皮', S.tool === 'eraser');
clickCell(10, 10);
ok('橡皮清空 3x3', S.cells.size === 0, 'cells=' + S.cells.size);

// --- 带完整属性的格子 + 吸管
document.querySelector('.tool[data-tool="brush"]').click();
document.querySelector('.sizes .btn[data-size="1"]').click();
document.getElementById('w0').checked = true;
document.getElementById('w2').checked = true;
document.getElementById('w0').dispatchEvent(new Event('change', { bubbles: true }));
document.getElementById('fPaint').value = '3';
document.getElementById('fPaint').dispatchEvent(new Event('change', { bubbles: true }));
document.querySelector('#slopeBtns .sb[data-slope="2"]').click();
document.getElementById('fHalf').checked = true;
document.getElementById('fHalf').dispatchEvent(new Event('change', { bubbles: true }));
clickCell(20, 20);
const c20 = S.cells.get('20,20');
ok('属性写入正确', c20 && c20.wire === 5 && c20.paint === 3 && c20.slope === 2 && c20.halfBrick === 1,
   c20 ? JSON.stringify({ wire: c20.wire, paint: c20.paint, slope: c20.slope, half: c20.halfBrick }) : 'missing');

// 改成别的属性后再吸回来
document.getElementById('btnClearProps').click();
ok('清空属性后 brush.type = 0', S.brush.type === 0);
document.querySelector('.tool[data-tool="picker"]').click();
clickCell(20, 20);
ok('吸管取回全部属性', S.brush.type === 38 && S.brush.wire === 5 && S.brush.paint === 3 && S.brush.slope === 2 && S.brush.halfBrick === 1,
   JSON.stringify({ t: S.brush.type, w: S.brush.wire, p: S.brush.paint, s: S.brush.slope, h: S.brush.halfBrick }));

// --- 映射表：加一行 38→477，预览 + 应用
document.getElementById('btnMapAdd').click();
const rowIn = document.querySelector('#mapRows input[data-side="from"]');
rowIn.value = '38';
rowIn.dispatchEvent(new Event('input', { bubbles: true }));
const toIn = document.querySelector('#mapRows input[data-side="to"]');
toIn.value = '477';
toIn.dispatchEvent(new Event('input', { bubbles: true }));
document.getElementById('btnMapPreview').click();
ok('预览命中 1 格', S.preview && S.preview.size === 1, 'preview=' + (S.preview ? S.preview.size : 'null'));
document.getElementById('btnMapApply').click();
ok('应用后类型变成 477', S.cells.get('20,20').type === 477, 'type=' + S.cells.get('20,20').type);
ok('替换可撤销', S.undo.length > 0, 'undo=' + S.undo.length);
key('z', { ctrlKey: true });
ok('撤销后类型回到 38', S.cells.get('20,20').type === 38, 'type=' + S.cells.get('20,20').type);

// --- 保存文本
const txt = BP.getText();
const lines = txt.trim().split('\n');
ok('保存输出 12 列', lines.every(l => l.split(',').length === 12), lines[0]);
ok('保存内容与内存一致', lines.length === S.cells.size, lines.length + ' vs ' + S.cells.size);

// --- 渲染语义：直接读 canvas 像素，验证斜坡方向 / 半砖只占下半
hdr('渲染语义（读 canvas 像素）');
BP.loadFromText([
  '0,0,38,0,-1,-1,0,0,0,0,0,0',
  '1,0,38,0,-1,-1,0,0,0,1,0,0',
  '2,0,38,0,-1,-1,0,0,1,0,0,0',
  '3,0,38,0,-1,-1,0,0,2,0,0,0',
  '4,0,38,0,-1,-1,0,0,3,0,0,0',
  '5,0,38,0,-1,-1,0,0,4,0,0,0'
].join('\n'), 'geom.bp');
const Z = 40;
BP.setView(0, 0, Z);
BP.render();
const g2d = cvEl.getContext('2d');
const DPRF = cvEl.width / cvEl.clientWidth || 1;
function px(cx, fy, fx) {
  const X = 26 + Math.round((cx - 0) * Z), Y = 26 + Math.round((0 - 0) * Z);
  const d = g2d.getImageData(Math.round((X + fx * Z) * DPRF), Math.round((Y + fy * Z) * DPRF), 1, 1).data;
  return d[0] + d[1] + d[2] > 240;   // 亮 = 有方块，暗 = 空地
}
ok('画布 DPR 取样基准', DPRF > 0 && DPRF <= 3, 'dpr=' + DPRF);
R.push('  画布 ' + cvEl.width + 'x' + cvEl.height + ' (CSS ' + cvEl.clientWidth + 'x' + cvEl.clientHeight + ')，cam=' +
       S.cam.x.toFixed(2) + ',' + S.cam.y.toFixed(2) + ' zoom=' + S.cam.zoom + '，格子数 ' + S.cells.size);
R.push('  px(4,.15,.85)=' + px(4, 0.15, 0.85) + ' px(4,.5,.5)=' + px(4, 0.5, 0.5) +
       ' px(5,.5,.5)=' + px(5, 0.5, 0.5) + ' px(3,.5,.5)=' + px(3, 0.5, 0.5));
ok('整块：中心是方块', px(0, 0.5, 0.5));
ok('半砖：上半为空 / 下半为方块', !px(1, 0.25, 0.5) && px(1, 0.75, 0.5),
   'top=' + px(1, 0.25, 0.5) + ' bottom=' + px(1, 0.75, 0.5));
ok('斜坡1：右下有 / 左上无', px(2, 0.85, 0.85) && !px(2, 0.15, 0.15),
   'br=' + px(2, 0.85, 0.85) + ' tl=' + px(2, 0.15, 0.15));
ok('斜坡2：左下有 / 右上无', px(3, 0.85, 0.15) && !px(3, 0.15, 0.85),
   'bl=' + px(3, 0.85, 0.15) + ' tr=' + px(3, 0.15, 0.85));
ok('斜坡3：右上有 / 左下无', px(4, 0.15, 0.85) && !px(4, 0.85, 0.15),
   'tr=' + px(4, 0.15, 0.85) + ' bl=' + px(4, 0.85, 0.15));
ok('斜坡4：左上有 / 右下无', px(5, 0.15, 0.15) && !px(5, 0.85, 0.85),
   'tl=' + px(5, 0.15, 0.15) + ' br=' + px(5, 0.85, 0.85));

// --- 右键平移
const beforeX = S.cam.x, z = S.cam.zoom;
const p0 = toClient(5, 5);
cvEl.dispatchEvent(new PointerEvent('pointerdown', { clientX: p0.x, clientY: p0.y, button: 2, buttons: 2, pointerId: 2, bubbles: true }));
cvEl.dispatchEvent(new PointerEvent('pointermove', { clientX: p0.x + 80, clientY: p0.y + 40, buttons: 2, pointerId: 2, bubbles: true }));
cvEl.dispatchEvent(new PointerEvent('pointerup', { clientX: p0.x + 80, clientY: p0.y + 40, button: 2, pointerId: 2, bubbles: true }));
ok('右键拖动改变视图', Math.abs(S.cam.x - (beforeX - 80 / z)) < 0.01,
   'camX ' + beforeX.toFixed(2) + ' → ' + S.cam.x.toFixed(2) + ' (期望 ' + (beforeX - 80 / z).toFixed(2) + ')');

// --- 选区
document.querySelector('.tool[data-tool="select"]').click();
cvEl.dispatchEvent(pev('pointerdown', 30, 30));
cvEl.dispatchEvent(pev('pointermove', 33, 32));
cvEl.dispatchEvent(pev('pointerup', 33, 32));
ok('选区 4x3', S.selection && (S.selection.x1 - S.selection.x0 + 1) === 4 && (S.selection.y1 - S.selection.y0 + 1) === 3,
   S.selection ? JSON.stringify(S.selection) : 'null');
document.querySelector('.tool[data-tool="brush"]').click();
document.querySelector('.sizes .btn[data-size="1"]').click();
const before = S.cells.size;
document.getElementById('btnSelFill').click();
ok('填充选区加了 12 格', S.cells.size - before === 12, (S.cells.size - before) + ' 新增');

/* ---- 一键替换（UI 事件链） ---- */
hdr('一键替换（UI：取源 → 目标 → 替换 → 撤销/重做 → 选区）');
BP.loadFromText(FIXTURE_STRESS, 'stress-120x80.bp');
BP.setView(0, 55, 20);
const countType = t => { let n = 0; for (const c of S.cells.values()) if (c.type === t) n++; return n; };
const srcIn = document.getElementById('repSrc');
const tgtIn = document.getElementById('repTgt');
const btnRep = document.getElementById('btnReplace');
const selChk = document.getElementById('repSelOnly');
const repInfo = document.getElementById('repInfo');

ok('初始状态替换按钮禁用', btnRep.disabled === true);
ok('初始提示包含"源方块"', repInfo.textContent.indexOf('源方块') >= 0, repInfo.textContent);

// 取源：点画布 (10,60)，该格是石块(1)
document.getElementById('btnPickSource').click();
ok('取源模式已开启', S.rep.pick === true);
clickCell(10, 60);
ok('源输入框 = 1', srcIn.value === '1', 'value=' + srcIn.value);
ok('取源后自动退出取源模式', S.rep.pick === false);
ok('源名称显示 = 石块', document.getElementById('repSrcName').textContent === '石块',
   document.getElementById('repSrcName').textContent);
ok('源行有高亮 class', document.getElementById('repSrcRow').classList.contains('flash'));

// 目标 477
tgtIn.value = '477';
tgtIn.dispatchEvent(new Event('input', { bubbles: true }));
ok('目标名称显示 = Golf Grass（477 无中文名，回退英文名）', document.getElementById('repTgtName').textContent === 'Golf Grass',
   document.getElementById('repTgtName').textContent);
ok('源≠目标时按钮可用', btnRep.disabled === false);
ok('命中数预览显示 2,500', repInfo.textContent.indexOf('2,500') >= 0, repInfo.textContent);

const before1 = countType(1), before477 = countType(477);
const p1 = Object.assign({}, S.cells.get('10,59'));   // 石块 + slope 1
const p2 = Object.assign({}, S.cells.get('10,78'));   // 石块 + wire 4
ok('探针格：斜坡格 slope=1', p1.slope === 1 && p1.type === 1, 'slope=' + p1.slope);
ok('探针格：电线格 wire=4', p2.wire === 4 && p2.type === 1, 'wire=' + p2.wire);
ok('替换前 type1 = 2500 / type477 = 0', before1 === 2500 && before477 === 0,
   before1 + ' / ' + before477);

btnRep.click();
ok('替换后 477 = 2500', countType(477) === before1, '477=' + countType(477));
ok('替换后 1 已清空', countType(1) === 0, '1=' + countType(1));
const q1 = S.cells.get('10,59'), q2 = S.cells.get('10,78');
ok('斜坡格只换 type、slope 保留', q1.type === 477 && q1.slope === p1.slope,
   'type=' + q1.type + ' slope=' + q1.slope);
ok('电线格只换 type、wire 保留', q2.type === 477 && q2.wire === 4,
   'type=' + q2.type + ' wire=' + q2.wire);
ok('状态栏显示替换结果',
   document.getElementById('stMsg').textContent.indexOf('Golf Grass(477)：替换 2,500 格') >= 0,
   document.getElementById('stMsg').textContent);
ok('面板显示已替换 + 可撤销提示', repInfo.textContent.indexOf('已替换 2,500 格') >= 0 && repInfo.textContent.indexOf('Ctrl+Z') >= 0,
   repInfo.textContent);

key('z', { ctrlKey: true });
ok('Ctrl+Z 回退（1 恢复 2500 / 477 归零）', countType(1) === 2500 && countType(477) === 0,
   countType(1) + ' / ' + countType(477));
key('z', { ctrlKey: true, shiftKey: true });
ok('Ctrl+Shift+Z 重做（477 = 2500）', countType(477) === 2500, '477=' + countType(477));
key('z', { ctrlKey: true });
key('y', { ctrlKey: true });
ok('Ctrl+Y 也能重做', countType(477) === 2500, '477=' + countType(477));

// 源 == 目标 / 源为空
srcIn.value = '477';
srcIn.dispatchEvent(new Event('input', { bubbles: true }));
ok('源 == 目标时按钮禁用', btnRep.disabled === true);
srcIn.value = '';
srcIn.dispatchEvent(new Event('input', { bubbles: true }));
ok('源为空时按钮禁用', btnRep.disabled === true);
ok('源为空时提示"请先选择源方块"', repInfo.textContent.indexOf('源方块') >= 0, repInfo.textContent);

// 回到初始状态，测「仅替换选区内」
key('z', { ctrlKey: true });   // 撤销上一次替换
ok('撤销后回到 2500 个石块', countType(1) === 2500, '1=' + countType(1));
srcIn.value = '1';
srcIn.dispatchEvent(new Event('input', { bubbles: true }));
tgtIn.value = '38';
tgtIn.dispatchEvent(new Event('input', { bubbles: true }));

document.querySelector('.tool[data-tool="select"]').click();
cvEl.dispatchEvent(pev('pointerdown', 0, 60));
cvEl.dispatchEvent(pev('pointermove', 9, 64));
cvEl.dispatchEvent(pev('pointerup', 9, 64));
ok('拖出 10×5 选区', S.selection && (S.selection.x1 - S.selection.x0 + 1) === 10 &&
   (S.selection.y1 - S.selection.y0 + 1) === 5, JSON.stringify(S.selection));
ok('有选区后勾选框可用', selChk.disabled === false);
ok('勾选前是"全图"', repInfo.textContent.indexOf('全图') >= 0, repInfo.textContent);

selChk.checked = true;
selChk.dispatchEvent(new Event('change', { bubbles: true }));
ok('勾选后提示"选区内"', repInfo.textContent.indexOf('选区内') >= 0, repInfo.textContent);
ok('选区内命中 50 格', repInfo.textContent.indexOf('50') >= 0, repInfo.textContent);

const before38 = countType(38);
btnRep.click();
ok('只替换选区内 50 格', countType(38) - before38 === 50, '新增 ' + (countType(38) - before38));
ok('选区外石块还在', S.cells.get('20,60').type === 1, 'type=' + S.cells.get('20,60').type);
ok('选区内已变灰砖', S.cells.get('0,60').type === 38, 'type=' + S.cells.get('0,60').type);
ok('全图石块少了 50', countType(1) === 2450, '1=' + countType(1));
ok('状态栏注明"仅选区内"', document.getElementById('stMsg').textContent.indexOf('仅选区内') >= 0,
   document.getElementById('stMsg').textContent);

/* ---- 全量 TileID 名称表 / 调色板搜索 / 悬停提示 ---- */
hdr('全量名称表（754 条）+ 调色板搜索 + 悬停提示');
BP.loadFromText(FIXTURE_STRESS, 'stress-120x80.bp');
BP.setView(0, 55, 20);
ok('内联名称表 754 条', BP.TILE_NAME_COUNT === 754, 'count=' + BP.TILE_NAME_COUNT);
ok('有中文名的 519 条', BP.TILE_LIST.filter(t => t.zh).length === 519, 'zh=' + BP.TILE_LIST.filter(t => t.zh).length);
ok('tileName(1) = 石块', BP.tileName(1) === '石块', BP.tileName(1));
ok('tileName(38) = 灰砖', BP.tileName(38) === '灰砖', BP.tileName(38));
ok('tileName(19) = 木平台', BP.tileName(19) === '木平台', BP.tileName(19));
ok('tileName(367) = 大理石块', BP.tileName(367) === '大理石块', BP.tileName(367));
ok('无中文名回退英文名（477 = Golf Grass）', BP.tileName(477) === 'Golf Grass', BP.tileName(477));
ok('表外 ID 仍显示 ID n', BP.tileName(9000) === 'ID 9000', BP.tileName(9000));
ok('tileLabel(12) = 名称(12)', BP.tileLabel(12) === BP.tileName(12) + '(12)', BP.tileLabel(12));

const palList = document.getElementById('palList');
const palSearch = document.getElementById('palSearch');
function searchPal(q) {
  palSearch.value = q;
  palSearch.dispatchEvent(new Event('input', { bubbles: true }));
  return [...palList.querySelectorAll('.palItem')].map(e => +e.dataset.id);
}
const allIds = searchPal('');
ok('调色板条目数 = 754', allIds.length === 754, 'rows=' + allIds.length);
ok('调色板角标显示全量 754', document.getElementById('palCount').textContent.indexOf('754') >= 0,
   document.getElementById('palCount').textContent);

const zhHits = searchPal('平台');
ok('中文搜索「平台」有命中', zhHits.length > 0, 'hits=' + zhHits.length + ' ids=' + zhHits.slice(0, 8).join(','));
ok('中文搜索「平台」命中 木平台(19)', zhHits.indexOf(19) >= 0, 'ids=' + zhHits.join(','));

const enHits = searchPal('brick');
ok('英文搜索「brick」有命中', enHits.length > 1, 'hits=' + enHits.length);
ok('英文搜索「brick」命中 灰砖(38)', enHits.indexOf(38) >= 0, 'ids=' + enHits.slice(0, 12).join(','));

const idHits = searchPal('472');
ok('ID 搜索「472」精确命中 1 条', idHits.length === 1 && idHits[0] === 472, 'ids=' + idHits.join(','));
ok('ID 472 显示真名', palList.textContent.indexOf('铁砖') >= 0, palList.querySelector('.palItem').textContent);

const multi = searchPal('brick 47');
ok('多词搜索「brick 47」取交集', multi.length > 0 && multi.every(id => BP.tileNameEn(id).toLowerCase().indexOf('brick') >= 0),
   'ids=' + multi.join(','));

searchPal('zzzz-不存在');
ok('搜不到时给提示', palList.textContent.indexOf('没有匹配项') >= 0, palList.textContent.slice(0, 24));
searchPal('');

// 配色散列：全量不能都一个色
const sws = [...palList.querySelectorAll('.palItem .sw')].slice(1, 401).map(e => e.style.background);
const uniqSw = new Set(sws).size;
ok('全量配色不全是一个色', uniqSw > 300, 'distinct=' + uniqSw + '/400');

// 悬停提示
const tip = document.getElementById('cellTip');
function hoverCell(x, y) {
  const hp = toClient(x, y);
  cvEl.dispatchEvent(new PointerEvent('pointermove', {
    clientX: hp.x, clientY: hp.y, buttons: 0, pointerId: 9, bubbles: true, cancelable: true
  }));
}
hoverCell(10, 60);
ok('悬停提示已显示', tip.classList.contains('on'), tip.className);
ok('悬停提示 = (10,60) 石块(1)', tip.textContent.indexOf('(10,60) 石块(1)') >= 0, tip.textContent);
hoverCell(10, 59);
ok('悬停提示带属性副标题（斜坡 1）', tip.textContent.indexOf('斜坡1') >= 0, tip.textContent);
hoverCell(60, 200);   // 图外的空地
ok('空白格显示"空"', tip.textContent.indexOf('空') >= 0, tip.textContent);

// 状态栏当前画笔名
const fTypeEl = document.getElementById('fType');
fTypeEl.value = '19';
fTypeEl.dispatchEvent(new Event('input', { bubbles: true }));
ok('状态栏显示当前画笔名', document.getElementById('stBrush').textContent.indexOf('木平台(19)') >= 0,
   document.getElementById('stBrush').textContent);
ok('当前属性角标同步名称', document.getElementById('brushTag').textContent.indexOf('木平台') >= 0,
   document.getElementById('brushTag').textContent);

// 映射表行内显示名字
BP.state.mappings = [{ from: 38, to: 477 }];
BP.refreshMapRows();
const mapNames = [...document.querySelectorAll('#mapRows .mnm')].map(e => e.textContent);
ok('映射表源/目标显示名字', mapNames[0] === '灰砖' && mapNames[1] === 'Golf Grass', mapNames.join(' → '));

R.push('');
R.push('=========================================');
R.push(fail === 0 ? 'RESULT: ALL PASS (' + R.filter(l => l.indexOf('PASS') >= 0).length + ' checks)' : 'RESULT: ' + fail + ' FAILED');
R.push('=========================================');
const pre = document.createElement('pre');
pre.id = 'out';
pre.textContent = R.join('\n');
document.body.appendChild(pre);
document.title = fail === 0 ? 'UITEST-OK' : 'UITEST-FAIL';
})();
'@
$uiHtml = $html.Replace('</body>', "<script>`nconst FIXTURE_STRESS = $stressJson;`n$uiJs`n</script>`n</body>")
$uiPath = Join-Path $self 'ui-test.html'
[IO.File]::WriteAllText($uiPath, $uiHtml, (New-Object Text.UTF8Encoding($false)))

# ---------------------------------------------------------------- 5.6 一键替换演示页（截图用）
$demoJs = @"
(function () {
  function boot() {
    try {
      BP.loadFromText(FIXTURE, 'stress-120x80.bp');
      BP.setView(0, 28, 15);
      var q = new URLSearchParams(location.search);
      if (q.get('mode') === 'after') {
        var s = document.getElementById('repSrc');
        s.value = '1';
        s.dispatchEvent(new Event('input', { bubbles: true }));
        var t = document.getElementById('repTgt');
        t.value = '477';
        t.dispatchEvent(new Event('input', { bubbles: true }));
        document.getElementById('btnReplace').click();
        document.title = 'REPLACE-AFTER';
      } else {
        document.title = 'REPLACE-BEFORE';
      }
    } catch (e) { document.title = 'DEMO-ERR ' + e.message; }
  }
  boot();
  window.addEventListener('load', boot);
})();
"@
$demoHtml = $html.Replace('</body>', "<script>`nconst FIXTURE = $stressJson;`n$demoJs`n</script>`n</body>")
$demoPath = Join-Path $self 'replace-demo.html'
[IO.File]::WriteAllText($demoPath, $demoHtml, (New-Object Text.UTF8Encoding($false)))

# ---------------------------------------------------------------- 5.7 全量名称 / 悬停提示演示页（截图用）
$namesJs = @"
(function () {
  function boot() {
    try {
      BP.loadFromText(FIXTURE, 'stress-120x80.bp');
      BP.setView(0, 55, 26);
      var q = new URLSearchParams(location.search);
      var mode = q.get('mode') || 'palette';
      var box = document.getElementById('palList');
      var cnt = document.getElementById('palCount');
      if (mode === 'search') {
        box.style.maxHeight = '430px';
        var s = document.getElementById('palSearch');
        s.value = q.get('q') || 'brick';
        s.dispatchEvent(new Event('input', { bubbles: true }));
        document.title = 'NAMES-SEARCH ' + cnt.textContent;
      } else {
        box.style.maxHeight = '520px';   // 截图时临时拉高多看几条，真实 UI 是 300px
        document.title = 'NAMES-PALETTE ' + cnt.textContent;
      }
      // 悬停到 (10,60) 石块(1)，让格子 tooltip 出现
      var cv = document.getElementById('cv');
      var r = cv.getBoundingClientRect(), S = BP.state;
      var cx = r.left + 26 + (10 - S.cam.x + 0.5) * S.cam.zoom;
      var cy = r.top + 26 + (60 - S.cam.y + 0.5) * S.cam.zoom;
      cv.dispatchEvent(new PointerEvent('pointermove', {
        clientX: cx, clientY: cy, buttons: 0, pointerId: 3, bubbles: true, cancelable: true
      }));
    } catch (e) { document.title = 'NAMES-ERR ' + e.message; }
  }
  boot();
  window.addEventListener('load', boot);
})();
"@
$namesHtml = $html.Replace('</body>', "<script>`nconst FIXTURE = $stressJson;`n$namesJs`n</script>`n</body>")
$namesPath = Join-Path $self 'names-demo.html'
[IO.File]::WriteAllText($namesPath, $namesHtml, (New-Object Text.UTF8Encoding($false)))

Write-Host '== 生成完成 =='
Write-Host ("harness  {0}" -f $harnessPath)
Write-Host ("render   {0}" -f $renderPath)
Write-Host ("ui-test  {0}" -f $uiPath)
Write-Host ("replace  {0}" -f $demoPath)
Write-Host ("names    {0}" -f $namesPath)

# ---------------------------------------------------------------- 6. 跑 Chrome
function New-Profile { Join-Path $env:TEMP ('bpchrome_' + [guid]::NewGuid().ToString('N')) }
if (Test-Path $chrome) {
  Write-Host '== Chrome 逻辑测试 =='
  $uri = ([uri]($harnessPath)).AbsoluteUri
  $prof = New-Profile
  $dom = & $chrome --headless=new --disable-gpu --no-first-run --no-default-browser-check `
        --user-data-dir="$prof" --window-size=1500,950 --virtual-time-budget=6000 --dump-dom $uri 2>$null | Out-String
  Remove-Item $prof -Recurse -Force -ErrorAction SilentlyContinue
  $mm = [regex]::Match($dom, '(?s)<pre id="out">(.*?)</pre>')
  if ($mm.Success) {
    $report = $mm.Groups[1].Value -replace '&lt;', '<' -replace '&gt;', '>' -replace '&amp;', '&' -replace '&quot;', '"'
    [IO.File]::WriteAllText((Join-Path $self 'report.txt'), $report, (New-Object Text.UTF8Encoding($false)))
    Write-Host '---- 测试报告 ----'
    Write-Host $report
  } else {
    Write-Host "未能从 DOM 中取到 <pre id=out>（DOM 长度 $($dom.Length)）"
    if ($dom.Length) { Write-Host $dom.Substring(0, [Math]::Min(600, $dom.Length)) }
  }

  Write-Host '== Chrome UI 测试 =='
  $uiUri = ([uri]$uiPath).AbsoluteUri
  $profU = New-Profile
  $domU = & $chrome --headless=new --disable-gpu --no-first-run --no-default-browser-check `
        --user-data-dir="$profU" --window-size=1500,950 --virtual-time-budget=6000 --dump-dom $uiUri 2>$null | Out-String
  Remove-Item $profU -Recurse -Force -ErrorAction SilentlyContinue
  $mmU = [regex]::Match($domU, '(?s)<pre id="out">(.*?)</pre>')
  if ($mmU.Success) {
    $repU = $mmU.Groups[1].Value -replace '&lt;', '<' -replace '&gt;', '>' -replace '&amp;', '&' -replace '&quot;', '"'
    [IO.File]::WriteAllText((Join-Path $self 'report-ui.txt'), $repU, (New-Object Text.UTF8Encoding($false)))
    Write-Host $repU
  } else {
    Write-Host "UI 测试未产生输出（DOM 长度 $($domU.Length)）"
    $t2 = [regex]::Match($domU, '<title>(.*?)</title>')
    if ($t2.Success) { Write-Host ('title = ' + $t2.Groups[1].Value) }
  }

  Write-Host '== Chrome 截图 =='
  $baseUri = ([uri]$renderPath).AbsoluteUri
  $demoUri = ([uri]$demoPath).AbsoluteUri
  $namesUri = ([uri]$namesPath).AbsoluteUri
  $shots = @(
    @{ file = 'editor-empty.png';    uri = ([uri]$editorPath).AbsoluteUri; desc = '刚打开（空状态）' },
    @{ file = 'render-overview.png'; uri = $baseUri;                      desc = '总览 120x80 自适应' },
    @{ file = 'render-closeup.png';  uri = ($baseUri + '?v=-0.5,0.5,44'); desc = '斜坡/半砖/电线/促动器 特写' },
    @{ file = 'render-walls.png';    uri = ($baseUri + '?v=25,8,36');     desc = '墙/油漆/纯墙格 特写' },
    @{ file = 'replace-before.png';  uri = ($demoUri + '?mode=before');   desc = '一键替换前（石块 1）' },
    @{ file = 'replace-after.png';   uri = ($demoUri + '?mode=after');    desc = '一键替换后（Golf Grass 477）' },
    @{ file = 'names-palette-tooltip.png'; uri = $namesUri; w = 1600; h = 1400; desc = '全量 754 名称 + 悬停 tooltip' },
    @{ file = 'names-search.png';    uri = ($namesUri + '?mode=search&q=brick'); w = 1600; h = 1200; desc = '英文搜索 brick（中文/ID 同样可搜）' }
  )
  foreach ($shot in $shots) {
    $outFile = Join-Path $shotDir $shot.file
    $sw = if ($shot.w) { $shot.w } else { 1500 }
    $sh = if ($shot.h) { $shot.h } else { 950 }
    $prof2 = New-Profile
    & $chrome --headless=new --disable-gpu --no-first-run --no-default-browser-check `
      --user-data-dir="$prof2" --hide-scrollbars --force-device-scale-factor=1 `
      --window-size="$sw,$sh" --virtual-time-budget=6000 --screenshot="$outFile" $shot.uri 2>$null | Out-Null
    Remove-Item $prof2 -Recurse -Force -ErrorAction SilentlyContinue
    if (Test-Path $outFile) {
      Write-Host ("{0,-20} {1}  {2:N0} 字节  ({3})" -f $shot.file, $outFile, (Get-Item $outFile).Length, $shot.desc)
    } else { Write-Host ("{0} 截图失败" -f $shot.file) }
  }
} else {
  Write-Host '找不到 Chrome，跳过浏览器测试'
}
