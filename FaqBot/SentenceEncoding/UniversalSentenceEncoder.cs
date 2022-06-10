using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace FaqBot.SentenceEncoding
{
    public class UniversalSentenceEncoder : IDisposable
    {
        public readonly string ModelPath;
        public readonly int OutputDimension;

        private readonly SessionOptions SessionOptions = new();
        private readonly InferenceSession Session;

        private readonly DenseTensor<string> InputTensor = new(1);
        private readonly NamedOnnxValue[] Inputs;

        public UniversalSentenceEncoder(string modelPath, int outputDimension = 512)
        {
            ModelPath = Path.GetFullPath(modelPath);
            OutputDimension = outputDimension;

            SessionOptions.RegisterCustomOpLibraryV2("libs/ortcustomops.dll", out var libraryHandle);
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
            SessionOptions.Dispose();
            Session.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
