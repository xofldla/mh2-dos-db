// 몬스터헌터 2 공략 — 검색・일본어 표시 (오프라인 동작)
(function () {
  var store = { get: function (k) { try { return localStorage.getItem(k); } catch (e) { return null; } },
                set: function (k, v) { try { localStorage.setItem(k, v); } catch (e) {} } };

  // 맨 위로 버튼 (오른쪽 아래, 조금 내려가면 나타남)
  var top = document.createElement("button");
  top.type = "button"; top.id = "totop"; top.title = "맨 위로"; top.setAttribute("aria-label", "맨 위로");
  top.innerHTML = '<span class="ar">▲</span><span class="lb">TOP</span>';
  top.addEventListener("click", function () { window.scrollTo({ top: 0, behavior: "smooth" }); });
  document.body.appendChild(top);
  var onScroll = function () { top.classList.toggle("show", window.scrollY > 600); };
  window.addEventListener("scroll", onScroll, { passive: true }); onScroll();

  // 열어 둔 창을 주소(#)에 기억 → 다른 페이지 갔다가 「돌아가기」로 오면 다시 열림
  function setHash(h) { try { history.replaceState(null, "", location.pathname + location.search + (h ? "#" + h : "")); } catch (e) {} }
  window.setHash = setHash;
  // 다른 페이지로 가는 링크(소재・몬스터)를 누르면 표시 → 도착한 페이지에 「돌아가기」 버튼
  document.addEventListener("click", function (e) {
    var a = e.target.closest ? e.target.closest("a.xlink") : null; if (!a) return;
    // 도착 페이지 주소에 bk=1 을 붙인다 (저장소・캐시와 상관없이 몇 번을 오가도 버튼이 나오게)
    var href = a.getAttribute("href"), hash = "", i = href.indexOf("#");
    if (i >= 0) { hash = href.slice(i); href = href.slice(0, i); }
    if (!/[?&]bk=1/.test(href)) href += (href.indexOf("?") >= 0 ? "&" : "?") + "bk=1";
    a.setAttribute("href", href + hash);
    try { window.name = (window.name || "").replace(/;?mh2back=1/g, "") + ";mh2back=1"; } catch (x) {}
    try { sessionStorage.setItem("mh2back", "1"); } catch (x) {}
  }, true);
  var cameBack = /[?&]bk=1/.test(location.search) || /mh2back=1/.test(window.name || ""); try { if (sessionStorage.getItem("mh2back") === "1") cameBack = true; sessionStorage.removeItem("mh2back"); } catch (x) {}
  try { window.name = (window.name || "").replace(/;?mh2back=1/g, ""); } catch (x) {}
  if (cameBack && history.length > 1) {
    var back = document.createElement("button"); back.type = "button"; back.id = "backbtn"; back.innerHTML = "↩ 돌아가기";
    back.title = "보던 화면으로 돌아가기"; back.addEventListener("click", function () { history.back(); });
    document.body.appendChild(back);
  }

  var ja = document.getElementById("showja");
  if (ja) {
    ja.checked = store.get("mh2-showja") === "1";
    document.body.classList.toggle("showja", ja.checked);
    ja.addEventListener("change", function () { document.body.classList.toggle("showja", ja.checked); store.set("mh2-showja", ja.checked ? "1" : "0"); });
  }

  // 맵: 낮/밤(구조) 탭
  function showVariant(sec, v) {
    if (!sec) return;
    [].forEach.call(sec.querySelectorAll(".vtabs button"), function (b) { var on = b.getAttribute("data-v") === v; b.classList.toggle("on", on); b.setAttribute("aria-selected", on ? "true" : "false"); });
    [].forEach.call(sec.querySelectorAll(".variant"), function (d) { d.classList.toggle("off", d.getAttribute("data-v") !== v); });
  }
  function showTime(t) {
    [].forEach.call(document.querySelectorAll("section.loc"), function (sec) {
      var b = sec.querySelector('.vtabs button[data-t="' + t + '"]'); if (b) showVariant(sec, b.getAttribute("data-v"));
    });
    [].forEach.call(document.querySelectorAll(".daynight button"), function (b) { b.classList.toggle("on", b.getAttribute("data-all") === t); });
    store.set("mh2-time", t);
  }
  document.addEventListener("click", function (e) {
    var b = e.target.closest ? e.target.closest("section.loc .vtabs button, .daynight button") : null; if (!b) return;
    if (b.hasAttribute("data-all")) showTime(b.getAttribute("data-all"));
    else showVariant(b.closest("section.loc"), b.getAttribute("data-v"));
  });
  if (document.querySelector(".daynight")) {
    var t0 = store.get("mh2-time"); showTime(t0 === "night" ? "night" : "day");
    // 링크로 숨은 탭 안의 대상에 오면 그 탭을 연다
    var openHash = function () {
      var id = location.hash ? decodeURIComponent(location.hash.slice(1)) : ""; var el = id && document.getElementById(id);
      var v = el && (el.classList.contains("variant") ? el : el.closest(".variant"));
      if (v) { showVariant(v.closest("section.loc"), v.getAttribute("data-v")); el.scrollIntoView(); }
    };
    window.addEventListener("hashchange", openHash); openHash();
  }

  // 맵 채집: 계절(1차) → 지역 → 시간대・등급・종류 필터, 아이템 검색, 아이콘 눌러 채집 창
  if (window.MP) (function () {
    var st = { s: 0, r: 0, q: "", off: {} };
    try { var sv = JSON.parse(store.get("mh2-mapf") || "{}"); if (sv.s >= 0) st.s = sv.s; if (sv.r >= 0) st.r = sv.r; } catch (e) {}
    var $$ = function (sel, root) { return [].slice.call((root || document).querySelectorAll(sel)); };
    var esc = function (s) { return String(s).replace(/[&<>"]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]; }); };
    var ico = function (cls) { var m = /^(\w+)-icon ([\w-]+-icon)/.exec(cls || ""); return m ? '<i class="ico ' + m[1] + "-icon " + m[2] + '"></i>' : ""; };
    function hasItem(d) {
      var rr = d.r[st.r]; if (!rr || !st.q) return false;
      return rr[1 + st.s].some(function (x) { return x[0].toLowerCase().indexOf(st.q) >= 0; });
    }
    function apply() {
      $$(".mseason .se").forEach(function (b) { b.classList.toggle("on", +b.getAttribute("data-s") === st.s); });
      $$(".mchip.rk").forEach(function (b) { b.classList.toggle("on", +b.getAttribute("data-r") === st.r); });
      $$(".mchip.kd").forEach(function (l) { var k = l.querySelector("input").getAttribute("data-k"); l.classList.toggle("on", !st.off[k]); l.querySelector("input").checked = !st.off[k]; });
      var hitBy = {}, total = 0;
      $$(".mapbox .pt, .xpt").forEach(function (b) {
        var d = window.MP[b.getAttribute("data-p")], k = b.getAttribute("data-k");
        b.classList.toggle("kout", k !== null && !!st.off[k]);
        var na = !d || !d.r[st.r]; b.classList.toggle("na", na);
        var hit = !na && hasItem(d); b.classList.toggle("hit", hit); b.classList.toggle("dim", !!st.q && !hit);
        if (hit) { var loc = b.closest("section.loc").id; hitBy[loc] = (hitBy[loc] || 0) + 1; total++; }
      });
      $$(".mregion .rg").forEach(function (b) { var n = hitBy[b.getAttribute("data-loc")] || 0; var i = b.querySelector(".hitn"); i.textContent = st.q && n ? n : ""; b.classList.toggle("has", !!(st.q && n)); });
      document.getElementById("mqn").textContent = st.q ? total + "곳" : "";
      store.set("mh2-mapf", JSON.stringify({ s: st.s, r: st.r }));
    }
    function showRegion(id) {
      $$(".mregion .rg").forEach(function (b) { b.classList.toggle("on", b.getAttribute("data-loc") === id); });
      $$("section.loc").forEach(function (s) { s.classList.toggle("off", s.id !== id); });
      store.set("mh2-maprg", id);
    }
    function openP(pid) {
      var d = window.MP[pid]; if (!d) return;
      var pop = document.getElementById("wpop"), body = pop.querySelector(".wp-body");
      var h = '<div class="wd-head">' + (d.i ? ico(d.i).replace('class="ico ', 'class="ico big ') : "") + '<div><b class="wd-name">' + (d.no ? d.no + ". " : "") + esc(d.n) + '</b><div class="meta">' + esc(d.m) + " · " + esc(d.z) + "</div></div></div>";
      h += '<div class="vtabs prk">' + window.MPR.map(function (n, i) { return '<button type="button" data-r="' + i + '"' + (i === st.r ? ' class="on"' : "") + (d.r[i] ? "" : " disabled") + ">" + n + "</button>"; }).join("") + "</div>";
      var rr = d.r[st.r];
      if (!rr) h += '<p class="meta">이 등급에서는 채집할 수 없는 포인트입니다.</p>';
      else {
        h += '<p class="meta">채집 가능 횟수: <b>' + (rr[0] || "-") + "</b>회</p>";
        var names = []; [1, 2, 3].forEach(function (j) { rr[j].forEach(function (x) { if (names.indexOf(x[0]) < 0) names.push(x[0]); }); });
        h += '<table class="wd-stats ptab"><tr><th>아이템</th>' + window.MPS.map(function (n, i) { return '<th class="' + (i === st.s ? "cur" : "") + '">' + n + "</th>"; }).join("") + "</tr>";
        names.forEach(function (nm) {
          var q = st.q && nm.toLowerCase().indexOf(st.q) >= 0;
          h += "<tr" + (q ? ' class="qhit"' : "") + "><td>" + esc(nm) + "</td>" + [1, 2, 3].map(function (j, i) { var f = rr[j].filter(function (x) { return x[0] === nm; })[0]; return '<td class="num' + (i === st.s ? " cur" : "") + '">' + (f ? f[1] + "%" : "-") + "</td>"; }).join("") + "</tr>";
        });
        h += "</table>";
      }
      body.innerHTML = h; body.setAttribute("data-p", pid); pop.hidden = false; setHash("p-" + pid);
    }
    document.addEventListener("click", function (e) {
      var t = e.target;
      var se = t.closest(".mseason .se"); if (se) { st.s = +se.getAttribute("data-s"); apply(); return; }
      var rg = t.closest(".mregion .rg"); if (rg) { showRegion(rg.getAttribute("data-loc")); return; }
      var rk = t.closest(".mchip.rk"); if (rk) { st.r = +rk.getAttribute("data-r"); apply(); return; }
      var all = t.closest(".mchip.all"); if (all) { var on = all.getAttribute("data-all") === "1"; $$(".mchip.kd input").forEach(function (i) { if (on) delete st.off[i.getAttribute("data-k")]; else st.off[i.getAttribute("data-k")] = 1; }); apply(); return; }
      var pr = t.closest(".prk button"); if (pr) { st.r = +pr.getAttribute("data-r"); apply(); openP(document.querySelector("#wpop .wp-body").getAttribute("data-p")); return; }
      var pt = t.closest(".mapbox .pt, .xpt"); if (pt) { openP(pt.getAttribute("data-p")); return; }
      var pop = document.getElementById("wpop"); if (t === pop || t.closest(".wp-x")) { pop.hidden = true; setHash(""); }
    });
    document.addEventListener("keydown", function (e) { if (e.key === "Escape") { document.getElementById("wpop").hidden = true; setHash(""); } });
    $$(".mchip.kd input").forEach(function (i) { i.addEventListener("change", function () { if (i.checked) delete st.off[i.getAttribute("data-k")]; else st.off[i.getAttribute("data-k")] = 1; apply(); }); });
    // 소분류 묶음 켜기/끄기
    $$(".mchip.grp").forEach(function (g) { g.addEventListener("click", function () {
      var gi = g.getAttribute("data-grp"), ins = $$('.mchip.kd[data-grp="' + gi + '"] input');
      var allOn = ins.every(function (i) { return !st.off[i.getAttribute("data-k")]; });
      ins.forEach(function (i) { if (allOn) st.off[i.getAttribute("data-k")] = 1; else delete st.off[i.getAttribute("data-k")]; });
      apply(); g.classList.toggle("on", !allOn);
    }); });
    // 확대 (100~300%). 포인트는 % 위치라 확대해도 제자리
    var zr = document.getElementById("mzoom"), zv = document.getElementById("mzv");
    function setZoom(z, keepCenter) {
      z = Math.max(100, Math.min(300, z)); zr.value = z; zv.textContent = z + "%";
      $$(".mapview").forEach(function (v) {
        var b = v.querySelector(".mapbox"), cx = (v.scrollLeft + v.clientWidth / 2) / (b.offsetWidth || 1), cy = (v.scrollTop + v.clientHeight / 2) / (b.offsetHeight || 1);
        b.style.width = z + "%"; v.classList.toggle("zoomed", z > 100);
        if (keepCenter) { v.scrollLeft = cx * b.offsetWidth - v.clientWidth / 2; v.scrollTop = cy * b.offsetHeight - v.clientHeight / 2; }
      });
      store.set("mh2-mapzoom", String(z));
    }
    zr.addEventListener("input", function () { setZoom(+zr.value, true); });
    $$(".mchip.zm").forEach(function (b) { b.addEventListener("click", function () { var d = b.getAttribute("data-z"); setZoom(d === "0" ? 100 : +zr.value + (d === "+" ? 25 : -25), true); }); });
    // 확대한 지도는 끌어서 옮기기 (짧게 누르면 포인트 클릭)
    $$(".mapview").forEach(function (v) {
      var sx, sy, sl, stp, drag = false, down = false;
      v.addEventListener("pointerdown", function (e) { if (!v.classList.contains("zoomed") || e.button !== 0) return; down = true; drag = false; sx = e.clientX; sy = e.clientY; sl = v.scrollLeft; stp = v.scrollTop; });
      v.addEventListener("pointermove", function (e) { if (!down) return; var dx = e.clientX - sx, dy = e.clientY - sy; if (!drag && Math.abs(dx) + Math.abs(dy) > 5) { drag = true; v.classList.add("dragging"); } if (drag) { v.scrollLeft = sl - dx; v.scrollTop = stp - dy; } });
      var up = function () { down = false; setTimeout(function () { drag = false; v.classList.remove("dragging"); }, 0); };
      v.addEventListener("pointerup", up); v.addEventListener("pointerleave", up);
      v.addEventListener("click", function (e) { if (drag) { e.stopPropagation(); e.preventDefault(); } }, true);
    });
    var z0 = +(store.get("mh2-mapzoom") || 100); setZoom(z0 || 100, false);
    var mq = document.getElementById("mq"), mt;
    mq.addEventListener("input", function () { clearTimeout(mt); mt = setTimeout(function () {
      st.q = mq.value.trim().toLowerCase(); apply();
      if (st.q) { var cur = document.querySelector(".mregion .rg.on"); if (!cur.classList.contains("has")) { var f = document.querySelector(".mregion .rg.has"); if (f) showRegion(f.getAttribute("data-loc")); } }
    }, 200); });
    var r0 = store.get("mh2-maprg"); if (r0 && document.getElementById(r0)) showRegion(r0);
    var hh = location.hash && document.getElementById(decodeURIComponent(location.hash.slice(1)));
    var hs = hh && hh.closest && hh.closest("section.loc"); if (hs) showRegion(hs.id);
    apply();
    var hp = /^#p-(.+)$/.exec(location.hash); if (hp && window.MP[decodeURIComponent(hp[1])]) { var pb = document.querySelector('[data-p="' + decodeURIComponent(hp[1]) + '"]'); if (pb) { var ls = pb.closest("section.loc"); if (ls) showRegion(ls.id); var vv = pb.closest(".variant"); if (vv) showVariant(vv.closest("section.loc"), vv.getAttribute("data-v")); } openP(decodeURIComponent(hp[1])); }
  })();

  // 여러 단 탭 (퀘스트: 마을 → 분류). 버튼 묶음 .gtabs[data-g] ↔ 패널 .gpanel[data-g][data-t]
  if (document.querySelector(".gtabs")) {
    var gkey = function (g) { return "mh2-g:" + location.pathname.split("/").pop() + ":" + g; };
    var showG = function (g, t) {
      [].forEach.call(document.querySelectorAll('.gtabs[data-g="' + g + '"] button'), function (b) { b.classList.toggle("on", b.getAttribute("data-t") === String(t)); });
      [].forEach.call(document.querySelectorAll('.gpanel[data-g="' + g + '"]'), function (p) { p.classList.toggle("off", p.getAttribute("data-t") !== String(t)); });
      store.set(gkey(g), String(t));
    };
    document.addEventListener("click", function (e) { var b = e.target.closest(".gtabs button"); if (b) showG(b.closest(".gtabs").getAttribute("data-g"), b.getAttribute("data-t")); });
    [].forEach.call(document.querySelectorAll(".gtabs"), function (gt) { var g = gt.getAttribute("data-g"), v = store.get(gkey(g)); if (v !== null && gt.querySelector('button[data-t="' + v + '"]')) showG(g, v); });
    var gh = location.hash && document.getElementById(decodeURIComponent(location.hash.slice(1)));
    if (gh) { var p = gh; while (p && p.closest) { p = p.closest(".gpanel"); if (!p) break; showG(p.getAttribute("data-g"), p.getAttribute("data-t")); p = p.parentElement; } gh.scrollIntoView(); }
  }

  // 아이템 등: 용도 탭 (검색 중에는 모든 탭을 보여 줌)
  var ctabs = document.querySelector(".ctabs");
  if (ctabs) {
    var ckey = "mh2-ctab:" + location.pathname.split("/").pop();
    var showCat = function (c) {
      [].forEach.call(ctabs.querySelectorAll("button"), function (b) { b.classList.toggle("on", b.getAttribute("data-c") === String(c)); });
      [].forEach.call(document.querySelectorAll("section.ccat"), function (s) { s.classList.toggle("off", s.getAttribute("data-c") !== String(c)); });
      store.set(ckey, String(c));
    };
    ctabs.addEventListener("click", function (e) { var b = e.target.closest("button"); if (b) showCat(b.getAttribute("data-c")); });
    var cc = store.get(ckey); if (cc !== null && document.querySelector('section.ccat[data-c="' + cc + '"]')) showCat(cc);
    var h0 = location.hash && document.getElementById(decodeURIComponent(location.hash.slice(1)));
    var sec0 = h0 && h0.closest && h0.closest("section.ccat"); if (sec0) { showCat(sec0.getAttribute("data-c")); h0.scrollIntoView(); }
  }

  // 무기 강화 트리: 종류 탭, 아이콘 눌러 상세 창, 이름 찾기
  var wtabs = document.querySelector(".wtabs");
  if (wtabs) {
    var showClass = function (c) {
      [].forEach.call(wtabs.querySelectorAll("button"), function (b) { b.classList.toggle("on", b.getAttribute("data-c") === String(c)); });
      [].forEach.call(document.querySelectorAll("section.wcls"), function (s) { s.classList.toggle("off", s.getAttribute("data-c") !== String(c)); });
      store.set("mh2-wclass:" + location.pathname.split("/").pop(), String(c));
    };
    wtabs.addEventListener("click", function (e) { var b = e.target.closest("button"); if (b) showClass(b.getAttribute("data-c")); });
    var c0 = store.get("mh2-wclass:" + location.pathname.split("/").pop()); if (c0 !== null && document.querySelector('section.wcls[data-c="' + c0 + '"]')) showClass(c0);
    var pop = document.getElementById("wpop"), body = pop.querySelector(".wp-body");
    var openW = function (id) {
      if (!window.WD || !(id in window.WD)) return;
      body.innerHTML = window.WD[id]; pop.hidden = false; pop.querySelector(".wp-box").scrollTop = 0; setHash("w-" + id);
      var any = document.querySelector('.wn[data-w="' + id + '"]'); var asec = any && any.closest("section.wcls"); if (asec && asec.classList.contains("off")) showClass(asec.getAttribute("data-c"));
      [].forEach.call(document.querySelectorAll(".wn.sel"), function (n) { n.classList.remove("sel"); });
      var sec = document.querySelector("section.wcls:not(.off)");
      var node = sec && sec.querySelector('.wn[data-w="' + id + '"]'); if (node) node.classList.add("sel");
    };
    var closeW = function () { pop.hidden = true; setHash(""); };
    document.addEventListener("click", function (e) {
      var n = e.target.closest(".wn, .wd-link"); if (n) { openW(n.getAttribute("data-w")); return; }
      if (e.target === pop || e.target.closest(".wp-x")) closeW();
    });
    document.addEventListener("keydown", function (e) { if (e.key === "Escape") closeW(); });
    var hw = /^#w-(.+)$/.exec(location.hash); if (hw) openW(decodeURIComponent(hw[1]));
    var wq = document.getElementById("wq"), wqn = document.getElementById("wqn"), wt;
    wq.addEventListener("input", function () {
      clearTimeout(wt); wt = setTimeout(function () {
        var s = wq.value.trim().toLowerCase(), first = null, n = 0, cls = null;
        [].forEach.call(document.querySelectorAll(".wn"), function (b) { b.classList.remove("hit", "dim"); });
        if (s) {
          var cur = document.querySelector("section.wcls:not(.off)");
          [].forEach.call(document.querySelectorAll(".wn"), function (b) {
            var hit = b.textContent.toLowerCase().indexOf(s) >= 0 || (window.WD[b.getAttribute("data-w")] || "").toLowerCase().indexOf(s) >= 0 && b.querySelector(".nm").textContent.toLowerCase().indexOf(s) >= 0;
            b.classList.add(hit ? "hit" : "dim"); if (hit) { n++; if (!first || (b.closest("section") === cur && first.closest("section") !== cur)) first = b; }
          });
          if (first) { showClass(first.closest("section").getAttribute("data-c")); first.scrollIntoView({ block: "center", inline: "center" }); }
        }
        wqn.textContent = s ? n + "건" : "";
      }, 200);
    });
  }

  var q = document.getElementById("q"), qn = document.getElementById("qn");
  if (!q) return;
  // 검색 대상: 표의 줄, 카드(몬스터・스킬), 퀘스트
  var units = [].slice.call(document.querySelectorAll("table.ft tbody tr, section.card, details.mon, details.q"));
  var texts = units.map(function (u) { return u.textContent.toLowerCase(); });
  var groups = [].slice.call(document.querySelectorAll("section.grp"));
  var timer;
  function run() {
    var s = q.value.trim().toLowerCase(), n = 0;
    document.body.classList.toggle("searching", !!s); // 검색 중엔 낮・밤 모두 보여 줌
    units.forEach(function (u, i) {
      var hit = !s || texts[i].indexOf(s) >= 0;
      u.classList.toggle("hidden", !hit);
      if (hit) n++;
      if (s && hit && u.tagName === "DETAILS" && u.classList.contains("q")) u.open = false;
    });
    groups.forEach(function (g) {
      var any = !s || g.querySelector("table.ft tbody tr:not(.hidden), details.q:not(.hidden), section.card:not(.hidden)");
      g.classList.toggle("hidden", !any);
      [].slice.call(g.querySelectorAll(".tw")).forEach(function (tw) {
        if (tw.querySelector("table.ft")) tw.classList.toggle("hidden", !!s && !tw.querySelector("tbody tr:not(.hidden)"));
      });
    });
    qn.textContent = s ? n + "건" : "";
  }
  q.addEventListener("input", function () { clearTimeout(timer); timer = setTimeout(run, 150); });
  var h = location.hash ? decodeURIComponent(location.hash.slice(1)) : "";
  if (h) { var t = document.getElementById(h); if (t && t.tagName === "DETAILS") t.open = true; }
})();
