using MathNet.Numerics.LinearAlgebra;

namespace BingusLib.SentenceEncoding
{
    public abstract class SentenceEncoder
    {
        public int EmbeddingDimension { get; protected set; }

        protected SentenceEncoder(int embeddingDimension)
        {
            EmbeddingDimension = embeddingDimension;
        }

        protected abstract float[] InternalComputeEmbedding(string input, float[] vectorBuffer);

        public float[] ComputeEmbedding(string input, float[] vectorBuffer)
        {
            if (vectorBuffer.Length < EmbeddingDimension)
            {
                throw new ArgumentException(
                    $"The provided buffer {nameof(vectorBuffer)} is not big enough to store the embedding vector (Expected: {EmbeddingDimension}, Provided: {vectorBuffer.Length})",
                    nameof(vectorBuffer)
                );
            }

            return InternalComputeEmbedding(input, vectorBuffer);
        }

        public Vector<float> ComputeEmbeddingVector(string input, Vector<float> vectorBuffer)
        {
            var internalArray = vectorBuffer.AsArray();
            if (internalArray != null)
            {
                ComputeEmbedding(input, internalArray);
                return vectorBuffer;
            }
            else
            {
                vectorBuffer.SetValues(ComputeEmbedding(input, vectorBuffer.ToArray()));
                return vectorBuffer;
            }
        }

        public float[] ComputeEmbedding(string input)
        {
            return ComputeEmbedding(input, new float[EmbeddingDimension]);
        }

        public Vector<float> ComputeEmbeddingVector(string input)
        {
            return Vector<float>.Build.Dense(ComputeEmbedding(input));
        }
    }
}
