using System;
using System.Collections.Generic;
using System.Linq;

// 방어구 세트 묶기: 같은 레어도・직업・성별에서 이름 앞부분이 같고 스킬 계통이 겹치는 부위를 한 세트로
public static class ArmorSets
{
    public class Piece { public int Part; public Dictionary<string, object> A; public string Name, Rar, Cls, Sex; public HashSet<string> Skills; }

    static string S(Dictionary<string, object> d, string k) { object v; return d != null && d.TryGetValue(k, out v) && v != null ? Convert.ToString(v) : ""; }

    static int Lcp(string a, string b) { int n = 0; while (n < a.Length && n < b.Length && a[n] == b[n]) n++; return n; }

    static int R(Piece p) { int r; int.TryParse(p.Rar, out r); return r; }
    static bool Compat(string a, string b) { return a == b || a == "" || b == ""; }
    static string Norm(string s) { return s.Normalize(System.Text.NormalizationForm.FormKC); }

    public static List<List<Piece>> Group(List<Piece> all)
    {
        int n = all.Count; var par = Enumerable.Range(0, n).ToArray();
        Func<int, int> find = null; find = x => par[x] == x ? x : (par[x] = find(par[x]));
        // 점수가 높은 짝부터 연결, 한 세트에 같은 부위가 둘 들어가지 않게
        var pairs = new List<Tuple<double, int, int>>();
        for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) {
            var a = all[i]; var b = all[j];
            if (a.Part == b.Part || Math.Abs(R(a) - R(b)) > 1 || !Compat(a.Cls, b.Cls) || !Compat(a.Sex, b.Sex)) continue;
            int l = Lcp(Norm(a.Name), Norm(b.Name));
            int inter = a.Skills.Intersect(b.Skills).Count(); int uni = a.Skills.Union(b.Skills).Count();
            double jac = uni == 0 ? 0 : (double)inter / uni;
            string na = Norm(a.Name), nb = Norm(b.Name);
            bool tail = l >= 1 && na.Contains("・") && nb.Contains("・") && na.Substring(na.LastIndexOf('・')) == nb.Substring(nb.LastIndexOf('・'));
            bool ok = (l >= 3 && jac >= 0.34) || (l >= 2 && jac >= 0.99 && a.Skills.Count >= 2) || (l >= 4) || (tail && jac >= 0.34);
            if (ok) pairs.Add(Tuple.Create(l + jac * 3, i, j));
        }
        var members = new Dictionary<int, List<int>>(); for (int i = 0; i < n; i++) members[i] = new List<int> { i };
        foreach (var p in pairs.OrderByDescending(x => x.Item1)) {
            int ra = find(p.Item2), rb = find(p.Item3); if (ra == rb) continue;
            var pa = new HashSet<int>(members[ra].Select(x => all[x].Part));
            if (members[rb].Any(x => pa.Contains(all[x].Part))) continue;
            var both = members[ra].Concat(members[rb]).Select(x => all[x]).ToList();
            if (both.Select(x => x.Cls).Where(x => x != "").Distinct().Count() > 1) continue;
            if (both.Select(x => x.Sex).Where(x => x != "").Distinct().Count() > 1) continue;
            if (both.Max(x => R(x)) - both.Min(x => R(x)) > 1) continue;
            par[rb] = ra; members[ra].AddRange(members[rb]); members.Remove(rb);
        }
        return members.Values.Select(m => m.Select(x => all[x]).OrderBy(x => x.Part).ToList()).ToList();
    }
}
