using System;
using System.Collections.Generic;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Drawing;

namespace SWWerkplaats.Configurator.Engine
{
    public sealed class ShippingBoxEngine
    {
        public WorkbenchModel Build(ShippingBoxConfig config)
        {
            Validate(config);

            var model = new WorkbenchModel { ProjectName = config.ProjectName };
            var material = config.PanelMaterial;
            var clip = config.Clip;
            var t = material.ThicknessMm;
            var outerWidth = config.InternalWidthMm + 2.0 * t;
            var outerDepth = config.InternalDepthMm + 2.0 * t;
            var outerHeight = config.InternalHeightMm + 2.0 * t;
            var rabbetWidth = t + Math.Max(0, config.RabbetClearanceMm);
            var rabbetDepth = Math.Max(0.5, Math.Min(t - 0.5, t * config.RabbetDepthFactor));
            var localizedTabs = string.Equals(config.JointMode, "localized_tabs", StringComparison.OrdinalIgnoreCase);

            var bottom = Sheet("Clipkist bodem", material, outerWidth, outerDepth);
            if (localizedTabs) ApplyHorizontalReceiverContour(bottom, config, t);
            else AddFourEdgeRabbets(bottom, rabbetWidth, rabbetDepth, OperationFace.PositiveY);
            AddPerimeterSlots(bottom, clip, t, config.InternalWidthMm);
            AddSheet(model, bottom, 0, t / 2.0, 0, AssemblyOrientation.SheetHorizontal);

            var lid = Sheet("Clipkist deksel", material, outerWidth, outerDepth);
            if (localizedTabs) ApplyHorizontalReceiverContour(lid, config, t);
            else AddFourEdgeRabbets(lid, rabbetWidth, rabbetDepth, OperationFace.NegativeY);
            AddPerimeterSlots(lid, clip, t, config.InternalWidthMm);
            AddSheet(model, lid, 0, t + config.InternalHeightMm + t / 2.0, 0, AssemblyOrientation.SheetHorizontal);

            var wallHeight = localizedTabs ? outerHeight : config.InternalHeightMm;
            var wallCenterY = localizedTabs ? outerHeight / 2.0 : t + config.InternalHeightMm / 2.0;
            var left = SidePanel("Clipkist zijpaneel links", material, outerDepth, wallHeight, rabbetWidth, rabbetDepth, clip, config.IncludeHandles, config, localizedTabs);
            AddSheet(model, left, -config.InternalWidthMm / 2.0 - t / 2.0, wallCenterY, 0, AssemblyOrientation.SheetVerticalZ);

            var right = SidePanel("Clipkist zijpaneel rechts", material, outerDepth, wallHeight, rabbetWidth, rabbetDepth, clip, config.IncludeHandles, config, localizedTabs);
            right.MirrorInNestingX = true;
            AddSheet(model, right, config.InternalWidthMm / 2.0 + t / 2.0, wallCenterY, 0, AssemblyOrientation.SheetVerticalZ);

            var endWidth = localizedTabs ? outerWidth : config.InternalWidthMm;
            var front = EndPanel("Clipkist voorpaneel", material, endWidth, wallHeight, clip, config, localizedTabs);
            AddSheet(model, front, 0, wallCenterY, -config.InternalDepthMm / 2.0 - t / 2.0, AssemblyOrientation.SheetVerticalX);

            var back = EndPanel("Clipkist achterpaneel", material, endWidth, wallHeight, clip, config, localizedTabs);
            back.MirrorInNestingX = true;
            AddSheet(model, back, 0, wallCenterY, config.InternalDepthMm / 2.0 + t / 2.0, AssemblyOrientation.SheetVerticalX);

            var verticalCount = ClipCount(config.InternalHeightMm, clip);
            var widthCount = ClipCount(config.InternalWidthMm, clip);
            var depthCount = ClipCount(outerDepth, clip);
            var totalClips = 4 * verticalCount + 4 * widthCount + 4 * depthCount;
            AddClipVisuals(model, config, outerWidth, outerDepth);
            model.Hardware.Add(new HardwareItem
            {
                Name = clip.Name,
                ArticleNumber = clip.Id,
                Quantity = totalClips,
                Unit = "st",
                Note = "Liangyue " + clip.SupplierModel + "; aantal parametrisch uit 12 kistnaden en maximale clipafstand.",
                ModelStatus = clip.VerificationStatus,
                BomStatus = "Proefstuk"
            });

            model.DesignNotes.Add("Binnenmaten: " + config.InternalWidthMm.ToString("0.#") + " x " + config.InternalDepthMm.ToString("0.#") + " x " + config.InternalHeightMm.ToString("0.#") + " mm. Berekende buitenmaten: " + outerWidth.ToString("0.#") + " x " + outerDepth.ToString("0.#") + " x " + outerHeight.ToString("0.#") + " mm.");
            model.DesignNotes.Add(localizedTabs
                ? "Opbouw volgens SnapCrate-referentie met lokale montagetappen: doorlopende sponningbanen vervallen; korte tap-/uitsparingparen schalen mee met de clipverdeling en positioneren de platen bij montage."
                : "Opbouw volgens SnapCrate-principe: bodem/deksel vierzijdig gesponningd, zijpanelen tweezijdig gesponningd, voor/achter zonder sponning.");
            model.DesignNotes.Add("Clipvorm gekoppeld aan Liangyue LY103-12. Sleuf " + clip.SlotLengthMm.ToString("0.#") + " x " + clip.SlotWidthMm.ToString("0.#") + " mm en randafstand " + clip.SlotCenterFromEdgeMm.ToString("0.#") + " mm zijn proefstukwaarden en blokkeren productievrijgave totdat een leverancierssample is ingemeten.");
            if (config.IncludeHandles) model.DesignNotes.Add("Uitgefreesde handgrepen actief in beide zijpanelen; draagvermogen afzonderlijk valideren voor OSB 18 mm.");
            return model;
        }

