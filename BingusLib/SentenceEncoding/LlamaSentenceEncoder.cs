using LLama;
using LLama.Common;
using LLama.Extensions;

namespace BingusLib.SentenceEncoding
{
    public class LlamaSentenceEncoder : SentenceEncoder, IDisposable
    {
        private readonly LLamaWeights _weights;
        private readonly LLamaEmbedder _embedder;

        public LlamaSentenceEncoder(ModelParams modelParams)
        {
            _weights = LLamaWeights.LoadFromFile(modelParams);
            _embedder = new LLamaEmbedder(_weights, modelParams);

            EmbeddingDimension = _embedder.EmbeddingSize;
        }

        protected override float[] InternalComputeEmbedding(string input, float[] vectorBuffer)
        {
            var result = ComputeEmbedding(input);
            Array.Copy(result, vectorBuffer, result.Length);
            return vectorBuffer;
        }

        public override float[] ComputeEmbedding(string input)
        {
            return _embedder.GetEmbeddings(input).Result.Single().EuclideanNormalization();
        }

        public void Dispose()
        {
            _embedder.Dispose();
            _weights.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
