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

            [JsonPropertyName("matched_questions")]
            public string[] Questions { get; set; } = [];

            public FaqConfigEntry() { }

            public FaqConfigEntry(string answer)
                : this()
            {
                Answer = answer;
            }

            public FaqConfigEntry(string answer, IEnumerable<string> questions)
                : this(answer)
            {
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

        public IEnumerable<(string title, string question, string answer)> QaEntryEnumerator()
        {
            foreach (var entry in FaqEntries)
            {
                foreach (var question in entry.Questions)
                {
                    yield return (entry.Title, question, entry.Answer);
                }
            }
        }

        public IEnumerable<(string title, string question, string answer)> AnswerEntryEnumerator()
        {
            foreach (var entry in FaqEntries)
            {
                // If we want to use an example question:
                // entry.Questions.FirstOrDefault(entry.Title)
                yield return (entry.Title, "", entry.Answer);
            }
        }
    }
}
