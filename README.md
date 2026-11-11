# 몬스터헌터2 (DOS) DB

몬스터헌터 2 (도스, PS2)의 한국어 공략 사이트입니다. 용어는 「몬스터헌터 2 (도스) 한글 패치」 표기를 따릅니다.

- 사이트: https://xofldla.github.io/mh2-dos-db/ (`guide/` 폴더, 인터넷 없이 열어도 동작)
- 무기 강화 트리, 방어구 세트・발동 스킬, 스킬, 아이템, 몬스터, 퀘스트, 맵 채집(필터・확대), 식사, 헌터 칭호, 마을 사람과의 교류

## 폴더

| 폴더 | 내용 |
| --- | --- |
| `guide/` | 완성된 사이트. `main` 브랜치에 올리면 GitHub Pages 로 자동 배포 (`.github/workflows/pages.yml`) |
| `work/guide/` | 사이트를 만드는 스크립트 (`build_mhos.ps1`, `build_villagepeople.ps1`, `tools/*.cs`), 공략 전용 번역표 (`terms_manual.tsv`, `prose_ko.tsv`) |

## 다시 만들기 (Windows PowerShell)

```powershell
powershell -NoProfile -File work\guide\build_mhos.ps1
powershell -NoProfile -File work\guide\build_villagepeople.ps1
```

고친 뒤 GitHub 에 올리기(빌드 + 커밋 + push, Pages 자동 배포):

```powershell
powershell -NoProfile -File work\guide\publish.ps1 -Message "수정 내용"
```

빌드에는 이 저장소에 없는 입력 자료가 필요합니다: 위키 데이터(`sample/json/*.json`), 위키 원본 페이지(`work/guide/src/`), 한글 패치 번역표(`work/trans/*.tsv`). 저작권・배포 문제로 저장소에는 결과물과 스크립트만 올립니다.

## 자료 출처와 라이선스

- 수치・데이터: [Monster Hunter Oldschool MH2 Wiki](https://wiki.mholdschool.com/mh2/), mmPotato 「モンスターハンター２解析情報」
- 지도・아이콘・몬스터 그림: 같은 위키 (몬스터 그림은 크기를 줄임)
- 지역・마을 아이콘: 이 공략을 위해 새로 그린 픽셀 그림
- 글꼴: Galmuri11 (quiple, SIL Open Font License 1.1) — `guide/fonts/LICENSE_Galmuri.txt`
- 게임 이름・용어의 권리는 CAPCOM 에 있습니다. 비영리 팬 공략입니다.
