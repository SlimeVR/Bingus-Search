using System.Text.Json;

namespace BingusLib.SentenceEncoding
{
    public class ApiSentenceEncoder : SentenceEncoder
    {
        public readonly HttpClient HttpClient;
        public readonly Uri ApiUri;
        public ApiSentenceEncoder(HttpClient httpClient, Uri apiUri, int embeddingDimension) : base(embeddingDimension)
        {
            HttpClient = httpClient;
            ApiUri = apiUri;
        }

        protected override float[] InternalComputeEmbedding(string input, float[] vectorBuffer)
        {
            using var httpContent = new StringContent(input);
            using var response = HttpClient.PostAsync(ApiUri, httpContent).GetAwaiter().GetResult().EnsureSuccessStatusCode();

            var responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var responseObject = JsonSerializer.Deserialize<ApiSentenceEncoderResponse>(responseString) ?? throw new SentenceEncodeException("Failed to deserialize API response JSON, deserialized object is null.");
            return responseObject.Embedding;
        }
    }
}
