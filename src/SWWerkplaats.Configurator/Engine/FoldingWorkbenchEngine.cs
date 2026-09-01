using System;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Drawing;

namespace SWWerkplaats.Configurator.Engine
{
    public sealed class FoldingWorkbenchEngine
    {
        public WorkbenchModel Build(FoldingWorkbenchConfig config)
        {
            Validate(config);
            var model = new WorkbenchModel
            {
                ProductId = "opvouwbare_werktafel",
                ProjectName = config.ProjectName
            };
            var t = config.PanelMaterial.ThicknessMm;
            var underframeLength = config.LengthMm - 2.0 * config.UnderframeInsetShortEdgeMm;
            var underframeDepth = config.WidthMm - 2.0 * config.UnderframeInsetLongEdgeMm;
            var bodyHeight = config.HeightMm - t;
            var endX = underframeLength / 2.0;
            var sideZ = underframeDepth / 2.0;

            var top = SheetDrawing.CreateSheet("Uitneembaar werkblad", config.PanelMaterial, config.LengthMm, config.WidthMm);
            top.CornerRadiusMm = config.CornerRadiusMm;
            AddTopSlots(top, config, underframeLength, underframeDepth, t);
            AddSheet(model, top, 0, config.HeightMm - t / 2.0, 0, AssemblyOrientation.SheetHorizontal);

            var frontLongPanel = CreateLongPanel("Langspaneel voor", config, underframeLength, bodyHeight, t);
            AddSheet(model, frontLongPanel, 0, (bodyHeight + t) / 2.0, -sideZ, AssemblyOrientation.SheetVerticalX);
            var rearLongPanel = CreateLongPanel("Langspaneel achter", config, underframeLength, bodyHeight, t);
            rearLongPanel.MirrorInNestingX = true;
            AddSheet(model, rearLongPanel, 0, (bodyHeight + t) / 2.0, sideZ, AssemblyOrientation.SheetVerticalX);

            AddFoldingEnd(model, config, "links", -endX, underframeDepth, bodyHeight, t, true);
            AddFoldingEnd(model, config, "rechts", endX, underframeDepth, bodyHeight, t, false);
            AddHinges(model, config, endX, sideZ, bodyHeight, t);

            model.Hardware.Add(new HardwareItem
            {
                Name = "AXA normaal kogellagerscharnier RVS 76×76×2,5 mm",
                ArticleNumber = config.HingeArticleNumber,
                PricingKey = config.HingeComponentId,
                Quantity = 6,
                Unit = "st",
                Note = "Eén AXA 1KL227676N per verticale vouwas. Fabrikanttekening: drie gaten per blad en knoop Ø10,8 mm. Exacte gatcoördinaten, gat-/verzinkmaat en tafelbevestigers blijven open tot sample-inmeting.",
                ModelStatus = "ProvisionalRenderEnvelope",
                BomStatus = "Voorlopig"
            });

            model.DesignNotes.Add("Conceptopbouw volgens fotoreferentie: twee vaste lange framepanelen en per korte zijde twee volledige framevormige vouwpanelen met zes verticale normale scharnierassen. Iedere as bevat twee fysieke scharnierbladen met eigen knoopsegmenten en één verticale pen.");
            model.DesignNotes.Add("Het werkblad steekt langs beide lange bladranden " + config.UnderframeInsetLongEdgeMm.ToString("0.###") + " mm en langs beide korte bladranden " + config.UnderframeInsetShortEdgeMm.ToString("0.###") + " mm voorbij het onderstel. Daardoor wordt de onderstellengte bladlengte min " + (2.0 * config.UnderframeInsetShortEdgeMm).ToString("0.###") + " mm en de ondersteldiepte bladbreedte min " + (2.0 * config.UnderframeInsetLongEdgeMm).ToString("0.###") + " mm; alle paneelnokken en werkbladslots volgen dezelfde afleiding.");
            model.DesignNotes.Add("De buitencontour volgt de fotoreferentie: ieder vast langspaneel staat op drie geïntegreerde voeten onder de linker-, midden- en rechterstijl; ieder kort half vouwpaneel staat op twee voeten onder zijn beide stijlen. Tussen de voeten ligt de onderrand " + config.IntegratedFootReliefHeightMm.ToString("0.###") + " mm vrij van de vloer.");
            model.DesignNotes.Add("Het losse blad zakt gereedschapsloos met zes nokken op de vaste langspanelen en acht nokken op de korte vouwpanelen in veertien doorlopende slots en borgt de uitgeklapte stand door geometrie en zwaartekracht.");
            model.DesignNotes.Add("Gat-/nokspeling is " + config.JointClearanceMm.ToString("0.###") + " mm per zijde; dogbones volgen de gekoppelde freesdiameter van " + config.DogboneToolDiameterMm.ToString("0.###") + " mm.");
            model.DesignNotes.Add("De panelen houden " + config.HingeGapMm.ToString("0.###") + " mm totale voeg per verticale scharnieras; de Ø" + config.HingeBarrelDiameterMm.ToString("0.###") + "-mm knoop ligt aan de binnenzijde van de platen en niet tussen de plaatranden. Conceptpakket in gevouwen stand: vier plaatdiktes plus " + config.FoldedClearanceMm.ToString("0.###") + " mm scharnierruimte. Wielen en wielgaten zijn niet opgenomen.");
            model.DesignNotes.Add("Niet productievrij: AXA publiceert voor dit artikel wel het gatenbeeld (3 per blad) en 4,0×40-mm deurschroeven, maar geen bemaatte gatcoördinaten of verzinkgeometrie. Voor 18-mm plaatmateriaal moeten proefstuk en definitieve bevestigerkeuze nog worden gevalideerd.");
            return model;
        }

