using System;
using System.Collections.Generic;
using System.Linq;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Drawing;

namespace SWWerkplaats.Configurator.Engine
{
    public sealed class MachineBaseEngine
    {
        private const double UprightWidthMm = 40.0;
        private const double UprightDepthMm = 80.0;
        private const double FootPlateThicknessMm = 16.0;
        private const double CasterOverallHeightMm = 82.0;
        private const double CasterLeveledExtensionMm = 10.0;
        private const double CasterLeveledHeightMm = CasterOverallHeightMm + CasterLeveledExtensionMm;
        private const double FloorToUprightMm = CasterLeveledHeightMm + FootPlateThicknessMm;

        public WorkbenchModel Build(MachineBaseConfig config)
        {
            Validate(config);
            var model = new WorkbenchModel
            {
                ProjectName = config.ProjectName,
                LowerFrameHeightMm = 0,
                MiddleLayerHeightMm = config.WorktopHeightMm,
                SawingMode = config.SawingMode
            };

            if (config.UpperPanelMaterial == null || config.UpperPanelMaterial.ThicknessMm <= 0)
                throw new ArgumentException("Bovenpaneelmateriaal ontbreekt in machinebasis-masterdata.");
            var topSheetThickness = config.UpperPanelMaterial.ThicknessMm;
            var frameTop = config.HeightMm - topSheetThickness;
            var uprightLength = frameTop - FloorToUprightMm;
            AddProfile(model, "Staander 40x80", config.UprightProfile, uprightLength, 4,
                "80 mm zijde in de diepte; lengte berekend vanaf vloer via GD-60S, voetplaat en 6 mm topplaat", config.SawingMode, false);
            AddUprightPlacements(model, config, frameTop);

            var connectorCount = 0;
            connectorCount += AddLayer(model, config, "Onderligger", config.LowerBeamProfile, FloorToUprightMm, false, 20.0, 20.0);
            connectorCount += AddIntermediateBeams(model, config, "Onderframe tussenligger", config.TopBeamProfile, FloorToUprightMm, false, 20.0, 20.0);
            var carrierTop = config.WorktopHeightMm - config.ReservedWorktopThicknessMm;
            connectorCount += AddLayer(model, config, "Bladligger", config.WorktopBeamProfile, carrierTop, true, 20.0, 20.0);
            connectorCount += AddIntermediateBeams(model, config, "Bladframe tussenligger", config.WorktopBeamProfile, carrierTop, true, 20.0, 20.0);
            connectorCount += AddLayer(model, config, "Bovenligger", config.TopBeamProfile, frameTop, true, 20.0, 20.0);
            connectorCount += AddIntermediateBeams(model, config, "Topframe tussenligger", config.TopBeamProfile, frameTop, true, 20.0, 20.0);

            AddWorktop(model, config);
            AddFeet(model, config);
            AddEnclosurePanels(model, config, carrierTop);
            AddTopAcrylic(model, config, frameTop);
            connectorCount += AddControlCabinet(model, config, carrierTop);
            connectorCount += AddFrontProtection(model, config, config.WorktopHeightMm, frameTop);
            AddBlackEndCaps(model, config, frameTop);
            connectorCount = model.AssemblyConnections.Count(connection => connection.JointType == AssemblyJointType.StandardConnector);
            AddStandardConnectorHardware(model, connectorCount);
            model.DesignNotes.Add("Bladhoogte is de bovenzijde van het gekozen HPL-werkblad. Het blad heeft vier rechthoekige hoekuitsparingen van 41x81 mm voor 1 mm montagespeling rondom de doorlopende 40x80-staanders.");
            model.DesignNotes.Add("Alle hoogtematen worden vanaf de vloer gerekend in de bedrijfsstand op de rubberen stelpoten: 82 mm rijhoogte + 10 mm uitstelling + 15 mm voetplaat.");
            model.DesignNotes.Add("GD-60S: wiel 50x25 mm, M12x1,75 en 36 mm wieloffset. De wielen zijn uitsluitend voor transport; in bedrijfsstand zijn ze vrij van de vloer.");
            model.DesignNotes.Add("De werkbladlaag gebruikt standaard 40x80 voor buitencontour en tussenliggers. Tussenliggers lopen in de diepte en worden gelijkmatig verdeeld op basis van de maximale hart-op-hartafstand.");
            model.DesignNotes.Add("De onderste voor- en achterligger staan op de buitenste groef van de 80 mm diepe staanders: 20 mm naar buiten ten opzichte van de staanderhartlijn en vlak met het buitenframe.");
            model.DesignNotes.Add("Alle vaste profielen aan de voorzijde hebben hun buitenvlak op dezelfde referentielijn als de voorzijde van de staanders. Kast, kastprofielen en voorbeplating volgen dit vlak; HPL ligt met zijn achterzijde tegen het profielvlak.");
            model.DesignNotes.Add("Het onderframe heeft altijd gelijk verdeelde 40x40-diepteliggers volgens dezelfde maximale h.o.h.-instelling als werkblad- en topframe.");
            model.DesignNotes.Add("De toplaag gebruikt dezelfde parametrische verdeling als de werkbladlaag, maar altijd 40x40, met een helder acrylaat dak van 6 mm. Totale hoogte blijft vloer tot bovenzijde dakplaat.");
            return model;
        }

        private static void AddWorktop(WorkbenchModel model, MachineBaseConfig config)
        {
            var material = config.WorktopMaterial;
            if (material == null) throw new ArgumentException("Werkbladmateriaal ontbreekt.");
            var sheet = SheetDrawing.CreateSheet("HPL werkblad met hoekuitsparingen", material, config.WidthMm, config.DepthMm);
            sheet.HasCornerNotches = true;
            sheet.CornerNotchLengthMm = UprightWidthMm + 1.0;
            sheet.CornerNotchWidthMm = UprightDepthMm + 1.0;

            foreach (var x in EvenPositions(50.0, config.WidthMm - 50.0, 250.0))
            {
                AddPanelHole(sheet, x, 20.0, 6.5, "werkbladligger voor");
                AddPanelHole(sheet, x, config.DepthMm - 20.0, 6.5, "werkbladligger achter");
            }
            var availableWidth = config.WidthMm - 2.0 * UprightWidthMm;
            var bayCount = Math.Max(1, (int)Math.Ceiling(availableWidth / config.WorktopIntermediateBeamMaxSpacingMm));
            for (var i = 1; i < bayCount; i++)
            {
                var localX = UprightWidthMm + availableWidth * i / bayCount;
                foreach (var z in EvenPositions(50.0, config.DepthMm - 50.0, 250.0))
                    AddPanelHole(sheet, localX, z, 6.5, "werkblad-tussenligger");
            }

            AddPanel(model, sheet, 0, config.WorktopHeightMm - material.ThicknessMm / 2.0, 0, AssemblyOrientation.SheetHorizontal, false);
            model.Hardware.Add(new HardwareItem { Name = "Fabory laagbolkopschroef M6x16 voor HPL werkblad", ArticleNumber = "07151.060.016", Quantity = sheet.Holes.Count, Unit = "st", Note = "ISO 7380-1, verzinkt; Ø6,5 mm werkbladgaten", ModelStatus = "Definitief artikel", BomStatus = "Inkoop" });
            model.Hardware.Add(new HardwareItem { Name = "TechXXL T-moer groef 8 met brug M6", ArticleNumber = "S208NSMS6", Quantity = sheet.Holes.Count, Unit = "st", Note = "TechXXL TIN 100242; groef 8, I-type; 23x13,2x7,3 mm; verzinkt met veerkogel", ModelStatus = "Definitief leveranciersartikel", BomStatus = "Inkoop" });
        }

