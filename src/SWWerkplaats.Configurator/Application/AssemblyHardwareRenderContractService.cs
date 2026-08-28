using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    /// <summary>
    /// Keeps all technical dimensions used by the standard-connector renderer on the
    /// backend. Values that are not yet present in masterdata are deliberately marked
    /// provisional and must never drive CAM, purchasing or release checks.
    /// </summary>
    public sealed class AssemblyHardwareRenderContractService
    {
        private readonly MasterDataRuntimeCatalog masterData;

        public AssemblyHardwareRenderContractService()
            : this(MasterDataRuntimeCatalog.LoadRequired())
        {
        }

        internal AssemblyHardwareRenderContractService(MasterDataRuntimeCatalog masterData)
        {
            this.masterData = masterData ?? throw new ArgumentNullException("masterData");
        }

        public AssemblyInstructionHardwareRenderGeometry Build(AssemblyConnection connection)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (connection.JointType != AssemblyJointType.StandardConnector) return null;
            if (connection.FastenerThreadMm <= 0)
                throw new InvalidOperationException("Hardware-rendercontract mist de boutdiameter voor " + connection.ConnectionId + ".");
            var connector = Component(connection.ConnectorId);
            var fastener = Component(connection.FastenerId);
            var contract = new AssemblyInstructionHardwareRenderGeometry
            {
                ContractVersion = 1,
                ConnectorId = connection.ConnectorId,
                FastenerId = connection.FastenerId,
                FastenerHeadStyle = "button-head-hex-socket",
                ConnectorPlateThicknessMm = OptionalDouble(connector, "Connectorplaatdikte mm"),
                ConnectorPlateWidthMm = OptionalDouble(connector, "Connectorplaatbreedte mm"),
                ConnectorPlateHeightMm = OptionalDouble(connector, "Connectorplaathoogte mm"),
                ConnectorJawLengthMm = OptionalDouble(connector, "Klauwlengte mm"),
                ConnectorJawWidthMm = OptionalDouble(connector, "Klauwbreedte mm"),
                ConnectorJawHeightMm = OptionalDouble(connector, "Klauwhoogte mm"),
                ConnectorJawSpacingMm = OptionalDouble(connector, "Klauwhartafstand mm"),
                ConnectorCenterFromProfileEndMm = OptionalDouble(connector, "Connectorhart vanaf profieleinde mm"),
                ShankCenterFromProfileEndMm = OptionalDouble(connector, "Schachthart vanaf profieleinde mm"),
                HeadCenterFromProfileEndMm = OptionalDouble(connector, "Kophart vanaf profieleinde mm"),
                InsertionTravelMm = OptionalDouble(connector, "Insteekweg mm"),
                BoltShankDiameterMm = OptionalDouble(fastener, "Draad Ø mm") ?? connection.FastenerThreadMm,
                BoltShankLengthMm = OptionalDouble(fastener, "Boutschachtlengte mm"),
                BoltHeadDiameterMm = OptionalDouble(fastener, "Boutkopdiameter mm"),
                BoltHeadHeightMm = OptionalDouble(fastener, "Boutkophoogte mm"),
                SocketAcrossFlatsMm = OptionalDouble(fastener, "Inbus SW mm")
            };
            AddOpen(contract.OpenData, connector, connection.ConnectorId);
            AddOpen(contract.OpenData, fastener, connection.FastenerId);
            if (!contract.ConnectorPlateThicknessMm.HasValue) contract.OpenData.Add(connection.ConnectorId + ": Connectorplaatdikte mm ontbreekt.");
            if (!contract.ConnectorJawLengthMm.HasValue || !contract.ConnectorJawWidthMm.HasValue
                || !contract.ConnectorJawHeightMm.HasValue || !contract.ConnectorJawSpacingMm.HasValue)
                contract.OpenData.Add(connection.ConnectorId + ": afzonderlijke klauwgeometrie en/of klauwhartafstand ontbreken.");
            if (!contract.ConnectorCenterFromProfileEndMm.HasValue || !contract.ShankCenterFromProfileEndMm.HasValue
                || !contract.HeadCenterFromProfileEndMm.HasValue || !contract.InsertionTravelMm.HasValue)
                contract.OpenData.Add(connection.ConnectorId + ": hartposities en/of insteekweg ontbreken.");
            if (!contract.BoltShankLengthMm.HasValue || !contract.BoltHeadDiameterMm.HasValue
                || !contract.BoltHeadHeightMm.HasValue || !contract.SocketAcrossFlatsMm.HasValue)
                contract.OpenData.Add(connection.FastenerId + ": exacte boutlengte, kop- of inbusgeometrie ontbreekt.");
            contract.Status = contract.OpenData.Count == 0 ? "ExactSupplierGeometry" : "ProvisionalRenderEnvelope";
            return contract;
        }

        public double RequiredConnectorAccessHoleDiameterMm(string connectorId)
        {
            return RequiredDouble(Component(connectorId), "Toegangsgatdiameter mm");
        }

        public double RequiredFastenerSocketAcrossFlatsMm(string fastenerId)
        {
            return RequiredDouble(Component(fastenerId), "Inbus SW mm");
        }

        public double RequiredFastenerThreadDiameterMm(string fastenerId)
        {
            return RequiredDouble(Component(fastenerId), "Draad Ø mm");
        }

        private Dictionary<string, string> Component(string id)
        {
            var row = masterData.Records("components").FirstOrDefault(record =>
                string.Equals(MasterDataRuntimeCatalog.Value(record, "Component-ID"), id, StringComparison.OrdinalIgnoreCase));
            if (row == null) throw new InvalidOperationException("Hardwarecomponent ontbreekt in masterdata: " + id);
            return row;
        }

        private static double? OptionalDouble(Dictionary<string, string> row, string field)
        {
            var raw = MasterDataRuntimeCatalog.Value(row, field);
            if (string.IsNullOrWhiteSpace(raw)) return null;
            double value;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || value <= 0)
                throw new InvalidOperationException("Hardwaremasterdata bevat geen geldige positieve waarde voor " + field + ".");
            return value;
        }

        private static double RequiredDouble(Dictionary<string, string> row, string field)
        {
            var value = OptionalDouble(row, field);
            if (!value.HasValue) throw new InvalidOperationException("Hardwaremasterdata mist " + field + ".");
            return value.Value;
        }

        private static void AddOpen(ICollection<string> target, Dictionary<string, string> row, string id)
        {
            var value = MasterDataRuntimeCatalog.Value(row, "Open hardwaregeometrie").Trim();
            if (value.Length > 0) target.Add(id + ": " + value);
        }
    }
}
