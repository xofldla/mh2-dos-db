# mholdschool MH2 위키 JSON(sample\json) → guide\*.html (한글 패치 용어)
# 사용: powershell -NoProfile -File work\guide\build_mhos.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not ('G' -as [type])) {
  Add-Type -Path "$PSScriptRoot\tools\Dict.cs", "$PSScriptRoot\tools\HtmlGrid.cs", "$PSScriptRoot\tools\ArmorSets.cs", "$PSScriptRoot\tools\WeaponTree.cs", "$PSScriptRoot\tools\GuideBuild.cs" -ReferencedAssemblies System.Web.Extensions, System.Core
}
. "$PSScriptRoot\load_dict.ps1"
[G]::JsonDir = "$root\sample\json"
[G]::OutDir = "$root\guide"
[G]::Prose.Clear(); [G]::EnName.Clear(); [G]::MissProse.Clear()
[G]::LoadProse("$PSScriptRoot\prose_ko.tsv")
foreach ($f in 'armor_skills','items','monsters','weapons','armor','quests','meals','maps') { [G]::CollectNames([G]::Load($f)) }

# 폰트(Spoqa Han Sans Neo, OFL — 2026-11-23 Galmuri11에서 변경)와 라이선스. Bold 대신 Medium을 굵은 글씨로 씀
$fd = "$root\guide\fonts"; New-Item -ItemType Directory -Force $fd | Out-Null
$sp = "$root\work\font\SpoqaHanSansNeo_all"
foreach ($p in @("$sp\SpoqaHanSansNeo_TTF_subset\SpoqaHanSansNeo-Regular.woff2", "$sp\SpoqaHanSansNeo_TTF_subset\SpoqaHanSansNeo-Medium.woff2", "$sp\LICENSE", "$root\work\font\WantedSans-1.0.3\webfonts\static\complete\woff2\WantedSans-Regular.woff2", "$root\work\font\WantedSans-1.0.3\webfonts\static\complete\woff2\WantedSans-Medium.woff2", "$root\work\font\WantedSans-1.0.3\OFL.txt")) {
  $n = Split-Path -Leaf $p; $dst = Join-Path $fd ($(if ($n -eq 'LICENSE') { 'LICENSE_SpoqaHanSansNeo.txt' } elseif ($n -eq 'OFL.txt') { 'LICENSE_WantedSans.txt' } else { $n }))
  if (-not (Test-Path $dst)) { Copy-Item $p $dst }
}

[G]::BuildLinks(); [G]::IconCss("$PSScriptRoot\icon_pos.txt"); [G]::Weapons(); [G]::WeaponsTree("$PSScriptRoot\src\mhos\weapons.html", "$PSScriptRoot\src\mhos\weapon_tree_codes.txt"); [G]::Armor(); [G]::Skills(); [G]::Items(); [G]::Bestiary(); [G]::Quests(); [G]::WriteScheduleQuests(); [G]::Maps(); [G]::Meals(); [G]::Misc("$PSScriptRoot\src\mhos\miscellany.html")
"pages done"
$miss = @([G]::MissProse)
"untranslated prose: $($miss.Count)"
[IO.File]::WriteAllLines("$PSScriptRoot\prose_missing.txt", [string[]]$miss, (New-Object Text.UTF8Encoding $true))
