// 사이트 일본어 글꼴(M PLUS Rounded 1c, OFL 1.1, 예약 글꼴 이름 없음)을 사이트에 실제 쓰인 글자만 남겨 woff2로 자른다. (2026-11-23)
// 사용: node work/guide/subset_ja.mjs   (publish.ps1에서 제목 이미지 다음에 실행)
// subset-font는 work/fonts_ext/node(git 제외)에 설치: cd work/fonts_ext/node; npm install subset-font@2 --ignore-scripts
import { createRequire } from "module";
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..", "..");
const require = createRequire(path.join(root, "work", "fonts_ext", "node", "package.json"));
const subsetFont = require("subset-font");

const guide = path.join(root, "guide");
const files = [];
for (const d of [guide, path.join(guide, "assets")])
  for (const f of fs.readdirSync(d)) if (/\.(html|js)$/.test(f)) files.push(path.join(d, f));

// 한글(스포카 담당)을 뺀 모든 글자 + 가나 전체 + ASCII
const set = new Set();
for (const f of files) for (const ch of fs.readFileSync(f, "utf8")) {
  const c = ch.codePointAt(0);
  if (c >= 0xac00 && c <= 0xd7a3) continue;
  if (c >= 0x1100 && c <= 0x11ff) continue;
  if (c >= 0x3130 && c <= 0x318f) continue;
  if (c < 0x20) continue;
  set.add(ch);
}
for (let c = 0x20; c <= 0x7e; c++) set.add(String.fromCodePoint(c));
for (let c = 0x3040; c <= 0x30ff; c++) set.add(String.fromCodePoint(c));
const text = [...set].join("");

const src = path.join(root, "work", "font", "M_PLUS_Rounded_1c");
const out = path.join(guide, "fonts");
for (const w of ["Regular", "Medium"]) {
  const buf = await subsetFont(fs.readFileSync(path.join(src, `MPLUSRounded1c-${w}.ttf`)), text, { targetFormat: "woff2" });
  fs.writeFileSync(path.join(out, `MPLUSRounded1c-${w}.woff2`), buf);
  console.log(`MPLUSRounded1c-${w}.woff2 ${buf.length} bytes`);
}
fs.copyFileSync(path.join(src, "OFL.txt"), path.join(out, "LICENSE_MPLUSRounded1c.txt"));
console.log(`ja font subset: ${set.size} chars`);