        private static SheetPart CreateLongPanel(string name, FoldingWorkbenchConfig config, double length, double bodyHeight, double t)
        {
            var frame = SheetDrawing.CreateSheet(name, config.PanelMaterial, length, bodyHeight + t);
            frame.CustomContourCornerRadiusMm = config.CornerRadiusMm;
            AddFootedTabbedContour(frame, config, bodyHeight, t, LongPanelTabCenters(length, config),
                config.TabWidthMm, LongPanelFootCenters(length, config));
            AddLongPanelOpenings(frame, config, length, bodyHeight);
            return frame;
        }

        private static SheetPart CreateShortFoldingPanel(string name, FoldingWorkbenchConfig config, double length,
            double bodyHeight, double t)
        {
            var panel = SheetDrawing.CreateSheet(name, config.PanelMaterial, length, bodyHeight + t);
            panel.CustomContourCornerRadiusMm = config.CornerRadiusMm;
            AddFootedTabbedContour(panel, config, bodyHeight, t, ShortPanelHalfTabCenters(length, config),
                config.TabWidthMm, ShortPanelFootCenters(length, config));
            AddFrameOpening(panel, config, length, bodyHeight, "Kort vouwpaneel gewichtsuitsparing");
            return panel;
        }

        private static void AddLongPanelOpenings(SheetPart frame, FoldingWorkbenchConfig config, double length,
            double bodyHeight)
        {
            var openingWidth = (length - 3.0 * config.FrameStileWidthMm) / 2.0;
            var openingHeight = bodyHeight - config.BottomRailHeightMm - config.TopRailHeightMm;
            SheetOperations.AddThroughCutout(frame, "Langspaneel gewichtsuitsparing links", config.FrameStileWidthMm,
                config.BottomRailHeightMm, openingWidth, openingHeight, OperationFace.CenterPlane,
                "Twee openingen laten een volledige middenstijl onder de middelste nok staan; definitieve breedtes blijven geblokkeerd tot belasting- en stijfheidscontrole.");
            SheetOperations.AddThroughCutout(frame, "Langspaneel gewichtsuitsparing rechts",
                2.0 * config.FrameStileWidthMm + openingWidth, config.BottomRailHeightMm,
                openingWidth, openingHeight, OperationFace.CenterPlane,
                "Twee openingen laten een volledige middenstijl onder de middelste nok staan; definitieve breedtes blijven geblokkeerd tot belasting- en stijfheidscontrole.");
        }

