using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

// 한글 패치 TSV(ja→ko)로 공략 JSON의 일본어 문자열을 옮긴다.
public static class Dict
{
    public static Dictionary<string, string> Map = new Dictionary<string, string>();
    public static Dictionary<string, string> Src = new Dictionary<string, string>();

    public static string Key(string s)
    {
        if (s == null) return "";
        s = s.Replace("\\n", "").Replace("\n", "");
        s = Regex.Replace(s, "~C\\d\\d", "");
        s = Regex.Replace(s, "(?s)<sub>.*?</sub>", "");
        s = Regex.Replace(s, "<[^>]+>", "");
        s = s.Normalize(NormalizationForm.FormKC);
        s = s.Replace("黑", "黒").Replace("理的な", "物理的な").Replace("物物理", "物理");
        s = Regex.Replace(s, "\\s+", "");
        return s;
    }

    public static string Clean(string ko)
    {
        ko = Regex.Replace(ko, "~C\\d\\d", "");
        ko = ko.Replace("\\n", " ").Replace("\n", " ");
        ko = Regex.Replace(ko, "(?<=[가-힣])　+(?=[가-힣])", ""); // 게임 화면 폭 맞춤용 전각 공백
        ko = Regex.Replace(ko, " {2,}", " ").Trim();
        return ko;
    }

    // 먼저 넣은 파일이 우선(이름표를 먼저 넣는다)
    public static void LoadTsv(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        foreach (var line in File.ReadAllLines(path, Encoding.UTF8)) {
            var p = line.Split('\t');
            if (p.Length < 2) continue;
            string ja, ko;
            if (p.Length >= 3 && Regex.IsMatch(p[0], "^[0-9A-Fa-f]{5,8}$")) { ja = p[1]; ko = p[2]; }
            else { ja = p[0]; ko = p[1]; }
            if (ja == "ja" || string.IsNullOrEmpty(ko)) continue;
            if (Regex.IsMatch(ko, "[ぁ-ヺ]")) continue; // 미번역(가나가 남음, ・ー는 허용)
            Put(ja, ko, name);
            // 퀘스트 제목 「≪제목≫\n부제」 → 제목·부제 따로도 등록
            if (ja.StartsWith("≪") && ko.StartsWith("≪")) {
                int a = ja.IndexOf('≫'), b = ko.IndexOf('≫');
                if (a > 0 && b > 0) { Put(ja.Substring(0, a + 1), ko.Substring(0, b + 1), name); Put(ja.Substring(a + 1), ko.Substring(b + 1), name); }
            }
            // 설명 끝의 「（精算アイテム）」 같은 꼬리표 뗀 것도 등록
            var m = Regex.Match(ja, "^(.*?)(\\\\n)?(~C\\d\\d)?（[^）]*）\\s*$", RegexOptions.Singleline);
            var m2 = Regex.Match(ko, "^(.*?)(\\\\n)?(~C\\d\\d)?\\s*\\([^)]*\\)\\s*$", RegexOptions.Singleline);
            if (m.Success && m2.Success) Put(m.Groups[1].Value, m2.Groups[1].Value, name);
        }
    }

    static void Put(string ja, string ko, string name)
    {
        string k = Key(ja); if (k.Length == 0) return;
        if (!Map.ContainsKey(k)) { Map[k] = Clean(ko); Src[k] = name; }
    }

    public static void Add(string ja, string ko) { Map[Key(ja)] = ko; Src[Key(ja)] = "manual"; }

    public static string Get(string ja)
    {
        string v; if (Map.TryGetValue(Key(ja), out v)) return v;
        string k = Key(ja);
        if (k.Length >= 30) { string f = Fuzzy(k); if (f != null) { Map[k] = f; return f; } }
        return null;
    }

    static HashSet<string> Bigrams(string s)
    {
        var h = new HashSet<string>(); for (int i = 0; i + 1 < s.Length; i++) h.Add(s.Substring(i, 2)); return h;
    }

    // 긴 설명문: 오타·표기 차이가 있어도 글자 2개 단위가 85% 이상 같으면 같은 글로 본다
    public static string Fuzzy(string k)
    {
        var a = Bigrams(k); double best = 0; string bv = null;
        foreach (var kv in Map) {
            if (kv.Key.Length < 20 || Math.Abs(kv.Key.Length - k.Length) > k.Length / 4) continue;
            var b = Bigrams(kv.Key); int n = 0; foreach (var x in a) if (b.Contains(x)) n++;
            double sc = 2.0 * n / (a.Count + b.Count);
            if (sc > best) { best = sc; bv = kv.Value; }
        }
        return best >= 0.85 ? bv : null;
    }

    public static object LoadJson(string path)
    {
        var js = new JavaScriptSerializer(); js.MaxJsonLength = int.MaxValue; js.RecursionLimit = 1000;
        return js.DeserializeObject(File.ReadAllText(path, Encoding.UTF8));
    }

    public static string EnKey(string k)
    {
        if (k == "j") return "n"; if (k == "japanese") return "name"; if (k == "japaneseTitle") return "title";
        if (k.EndsWith("Japanese")) return k.Substring(0, k.Length - 8);
        return null;
    }

    // 사전에 없는 일본어와 짝 영어
    public static void Misses(object o, Dictionary<string, string> outv)
    {
        var d = o as Dictionary<string, object>;
        if (d != null) {
            foreach (var kv in d) {
                var s = kv.Value as string; string ek = EnKey(kv.Key);
                if (s != null && ek != null && Get(s) == null && !outv.ContainsKey(s)) {
                    object en; d.TryGetValue(ek, out en); outv[s] = en as string ?? "";
                } else Misses(kv.Value, outv);
            }
            return;
        }
        var a = o as object[];
        if (a != null) foreach (var x in a) Misses(x, outv);
    }

    // JSON 안의 일본어 필드 모두 모으기
    public static void Collect(object o, string key, Dictionary<string, int> outv)
    {
        var d = o as Dictionary<string, object>;
        if (d != null) { foreach (var kv in d) Collect(kv.Value, kv.Key, outv); return; }
        var a = o as object[];
        if (a != null) { foreach (var x in a) Collect(x, key, outv); return; }
        var s = o as string;
        if (s != null && key != null && (key == "j" || key.ToLower().Contains("japanese"))) {
            int c; outv.TryGetValue(s, out c); outv[s] = c + 1;
        }
    }
}
