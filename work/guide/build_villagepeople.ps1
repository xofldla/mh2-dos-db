# 공략 페이지 「村人との交流」(mh2kaiseki, EUC-JP)를 한글 패치 용어로 옮긴 오프라인 HTML(guide/villagepeople.html)을 만든다.
# 원문 HTML은 work/guide/src/villagepeople.html 에 둔다(EUC-JP 그대로). 아이템 이름은 work/trans/ov0_items_names.tsv(한글 패치)에서 가져온다.
$root = (Resolve-Path "$PSScriptRoot\..\..").Path; [Environment]::CurrentDirectory = $root
Add-Type -Path "$root\work\guide\tools\HtmlGrid.cs"
$u8 = New-Object Text.UTF8Encoding $false
$src = [Text.Encoding]::GetEncoding(51932).GetString([IO.File]::ReadAllBytes("$root\work\guide\src\villagepeople.html"))
$tables = [regex]::Matches($src, '(?is)<table.*?</table>')

# 아이템 이름: 한글 패치 번역 + 원문 표기 차이 보정
$item = @{}; foreach ($l in [IO.File]::ReadAllLines("$root\work\trans\ov0_items_names.tsv", $u8) | Select-Object -Skip 1) { $c = $l -split "`t"; if ($c.Count -ge 3 -and -not $item.ContainsKey($c[1])) { $item[$c[1]] = $c[2] } }
$item['落葉草'] = $item['落陽草']; $item['打ち上げタル爆弾'] = $item['打上げタル爆弾']
function KoItem([string]$ja) {
  $lines = $ja -split "`n"; $name = $lines[0]; $ko = $item[$name]; if (-not $ko) { throw "no Korean name for item: $name" }
  $note = ''; if ($lines.Count -gt 1) { $note = switch -regex ($lines[1]) { '食材' { ' (식재료)' } '薬品' { ' (약품)' } default { " ($($lines[1]))" } } }
  $ko + $note
}
function Esc([string]$s) { [Net.WebUtility]::HtmlEncode($s) }

# 용어(게임 안 표기 기준)
$T = @{ '一般' = '일반'; '工房関係者' = '공방 관계자'; '港関係者' = '항구 관계자'
  '村長' = '촌장'; '工房のばあちゃん' = '공방 할머니'; '船大工の親方' = '조선소 대장'; '教官' = '교관'; '工房の一番弟子' = '공방 수제자'
  '調合屋のあるじ' = '조합점 주인'; '雑貨屋ネコ' = '잡화고양이'; '工房の二番弟子' = '공방 둘째 제자'; '食材屋の女将' = '식재료 상인'
  '修行中女ハンター' = '수행 중인 여자 헌터'; '工房の新弟子' = '공방 새 제자'; '道具屋の主人' = '도구점 주인'; 'ライバルハンター' = '라이벌 헌터'; '竜人族のお姉さま' = '용인족 언니'
  'プレゼント' = '선물'; '食事の誘い' = '식사 초대'; '村特売' = '마을 특매'; '竜職人' = '용 장인'; '行商' = '행상'; '行商特売' = '행상 특매'; '美容師猫' = '미용사 고양이'
  '村人個別の友好度' = '마을 사람 개인 친밀도'; '港関係者の友好度合計' = '항구 관계자 친밀도 합계'; '工房関係者の友好度合計' = '공방 관계자 친밀도 합계'; '村全体の友好度合計' = '마을 전체 친밀도 합계'
  '港がレベル３' = '항구 LV3'; '工房がレベル３' = '공방 LV3' }
function Tr([string]$s) { if ($T.ContainsKey($s)) { $T[$s] } elseif ($s -eq '') { '' } else { throw "no term: $s" } }

# 1) 마을 사람 유형 (표 2: 머리 2행 다음 3열씩)
$g = [HtmlGrid]::Parse($tables[1].Value)
$types = @{ '일반' = @(); '공방 관계자' = @(); '항구 관계자' = @() }; $hdr = $null
foreach ($row in $g) { $all = @($row | ForEach-Object { $_.Trim() }); if (($all -join '') -match '^●') { continue }; $cells = $all; if (-not ($cells -join '')) { continue }
  if (($cells -join ',') -eq '一般,工房関係者,港関係者') { $hdr = $cells; continue }
  if ($hdr) { for ($i = 0; $i -lt $cells.Count -and $i -lt 3; $i++) { $types[(Tr $hdr[$i])] += (Tr $cells[$i]) } } }

