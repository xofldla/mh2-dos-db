# 한글 패치 TSV → 공략용 ja→ko 사전 (다른 빌드 스크립트가 . 으로 불러 씀)
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not ('Dict' -as [type])) { Add-Type -Path "$PSScriptRoot\tools\Dict.cs" -ReferencedAssemblies System.Web.Extensions }
[Dict]::Map.Clear(); [Dict]::Src.Clear()
# 공략 전용 수동 번역이 가장 우선
foreach ($m in @("$PSScriptRoot\terms_manual.tsv")) { if (Test-Path $m) { [Dict]::LoadTsv($m) } }
$order = @('ov0_items_names','ov0_equip_names','equip_new_terms','items_new_terms','ov0_titles','ov0_areas','ov0_misc','ov0_misc6',
  'ov0_ui1','ov0_ui2','ov0_ui3','ov0_ui4','ov0_ui5','ov0_items_desc1','ov0_items_desc2','ov0_items_desc3','ov0_items_desc4',
  'ov0_items_desc5','ov0_items_desc6','ov0_items_desc_pots','ov0_ecology','ov0_deco_gen','ov0_missed','ov0_equip_desc1',
  'ov0_equip_desc2','ov0_equip_desc3','ov0_equip_desc4','ov0_equip_desc5','ov0_equip_desc6','ov0_equip_desc7','ov0_equip_desc8',
  'ov1_select','ov9_expanded','ov9_dict1','ov9_dict2','ov9_dict3','ov9_dict4','ov3_b2','ov3_b3','ov3_b4','ov3_b5','ov3_b6',
  'ov3_b7','ov3_b8','ov3_b9','ov3_b10','ov3_b11','ov3_b12','ov3_b13','ov3_b14','ov3_house','ov3_misc','ov3_npcnames',
  'ov2_b1','ov2_b2','ov2_b3','ov2_b4','ov8_net','dict_ov3_b14')
foreach ($n in $order) { [Dict]::LoadTsv("$root\work\trans\$n.tsv") }
# 패치에 없는 것만 공략 전용 수동 번역으로 채움
[Dict]::LoadTsv("$PSScriptRoot\terms_manual.tsv")
