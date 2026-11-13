# 공략 사이트를 다시 만들고 GitHub(main)에 올린다 → GitHub Pages 자동 배포
# 사용: powershell -NoProfile -File work\guide\publish.ps1 -Message "무기 트리 수정"
param([string]$Message = "공략 사이트 갱신")
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot); Set-Location $root
$env:Path = [Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [Environment]::GetEnvironmentVariable('Path','User')

powershell -NoProfile -File "$PSScriptRoot\build_mhos.ps1"
powershell -NoProfile -File "$PSScriptRoot\build_villagepeople.ps1"
powershell -NoProfile -File "$PSScriptRoot\title_images.ps1"
# 장비 시뮬레이터 스크립트 주소에 버전 꼬리표(?v=)를 새로 붙여, 방문자 브라우저가 예전 JS를 계속 쓰지 않게 한다
$simV = (Get-Date).ToString('yyyyMMddHHmm'); $u8n = New-Object Text.UTF8Encoding $false
foreach ($sf in "$root\guide\simulator.html", "$root\guide\assets\sim.js", "$root\guide\assets\sim-worker.js") {
  if (Test-Path $sf) { $tx = [IO.File]::ReadAllText($sf, $u8n); $nx = [regex]::Replace($tx, '(sim(-data|-core|-worker)?\.js)\?v=\d+', ('$1?v=' + $simV)); if ($nx -ne $tx) { [IO.File]::WriteAllText($sf, $nx, $u8n) } }
}

git add -A
if (-not (git status --porcelain)) { "바뀐 것이 없습니다."; return }
git -c core.quotepath=false status --short
git commit -q -m $Message
git push -q origin main
"올렸습니다: https://github.com/xofldla/mh2-dos-db  → 사이트: https://xofldla.github.io/mh2-dos-db/ (1~2분 뒤 반영)"
