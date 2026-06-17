using System;
using System.IO;
using System.Web.Script.Serialization;

namespace SWWerkplaats.Configurator.Domain
{
    public sealed class AppSettings
    {
        public double NestStockLengthMm { get; set; }
        public double NestStockWidthMm { get; set; }
        public double NestSpacingMm { get; set; }
        public double NestMarginMm { get; set; }

        public static AppSettings Load()
        {
            var fallback = Defaults();
            var path = SettingsPath(false);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return fallback;

            try
            {
                var settings = new JavaScriptSerializer().Deserialize<AppSettings>(File.ReadAllText(path));
                if (settings == null) return fallback;

                return new AppSettings
                {
                    NestStockLengthMm = Positive(settings.NestStockLengthMm, fallback.NestStockLengthMm),
                    NestStockWidthMm = Positive(settings.NestStockWidthMm, fallback.NestStockWidthMm),
                    NestSpacingMm = NonNegative(settings.NestSpacingMm, fallback.NestSpacingMm),
                    NestMarginMm = NonNegative(settings.NestMarginMm, fallback.NestMarginMm)
                };
            }
            catch
            {
                return fallback;
            }
        }

        public void Save()
        {
            var path = SettingsPath(true);
            if (string.IsNullOrEmpty(path)) return;

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, new JavaScriptSerializer().Serialize(this));
        }

        private static AppSettings Defaults()
        {
            return new AppSettings
            {
                NestStockLengthMm = 3020,
                NestStockWidthMm = 1520,
                NestSpacingMm = 15,
                NestMarginMm = 15
            };
        }

        private static string SettingsPath(bool create)
        {
            var existingConfig = FindConfigFolder(AppDomain.CurrentDomain.BaseDirectory);
            if (existingConfig != null) return Path.Combine(existingConfig, "app-settings.json");

            existingConfig = FindConfigFolder(Environment.CurrentDirectory);
            if (existingConfig != null) return Path.Combine(existingConfig, "app-settings.json");

            if (!create) return null;
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.GetFullPath(Path.Combine(baseDir, "..", "config", "app-settings.json"));
        }

        private static string FindConfigFolder(string startFolder)
        {
            if (string.IsNullOrEmpty(startFolder)) return null;

            var folder = Path.GetFullPath(startFolder);
            for (var i = 0; i < 6 && !string.IsNullOrEmpty(folder); i++)
            {
                var candidate = Path.Combine(folder, "config");
                if (Directory.Exists(candidate)) return candidate;

                var parent = Directory.GetParent(folder);
                if (parent == null) break;
                folder = parent.FullName;
            }

            return null;
        }

        private static double Positive(double value, double fallback)
        {
            return value > 0 ? value : fallback;
        }

        private static double NonNegative(double value, double fallback)
        {
            return value >= 0 ? value : fallback;
        }
    }
}
