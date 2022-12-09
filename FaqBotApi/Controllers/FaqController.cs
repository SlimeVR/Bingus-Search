using FaqBot.FaqHandling;
using FaqBot.HNSW;
using Microsoft.AspNetCore.Mvc;
using static FaqBot.FaqHandling.FaqHandler;

namespace FaqBatApi.Controllers;

[ApiController]
[Route("[controller]")]
public class FaqController : ControllerBase
{
    private readonly ILogger<FaqController> _logger;

    private readonly FaqHandler _faqHandler;

    public FaqController(ILogger<FaqController> logger, FaqHandler faqHandler)
    {
        _logger = logger;
        _faqHandler = faqHandler;
    }

    private FaqEntry GetEntry(ILazyItem<float[]> item)
    {
        return _faqHandler.GetEntry(((LazyKeyItem<int, float[]>)item).Key);
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
        var results = _faqHandler.Search(question, 5);

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
