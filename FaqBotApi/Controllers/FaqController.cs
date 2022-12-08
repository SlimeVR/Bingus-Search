using FaqBot.FaqHandling;
using FaqBot.HNSW;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Mvc;
using static FaqBot.FaqHandling.FaqHandler;

namespace FaqBatApi.Controllers;

[ApiController]
[Route("[controller]")]
public class FaqController : ControllerBase
{
    private readonly ILogger<FaqController> _logger;

    private readonly FaqHandler faqHandler;

    public FaqController(ILogger<FaqController> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;

        // Load the config
        var faqConfig = FaqConfigUtils.InitializeConfig(loggerFactory.CreateLogger<FaqConfig>());

        // Setup the FAQ handler
        var modelPath = Path.Join(Environment.CurrentDirectory, faqConfig.ModelPath);
        faqHandler = new(loggerFactory, modelPath);

        // Add all questions
        faqHandler.AddItems(faqConfig.QAEntryEnumerator());

        // Setup HNSW serialization stuff
        StaticCompositeResolver.Instance.Register(MessagePackSerializer.DefaultOptions.Resolver);
        StaticCompositeResolver.Instance.Register(new LazyKeyItemFormatter<int, float[]>(i => faqHandler.GetEntry(i).Vector!.AsArray()));
        MessagePackSerializer.DefaultOptions.WithResolver(StaticCompositeResolver.Instance);
    }

    private FaqEntry GetEntry(ILazyItem<float[]> item)
    {
        return faqHandler.GetEntry(((LazyKeyItem<int, float[]>)item).Key);
    }

    [HttpGet(Name = "Search")]
    public IEnumerable<FaqEntryResponse> Search(string question, int responseCount = 5)
    {
        // Needs to have something at least
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("There must be a question to query.", nameof(question));
        }

        responseCount = Math.Clamp(responseCount, 1, 10);
        var results = faqHandler.Search(question, 5);

        var responses = results.Select(result =>
        {
            var entry = GetEntry(result.Item);
            return new FaqEntryResponse()
            {
                Relevance = (1f - result.Distance) * 100f,
                Title = entry.Question,
                Text = entry.Answer,
            };
        }).Reverse();

        return responses;
    }
}
