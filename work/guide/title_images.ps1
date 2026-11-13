# 페이지 제목(h1)과 상단 로고를 고운바탕(Gowun Batang, SIL OFL 1.1) Bold로 그린 이미지로 바꾼다. (2026-11-13 리틱체에서 변경)
# 8MB짜리 한글 TTF를 웹폰트로 싣는 대신 제목 글자만 이미지로 만든다. 폰트 파일은 work/fonts_ext(git 제외)에 두고 PNG만 올린다.
# 이미지는 흰 글자+투명 배경 마스크라, CSS에서 글자색(var(--ink)/var(--acc))을 입혀 라이트/다크 테마를 그대로 따른다.
# build_mhos.ps1 / build_villagepeople.ps1 다음에 실행 (publish.ps1에 포함). 이미 바뀐 제목도 지금 글꼴로 다시 그린다.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$fontFile = "$root\work\fonts_ext\gowun\GowunBatang-Bold.ttf"
if (-not (Test-Path $fontFile)) { "고운바탕 TTF가 없어 제목 이미지를 건너뜁니다 ($fontFile)"; return }
Add-Type -AssemblyName System.Drawing
$pfc = New-Object System.Drawing.Text.PrivateFontCollection
$pfc.AddFontFile($fontFile)
$fam = @{}; foreach ($f in $pfc.Families) { $fam[$f.Name] = $f }
$TF = @($fam.Keys)[0]
$outDir = "$root\guide\images\titles"; New-Item -ItemType Directory -Force $outDir | Out-Null
$used = @{}
$md5 = [Security.Cryptography.MD5]::Create()
$u8 = New-Object Text.UTF8Encoding $false

$cs = @"
using System; using System.Drawing; using System.Drawing.Imaging; using System.Drawing.Text;
public static class TitleImg {
  // returns "w,h" in CSS px; image drawn at 3x, white on transparent, cropped to ink rows
  public static string Render(FontFamily ff, string text, double cssPx, string path) {
    int sc = 3, pad = 2 * sc;
    FontStyle st = ff.IsStyleAvailable(FontStyle.Regular) ? FontStyle.Regular : FontStyle.Bold;
    using (var fnt = new Font(ff, (float)(cssPx * sc), st, GraphicsUnit.Pixel)) {
      var fmt = (StringFormat)StringFormat.GenericTypographic.Clone();
      fmt.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
      SizeF sz; using (var t = new Bitmap(4, 4)) using (var g0 = Graphics.FromImage(t)) sz = g0.MeasureString(text, fnt, 100000, fmt);
      int w = (int)Math.Ceiling(sz.Width) + 2 * pad, h = (int)Math.Ceiling(fnt.GetHeight()) + 2 * pad;
      using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb)) {
        using (var g = Graphics.FromImage(bmp)) { g.Clear(Color.Transparent); g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit; g.DrawString(text, fnt, Brushes.White, pad, pad, fmt); }
        Func<int, bool> empty = y => { for (int x = 0; x < w; x++) if (bmp.GetPixel(x, y).A > 0) return false; return true; };
        int top = 0, bot = h - 1;
        while (top < bot && empty(top)) top++;
        while (bot > top && empty(bot)) bot--;
        top = Math.Max(0, top - pad); bot = Math.Min(h - 1, bot + pad);
        using (var crop = bmp.Clone(new Rectangle(0, top, w, bot - top + 1), bmp.PixelFormat)) crop.Save(path, ImageFormat.Png);
        return Math.Round(w / (double)sc, 1) + "," + Math.Round((bot - top + 1) / (double)sc, 1);
      }
    }
  }
}
"@
Add-Type -TypeDefinition $cs -ReferencedAssemblies System.Drawing
function Render([string]$text, [string]$famName, [double]$cssPx) {
  $key = [BitConverter]::ToString($md5.ComputeHash($u8.GetBytes("$famName|$cssPx|$text"))).Replace('-', '').Substring(0, 12).ToLower()
  $file = "t_$key.png"; $used[$file] = 1
  $wh = [TitleImg]::Render($fam[$famName], $text, $cssPx, "$outDir\$file") -split ','
  return @{ File = $file; W = $wh[0]; H = $wh[1] }
}
function Span($r, [string]$text, [string]$cls) {
  $u = "images/titles/$($r.File)"
  '<span class="ttl {0}" style="width:{1}px;height:{2}px;-webkit-mask-image:url({3});mask-image:url({3})" aria-hidden="true"></span><span class="sr">{4}</span>' -f $cls, $r.W, $r.H, $u, $text
}