        private static void AddFrameOpening(SheetPart frame, FoldingWorkbenchConfig config, double length,
            double bodyHeight, string name)
        {
            SheetOperations.AddThroughCutout(frame, name, config.FrameStileWidthMm,
                config.BottomRailHeightMm, length - 2.0 * config.FrameStileWidthMm,
                bodyHeight - config.BottomRailHeightMm - config.TopRailHeightMm, OperationFace.CenterPlane,
                "Brede stijlen en regels volgen de fotoreferentie; definitieve breedtes blijven geblokkeerd tot belasting- en stijfheidscontrole.");
        }

        private static void AddFootedTabbedContour(SheetPart part, FoldingWorkbenchConfig config,
            double bodyHeight, double t, double[] tabCenters, double tabWidth, double[] footCenters)
        {
            AddFootedBottomContour(part, footCenters, config.IntegratedFootWidthMm,
                config.IntegratedFootReliefHeightMm);
            part.CustomContour.Add(new SheetContourPoint(part.LengthMm, bodyHeight));
            for (var index = tabCenters.Length - 1; index >= 0; index--)
            {
                var start = tabCenters[index] - tabWidth / 2.0;
                var end = tabCenters[index] + tabWidth / 2.0;
                part.CustomContour.Add(new SheetContourPoint(end, bodyHeight));
                part.CustomContour.Add(new SheetContourPoint(end, bodyHeight + t));
                part.CustomContour.Add(new SheetContourPoint(start, bodyHeight + t));
                part.CustomContour.Add(new SheetContourPoint(start, bodyHeight));
            }
            part.CustomContour.Add(new SheetContourPoint(0, bodyHeight));
        }

        private static void AddFootedBottomContour(SheetPart part, double[] footCenters,
            double footWidth, double reliefHeight)
        {
            if (footCenters == null || footCenters.Length < 2)
                throw new InvalidOperationException("Een framepaneel vereist minimaal twee geïntegreerde voeten.");

            part.CustomContour.Add(new SheetContourPoint(0, 0));
            for (var index = 0; index < footCenters.Length - 1; index++)
            {
                var currentEnd = footCenters[index] + footWidth / 2.0;
                var nextStart = footCenters[index + 1] - footWidth / 2.0;
                part.CustomContour.Add(new SheetContourPoint(currentEnd, 0));
                part.CustomContour.Add(new SheetContourPoint(currentEnd, reliefHeight));
                part.CustomContour.Add(new SheetContourPoint(nextStart, reliefHeight));
                part.CustomContour.Add(new SheetContourPoint(nextStart, 0));
            }
            part.CustomContour.Add(new SheetContourPoint(part.LengthMm, 0));
        }

        private static double[] LongPanelFootCenters(double length, FoldingWorkbenchConfig config)
        {
            return new[] { config.FrameStileWidthMm / 2.0, length / 2.0, length - config.FrameStileWidthMm / 2.0 };
        }

        private static double[] ShortPanelFootCenters(double length, FoldingWorkbenchConfig config)
        {
            return new[] { config.FrameStileWidthMm / 2.0, length - config.FrameStileWidthMm / 2.0 };
        }

        private static double[] LongPanelTabCenters(double length, FoldingWorkbenchConfig config)
        {
            if (config.TabsPerLongPanel != 3)
                throw new InvalidOperationException("Conceptgeometrie vereist exact drie bladnokken per vast langspaneel.");
            var center = Math.Max(config.TabWidthMm / 2.0, config.FrameStileWidthMm / 2.0);
            return new[] { center, length / 2.0, length - center };
        }

