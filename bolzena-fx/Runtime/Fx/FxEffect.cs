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
        public GameObject Ps;        // (10-16) 원작 ParticleSystem 그대로 조립한 프리팹(FxPsBuild) — 있으면 FxRun 이 우리 시뮬 대신 이것을 띄운다(BolzenaFx.UsePs)
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
        public bool SimW;            // 월드 시뮬(원작 simulationSpace World) — 방출 뒤 원점(총구 · 몸)을 따라가지 않는다(10-15 추출 보강)
        public int Rm = -1, Ra = -1; // 원작 그리는 법 · 정렬(없으면 -1) — 지금은 기록만
        // (10-15) 메시 몸 — 원작 메시 입자(휘는 칼빛 · 고리 · 띠)를 판 대신 그대로. 꼭짓점은 메시 제 3D(입자 크기 1 기준), MeshAx · MeshAy 가 화면 투영(view ∘ R)의 두 줄.
        //   입자 돌림은 메시 평면(z 축)에서 먼저 — 눕힌 칼빛이 제 평면에서 돌아 몸을 감싸는 타원이 된다. UV 는 유니티식(아래 0)
        public Vector3[] MeshV; public Vector2[] MeshUV; public int[] MeshT; public float[] MeshA;
        public Vector3 MeshAx = Vector3.right, MeshAy = Vector3.up;
        public string Shn;
        public string MaskPath; public Texture2D Mask;   // (10-15) 가리개 그림(원작 _Mask_Tex) — 알파에 곱한다. 판은 칸 0~1, 메시는 제 UV
        // (10-15) 사용자 데이터 Custom1 (x, y = 주 그림 UV 밀기 · w = 밝기 배율) — 성분마다 수명 곡선(키 하나면 상수). 비면 없음
        public Vector2[] C1x, C1y, C1w;
        public bool HasC1 => C1x != null && C1x.Length > 0;
        public Vector2[] C1z, C2x, C2y, C2z, C2w;   // (10-15) 나머지 사용자 데이터 성분 — 셰이더 그래프 재현이 쓴다(Custom1.z = 디졸브 문턱, Custom2.xy = 주 그림 배율, Custom2.z = 노이즈 세기)
        // 셰이더 그래프 재현(10-15) — 원작 재질 값을 담은 우리 셰이더 재질(Bolzena/FxMaster 등). 있으면 판 · 메시를 이 재질로 그린다
        public Material Sg;           // 원작 셰이더 이름(「Shader Graphs/FX_Master」 · 「Mobile/Particles/Additive」 …) — 메시를 그릴지 고를 때
        public bool HasMesh => MeshT != null && MeshT.Length >= 3 && MeshV != null && MeshUV != null && MeshUV.Length == MeshV.Length && MeshV.Length > 0;
    }
}
