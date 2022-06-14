using FaqBot;
using FaqBot.HNSW;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder => { builder.AddSimpleConsole(options => { options.TimestampFormat = "[hh:mm:ss] "; }); });
var logger = loggerFactory.CreateLogger<Program>();

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

var modelPath = Path.Join(Environment.CurrentDirectory, "models/onnx/use_l_v5.onnx");
var faqHandler = new FaqHandler(loggerFactory, modelPath);

IEnumerable<ValueTuple<string, string>> QuestionEnumerable()
{
    foreach (var question in questions)
    {
        yield return new(question, "");
    }
}

faqHandler.AddItems(QuestionEnumerable());

StaticCompositeResolver.Instance.Register(MessagePackSerializer.DefaultOptions.Resolver);
StaticCompositeResolver.Instance.Register(new LazyKeyItemFormatter<int, float[]>(i => faqHandler.GetEntry(i).Vector!.AsArray()));
MessagePackSerializer.DefaultOptions.WithResolver(StaticCompositeResolver.Instance);

var knnQuery = "when do my trackers arrive";
var results = faqHandler.Search(knnQuery, 15);
var sortedResults = results.OrderBy(i => i.Distance);

logger.LogInformation("Query \"{KnnQuery}\" results:\n{KnnResults}", knnQuery, string.Join(Environment.NewLine, sortedResults.Select(i => $"\"{questions[((LazyKeyItem<int, float[]>)i.Item).Key]}\": {i.Distance}")));
