using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

// mholdschool MH2 위키 JSON → 한글 패치 용어로 옮긴 오프라인 HTML
public static class G
{
    public static string JsonDir, OutDir;
    public static Dictionary<string, string> Prose = new Dictionary<string, string>();
    public static Dictionary<string, string> EnName = new Dictionary<string, string>();
    public static HashSet<string> MissProse = new HashSet<string>();

    // ---------- JSON 도우미 ----------
    public static Dictionary<string, object> D(object o) { return o as Dictionary<string, object>; }
    public static object[] A(object o) { return o as object[] ?? new object[0]; }
    public static object[] A(Dictionary<string, object> d, string k) { object v; return d != null && d.TryGetValue(k, out v) ? A(v) : new object[0]; }
    public static string S(Dictionary<string, object> d, string k) { object v; if (d == null || !d.TryGetValue(k, out v) || v == null) return ""; return Convert.ToString(v, CultureInfo.InvariantCulture); }
    public static bool Has(Dictionary<string, object> d, string k) { object v; return d != null && d.TryGetValue(k, out v) && v != null && Convert.ToString(v, CultureInfo.InvariantCulture) != "" && !(v is bool && !(bool)v); }
    public static object Load(string name) { return Dict.LoadJson(Path.Combine(JsonDir, name + ".json")); }

    // ---------- 글자 ----------
    public static string H(string s) { return System.Net.WebUtility.HtmlEncode(s ?? ""); }
    public static string Tk(string ja) { if (string.IsNullOrEmpty(ja)) return ""; return Dict.Get(ja) ?? ja; }
    public static string T(string ja) { return H(Tk(ja)); }
    // 한글 + 작은 일본어
    // 이름 줄바꿈: 띄어쓰기・【・=・「・」 자리에서만 끊고, 그래도 8글자 넘게 붙은 덩어리는 가운데에서 끊는다
    public static string NB(string k)
    {
        var parts = Regex.Split(k ?? "", "(\\s+|(?=【)|(?<==)|(?<=・))");
        var sb = new StringBuilder();
        foreach (var p in parts) {
            if (p.Length == 0) continue;
            if (Regex.IsMatch(p, "^\\s+$")) { sb.Append(" "); continue; }
            string h = p.Length >= 8 && !p.StartsWith("【") ? H(p.Substring(0, (p.Length + 1) / 2)) + "<wbr>" + H(p.Substring((p.Length + 1) / 2)) : H(p);
            if (sb.Length > 0 && !sb.ToString().EndsWith(" ")) sb.Append("<wbr>");
            sb.Append(h);
        }
        return sb.ToString();
    }
    public static string TJ(string ja, string after = "") { if (string.IsNullOrEmpty(ja)) return after; string k = Tk(ja); return NB(k) + after + (k != ja ? " <span class=\"ja\">" + H(Regex.Replace(ja, "<[^>]+>", "")) + "</span>" : ""); }
    public static string P(string en)
    {
        if (string.IsNullOrEmpty(en)) return "";
        string t = en.Trim(), v;
        if (Prose.TryGetValue(t, out v)) return v;
        if (EnName.TryGetValue(t, out v)) return H(v);
        if (Regex.IsMatch(t, "^[-+0-9.,:%/ ×x&;a-z<>=()]*$") && !Regex.IsMatch(t, "[a-z]{3,}")) return t; // 숫자
        if (Regex.IsMatch(t, "^HR\\d+\\+$")) return t.Substring(0, t.Length - 1) + " 이상";
        MissProse.Add(t);
        return t;
    }
    public static string Num(string s) { return string.IsNullOrEmpty(s) ? "-" : s; }
    public static string Money(string s) { int n; return int.TryParse(s, out n) ? n.ToString("N0") + "z" : Num(s); }

    public static void LoadProse(string path)
    {
        foreach (var line in File.ReadAllLines(path, Encoding.UTF8)) {
            var p = line.Split('\t'); if (p.Length < 2 || p[0] == "en") continue;
            Prose[p[0].Trim()] = p[1];
        }
    }

    // JSON 전체에서 영어 이름 → 한글 이름
    public static void CollectNames(object o)
    {
        var d = D(o);
        if (d != null) {
            foreach (var pair in new[] { new[] { "name", "japanese" }, new[] { "n", "j" }, new[] { "stat", "statJapanese" }, new[] { "part", "partJapanese" }, new[] { "rank", "rankJapanese" }, new[] { "dish", "dishJapanese" } }) {
                string en = S(d, pair[0]), ja = S(d, pair[1]);
                if (en != "" && ja != "" && !EnName.ContainsKey(en)) { string k = Dict.Get(ja); if (k != null) EnName[en] = k; }
            }
            foreach (var kv in d) CollectNames(kv.Value);
            return;
        }
        foreach (var x in A(o)) CollectNames(x);
    }

    // ---------- 틀 ----------
    static readonly string[][] Nav = {
        new[]{"index.html","홈"}, new[]{"weapons.html","무기"}, new[]{"armor.html","방어구"}, new[]{"armor-skills.html","스킬"},
        new[]{"items.html","아이템"}, new[]{"bestiary.html","몬스터"}, new[]{"quests.html","퀘스트"}, new[]{"maps.html","맵"},
        new[]{"meals.html","식사"}, new[]{"miscellany.html","기타"}, new[]{"villagepeople.html","마을 사람"} };

