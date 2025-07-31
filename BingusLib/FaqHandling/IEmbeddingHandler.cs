using MathNet.Numerics.LinearAlgebra;

namespace BingusLib.FaqHandling
{
    public interface IEmbeddingHandler
    {
        void Put(string key, Vector<float> embedding);
        void Put(string key, float[] embedding);
        Vector<float>? Get(string key);
        float[]? GetRaw(string key);
        bool Has(string key);
    }
}
