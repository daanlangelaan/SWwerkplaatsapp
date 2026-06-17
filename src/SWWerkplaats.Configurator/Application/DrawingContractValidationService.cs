using System;
using System.Collections.Generic;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Engine;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class DrawingContractFinding
    {
        public string Severity { get; private set; }
        public string Code { get; private set; }
        public string PartName { get; private set; }
        public string Message { get; private set; }

        public DrawingContractFinding(string severity, string code, string partName, string message)
        {
            Severity = severity;
            Code = code;
            PartName = partName;
            Message = message;
        }
    }

    public sealed class DrawingContractValidationService
    {
        private const double ToleranceMm = 0.001;

        public List<DrawingContractFinding> Validate(WorkbenchModel model)
        {
            var findings = new List<DrawingContractFinding>();
            if (model == null)
            {
                findings.Add(new DrawingContractFinding("Fout", "MODEL_ONTBREEKT", "", "Geen model beschikbaar voor tekencontractcontrole."));
                return findings;
            }

            var sheetPlacements = SheetPlacementsByPartName(model);
            foreach (var sheet in model.Sheets)
            {
                ValidateSheet(sheet, sheetPlacements, findings);
            }

            foreach (var placement in model.AssemblyPlacements)
            {
                if (placement.Kind == AssemblyComponentKind.Sheet && DrawingContracts.ForOrientation(placement.Orientation).BasePlane == SolidWorksBasePlane.Default)
                {
                    findings.Add(new DrawingContractFinding("Fout", "ORIENTATIE_ZONDER_CONTRACT", placement.PartName, "Plaatorientatie heeft geen expliciet tekencontract."));
                }
            }

            return findings;
        }

        private static void ValidateSheet(SheetPart sheet, Dictionary<string, List<AssemblyPlacement>> placementsByName, List<DrawingContractFinding> findings)
        {
            if (sheet == null) return;

            if (sheet.LengthMm <= 0 || sheet.WidthMm <= 0)
            {
                findings.Add(new DrawingContractFinding("Fout", "PLAATMAAT_ONGELDIG", sheet.Name, "Plaatdeel heeft geen geldige lengte/breedte."));
            }

            List<AssemblyPlacement> placements;
            if (string.IsNullOrWhiteSpace(sheet.Name) || !placementsByName.TryGetValue(sheet.Name, out placements) || placements.Count == 0)
            {
                findings.Add(new DrawingContractFinding("Fout", "PLAATSING_ONTBREEKT", sheet.Name, "Plaatdeel heeft geen assemblyplaatsing en dus geen orientatiecontract."));
            }
            else
            {
                if (placements.Count > 1)
                {
                    findings.Add(new DrawingContractFinding("Waarschuwing", "PLAATSING_DUBBEL", sheet.Name, "Plaatdeel heeft meerdere assemblyplaatsingen met dezelfde naam."));
                }

                ValidatePlacementMatchesSheet(sheet, placements[0], findings);
            }

            foreach (var hole in sheet.Holes)
            {
                ValidateHole(sheet, hole, findings);
            }

            foreach (var pocket in sheet.Pockets)
            {
                ValidatePocket(sheet, pocket, findings);
            }
        }

        private static void ValidatePlacementMatchesSheet(SheetPart sheet, AssemblyPlacement placement, List<DrawingContractFinding> findings)
        {
            var contract = DrawingContracts.ForOrientation(placement.Orientation);
            if (contract.BasePlane == SolidWorksBasePlane.Default)
            {
                findings.Add(new DrawingContractFinding("Fout", "PLAATSING_ORIENTATIE_ONBEKEND", sheet.Name, "Assemblyplaatsing gebruikt een orientatie zonder tekencontract."));
            }

            if (Math.Abs(placement.LengthMm - sheet.LengthMm) > ToleranceMm || Math.Abs(placement.WidthMm - sheet.WidthMm) > ToleranceMm)
            {
                findings.Add(new DrawingContractFinding("Fout", "PLAATSING_MAAT_MISMATCH", sheet.Name, "Assemblyplaatsing en plaatdeel hebben verschillende lokale plaatmaten."));
            }
        }

        private static void ValidateHole(SheetPart sheet, SheetHole hole, List<DrawingContractFinding> findings)
        {
            if (hole == null) return;
            if (hole.Xmm < -ToleranceMm || hole.Xmm > sheet.LengthMm + ToleranceMm || hole.Ymm < -ToleranceMm || hole.Ymm > sheet.WidthMm + ToleranceMm)
            {
                findings.Add(new DrawingContractFinding("Fout", "GAT_BUITEN_LOKALE_PLAAT", sheet.Name, HoleName(hole) + " valt buiten lokale sheetcoordinaten."));
            }

            if (hole.DiameterMm <= 0)
            {
                findings.Add(new DrawingContractFinding("Fout", "GAT_DIAMETER_ONGELDIG", sheet.Name, HoleName(hole) + " heeft geen geldige diameter."));
            }

            if (hole.DepthMm > 0 && hole.DepthMode == OperationDepthMode.Through)
            {
                findings.Add(new DrawingContractFinding("Waarschuwing", "GAT_DIEPTE_MODUS_IMPLICIET", sheet.Name, HoleName(hole) + " heeft diepte maar staat nog op Through."));
            }

            if (hole.DepthMode == OperationDepthMode.BlindFromFace && hole.Face == OperationFace.CenterPlane)
            {
                findings.Add(new DrawingContractFinding("Waarschuwing", "BLIND_GAT_ZONDER_ZIJDE", sheet.Name, HoleName(hole) + " is blind maar heeft geen fysieke zijde."));
            }
        }

        private static void ValidatePocket(SheetPart sheet, SheetPocket pocket, List<DrawingContractFinding> findings)
        {
            if (pocket == null) return;
            if (pocket.Xmm < -ToleranceMm || pocket.Ymm < -ToleranceMm || pocket.Xmm + pocket.LengthMm > sheet.LengthMm + ToleranceMm || pocket.Ymm + pocket.WidthMm > sheet.WidthMm + ToleranceMm)
            {
                findings.Add(new DrawingContractFinding("Fout", "POCKET_BUITEN_LOKALE_PLAAT", sheet.Name, PocketName(pocket) + " valt buiten lokale sheetcoordinaten."));
            }

            if (pocket.LengthMm <= 0 || pocket.WidthMm <= 0 || pocket.DepthMm <= 0)
            {
                findings.Add(new DrawingContractFinding("Fout", "POCKET_MAAT_ONGELDIG", sheet.Name, PocketName(pocket) + " heeft geen geldige lengte/breedte/diepte."));
            }

            if (pocket.DepthMode == OperationDepthMode.PocketFromFace && pocket.Face == OperationFace.CenterPlane)
            {
                findings.Add(new DrawingContractFinding("Waarschuwing", "POCKET_ZONDER_ZIJDE", sheet.Name, PocketName(pocket) + " heeft nog geen expliciete fysieke zijde."));
            }
        }

        private static Dictionary<string, List<AssemblyPlacement>> SheetPlacementsByPartName(WorkbenchModel model)
        {
            var placementsByName = new Dictionary<string, List<AssemblyPlacement>>(StringComparer.OrdinalIgnoreCase);
            foreach (var placement in model.AssemblyPlacements)
            {
                if (placement.Kind != AssemblyComponentKind.Sheet || string.IsNullOrWhiteSpace(placement.PartName)) continue;
                List<AssemblyPlacement> placements;
                if (!placementsByName.TryGetValue(placement.PartName, out placements))
                {
                    placements = new List<AssemblyPlacement>();
                    placementsByName[placement.PartName] = placements;
                }

                placements.Add(placement);
            }

            return placementsByName;
        }

        private static string HoleName(SheetHole hole)
        {
            return string.IsNullOrWhiteSpace(hole.Name) ? "Gat" : hole.Name;
        }

        private static string PocketName(SheetPocket pocket)
        {
            return string.IsNullOrWhiteSpace(pocket.Name) ? "Pocket" : pocket.Name;
        }
    }
}
