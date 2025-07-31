using System.Text.RegularExpressions;
using static BingusLib.FaqHandling.FaqConfig;

namespace BingusLib.FaqHandling
{
    public partial class FaqDict
    {
        private readonly Dictionary<string, FaqEntry> _faqDict = [];

        public FaqDict(FaqConfig faqConfig)
            : this(faqConfig.FaqEntries) { }

        public FaqDict(IEnumerable<FaqConfigEntry> faqConfigEntries)
        {
            foreach (var entry in faqConfigEntries)
            {
                _faqDict[CleanQuery(entry.Title)] = new FaqEntry()
                {
                    Title = entry.Title,
                    Question = entry.Title,
                    Answer = entry.Answer,
                };

                foreach (var keyword in entry.Keywords)
                {
                    _faqDict[CleanQuery(keyword)] = new FaqEntry()
                    {
                        Title = entry.Title,
                        Question = keyword,
                        Answer = entry.Answer,
                    };
                }

                foreach (var question in entry.Questions)
                {
                    _faqDict[CleanQuery(question)] = new FaqEntry()
                    {
                        Title = entry.Title,
                        Question = question,
                        Answer = entry.Answer,
                    };
                }
            }
        }

        private static string CleanQuery(string query) =>
            QueryFilterRegex().Replace(query.ToLowerInvariant(), "");

        public FaqEntry? Search(string query)
        {
            return _faqDict.TryGetValue(CleanQuery(query), out var entry) ? entry : null;
        }

        [GeneratedRegex("[^a-z]", RegexOptions.IgnoreCase)]
        private static partial Regex QueryFilterRegex();
    }
}
