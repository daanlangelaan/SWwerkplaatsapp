using System;
using System.IO;
using System.Reflection;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class HealthData
    {
        public bool Ok { get; set; }
        public string Version { get; set; }
        public string StartedAt { get; set; }
        public string RootFolder { get; set; }
        public bool RootFolderExists { get; set; }
        public string Prefix { get; set; }
    }

    public sealed class HealthApplicationService
    {
        private readonly DateTime startedAt;

        public HealthApplicationService(DateTime startedAt)
        {
            this.startedAt = startedAt;
        }

        public HealthData GetHealth(string rootFolder, string prefix)
        {
            return new HealthData
            {
                Ok = true,
                Version = Version(),
                StartedAt = startedAt.ToString("s"),
                RootFolder = rootFolder,
                RootFolderExists = !string.IsNullOrWhiteSpace(rootFolder) && Directory.Exists(rootFolder),
                Prefix = prefix
            };
        }

        private static string Version()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName();
            return assembly.Version == null ? "" : assembly.Version.ToString();
        }
    }
}