        private static SheetPart SidePanel(string name, Material material, double length, double height, double rabbetWidth, double rabbetDepth, CrateClipTemplate clip, bool includeHandle, ShippingBoxConfig config, bool localizedTabs)
        {
            var panel = Sheet(name, material, length, height);
            if (localizedTabs)
            {
                ApplySidePanelContour(panel, config, material.ThicknessMm);
                AddHorizontalEdgeSlots(panel, clip, true);
                AddHorizontalEdgeSlots(panel, clip, false);
                AddVerticalEdgeSlots(panel, clip, true, material.ThicknessMm, config.InternalHeightMm);
                AddVerticalEdgeSlots(panel, clip, false, material.ThicknessMm, config.InternalHeightMm);
            }
            else
            {
                SheetOperations.AddPocket(panel, "Sponning voorpaneel", 0, 0, rabbetWidth, height, rabbetDepth, OperationFace.NegativeZ, "Tweezijdige zijpaneelsponning volgens SnapCrate-opbouw.");
                SheetOperations.AddPocket(panel, "Sponning achterpaneel", length - rabbetWidth, 0, rabbetWidth, height, rabbetDepth, OperationFace.NegativeZ, "Tweezijdige zijpaneelsponning volgens SnapCrate-opbouw.");
                AddAllEdgeSlots(panel, clip);
            }
            if (includeHandle)
            {
                var handleLength = Math.Min(config.HandleLengthMm, Math.Max(60, length - 160));
                var handleHeight = Math.Min(config.HandleHeightMm, Math.Max(24, config.InternalHeightMm / 4.0));
                var coreOffsetY = localizedTabs ? material.ThicknessMm : 0;
                SheetOperations.AddCapsuleThroughCutout(panel, "Uitgefreesde handgreep proefstuk", (length - handleLength) / 2.0, coreOffsetY + Math.Max(60, config.InternalHeightMm * config.HandleCenterHeightRatio - handleHeight / 2.0), handleLength, handleHeight, OperationFace.CenterPlane, "Optionele capsule-handgreep; reststerkte fysiek beproeven.");
            }
            return panel;
        }

        private static SheetPart EndPanel(string name, Material material, double length, double height, CrateClipTemplate clip, ShippingBoxConfig config, bool localizedTabs)
        {
            var panel = Sheet(name, material, length, height);
            if (localizedTabs)
            {
                ApplyEndPanelContour(panel, config, material.ThicknessMm);
                AddHorizontalEdgeSlots(panel, clip, true, material.ThicknessMm, config.InternalWidthMm);
                AddHorizontalEdgeSlots(panel, clip, false, material.ThicknessMm, config.InternalWidthMm);
                AddVerticalEdgeSlots(panel, clip, true, material.ThicknessMm, config.InternalHeightMm);
                AddVerticalEdgeSlots(panel, clip, false, material.ThicknessMm, config.InternalHeightMm);
            }
            else AddAllEdgeSlots(panel, clip);
            return panel;
        }

