. .\work\guide\load_dict.ps1
"dict " + [Dict]::Map.Count
$all = New-Object 'System.Collections.Generic.Dictionary[string,string]'
foreach($f in 'armor_skills','items','maps','meals','monsters','quests','weapons','armor'){
  $o=[Dict]::LoadJson((Resolve-Path "sample\json\$f.json").Path)
  $m = New-Object 'System.Collections.Generic.Dictionary[string,string]'
  [Dict]::Misses($o,$m); "$f miss $($m.Count)"
  foreach($k in $m.Keys){ if(-not $all.ContainsKey($k)){ $all[$k]="$f`t"+$m[$k] } }
}
$lines = foreach($k in $all.Keys){ "$k`t$($all[$k])" }
[IO.File]::WriteAllLines("$PWD\work\guide\misses.tsv", $lines, (New-Object Text.UTF8Encoding $true))
"total $($all.Count)"