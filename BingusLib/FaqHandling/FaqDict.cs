namespace BingusLib.FaqHandling
{
    public class FaqDict
    {
        private readonly Dictionary<string, FaqEntry> _faqDict = [];

        public FaqDict(FaqConfig faqConfig)
            : this(faqConfig.QaEntryEnumerator()) { }

        public FaqDict(IEnumerable<(string title, string question, string answer)> tqaMapping)
        {
            foreach (var (title, question, answer) in tqaMapping)
            {
                _faqDict[CleanQuery(question)] = new FaqEntry()
                {
                    Title = title,
                    Question = question,
                    Answer = answer,
                };
            }
        }

        private static string CleanQuery(string query) => query.Trim().ToLowerInvariant();

        public FaqEntry? Search(string query)
        {
            return _faqDict.TryGetValue(CleanQuery(query), out var entry) ? entry : null;
        }
    }
}
