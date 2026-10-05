using System;

namespace Bolzena.Core
{
    /// <summary>
    /// 결정적 난수 — 웹판 combat.js makeRng 와 같은 xorshift32(13 · 17 · 5). 씨앗이 같으면 같은 수열.
    /// State 로 속 상태를 읽고 되돌린다(저장 · 이어하기).
    /// </summary>
    public sealed class Rng
    {
        uint s;

        public Rng(long seed)
        {
            s = unchecked((uint)seed);
            if (s == 0) s = 1;
        }

        public uint State
        {
            get => s;
            set => s = value == 0 ? 1u : value;
        }

        /// <summary>[0, 1) 실수.</summary>
        public double Next()
        {
            s ^= s << 13;
            s ^= s >> 17;
            s ^= s << 5;
            return s / 4294967296.0;
        }

        /// <summary>[0, n) 정수 — Math.floor(rng() * n).</summary>
        public int Int(int n) => n <= 0 ? 0 : (int)Math.Floor(Next() * n);

        public T Pick<T>(System.Collections.Generic.IList<T> list) => list[Int(list.Count)];

        public Rng Clone() => new Rng(1) { s = s };
    }

    public static class Num
    {
        /// <summary>JS Math.round — 반은 위로(C# Math.Round 는 짝수 쪽이라 쓰지 않는다).</summary>
        public static int Round(double x) => (int)Math.Floor(x + 0.5);

        public static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;
    }
}
