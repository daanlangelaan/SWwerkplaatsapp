using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace SWWerkplaats.Configurator.Portal
{
    public static class PortalWorkspaceHtml
    {
        public static string Page()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PortalAssets", "portal-workspace.html");
            if (!File.Exists(path)) path = FindSource(Environment.CurrentDirectory);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new InvalidOperationException("Portal-shell ontbreekt: Portal/portal-workspace.html is niet gevonden.");
            var presentation = PortalPresentationContract.LoadRequired();
            return File.ReadAllText(path)
                .Replace("/*DESIGN_TOKENS*/", CssTokens(presentation.DesignTokens))
                .Replace("/*MEDIUM_BREAKPOINT*/", Px(presentation.DesignTokens.BreakpointsPx["medium"]));
        }

        private static string CssTokens(PortalDesignTokens tokens)
        {
            var css = new StringBuilder();
            foreach (var value in tokens.Colors) css.Append("--color-").Append(Kebab(value.Key)).Append(':').Append(value.Value).Append(';');
            foreach (var value in tokens.SpacingPx) css.Append("--space-").Append(Kebab(value.Key)).Append(':').Append(Px(value.Value)).Append(';');
            foreach (var value in tokens.RadiiPx) css.Append("--radius-").Append(Kebab(value.Key)).Append(':').Append(Px(value.Value)).Append(';');
            foreach (var value in tokens.LayoutPx) css.Append("--layout-").Append(Kebab(value.Key)).Append(':').Append(Px(value.Value)).Append(';');
            foreach (var value in tokens.ControlsPx) css.Append("--control-").Append(Kebab(value.Key)).Append(':').Append(Px(value.Value)).Append(';');
            foreach (var value in tokens.Typography.FontSizesPx) css.Append("--font-").Append(Kebab(value.Key)).Append(':').Append(Px(value.Value)).Append(';');
            foreach (var value in tokens.Typography.FontWeights) css.Append("--weight-").Append(Kebab(value.Key)).Append(':').Append(Number(value.Value)).Append(';');
            foreach (var value in tokens.Typography.LineHeights) css.Append("--line-").Append(Kebab(value.Key)).Append(':').Append(Number(value.Value)).Append(';');
            foreach (var value in tokens.Shadows) css.Append("--shadow-").Append(Kebab(value.Key)).Append(':').Append(value.Value).Append(';');
            css.Append("--font-family:").Append(tokens.Typography.FontFamily).Append(';');
            return css.ToString();
        }

        private static string FindSource(string startFolder)
        {
            if (string.IsNullOrWhiteSpace(startFolder)) return null;
            var folder = Path.GetFullPath(startFolder);
            for (var index = 0; index < 8; index++)
            {
                var candidate = Path.Combine(folder, "src", "SWWerkplaats.Configurator", "Portal", "portal-workspace.html");
                if (File.Exists(candidate)) return candidate;
                var parent = Directory.GetParent(folder);
                if (parent == null) break;
                folder = parent.FullName;
            }
            return null;
        }

        private static string Px(double value) { return Number(value) + "px"; }
        private static string Number(double value) { return value.ToString("0.###", CultureInfo.InvariantCulture); }

        private static string Kebab(string value)
        {
            var result = new StringBuilder();
            foreach (var character in value ?? string.Empty)
                result.Append(char.IsUpper(character) ? "-" + char.ToLowerInvariant(character) : character.ToString());
            return result.ToString();
        }
    }
}
