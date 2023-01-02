using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BingusLib.FaqHandling
{
    public static class FaqConfigUtils
    {
        public static readonly string FaqConfigFile = Path.GetFullPath("faq_config.json");

        private static readonly JsonSerializerOptions options = new() { WriteIndented = true };

        public static FaqConfig? LoadConfig(string file)
        {
            using var stream = new FileStream(file, FileMode.Open);
            return JsonSerializer.Deserialize<FaqConfig>(stream, options);
        }

        public static void WriteConfigUnsafe(string file, FaqConfig config)
        {
            using var stream = new FileStream(file, FileMode.OpenOrCreate);
            JsonSerializer.Serialize(stream, config);
        }

        public static void WriteConfig(string file, FaqConfig config)
        {
            ArgumentException.ThrowIfNullOrEmpty(file);

            var tempFile = Path.GetTempFileName();
            WriteConfigUnsafe(tempFile, config);
            File.Move(tempFile, file, overwrite: true);
        }

        public static FaqConfig InitializeConfig(ILogger<FaqConfig>? logger = null)
        {
            if (File.Exists(FaqConfigFile))
            {
                try
                {
                    return LoadConfig(FaqConfigFile) ?? throw new NullReferenceException($"Unable to load config at \"{FaqConfigFile}\".");
                }
                catch (Exception e)
                {
                    logger?.LogError(e, "Unable to load the config file at \"{FaqConfigFile}\", backing up the current config...", FaqConfigFile);

                    var backupFile = $"{FaqConfigFile}.bak";
                    try
                    {
                        File.Move(FaqConfigFile, backupFile, overwrite: true);
                    }
                    catch
                    {
                        throw new IOException($"Unable to back up the config file at \"{FaqConfigFile}\" to \"{backupFile}\".", e);
                    }

                    logger?.LogInformation("Backed up the erroneous config file to {backupFile}, generating a new config...", backupFile);
                }
            }

            logger?.LogInformation("Generating a default config file at {FaqConfigFile}...", FaqConfigFile);

            var faqConfig = new FaqConfig();
            WriteConfig(FaqConfigFile, faqConfig);

            logger?.LogInformation("Generated a default config file at {FaqConfigFile}.", FaqConfigFile);

            return faqConfig;
        }
    }
}
