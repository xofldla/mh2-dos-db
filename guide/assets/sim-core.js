/* 장비 조합 시뮬레이터 — 검색 엔진 (브라우저 / Web Worker 공용)
 * 데이터: window.SIMD (sim-data.js)
 *   a: [부위(0머리 1몸통 2팔 3허리 4다리), 이름, 레어, 직업(0검사 1거너 2공용), 성별(0공용 1남 2여), 방어(처음), 방어(최대), 슬롯, [내성5], [[스킬,포인트]], 동계통배가, 검사링크, 거너링크]
 *   d: [이름, 슬롯, 레어, [[스킬,포인트]], 링크]
 *   s: [스킬계통 이름, [[발동포인트, 발동 스킬 이름]]]
 * 동계통배가: 몸통 "방어구" 스킬 포인트만 한 번 더 더한다(몸통 장식주는 배가하지 않는 보수적 계산).
 */
(function (root) {
  "use strict";
  var PART_ORDER = [1, 0, 2, 3, 4]; // 몸통을 먼저 정해야 동계통배가를 계산할 수 있다

  function search(D, o, progress) {
    var T = o.targets, K = T.length, tix = {};
    T.forEach(function (t, i) { tix[t.sk] = i; });
    var t0 = Date.now(), timeLimit = o.timeLimit || 12000, maxSol = o.maxSol || 2000;
    function vec(list) { var v = new Array(K); for (var i = 0; i < K; i++) v[i] = 0; list.forEach(function (x) { var i = tix[x[0]]; if (i !== undefined) v[i] += x[1]; }); return v; }
    function geq(a, b) { for (var i = 0; i < a.length; i++) if (a[i] < b[i]) return false; return true; }

    // ---- 부위별 후보 ----
    var parts = [[], [], [], [], []];
    D.a.forEach(function (a, i) {
      if (a[3] !== 2 && a[3] !== o.cls) return;
      if (a[4] !== 0 && a[4] !== o.sex) return;
      if (a[2] > o.maxRar) return;
      if (o.exclude && o.exclude[i]) return;
      parts[a[0]].push({ i: i, v: vec(a[9]), s: a[7], tu: a[10] === 1, def: a[6] });
    });
    // 지배되는 후보 제거: 포인트・슬롯이 모두 같거나 낮고 방어력도 낮으면 뺀다
    parts = parts.map(function (list) {
      var keep = [];
      list.forEach(function (x) {
        for (var j = 0; j < list.length; j++) {
          var y = list[j]; if (y === x || y.tu !== x.tu) continue;
          if (geq(y.v, x.v) && y.s >= x.s) {
            var strict = y.s > x.s || y.def > x.def || y.v.some(function (q, k) { return q > x.v[k]; });
            if (strict || (y.def === x.def && j < list.indexOf(x))) return;
          }
        }
        keep.push(x);
      });
      return keep.sort(function (a, b) { return b.def - a.def || b.s - a.s; }); // 방어력 높은 후보부터 → 좋은 조합이 먼저 나온다
    });

    // ---- 장식주 묶음 (슬롯 1/2/3칸에 들어가는 조합) ----
    var decos = [];
    D.d.forEach(function (d, i) {
      // 장식주 레어도(4~5)는 진행도와 무관해서 레어 제한을 걸지 않는다
      var v = vec(d[3]); if (!v.some(function (q) { return q > 0; })) return;
      decos.push({ i: i, s: d[1], v: v });
    });
    var combos = { 0: [{ v: vec([]), d: [] }], 1: [], 2: [], 3: [] };
    function addCombo(cap, list) {
      var v = vec([]); list.forEach(function (x) { for (var k = 0; k < K; k++) v[k] += x.v[k]; });
      combos[cap].push({ v: v, d: list.map(function (x) { return x.i; }) });
    }
    for (var cap = 1; cap <= 3; cap++) {
      (function rec(start, left, cur) {
        addCombo(cap, cur.slice());
        for (var j = start; j < decos.length; j++) if (decos[j].s <= left) { cur.push(decos[j]); rec(j, left - decos[j].s, cur); cur.pop(); }
      })(0, cap, []);
      // 지배 제거
      var c = combos[cap], keep = [];
      c.forEach(function (x, xi) {
        for (var j = 0; j < c.length; j++) { var y = c[j]; if (j === xi) continue; if (geq(y.v, x.v) && (y.v.some(function (q, k) { return q > x.v[k]; }) || (j < xi && y.d.length <= x.d.length))) return; }
        keep.push(x);
      });
      combos[cap] = keep;
    }
    var gainMax = { 0: vec([]), 1: vec([]), 2: vec([]), 3: vec([]) };
    [1, 2, 3].forEach(function (cp) { combos[cp].forEach(function (x) { for (var k = 0; k < K; k++) if (x.v[k] > gainMax[cp][k]) gainMax[cp][k] = x.v[k]; }); });

    // ---- 장식주 채우기: 슬롯 묶음(부위마다 따로)에 묶음 조합을 고른다 ----
    function fill(groups, need) {
      groups = groups.slice().sort(function (a, b) { return b.cap - a.cap; });
      var memo = {};
      function rec(gi, need) {
        var done = true; for (var k = 0; k < K; k++) if (need[k] > 0) { done = false; break; }
        if (done) return [];
        if (gi >= groups.length) return null;
        for (var k2 = 0; k2 < K; k2++) if (need[k2] > 0) { var b = 0; for (var g = gi; g < groups.length; g++) b += gainMax[groups[g].cap][k2]; if (b < need[k2]) return null; }
        var key = gi + "|" + need.join(","); if (memo[key] !== undefined) return memo[key];
        var cs = combos[groups[gi].cap], res = null;
        for (var c = 0; c < cs.length && !res; c++) {
          var nn = need.slice(); for (var k3 = 0; k3 < K; k3++) nn[k3] -= cs[c].v[k3];
          var r = rec(gi + 1, nn); if (r) res = [{ g: groups[gi].id, d: cs[c].d }].concat(r);
        }
        if (!res) { var r0 = rec(gi + 1, need); if (r0) res = r0; }
        memo[key] = res; return res;
      }
      return rec(0, need);
    }

    // ---- 부위 조합 탐색 ----
    var target = T.map(function (t) { return t.pts; });
    var maxV = parts.map(function (list) { var m = vec([]); list.forEach(function (x) { if (!x.tu) for (var k = 0; k < K; k++) if (x.v[k] > m[k]) m[k] = x.v[k]; }); return m; });
    var maxS = parts.map(function (list) { var m = 0; list.forEach(function (x) { if (x.s > m) m = x.s; }); return m; });
    var sols = [], nodes = 0, stopped = false, pick = new Array(5);
    function dfs(pi, cur) {
      if (stopped) return;
      if (++nodes % 4000 === 0) { if (Date.now() - t0 > timeLimit) { stopped = true; return; } if (progress) progress(nodes, sols.length); }
      if (pi === 5) {
        var need = target.map(function (t, k) { return t - cur[k]; });
        var groups = [];
        for (var p = 0; p < 5; p++) if (pick[p].s > 0) groups.push({ id: p, cap: pick[p].s });
        if (o.wslots > 0) groups.push({ id: 5, cap: o.wslots });
        var f = fill(groups, need);
        if (f) { sols.push({ p: pick.map(function (x) { return x.i; }), f: f }); if (sols.length >= maxSol) stopped = true; }
        return;
      }
      var part = PART_ORDER[pi], torso = pick[1];
      // 상한: 남은 부위 최대 포인트 + 장식주 최대
      var slotCaps = [];
      for (var q = 0; q < pi; q++) if (pick[PART_ORDER[q]].s > 0) slotCaps.push(pick[PART_ORDER[q]].s);
      for (var q2 = pi; q2 < 5; q2++) if (maxS[PART_ORDER[q2]] > 0) slotCaps.push(maxS[PART_ORDER[q2]]);
      if (o.wslots > 0) slotCaps.push(o.wslots);
      for (var k = 0; k < K; k++) {
        var b = cur[k];
        for (var q3 = pi; q3 < 5; q3++) { var pp = PART_ORDER[q3]; var m = maxV[pp][k]; if (torso && torso.v[k] > m && parts[pp].some(function (x) { return x.tu; })) m = torso.v[k]; b += m; }
        slotCaps.forEach(function (cp) { b += gainMax[cp][k]; });
        if (b < target[k]) return;
      }
      var list = parts[part];
      for (var c = 0; c < list.length; c++) {
        var x = list[c]; var v = x.tu ? (torso ? torso.v : null) : x.v; if (!v) continue;
        pick[part] = x; var nc = cur.slice(); for (var k4 = 0; k4 < K; k4++) nc[k4] += v[k4];
        dfs(pi + 1, nc);
        if (stopped) return;
      }
      pick[part] = undefined;
    }
    dfs(0, vec([]));
    return { sols: sols, nodes: nodes, stopped: stopped, ms: Date.now() - t0, cand: parts.map(function (l) { return l.length; }), combos: [combos[1].length, combos[2].length, combos[3].length] };
  }

  // 조합 하나의 전체 합계 (모든 스킬 계통, 방어력, 내성, 남는 슬롯)
  function evaluate(D, sol, wslots) {
    var pts = {}, def0 = 0, def1 = 0, res = [0, 0, 0, 0, 0], tuN = 0, torso = D.a[sol.p[1]];
    sol.p.forEach(function (ai) {
      var a = D.a[ai]; def0 += a[5]; def1 += a[6];
      for (var e = 0; e < 5; e++) res[e] += +a[8][e];
      if (a[10]) tuN++;
      a[9].forEach(function (x) { pts[x[0]] = (pts[x[0]] || 0) + x[1]; });
    });
    if (tuN) torso[9].forEach(function (x) { pts[x[0]] = (pts[x[0]] || 0) + x[1] * tuN; });
    var used = [0, 0, 0, 0, 0, 0];
    sol.f.forEach(function (g) { g.d.forEach(function (di) { var d = D.d[di]; used[g.g] += d[1]; d[3].forEach(function (x) { pts[x[0]] = (pts[x[0]] || 0) + x[1]; }); }); });
    var cap = sol.p.map(function (ai) { return D.a[ai][7]; }).concat([wslots]);
    var free = 0; for (var g = 0; g < 6; g++) free += Math.max(0, cap[g] - used[g]);
    var act = [];
    Object.keys(pts).forEach(function (sk) {
      var v = pts[sk], st = D.s[sk][1], best = null;
      st.forEach(function (x) { if (x[0] > 0 && v >= x[0] && (!best || x[0] > best[0])) best = x; if (x[0] < 0 && v <= x[0] && (!best || x[0] < best[0])) best = x; });
      if (best) act.push({ sk: +sk, v: v, p: best[0], n: best[1] });
    });
    act.sort(function (a, b) { return (b.p > 0) - (a.p > 0) || Math.abs(b.p) - Math.abs(a.p); });
    return { pts: pts, def0: def0, def1: def1, res: res, free: free, used: used, act: act, neg: act.some(function (x) { return x.p < 0; }) };
  }

  root.SimCore = { search: search, evaluate: evaluate };
})(typeof self !== "undefined" ? self : this);
