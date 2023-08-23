using System.Text.Json.Serialization;

namespace BingusLib.SentenceEncoding
{
    public record ApiSentenceEncoderResponse
    {
        [JsonPropertyName("embedding")] public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
