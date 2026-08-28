using System;
using System.Collections.Generic;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    /// <summary>
    /// Maps the canonical profile core-hole grid to placement-local coordinates.
    /// Masterdata remains the only source for hole count and 20/40-mm axis positions.
    /// </summary>
    public sealed class ProfileCoreHolePositionService
    {
        private readonly ProfileSlotGeometryCatalog catalog;

        public ProfileCoreHolePositionService()
            : this(new ProfileSlotGeometryCatalog())
        {
        }

        internal ProfileCoreHolePositionService(ProfileSlotGeometryCatalog catalog)
        {
            this.catalog = catalog ?? throw new ArgumentNullException("catalog");
        }

        public Material MaterialForPlacement(WorkbenchModel model, AssemblyPlacement placement)
        {
            if (model == null || placement == null || string.IsNullOrWhiteSpace(placement.TraceId)) return null;
            return model.Profiles
                .Where(profile => profile.Material != null && profile.PieceTraceIds.Contains(placement.TraceId, StringComparer.OrdinalIgnoreCase))
                .Select(profile => profile.Material)
                .FirstOrDefault();
        }

        public IReadOnlyList<ProfileCoreHolePosition> Build(WorkbenchModel model, AssemblyPlacement placement)
        {
            var material = MaterialForPlacement(model, placement);
            if (material == null) return new ProfileCoreHolePosition[0];
            return Build(placement, material);
        }

        public IReadOnlyList<ProfileCoreHolePosition> Build(AssemblyPlacement placement, Material material)
        {
            if (placement == null) throw new ArgumentNullException("placement");
            if (material == null) throw new ArgumentNullException("material");
            var geometry = catalog.FindRequired(material.Id);
            var dimensions = new[]
            {
                Math.Max(2, placement.LengthMm),
                Math.Max(2, placement.HeightMm),
                Math.Max(2, placement.WidthMm)
            };
            var longitudinalAxis = placement.Sticker != null && placement.Sticker.LongitudinalAxis >= 0 && placement.Sticker.LongitudinalAxis <= 2
                ? placement.Sticker.LongitudinalAxis
                : Array.IndexOf(dimensions, dimensions.Max());
            var crossAxes = Enumerable.Range(0, 3).Where(axis => axis != longitudinalAxis).ToArray();
            var widthAxis = Math.Abs(dimensions[crossAxes[0]] - material.WidthMm) <= Math.Abs(dimensions[crossAxes[1]] - material.WidthMm)
                ? crossAxes[0] : crossAxes[1];
            var heightAxis = crossAxes.Single(axis => axis != widthAxis);
            var result = new List<ProfileCoreHolePosition>();
            var index = 0;
            foreach (var heightOffset in geometry.HeightFaceAxisOffsetsMm)
            foreach (var widthOffset in geometry.WidthFaceAxisOffsetsMm)
            {
                var local = new double[3];
                local[widthAxis] = widthOffset - material.WidthMm / 2.0;
                local[heightAxis] = heightOffset - material.HeightMm / 2.0;
                result.Add(new ProfileCoreHolePosition
                {
                    CoreHoleIndex = ++index,
                    WidthOffsetMm = widthOffset,
                    HeightOffsetMm = heightOffset,
                    LocalXmm = local[0],
                    LocalYmm = local[1],
                    LocalZmm = local[2]
                });
            }
            return result;
        }
    }
}
