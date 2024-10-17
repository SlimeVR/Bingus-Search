using System.Text.Json.Serialization;
using BingusLib.SentenceEncoding;
using BingusLib.SentenceEncoding.Api;
using LLama.Common;
using LLama.Native;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        [JsonPropertyName("llama_model_path")]
        public string LlamaModelPath { get; set; } = "";

        [JsonPropertyName("hnsw_seed")]
        public int HnswSeed { get; set; } = 42;

        [JsonPropertyName("use_q2a")]
        public bool UseQ2A { get; set; } = false;

        public SentenceEncoder GetSentenceEncoder(IServiceProvider serviceProvider)
        {
            // Select and set up the sentence encoder based on the config
            switch (EncoderType.ToLower())
            {
                case "use":
                    var modelPath = Path.Combine(Environment.CurrentDirectory, UseModelPath);
                    return new UniversalSentenceEncoder(
                        modelPath,
                        serviceProvider.GetService<ILogger<UniversalSentenceEncoder>>()
                    );

                case "api":
                    return new ApiSentenceEncoder(
                        serviceProvider.GetRequiredService<HttpClient>(),
                        new Uri(ApiUri)
                    );

                case "llama":
                    return new LlamaSentenceEncoder(
                        new ModelParams(LlamaModelPath) { PoolingType = LLamaPoolingType.Mean }
                    );

                default:
                    throw new JsonConfigException("No valid sentence encoder type was selected.");
            }
        }
    }
}
