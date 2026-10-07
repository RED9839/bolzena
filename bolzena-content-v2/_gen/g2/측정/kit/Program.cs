using System; using System.Linq; using Bolzena.Core;
static class P { static int Main(string[] a) {
  Console.OutputEncoding = System.Text.Encoding.UTF8;
  GameData d; try { d = GameData.FromFolder(a[1]); } catch (Exception ex) { Console.WriteLine("읽기 실패 " + ex.Message); return 2; }
  var tx = new CardText(d);
  if (a[0] == "check") {
    var v = Validator.Check(d);
    Console.WriteLine($"오류 {v.Errors.Count} · 주의 {v.Warnings.Count}");
    foreach (var e in v.Errors) Console.WriteLine("오류 " + e);
    foreach (var w in v.Warnings) Console.WriteLine("주의 " + w);
    return 0;
  }
  if (a[0] == "text") {
    var ids = a.Length > 2 ? a[2].Split(',') : d.Equips.Values.Where(e => e.Affinity == null).Select(e => e.Id).Concat(d.Cards.Values.Where(c => c.Neutral).Select(c => c.Id)).ToArray();
    foreach (var id in ids) {
      var e = d.Equip(id);
      if (e != null) { Console.WriteLine($"[장비] {e.Name} ({e.Grade} · {e.Slot}) 값 {CardValue.GearWorth(e.Effect):0.00}\n  {tx.Equip(e).Replace("\n","\n  ")}\n  「{e.Blurb}」"); continue; }
      var c = d.Card(id); if (c == null) { Console.WriteLine("없음 " + id); continue; }
      Console.WriteLine($"[교주] {c.Name} ({c.Grade} · {c.Cost}코 {c.Type} · {c.Price}골드) 값 {CardValue.Efficiency(d.View(c.Id)):0.00}\n  기본  {tx.Card(c).Replace("\n"," / ")}");
      for (int i = 0; i < c.Oracles.Count; i++) Console.WriteLine($"  신탁{i+1} 「{c.Oracles[i].Name}」 {tx.Oracle(c, c.Oracles[i]).Replace("\n"," / ")}  (값 {CardValue.Efficiency(d.View(c.Id, i+1)):0.00})");
      foreach (var b in c.Blesses) Console.WriteLine($"  축복 「{b.Name}」 {tx.Bless(b)}");
      Console.WriteLine($"  「{c.Blurb}」");
    }
  }
  return 0; } }
