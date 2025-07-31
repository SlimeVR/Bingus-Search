using System.Text.Json.Serialization;

namespace BingusLib.FaqHandling
{
    public record FaqConfig
    {
        public static readonly FaqConfig Default = new();

        public record FaqConfigEntry
        {
            [JsonPropertyName("title")]
            public string Title { get; set; } = "";

            [JsonPropertyName("answer")]
            public string Answer { get; set; } = "";

            [JsonPropertyName("keywords")]
            public string[] Keywords { get; set; } = [];

            [JsonPropertyName("questions")]
            public string[] Questions { get; set; } = [];

            [JsonPropertyName("exact_only")]
            public bool ExactOnly { get; set; } = false;

            public FaqConfigEntry() { }

            public FaqConfigEntry(string answer)
                : this()
            {
                Answer = answer;
            }

            public FaqConfigEntry(
                string answer,
                IEnumerable<string> keywords,
                IEnumerable<string> questions
            )
                : this(answer)
            {
                Keywords = keywords.ToArray();
                Questions = questions.ToArray();
            }
        }

        [JsonPropertyName("faqs")]
        public FaqConfigEntry[] FaqEntries { get; set; } = [];

        public FaqConfigEntry? GetAnswerEntry(string answer)
        {
            foreach (var entry in FaqEntries)
            {
                if (entry.Answer == answer)
                    return entry;
            }

            return null;
        }

        public IEnumerable<(string title, string question, string answer)> QaEntryEnumerator(
            bool noExactOnly = true
        )
        {
            foreach (var entry in FaqEntries)
            {
                if (noExactOnly && entry.ExactOnly)
                    continue;

                foreach (var keyword in entry.Keywords)
                {
                    yield return (entry.Title, keyword, entry.Answer);
                }

                foreach (var question in entry.Questions)
                {
                    yield return (entry.Title, question, entry.Answer);
                }
            }
        }

        public IEnumerable<(string title, string question, string answer)> AnswerEntryEnumerator(
            bool noExactOnly = true
        )
        {
            foreach (var entry in FaqEntries)
            {
                if (noExactOnly && entry.ExactOnly)
                    continue;

                // If we want to use an example question:
                // entry.Questions.FirstOrDefault(entry.Title)
                yield return (entry.Title, "", entry.Answer);
            }
        }
    }
}