# 2) 퀘스트 레벨
$q = [HtmlGrid]::Parse($tables[2].Value) | Select-Object -Skip 1 | ForEach-Object { , @($_[0], $_[1]) }

# 3) 납품 아이템 (표 4: 4묶음 × (이름, 개수, 친밀도))
$g = [HtmlGrid]::Parse($tables[3].Value); $req = [ordered]@{}
foreach ($base in 0, 4, 8, 12) { for ($r = 1; $r -lt $g.Count; $r++) { $row = $g[$r]; if ($row.Length -le $base + 2 -or -not $row[$base]) { continue }
  $k = KoItem $row[$base]; if (-not $req.Contains($k)) { $req[$k] = @{ ja = ($row[$base] -replace "`n", ' '); tiers = @() } }
  $req[$k].tiers += , @(($row[$base + 1] -replace '個', '개'), $row[$base + 2]) } }

# 4) 이벤트
$ev = [HtmlGrid]::Parse($tables[4].Value) | Select-Object -Skip 1 | ForEach-Object { , @((Tr $_[0]), (Tr $_[1]), $_[2], $(if ($_.Length -gt 3) { Tr $_[3] } else { '' })) }

# 5) 선물 (표 6), 누적 인원 (표 7)
$gift = [HtmlGrid]::Parse($tables[5].Value) | Select-Object -Skip 1 | ForEach-Object { $r = $_; , @(($r[0] -replace '１', '1' -replace '２', '2' -replace '３', '3' -replace '４', '4' -replace '５', '5' -replace '回目', '회째'), "$(KoItem $r[1]) $($r[2])", "$(KoItem $r[3]) $($r[4])", "$(KoItem $r[5]) $($r[6])") }
$cum = [HtmlGrid]::Parse($tables[6].Value) | Select-Object -Skip 1 | ForEach-Object { $n = $_[0] -replace '人目', '' ; $n = -join ($n.ToCharArray() | ForEach-Object { if ([int]$_ -ge 0xFF10 -and [int]$_ -le 0xFF19) { [char]([int]$_ - 0xFF10 + 48) } else { $_ } }); , @("${n}번째", ($_[1] -replace '星鉄', '성철 ')) }

# 6) 식사 초대 (표 8)
$meal = [HtmlGrid]::Parse($tables[7].Value); $who = $meal[1] | Select-Object -Skip 1 | ForEach-Object { (($_ -split "`n") | ForEach-Object { Tr $_ }) -join ', ' }
$fx = { param($s) $s -replace '体力', '체력' -replace 'スタミナ', '스태미나' -replace '防御', '방어' -replace '攻撃', '공격' }
$mealRows = $meal | Select-Object -Skip 2 | ForEach-Object { , @((($_[0] -replace 'マイハウスレベル', '마이하우스 LV') -replace '１','1' -replace '２','2' -replace '３','3'), (& $fx $_[1]), (& $fx $_[2]), (& $fx $_[3])) }

