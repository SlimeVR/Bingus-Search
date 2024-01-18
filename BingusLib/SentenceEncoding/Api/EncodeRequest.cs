using System.Text.Json.Serialization;

namespace BingusLib.SentenceEncoding.Api
{
    public record EncodeRequest
    {
        [JsonPropertyName("sentence")]
        public string Sentence { get; set; } = "";
    }
}
