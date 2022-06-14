using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FaqBot;
using FaqBot.HNSW;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Logging;
using static FaqBot.FaqHandler;

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
/*
var knnQuery = "when do my trackers arrive";
var results = faqHandler.Search(knnQuery, 15);
var sortedResults = results.OrderBy(i => i.Distance);

logger.LogInformation("Query \"{KnnQuery}\" results:\n{KnnResults}", knnQuery, string.Join(Environment.NewLine, sortedResults.Select(i =>
{
    var entry = GetEntry(i.Item);
    return $"Answer ({i.Distance}): \"{entry.Answer}\"";
})));
*/

FaqEntry GetEntry(ILazyItem<float[]> item)
{
    return faqHandler.GetEntry(((LazyKeyItem<int, float[]>)item).Key);
}

using var discordClient = new DiscordSocketClient();

async Task HandleCommandAsync(SocketMessage messageParam)
{
    // Don't process the command if it was a system message
    if (messageParam is not SocketUserMessage message) return;

    // Create a number to track where the prefix ends and the command begins
    int argPos = 0;

    // Determine if the message is a command based on the prefix and make sure no bots trigger commands
    if (!message.HasMentionPrefix(discordClient.CurrentUser, ref argPos) || message.Author.IsBot) return;

    var args = message.Content.Substring(argPos).Trim();
    logger.LogInformation(args);

    var searchResults = faqHandler.Search(args, 1);
    try
    {
        await message.ReplyAsync(GetEntry(searchResults.First().Item).Answer);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unable to reply");
    }
}

async Task Log(LogMessage message)
{
    logger.LogInformation(message.ToString());
}

async Task MainAsync()
{
    discordClient.MessageReceived += HandleCommandAsync;
    discordClient.Log += Log;
    await discordClient.LoginAsync(TokenType.Bot, File.ReadAllText("token.txt"));
    await discordClient.StartAsync();
    await Task.Delay(-1);
}

MainAsync().Wait();