        private static void AddEnclosurePanels(WorkbenchModel model, MachineBaseConfig config, double carrierTopMm)
        {
            var transitionY = carrierTopMm - 40.0;
            var lowerBottomY = FloorToUprightMm;
            var lowerHeight = transitionY - lowerBottomY;
            var upperHeight = config.HeightMm - transitionY;
            if (lowerHeight <= 100 || upperHeight <= 100) throw new ArgumentException("Onvoldoende plaathoogte voor de gekozen overgang op het werkbladprofiel.");

            var hplHoleCount = 0;
            var acrylicHoleCount = 0;
            hplHoleCount += AddSidePanel(model, "HPL 6mm zijplaat links", config.LowerPanelMaterial, config, lowerHeight, lowerBottomY + lowerHeight / 2.0, -1, 6.5, false);
            hplHoleCount += AddSidePanel(model, "HPL 6mm zijplaat rechts", config.LowerPanelMaterial, config, lowerHeight, lowerBottomY + lowerHeight / 2.0, 1, 6.5, false);
            hplHoleCount += AddRearPanel(model, "HPL 6mm achterplaat", config.LowerPanelMaterial, config, lowerHeight, lowerBottomY + lowerHeight / 2.0, 6.5, false);

            acrylicHoleCount += AddSidePanel(model, "Acryl helder 6mm zijplaat links", config.UpperPanelMaterial, config, upperHeight, transitionY + upperHeight / 2.0, -1, 7.0, true);
            acrylicHoleCount += AddSidePanel(model, "Acryl helder 6mm zijplaat rechts", config.UpperPanelMaterial, config, upperHeight, transitionY + upperHeight / 2.0, 1, 7.0, true);
            acrylicHoleCount += AddRearPanel(model, "Acryl helder 6mm achterplaat", config.UpperPanelMaterial, config, upperHeight, transitionY + upperHeight / 2.0, 7.0, true);

            var totalFasteners = hplHoleCount + acrylicHoleCount;
            model.Hardware.Add(new HardwareItem
            {
                Name = "Fabory laagbolkopschroef binnenzeskant M6x16 ISO 7380-1 verzinkt",
                ArticleNumber = "07151.060.016",
                Quantity = totalFasteners,
                Unit = "st",
                Note = "Fabory; klasse 10.9; kop Ø10,5x3,3; binnenzeskant 4; EAN 8715492070029; https://www.fabory.com/nl/laagbolkopschroef-met-binnenzeskant-iso-7380-1-staal-elektrolytisch-verzinkt-010-9-m6x16/p/07151060016",
                ModelStatus = "Definitief artikel",
                BomStatus = "Inkoop"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "TechXXL T-moer groef 8 met brug M6",
                ArticleNumber = "S208NSMS6",
                Quantity = acrylicHoleCount,
                Unit = "st",
                Note = "TechXXL TIN 100242; groef 8, I-type; 23x13,2x7,3 mm; verzinkt en door veerkogel op positie gehouden.",
                ModelStatus = "Definitief leveranciersartikel",
                BomStatus = "Inkoop"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "TechXXL T-moer groef 8 met brug M6",
                ArticleNumber = "S208NSMS6",
                Quantity = hplHoleCount,
                Unit = "st",
                Note = "TechXXL TIN 100242; groef 8, I-type; 23x13,2x7,3 mm; verzinkt met veerkogel.",
                ModelStatus = "Definitief leveranciersartikel",
                BomStatus = "Inkoop"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "Transparante kunststof ring M6 voor acryl",
                ArticleNumber = "WASHER-M6-CLEAR",
                Quantity = acrylicHoleCount,
                Unit = "st",
                Note = "Onder laagbolkop; beschermt helder acryl tegen puntbelasting",
                ModelStatus = "Functioneel gemodelleerd",
                BomStatus = "Definitief leverancierartikel nog koppelen"
            });
            model.DesignNotes.Add("HPL loopt van onderzijde onderframe tot de middenlijn van het 80 mm hoge werkbladprofiel; helder acryl loopt vanaf die middenlijn tot bovenzijde bovenframe.");
            model.DesignNotes.Add("Achterplaten zijn 12 mm breder dan het frame en vallen over de 6 mm dikke kopse achterkanten van beide zijplaten; de voorzijde blijft open.");
            model.DesignNotes.Add("Plaatgaten: HPL Ø6,5; acryl Ø7 met transparante ring. Eerste en laatste bevestiging 50 mm vanaf het ondersteunde einde; tussenafstanden gelijk verdeeld en maximaal 250 mm.");
        }

        private static int AddSidePanel(WorkbenchModel model, string name, Material material, MachineBaseConfig config, double height, double centerY, int side, double holeDiameter, bool acrylic)
        {
            var sheet = SheetDrawing.CreateSheet(name, material, config.DepthMm, height);
            AddPanelHoles(sheet, 20.0, 80.0, config.DepthMm - 80.0, holeDiameter);
            var x = side * (config.WidthMm / 2.0 + material.ThicknessMm / 2.0);
            AddPanel(model, sheet, x, centerY, 0, AssemblyOrientation.SheetVerticalZ, acrylic);
            return sheet.Holes.Count;
        }

        private static int AddRearPanel(WorkbenchModel model, string name, Material material, MachineBaseConfig config, double height, double centerY, double holeDiameter, bool acrylic)
        {
            var length = config.WidthMm + 2.0 * material.ThicknessMm;
            var sheet = SheetDrawing.CreateSheet(name, material, length, height);
            const double rearPostGrooveFromPanelEdgeMm = 26.0;
            const double horizontalBeamStartFromPanelEdgeMm = 46.0;
            AddPanelHoles(sheet, rearPostGrooveFromPanelEdgeMm, horizontalBeamStartFromPanelEdgeMm, length - horizontalBeamStartFromPanelEdgeMm, holeDiameter);
            var z = config.DepthMm / 2.0 + material.ThicknessMm / 2.0;
            AddPanel(model, sheet, 0, centerY, z, AssemblyOrientation.SheetVerticalX, acrylic);
            return sheet.Holes.Count;
        }

        private static void AddPanel(WorkbenchModel model, SheetPart sheet, double x, double y, double z, AssemblyOrientation orientation, bool acrylic)
        {
            SheetDrawing.AddSheetToModel(model, sheet, x, y, z, orientation);
            if (acrylic && model.AssemblyPlacements.Count > 0)
            {
                model.AssemblyPlacements[model.AssemblyPlacements.Count - 1].Shape = "acrylic-panel";
            }
        }

        private static void AddPanelHoles(SheetPart sheet, double verticalGrooveInset, double horizontalSupportStart, double horizontalSupportEnd, double diameter)
        {
            foreach (var y in EvenPositions(50.0, sheet.WidthMm - 50.0, 250.0))
            {
                AddPanelHole(sheet, verticalGrooveInset, y, diameter, "hoekprofiel links/voor");
                AddPanelHole(sheet, sheet.LengthMm - verticalGrooveInset, y, diameter, "hoekprofiel rechts/achter");
            }

            var firstHorizontal = horizontalSupportStart + 50.0;
            var lastHorizontal = horizontalSupportEnd - 50.0;
            foreach (var x in EvenPositions(firstHorizontal, lastHorizontal, 250.0))
            {
                AddPanelHole(sheet, x, 20.0, diameter, "onderste profielgroef");
                AddPanelHole(sheet, x, sheet.WidthMm - 20.0, diameter, "bovenste profielgroef");
            }
        }

        private static IList<double> EvenPositions(double first, double last, double maxSpacing)
        {
            var result = new List<double>();
            if (last < first)
            {
                result.Add((first + last) / 2.0);
                return result;
            }

            var span = last - first;
            if (span < 50.0)
            {
                result.Add(Math.Round((first + last) / 2.0, 3));
                return result;
            }
            var segmentCount = Math.Max(1, (int)Math.Ceiling(span / maxSpacing));
            for (var i = 0; i <= segmentCount; i++) result.Add(Math.Round(first + span * i / segmentCount, 3));
            return result;
        }

