using BingusLib.HNSW;
using BingusLib.SentenceEncoding;
using HNSW.Net;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;

namespace BingusLib.FaqHandling
{
    public class FaqHandler
    {
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
            IProvideRandomValues? randomProvider = null,
            SmallWorld<ILazyItem<float[]>, float>.Parameters? parameters = null
        )
        {
            _sentenceEncoder = sentenceEncoder;
            _embeddingStore = embeddingStore;
            _embeddingCache = embeddingCache;
            _logger = logger;
            _hnswHandler = new(distanceFunction, randomProvider, parameters);
        }

        public void AddItems(FaqConfig faq, bool useQ2A = true)
        {
            AddItems(useQ2A ? faq.AnswerEntryEnumerator() : faq.QaEntryEnumerator(), useQ2A);
        }

        public void AddItems(
            IEnumerable<(string title, string question, string answer)> tqaMapping,
            bool useQ2A = true
        )
        {
            var hnswItems = new List<LazyKeyItem<FaqEntry, float[]>>();
            foreach (var (title, question, answer) in tqaMapping)
            {
                var query = useQ2A ? answer : question;
                Vector<float>? embedding =
                    _embeddingCache?.Get(query) ?? _embeddingStore?.Get(query);

                if (embedding == null)
                {
                    embedding = _sentenceEncoder.ComputeEmbeddingVector(query);
                    _embeddingStore?.Add(query, embedding);
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
