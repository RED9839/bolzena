using System;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 사도 135명의 겉모습(이름 · 성격 · 종족 · 역할 · 별 · 그림) — 웹판에서 뽑은 화면용 표(Tools~/build_roster.mjs → Resources/RunUI/roster.json).
    // 규칙(카드 · 수치)은 코어 데이터(GameData.Heroes)에 있다. 코어에 있는 사도만 판에 데려갈 수 있다.
    // 2026-10-11 엘다인 20명 전환 — 사도 목록 · 편성 · 도감 · 로비 고르기는 Players(고를 수 있는 사도)만 보인다. 표(All)는 이벤트 NPC · 보스 클론의 겉모습 찾기용으로 그대로 둔다.
    [Serializable]
    public class HeroInfo
    {
        public string key, ko, nature, race, role, blurb, keyword, ult, art;
        /// <summary>원작 표의 서는 자리(front · mid · back) — 규칙이 아니다(사도 열은 없다). 전투 연출의 공격 모습(근접 돌진 · 타격 결)을 고르는 데만 쓴다(Look).</summary>
        public string row;
        public int star, hp, atk, def, crit;
        [NonSerialized] public string CoreId;          // 코어 데이터의 사도 id(플레이어 사도면 고를 수 있다)
        [NonSerialized] public bool CloneOnly;         // 코어의 클론 전용 사도(GameData.CloneHeroes) — 보스 클론으로만 나온다(고를 수 없다)
        public bool Playable => CoreId != null && !CloneOnly;
        public Sprite Icon => Theme.HeroIcon(art);
    }

    [Serializable] class RosterFile { public List<HeroInfo> heroes = new List<HeroInfo>(); }

    public static class Roster
    {
        static List<HeroInfo> all;
        static Dictionary<string, HeroInfo> byCore;

        public static List<HeroInfo> All => all ??= Load();
        /// <summary>고를 수 있는 사도(코어 플레이어 사도) — 사도 목록 · 편성 · 도감 · 로비 고르기 · 무작위 편성.</summary>
        public static IEnumerable<HeroInfo> Players => All.Where(h => h.Playable);

        static List<HeroInfo> Load()
        {
            var ta = Resources.Load<TextAsset>("RunUI/roster");
            var list = ta != null ? JsonUtility.FromJson<RosterFile>(ta.text).heroes : new List<HeroInfo>();
            if (ta == null) Debug.LogWarning("[RunUI] roster.json 없음 — Tools~/build_roster.mjs 를 돌리세요");
            return list;
        }

        /// <summary>코어 데이터의 사도를 표에 잇는다 — 이름이 같거나(공백 무시) 표 이름이 코어 이름으로 시작하면.</summary>
        public static void Link(GameData data)
        {
            byCore = new Dictionary<string, HeroInfo>();
            foreach (var h in All) { h.CoreId = null; h.CloneOnly = false; }
            // 플레이어 사도 먼저, 그다음 클론 전용 사도(보스 클론의 겉모습 · 클론 상세 — OfCore 로 찾는다)
            foreach (var hd in data.Heroes.Values.Concat(data.CloneHeroes.Values.Where(c => !data.Heroes.ContainsKey(c.Id))))
            {
                bool cloneOnly = !data.Heroes.ContainsKey(hd.Id);
                string n = Norm(hd.Name);
                var hit = All.FirstOrDefault(h => Norm(h.ko) == n || Norm(h.key) == n)
                          ?? All.FirstOrDefault(h => Norm(h.key).StartsWith(n) || Norm(h.ko).StartsWith(n));
                if (hit == null)
                {
                    // 표에 없는 코어 사도 — 겉모습을 코어에서 만든다
                    hit = new HeroInfo { key = hd.Id, ko = hd.Name, nature = hd.Nature, race = hd.Race, role = hd.Role, star = hd.Star, blurb = hd.Blurb };
                    All.Add(hit);
                }
                hit.CoreId = hd.Id;
                hit.CloneOnly = cloneOnly;
                // 규칙(성격 · 역할)은 코어 데이터가 정한다 — 원작 표와 다르면 코어를 따른다(카드 틀 색 · 초상 테가 한 값이 되게)
                if (!string.IsNullOrEmpty(hd.Nature) && hd.Nature != hit.nature) { Debug.Log($"[Roster] {hit.ko} 성격 표 {hit.nature} → 코어 {hd.Nature}"); hit.nature = hd.Nature; }
                byCore[hd.Id] = hit;
            }
        }

        static string Norm(string s) => (s ?? "").Replace(" ", "");

        public static HeroInfo OfCore(string coreId)
        {
            if (coreId != null && byCore != null && byCore.TryGetValue(coreId, out var h)) return h;
            return new HeroInfo { key = coreId, ko = coreId };
        }

        public static HeroInfo ByKey(string key) => All.FirstOrDefault(h => h.key == key);
    }
}
