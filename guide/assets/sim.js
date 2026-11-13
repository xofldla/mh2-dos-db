/* 장비 시뮬레이터: 화면 */
(function () {
  "use strict";
  var D = window.SIMD, C = window.SimCore;
  var $ = function (id) { return document.getElementById(id); };
  var esc = function (s) { return String(s).replace(/[&<>"]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]; }); };
  var PART = ["머리", "몸통", "팔", "허리", "다리", "무기"];
  var RES = [["fire", "화"], ["water", "수"], ["thunder", "뇌"], ["ice", "빙"], ["dragon", "용"]];
  var store = { get: function (k) { try { return localStorage.getItem(k); } catch (e) { return null; } }, set: function (k, v) { try { localStorage.setItem(k, v); } catch (e) {} } };

  // 고를 수 있는 발동 스킬 (+ 단계만)
  var stages = [];
  D.s.forEach(function (s, sk) { s[1].forEach(function (x) { if (x[0] > 0) stages.push({ sk: sk, pts: x[0], n: x[1], tree: s[0] }); }); });
  stages.sort(function (a, b) { return a.tree.localeCompare(b.tree, "ko") || a.pts - b.pts; });
  var picked = [];

  function renderList() {
    var q = $("skq").value.trim();
    var list = stages.filter(function (s) { return !q || s.n.indexOf(q) >= 0 || s.tree.indexOf(q) >= 0; });
    $("sklist").innerHTML = list.map(function (s) {
      var on = picked.some(function (p) { return p.sk === s.sk && p.pts === s.pts; });
      return '<button type="button" data-sk="' + s.sk + '" data-p="' + s.pts + '"' + (on ? ' style="border-color:var(--acc);color:var(--acc)"' : "") + ">" + esc(s.n) + ' <span class="tr">' + esc(s.tree) + " " + s.pts + "</span></button>";
    }).join("") || '<span class="note">맞는 스킬이 없습니다.</span>';
  }
  function renderChips() {
    $("chips").innerHTML = picked.length ? picked.map(function (p, i) {
      return '<span class="chip">' + esc(p.n) + ' <span class="tr">(' + esc(p.tree) + " " + p.pts + ')</span></span>';
    }).join("") : '<span class="note">위 목록에서 눌러 추가하세요 (최대 10개). 한 번 더 누르면 빠집니다.</span>';
    $("go").disabled = !picked.length;
    store.set("mh2-sim-skills", JSON.stringify(picked.map(function (p) { return [p.sk, p.pts]; })));
    renderList();
  }
  $("sklist").addEventListener("click", function (e) {
    var b = e.target.closest("button[data-sk]"); if (!b) return;
    var sk = +b.getAttribute("data-sk"), pts = +b.getAttribute("data-p");
    var st = stages.filter(function (s) { return s.sk === sk && s.pts === pts; })[0];
    var i = picked.findIndex(function (p) { return p.sk === sk; });
    if (i >= 0 && picked[i].pts === pts) picked.splice(i, 1); /* 같은 스킬을 다시 누르면 뺀다 */ else if (i >= 0) picked[i] = st; else if (picked.length < 10) picked.push(st);
    renderChips();
  });
 $("skq").addEventListener("input", renderList);
  $("reset").addEventListener("click", function () { picked = []; $("skq").value = ""; renderChips(); });

  // 조건
  var maxRar = Math.max.apply(null, D.a.map(function (a) { return a[2]; }));
  for (var r = maxRar; r >= 1; r--) $("rar").insertAdjacentHTML("beforeend", '<option value="' + r + '">레어 ' + r + "</option>");
  function radio(n) { return +document.querySelector('input[name="' + n + '"]:checked').value; }
  var saved = {}; try { saved = JSON.parse(store.get("mh2-sim-opt") || "{}"); } catch (e) {}
  if (saved.cls !== undefined) document.querySelector('input[name="cls"][value="' + saved.cls + '"]').checked = true;
  if (saved.sex !== undefined) document.querySelector('input[name="sex"][value="' + saved.sex + '"]').checked = true;
  if (saved.rar) $("rar").value = saved.rar; if (saved.ws !== undefined) $("ws").value = saved.ws;
  if (saved.noneg) $("noneg").checked = true; if (saved.sort) $("sort").value = saved.sort;
  try { JSON.parse(store.get("mh2-sim-skills") || "[]").slice(0, 10).forEach(function (x) { var st = stages.filter(function (s) { return s.sk === x[0] && s.pts === x[1]; })[0]; if (st) picked.push(st); }); } catch (e) {}
  renderChips();

  // 검색
  var worker = null; try { worker = new Worker("assets/sim-worker.js"); } catch (e) { worker = null; }
  var last = null, shown = 0;
  function opts() {
    var o = { targets: picked.map(function (p) { return { sk: p.sk, pts: p.pts }; }), cls: radio("cls"), sex: radio("sex"), maxRar: +$("rar").value, wslots: +$("ws").value };
    store.set("mh2-sim-opt", JSON.stringify({ cls: o.cls, sex: o.sex, rar: o.maxRar, ws: o.wslots, noneg: $("noneg").checked, sort: $("sort").value }));
    return o;
  }
  function done(r, o) {
    $("go").disabled = false;
    var list = r.sols.map(function (s) { return { s: s, e: C.evaluate(D, s, o.wslots) }; });
    if ($("noneg").checked) list = list.filter(function (x) { return !x.e.neg; });
    var tset = {}; o.targets.forEach(function (t) { tset[t.sk] = 1; });
    list.forEach(function (x) { x.rar = Math.max.apply(null, x.s.p.map(function (i) { return D.a[i][2]; })); x.extra = x.e.act.filter(function (a) { return a.p > 0 && !tset[a.sk]; }).length; });
    var by = $("sort").value;
    list.sort(function (a, b) {
      if (by === "free") return b.e.free - a.e.free || b.e.def1 - a.e.def1;
      if (by === "rar") return a.rar - b.rar || b.e.def1 - a.e.def1;
      if (by === "extra") return b.extra - a.extra || b.e.def1 - a.e.def1;
      return b.e.def1 - a.e.def1 || b.e.free - a.e.free;
    });
    last = { list: list, o: o, tset: tset }; shown = 0;
    $("stat").textContent = list.length ? list.length + "개 조합" + (r.stopped ? " (많아서 일부만)" : "") + " · " + r.ms + "ms" : "";
    $("out").innerHTML = list.length ? "" : (r.stopped ? '<div class="sim-box">검색 시간(12초)을 넘어 끝까지 찾지 못했습니다. 조합이 없을 가능성이 높습니다. 스킬을 하나 줄이거나 단계를 낮춰 보세요.</div>' : '<div class="sim-box">조건을 만족하는 조합이 없습니다. 레어 제한을 올리거나, 무기 슬롯을 늘리거나, 스킬을 줄여 보세요.</div>');
    more();
  }
  function more() {
    var L = last.list, o = last.o, html = "", n = Math.min(L.length, shown + 30);
    for (var k = shown; k < n; k++) html += card(L[k], k + 1, o);
    shown = n;
    var mb = $("more"); if (mb) mb.remove();
    $("out").insertAdjacentHTML("beforeend", html + (shown < L.length ? '<p><button id="more" type="button">더 보기 (' + (L.length - shown) + "개 남음)</button></p>" : ""));
  }
  $("out").addEventListener("click", function (e) { if (e.target.id === "more") more(); });
  function slots(n) { return n > 0 ? new Array(n + 1).join("◯") : "-"; }
  function card(x, no, o) {
    var s = x.s, e = x.e, byG = {};
    s.f.forEach(function (g) { byG[g.g] = (byG[g.g] || []).concat(g.d); });
    var deco = function (g) {
      var cnt = {}; (byG[g] || []).forEach(function (di) { cnt[di] = (cnt[di] || 0) + 1; });
      return Object.keys(cnt).map(function (di) { var d = D.d[di]; return '<a class="xlink" href="' + d[4] + '">' + esc(d[0]) + "</a>" + (cnt[di] > 1 ? " ×" + cnt[di] : ""); }).join(", ");
    };
    var rows = s.p.map(function (ai, p) {
      var a = D.a[ai], link = o.cls === 0 ? (a[11] || a[12]) : (a[12] || a[11]);
      return '<tr><td class="pt">' + PART[p] + '</td><td><a class="xlink" href="' + link + '">' + esc(a[1]) + "</a>" + (a[10] ? ' <span class="note">(동계통배가)</span>' : "") + ' <span class="meta">레어' + a[2] + '</span></td><td class="sl">' + slots(a[7]) + '</td><td class="dc">' + deco(p) + "</td></tr>";
    }).join("");
    if (o.wslots > 0) rows += '<tr><td class="pt">무기</td><td><span class="note">(무기 슬롯)</span></td><td class="sl">' + slots(o.wslots) + '</td><td class="dc">' + deco(5) + "</td></tr>";
    var res = RES.map(function (r, i) { return '<span class="res ' + r[0] + (e.res[i] === 0 ? " z" : "") + '">' + e.res[i] + "</span>"; }).join(" ");
    var acts = e.act.map(function (a) { return '<span class="act' + (a.p < 0 ? " neg" : last.tset[a.sk] ? " tg" : "") + '" title="' + esc(D.s[a.sk][0]) + " " + (a.v > 0 ? "+" : "") + a.v + '">' + esc(a.n) + "</span>"; }).join("");
    var desc = e.act.map(function (a) {
      var tr = D.s[a.sk], stg = tr[1].filter(function (x) { return x[0] === a.p; })[0], eff = stg && stg[2] ? stg[2] : "";
      return '<li class="' + (a.p < 0 ? "neg" : last.tset[a.sk] ? "tg" : "pos") + '"><b>' + esc(a.n) + '</b> <span class="meta">(' + esc(tr[0]) + " " + (a.v > 0 ? "+" : "") + a.v + ")</span>" + (eff ? ' <span class="eff">' + esc(eff) + "</span>" : "") + (tr[2] ? ' <span class="desc">' + esc(tr[2]) + "</span>" : "") + "</li>";
    }).join("");
    return '<div class="sol"><div class="hd"><span class="no">#' + no + "</span><span>방어력 " + e.def0 + " → <b>" + e.def1 + "</b></span><span>내성 " + res + "</span><span>남는 슬롯 " + e.free + "</span></div><table>" + rows + '</table><div class="acts">' + acts + '</div>' + (desc ? '<details class="skd" open><summary>발동 스킬 효과</summary><ul>' + desc + "</ul></details>" : "") + "</div>";
  }
  $("go").addEventListener("click", function () {
    if (!picked.length) return;
    var o = opts(); $("go").disabled = true; $("stat").textContent = "검색 중…"; $("out").innerHTML = "";
    if (worker) {
      worker.onmessage = function (m) { if (m.data.t === "p") $("stat").textContent = "검색 중… " + m.data.s + "개 찾음"; else done(m.data.r, o); };
      worker.onerror = function () { worker = null; setTimeout(function () { done(C.search(D, o), o); }, 10); };
      worker.postMessage(o);
    } else setTimeout(function () { done(C.search(D, o), o); }, 10);
  });
  $("sort").addEventListener("change", function () { if (last) { $("go").click(); } });
})();
