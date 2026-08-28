using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Script.Serialization;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class ComponentPrimitiveRenderContract
    {
        public int ContractVersion { get; set; }
        public string ComponentId { get; set; }
        public string Status { get; set; }
        public string Source { get; set; }
        public List<string> OpenData { get; private set; }
        public List<ComponentPrimitiveRenderPart> Primitives { get; private set; }

        public ComponentPrimitiveRenderContract()
        {
            OpenData = new List<string>();
            Primitives = new List<ComponentPrimitiveRenderPart>();
        }
    }

    public sealed class ComponentPrimitiveRenderPart
    {
        public string Id { get; set; }
        public string Shape { get; set; }
        public string AppearanceRole { get; set; }
        public double Xmm { get; set; }
        public double Ymm { get; set; }
        public double Zmm { get; set; }
        public double SizeXmm { get; set; }
        public double SizeYmm { get; set; }
        public double SizeZmm { get; set; }
        public double RotationXDeg { get; set; }
        public double RotationYDeg { get; set; }
        public double RotationZDeg { get; set; }
        public double RadiusTopMm { get; set; }
        public double RadiusBottomMm { get; set; }
        public int RadialSegments { get; set; }
        public bool InheritPlacementDimensions { get; set; }
        public List<ComponentPrimitiveRenderHole> Holes { get; private set; }

        public ComponentPrimitiveRenderPart()
        {
            Holes = new List<ComponentPrimitiveRenderHole>();
        }
    }

    public sealed class ComponentPrimitiveRenderHole
    {
        public string Id { get; set; }
        public double Xmm { get; set; }
        public double Ymm { get; set; }
        public double Zmm { get; set; }
        public double DiameterMm { get; set; }
        public double DepthMm { get; set; }
        public string Plane { get; set; }
        public double CountersinkDiameterMm { get; set; }
        public double CountersinkDepthMm { get; set; }
    }

    /// <summary>
    /// Loads globally reusable, component-local visual geometry from the canonical
    /// masterdata runtime. The contract is render-only and never drives CAM, BOM,
    /// purchasing dimensions or production release.
    /// </summary>
    public sealed class ComponentPrimitiveRenderContractService
    {
        private readonly MasterDataRuntimeCatalog masterData;

        public ComponentPrimitiveRenderContractService()
            : this(MasterDataRuntimeCatalog.LoadRequired())
        {
        }

        internal ComponentPrimitiveRenderContractService(MasterDataRuntimeCatalog masterData)
        {
            this.masterData = masterData ?? throw new ArgumentNullException("masterData");
        }

        public ComponentPrimitiveRenderContract BuildRequired(string componentId)
        {
            if (string.IsNullOrWhiteSpace(componentId))
                throw new ArgumentException("Component-ID ontbreekt voor primitieve rendergeometrie.", "componentId");
            var row = masterData.Records("components").FirstOrDefault(record =>
                string.Equals(MasterDataRuntimeCatalog.Value(record, "Component-ID"), componentId, StringComparison.OrdinalIgnoreCase));
            if (row == null) throw new InvalidOperationException("Rendercomponent ontbreekt in masterdata: " + componentId + ".");

            var raw = MasterDataRuntimeCatalog.Value(row, "Renderprimitieven JSON").Trim();
            if (raw.Length == 0) throw new InvalidOperationException("Rendercomponent " + componentId + " mist Renderprimitieven JSON.");
            var status = MasterDataRuntimeCatalog.Value(row, "Primitieve renderstatus").Trim();
            var source = MasterDataRuntimeCatalog.Value(row, "Primitieve renderbron").Trim();
            var open = MasterDataRuntimeCatalog.Value(row, "Open primitieve renderdata").Trim();
            if (status.Length == 0 || source.Length == 0)
                throw new InvalidOperationException("Rendercomponent " + componentId + " mist status of bron.");
            if (string.Equals(status, "ProvisionalRenderEnvelope", StringComparison.OrdinalIgnoreCase) && open.Length == 0)
                throw new InvalidOperationException("Rendercomponent " + componentId + " is voorlopig maar mist OpenData.");

            var root = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.DeserializeObject(raw) as Dictionary<string, object>;
            if (root == null) throw new InvalidOperationException("Renderprimitieven JSON van " + componentId + " is geen object.");
            object rawVersion;
            object rawPrimitives;
            if (!root.TryGetValue("version", out rawVersion) || Convert.ToInt32(rawVersion, CultureInfo.InvariantCulture) != 1
                || !root.TryGetValue("primitives", out rawPrimitives))
                throw new InvalidOperationException("Renderprimitieven JSON van " + componentId + " mist contractversie 1 of primitives.");
            var primitiveRows = rawPrimitives as object[];
            if (primitiveRows == null || primitiveRows.Length == 0)
                throw new InvalidOperationException("Rendercomponent " + componentId + " bevat geen primitives.");

            var contract = new ComponentPrimitiveRenderContract
            {
                ContractVersion = 1,
                ComponentId = componentId,
                Status = status,
                Source = source
            };
            if (open.Length > 0) contract.OpenData.AddRange(open.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(value => value.Trim()));
            foreach (var value in primitiveRows)
            {
                var primitive = ParsePrimitive(componentId, value as Dictionary<string, object>);
                contract.Primitives.Add(primitive);
            }
            if (contract.Primitives.Select(item => item.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != contract.Primitives.Count)
                throw new InvalidOperationException("Rendercomponent " + componentId + " bevat dubbele primitive-ID's.");
            return contract;
        }

        private static ComponentPrimitiveRenderPart ParsePrimitive(string componentId, Dictionary<string, object> row)
        {
            if (row == null) throw new InvalidOperationException("Rendercomponent " + componentId + " bevat een ongeldige primitive.");
            var inheritPlacementDimensions = Boolean(row, "inheritPlacementDimensions");
            var primitive = new ComponentPrimitiveRenderPart
            {
                Id = RequiredText(row, "id", componentId),
                Shape = RequiredText(row, "shape", componentId).ToLowerInvariant(),
                AppearanceRole = RequiredText(row, "role", componentId),
                Xmm = Number(row, "x"), Ymm = Number(row, "y"), Zmm = Number(row, "z"),
                SizeXmm = inheritPlacementDimensions ? Number(row, "sizeX") : Positive(row, "sizeX", componentId),
                SizeYmm = inheritPlacementDimensions ? Number(row, "sizeY") : Positive(row, "sizeY", componentId),
                SizeZmm = inheritPlacementDimensions ? Number(row, "sizeZ") : Positive(row, "sizeZ", componentId),
                RotationXDeg = Number(row, "rotationX"),
                RotationYDeg = Number(row, "rotationY"),
                RotationZDeg = Number(row, "rotationZ"),
                RadiusTopMm = Number(row, "radiusTop"),
                RadiusBottomMm = Number(row, "radiusBottom"),
                RadialSegments = (int)Number(row, "segments"),
                InheritPlacementDimensions = inheritPlacementDimensions
            };
            if (primitive.Shape != "box" && primitive.Shape != "cylinder")
                throw new InvalidOperationException("Rendercomponent " + componentId + " gebruikt een niet-ondersteunde primitive: " + primitive.Shape + ".");
            if (primitive.Shape == "cylinder")
            {
                if (!inheritPlacementDimensions && primitive.RadiusTopMm <= 0) primitive.RadiusTopMm = primitive.SizeXmm / 2.0;
                if (!inheritPlacementDimensions && primitive.RadiusBottomMm <= 0) primitive.RadiusBottomMm = primitive.SizeZmm / 2.0;
                if (primitive.RadialSegments <= 0) primitive.RadialSegments = 32;
            }
            object rawHoles;
            var holes = row.TryGetValue("holes", out rawHoles) ? rawHoles as object[] : null;
            if (holes != null)
            {
                foreach (var value in holes)
                {
                    var hole = value as Dictionary<string, object>;
                    if (hole == null) throw new InvalidOperationException("Rendercomponent " + componentId + " bevat een ongeldig gat.");
                    var plane = RequiredText(hole, "plane", componentId).ToLowerInvariant();
                    if (plane != "x" && plane != "y" && plane != "z")
                        throw new InvalidOperationException("Rendercomponent " + componentId + " bevat een gat met ongeldig vlak.");
                    primitive.Holes.Add(new ComponentPrimitiveRenderHole
                    {
                        Id = RequiredText(hole, "id", componentId),
                        Xmm = Number(hole, "x"), Ymm = Number(hole, "y"), Zmm = Number(hole, "z"),
                        DiameterMm = Positive(hole, "diameter", componentId),
                        DepthMm = Number(hole, "depth"),
                        Plane = plane,
                        CountersinkDiameterMm = Number(hole, "countersinkDiameter"),
                        CountersinkDepthMm = Number(hole, "countersinkDepth")
                    });
                }
            }
            return primitive;
        }

        private static string RequiredText(Dictionary<string, object> row, string key, string componentId)
        {
            object raw;
            var value = row.TryGetValue(key, out raw) && raw != null ? Convert.ToString(raw, CultureInfo.InvariantCulture).Trim() : string.Empty;
            if (value.Length == 0) throw new InvalidOperationException("Rendercomponent " + componentId + " mist veld " + key + ".");
            return value;
        }

        private static double Positive(Dictionary<string, object> row, string key, string componentId)
        {
            var value = Number(row, key);
            if (value <= 0) throw new InvalidOperationException("Rendercomponent " + componentId + " mist positieve waarde " + key + ".");
            return value;
        }

        private static double Number(Dictionary<string, object> row, string key)
        {
            object raw;
            if (!row.TryGetValue(key, out raw) || raw == null || string.IsNullOrWhiteSpace(Convert.ToString(raw, CultureInfo.InvariantCulture))) return 0;
            return Convert.ToDouble(raw, CultureInfo.InvariantCulture);
        }

        private static bool Boolean(Dictionary<string, object> row, string key)
        {
            object raw;
            if (!row.TryGetValue(key, out raw) || raw == null) return false;
            if (raw is bool) return (bool)raw;
            bool result;
            return bool.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), out result) && result;
        }
    }
}
