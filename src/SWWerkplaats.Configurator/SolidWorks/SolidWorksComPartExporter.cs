using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using SolidWorks.Interop.sldworks;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.SolidWorks
{
    public sealed class SolidWorksComPartExporter
    {
        private const int SaveAsCurrentVersion = 0;
        private const int SaveAsOptionsSilent = 1;
        private const int SolidBodyCutOperation = 15902;
        private const double BooleanOverlapMm = 0.02;
        private const string ThreeExperienceLauncherPath = @"C:\Program Files\Dassault Systemes\SOLIDWORKS 3DEXPERIENCE R2026x\win_b64\code\bin\CATSTART.exe";
        private const string ThreeExperienceLauncherArguments = "-run \"SWXDesktopLauncher.exe\" -object \"--AppName=\\\"SWXCSWK_AP\\\" -tenant=R1132104190977 -monoapp -3DRegistryURL=https://eu1-registry.3dexperience.3ds.com\" -nowindow";

        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable runningObjectTable);

        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(int reserved, out IBindCtx bindContext);

        public void ExportParts(WorkbenchModel model, string outputFolder)
        {
            ExportPartsAndAssembly(model, outputFolder);
        }

        public int CloseGeneratedDocumentsUnder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("Map voor SolidWorks-documentopruiming ontbreekt.");
            var root = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var solidWorks = GetOrCreateSolidWorks();
            var documents = solidWorks.GetDocuments() as object[];
            if (documents == null) return 0;
            var titles = new List<string>();
            foreach (var item in documents)
            {
                var document = item as ModelDoc2;
                if (document == null) continue;
                try
                {
                    var path = document.GetPathName();
                    if (!string.IsNullOrWhiteSpace(path) && Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase))
                        titles.Add(document.GetTitle());
                }
                finally
                {
                    ReleaseCom(document);
                }
            }
            foreach (var title in titles) solidWorks.CloseDoc(title);
            ForceComCleanup();
            return titles.Count;
        }

        public string ExportPartsAndAssembly(WorkbenchModel model, string outputFolder, PortalQuoteRequest request = null)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (string.IsNullOrWhiteSpace(outputFolder)) throw new ArgumentException("Outputmap ontbreekt.");

            var solidWorks = GetOrCreateSolidWorks();
            solidWorks.Visible = true;
            var cadFolder = Path.Combine(outputFolder, "02_SolidWorks");
            Directory.CreateDirectory(cadFolder);
            var partPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var visualParts = new PortalAssembly3DService().Build(model, request);
            var exportedSheetGeometries = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var profile in model.Profiles)
            {
                var partPath = Path.Combine(cadFolder, SafeName(profile.Name) + "_" + profile.LengthMm.ToString("0") + "mm.SLDPRT");
                CreateBoxPart(solidWorks, partPath, profile.Material.WidthMm, profile.Material.HeightMm, profile.LengthMm);
                partPaths[PartKey(AssemblyComponentKind.Profile, profile.Name)] = partPath;
            }

            foreach (var sheet in model.Sheets)
            {
                var placement = FindPlacement(model, AssemblyComponentKind.Sheet, sheet.Name);
                var dimensions = OrientedSheetDimensions(sheet, placement == null ? AssemblyOrientation.SheetHorizontal : placement.Orientation);
                var partPath = Path.Combine(cadFolder, SafeName(sheet.Name) + "_" + sheet.LengthMm.ToString("0") + "x" + sheet.WidthMm.ToString("0") + ".SLDPRT");
                var matchingVisuals = FindSheetVisuals(visualParts, sheet.Name);
                if (placement != null && matchingVisuals.Count > 0)
                {
                    var geometryKey = MachinedSheetGeometryKey(matchingVisuals, placement);
                    string existingPart;
                    if (exportedSheetGeometries.TryGetValue(geometryKey, out existingPart) && File.Exists(existingPart))
                        File.Copy(existingPart, partPath, true);
                    else
                    {
                        CreateMachinedSheetPart(solidWorks, partPath, matchingVisuals, placement);
                        exportedSheetGeometries[geometryKey] = partPath;
                    }
                }
                else
                    CreateBoxPart(solidWorks, partPath, dimensions.Xmm, dimensions.Ymm, dimensions.Zmm);
                partPaths[PartKey(AssemblyComponentKind.Sheet, sheet.Name)] = partPath;
            }

            if (request != null && string.Equals(request.Product, "werkbankkast", StringComparison.OrdinalIgnoreCase))
            {
                var config = new PortalConfigurationFactory().BuildWorkbenchCabinet(request);
                var foot = config.AdjustableFoot ?? ProductDefaults.WorkbenchCabinetAdjustableFoot();
                var adapter = foot.PlinthClipAdapter ?? ProductDefaults.WorkbenchCabinetPlinthClipAdapter();
                CreatePlinthAdapterPart(
                    solidWorks,
                    Path.Combine(cadFolder, "SEKTION_plintclip_adapter_voor_v2_vleugel_rechts.SLDPRT"),
                    Path.Combine(cadFolder, "SEKTION_plintclip_adapter_voor_v2_vleugel_rechts.STL"),
                    adapter,
                    ProductDefaults.WorkbenchCabinetFrontAdapterStandOffMm(config),
                    1.0);
                CreatePlinthAdapterPart(
                    solidWorks,
                    Path.Combine(cadFolder, "SEKTION_plintclip_adapter_voor_v2_vleugel_links.SLDPRT"),
                    Path.Combine(cadFolder, "SEKTION_plintclip_adapter_voor_v2_vleugel_links.STL"),
                    adapter,
                    ProductDefaults.WorkbenchCabinetFrontAdapterStandOffMm(config),
                    -1.0);
                if (config.IncludeLeftSidePlinth || config.IncludeRightSidePlinth)
                {
                    CreatePlinthAdapterPart(
                        solidWorks,
                        Path.Combine(cadFolder, "SEKTION_plintclip_adapter_zijde_v2_vleugel_rechts.SLDPRT"),
                        Path.Combine(cadFolder, "SEKTION_plintclip_adapter_zijde_v2_vleugel_rechts.STL"),
                        adapter,
                        ProductDefaults.WorkbenchCabinetSideAdapterStandOffMm(config),
                        1.0);
                    CreatePlinthAdapterPart(
                        solidWorks,
                        Path.Combine(cadFolder, "SEKTION_plintclip_adapter_zijde_v2_vleugel_links.SLDPRT"),
                        Path.Combine(cadFolder, "SEKTION_plintclip_adapter_zijde_v2_vleugel_links.STL"),
                        adapter,
                        ProductDefaults.WorkbenchCabinetSideAdapterStandOffMm(config),
                        -1.0);
                }
            }
            else if (request != null &&
                (string.Equals(request.Product, "werktafel_lex", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(request.Product, "werktafel_lex_revolution", StringComparison.OrdinalIgnoreCase)))
            {
                CreateLexLevelingFootParts(solidWorks, visualParts, model, cadFolder);
            }

            // Een externe assembly wordt op sommige werkplekken door Windows Application
            // Control geblokkeerd zodra SolidWorks het tweede lokale part wil invoegen.
            // Een multibody controlepart gebruikt dezelfde wereldcoordinaten, maar blijft
            // volledig binnen een SolidWorks-document en is daardoor betrouwbaarder.
            var controlPath = Path.Combine(cadFolder, SafeName(model.ProjectName) + "_CONTROLE.SLDPRT");
            CreateMultibodyControlPart(solidWorks, visualParts, model, request, controlPath);
            return controlPath;
        }

        private static void CreateMultibodyControlPart(
            SldWorks solidWorks,
            List<PortalAssemblyPart> visualParts,
            WorkbenchModel workbenchModel,
            PortalQuoteRequest request,
            string controlPath)
        {
            var model = (ModelDoc2)solidWorks.NewPart();
            if (model == null) throw new InvalidOperationException("SolidWorks kon geen nieuw controlepart maken. Controleer de default part template.");

            var part = (PartDoc)model;
            var modeler = (Modeler)solidWorks.GetModeler();
            var customerPresentation = new SolidWorksCustomerPresentation(model, controlPath);
            var bodyCount = 0;
            foreach (var visual in visualParts)
            {
                var body = CreateVisualBody(modeler, visual);

                if (body == null) throw new InvalidOperationException("SolidWorks kon geen controlebody maken voor " + visual.Name + ".");
                var feature = part.CreateFeatureFromBody3(body, false, 1) as Feature;
                if (feature == null) throw new InvalidOperationException("SolidWorks kon de controlebody niet invoegen voor " + visual.Name + ".");
                feature.Name = SafeFeatureName((bodyCount + 1).ToString("000") + "_" + visual.Name);
                customerPresentation.ApplyAppearance(feature, visual, workbenchModel);
                ReleaseCom(body);
                ReleaseCom(feature);
                bodyCount++;
            }

            if (bodyCount == 0) throw new InvalidOperationException("Het controlemodel bevat geen plaatsingen.");
            customerPresentation.CommitAppearances();
            model.EditRebuild3();
            model.ViewZoomtofit2();
            var errors = 0;
            var warnings = 0;
            model.Extension.SaveAs(controlPath, SaveAsCurrentVersion, SaveAsOptionsSilent, null, ref errors, ref warnings);
            if (errors != 0) throw new InvalidOperationException("SolidWorks kon het multibody-controlepart niet opslaan (code " + errors + ").");
            customerPresentation.ExportGlb(model, controlPath);
            if (request != null)
                SolidWorksCustomerDrawingExporter.Export(solidWorks, model, controlPath, request, workbenchModel);
        }

        private static void CreateLexLevelingFootParts(SldWorks solidWorks, List<PortalAssemblyPart> visualParts, WorkbenchModel model, string cadFolder)
        {
            var adapterPlacement = model.AssemblyPlacements.FirstOrDefault(p =>
                p != null && string.Equals(p.Shape, "leveling-foot-adapter", StringComparison.OrdinalIgnoreCase));
            if (adapterPlacement == null) throw new InvalidOperationException("LEX ZI-1744 hoekadapter ontbreekt in het assemblymodel.");

            var adapterVisuals = visualParts
                .Where(p => p != null && p.Name != null && p.Name.StartsWith(adapterPlacement.PartName + " - ", StringComparison.OrdinalIgnoreCase))
                .Select(p => TranslateAssemblyPart(p, -adapterPlacement.Xmm, -adapterPlacement.Ymm, -adapterPlacement.Zmm))
                .ToList();
            if (adapterVisuals.Count != 5) throw new InvalidOperationException("LEX ZI-1744 hoekadapter heeft geen complete 3D-opbouw.");
            CreateVisualMultibodyPart(
                solidWorks,
                adapterVisuals,
                Path.Combine(cadFolder, "Maunsystem_ZI-1744_hoekadapter_M16.SLDPRT"),
                "ZI-1744");

            var direction = adapterPlacement.Zmm < 0 ? -1.0 : 1.0;
            var config = ProductDefaults.LexLevelingFootCornerAdapter();
            var mountingFaceZ = adapterPlacement.Zmm - direction * config.ReachMm / 2.0;
            var footAxisZ = mountingFaceZ + direction * config.FootAxisFromMountingFaceMm;
            var footVisuals = visualParts
                .Where(p => p != null
                    && p.Name != null
                    && p.Name.StartsWith("Stelvoet ", StringComparison.OrdinalIgnoreCase)
                    && Math.Abs(p.Xmm - adapterPlacement.Xmm) < 0.05
                    && Math.Abs(p.Zmm - footAxisZ) < 0.05)
                .Select(p => TranslateAssemblyPart(p, -adapterPlacement.Xmm, 0, -footAxisZ))
                .ToList();
            if (footVisuals.Count != 4) throw new InvalidOperationException("LEX ZI-1415-S stelvoet heeft geen complete 3D-opbouw.");
            CreateVisualMultibodyPart(
                solidWorks,
                footVisuals,
                Path.Combine(cadFolder, "Maunsystem_ZI-1415-S_stelvoet_D80_M16x130.SLDPRT"),
                "ZI-1415-S");
        }

        private static void CreateVisualMultibodyPart(SldWorks solidWorks, List<PortalAssemblyPart> visuals, string partPath, string featurePrefix)
        {
            var model = (ModelDoc2)solidWorks.NewPart();
            if (model == null) throw new InvalidOperationException("SolidWorks kon geen nieuw leverancierspart maken voor " + featurePrefix + ".");
            var part = (PartDoc)model;
            var modeler = (Modeler)solidWorks.GetModeler();
            for (var index = 0; index < visuals.Count; index++)
            {
                var visual = visuals[index];
                var body = CreateVisualBody(modeler, visual);
                if (body == null) throw new InvalidOperationException("SolidWorks kon geen body maken voor " + visual.Name + ".");
                var feature = part.CreateFeatureFromBody3(body, false, 1) as Feature;
                if (feature == null) throw new InvalidOperationException("SolidWorks kon de body niet invoegen voor " + visual.Name + ".");
                feature.Name = SafeFeatureName(featurePrefix + "_" + (index + 1).ToString("00") + "_" + visual.Name);
                ReleaseCom(body);
                ReleaseCom(feature);
            }
            model.EditRebuild3();
            model.ViewZoomtofit2();
            var errors = 0;
            var warnings = 0;
            model.Extension.SaveAs(partPath, SaveAsCurrentVersion, SaveAsOptionsSilent, null, ref errors, ref warnings);
            var title = model.GetTitle();
            solidWorks.CloseDoc(title);
            if (errors != 0) throw new InvalidOperationException("SolidWorks kon " + featurePrefix + " niet opslaan (code " + errors + ").");
        }

        private static Body2 CreateVisualBody(Modeler modeler, PortalAssemblyPart visual)
        {
            Body2 body;
            if (string.Equals(visual.Shape, "cylinder", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(visual.Shape, "ball-transfer", StringComparison.OrdinalIgnoreCase))
            {
                var cylinder = new double[]
                {
                    MmToM(visual.Xmm), MmToM(visual.Ymm - visual.SizeYmm / 2.0), MmToM(visual.Zmm),
                    0, 1, 0,
                    MmToM(Math.Min(visual.SizeXmm, visual.SizeZmm) / 2.0), MmToM(visual.SizeYmm)
                };
                body = modeler.CreateBodyFromCyl(cylinder) as Body2;
            }
            else
            {
                body = CreateBoxBody(modeler, visual.Xmm, visual.Ymm, visual.Zmm, visual.SizeXmm, visual.SizeYmm, visual.SizeZmm);
                if (body != null) body = ApplyPanelCornerRadius(modeler, visual, body);
            }
            if (body == null) throw new InvalidOperationException("SolidWorks kon geen 3D-body maken voor " + visual.Name + ".");
            body = ApplyPocketCuts(modeler, visual, body);
            body = ApplyHoleCuts(modeler, visual, body);
            return body;
        }

        private static List<PortalAssemblyPart> FindSheetVisuals(List<PortalAssemblyPart> visualParts, string sheetName)
        {
            if (visualParts == null || string.IsNullOrWhiteSpace(sheetName)) return new List<PortalAssemblyPart>();
            return visualParts
                .Where(p => p != null
                    && string.Equals(p.Kind, "sheet", StringComparison.OrdinalIgnoreCase)
                    && (string.Equals(p.Name, sheetName, StringComparison.OrdinalIgnoreCase)
                        || (p.Name != null && p.Name.StartsWith(sheetName + " ", StringComparison.OrdinalIgnoreCase))))
                .ToList();
        }

        private static void CreateMachinedSheetPart(
            SldWorks solidWorks,
            string partPath,
            List<PortalAssemblyPart> visualParts,
            AssemblyPlacement placement)
        {
            var model = (ModelDoc2)solidWorks.NewPart();
            if (model == null) throw new InvalidOperationException("SolidWorks kon geen bewerkt plaatpart maken voor " + partPath + ".");
            var part = (PartDoc)model;
            var modeler = (Modeler)solidWorks.GetModeler();
            var bodyCount = 0;
            var title = model.GetTitle();
            try
            {
                foreach (var visual in visualParts)
                {
                    var local = TranslateAssemblyPart(visual, -placement.Xmm, -placement.Ymm, -placement.Zmm);
                    var body = CreateBoxBody(modeler, local.Xmm, local.Ymm, local.Zmm, local.SizeXmm, local.SizeYmm, local.SizeZmm);
                    if (body == null) throw new InvalidOperationException("SolidWorks kon de plaatbody niet maken voor " + visual.Name + ".");
                    body = ApplyPanelCornerRadius(modeler, local, body);
                    body = ApplyPocketCuts(modeler, local, body);
                    body = ApplyHoleCuts(modeler, local, body);
                    var feature = part.CreateFeatureFromBody3(body, false, 1) as Feature;
                    if (feature == null) throw new InvalidOperationException("SolidWorks kon de bewerkte plaatbody niet invoegen voor " + visual.Name + ".");
                    feature.Name = SafeFeatureName((bodyCount + 1).ToString("000") + "_" + visual.Name);
                    ReleaseCom(body);
                    ReleaseCom(feature);
                    bodyCount++;
                }
                if (bodyCount == 0) throw new InvalidOperationException("Geen plaatlichamen gevonden voor " + partPath + ".");
                model.EditRebuild3();
                model.ViewZoomtofit2();
                var errors = 0;
                var warnings = 0;
                model.Extension.SaveAs(partPath, SaveAsCurrentVersion, SaveAsOptionsSilent, null, ref errors, ref warnings);
                title = model.GetTitle();
                if (errors != 0) throw new InvalidOperationException("SolidWorks kon het bewerkte plaatpart niet opslaan (code " + errors + "): " + partPath);
            }
            finally
            {
                solidWorks.CloseDoc(title);
                ReleaseCom(modeler);
                ReleaseCom(part);
                ReleaseCom(model);
                ForceComCleanup();
            }
        }

        private static string MachinedSheetGeometryKey(List<PortalAssemblyPart> visualParts, AssemblyPlacement placement)
        {
            var tokens = new List<string>();
            foreach (var visual in visualParts)
            {
                var local = TranslateAssemblyPart(visual, -placement.Xmm, -placement.Ymm, -placement.Zmm);
                tokens.Add("B|" + R(local.Xmm) + "|" + R(local.Ymm) + "|" + R(local.Zmm) + "|" + R(local.SizeXmm) + "|" + R(local.SizeYmm) + "|" + R(local.SizeZmm) + "|R" + R(local.CornerRadiusMm));
                foreach (var hole in local.Holes)
                    tokens.Add("H|" + hole.Plane + "|" + R(hole.Xmm) + "|" + R(hole.Ymm) + "|" + R(hole.Zmm) + "|" + R(hole.DiameterMm) + "|" + R(hole.DepthMm) + "|" + hole.IsThroughCutout + "|" + hole.Countersunk + "|" + R(hole.CountersinkDiameterMm) + "|" + R(hole.CountersinkDepthMm));
                foreach (var pocket in local.Pockets.Where(p => !IsVisualReveal(p)))
                    tokens.Add("P|" + pocket.Shape + "|" + pocket.Plane + "|" + R(pocket.Xmm) + "|" + R(pocket.Ymm) + "|" + R(pocket.Zmm) + "|" + R(pocket.SizeXmm) + "|" + R(pocket.SizeYmm) + "|" + R(pocket.SizeZmm) + "|" + R(pocket.MinorDiameterMm));
            }
            tokens.Sort(StringComparer.Ordinal);
            return string.Join(";", tokens.ToArray());
        }

        private static string R(double value)
        {
            return Math.Round(value, 3).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static PortalAssemblyPart TranslateAssemblyPart(PortalAssemblyPart source, double dx, double dy, double dz)
        {
            var translated = new PortalAssemblyPart
            {
                Name = source.Name,
                Kind = source.Kind,
                Shape = source.Shape,
                Xmm = source.Xmm + dx,
                Ymm = source.Ymm + dy,
                Zmm = source.Zmm + dz,
                SizeXmm = source.SizeXmm,
                SizeYmm = source.SizeYmm,
                SizeZmm = source.SizeZmm,
                CornerRadiusMm = source.CornerRadiusMm
            };
            foreach (var hole in source.Holes)
            {
                translated.Holes.Add(new PortalAssemblyHole
                {
                    Name = hole.Name,
                    Xmm = hole.Xmm + dx,
                    Ymm = hole.Ymm + dy,
                    Zmm = hole.Zmm + dz,
                    DiameterMm = hole.DiameterMm,
                    DepthMm = hole.DepthMm,
                    Plane = hole.Plane,
                    IsThroughCutout = hole.IsThroughCutout,
                    Countersunk = hole.Countersunk,
                    CountersinkDiameterMm = hole.CountersinkDiameterMm,
                    CountersinkDepthMm = hole.CountersinkDepthMm
                });
            }
            foreach (var pocket in source.Pockets)
            {
                translated.Pockets.Add(new PortalAssemblyPocket
                {
                    Name = pocket.Name,
                    Shape = pocket.Shape,
                    Xmm = pocket.Xmm + dx,
                    Ymm = pocket.Ymm + dy,
                    Zmm = pocket.Zmm + dz,
                    SizeXmm = pocket.SizeXmm,
                    SizeYmm = pocket.SizeYmm,
                    SizeZmm = pocket.SizeZmm,
                    Plane = pocket.Plane,
                    IsThroughCutout = pocket.IsThroughCutout,
                    MinorDiameterMm = pocket.MinorDiameterMm
                });
            }
            return translated;
        }

        private static Body2 ApplyHoleCuts(Modeler modeler, PortalAssemblyPart part, Body2 target)
        {
            if (part.Holes == null || part.Holes.Count == 0) return target;
            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var hole in part.Holes)
            {
                if (hole == null || hole.DiameterMm <= 0 || (hole.DepthMm <= 0 && !hole.IsThroughCutout)) continue;
                var faceSign = HoleFaceSign(part, hole);
                var key = HoleCutKey(part, hole, faceSign);
                if (!processed.Add(key)) continue;

                var cutter = CreateHoleCutter(modeler, part, hole, faceSign);
                target = SubtractCutter(target, cutter, part.Name, hole.Name ?? "bevestigingsboring");
                if (hole.Countersunk && hole.CountersinkDiameterMm > hole.DiameterMm && hole.CountersinkDepthMm > 0)
                {
                    var countersink = CreateHoleCountersinkCutter(modeler, part, hole, faceSign);
                    target = SubtractCutter(target, countersink, part.Name, (hole.Name ?? "bevestigingsboring") + " verzinking");
                }
            }
            return target;
        }

        private static Body2 SubtractCutter(Body2 target, Body2 cutter, string partName, string operationName)
        {
            if (cutter == null) throw new InvalidOperationException("SolidWorks kon de boorbewerking niet opbouwen voor " + partName + ": " + operationName + ".");
            var error = 0;
            object[] result;
            try
            {
                result = target.Operations2(SolidBodyCutOperation, cutter, out error) as object[];
            }
            finally
            {
                ReleaseCom(cutter);
            }
            if (error != 0 || result == null || result.Length == 0 || !(result[0] is Body2))
                throw new InvalidOperationException("SolidWorks kon de boorbewerking niet aftrekken voor " + partName + ": " + operationName + " (code " + error + ").");
            var next = (Body2)result[0];
            if (!ReferenceEquals(target, next)) ReleaseCom(target);
            for (var i = 1; i < result.Length; i++) ReleaseCom(result[i]);
            return next;
        }

        private static Body2 CreateHoleCutter(Modeler modeler, PortalAssemblyPart part, PortalAssemblyHole hole, double faceSign)
        {
            var normalSize = HoleNormalSize(part, hole.Plane);
            var depth = hole.IsThroughCutout ? normalSize : Math.Min(normalSize, hole.DepthMm);
            var face = HoleNormalCenter(part, hole.Plane) + faceSign * normalSize / 2.0;
            var start = face + faceSign * BooleanOverlapMm;
            var direction = -faceSign;
            var radius = MmToM(hole.DiameterMm / 2.0);
            var length = MmToM(depth + 2.0 * BooleanOverlapMm);
            double[] cylinder;
            if (string.Equals(hole.Plane, "x", StringComparison.OrdinalIgnoreCase))
                cylinder = new[] { MmToM(start), MmToM(hole.Ymm), MmToM(hole.Zmm), direction, 0.0, 0.0, radius, length };
            else if (string.Equals(hole.Plane, "y", StringComparison.OrdinalIgnoreCase))
                cylinder = new[] { MmToM(hole.Xmm), MmToM(start), MmToM(hole.Zmm), 0.0, direction, 0.0, radius, length };
            else
                cylinder = new[] { MmToM(hole.Xmm), MmToM(hole.Ymm), MmToM(start), 0.0, 0.0, direction, radius, length };
            return modeler.CreateBodyFromCyl(cylinder) as Body2;
        }

        private static Body2 CreateHoleCountersinkCutter(Modeler modeler, PortalAssemblyPart part, PortalAssemblyHole hole, double faceSign)
        {
            var normalSize = HoleNormalSize(part, hole.Plane);
            var depth = Math.Min(normalSize, hole.CountersinkDepthMm);
            var face = HoleNormalCenter(part, hole.Plane) + faceSign * normalSize / 2.0;
            if (!string.IsNullOrWhiteSpace(hole.Name) && hole.Name.StartsWith("Kogelpot ", StringComparison.OrdinalIgnoreCase))
            {
                var start = face + faceSign * BooleanOverlapMm;
                var direction = -faceSign;
                var radius = MmToM(hole.CountersinkDiameterMm / 2.0);
                var counterboreLength = MmToM(depth + 2.0 * BooleanOverlapMm);
                double[] cylinder;
                if (string.Equals(hole.Plane, "x", StringComparison.OrdinalIgnoreCase))
                    cylinder = new[] { MmToM(start), MmToM(hole.Ymm), MmToM(hole.Zmm), direction, 0.0, 0.0, radius, counterboreLength };
                else if (string.Equals(hole.Plane, "y", StringComparison.OrdinalIgnoreCase))
                    cylinder = new[] { MmToM(hole.Xmm), MmToM(start), MmToM(hole.Zmm), 0.0, direction, 0.0, radius, counterboreLength };
                else
                    cylinder = new[] { MmToM(hole.Xmm), MmToM(hole.Ymm), MmToM(start), 0.0, 0.0, direction, radius, counterboreLength };
                return modeler.CreateBodyFromCyl(cylinder) as Body2;
            }
            var smallEnd = face - faceSign * (depth + BooleanOverlapMm);
            var smallRadius = MmToM(hole.DiameterMm / 2.0);
            var largeRadius = MmToM(hole.CountersinkDiameterMm / 2.0);
            var length = MmToM(depth + 2.0 * BooleanOverlapMm);
            double[] cone;
            if (string.Equals(hole.Plane, "x", StringComparison.OrdinalIgnoreCase))
                cone = new[] { MmToM(smallEnd), MmToM(hole.Ymm), MmToM(hole.Zmm), faceSign, 0.0, 0.0, smallRadius, largeRadius, length };
            else if (string.Equals(hole.Plane, "y", StringComparison.OrdinalIgnoreCase))
                cone = new[] { MmToM(hole.Xmm), MmToM(smallEnd), MmToM(hole.Zmm), 0.0, faceSign, 0.0, smallRadius, largeRadius, length };
            else
                cone = new[] { MmToM(hole.Xmm), MmToM(hole.Ymm), MmToM(smallEnd), 0.0, 0.0, faceSign, smallRadius, largeRadius, length };
            return modeler.CreateBodyFromCone(cone) as Body2;
        }

        private static double HoleFaceSign(PortalAssemblyPart part, PortalAssemblyHole hole)
        {
            return HoleNormalCoordinate(hole, hole.Plane) >= HoleNormalCenter(part, hole.Plane) ? 1.0 : -1.0;
        }

        private static double HoleNormalCoordinate(PortalAssemblyHole hole, string plane)
        {
            if (string.Equals(plane, "x", StringComparison.OrdinalIgnoreCase)) return hole.Xmm;
            if (string.Equals(plane, "y", StringComparison.OrdinalIgnoreCase)) return hole.Ymm;
            return hole.Zmm;
        }

        private static double HoleNormalCenter(PortalAssemblyPart part, string plane)
        {
            if (string.Equals(plane, "x", StringComparison.OrdinalIgnoreCase)) return part.Xmm;
            if (string.Equals(plane, "y", StringComparison.OrdinalIgnoreCase)) return part.Ymm;
            return part.Zmm;
        }

        private static double HoleNormalSize(PortalAssemblyPart part, string plane)
        {
            if (string.Equals(plane, "x", StringComparison.OrdinalIgnoreCase)) return part.SizeXmm;
            if (string.Equals(plane, "y", StringComparison.OrdinalIgnoreCase)) return part.SizeYmm;
            return part.SizeZmm;
        }

        private static string HoleCutKey(PortalAssemblyPart part, PortalAssemblyHole hole, double faceSign)
        {
            double a;
            double b;
            if (string.Equals(hole.Plane, "x", StringComparison.OrdinalIgnoreCase)) { a = hole.Ymm; b = hole.Zmm; }
            else if (string.Equals(hole.Plane, "y", StringComparison.OrdinalIgnoreCase)) { a = hole.Xmm; b = hole.Zmm; }
            else { a = hole.Xmm; b = hole.Ymm; }
            return (hole.Plane ?? "z") + "|" + Math.Round(a, 3) + "|" + Math.Round(b, 3) + "|" + Math.Round(hole.DiameterMm, 3)
                + "|" + (hole.IsThroughCutout ? "through" : faceSign.ToString("0"));
        }

        private static Body2 ApplyPocketCuts(Modeler modeler, PortalAssemblyPart part, Body2 target)
        {
            if (part.Pockets == null || part.Pockets.Count == 0) return target;
            foreach (var pocket in part.Pockets)
            {
                if (pocket == null || IsVisualReveal(pocket) || !BoundsIntersect(part, pocket)) continue;
                var cutter = CreatePocketCutter(modeler, pocket);
                if (cutter == null) throw new InvalidOperationException("SolidWorks kon de freesbewerking niet opbouwen voor " + part.Name + ": " + pocket.Name + ".");
                target = SubtractCutter(target, cutter, part.Name, pocket.Name);
            }
            return target;
        }

        private static Body2 CreatePocketCutter(Modeler modeler, PortalAssemblyPocket pocket)
        {
            var sx = pocket.SizeXmm;
            var sy = pocket.SizeYmm;
            var sz = pocket.SizeZmm;
            if (!string.IsNullOrWhiteSpace(pocket.Shape) && pocket.Shape.StartsWith("cone", StringComparison.OrdinalIgnoreCase))
            {
                var negative = pocket.Shape.EndsWith("-", StringComparison.OrdinalIgnoreCase);
                var smallRadius = MmToM(Math.Max(0.1, pocket.MinorDiameterMm) / 2.0);
                double[] cone;
                if (string.Equals(pocket.Plane, "x", StringComparison.OrdinalIgnoreCase))
                {
                    cone = new[]
                    {
                        MmToM(pocket.Xmm + (negative ? sx / 2.0 + BooleanOverlapMm : -sx / 2.0 - BooleanOverlapMm)), MmToM(pocket.Ymm), MmToM(pocket.Zmm),
                        negative ? -1.0 : 1.0, 0.0, 0.0,
                        smallRadius, MmToM(Math.Min(sy, sz) / 2.0), MmToM(sx + 2.0 * BooleanOverlapMm)
                    };
                }
                else if (string.Equals(pocket.Plane, "y", StringComparison.OrdinalIgnoreCase))
                {
                    cone = new[]
                    {
                        MmToM(pocket.Xmm), MmToM(pocket.Ymm + (negative ? sy / 2.0 + BooleanOverlapMm : -sy / 2.0 - BooleanOverlapMm)), MmToM(pocket.Zmm),
                        0.0, negative ? -1.0 : 1.0, 0.0,
                        smallRadius, MmToM(Math.Min(sx, sz) / 2.0), MmToM(sy + 2.0 * BooleanOverlapMm)
                    };
                }
                else
                {
                    cone = new[]
                    {
                        MmToM(pocket.Xmm), MmToM(pocket.Ymm), MmToM(pocket.Zmm + (negative ? sz / 2.0 + BooleanOverlapMm : -sz / 2.0 - BooleanOverlapMm)),
                        0.0, 0.0, negative ? -1.0 : 1.0,
                        smallRadius, MmToM(Math.Min(sx, sy) / 2.0), MmToM(sz + 2.0 * BooleanOverlapMm)
                    };
                }
                return modeler.CreateBodyFromCone(cone) as Body2;
            }
            if (string.Equals(pocket.Shape, "cylinder", StringComparison.OrdinalIgnoreCase))
            {
                double[] cylinder;
                if (string.Equals(pocket.Plane, "x", StringComparison.OrdinalIgnoreCase))
                {
                    cylinder = new[] { MmToM(pocket.Xmm - sx / 2.0 - BooleanOverlapMm), MmToM(pocket.Ymm), MmToM(pocket.Zmm), 1.0, 0.0, 0.0, MmToM(Math.Min(sy, sz) / 2.0), MmToM(sx + 2.0 * BooleanOverlapMm) };
                }
                else if (string.Equals(pocket.Plane, "y", StringComparison.OrdinalIgnoreCase))
                {
                    cylinder = new[] { MmToM(pocket.Xmm), MmToM(pocket.Ymm - sy / 2.0 - BooleanOverlapMm), MmToM(pocket.Zmm), 0.0, 1.0, 0.0, MmToM(Math.Min(sx, sz) / 2.0), MmToM(sy + 2.0 * BooleanOverlapMm) };
                }
                else
                {
                    cylinder = new[] { MmToM(pocket.Xmm), MmToM(pocket.Ymm), MmToM(pocket.Zmm - sz / 2.0 - BooleanOverlapMm), 0.0, 0.0, 1.0, MmToM(Math.Min(sx, sy) / 2.0), MmToM(sz + 2.0 * BooleanOverlapMm) };
                }
                return modeler.CreateBodyFromCyl(cylinder) as Body2;
            }

            return CreateBoxBody(
                modeler,
                pocket.Xmm,
                pocket.Ymm,
                pocket.Zmm,
                sx + (string.Equals(pocket.Plane, "x", StringComparison.OrdinalIgnoreCase) ? 2.0 * BooleanOverlapMm : 0.0),
                sy + (string.Equals(pocket.Plane, "y", StringComparison.OrdinalIgnoreCase) ? 2.0 * BooleanOverlapMm : 0.0),
                sz + (string.Equals(pocket.Plane, "z", StringComparison.OrdinalIgnoreCase) ? 2.0 * BooleanOverlapMm : 0.0));
        }

        private static Body2 CreateBoxBody(Modeler modeler, double x, double y, double z, double sx, double sy, double sz)
        {
            var box = new[]
            {
                MmToM(x), MmToM(y), MmToM(z - sz / 2.0),
                0.0, 0.0, 1.0,
                MmToM(sx), MmToM(sy), MmToM(sz)
            };
            return modeler.CreateBodyFromBox3(box) as Body2;
        }

        private static Body2 ApplyPanelCornerRadius(Modeler modeler, PortalAssemblyPart part, Body2 target)
        {
            var radius = Math.Max(0, Math.Min(part.CornerRadiusMm, Math.Min(part.SizeXmm, part.SizeYmm) / 2.0));
            if (radius <= 0.001) return target;
            if (part.SizeZmm > part.SizeXmm || part.SizeZmm > part.SizeYmm)
                throw new InvalidOperationException("Hoekradius voor " + part.Name + " verwacht een frontpaneel met de plaatdikte in de Z-richting.");

            var minX = part.Xmm - part.SizeXmm / 2.0;
            var maxX = part.Xmm + part.SizeXmm / 2.0;
            var minY = part.Ymm - part.SizeYmm / 2.0;
            var maxY = part.Ymm + part.SizeYmm / 2.0;
            foreach (var xSign in new[] { -1.0, 1.0 })
            foreach (var ySign in new[] { -1.0, 1.0 })
            {
                var centerX = xSign < 0 ? minX + radius : maxX - radius;
                var centerY = ySign < 0 ? minY + radius : maxY - radius;
                var scrapCenterX = xSign < 0 ? minX + (radius - BooleanOverlapMm) / 2.0 : maxX - (radius - BooleanOverlapMm) / 2.0;
                var scrapCenterY = ySign < 0 ? minY + (radius - BooleanOverlapMm) / 2.0 : maxY - (radius - BooleanOverlapMm) / 2.0;
                var scrap = CreateBoxBody(modeler, scrapCenterX, scrapCenterY, part.Zmm, radius + BooleanOverlapMm, radius + BooleanOverlapMm, part.SizeZmm + 2.0 * BooleanOverlapMm);
                if (scrap == null) throw new InvalidOperationException("SolidWorks kon de hoekuitsparing niet opbouwen voor " + part.Name + ".");
                var cylinderData = new[]
                {
                    MmToM(centerX), MmToM(centerY), MmToM(part.Zmm - part.SizeZmm / 2.0 - BooleanOverlapMm),
                    0.0, 0.0, 1.0,
                    MmToM(radius), MmToM(part.SizeZmm + 2.0 * BooleanOverlapMm)
                };
                var cylinder = modeler.CreateBodyFromCyl(cylinderData) as Body2;
                scrap = SubtractCutter(scrap, cylinder, part.Name, "hoekradius hulpvolume");
                target = SubtractCutter(target, scrap, part.Name, "hoekradius " + radius.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " mm");
            }
            return target;
        }

        private static void CreatePlinthAdapterPart(
            SldWorks solidWorks,
            string partPath,
            string stlPath,
            PlinthClipAdapterTemplate adapter,
            double standOff,
            double wingSign)
        {
            var model = (ModelDoc2)solidWorks.NewPart();
            if (model == null) throw new InvalidOperationException("SolidWorks kon geen nieuw adapterpart maken.");
            var part = (PartDoc)model;
            var modeler = (Modeler)solidWorks.GetModeler();
            var slotWidth = adapter.SlotWidthMm;
            var slotHeight = adapter.SlotHeightMm;
            var slotDepth = adapter.SlotDepthMm;
            var lipOverlap = adapter.GuideLipOverlapMm;
            var lipThickness = adapter.GuideLipThicknessMm;
            var totalDepth = standOff + slotDepth + lipThickness;
            var wingExtension = Math.Max(0, adapter.MountingWingExtensionMm);
            var body = CreateBoxBody(modeler, wingSign * wingExtension / 2.0, 0, totalDepth / 2.0, adapter.BackPlateWidthMm + wingExtension, adapter.BackPlateHeightMm, totalDepth);
            if (body == null) throw new InvalidOperationException("SolidWorks kon de adapterbasis niet maken.");

            var channelBottom = -slotHeight / 2.0;
            var channelTop = adapter.BackPlateHeightMm / 2.0 + BooleanOverlapMm;
            var channelHeight = channelTop - channelBottom;
            var channelCenterY = (channelTop + channelBottom) / 2.0;
            body = CutAdapterBox(modeler, body, 0, channelCenterY, standOff + slotDepth / 2.0, slotWidth, channelHeight, slotDepth, partPath);
            body = CutAdapterBox(modeler, body, 0, channelCenterY, standOff + slotDepth + lipThickness / 2.0, slotWidth - 2.0 * lipOverlap, channelHeight, lipThickness, partPath);

            var screwOffset = adapter.MountingHoleSpacingMm / 2.0;
            var upperScrewX = wingSign * adapter.UpperMountingHoleHorizontalOffsetMm;
            body = CutAdapterScrewHole(modeler, body, 0, -screwOffset, adapter.MountingHoleDiameterMm, totalDepth, partPath);
            body = CutAdapterScrewHole(modeler, body, upperScrewX, screwOffset, adapter.MountingHoleDiameterMm, totalDepth, partPath);
            body = CutAdapterCountersink(modeler, body, 0, -screwOffset, totalDepth, adapter.MountingHoleDiameterMm, adapter.MountingCountersinkDiameterMm, adapter.MountingCountersinkDepthMm, partPath);
            body = CutAdapterCountersink(modeler, body, upperScrewX, screwOffset, totalDepth, adapter.MountingHoleDiameterMm, adapter.MountingCountersinkDiameterMm, adapter.MountingCountersinkDepthMm, partPath);

            var feature = part.CreateFeatureFromBody3(body, false, 1) as Feature;
            if (feature == null) throw new InvalidOperationException("SolidWorks kon het adapterbody niet invoegen voor " + partPath + ".");
            feature.Name = "SEKTION_plintclip_adapter";
            model.EditRebuild3();
            model.ViewZoomtofit2();

            var errors = 0;
            var warnings = 0;
            model.Extension.SaveAs(partPath, SaveAsCurrentVersion, SaveAsOptionsSilent, null, ref errors, ref warnings);
            if (errors != 0) throw new InvalidOperationException("SolidWorks kon het adapterpart niet opslaan (code " + errors + "): " + partPath);
            errors = 0;
            warnings = 0;
            model.Extension.SaveAs(stlPath, SaveAsCurrentVersion, SaveAsOptionsSilent, null, ref errors, ref warnings);
            var title = model.GetTitle();
            solidWorks.CloseDoc(title);
            if (errors != 0 || !File.Exists(stlPath)) throw new InvalidOperationException("SolidWorks kon de adapter-STL niet opslaan (code " + errors + "): " + stlPath);
        }

        private static Body2 CutAdapterBox(Modeler modeler, Body2 target, double x, double y, double z, double sx, double sy, double sz, string partPath)
        {
            var cutter = CreateBoxBody(modeler, x, y, z, sx, sy, sz);
            if (cutter == null) throw new InvalidOperationException("SolidWorks kon een adapterkamer niet maken voor " + partPath + ".");
            var error = 0;
            var result = target.Operations2(SolidBodyCutOperation, cutter, out error) as object[];
            if (error != 0 || result == null || result.Length == 0 || !(result[0] is Body2))
                throw new InvalidOperationException("SolidWorks kon een adapterkamer niet aftrekken (code " + error + "): " + partPath);
            return (Body2)result[0];
        }

        private static Body2 CutAdapterScrewHole(Modeler modeler, Body2 target, double x, double y, double diameter, double totalDepth, string partPath)
        {
            var cylinder = new[]
            {
                MmToM(x), MmToM(y), MmToM(-BooleanOverlapMm),
                0.0, 0.0, 1.0,
                MmToM(diameter / 2.0), MmToM(totalDepth + 2.0 * BooleanOverlapMm)
            };
            var cutter = modeler.CreateBodyFromCyl(cylinder) as Body2;
            if (cutter == null) throw new InvalidOperationException("SolidWorks kon een adapterschroefgat niet maken voor " + partPath + ".");
            var error = 0;
            var result = target.Operations2(SolidBodyCutOperation, cutter, out error) as object[];
            if (error != 0 || result == null || result.Length == 0 || !(result[0] is Body2))
                throw new InvalidOperationException("SolidWorks kon een adapterschroefgat niet aftrekken (code " + error + "): " + partPath);
            return (Body2)result[0];
        }

        private static Body2 CutAdapterCountersink(Modeler modeler, Body2 target, double x, double y, double seatZ, double holeDiameter, double countersinkDiameter, double depth, string partPath)
        {
            if (countersinkDiameter <= holeDiameter || depth <= 0) return target;
            var cone = new[]
            {
                MmToM(x), MmToM(y), MmToM(seatZ - depth - BooleanOverlapMm),
                0.0, 0.0, 1.0,
                MmToM(holeDiameter / 2.0), MmToM(countersinkDiameter / 2.0), MmToM(depth + 2.0 * BooleanOverlapMm)
            };
            var cutter = modeler.CreateBodyFromCone(cone) as Body2;
            if (cutter == null) throw new InvalidOperationException("SolidWorks kon een conische adapterverzinking niet maken voor " + partPath + ".");
            var error = 0;
            var result = target.Operations2(SolidBodyCutOperation, cutter, out error) as object[];
            if (error != 0 || result == null || result.Length == 0 || !(result[0] is Body2))
                throw new InvalidOperationException("SolidWorks kon een conische adapterverzinking niet aftrekken (code " + error + "): " + partPath);
            return (Body2)result[0];
        }

        private static bool IsVisualReveal(PortalAssemblyPocket pocket)
        {
            return pocket.Name != null && pocket.Name.EndsWith(" zichtbaar", StringComparison.OrdinalIgnoreCase);
        }

        private static bool BoundsIntersect(PortalAssemblyPart part, PortalAssemblyPocket pocket)
        {
            const double epsilon = 0.001;
            return Math.Min(part.Xmm + part.SizeXmm / 2.0, pocket.Xmm + pocket.SizeXmm / 2.0) - Math.Max(part.Xmm - part.SizeXmm / 2.0, pocket.Xmm - pocket.SizeXmm / 2.0) > epsilon &&
                   Math.Min(part.Ymm + part.SizeYmm / 2.0, pocket.Ymm + pocket.SizeYmm / 2.0) - Math.Max(part.Ymm - part.SizeYmm / 2.0, pocket.Ymm - pocket.SizeYmm / 2.0) > epsilon &&
                   Math.Min(part.Zmm + part.SizeZmm / 2.0, pocket.Zmm + pocket.SizeZmm / 2.0) - Math.Max(part.Zmm - part.SizeZmm / 2.0, pocket.Zmm - pocket.SizeZmm / 2.0) > epsilon;
        }

        private static SldWorks GetOrCreateSolidWorks()
        {
            var running = TryGetRunningSolidWorks();
            if (running != null) return running;

            if (File.Exists(ThreeExperienceLauncherPath))
            {
                StartThreeExperienceSolidWorks();
                var deadline = DateTime.UtcNow.AddMinutes(5);
                while (DateTime.UtcNow < deadline)
                {
                    Thread.Sleep(2000);
                    running = TryGetRunningSolidWorks();
                    if (running != null) return running;
                }

                throw new InvalidOperationException(
                    "SOLIDWORKS Design is via de 3DEXPERIENCE-launcher gestart, maar registreerde binnen 5 minuten geen COM-sessie. "
                    + "Controleer of een 3DEXPERIENCE-inlogvenster wacht op aanmelding, rond de login af en probeer daarna opnieuw.");
            }

            try
            {
                var type = Type.GetTypeFromProgID("SldWorks.Application");
                if (type != null) return (SldWorks)Activator.CreateInstance(type);
            }
            catch (Exception error)
            {
                throw new InvalidOperationException("Geen actieve of startbare SOLIDWORKS COM-sessie gevonden.", error);
            }

            throw new InvalidOperationException("Geen actieve of startbare SOLIDWORKS COM-sessie gevonden.");
        }

        private static SldWorks TryGetRunningSolidWorks()
        {
            try
            {
                var standard = Marshal.GetActiveObject("SldWorks.Application") as SldWorks;
                if (IsSolidWorksResponsive(standard)) return standard;
                ReleaseCom(standard);
            }
            catch
            {
            }

            IRunningObjectTable table = null;
            IEnumMoniker enumerator = null;
            IBindCtx bindContext = null;
            try
            {
                if (GetRunningObjectTable(0, out table) != 0 || table == null) return null;
                if (CreateBindCtx(0, out bindContext) != 0 || bindContext == null) return null;
                table.EnumRunning(out enumerator);
                if (enumerator == null) return null;

                var monikers = new IMoniker[1];
                while (enumerator.Next(1, monikers, IntPtr.Zero) == 0)
                {
                    string displayName = null;
                    try { monikers[0].GetDisplayName(bindContext, null, out displayName); }
                    catch { }
                    if (string.IsNullOrWhiteSpace(displayName)
                        || !displayName.StartsWith("SolidWorks_PID_", StringComparison.OrdinalIgnoreCase)) continue;

                    object instance;
                    table.GetObject(monikers[0], out instance);
                    var solidWorks = instance as SldWorks;
                    if (IsSolidWorksResponsive(solidWorks)) return solidWorks;
                    ReleaseCom(solidWorks);
                }
            }
            catch
            {
            }
            finally
            {
                if (enumerator != null) Marshal.ReleaseComObject(enumerator);
                if (bindContext != null) Marshal.ReleaseComObject(bindContext);
                if (table != null) Marshal.ReleaseComObject(table);
            }

            return null;
        }

        private static bool IsSolidWorksResponsive(SldWorks solidWorks)
        {
            if (solidWorks == null) return false;
            try
            {
                // Een achtergebleven ROT/COM-verwijzing kan nog castbaar zijn nadat
                // SLDWORKS is gestopt. Deze echte serveraanroep filtert zo'n dode
                // sessie uit voordat de partgeneratie begint.
                return !string.IsNullOrWhiteSpace(solidWorks.RevisionNumber());
            }
            catch
            {
                return false;
            }
        }

        private static void StartThreeExperienceSolidWorks()
        {
            var desktopShortcut = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory),
                "SOLIDWORKS Design.lnk");
            var start = File.Exists(desktopShortcut)
                ? new ProcessStartInfo
                {
                    FileName = desktopShortcut,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal
                }
                : new ProcessStartInfo
                {
                    FileName = ThreeExperienceLauncherPath,
                    Arguments = ThreeExperienceLauncherArguments,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(ThreeExperienceLauncherPath),
                    WindowStyle = ProcessWindowStyle.Normal
                };

            try
            {
                Process.Start(start);
            }
            catch (Exception error)
            {
                throw new InvalidOperationException("De SOLIDWORKS 3DEXPERIENCE-launcher kon niet worden gestart: " + ThreeExperienceLauncherPath, error);
            }
        }

        private static void CreateBoxPart(SldWorks solidWorks, string filePath, double xMm, double yMm, double zMm)
        {
            var model = (ModelDoc2)solidWorks.NewPart();
            if (model == null) throw new InvalidOperationException("SolidWorks kon geen nieuw part-document maken. Controleer de default part template.");
            var title = model.GetTitle();
            Feature baseFeature = null;
            try
            {
                var x = MmToM(xMm);
                var y = MmToM(yMm);
                var z = MmToM(zMm);
                var selected = model.Extension.SelectByID2("Front Plane", "PLANE", 0, 0, 0, false, 0, null, 0);
                if (!selected) selected = model.Extension.SelectByID2("Vlak Voor", "PLANE", 0, 0, 0, false, 0, null, 0);
                if (!selected) throw new InvalidOperationException("Kon het Front Plane/Vlak Voor niet selecteren in het nieuwe part.");

                model.SketchManager.InsertSketch(true);
                model.SketchManager.CreateCenterRectangle(0, 0, 0, x / 2.0, y / 2.0, 0);
                model.SketchManager.InsertSketch(true);
                baseFeature = model.FeatureManager.FeatureExtrusion2(
                    true, false, false, 6, 0, z, 0,
                    false, false, false, false, 0, 0,
                    false, false, false, false, true, true, true,
                    0, 0, false);
                if (baseFeature == null) throw new InvalidOperationException("SolidWorks maakte geen basis-extrusie voor " + filePath);

                var errors = 0;
                var warnings = 0;
                model.Extension.SaveAs(filePath, SaveAsCurrentVersion, SaveAsOptionsSilent, null, ref errors, ref warnings);
                title = model.GetTitle();
                if (errors != 0) throw new InvalidOperationException("SolidWorks SaveAs gaf foutcode " + errors + " voor " + filePath);
            }
            finally
            {
                ReleaseCom(baseFeature);
                try { solidWorks.CloseDoc(title); } catch { }
                ReleaseCom(model);
                ForceComCleanup();
            }
        }

        private static void CreateAssembly(SldWorks solidWorks, WorkbenchModel workbench, Dictionary<string, string> partPaths, string assemblyPath)
        {
            var assemblyModel = (ModelDoc2)solidWorks.NewAssembly();
            if (assemblyModel == null) throw new InvalidOperationException("SolidWorks kon geen nieuwe assembly maken. Controleer de default assembly template.");

            var assemblyTitle = assemblyModel.GetTitle();
            var saveErrors = 0;
            var saveWarnings = 0;
            assemblyModel.Extension.SaveAs(assemblyPath, SaveAsCurrentVersion, SaveAsOptionsSilent, null, ref saveErrors, ref saveWarnings);
            if (saveErrors != 0) throw new InvalidOperationException("SolidWorks SaveAs gaf foutcode " + saveErrors + " voor " + assemblyPath);

            var assembly = (AssemblyDoc)assemblyModel;
            foreach (var placement in workbench.AssemblyPlacements)
            {
                string partPath;
                if (!partPaths.TryGetValue(PartKey(placement.Kind, placement.PartName), out partPath) || !File.Exists(partPath))
                {
                    throw new InvalidOperationException("Part ontbreekt voor assemblycomponent: " + placement.PartName);
                }

                var component = assembly.AddComponent5(
                    partPath, 0, "", false, "",
                    MmToM(placement.Xmm), MmToM(placement.Ymm), MmToM(placement.Zmm));
                if (component == null) throw new InvalidOperationException("SolidWorks kon component niet toevoegen: " + placement.PartName);

                component.Select4(false, null, false);
                assembly.FixComponent();
            }

            assemblyModel.EditRebuild3();
            assemblyModel.ViewZoomtofit2();
            saveErrors = 0;
            saveWarnings = 0;
            assemblyModel.Extension.SaveAs(assemblyPath, SaveAsCurrentVersion, SaveAsOptionsSilent, null, ref saveErrors, ref saveWarnings);
            if (saveErrors != 0) throw new InvalidOperationException("SolidWorks kon de controle-assembly niet opslaan (code " + saveErrors + ").");
        }

        private static AssemblyPlacement FindPlacement(WorkbenchModel model, AssemblyComponentKind kind, string partName)
        {
            foreach (var placement in model.AssemblyPlacements)
            {
                if (placement.Kind == kind && string.Equals(placement.PartName, partName, StringComparison.OrdinalIgnoreCase)) return placement;
            }
            return null;
        }

        private static Dimensions OrientedSheetDimensions(SheetPart sheet, AssemblyOrientation orientation)
        {
            var thickness = sheet.Material == null || sheet.Material.ThicknessMm <= 0 ? 18.0 : sheet.Material.ThicknessMm;
            if (orientation == AssemblyOrientation.SheetVerticalX) return new Dimensions(sheet.LengthMm, sheet.WidthMm, thickness);
            if (orientation == AssemblyOrientation.SheetVerticalZ) return new Dimensions(thickness, sheet.WidthMm, sheet.LengthMm);
            return new Dimensions(sheet.LengthMm, thickness, sheet.WidthMm);
        }

        private static string PartKey(AssemblyComponentKind kind, string name)
        {
            return kind + "|" + (name ?? "");
        }

        private static double MmToM(double value)
        {
            return value / 1000.0;
        }

        private static void ReleaseCom(object value)
        {
            if (value == null || !Marshal.IsComObject(value)) return;
            try { Marshal.FinalReleaseComObject(value); }
            catch { }
        }

        private static void ForceComCleanup()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private static string SafeName(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
            return value.Replace("/", "-");
        }

        private static string SafeFeatureName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Controlebody";
            return value.Replace("/", "-").Replace("\\", "-").Replace(":", "-");
        }

        private sealed class Dimensions
        {
            public Dimensions(double x, double y, double z)
            {
                Xmm = x;
                Ymm = y;
                Zmm = z;
            }

            public double Xmm { get; private set; }
            public double Ymm { get; private set; }
            public double Zmm { get; private set; }
        }

    }
}
