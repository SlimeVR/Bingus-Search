using System.Runtime.InteropServices;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace BingusLib.SentenceEncoding
{
    public class UniversalSentenceEncoder : IDisposable
    {
        private readonly ILogger<UniversalSentenceEncoder> Logger;

        public readonly string ModelPath;
        public readonly int OutputDimension = 512;

        private readonly SessionOptions _sessionOptions = new();
        private readonly IntPtr _libraryHandle;
        private readonly InferenceSession _session;

        private readonly DenseTensor<string> _inputTensor = new(1);
        private readonly NamedOnnxValue[] _inputs;

        public UniversalSentenceEncoder(ILogger<UniversalSentenceEncoder> logger, string modelPath)
        {
            Logger = logger;

            ModelPath = Path.GetFullPath(modelPath);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _sessionOptions.RegisterCustomOpLibraryV2("libs/ortextensions.dll", out _libraryHandle);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _sessionOptions.RegisterCustomOpLibraryV2("libs/libortextensions.so", out _libraryHandle);
            }
            else
            {
                Logger.LogError("Running on an unsupported OS, could not load ONNX custom ops!");
            }

            try
            {
                _sessionOptions.AppendExecutionProvider_CPU();
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "CPU is not available");
            }

            try
            {
                _sessionOptions.AppendExecutionProvider_CUDA();
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "CUDA is not available");
            }

            _session = new(ModelPath, _sessionOptions);

            try
            {
                OutputDimension = _session.OutputMetadata.Single().Value.Dimensions[1];
                logger.LogInformation("Output dimension detected as {OutputDimension}", OutputDimension);
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "Output dimension could not be detected, defaulting to 512");
            }

            _inputs = new[] { NamedOnnxValue.CreateFromTensor("inputs", _inputTensor) };
        }

        public float[] ComputeEmbedding(string input, float[] vectorBuffer)
        {
            _inputTensor.SetValue(0, input);

            using var outputs = _session.Run(_inputs);
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
            _session.Dispose();
            _sessionOptions.Dispose();
            NativeLibrary.Free(_libraryHandle);
            GC.SuppressFinalize(this);
        }
    }
}
