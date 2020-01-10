using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Microsoft.CognitiveServices.Inventory
{
    public class AppSettings
    {
        private static AppSettings _instance;
        private JObject _settingsJson;

        private const string Namespace = "Microsoft.CognitiveServices.Inventory";
        private const string FileName = "appSettings.json";

        private AppSettings()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{Namespace}.{FileName}");
            using (var reader = new StreamReader(stream))
            {
                var json = reader.ReadToEnd();
                _settingsJson = JObject.Parse(json);
            }
        }

        public static AppSettings Settings
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AppSettings();
                }

                return _instance;
            }
        }

        public string GetValue(string settingName)
        {
            return GetValue<string>(settingName);
        }

        public T GetValue<T>(string settingName, T defaultValue = default)
        {
            try
            {
                var path = settingName.Split(':');

                JToken node = _settingsJson[path[0]];
                for (int index = 1; index < path.Length; index++)
                {
                    node = node[path[index]];
                }

                return node.Value<T>();
            }
            catch (Exception)
            {
                Debug.WriteLine($"Unable to retrieve setting '{settingName}'");
                return defaultValue;
            }
        }
    }
}
