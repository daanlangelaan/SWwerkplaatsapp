using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class LexMotionMasterDataSettings
    {
        public double[] HorizontalLockPositionsMm { get; private set; }

        public static LexMotionMasterDataSettings LoadRequired()
        {
            var catalog = MasterDataRuntimeCatalog.LoadRequired();
            var matches = catalog.Records("productRules")
                .Where(row => string.Equals(MasterDataRuntimeCatalog.Value(row, "Product-ID"), "werktafel_lex", StringComparison.OrdinalIgnoreCase))
                .Where(row => string.Equals(MasterDataRuntimeCatalog.Value(row, "Parametertype"), "Borgposities", StringComparison.OrdinalIgnoreCase))
                .Where(row => !string.Equals(MasterDataRuntimeCatalog.Value(row, "Status"), "Open", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("LEX-masterdata vereist precies één vrijgegeven productregel voor de horizontale borgposities.");

            var positions = MasterDataRuntimeCatalog.Value(matches[0], "Waarde")
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseMillimetres)
                .OrderBy(value => value)
                .ToArray();
            if (positions.Length != 3 || positions[0] >= 0 || Math.Abs(positions[1]) > 0.001 || positions[2] <= 0)
                throw new InvalidOperationException("LEX-borgposities moeten exact drie waarden bevatten: links, midden 0 en rechts.");

            return new LexMotionMasterDataSettings { HorizontalLockPositionsMm = positions };
        }

        private static double ParseMillimetres(string value)
        {
            double result;
            if (!double.TryParse((value ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                throw new InvalidOperationException("Ongeldige LEX-borgpositie: " + value + ".");
            return result;
        }
    }
}
