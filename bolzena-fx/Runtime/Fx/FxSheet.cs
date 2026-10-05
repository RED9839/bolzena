using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bolzena.Fx
{
    // 구운 낱장 — 원작 이펙트를 유니티에서 프레임으로 구운 것(웹판 assets/fx-baked). 같은 이름의 파티클 판보다 먼저 쓴다
    public class FxSheet : ScriptableObject
    {
        public float Fps = 30;
        public int Frames, W, H, Cols;
        public Texture2D[] Pages;
        public int[] PageFrames;
        public Vector2 Anchor;       // 원점 픽셀(왼위 기준)
        public float Ppu;            // 시트 px / 원작 단위
        public float Cx;             // 보이는 영역 가로 가운데(원작 단위)
        public float Dur;
        // 웹판 tools/fx-baked.py 의 TUNE
        public float Scale = 1, Dx, Dy;
        public bool Reach;
        public float FadeFrom = -1;  // < 0 이면 없음
    }
}
