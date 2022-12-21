using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FaqBot.FaqHandling
{
    public static class FaqConfigUtils
    {
        public static readonly string FaqConfigFile = Path.GetFullPath("faq_config.json");

        private static readonly JsonSerializer JsonSerializer = new()
        {
            Formatting = Formatting.Indented
        };

        public static FaqConfig? LoadConfig(string file)
        {
            using var streamReader = File.OpenText(file);
            using var reader = new JsonTextReader(streamReader);
            return JsonSerializer.Deserialize<FaqConfig>(reader);
        }

        public static void WriteConfig(string file, FaqConfig config)
        {
            using var streamWriter = File.CreateText(file);
            JsonSerializer.Serialize(streamWriter, config);
        }

        public static FaqConfig InitializeConfig(ILogger<FaqConfig>? logger = null)
        {
            if (File.Exists(FaqConfigFile))
            {
                try
                {
                    return LoadConfig(FaqConfigFile) ?? throw new NullReferenceException($"Unable to load config \"{FaqConfigFile}\"...");
                }
                catch (Exception e)
                {
                    logger?.LogError(e, "Unable to load config");
                }
            }

            var faqConfig = new FaqConfig();
            WriteConfig(FaqConfigFile, faqConfig);
            return faqConfig;
        }
    }
}
