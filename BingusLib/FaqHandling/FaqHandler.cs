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
            public int Id { get; set; } = -1;
            public string Question { get; set; } = "";
            public string Answer { get; set; } = "";
            public Vector<float>? Vector { get; set; }
        }

        private int _idCounter = 0;

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<FaqHandler> _logger;

        private readonly IEmbeddingCache? _embeddingCache;

        private readonly UniversalSentenceEncoder _useHandler;
        private readonly HnswHandler _hnswHandler = new();

        private readonly Dictionary<int, FaqEntry> _idMapping = new();

        public FaqHandler(ILoggerFactory loggerFactory, string modelPath, IEmbeddingCache? embeddingCache = null)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<FaqHandler>();

            _embeddingCache = embeddingCache;

            _useHandler = new(_loggerFactory.CreateLogger<UniversalSentenceEncoder>(), modelPath);
        }

        private int GetId()
        {
            return _idCounter++;
        }

        public void AddItems(IEnumerable<(string, string)> questionAnswerMappings)
        {
            var hnswItems = new List<LazyKeyItem<int, float[]>>();
            foreach (var (question, answer) in questionAnswerMappings)
            {
                var id = GetId();
                Vector<float>? embedding = null;

                if (_embeddingCache != null) embedding = _embeddingCache?.Get(question);

                if (embedding == null)
                {
                    embedding = _useHandler.ComputeEmbeddingVector(question);
                    _embeddingCache?.Add(question, embedding);
                }

                _idMapping[id] = new FaqEntry()
                {
                    Id = id,
                    Question = question,
                    Answer = answer,
                    Vector = embedding
                };
                hnswItems.Add(new LazyKeyItem<int, float[]>(id, embedding.AsArray));
            }

            _hnswHandler.AddItems(hnswItems);
        }

        public IList<SmallWorld<ILazyItem<float[]>, float>.KNNSearchResult> Search(string query, int numResults)
        {
            var vector = _embeddingCache?.GetRaw(query) ?? _useHandler.ComputeEmbedding(query);
            return _hnswHandler.SearchItems(vector, numResults);
        }

        public FaqEntry GetEntry(int id)
        {
            return _idMapping[id];
        }
    }
}
