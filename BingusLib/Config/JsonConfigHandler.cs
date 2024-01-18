using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BingusLib.Config
{
    public class JsonConfigHandler<T>
    {
        public readonly JsonSerializerOptions Options = new() { WriteIndented = true };
        public readonly string ConfigFilePath;

        public JsonConfigHandler(string configFile)
        {
            ConfigFilePath = Path.GetFullPath(configFile);
        }

        public T? LoadConfig(string file)
        {
            using var stream = new FileStream(file, FileMode.Open);
            return JsonSerializer.Deserialize<T>(stream, Options);
        }

        public void WriteConfigUnsafe(string file, T config)
        {
            using var stream = new FileStream(file, FileMode.OpenOrCreate);
            JsonSerializer.Serialize(stream, config, Options);
        }

        public static void AtomicFileOp(
            string file,
            Action<string> operation,
            bool overwrite = false
        )
        {
            ArgumentException.ThrowIfNullOrEmpty(file);

            if (!overwrite && File.Exists(file))
                throw new IOException(
                    $"File \"{file}\" exists and \"{nameof(overwrite)}\" is set to false."
                );

            var tempFile = Path.GetTempFileName();
            operation(tempFile);
            File.Move(tempFile, file, overwrite);
        }

        public void WriteConfig(string file, T config)
        {
            AtomicFileOp(file, tempFile => WriteConfigUnsafe(tempFile, config), overwrite: true);
        }

        /// <summary>
        /// Initializes the config from the config file path or uses the default config if the file does not exist.
        /// </summary>
        /// <param name="defaultConfig">The default config to use, this value is returned if it's used.</param>
        /// <param name="logger">The logger to output to.</param>
        /// <returns>The config being used.</returns>
        public T InitializeConfig(T defaultConfig, ILogger<T>? logger = null)
        {
            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    return LoadConfig(ConfigFilePath)
                        ?? throw new JsonConfigException(
                            $"Unable to load the config file at \"{ConfigFilePath}\"."
                        );
                }
                catch (Exception e)
                {
                    logger?.LogError(
                        e,
                        "Unable to load the config file at \"{ConfigFilePath}\", backing up the current config...",
                        ConfigFilePath
                    );

                    var backupFile = $"{ConfigFilePath}.bak";
                    try
                    {
                        AtomicFileOp(
                            backupFile,
                            tempFile => File.Copy(ConfigFilePath, tempFile, overwrite: true),
                            overwrite: true
                        );
                    }
                    catch (Exception e2)
                    {
                        throw new AggregateException(
                            new JsonConfigException(
                                $"Unable to back up the config file at \"{ConfigFilePath}\" to \"{backupFile}\".",
                                e2
                            ),
                            e
                        );
                    }

                    logger?.LogInformation(
                        "Backed up the current config file to \"{backupFile}\".",
                        backupFile
                    );

                    // We shouldn't continue past this point as the config is required
                    throw new JsonConfigException(
                        $"Unable to load the config file at \"{ConfigFilePath}\"."
                    );
                }
            }

            logger?.LogInformation(
                "Generating a default config file at {ConfigFilePath}...",
                ConfigFilePath
            );
            WriteConfig(ConfigFilePath, defaultConfig);
            logger?.LogInformation(
                "Generated a default config file at {ConfigFilePath}.",
                ConfigFilePath
            );

            return defaultConfig;
        }
    }
}