        private static double[] ShortPanelHalfTabCenters(double halfDepth, FoldingWorkbenchConfig config)
        {
            if (config.TabsPerShortPanelHalf != 2)
                throw new InvalidOperationException("Conceptgeometrie vereist exact twee bladnokken per kort half vouwpaneel.");
            var center = Math.Max(config.TabWidthMm / 2.0, config.FrameStileWidthMm / 2.0);
            return new[] { center, halfDepth - center };
        }

        private static void AddTopSlots(SheetPart top, FoldingWorkbenchConfig config, double length, double depth, double t)
        {
            foreach (var z in new[] { -depth / 2.0, depth / 2.0 })
            foreach (var localCenter in LongPanelTabCenters(length, config))
            {
                var x = -length / 2.0 + localCenter;
                AddTopSlot(top, config, x, z, config.TabWidthMm + 2.0 * config.JointClearanceMm,
                    t + 2.0 * config.JointClearanceMm, "Noksleuf vast langspaneel proefstuk");
            }

            var halfDepth = depth / 2.0;
            var shortPanelLength = halfDepth - config.HingeGapMm;
            foreach (var x in new[] { -length / 2.0, length / 2.0 })
            foreach (var halfStart in new[] { -depth / 2.0 + config.HingeGapMm / 2.0, config.HingeGapMm / 2.0 })
            foreach (var localCenter in ShortPanelHalfTabCenters(shortPanelLength, config))
            {
                var z = halfStart + localCenter;
                AddTopSlot(top, config, x, z, t + 2.0 * config.JointClearanceMm,
                    config.TabWidthMm + 2.0 * config.JointClearanceMm, "Noksleuf kort vouwpaneel proefstuk");
            }
        }

        private static void AddTopSlot(SheetPart top, FoldingWorkbenchConfig config, double centerX, double centerZ,
            double sizeX, double sizeZ, string name)
        {
            var localX = centerX + top.LengthMm / 2.0 - sizeX / 2.0;
            var localY = centerZ + top.WidthMm / 2.0 - sizeZ / 2.0;
            var slot = SheetOperations.AddThroughCutout(top, name, localX, localY, sizeX, sizeZ,
                OperationFace.CenterPlane, "Volle bladdiepte; passing per zijde uit masterdata en dogbones uit gekoppeld freesrecord.");
            SheetOperations.RequireAssemblyOccupant(
                slot,
                "folding-workbench-top-tab-slot-" + Math.Round(centerX, 3).ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + "-" + Math.Round(centerZ, 3).ToString(System.Globalization.CultureInfo.InvariantCulture),
                0.75);
            AddDogbones(top, localX, localY, sizeX, sizeZ, config.DogboneToolDiameterMm);
        }

        private static void AddDogbones(SheetPart sheet, double x, double y, double length, double width, double diameter)
        {
            foreach (var px in new[] { x, x + length })
            foreach (var py in new[] { y, y + width })
            {
                sheet.Holes.Add(new SheetHole
                {
                    Name = "Dogbone hoekontlasting",
                    Xmm = px,
                    Ymm = py,
                    DiameterMm = diameter,
                    DepthMm = sheet.Material.ThicknessMm,
                    Face = OperationFace.CenterPlane,
                    DepthMode = OperationDepthMode.Through,
                    SupportKind = SheetHoleSupportKind.MachiningCutout
                });
            }
        }

        private static void AddFoldingEnd(WorkbenchModel model, FoldingWorkbenchConfig config, string end,
            double x, double span, double bodyHeight, double t, bool mirror)
        {
            var half = span / 2.0;
            var panelLength = half - config.HingeGapMm;
            var front = CreateShortFoldingPanel("Vouwpaneel " + end + " voor", config, panelLength, bodyHeight, t);
            front.MirrorInNestingX = mirror;
            AddSheet(model, front, x, (bodyHeight + t) / 2.0, -span / 4.0, AssemblyOrientation.SheetVerticalZ);

            var rear = CreateShortFoldingPanel("Vouwpaneel " + end + " achter", config, panelLength, bodyHeight, t);
            rear.MirrorInNestingX = !mirror;
            AddSheet(model, rear, x, (bodyHeight + t) / 2.0, span / 4.0, AssemblyOrientation.SheetVerticalZ);
        }

