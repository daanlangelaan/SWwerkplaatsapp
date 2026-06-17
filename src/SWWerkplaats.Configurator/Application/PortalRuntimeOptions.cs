using System;
using System.IO;
using System.Web.Script.Serialization;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class PortalRuntimeOptions
    {
        public string RootFolder { get; set; }
        public string Prefix { get; set; }
        public int Port { get; set; }
        public bool PortalOnly { get; set; }

        public static PortalRuntimeOptions Load(string[] args)
        {
            var options = Defaults();
            ApplyConfigFile(options);
            ApplyEnvironment(options);
            ApplyArgs(options, args);
            Normalize(options);
            return options;
        }

        private static PortalRuntimeOptions Defaults()
        {
            return new PortalRuntimeOptions
            {
                RootFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PortalData"),
                Prefix = "http://localhost:8088/",
                Port = 8088,
                PortalOnly = false
            };
        }

        private static void ApplyConfigFile(PortalRuntimeOptions options)
        {
            var path = ConfigPath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            try
            {
                var configured = new JavaScriptSerializer().Deserialize<PortalRuntimeOptions>(File.ReadAllText(path));
            if (configured == null) return;
            if (!string.IsNullOrWhiteSpace(configured.RootFolder)) options.RootFolder = configured.RootFolder;
            if (configured.Port > 0) ApplyPort(options, configured.Port);
            if (!string.IsNullOrWhiteSpace(configured.Prefix)) options.Prefix = configured.Prefix;
            if (configured.PortalOnly) options.PortalOnly = true;
            }
            catch
            {
                // Keep defaults when local configuration is invalid; startup should remain forgiving.
            }
        }

        private static void ApplyEnvironment(PortalRuntimeOptions options)
        {
            ApplyString(options, "SW_PORTAL_ROOT", delegate(string value) { options.RootFolder = value; });
            ApplyInt(options, "SW_PORTAL_PORT", delegate(int value) { ApplyPort(options, value); });
            ApplyString(options, "SW_PORTAL_PREFIX", delegate(string value) { options.Prefix = value; });
        }

        private static void ApplyArgs(PortalRuntimeOptions options, string[] args)
        {
            if (args == null) return;
            foreach (var arg in args)
            {
                if (string.Equals(arg, "--portal-only", StringComparison.OrdinalIgnoreCase))
                {
                    options.PortalOnly = true;
                    continue;
                }

                ApplyArg(arg, "--portal-root=", delegate(string value) { options.RootFolder = value; });
                ApplyArg(arg, "--portal-prefix=", delegate(string value) { options.Prefix = value; });
                ApplyArg(arg, "--portal-port=", delegate(string value)
                {
                    int port;
                    if (int.TryParse(value, out port) && port > 0) ApplyPort(options, port);
                });
            }
        }

        private static void ApplyPort(PortalRuntimeOptions options, int port)
        {
            options.Port = port;
            if (string.IsNullOrWhiteSpace(options.Prefix) || IsDefaultLocalPrefix(options.Prefix))
            {
                options.Prefix = "http://localhost:" + port.ToString() + "/";
            }
        }

        private static bool IsDefaultLocalPrefix(string prefix)
        {
            Uri uri;
            if (!Uri.TryCreate(prefix, UriKind.Absolute, out uri)) return false;
            return string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                && (uri.Port == 8088 || uri.Port <= 0);
        }

        private static void Normalize(PortalRuntimeOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.RootFolder))
            {
                options.RootFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PortalData");
            }

            options.RootFolder = Path.GetFullPath(Environment.ExpandEnvironmentVariables(options.RootFolder));

            if (options.Port <= 0) options.Port = 8088;
            if (string.IsNullOrWhiteSpace(options.Prefix))
            {
                options.Prefix = "http://localhost:" + options.Port.ToString() + "/";
            }

            if (!options.Prefix.EndsWith("/", StringComparison.Ordinal)) options.Prefix += "/";

            Uri uri;
            if (Uri.TryCreate(options.Prefix, UriKind.Absolute, out uri) && uri.Port > 0)
            {
                options.Port = uri.Port;
            }
            else
            {
                options.Prefix = "http://localhost:" + options.Port.ToString() + "/";
            }
        }

        private static void ApplyString(PortalRuntimeOptions options, string key, Action<string> apply)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(value)) apply(value);
        }

        private static void ApplyInt(PortalRuntimeOptions options, string key, Action<int> apply)
        {
            var value = Environment.GetEnvironmentVariable(key);
            int parsed;
            if (int.TryParse(value, out parsed) && parsed > 0) apply(parsed);
        }

        private static void ApplyArg(string arg, string prefix, Action<string> apply)
        {
            if (arg == null || !arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return;
            var value = arg.Substring(prefix.Length);
            if (!string.IsNullOrWhiteSpace(value)) apply(value);
        }

        private static string ConfigPath()
        {
            var existingConfig = FindConfigFolder(AppDomain.CurrentDomain.BaseDirectory);
            if (existingConfig != null) return Path.Combine(existingConfig, "portal-runtime.json");

            existingConfig = FindConfigFolder(Environment.CurrentDirectory);
            if (existingConfig != null) return Path.Combine(existingConfig, "portal-runtime.json");

            return null;
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
    }
}
