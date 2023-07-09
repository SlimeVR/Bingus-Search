using System.Text.Json.Serialization;

namespace BingusLib.Config
{
    public record BingusConfig
    {
        [JsonPropertyName("model_path")] public string ModelPath { get; set; } = "";
    }
}
