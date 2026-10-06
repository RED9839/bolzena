using System; using System.Linq; using System.IO; using System.Collections.Generic; using Bolzena.Core; using Newtonsoft.Json;
static class M {
  static int Main(string[] a) {
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    var d = GameData.FromFolders(new List<string>{ a.Length>0?a[0]:@"C:\projects\bolzena-content-v2" });
    var t = new CardText(d);
    string mode = a.Length>1?a[1]:"sheet";
    if (mode=="json") {
      var o = d.Heroes.Values.Select(h => new { id=h.Id, name=h.Name, traits=t.Traits(h).Select(x=>new{x.Kind,x.Name,x.Sub,x.Body}).ToList() });
      Console.WriteLine(JsonConvert.SerializeObject(o, Formatting.Indented));
    } else if (mode=="review") {
      foreach (var h in d.Heroes.Values) foreach (var k in h.AllKeywords) {
        Console.WriteLine($"## {h.Id} 「{k.Name}」\n  옛 desc: {k.Desc}\n  엔진 글:\n    " + t.Trait(h,k).Replace("\n","\n    "));
        var ids = h.Starter.Concat(d.UniquesOf(h.Id)).Distinct();
        foreach (var id in ids) { var c = d.Card(id); if (c==null) continue; var tx = t.Card(c); if (tx.Contains(k.Name)) Console.WriteLine($"  카드 「{c.Name}」 {tx}"); }
        if (h.Ult != null && t.Fx(h.Ult.Fx).Contains(k.Name)) Console.WriteLine($"  고학년 {t.Fx(h.Ult.Fx)}");
        Console.WriteLine();
      }
    } else if (mode=="tips") { foreach (var kv in CardText.TIPS) Console.WriteLine($"{kv.Key}\t{kv.Value}"); }
    else foreach (var h in d.Heroes.Values) { Console.WriteLine(t.HeroShort(h)); Console.WriteLine(); }
    return 0;
  }
}
