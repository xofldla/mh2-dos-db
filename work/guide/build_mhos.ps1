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

# 폰트(Galmuri11, OFL)와 라이선스
$fd = "$root\guide\fonts"; New-Item -ItemType Directory -Force $fd | Out-Null
foreach ($n in 'Galmuri11.woff2','Galmuri11-Bold.woff2','LICENSE.txt') {
  $dst = Join-Path $fd ($(if ($n -eq 'LICENSE.txt') { 'LICENSE_Galmuri.txt' } else { $n }))
  if (-not (Test-Path $dst)) { Copy-Item "$root\font\$n" $dst }
}

[G]::BuildLinks(); [G]::IconCss("$PSScriptRoot\icon_pos.txt"); [G]::Weapons(); [G]::WeaponsTree("$PSScriptRoot\src\mhos\weapons.html", "$PSScriptRoot\src\mhos\weapon_tree_codes.txt"); [G]::Armor(); [G]::Skills(); [G]::Items(); [G]::Bestiary(); [G]::Quests(); [G]::Maps(); [G]::Meals(); [G]::Misc("$PSScriptRoot\src\mhos\miscellany.html")
"pages done"
$miss = @([G]::MissProse)
"untranslated prose: $($miss.Count)"
[IO.File]::WriteAllLines("$PSScriptRoot\prose_missing.txt", [string[]]$miss, (New-Object Text.UTF8Encoding $true))
