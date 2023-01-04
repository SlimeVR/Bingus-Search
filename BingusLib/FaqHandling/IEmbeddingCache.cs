using MathNet.Numerics.LinearAlgebra;

namespace BingusLib.FaqHandling
{
    public interface IEmbeddingCache
    {
        void Add(string key, Vector<float> embedding);
        void Add(string key, float[] embedding);
        Vector<float>? Get(string key);
        float[]? GetRaw(string key);
        bool Has(string key);
    }
}
