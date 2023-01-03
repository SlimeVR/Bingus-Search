using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BingusLib.FaqHandling
{
    public static class FaqConfigUtils
    {
        public static readonly string FaqConfigFile = Path.GetFullPath("faq_config.json");

        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public static FaqConfig? LoadConfig(string file)
        {
            using var stream = new FileStream(file, FileMode.Open);
            return JsonSerializer.Deserialize<FaqConfig>(stream, Options);
        }

        public static void WriteConfigUnsafe(string file, FaqConfig config)
        {
            using var stream = new FileStream(file, FileMode.OpenOrCreate);
            JsonSerializer.Serialize(stream, config);
        }

        public static void AtomicFileOp(string file, Action<string> operation, bool overwrite = false)
        {
            ArgumentException.ThrowIfNullOrEmpty(file);

            if (!overwrite && File.Exists(file))
                throw new IOException($"File \"{file}\" exists and \"{nameof(overwrite)}\" is set to false.");

            var tempFile = Path.GetTempFileName();
            operation(tempFile);
            File.Move(tempFile, file, overwrite);
        }

        public static void WriteConfig(string file, FaqConfig config)
        {
            AtomicFileOp(file, tempFile => WriteConfigUnsafe(tempFile, config), overwrite: true);
        }

        public static FaqConfig InitializeConfig(ILogger<FaqConfig>? logger = null)
        {
            if (File.Exists(FaqConfigFile))
            {
                try
                {
                    return LoadConfig(FaqConfigFile) ??
                           throw new FaqConfigException($"Unable to load the config file at \"{FaqConfigFile}\".");
                }
                catch (Exception e)
                {
                    logger?.LogError(e,
                        "Unable to load the config file at \"{FaqConfigFile}\", backing up the current config...",
                        FaqConfigFile);

                    var backupFile = $"{FaqConfigFile}.bak";
                    try
                    {
                        AtomicFileOp(backupFile, tempFile => File.Copy(FaqConfigFile, tempFile, overwrite: true),
                            overwrite: true);
                    }
                    catch (Exception e2)
                    {
                        throw new AggregateException(
                            new FaqConfigException(
                                $"Unable to back up the config file at \"{FaqConfigFile}\" to \"{backupFile}\".", e2),
                            e);
                    }

                    logger?.LogInformation("Backed up the current config file to \"{backupFile}\".", backupFile);

                    // We shouldn't continue past this point as the config is required
                    throw new FaqConfigException($"Unable to load the config file at \"{FaqConfigFile}\".");
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