        private static void AddFourEdgeRabbets(SheetPart panel, double width, double depth, OperationFace face)
        {
            SheetOperations.AddPocket(panel, "Sponning voorzijde", 0, 0, panel.LengthMm, width, depth, face, "Vierzijdige sponning volgens SnapCrate-opbouw.");
            SheetOperations.AddPocket(panel, "Sponning achterzijde", 0, panel.WidthMm - width, panel.LengthMm, width, depth, face, "Vierzijdige sponning volgens SnapCrate-opbouw.");
            SheetOperations.AddPocket(panel, "Sponning links", 0, 0, width, panel.WidthMm, depth, face, "Vierzijdige sponning volgens SnapCrate-opbouw.");
            SheetOperations.AddPocket(panel, "Sponning rechts", panel.LengthMm - width, 0, width, panel.WidthMm, depth, face, "Vierzijdige sponning volgens SnapCrate-opbouw.");
        }

        private static void AddPerimeterSlots(SheetPart panel, CrateClipTemplate clip, double endInset, double horizontalSeamLength)
        {
            AddHorizontalEdgeSlots(panel, clip, true, endInset, horizontalSeamLength);
            AddHorizontalEdgeSlots(panel, clip, false, endInset, horizontalSeamLength);
            AddVerticalEdgeSlots(panel, clip, true);
            AddVerticalEdgeSlots(panel, clip, false);
        }

        private static void AddAllEdgeSlots(SheetPart panel, CrateClipTemplate clip)
        {
            AddHorizontalEdgeSlots(panel, clip, true);
            AddHorizontalEdgeSlots(panel, clip, false);
            AddVerticalEdgeSlots(panel, clip, true);
            AddVerticalEdgeSlots(panel, clip, false);
        }

        private static void AddHorizontalEdgeSlots(SheetPart panel, CrateClipTemplate clip, bool lower)
        {
            AddHorizontalEdgeSlots(panel, clip, lower, 0, panel.LengthMm);
        }

        private static void AddHorizontalEdgeSlots(SheetPart panel, CrateClipTemplate clip, bool lower, double startOffset, double seamLength)
        {
            foreach (var position in Positions(seamLength, clip))
            {
                var x = startOffset + position - clip.SlotLengthMm / 2.0;
                var y = (lower ? clip.SlotCenterFromEdgeMm : panel.WidthMm - clip.SlotCenterFromEdgeMm) - clip.SlotWidthMm / 2.0;
                SheetOperations.AddCapsuleThroughCutout(panel, "Clipsleuf " + (lower ? "onder" : "boven"), x, y, clip.SlotLengthMm, clip.SlotWidthMm, OperationFace.CenterPlane, "Capsulesleuf proefstukmaat voor Liangyue LY103-12; na sample-inmeting vrijgeven.");
            }
        }

        private static void AddVerticalEdgeSlots(SheetPart panel, CrateClipTemplate clip, bool left)
        {
            AddVerticalEdgeSlots(panel, clip, left, 0, panel.WidthMm);
        }

        private static void AddVerticalEdgeSlots(SheetPart panel, CrateClipTemplate clip, bool left, double startOffset, double seamLength)
        {
            foreach (var position in Positions(seamLength, clip))
            {
                var x = (left ? clip.SlotCenterFromEdgeMm : panel.LengthMm - clip.SlotCenterFromEdgeMm) - clip.SlotWidthMm / 2.0;
                var y = startOffset + position - clip.SlotLengthMm / 2.0;
                SheetOperations.AddCapsuleThroughCutout(panel, "Clipsleuf " + (left ? "links" : "rechts"), x, y, clip.SlotWidthMm, clip.SlotLengthMm, OperationFace.CenterPlane, "Capsulesleuf proefstukmaat voor Liangyue LY103-12; na sample-inmeting vrijgeven.");
            }
        }

