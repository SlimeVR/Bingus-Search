using FaqBot;
using FaqBot.HNSW;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Logging;

// Setup logging
using var loggerFactory = LoggerFactory.Create(builder => { builder.AddSimpleConsole(options => { options.TimestampFormat = "[hh:mm:ss] "; }); });
var logger = loggerFactory.CreateLogger<Program>();

// Setup the FAQ handler
var modelPath = Path.Join(Environment.CurrentDirectory, "models/onnx/use_l_v5.onnx");
var faqHandler = new FaqHandler(loggerFactory, modelPath);

IEnumerable<ValueTuple<string, string>> FaqEnumerable(string path)
{
    foreach (var question in File.ReadLines(path))
    {
        if (string.IsNullOrWhiteSpace(question) || question.StartsWith('#')) continue;

        var qa = question.Split('|', 2);
        if (qa.Length < 2) continue;

        yield return new(qa[0], qa[1]);
    }
}

// Add all questions
faqHandler.AddItems(FaqEnumerable("FAQ.txt"));

// Setup HNSW serialization stuff
StaticCompositeResolver.Instance.Register(MessagePackSerializer.DefaultOptions.Resolver);
StaticCompositeResolver.Instance.Register(new LazyKeyItemFormatter<int, float[]>(i => faqHandler.GetEntry(i).Vector!.AsArray()));
MessagePackSerializer.DefaultOptions.WithResolver(StaticCompositeResolver.Instance);

// Test query
var knnQuery = "when do my trackers arrive";
var results = faqHandler.Search(knnQuery, 15);
var sortedResults = results.OrderBy(i => i.Distance);

logger.LogInformation("Query \"{KnnQuery}\" results:\n{KnnResults}", knnQuery, string.Join(Environment.NewLine, sortedResults.Select(i =>
{
    var entry = faqHandler.GetEntry(((LazyKeyItem<int, float[]>)i.Item).Key);
    return $"Answer ({i.Distance}): \"{entry.Answer}\"";
})));
