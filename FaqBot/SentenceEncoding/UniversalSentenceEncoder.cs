using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;

namespace FaqBot.SentenceEncoding
{
    public class USEInput
    {
        [ColumnName("serving_default_inputs")]
        [VectorType(1)]
        public string[] Input { get; set; } = Array.Empty<string>();
    }

    public class USEOutput
    {
        [ColumnName("StatefulPartitionedCall")]
        [VectorType(512)]
        public float[] Vector { get; set; } = Array.Empty<float>();
    }

    public class UniversalSentenceEncoder
    {
        public readonly string ModelDirectory;

        public readonly MLContext MLContext;
        public readonly TensorFlowModel Model;
        public readonly TensorFlowEstimator Estimator;

        public UniversalSentenceEncoder(string modelDirectory)
        {
            ModelDirectory = Path.GetFullPath(modelDirectory);

            MLContext = new MLContext();
            Model = MLContext.Model.LoadTensorFlowModel(ModelDirectory);
            Estimator = Model.ScoreTensorFlowModel("StatefulPartitionedCall", "serving_default_inputs");
        }

        public float[] ComputeEmbedding(string input)
        {
            var modelInput = new USEInput
            {
                Input = new string[] { input }
            };

            var dataView = MLContext.Data.LoadFromEnumerable(new List<USEInput>() {
                modelInput
            });
            var transformer = Estimator.Fit(dataView);

            var engine = MLContext.Model.CreatePredictionEngine<USEInput, USEOutput>(transformer);

            return engine.Predict(modelInput).Vector;
        }

    }
}
