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
    private static readonly int SearchAmount = MaxQuery * 4;

    private static readonly int MaxLength = 4000;

    private readonly FaqHandler _faqHandler;
    private readonly FaqConfig _faqConfig;

    public FaqController(FaqHandler faqHandler, FaqConfig faqConfig)
    {
        _faqHandler = faqHandler;
        _faqConfig = faqConfig;
    }

    private static FaqEntry GetEntry(ILazyItem<float[]> item)
    {
        return ((LazyKeyItem<FaqEntry, float[]>)item).Key;
    }

    [HttpGet(template: "Search", Name = "Search")]
    public IEnumerable<FaqEntryResponse> Search(string question, int responseCount = 5)
    {
        // Needs to have something at least
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("There must be a question to query.", nameof(question));
        }

        question = question.Length > MaxLength ? question[..MaxLength] : question;
        responseCount = Math.Clamp(responseCount, MinQuery, MaxQuery);

        // Actually query a larger set amount to reduce duplicates in the response,
        // but one result will never have duplicates
        var results = _faqHandler.Search(question, responseCount > 1 ? SearchAmount : 1);

        // Format the entry JSON
        // Group the duplicates
        // Select the highest relevance entry for each duplicate group
        // Sort the entries by relevance
        // Take only the requested number of results
        var responses = results
            .Select(result =>
            {
                var entry = GetEntry(result.Item);
                return new FaqEntryResponse()
                {
                    Relevance = Math.Clamp((1f - result.Distance) * 100f, 0f, 100f),
                    MatchedQuestion = entry.Question,
                    Title = entry.Title,
                    Text = entry.Answer,
                };
            })
            .GroupBy(result => result.Text)
            .Select(groupedResults =>
                groupedResults.MaxBy(result => result.Relevance) ?? groupedResults.First()
            )
            .OrderByDescending(response => response.Relevance)
            .Take(responseCount);

        return responses;
    }

    [HttpGet(template: "Config", Name = "Config")]
    public FaqConfig Config()
    {
        return _faqConfig;
    }
}
