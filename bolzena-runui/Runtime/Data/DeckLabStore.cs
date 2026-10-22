using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bolzena.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Bolzena.RunUI
{
    /// <summary>
    /// 「덱 만들기」(2026-10-20 사용자 — 시험 전용, Flow.DeckLab)의 덱 — 이 기기에만 둔다(여러 벌 · 이름).
    /// 덱 하나 = 자리 셋(사도 슬롯 글 HeroSlot.Json — 빈 자리는 null) + 시험 칸 id(시험 계정이 서버 「시험 칸」 에 올린 것 — 자리마다, 없으면 0).
    /// 파일: persistentDataPath/decklab(-save 꼬리)/decks.json — 판 세이브 · 사도 저장함(hero-slots)과 섞지 않는다. 서버로는 시험 계정이 「서버 시험 도전」 할 때만 올린다.
    /// </summary>
    public static class DeckLabStore
    {
        public sealed class Deck
        {
            public string Id;
            public string Name;
            public long Updated;
            /// <summary>자리 셋의 슬롯 글(HeroSlot.Json) — 빈 자리는 null.</summary>
            public List<string> Slots = new List<string> { null, null, null };
            /// <summary>자리마다 서버 시험 칸 id(/api/test/slots — 시험 계정만) · 없으면 0.</summary>
            public List<long> Test = new List<long> { 0, 0, 0 };

            public HeroSlot Slot(int i)
            {
                if (i < 0 || i >= Slots.Count || Slots[i] == null) return null;
                try { return HeroSlot.Parse(Slots[i]); } catch (Exception) { return null; }
            }
            public List<HeroSlot> Party() => Enumerable.Range(0, Slots.Count).Select(Slot).Where(s => s != null).ToList();
        }

        static string Tail { get { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-save"); return i >= 0 && i < a.Length - 1 ? "." + a[i + 1] : ""; } }
        static string Dir => Path.Combine(Application.persistentDataPath, "decklab" + Tail);
        static string FilePath => Path.Combine(Dir, "decks.json");

        static List<Deck> decks;
        public static List<Deck> All
        {
            get
            {
                if (decks != null) return decks;
                try { decks = File.Exists(FilePath) ? JsonConvert.DeserializeObject<List<Deck>>(File.ReadAllText(FilePath)) : null; }
                catch (Exception e) { Debug.LogWarning("[DeckLab] 읽지 못함 — " + e.Message); }
                decks ??= new List<Deck>();
                foreach (var d in decks)
                {
                    while (d.Slots.Count < Frontier.PARTY_MAX) d.Slots.Add(null);
                    while (d.Test.Count < Frontier.PARTY_MAX) d.Test.Add(0);
                }
                return decks;
            }
        }
        public static Deck Get(string id) => All.FirstOrDefault(d => d.Id == id);

        public static void Put(Deck d)
        {
            d.Updated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            All.RemoveAll(x => x.Id == d.Id);
            All.Insert(0, d);
            Save();
        }
        public static void Remove(string id) { All.RemoveAll(x => x.Id == id); Save(); }
        /// <summary>시험 · 데모가 비울 때.</summary>
        public static void Clear() { decks = new List<Deck>(); Save(); }

        static void Save()
        {
            try { Directory.CreateDirectory(Dir); File.WriteAllText(FilePath, JsonConvert.SerializeObject(All, Formatting.Indented)); }
            catch (Exception e) { Debug.LogWarning("[DeckLab] 쓰지 못함 — " + e.Message); }
        }

        public static string NewId() => DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" + UnityEngine.Random.Range(1000, 9999);
    }
}