        private static void AddPanelHole(SheetPart sheet, double x, double y, double diameter, string support)
        {
            foreach (var existing in sheet.Holes)
            {
                if (Math.Abs(existing.Xmm - x) < 0.01 && Math.Abs(existing.Ymm - y) < 0.01) return;
            }

            sheet.Holes.Add(new SheetHole
            {
                Name = "Plaatmontage " + support + " " + (sheet.Holes.Count + 1),
                Xmm = Math.Round(x, 3),
                Ymm = Math.Round(y, 3),
                DiameterMm = diameter,
                DepthMm = 0,
                Face = OperationFace.CenterPlane,
                DepthMode = OperationDepthMode.Through,
                Countersunk = false,
                SupportKind = SheetHoleSupportKind.ProfileNut
            });
        }

        private static int AddIntermediateBeams(WorkbenchModel model, MachineBaseConfig config, string name, Material profile, double referenceTopMm, bool alignTop, double frontSupportOffsetMm, double rearSupportOffsetMm)
        {
            var verticalSize = ProfileVerticalSize(profile);
            var beamThickness = ProfileHorizontalSize(profile);
            var availableWidth = config.WidthMm - 2.0 * UprightWidthMm;
            var frontBackCenterDistance = config.DepthMm - UprightDepthMm;
            var beamLength = frontBackCenterDistance + frontSupportOffsetMm + rearSupportOffsetMm - beamThickness;
            var beamCenterZ = (rearSupportOffsetMm - frontSupportOffsetMm) / 2.0;
            var bayCount = Math.Max(1, (int)Math.Ceiling(availableWidth / config.WorktopIntermediateBeamMaxSpacingMm));
            var beamCount = Math.Max(0, bayCount - 1);
            if (beamCount == 0) return 0;

            var actualSpacing = availableWidth / bayCount;
            var connectionCount = AddProfile(model, name, profile, beamLength, beamCount,
                "Diepteligger; gelijk verdeeld h.o.h. " + actualSpacing.ToString("0.##") + " mm", config.SawingMode, true);
            var centerY = alignTop ? referenceTopMm - verticalSize / 2.0 : referenceTopMm + verticalSize / 2.0;
            for (var i = 1; i <= beamCount; i++)
            {
                var x = -availableWidth / 2.0 + i * actualSpacing;
                AddPlacement(model, name + " " + i, AssemblyComponentKind.Profile,
                    beamThickness, beamLength, verticalSize, x, centerY, beamCenterZ, "profile", "box");
                var accessY = centerY - verticalSize / 2.0 - 0.5;
                AddBlackConnectorHole(model, name + " voor " + i, x, accessY, beamCenterZ - beamLength / 2.0 + 20.0, "y");
                AddBlackConnectorHole(model, name + " achter " + i, x, accessY, beamCenterZ + beamLength / 2.0 - 20.0, "y");
                var slotBase = IntermediateSlotBase(name);
                AddStandardConnection(model, name, name + " " + i, ProfileEnd.A, slotBase + " voor");
                AddStandardConnection(model, name, name + " " + i, ProfileEnd.B, slotBase + " achter");
            }

            model.DesignNotes.Add(beamCount + " " + name.ToLowerInvariant() + "s, werkelijk gelijk verdeeld op " + actualSpacing.ToString("0.##") + " mm h.o.h.");
            return connectionCount;
        }

        private static void AddUprightPlacements(WorkbenchModel model, MachineBaseConfig config, double frameTopMm)
        {
            var uprightLength = frameTopMm - FloorToUprightMm;
            var x = (config.WidthMm - UprightWidthMm) / 2.0;
            var z = (config.DepthMm - UprightDepthMm) / 2.0;
            var centerY = FloorToUprightMm + uprightLength / 2.0;
            AddPlacement(model, "Staander linksvoor", AssemblyComponentKind.Profile, UprightWidthMm, UprightDepthMm, uprightLength, -x, centerY, -z, "profile", "box");
            AddPlacement(model, "Staander rechtsvoor", AssemblyComponentKind.Profile, UprightWidthMm, UprightDepthMm, uprightLength, x, centerY, -z, "profile", "box");
            AddPlacement(model, "Staander linksachter", AssemblyComponentKind.Profile, UprightWidthMm, UprightDepthMm, uprightLength, -x, centerY, z, "profile", "box");
            AddPlacement(model, "Staander rechtsachter", AssemblyComponentKind.Profile, UprightWidthMm, UprightDepthMm, uprightLength, x, centerY, z, "profile", "box");
        }

        private static int AddLayer(WorkbenchModel model, MachineBaseConfig config, string name, Material profile, double referenceHeightMm, bool alignTop, double frontOutwardOffsetMm, double rearOutwardOffsetMm)
        {
            var verticalSize = ProfileVerticalSize(profile);
            var centerY = alignTop ? referenceHeightMm - verticalSize / 2.0 : referenceHeightMm + verticalSize / 2.0;
            var frontBackLength = config.WidthMm - 2.0 * UprightWidthMm;
            var sideLength = config.DepthMm - 2.0 * UprightDepthMm;
            var xPost = (config.WidthMm - UprightWidthMm) / 2.0;
            var zPost = (config.DepthMm - UprightDepthMm) / 2.0;
            var beamThickness = ProfileHorizontalSize(profile);
            var frontBeamZ = -zPost - frontOutwardOffsetMm;
            var rearBeamZ = zPost + rearOutwardOffsetMm;
            var leftBeamX = -xPost;
            var rightBeamX = xPost;

            var connectionCount = 0;
            connectionCount += AddProfile(model, name + " voor/achter", profile, frontBackLength, 2, "80 mm maat verticaal indien profiel 40x80", config.SawingMode, true);
            connectionCount += AddProfile(model, name + " links/rechts", profile, sideLength, 2, "80 mm maat verticaal indien profiel 40x80", config.SawingMode, true);
            AddPlacement(model, name + " voor", AssemblyComponentKind.Profile, frontBackLength, beamThickness, verticalSize, 0, centerY, frontBeamZ, "profile", "box");
            AddPlacement(model, name + " achter", AssemblyComponentKind.Profile, frontBackLength, beamThickness, verticalSize, 0, centerY, rearBeamZ, "profile", "box");
            AddPlacement(model, name + " links", AssemblyComponentKind.Profile, beamThickness, sideLength, verticalSize, leftBeamX, centerY, 0, "profile", "box");
            AddPlacement(model, name + " rechts", AssemblyComponentKind.Profile, beamThickness, sideLength, verticalSize, rightBeamX, centerY, 0, "profile", "box");
            foreach (var px in new[] { -frontBackLength / 2.0 + 20, frontBackLength / 2.0 - 20 })
            {
                AddBlackConnectorHole(model, name + " voor", px, centerY - verticalSize / 2.0 - 0.5, frontBeamZ, "y");
                AddBlackConnectorHole(model, name + " achter", px, centerY - verticalSize / 2.0 - 0.5, rearBeamZ, "y");
            }
            foreach (var pz in new[] { -sideLength / 2.0 + 20, sideLength / 2.0 - 20 })
            {
                AddBlackConnectorHole(model, name + " links", leftBeamX, centerY - verticalSize / 2.0 - 0.5, pz, "y");
                AddBlackConnectorHole(model, name + " rechts", rightBeamX, centerY - verticalSize / 2.0 - 0.5, pz, "y");
            }
            AddStandardConnection(model, name, name + " voor", ProfileEnd.A, "Staander linksvoor");
            AddStandardConnection(model, name, name + " voor", ProfileEnd.B, "Staander rechtsvoor");
            AddStandardConnection(model, name, name + " achter", ProfileEnd.A, "Staander linksachter");
            AddStandardConnection(model, name, name + " achter", ProfileEnd.B, "Staander rechtsachter");
            AddStandardConnection(model, name, name + " links", ProfileEnd.A, "Staander linksvoor");
            AddStandardConnection(model, name, name + " links", ProfileEnd.B, "Staander linksachter");
            AddStandardConnection(model, name, name + " rechts", ProfileEnd.A, "Staander rechtsvoor");
            AddStandardConnection(model, name, name + " rechts", ProfileEnd.B, "Staander rechtsachter");
            return connectionCount;
        }

