using Newtonsoft.Json;

namespace BingusLib.FaqHandling
{
    public record FaqConfig
    {
        public record FaqConfigEntry
        {
            [JsonProperty(PropertyName = "answer")]
            public string Answer { get; set; } = "";

            [JsonProperty(PropertyName = "matched_questions")]
            public List<string> Questions { get; set; } = new();

            [JsonProperty(PropertyName = "average_questions")]
            public bool AverageQuestions { get; set; } = false;

            public FaqConfigEntry()
            {
            }

            public FaqConfigEntry(string answer, bool averageQuestions = false) : this()
            {
                Answer = answer;
                AverageQuestions = averageQuestions;
            }

            public FaqConfigEntry(string answer, IEnumerable<string> questions, bool averageQuestions = false) : this(answer, averageQuestions)
            {
                Questions.AddRange(questions);
            }
        }

        [JsonProperty(PropertyName = "model_path")]
        public string ModelPath { get; set; } = "";

        [JsonProperty(PropertyName = "faqs")]
        public List<FaqConfigEntry> FaqEntries { get; set; } = new();

        public FaqConfigEntry? GetAnswerEntry(string answer)
        {
            foreach (var entry in FaqEntries)
            {
                if (entry.Answer == answer) return entry;
            }

            return null;
        }

        public IEnumerable<(string, string)> QAEntryEnumerator()
        {
            foreach (var entry in FaqEntries)
            {
                foreach (var question in entry.Questions)
                {
                    yield return (question, entry.Answer);
                }
            }
        }
    }
}