        private static void ApplyHorizontalReceiverContour(SheetPart panel, ShippingBoxConfig config, double thickness)
        {
            var relief = Math.Max(1.0, thickness * 0.4);
            var frontBack = TabIntervals(config.InternalWidthMm, config.Clip, thickness, thickness, relief);
            var sides = TabIntervals(panel.WidthMm, config.Clip, thickness, 0, relief);
            SetOrthogonalContour(panel, 0, 0, panel.LengthMm, panel.WidthMm,
                thickness, panel.LengthMm - thickness, panel.WidthMm - thickness, thickness,
                frontBack, sides, frontBack, sides);
        }

        private static void ApplySidePanelContour(SheetPart panel, ShippingBoxConfig config, double thickness)
        {
            var relief = Math.Max(1.0, thickness * 0.4);
            var horizontalTabs = TabIntervals(panel.LengthMm, config.Clip, thickness, 0, 0);
            var verticalNotches = TabIntervals(config.InternalHeightMm, config.Clip, thickness, thickness, relief);
            SetOrthogonalContour(panel, 0, thickness, panel.LengthMm, panel.WidthMm - thickness,
                0, panel.LengthMm - thickness, panel.WidthMm, thickness,
                horizontalTabs, verticalNotches, horizontalTabs, verticalNotches);
        }

        private static void ApplyEndPanelContour(SheetPart panel, ShippingBoxConfig config, double thickness)
        {
            var horizontalTabs = TabIntervals(config.InternalWidthMm, config.Clip, thickness, thickness, 0);
            var verticalTabs = TabIntervals(config.InternalHeightMm, config.Clip, thickness, thickness, 0);
            SetOrthogonalContour(panel, thickness, thickness, panel.LengthMm - thickness, panel.WidthMm - thickness,
                0, panel.LengthMm, panel.WidthMm, 0,
                horizontalTabs, verticalTabs, horizontalTabs, verticalTabs);
        }

        private static List<EdgeInterval> TabIntervals(double seamLength, CrateClipTemplate clip, double thickness, double offset, double extraWidth)
        {
            var positions = new List<double>(Positions(seamLength, clip));
            var pitch = positions.Count > 1 ? positions[1] - positions[0] : seamLength;
            var desired = Math.Max(clip.WidthMm * 2.8, thickness * 5.5);
            var width = Math.Min(desired, Math.Max(clip.WidthMm * 1.8, pitch * 0.72)) + Math.Max(0, extraWidth);
            var intervals = new List<EdgeInterval>();
            foreach (var position in positions)
            {
                intervals.Add(new EdgeInterval(offset + position - width / 2.0, offset + position + width / 2.0));
            }
            return intervals;
        }

        private static void SetOrthogonalContour(
            SheetPart panel,
            double x0,
            double y0,
            double x1,
            double y1,
            double bottomY,
            double rightX,
            double topY,
            double leftX,
            List<EdgeInterval> bottom,
            List<EdgeInterval> right,
            List<EdgeInterval> top,
            List<EdgeInterval> left)
        {
            panel.CustomContour.Clear();
            panel.CustomContourCornerRadiusMm = 3.0;
            AddPoint(panel, x0, y0);
            foreach (var interval in bottom)
            {
                AddPoint(panel, interval.Start, y0); AddPoint(panel, interval.Start, bottomY);
                AddPoint(panel, interval.End, bottomY); AddPoint(panel, interval.End, y0);
            }
            AddPoint(panel, x1, y0);
            foreach (var interval in right)
            {
                AddPoint(panel, x1, interval.Start); AddPoint(panel, rightX, interval.Start);
                AddPoint(panel, rightX, interval.End); AddPoint(panel, x1, interval.End);
            }
            AddPoint(panel, x1, y1);
            for (var i = top.Count - 1; i >= 0; i--)
            {
                var interval = top[i];
                AddPoint(panel, interval.End, y1); AddPoint(panel, interval.End, topY);
                AddPoint(panel, interval.Start, topY); AddPoint(panel, interval.Start, y1);
            }
            AddPoint(panel, x0, y1);
            for (var i = left.Count - 1; i >= 0; i--)
            {
                var interval = left[i];
                AddPoint(panel, x0, interval.End); AddPoint(panel, leftX, interval.End);
                AddPoint(panel, leftX, interval.Start); AddPoint(panel, x0, interval.Start);
            }
        }

