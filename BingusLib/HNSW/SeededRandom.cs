using System.Runtime.CompilerServices;
using HNSW.Net;

namespace BingusLib.HNSW
{
    public sealed class SeededRandom(int seed) : IProvideRandomValues
    {
        private readonly Random _random = new(seed);

        public int Seed { get; } = seed;
        public bool IsThreadSafe { get; } = true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Next(int minValue, int maxValue)
        {
            lock (_random)
            {
                return _random.Next(minValue, maxValue);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float NextFloat()
        {
            lock (_random)
            {
                return _random.NextSingle();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NextFloats(Span<float> buffer)
        {
            lock (_random)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = NextFloat();
                }
            }
        }
    }
}
