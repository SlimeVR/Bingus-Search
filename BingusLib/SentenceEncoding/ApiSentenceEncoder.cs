using System.Net.Http.Json;
using System.Text.Json;

namespace BingusLib.SentenceEncoding
{
    public class ApiSentenceEncoder : SentenceEncoder
    {
        public readonly HttpClient HttpClient;
        public readonly Uri BaseApiUri;
        public readonly Uri EncodeApiUri;
        public ApiSentenceEncoder(HttpClient httpClient, Uri baseApiUri, int embeddingDimension) : base(embeddingDimension)
        {
            HttpClient = httpClient;
            BaseApiUri = baseApiUri;

            EncodeApiUri = new Uri(baseApiUri, "encode");
        }

        protected override float[] InternalComputeEmbedding(string input, float[] vectorBuffer)
        {
            using var httpContent = JsonContent.Create(new ApiSentenceEncoderRequest() { Sentence = input });
            using var response = HttpClient.PostAsync(EncodeApiUri, httpContent).GetAwaiter().GetResult().EnsureSuccessStatusCode();

            var responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var responseObject = JsonSerializer.Deserialize<ApiSentenceEncoderResponse>(responseString) ?? throw new SentenceEncodeException("Failed to deserialize API response JSON, deserialized object is null.");
            return responseObject.Embedding;
        }
    }
}
