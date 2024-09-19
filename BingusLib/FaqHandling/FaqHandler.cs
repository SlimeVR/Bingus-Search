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

        private readonly ILogger<FaqHandler>? _logger;

        private readonly IEmbeddingStore? _embeddingStore;
        private readonly IEmbeddingCache? _embeddingCache;

        private readonly SentenceEncoder _sentenceEncoder;
        private readonly HnswHandler _hnswHandler;

        public FaqHandler(
            SentenceEncoder sentenceEncoder,
            IEmbeddingStore? embeddingStore = null,
            IEmbeddingCache? embeddingCache = null,
            ILogger<FaqHandler>? logger = null,
            Func<float[], float[], float>? distanceFunction = null,
            SmallWorld<ILazyItem<float[]>, float>.Parameters? parameters = null
        )
        {
            _sentenceEncoder = sentenceEncoder;
            _embeddingStore = embeddingStore;
            _embeddingCache = embeddingCache;
            _logger = logger;
            _hnswHandler = new(distanceFunction, parameters);
        }

        public void AddItems(IEnumerable<(string title, string question, string answer)> tqaMapping)
        {
            var hnswItems = new List<LazyKeyItem<FaqEntry, float[]>>();
            foreach (var (title, question, answer) in tqaMapping)
            {
                Vector<float>? embedding =
                    _embeddingCache?.Get(question) ?? _embeddingStore?.Get(question);

                if (embedding == null)
                {
                    embedding = _sentenceEncoder.ComputeEmbeddingVector(question);
                    _embeddingStore?.Add(question, embedding);
                }

                var faqEntry = new FaqEntry()
                {
                    Title = title,
                    Question = question,
                    Answer = answer,
                    Vector = embedding,
                };
                hnswItems.Add(new LazyKeyItem<FaqEntry, float[]>(faqEntry, embedding.AsArray));
            }

            _hnswHandler.AddItems(hnswItems);
        }

        public IList<SmallWorld<ILazyItem<float[]>, float>.KNNSearchResult> Search(
            string query,
            int numResults
        )
        {
            var vector =
                _embeddingCache?.GetRaw(query)
                ?? _embeddingStore?.GetRaw(query)
                ?? _sentenceEncoder.ComputeEmbedding(query);
            _embeddingCache?.Add(query, vector);
            return _hnswHandler.SearchItems(vector, numResults);
        }
    }
}
