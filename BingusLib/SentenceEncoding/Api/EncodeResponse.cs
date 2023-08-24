using System.Text.Json.Serialization;

namespace BingusLib.SentenceEncoding.Api
{
    public record EncodeResponse
    {
        [JsonPropertyName("embedding")] public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
