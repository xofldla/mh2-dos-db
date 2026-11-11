// 라이트/다크 모드 전환 (기본: 다크). <head> 에서 먼저 적용해 깜빡임을 막고, 버튼은 오른쪽 위에 둔다.
// 저장: localStorage 가 막힌 환경(파일로 열기, 공유 링크의 격리된 화면)에서도 유지되도록
//       ① localStorage ② 창 이름(window.name, 같은 탭에서 페이지를 옮겨도 남음) ③ 주소의 ?theme= 를 함께 쓴다.
(function () {
  var root = document.documentElement, KEY = "mh2-theme";
  function fromUrl() { var m = /[?&]theme=(light|dark)/.exec(location.search); return m ? m[1] : null; }
  function fromName() { var m = /(?:^|;)mh2theme=(light|dark)/.exec(window.name || ""); return m ? m[1] : null; }
  function fromStore() { try { return localStorage.getItem(KEY); } catch (e) { return null; } }
  function save(v) {
    try { localStorage.setItem(KEY, v); } catch (e) {}
    try { window.name = (window.name || "").replace(/(?:^|;)mh2theme=(light|dark)/, "") + ";mh2theme=" + v; } catch (e) {}
  }
  var cur = fromName() || fromUrl() || fromStore() || "dark";
  if (cur !== "light") cur = "dark";
  root.setAttribute("data-theme", cur); save(cur);

  // 안쪽 링크로 옮길 때 주소에 현재 모드를 붙인다 (저장소가 전혀 안 되는 환경 대비)
  document.addEventListener("click", function (e) {
    var a = e.target.closest ? e.target.closest("a[href]") : null; if (!a) return;
    var href = a.getAttribute("href");
    if (!/^[\w.-]+\.html(\?[^#]*)?(#.*)?$/.test(href)) return; // 같은 폴더의 공략 페이지만
    var t = root.getAttribute("data-theme"), hash = "", i = href.indexOf("#");
    if (i >= 0) { hash = href.slice(i); href = href.slice(0, i); }
    href = href.replace(/([?&])theme=(light|dark)&?/, "$1").replace(/[?&]$/, "");
    a.setAttribute("href", href + (href.indexOf("?") >= 0 ? "&" : "?") + "theme=" + t + hash);
  }, true);

  function mount() {
    var btn = document.createElement("button");
    btn.type = "button"; btn.id = "themebtn";
    var paint = function () {
      var dark = root.getAttribute("data-theme") === "dark";
      btn.innerHTML = dark ? '<span class="ti">☀</span><span class="tl">라이트</span>' : '<span class="ti">☾</span><span class="tl">다크</span>';
      btn.title = dark ? "라이트 모드로 바꾸기" : "다크 모드로 바꾸기";
      btn.setAttribute("aria-label", btn.title);
    };
    btn.addEventListener("click", function () {
      var next = root.getAttribute("data-theme") === "dark" ? "light" : "dark";
      root.setAttribute("data-theme", next); save(next); paint();
      // 주소에 남은 ?theme= 도 맞춰 둔다 (새로고침해도 유지)
      if (fromUrl() && history.replaceState) try { history.replaceState(null, "", location.href.replace(/theme=(light|dark)/, "theme=" + next)); } catch (e) {}
    });
    paint();
    var bar = document.querySelector("header.top .wrap");
    if (bar) bar.appendChild(btn); else { btn.classList.add("floating"); document.body.appendChild(btn); }
    visits(bar);
  }

  // 접속 수: GitHub Pages 사이트에서만 센다(hitscounter.dev, 계정 불필요). 같은 탭(세션)에서는 한 번만 올리고 다른 페이지는 저장값을 보여 준다.
  function visits(bar) {
    if (!/(^|\.)github\.io$/.test(location.hostname)) return;
    var st = document.createElement("style");
    st.textContent = "#visits{font:12px/1 \"Galmuri11\",monospace;color:var(--sub);white-space:nowrap;z-index:30}" +
      "header.top .wrap{position:relative}#visits.inbar{position:absolute;right:16px;top:calc(100% + 6px)}" +
      "#visits.floating{position:fixed;top:48px;right:16px}#visits b{color:var(--acc);font-weight:700}";
    document.head.appendChild(st);
    var el = document.createElement("div"); el.id = "visits"; el.className = bar ? "inbar" : "floating";
    // 앞자리 0 없이, 천 단위 쉼표 (예: 0, 27, 1,234)
    var fmt = function (n) { return String(Number(n) || 0).replace(/\B(?=(\d{3})+(?!\d))/g, ","); };
    var show = function (d) { el.innerHTML = "[ Total : <b>" + fmt(d.total_hits) + "</b> ]"; el.title = "누적 접속 수"; };
    el.textContent = "[ Total : … ]";
    (bar || document.body).appendChild(el);
    var KEYS = "mh2-visits", cached = null;
    try { cached = JSON.parse(sessionStorage.getItem(KEYS) || "null"); } catch (e) {}
    if (cached && cached.d === new Date().toDateString()) { show(cached); return; }
    var site = "https://xofldla.github.io/mh2-dos-db/";
    fetch("https://hitscounter.dev/api/hit?output=json&url=" + encodeURIComponent(site))
      .then(function (r) { return r.json(); })
      .then(function (d) { if (d && d.total_hits != null) { show(d); d.d = new Date().toDateString(); try { sessionStorage.setItem(KEYS, JSON.stringify(d)); } catch (e) {} } })
      .catch(function () { el.textContent = ""; });
  }
  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", mount); else mount();
})();
