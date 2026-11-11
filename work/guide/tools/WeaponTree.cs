using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

// 무기 강화 트리: 위키 표의 들여쓰기(깊이)와 ∟ 표시로 부모-자식을 복원해 게임식 트리로 그린다
public static class WeaponTree
{
    public class Node
    {
        public int Id, Level, Group; public char Pre; public string Ja; public Dictionary<string, object> W;
        public Node Parent; public List<Node> Kids = new List<Node>();
        public int Row, Col; public bool Main; // Main: 부모와 같은 줄로 이어짐
    }

    const int CW = 100, RH = 64, LW = 100, NW = 92, NH = 56; // 칸 너비・줄 높이・이름표 너비・노드 크기

    public static List<Node> Build(string html, string codes, List<Dictionary<string, object>> weapons)
    {
        var jas = Regex.Matches(html, "<tr class=\"weapon-row[^>]*?data-japanese=\"([^\"]*)\"").Cast<Match>().Select(m => System.Net.WebUtility.HtmlDecode(m.Groups[1].Value)).ToList();
        var cs = codes.Trim().Split(',');
        // 일본어 이름(같은 이름은 나온 순서)으로 JSON 무기와 짝짓기
        var pool = new Dictionary<string, Queue<Dictionary<string, object>>>();
        foreach (var w in weapons) { string j = G.S(w, "japanese"); if (!pool.ContainsKey(j)) pool[j] = new Queue<Dictionary<string, object>>(); pool[j].Enqueue(w); }
        var all = new List<Node>(); int grp = -1;
        for (int i = 0; i < cs.Length && i < jas.Count; i++) {
            string c = cs[i]; if (c.StartsWith("#")) { grp++; c = c.Substring(1); }
            var n = new Node { Id = i, Group = grp, Pre = c[c.Length - 1], Level = int.Parse(c.Substring(0, c.Length - 1)) / 3, Ja = jas[i] };
            Queue<Dictionary<string, object>> q; if (pool.TryGetValue(n.Ja, out q) && q.Count > 0) n.W = q.Dequeue();
            all.Add(n);
        }
        // 부모 찾기
        foreach (var g in all.GroupBy(n => n.Group)) {
            var list = g.ToList();
            for (int i = 0; i < list.Count; i++) {
                var n = list[i]; if (n.Pre == '-') continue;
                for (int j = i - 1; j >= 0; j--) {
                    var p = list[j]; if (p.Level > n.Level) continue;
                    if (p.Level < n.Level) { n.Parent = p; break; }
                    // 같은 깊이: ∟ 로 끝난 줄이면 그 형제, 아니면 바로 앞 단계
                    if (p.Pre == 'L') { n.Parent = p.Parent; } else n.Parent = p;
                    break;
                }
            }
        }
        // 짝이 없는(JSON에 없는・개발용) 노드는 빼고 자식을 위로 올림
        Func<Node, bool> ok = x => x.W != null && !G.Has(x.W, "dev");
        foreach (var n in all) { var p = n.Parent; while (p != null && !ok(p)) p = p.Parent; n.Parent = p; }
        var res = all.Where(ok).ToList();
        foreach (var n in res) if (n.Parent != null) n.Parent.Kids.Add(n);
        foreach (var n in res) n.Main = n.Parent != null && n.Parent.Level == n.Level && n.Pre != '-';
        return res;
    }

