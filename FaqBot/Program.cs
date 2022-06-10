using FaqBot.HNSW;
using FaqBot.SentenceEncoding;
using HNSW.Net;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder => { builder.AddSimpleConsole(options => { options.TimestampFormat = "[hh:mm:ss] "; }); });
var logger = loggerFactory.CreateLogger<Program>();

var vectors = new List<float[]>(2);

StaticCompositeResolver.Instance.Register(MessagePackSerializer.DefaultOptions.Resolver);
StaticCompositeResolver.Instance.Register(new LazyKeyItemFormatter<int, float[]>(i => vectors[i]));
MessagePackSerializer.DefaultOptions.WithResolver(StaticCompositeResolver.Instance);

var modelPath = Path.Join(Environment.CurrentDirectory, "models/onnx/use-m_l_v3.onnx");
using var encoder = new UniversalSentenceEncoder(loggerFactory.CreateLogger<UniversalSentenceEncoder>(), modelPath);

var vectorBuffer = Vector<float>.Build.Dense(encoder.OutputDimension);

Vector<float> PrintVectorEmbedding(string input, Vector<float> vector)
{
    logger.LogInformation($"\"{input}\":\n[{vector.ToVectorString()}]");
    return vector;
}

Vector<float> PrintEmbedding(string input)
{
    return PrintVectorEmbedding(input, encoder.ComputeEmbeddingVector(input, vectorBuffer));
}

Vector<float> PrintEmbeddingNewVector(string input)
{
    return PrintVectorEmbedding(input, encoder.ComputeEmbeddingVector(input));
}

PrintEmbedding("dog");
PrintEmbedding("Puppies are nice.");
PrintEmbedding("I enjoy taking long walks along the beach with my dog.");

var vector1 = PrintEmbeddingNewVector("The WiFi Settings window outputs symbols and nothing else");
var vector2 = PrintEmbeddingNewVector("My tracker is not appearing on the Server");

logger.LogInformation($"Distance: {Distance.Cosine(vector1.AsArray(), vector2.AsArray())}");

var questions = new string[]
{
    ""
};

vectors.Add(vector1.AsArray());
vectors.Add(vector2.AsArray());

var parameters = new SmallWorld<ILazyItem<float[]>, float>.Parameters();
var distance = new WrappedDistance<ILazyItem<float[]>, float[], float>(i => i.Value, CosineDistance.NonOptimized);

var graph = new SmallWorld<ILazyItem<float[]>, float>(distance.WrappedDistanceFunc, DefaultRandomGenerator.Instance, parameters);

graph.AddItems(new LazyKeyItem<int, float[]>[] {
    new(0, i => vectors[i]),
    new(1, i => vectors[i]),
});

var results = graph.KNNSearch(new LazyItemValue<float[]>(() => encoder.ComputeEmbedding("My trackers don't show up")), 2);

logger.LogInformation(string.Join(Environment.NewLine, results.Select(i => $"{(i.Item as LazyKeyItem<int, float[]>)?.Key}: {i.Distance}")));
