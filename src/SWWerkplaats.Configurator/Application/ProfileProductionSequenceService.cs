using System;
using System.Collections.Generic;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class ProfileProductionSequenceService
    {
        public IReadOnlyList<ProfileProductionSequenceItem> Build(WorkbenchModel model)
        {
            if (model == null) throw new ArgumentNullException("model");

            var placementByTraceId = model.AssemblyPlacements
                .Where(placement => placement.Sticker != null)
                .SelectMany(placement => placement.Sticker.TraceIds.Select(traceId => new { traceId, placement }))
                .GroupBy(item => item.traceId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().placement, StringComparer.OrdinalIgnoreCase);

            var operationsByTraceId = model.ProfileOperations
                .SelectMany(operation => operation.PieceTraceIds.Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(traceId => new { traceId, operation }))
                .GroupBy(item => item.traceId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Select(item => item.operation)
                    .OrderBy(operation => operation.Sequence).ThenBy(operation => operation.Kind).ToArray(), StringComparer.OrdinalIgnoreCase);

            var items = new List<ProfileProductionSequenceItem>();
            foreach (var profile in model.Profiles)
            {
                foreach (var traceId in profile.PieceTraceIds)
                {
                    AssemblyPlacement placement;
                    placementByTraceId.TryGetValue(traceId, out placement);
                    if (placement == null || placement.Sticker == null)
                        throw new InvalidOperationException("Profielproductie geblokkeerd: gevalideerde stickerplaatsing ontbreekt voor " + traceId + ".");
                    ProfileOperation[] operations;
                    if (!operationsByTraceId.TryGetValue(traceId, out operations)) operations = new ProfileOperation[0];
                    var profileId = operations.FirstOrDefault() == null
                        ? (profile.Material == null ? profile.Name : profile.Material.Id + "_" + profile.Name.Replace(' ', '_'))
                        : operations.First().ProfileId;
                    var item = new ProfileProductionSequenceItem
                    {
                        TraceId = traceId,
                        ProfileId = profileId,
                        PartName = profile.Name,
                        Material = profile.Material,
                        ProfileLengthMm = profile.LengthMm,
                        Sticker = placement.Sticker
                    };
                    item.MachiningFrame = new ProfileMachiningFrameService().Build(traceId, placement, profile.Material);
                    item.Operations.AddRange(operations);
                    item.ClampInstruction = ProfileCncOperatorText.ClampInstruction(item);
                    item.StickerInstruction = ProfileCncOperatorText.StickerInstruction(item);
                    items.Add(item);
                }
            }

            var ordered = items
                .OrderByDescending(item => CrossSectionMax(item.Material))
                .ThenByDescending(item => CrossSectionMin(item.Material))
                .ThenByDescending(item => item.ProfileLengthMm)
                .ThenBy(item => item.TraceId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            for (var index = 0; index < ordered.Length; index++) ordered[index].ProductionOrder = index + 1;
            return ordered;
        }

        private static double CrossSectionMax(Material material)
        {
            return material == null ? 0 : Math.Max(material.WidthMm, material.HeightMm);
        }

        private static double CrossSectionMin(Material material)
        {
            return material == null ? 0 : Math.Min(material.WidthMm, material.HeightMm);
        }

    }
}