        private static void AddHinges(WorkbenchModel model, FoldingWorkbenchConfig config, double endX,
            double sideZ, double bodyHeight, double panelThickness)
        {
            var y = bodyHeight - config.TopRailHeightMm / 2.0;
            foreach (var end in new[]
            {
                new { Name = "links", X = -endX },
                new { Name = "rechts", X = endX }
            })
            foreach (var axis in new[]
            {
                new { Name = "voor", Z = -sideZ },
                new { Name = "midden", Z = 0.0 },
                new { Name = "achter", Z = sideZ }
            })
            {
                var inward = end.Name == "links" ? 1.0 : -1.0;
                var z = axis.Z;
                if (axis.Name == "voor") z += (panelThickness + config.HingeLeafThicknessMm) / 2.0;
                else if (axis.Name == "achter") z -= (panelThickness + config.HingeLeafThicknessMm) / 2.0;
                var placement = new AssemblyPlacement
                {
                    Kind = AssemblyComponentKind.Purchased,
                    PartName = "Scharnier " + end.Name + " " + axis.Name,
                    ComponentId = config.HingeComponentId,
                    LengthMm = config.HingeOpenWidthMm,
                    WidthMm = config.HingeLeafThicknessMm,
                    HeightMm = config.HingeHeightMm,
                    Xmm = end.X + inward * (panelThickness + config.HingeLeafThicknessMm) / 2.0,
                    Ymm = y,
                    Zmm = z,
                    RotationYDeg = 90,
                    VisualKind = "hardware-hinge",
                    Shape = "component-primitives"
                };

                string firstPart;
                string secondPart;
                if (axis.Name == "voor")
                {
                    firstPart = "Langspaneel voor";
                    secondPart = "Vouwpaneel " + end.Name + " voor";
                    placement.ComponentPartPoses.Add(new AssemblyComponentPartPose
                    {
                        PartId = "blad-a",
                        RotationYDeg = end.Name == "links" ? -90 : 90
                    });
                    Attach(placement, "blad-a", firstPart);
                    Attach(placement, "blad-b", secondPart);
                    Attach(placement, "pen", firstPart);
                }
                else if (axis.Name == "midden")
                {
                    firstPart = "Vouwpaneel " + end.Name + " voor";
                    secondPart = "Vouwpaneel " + end.Name + " achter";
                    Attach(placement, "blad-a", firstPart);
                    Attach(placement, "blad-b", secondPart);
                    Attach(placement, "pen", firstPart);
                }
                else
                {
                    firstPart = "Vouwpaneel " + end.Name + " achter";
                    secondPart = "Langspaneel achter";
                    placement.ComponentPartPoses.Add(new AssemblyComponentPartPose
                    {
                        PartId = "blad-b",
                        RotationYDeg = end.Name == "links" ? 90 : -90
                    });
                    Attach(placement, "blad-a", firstPart);
                    Attach(placement, "blad-b", secondPart);
                    Attach(placement, "pen", secondPart);
                }

                model.AssemblyPlacements.Add(placement);
                AddSheetHingeConnection(model, config, end.Name, axis.Name, firstPart, secondPart);
            }
        }

        private static void Attach(AssemblyPlacement placement, string partId, string partName)
        {
            placement.ComponentPartAttachments.Add(new AssemblyComponentPartAttachment
            {
                PartId = partId,
                PartName = partName
            });
        }

