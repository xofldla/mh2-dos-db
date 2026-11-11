using System;
using System.Collections.Generic;
using System.Linq;

// 방어구 세트 묶기: 같은 레어도・직업・성별에서 이름 앞부분이 같고 스킬 계통이 겹치는 부위를 한 세트로
public static class ArmorSets
{
    public class Piece { public int Part; public Dictionary<string, object> A; public string Name, Rar, Cls, Sex; public HashSet<string> Skills; public HashSet<string> Mats = new HashSet<string>(); }
    static double Jac(HashSet<string> a, HashSet<string> b) { int u = a.Union(b).Count(); return u == 0 ? 0 : (double)a.Intersect(b).Count() / u; }

    static string S(Dictionary<string, object> d, string k) { object v; return d != null && d.TryGetValue(k, out v) && v != null ? Convert.ToString(v) : ""; }

    static int Lcp(string a, string b) { int n = 0; while (n < a.Length && n < b.Length && a[n] == b[n]) n++; return n; }

    static int R(Piece p) { int r; int.TryParse(p.Rar, out r); return r; }
    static bool Compat(string a, string b) { return a == b || a == "" || b == ""; }
    static string Norm(string s) { return s.Normalize(System.Text.NormalizationForm.FormKC); }

    // 부위 이름(접미사)을 뗀 시리즈 이름. 같은 시리즈면 스킬이 달라도 한 세트 (예: バトルヘルム/メイル/アーム/フォールド/グリーヴ)
    static readonly string[] PartWords = { "ヘルム", "キャップ", "ピアス", "フェイク", "【鉢金】", "メイル", "レジスト", "ベスト", "スキン", "シェル", "【胸当て】", "【胴当て】",
        "アーム", "ガード", "カフス", "【篭手】", "フォールド", "コート", "コイル", "ベルト", "【腰当て】", "グリーヴ", "レギンス", "フット", "ブーツ", "【具足】", "【袴】" };
    static string Stem(string n)
    {
        n = n.Replace("キング", "K・"); // キングロブスタシェル ≡ Ｋ・ロブスタキャップ 시리즈
        foreach (var w in PartWords.OrderByDescending(x => x.Length)) if (n.Length > w.Length && n.EndsWith(w)) return n.Substring(0, n.Length - w.Length);
        return null;
    }
    static string Hint(string n)
    {
        foreach (var w in new[] { "ヘルム", "メイル", "アーム", "フォールド", "グリーヴ" }) if (n.EndsWith(w)) return "Blademaster";
        foreach (var w in new[] { "キャップ", "レジスト", "ガード", "コート", "レギンス" }) if (n.EndsWith(w)) return "Gunner";
        return "";
    }
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
            string sa = Stem(na), sb = Stem(nb);
            if (sa != null && sa == sb) {
                // 직업이 정해진 부위끼리 먼저 묶고, 직업 공용(빈칸) 부위는 이름(ヘルム=검사, キャップ=거너)이 맞는 쪽에 붙인다
                double bonus = (a.Cls != "" && b.Cls != "") ? 50 : 0;
                if (a.Cls == "" && Hint(na) != "" && Hint(na) == b.Cls) bonus += 1;
                if (b.Cls == "" && Hint(nb) != "" && Hint(nb) == a.Cls) bonus += 1;
                pairs.Add(Tuple.Create(100.0 + bonus + l + jac * 3, i, j));
            }
            else if (ok) pairs.Add(Tuple.Create(l + jac * 3, i, j));
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
        // 2단계: 이름이 달라 혼자 남은 부위(예: グリーンジャージー = レザーライト의 다리)를
        //        같은 레어도・같은 생산 소재인 세트의 빈 부위에 넣는다 (소재가 절반 이상 겹칠 때, 가장 잘 맞는 것부터)
        var groups = members.Values.Select(m => m.Select(x => all[x]).ToList()).ToList();
        var cands = new List<Tuple<double, List<Piece>, List<Piece>>>();
        foreach (var single in groups.Where(g => g.Count == 1)) {
            var p = single[0]; if (p.Mats.Count == 0) continue;
            foreach (var g in groups.Where(g => g.Count >= 2)) {
                if (g.Any(x => x.Part == p.Part) || g.Any(x => R(x) != R(p))) continue;
                if (!g.All(x => Compat(x.Cls, p.Cls) && Compat(x.Sex, p.Sex))) continue;
                double best = g.Max(x => Jac(x.Mats, p.Mats));
                if (best >= 0.5) cands.Add(Tuple.Create(best, single, g));
            }
        }
        var used = new HashSet<List<Piece>>();
        foreach (var c in cands.OrderByDescending(x => x.Item1)) {
            if (used.Contains(c.Item2) || c.Item3.Any(x => x.Part == c.Item2[0].Part)) continue;
            c.Item3.Add(c.Item2[0]); used.Add(c.Item2);
        }
        return groups.Where(g => !used.Contains(g)).Select(g => g.OrderBy(x => x.Part).ToList()).ToList();
    }
}
