using System.Text.Json.Serialization;

namespace BingusLib.Config
{
    public record BingusConfig
    {
        public static readonly BingusConfig Default = new();

        [JsonPropertyName("encoder_type")]
        public string EncoderType { get; set; } = "";

        [JsonPropertyName("use_model_path")]
        public string UseModelPath { get; set; } = "";

        [JsonPropertyName("api_uri")]
        public string ApiUri { get; set; } = "";
    }
}
