using System.Text.Json;
using BingusLib.FaqHandling;
using Microsoft.Extensions.Logging;

namespace BingusLib.Config
{
    public static class BingusConfigUtils
    {
        public static readonly string BingusConfigFile = Path.GetFullPath("bingus_config.json");

        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public static BingusConfig? LoadConfig(string file)
        {
            using var stream = new FileStream(file, FileMode.Open);
            return JsonSerializer.Deserialize<BingusConfig>(stream, Options);
        }

        public static void WriteConfigUnsafe(string file, BingusConfig config)
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

        public static void WriteConfig(string file, BingusConfig config)
        {
            AtomicFileOp(file, tempFile => WriteConfigUnsafe(tempFile, config), overwrite: true);
        }

        public static BingusConfig InitializeConfig(ILogger<BingusConfig>? logger = null)
        {
            if (File.Exists(BingusConfigFile))
            {
                try
                {
                    return LoadConfig(BingusConfigFile) ??
                           throw new FaqConfigException($"Unable to load the config file at \"{BingusConfigFile}\".");
                }
                catch (Exception e)
                {
                    logger?.LogError(e,
                        "Unable to load the config file at \"{BingusConfigFile}\", backing up the current config...",
                        BingusConfigFile);

                    var backupFile = $"{BingusConfigFile}.bak";
                    try
                    {
                        AtomicFileOp(backupFile, tempFile => File.Copy(BingusConfigFile, tempFile, overwrite: true),
                            overwrite: true);
                    }
                    catch (Exception e2)
                    {
                        throw new AggregateException(
                            new FaqConfigException(
                                $"Unable to back up the config file at \"{BingusConfigFile}\" to \"{backupFile}\".", e2),
                            e);
                    }

                    logger?.LogInformation("Backed up the current config file to \"{backupFile}\".", backupFile);

                    // We shouldn't continue past this point as the config is required
                    throw new FaqConfigException($"Unable to load the config file at \"{BingusConfigFile}\".");
                }
            }

            logger?.LogInformation("Generating a default config file at {BingusConfigFile}...", BingusConfigFile);

            var faqConfig = new BingusConfig();
            WriteConfig(BingusConfigFile, faqConfig);

            logger?.LogInformation("Generated a default config file at {BingusConfigFile}.", BingusConfigFile);

            return faqConfig;
        }
    }
}
