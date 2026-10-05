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
    }
}