        private static void AddFeet(WorkbenchModel model, MachineBaseConfig config)
        {
            model.Hardware.Add(new HardwareItem { Name = "Voetplaat 8 80x40 M12 aluminiumkleur", ArticleNumber = "S208FP804012WA", Quantity = 4, Unit = "st", Note = "80x40x16 mm; TechXXL TIN 101030; groef 8, I-type", ModelStatus = "Leveranciers-CAD gecontroleerd", BomStatus = "Inkoop" });
            model.Hardware.Add(new HardwareItem { Name = "Nivellerend zwenkwiel GD-60S M12", ArticleNumber = "GD-60S", Quantity = 4, Unit = "st", Note = "Wiel 50x25; hoogte 82+10; offset 36; M12x1,75; officiële draaglast 280 kg per wiel", ModelStatus = "Maat-envelop opgenomen", BomStatus = "Aangeleverd inkoopartikel" });
            var x = (config.WidthMm - UprightWidthMm) / 2.0;
            var z = (config.DepthMm - UprightDepthMm) / 2.0;
            AddFootAt(model, -x, -z, "linksvoor");
            AddFootAt(model, x, -z, "rechtsvoor");
            AddFootAt(model, -x, z, "linksachter");
            AddFootAt(model, x, z, "rechtsachter");
        }

        private static void AddFootAt(WorkbenchModel model, double x, double z, string suffix)
        {
            AddPlacement(model, "Voetplaat " + suffix, AssemblyComponentKind.Purchased, 40, 80, FootPlateThicknessMm, x, CasterLeveledHeightMm + FootPlateThicknessMm / 2.0, z, "hardware-adapter", "footplate-m12", "techxxl_footplate_8_80x40_m12");
            AddPlacement(model, "Zwenkwiel GD-60S " + suffix, AssemblyComponentKind.Purchased, 97, 74, CasterLeveledHeightMm, x, CasterLeveledHeightMm / 2.0, z, "hardware-wheel", "leveling-caster-leveled", "gd60s_leveling_caster_m12");
        }

        private static void AddTopAcrylic(WorkbenchModel model, MachineBaseConfig config, double frameTopMm)
        {
            var sheet = SheetDrawing.CreateSheet("Acryl helder 6mm topplaat", config.UpperPanelMaterial, config.WidthMm, config.DepthMm);
            foreach (var x in EvenPositions(50, config.WidthMm - 50, 250))
            {
                AddPanelHole(sheet, x, 20, 7, "topprofiel voor");
                AddPanelHole(sheet, x, config.DepthMm - 20, 7, "topprofiel achter");
            }
            foreach (var y in EvenPositions(50, config.DepthMm - 50, 250))
            {
                AddPanelHole(sheet, 20, y, 7, "topprofiel links");
                AddPanelHole(sheet, config.WidthMm - 20, y, 7, "topprofiel rechts");
            }
            var availableWidth = config.WidthMm - 2 * UprightWidthMm;
            var bayCount = Math.Max(1, (int)Math.Ceiling(availableWidth / config.WorktopIntermediateBeamMaxSpacingMm));
            for (var i = 1; i < bayCount; i++)
            {
                var localX = UprightWidthMm + availableWidth * i / bayCount;
                foreach (var y in EvenPositions(50, config.DepthMm - 50, 250)) AddPanelHole(sheet, localX, y, 7, "top-tussenligger");
            }
            AddPanel(model, sheet, 0, frameTopMm + sheet.Material.ThicknessMm / 2.0, 0, AssemblyOrientation.SheetHorizontal, true);
            model.Hardware.Add(new HardwareItem { Name = "Transparante kunststof ring M6 voor acryl top", ArticleNumber = "WASHER-M6-CLEAR", Quantity = sheet.Holes.Count, Unit = "st", Note = "Onder laagbolkop op topplaat", ModelStatus = "Functioneel gemodelleerd", BomStatus = "Definitief leverancierartikel nog koppelen" });
            model.Hardware.Add(new HardwareItem { Name = "Fabory laagbolkopschroef M6x16 voor acryl top", ArticleNumber = "07151.060.016", Quantity = sheet.Holes.Count, Unit = "st", Note = "ISO 7380-1, verzinkt", ModelStatus = "Definitief artikel", BomStatus = "Inkoop" });
            model.Hardware.Add(new HardwareItem { Name = "TechXXL M6 T-moer groef 8 met brug en verende kogel voor top", ArticleNumber = "TIN 100242 / S208NSMS6", Quantity = sheet.Holes.Count, Unit = "st", Note = "Serie 8 / groef 8; 23×13,2×7,3 mm", ModelStatus = "Exact leveranciersartikel", BomStatus = "Inkoop vrijgegeven" });
        }