        private static void AddSheetHingeConnection(WorkbenchModel model, FoldingWorkbenchConfig config,
            string end, string axis, string firstPart, string secondPart)
        {
            var connection = new AssemblyConnection
            {
                ConnectionId = "folding-workbench-hinge-" + end + "-" + axis,
                WorkflowId = "folding-workbench-underframe",
                JointType = AssemblyJointType.SheetHinge,
                InstructionGroup = "vouwscharnieren",
                TappedPartName = firstPart,
                SlotPartName = secondPart,
                ConnectorId = config.HingeComponentId,
                Status = AssemblyDataStatus.Provisional
            };
            connection.OpenData.Add("Exacte hartcoördinaten, gatdiameter en verzinkgeometrie van AXA 1KL227676N zijn niet bemaat in de fabrikanttekening.");
            connection.OpenData.Add("Definitieve bevestiger voor 18-mm betonplex en belastbaarheid moeten met sample en proefstuk worden gevalideerd.");
            model.AssemblyConnections.Add(connection);
        }

        private static void AddSheet(WorkbenchModel model, SheetPart sheet, double x, double y, double z, AssemblyOrientation orientation)
        {
            SheetDrawing.AddSheetToModel(model, sheet, x, y, z, orientation);
            sheet.UseTabs = true;
            sheet.BomStatus = "Proefstuk";
        }

        private static void Validate(FoldingWorkbenchConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (config.PanelMaterial == null || config.PanelMaterial.ThicknessMm <= 0)
                throw new InvalidOperationException("Opvouwbare werktafel mist geldig plaatmateriaal.");
            if (config.LengthMm <= 0 || config.WidthMm <= 0 || config.HeightMm <= 0)
                throw new InvalidOperationException("Opvouwbare werktafel heeft ongeldige hoofdmaten.");
            var usableLength = config.PanelMaterial.SheetLengthMm - config.StockAllowanceMm;
            var usableWidth = config.PanelMaterial.SheetWidthMm - config.StockAllowanceMm;
            var fits = (config.LengthMm <= usableLength && config.WidthMm <= usableWidth)
                || (config.WidthMm <= usableLength && config.LengthMm <= usableWidth);
            if (!fits) throw new InvalidOperationException("Werkblad past na plaatmarge niet uit de gekoppelde handelsplaat.");

            var underframeLength = config.LengthMm - 2.0 * config.UnderframeInsetShortEdgeMm;
            var underframeDepth = config.WidthMm - 2.0 * config.UnderframeInsetLongEdgeMm;
            var bodyHeight = config.HeightMm - config.PanelMaterial.ThicknessMm;
            if (bodyHeight <= config.TopRailHeightMm + config.BottomRailHeightMm)
                throw new InvalidOperationException("Tafelhoogte laat onvoldoende opening tussen boven- en onderregel over.");
            if (config.IntegratedFootWidthMm <= 0 || config.IntegratedFootWidthMm > config.FrameStileWidthMm)
                throw new InvalidOperationException("Geïntegreerde voetbreedte moet positief zijn en binnen de stijlbreedte blijven.");
            if (config.IntegratedFootReliefHeightMm <= 0
                || config.IntegratedFootReliefHeightMm >= config.BottomRailHeightMm)
                throw new InvalidOperationException("Vrijloophoogte tussen de geïntegreerde voeten moet binnen de onderregelhoogte blijven.");
            if (underframeLength <= 3.0 * config.FrameStileWidthMm)
                throw new InvalidOperationException("Bladlengte laat onvoldoende openingen rond de middenstijl van de vaste langspanelen over.");
            if (underframeDepth / 2.0 <= 2.0 * config.FrameStileWidthMm)
                throw new InvalidOperationException("Bladbreedte laat onvoldoende opening in de halve korte vouwpanelen over.");
            if (config.HingeBarrelDiameterMm <= 0 || config.HingeGapMm <= 0)
                throw new InvalidOperationException("Scharnierknoop en paneelvoeg moeten positief zijn.");
            if (underframeDepth / 2.0 - config.HingeGapMm <= 2.0 * config.FrameStileWidthMm)
                throw new InvalidOperationException("Bladbreedte laat na de scharnierspleten onvoldoende opening in de halve korte vouwpanelen over.");
        }
    }
}