        private static void AddPoint(SheetPart panel, double x, double y)
        {
            panel.CustomContour.Add(new SheetContourPoint(Math.Round(x, 3), Math.Round(y, 3)));
        }

        private sealed class EdgeInterval
        {
            public EdgeInterval(double start, double end) { Start = start; End = end; }
            public double Start { get; private set; }
            public double End { get; private set; }
        }

        private static IEnumerable<double> Positions(double length, CrateClipTemplate clip)
        {
            var count = ClipCount(length, clip);
            if (count <= 1) return new[] { length / 2.0 };
            var margin = Math.Min(clip.EndMarginMm, Math.Max(clip.SlotLengthMm, length / 3.0));
            var result = new List<double>();
            for (var i = 0; i < count; i++) result.Add(margin + i * (length - 2.0 * margin) / (count - 1));
            return result;
        }

        private static int ClipCount(double length, CrateClipTemplate clip)
        {
            var usable = Math.Max(0, length - 2.0 * clip.EndMarginMm);
            return Math.Max(2, (int)Math.Ceiling(usable / Math.Max(120, clip.MaxSpacingMm)) + 1);
        }

        private static void AddClipVisuals(WorkbenchModel model, ShippingBoxConfig config, double outerWidth, double outerDepth)
        {
            var t = config.PanelMaterial.ThicknessMm;
            foreach (var yLocal in Positions(config.InternalHeightMm, config.Clip))
            {
                var y = t + yLocal;
                foreach (var sx in new[] { -1.0, 1.0 })
                foreach (var sz in new[] { -1.0, 1.0 })
                {
                    AddClipPlacement(model, "hoek", sx * outerWidth / 2.0, y, sz * outerDepth / 2.0,
                        0, sz > 0 ? 180 : 0, sx > 0 ? 90 : -90, config.Clip);
                }
            }

            foreach (var position in Positions(config.InternalWidthMm, config.Clip))
            foreach (var sz in new[] { -1.0, 1.0 })
            {
                var x = position - config.InternalWidthMm / 2.0;
                AddClipPlacement(model, "bodem voor/achter", x, 0, sz * outerDepth / 2.0,
                    0, sz > 0 ? 180 : 0, 0, config.Clip);
                AddClipPlacement(model, "deksel voor/achter", x, config.InternalHeightMm + 2.0 * t, sz * outerDepth / 2.0,
                    sz > 0 ? 180 : 0, 0, sz < 0 ? 180 : 0, config.Clip);
            }

            foreach (var position in Positions(outerDepth, config.Clip))
            foreach (var sx in new[] { -1.0, 1.0 })
            {
                var z = position - outerDepth / 2.0;
                AddClipPlacement(model, "bodem zijwand", sx * outerWidth / 2.0, 0, z,
                    0, sx > 0 ? -90 : 90, 0, config.Clip);
                AddClipPlacement(model, "deksel zijwand", sx * outerWidth / 2.0, config.InternalHeightMm + 2.0 * t, z,
                    180, sx > 0 ? 90 : -90, 0, config.Clip);
            }
        }

        private static void AddClipPlacement(WorkbenchModel model, string location, double x, double y, double z,
            double rotationXDeg, double rotationYDeg, double rotationZDeg, CrateClipTemplate clip)
        {
            // Het lokale clipcontract heeft zijn naadnulpunt op de binnenvlakken
            // van beide cliparmen. Verplaats dat nulpunt één plaatdikte van de
            // clip naar buiten, zodat de armen tegen het hout liggen in plaats
            // van coplanair met/in het plaatvolume te worden gerenderd.
            var outsideOffset = RotateLocal(0, -clip.ThicknessMm, -clip.ThicknessMm,
                rotationXDeg, rotationYDeg, rotationZDeg);
            model.AssemblyPlacements.Add(new AssemblyPlacement
            {
                Kind = AssemblyComponentKind.Purchased,
                PartName = "Liangyue LY103-12 clip " + location,
                ComponentId = clip.Id,
                LengthMm = clip.WidthMm,
                WidthMm = clip.ArmLengthAMm,
                HeightMm = clip.ArmLengthBMm,
                Xmm = x + outsideOffset[0],
                Ymm = y + outsideOffset[1],
                Zmm = z + outsideOffset[2],
                VisualKind = "crate-clip",
                Shape = "component-primitives",
                RotationXDeg = rotationXDeg,
                RotationYDeg = rotationYDeg,
                RotationZDeg = rotationZDeg
            });
        }

