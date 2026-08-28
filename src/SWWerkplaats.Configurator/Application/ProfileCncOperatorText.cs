using System;
using System.Globalization;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public static class ProfileCncOperatorText
    {
        public static string FaceName(Material material, ProfileMachiningFace face)
        {
            if (face == null) return "de aangegeven zijde";
            var span = face.FaceSpanMm;
            if (material == null) return "de zijde van " + F(span) + " mm";

            var small = Math.Min(material.WidthMm, material.HeightMm);
            var large = Math.Max(material.WidthMm, material.HeightMm);
            if (Close(small, large)) return "een zijde van " + F(span) + " mm";
            if (Close(span, small)) return "de smalle zijde van " + F(span) + " mm";
            if (Close(span, large)) return "de brede zijde van " + F(span) + " mm";
            return "de zijde van " + F(span) + " mm";
        }

        public static string CompactFaceName(Material material, ProfileMachiningFace face)
        {
            if (face == null) return "AANGEGEVEN KANT";
            var span = face.FaceSpanMm;
            if (material == null) return F(span) + "-MM-KANT";
            var small = Math.Min(material.WidthMm, material.HeightMm);
            var large = Math.Max(material.WidthMm, material.HeightMm);
            if (Close(small, large)) return F(span) + "-MM-KANT";
            if (Close(span, small)) return "KORTE " + F(span) + "-MM-KANT";
            if (Close(span, large)) return "LANGE " + F(span) + "-MM-KANT";
            return F(span) + "-MM-KANT";
        }

        public static string FaceSetup(Material material, ProfileMachiningFace face)
        {
            if (face == null) return "Leg de aangegeven zijde boven.";
            var text = "Leg " + FaceName(material, face) + " boven; het profiel staat dan "
                + F(face.ProfileHeightWhenUpMm) + " mm hoog.";
            var slotCount = face.SlotAxisOffsetsMm == null ? 0 : face.SlotAxisOffsetsMm.Count;
            if (slotCount == 1) return text + " Boven is een sleuf zichtbaar.";
            if (slotCount > 1) return text + " Boven zijn " + Number(slotCount) + " sleuven zichtbaar.";
            return text;
        }

        public static string ClampInstruction(ProfileProductionSequenceItem item)
        {
            var d0 = item == null || item.MachiningFrame == null ? null : item.MachiningFrame.Face("D0");
            if (item == null || d0 == null) return "STOP: de juiste profielstand kan niet worden bepaald.";
            return "Zet " + item.TraceId + " met de afgezaagde kop waar de sticker komt tegen de vaste aanslag. "
                + FaceSetup(item.Material, d0) + " Klem het profiel vast. Keer het profiel nooit in de lengterichting om.";
        }

        public static string StickerInstruction(ProfileProductionSequenceItem item)
        {
            if (item == null || item.Sticker == null)
                return "STOP: stickerpositie ontbreekt; profiel niet vrijgeven voor bewerking.";
            return "Plak sticker " + item.TraceId + " op " + WholeCentimeters(item.Sticker.OffsetFromAnchorEndMm)
                + " cm vanaf de vaste aanslag, dwars in het midden van de bovenzijde.";
        }

        public static string RollInstruction(ProfileProductionSequenceItem item, int quarterTurns, ProfileMachiningFace targetFace)
        {
            if (item == null || targetFace == null) return "STOP: de volgende profielstand kan niet worden bepaald.";
            return "Maak de klem los. Kijk vanaf de vaste aanslag langs het profiel en draai het profiel "
                + QuarterTurns(quarterTurns) + " met de klok mee. Zet dezelfde afgezaagde kop weer tegen de aanslag. "
                + FaceSetup(item.Material, targetFace) + " Klem vast en druk op CYCLE START.";
        }

        public static string SlotName(int oneBasedIndex)
        {
            if (oneBasedIndex == 1) return "eerste zichtbare sleuf vanaf links";
            if (oneBasedIndex == 2) return "tweede zichtbare sleuf vanaf links";
            if (oneBasedIndex == 3) return "derde zichtbare sleuf vanaf links";
            if (oneBasedIndex == 4) return "vierde zichtbare sleuf vanaf links";
            return "zichtbare sleuf " + oneBasedIndex.ToString(CultureInfo.InvariantCulture) + " vanaf links";
        }

        public static string CompactTurn(int quarterTurns)
        {
            if (quarterTurns == 1) return "1/4";
            if (quarterTurns == 2) return "1/2";
            if (quarterTurns == 3) return "3/4";
            return quarterTurns.ToString(CultureInfo.InvariantCulture) + " X 1/4";
        }

        public static string StickerCentimeters(ProfileProductionSequenceItem item)
        {
            return item == null || item.Sticker == null ? "?" : WholeCentimeters(item.Sticker.OffsetFromAnchorEndMm);
        }

        private static string QuarterTurns(int value)
        {
            if (value == 1) return "een kwartslag";
            if (value == 2) return "twee kwartslagen (een halve slag)";
            if (value == 3) return "drie kwartslagen";
            return value.ToString(CultureInfo.InvariantCulture) + " kwartslagen";
        }

        private static string Number(int value)
        {
            if (value == 1) return "een";
            if (value == 2) return "twee";
            if (value == 3) return "drie";
            if (value == 4) return "vier";
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string WholeCentimeters(double millimeters)
        {
            return Math.Round(millimeters / 10.0, MidpointRounding.AwayFromZero)
                .ToString("0", CultureInfo.InvariantCulture);
        }

        private static bool Close(double left, double right) { return Math.Abs(left - right) <= 0.01; }
        private static string F(double value) { return value.ToString("0.##", CultureInfo.InvariantCulture); }
    }
}
