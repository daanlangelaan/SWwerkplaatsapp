using System;
using System.Globalization;

namespace SWWerkplaats.Configurator.Manufacturing
{
    public static class GCodeSafetyValidator
    {
        public static void Validate(string gcode, double safeTravelZMm, double maximumCutDepthMm)
        {
            if (string.IsNullOrWhiteSpace(gcode))
                throw new InvalidOperationException("CAM-export geblokkeerd: leeg G-codebestand.");

            var modalZ = safeTravelZMm;
            var lines = gcode.Replace("\r", string.Empty).Split('\n');
            for (var index = 0; index < lines.Length; index++)
            {
                var line = StripComment(lines[index]).Trim();
                if (line.Length == 0) continue;

                if (line.StartsWith("G28", StringComparison.OrdinalIgnoreCase))
                {
                    if (HasWord(line, 'Z')) modalZ = safeTravelZMm;
                    continue;
                }

                double z;
                var hasZ = TryWord(line, 'Z', out z);
                if (hasZ)
                {
                    if (z < -maximumCutDepthMm - 0.001)
                        throw new InvalidOperationException("CAM-export geblokkeerd: Z" + F(z) + " is dieper dan de toegestane Z-" + F(maximumCutDepthMm) + " op G-code-regel " + (index + 1) + ".");
                    modalZ = z;
                }

                var rapid = StartsWithCode(line, "G0") || StartsWithCode(line, "G00");
                var movesXy = HasWord(line, 'X') || HasWord(line, 'Y');
                if (rapid && movesXy && modalZ < safeTravelZMm - 0.001)
                    throw new InvalidOperationException("CAM-export geblokkeerd: X/Y-snelverplaatsing onder veilige Z+" + F(safeTravelZMm) + " op G-code-regel " + (index + 1) + " (modale Z" + F(modalZ) + ").");
            }
        }

        private static bool StartsWithCode(string line, string code)
        {
            if (!line.StartsWith(code, StringComparison.OrdinalIgnoreCase)) return false;
            return line.Length == code.Length || char.IsWhiteSpace(line[code.Length]);
        }

        private static string StripComment(string line)
        {
            var open = line.IndexOf('(');
            return open >= 0 ? line.Substring(0, open) : line;
        }

        private static bool HasWord(string line, char word)
        {
            double ignored;
            return TryWord(line, word, out ignored);
        }

        private static bool TryWord(string line, char word, out double value)
        {
            value = 0;
            for (var i = 0; i < line.Length; i++)
            {
                if (char.ToUpperInvariant(line[i]) != word) continue;
                var start = i + 1;
                var end = start;
                while (end < line.Length && (char.IsDigit(line[end]) || line[end] == '-' || line[end] == '+' || line[end] == '.')) end++;
                if (end > start && double.TryParse(line.Substring(start, end - start), NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return true;
            }
            return false;
        }

        private static string F(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
