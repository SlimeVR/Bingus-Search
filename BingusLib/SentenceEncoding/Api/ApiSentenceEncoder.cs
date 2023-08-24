using System.Net.Http.Json;
using System.Text.Json;

namespace BingusLib.SentenceEncoding.Api
{
    public class ApiSentenceEncoder : SentenceEncoder
    {
        public readonly HttpClient HttpClient;
        public readonly Uri BaseApiUri;
        public readonly Uri DimensionsApiUri;
        public readonly Uri EncodeApiUri;

        public ApiSentenceEncoder(HttpClient httpClient, Uri baseApiUri) : base(-1)
        {
            HttpClient = httpClient;
            BaseApiUri = baseApiUri;

            DimensionsApiUri = new Uri(baseApiUri, "/dimensions/");
            EncodeApiUri = new Uri(baseApiUri, "/encode/");

            // Automatically set dimensions from API
            EmbeddingDimension = RequestDimensions();
        }

        private T SendRequest<T>(Uri uri, HttpContent? httpContent = null)
        {
            using var response = HttpClient.PostAsync(uri, httpContent).GetAwaiter().GetResult().EnsureSuccessStatusCode();
            var responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonSerializer.Deserialize<T>(responseString) ?? throw new SentenceEncodeException("Failed to deserialize API response JSON, deserialized object is null.");
        }

        private int RequestDimensions()
        {
            return SendRequest<DimensionsResponse>(DimensionsApiUri).Dimensions;
        }

        protected override float[] InternalComputeEmbedding(string input, float[] vectorBuffer)
        {
            using var httpContent = JsonContent.Create(new EncodeRequest() { Sentence = input });
            return SendRequest<EncodeResponse>(EncodeApiUri, httpContent).Embedding;
        }
    }
}
