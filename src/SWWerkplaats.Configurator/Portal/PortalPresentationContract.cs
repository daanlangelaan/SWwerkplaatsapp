using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace SWWerkplaats.Configurator.Portal
{
    public sealed class PortalPresentationContract
    {
        public int ContractVersion { get; set; }
        public PortalDesignTokens DesignTokens { get; set; }
        public PortalAssemblyPresentation Assembly { get; set; }

        public static PortalPresentationContract LoadRequired()
        {
            var path = FindUpwards(AppDomain.CurrentDomain.BaseDirectory)
                ?? FindUpwards(Environment.CurrentDirectory);
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("UI-presentatiecontract ontbreekt: config/ui/presentation-contract.json is niet gevonden.");

            try
            {
                var value = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }
                    .Deserialize<PortalPresentationContract>(File.ReadAllText(path));
                Validate(value);
                return value;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("UI-presentatiecontract kan niet worden gelezen uit " + path + ": " + ex.Message, ex);
            }
        }

        private static void Validate(PortalPresentationContract value)
        {
            if (value == null || value.ContractVersion != 1)
                throw new InvalidOperationException("UI-presentatiecontract heeft geen ondersteunde contractVersion 1.");
            if (value.DesignTokens == null || value.DesignTokens.Colors == null || value.DesignTokens.Colors.Count == 0)
                throw new InvalidOperationException("UI-presentatiecontract mist designTokens.colors.");
            var invalidColor = value.DesignTokens.Colors.FirstOrDefault(pair => !IsHexColor(pair.Value));
            if (!string.IsNullOrWhiteSpace(invalidColor.Key))
                throw new InvalidOperationException("UI-presentatiecontract bevat een ongeldige kleur voor " + invalidColor.Key + ".");
            if (value.Assembly == null || value.Assembly.Animation == null || value.Assembly.Camera == null || value.Assembly.Markers == null || value.Assembly.Materials == null)
                throw new InvalidOperationException("UI-presentatiecontract mist assembly.animation, assembly.camera, assembly.markers of assembly.materials.");
            RequirePositive(value.Assembly.Animation, "assembly.animation");
            RequirePositive(value.Assembly.Camera, "assembly.camera");
            RequirePositive(value.Assembly.Markers, "assembly.markers");
            RequirePositive(value.Assembly.Materials, "assembly.materials");
        }

        private static bool IsHexColor(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != 7 || value[0] != '#') return false;
            return value.Skip(1).All(character => (character >= '0' && character <= '9')
                || (character >= 'a' && character <= 'f') || (character >= 'A' && character <= 'F'));
        }

        private static void RequirePositive(IDictionary<string, double> values, string name)
        {
            if (values.Count == 0 || values.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value <= 0))
                throw new InvalidOperationException("UI-presentatiecontract bevat een ontbrekende of niet-positieve waarde in " + name + ".");
        }

        private static string FindUpwards(string startFolder)
        {
            if (string.IsNullOrWhiteSpace(startFolder)) return null;
            var folder = Path.GetFullPath(startFolder);
            for (var index = 0; index < 8 && !string.IsNullOrWhiteSpace(folder); index++)
            {
                var candidate = Path.Combine(folder, "config", "ui", "presentation-contract.json");
                if (File.Exists(candidate)) return candidate;
                var parent = Directory.GetParent(folder);
                if (parent == null) break;
                folder = parent.FullName;
            }
            return null;
        }
    }

    public sealed class PortalDesignTokens
    {
        public Dictionary<string, string> Colors { get; set; }
    }

    public sealed class PortalAssemblyPresentation
    {
        public Dictionary<string, double> Animation { get; set; }
        public Dictionary<string, double> Camera { get; set; }
        public Dictionary<string, double> Markers { get; set; }
        public Dictionary<string, double> Materials { get; set; }
    }
}
