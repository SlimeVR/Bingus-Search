using System.Text.Json.Serialization;

namespace BingusLib.SentenceEncoding.Api
{
    public record DimensionsResponse
    {
        [JsonPropertyName("dimensions")]
        public int Dimensions { get; set; }
    }
}
