// 엔진이 짓는 글을 모두 뽑아 낸다(문체 검사 style_check.py 의 입력) — Docs/문체.md
// 쓰는 법: StyleDump.exe <콘텐츠 폴더> > out.json   · 줄마다 {src, cat, text}
//   cat: fx = 효과 글(명사형 · 개조식), name = 이름(명사), ui = 안내 글(합니다체)
using System; using System.Linq; using System.Collections.Generic; using Bolzena.Core; using Newtonsoft.Json;
static class StyleDump {
  static readonly List<object> o = new();
  static void Add(string src, string cat, string text) { if (!string.IsNullOrWhiteSpace(text)) o.Add(new { src, cat, text }); }
  static int Main(string[] a) {
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    var d = GameData.FromFolders(new List<string> { a.Length > 0 ? a[0] : @"C:\projects\bolzena-content-v2" });
    var t = new CardText(d);
    bool heroes = a.Length > 1 && a[1] == "heroes";   // 사도 글까지(2단계)
    foreach (var c in d.Cards.Values) {
      bool hero = c.Hero != null;
      if (hero && !heroes) continue;
      string w = hero ? "heroes" : "world";
      Add($"{w}/card/{c.Id}", "fx", t.Card(c));
      foreach (var od in c.Oracles) Add($"{w}/oracle/{c.Id}/{od.Name}", "fx", t.Oracle(c, od));
      foreach (var b in c.Blesses) Add($"{w}/bless/{c.Id}/{b.Name}", "fx", t.Bless(b));
    }
    if (heroes) foreach (var h in d.Heroes.Values) {
      foreach (var x in t.Traits(h)) { Add($"heroes/trait/{h.Id}/{x.Name}", "fx", x.Body); }
      foreach (var f in h.Forms ?? new List<FormDef>()) Add($"heroes/form/{h.Id}/{f.Id}", "fx", t.Form(f));
    }
    foreach (var e in d.Equips.Values) Add($"world/equip/{e.Id}", "fx", t.Equip(e));
    foreach (var e in d.Enemies.Values) {
      Add($"world/enemy/{e.Id}", "fx", t.Enemy(e));
      var its = new List<Intent>(e.Intents); if (e.Open != null) its.Add(e.Open);
      if (e.Phase != null) its.AddRange(e.Phase.Intents); if (e.Phase2 != null) its.AddRange(e.Phase2.Intents);
      foreach (var it in its) Add($"world/intent/{e.Id}/{it.Say}", "fx", t.Intent(it));
    }
    foreach (var ev in d.Events) foreach (var op in ev.Options) {
      Add($"world/outcome/{ev.Id}/{op.Label}", "fx", t.Outcomes(op.Out));
      foreach (var g in op.Gamble ?? new List<Gamble>()) Add($"world/outcome/{ev.Id}/{op.Label}/gamble", "fx", t.Outcomes(g.Out));
    }
    foreach (var kv in CardText.TIPS) Add($"core/tip/{kv.Key}", "fx", kv.Value);
    foreach (var kv in R.DIVINE_KO) Add($"core/divine/{kv.Key}", "fx", kv.Value);
    foreach (var kv in R.RARES) Add($"core/rare/{kv.Key}", "fx", kv.Value);
    Console.WriteLine(JsonConvert.SerializeObject(o, Formatting.Indented));
    return 0;
  }
}
