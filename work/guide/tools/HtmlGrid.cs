using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// HTML 표를 rowspan/colspan을 펼친 2차원 격자로 읽는다(공략 사이트 번역용).
public static class HtmlGrid
{
    static string Clean(string s)
    {
        s = Regex.Replace(s, "(?i)<br\\s*/?>", "\n");
        s = Regex.Replace(s, "<[^>]+>", "");
        s = System.Net.WebUtility.HtmlDecode(s).Replace('\u00A0', ' ');
        return Regex.Replace(s, "[ \\t\\r]*\\n[ \\t\\r]*", "\n").Trim();
    }

    public static List<string[]> Parse(string tableHtml)
    {
        var grid = new List<List<string>>(); var pending = new Dictionary<long, string>(); // (row<<16|col) -> text
        int r = 0;
        foreach (Match tr in Regex.Matches(tableHtml, "(?is)<tr.*?</tr>")) {
            var row = new List<string>(); int c = 0;
            foreach (Match td in Regex.Matches(tr.Value, "(?is)<(td|th)([^>]*)>(.*?)</\\1>")) {
                while (pending.ContainsKey(((long)r << 16) | (uint)c)) { row.Add(pending[((long)r << 16) | (uint)c]); c++; }
                string attrs = td.Groups[2].Value; string text = Clean(td.Groups[3].Value);
                int rs = 1, cs = 1; var m = Regex.Match(attrs, "(?i)rowspan\\s*=\\s*\"?(\\d+)"); if (m.Success) rs = int.Parse(m.Groups[1].Value);
                m = Regex.Match(attrs, "(?i)colspan\\s*=\\s*\"?(\\d+)"); if (m.Success) cs = int.Parse(m.Groups[1].Value);
                for (int k = 0; k < cs; k++) {
                    row.Add(text);
                    for (int j = 1; j < rs; j++) pending[((long)(r + j) << 16) | (uint)(c + k)] = text;
                }
                c += cs;
            }
            while (pending.ContainsKey(((long)r << 16) | (uint)c)) { row.Add(pending[((long)r << 16) | (uint)c]); c++; }
            grid.Add(row); r++;
        }
        var res = new List<string[]>(); foreach (var g in grid) res.Add(g.ToArray()); return res;
    }
}
