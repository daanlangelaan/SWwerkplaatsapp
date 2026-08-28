using System;
using System.Collections.Generic;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class ProfileTraceabilityService
    {
        public void Assign(WorkbenchModel model, string productId, int orderQuantity)
        {
            if (model == null || model.Profiles.Count == 0) return;
            var units = Math.Max(1, orderQuantity);
            var prefix = Prefix(productId);
            var traceNumber = 1;
            foreach (var profile in model.Profiles) profile.PieceTraceIds.Clear();
            foreach (var operation in model.ProfileOperations) operation.PieceTraceIds.Clear();
            foreach (var placement in model.AssemblyPlacements.Where(item => item.Kind == AssemblyComponentKind.Profile)) placement.TraceId = null;

            for (var unit = 1; unit <= units; unit++)
            {
                foreach (var profile in model.Profiles)
                {
                    if (profile.Quantity % units != 0)
                        throw new InvalidOperationException("Profieltraceerbaarheid: profielaantal kan niet over de orderunits worden verdeeld.");
                    var piecesPerUnit = profile.Quantity / units;
                    for (var piece = 0; piece < piecesPerUnit; piece++)
                    {
                        var unitLabel = units > 1 ? "-U" + unit.ToString("00") : string.Empty;
                        profile.PieceTraceIds.Add(prefix + unitLabel + "-P" + traceNumber.ToString("000"));
                        traceNumber++;
                    }
                }
            }

            foreach (var operation in model.ProfileOperations)
            {
                var profile = model.Profiles.FirstOrDefault(item => string.Equals(item.Name, operation.PartName, StringComparison.Ordinal));
                if (profile == null || profile.PieceTraceIds.Count == 0)
                    throw new InvalidOperationException("Profieltraceerbaarheid: bewerking heeft geen gekoppelde profielstukken: " + operation.PartName);
                if (operation.Quantity % profile.PieceTraceIds.Count != 0)
                    throw new InvalidOperationException("Profieltraceerbaarheid: bewerkingsaantal kan niet over de profielstukken worden verdeeld: " + operation.PartName);
                var operationsPerPiece = operation.Quantity / profile.PieceTraceIds.Count;
                foreach (var traceId in profile.PieceTraceIds)
                    for (var repeat = 0; repeat < operationsPerPiece; repeat++)
                        operation.PieceTraceIds.Add(traceId);
            }

            var placements = model.AssemblyPlacements.Where(item => item.Kind == AssemblyComponentKind.Profile).ToArray();
            var piecesInOneUnit = model.Profiles.Sum(profile => profile.Quantity / units);
            if (placements.Length == piecesInOneUnit)
            {
                var placementIndex = 0;
                foreach (var profile in model.Profiles)
                {
                    var piecesPerUnit = profile.Quantity / units;
                    foreach (var traceId in profile.PieceTraceIds.Take(piecesPerUnit))
                        placements[placementIndex++].TraceId = traceId;
                }
            }
            else if (placements.Length > 0)
            {
                model.DesignNotes.Add("OPEN: 3D-profielplaatsingen dekken niet alle genummerde profielstukken; assemblykoppeling vereist aanvulling.");
            }
        }

        private static string Prefix(string productId)
        {
            switch ((productId ?? string.Empty).ToLowerInvariant())
            {
                case "machinebasis": return "MB";
                case "robotcel": return "RC";
                case "lineaire_robotcel": return "LRC";
                case "materiaalwagen": return "MW";
                case "sim_rig_4080": return "SR";
                case "werktafel": return "WT";
                case "werktafel_lex": return "LX";
                case "werktafel_lex_revolution": return "LR";
                case "hoogteverstelbare_werktafel": return "HW";
                default: return "PF";
            }
        }
    }
}
