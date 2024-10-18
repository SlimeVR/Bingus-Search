using BingusLib.Config;
using BingusLib.FaqHandling;
using BingusLib.HNSW;
using HNSW.Net;
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
    private readonly BingusConfig _bingusConfig;
    private readonly FaqConfig _faqConfig;

    public FaqController(FaqHandler faqHandler, BingusConfig bingusConfig, FaqConfig faqConfig)
    {
        _faqHandler = faqHandler;
        _bingusConfig = bingusConfig;
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
        var searchAmount = _bingusConfig.UseQ2A
            ? responseCount
            : (responseCount > 1 ? SearchAmount : 1);
        var results = _faqHandler.Search(question, searchAmount);

        IEnumerable<SmallWorld<ILazyItem<float[]>, float>.KNNSearchResult> response = results;

        // Only consider duplicates if Q2Q, there will only be one for Q2A
        if (!_bingusConfig.UseQ2A)
        {
            // Group the duplicates
            // Select the highest relevance entry for each duplicate group
            response = response
                .GroupBy(result => GetEntry(result.Item).Answer)
                .Select(groupedResults =>
                    groupedResults.MinBy(result => result.Distance) ?? groupedResults.First()
                );
        }

        // Sort the entries by relevance
        // Take only the requested number of results
        return response
            .OrderByDescending(result => -result.Distance)
            .Take(responseCount)
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
            });
        ;
    }

    [HttpGet(template: "Config", Name = "Config")]
    public FaqConfig Config()
    {
        return _faqConfig;
    }
}
