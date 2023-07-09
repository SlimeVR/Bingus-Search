using BingusLib.FaqHandling;
using BingusLib.HNSW;
using Microsoft.AspNetCore.Mvc;
using static BingusLib.FaqHandling.FaqHandler;

namespace BingusApi.Controllers;

[ApiController]
[Route("[controller]")]
public class FaqController : ControllerBase
{
    private static readonly int MinQuery = 1;
    private static readonly int MaxQuery = 50;

    private readonly FaqHandler _faqHandler;

    public FaqController(FaqHandler faqHandler)
    {
        _faqHandler = faqHandler;
    }

    private FaqEntry GetEntry(ILazyItem<float[]> item)
    {
        return _faqHandler.GetEntry(((LazyKeyItem<int, float[]>)item).Key);
    }

    [HttpGet(template: "Search", Name = "Search")]
    public IEnumerable<FaqEntryResponse> Search(string question, int responseCount = 5)
    {
        // Needs to have something at least
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("There must be a question to query.", nameof(question));
        }

        responseCount = Math.Clamp(responseCount, MinQuery, MaxQuery);
        var results = _faqHandler.Search(question, responseCount);

        var responses = results.Select(result =>
            {
                var entry = GetEntry(result.Item);
                return new FaqEntryResponse()
                {
                    Relevance = (1f - result.Distance) * 100f,
                    MatchedQuestion = entry.Question,
                    Title = entry.Title,
                    Text = entry.Answer,
                };
            }).GroupBy(result => result.Text)
            .Select(groupedResults => groupedResults.MaxBy(result => result.Relevance) ?? groupedResults.First())
            .OrderByDescending(response => response.Relevance);

        return responses;
    }
}
