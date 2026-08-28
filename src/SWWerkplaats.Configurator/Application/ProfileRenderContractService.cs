using System;
using System.Linq;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    /// <summary>
    /// Produces directly renderable profile geometry. Placement, material and slot-axis
    /// positions are authoritative; the still-unreleased internal cavity envelope remains
    /// explicitly provisional until the supplier section drawing is stored in masterdata.
    /// </summary>
    public sealed class ProfileRenderContractService
    {
        private readonly ProfileSlotGeometryCatalog geometryCatalog;
        private readonly ProfileCoreHolePositionService coreHoles;

        public ProfileRenderContractService()
            : this(new ProfileSlotGeometryCatalog(), new ProfileCoreHolePositionService())
        {
        }

        internal ProfileRenderContractService(ProfileSlotGeometryCatalog geometryCatalog, ProfileCoreHolePositionService coreHoles)
        {
            this.geometryCatalog = geometryCatalog ?? throw new ArgumentNullException("geometryCatalog");
            this.coreHoles = coreHoles ?? throw new ArgumentNullException("coreHoles");
        }

        public PortalProfileRenderGeometry Build(WorkbenchModel model, AssemblyPlacement placement)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (placement == null) throw new ArgumentNullException("placement");
            var material = coreHoles.MaterialForPlacement(model, placement);
            if (material == null) throw new InvalidOperationException("Profielrendercontract mist materiaal voor " + placement.TraceId + ".");
            var geometry = geometryCatalog.FindRequired(material.Id);
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
            var centers = new[] { new double[0], new double[0], new double[0] };
            centers[widthAxis] = geometry.WidthFaceAxisOffsetsMm.Select(value => value - material.WidthMm / 2.0).ToArray();
            centers[heightAxis] = geometry.HeightFaceAxisOffsetsMm.Select(value => value - material.HeightMm / 2.0).ToArray();

            var result = new PortalProfileRenderGeometry
            {
                ContractVersion = 1,
                MaterialId = material.Id,
                ProfileSeries = geometry.ProfileSeries,
                LongitudinalAxis = longitudinalAxis,
                SlotAxisCentersLocalMm = centers,
                ModulePitchMm = geometry.PitchMm,
                SlotMouthWidthMm = geometry.SlotMouthWidthMm,
                SlotMouthDepthMm = geometry.SlotMouthDepthMm,
                SlotCavityWidthMm = geometry.SlotCavityWidthMm,
                SlotCavityDepthMm = geometry.SlotCavityDepthMm,
                OutsideCornerRadiusMm = geometry.OutsideCornerRadiusMm,
                CoreHoleDiameterMm = geometry.CoreHoleDiameterMm,
                Status = Complete(geometry) ? "ExactSupplierGeometry" : "ProvisionalRenderEnvelope"
            };
            if (!geometry.SlotMouthWidthMm.HasValue) result.OpenData.Add(material.Id + ": Sleufmondbreedte mm ontbreekt.");
            if (!geometry.SlotMouthDepthMm.HasValue) result.OpenData.Add(material.Id + ": Sleufmond-diepte mm ontbreekt.");
            if (!geometry.SlotCavityWidthMm.HasValue) result.OpenData.Add(material.Id + ": Sleufkamerbreedte mm ontbreekt.");
            if (!geometry.SlotCavityDepthMm.HasValue) result.OpenData.Add(material.Id + ": Sleufkamerdiepte mm ontbreekt.");
            if (!geometry.OutsideCornerRadiusMm.HasValue) result.OpenData.Add(material.Id + ": Buitenradius mm ontbreekt.");
            if (!geometry.CoreHoleDiameterMm.HasValue) result.OpenData.Add(material.Id + ": Kernboringdiameter mm ontbreekt.");
            if (!string.IsNullOrWhiteSpace(geometry.OpenGeometryData)) result.OpenData.Add(material.Id + ": " + geometry.OpenGeometryData.Trim());
            return result;
        }

        private static bool Complete(ProfileSlotGeometry geometry)
        {
            return geometry.SlotMouthWidthMm.HasValue && geometry.SlotMouthDepthMm.HasValue && geometry.SlotCavityWidthMm.HasValue
                && geometry.SlotCavityDepthMm.HasValue && geometry.OutsideCornerRadiusMm.HasValue
                && geometry.CoreHoleDiameterMm.HasValue && !string.IsNullOrWhiteSpace(geometry.GeometrySource)
                && string.IsNullOrWhiteSpace(geometry.OpenGeometryData)
                && string.Equals(geometry.Status, "ExactSupplierGeometry", StringComparison.Ordinal);
        }
    }
}
