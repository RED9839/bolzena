using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bolzena.Fx
{
    // 색인 — 사도 키(에르핀) → 그 사도의 고학년 이펙트 이름들. 이펙트 본문은 이름으로 따로 읽는다(Resources)
    public class FxLibraryAsset : ScriptableObject
    {
        [Serializable]
        public class Hero
        {
            public string Key;       // 기획서 사도 키(에르핀 · 에르핀_왕도)
            public string Art;       // 원작 그림 이름(erpin)
            public string From;      // "" = 궁극기 · skill / other = 궁극기가 없어 스킬 · 다른 이펙트로 메움
            public List<string> Ult = new List<string>();
            // 카드 이펙트(Tools/fx_extract_more.py) — 원작 이름 낱말로 가른 갈래: 평타(attack) · 센 공격(Attack2 — power) · 스킬(skill · personal) · 시그니처(signaturecard)
            public List<string> Attack = new List<string>(), Power = new List<string>(), Skill = new List<string>(), Sig = new List<string>();
        }

        // 공용 갈래(heal · shield · shieldHit · buff · debuff · break · kill · oracle · revive) → 원작 이펙트 후보들
        [Serializable]
        public class Group
        {
            public string Kind;
            public List<string> Names = new List<string>();
        }

        [Serializable]
        public class Entry
        {
            public string Name;
            public string Hero;      // 원작 그림 이름
            public float Dur;
            public int Emitters;
            public bool Baked;       // 구운 낱장이 있다
        }

        public List<Hero> Heroes = new List<Hero>();
        public List<Entry> Effects = new List<Entry>();
        public List<Group> Common = new List<Group>();
    }
}