        private static int AddControlCabinet(WorkbenchModel model, MachineBaseConfig config, double carrierTopMm)
        {
            var bottom = FloorToUprightMm + ProfileVerticalSize(config.LowerBeamProfile);
            var worktopBeamHeight = ProfileVerticalSize(config.WorktopBeamProfile);
            var cabinetTopProfileHeight = ProfileVerticalSize(config.TopBeamProfile);
            var sideTop = carrierTopMm - worktopBeamHeight;
            // De kastmaat beschrijft de behuizing. Boven die behuizing komt nog een
            // afzonderlijk 40x40-profiel; reserveer dat volume voordat we klemmen.
            var maxTop = sideTop - cabinetTopProfileHeight;
            var cabinetHeight = Math.Min(config.ControlCabinetHeightMm, maxTop - bottom);
            if (cabinetHeight <= 0) throw new ArgumentException("Onvoldoende hoogte voor besturingskast en 40x40-kasttopprofiel onder de werkbladligger.");
            var cabinetCenterY = bottom + cabinetHeight / 2.0;
            var frontProfileOuterZ = -config.DepthMm / 2.0;
            var frontProfileCenterZ = frontProfileOuterZ + 20.0;
            var cabinetCenterZ = frontProfileOuterZ + config.ControlCabinetDepthMm / 2.0;
            var availableInnerWidth = config.WidthMm - 2.0 * UprightWidthMm;
            var cabinetSupportWidth = config.ControlCabinetWidthMm + 80.0;
            var cabinetCenterOffset = Math.Max(0, (availableInnerWidth - cabinetSupportWidth) / 2.0);
            // De echte voorzijde ligt op -Z. Vanaf die zijde gezien is model +X visueel links.
            var cabinetCenterX = string.Equals(config.ControlCabinetPosition, "right", StringComparison.OrdinalIgnoreCase) ? -cabinetCenterOffset : cabinetCenterOffset;
            AddPlacement(model, "Besturingskast behuizing", AssemblyComponentKind.Purchased,
                config.ControlCabinetWidthMm, config.ControlCabinetDepthMm, cabinetHeight, cabinetCenterX, cabinetCenterY, cabinetCenterZ, "hardware-cabinet", "box");
            var cabinetDoorGap = config.ControlCabinetDoorCount == 1 ? 0 : 3.0;
            var cabinetDoorWidth = config.ControlCabinetDoorCount == 1 ? config.ControlCabinetWidthMm - 6 : (config.ControlCabinetWidthMm - 9) / 2.0;
            const double cabinetDoorStandOffMm = 4.0;
            const double cabinetDoorThicknessMm = 17.0;
            var cabinetDoorHeight = cabinetHeight - 6;
            var cabinetDoorCenterZ = frontProfileOuterZ - cabinetDoorStandOffMm - cabinetDoorThicknessMm / 2.0;
            for (var door = 0; door < config.ControlCabinetDoorCount; door++)
            {
                var doorX = cabinetCenterX + (config.ControlCabinetDoorCount == 1 ? 0 : (door == 0 ? -(cabinetDoorWidth + cabinetDoorGap) / 2.0 : (cabinetDoorWidth + cabinetDoorGap) / 2.0));
                var side = config.ControlCabinetDoorCount == 1 ? config.ControlCabinetHingeSide : (door == 0 ? "left" : "right");
                AddPlacement(model, "Besturingskast deur " + (door + 1) + " scharnierend " + side, AssemblyComponentKind.Purchased,
                    cabinetDoorWidth, cabinetDoorThicknessMm, cabinetDoorHeight, doorX, cabinetCenterY, cabinetDoorCenterZ, "hardware-cabinet-door", "box");
            }
            AddCabinetDoorGapOutline(model, cabinetCenterX, cabinetCenterY, config.ControlCabinetWidthMm, cabinetHeight,
                frontProfileOuterZ - config.LowerPanelMaterial.ThicknessMm);

            // De verticale kaststeunen sluiten met hun kop tegen de onderzijde van de
            // 40x80-werkbladligger aan. Ze mogen niet door het volume van die ligger lopen.
            var sideHeight = sideTop - bottom;
            var leftSupportX = cabinetCenterX - config.ControlCabinetWidthMm / 2.0 - 20.0;
            var rightSupportX = cabinetCenterX + config.ControlCabinetWidthMm / 2.0 + 20.0;
            var connections = AddProfile(model, "Besturingskast zijprofiel 40x40", config.TopBeamProfile, sideHeight, 2, "Verticaal doorlopend tot en kop tegen onderzijde werkbladdrager", config.SawingMode, true);
            AddPlacement(model, "Besturingskast zijprofiel links", AssemblyComponentKind.Profile, 40, 40, sideHeight, leftSupportX, bottom + sideHeight / 2.0, frontProfileCenterZ, "profile", "box");
            AddPlacement(model, "Besturingskast zijprofiel rechts", AssemblyComponentKind.Profile, 40, 40, sideHeight, rightSupportX, bottom + sideHeight / 2.0, frontProfileCenterZ, "profile", "box");
            connections += AddProfile(model, "Besturingskast topprofiel 40x40", config.TopBeamProfile, config.ControlCabinetWidthMm, 1, "Horizontale insluiting bovenzijde kast", config.SawingMode, true);
            AddPlacement(model, "Besturingskast topprofiel", AssemblyComponentKind.Profile, config.ControlCabinetWidthMm, 40, 40, cabinetCenterX, bottom + cabinetHeight + 20, frontProfileCenterZ, "profile", "box");
            AddBlackConnectorHole(model, "Besturingskast links onder", leftSupportX + 20.5, bottom + 20, frontProfileCenterZ, "x");
            AddBlackConnectorHole(model, "Besturingskast rechts onder", rightSupportX - 20.5, bottom + 20, frontProfileCenterZ, "x");
            AddBlackConnectorHole(model, "Besturingskast links boven", leftSupportX + 20.5, sideTop - 20, frontProfileCenterZ, "x");
            AddBlackConnectorHole(model, "Besturingskast rechts boven", rightSupportX - 20.5, sideTop - 20, frontProfileCenterZ, "x");
            AddBlackConnectorHole(model, "Besturingskast topprofiel links", cabinetCenterX - config.ControlCabinetWidthMm / 2 + 20, bottom + cabinetHeight - 0.5, frontProfileCenterZ, "y");
            AddBlackConnectorHole(model, "Besturingskast topprofiel rechts", cabinetCenterX + config.ControlCabinetWidthMm / 2 - 20, bottom + cabinetHeight - 0.5, frontProfileCenterZ, "y");
            AddFrontCabinetHplPanels(model, config, carrierTopMm, bottom, cabinetHeight, cabinetCenterX, leftSupportX, rightSupportX, frontProfileOuterZ - config.LowerPanelMaterial.ThicknessMm / 2.0);
            model.DesignNotes.Add("Besturingskast staat " + (config.ControlCabinetPosition == "right" ? "rechts" : "links") + " in het voorvak; kastbehuizing en kastdeuren zijn RAL 7035 lichtgrijs. De opbouwdeuren staan 4 mm vrij van het kastfront, zijn 17 mm dik en steken daardoor aan de voorzijde totaal 21 mm uit. De 4 mm spleetcontour wordt rondom zwart weergegeven.");
            return connections;
        }

        private static void AddCabinetDoorGapOutline(WorkbenchModel model, double centerX, double centerY, double openingWidth, double openingHeight, double frontSurfaceZ)
        {
            const double lineWidthMm = 4.0;
            const double lineDepthMm = 2.0;
            var lineZ = frontSurfaceZ - lineDepthMm / 2.0;
            const string prefix = "Zwarte spleet rondom kastopening ";
            AddPlacement(model, prefix + "boven", AssemblyComponentKind.Purchased, openingWidth, lineDepthMm, lineWidthMm,
                centerX, centerY + openingHeight / 2.0 - lineWidthMm / 2.0, lineZ, "hardware-door-gap", "box");
            AddPlacement(model, prefix + "onder", AssemblyComponentKind.Purchased, openingWidth, lineDepthMm, lineWidthMm,
                centerX, centerY - openingHeight / 2.0 + lineWidthMm / 2.0, lineZ, "hardware-door-gap", "box");
            AddPlacement(model, prefix + "links", AssemblyComponentKind.Purchased, lineWidthMm, lineDepthMm, Math.Max(1.0, openingHeight - 2.0 * lineWidthMm),
                centerX - openingWidth / 2.0 + lineWidthMm / 2.0, centerY, lineZ, "hardware-door-gap", "box");
            AddPlacement(model, prefix + "rechts", AssemblyComponentKind.Purchased, lineWidthMm, lineDepthMm, Math.Max(1.0, openingHeight - 2.0 * lineWidthMm),
                centerX + openingWidth / 2.0 - lineWidthMm / 2.0, centerY, lineZ, "hardware-door-gap", "box");
        }

