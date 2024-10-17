using System.Net.Http.Json;

namespace BingusLib.SentenceEncoding.Api
{
    public class ApiSentenceEncoder : SentenceEncoder
    {
        public readonly HttpClient HttpClient;
        public readonly Uri BaseApiUri;
        public readonly Uri DimensionsApiUri;
        public readonly Uri EncodeApiUri;

        public ApiSentenceEncoder(HttpClient httpClient, Uri baseApiUri)
        {
            HttpClient = httpClient;
            BaseApiUri = baseApiUri;

            DimensionsApiUri = new Uri(baseApiUri, "/dimensions/");
            EncodeApiUri = new Uri(baseApiUri, "/encode/");

            // Automatically set dimensions from API
            EmbeddingDimension = RequestDimensions().Result;
        }

        private static async Task<T> HandleResponse<T>(
            HttpResponseMessage response,
            CancellationToken ct = default
        )
        {
            return await response.Content.ReadFromJsonAsync<T>(ct)
                ?? throw new SentenceEncodeException(
                    "Failed to deserialize API response JSON, deserialized object is null."
                );
        }

        private async Task<T> PostRequest<T>(
            Uri uri,
            HttpContent? httpContent = null,
            CancellationToken ct = default
        )
        {
            using var response = await HttpClient.PostAsync(uri, httpContent, ct);
            response.EnsureSuccessStatusCode();
            return await HandleResponse<T>(response, ct);
        }

        private async Task<T> GetRequest<T>(Uri uri, CancellationToken ct = default)
        {
            using var response = await HttpClient.GetAsync(uri, ct);
            response.EnsureSuccessStatusCode();
            return await HandleResponse<T>(response, ct);
        }

        private async Task<int> RequestDimensions(CancellationToken ct = default)
        {
            return (await GetRequest<DimensionsResponse>(DimensionsApiUri, ct)).Dimensions;
        }

        protected override float[] InternalComputeEmbedding(string input, float[] vectorBuffer)
        {
            using var httpContent = JsonContent.Create(new EncodeRequest() { Sentence = input });
            return PostRequest<EncodeResponse>(EncodeApiUri, httpContent).Result.Embedding;
        }
    }
}