    public static string Head(string file, string title, string sub, bool search)
    {
        var sb = new StringBuilder();
        sb.Append("<!doctype html>\n<html lang=\"ko\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
        sb.Append("<title>" + H(title) + " | 몬스터헌터2 (DOS) DB</title>\n<link rel=\"stylesheet\" href=\"assets/guide.css\">\n<link rel=\"stylesheet\" href=\"assets/icons.css\">\n<script src=\"assets/theme.js\"></script>\n</head><body>\n");
        sb.Append("<header class=\"top\"><div class=\"wrap\"><a class=\"logo\" href=\"index.html\">몬스터헌터2 (DOS) DB</a><nav>");
        foreach (var n in Nav) sb.Append("<a href=\"" + n[0] + "\"" + (n[0] == file ? " class=\"on\"" : "") + ">" + n[1] + "</a>");
        sb.Append("</nav></div></header>\n<main class=\"wrap\">\n<h1>" + H(title) + "</h1>\n");
        if (sub != null) sb.Append("<p class=\"sub\">" + sub + "</p>\n");
        sb.Append("<div class=\"tools\">");
        if (search) sb.Append("<input id=\"q\" type=\"search\" placeholder=\"검색 (이름・소재・스킬…)\" autocomplete=\"off\"><span id=\"qn\"></span>");
        sb.Append("</div>\n");
        return sb.ToString();
    }

    public static string Foot()
    {
        return "\n<footer>자료: <a href=\"https://wiki.mholdschool.com/mh2/\">Monster Hunter Oldschool MH2 Wiki</a> (mmPotato 「モンスターハンター２解析情報」 및 데이터 추출 자료 기반). " +
            "용어는 몬스터헌터 2 (도스) 한글 패치 표기를 따랐습니다. 설명문은 한국어로 새로 옮겼습니다. 지도・아이콘・몬스터 그림은 같은 위키의 것입니다.</footer>\n</main>\n<script src=\"assets/guide.js\"></script>\n</body></html>\n";
    }

    // 이름 → 다른 페이지로 가는 링크
    public static Dictionary<string, string> ItemLink = new Dictionary<string, string>(), MonLink = new Dictionary<string, string>();
    public static void BuildLinks()
    {
        ItemLink.Clear(); MonLink.Clear(); int idx = 0;
        foreach (var o in A(Load("items"))) { var it = D(o); idx++; if (Has(it, "hide")) continue; string j = S(it, "japanese"); if (j != "" && !ItemLink.ContainsKey(j)) ItemLink[j] = "items.html#item-" + idx; }
        foreach (var o in A(Load("monsters"))) { var m = D(o); string j = S(m, "japanese"); if (j != "" && !MonLink.ContainsKey(j)) MonLink[j] = "bestiary.html#w-" + S(m, "id"); }
    }
    public static string ILink(string ja, string inner) { string u; return ItemLink.TryGetValue(ja, out u) ? "<a class=\"xlink\" href=\"" + u + "\">" + inner + "</a>" : inner; }
    public static string MLink(string ja, string inner) { string u; return MonLink.TryGetValue(ja, out u) ? "<a class=\"xlink\" href=\"" + u + "\">" + inner + "</a>" : inner; }

    public static void Write(string file, string html) { File.WriteAllText(Path.Combine(OutDir, file), html, new UTF8Encoding(false)); }

    // ---------- 공통 조각 ----------
    static readonly Dictionary<string, string[]> Attr = new Dictionary<string, string[]> {
        {"fire", new[]{"화","fire"}}, {"water", new[]{"수","water"}}, {"thunder", new[]{"뇌","thunder"}}, {"ice", new[]{"빙","ice"}}, {"dragon", new[]{"용","dragon"}},
        {"poison", new[]{"독","poison"}}, {"paralysis", new[]{"마비","para"}}, {"sleep", new[]{"수면","sleep"}}, {"snowman", new[]{"눈사람","ice"}}, {"stun", new[]{"기절","para"}},
        {"stench", new[]{"악취","poison"}}, {"silence", new[]{"성대마비","sleep"}}, {"blast", new[]{"폭발","fire"}} };
    public static string AttrChip(string a, string val)
    {
        if (string.IsNullOrEmpty(a)) return "-";
        string[] v; if (!Attr.TryGetValue(a.ToLower(), out v)) v = new[] { a, "" };
        return "<span class=\"el " + v[1] + "\">" + v[0] + (string.IsNullOrEmpty(val) ? "" : " " + H(val)) + "</span>";
    }

    public static string Mats(object[] list)
    {
        if (list.Length == 0) return "";
        var parts = new List<string>();
        foreach (var o in list) { var d = D(o); string q = S(d, "q"); if (q == "") q = S(d, "quantity"); string jn = S(d, "j") != "" ? S(d, "j") : S(d, "japanese"); parts.Add("<span class=\"mat\">" + ILink(jn, TJ(jn, q != "" ? " ×" + H(q) : "")) + "</span>"); }
        return string.Join(" ", parts);
    }

    public static string Rar(string r) { return r == "" ? "-" : "<span class=\"rar r" + H(r) + "\">" + H(r) + "</span>"; }

    static readonly string[] SharpCol = { "red", "orange", "yellow", "green", "blue", "white", "purple" };
    public static string SharpBar(object[] lv, string cls)
    {
        if (lv.Length == 0) return "";
        var sb = new StringBuilder("<span class=\"sh " + cls + "\">"); int prev = 0;
        for (int i = 0; i < lv.Length; i++) {
            int v = Convert.ToInt32(lv[i], CultureInfo.InvariantCulture); int w = v - prev; prev = v;
            if (w > 0) sb.Append("<i class=\"" + SharpCol[Math.Min(i, 6)] + "\" style=\"width:" + (w * 100.0 / 120).ToString("0.##", CultureInfo.InvariantCulture) + "%\"></i>");
        }
        return sb.Append("</span>").ToString();
    }

    // 「표」 시작/끝
    public static string Table(string cls, params string[] heads)
    {
        var sb = new StringBuilder("<div class=\"tw\"><table class=\"" + cls + "\"><thead><tr>");
        foreach (var h in heads) sb.Append("<th>" + h + "</th>");
        return sb.Append("</tr></thead><tbody>\n").ToString();
    }
    // 열 너비 고정 표 (같은 종류 표끼리 칸이 맞게)
    public static string Cols(string cls, int minW, int[] pct, params string[] heads)
    {
        var sb = new StringBuilder("<div class=\"tw\"><table class=\"" + cls + " fixed\" style=\"min-width:" + minW + "px\"><colgroup>");
        foreach (var x in pct) sb.Append("<col style=\"width:" + x + "%\">");
        sb.Append("</colgroup><thead><tr>");
        foreach (var h in heads) sb.Append("<th>" + h + "</th>");
        return sb.Append("</tr></thead><tbody>\n").ToString();
    }
    public const string TEnd = "</tbody></table></div>\n";
    public static string Tr(params string[] cells)
    {
        var sb = new StringBuilder("<tr>"); foreach (var c in cells) sb.Append("<td>" + c + "</td>"); return sb.Append("</tr>\n").ToString();
    }
    public static string Slug(string s) { return Regex.Replace((s ?? "").ToLower(), "[^a-z0-9]+", "-").Trim('-'); }

    // ================= 무기 =================
    static readonly string[][] Ammo = {
        new[]{"normal","通常弾"}, new[]{"pierce","貫通弾"}, new[]{"pellet","散弾"}, new[]{"crag","徹甲榴弾"}, new[]{"clust","拡散弾"},
        new[]{"recover","回復弾"}, new[]{"poison","毒弾"}, new[]{"para","麻痺弾"}, new[]{"sleep","睡眠弾"},
        new[]{"flame","火炎弾"}, new[]{"water","水冷弾"}, new[]{"thunder","電撃弾"}, new[]{"freeze","氷結弾"}, new[]{"dragon","滅龍弾"},
        new[]{"demon","鬼人弾"}, new[]{"armor","硬化弾"} };
    static readonly Dictionary<string, string> NoteCol = new Dictionary<string, string> { { "W", "white" }, { "B", "blue" }, { "R", "red" }, { "P", "purple" }, { "Y", "yellow" }, { "G", "green" }, { "C", "cyan" }, { "O", "orange" } };

    static string AmmoCell(Dictionary<string, object> w)
    {
        var parts = new List<string>();
        foreach (var am in Ammo) {
            var arr = A(w, am[0]); var add = A(w, am[0] + "Add");
            if (arr.Length == 0 && add.Length == 0) continue;
            var lv = new List<string>(); bool any = false;
            int n = Math.Max(arr.Length, add.Length);
            for (int i = 0; i < n; i++) {
                string v = i < arr.Length ? Convert.ToString(arr[i]) : ""; string a = i < add.Length ? Convert.ToString(add[i]) : "";
                if (v != "") { lv.Add(H(v)); any = true; } else if (a != "") { lv.Add("<em title=\"스킬로 추가\">+" + H(a) + "</em>"); any = true; } else lv.Add("·");
            }
            if (any) parts.Add("<span class=\"ammo\"><b>" + T(am[1]) + "</b> " + string.Join("/", lv) + "</span>");
        }
        return string.Join(" ", parts);
    }

    public static void Weapons()
    {
        var data = A(Load("weapons"));
        var sb = new StringBuilder(Head("weapons.html", "무기 (표)", "무기 종류별 성능과 생산・강화 소재를 표로 봅니다. <a href=\"weapons.html\">강화 트리로 보기</a>. 예리도 막대: 위는 기본, 아래 얇은 줄은 스킬 「장인」 적용 시.", true));
        sb.Append("<div class=\"toc\">");
        foreach (var c in data) { var cd = D(c); var cls = A(cd, "weapons").Select(x => S(D(x), "weaponClass")).Distinct(); foreach (var wc in cls) sb.Append("<a href=\"#" + Slug(wc) + "\">" + Ico("white", ClassIcon(wc)) + H(ClassKo(wc)) + "</a>"); }
        sb.Append("</div>\n");
        foreach (var c in data) {
            var cd = D(c);
            foreach (var g in A(cd, "weapons").GroupBy(x => S(D(x), "weaponClass"))) {
                string wc = g.Key; bool gun = wc.Contains("Bowgun"), bow = wc == "Bow";
                sb.Append("<section class=\"grp\" id=\"" + Slug(wc) + "\"><h2>" + Ico("white", ClassIcon(wc), " big") + H(ClassKo(wc)) + "</h2>\n");
                if (gun) sb.Append(Cols("ft wpn gun", 900, new[] { 14, 4, 5, 5, 6, 9, 8, 22, 7, 20 }, "이름", "레어", "공격력", "회심", "슬롯", "장전 / 반동", "속사", "장전 가능 탄 (LV1/2/3)", "가격", "생산・강화 소재"));
                else if (bow) sb.Append(Cols("ft wpn", 900, new[] { 15, 4, 5, 7, 5, 6, 9, 11, 7, 31 }, "이름", "레어", "공격력", "속성", "회심", "슬롯", "모으기 단계", "병", "가격", "생산・강화 소재"));
                else sb.Append(Cols("ft wpn", 900, new[] { 15, 4, 5, 7, 5, 6, 12, 9, 7, 30 }, "이름", "레어", "공격력", "속성", "회심", "슬롯", "예리도", "특성", "가격", "생산・강화 소재"));
                foreach (var o in g) {
                    var w = D(o); if (Has(w, "dev")) continue;
                    string name = Ico(RC(S(w, "rarity")), ClassIcon(wc)) + "<b>" + TJ(S(w, "japanese")) + "</b>";
                    if (Has(w, "acquire")) name += "<div class=\"note\">" + P(S(w, "acquire")) + "</div>";
                    string aff = S(w, "affinity"); aff = aff == "" || aff == "0" ? "-" : aff + "%";
                    string slots = S(w, "slots"); slots = slots == "" || slots == "0" ? "-" : new string('◯', int.Parse(slots));
                    string forge = Mats(A(w, "forge")); if (A(w, "ticketForge").Length > 0) forge += (forge != "" ? "<br>" : "") + "<span class=\"tk\">교환권:</span> " + Mats(A(w, "ticketForge"));
                    string up = Mats(A(w, "upgrade")); string mats = (forge != "" ? "<div class=\"mrow\"><span class=\"mk\">생산</span>" + forge + "</div>" : "") + (up != "" ? "<div class=\"mrow\"><span class=\"mk up\">강화</span>" + up + "</div>" : "");
                    string atk = "<b>" + Num(S(w, "attack")) + "</b>";
                    if (gun) {
                        string rr = (S(w, "reloadJapanese") != "" ? T(S(w, "reloadJapanese")) : "-") + " / " + (S(w, "recoilJapanese") != "" ? T(S(w, "recoilJapanese")) : "-");
                        string rf = S(w, "rapidfireJapanese") != "" ? T(S(w, "rapidfireJapanese")) : "-";
                        sb.Append(Tr(name, Rar(S(w, "rarity")), atk, aff, slots, rr, rf, AmmoCell(w), Money(S(w, "price")), mats));
                    } else if (bow) {
                        var ch = A(w, "chargeLevels").Select(x => T(S(D(x), "typeJapanese")) + " " + H(S(D(x), "level")));
                        var coat = new List<string>();
                        foreach (var cc in new[] { new[] { "powerCoating", "強撃ビン" }, new[] { "poisonCoating", "毒ビン" }, new[] { "paraCoating", "麻痺ビン" }, new[] { "sleepCoating", "睡眠ビン" } })
                            if (Has(w, cc[0])) coat.Add(T(cc[1]));
                        sb.Append(Tr(name, Rar(S(w, "rarity")), atk, AttrChip(S(w, "attribute"), S(w, "attributeValue")), aff, slots, string.Join("<br>", ch), string.Join(" ", coat), Money(S(w, "price")), mats));
                    } else {
                        var sh = D(w.ContainsKey("sharpnessLevels") ? w["sharpnessLevels"] : null);
                        string bar = sh == null ? "" : SharpBar(A(sh, "normal"), "n") + SharpBar(A(sh, "handicraft"), "h");
                        string extra = "";
                        if (S(w, "shellTypeJapanese") != "") extra = "포격: " + T(S(w, "shellTypeJapanese")) + " Lv" + H(S(w, "shellLevel"));
                        if (S(w, "noteLetters") != "") extra = "음색: " + string.Join("", S(w, "noteLetters").Split(',').Select(n => { string c2; NoteCol.TryGetValue(n.Trim(), out c2); return "<i class=\"note-dot " + c2 + "\" title=\"" + H(n) + "\"></i>"; }));
                        sb.Append(Tr(name, Rar(S(w, "rarity")), atk, AttrChip(S(w, "attribute"), S(w, "attributeValue")), aff, slots, bar, extra, Money(S(w, "price")), mats));
                    }
                }
                sb.Append(TEnd).Append("</section>\n");
            }
        }
        Write("weapons-table.html", sb.Append(Foot()).ToString());
    }

    static string ClassKo(string wc)
    {
        switch (wc) {
            case "Great Sword": return Tk("大剣"); case "Longsword": return Tk("太刀"); case "Hammer": return Tk("ハンマー");
            case "Hunting Horn": return "수렵피리"; case "Lance": return Tk("ランス"); case "Gunlance": return Tk("ガンランス");
            case "Sword and Shield": return Tk("片手剣"); case "Dual Swords": return Tk("双剣");
            case "Light Bowgun": return Tk("ライトボウガン"); case "Heavy Bowgun": return Tk("ヘビィボウガン"); case "Bow": return Tk("弓");
        }
        return wc;
    }

    // ---------- 무기・방어구 아이콘 ----------
    // 게임 속 레어도 색 (R1 흰색 … R10)
    static readonly string[] RarCol = { "white", "white", "purple", "yellow", "pink", "green", "blue", "red", "cyan", "orange", "gray" };
    static string RC(string r) { int n; return int.TryParse(r, out n) && n >= 0 && n < RarCol.Length ? RarCol[n] : "white"; }
    static readonly string[] ArmorIcon = { "helm", "torso", "arms", "waist", "legs" };
    public static string Ico(string color, string kind, string extra = "") { return "<i class=\"ico " + color + "-icon " + kind + "-icon" + extra + "\"></i>"; }
    static string ClassIcon(string wc)
    {
        switch (wc) {
            case "Great Sword": return "great-sword"; case "Longsword": return "longsword"; case "Hammer": return "hammer";
            case "Hunting Horn": return "hunting-horn"; case "Lance": return "lance"; case "Gunlance": return "gunlance";
            case "Sword and Shield": return "sword"; case "Dual Swords": return "dual-swords";
            case "Light Bowgun": return "light-bowgun"; case "Heavy Bowgun": return "heavy-bowgun"; case "Bow": return "bow";
        }
        return "mystery";
    }
    static string PartIcon(string cat, int ci, string extra) { return cat == "decorations" ? Ico("white", "gem", extra) : ci < 5 ? Ico("white", ArmorIcon[ci], extra) : ""; }

    // ================= 무기 강화 트리 =================
    static readonly string[] WClasses = { "Great Sword", "Longsword", "Sword and Shield", "Dual Swords", "Hammer", "Hunting Horn", "Lance", "Gunlance", "Light Bowgun", "Heavy Bowgun", "Bow" };

    static string WeaponDetail(WeaponTree.Node n)
    {
        var w = n.W; string wc = S(w, "weaponClass"); bool gun = wc.Contains("Bowgun"), bow = wc == "Bow";
        var sb = new StringBuilder("<div class=\"wd-head\">" + Ico(RC(S(w, "rarity")), ClassIcon(wc), " big") + "<div><b class=\"wd-name\">" + T(S(w, "japanese")) + "</b><div class=\"wd-ja\">" + H(S(w, "japanese")) + "</div><div class=\"meta\">" + H(ClassKo(wc)) + " · 레어 " + H(S(w, "rarity")) + "</div></div></div>");
        var rows = new List<string[]>();
        rows.Add(new[] { "공격력", "<b>" + Num(S(w, "attack")) + "</b>" });
        if (!gun) rows.Add(new[] { "속성", AttrChip(S(w, "attribute"), S(w, "attributeValue")) });
        string aff = S(w, "affinity"); rows.Add(new[] { "회심", aff == "" || aff == "0" ? "-" : H(aff) + "%" });
        rows.Add(new[] { "슬롯", Slots(S(w, "slots")) });
        if (S(w, "defense") != "") rows.Add(new[] { "방어", "+" + H(S(w, "defense")) });
        if (gun) {
            rows.Add(new[] { "장전 / 반동", (S(w, "reloadJapanese") != "" ? T(S(w, "reloadJapanese")) : "-") + " / " + (S(w, "recoilJapanese") != "" ? T(S(w, "recoilJapanese")) : "-") });
            if (S(w, "rapidfireJapanese") != "") rows.Add(new[] { "속사", T(S(w, "rapidfireJapanese")) });
            rows.Add(new[] { "장전 가능 탄", AmmoCell(w) });
        } else if (bow) {
            rows.Add(new[] { "모으기", string.Join(" / ", A(w, "chargeLevels").Select(x => T(S(D(x), "typeJapanese")) + " " + H(S(D(x), "level")))) });
            var coat = new List<string>();
            foreach (var cc in new[] { new[] { "powerCoating", "強撃ビン" }, new[] { "poisonCoating", "毒ビン" }, new[] { "paraCoating", "麻痺ビン" }, new[] { "sleepCoating", "睡眠ビン" } }) if (Has(w, cc[0])) coat.Add(T(cc[1]));
            rows.Add(new[] { "병", coat.Count > 0 ? string.Join(" ", coat) : "-" });
        } else {
            var sh = D(w.ContainsKey("sharpnessLevels") ? w["sharpnessLevels"] : null);
            if (sh != null) rows.Add(new[] { "예리도", SharpBar(A(sh, "normal"), "n wide") + "<div class=\"meta\">장인 적용</div>" + SharpBar(A(sh, "handicraft"), "n wide") });
            if (S(w, "shellTypeJapanese") != "") rows.Add(new[] { "포격", T(S(w, "shellTypeJapanese")) + " Lv" + H(S(w, "shellLevel")) });
            if (S(w, "noteLetters") != "") rows.Add(new[] { "음색", string.Join("", S(w, "noteLetters").Split(',').Select(x => { string c2; NoteCol.TryGetValue(x.Trim(), out c2); return "<i class=\"note-dot " + c2 + "\"></i>"; })) });
        }
        rows.Add(new[] { "가격", Money(S(w, "price")) });
        sb.Append("<table class=\"wd-stats\">" + string.Join("", rows.Select(r => "<tr><th>" + r[0] + "</th><td>" + r[1] + "</td></tr>")) + "</table>");
        string forge = Mats(A(w, "forge")); if (A(w, "ticketForge").Length > 0) forge += " <span class=\"tk\">교환권:</span> " + Mats(A(w, "ticketForge"));
        sb.Append("<h4 class=\"wd-h\"><span class=\"mk\">생산</span> 바로 만들기</h4><div class=\"wd-mats\">" + (forge != "" ? forge : "<span class=\"meta\">바로 만들 수 없음 (강화로만 입수)</span>") + "</div>");
        if (n.Parent != null) sb.Append("<h4 class=\"wd-h\"><span class=\"mk up\">강화</span> <button type=\"button\" class=\"wd-link\" data-w=\"" + n.Parent.Id + "\">" + Ico(RC(S(n.Parent.W, "rarity")), ClassIcon(S(n.Parent.W, "weaponClass"))) + T(S(n.Parent.W, "japanese")) + "</button> 에서</h4><div class=\"wd-mats\">" + (A(w, "upgrade").Length > 0 ? Mats(A(w, "upgrade")) : "-") + "</div>");
        if (n.Kids.Count > 0) sb.Append("<h4 class=\"wd-h\">다음 강화</h4><div class=\"wd-next\">" + string.Join("", n.Kids.Select(k => "<button type=\"button\" class=\"wd-link\" data-w=\"" + k.Id + "\">" + Ico(RC(S(k.W, "rarity")), ClassIcon(S(k.W, "weaponClass"))) + T(S(k.W, "japanese")) + "</button>")) + "</div>");
        else sb.Append("<div class=\"wd-final\">★ 최종 강화</div>");
        if (Has(w, "acquire")) sb.Append("<p class=\"note\">" + P(S(w, "acquire")) + "</p>");
        return sb.ToString();
    }

    public static void WeaponsTree(string srcHtml, string codesFile)
    {
        var weapons = new List<Dictionary<string, object>>();
        foreach (var c in A(Load("weapons"))) foreach (var o in A(D(c), "weapons")) weapons.Add(D(o));
        var nodes = WeaponTree.Build(File.ReadAllText(srcHtml, Encoding.UTF8), File.ReadAllText(codesFile), weapons);
        var sb = new StringBuilder(Head("weapons.html", "무기 강화 트리", "생산(처음 만들기)에서 최종 강화까지 화살표로 잇습니다. 가로로 이어지는 줄이 본줄, 아래로 갈라지는 줄이 「파생」입니다. <b>아이콘을 누르면</b> 성능과 생산・강화 소재가 나옵니다. 아이콘 색 = 레어도. <a href=\"weapons-table.html\">표로 보기</a>", false));
        sb.Append("<div class=\"tools wtools\"><input id=\"wq\" type=\"search\" placeholder=\"무기 이름 찾기\" autocomplete=\"off\"><span id=\"wqn\"></span></div>");
        sb.Append("<div class=\"vtabs wtabs\">");
        for (int i = 0; i < WClasses.Length; i++) sb.Append("<button type=\"button\" data-c=\"" + i + "\"" + (i == 0 ? " class=\"on\"" : "") + ">" + Ico("white", ClassIcon(WClasses[i])) + H(ClassKo(WClasses[i])) + "</button>");
        sb.Append("</div>\n");
        for (int i = 0; i < WClasses.Length; i++) {
            sb.Append("<section class=\"wcls" + (i == 0 ? "" : " off") + "\" data-c=\"" + i + "\"><h2>" + Ico("white", ClassIcon(WClasses[i]), " big") + H(ClassKo(WClasses[i])) + "</h2><div class=\"wscroll\">");
            sb.Append(WeaponTree.Render(nodes, WClasses[i],
                n => Ico(RC(S(n.W, "rarity")), ClassIcon(S(n.W, "weaponClass"))) + "<span class=\"nm\">" + NB(Tk(S(n.W, "japanese"))) + "</span>",
                n => T(S(n.W, "japanese"))));
            sb.Append("</div></section>\n");
        }
        sb.Append("<div id=\"wpop\" hidden><div class=\"wp-box\" role=\"dialog\" aria-modal=\"true\"><button type=\"button\" class=\"wp-x\" aria-label=\"닫기\">✕</button><div class=\"wp-body\"></div></div></div>\n");
        sb.Append("<script>window.WD={");
        sb.Append(string.Join(",", nodes.Select(n => n.Id + ":" + Js(WeaponDetail(n)))));
        sb.Append("};window.WC={" + string.Join(",", nodes.Select(n => n.Id + ":" + Array.IndexOf(WClasses, S(n.W, "weaponClass")))) + "};</script>\n");
        Write("weapons.html", sb.Append(Foot()).ToString());
    }
    static string Js(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("</", "<\\/") + "\""; }

    // ================= 방어구 =================
    static readonly string[] ArmorCat = { "머리", "몸통", "팔", "허리", "다리" };
    static readonly string[] PartSlug = { "head", "torso", "arms", "waist", "legs" };
    static string ArmorRow(Dictionary<string, object> a, int ci, string id)
    {
        var def = A(a, "defense"); var sl = A(a, "slots");
        string defs = def.Length == 0 ? "-" : def.Length == 1 ? H(Convert.ToString(def[0])) : H(Convert.ToString(def[0])) + " → <b>" + H(Convert.ToString(def[def.Length - 1])) + "</b>";
        string sls = sl.Length == 0 ? "-" : Slots(sl[0]) + (sl.Length > 1 && Convert.ToString(sl[0]) != Convert.ToString(sl[sl.Length - 1]) ? " → " + Slots(sl[sl.Length - 1]) : "");
        var res = A(a, "resistances").Select(x => Convert.ToString(x)).ToArray();
        string ress = res.Length < 5 ? "-" : string.Join(" ", new[] { "fire", "water", "thunder", "ice", "dragon" }.Select((e, i) => "<span class=\"res " + e + (res[i] == "0" ? " z" : "") + "\">" + H(res[i]) + "</span>"));
        string kind = (S(a, "hunterClassJapanese") != "" ? T(S(a, "hunterClassJapanese")) : "공용") + (S(a, "sexJapanese") != "" ? "・" + T(S(a, "sexJapanese")) : "");
        string skills = string.Join("<br>", A(a, "skills").Select(x => T(S(D(x), "j")) + " <b>" + Sign(S(D(x), "q")) + "</b>"));
        string forge = Mats(A(a, "forge")); if (Has(a, "acquire")) forge += "<div class=\"note\">" + P(S(a, "acquire")) + "</div>";
        var ups = A(a, "upgradeLevels").Select(x => { var u = D(x); return "<span class=\"up\">Lv" + H(S(u, "level")) + ": " + Money(S(u, "price")) + (A(u, "materials").Length > 0 ? " " + Mats(A(u, "materials")) : "") + "</span>"; });
        var cells = new[] { Ico(RC(S(a, "rarity")), ArmorIcon[ci]) + "<b>" + TJ(S(a, "japanese")) + "</b>", kind, Rar(S(a, "rarity")), defs, sls, ress, skills, forge, Money(S(a, "cost")) };
        return "<tr id=\"" + id + "\">" + string.Join("", cells.Select(c => "<td>" + c + "</td>")) + "</tr>\n";
    }

    // 강화 비용・소재: 접어 두고 누를 때만 펼침
    static string UpsBox(List<Dictionary<string, object>> list, List<int> parts)
    {
        if (!list.Any(a => A(a, "upgradeLevels").Length > 0)) return "";
        var sb = new StringBuilder("<details class=\"ups\"><summary>강화 비용・소재 보기</summary><div class=\"tw\"><table class=\"st upt\"><thead><tr><th>장비</th><th>강화 단계: 비용・소재</th></tr></thead><tbody>");
        for (int i = 0; i < list.Count; i++) {
            var a = list[i]; var ups = A(a, "upgradeLevels"); if (ups.Length == 0) continue;
            sb.Append("<tr><td>" + Ico(RC(S(a, "rarity")), ArmorIcon[parts[i]]) + "<b>" + T(S(a, "japanese")) + "</b></td><td>" + string.Join("", ups.Select(x => { var u = D(x); return "<div class=\"upl\"><b>Lv" + H(S(u, "level")) + "</b> " + Money(S(u, "price")) + (A(u, "materials").Length > 0 ? " · " + Mats(A(u, "materials")) : "") + "</div>"; })) + "</td></tr>");
        }
        return sb.Append("</tbody></table></div></details>").ToString();
    }

    // 검사/거너 → 레어도 / 장식주 이동 탭  (cls: 0 검사, 1 거너, 2 장식주)
    static readonly string[] ClsName = { "검사", "거너" }, ClsFile = { "b", "g" }, ClsIcon = { "great-sword", "light-bowgun" };
    static string ArmorTabs(int cls, string cur, List<int>[] rarsBy, List<int> decoSlots)
    {
        var sb = new StringBuilder("<div class=\"vtabs parts\">");
        for (int c = 0; c < 2; c++) sb.Append("<a class=\"tab big" + (c == cls ? " on" : "") + "\" href=\"armor-" + ClsFile[c] + "-r" + (cur.StartsWith("r") && rarsBy[c].Contains(int.Parse(cur.Substring(1))) ? cur.Substring(1) : rarsBy[c][0].ToString()) + ".html\">" + Ico("white", ClsIcon[c]) + ClsName[c] + "</a>");
        sb.Append("<a class=\"tab big" + (cls == 2 ? " on" : "") + "\" href=\"armor-deco-s" + decoSlots[0] + ".html\">" + Ico("white", "gem") + "장식주</a>");
        sb.Append("<a class=\"tab home\" href=\"armor.html\" title=\"방어구 목차・전체 검색\">↩ 처음으로</a></div>\n<div class=\"vtabs sub\">");
        if (cls < 2) foreach (var r in rarsBy[cls]) sb.Append("<a class=\"tab" + ("r" + r == cur ? " on" : "") + "\" href=\"armor-" + ClsFile[cls] + "-r" + r + ".html\">" + Ico(RC(r.ToString()), "torso") + "레어 " + r + "</a>");
        else foreach (var n in decoSlots) sb.Append("<a class=\"tab" + ("s" + n == cur ? " on" : "") + "\" href=\"armor-deco-s" + n + ".html\">" + Slots(n) + " 슬롯 " + n + "개</a>");
        return sb.Append("</div>\n").ToString();
    }

    static int Ri(Dictionary<string, object> a) { int r; int.TryParse(S(a, "rarity"), out r); return r; }

    // 세트 이름: 한글 이름들의 공통 앞부분
    static string SetName(List<ArmorSets.Piece> set)
    {
        var names = set.Select(p => Tk(S(p.A, "japanese"))).ToList();
        string pre = names[0];
        foreach (var n in names.Skip(1)) { int k = 0; while (k < pre.Length && k < n.Length && pre[k] == n[k]) k++; pre = pre.Substring(0, k); }
        pre = pre.TrimEnd(' ', '・', '【', '[', '(', '·');
        if (set.Count == 1 || pre.Length < 2) return names[0] + (set.Count > 1 ? " 외" : "");
        return pre;
    }

    // 세트 전체 착용 시: 방어력・내성・스킬 포인트 합계와 발동 스킬
    static string SetSummary(List<ArmorSets.Piece> set, Dictionary<string, Dictionary<string, object>> skillTree)
    {
        int d0 = 0, d1 = 0; var res = new int[5];
        var pts = new Dictionary<string, int>(); var order = new List<string>();
        int torsoUp = 0; ArmorSets.Piece torso = set.FirstOrDefault(p => p.Part == 1);
        foreach (var p in set) {
            var def = A(p.A, "defense"); if (def.Length > 0) { d0 += Convert.ToInt32(def[0]); d1 += Convert.ToInt32(def[def.Length - 1]); }
            var rs = A(p.A, "resistances"); for (int i = 0; i < 5 && i < rs.Length; i++) res[i] += Convert.ToInt32(rs[i]);
            foreach (var x in A(p.A, "skills")) {
                string j = S(D(x), "j"); if (j == "胴系統倍加") { torsoUp++; continue; }
                int q; int.TryParse(S(D(x), "q").Replace("+", ""), out q);
                if (!pts.ContainsKey(j)) { pts[j] = 0; order.Add(j); } pts[j] += q;
            }
        }
        if (torsoUp > 0 && torso != null) foreach (var x in A(torso.A, "skills")) {
            string j = S(D(x), "j"); if (j == "胴系統倍加") continue; int q; int.TryParse(S(D(x), "q").Replace("+", ""), out q);
            if (!pts.ContainsKey(j)) { pts[j] = 0; order.Add(j); } pts[j] += q * torsoUp;
        }
        var sb = new StringBuilder("<div class=\"setsum\"><div class=\"sumrow\"><span class=\"lbl\">세트 합계</span> 방어력 " + d0 + " → <b>" + d1 + "</b>　내성 ");
        sb.Append(string.Join(" ", new[] { "fire", "water", "thunder", "ice", "dragon" }.Select((e, i) => "<span class=\"res " + e + (res[i] == 0 ? " z" : "") + "\">" + res[i] + "</span>")));
        sb.Append("</div><div class=\"sumrow\"><span class=\"lbl\">스킬 포인트</span> " + string.Join("　", order.Select(j => T(j) + " <b class=\"" + (pts[j] < 0 ? "neg" : "pos") + "\">" + (pts[j] > 0 ? "+" : "") + pts[j] + "</b>")) + (torsoUp > 0 ? " <span class=\"tag\">동계통배가 " + torsoUp + "부위 반영</span>" : "") + "</div>");
        // 발동 스킬
        var act = new List<string>();
        foreach (var j in order) {
            Dictionary<string, object> tree; if (!skillTree.TryGetValue(j, out tree)) continue;
            int v = pts[j]; Dictionary<string, object> best = null; int bestP = 0;
            foreach (var st in A(tree, "stages")) {
                var sd = D(st); int sp; if (!int.TryParse(S(sd, "points").Replace("+", ""), out sp) || sp == 0) continue;
                if (sp > 0 && v >= sp && sp > bestP) { best = sd; bestP = sp; }
                if (sp < 0 && v <= sp && sp < bestP) { best = sd; bestP = sp; }
            }
            if (best == null) continue;
            string d = S(best, "d"); d = Regex.Replace(d, "</span>$", "");
            act.Add("<li class=\"" + (bestP < 0 ? "neg" : "pos") + "\"><b>" + T(S(best, "j")) + "</b> <span class=\"meta\">(" + T(j) + " " + (v > 0 ? "+" : "") + v + ")</span> : " +
                (d != "" ? "<span class=\"eff\">" + P(d) + "</span> " : "") + "<span class=\"desc\">" + DescClean(P(S(tree, "desc")), false) + "</span></li>");
        }
        sb.Append(act.Count > 0 ? "<ul class=\"actsk\">" + string.Join("", act) + "</ul>" : "<div class=\"sumrow note\">세트만으로 발동하는 스킬은 없습니다 (장식주로 보충 필요).</div>");
        return sb.Append("</div>").ToString();
    }

    public static void Armor()
    {
        var data = A(Load("armor"));
        var skillTree = new Dictionary<string, Dictionary<string, object>>();
        foreach (var o in A(Load("armor_skills"))) skillTree[S(D(o), "japanese")] = D(o);
        // 부위별 방어구 / 장식주 모으기
        var all = new List<ArmorSets.Piece>(); var decos = new List<Dictionary<string, object>>();
        int ci = 0;
        foreach (var c in data) {
            var cd = D(c);
            if (ci < 5) foreach (var o in A(cd, "armor")) {
                var a = D(o); if (Has(a, "dev") || S(a, "index") == "0" || S(a, "rarity") == "") continue;
                var p = new ArmorSets.Piece { Part = ci, A = a, Name = S(a, "japanese"), Rar = S(a, "rarity"), Cls = S(a, "hunterClass"), Sex = S(a, "sex"), Skills = new HashSet<string>(A(a, "skills").Select(x => S(D(x), "j"))) };
                all.Add(p);
            }
            foreach (var o in A(cd, "decorations")) { var a = D(o); if (!Has(a, "dev")) decos.Add(a); }
            if (S(cd, "category") != "decorations") ci++;
        }
        var sets = ArmorSets.Group(all);
        // 세트 레어도 = 가장 많은 레어도(같으면 낮은 쪽)
        Func<List<ArmorSets.Piece>, int> setRar = s => s.GroupBy(p => Ri(p.A)).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;
        var rars = sets.Select(setRar).Distinct().OrderBy(x => x).ToList();
        var dslots = decos.Select(a => int.Parse(S(a, "slots"))).Distinct().OrderBy(x => x).ToList();
        var index = new StringBuilder(); var counts = new Dictionary<int, int[]>();

        Func<List<ArmorSets.Piece>, int> clsOrd = s => { var c = s.Select(p => p.Cls).FirstOrDefault(x => x != "") ?? ""; return c == "Blademaster" ? 0 : c == "Gunner" ? 1 : 2; };
        Func<List<ArmorSets.Piece>, string> sortKey = s => S(s.OrderBy(p => Convert.ToInt32(S(p.A, "index"))).First().A, "index").PadLeft(4, '0');
        // 직업별로 보이는 세트: 그 직업 전용 + 공용
        var rarsBy = new List<int>[2];
        for (int c = 0; c < 2; c++) rarsBy[c] = sets.Where(s => clsOrd(s) == c || clsOrd(s) == 2).Select(setRar).Distinct().OrderBy(x => x).ToList();
        var cnt = new Dictionary<string, int>();
        for (int c = 0; c < 2; c++) foreach (var r in rarsBy[c]) {
            string file = "armor-" + ClsFile[c] + "-r" + r + ".html";
            var rs = sets.Where(s => setRar(s) == r && (clsOrd(s) == c || clsOrd(s) == 2)).ToList();
            var own = rs.Where(s => s.Count > 1 && clsOrd(s) == c).OrderBy(sortKey).ToList();
            var shared = rs.Where(s => s.Count > 1 && clsOrd(s) == 2).OrderBy(sortKey).ToList();
            var singles = rs.Where(s => s.Count == 1).OrderBy(s => s[0].Part).ToList();
            cnt[c + "-" + r] = own.Count + shared.Count;
            var sb = new StringBuilder(Head("armor.html", "방어구 · " + ClsName[c] + " · 레어 " + r, "같은 소재로 만드는 세트별로 묶었습니다. 각 세트 아래에 <b>모두 착용했을 때</b>의 방어력・스킬 포인트 합계와 발동 스킬을 적었습니다 (장식주・무기 슬롯 제외). 방어력・슬롯은 「처음 → 최대 강화」, 내성은 화/수/뇌/빙/용 순.", true));
            sb.Append(ArmorTabs(c, "r" + r, rarsBy, dslots));
            sb.Append("<div class=\"toc\">");
            int k = 0;
            if (own.Count > 0) { sb.Append("<span class=\"tocsep\">" + ClsName[c] + " 전용</span>"); foreach (var s in own) sb.Append("<a href=\"#set-" + (++k) + "\">" + Ico(RC(r.ToString()), "torso") + H(SetName(s)) + "</a>"); }
            if (shared.Count > 0) { sb.Append("<span class=\"tocsep\">검사・거너 공용</span>"); foreach (var s in shared) sb.Append("<a href=\"#set-" + (++k) + "\">" + Ico(RC(r.ToString()), "torso") + H(SetName(s)) + "</a>"); }
            if (singles.Count > 0) sb.Append("<span class=\"tocsep\"></span><a href=\"#single\">단품 " + singles.Count + "개</a>");
            sb.Append("</div>\n");
            int sn = 0;
            foreach (var s in own.Concat(shared)) {
                sn++; string sid = "set-" + sn; string sname = SetName(s);
                var pc = s.FirstOrDefault(q => S(q.A, "hunterClass") != ""); string cls = pc == null ? "" : S(pc.A, "hunterClassJapanese");
                var ps = s.FirstOrDefault(q => S(q.A, "sex") != ""); string sex = ps == null ? "" : S(ps.A, "sexJapanese");
                sb.Append("<section class=\"grp card set\" id=\"" + sid + "\"><h2>" + Ico(RC(r.ToString()), "torso", " big") + H(sname) + " <span class=\"meta\">" + (cls != "" ? T(cls) : "검사・거너 공용") + (sex != "" ? "・" + T(sex) : "") + " · " + s.Count + "부위</span></h2>\n");
                sb.Append(Cols("ft armor", 860, new[] { 17, 7, 4, 9, 9, 13, 16, 18, 7 }, "이름", "구분", "레어", "방어력", "슬롯", "내성", "스킬", "생산 소재", "가격"));
                foreach (var q in s) {
                    string id = "a" + q.Part + "-" + S(q.A, "index");
                    sb.Append(ArmorRow(q.A, q.Part, id));
                    if (clsOrd(s) == c || c == 0) index.Append("<tr><td><a href=\"" + file + "#" + id + "\">" + Ico(RC(S(q.A, "rarity")), ArmorIcon[q.Part]) + TJ(S(q.A, "japanese")) + "</a></td><td><a href=\"" + file + "#" + sid + "\">" + H(sname) + "</a> <span class=\"meta\">" + (clsOrd(s) == 2 ? "공용" : ClsName[c]) + "</span></td><td>" + ArmorCat[q.Part] + "</td><td>" + Rar(S(q.A, "rarity")) + "</td><td>" + string.Join(" ", A(q.A, "skills").Select(x => T(S(D(x), "j")) + Sign(S(D(x), "q")))) + "</td></tr>\n");
                }
                sb.Append(TEnd + UpsBox(s.Select(q => q.A).ToList(), s.Select(q => q.Part).ToList()) + SetSummary(s, skillTree) + "</section>\n");
            }
            if (singles.Count > 0) {
                sb.Append("<section class=\"grp\" id=\"single\"><h2>단품 <span class=\"meta\">세트가 없는 장비</span></h2>\n");
                sb.Append(Cols("ft armor", 860, new[] { 17, 7, 4, 9, 9, 13, 16, 18, 7 }, "이름", "구분", "레어", "방어력", "슬롯", "내성", "스킬", "생산 소재", "가격"));
                foreach (var s in singles) {
                    var q = s[0]; string id = "a" + q.Part + "-" + S(q.A, "index");
                    sb.Append(ArmorRow(q.A, q.Part, id));
                    if (clsOrd(s) == c || c == 0) index.Append("<tr><td><a href=\"" + file + "#" + id + "\">" + Ico(RC(S(q.A, "rarity")), ArmorIcon[q.Part]) + TJ(S(q.A, "japanese")) + "</a></td><td>단품 <span class=\"meta\">" + (clsOrd(s) == 2 ? "공용" : ClsName[c]) + "</span></td><td>" + ArmorCat[q.Part] + "</td><td>" + Rar(S(q.A, "rarity")) + "</td><td>" + string.Join(" ", A(q.A, "skills").Select(x => T(S(D(x), "j")) + Sign(S(D(x), "q")))) + "</td></tr>\n");
                }
                sb.Append(TEnd + UpsBox(singles.Select(s => s[0].A).ToList(), singles.Select(s => s[0].Part).ToList()) + "</section>\n");
            }
            Write(file, sb.Append(Foot()).ToString());
        }
        // 장식주: 필요 슬롯별
        foreach (var n in dslots) {
            string file = "armor-deco-s" + n + ".html";
            var sb = new StringBuilder(Head("armor.html", "방어구 · 장식주 · 슬롯 " + n + "개", "방어구・무기의 빈 슬롯에 끼우는 장식주. 필요 슬롯 수별.", true));
            sb.Append(ArmorTabs(2, "s" + n, rarsBy, dslots));
            sb.Append("<section class=\"grp\"><h2>" + Ico("white", "gem", " big") + "장식주 <span class=\"meta\">" + Slots(n) + " 슬롯 " + n + "개</span></h2>\n");
            sb.Append(Table("ft", "이름", "레어", "필요 슬롯", "스킬", "소재", "가격"));
            foreach (var a in decos.Where(a => S(a, "slots") == n.ToString())) {
                string id = "d" + S(a, "index");
                string skills = string.Join("<br>", A(a, "skills").Select(x => T(S(D(x), "j")) + " <b>" + Sign(S(D(x), "q")) + "</b>"));
                string col = S(a, "color") != "" ? S(a, "color") : "white";
                sb.Append("<tr id=\"" + id + "\">" + string.Join("", new[] { Ico(col, "gem") + "<b>" + TJ(S(a, "japanese")) + "</b>", Rar(S(a, "rarity")), Slots(S(a, "slots")), skills, Mats(A(a, "forge")), Money(S(a, "price")) }.Select(x => "<td>" + x + "</td>")) + "</tr>\n");
                index.Append("<tr><td><a href=\"" + file + "#" + id + "\">" + Ico(col, "gem") + TJ(S(a, "japanese")) + "</a></td><td>장식주</td><td>장식주</td><td>" + Rar(S(a, "rarity")) + "</td><td>" + string.Join(" ", A(a, "skills").Select(x => T(S(D(x), "j")) + Sign(S(D(x), "q")))) + "</td></tr>\n");
            }
            sb.Append(TEnd + "</section>\n");
            Write(file, sb.Append(Foot()).ToString());
        }
        // 목차 페이지
        var hub = new StringBuilder(Head("armor.html", "방어구", "먼저 <b>검사 / 거너</b>를 고르고, 다음에 <b>레어도</b>를 고르세요. 같은 소재 세트로 묶어 보여 주고, 세트마다 모두 착용했을 때 발동하는 스킬과 효과를 적었습니다. 검색창에 이름・세트・스킬을 넣으면 전체에서 찾아 이동합니다.", true));
        hub.Append("<div class=\"partgrid cls\">");
        for (int c = 0; c < 2; c++) {
            hub.Append("<div class=\"card part\"><h2>" + Ico("white", ClsIcon[c], " big") + ClsName[c] + "</h2><div class=\"vtabs\">");
            foreach (var r in rarsBy[c]) hub.Append("<a class=\"tab\" href=\"armor-" + ClsFile[c] + "-r" + r + ".html\">" + Ico(RC(r.ToString()), "torso") + "레어 " + r + " <span class=\"meta\">" + cnt[c + "-" + r] + "세트</span></a>");
            hub.Append("</div></div>");
        }
        hub.Append("<div class=\"card part\"><h2>" + Ico("white", "gem", " big") + "장식주</h2><div class=\"vtabs col\">");
        foreach (var n in dslots) hub.Append("<a class=\"tab\" href=\"armor-deco-s" + n + ".html\">" + Slots(n) + " 슬롯 " + n + "개 <span class=\"meta\">" + decos.Count(a => S(a, "slots") == n.ToString()) + "</span></a>");
        hub.Append("</div></div>");
        hub.Append("</div>\n<section class=\"grp idx\"><h2>검색 결과</h2>" + Table("ft", "이름", "세트", "부위", "레어", "스킬") + index + TEnd + "</section>\n");
        Write("armor.html", hub.Append(Foot()).ToString());
    }    static string Slots(object n) { int k; return int.TryParse(Convert.ToString(n), out k) && k > 0 ? new string('◯', k) : "-"; }
    static string Sign(string q) { return q.StartsWith("-") || q.StartsWith("+") ? H(q) : "+" + H(q); }

    // ================= 스킬 =================
    static readonly string[] SkillCatName = { "공격계", "방어계", "유틸계" };
    static readonly HashSet<string> SkAtk = new HashSet<string> { "artillery", "attack", "bomb-boost", "clust-plus", "crag-plus", "expert", "handicraft", "load", "normal-plus", "normal-up", "pellet-plus", "pellet-up", "pierce-plus", "pierce-up", "potential", "precision", "rapid-fire", "recoil", "reload", "sharpness", "status", "speed-sharpen", "throw" };
    static readonly HashSet<string> SkDef = new HashSet<string> { "antiseptic", "auto-guard", "cold-res", "defense", "dragon-res", "earplugs", "element-res", "evasion", "fire-res", "guard", "health", "heat-res", "ice-res", "paralysis", "poison", "protection", "recover-speed", "recovery-items", "sleep", "snow-res", "stun", "terrain", "thunder-res", "tremor-res", "vocal-chords", "water-res", "wind-pressure" };
    // 「아래 몬스터 공격이 …:」 처럼 뒤에 목록이 와야 하는 마지막 문장은 목록이 없으면 뺀다
    static string DescClean(string d, bool hasList)
    {
        d = d.Trim(); if (hasList || !d.EndsWith(":")) return d;
        int i = d.LastIndexOf(". "); return i > 0 ? d.Substring(0, i + 1) : d.TrimEnd(':');
    }
    static int SkCat(string id) { return SkAtk.Contains(id) ? 0 : SkDef.Contains(id) ? 1 : 2; }

    public static void Skills()
    {
        var data = A(Load("armor_skills"));
        // 스킬 계통별 장식주(+포인트 주는 것)와 대표 색
        var decoBy = new Dictionary<string, List<Dictionary<string, object>>>();
        foreach (var c in A(Load("armor"))) foreach (var o in A(D(c), "decorations")) {
            var d = D(o); if (Has(d, "dev")) continue;
            foreach (var sk in A(d, "skills")) { string j = S(D(sk), "j"); int q; int.TryParse(S(D(sk), "q").Replace("+", ""), out q); if (q <= 0) continue; if (!decoBy.ContainsKey(j)) decoBy[j] = new List<Dictionary<string, object>>(); decoBy[j].Add(d); }
        }
        Func<string, string> colOf = j => { List<Dictionary<string, object>> l; return decoBy.TryGetValue(j, out l) && S(l[0], "color") != "" ? S(l[0], "color") : "white"; };

        var sb = new StringBuilder(Head("armor-skills.html", "스킬", "방어구・장식주의 스킬 포인트 합계가 +10 이상(또는 -10 이하)이면 스킬이 발동합니다. <b>스킬을 누르면</b> 단계별 효과와 그 스킬을 올리는 장식주가 나옵니다. 아이콘은 해당 장식주 색.", false));
        sb.Append("<div class=\"tools wtools\"><input id=\"wq\" type=\"search\" placeholder=\"스킬 찾기 (예: 귀마개, 공격)\" autocomplete=\"off\"><span id=\"wqn\"></span></div>");
        sb.Append("<div class=\"vtabs wtabs\">");
        for (int i = 0; i < 3; i++) sb.Append("<button type=\"button\" data-c=\"" + i + "\"" + (i == 0 ? " class=\"on\"" : "") + ">" + Ico(i == 0 ? "red" : i == 1 ? "blue" : "green", "gem") + SkillCatName[i] + " <span class=\"cnt\">" + data.Count(o => SkCat(S(D(o), "id")) == i) + "</span></button>");
        sb.Append("</div>\n");
        var pop = new List<string>(); int id = 0;
        for (int ci = 0; ci < 3; ci++) {
            sb.Append("<section class=\"wcls" + (ci == 0 ? "" : " off") + "\" data-c=\"" + ci + "\"><h2>" + Ico(ci == 0 ? "red" : ci == 1 ? "blue" : "green", "gem", " big") + SkillCatName[ci] + "</h2><div class=\"skgrid\">");
            foreach (var o in data.Where(o => SkCat(S(D(o), "id")) == ci).OrderBy(o => Tk(S(D(o), "japanese")))) {
                var s = D(o); string ja = S(s, "japanese"); var stages = A(s, "stages").Select(D).ToList();
                var pos = stages.Where(x => !S(x, "points").StartsWith("-")).ToList();
                string range = pos.Count == 0 ? "" : pos.Count == 1 ? T(S(pos[0], "j")) : T(S(pos[0], "j")) + " ~ " + T(S(pos[pos.Count - 1], "j"));
                sb.Append("<button type=\"button\" class=\"wn sk\" data-w=\"" + id + "\">" + Ico(colOf(ja), "gem") + "<span class=\"sk-t\"><span class=\"nm\">" + T(ja) + "</span><span class=\"sk-r\">" + range + "</span></span></button>");
                // 효과 창
                var d = new StringBuilder("<div class=\"wd-head\">" + Ico(colOf(ja), "gem", " big") + "<div><b class=\"wd-name\">" + T(ja) + "</b><div class=\"wd-ja\">" + H(ja) + "</div><div class=\"meta\">" + SkillCatName[ci] + " 스킬</div></div></div>");
                var tbx = D(s.ContainsKey("table") ? s["table"] : null); d.Append("<p class=\"sk-desc\">" + DescClean(P(S(s, "desc")), tbx != null && A(tbx, "body").Length > 0) + "</p>");
                d.Append("<table class=\"wd-stats sk-st\"><tr><th>포인트</th><th>발동 스킬</th><th>효과</th></tr>");
                foreach (var t in stages) { string pt = S(t, "points"); d.Append("<tr><td><b class=\"" + (pt.StartsWith("-") ? "neg" : "pos") + "\">" + H(pt == "" ? "-" : pt) + "</b></td><td>" + T(S(t, "j")) + "</td><td>" + P(Regex.Replace(S(t, "d"), "</span>$", "")) + "</td></tr>"); }
                d.Append("</table>");
                var tb = D(s.ContainsKey("table") ? s["table"] : null);
                if (tb != null && A(tb, "body").Length > 0) {
                    d.Append("<h4 class=\"wd-h\">자세히</h4><table class=\"wd-stats sk-st\"><tr>");
                    foreach (var h in A(tb, "head")) { var hd = D(h); d.Append("<th" + PropStr(hd) + ">" + P(S(hd, "content")) + "</th>"); }
                    d.Append("</tr>");
                    foreach (var r in A(tb, "body")) { d.Append("<tr>"); foreach (var c in A(D(r), "cells")) { var cd = D(c); d.Append("<td" + PropStr(cd) + ">" + P(S(cd, "content")) + "</td>"); } d.Append("</tr>"); }
                    d.Append("</table>");
                }
                List<Dictionary<string, object>> decos;
                if (decoBy.TryGetValue(ja, out decos)) {
                    d.Append("<h4 class=\"wd-h\">이 스킬을 올리는 장식주</h4><div class=\"wd-mats\">");
                    foreach (var de in decos) {
                        string pts = string.Join(" ", A(de, "skills").Select(x => T(S(D(x), "j")) + " <b>" + Sign(S(D(x), "q")) + "</b>"));
                        d.Append("<div class=\"sk-deco\">" + Ico(S(de, "color") != "" ? S(de, "color") : "white", "gem") + "<b>" + T(S(de, "japanese")) + "</b> <span class=\"meta\">" + Slots(S(de, "slots")) + "</span> " + pts + "</div>");
                    }
                    d.Append("</div>");
                } else d.Append("<p class=\"meta\">이 스킬을 올리는 장식주는 없습니다 (방어구로만 발동).</p>");
                pop.Add(id + ":" + Js(d.ToString()));
                id++;
            }
            sb.Append("</div></section>\n");
        }
        sb.Append("<div id=\"wpop\" hidden><div class=\"wp-box\" role=\"dialog\" aria-modal=\"true\"><button type=\"button\" class=\"wp-x\" aria-label=\"닫기\">✕</button><div class=\"wp-body\"></div></div></div>\n");
        sb.Append("<script>window.WD={" + string.Join(",", pop) + "};</script>\n");
        Write("armor-skills.html", sb.Append(Foot()).ToString());
    }    static string PropStr(Dictionary<string, object> d)
    {
        object v; if (d == null || !d.TryGetValue("props", out v) || v == null) return "";
        var list = v is string ? new[] { (string)v } : A(v).Select(x => Convert.ToString(x)).ToArray();
        var sb = new StringBuilder();
        foreach (var p in list) { var m = Regex.Match(p, "^(rowspan|colspan)=(\\d+)$"); if (m.Success) sb.Append(" " + m.Groups[1].Value + "=\"" + m.Groups[2].Value + "\""); }
        return sb.ToString();
    }

    // ================= 아이템 =================
    // 아이템 분류: 0 회복용 / 1 전투용 / 2 소재 / 3 기타
    static readonly string[] ItemCatName = { "회복용", "전투용", "소재", "기타・식재료" };
    static readonly string[] ItemCatIcon = { "green-icon jar-icon", "red-icon barrel-bomb-icon", "white-icon claw-icon", "white-icon book-icon" };
    public static HashSet<string> MealIngredients = new HashSet<string>();
    static int ItemCat(Dictionary<string, object> it)
    {
        string kind = Regex.Match(S(it, "icon"), "\\s([\\w-]+-icon)").Groups[1].Value.Replace("-icon", ""), ja = S(it, "japanese");
        if (MealIngredients.Contains(ja)) return 3;                          // 식사용 식재료
        if (ja == "力の爪" || ja == "守りの爪") return 0;                      // 가지고 있으면 효과(보조)
        if (Regex.IsMatch(ja, "体液|濃汁|麻酔薬|エキス|塵粉")) return 2;      // 조합・장비 소재
        switch (kind) {
            case "jar": return 0;
            case "meat": return !ja.Contains("解毒") && Regex.IsMatch(ja, "毒|麻痺|マヒ|シビレ|眠") ? 1 : 0;
            case "fish": return Regex.IsMatch(ja, "焼|コゲ|こんがり") ? 0 : 2;
            case "powder": return ja.Contains("袋") || ja == "爆薬" ? 2 : ja.Contains("ポーチ") ? 3 : 0;
            case "seed": return ja == "怪力の種" || ja == "忍耐の種" ? 0 : 2;
            case "flute": return ja.Contains("角笛") ? 1 : 0;
            case "bomb": return Regex.IsMatch(ja, "紅玉|宝玉|竜玉|素材玉") ? 2 : 1;
            case "dung": return ja.Contains("こやし玉") ? 1 : 2;
            case "barrel-bomb": case "knife": case "trap": case "shot": case "coating": case "smoke": case "boomerang":
            case "whetstone": case "pick": case "bugnet": case "ocarina": return 1;
            case "book": case "house": case "map": case "compass": case "tools": case "maka-pot": case "maka-pot-prepared":
            case "sheet-music": case "monster-eating": case "spit": case "binoculars": case "bait": return 3;
        }
        return 2; // 몬스터 소재・광석・뼈・곤충・코인・티켓 등
    }

    public static void Items()
    {
        var data = A(Load("items"));
        MealIngredients.Clear();
        foreach (var s in A(Load("meals"))) foreach (var m in A(D(s), "meals")) foreach (var g in A(D(m), "ingredients")) MealIngredients.Add(S(D(g), "japanese"));
        var sb = new StringBuilder(Head("items.html", "아이템", "용도별 탭으로 나눴습니다. 게임 속 설명은 한글 패치 문장 그대로이고, 「효과・입수」는 공략 위키의 실측・해석 정보입니다. 검색하면 모든 탭에서 찾습니다.", true));
        var rows = new List<string>[4]; for (int i = 0; i < 4; i++) rows[i] = new List<string>();
        int idx = 0;
        foreach (var o in data) {
            var it = D(o); idx++;
            if (Has(it, "hide") || S(it, "japanese") == "（ダミー）" || S(it, "japanese") == "ソフト仮データ") continue;
            string col = Regex.Match(S(it, "icon"), "^(\\w+)-icon").Groups[1].Value;
            string ico = Icon(S(it, "icon")); string name = (ico != "" ? ico : "<i class=\"dot " + H(col) + "\"></i>") + "<b>" + TJ(S(it, "japanese")) + "</b>";
            string desc = S(it, "descriptionJapanese") != "" ? T(S(it, "descriptionJapanese")) : "";
            var fx = new List<string>();
            if (Has(it, "details")) fx.Add(P(S(it, "details")));
            if (Has(it, "acquireSpecial")) fx.Add("<span class=\"tk\">입수:</span> " + P(S(it, "acquireSpecial")));
            if (Has(it, "supply")) fx.Add("<span class=\"tag\">지급품 전용</span>");
            if (Has(it, "account")) fx.Add("<span class=\"tag\">정산 아이템</span>");
            if (Has(it, "unavailable")) fx.Add("<span class=\"tag\">입수 불가</span>");
            if (A(it, "rewards").Length > 0) fx.Add("<span class=\"tk\">내용물:</span> " + string.Join(" ", A(it, "rewards").Select(x => "<span class=\"mat\">" + T(S(D(x), "japanese")) + " " + H(S(D(x), "percent")) + "%</span>")));
            string buy = S(it, "buy"), sell = S(it, "sell");
            rows[ItemCat(it)].Add("<tr id=\"item-" + idx + "\">" + string.Join("", new[] { name, Rar(S(it, "rarity")), Num(S(it, "maxStack")), buy == "0" || buy == "" ? "-" : Money(buy), sell == "" ? "-" : Money(sell), desc, string.Join("<br>", fx) }.Select(c => "<td>" + c + "</td>")) + "</tr>\n");
        }
        sb.Append("<div class=\"vtabs ctabs\">");
        for (int i = 0; i < 4; i++) sb.Append("<button type=\"button\" data-c=\"" + i + "\"" + (i == 0 ? " class=\"on\"" : "") + ">" + Icon(ItemCatIcon[i]) + ItemCatName[i] + " <span class=\"cnt\">" + rows[i].Count + "</span></button>");
        sb.Append("</div>\n");
        for (int i = 0; i < 4; i++) {
            sb.Append("<section class=\"grp ccat" + (i == 0 ? "" : " off") + "\" data-c=\"" + i + "\"><h2>" + Icon(ItemCatIcon[i]).Replace("class=\"ico ", "class=\"ico big ") + ItemCatName[i] + "</h2>\n");
            sb.Append(Table("ft items", "이름", "레어", "최대", "매입", "매각", "게임 설명", "효과・입수"));
            foreach (var r in rows[i]) sb.Append(r);
            sb.Append(TEnd + "</section>\n");
        }
        Write("items.html", sb.Append(Foot()).ToString());
    }
    // ================= 몬스터 =================
    static readonly string[] HzHead = { "베기", "타격", "탄", "화", "수", "뇌", "빙", "용", "기절", "경직" };
    public static void Bestiary()
    {
        var data = A(Load("monsters"));
        var spec = new Dictionary<string, string[]> {
            {"조룡종", new[]{"velociprey","white-velociprey","velocidrome","genprey","gendrome","ioprey","iodrome","yian-kut-ku","blue-yian-kut-ku","yian-garuga","gypceros","purple-gypceros"}},
            {"비룡종", new[]{"rathian","pink-rathian","gold-rathian","rathalos","azure-rathalos","silver-rathalos","khezu","red-khezu","basarios","gravios","black-gravios","monoblos","white-monoblos","diablos","black-diablos","remobra"}},
            {"어룡종", new[]{"cephalos","cephadrome","plesioth","green-plesioth"}},
            {"갑각종", new[]{"hermitaur","daimyo-hermitaur","ceanataur","shogun-ceanataur","shen-gaoren"}},
            {"아수종", new[]{"bullfango","bulldrome","conga","congalala","blango","blangonga","rajang"}},
            {"고룡종", new[]{"kirin","kushala-daora","kushala-daora-rusted","chameleos","lunastra","teostra","lao-shan-lung","ashen-lao-shan-lung","yama-tsukami","fatalis","crimson-fatalis","white-fatalis"}},
            {"갑충종", new[]{"vespoid","hornetaur","great-thunderbug"}},
            {"초식종", new[]{"kelbi","mosswine","anteka","popo","aptonoth","apceros"}},
            {"수인종", new[]{"felyne","melynx","shakalaka","king-shakalaka"}} };
        var specOf = new Dictionary<string, int>(); int si0 = 0; foreach (var kv in spec) { foreach (var id in kv.Value) specOf[id] = si0; si0++; }
        var specNames = spec.Keys.ToList();
        var tiles = new StringBuilder[specNames.Count + 1]; for (int i = 0; i < tiles.Length; i++) tiles[i] = new StringBuilder();
        var pops = new List<string>(); int mid = 0;
        Func<Dictionary<string, object>, string> imgOf = mm => { string b = Path.GetFileName(S(mm, "image")); return File.Exists(Path.Combine(OutDir, "images", "monsters", b + ".jpg")) ? "images/monsters/" + b + ".jpg" : ""; };
        var outer = new StringBuilder(Head("bestiary.html", "몬스터", "종족별 탭에서 몬스터를 고르고 <b>그림을 누르면</b> 생태・약점・육질・상태이상・갈무리・보수・공격이 나옵니다. 생태 설명은 게임 속 「몬스터 생태」 한글 패치 문장(다른 시리즈 출처는 새로 번역). 육질은 숫자가 클수록 잘 들어갑니다.", false));
        foreach (var o in data) {
            var m = D(o); var sb = new StringBuilder();
            string img = imgOf(m); int sp; if (!specOf.TryGetValue(S(m, "id"), out sp)) sp = specNames.Count;
            tiles[sp].Append("<button type=\"button\" class=\"wn mon\" data-w=\"" + H(S(m, "id")) + "\">" + (img != "" ? "<img src=\"" + img + "\" alt=\"\" loading=\"lazy\">" : "<span class=\"noimg\"></span>") + "<span class=\"nm\">" + T(S(m, "japanese")) + "</span><span class=\"mt\">" + (S(m, "category") == "0" ? "소형" : "대형") + "</span></button>");
            sb.Append("<div class=\"mon-head\">" + (img != "" ? "<img src=\"" + img + "\" alt=\"\">" : "") + "<div><b class=\"wd-name\">" + T(S(m, "japanese")) + "</b><div class=\"wd-ja\">" + H(S(m, "japanese")) + "</div><div class=\"meta\">" + (sp < specNames.Count ? specNames[sp] + " · " : "") + (S(m, "category") == "0" ? "소형" : "대형") + " · 기본 체력 " + H(S(m, "baseHP")) + "</div></div></div>\n");
            string desc = S(m, "descriptionJapanese");
            if (desc != "") { var src = Regex.Match(desc, "<sub>\\[(.*?)\\]</sub>"); sb.Append("<p class=\"eco\">" + T(desc) + (src.Success && !src.Groups[1].Value.Contains("モンスターハンター２") && !src.Groups[1].Value.Contains("引用") ? " <span class=\"src\">(출처: " + H(src.Groups[1].Value) + ")</span>" : "") + "</p>\n"); }
            if (A(m, "notes").Length > 0) sb.Append("<ul class=\"notes\">" + string.Join("", A(m, "notes").Select(x => "<li>" + P(Convert.ToString(x)) + "</li>")) + "</ul>\n");
            if (Has(m, "detailedNotes")) sb.Append("<p class=\"note\">" + P(S(m, "detailedNotes")) + "</p>");
            // 약점 별
            var stars = D(m.ContainsKey("stars") ? m["stars"] : null);
            if (stars != null) {
                sb.Append("<div class=\"stars\">");
                StarRow(sb, A(stars, "weapons"), new[] { "베기", "타격", "탄" });
                StarRow(sb, A(stars, "elements"), new[] { "화", "수", "뇌", "빙", "용" });
                StarRow(sb, A(stars, "statuses"), new[] { "독", "마비", "수면", "기절" });
                sb.Append("</div>\n");
            }
            var rage = D(m.ContainsKey("rage") ? m["rage"] : null);
            var sizes = A(m, "sizes");
            if (rage != null || sizes.Length > 0) {
                sb.Append("<p class=\"kv\">");
                if (rage != null) sb.Append("분노 시: 공격 ×" + H(S(rage, "attack")) + " · 방어 ×" + H(S(rage, "defense")) + " · 속도 ×" + H(S(rage, "speed")) + "　");
                if (sizes.Length >= 4) sb.Append("크기: 금관 ≥" + H(Convert.ToString(sizes[0])) + " · 은관 ≥" + H(Convert.ToString(sizes[1])) + " · 미니 ≤" + H(Convert.ToString(sizes[3])) + " cm");
                sb.Append("</p>\n");
            }
            // 육질
            if (A(m, "hitzones").Length > 0) {
                sb.Append("<h3>육질</h3>"); Hitzones(sb, A(m, "hitzones"));
                if (Has(m, "hitzoneNotes")) sb.Append("<p class=\"note\">" + P(S(m, "hitzoneNotes")) + "</p>");
                foreach (var alt in A(m, "hitzonesAlt")) { var ad = D(alt); sb.Append("<h4>" + P(S(ad, "name")) + "</h4>"); Hitzones(sb, A(ad, "hitzones")); }
            }
            // 상태이상 내성
            if (A(m, "statusTolerances").Length > 0) {
                sb.Append("<h3>상태이상</h3>" + Table("st", "상태", "내성 (1회째 → 이후 단계)", "지속 시간(초)", "축적 감소 (양/초)", "피해"));
                foreach (var x in A(m, "statusTolerances")) {
                    var t = D(x);
                    if (Has(t, "immune")) { sb.Append(Tr(P(S(t, "title")), "무효", "-", "-", "-")); continue; }
                    string dmg = S(t, "damage") != "" ? H(S(t, "damage")) + (S(t, "totalDamage") != "" ? " (총 " + H(S(t, "totalDamage")) + ")" : "") : "-";
                    sb.Append(Tr(P(S(t, "title")), H(S(t, "tolerances").Replace(",", " → ")), Num(S(t, "duration")), Num(S(t, "recovery")), dmg));
                }
                sb.Append(TEnd);
            }
            Rewards(sb, "갈무리", A(m, "carves"), true);
            Rewards(sb, "포획 보수", A(m, "captureRewards"), false);
            Rewards(sb, "부위 파괴 보수", A(m, "breakRewards"), true);
            if (A(m, "attacks").Length > 0) {
                sb.Append("<h3>공격</h3>" + Table("st", "이름", "공격력", "충격", "기절치", "효과"));
                foreach (var x in A(m, "attacks")) {
                    var t = D(x); object av; t.TryGetValue("attack", out av);
                    string atk = av is object[] ? string.Join(" / ", A(av).Select(z => Convert.ToString(z))) : Convert.ToString(av);
                    var fx = new List<string>();
                    if (S(t, "attribute") != "") fx.Add(AttrChip(S(t, "attribute"), ""));
                    if (S(t, "attribute2") != "") fx.Add(AttrChip(S(t, "attribute2"), ""));
                    if (S(t, "effectJapanese") != "") fx.Add(T(S(t, "effectJapanese")));
                    if (Has(t, "guardUp")) fx.Add("<span class=\"tag\">가드성능+2로 가드</span>");
                    if (S(t, "note") != "") fx.Add("<span class=\"note\">" + P(S(t, "note")) + "</span>");
                    sb.Append(Tr(TJ(S(t, "japanese")), Num(atk), Num(S(t, "impact")), Num(S(t, "knockout")), string.Join(" ", fx)));
                }
                sb.Append(TEnd);
            }
            pops.Add(Js(S(m, "id")) + ":" + Js(sb.ToString())); mid++;
        }
        outer.Append("<div class=\"tools wtools\"><input id=\"wq\" type=\"search\" placeholder=\"몬스터 이름 찾기\" autocomplete=\"off\"><span id=\"wqn\"></span></div><div class=\"vtabs wtabs\">");
        for (int i = 0; i < specNames.Count; i++) outer.Append("<button type=\"button\" data-c=\"" + i + "\"" + (i == 0 ? " class=\"on\"" : "") + ">" + specNames[i] + " <span class=\"cnt\">" + spec[specNames[i]].Length + "</span></button>");
        outer.Append("</div>\n");
        for (int i = 0; i < specNames.Count; i++) outer.Append("<section class=\"wcls" + (i == 0 ? "" : " off") + "\" data-c=\"" + i + "\"><h2>" + specNames[i] + "</h2><div class=\"mongrid\">" + tiles[i] + "</div></section>\n");
        if (tiles[specNames.Count].Length > 0) outer.Append("<section class=\"wcls off\" data-c=\"" + specNames.Count + "\"><div class=\"mongrid\">" + tiles[specNames.Count] + "</div></section>");
        outer.Append("<div id=\"wpop\" class=\"wide\" hidden><div class=\"wp-box\" role=\"dialog\" aria-modal=\"true\"><button type=\"button\" class=\"wp-x\" aria-label=\"닫기\">✕</button><div class=\"wp-body\"></div></div></div>\n");
        outer.Append("<script>window.WD={" + string.Join(",", pops) + "};</script>\n");
        Write("bestiary.html", outer.Append(Foot()).ToString());
    }
    static void StarRow(StringBuilder sb, object[] v, string[] names)
    {
        for (int i = 0; i < v.Length && i < names.Length; i++) {
            int n = Convert.ToInt32(v[i]); sb.Append("<span class=\"star\">" + names[i] + " <b>" + new string('★', n) + "<i>" + new string('☆', Math.Max(0, 3 - n)) + "</i></b></span>");
        }
        sb.Append("<br>");
    }
    static void Hitzones(StringBuilder sb, object[] hz)
    {
        var heads = new List<string> { "부위" }; heads.AddRange(HzHead); heads.Add("파괴");
        sb.Append(Table("st hz", heads.ToArray()));
        foreach (var x in hz) {
            var h = D(x); var z = A(h, "zones").Select(v => Convert.ToString(v)).ToList();
            var cells = new List<string> { TJ(S(h, "partJapanese")) };
            for (int i = 0; i < 10; i++) { string v = i < z.Count ? z[i] : ""; int n; int.TryParse(v, out n); cells.Add(i < 8 && n >= 45 ? "<b class=\"hi\">" + H(v) + "</b>" : H(v)); }
            cells.Add(P(S(h, "break")));
            sb.Append(Tr(cells.ToArray()));
        }
        sb.Append(TEnd);
    }
    static void Rewards(StringBuilder sb, string title, object[] ranks, bool sources)
    {
        if (ranks.Length == 0) return;
        sb.Append("<h3>" + title + "</h3><div class=\"cols\">");
        foreach (var r in ranks) {
            var rd = D(r);
            sb.Append("<div class=\"col\"><h4>" + T(S(rd, "rankJapanese")) + "</h4>");
            if (sources) foreach (var s in A(rd, "sources")) {
                var sd = D(s);
                string head = T(S(sd, "japanese")); if (S(sd, "carveCount") != "") head += " <span class=\"meta\">×" + H(S(sd, "carveCount")) + "회</span>";
                if (S(sd, "require") != "") head += " <span class=\"meta\">(" + P(S(sd, "require")) + ")</span>";
                sb.Append(RewardTable(head, A(sd, "materials")));
            }
            else sb.Append(RewardTable(null, A(rd, "materials")));
            sb.Append("</div>");
        }
        sb.Append("</div>\n");
    }
    static string RewardTable(string head, object[] mats)
    {
        var sb = new StringBuilder("<table class=\"rw\">");
        if (head != null) sb.Append("<caption>" + head + "</caption>");
        foreach (var x in mats) { var d = D(x); string j = S(d, "j") != "" ? S(d, "j") : S(d, "japanese"); string q = S(d, "quantity"); sb.Append("<tr><td>" + ILink(j, TJ(j, q != "" && q != "1" ? " ×" + H(q) : "")) + "</td><td class=\"num\">" + H(S(d, "percent")) + "%</td></tr>"); }
        return sb.Append("</table>").ToString();
    }

    // ================= 퀘스트 =================
    public static void Quests()
    {
        var data = A(Load("quests"));
        var sb = new StringBuilder(Head("quests.html", "퀘스트", "먼저 <b>마을</b>을 고르고, 다음에 <b>분류</b>(프리 퀘스트・하위・상위 등)를 고르세요. 퀘스트 제목・목표는 한글 패치 표기. 보수금은 「메인 / 서브A / 서브B」. 줄을 누르면 지급품・보수가 펼쳐지고, 몬스터・아이템 이름을 누르면 해당 페이지로 이동합니다. 검색하면 모든 마을・분류에서 찾습니다.", true));
        var locs = A(data).Select(D).ToList();
        sb.Append("<div class=\"gtabs vill\" data-g=\"vill\">");
        foreach (var ld in locs) { string loc = S(ld, "location"); sb.Append("<button type=\"button\" data-t=\"" + loc + "\"" + (ld == locs[0] ? " class=\"on\"" : "") + "><img src=\"images/regions/" + loc + ".svg\" alt=\"\"><span>" + LocKo(loc) + "</span><span class=\"cnt\">" + A(ld, "categories").Sum(c => A(D(c), "questLocales").Sum(x => A(D(x), "quests").Length)) + "</span></button>"); }
        sb.Append("</div>\n");
        foreach (var ld in locs) {
            string loc = S(ld, "location");
            sb.Append("<section class=\"gpanel vill" + (ld == locs[0] ? "" : " off") + "\" data-g=\"vill\" data-t=\"" + loc + "\"><h2 class=\"villh\"><img src=\"images/regions/" + loc + ".svg\" alt=\"\">" + LocKo(loc) + "</h2>");
            var cats = A(ld, "categories").Select(D).ToList();
            sb.Append("<div class=\"gtabs\" data-g=\"cat-" + loc + "\">");
            for (int i = 0; i < cats.Count; i++) sb.Append("<button type=\"button\" data-t=\"" + i + "\"" + (i == 0 ? " class=\"on\"" : "") + ">" + T(S(cats[i], "categoryJapanese")) + " <span class=\"cnt\">" + A(cats[i], "questLocales").Sum(x => A(D(x), "quests").Length) + "</span></button>");
            sb.Append("</div>\n");
            for (int i = 0; i < cats.Count; i++) {
                var cd = cats[i]; string id = loc + "-" + Slug(S(cd, "category"));
                sb.Append("<section class=\"grp gpanel" + (i == 0 ? "" : " off") + "\" data-g=\"cat-" + loc + "\" data-t=\"" + i + "\" id=\"" + id + "\"><h2>" + TJ(S(cd, "categoryJapanese")) + "</h2>\n");
                foreach (var ql in A(cd, "questLocales")) {
                    var qd = D(ql);
                    string lh = S(qd, "localeJapanese") != "" ? T(S(qd, "localeJapanese")) : "";
                    if (S(qd, "examRank") != "") lh = "HR" + H(S(qd, "examRank")) + " 승급 시험" + (S(qd, "examRequire") != "" ? " (HR" + H(S(qd, "examRequire")) + " 이상 수주)" : "");
                    if (lh != "") sb.Append("<h3>" + lh + "</h3>\n");
                    foreach (var q in A(qd, "quests")) Quest(sb, D(q));
                }
                sb.Append("</section>\n");
            }
            sb.Append("</section>\n");
        }
        Write("quests.html", sb.Append(Foot()).ToString());
    }
    static string LocKo(string loc) { return loc == "jumbo" ? Tk("ジャンボ村") : loc == "dundorma" ? "돈도르마" : H(loc); }
    static void Quest(StringBuilder sb, Dictionary<string, object> q)
    {
        string stars = S(q, "stars"); int st; int.TryParse(stars, out st);
        var money = A(q, "rewardMoney").Select(x => Money(Convert.ToString(x)));
        sb.Append("<details class=\"q\"><summary><span class=\"qs\">" + (st > 0 ? new string('★', st) : "") + "</span><span class=\"qt\">" + T(S(q, "japaneseTitle")) + " <b>" + T(S(q, "japanese")) + "</b></span>");
        sb.Append("<span class=\"qm\">" + (S(q, "localeJapanese") != "" ? T(S(q, "localeJapanese")) + " · " : "") + T(S(q, "seasonJapanese")) + (S(q, "timeOfDayJapanese") != "" ? " · " + T(S(q, "timeOfDayJapanese")) : "") + " · " + H(S(q, "time")) + "분 · 계약금 " + Money(S(q, "fee")) + " · 보수 " + string.Join(" / ", money) + "</span></summary>\n<div class=\"qb\">");
        sb.Append("<p class=\"goals\">" + string.Join("<br>", A(q, "goals").Select((g, i) => "<span class=\"gl\">" + (i == 0 ? "메인" : i == 1 ? "서브A" : "서브B") + "</span> " + TJ(S(D(g), "j")))) + "</p>");
        var info = new List<string>();
        if (A(q, "largeMonsters").Length > 0) info.Add("대형 몬스터: " + string.Join(", ", A(q, "largeMonsters").Select(x => MLink(S(D(x), "japanese"), T(S(D(x), "japanese"))) + (S(D(x), "quantity") != "" ? " ×" + H(S(D(x), "quantity")) : ""))));
        if (S(q, "require") != "") info.Add("조건: " + P(S(q, "require")));
        if (A(q, "hrp").Length > 0) info.Add("HRP: " + string.Join(" / ", A(q, "hrp").Select(x => H(Convert.ToString(x)))));
        if (S(q, "hex") != "") info.Add("<span class=\"meta\">ID " + H(S(q, "hex")) + "</span>");
        if (info.Count > 0) sb.Append("<p class=\"kv\">" + string.Join(" · ", info) + "</p>");
        if (S(q, "notes") != "") sb.Append("<p class=\"note\">" + P(S(q, "notes")) + "</p>");
        sb.Append("<div class=\"cols\">");
        if (A(q, "supplies").Length > 0) sb.Append("<div class=\"col\">" + SupplyTable("지급품", A(q, "supplies")) + "</div>");
        if (A(q, "suppliesA").Length > 0) sb.Append("<div class=\"col\">" + SupplyTable("서브A 달성 시 추가 지급", A(q, "suppliesA")) + "</div>");
        if (A(q, "suppliesB").Length > 0) sb.Append("<div class=\"col\">" + SupplyTable("서브B 달성 시 추가 지급", A(q, "suppliesB")) + "</div>");
        foreach (var r in A(q, "rewards")) {
            var rd = D(r); string t = S(rd, "rewardNameJapanese") != "" ? T(S(rd, "rewardNameJapanese")) : "메인 보수";
            sb.Append("<div class=\"col\">" + RewardTable(t, A(rd, "materials")) + "</div>");
        }
        sb.Append("</div></div></details>\n");
    }
    static string SupplyTable(string title, object[] list)
    {
        var sb = new StringBuilder("<table class=\"rw\"><caption>" + title + "</caption>");
        foreach (var x in list) { var d = D(x); sb.Append("<tr><td>" + ILink(S(d, "japanese"), T(S(d, "japanese"))) + "</td><td class=\"num\">" + H(S(d, "quantity")) + "</td></tr>"); }
        return sb.Append("</table>").ToString();
    }

    // ================= 맵 =================
    public static readonly string[] SeasonEmoji = { "☀️", "⛄", "🌳" };
    // 채집 포인트 종류 → 소분류 (0 소비 / 1 소재 / 2 기타)
    static readonly string[] KindGroupName = { "소비 아이템", "소재 아이템", "기타" };
    static int KindGroup(string nm)
    {
        if (Regex.IsMatch(nm, "양봉|낚시|버섯|^풀$|씨앗|열매|선인장|덤불|회복약|담쟁이")) return 0;
        if (Regex.IsMatch(nm, "광석|곤충|자갈|조개|뼈|알 둥지|배설물|거미줄|화약|사체|원주")) return 1;
        return 2;
    }
    public static void Maps()
    {
        var data = A(Load("maps"));
        // 등급 키: 0 마을・하위, 1 상위, 2 훈련
        Func<string, int> rk = j => j.Contains("ハード") ? 1 : j.Contains("演習") ? 2 : 0;
        string[] rkName = { "마을・하위", "상위", "훈련" };
        var kinds = new List<string>(); var kindIcon = new Dictionary<string, string>(); var kindCount = new Dictionary<string, int>();
        var js = new StringBuilder(); var sb0 = new StringBuilder(); var regions = new List<string[]>();
        Func<object[], string> items = list => "[" + string.Join(",", list.Select(x => { var d = D(x); return "[" + Js(Tk(S(d, "japanese"))) + "," + (S(d, "percent") == "" ? "0" : S(d, "percent")) + "]"; })) + "]";
        foreach (var l in data) {
            var ms = A(D(l), "maps"); if (ms.Length == 0) continue;
            string rslug = Slug(S(D(l), "name")); string locImg = File.Exists(Path.Combine(OutDir, "images", "regions", rslug + ".svg")) ? "regions/" + rslug + ".svg" : "maps/" + MapImage(S(D(ms[0]), "title")) + ".png"; regions.Add(new[] { "loc-" + rslug, LocName(D(ms[0])), locImg });
            sb0.Append("<section class=\"grp loc" + (regions.Count > 1 ? " off" : "") + "\" id=\"loc-" + Slug(S(D(l), "name")) + "\"><h2>" + "<img class=\"rgimg big\" src=\"images/" + locImg + "\" alt=\"\">" + H(LocName(D(ms[0]))) + "</h2>\n");
            if (ms.Length > 1) {
                sb0.Append("<div class=\"vtabs\" role=\"tablist\">");
                for (int vi = 0; vi < ms.Length; vi++) {
                    string tag = TimeTag(S(D(ms[vi]), "title"));
                    sb0.Append("<button type=\"button\" role=\"tab\" data-v=\"" + vi + "\" data-t=\"" + tag + "\"" + (vi == 0 ? " class=\"on\" aria-selected=\"true\"" : " aria-selected=\"false\"") + ">" + (tag == "day" ? "☀ " : tag == "night" ? "☾ " : "") + H(VarName(D(ms[vi]))) + "</button>");
                }
                sb0.Append("</div>\n");
            }
            for (int vi = 0; vi < ms.Length; vi++) {
                var md = D(ms[vi]); string slug = Slug(S(md, "title")); var icons = A(md, "icons"); var ranks = A(md, "yieldRanks");
                sb0.Append("<div class=\"variant" + (vi > 0 ? " off" : "") + "\" data-v=\"" + vi + "\" data-t=\"" + TimeTag(S(md, "title")) + "\" id=\"" + slug + "\">" + (ms.Length > 1 ? "<h3 class=\"vt\">" + TJ(S(md, "titleJapanese")) + "</h3>\n" : ""));
                // 포인트 데이터: 첫 등급의 (에리어, 순번) 순서 = 지도 아이콘 순서
                var pts = new Dictionary<string, Dictionary<int, string>>(); // key → 등급 → json
                var zoneName = new Dictionary<string, string>(); var order = new List<string>();
                foreach (var yr in ranks) {
                    var yd = D(yr); int r = rk(S(yd, "rankJapanese"));
                    foreach (var z in A(yd, "zones")) {
                        var zd = D(z); int k = 0;
                        foreach (var sp in A(zd, "spots")) {
                            var s = D(sp); string key = S(zd, "zone") + "#" + (++k);
                            if (!pts.ContainsKey(key)) { pts[key] = new Dictionary<int, string>(); zoneName[key] = Tk(S(zd, "zoneJapanese")); order.Add(key); }
                            pts[key][r] = "[" + (S(s, "count") == "" ? "0" : S(s, "count")) + "," + items(A(s, "summer")) + "," + items(A(s, "winter")) + "," + items(A(s, "breeding")) + "]";
                        }
                    }
                }
                var firstKeys = new List<string>();
                if (ranks.Length > 0) foreach (var z in A(D(ranks[0]), "zones")) { int k = 0; foreach (var sp in A(D(z), "spots")) firstKeys.Add(S(D(z), "zone") + "#" + (++k)); }
                string img = MapImage(S(md, "title"));
                sb0.Append("<div class=\"mapwrap\"><div class=\"mapview\"><div class=\"mapbox big\">" + (img != null ? "<img src=\"images/maps/" + img + ".png\" alt=\"" + T(S(md, "titleJapanese")) + " 지도\">" : ""));
                for (int i = 0; i < icons.Length; i++) {
                    var ic = D(icons[i]); string nm = Tk(S(ic, "japanese")); if (nm == "") nm = "기타 표시";
                    if (!kindIcon.ContainsKey(nm)) { kinds.Add(nm); kindIcon[nm] = S(ic, "icon"); kindCount[nm] = 0; }
                    kindCount[nm]++;
                    string pid = slug + "-" + (i + 1); bool has = i < firstKeys.Count;
                    sb0.Append("<button type=\"button\" class=\"pt" + (has ? "" : " nodata") + "\" data-p=\"" + pid + "\" data-k=\"" + kinds.IndexOf(nm) + "\" style=\"left:" + H(S(ic, "x")) + "%;top:" + H(S(ic, "y")) + "%\" title=\"" + (has ? (i + 1) + ". " : "") + H(nm) + "\">" + Icon(S(ic, "icon")) + (has ? "<b>" + (i + 1) + "</b>" : "") + "</button>");
                    if (has) {
                        string key = firstKeys[i];
                        js.Append(Js(pid) + ":{n:" + Js(nm) + ",i:" + Js(S(ic, "icon")) + ",z:" + Js(zoneName[key]) + ",no:" + (i + 1) + ",m:" + Js(Tk(S(md, "titleJapanese"))) + ",r:{" + string.Join(",", pts[key].Select(kv => kv.Key + ":" + kv.Value)) + "}},");
                    }
                }
                sb0.Append("</div></div>");
                // 지도 위치가 없는(상위 등에만 있는) 포인트
                var extra = order.Where(k => !firstKeys.Contains(k)).ToList();
                sb0.Append("<div class=\"mapside\"><div class=\"mapcount\"></div>");
                if (extra.Count > 0) {
                    sb0.Append("<div class=\"extra\"><b>지도에 위치가 없는 포인트</b><span class=\"meta\"> (상위 등에서만 생김)</span><div>");
                    int e = 0; foreach (var k in extra) { e++; string pid = slug + "-x" + e; sb0.Append("<button type=\"button\" class=\"xpt\" data-p=\"" + pid + "\">" + H(zoneName[k]) + "</button>"); js.Append(Js(pid) + ":{n:\"추가 포인트\",i:\"\",z:" + Js(zoneName[k]) + ",no:0,m:" + Js(Tk(S(md, "titleJapanese"))) + ",r:{" + string.Join(",", pts[k].Select(kv => kv.Key + ":" + kv.Value)) + "}},"); }
                    sb0.Append("</div></div>");
                }
                sb0.Append("</div></div></div>\n");
            }
            sb0.Append("</section>\n");
        }
        var sb = new StringBuilder(Head("maps.html", "맵 채집", "지도 위 <b>아이콘을 누르면</b> 그 채집 포인트에서 나오는 아이템과 확률이 나옵니다. 위 필터에서 등급・계절과 포인트 종류를 고르고, 아이템 이름을 넣으면 그 아이템이 나오는 포인트만 빛납니다.", false));
        sb.Append("<div class=\"mseason\">" + string.Join("", new[] { "温暖期", "寒冷期", "繁殖期" }.Select((n, i) => "<button type=\"button\" class=\"se" + (i == 0 ? " on" : "") + "\" data-s=\"" + i + "\"><span class=\"emo\">" + SeasonEmoji[i] + "</span>" + T(n) + "</button>")) + "</div>\n");
        sb.Append("<div class=\"mregion\">" + string.Join("", regions.Select((g, i) => "<button type=\"button\" class=\"rg" + (i == 0 ? " on" : "") + "\" data-loc=\"" + g[0] + "\">" + "<img class=\"rgimg\" src=\"images/" + g[2] + "\" alt=\"\">" + "<span>" + H(g[1]) + "</span><i class=\"hitn\"></i></button>")) + "</div>\n");
        sb.Append("<div class=\"mfilter\">");
        sb.Append("<div class=\"mrow\"><span class=\"mlab\">시간대</span><span class=\"daynight\"><button type=\"button\" data-all=\"day\">☀ 낮</button><button type=\"button\" data-all=\"night\">☾ 밤</button></span><span class=\"mlab gap\">등급</span>" + string.Join("", rkName.Select((n, i) => "<button type=\"button\" class=\"mchip rk" + (i == 0 ? " on" : "") + "\" data-r=\"" + i + "\">" + n + "</button>")) + "</div>");
        sb.Append("<div class=\"mrow\"><span class=\"mlab\">종류</span><button type=\"button\" class=\"mchip all\" data-all=\"1\">모두 켜기</button><button type=\"button\" class=\"mchip all\" data-all=\"0\">모두 끄기</button></div>");
        for (int gi = 0; gi < 3; gi++) {
            var ks = kinds.Select((k, i) => new { k, i }).Where(x => KindGroup(x.k) == gi).ToList(); if (ks.Count == 0) continue;
            sb.Append("<div class=\"mrow kgrp\"><button type=\"button\" class=\"mchip grp on\" data-grp=\"" + gi + "\">" + KindGroupName[gi] + "</button><span class=\"kinds\">" + string.Join("", ks.Select(x => "<label class=\"mchip kd on\" data-grp=\"" + gi + "\"><input type=\"checkbox\" data-k=\"" + x.i + "\" checked>" + Icon(kindIcon[x.k]) + H(x.k) + "</label>")) + "</span></div>");
        }
        sb.Append("<div class=\"mrow\"><span class=\"mlab\">확대</span><button type=\"button\" class=\"mchip zm\" data-z=\"-\">－</button><input id=\"mzoom\" type=\"range\" min=\"100\" max=\"300\" step=\"25\" value=\"100\"><button type=\"button\" class=\"mchip zm\" data-z=\"+\">＋</button><span id=\"mzv\">100%</span><button type=\"button\" class=\"mchip zm\" data-z=\"0\">원래대로</button><span class=\"meta\">확대하면 지도를 끌어서 옮길 수 있습니다</span></div>");
        sb.Append("<div class=\"mrow\"><span class=\"mlab\">아이템</span><input id=\"mq\" type=\"search\" placeholder=\"아이템 이름 (예: 마카라이트, 벌꿀, 각메뚜기)\" autocomplete=\"off\"><span id=\"mqn\"></span></div>");        sb.Append("</div>\n").Append(sb0);
        sb.Append("<div id=\"wpop\" hidden><div class=\"wp-box\" role=\"dialog\" aria-modal=\"true\"><button type=\"button\" class=\"wp-x\" aria-label=\"닫기\">✕</button><div class=\"wp-body\"></div></div></div>\n");
        sb.Append("<script>window.MP={" + js.ToString().TrimEnd(',') + "};window.MPR=" + "[" + string.Join(",", rkName.Select(Js)) + "];window.MPS=[" + string.Join(",", new[] { T("温暖期"), T("寒冷期"), T("繁殖期") }.Select(Js)) + "];</script>\n");
        Write("maps.html", sb.Append(Foot()).ToString());
    }
    static string TimeTag(string t) { return t.Contains("(Day)") ? "day" : t.Contains("(Night)") ? "night" : ""; }
    // 「밀림 (낮)」 → 지역 「밀림」, 탭 「낮」
    static string LocName(Dictionary<string, object> md) { string k = Tk(S(md, "titleJapanese")); int i = k.IndexOf(" ("); return i > 0 ? k.Substring(0, i) : k; }
    static string VarName(Dictionary<string, object> md) { var m = Regex.Match(Tk(S(md, "titleJapanese")), "\\((.+)\\)\\s*$"); return m.Success ? m.Groups[1].Value : Tk(S(md, "titleJapanese")); }
    static string MapImage(string t)
    {
        var m = new Dictionary<string, string> {
            {"Jungle (Day)","jungle_day"},{"Jungle (Night)","jungle_night"},{"Desert (Day)","desert_day"},{"Desert (Night)","desert_night"},
            {"Swamp (Day)","swamp"},{"Swamp (Night)","swamp"},{"Snowy Mountain (Day)","snowy_mountain"},{"Snowy Mountain (Night)","snowy_mountain"},
            {"Volcano (Day)","volcano_day"},{"Volcano (Night)","volcano_night"},{"Forest & Hills (Day)","forest"},{"Forest & Hills (Night)","forest"},
            {"The Tower (Foothills)","tower_camp"},{"The Tower (Layout 1)","tower_1"},{"The Tower (Layout 2)","tower_2"},{"The Tower (Layout 3)","tower_3"},
            {"Fort","fort"},{"Battle Quarters","battle_quarters"},{"Castle Schrade","schrade"},{"The Battleground","battlegrounds"},{"The Arena","arena"},{"The Grand Arena","great_arena"} };
        string v; return m.TryGetValue(t, out v) ? v : null;
    }

    // 「orange-icon honey-icon」 → 아이콘 묶음 그림
    public static string Icon(string cls)
    {
        var m = Regex.Match(cls ?? "", "^(\\w+)-icon ([\\w-]+-icon)$");
        return m.Success ? "<i class=\"ico " + m.Groups[1].Value + "-icon " + m.Groups[2].Value + "\"></i>" : "";
    }

    // 위키 아이콘 묶음(32px 칸, 8열×10행) 위치 → 크기와 상관없는 % 위치
    public static void IconCss(string posFile)
    {
        var sb = new StringBuilder("/* 아이콘 묶음: wiki.mholdschool.com MH2 위키 */\n.ico { display:inline-block; width:20px; height:20px; background-size:800% auto; background-repeat:no-repeat; vertical-align:-5px; margin-right:4px; image-rendering:pixelated; }\n");
        foreach (var c in new[] { "orange", "gray", "green", "blue", "red", "white", "yellow", "pink", "cyan", "purple" })
            sb.Append(".ico." + c + "-icon { background-image:url(\"../images/icons/icons_" + c + ".png\"); }\n");
        foreach (Match m in Regex.Matches(File.ReadAllText(posFile), "\\.([\\w-]+)\\{background-position:(-?\\d+)px (-?\\d+)px\\}")) {
            int col = -int.Parse(m.Groups[2].Value) / 32, row = -int.Parse(m.Groups[3].Value) / 32;
            sb.Append(".ico." + m.Groups[1].Value + " { background-position:" + (col * 100.0 / 7).ToString("0.###", CultureInfo.InvariantCulture) + "% " + (row * 100.0 / 9).ToString("0.###", CultureInfo.InvariantCulture) + "%; }\n");
        }
        File.WriteAllText(Path.Combine(OutDir, "assets", "icons.css"), sb.ToString(), new UTF8Encoding(false));
    }

    static string Yield(object[] list)
    {
        if (list.Length == 0) return "-";
        return string.Join("<br>", list.Select(x => { var d = D(x); return T(S(d, "japanese")) + " <span class=\"pc\">" + H(S(d, "percent")) + "%</span>"; }));
    }

    // ================= 기타 (헌터 칭호) =================
    public static void Misc(string srcHtml)
    {
        string t = File.ReadAllText(srcHtml, Encoding.UTF8);
        t = t.Substring(t.IndexOf("<article"));
        var sb = new StringBuilder(Head("miscellany.html", "기타 — 헌터 칭호", "조건을 채우면 열리는 칭호. 칭호 하나를 골라 프로필에 표시할 수 있고, 다른 플레이어에게 온라인으로 보인다. <span class=\"tag\">온라인</span>은 온라인에서만 얻는 칭호.", true));
        foreach (Match sec in Regex.Matches(t, "(?s)<details><summary><h3>(.*?)</h3></summary>(.*?)</details>")) {
            string grp = sec.Groups[1].Value.Trim();
            var tb = Regex.Match(sec.Groups[2].Value, "(?s)<table.*?</table>"); if (!tb.Success) continue;
            sb.Append("<section class=\"grp\"><h2>" + P(grp) + "</h2>\n" + Table("ft", "칭호", "조건"));
            foreach (var r in HtmlGrid.Parse(tb.Value)) {
                if (r.Length < 3 || r[2] == "Requirements") continue;
                string ja = r[0], req = r[2]; bool online = false;
                if (req.EndsWith(" Online")) { online = true; req = req.Substring(0, req.Length - 7).Trim(); }
                string ko = Dict.Get(ja) ?? Dict.Get("「" + ja + "」") ?? ja; ko = ko.Trim('「', '」');
                sb.Append(Tr("<b>" + H(ko) + "</b>" + (ko != ja ? " <span class=\"ja\">" + H(ja) + "</span>" : ""), ReqKo(req) + (online ? " <span class=\"tag\">온라인</span>" : "")));
            }
            sb.Append(TEnd + "</section>\n");
        }
        Write("miscellany.html", sb.Append(Foot()).ToString());
    }
    static string ReqKo(string req)
    {
        Match m;
        if ((m = Regex.Match(req, "^Reach HR(\\d+)$")).Success) return "HR " + m.Groups[1].Value + " 달성";
        if ((m = Regex.Match(req, "^Hunt (\\d+) (.+)$")).Success && EnName.ContainsKey(m.Groups[2].Value)) return H(EnName[m.Groups[2].Value]) + " " + m.Groups[1].Value + "마리 수렵";
        if ((m = Regex.Match(req, "^Hunt (\\d+) Rusted Kushala Daora$")).Success) return "녹슨 크샬다오라 " + m.Groups[1].Value + "마리 수렵";
        if ((m = Regex.Match(req, "^Hunt (\\d+) (Crimson|White) Fatalis$")).Success) return "밀라보레아스(" + (m.Groups[2].Value == "White" ? "선조룡" : "홍룡") + ") " + m.Groups[1].Value + "마리 수렵";
        if ((m = Regex.Match(req, "^Craft the (.+)$")).Success) {
            var names = Regex.Split(m.Groups[1].Value, ", (?=[A-Z])").Select(n => { string k; return EnName.TryGetValue(n.Trim(), out k) ? H(k) : P(n.Trim()); });
            return "생산: " + string.Join(", ", names);
        }
        return P(req);
    }

    // ================= 식사 =================
    public static void Meals()
    {
        var data = A(Load("meals"));
        // 식재료 아이콘: 아이템 데이터의 아이콘
        var icoOf = new Dictionary<string, string>();
        foreach (var o in A(Load("items"))) { var it = D(o); string j = S(it, "japanese"); if (j != "" && !icoOf.ContainsKey(j)) icoOf[j] = S(it, "icon"); }
        var sb = new StringBuilder(Head("meals.html", "식사", "점보 마을 마이하우스(주방 아이루) 식사. 먼저 <b>계절</b>을 고르세요. 요리는 가나다순이고, 식재료 이름을 누르면 아이템 페이지로 이동합니다.", true));
        var seasons = A(data).Select(D).ToList();
        sb.Append("<div class=\"gtabs seas\" data-g=\"season\">");
        for (int i = 0; i < seasons.Count; i++) sb.Append("<button type=\"button\" data-t=\"" + i + "\"" + (i == 0 ? " class=\"on\"" : "") + "><span class=\"emo\">" + (i < 3 ? SeasonEmoji[i] : "") + "</span>" + T(S(seasons[i], "seasonJapanese")) + " <span class=\"cnt\">" + A(seasons[i], "meals").Length + "</span></button>");
        sb.Append("</div>\n");
        for (int i = 0; i < seasons.Count; i++) {
            var sd = seasons[i];
            sb.Append("<section class=\"grp gpanel" + (i == 0 ? "" : " off") + "\" data-g=\"season\" data-t=\"" + i + "\"><h2><span class=\"emo\">" + (i < 3 ? SeasonEmoji[i] : "") + "</span> " + TJ(S(sd, "seasonJapanese")) + "</h2>\n" + Table("ft meals", "요리", "식재료", "효과"));
            var ko = new CultureInfo("ko-KR");
            foreach (var md in A(sd, "meals").Select(D).OrderBy(m => Tk(S(m, "dishJapanese")), StringComparer.Create(ko, false))) {
                string ing = string.Join("<span class=\"plus\">+</span>", A(md, "ingredients").Select(x => { string j = S(D(x), "japanese"); string ic; icoOf.TryGetValue(j, out ic); return "<span class=\"ing\">" + ILink(j, Icon(ic ?? "") + T(j)) + "</span>"; }));
                string fx = string.Join(" ", A(md, "effects").Select(x => { var e = D(x); string amt = S(e, "amount"); return "<span class=\"fx " + (amt.StartsWith("-") ? "neg" : "pos") + "\">" + T(S(e, "statJapanese")) + " " + H(amt) + "</span>"; }));
                sb.Append(Tr("<b>" + TJ(S(md, "dishJapanese")) + "</b>", ing, fx == "" ? "-" : fx));
            }
            sb.Append(TEnd + "</section>\n");
        }
        Write("meals.html", sb.Append(Foot()).ToString());
    }
}
