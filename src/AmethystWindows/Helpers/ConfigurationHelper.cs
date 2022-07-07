using System;
using System.IO;
using System.Reflection;
using AmethystWindows.Models.Configuration;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace AmethystWindows
{
    /// <summary>
    /// Configuration helper responsible for creating and returning configuration object
    /// </summary>
    public class ConfigurationHelper
    {
        /// <summary>
        /// Create dir/file path and then add it to the configuration
        /// </summary>
        public static IConfigurationRoot? GetRoot()
        {
            const string settingsFile = "appsettings.json";
            var basePath = PrepareBasePath(settingsFile);

            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile(settingsFile, optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.dev.json", optional: true, reloadOnChange: true);

            var configuration = builder.Build();

            return configuration;
        }

        private static string PrepareBasePath(string settingsFile)
        {
            var basePath = ConfigurationHelper.GetLocalAppDataPath();
            var filePath = Path.Combine(basePath, settingsFile);
            var dirPath = Path.GetDirectoryName(filePath) ?? throw new ArgumentNullException();

            Directory.CreateDirectory(dirPath);
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, GetDefaultContents());
            }

            return basePath;
        }

        private static string GetDefaultContents()
        {
            var appSettings = new AppSettings();
            var jsonContent = JsonConvert.SerializeObject(appSettings, Formatting.Indented);
            return jsonContent;
        }

        private static string GetLocalAppDataPath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // TODO confirm (based on old SettingsManager) why get this from the assembly instead of just a simple constant
            var assembly = Assembly.GetEntryAssembly();
            var assemblyName = assembly?.GetName().Name ?? "AmethystWindows";

            var basePath = Path.Combine(appData, assemblyName);

            return basePath;
        }
    }
}