        private static void AddFrontCabinetHplPanels(WorkbenchModel model, MachineBaseConfig config, double carrierTopMm, double cabinetBottomMm, double cabinetHeightMm, double cabinetCenterX, double leftSupportX, double rightSupportX, double frontPanelZ)
        {
            const double holeDiameterMm = 6.5;
            var transitionY = carrierTopMm - 40.0;
            var cabinetTop = cabinetBottomMm + cabinetHeightMm;
            var totalHoleCount = 0;
            var cabinetOnPositiveModelX = cabinetCenterX > 0;
            var frameOuterX = cabinetOnPositiveModelX ? config.WidthMm / 2.0 : -config.WidthMm / 2.0;
            var cabinetInnerX = cabinetCenterX + (cabinetOnPositiveModelX ? -config.ControlCabinetWidthMm / 2.0 : config.ControlCabinetWidthMm / 2.0);
            var cabinetOuterX = cabinetCenterX + (cabinetOnPositiveModelX ? config.ControlCabinetWidthMm / 2.0 : -config.ControlCabinetWidthMm / 2.0);
            var surroundLeft = Math.Min(frameOuterX, cabinetInnerX);
            var surroundRight = Math.Max(frameOuterX, cabinetInnerX);
            var surroundWidth = surroundRight - surroundLeft;
            var surroundCenterX = (surroundLeft + surroundRight) / 2.0;

            var aboveHeight = transitionY - cabinetTop;
            if (aboveHeight >= 40)
            {
                var above = SheetDrawing.CreateSheet("HPL 6mm doorlopende voorplaat boven besturingskast", config.LowerPanelMaterial, surroundWidth, aboveHeight);
                AddPanelHoles(above, 20.0, 0, above.LengthMm, holeDiameterMm);
                AddPanel(model, above, surroundCenterX, cabinetTop + aboveHeight / 2.0, frontPanelZ, AssemblyOrientation.SheetVerticalX, false);
                totalHoleCount += above.Holes.Count;
            }

            var outerStripLeft = Math.Min(frameOuterX, cabinetOuterX);
            var outerStripRight = Math.Max(frameOuterX, cabinetOuterX);
            var outerStripWidth = outerStripRight - outerStripLeft;
            if (outerStripWidth >= 40 && cabinetHeightMm >= 100)
            {
                var outerStrip = SheetDrawing.CreateSheet("HPL 6mm voorplaat buitenzijde besturingskast", config.LowerPanelMaterial, outerStripWidth, cabinetHeightMm);
                AddPanelHoles(outerStrip, 20.0, 0, outerStrip.LengthMm, holeDiameterMm);
                AddPanel(model, outerStrip, (outerStripLeft + outerStripRight) / 2.0, cabinetBottomMm + cabinetHeightMm / 2.0, frontPanelZ, AssemblyOrientation.SheetVerticalX, false);
                totalHoleCount += outerStrip.Holes.Count;
            }

            var belowHeight = cabinetBottomMm - FloorToUprightMm;
            if (surroundWidth >= 80 && belowHeight >= 20)
            {
                var below = SheetDrawing.CreateSheet("HPL 6mm doorlopende voorplaat onder besturingskast", config.LowerPanelMaterial, surroundWidth, belowHeight);
                foreach (var x in EvenPositions(50.0, below.LengthMm - 50.0, 250.0))
                {
                    AddPanelHole(below, x, below.WidthMm / 2.0, holeDiameterMm, "onderste voorprofiel");
                }
                AddPanel(model, below, surroundCenterX, FloorToUprightMm + belowHeight / 2.0, frontPanelZ, AssemblyOrientation.SheetVerticalX, false);
                totalHoleCount += below.Holes.Count;
            }

            var cabinetOnRight = string.Equals(config.ControlCabinetPosition, "right", StringComparison.OrdinalIgnoreCase);
            var sidePanelLeft = cabinetOnPositiveModelX ? -config.WidthMm / 2.0 : rightSupportX - 20.0;
            var sidePanelRight = cabinetOnPositiveModelX ? leftSupportX + 20.0 : config.WidthMm / 2.0;
            var sidePanelWidth = sidePanelRight - sidePanelLeft;
            var sidePanelBottom = FloorToUprightMm;
            var sidePanelHeight = transitionY - sidePanelBottom;
            if (sidePanelWidth >= 80 && sidePanelHeight >= 100)
            {
                var sideName = cabinetOnRight ? "HPL 6mm voorplaat links naast besturingskast" : "HPL 6mm voorplaat rechts naast besturingskast";
                var side = SheetDrawing.CreateSheet(sideName, config.LowerPanelMaterial, sidePanelWidth, sidePanelHeight);
                AddPanelHoles(side, 20.0, 0, side.LengthMm, holeDiameterMm);
                AddPanel(model, side, (sidePanelLeft + sidePanelRight) / 2.0, sidePanelBottom + sidePanelHeight / 2.0, frontPanelZ, AssemblyOrientation.SheetVerticalX, false);
                totalHoleCount += side.Holes.Count;
            }

            if (totalHoleCount <= 0) return;
            model.Hardware.Add(new HardwareItem
            {
                Name = "Fabory laagbolkopschroef M6x16 voor HPL voorfront",
                ArticleNumber = "07151.060.016",
                Quantity = totalHoleCount,
                Unit = "st",
                Note = "ISO 7380-1, verzinkt; dezelfde bevestigingsregel als overige HPL-platen",
                ModelStatus = "Definitief artikel",
                BomStatus = "Inkoop"
            });
            model.Hardware.Add(new HardwareItem
            {
                Name = "TechXXL M6 T-moer groef 8 met brug en verende kogel voor HPL voorfront",
                ArticleNumber = "TIN 100242 / S208NSMS6",
                Quantity = totalHoleCount,
                Unit = "st",
                Note = "Eén per Ø6,5 mm HPL-gat; serie 8 / groef 8; 23×13,2×7,3 mm",
                ModelStatus = "Exact leveranciersartikel",
                BomStatus = "Inkoop vrijgegeven"
            });
            model.DesignNotes.Add("Onderste voorfront is met HPL 6 mm gesloten. Rond de besturingskast loopt het plaatwerk door als bovenstrook, buitenstijl en onderstrook; het tegenoverliggende vak is volledig beplaat. Gaten Ø6,5 mm, groef-offset 20 mm, eerste/laatste bevestiging circa 50 mm en tussenafstand maximaal 250 mm.");
        }

        private static int AddFrontProtection(WorkbenchModel model, MachineBaseConfig config, double carrierTopMm, double frameTopMm)
        {
            var openingBottom = carrierTopMm;
            var openingTop = frameTopMm - 40;
            var openingHeight = openingTop - openingBottom;
            var openingWidth = config.WidthMm - 2 * UprightWidthMm;
            var frontProfileOuterZ = -config.DepthMm / 2.0;
            var frontZ = frontProfileOuterZ + 20.0;
            if (string.Equals(config.FrontProtectionMode, "lightcurtain", StringComparison.OrdinalIgnoreCase))
            {
                AddPlacement(model, "Lichtgordijn zender", AssemblyComponentKind.Purchased, 28, 28, openingHeight, -openingWidth / 2 + 18, openingBottom + openingHeight / 2, frontProfileOuterZ, "hardware-safety", "box");
                AddPlacement(model, "Lichtgordijn ontvanger", AssemblyComponentKind.Purchased, 28, 28, openingHeight, openingWidth / 2 - 18, openingBottom + openingHeight / 2, frontProfileOuterZ, "hardware-safety", "box");
                model.Hardware.Add(new HardwareItem { Name = "Lichtgordijn set, lengte volgens opening", ArticleNumber = "LIGHT-CURTAIN-TBD", Quantity = 1, Unit = "set", Note = "Veiligheidsafstand en PL/SIL-selectie projectspecifiek valideren", ModelStatus = "Maat-envelop", BomStatus = "Merk/type nog selecteren" });
                return 0;
            }

            const double perimeterGap = 3;
            const double centerGap = 6;
            var doorCount = config.FrontDoorCount == 1 ? 1 : 2;
            var doorWidth = doorCount == 1 ? openingWidth - 2 * perimeterGap : (openingWidth - 2 * perimeterGap - centerGap) / 2.0;
            var doorHeight = openingHeight - 2 * perimeterGap;
            var connections = 0;
            for (var d = 0; d < doorCount; d++)
            {
                var centerX = doorCount == 1 ? 0 : (d == 0 ? -(doorWidth + centerGap) / 2.0 : (doorWidth + centerGap) / 2.0);
                var prefix = "Veiligheidsdeur " + (d + 1);
                connections += AddProfile(model, prefix + " horizontaal 40x40", config.TopBeamProfile, doorWidth, 2, "Profieldeur met standaard M8-verbinding", config.SawingMode, true);
                connections += AddProfile(model, prefix + " verticaal 40x40", config.TopBeamProfile, doorHeight - 80, 2, "Profieldeur; verbindingen zitten in de koppen van de horizontale regels", config.SawingMode, false);
                AddPlacement(model, prefix + " boven", AssemblyComponentKind.Profile, doorWidth, 40, 40, centerX, openingTop - perimeterGap - 20, frontZ, "profile", "box");
                AddPlacement(model, prefix + " onder", AssemblyComponentKind.Profile, doorWidth, 40, 40, centerX, openingBottom + perimeterGap + 20, frontZ, "profile", "box");
                AddPlacement(model, prefix + " links", AssemblyComponentKind.Profile, 40, 40, doorHeight - 80, centerX - doorWidth / 2 + 20, openingBottom + openingHeight / 2, frontZ, "profile", "box");
                AddPlacement(model, prefix + " rechts", AssemblyComponentKind.Profile, 40, 40, doorHeight - 80, centerX + doorWidth / 2 - 20, openingBottom + openingHeight / 2, frontZ, "profile", "box");
                AddStandardConnection(model, prefix + " frame", prefix + " boven", ProfileEnd.A, prefix + " links");
                AddStandardConnection(model, prefix + " frame", prefix + " boven", ProfileEnd.B, prefix + " rechts");
                AddStandardConnection(model, prefix + " frame", prefix + " onder", ProfileEnd.A, prefix + " links");
                AddStandardConnection(model, prefix + " frame", prefix + " onder", ProfileEnd.B, prefix + " rechts");
                foreach (var hx in new[] { centerX - doorWidth / 2 + 20, centerX + doorWidth / 2 - 20 })
                {
                    AddBlackConnectorHole(model, prefix + " onder", hx, openingBottom + perimeterGap - 0.5, frontZ, "y");
                    AddBlackConnectorHole(model, prefix + " boven", hx, openingTop - perimeterGap - 40.5, frontZ, "y");
                }
                var infill = SheetDrawing.CreateSheet(prefix + " acryl 6mm", config.UpperPanelMaterial, doorWidth - 80, doorHeight - 80);
                AddPanel(model, infill, centerX, openingBottom + openingHeight / 2, frontZ - 3, AssemblyOrientation.SheetVerticalX, true);
                var hingeAtRight = doorCount == 2 ? d == 1 : string.Equals(config.FrontSingleDoorHingeSide, "right", StringComparison.OrdinalIgnoreCase);
                var hingeX = hingeAtRight ? centerX + doorWidth / 2 : centerX - doorWidth / 2;
                var doorHingeProfile = prefix + (hingeAtRight ? " rechts" : " links");
                var frameHingeProfile = hingeAtRight ? "Staander rechtsvoor" : "Staander linksvoor";
                AddDoorHinge(model, hingeX, openingBottom + openingHeight * .28, frontZ, prefix + " onder", doorHingeProfile, frameHingeProfile, prefix + " scharnieren");
                AddDoorHinge(model, hingeX, openingBottom + openingHeight * .72, frontZ, prefix + " boven", doorHingeProfile, frameHingeProfile, prefix + " scharnieren");
            }
            var hingeCount = doorCount * 2;
            model.Hardware.Add(new HardwareItem { Name = "TechXXL scharnier 8 40x40 licht", ArticleNumber = "S208SCHALL4040", Quantity = hingeCount, Unit = "st", Note = "TechXXL TIN 102930; groef 8, I-type; 103x44x10 mm; intern tussen basisframe en deur", ModelStatus = "Definitief leveranciersartikel", BomStatus = "Inkoop" });
            model.Hardware.Add(new HardwareItem { Name = "TechXXL verzonken inbusbout M6x12 verzinkt", ArticleNumber = "S208SKS612V", Quantity = hingeCount * 4, Unit = "st", Note = "TechXXL TIN 100691; vereist vier per scharnier", ModelStatus = "Definitief leveranciersartikel", BomStatus = "Inkoop" });
            model.Hardware.Add(new HardwareItem { Name = "TechXXL T-moer groef 8 met brug M6", ArticleNumber = "S208NSMS6", Quantity = hingeCount * 4, Unit = "st", Note = "TechXXL TIN 100242; vereist vier per scharnier", ModelStatus = "Definitief leveranciersartikel", BomStatus = "Inkoop" });
            return connections;
        }

