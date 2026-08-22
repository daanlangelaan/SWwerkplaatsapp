using System;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public static class ProductDefaults
    {
        public const double ShippingBoxInternalWidthMm = 1200;
        public const double ShippingBoxInternalDepthMm = 800;
        public const double ShippingBoxInternalHeightMm = 800;
        public const string ShippingBoxDefaultMaterialId = "osb_18";

        public static CrateClipTemplate ShippingBoxCrateClip()
        {
            return new CrateClipTemplate
            {
                Id = "liangyue_ly103_12_candidate",
                Name = "Liangyue stalen veerclip LY103-12",
                Supplier = "Nanjing Liangyue Packaging Products Co., Ltd.",
                SupplierModel = "LY103-12",
                SupplierUrl = "https://liangyuepacking.en.made-in-china.com/product/EAOprPFJmGYT/China-Heavy-Duty-Spring-Clips-for-Secure-Wooden-Box-Assembly.html",
                GeometryReference = "Liangyue-foto, gekalibreerd tegen C058-equivalent",
                GeometryReferenceUrl = "https://wierfab.en.made-in-china.com/product/eTNrDlpMsQUz/China-Metal-Crate-Clips-Spring-Snap-Clips-Retaining-Crate.html",
                Material = "veerstaal; exacte legering door Liangyue te bevestigen",
                Finish = "zwart of verchroomd; keuze bij samplebestelling",
                ArmLengthAMm = 63,
                ArmLengthBMm = 71,
                WidthMm = 35,
                ThicknessMm = 1.5,
                SlotLengthMm = 32,
                SlotWidthMm = 8,
                SlotCenterFromEdgeMm = 32,
                EndMarginMm = 100,
                MaxSpacingMm = 350,
                VerificationStatus = "Proefstuk: Liangyue noemt model en staal, maar publiceert geen maatblad; clip- en sleufmaten fysiek inmeten"
            };
        }

        public const double MachineBaseWidthMm = 2000;
        public const double MachineBaseDepthMm = 800;
        public const double MachineBaseHeightMm = 2000;
        public const double MachineBaseWorktopHeightMm = 900;
        public const double MachineBaseReservedWorktopThicknessMm = 10;
        public const double MachineBaseFootPlateThicknessMm = 15;
        public const double MachineBaseCasterOverallHeightMm = 82;
        public const double MachineBaseCasterAdjustmentMm = 10;
        public const double MachineBaseCasterWheelDiameterMm = 50;
        public const double MachineBaseCasterWheelWidthMm = 25;
        public const double MachineBaseCasterOffsetMm = 36;
        public const double MachineBaseWorktopIntermediateBeamMaxSpacingMm = 500;
        public const double MachineBaseControlCabinetWidthMm = 800;
        public const double MachineBaseControlCabinetDepthMm = 400;
        public const double MachineBaseControlCabinetHeightMm = 600;

        public const double RobotCellWidthMm = 1200;
        public const double RobotCellDepthMm = 800;
        public const double RobotCellWorktopHeightMm = 900;
        public const double RobotCellIntermediateBeamMaxSpacingMm = 500;

        public const double WorkbenchWidthMm = 1500;
        public const double WorkbenchDepthMm = 750;
        public const double WorkbenchHeightMm = 900;
        public const int WorkbenchUnitCount = 1;
        public const int WorkbenchDefaultShelfCount = 0;
        public const int WorkbenchDefaultDrawerCount = 0;

        public const double LexWorkbenchWidthMm = 1650;
        public const double LexWorkbenchDepthMm = 1000;
        public const double LexWorkbenchHeightMm = 833;
        public const double LexWorkbenchColumnCenterDistanceMm = 890;

        public static LinearGuideTemplate LexHsr15LinearGuide()
        {
            return new LinearGuideTemplate
            {
                Id = "lex_hsr15_2x1500_4carts",
                Name = "HSR15-compatible lineaire geleidingsset, 2x 1500 mm + 4 wagens",
                SelectedSupplierUrl = "https://nl.aliexpress.com/item/1005009355793291.html",
                ReferenceManufacturer = "THK",
                ReferenceModel = "HSR15R",
                ReferenceSourceUrl = "https://www.thk.com/us/en/products/lm_guide/full_ball/hsr/",
                VerificationStatus = "HSR15 nominale standaardmaatvoering; raildoorsnede en HSR15R-wagenmaat als vaste systeemdata",
                RailLengthMm = 1500,
                RailQuantity = 2,
                RailWidthMm = 15,
                RailHeightMm = 15,
                RailMountingPitchMm = 60,
                RailEndDistanceMm = 30,
                RailHoleThroughDiameterMm = 4.5,
                RailHoleCounterboreDiameterMm = 7.5,
                RailHoleCounterboreDepthMm = 5.3,
                RailHoleCount = 25,
                CarriageQuantity = 4,
                CarriageWidthMm = 34,
                CarriageLengthMm = 56.6,
                AssemblyHeightMm = 28,
                CarriageMountingPitchXmm = 26,
                CarriageMountingPitchZmm = 26,
                CarriageMountingThread = "M4x5",
                ReferenceDynamicLoadKn = 10.9,
                ReferenceStaticLoadKn = 15.7
            };
        }

        public static LiftColumnTemplate LexHte2LiftColumn()
        {
            return new LiftColumnTemplate
            {
                Id = "geming_hte2_o1_400",
                Name = "GeMinG HTE2 hefkolom O1, slag 400 mm",
                Manufacturer = "GeMinG Linear Drive",
                Model = "HTE2 O1",
                VerificationStatus = "Plaatmaten en sleufpatroon uit leveranciers-PDF",
                Quantity = 2,
                RetractedLengthMm = 600,
                StrokeMm = 400,
                BodyWidthMm = 160,
                BodyDepthMm = 65,
                EndPlateLengthMm = 280,
                EndPlateWidthMm = 65,
                EndPlateThicknessMm = 5,
                SlotCenterPitchMm = 250,
                SlotLengthMm = 50,
                SlotWidthMm = 8.5
            };
        }

        public static LevelingFootCornerAdapterTemplate LexLevelingFootCornerAdapter()
        {
            return new LevelingFootCornerAdapterTemplate
            {
                Id = "maunsystem_zi_1744",
                Name = "Maunsystem Stellfusssockel 8 D80",
                ArticleNumber = "ZI-1744 / 1009657",
                SupplierUrl = "https://www.maunsystem.de/stellfusssockel-8-d80-weissaluminium/zi-1744",
                WidthMm = 80,
                ReachMm = 100,
                MountingPlateHeightMm = 80,
                MountingPlateThicknessMm = 14,
                SupportArmThicknessMm = 14,
                FootAxisFromMountingFaceMm = 60,
                ThreadDiameterMm = 16,
                MountingHoleDiameterMm = 8.5,
                MountingHolePitchMm = 40
            };
        }

        public static LevelingFootTemplate LexLevelingFoot()
        {
            return new LevelingFootTemplate
            {
                Id = "maunsystem_zi_1415_s",
                Name = "Maunsystem Stellfuss D80 M16x130 zwart",
                ArticleNumber = "ZI-1415-S / 1003339",
                SupplierUrl = "https://www.maunsystem.de/stellfuss-d80-m16x130-schwarz/zi-1415-s",
                NominalDiameterMm = 80,
                ActualFootDiameterMm = 76,
                OverallHeightMm = 130,
                FootHeightMm = 20,
                ThreadDiameterMm = 16,
                ThreadLengthMm = 96,
                NutAcrossFlatsMm = 24,
                NutHeightMm = 13,
                MaxLoadKg = 1000
            };
        }

        public const double WorkbenchCabinetWidthMm = 2400;
        public const double WorkbenchCabinetDepthMm = 600;
        public const double WorkbenchCabinetHeightMm = 900;
        public const int WorkbenchCabinetUnitCount = 4;
        public const double WorkbenchCabinetPlinthHeightMm = 114;
        public const double WorkbenchCabinetPlinthSetbackMm = 45;
        public const double WorkbenchCabinetPlinthFloorClearanceMm = 8;
        public const double WorkbenchCabinetFootInsetMm = 55;
        public const double WorkbenchCabinetPlinthClipCenterBehindBackFaceMm = 25.4;
        public const double WorkbenchCabinetFootToPlinthClearanceMm = 1.0;
        public const double WorkbenchCabinetFootClickPositionMm = 47;
        public const double WorkbenchCabinetDoorStopWidthMm = 50;
        public const double WorkbenchCabinetDoorGapMm = 3;
        public const double WorkbenchCabinetDoorToCarcassClearanceMm = 2;
        public const double WorkbenchCabinetFrontPanelCornerRadiusMm = 2;
        public const int WorkbenchCabinetDefaultShelfCount = 3;
        public const int WorkbenchCabinetAdjustableShelfPositionCount = 6;
        public const string WorkbenchCabinetDefaultShelfStartMode = "bottom";
        public const double WorkbenchCabinetShelfClearanceMm = 2;
        public const double WorkbenchCabinetShelfFrontInsetMm = 0;
        public const double WorkbenchCabinetAdjustableShelfHoleEndMarginMm = 80;
        public const double WorkbenchCabinetTopDrawerHeightMm = 160;
        public const double WorkbenchCabinetDrawerBackClearanceMm = 30;

        public static AdjustableFootTemplate WorkbenchCabinetAdjustableFoot()
        {
            return new AdjustableFootTemplate
            {
                Id = "ikea_sektion_90556071",
                Name = "IKEA SEKTION poot voor kast, 11 cm",
                ArticleNumber = "905.560.71",
                NominalHeightMm = 114,
                MinHeightMm = 88.9,
                MaxHeightMm = 130.2,
                FootDiameterMm = 50.8,
                MountingBlockLengthMm = 76,
                MountingBlockWidthMm = 51,
                MountingBlockThicknessMm = 12,
                PinDiameterMm = 9.6,
                PinLengthMm = 11.5,
                PinSpacingMm = 33,
                PinCenterFromShortEdgeMm = 18,
                CentralFastenerClearanceDiameterMm = 4.5,
                CentralFastenerNominalDiameterMm = 4,
                CentralFastenerCncPilotHole = false,
                CentralFastenerCenterFromShortEdgeMm = 24,
                SlotLengthMm = 50,
                SlotWidthMm = 32,
                ClipStemDiameterMm = 32.5,
                FootCenterPositionsFromShortEdgeMm = new[] { 32.0, 47.0 },
                DefaultFootCenterFromShortEdgeMm = WorkbenchCabinetFootClickPositionMm,
                MaxLoadKgPerFoot = 125,
                PackQuantity = 2,
                IncludesPlinthClips = true,
                PlinthClipQuantityPerPack = 3,
                MountingPatternVerified = true,
                PlinthClipPatternVerified = true,
                PlinthClipAdapter = WorkbenchCabinetPlinthClipAdapter(),
                MountingHoles = new[]
                {
                    new AdjustableFootMountingHole
                    {
                        Name = "klikpen A Ø10 door",
                        XOffsetMm = -29,
                        YOffsetMm = -16.5,
                        DiameterMm = 10,
                        DepthMm = 0,
                        Through = true
                    },
                    new AdjustableFootMountingHole
                    {
                        Name = "klikpen B Ø10 door",
                        XOffsetMm = -29,
                        YOffsetMm = 16.5,
                        DiameterMm = 10,
                        DepthMm = 0,
                        Through = true
                    }
                }
            };
        }

        public static PlinthClipAdapterTemplate WorkbenchCabinetPlinthClipAdapter()
        {
            return new PlinthClipAdapterTemplate
            {
                Id = "custom_sektion_plinth_clip_adapter_v2",
                Name = "Geschroefde inschuifadapter met montagevleugel voor IKEA SEKTION C-clip",
                TongueWidthMm = 28,
                TongueHeightMm = 34.5,
                TongueThicknessMm = 3.3,
                FootAxisFromTongueBackMm = WorkbenchCabinetPlinthClipCenterBehindBackFaceMm,
                PrintClearancePerSideMm = 0.25,
                BackPlateWidthMm = 38,
                BackPlateHeightMm = 58,
                MinimumBackPlateThicknessMm = 3,
                GuideWallThicknessMm = 3,
                GuideLipOverlapMm = 1.5,
                GuideLipThicknessMm = 1.5,
                BottomStopThicknessMm = 2.5,
                MountingHoleDiameterMm = 4.5,
                MountingHoleSpacingMm = 46,
                UpperMountingHoleHorizontalOffsetMm = 19,
                MountingWingExtensionMm = 6,
                MountingCountersinkDiameterMm = 8.3,
                MountingCountersinkDepthMm = 4.2,
                PlinthCenterMarkDiameterMm = 3,
                PlinthCenterMarkDepthMm = 10,
                FrontScrewDiameterMm = 4,
                FrontScrewLengthMm = 20,
                SideScrewDiameterMm = 4,
                SideScrewLengthMm = 40,
                TonguePatternVerified = true,
                FullDesignVerified = true
            };
        }

        public static double WorkbenchCabinetFrontFootAxisBehindPlinthMm(WorkbenchCabinetConfig config)
        {
            var foot = config.AdjustableFoot ?? WorkbenchCabinetAdjustableFoot();
            var adapter = foot.PlinthClipAdapter ?? WorkbenchCabinetPlinthClipAdapter();
            var clipAxis = Math.Max(config.PlinthClipCenterBehindBackFaceMm, adapter.FootAxisFromTongueBackMm);
            var minimumForAdapter = clipAxis + adapter.MinimumBackPlateThicknessMm;
            var minimumForMountingBlock = foot.MountingBlockWidthMm / 2.0 + WorkbenchCabinetFootToPlinthClearanceMm;
            return Math.Max(minimumForAdapter, minimumForMountingBlock);
        }

        public static double WorkbenchCabinetSideFootAxisBehindPlinthMm(WorkbenchCabinetConfig config)
        {
            var foot = config.AdjustableFoot ?? WorkbenchCabinetAdjustableFoot();
            var adapter = foot.PlinthClipAdapter ?? WorkbenchCabinetPlinthClipAdapter();
            var clipAxis = Math.Max(config.PlinthClipCenterBehindBackFaceMm, adapter.FootAxisFromTongueBackMm);
            var minimumForAdapter = clipAxis + adapter.MinimumBackPlateThicknessMm;
            var maximumBlockReach = Math.Max(
                foot.DefaultFootCenterFromShortEdgeMm,
                foot.MountingBlockLengthMm - foot.DefaultFootCenterFromShortEdgeMm);
            var minimumForMountingBlock = maximumBlockReach + WorkbenchCabinetFootToPlinthClearanceMm;
            return Math.Max(minimumForAdapter, minimumForMountingBlock);
        }

        public static double WorkbenchCabinetFrontAdapterStandOffMm(WorkbenchCabinetConfig config)
        {
            var foot = config.AdjustableFoot ?? WorkbenchCabinetAdjustableFoot();
            var adapter = foot.PlinthClipAdapter ?? WorkbenchCabinetPlinthClipAdapter();
            return WorkbenchCabinetFrontFootAxisBehindPlinthMm(config) - adapter.FootAxisFromTongueBackMm;
        }

        public static double WorkbenchCabinetSideAdapterStandOffMm(WorkbenchCabinetConfig config)
        {
            var foot = config.AdjustableFoot ?? WorkbenchCabinetAdjustableFoot();
            var adapter = foot.PlinthClipAdapter ?? WorkbenchCabinetPlinthClipAdapter();
            return WorkbenchCabinetSideFootAxisBehindPlinthMm(config) - adapter.FootAxisFromTongueBackMm;
        }

        public static double WorkbenchCabinetFrontFootCenterFromFrontMm(WorkbenchCabinetConfig config)
        {
            var plinthThickness = config.CarcassMaterial == null ? DefaultSheetThicknessMm : Math.Max(2, config.CarcassMaterial.ThicknessMm);
            return config.PlinthSetbackMm + plinthThickness + WorkbenchCabinetFrontFootAxisBehindPlinthMm(config);
        }

        public static double WorkbenchCabinetSideFootCenterFromOuterEdgeMm(WorkbenchCabinetConfig config)
        {
            var plinthThickness = config.CarcassMaterial == null ? DefaultSheetThicknessMm : Math.Max(2, config.CarcassMaterial.ThicknessMm);
            return plinthThickness + WorkbenchCabinetSideFootAxisBehindPlinthMm(config);
        }

        public const double CabinetWidthMm = 2400;
        public const double CabinetDepthMm = 600;
        public const double CabinetHeightMm = 900;
        public const int CabinetUnitCount = 4;
        public const int CabinetDefaultShelfCount = 3;
        public const int CabinetDefaultDrawerCount = 1;
        public const string CabinetDefaultShelfStartMode = "top";

        public const double CubbyCabinetCellWidthMm = 400;
        public const double CubbyCabinetCellDepthMm = 350;
        public const double CubbyCabinetCellHeightMm = 350;
        public const int CubbyCabinetColumnCount = 3;
        public const int CubbyCabinetRowCount = 4;
        public const double CubbyCabinetPlinthHeightMm = 80;
        public const double CubbyCabinetPlinthDepthMm = 40;
        public const double CubbyCabinetGridInsetMm = 20;
        public const double CubbyCabinetCombSlotClearanceMm = 0.3;
        public const double CubbyCabinetBackGrooveDepthMm = 6;
        public const double CubbyCabinetBackGrooveClearanceMm = 0.5;
        public const double CubbyCabinetBackFastenerMaxSpacingMm = 220;
        public const double CubbyCabinetDividerBackFastenerMaxSpacingMm = 260;

        public const int DefaultSheetIndex = 2;
        public const int DefaultProfileIndex = 1;
        public const int DefaultDrawerRailIndex = 2;
        public const string DefaultDrawerRailId = "measured_500_r2";
        public const int DefaultShelfSupportIndex = 0;
        public const double DefaultSheetThicknessMm = 18;

        public const string DefaultDrawerMaterialId = "multiplex_15";
        public const string DefaultBackMaterialId = "multiplex_15";
        public const string DefaultSlidingDoorMaterialId = "betonplex_12";

        public const double CabinetPlinthHeightMm = 100;
        public const double CabinetPlinthDepthMm = 60;
        public const double FullWidthTopDrawerHeightMm = 160;
        public const double AdjustableShelfHoleEndMarginMm = 80;
        public const double ShelfClearanceMm = 2;
        public const double CabinetShelfFrontInsetMm = 0;
        public const double CabinetSlidingDoorOverlapMm = 25;
        public const double CabinetSlidingDoorFreeSpaceBehindMm = 10;
        public const double CabinetSlidingDoorTrackCenterSpacingMm = 18;
        public const double CabinetSlidingDoorTopProfileHeightMm = 25;
        public const double CabinetSlidingDoorBottomProfileHeightMm = 18;
        public const double CabinetSlidingDoorTapeThicknessMm = 1;
        public const double CabinetSlidingDoorTopProfileDepthMm = 15;
        public const double CabinetSlidingDoorBottomProfileDepthMm = 18;
        public const double DrawerBackClearanceMm = 30;
        public const double DoorGapMm = 2;

        public const double DefaultToolDiameterMm = 6;
        public const double DefaultToolPassDepthMm = 6.25;

        public static MachineProfile DefaultMachine()
        {
            return new MachineProfile
            {
                Id = "mach3_portaal_3020x1520",
                Name = "Mach3 portaalfrees 3020x1520",
                MaxXmm = 3020,
                MaxYmm = 1520,
                FileExtension = ".tap",
                SafeZmm = 15,
                Origin = "links onder"
            };
        }
    }
}
