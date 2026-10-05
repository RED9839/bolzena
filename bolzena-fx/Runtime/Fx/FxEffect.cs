using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bolzena.Fx
{
    // 원작 이펙트 한 벌 — 웹판 assets/fx/<사도>/fx.json 의 한 항목(tools/extract-fx.py 가 원작 ParticleSystem 에서 값싼 몫만 옮긴 것)을
    // 그대로 담는다. 단위는 원작 유니티 단위, 원점은 그 유닛의 발밑, y 는 위. 방출 공간 → 화면은 m(2×3)으로 이미 떨궈져 있다.
    // FxImport(에디터)가 만들고, FxRun 이 웹판 js/fx-burst.js 와 같은 셈으로 돌린다.
    public class FxEffect : ScriptableObject
    {
        public string Hero;          // 원작 그림 이름(erpin)
        public float Dur;            // 방출이 끝나고 마지막 입자가 사라질 때까지(길어야 6초)
        public List<FxEmitter> Em = new List<FxEmitter>();
    }

    public enum FxShapeKind { None, Sphere, Hemi, Cone, Circle, Donut, Edge, Box, Rect }

    [Serializable]
    public class FxShape
    {
        public FxShapeKind K;
        public float R;
        public float T = 1;          // 두께(0 = 껍질에서만)
        public float Arc = 360;
        public float A;              // 원뿔 각(도)
        public float Rnd, Sph;       // 무작위 방향 · 구 방향 섞기
    }

    [Serializable]
    public class FxSheetAnim
    {
        public int Tx = 1, Ty = 1;
        public int Row;              // 0 = 칸 전체, 1 = 줄 무작위, 2+ = 그 줄(Row-2)
        public bool HasF; public Vector2 F;        // 상수 프레임(0~1)
        public Vector2[] Fk;         // 수명에 따른 프레임(0~1) 키
        public bool HasSf; public Vector2 Sf;      // 시작 프레임
        public float Cyc = 1;
        public float Fps;            // 0 이면 수명 기준
    }

    [Serializable]
    public class FxEmitter
    {
        public string N;
        public Texture2D Tex;
        public string TexPath;       // 원본 경로(_shared/FX_IN_Glow.png) — 살펴보기용
        public bool Add;
        public Color Tint = Color.white;   // HDR(최대 4)
        public float Dur = 1;
        public bool Loop;
        public Vector2 Delay, Life, Speed, Size, Rot, Spin;
        public float Sy = 1;
        public Color[] Col = { Color.white };
        public bool ColLerp;
        public float Grav, Rate;
        public Vector4[] Bursts;     // (t, n, 횟수, 간격)
        public int Max = 1000;
        public bool HasShape; public FxShape Shape = new FxShape();
        public Vector2 Pos;
        public float[] M = { 1, 0, 0, 0, 1, 0 };
        public bool HasMt; public float[] Mt;
        public bool HasSc; public Vector2 Sc = Vector2.one;
        public Vector2[] SizeOL;     // (t, 배율)
        public float SizeK = 1;
        public Vector4[] ColOL;      // (t, r, g, b)
        public Vector2[] AlphaOL;    // (t, a)
        public bool HasVel; public Vector3 Vel; public bool VelW;
        public float Radial;
        public float SpdMul = 1;
        public bool HasForce; public Vector3 Force; public bool ForceW;
        public bool HasLimit; public float Limit, Damp;
        public float Drag;
        public bool HasSheet; public FxSheetAnim Sheet = new FxSheetAnim();
        public float Asp = 1;
        public bool HasStretch; public Vector2 Stretch;
        public bool Flat;
        public bool HasQuad; public Vector4 Quad;
        public bool HasPivot; public Vector2 Pivot;
        public bool HasExt; public Vector2 Ext = Vector2.one;
        public bool M3d;             // 입체 메시 — 판 한 장으로는 못 그려 뺀다(웹판과 같다)
        public float FlipU;          // 가로 뒤집기 확률
    }
}
