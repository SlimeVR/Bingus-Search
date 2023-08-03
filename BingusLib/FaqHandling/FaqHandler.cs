using BingusLib.HNSW;
using BingusLib.SentenceEncoding;
using HNSW.Net;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;

namespace BingusLib.FaqHandling
{
    public record FaqHandler
    {
        public record FaqEntry
        {
            public string Title { get; set; } = "";
            public string Question { get; set; } = "";
            public string Answer { get; set; } = "";
            public Vector<float>? Vector { get; set; }
        }

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<FaqHandler> _logger;

        private readonly IEmbeddingStore? _embeddingStore;
        private readonly IEmbeddingCache? _embeddingCache;

        private readonly UniversalSentenceEncoder _useHandler;
        private readonly HnswHandler _hnswHandler = new();

        public FaqHandler(ILoggerFactory loggerFactory, string modelPath, IEmbeddingStore? embeddingStore = null, IEmbeddingCache? embeddingCache = null)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<FaqHandler>();

            _embeddingStore = embeddingStore;
            _embeddingCache = embeddingCache;

            _useHandler = new(_loggerFactory.CreateLogger<UniversalSentenceEncoder>(), modelPath);
        }

        public void AddItems(IEnumerable<(string, string, string)> questionAnswerMappings)
        {
            var hnswItems = new List<LazyKeyItem<FaqEntry, float[]>>();
            foreach (var (title, question, answer) in questionAnswerMappings)
            {
                Vector<float>? embedding = _embeddingCache?.Get(question) ?? _embeddingStore?.Get(question);

                if (embedding == null)
                {
                    embedding = _useHandler.ComputeEmbeddingVector(question);
                    _embeddingStore?.Add(question, embedding);
                }

                var faqEntry = new FaqEntry()
                {
                    Title = title,
                    Question = question,
                    Answer = answer,
                    Vector = embedding
                };
                hnswItems.Add(new LazyKeyItem<FaqEntry, float[]>(faqEntry, embedding.AsArray));
            }

            _hnswHandler.AddItems(hnswItems);
        }

        public IList<SmallWorld<ILazyItem<float[]>, float>.KNNSearchResult> Search(string query, int numResults)
        {
            var vector = _embeddingCache?.GetRaw(query) ?? _embeddingStore?.GetRaw(query) ?? _useHandler.ComputeEmbedding(query);
            _embeddingCache?.Add(query, vector);
            return _hnswHandler.SearchItems(vector, numResults);
        }
    }
}
