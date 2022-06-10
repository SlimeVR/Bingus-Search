using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace FaqBot.SentenceEncoding
{
    public class UniversalSentenceEncoder : IDisposable
    {
        private readonly ILogger<UniversalSentenceEncoder> Logger;

        public readonly string ModelPath;
        public readonly int OutputDimension;

        private readonly SessionOptions SessionOptions = new();
        private readonly IntPtr LibraryHandle;
        private readonly InferenceSession Session;

        private readonly DenseTensor<string> InputTensor = new(1);
        private readonly NamedOnnxValue[] Inputs;

        public UniversalSentenceEncoder(ILogger<UniversalSentenceEncoder> logger, string modelPath, int outputDimension = 512)
        {
            Logger = logger;

            ModelPath = Path.GetFullPath(modelPath);
            OutputDimension = outputDimension;

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

        public float[] ComputeEmbedding(string input)
        {
            return ComputeEmbedding(input, new float[OutputDimension]);
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