        private static double[] RotateLocal(double x, double y, double z,
            double rotationXDeg, double rotationYDeg, double rotationZDeg)
        {
            var rx = rotationXDeg * Math.PI / 180.0;
            var ry = rotationYDeg * Math.PI / 180.0;
            var rz = rotationZDeg * Math.PI / 180.0;
            var cosX = Math.Cos(rx); var sinX = Math.Sin(rx);
            var y1 = y * cosX - z * sinX; var z1 = y * sinX + z * cosX;
            var cosY = Math.Cos(ry); var sinY = Math.Sin(ry);
            var x2 = x * cosY + z1 * sinY; var z2 = -x * sinY + z1 * cosY;
            var cosZ = Math.Cos(rz); var sinZ = Math.Sin(rz);
            return new[] { x2 * cosZ - y1 * sinZ, x2 * sinZ + y1 * cosZ, z2 };
        }

        private static SheetPart Sheet(string name, Material material, double length, double width)
        {
            return SheetDrawing.CreateSheet(name, material, length, width);
        }

        private static void AddSheet(WorkbenchModel model, SheetPart sheet, double x, double y, double z, AssemblyOrientation orientation)
        {
            SheetDrawing.AddSheetToModel(model, sheet, x, y, z, orientation);
            sheet.UseTabs = true;
        }

        private static void Validate(ShippingBoxConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (config.PanelMaterial == null || config.PanelMaterial.ThicknessMm <= 0) throw new InvalidOperationException("Shipping box mist geldig plaatmateriaal.");
            if (config.PanelMaterial.SheetLengthMm <= 0 || config.PanelMaterial.SheetWidthMm <= 0) throw new InvalidOperationException("Shipping box mist een geldige handelsmaat voor het plaatmateriaal.");
            if (config.Clip == null) throw new InvalidOperationException("Shipping box mist cliptemplate.");
            if (config.InternalWidthMm <= 0 || config.InternalDepthMm <= 0 || config.InternalHeightMm <= 0) throw new InvalidOperationException("Shipping box heeft ongeldige binnenmaten.");

            var thickness = config.PanelMaterial.ThicknessMm;
            var outerWidth = config.InternalWidthMm + 2.0 * thickness;
            var outerDepth = config.InternalDepthMm + 2.0 * thickness;
            var localizedTabs = string.Equals(config.JointMode, "localized_tabs", StringComparison.OrdinalIgnoreCase);
            var wallHeight = localizedTabs ? config.InternalHeightMm + 2.0 * thickness : config.InternalHeightMm;
            var endWidth = localizedTabs ? outerWidth : config.InternalWidthMm;

            RequirePanelFits(config.PanelMaterial, "bodem/deksel", outerWidth, outerDepth, config.StockAllowanceMm);
            RequirePanelFits(config.PanelMaterial, "zijpaneel", outerDepth, wallHeight, config.StockAllowanceMm);
            RequirePanelFits(config.PanelMaterial, "voor-/achterpaneel", endWidth, wallHeight, config.StockAllowanceMm);
        }

        private static void RequirePanelFits(Material material, string panelName, double lengthMm, double widthMm, double stockAllowanceMm)
        {
            var usableLength = material.SheetLengthMm - stockAllowanceMm;
            var usableWidth = material.SheetWidthMm - stockAllowanceMm;
            if (usableLength <= 0 || usableWidth <= 0)
                throw new InvalidOperationException("De plaatmarge laat geen bruikbaar plaatformaat over.");
            var fits = (lengthMm <= usableLength && widthMm <= usableWidth)
                || (widthMm <= usableLength && lengthMm <= usableWidth);
            if (fits) return;

            throw new InvalidOperationException(
                "De gekozen binnenmaten leveren een " + panelName + " van "
                + lengthMm.ToString("0.#") + " x " + widthMm.ToString("0.#")
                + " mm op. Dit paneel past ook na draaien niet uit plaat "
                + usableLength.ToString("0.#") + " x " + usableWidth.ToString("0.#")
                + " mm na " + stockAllowanceMm.ToString("0.#") + " mm totale plaatmarge.");
        }
    }
}
