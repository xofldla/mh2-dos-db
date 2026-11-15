/* 온라인 일정 — schedule-data.js(위키 자료, GitHub Actions가 1시간마다 갱신) + schedule-quests.js(한글 패치 용어 퀘스트)로 한국 시간 일정을 그린다.
 * 규칙(위키와 같은 계산을 새로 작성):
 *  - 서버 하루 = 서버 시간대(CAL_META.tz, 기본 Europe/Berlin)의 날짜. startHour 시에 바뀐다.
 *  - 그날의 이벤트 = eventsRotation[(일 - 1) % 길이] 에 적힌 퀘스트들
 *  - 그날의 특매 = shopSpecials[ shopSpecialRotation[(1월 1일부터 지난 날 수) % 길이] ]
 */
(function () {
  "use strict";
  var C = window.CAL, M = window.CAL_META || { tz: "Europe/Berlin", startHour: 0 }, Q = window.QKO || {};
  var $ = function (id) { return document.getElementById(id); };
  var esc = function (s) { return String(s).replace(/[&<>"]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]; }); };
  var GUIDE = "";
  if (!C) { $("now").textContent = "일정 자료를 불러오지 못했습니다."; return; }

  // 특매 한글 (한글 패치 아이템 이름). 위키에 새 특매가 생기면 일본어 그대로 보인다
  var SHOP_KO = {
    "なし": ["특매 없음", ""],
    "調合屋の日": ["조합점의 날", "조합점: 담쟁이잎, 니트로버섯, 마비버섯, 독버섯, 용이빨, 용발톱, 벌꿀, 만드라고라"],
    "道具屋の日": ["도구점의 날", "도구점: 소재옥, 낚시반딧불, 낚시용파리미끼, 곡괭이Ｇ, 곤충망Ｇ, 폭약, 마비함정, 아가씨 구이세트"],
    "食材屋の日": ["식재료점의 날", "식재료점: 희귀한양파, 불사충, 장수잼, 로열치즈, 황금우주"],
    "半額の日": ["반값의 날", "모든 시장 상인이 평소의 반값에 판다"]
  };
  var DOW = ["일", "월", "화", "수", "목", "금", "토"];

  var fmt = new Intl.DateTimeFormat("en-US", { timeZone: M.tz, year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", hourCycle: "h23" });
  function srvParts(ms) {
    var o = {}; fmt.formatToParts(new Date(ms - (M.startHour || 0) * 3600000)).forEach(function (p) { o[p.type] = p.value; });
    return { y: +o.year, m: +o.month, d: +o.day };
  }
  function dayKey(p) { return p.y + "-" + p.m + "-" + p.d; }
  function eventsOf(p) {
    var r = C.eventsRotation, row = r[(p.d - 1) % r.length] || [];
    return row.map(function (k) { var e = C.events[k]; return e ? { key: k, e: e } : null; }).filter(Boolean);
  }
  function shopOf(p) {
    var days = Math.round((Date.UTC(p.y, p.m - 1, p.d) - Date.UTC(p.y, 0, 1)) / 86400000);
    var rot = C.shopSpecialRotation; return C.shopSpecials[rot[days % rot.length]] || C.shopSpecials[0];
  }
  function shopKo(s) { var t = SHOP_KO[s.japanese]; return t ? { n: t[0], d: t[1] } : { n: s.japanese, d: s.descriptionJapanese }; }
  function qName(e) { var q = Q[e.japanese]; return q ? q[1] : e.japanese; }
  function qLink(e) {
    var q = Q[e.japanese], st = '<span class="stars">★' + e.stars + "</span> ";
    return q ? '<a class="xlink" href="' + GUIDE + "quests.html#" + q[0] + '" title="' + esc(e.japanese) + '">' + st + esc(q[1]) + "</a>" : "<span>" + st + esc(e.japanese) + "</span>";
  }
  function nextChange(now) {
    var cur = dayKey(srvParts(now)), t = Math.floor(now / 3600000) * 3600000;
    for (var i = 1; i <= 30; i++) { var x = t + i * 3600000; if (dayKey(srvParts(x)) !== cur) return x; }
    return null;
  }
  var tfmt = function (ms, opt) { return new Date(ms).toLocaleString("ko-KR", opt); };

  function render() {
    var now = Date.now(), sp = srvParts(now), shop = shopKo(shopOf(sp)), nc = nextChange(now);
    var left = nc ? Math.max(0, nc - now) : 0, hh = Math.floor(left / 3600000), mm = Math.floor(left % 3600000 / 60000);
    $("now").innerHTML = '<div class="sch-now"><span class="big">서버 날짜 ' + sp.m + "월 " + sp.d + "일</span>" +
      "<span>오늘의 특매: <span class=\"shop" + (shop.d ? "" : " none") + "\">" + esc(shop.n) + "</span></span>" +
      (nc ? "<span>다음 변경: <b>" + tfmt(nc, { month: "numeric", day: "numeric", weekday: "short", hour: "2-digit", minute: "2-digit" }) + "</b> (" + hh + "시간 " + mm + "분 남음)</span>" : "") + "</div>" +
      (shop.d ? '<div class="shopd">' + esc(shop.d) + "</div>" : "");

    // 오늘의 이벤트: 한글 패치 용어 퀘스트 상세(공략 사이트와 같은 모양)
    $("today").innerHTML = eventsOf(sp).map(function (x) {
      var q = Q[x.e.japanese];
      if (!q) return '<p class="note">' + esc(x.e.japanese) + " (한글 자료 없음)</p>";
      return q[3].replace(/href="([a-z0-9-]+\.html)/g, 'href="' + GUIDE + "$1");
    }).join("") || '<p class="note">이벤트 퀘스트 없음</p>';

    // 이번 주: 오늘(한국 날짜)부터 7일, 하루를 서버 날짜 구간으로 나눔
    var d0 = new Date(); d0.setHours(0, 0, 0, 0);
    var html = "";
    for (var i = 0; i < 7; i++) {
      var start = d0.getTime() + i * 86400000, segs = [];
      for (var h = 0; h < 24; h++) {
        var t = start + h * 3600000, p = srvParts(t), k = dayKey(p);
        if (!segs.length || segs[segs.length - 1].k !== k) segs.push({ k: k, p: p, from: h, to: h + 1 }); else segs[segs.length - 1].to = h + 1;
      }
      var dd = new Date(start);
      html += '<div class="day' + (i === 0 ? " today" : "") + '"><h3>' + (dd.getMonth() + 1) + "/" + dd.getDate() + " (" + DOW[dd.getDay()] + ")</h3>" +
        segs.map(function (s) {
          var isCur = s.k === dayKey(sp) && i === 0, sh = shopKo(shopOf(s.p));
          return '<div class="seg' + (isCur ? " cur" : "") + '"><div class="tm">' + String(s.from).padStart(2, "0") + ":00 – " + String(s.to).padStart(2, "0") + ":00</div>" +
            eventsOf(s.p).map(function (x) { return qLink(x.e); }).join("") +
            (sh.d ? '<div class="sp">🛒 ' + esc(sh.n) + "</div>" : "") + "</div>";
        }).join("") + "</div>";
    }
    $("week").innerHTML = html;
    $("meta").textContent = "1시간마다 위키에서 자동으로 확인합니다. 마지막으로 바뀐 시각: " + (M.updated ? tfmt(Date.parse(M.updated), { year: "numeric", month: "numeric", day: "numeric", hour: "2-digit", minute: "2-digit" }) : "?") + " · 서버 시간대: " + M.tz;
  }
  render();
  setInterval(render, 60000); // 1분마다 남은 시간・현재 구간 갱신
})();