        private static void AddDoorHinge(WorkbenchModel model, double x, double y, double z, string suffix,
            string doorProfile, string frameProfile, string instructionGroup)
        {
            const double doorProfileDepthMm = 40.0;
            const double hingeDepthMm = 10.0;
            var doorProfileFrontSurfaceZ = z - doorProfileDepthMm / 2.0;
            var hingeCenterZ = doorProfileFrontSurfaceZ - hingeDepthMm / 2.0;
            AddPlacement(model, "Scharnier 8 40x40 licht " + suffix, AssemblyComponentKind.Purchased,
                44, hingeDepthMm, 103, x, y, hingeCenterZ, "hardware-hinge", "pa40-hinge", "techxxl_hinge_8_40x40_light");
            var connection = new AssemblyConnection
            {
                ConnectionId = "machinebasis-hinge-" + StableId(suffix),
                WorkflowId = "hinge-sliding-nut-v1",
                JointType = AssemblyJointType.HingeSlidingNut,
                InstructionGroup = instructionGroup,
                TappedMemberId = StableId(doorProfile),
                TappedPartName = doorProfile,
                SlotMemberId = StableId(frameProfile),
                SlotPartName = frameProfile,
                SlotFace = "interne, naar elkaar gerichte T-sleuven van deurprofiel en staander",
                SlotLane = "S1 op beide 40x40 groef-8-profielen",
                ConnectorId = "techxxl_hinge_8_40x40_light",
                FastenerStandardId = "techxxl-hinge-8-40x40-4xm6x12-4x-nut-m6",
                FastenerId = "techxxl_countersunk_socket_m6x12_galvanized",
                FastenerThreadMm = 6,
                HexKeyAcrossFlatsMm = 4,
                FastenerAxisOrder = "vier M6x12-bouten → verzonken scharniergaten → vier S208NSMS6-moeren in groef 8",
                AccessFace = "intern tussen basisframe en deur; leveranciersmontage volgens TIN 102930",
                Tool = "inbussleutel SW4",
                Status = AssemblyDataStatus.Confirmed
            };
            model.AssemblyConnections.Add(connection);
        }

        private static void AddBlackConnectorHole(WorkbenchModel model, string name, double x, double y, double z, string axis)
        {
            var alongX = string.Equals(axis, "x", StringComparison.OrdinalIgnoreCase);
            var alongY = string.Equals(axis, "y", StringComparison.OrdinalIgnoreCase);
            AddPlacement(model, "Zwart Ø7 verbindingsgat " + name, AssemblyComponentKind.Purchased,
                alongX ? 1 : 7, alongY ? 7 : (alongX ? 7 : 1), alongY ? 1 : 7,
                x, y, z, "machining-hole", alongX ? "black-hole-x" : (alongY ? "black-hole-y" : "black-hole-z"));
        }

        private static void AddBlackEndCaps(WorkbenchModel model, MachineBaseConfig config, double frameTopMm)
        {
            model.Hardware.Add(new HardwareItem { Name = "Eindkap 40x80 zwart serie 10", ArticleNumber = "ENDCAP-40X80-BLACK", Quantity = 4, Unit = "st", Note = "Alle zichtbare staanderkoppen altijd zwart", ModelStatus = "Functioneel gemodelleerd", BomStatus = "Leverancierartikel koppelen" });
            var x = (config.WidthMm - UprightWidthMm) / 2.0;
            var z = (config.DepthMm - UprightDepthMm) / 2.0;
            foreach (var px in new[] { -x, x }) foreach (var pz in new[] { -z, z }) AddPlacement(model, "Zwarte eindkap 40x80", AssemblyComponentKind.Purchased, 40, 80, 2, px, frameTopMm - 1, pz, "hardware-cap", "box");
        }

        private static void AddStandardConnectorHardware(WorkbenchModel model, int connectionCount)
        {
            model.Hardware.Add(new HardwareItem { Name = "TechXXL standaardverbinder 8 40", ArticleNumber = "techxxl_standard_connector_8_40", Quantity = connectionCount, Unit = "st", Note = "TIN 100342; exacte plaat-, klauw-, hart- en insteekgeometrie uit componentmasterdata", ModelStatus = "ExactSupplierGeometry", BomStatus = "Inkoop vrijgegeven" });
            model.Hardware.Add(new HardwareItem { Name = "TechXXL bolkop-inbusbout ISO 7380 M8x25", ArticleNumber = "techxxl_button_head_iso7380_m8x25", Quantity = connectionCount, Unit = "st", Note = "Artikel- en maatvoering uit componentmasterdata", ModelStatus = "Leveranciersartikel gekoppeld", BomStatus = "Exact artikel" });
            model.DesignNotes.Add("Vaste groef-8 standaardverbinder-volgorde uit masterdata. D0-D3-vlak, S-baan, Ø7-toegangsgat en kop-A-positie worden per fysieke verbinding afgeleid; aanhaalmoment is geen vrijgave-eis.");
        }

