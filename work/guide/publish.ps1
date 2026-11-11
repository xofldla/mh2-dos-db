# 공략 사이트를 다시 만들고 GitHub(main)에 올린다 → GitHub Pages 자동 배포
# 사용: powershell -NoProfile -File work\guide\publish.ps1 -Message "무기 트리 수정"
param([string]$Message = "공략 사이트 갱신")
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot); Set-Location $root
$env:Path = [Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [Environment]::GetEnvironmentVariable('Path','User')

powershell -NoProfile -File "$PSScriptRoot\build_mhos.ps1"
powershell -NoProfile -File "$PSScriptRoot\build_villagepeople.ps1"

git add -A
if (-not (git status --porcelain)) { "바뀐 것이 없습니다."; return }
git -c core.quotepath=false status --short
git commit -q -m $Message
git push -q origin main
"올렸습니다: https://github.com/xofldla/mh2-dos-db  → 사이트: https://xofldla.github.io/mh2-dos-db/ (1~2분 뒤 반영)"
