/* 장비 시뮬레이터: 백그라운드 검색 (화면이 멈추지 않게) */
self.window = self; // sim-data.js는 window.SIMD에 넣는다
importScripts("sim-data.js?v=202611132333", "sim-core.js?v=202611132333");
self.onmessage = function (e) {
  var o = e.data, D = self.SIMD;
  var r = self.SimCore.search(D, o, function (n, s) { self.postMessage({ t: "p", n: n, s: s }); });
  self.postMessage({ t: "r", r: r });
};