        private static int AddProfile(WorkbenchModel model, string name, Material material, double lengthMm, int quantity, string note, ProfileSawingMode sawingMode, bool standardConnections)
        {
            model.Profiles.Add(new ProfilePart { Name = name, Material = material, LengthMm = lengthMm, Quantity = quantity, OrientationNote = note, BomStatus = "Frame fase 1" });
            model.ProfileOperations.Add(new ProfileOperation
            {
                ProfileId = (material == null ? "profile" : material.Id) + "_" + name.Replace(' ', '_'),
                PartName = name,
                Quantity = quantity,
                Material = material,
                ProfileLengthMm = lengthMm,
                Sequence = 1,
                Kind = ProfileOperationKind.SawCut,
                SawAngleDeg = 90,
                WorkOrigin = "Kop A",
                MachineHint = sawingMode == ProfileSawingMode.InHouse ? "SAW_CUT" : "SUPPLIER_CUT_TO_LENGTH",
                ExecutionParty = sawingMode == ProfileSawingMode.InHouse ? "WERKPLAATS" : "INKOOP/LEVERANCIER",
                Note = note
            });
            return standardConnections ? quantity * 2 : 0;
        }

        private static void AddPlacement(WorkbenchModel model, string name, AssemblyComponentKind kind, double length, double width, double height, double x, double y, double z, string visualKind, string shape, string componentId = null)
        {
            model.AssemblyPlacements.Add(new AssemblyPlacement { MemberId = StableId(name), Kind = kind, PartName = name, ComponentId = componentId, LengthMm = length, WidthMm = width, HeightMm = height, Xmm = x, Ymm = y, Zmm = z, Orientation = AssemblyOrientation.Default, VisualKind = visualKind, Shape = shape });
        }

        private static void AddStandardConnection(WorkbenchModel model, string group, string tappedPart, ProfileEnd tappedEnd, string slotPart)
        {
            const string connectorId = "techxxl_standard_connector_8_40";
            const string fastenerId = "techxxl_button_head_iso7380_m8x25";
            var hardwareCatalog = new AssemblyHardwareRenderContractService();
            var defaultHexKeyAcrossFlatsMm = hardwareCatalog.RequiredFastenerSocketAcrossFlatsMm(fastenerId);
            var supplierFastenerThreadMm = hardwareCatalog.RequiredFastenerThreadDiameterMm(fastenerId);
            var supplierAccessHoleDiameterMm = hardwareCatalog.RequiredConnectorAccessHoleDiameterMm(connectorId);
            var tappedMaterial = MaterialForPlacement(model, tappedPart);
            var coreHoleCount = new ProfileSlotGeometryCatalog().FindRequired(tappedMaterial.Id).CalculatedCoreHoleCountPerEnd;
            for (var coreHoleIndex = 1; coreHoleIndex <= coreHoleCount; coreHoleIndex++)
            {
            var connection = new AssemblyConnection
            {
                ConnectionId = "machinebasis-" + StableId(tappedPart) + "-" + tappedEnd.ToString().ToLowerInvariant() + "-k" + coreHoleIndex,
                WorkflowId = "standard-connector-v1",
                JointType = AssemblyJointType.StandardConnector,
                InstructionGroup = group,
                TappedMemberId = StableId(tappedPart),
                TappedPartName = tappedPart,
                TappedEnd = tappedEnd,
                CoreHoleIndex = coreHoleIndex,
                SlotMemberId = StableId(slotPart),
                SlotPartName = slotPart,
                SlotFace = "wordt uit fysieke profielplaatsing afgeleid",
                SlotLane = "wordt uit fysieke profielplaatsing afgeleid",
                ConnectorId = connectorId,
                FastenerStandardId = "standard-profile-connector-groove8-m8",
                FastenerId = fastenerId,
                FastenerThreadMm = supplierFastenerThreadMm,
                HexKeyAcrossFlatsMm = defaultHexKeyAcrossFlatsMm,
                ToolPassageClearanceMm = 0,
                DrillIncrementMm = 0,
                AccessHoleDiameterMm = supplierAccessHoleDiameterMm,
                AccessHoleCalculation = "Leveranciersveld Toegangsgatdiameter mm uit componentmasterdata",
                AccessHoleOffsetMm = 0,
                AccessHoleReference = "wordt uit fysieke profielplaatsing afgeleid",
                AccessFace = "wordt uit fysieke profielplaatsing afgeleid",
                FastenerAxisOrder = "inbuskop → standaardverbinder in T-sleuf → bout → getapte kop van kopprofiel",
                Tool = "inbussleutel SW" + defaultHexKeyAcrossFlatsMm.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                Status = AssemblyDataStatus.Provisional
            };
            connection.OpenData.Add("Fysieke D0-D3/S1..Sn-koppeling en toegangsgatpositie worden na sticker- en traceertoekenning afgeleid.");
            model.AssemblyConnections.Add(connection);
            }
        }

        private static Material MaterialForPlacement(WorkbenchModel model, string partName)
        {
            var memberId = StableId(partName);
            var placement = model.AssemblyPlacements.Single(item => string.Equals(item.MemberId, memberId, StringComparison.OrdinalIgnoreCase));
            var dimensions = new[] { placement.LengthMm, placement.WidthMm, placement.HeightMm }.OrderBy(value => value).ToArray();
            var crossA = dimensions[0];
            var crossB = dimensions[1];
            var length = dimensions[2];
            var materials = model.Profiles
                .Where(profile => profile.Material != null && Math.Abs(profile.LengthMm - length) < 0.01)
                .Where(profile =>
                {
                    var materialCross = new[] { profile.Material.WidthMm, profile.Material.HeightMm }.OrderBy(value => value).ToArray();
                    return Math.Abs(materialCross[0] - crossA) < 0.01 && Math.Abs(materialCross[1] - crossB) < 0.01;
                })
                .Select(profile => profile.Material)
                .GroupBy(material => material.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToArray();
            if (materials.Length != 1)
                throw new InvalidOperationException("Standaardverbinding geblokkeerd: profielmateriaal voor " + partName
                    + " is niet eenduidig uit de assemblygeometrie af te leiden.");
            return materials[0];
        }

        private static string IntermediateSlotBase(string name)
        {
            if (name.StartsWith("Onderframe", StringComparison.Ordinal)) return "Onderligger";
            if (name.StartsWith("Bladframe", StringComparison.Ordinal)) return "Bladligger";
            return "Bovenligger";
        }

        private static string StableId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "member";
            var chars = value.Trim().ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray();
            var id = new string(chars);
            while (id.Contains("--")) id = id.Replace("--", "-");
            return id.Trim('-');
        }

        private static double ProfileVerticalSize(Material profile)
        {
            return profile == null ? 40 : Math.Max(profile.WidthMm, profile.HeightMm);
        }

        private static double ProfileHorizontalSize(Material profile)
        {
            return profile == null ? 40 : Math.Min(profile.WidthMm, profile.HeightMm);
        }

        private static void Validate(MachineBaseConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (config.WidthMm < 1000 || config.WidthMm > 6000) throw new ArgumentException("Machinebasis breedte moet tussen 1000 en 6000 mm liggen.");
            if (config.DepthMm < 600 || config.DepthMm > 2000) throw new ArgumentException("Machinebasis diepte moet tussen 600 en 2000 mm liggen.");
            if (config.HeightMm < 1000 || config.HeightMm > 2300) throw new ArgumentException("Machinebasis hoogte moet tussen 1000 en 2300 mm liggen.");
            if (config.WorktopHeightMm < 600 || config.WorktopHeightMm > 1000) throw new ArgumentException("Bladhoogte moet tussen 600 en 1000 mm liggen.");
            if (config.WorktopHeightMm + 80 > config.HeightMm) throw new ArgumentException("Totale hoogte moet minimaal 80 mm boven de bladhoogte liggen.");
            if (config.WorktopHeightMm <= FloorToUprightMm + 80) throw new ArgumentException("Bladhoogte moet voldoende boven wiel, voetplaat en onderliggers liggen.");
            if (config.WorktopIntermediateBeamMaxSpacingMm < 300 || config.WorktopIntermediateBeamMaxSpacingMm > 1000) throw new ArgumentException("Maximale h.o.h.-afstand van bladframe-tussenliggers moet tussen 300 en 1000 mm liggen.");
        }
    }
}
