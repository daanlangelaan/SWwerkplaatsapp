using System;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Portal
{
    public sealed class PortalConfigurationFactory
    {
        private readonly ICatalogRepository catalog;

        public PortalConfigurationFactory()
            : this(new LibraryCatalogRepository())
        {
        }

        public PortalConfigurationFactory(ICatalogRepository catalog)
        {
            this.catalog = catalog ?? new LibraryCatalogRepository();
        }

        public WorkbenchConfig BuildWorkbench(PortalQuoteRequest request)
        {
            var width = Clamp(request.WidthMm, 300, 3020, ProductDefaults.WorkbenchWidthMm);
            var depth = Clamp(request.DepthMm, 300, 1520, ProductDefaults.WorkbenchDepthMm);
            var height = Clamp(request.HeightMm, 300, 2400, ProductDefaults.WorkbenchHeightMm);
            var profileMaterialId = string.IsNullOrWhiteSpace(request.ProfileMaterialId)
                ? ProductDefaultProfileId("werktafel")
                : request.ProfileMaterialId;
            var topSheet = CloneMaterial(FindSheet(request.SheetMaterialId));
            topSheet.ThicknessMm = topSheet.ThicknessMm <= 0 ? 18 : topSheet.ThicknessMm;

            var fastener = ProductFastener(request.Product, "werktafel", false);
            return new WorkbenchConfig
            {
                ProjectName = "Werktafel_" + width.ToString("0") + "x" + depth.ToString("0") + "x" + height.ToString("0"),
                WidthMm = width,
                DepthMm = depth,
                HeightMm = height,
                FrameProfile = CloneMaterial(FindProfile(profileMaterialId)),
                TopSheet = topSheet,
                ShelfSheet = CloneMaterial(FindSheet(request.SheetMaterialId)),
                IncludeLowerFrame = true,
                LowerFrameHeightMm = Clamp(request.LowerShelfHeightMm, 80, height - topSheet.ThicknessMm - 80, 180),
                IncludeLowerShelf = request.IncludeLowerShelf,
                IncludeMiddleLayer = request.IncludeMiddleShelf,
                MiddleLayerHeightMm = Clamp(request.MiddleShelfHeightMm, 120, height - topSheet.ThicknessMm - 60, 450),
                IncludeMiddleShelf = request.IncludeMiddleShelf,
                ShelfCornerClearanceMm = 2,
                BoltMaxSpacingMm = 300,
                TopOverhangFrontMm = 0,
                TopOverhangBackMm = 0,
                TopOverhangLeftMm = 0,
                TopOverhangRightMm = 0,
                SheetFastener = fastener,
                ConnectorHoleDiameterMm = fastener.ClearanceHoleDiameterMm,
                CountersinkSheetHoles = true,
                CountersinkDiameterMm = fastener.CounterboreDiameterMm,
                CountersinkDepthMm = fastener.CounterboreDepthMm,
                AutoTabs = true,
                SmallPartAreaThresholdMm2 = 300 * 300,
                TabWidthMm = 8,
                TabHeightMm = 1.5,
                SawingMode = ParseSawingMode(request.ProfileSawingMode)
            };
        }

        public MachineBaseConfig BuildMachineBase(PortalQuoteRequest request)
        {
            request = request ?? new PortalQuoteRequest();
            var master = MachineBaseMasterDataSettings.LoadRequired();
            var width = Clamp(request.WidthMm, master.MinimumWidthMm, master.MaximumWidthMm, ProductDefaults.MachineBaseWidthMm);
            var depth = Clamp(request.DepthMm, master.MinimumDepthMm, master.MaximumDepthMm, ProductDefaults.MachineBaseDepthMm);
            var height = Clamp(request.HeightMm, master.MinimumHeightMm, master.MaximumHeightMm, ProductDefaults.MachineBaseHeightMm);
            var worktopHeight = Clamp(request.MachineBaseWorktopHeightMm, 600, 1000, ProductDefaults.MachineBaseWorktopHeightMm);
            if (height < worktopHeight + 80) height = Math.Min(master.MaximumHeightMm, worktopHeight + 80);
            var worktopMaterialId = master.ResolveWorktopMaterial(request.MachineBaseWorktopMaterialId);
            var worktopMaterial = CloneMaterial(FindSheet(worktopMaterialId));
            return new MachineBaseConfig
            {
                ProjectName = "MACHINEBASIS_" + width.ToString("0") + "x" + depth.ToString("0") + "x" + height.ToString("0"),
                WidthMm = width,
                DepthMm = depth,
                HeightMm = height,
                WorktopHeightMm = worktopHeight,
                ReservedWorktopThicknessMm = worktopMaterial.ThicknessMm,
                WorktopMaterial = worktopMaterial,
                UprightProfile = CloneMaterial(FindProfile(master.PrimaryProfileId)),
                LowerBeamProfile = CloneMaterial(FindProfile(master.ResolveLowerBeamProfile(request.MachineBaseLowerBeamProfileId))),
                WorktopBeamProfile = CloneMaterial(FindProfile(master.PrimaryProfileId)),
                WorktopIntermediateBeamMaxSpacingMm = Clamp(request.MachineBaseWorktopIntermediateBeamMaxSpacingMm, 300, 1000, ProductDefaults.MachineBaseWorktopIntermediateBeamMaxSpacingMm),
                TopBeamProfile = CloneMaterial(FindProfile(master.TopFrameProfileId)),
                LowerPanelMaterial = CloneMaterial(FindSheet(master.LowerPanelMaterialId)),
                UpperPanelMaterial = CloneMaterial(FindSheet(master.UpperPanelMaterialId)),
                FrontProtectionMode = string.Equals(request.MachineBaseFrontProtectionMode, "lightcurtain", StringComparison.OrdinalIgnoreCase) ? "lightcurtain" : "doors",
                ControlCabinetWidthMm = Clamp(request.MachineBaseControlCabinetWidthMm, 300, width - 160, ProductDefaults.MachineBaseControlCabinetWidthMm),
                ControlCabinetDepthMm = Clamp(request.MachineBaseControlCabinetDepthMm, 200, depth - 120, ProductDefaults.MachineBaseControlCabinetDepthMm),
                ControlCabinetHeightMm = Clamp(request.MachineBaseControlCabinetHeightMm, 300, worktopHeight - 180, ProductDefaults.MachineBaseControlCabinetHeightMm),
                ControlCabinetPosition = string.Equals(request.MachineBaseControlCabinetPosition, "right", StringComparison.OrdinalIgnoreCase) ? "right" : "left",
                ControlCabinetDoorCount = request.MachineBaseControlCabinetDoorCount == 1 ? 1 : 2,
                ControlCabinetHingeSide = string.Equals(request.MachineBaseControlCabinetHingeSide, "right", StringComparison.OrdinalIgnoreCase) ? "right" : "left",
                FrontDoorCount = request.MachineBaseFrontDoorCount == 1 ? 1 : 2,
                FrontSingleDoorHingeSide = string.Equals(request.MachineBaseFrontSingleDoorHingeSide, "right", StringComparison.OrdinalIgnoreCase) ? "right" : "left",
                SawingMode = ParseSawingMode(request.ProfileSawingMode)
            };
        }

        public RobotCellConfig BuildRobotCell(PortalQuoteRequest request)
        {
            request = request ?? new PortalQuoteRequest();
            var width = Clamp(request.WidthMm, 600, 6000, ProductDefaults.RobotCellWidthMm);
            var depth = Clamp(request.DepthMm, 500, 2000, ProductDefaults.RobotCellDepthMm);
            var worktopHeight = Clamp(request.HeightMm, 650, 1200, ProductDefaults.RobotCellWorktopHeightMm);
            return new RobotCellConfig
            {
                ProjectName = "ROBOTCEL_" + width.ToString("0") + "x" + depth.ToString("0") + "x" + worktopHeight.ToString("0"),
                WidthMm = width,
                DepthMm = depth,
                WorktopHeightMm = worktopHeight,
                IntermediateBeamMaxSpacingMm = Clamp(request.RobotCellIntermediateBeamMaxSpacingMm, 300, 1000, ProductDefaults.RobotCellIntermediateBeamMaxSpacingMm),
                UprightProfile = CloneMaterial(FindProfile("alu_system_80x80")),
                FrameBeamProfile = CloneMaterial(FindProfile("alu_system_80x40")),
                RearRailProfile = CloneMaterial(FindProfile("alu_system_160x40")),
                WorktopMaterial = CloneMaterial(FindSheet("hpl_10_lex")),
                SawingMode = ParseSawingMode(request.ProfileSawingMode)
            };
        }

        public LinearRobotCellConfig BuildLinearRobotCell(PortalQuoteRequest request)
        {
            request = request ?? new PortalQuoteRequest();
            var master = LinearRobotCellMasterDataSettings.LoadRequired();
            var length = Clamp(request.WidthMm, master.MinimumLengthMm, master.MaximumLengthMm, master.DefaultLengthMm);
            var worktopDepth = Clamp(request.DepthMm, master.MinimumWorktopDepthMm, master.MaximumWorktopDepthMm, master.DefaultWorktopDepthMm);
            var worktopHeight = Clamp(request.HeightMm, master.MinimumWorktopHeightMm, master.MaximumWorktopHeightMm, master.DefaultWorktopHeightMm);
            var sideCount = request.LinearRobotCellWorktopSideCount == 2 ? 2 : master.DefaultWorktopSideCount;
            var guardHeight = Clamp(request.LinearRobotCellGuardHeightAboveWorktopMm, master.MinimumGuardHeightMm, master.MaximumGuardHeightMm, master.DefaultGuardHeightMm);
            var lightCurtain = master.SelectLightCurtain(guardHeight);
            return new LinearRobotCellConfig
            {
                ProjectName = "LINEAIRE_ROBOTCEL_" + length.ToString("0") + "x" + worktopDepth.ToString("0") + "x" + worktopHeight.ToString("0") + "_" + sideCount + "Z",
                LengthMm = length,
                WorktopDepthMm = worktopDepth,
                WorktopHeightMm = worktopHeight,
                WorktopSideCount = sideCount,
                GuardHeightAboveWorktopMm = guardHeight,
                IntermediateSupportMaxSpacingMm = Clamp(request.LinearRobotCellIntermediateSupportMaxSpacingMm, master.MinimumSupportSpacingMm, master.MaximumSupportSpacingMm, master.DefaultSupportSpacingMm),
                RailZoneWidthMm = master.RailZoneWidthMm,
                RailCenterSpacingMm = master.RailCenterSpacingMm,
                RailWidthMm = master.RailEnvelope[0],
                RailHeightMm = master.RailEnvelope[1],
                CarriageLengthMm = master.CarriageEnvelope[0],
                CarriageWidthMm = master.CarriageEnvelope[1],
                CarriageHeightMm = master.CarriageEnvelope[2],
                RobotAdapterLengthMm = master.RobotAdapterEnvelope[0],
                RobotAdapterWidthMm = master.RobotAdapterEnvelope[1],
                RobotAdapterThicknessMm = master.RobotAdapterEnvelope[2],
                MotorAdapterLengthMm = master.MotorAdapterEnvelope[0],
                MotorAdapterHeightMm = master.MotorAdapterEnvelope[1],
                MotorAdapterThicknessMm = master.MotorAdapterEnvelope[2],
                RackWidthMm = master.RackEnvelope[0],
                RackHeightMm = master.RackEnvelope[1],
                FootVisibleHeightMm = master.SupportEnvelope[0],
                FootDiameterMm = master.SupportEnvelope[1],
                FootPlateThicknessMm = master.SupportEnvelope[2],
                LowerFrameEndCrossmembersUseOuterLane = master.LowerFrameEndCrossmembersUseOuterLane,
                TwoSidedEndWallIntermediatePostCount = master.TwoSidedEndWallIntermediatePostCount,
                ThroughCornerUprightCount = master.ThroughCornerUprightCount,
                TwoSidedCenterSupportRowCount = master.TwoSidedCenterSupportRowCount,
                LightCurtainSetComponentId = lightCurtain.SetComponentId,
                LightCurtainEmitterComponentId = lightCurtain.EmitterComponentId,
                LightCurtainReceiverComponentId = lightCurtain.ReceiverComponentId,
                LightCurtainDisplayName = lightCurtain.DisplayName,
                LightCurtainArticleNumber = lightCurtain.ArticleNumber,
                LightCurtainProtectedHeightMm = lightCurtain.ProtectedHeightMm,
                LightCurtainWidthMm = lightCurtain.WidthMm,
                LightCurtainOverallHeightMm = lightCurtain.OverallHeightMm,
                LightCurtainDepthMm = lightCurtain.DepthMm,
                UprightProfile = CloneMaterial(FindProfile(master.UprightProfileId)),
                FrameBeamProfile = CloneMaterial(FindProfile(master.FrameBeamProfileId)),
                RailCarrierProfile = CloneMaterial(FindProfile(master.RailCarrierProfileId)),
                GuardProfile = CloneMaterial(FindProfile(master.GuardProfileId)),
                WorktopMaterial = CloneMaterial(FindSheet(master.WorktopMaterialId)),
                GuardPanelMaterial = CloneMaterial(FindSheet(master.GuardPanelMaterialId)),
                SawingMode = ParseSawingMode(request.ProfileSawingMode)
            };
        }

        public MaterialCartConfig BuildMaterialCart(PortalQuoteRequest request)
        {
            request = request ?? new PortalQuoteRequest();
            var width = Clamp(request.WidthMm, 600, 1800, ProductDefaults.MaterialCartWidthMm);
            var depth = Clamp(request.DepthMm, 450, 1000, ProductDefaults.MaterialCartDepthMm);
            var height = Clamp(request.HeightMm, 700, 1200, ProductDefaults.MaterialCartTopShelfHeightMm);
            var shelfCount = request.MaterialCartShelfCount < 2 || request.MaterialCartShelfCount > 4 ? ProductDefaults.MaterialCartShelfCount : request.MaterialCartShelfCount;
            var shelfMaterialId = string.IsNullOrWhiteSpace(request.MaterialCartShelfMaterialId) ? "hpl_10_lex" : request.MaterialCartShelfMaterialId;
            return new MaterialCartConfig
            {
                ProjectName = "MATERIAALWAGEN_" + width.ToString("0") + "x" + depth.ToString("0") + "x" + height.ToString("0"),
                WidthMm = width,
                DepthMm = depth,
                TopShelfHeightMm = height,
                ShelfCount = shelfCount,
                HandleSide = string.Equals(request.MaterialCartHandleSide, "none", StringComparison.OrdinalIgnoreCase) ? "none" : string.Equals(request.MaterialCartHandleSide, "left", StringComparison.OrdinalIgnoreCase) ? "left" : "right",
                SteeringMode = string.Equals(request.MaterialCartSteeringMode, "four-swivel", StringComparison.OrdinalIgnoreCase) ? "four-swivel" : "fixed-and-swivel",
                FrameProfile = CloneMaterial(FindProfile("alu_system_40x40")),
                ShelfMaterial = CloneMaterial(FindSheet(shelfMaterialId)),
                SawingMode = ParseSawingMode(request.ProfileSawingMode)
            };
        }

        public SimRigConfig BuildSimRig(PortalQuoteRequest request)
        {
            request = request ?? new PortalQuoteRequest();
            var width = Clamp(request.WidthMm, 600, 800, ProductDefaults.SimRigOutsideWidthMm);
            var length = Clamp(request.DepthMm, 1200, 1800, ProductDefaults.SimRigLengthMm);
            var height = Clamp(request.HeightMm, 550, 850, ProductDefaults.SimRigSteeringBridgeHeightMm);
            var bridgePosition = Clamp(request.SimRigSteeringBridgePositionMm, 350, length - 350, ProductDefaults.SimRigSteeringBridgePositionMm);
            var pedalPosition = Clamp(request.SimRigPedalDeckPositionMm, 180, bridgePosition - 180, ProductDefaults.SimRigPedalDeckPositionMm);
            return new SimRigConfig
            {
                ProjectName = "SIMRIG_4080_" + length.ToString("0") + "x" + width.ToString("0") + "x" + height.ToString("0"),
                OutsideWidthMm = width,
                LengthMm = length,
                SteeringBridgeHeightMm = height,
                SteeringBridgePositionMm = bridgePosition,
                PedalDeckPositionMm = pedalPosition,
                PedalAngleDeg = Math.Max(0, Math.Min(25, request.SimRigPedalAngleDeg)),
                WheelMountPattern = string.Equals(request.SimRigWheelMountPattern, "blank", StringComparison.OrdinalIgnoreCase) ? "blank" : "csl-dd",
                Profile4080 = CloneMaterial(FindProfile("alu_system_80x40")),
                AdapterPlateMaterial = new Material { Id = "steel_s235_10_custom", Name = "S235 staalplaat 10 mm custom", Kind = MaterialKind.Sheet, ThicknessMm = 10, SheetLengthMm = 2000, SheetWidthMm = 1000 },
                SawingMode = ParseSawingMode(request.ProfileSawingMode)
            };
        }

        public ShippingBoxConfig BuildShippingBox(PortalQuoteRequest request)
        {
            request = request ?? new PortalQuoteRequest();
            var internalWidth = Clamp(request.WidthMm, 200, 6000, ProductDefaults.ShippingBoxInternalWidthMm);
            var internalDepth = Clamp(request.DepthMm, 200, 3000, ProductDefaults.ShippingBoxInternalDepthMm);
            var internalHeight = Clamp(request.HeightMm, 200, 3000, ProductDefaults.ShippingBoxInternalHeightMm);
            var materialId = string.IsNullOrWhiteSpace(request.SheetMaterialId) ? ProductDefaults.ShippingBoxDefaultMaterialId : request.SheetMaterialId;
            var material = CloneMaterial(FindSheet(materialId));
            return new ShippingBoxConfig
            {
                ProjectName = "Shipping_box_" + internalWidth.ToString("0") + "x" + internalDepth.ToString("0") + "x" + internalHeight.ToString("0") + "_binnen",
                InternalWidthMm = internalWidth,
                InternalDepthMm = internalDepth,
                InternalHeightMm = internalHeight,
                PanelMaterial = material,
                Clip = ProductDefaults.ShippingBoxCrateClip(),
                JointMode = string.Equals(request.ShippingBoxJointMode, "localized_tabs", StringComparison.OrdinalIgnoreCase) ? "localized_tabs" : "rabbet",
                IncludeHandles = request.ShippingBoxIncludeHandles,
                HandleLengthMm = 140,
                HandleHeightMm = 40,
                HandleCenterHeightRatio = 0.68,
                RabbetClearanceMm = 0.4,
                RabbetDepthFactor = 0.5
            };
        }

        public LexWorkbenchConfig BuildLexWorkbench(PortalQuoteRequest request)
        {
            request = request ?? new PortalQuoteRequest();
            var width = Clamp(request.WidthMm, 1200, 2200, ProductDefaults.LexWorkbenchWidthMm);
            var depth = Clamp(request.DepthMm, 700, 1400, ProductDefaults.LexWorkbenchDepthMm);
            var height = Clamp(request.HeightMm, ProductDefaults.LexWorkbenchHeightMm, ProductDefaults.LexWorkbenchHeightMm + 400, ProductDefaults.LexWorkbenchHeightMm);
            var motionSettings = LexMotionMasterDataSettings.LoadRequired();
            var config = new LexWorkbenchConfig
            {
                ProductVariant = "lex_standard",
                ProjectName = "WORKSTATION_" + width.ToString("0") + "x" + depth.ToString("0") + "x" + height.ToString("0"),
                WidthMm = width,
                DepthMm = depth,
                HeightMm = height,
                ColumnCenterDistanceMm = ProductDefaults.LexWorkbenchColumnCenterDistanceMm,
                ColumnBaseHeightMm = 75,
                StabilizationPlateWidthMm = ProductDefaults.LexWorkbenchColumnCenterDistanceMm + ProductDefaults.LexHte2LiftColumn().BodyDepthMm,
                StabilizationPlateHeightMm = 240,
                RailCenterDistanceMm = 700,
                WorktopCenterSupportOffsetMm = 70,
                FixedRailFrameWidthMm = ProductDefaults.LexWorkbenchColumnCenterDistanceMm + 80,
                FixedRailFrameDepthMm = 780,
                CarriageCenterDistanceMm = ProductDefaults.LexWorkbenchColumnCenterDistanceMm,
                CarriageAdapterLengthMm = 80,
                CarriageAdapterWidthMm = 80,
                CarriageAdapterThicknessMm = 10,
                CarriageAdapterClearanceHoleDiameterMm = 4.5,
                CarriageAdapterSlotLengthMm = 20,
                CarriageAdapterSlotWidthMm = 9,
                CarriageAdapterSlotPitchMm = 40,
                CarriageAdapterProfileGrooveOffsetMm = 20,
                BallTransferHoleDiameterMm = 30.1,
                BallTransferBodyDiameterMm = 30.01,
                BallTransferFlangeDiameterMm = 32,
                BallTransferFlangeThicknessMm = 2,
                BallTransferFlangeRecessDiameterMm = 32.1,
                BallTransferFlangeRecessDepthMm = 2,
                BallTransferInsertionLengthMm = 17,
                BallTransferBallDiameterMm = 19.05,
                BallTransferWorkingHeightMm = 6.3,
                SawingMode = ParseSawingMode(request.ProfileSawingMode),
                Profile80x80 = CloneMaterial(FindProfile("alu_system_80x80")),
                Profile40x40 = CloneMaterial(FindProfile("alu_system_40x40")),
                TopSheet = CloneMaterial(FindSheet("hpl_10_lex")),
                StabilizationSheet = CloneMaterial(FindSheet("hpl_6_lex_stabilizer")),
                CarriageAdapterSheet = CloneMaterial(FindSheet("pa_cf_print_10_adapter")),
                LinearGuide = ProductDefaults.LexHsr15LinearGuide(),
                LiftColumn = ProductDefaults.LexHte2LiftColumn(),
                LevelingFootCornerAdapter = ProductDefaults.LexLevelingFootCornerAdapter(),
                LevelingFoot = ProductDefaults.LexLevelingFoot(),
                SwingLatch = new SwingLatchCatalog().LoadRequired()
            };
            config.HorizontalLockPositionsMm.AddRange(motionSettings.HorizontalLockPositionsMm);
            config.MovingFrameSlotAxisEdgeOffsetMm = new ProfileSlotGeometryCatalog().FindRequired(config.Profile40x40.Id).EdgeOffsetMm;
            config.DesignNotes.Add("Basisontwerp v0.1: rollenbaan vervallen; een afzonderlijk eigen maakdeel in HPL 6 mm vervangt het eerdere 240 mm verbindingsprofiel. Deze tussenplaat is niet inbegrepen bij de HTE2-hefset.");
            config.DesignNotes.Add("HSR15 is als systeemcode vastgelegd: de vier HSR15R-wagens 34x56,6x28 staan per rail op 890 mm h.o.h., exact boven de HTE2-poot- en vaste zijframeharten. Twee voorraadrails 1500x15x15 worden voor de vrijgegeven slag ±220 mm symmetrisch op 1440 mm afgezaagd; 24 patroonboringen blijven behouden en de mechanische eindstopharten liggen op X=±699,3 mm.");
            config.DesignNotes.Add("Kogelpot geselecteerd als VCN310-30: D1 30 mm, flens D 32 mm, huislengte L 17 mm, flenshoogte H 2 mm, kogeltophoogte L1 6,3 mm en hoofdkogel d 19,05 mm; leveranciersbelasting 412 N bij rechtop gebruik. Montagepassing en borging in HPL met een proefexemplaar vrijgeven.");
            config.DesignNotes.Add("De verticale stabilisatieplaat ligt tegen de achterste kopse zijde van de 160 mm diepe, 90 graden gedraaide HTE2-kolommen en loopt over 955 mm door tot de beide buitenzijden van de 65 mm brede kolommen.");
            config.DesignNotes.Add("HSR15-rails zijn ondersteboven tegen de onderzijde van twee raildragers in het bewegende werkbladframe gemonteerd; railgaten liggen op de profielgroef en gebruiken verschuifbare M4-schuifmoeren. Boutlengte volgt uit raildoorvoer plus beschikbare draadzone en stopt vóór de sleufbodem.");
            config.DesignNotes.Add("Vier 3D-geprinte PA-CF adapterplaten 80x80x10, 100% infill, koppelen de omgekeerde HSR15R-wagens aan het vaste 80x80-frame: wagenpatroon 26x26 en twee sleufgaten in de profielgroeven op 20 en 60 mm, dus 40 mm hartafstand. Naar het profiel worden M8-inklikmoeren met verende kogel gebruikt.");
            config.DesignNotes.Add("De bewegende werkbladhouder heeft een gesloten rechthoekige buitencontour van groef-8 40x40-profielen. Binnen die contour liggen drie 1570 mm lange 40x40-liggers tussen de linker- en rechterzijprofielen op Z=-350, +70 en +350 mm; de buitenste twee zijn tevens raildrager. De middenligger ligt daarmee tussen de kogelpotrijen op Z=0 en Z=+140 mm, zodat de circa 8,2 mm uitstekende kogelpotbehuizingen vrij blijven.");
            config.DesignNotes.Add("Alle werkelijk blootliggende profielkoppen krijgen zwarte PA-GF serie-8 afdekkappen: 4x 80x80 op het vaste railframe en 4x TechXXL TIN 100184 40x40 op het bewegende buitenframe. De vier voetprofielkoppen worden volledig afgedekt door de ZI-1744 hoekadapters en krijgen daarom geen losse kap.");
            config.DesignNotes.Add("Vier Maunsystem ZI-1744 hoekadapters met M16-opname dragen ZI-1415-S stelvoeten D80/M16x130. De voetprofielen worden tussen de adapters ingekort; adapter, voet en blad blijven samen exact binnen de diepte-envelope van " + depth.ToString("0.##") + " mm.");
            config.DesignNotes.Add("Zes Item Schwenkriegel 8 Pi 54 PA (0.0.700.81) vormen draaibare werkstukaanslagen: twee per lange zijde en één per korte zijde. De 90-graden raststanden maken iedere werkbladhoek bruikbaar tegen twee aanslagen; externe maten komen uit Item-masterdata.");
            return config;
        }

        public LexWorkbenchConfig BuildLexRevolutionWorkbench(PortalQuoteRequest request)
        {
            var config = BuildLexWorkbench(request);
            config.ProductVariant = "lex_revolution";
            config.ProjectName = config.ProjectName.Replace("WORKSTATION_", "WORKSTATION_ONTWIKKELVARIANT_");
            config.DesignNotes.Insert(0, "LEX Revolution ontwikkelvariant: zelfstandige productroute op basis van de offerbare LEX-revisie. De startgeometrie, stuklijst en calculatie zijn bij aanmaak bewust gelijk; nieuwe slimme oplossingen worden alleen in deze variant doorontwikkeld.");
            return config;
        }

        public HeightAdjustableWorkbenchConfig BuildHeightAdjustableWorkbench(PortalQuoteRequest request)
        {
            request = request ?? new PortalQuoteRequest();
            var master = HeightAdjustableWorkbenchMasterDataSettings.LoadRequired();
            var width = Clamp(request.WidthMm, master.MinimumWidthMm, master.MaximumWidthMm, master.DefaultWidthMm);
            var depth = Clamp(request.DepthMm, master.MinimumDepthMm, master.MaximumDepthMm, master.DefaultDepthMm);
            var height = Clamp(request.HeightMm, master.MinimumHeightMm, master.MaximumHeightMm, master.DefaultHeightMm);
            var worktopMaterialId = master.ResolveWorktopMaterial(request.SheetMaterialId);

            var config = new HeightAdjustableWorkbenchConfig
            {
                ProjectName = "HOOGTEVERSTELBARE_WERKTAFEL_" + width.ToString("0") + "x" + depth.ToString("0") + "x" + height.ToString("0"),
                WidthMm = width,
                DepthMm = depth,
                HeightMm = height,
                ColumnCenterDistanceMm = ProductDefaults.LexWorkbenchColumnCenterDistanceMm,
                StabilizationPlateWidthMm = ProductDefaults.LexWorkbenchColumnCenterDistanceMm + ProductDefaults.LexHte2LiftColumn().BodyDepthMm,
                StabilizationPlateHeightMm = 240,
                FootProfile = CloneMaterial(FindProfile(master.FootProfileId)),
                TopFrameProfile = CloneMaterial(FindProfile(master.ResolveTopFrameProfile(request.ProfileMaterialId))),
                TopSheet = CloneMaterial(FindSheet(worktopMaterialId)),
                StabilizationSheet = CloneMaterial(FindSheet(master.StabilizationMaterialId)),
                LiftColumn = ProductDefaults.LexHte2LiftColumn(),
                LevelingFootCornerAdapter = ProductDefaults.LexLevelingFootCornerAdapter(),
                LevelingFoot = ProductDefaults.LexLevelingFoot(),
                SawingMode = ParseSawingMode(request.ProfileSawingMode)
            };
            config.DesignNotes.Add("Onderstel hergebruikt de Workstation-keten: twee 80x80-voetprofielen, twee HTE2 O1-hefkolommen, vier ZI-1744-hoekadapters met ZI-1415-S-stelvoeten en de vaste HPL-stabilisatieplaat.");
            config.DesignNotes.Add("Vaste bovenlaag: vier randprofielen en exact twee aansluitprofielen op de HTE2-kolomharten. Keuze 40x40 of 40x80 staand; het gekozen werkblad is vast en beweegt uitsluitend verticaal.");
            config.DesignNotes.Add("Niet aanwezig: kogelpotten, HSR15-rails en -wagens, wagenadapterplaten, horizontale borgposities, eindstops en draaibare werkstukaanslagen.");
            return config;
        }

        public CabinetConfig BuildCabinet(PortalQuoteRequest request)
        {
            request = request ?? new PortalQuoteRequest();
            var width = Clamp(request.WidthMm, 300, 3020, ProductDefaults.CabinetWidthMm);
            var depth = Clamp(request.DepthMm, 250, 1520, ProductDefaults.CabinetDepthMm);
            var height = Clamp(request.HeightMm, 300, 2400, ProductDefaults.CabinetHeightMm);
            var units = (int)Clamp(request.UnitCount, 1, 12, ProductDefaults.CabinetUnitCount);
            var carcass = CloneMaterial(FindSheet(request.SheetMaterialId));
            var drawer = CloneMaterial(FindSheet(string.IsNullOrWhiteSpace(request.DrawerMaterialId) ? ProductDefaults.DefaultDrawerMaterialId : request.DrawerMaterialId));
            var back = CloneMaterial(FindSheet(string.IsNullOrWhiteSpace(request.BackMaterialId) ? ProductDefaults.DefaultBackMaterialId : request.BackMaterialId));
            var slidingDoor = CloneMaterial(FindSheet(string.IsNullOrWhiteSpace(request.SlidingDoorMaterialId) ? ProductDefaults.DefaultSlidingDoorMaterialId : request.SlidingDoorMaterialId));
            var rail = CloneRail(DefaultRail(catalog.DrawerRails()));
            var sheetFastener = ProductFastener(request.Product, "cabinet", true);
            sheetFastener.LengthMm = FastenerSelectionService.SelectWoodToWoodEdgeLength(sheetFastener, carcass.ThicknessMm, Math.Max(depth, height));
            var sliding = string.Equals(request.DoorMode, "sliding", StringComparison.OrdinalIgnoreCase);
            var slidingStart = (int)Clamp(request.SlidingDoorStartUnit, 1, units, 1);
            var slidingEnd = (int)Clamp(request.SlidingDoorEndUnit, 1, units, units);
            if (slidingEnd < slidingStart)
            {
                var tmp = slidingStart;
                slidingStart = slidingEnd;
                slidingEnd = tmp;
            }

            var config = new CabinetConfig
            {
                ProjectName = "Cabinet_" + width.ToString("0") + "x" + depth.ToString("0") + "x" + height.ToString("0"),
                WidthMm = width,
                DepthMm = depth,
                WorktopHeightMm = height,
                UnitCount = units,
                PlinthHeightMm = ProductDefaults.CabinetPlinthHeightMm,
                PlinthDepthMm = ProductDefaults.CabinetPlinthDepthMm,
                IncludeBackPanel = request.IncludeBackPanel,
                CarcassMaterial = carcass,
                WorktopMaterial = CloneMaterial(FindSheet(request.SheetMaterialId)),
                DrawerMaterial = drawer,
                FrontMaterial = CloneMaterial(FindSheet(request.SheetMaterialId)),
                SlidingDoorMaterial = slidingDoor,
                BackMaterial = back,
                SheetFastener = sheetFastener,
                DrawerRail = rail,
                ShelfSupport = CloneShelfSupport(catalog.ShelfSupports()[ProductDefaults.DefaultShelfSupportIndex]),
                IncludeFullWidthTopDrawer = request.IncludeTopDrawer,
                IncludeDrawerPullCutouts = request.IncludeDrawerPullCutouts,
                FullWidthTopDrawerHeightMm = ProductDefaults.FullWidthTopDrawerHeightMm,
                ShelfStartMode = NormalizeShelfStartMode(request.ShelfStartMode),
                IncludeAdjustableShelfHoles = request.IncludeAdjustableShelfHoles,
                AdjustableShelfHoleEndMarginMm = ProductDefaults.AdjustableShelfHoleEndMarginMm,
                AutoTabs = true,
                SmallPartAreaThresholdMm2 = 300 * 300,
                TabWidthMm = 8,
                TabHeightMm = 1.5,
                ShelfClearanceMm = ProductDefaults.ShelfClearanceMm,
                ShelfFrontInsetMm = Clamp(request.ShelfFrontInsetMm, 0, Math.Max(0, depth - 160), ProductDefaults.CabinetShelfFrontInsetMm),
                DrawerSideClearanceMm = Math.Max(13, rail.ThicknessMm),
                DrawerBackClearanceMm = ProductDefaults.DrawerBackClearanceMm,
                DoorGapMm = ProductDefaults.DoorGapMm,
                SlidingDoorStartUnit = slidingStart,
                SlidingDoorEndUnit = slidingEnd,
                SlidingDoorOverlapMm = Clamp(request.SlidingDoorOverlapMm, 0, 120, ProductDefaults.CabinetSlidingDoorOverlapMm),
                SlidingDoorFreeSpaceBehindMm = ProductDefaults.CabinetSlidingDoorFreeSpaceBehindMm,
                SlidingDoorTrackCenterSpacingMm = ProductDefaults.CabinetSlidingDoorTrackCenterSpacingMm,
                SlidingDoorTopProfileHeightMm = ProductDefaults.CabinetSlidingDoorTopProfileHeightMm,
                SlidingDoorBottomProfileHeightMm = ProductDefaults.CabinetSlidingDoorBottomProfileHeightMm,
                SlidingDoorTapeThicknessMm = ProductDefaults.CabinetSlidingDoorTapeThicknessMm,
                SlidingDoorTopProfileDepthMm = ProductDefaults.CabinetSlidingDoorTopProfileDepthMm,
                SlidingDoorBottomProfileDepthMm = ProductDefaults.CabinetSlidingDoorBottomProfileDepthMm
            };

            for (var i = 1; i <= units; i++)
            {
                var unitHasSlidingDoor = sliding && i >= slidingStart && i <= slidingEnd;
                config.Units.Add(new CabinetUnitConfig
                {
                    UnitNumber = i,
                    ShelfCount = Math.Max(0, request.DefaultShelfCount),
                    ShelfHeightsMm = "",
                    DrawerCount = Math.Max(0, request.DefaultDrawerCount),
                    DrawerHeightMm = 160,
                    Door = unitHasSlidingDoor ? CabinetDoorHand.Geen : ParseDoor(request.DoorMode),
                    SlidingDoors = unitHasSlidingDoor,
                    SlidingDoorMaxWidthMm = 600
                });
            }

            return config;
        }

        public WorkbenchCabinetConfig BuildWorkbenchCabinet(PortalQuoteRequest request)
        {
            request = request ?? new PortalQuoteRequest();
            var width = Clamp(request.WidthMm, 600, 3020, ProductDefaults.WorkbenchCabinetWidthMm);
            var depth = Clamp(request.DepthMm, 300, 1520, ProductDefaults.WorkbenchCabinetDepthMm);
            var height = Clamp(request.HeightMm, 500, 2400, ProductDefaults.WorkbenchCabinetHeightMm);
            var units = (int)Clamp(request.UnitCount, 1, 12, ProductDefaults.WorkbenchCabinetUnitCount);
            var carcass = CloneMaterial(FindSheet(request.SheetMaterialId));
            var rail = CloneRail(DefaultRail(catalog.DrawerRails()));
            var shelfSupport = CloneShelfSupport(catalog.ShelfSupports()[ProductDefaults.DefaultShelfSupportIndex]);
            var backMaterial = CloneMaterial(FindSheet(string.IsNullOrWhiteSpace(request.BackMaterialId) ? ProductDefaults.DefaultBackMaterialId : request.BackMaterialId));
            var backThickness = request.IncludeBackPanel ? backMaterial.ThicknessMm : 0.0;
            var maxShelfFrontInset = Math.Max(0, depth - backThickness - shelfSupport.BackInsetMm - shelfSupport.FrontInsetMm - 6.0);
            var shelfFrontInset = Math.Max(0, Math.Min(maxShelfFrontInset, request.ShelfFrontInsetMm));
            var adjustableFoot = ProductDefaults.WorkbenchCabinetAdjustableFoot();
            var sheetFastener = ProductFastener(request.Product, "werkbankkast", true);
            var backPanelSettings = WorkbenchCabinetBackPanelSettings.LoadRequired();
            sheetFastener.LengthMm = FastenerSelectionService.SelectWoodToWoodEdgeLength(sheetFastener, carcass.ThicknessMm, Math.Max(depth, height));
            var minimumFootInset = Math.Max(
                adjustableFoot.FootDiameterMm / 2.0 + 1.0,
                adjustableFoot.MountingBlockWidthMm / 2.0 + 1.0);

            var config = new WorkbenchCabinetConfig
            {
                ProjectName = "Werkbankkast_" + width.ToString("0") + "x" + depth.ToString("0") + "x" + height.ToString("0"),
                WidthMm = width,
                DepthMm = depth,
                WorktopHeightMm = height,
                UnitCount = units,
                PlinthHeightMm = Clamp(request.WorkbenchCabinetPlinthHeightMm, adjustableFoot.MinHeightMm, Math.Min(adjustableFoot.MaxHeightMm, Math.Max(adjustableFoot.MinHeightMm, height - 300)), adjustableFoot.NominalHeightMm),
                PlinthSetbackMm = Clamp(request.WorkbenchCabinetPlinthSetbackMm, 0, Math.Max(0, depth / 2.0 - 1), ProductDefaults.WorkbenchCabinetPlinthSetbackMm),
                PlinthFloorClearanceMm = ProductDefaults.WorkbenchCabinetPlinthFloorClearanceMm,
                IncludeLeftSidePlinth = request.WorkbenchCabinetIncludeLeftSidePlinth,
                IncludeRightSidePlinth = request.WorkbenchCabinetIncludeRightSidePlinth,
                AdjustableFoot = adjustableFoot,
                AdjustableFootInsetMm = Clamp(request.WorkbenchCabinetFootInsetMm, minimumFootInset, Math.Max(minimumFootInset, Math.Min(width, depth) / 2.0 - 1), ProductDefaults.WorkbenchCabinetFootInsetMm),
                PlinthClipCenterBehindBackFaceMm = Clamp(
                    request.WorkbenchCabinetPlinthClipCenterBehindBackFaceMm,
                    5,
                    Math.Max(5, depth / 2.0 - carcass.ThicknessMm - 1),
                    ProductDefaults.WorkbenchCabinetPlinthClipCenterBehindBackFaceMm),
                DoorStopWidthMm = Clamp(request.WorkbenchCabinetDoorStopWidthMm, carcass.ThicknessMm + 4, 240, ProductDefaults.WorkbenchCabinetDoorStopWidthMm),
                DoorGapMm = ProductDefaults.WorkbenchCabinetDoorGapMm,
                DoorToCarcassClearanceMm = ProductDefaults.WorkbenchCabinetDoorToCarcassClearanceMm,
                FrontPanelCornerRadiusMm = request.WorkbenchCabinetFrontPanelCornerRadiusMm.HasValue
                    ? Math.Max(0, Math.Min(25, request.WorkbenchCabinetFrontPanelCornerRadiusMm.Value))
                    : ProductDefaults.WorkbenchCabinetFrontPanelCornerRadiusMm,
                ShelfCountPerUnit = Math.Max(0, Math.Min(20, request.DefaultShelfCount)),
                ShelfStartMode = NormalizeShelfStartMode(string.IsNullOrWhiteSpace(request.ShelfStartMode) ? ProductDefaults.WorkbenchCabinetDefaultShelfStartMode : request.ShelfStartMode),
                ShelfClearanceMm = ProductDefaults.WorkbenchCabinetShelfClearanceMm,
                ShelfFrontInsetMm = shelfFrontInset,
                AdjustableShelfHoleEndMarginMm = ProductDefaults.WorkbenchCabinetAdjustableShelfHoleEndMarginMm,
                AdjustableShelfPositionCount = Math.Max(
                    Math.Max(0, Math.Min(20, request.DefaultShelfCount)),
                    request.AdjustableShelfPositionCount > 0
                        ? Math.Min(20, request.AdjustableShelfPositionCount)
                        : ProductDefaults.WorkbenchCabinetAdjustableShelfPositionCount),
                IncludeAdjustableShelfHoles = request.IncludeAdjustableShelfHoles,
                IncludeTopDrawer = request.IncludeTopDrawer,
                IncludeDrawerPullCutouts = request.IncludeDrawerPullCutouts,
                TopDrawerHeightMm = Clamp(request.WorkbenchCabinetTopDrawerHeightMm, 100, 320, ProductDefaults.WorkbenchCabinetTopDrawerHeightMm),
                DrawerSideClearanceMm = Math.Max(13, rail.ThicknessMm),
                DrawerBackClearanceMm = ProductDefaults.WorkbenchCabinetDrawerBackClearanceMm,
                IncludeBackPanel = request.IncludeBackPanel,
                BackPanelGrooveDepthMm = backPanelSettings.GrooveDepthMm,
                BackPanelGrooveClearanceMm = backPanelSettings.GrooveClearanceMm,
                BackPanelFastenerEndInsetMm = backPanelSettings.FastenerEndInsetMm,
                BackPanelFastenerMaxSpacingMm = backPanelSettings.FastenerMaxSpacingMm,
                CarcassMaterial = carcass,
                WorktopMaterial = CloneMaterial(FindSheet(request.SheetMaterialId)),
                FrontMaterial = CloneMaterial(FindSheet(request.SheetMaterialId)),
                DrawerMaterial = CloneMaterial(FindSheet(string.IsNullOrWhiteSpace(request.DrawerMaterialId) ? ProductDefaults.DefaultDrawerMaterialId : request.DrawerMaterialId)),
                BackMaterial = backMaterial,
                SheetFastener = sheetFastener,
                ShelfSupport = shelfSupport,
                DrawerRail = rail
            };
            config.AdjustableFoot.CentralFastenerLengthMm = FastenerSelectionService.SelectComponentToWoodFaceLength(
                sheetFastener,
                2.0,
                carcass.ThicknessMm);
            var adapter = config.AdjustableFoot == null ? null : config.AdjustableFoot.PlinthClipAdapter;
            if (adapter != null)
            {
                adapter.FrontScrewLengthMm = FastenerSelectionService.SelectComponentToWoodFaceLength(
                    sheetFastener,
                    ProductDefaults.WorkbenchCabinetFrontAdapterStandOffMm(config),
                    carcass.ThicknessMm);
                adapter.SideScrewLengthMm = FastenerSelectionService.SelectComponentToWoodFaceLength(
                    sheetFastener,
                    ProductDefaults.WorkbenchCabinetSideAdapterStandOffMm(config),
                    carcass.ThicknessMm);
            }
            return config;
        }

        public ToolDefinition DefaultTool()
        {
            return catalog.DefaultEndMill(ProductDefaults.DefaultToolDiameterMm, ProductDefaults.DefaultToolPassDepthMm);
        }

        private static RailTemplate DefaultRail(RailTemplate[] rails)
        {
            if (rails == null || rails.Length == 0) throw new InvalidOperationException("Geen railtemplates gevonden.");

            foreach (var rail in rails)
            {
                if (rail != null && string.Equals(rail.Id, ProductDefaults.DefaultDrawerRailId, StringComparison.OrdinalIgnoreCase))
                {
                    return rail;
                }
            }

            var index = ProductDefaults.DefaultDrawerRailIndex >= 0 && ProductDefaults.DefaultDrawerRailIndex < rails.Length
                ? ProductDefaults.DefaultDrawerRailIndex
                : 0;
            return rails[index];
        }

        public MachineProfile DefaultMachine()
        {
            return ProductDefaults.DefaultMachine();
        }

        private static CabinetDoorHand ParseDoor(string value)
        {
            value = (value ?? "").Trim().ToLowerInvariant();
            if (value == "links" || value == "left") return CabinetDoorHand.Links;
            if (value == "rechts" || value == "right") return CabinetDoorHand.Rechts;
            return CabinetDoorHand.Geen;
        }

        private static string NormalizeShelfStartMode(string value)
        {
            value = (value ?? "").Trim().ToLowerInvariant();
            if (value == "boven" || value == "top") return "top";
            return "bottom";
        }

        private static ProfileSawingMode ParseSawingMode(string value)
        {
            value = (value ?? "").Trim().ToLowerInvariant();
            return value == "inhouse" || value == "werkplaats" || value == "zelf_zagen"
                ? ProfileSawingMode.InHouse
                : ProfileSawingMode.SupplierCutToLength;
        }

        private static Material ProfileMaterial(string id, string name, double width, double height)
        {
            return new Material
            {
                Id = id,
                Name = name,
                Kind = MaterialKind.Profile,
                WidthMm = width,
                HeightMm = height,
                StockLengthMm = 6000
            };
        }

        private static Material SheetMaterial(string id, string name, double thickness)
        {
            return new Material
            {
                Id = id,
                Name = name,
                Kind = MaterialKind.Sheet,
                ThicknessMm = thickness,
                SheetLengthMm = 3050,
                SheetWidthMm = 1300
            };
        }

        private static double Clamp(double value, double min, double max, double fallback)
        {
            if (value <= 0) value = fallback;
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private Material FindSheet(string id)
        {
            var sheets = catalog.Sheets();
            return FindMaterial(sheets, id, sheets[ProductDefaults.DefaultSheetIndex]);
        }

        private Material FindProfile(string id)
        {
            var profiles = catalog.Profiles();
            return FindMaterial(profiles, id, profiles[ProductDefaults.DefaultProfileIndex]);
        }

        private static string ProductDefaultProfileId(string productId)
        {
            var product = Array.Find(new ProductCatalogApplicationService().ListProducts(), item =>
                string.Equals(item.Product, productId, StringComparison.OrdinalIgnoreCase));
            if (product == null || string.IsNullOrWhiteSpace(product.DefaultProfileMaterialId))
                throw new InvalidOperationException("Standaard profielmateriaal ontbreekt in productmasterdata voor " + productId + ".");
            return product.DefaultProfileMaterialId;
        }

        private static Material FindMaterial(Material[] materials, string id, Material fallback)
        {
            foreach (var material in materials)
            {
                if (string.Equals(material.Id, id, StringComparison.OrdinalIgnoreCase)) return material;
            }

            return fallback;
        }

        private FastenerDefinition ProductFastener(string requestedProductId, string fallbackProductId, bool woodToWood)
        {
            var productId = string.IsNullOrWhiteSpace(requestedProductId) ? fallbackProductId : requestedProductId;
            var standard = ProductFastenerStandards.Resolve(productId);
            var fastenerId = woodToWood ? standard.WoodToWoodFastenerId : standard.StructuralFastenerId;
            if (string.IsNullOrWhiteSpace(fastenerId))
                throw new InvalidOperationException("Producttype " + productId + " heeft geen passende bevestigerstandaard.");
            foreach (var fastener in catalog.SheetFasteners())
            {
                if (string.Equals(fastener.Id, fastenerId, StringComparison.OrdinalIgnoreCase)) return CloneFastener(fastener);
            }
            throw new InvalidOperationException("Bevestiger " + fastenerId + " uit productstandaard " + productId + " ontbreekt in de componentbibliotheek.");
        }

        public static Material CloneMaterial(Material material)
        {
            return new Material
            {
                Id = material.Id,
                Name = material.Name,
                Kind = material.Kind,
                WidthMm = material.WidthMm,
                HeightMm = material.HeightMm,
                ThicknessMm = material.ThicknessMm,
                StockLengthMm = material.StockLengthMm,
                SheetLengthMm = material.SheetLengthMm,
                SheetWidthMm = material.SheetWidthMm,
                RenderAppearance = material.RenderAppearance,
                IsAdditiveManufactured = material.IsAdditiveManufactured
            };
        }

        public static FastenerDefinition CloneFastener(FastenerDefinition fastener)
        {
            return new FastenerDefinition
            {
                Id = fastener.Id,
                Name = fastener.Name,
                Standard = fastener.Standard,
                NominalDiameterMm = fastener.NominalDiameterMm,
                ClearanceHoleDiameterMm = fastener.ClearanceHoleDiameterMm,
                ReceivingPilotHoleDiameterMm = fastener.ReceivingPilotHoleDiameterMm,
                HeadKind = fastener.HeadKind,
                HeadDiameterMm = fastener.HeadDiameterMm,
                HeadHeightMm = fastener.HeadHeightMm,
                HeadClearanceMm = fastener.HeadClearanceMm,
                UsageKind = fastener.UsageKind,
                LengthMm = fastener.LengthMm,
                AvailableLengthsMm = fastener.AvailableLengthsMm == null ? null : (double[])fastener.AvailableLengthsMm.Clone(),
                MinimumEdgePenetrationMm = fastener.MinimumEdgePenetrationMm,
                MinimumTipClearanceMm = fastener.MinimumTipClearanceMm
            };
        }

        public static RailTemplate CloneRail(RailTemplate rail)
        {
            return new RailTemplate
            {
                Id = rail.Id,
                Name = rail.Name,
                LengthMm = rail.LengthMm,
                ThicknessMm = rail.ThicknessMm,
                CabinetHoleCount = rail.CabinetHoleCount,
                CabinetFirstHoleOffsetMm = rail.CabinetFirstHoleOffsetMm,
                CabinetHoleSpacingMm = rail.CabinetHoleSpacingMm,
                CabinetHolePositionsMm = rail.CabinetHolePositionsMm,
                CabinetOppositeHolePositionsMm = rail.CabinetOppositeHolePositionsMm,
                CabinetVerticalOffsetMm = rail.CabinetVerticalOffsetMm,
                CabinetHoleDiameterMm = rail.CabinetHoleDiameterMm,
                DrawerHoleCount = rail.DrawerHoleCount,
                DrawerFirstHoleOffsetMm = rail.DrawerFirstHoleOffsetMm,
                DrawerHoleSpacingMm = rail.DrawerHoleSpacingMm,
                DrawerHolePositionsMm = rail.DrawerHolePositionsMm,
                DrawerVerticalOffsetMm = rail.DrawerVerticalOffsetMm,
                DrawerHoleDiameterMm = rail.DrawerHoleDiameterMm,
                DrawerFrontInsertionCompensationMm = rail.DrawerFrontInsertionCompensationMm,
                FastenerName = rail.FastenerName,
                CabinetFastenerDiameterMm = rail.CabinetFastenerDiameterMm,
                CabinetFastenerLengthMm = rail.CabinetFastenerLengthMm,
                CabinetFastenerPassingStackMm = rail.CabinetFastenerPassingStackMm,
                CabinetFastenerHeadStyle = rail.CabinetFastenerHeadStyle,
                CabinetOpposingFitVerificationSignature = rail.CabinetOpposingFitVerificationSignature
            };
        }

        public static ShelfSupportTemplate CloneShelfSupport(ShelfSupportTemplate support)
        {
            return new ShelfSupportTemplate
            {
                Id = support.Id,
                Name = support.Name,
                ThicknessMm = support.ThicknessMm,
                HeightMm = support.HeightMm,
                HoleDiameterMm = support.HoleDiameterMm,
                HoleSpacingMm = support.HoleSpacingMm,
                FrontInsetMm = support.FrontInsetMm,
                BackInsetMm = support.BackInsetMm,
                FirstHoleHeightMm = support.FirstHoleHeightMm
            };
        }
    }
}