# HTML
$sb = New-Object Text.StringBuilder
function A($s) { [void]$sb.AppendLine($s) }
A @'
<!doctype html>
<html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>마을 사람과의 교류 | 몬스터헌터2 (DOS) DB</title>
<style>
@font-face { font-family:"Galmuri11"; src:url("fonts/Galmuri11.woff2") format("woff2"); font-weight:400 }
@font-face { font-family:"Galmuri11"; src:url("fonts/Galmuri11-Bold.woff2") format("woff2"); font-weight:700 }
:root { --bg:#f6f1e7; --card:#fffdf8; --ink:#2b2621; --sub:#6f665c; --line:#ddd2c2; --head:#efe4d1; --acc:#8a3b1e; }
:root[data-theme="dark"] { --bg:#1d1a17; --card:#26221e; --ink:#ece5da; --sub:#a59a8c; --line:#3b352f; --head:#302a24; --acc:#e08a5c; }
* { box-sizing:border-box } body { margin:0; background:var(--bg); color:var(--ink); font:12px/1.75 "Galmuri11","Malgun Gothic","Apple SD Gothic Neo",sans-serif }
main { max-width:960px; margin:0 auto; padding:24px 16px 64px }
h1 { font-size:24px; margin:0 0 4px } h2 { font-size:24px; margin:32px 0 10px; color:var(--acc) }
.sub { color:var(--sub) } nav a { color:var(--acc); margin-right:12px }
table { border-collapse:collapse; width:100%; background:var(--card) }
th, td { border:1px solid var(--line); padding:6px 8px; text-align:left; vertical-align:top } th { background:var(--head); font-weight:700 }
td.n { text-align:right; white-space:nowrap } .ja { color:var(--sub) } .wrap { overflow-x:auto }
input[type=search] { width:100%; max-width:360px; padding:8px 10px; border:1px solid var(--line); border-radius:6px; background:var(--card); color:var(--ink); font:inherit; margin:4px 0 10px }
p.note { color:var(--sub); margin:6px 0 } .tips { background:var(--card); border:1px solid var(--line); border-radius:8px; padding:6px 14px } .tips p { margin:8px 0 } .eff { color:var(--sub); font-size:12px } .star { font-size:14px }
:root { --red:#c0201a } :root[data-theme="dark"] { --red:#ff5a4a }
h2.why { color:var(--red); font-weight:700 } .whybox, .whybox b { color:var(--ink); font-weight:700 } u.lv { text-decoration:underline; text-decoration-thickness:2px; text-underline-offset:3px } .whybox { background:var(--card); border:2px solid var(--red); border-radius:8px; padding:4px 14px } ul.why { margin:6px 0 10px; padding-left:20px } ul.why li { margin:4px 0 }
ol.tips { background:var(--card); border:1px solid var(--line); border-radius:8px; padding:8px 14px 8px 34px } ol.tips > li { margin:8px 0 } ol.best { margin:6px 0; padding-left:22px } ol.best li { margin:2px 0 }
footer { margin-top:40px; color:var(--sub) }
#themebtn { font:inherit; display:inline-flex; align-items:center; gap:6px; padding:3px 10px; cursor:pointer; color:var(--ink); background:var(--card); border:1px solid var(--line); border-radius:6px } #themebtn.floating { position:fixed; top:12px; right:16px; z-index:30 } #themebtn .ti { color:var(--acc) }
</style><script src="assets/theme.js"></script></head><body><main>
<nav><a href="index.html">← 공략 목차</a></nav>
<h1>마을 사람과의 교류</h1>
<div class="sub">점보 마을 사람들의 납품 의뢰와 친밀도</div>
<p>마을 사람의 의뢰(아이템 납품·퀘스트)를 해결하면 그 사람의 <b>친밀도</b>가 오릅니다. 친밀도가 쌓이면 선물·식사 초대 같은 이벤트가 생기고 마을 시설이 발전합니다.</p>
'@
# 효율: 1개당 친밀도(요구 수 단계 중 가장 좋은 값). 상위 15%에 ⭐
$eff = @{}; $bestTier = @{}; foreach ($k in $req.Keys) { $best = 0.0; foreach ($t in $req[$k].tiers) { $c = [double](($t[0] -replace '[^0-9]', '')); $pt = [double](($t[1] -replace '[^0-9]', '')); if ($c -gt 0 -and $pt / $c -gt $best) { $best = $pt / $c; $bestTier[$k] = "$($t[0]) → $($t[1])" } }; $eff[$k] = $best }
$sorted = @($eff.Values | Sort-Object -Descending); $cut = $sorted[[Math]::Min($sorted.Count - 1, [Math]::Ceiling($sorted.Count * 0.15) - 1)]
$starKeys = @($eff.Keys | Where-Object { $eff[$_] -ge $cut } | Sort-Object @{ e = { $eff[$_] }; Descending = $true }, @{ e = { $_ } })

# 1) 왜 해야 하나 (가장 중요 — 맨 위, 빨간 굵은 글씨)
A '<h2 class="why">★ 왜 마을의뢰를 해야 하나? (가장 중요)</h2><div class="whybox">'
A '<p class="why">마을 사람의 의뢰를 해결해 친밀도를 쌓으면 아래 이벤트가 생깁니다. 선물로 회복약・광석・성철을 받고, 식사 초대로 능력치가 오르고, 마을 특매・용 장인・행상 같은 시설 이벤트가 열립니다.</p><ul class="why">'
foreach ($r in $ev) {
  $extra = if ($r[3]) { " + <u class=lv>$(Esc $r[3])</u>" } else { '' }
  $what = switch ($r[0]) {
    '선물' { " → $(Esc $gift[0][1]) / $(Esc $gift[0][2]) / $(Esc $gift[0][3]) 등 (마을 사람 유형마다 다름)" }
    '식사 초대' { " → 식사 효과: $(Esc (($mealRows[-1][1..3] | ForEach-Object { $_ -replace "`n", ', ' }) -join ' / ')) (최고 마이하우스 LV 기준)" }
    default { '' } }
  A ("<li><b>{0}</b>: {1} {2}{3}{4}</li>" -f (Esc $r[0]), (Esc $r[1]), (Esc $r[2]), $extra, $what) }
A ('<li><b>성철</b>: 선물을 준 마을 사람이 누적 {0}가 될 때마다 성철을 따로 받습니다.</li></ul></div>' -f (Esc (($cum | ForEach-Object { $_[0] }) -join '・')))

# 2) 효율적으로 올리는 팁 (순서대로)
$qpt = ($q | ForEach-Object { "{0} {1}" -f ($_[0] -replace '１','1' -replace '２','2' -replace '３','3' -replace '４','4'), $_[1] }) -join ', '
A '<h2>친밀도를 효율적으로 올리는 순서</h2><ol class="tips">'
A ('<li><b>누구에게 할지 정하기:</b> 항구 쪽 이벤트는 <b>항구 관계자</b>({0}), 공방 쪽 이벤트는 <b>공방 관계자</b>({1})의 의뢰를 해결해야 그쪽 친밀도 합계가 쌓입니다. 일반 마을 사람의 의뢰는 마을 전체 친밀도에만 들어갑니다.</li>' -f (Esc (($types['항구 관계자'] | Where-Object { $_ }) -join '・')), (Esc (($types['공방 관계자'] | Where-Object { $_ }) -join '・')))
A '<li><b>공방부터 노리기:</b> <b><u class=lv>공방 LV3</u></b>이고 공방 관계자 친밀도 합계가 <b>20pt</b>면 「용 장인」, <b><u class=lv>항구 LV3</u></b>이고 항구 관계자 합계가 <b>50pt</b>면 「마을 특매」가 생깁니다. 공방 쪽 필요량이 적으니 먼저 노리기 좋습니다.</li>'
A ('<li><b>의뢰 퀘스트는 별이 높은 것부터:</b> 한 번에 오르는 친밀도는 {0}입니다.</li>' -f (Esc $qpt))
A '<li><b>납품은 아래 순서의 아이템부터:</b> 1개당 친밀도가 높은 순서입니다. 요구 수가 여러 단계면 적힌 단계로 납품할 때 가장 효율이 좋습니다.<ol class="best">'
foreach ($k in $starKeys) { A ("<li><b>{0}</b> <span class=eff>{1:0.##}pt/개 ({2})</span></li>" -f (Esc $k), $eff[$k], (Esc $bestTier[$k])) }
A '</ol></li></ol><p class="note">※ 항구・공방 레벨이 몇 pt에서 오르는지는 원문 자료(mh2kaiseki)에도 적혀 있지 않아 여기서는 다루지 않습니다.</p>'

A '<h2>마을 사람 유형</h2><div class="wrap"><table><tr><th>일반</th><th>공방 관계자</th><th>항구 관계자</th></tr>'
$max = (($types.Values | ForEach-Object { $_.Count }) | Measure-Object -Maximum).Maximum
for ($i = 0; $i -lt $max; $i++) { A ("<tr><td>{0}</td><td>{1}</td><td>{2}</td></tr>" -f (Esc $types['일반'][$i]), (Esc $types['공방 관계자'][$i]), (Esc $types['항구 관계자'][$i])) }
A '</table></div>'
A '<h2>퀘스트 레벨별 친밀도</h2><div class="wrap"><table style="max-width:320px"><tr><th>퀘스트 레벨</th><th>친밀도</th></tr>'
foreach ($r in $q) { A ("<tr><td>{0}</td><td class=n>{1}</td></tr>" -f (Esc ($r[0] -replace '１', '1' -replace '２', '2' -replace '３', '3' -replace '４', '4')), (Esc $r[1])) }
A '</table></div>'
A '<h2>납품 아이템별 친밀도</h2><p class="note">같은 아이템도 요구 수가 많으면 친밀도가 더 오릅니다. 이름으로 찾을 수 있습니다. <b>⭐ = 1개당 친밀도가 높은 효율 좋은 납품</b>(상위 15%).</p>'
A '<input type="search" id="q" placeholder="아이템 이름 검색 (예: 버섯)" oninput="f()"><div class="wrap"><table id="items"><tr><th>아이템</th><th>요구 수 → 친밀도</th></tr>'
foreach ($k in ($req.Keys | Sort-Object)) { $v = $req[$k]; $tiers = ($v.tiers | ForEach-Object { "$($_[0]) → $($_[1])" }) -join '<br>'
  $star = if ($eff[$k] -ge $cut) { '<span class="star" title="1개당 친밀도가 높은 납품">⭐</span> ' } else { '' }
  A ("<tr><td>{0}{1}<div class=ja>{2}</div></td><td>{3}<div class=eff>1개당 최대 {4:0.##}pt</div></td></tr>" -f $star, (Esc $k), (Esc $v.ja), $tiers, $eff[$k]) }
A '</table></div>'
A '<h2>이벤트 발생 조건</h2><div class="wrap"><table><tr><th>이벤트</th><th>필요한 친밀도</th><th>필요량</th><th>추가 조건</th></tr>'
foreach ($r in $ev) { A ("<tr><td>{0}</td><td>{1}</td><td class=n>{2}</td><td>{3}</td></tr>" -f (Esc $r[0]), (Esc $r[1]), (Esc $r[2]), (Esc $r[3])) }
A '</table></div>'
A '<h2>마을 사람에게 받는 선물</h2><p class="note">선물은 마을 사람 유형마다 다릅니다. 1~5회째는 아래 순서이고, 6회째부터는 같은 유형의 선물 중 무작위입니다.</p>'
A '<div class="wrap"><table><tr><th></th><th>일반</th><th>공방 관계자</th><th>항구 관계자</th></tr>'
foreach ($r in $gift) { A ("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td></tr>" -f (Esc $r[0]), (Esc $r[1]), (Esc $r[2]), (Esc $r[3])) }
A '</table></div>'
A '<p class="note">선물을 준 마을 사람이 누적으로 아래 인원이 되면 유형과 관계없이 성철을 받습니다. 21번째부터는 다시 1부터 셉니다.</p>'
A '<div class="wrap"><table style="max-width:320px"><tr><th>누적 인원</th><th>받는 아이템</th></tr>'
foreach ($r in $cum) { A ("<tr><td>{0}</td><td>{1}</td></tr>" -f (Esc $r[0]), (Esc $r[1])) }
A '</table></div>'
A '<h2>식사 초대</h2><p class="note">식사 효과는 초대한 사람의 유형(A·B·C)과 마이하우스 레벨에 따라 정해집니다.</p>'
A '<div class="wrap"><table><tr><th></th><th>A 유형</th><th>B 유형</th><th>C 유형</th></tr>'
A ("<tr><td>초대하는 사람</td><td>{0}</td><td>{1}</td><td>{2}</td></tr>" -f (Esc $who[0]), (Esc $who[1]), (Esc $who[2]))
foreach ($r in $mealRows) { A ("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td></tr>" -f (Esc $r[0]), (Esc $r[1]), (Esc $r[2]), (Esc $r[3])) }
A '</table></div>'
A @'
<footer>자료 출처: mmpotato「モンスターハンター２解析情報」— 村人との交流 (원문 갱신 2011-11-04). 아이템·인물 이름은 몬스터헌터 2 한글 패치 표기를 따릅니다.</footer>
</main>
<script>
function f(){ var q=document.getElementById('q').value.trim(); var rows=document.querySelectorAll('#items tr'); for (var i=1;i<rows.length;i++){ rows[i].style.display = (!q || rows[i].textContent.indexOf(q)>=0) ? '' : 'none'; } }
</script></body></html>
'@
New-Item -ItemType Directory -Force "$root\guide" | Out-Null
[IO.File]::WriteAllText("$root\guide\villagepeople.html", $sb.ToString(), $u8)
"items $($req.Count), events $($ev.Count), gifts $($gift.Count), meal rows $($mealRows.Count), types " + (($types.Keys | ForEach-Object { "$_=$($types[$_].Count)" }) -join ' ')
