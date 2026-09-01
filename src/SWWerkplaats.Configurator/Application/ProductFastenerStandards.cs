using System;
using System.Collections.Generic;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public enum ProfileAttachmentKind
    {
        ProfileToProfile,
        ComponentOrPlateToProfile,
        LinearRailToProfile
    }

    public enum ProfileNutRetentionKind
    {
        StandardConnector,
        ClickNutWithSpringBall,
        SlidingNut
    }

    public static class ProfileFastenerPolicy
    {
        public static ProfileNutRetentionKind NutFor(ProfileAttachmentKind attachment)
        {
            switch (attachment)
            {
                case ProfileAttachmentKind.ProfileToProfile:
                    return ProfileNutRetentionKind.StandardConnector;
                case ProfileAttachmentKind.LinearRailToProfile:
                    return ProfileNutRetentionKind.SlidingNut;
                default:
                    return ProfileNutRetentionKind.ClickNutWithSpringBall;
            }
        }
    }

    public static class ProductFastenerStandards
    {
        private static readonly ProductFastenerStandard[] Standards =
        {
            new ProductFastenerStandard { ProductId = "cabinet", WoodToWoodFastenerId = "WOODSCREW_4" },
            new ProductFastenerStandard { ProductId = "werkbankkast", BaseProductId = "cabinet" },
            new ProductFastenerStandard { ProductId = "vakjeskast", BaseProductId = "cabinet" },
            new ProductFastenerStandard { ProductId = "shipping_box" },
            new ProductFastenerStandard { ProductId = "opvouwbare_werktafel" },
            new ProductFastenerStandard { ProductId = "werktafel", StructuralFastenerId = "M8_ISO4762" },
            new ProductFastenerStandard { ProductId = "machinebasis", BaseProductId = "werktafel" },
            new ProductFastenerStandard { ProductId = "robotcel", BaseProductId = "machinebasis" },
            new ProductFastenerStandard { ProductId = "lineaire_robotcel", BaseProductId = "machinebasis" },
            new ProductFastenerStandard { ProductId = "materiaalwagen", BaseProductId = "werktafel" },
            new ProductFastenerStandard { ProductId = "sim_rig_4080", BaseProductId = "werktafel" },
            new ProductFastenerStandard { ProductId = "werktafel_lex", BaseProductId = "werktafel" },
            new ProductFastenerStandard { ProductId = "werktafel_lex_revolution", BaseProductId = "werktafel_lex" },
            new ProductFastenerStandard { ProductId = "hoogteverstelbare_werktafel", BaseProductId = "werktafel_lex" }
        };

        public static ProductFastenerStandard Resolve(string productId)
        {
            var id = string.IsNullOrWhiteSpace(productId) ? "cabinet" : productId.Trim();
            var chain = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var resolved = new ProductFastenerStandard { ProductId = id };
            ResolveInto(id, resolved, chain);
            return resolved;
        }

        private static void ResolveInto(string productId, ProductFastenerStandard resolved, HashSet<string> chain)
        {
            if (!chain.Add(productId)) throw new InvalidOperationException("Cyclische productstandaard voor " + productId + ".");
            var current = Find(productId);
            if (current == null) throw new InvalidOperationException("Geen bevestigerstandaard vastgelegd voor producttype " + productId + ".");
            if (!string.IsNullOrWhiteSpace(current.BaseProductId)) ResolveInto(current.BaseProductId, resolved, chain);
            if (!string.IsNullOrWhiteSpace(current.WoodToWoodFastenerId)) resolved.WoodToWoodFastenerId = current.WoodToWoodFastenerId;
            if (!string.IsNullOrWhiteSpace(current.StructuralFastenerId)) resolved.StructuralFastenerId = current.StructuralFastenerId;
            resolved.BaseProductId = current.BaseProductId;
        }

        private static ProductFastenerStandard Find(string productId)
        {
            foreach (var standard in Standards)
            {
                if (string.Equals(standard.ProductId, productId, StringComparison.OrdinalIgnoreCase)) return standard;
            }
            return null;
        }
    }

    public static class FastenerSelectionService
    {
        public static double SelectWoodToWoodEdgeLength(FastenerDefinition screw, double passingThicknessMm, double receivingDepthMm)
        {
            ValidateWoodScrew(screw);
            var minimum = passingThicknessMm + Math.Max(20.0, screw.MinimumEdgePenetrationMm);
            var maximum = passingThicknessMm + receivingDepthMm - Math.Max(1.0, screw.MinimumTipClearanceMm);
            foreach (var length in SortedLengths(screw))
            {
                if (length + 0.001 >= minimum && length <= maximum + 0.001) return length;
            }
            throw new InvalidOperationException("Geen verkrijgbare houtschroeflengte past veilig: minimaal " + minimum.ToString("0.#") + " mm en maximaal " + maximum.ToString("0.#") + " mm.");
        }

        public static double SelectComponentToWoodFaceLength(FastenerDefinition screw, double componentStackMm, double woodThicknessMm)
        {
            ValidateWoodScrew(screw);
            const double minimumGripMm = 4.001;
            var minimum = componentStackMm + minimumGripMm;
            var maximum = componentStackMm + woodThicknessMm - Math.Max(1.0, screw.MinimumTipClearanceMm);
            var selected = 0.0;
            foreach (var length in SortedLengths(screw))
            {
                if (length >= minimum && length <= maximum + 0.001) selected = length;
            }
            if (selected <= 0) throw new InvalidOperationException("Geen verkrijgbare houtschroef geeft meer dan 4mm grip zonder door de aanliggende plaat te steken (bereik " + minimum.ToString("0.#") + " t/m " + maximum.ToString("0.#") + " mm).");
            return selected;
        }

        public static double OpposingScrewTipClearance(double receivingThicknessMm, double firstPenetrationMm, double secondPenetrationMm)
        {
            if (receivingThicknessMm <= 0) throw new ArgumentOutOfRangeException("receivingThicknessMm");
            if (firstPenetrationMm < 0) throw new ArgumentOutOfRangeException("firstPenetrationMm");
            if (secondPenetrationMm < 0) throw new ArgumentOutOfRangeException("secondPenetrationMm");
            return receivingThicknessMm - firstPenetrationMm - secondPenetrationMm;
        }

        public static double SelectTSlotBoltLength(
            FastenerDefinition bolt,
            double passingStackMm,
            double threadInletOffsetMm,
            double minimumThreadEngagementMm,
            double maximumInsertionDepthMm,
            double bottomClearanceMm)
        {
            if (bolt == null || bolt.UsageKind != FastenerUsageKind.StructuralBolt)
                throw new ArgumentException("Constructieboutfamilie ontbreekt.");
            if (passingStackMm < 0) throw new ArgumentOutOfRangeException("passingStackMm");
            if (threadInletOffsetMm < 0) throw new ArgumentOutOfRangeException("threadInletOffsetMm");
            if (minimumThreadEngagementMm <= 0) throw new ArgumentOutOfRangeException("minimumThreadEngagementMm");
            if (maximumInsertionDepthMm <= threadInletOffsetMm)
                throw new ArgumentOutOfRangeException("maximumInsertionDepthMm");
            if (bottomClearanceMm < 0 || bottomClearanceMm >= maximumInsertionDepthMm - threadInletOffsetMm)
                throw new ArgumentOutOfRangeException("bottomClearanceMm");

            var minimum = passingStackMm + threadInletOffsetMm + minimumThreadEngagementMm;
            var maximum = passingStackMm + maximumInsertionDepthMm - bottomClearanceMm;
            var selected = 0.0;
            foreach (var length in SortedLengths(bolt))
            {
                if (length + 0.001 >= minimum && length <= maximum + 0.001) selected = length;
            }
            if (selected <= 0)
                throw new InvalidOperationException("Geen verkrijgbare T-slotbout past tussen minimale draadaangrijping "
                    + minimum.ToString("0.###") + " mm en de sleufbodemgrens " + maximum.ToString("0.###") + " mm.");
            return selected;
        }

        private static double[] SortedLengths(FastenerDefinition screw)
        {
            var source = screw.AvailableLengthsMm == null || screw.AvailableLengthsMm.Length == 0
                ? new[] { screw.LengthMm }
                : (double[])screw.AvailableLengthsMm.Clone();
            Array.Sort(source);
            return source;
        }

        private static void ValidateWoodScrew(FastenerDefinition screw)
        {
            if (screw == null || screw.UsageKind != FastenerUsageKind.WoodScrew) throw new ArgumentException("Houtschroeffamilie ontbreekt.");
        }
    }
}
