using System.Text.Json.Serialization;

namespace BingusLib.Config
{
    public record BingusConfig
    {
        public static readonly BingusConfig Default = new();
        [JsonPropertyName("model_path")] public string ModelPath { get; set; } = "";
    }
}
