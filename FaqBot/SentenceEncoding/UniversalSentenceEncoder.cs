using System.Runtime.InteropServices;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace FaqBot.SentenceEncoding
{
    public class UniversalSentenceEncoder : IDisposable
    {
        private readonly ILogger<UniversalSentenceEncoder> Logger;

        public readonly string ModelPath;
        public readonly int OutputDimension = 512;

        private readonly SessionOptions SessionOptions = new();
        private readonly IntPtr LibraryHandle;
        private readonly InferenceSession Session;

        private readonly DenseTensor<string> InputTensor = new(1);
        private readonly NamedOnnxValue[] Inputs;

        public UniversalSentenceEncoder(ILogger<UniversalSentenceEncoder> logger, string modelPath)
        {
            Logger = logger;

            ModelPath = Path.GetFullPath(modelPath);

            SessionOptions.RegisterCustomOpLibraryV2("libs/ortcustomops.dll", out LibraryHandle);

            try
            {
                SessionOptions.AppendExecutionProvider_CPU();
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "CPU is not available");
            }

            try
            {
                SessionOptions.AppendExecutionProvider_CUDA();
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "CUDA is not available");
            }

            Session = new(ModelPath, SessionOptions);

            try
            {
                OutputDimension = Session.OutputMetadata.Single().Value.Dimensions[1];
                logger.LogInformation("Output dimension detected as {OutputDimension}", OutputDimension);
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "Output dimension could not be detected, defaulting to 512");
            }

            Inputs = new[] { NamedOnnxValue.CreateFromTensor("inputs", InputTensor) };
        }

        public float[] ComputeEmbedding(string input, float[] vectorBuffer)
        {
            InputTensor.SetValue(0, input);

            using var outputs = Session.Run(Inputs);
            var outputTensor = (DenseTensor<float>)outputs.Single().Value;

            for (var i = 0; i < OutputDimension; i++)
            {
                vectorBuffer[i] = outputTensor.GetValue(i);
            }

            return vectorBuffer;
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
            return ComputeEmbedding(input, new float[OutputDimension]);
        }

        public Vector<float> ComputeEmbeddingVector(string input)
        {
            return Vector<float>.Build.Dense(ComputeEmbedding(input));
        }

        public void Dispose()
        {
            Session.Dispose();
            SessionOptions.Dispose();
            NativeLibrary.Free(LibraryHandle);
            GC.SuppressFinalize(this);
        }
    }
}
