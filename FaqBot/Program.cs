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

var modelPath = Path.Join(Environment.CurrentDirectory, "models/onnx/use_l_v5.onnx");
using var encoder = new UniversalSentenceEncoder(loggerFactory.CreateLogger<UniversalSentenceEncoder>(), modelPath);

var vectorBuffer = Vector<float>.Build.Dense(encoder.OutputDimension);

Vector<float> PrintVectorEmbedding(string input, Vector<float> vector)
{
    logger.LogInformation("\"{Input}\":\n[{Vector}]", input, vector.ToVectorString());
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

logger.LogInformation("Distance: {Distance}", Distance.Cosine(vector1.AsArray(), vector2.AsArray()));

var questions = new string[]
{
    "Please specify upload_port while updating firmware",
    "Trying to upload firmware fails",
    "The server won’t start",
    "The SlimeVR server won’t start",
    "The WiFi Settings window outputs ERROR",
    "WiFi Settings window outputs symbols and nothing else",
    "tracker keeps flashing",
    "tracker never connects to Wi-Fi",
    "tracker is not appearing on the Server",
    "tracker doesn't show up",
    "aux tracker isn’t working",
    "extension isn’t working",
    "Sensor was reset error",
    "trackers are connected to my wifi but don’t turn up on the server",
    "trackers are connected to the server but aren’t turning up on Steam",
    "trackers aren't showing up in SteamVR",
    "Tracker doesn't show up on SteamVR but it does on the server",
    "trackers are bound to the wrong controllers in SteamVR",
    "trackers are drifting a lot",
    "feet sink into the floor",
    "feet slide a lot",
    "Trackers are moving in the wrong direction when I move",
    "Trackers are rotating on SteamVR in the wrong direction when I move",
    "Does setup take a long time and/or do you need to do it every time you play?",
    "When are they shipping?",
    "When does it ship?",
    "When are the trackers coming?",
    "can I use 3 trackers for full body tracking",
    "how many trackers do I need",
    "how do I set up my body proportions?",
};

foreach (var question in questions)
{
    vectors.Add(encoder.ComputeEmbedding(question));
}

var parameters = new SmallWorld<ILazyItem<float[]>, float>.Parameters();
var distance = new WrappedDistance<ILazyItem<float[]>, float[], float>(i => i.Value, CosineDistance.SIMD);

var graph = new SmallWorld<ILazyItem<float[]>, float>(distance.WrappedDistanceFunc, DefaultRandomGenerator.Instance, parameters);

IEnumerable<LazyKeyItem<int, float[]>> ConvertToLazyKeyItems(List<float[]> input)
{
    for (var i = 0; i < input.Count; i++)
    {
        yield return new(i, key => input[key]);
    }
}

graph.AddItems(ConvertToLazyKeyItems(vectors).ToArray());

var knnQuery = "when do my trackers arrive";
var results = graph.KNNSearch(new LazyItemValue<float[]>(encoder.ComputeEmbedding(knnQuery)), 15);
var sortedResults = results.OrderBy(i => i.Distance);

logger.LogInformation("Query \"{KnnQuery}\" results:\n{KnnResults}", knnQuery, string.Join(Environment.NewLine, sortedResults.Select(i => $"\"{questions[((LazyKeyItem<int, float[]>)i.Item).Key]}\": {i.Distance}")));