$cache = @{}
$n = 0
foreach ($p in Get-ChildItem "$root\guide" -Filter *.html) {
  $html = [IO.File]::ReadAllText($p.FullName, $u8); $orig = $html
  # h1: 태그 없는 글자만 있는 제목
  $html = [regex]::Replace($html, '<h1>([^<]+)</h1>', {
    param($m)
    $raw = $m.Groups[1].Value; $text = [Net.WebUtility]::HtmlDecode($raw)
    $k = "h1|$text"; if (-not $cache[$k]) { $cache[$k] = Render $text $TF 26 }
    $pre = if ($text -eq '몬스터헌터2 (DOS) DB') { '<img class="mhl big" src="images/mh2_logo.png" alt="" aria-hidden="true">' } else { '' }
    '<h1 class="timg">' + $pre + (Span $cache[$k] $raw 'h') + '</h1>'
  })
  # 상단 로고
  $html = [regex]::Replace($html, '<a class="logo" href="index.html">([^<]+)</a>', {
    param($m)
    $raw = $m.Groups[1].Value; $text = [Net.WebUtility]::HtmlDecode($raw)
    $k = "logo|$text"; if (-not $cache[$k]) { $cache[$k] = Render $text $TF 16 }
    '<a class="logo timg" href="index.html"><img class="mhl" src="images/mh2_logo.png" alt="" aria-hidden="true">' + (Span $cache[$k] $raw 'l') + '</a>'
  })
  # 이미 바뀐 페이지(index.html처럼 빌드가 다시 만들지 않는 파일)에도 로고 이미지를 넣는다
  $img = '<img class="mhl" src="images/mh2_logo.png" alt="" aria-hidden="true">'
  $html = $html.Replace('<a class="logo timg" href="index.html"><span', '<a class="logo timg" href="index.html">' + $img + '<span')
  $html = [regex]::Replace($html, '<h1 class="timg">(<span class="ttl h"[^>]*></span><span class="sr">몬스터헌터2 \(DOS\) DB</span>)', '<h1 class="timg"><img class="mhl big" src="images/mh2_logo.png" alt="" aria-hidden="true">$1')
  # 이미 이미지로 바뀐 제목도 지금 글꼴로 다시 그린다 (글꼴을 바꿨을 때)
  $html = [regex]::Replace($html, '<span class="ttl (h|l)" style="[^"]*" aria-hidden="true"></span><span class="sr">([^<]+)</span>', {
    param($m)
    $cls = $m.Groups[1].Value; $raw = $m.Groups[2].Value; $text = [Net.WebUtility]::HtmlDecode($raw)
    $k = $(if ($cls -eq 'h') { "h1|$text" } else { "logo|$text" })
    if (-not $cache[$k]) { $cache[$k] = Render $text $TF $(if ($cls -eq 'h') { 26 } else { 16 }) }
    Span $cache[$k] $raw $cls
  })
  if ($html -ne $orig) { [IO.File]::WriteAllText($p.FullName, $html, $u8); $n++ }
}
# 이미 바뀐 페이지가 쓰는 이미지도 사용 중으로 친다 (다시 실행해도 지우지 않게)
foreach ($p in Get-ChildItem "$root\guide" -Filter *.html) { foreach ($m in [regex]::Matches([IO.File]::ReadAllText($p.FullName, $u8), 'images/titles/(t_[0-9a-f]+\.png)')) { $used[$m.Groups[1].Value] = 1 } }
# 더 이상 쓰지 않는 제목 이미지 정리
Get-ChildItem $outDir -Filter 't_*.png' | Where-Object { -not $used[$_.Name] } | Remove-Item
"title images: $($used.Count) files, $n pages (fonts: $(($fam.Keys) -join ', '))"