    // 한 무기 종류의 트리를 그린다: 그 종류의 무기와, 거기로 이어지는 조상만
    public static string Render(List<Node> all, string cls, Func<Node, string> nodeHtml, Func<Node, string> label)
    {
        var keep = new HashSet<Node>();
        foreach (var n in all.Where(n => G.S(n.W, "weaponClass") == cls)) { var p = n; while (p != null && keep.Add(p)) p = p.Parent; }
        var roots = all.Where(n => keep.Contains(n) && n.Parent == null).ToList();
        var sb = new StringBuilder();
        // 강화 관계가 하나도 없으면(보우건) 아이콘 격자로
        if (roots.All(r => r.Kids.Count(keep.Contains) == 0)) {
            sb.Append("<p class=\"wflat-note\">이 무기는 강화 없이 공방에서 바로 생산합니다. 아이콘을 누르면 성능과 생산 소재가 나옵니다.</p><div class=\"wflat\">");
            foreach (var r in roots.OrderBy(r => int.Parse(G.S(r.W, "rarity") == "" ? "0" : G.S(r.W, "rarity"))).ThenBy(r => r.Id)) sb.Append("<button type=\"button\" class=\"wn flat\" data-w=\"" + r.Id + "\">" + nodeHtml(r) + "</button>");
            return sb.Append("</div>").ToString();
        }
        foreach (var gr in roots.GroupBy(r => r.Group)) {
            int row = 0; var placed = new List<Node>(); var rowLabel = new Dictionary<int, string>();
            foreach (var r in gr) { rowLabel[row] = label(r) + " 계열"; Place(r, row, 0, keep, placed, ref row, rowLabel, label); row++; }
            int rows = row, cols = placed.Max(n => n.Col) + 1;
            int w = LW + cols * CW, h = rows * RH;
            sb.Append("<div class=\"wtree\" style=\"width:" + w + "px;height:" + h + "px\">");
            // 줄 바탕・이름표
            for (int i = 0; i < rows; i++) sb.Append("<div class=\"wrow" + (i % 2 == 1 ? " alt" : "") + "\" style=\"top:" + (i * RH) + "px;width:" + w + "px\"><span class=\"wlab\">" + (rowLabel.ContainsKey(i) ? rowLabel[i] : "") + "</span></div>");
            // 연결선
            var svg = new StringBuilder("<svg class=\"wlines\" width=\"" + w + "\" height=\"" + h + "\">");
            foreach (var n in placed) foreach (var k in n.Kids.Where(placed.Contains)) {
                double x1 = LW + n.Col * CW + NW, y1 = n.Row * RH + RH / 2.0, x2 = LW + k.Col * CW, y2 = k.Row * RH + RH / 2.0, xm = x1 + (x2 - x1) / 2;
                string d = y1 == y2 ? "M" + F(x1) + " " + F(y1) + " H" + F(x2 - 4) : "M" + F(x1) + " " + F(y1) + " H" + F(xm) + " V" + F(y2) + " H" + F(x2 - 4);
                svg.Append("<path d=\"" + d + "\"/><path class=\"ah\" d=\"M" + F(x2 - 8) + " " + F(y2 - 4) + " L" + F(x2 - 2) + " " + F(y2) + " L" + F(x2 - 8) + " " + F(y2 + 4) + "Z\"/>");
            }
            sb.Append(svg.Append("</svg>"));
            foreach (var n in placed) {
                bool other = G.S(n.W, "weaponClass") != cls;
                sb.Append("<button type=\"button\" class=\"wn" + (other ? " other" : "") + "\" data-w=\"" + n.Id + "\" style=\"left:" + (LW + n.Col * CW) + "px;top:" + (n.Row * RH + (RH - NH) / 2) + "px\">" + nodeHtml(n) + "</button>");
            }
            sb.Append("</div>\n");
        }
        return sb.ToString();
    }
    static string F(double v) { return v.ToString("0.#", CultureInfo.InvariantCulture); }

    // 본줄(Main)은 같은 줄 오른쪽, 파생은 아래 새 줄
    static void Place(Node n, int row, int col, HashSet<Node> keep, List<Node> placed, ref int maxRow, Dictionary<int, string> rowLabel, Func<Node, string> label)
    {
        n.Row = row; n.Col = col; placed.Add(n);
        var kids = n.Kids.Where(keep.Contains).ToList();
        var main = kids.FirstOrDefault(k => k.Main);
        if (main == null && kids.Count > 0 && kids.All(k => !k.Main)) main = null;
        if (main != null) Place(main, row, col + 1, keep, placed, ref maxRow, rowLabel, label);
        foreach (var k in kids.Where(k => k != main)) {
            maxRow++; rowLabel[maxRow] = label(k) + " 파생";
            Place(k, maxRow, col + 1, keep, placed, ref maxRow, rowLabel, label);
        }
    }
}
