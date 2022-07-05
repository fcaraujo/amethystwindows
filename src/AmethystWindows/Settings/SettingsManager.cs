using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;

namespace AmethystWindows.Settings
{
    public abstract class SettingsManager<T> where T : SettingsManager<T>, new()
    {
        private static readonly string path = GetLocalFilePath($"{typeof(T).Name}.json");

        public static T? Instance { get; private set; }

        private static string GetLocalFilePath(string fileName)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var assembly = Assembly.GetEntryAssembly() ?? throw new ArgumentNullException(nameof(Assembly.GetEntryAssembly));
            var assemblyName = assembly.GetName().Name ?? "AmethystWindows";

            return Path.Combine(appData, assemblyName, fileName);
        }

        public static void Load()
        {
            if (File.Exists(path))
                Instance = JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            else
                Instance = new T();
        }

        public static void Save()
        {
            var dir = Path.GetDirectoryName(path) ?? throw new ArgumentNullException();
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var contents = JsonConvert.SerializeObject(Instance, Formatting.Indented);
            File.WriteAllText(path, contents);
        }
    }
}
