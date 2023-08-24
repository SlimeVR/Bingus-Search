using System.Text.Json.Serialization;

namespace BingusLib.SentenceEncoding
{
    public record ApiSentenceEncoderRequest
    {
        [JsonPropertyName("sentence")] public string Sentence { get; set; } = "";
    }
}
