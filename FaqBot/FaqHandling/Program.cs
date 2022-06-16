using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FaqBot.FaqHandling;
using FaqBot.HNSW;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Logging;
using static FaqBot.FaqHandling.FaqHandler;

// Setup logging
using var loggerFactory = LoggerFactory.Create(builder => { builder.AddSimpleConsole(options => { options.TimestampFormat = "[hh:mm:ss] "; }); });
var logger = loggerFactory.CreateLogger<Program>();

// Load the config
var faqConfig = FaqConfigUtils.InitializeConfig(loggerFactory.CreateLogger<FaqConfig>());

// Setup the FAQ handler
var modelPath = Path.Join(Environment.CurrentDirectory, "models/onnx/use_l_v5.onnx");
var faqHandler = new FaqHandler(loggerFactory, modelPath);

// Add all questions
faqHandler.AddItems(faqConfig.QAEntryEnumerator());

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
    if (!(faqConfig.TargetChannels.Contains(message.Channel.Id.ToString()) || message.HasMentionPrefix(discordClient.CurrentUser, ref argPos)) || message.Author.IsBot) return;

    var args = message.Content.Substring(argPos).Trim();
    //logger.LogInformation("Discord Message: {Message}", args);

    var searchResults = faqHandler.Search(args, 1);
    try
    {
        var topResult = searchResults.First();
        var entry = GetEntry(topResult.Item);
        var reply = entry.Answer;

        if (faqConfig.PrintRelevanceLevel)
        {
            reply = $"*({(1f - topResult.Distance) * 100f:0.0}% relevant)* {reply}";
        }

        await message.ReplyAsync(reply);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unable to reply to a Discord message");
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
