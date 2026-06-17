using IISTools.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;

namespace IISTools
{
    public static class Assistant
    {
        /// <summary>
        /// Получение папки с данными плагина плагина
        /// </summary>
        /// <returns></returns>
        public static DirectoryInfo GetPluginDirectory()
        {
            string path = Environment.ExpandEnvironmentVariables("%appdata%");
            var directory = new DirectoryInfo(path);

            path = Path.Combine(directory.FullName, "IISTools");
            return Directory.CreateDirectory(path);
        }

        public static string GetSettingsFile()
        {
            return Path.Combine(GetPluginDirectory().FullName, "settings.json");
        }

        /// <summary> Настройки приложения </summary>
        public static Lazy<IisSettingsModel> Settings = new Lazy<IisSettingsModel>(() =>
        {
            return new IisSettingsModel().Load();
        });

        public static ProcessModel[] GetIISProcess()
        {
            var processes = Process.GetProcesses().Where(x =>
                    String.Equals(x.ProcessName, "w3wp", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(x.ProcessName, "w3wp.exe", StringComparison.OrdinalIgnoreCase))
                .Select(x => GetProcessMdel(x))
                .OrderBy(x => x.Owner)
                .ToArray();

            return processes;
        }

        public static ProcessModel GetProcessMdel(Process process)
        {
            var definition = new ProcessModel();
            definition.Id = process.Id;
            definition.Name = process.ProcessName;

            List<Exception> listException = new List<Exception>();
            try
            {
                string query = "Select * From Win32_Process Where ProcessID = " + process.Id;
                var searcher = new ManagementObjectSearcher(query);
                var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach(var pair in GetNameValues(searcher.Get()))
                {
                    dictionary[pair.Key] = pair.Value;
                }

                definition.Owner = dictionary.ContainsKey("GetOwner")
                    ? dictionary["GetOwner"]
                    : "";

                definition.CommandLine = dictionary.ContainsKey("CommandLine")
                    ? dictionary["CommandLine"]
                    : "";
            }
            catch (Exception ex)
            {
                listException.Add(ex);
            }

            definition.Exceptions = listException.ToArray();
            return definition;
        }

        public static IEnumerable<KeyValuePair<string, string>> GetNameValues(ManagementObjectCollection processList)
        {
            foreach (ManagementObject obj in processList)
            {
                foreach (var property in obj.Properties)
                {
                    if (property.Name != null && property.Value is string)
                    {
                        yield return new KeyValuePair<string, string>(property.Name, property.Value.ToString());
                    }
                }

                string[] argList = new string[] { string.Empty, string.Empty };
                int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                yield return new KeyValuePair<string, string>("GetOwner", argList[1] + "\\" + argList[0]);
            }
        }
    }
}
