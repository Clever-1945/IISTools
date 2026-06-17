using System;
using System.IO;
using System.Text.Json;

namespace IISTools.Models
{
    public class IisSettingsModel
    {
        public IisSettingsPingUrlModel[] PingUrls { set; get; }

        public void Save()
        {
            var options = new JsonSerializerOptions 
            {
                WriteIndented = true 
            };
            string prettyJson = JsonSerializer.Serialize(this, options);
            File.WriteAllText(Assistant.GetSettingsFile(), prettyJson);
        }

        /// <summary>
        /// Загрузить настройки из файла
        /// </summary>
        public IisSettingsModel Load()
        {
            IisSettingsModel instance = null;
            if (File.Exists(Assistant.GetSettingsFile()))
            {
                var text = File.ReadAllText(Assistant.GetSettingsFile());
                try
                {
                    instance = JsonSerializer.Deserialize<IisSettingsModel>(text);
                }
                finally { }
            }

            PingUrls = instance?.PingUrls ?? Array.Empty<IisSettingsPingUrlModel>();
            foreach (var pingUrl in PingUrls)
            { 
                if (pingUrl.Id == Guid.Empty)
                    pingUrl.Id = Guid.NewGuid();
            }

            return this;
        }
    }

    public class IisSettingsPingUrlModel
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
    }
}
