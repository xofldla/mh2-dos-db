// MH Oldschool 위키(https://wiki.mholdschool.com/mh2/)의 온라인 일정 자료(window.calendarData)를 받아
// guide/assets/schedule-data.js 로 쓴다. 자료가 바뀌었을 때만 파일을 고친다(바뀌면 종료 코드 0 + "changed" 출력).
// 사용: node work/guide/fetch_schedule.mjs   (GitHub Actions에서 1시간마다, 로컬에서도 같은 명령)
import { readFileSync, writeFileSync, existsSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const root = join(dirname(fileURLToPath(import.meta.url)), "..", "..");
const out = join(root, "guide", "assets", "schedule-data.js");
const res = await fetch("https://wiki.mholdschool.com/mh2/", { headers: { "User-Agent": "mh2-dos-db schedule updater" } });
if (!res.ok) { console.error("wiki HTTP " + res.status); process.exit(1); }
const html = await res.text();
const key = "window.calendarData=";
const i = html.indexOf(key);
if (i < 0) { console.error("calendarData not found (wiki page changed?)"); process.exit(1); }
// 문자열 안의 괄호는 건너뛰며 짝이 맞는 } 까지 자른다
let s = i + key.length, depth = 0, inStr = false, esc = false, e = -1;
for (let k = s; k < html.length; k++) {
  const ch = html[k];
  if (inStr) { if (esc) esc = false; else if (ch === "\\") esc = true; else if (ch === '"') inStr = false; continue; }
  if (ch === '"') inStr = true; else if (ch === "{") depth++; else if (ch === "}") { if (--depth === 0) { e = k; break; } }
}
if (e < 0) { console.error("calendarData end not found"); process.exit(1); }
const data = JSON.parse(html.slice(s, e + 1));
if (!data.events || !data.eventsRotation || !data.shopSpecials || !data.shopSpecialRotation) { console.error("unexpected calendarData shape"); process.exit(1); }
const tz = (html.match(/SERVER_TZ="([^"]+)"/) || [])[1] || "Europe/Berlin";
const startHour = +((html.match(/START_HOUR=(\d+)/) || [])[1] || 0);
const body = JSON.stringify(data);
const meta = { tz, startHour };
if (existsSync(out)) {
  const old = readFileSync(out, "utf8");
  const m = old.match(/^window\.CAL=(.*);$/m), mm = old.match(/^window\.CAL_META=(.*);$/m);
  if (m && mm) {
    const om = JSON.parse(mm[1]);
    if (m[1] === body && om.tz === tz && om.startHour === startHour) { console.log("unchanged"); process.exit(0); }
  }
}
meta.updated = new Date().toISOString();
writeFileSync(out, "// 자동 생성: work/guide/fetch_schedule.mjs — 출처 MH Oldschool MH2 Wiki (wiki.mholdschool.com/mh2/)\nwindow.CAL=" + body + ";\nwindow.CAL_META=" + JSON.stringify(meta) + ";\n");
console.log("changed");
