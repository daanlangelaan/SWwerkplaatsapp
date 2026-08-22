using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using SolidWorks.Interop.sldworks;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.SolidWorks
{
    /// <summary>
    /// Houdt klantpresentatie los van de geometrie-export. De geometrie blijft uit het
    /// controlemodel komen; deze module voegt alleen appearances toe en maakt een GLB.
    /// </summary>
    public sealed class SolidWorksCustomerPresentation
    {
        private const int SaveAsCurrentVersion = 0;
        private const int SaveAsOptionsSilent = 1;
        private const int ThisConfiguration = 1;
        private const int AllDisplayStates = 2;
        private const int AutoScaleTextureToModelSizePreference = 510;
        private const int NoUserPreferenceOption = 0;
        private readonly ModelDoc2 model;
        private readonly string controlPath;
        private readonly Dictionary<string, RenderMaterial> betonplexEdgeMaterials = new Dictionary<string, RenderMaterial>();
        private readonly HashSet<RenderMaterial> betonplexEdgeMaterialsWithFaces = new HashSet<RenderMaterial>();
        private readonly Dictionary<RenderMaterial, int[]> betonplexEdgeMappingAxes = new Dictionary<RenderMaterial, int[]>();
        private readonly Dictionary<string, string> betonplexEdgeAppearancePaths = new Dictionary<string, string>();
        private readonly Dictionary<string, RenderMaterial> solidRenderMaterials = new Dictionary<string, RenderMaterial>();
        private readonly HashSet<RenderMaterial> solidRenderMaterialsWithFaces = new HashSet<RenderMaterial>();
        private readonly Dictionary<string, RenderMaterial> aluminiumProfileMaterials = new Dictionary<string, RenderMaterial>();
        private readonly HashSet<RenderMaterial> aluminiumProfileMaterialsWithFaces = new HashSet<RenderMaterial>();
        private readonly Dictionary<RenderMaterial, int[]> aluminiumProfileMappingAxes = new Dictionary<RenderMaterial, int[]>();
        private readonly Dictionary<string, string> aluminiumProfileAppearancePaths = new Dictionary<string, string>();

        public SolidWorksCustomerPresentation(ModelDoc2 model, string controlPath)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (controlPath == null) throw new ArgumentNullException("controlPath");
            this.model = model;
            this.controlPath = controlPath;
            // Bewaar de fysieke textuurmaat uit de P2M. Anders rekt SOLIDWORKS de
            // 18 mm laagopbouw automatisch uit tot ongeveer de volledige modelmaat.
            this.model.Extension.SetUserPreferenceToggle(
                AutoScaleTextureToModelSizePreference,
                NoUserPreferenceOption,
                false);
        }

        public void ApplyAppearance(Feature feature, PortalAssemblyPart visual, WorkbenchModel model)
        {
            if (feature == null || visual == null) return;
            var materialName = FindMaterialName(visual, model);
            feature.SetMaterialPropertyValues2(ResolveAppearance(visual, materialName), ThisConfiguration, null);
            if (materialName.IndexOf("betonplex", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ApplyBetonplexAppearance(feature, visual);
                return;
            }

            if (string.Equals(visual.Kind, "profile", StringComparison.OrdinalIgnoreCase))
            {
                ApplyAluminiumProfileAppearance(feature, visual);
                return;
            }

            ApplySolidAppearance(feature, ResolveSolidAppearance(visual, materialName));
        }

        public void CommitAppearances()
        {
            if (betonplexEdgeMaterialsWithFaces.Count == 0 &&
                aluminiumProfileMaterialsWithFaces.Count == 0 &&
                solidRenderMaterialsWithFaces.Count == 0) return;
            var configuration = model.GetActiveConfiguration() as Configuration;
            var displayStates = configuration == null ? null : configuration.GetDisplayStates();
            var materials = new HashSet<RenderMaterial>(solidRenderMaterialsWithFaces);
            materials.UnionWith(betonplexEdgeMaterialsWithFaces);
            materials.UnionWith(aluminiumProfileMaterialsWithFaces);
            foreach (var material in materials)
            {
                int[] mappingAxes;
                if (betonplexEdgeMappingAxes.TryGetValue(material, out mappingAxes))
                    ConfigureBetonplexEdgeMapping(material, mappingAxes[0], mappingAxes[1]);
                else if (aluminiumProfileMappingAxes.TryGetValue(material, out mappingAxes))
                    ConfigureAluminiumProfileMapping(material, mappingAxes[0], mappingAxes[1]);
                var firstId = 0;
                var secondId = 0;
                var added = model.Extension.AddDisplayStateSpecificRenderMaterial(
                    material,
                    AllDisplayStates,
                    displayStates,
                    out firstId,
                    out secondId);
                if (!added)
                {
                    throw new InvalidOperationException("SolidWorks kon een klantappearance niet aan het controlemodel koppelen.");
                }

                // AddDisplayStateSpecificRenderMaterial initialiseert de mapping opnieuw.
                // Zet de plaatgerichte schaal en assen daarom ook op de geregistreerde appearance.
                if (mappingAxes != null)
                {
                    if (betonplexEdgeMappingAxes.ContainsKey(material))
                        ConfigureBetonplexEdgeMapping(material, mappingAxes[0], mappingAxes[1]);
                    else
                        ConfigureAluminiumProfileMapping(material, mappingAxes[0], mappingAxes[1]);
                }
            }


            model.Extension.UpdateRenderMaterialsInSceneGraph(true);
        }

        public string ExportGlb(ModelDoc2 model, string controlPath)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (string.IsNullOrWhiteSpace(controlPath)) throw new ArgumentException("Pad van het SolidWorks-controlemodel ontbreekt.");

            var glbPath = CustomerModelPath(controlPath);
            var errors = 0;
            var warnings = 0;
            model.EditRebuild3();
            model.GraphicsRedraw2();
            model.Extension.SaveAs(glbPath, SaveAsCurrentVersion, SaveAsOptionsSilent, null, ref errors, ref warnings);
            if (errors != 0 || !File.Exists(glbPath))
            {
                throw new InvalidOperationException("SolidWorks kon het GLB-klantmodel niet opslaan (code " + errors + "): " + glbPath);
            }

            SolidWorksGlbTextureMapper.ApplyBetonplexEdgeMapping(glbPath);
            SolidWorksCustomerHtmlExporter.Export(glbPath);

            return glbPath;
        }

        public static string CustomerModelPath(string controlPath)
        {
            var folder = Path.GetDirectoryName(controlPath) ?? "";
            var name = Path.GetFileNameWithoutExtension(controlPath) ?? "Klantmodel";
            if (name.EndsWith("_CONTROLE", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - "_CONTROLE".Length);
            }

            return Path.Combine(folder, name + "_KLANTMODEL.glb");
        }

        public static string CustomerHtmlPath(string controlPath)
        {
            return Path.ChangeExtension(CustomerModelPath(controlPath), ".html");
        }

        private void ApplyBetonplexAppearance(Feature feature, PortalAssemblyPart visual)
        {
            var faces = feature.GetFaces() as object[];
            if (faces == null || faces.Length == 0) return;
            var thicknessAxis = SmallestAxis(visual);
            var filmMaterial = EnsureSolidRenderMaterial(SolidAppearance.BetonplexFilm);
            foreach (var value in faces)
            {
                var face = value as Face2;
                if (face == null) continue;
                var normal = face.Normal as double[];
                var length = normal == null || normal.Length < 3
                    ? 0.0
                    : Math.Sqrt(normal[0] * normal[0] + normal[1] * normal[1] + normal[2] * normal[2]);
                if (length >= 0.9 && Math.Abs(normal[thicknessAxis]) <= 0.55)
                {
                    var normalAxis = DominantAxis(normal);
                    var edgeMaterial = EnsureBetonplexEdgeMaterial(thicknessAxis, normalAxis);
                    if (edgeMaterial.AddEntity(face)) betonplexEdgeMaterialsWithFaces.Add(edgeMaterial);
                }
                else if (length < 0.9)
                {
                    // Een cilindrische of anderszins gebogen snijwand heeft geen vaste
                    // Face2.Normal. Dit zijn onder meer de halfronde uiteinden van een
                    // handgreepsleuf en de binnenwanden van boorgaten. Ook daar kijk je
                    // fysiek tegen de lagen van de plaat aan, dus gebruik dezelfde
                    // kopse-kanttextuur. De GLB-nabewerking laat de laagrichting altijd
                    // over de plaatdikte lopen; de gekozen tweede as bepaalt alleen de
                    // projectierichting rondom het gebogen vlak.
                    var curvedProjectionAxis = FirstAxisOtherThan(thicknessAxis);
                    var edgeMaterial = EnsureBetonplexEdgeMaterial(thicknessAxis, curvedProjectionAxis);
                    if (edgeMaterial.AddEntity(face)) betonplexEdgeMaterialsWithFaces.Add(edgeMaterial);
                }
                else if (filmMaterial.AddEntity(face))
                {
                    // Alleen de grote boven-/ondervlakken houden de donkere fenolfilm.
                    solidRenderMaterialsWithFaces.Add(filmMaterial);
                }
            }
        }

        private void ApplySolidAppearance(Feature feature, SolidAppearance appearance)
        {
            var faces = feature.GetFaces() as object[];
            if (faces == null || faces.Length == 0) return;
            var material = EnsureSolidRenderMaterial(appearance);
            foreach (var value in faces)
            {
                var face = value as Face2;
                if (face != null && material.AddEntity(face)) solidRenderMaterialsWithFaces.Add(material);
            }
        }

        private void ApplyAluminiumProfileAppearance(Feature feature, PortalAssemblyPart visual)
        {
            var faces = feature.GetFaces() as object[];
            if (faces == null || faces.Length == 0) return;
            var lengthAxis = LongestAxis(visual);
            var endMaterial = EnsureSolidRenderMaterial(SolidAppearance.AnodizedAluminium);
            foreach (var value in faces)
            {
                var face = value as Face2;
                if (face == null) continue;
                var normal = face.Normal as double[];
                var length = normal == null || normal.Length < 3
                    ? 0.0
                    : Math.Sqrt(normal[0] * normal[0] + normal[1] * normal[1] + normal[2] * normal[2]);
                if (length >= 0.9 && Math.Abs(normal[lengthAxis]) <= 0.55)
                {
                    var normalAxis = DominantAxis(normal);
                    var profileMaterial = EnsureAluminiumProfileMaterial(lengthAxis, normalAxis);
                    if (profileMaterial.AddEntity(face)) aluminiumProfileMaterialsWithFaces.Add(profileMaterial);
                }
                else if (endMaterial.AddEntity(face))
                {
                    solidRenderMaterialsWithFaces.Add(endMaterial);
                }
            }
        }

        private RenderMaterial EnsureSolidRenderMaterial(SolidAppearance appearance)
        {
            RenderMaterial existing;
            if (solidRenderMaterials.TryGetValue(appearance.Key, out existing)) return existing;
            var materialFolder = Path.Combine(Path.GetDirectoryName(controlPath) ?? "", "Materialen");
            Directory.CreateDirectory(materialFolder);
            var appearancePath = Path.Combine(materialFolder, appearance.Key + ".p2m");
            File.WriteAllText(appearancePath, BuildSolidP2m(appearance), Encoding.ASCII);
            var material = model.Extension.CreateRenderMaterial(appearancePath);
            if (material == null)
                throw new InvalidOperationException("SolidWorks kon de appearance niet laden: " + appearancePath);
            material.LinkToFile = false;
            solidRenderMaterials.Add(appearance.Key, material);
            return material;
        }

        private static string BuildSolidP2m(SolidAppearance appearance)
        {
            return
                "\"SurfaceFinishShaderType\" 0\r\n" +
                "\"blurryReflections\" " + (appearance.BlurryReflections ? "on" : "off") + "\r\n" +
                "\"col1\" " + Number(appearance.Red) + " " + Number(appearance.Green) + " " + Number(appearance.Blue) + "\r\n" +
                "\"diffuse_factor\" 1\r\n" +
                "\"doubleSided\" on\r\n" +
                "\"luminousIntensity\" 0\r\n" +
                "\"mtl_ior\" 1.45\r\n" +
                "\"num_cols\" 1\r\n" +
                "\"reflectivity\" " + Number(appearance.Reflectivity) + "\r\n" +
                "\"roughness\" " + Number(appearance.Roughness) + "\r\n" +
                "\"specular_color\" 1 1 1\r\n" +
                "\"specular_factor\" " + Number(appearance.Specular) + "\r\n" +
                "\"swP2M\" on\r\n" +
                "\"sw_shader\" " + appearance.Shader + "\r\n" +
                "\"transparency\" 0\r\n";
        }

        private RenderMaterial EnsureBetonplexEdgeMaterial(int thicknessAxis, int normalAxis)
        {
            var key = thicknessAxis + ":" + normalAxis;
            RenderMaterial existing;
            if (betonplexEdgeMaterials.TryGetValue(key, out existing)) return existing;

            var appearancePath = EnsureBetonplexEdgeAppearanceFile(thicknessAxis, normalAxis);
            var material = model.Extension.CreateRenderMaterial(appearancePath);
            if (material == null)
                throw new InvalidOperationException("SolidWorks kon de betonplex-kopse-kantappearance niet laden: " + appearancePath);

            ConfigureBetonplexEdgeMapping(material, thicknessAxis, normalAxis);

            betonplexEdgeMaterials.Add(key, material);
            betonplexEdgeMappingAxes.Add(material, new[] { thicknessAxis, normalAxis });
            return material;
        }

        private static void ConfigureBetonplexEdgeMapping(RenderMaterial material, int thicknessAxis, int normalAxis)
        {
            // De PNG bevat de laaglijnen horizontaal: U loopt langs de plaatrand en
            // V altijd over de lokale plaatdikte. Een eigen projection per randvlak
            // voorkomt dat de automatische box-mapping een haakse rand 90 graden draait.
            var edgeAxis = RemainingAxis(thicknessAxis, normalAxis);
            material.LinkToFile = false;
            material.MappingType = 1; // Projection
            material.ProjectionReference = ProjectionReferenceForNormalAxis(normalAxis);
            material.FixedAspectRatio = false;
            material.FitWidth = false;
            material.FitHeight = false;
            var thicknessRunsOnU = TextureUsesThicknessOnU(thicknessAxis, normalAxis);
            material.Width = thicknessRunsOnU ? 0.018 : 0.036;
            material.Height = thicknessRunsOnU ? 0.036 : 0.018;
            material.SetUDirection2(edgeAxis == 0 ? 1.0 : 0.0, edgeAxis == 1 ? 1.0 : 0.0, edgeAxis == 2 ? 1.0 : 0.0);
            material.SetVDirection2(thicknessAxis == 0 ? 1.0 : 0.0, thicknessAxis == 1 ? 1.0 : 0.0, thicknessAxis == 2 ? 1.0 : 0.0);
        }

        private string EnsureBetonplexEdgeAppearanceFile(int thicknessAxis, int normalAxis)
        {
            var key = thicknessAxis + ":" + normalAxis;
            string existing;
            if (betonplexEdgeAppearancePaths.TryGetValue(key, out existing)) return existing;
            var sourceTexture = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SolidWorksAssets", "betonplex-birch-edge.png");
            if (!File.Exists(sourceTexture)) throw new FileNotFoundException("Betonplex-kopse-kanttextuur ontbreekt.", sourceTexture);

            var materialFolder = Path.Combine(Path.GetDirectoryName(controlPath) ?? "", "Materialen");
            Directory.CreateDirectory(materialFolder);
            var mappingSuffix = "_T" + thicknessAxis + "_N" + normalAxis;
            var texturePath = Path.Combine(materialFolder, "Betonplex_berken_kopse_kant" + mappingSuffix + ".png");
            var appearancePath = Path.Combine(
                materialFolder,
                "Betonplex_berken_kopse_kant" + mappingSuffix + ".p2m");
            var thicknessRunsOnU = TextureUsesThicknessOnU(thicknessAxis, normalAxis);
            if (thicknessRunsOnU)
            {
                using (var image = Image.FromFile(sourceTexture))
                {
                    image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    image.Save(texturePath, ImageFormat.Png);
                }
            }
            else
            {
                File.Copy(sourceTexture, texturePath, true);
            }

            File.WriteAllText(appearancePath, BuildBetonplexEdgeP2m(texturePath, thicknessRunsOnU), Encoding.ASCII);
            betonplexEdgeAppearancePaths.Add(key, appearancePath);
            return appearancePath;
        }

        private static string BuildBetonplexEdgeP2m(string texturePath, bool thicknessRunsOnU)
        {
            var texture = (texturePath ?? "").Replace('/', '\\');
            var textureHeight = thicknessRunsOnU ? "0.036" : "0.018";
            var textureWidth = thicknessRunsOnU ? "0.018" : "0.036";
            return
                "\"SurfaceFinishShaderType\" 0\r\n" +
                "\"blurryReflections\" off\r\n" +
                "\"bumpIsNormalMap\" off\r\n" +
                "\"bumpStrength\" 0.00015\r\n" +
                "\"bumpTexture\" \"\"\r\n" +
                "\"col1\" 0.82 0.67 0.45\r\n" +
                "color texture \"color_texname\" \"" + texture + "\" \r\n" +
                "\"diffuse_factor\" 1\r\n" +
                "\"displacementDistance\" 0.00015\r\n" +
                "\"doubleSided\" on\r\n" +
                "\"initTextureHeight\" " + textureHeight + "\r\n" +
                "\"initTextureWidth\" " + textureWidth + "\r\n" +
                "\"luminousIntensity\" 0\r\n" +
                "\"mtl_ior\" 1.45\r\n" +
                "\"num_cols\" 0\r\n" +
                "\"reflectivity\" 0.02\r\n" +
                "\"refractionRoughness\" 0\r\n" +
                "\"roughness\" 0.72\r\n" +
                "\"specular_color\" 1 1 1\r\n" +
                "\"specular_factor\" 0.04\r\n" +
                "\"swP2M\" on\r\n" +
                "\"sw_shader\" orientedstrandboard\r\n" +
                "\"transparency\" 0\r\n";
        }

        private RenderMaterial EnsureAluminiumProfileMaterial(int lengthAxis, int normalAxis)
        {
            var key = lengthAxis + ":" + normalAxis;
            RenderMaterial existing;
            if (aluminiumProfileMaterials.TryGetValue(key, out existing)) return existing;

            var appearancePath = EnsureAluminiumProfileAppearanceFile(lengthAxis, normalAxis);
            var material = model.Extension.CreateRenderMaterial(appearancePath);
            if (material == null)
                throw new InvalidOperationException("SolidWorks kon de aluminium-profielappearance niet laden: " + appearancePath);

            ConfigureAluminiumProfileMapping(material, lengthAxis, normalAxis);
            aluminiumProfileMaterials.Add(key, material);
            aluminiumProfileMappingAxes.Add(material, new[] { lengthAxis, normalAxis });
            return material;
        }

        private string EnsureAluminiumProfileAppearanceFile(int lengthAxis, int normalAxis)
        {
            var key = lengthAxis + ":" + normalAxis;
            string existing;
            if (aluminiumProfileAppearancePaths.TryGetValue(key, out existing)) return existing;

            var materialFolder = Path.Combine(Path.GetDirectoryName(controlPath) ?? "", "Materialen");
            Directory.CreateDirectory(materialFolder);
            var texturePath = Path.Combine(materialFolder, "Aluminium_profiel_sleuf.png");
            WriteAluminiumProfileTexture(texturePath);
            var appearancePath = Path.Combine(
                materialFolder,
                "Aluminium_profiel_sleuf_L" + lengthAxis + "_N" + normalAxis + ".p2m");
            File.WriteAllText(appearancePath, BuildAluminiumProfileP2m(texturePath), Encoding.ASCII);
            aluminiumProfileAppearancePaths.Add(key, appearancePath);
            return appearancePath;
        }

        private static void ConfigureAluminiumProfileMapping(RenderMaterial material, int lengthAxis, int normalAxis)
        {
            // Een texturetegel is 40 mm breed. De donkere lijn staat in het midden,
            // zodat een 40-profiel een sleuf en een 80-profiel twee sleuven toont.
            var widthAxis = RemainingAxis(lengthAxis, normalAxis);
            material.LinkToFile = false;
            material.MappingType = 1; // Projection
            material.ProjectionReference = ProjectionReferenceForNormalAxis(normalAxis);
            material.FixedAspectRatio = false;
            material.FitWidth = false;
            material.FitHeight = false;
            material.Width = 0.040;
            material.Height = 0.080;
            material.SetUDirection2(widthAxis == 0 ? 1.0 : 0.0, widthAxis == 1 ? 1.0 : 0.0, widthAxis == 2 ? 1.0 : 0.0);
            material.SetVDirection2(lengthAxis == 0 ? 1.0 : 0.0, lengthAxis == 1 ? 1.0 : 0.0, lengthAxis == 2 ? 1.0 : 0.0);
        }

        private static string BuildAluminiumProfileP2m(string texturePath)
        {
            var texture = (texturePath ?? "").Replace('/', '\\');
            return
                "\"SurfaceFinishShaderType\" 0\r\n" +
                "\"blurryReflections\" on\r\n" +
                "\"bumpIsNormalMap\" off\r\n" +
                "\"bumpStrength\" 0\r\n" +
                "\"bumpTexture\" \"\"\r\n" +
                "\"col1\" 0.50 0.54 0.56\r\n" +
                "color texture \"color_texname\" \"" + texture + "\" \r\n" +
                "\"diffuse_factor\" 1\r\n" +
                "\"doubleSided\" on\r\n" +
                "\"initTextureHeight\" 0.080\r\n" +
                "\"initTextureWidth\" 0.040\r\n" +
                "\"luminousIntensity\" 0\r\n" +
                "\"mtl_ior\" 1.45\r\n" +
                "\"num_cols\" 0\r\n" +
                "\"reflectivity\" 0.24\r\n" +
                "\"roughness\" 0.62\r\n" +
                "\"specular_color\" 1 1 1\r\n" +
                "\"specular_factor\" 0.38\r\n" +
                "\"swP2M\" on\r\n" +
                "\"sw_shader\" mattealuminum\r\n" +
                "\"transparency\" 0\r\n";
        }

        private static void WriteAluminiumProfileTexture(string texturePath)
        {
            const int width = 256;
            const int height = 64;
            using (var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb))
            {
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var distanceMm = Math.Abs((x + 0.5) * 40.0 / width - 20.0);
                        int red;
                        int green;
                        int blue;
                        if (distanceMm < 0.55)
                        {
                            red = 54; green = 59; blue = 62;
                        }
                        else if (distanceMm < 1.35)
                        {
                            red = 83; green = 90; blue = 94;
                        }
                        else if (distanceMm < 2.20)
                        {
                            red = 121; green = 130; blue = 135;
                        }
                        else
                        {
                            // Een heel fijne, langs de extrusierichting lopende variatie.
                            // Geen diagonaal patroon: dat oogt in PowerPoint snel als houtnerf.
                            var grain = ((x * 17 + 3) % 5) - 2;
                            red = 171 + grain; green = 179 + grain; blue = 183 + grain;
                        }

                        bitmap.SetPixel(x, y, Color.FromArgb(red, green, blue));
                    }
                }

                bitmap.Save(texturePath, ImageFormat.Png);
            }
        }

        private static int SmallestAxis(PortalAssemblyPart visual)
        {
            if (visual.SizeXmm <= visual.SizeYmm && visual.SizeXmm <= visual.SizeZmm) return 0;
            if (visual.SizeYmm <= visual.SizeZmm) return 1;
            return 2;
        }

        private static int LongestAxis(PortalAssemblyPart visual)
        {
            if (visual.SizeXmm >= visual.SizeYmm && visual.SizeXmm >= visual.SizeZmm) return 0;
            if (visual.SizeYmm >= visual.SizeZmm) return 1;
            return 2;
        }

        private static int DominantAxis(double[] vector)
        {
            var x = Math.Abs(vector[0]);
            var y = Math.Abs(vector[1]);
            var z = Math.Abs(vector[2]);
            if (x >= y && x >= z) return 0;
            return y >= z ? 1 : 2;
        }

        private static int RemainingAxis(int firstAxis, int secondAxis)
        {
            for (var axis = 0; axis < 3; axis++)
            {
                if (axis != firstAxis && axis != secondAxis) return axis;
            }

            return 0;
        }

        private static int FirstAxisOtherThan(int excludedAxis)
        {
            for (var axis = 0; axis < 3; axis++)
            {
                if (axis != excludedAxis) return axis;
            }

            return 0;
        }

        private static int ProjectionReferenceForNormalAxis(int normalAxis)
        {
            if (normalAxis == 2) return 0; // XY
            if (normalAxis == 1) return 1; // ZX
            return 2;                      // YZ
        }

        private static bool TextureUsesThicknessOnU(int thicknessAxis, int normalAxis)
        {
            // SOLIDWORKS projection plane order: XY, ZX en YZ.
            var projectionUAxis = normalAxis == 2 ? 0 : normalAxis == 1 ? 2 : 1;
            return thicknessAxis == projectionUAxis;
        }

        private static SolidAppearance ResolveSolidAppearance(PortalAssemblyPart visual, string resolvedMaterialName)
        {
            var materialName = (resolvedMaterialName ?? "").ToLowerInvariant();
            var name = (visual.Name ?? "").ToLowerInvariant();
            var kind = (visual.Kind ?? "").ToLowerInvariant();

            if (name.Contains("hoekadapter") || name.Contains("stellfußsockel")) return SolidAppearance.AnodizedAluminium;
            if (materialName.Contains("alu") || materialName.Contains("aluminium") || kind == "profile")
                return SolidAppearance.AnodizedAluminium;
            if (materialName.Contains("hpl")) return SolidAppearance.HplWhite;
            if (materialName.Contains("osb")) return SolidAppearance.Osb;
            if (materialName.Contains("multiplex") || materialName.Contains("berken") || kind == "sheet")
                return SolidAppearance.BirchPlywood;
            if (name.Contains("afdekkap") || name.Contains("stelvoet") || name.Contains("stelpoot") || name.Contains("plunjer") || name.Contains("eindstop"))
                return SolidAppearance.BlackPlastic;
            if (name.Contains("kogelpot")) return SolidAppearance.PolishedStainlessSteel;
            if (name.Contains("hsr15") || name.Contains("rail") || kind.Contains("hardware"))
                return SolidAppearance.SatinSteel;
            return SolidAppearance.Neutral;
        }

        private static string Number(double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static double[] ResolveAppearance(PortalAssemblyPart visual, string resolvedMaterialName)
        {
            var materialName = (resolvedMaterialName ?? "").ToLowerInvariant();
            var name = (visual.Name ?? "").ToLowerInvariant();
            var kind = (visual.Kind ?? "").ToLowerInvariant();

            if (name.Contains("hoekadapter") || name.Contains("stellfußsockel")) return Appearance(0.58, 0.62, 0.64, 0.72, 0.62);
            if (materialName.Contains("betonplex")) return Appearance(0.24, 0.135, 0.065, 0.22, 0.58);
            if (materialName.Contains("hpl")) return Appearance(0.66, 0.66, 0.63, 0.10, 0.50);
            if (materialName.Contains("osb")) return Appearance(0.64, 0.43, 0.23, 0.08, 0.28);
            if (materialName.Contains("multiplex") || materialName.Contains("berken")) return Appearance(0.78, 0.62, 0.39, 0.08, 0.34);
            if (materialName.Contains("alu") || materialName.Contains("aluminium") || kind == "profile") return Appearance(0.48, 0.52, 0.54, 0.62, 0.58);
            if (name.Contains("kogelpot")) return Appearance(0.18, 0.22, 0.26, 1.00, 0.96);
            if (name.Contains("hsr15") || name.Contains("rail")) return Appearance(0.50, 0.54, 0.57, 0.86, 0.78);
            if (name.Contains("afdekkap") || name.Contains("stelvoet") || name.Contains("stelpoot") || name.Contains("plunjer") || name.Contains("eindstop")) return Appearance(0.055, 0.065, 0.075, 0.24, 0.42);
            if (name.Contains("adapter") || kind.Contains("hardware")) return Appearance(0.18, 0.20, 0.22, 0.58, 0.58);
            if (kind == "sheet") return Appearance(0.76, 0.64, 0.47, 0.08, 0.34);
            return Appearance(0.48, 0.52, 0.55, 0.46, 0.52);
        }

        private static string FindMaterialName(PortalAssemblyPart visual, WorkbenchModel model)
        {
            if (visual == null || model == null) return "";
            foreach (var sheet in model.Sheets)
            {
                if (sheet == null || sheet.Material == null) continue;
                if (NameMatches(visual.Name, sheet.Name)) return sheet.Material.Name ?? "";
            }

            foreach (var profile in model.Profiles)
            {
                if (profile == null || profile.Material == null) continue;
                if (NameMatches(visual.Name, profile.Name)) return profile.Material.Name ?? "";
            }

            return "";
        }

        private static bool NameMatches(string visualName, string partName)
        {
            if (string.IsNullOrWhiteSpace(visualName) || string.IsNullOrWhiteSpace(partName)) return false;
            return string.Equals(visualName, partName, StringComparison.OrdinalIgnoreCase)
                || visualName.StartsWith(partName + " ", StringComparison.OrdinalIgnoreCase);
        }

        private static double[] Appearance(double red, double green, double blue, double specular, double shininess)
        {
            // SOLIDWORKS: R, G, B, ambient, diffuse, specular, shininess,
            // transparency en emission. Alle waarden liggen tussen 0 en 1.
            return new[] { red, green, blue, 0.22, 0.78, specular, shininess, 0.0, 0.0 };
        }

        private sealed class SolidAppearance
        {
            public static readonly SolidAppearance BetonplexFilm = new SolidAppearance(
                "Betonplex_donkere_fenolfilm", "defaultplastic", 0.18, 0.09, 0.04, 0.08, 0.58, 0.12, false);
            public static readonly SolidAppearance AnodizedAluminium = new SolidAppearance(
                "Aluminium_geanodiseerd_mat", "mattealuminum", 0.43, 0.47, 0.50, 0.24, 0.68, 0.36, true);
            public static readonly SolidAppearance HplWhite = new SolidAppearance(
                "HPL_wit_mat", "defaultplastic", 0.66, 0.66, 0.63, 0.05, 0.68, 0.12, false);
            public static readonly SolidAppearance SatinSteel = new SolidAppearance(
                "Staal_satijn", "brushedsteel", 0.42, 0.45, 0.48, 0.42, 0.52, 0.58, true);
            public static readonly SolidAppearance PolishedStainlessSteel = new SolidAppearance(
                "RVS_gepolijst", "polishedsteel", 0.18, 0.22, 0.26, 0.94, 0.08, 0.98, false);
            public static readonly SolidAppearance BlackPlastic = new SolidAppearance(
                "Kunststof_zwart", "defaultplastic", 0.045, 0.055, 0.065, 0.04, 0.66, 0.14, false);
            public static readonly SolidAppearance BirchPlywood = new SolidAppearance(
                "Berken_multiplex", "defaultplastic", 0.74, 0.59, 0.38, 0.03, 0.72, 0.08, false);
            public static readonly SolidAppearance Osb = new SolidAppearance(
                "OSB_plaat", "defaultplastic", 0.59, 0.40, 0.22, 0.02, 0.78, 0.06, false);
            public static readonly SolidAppearance Neutral = new SolidAppearance(
                "Materiaal_neutraal", "defaultplastic", 0.44, 0.48, 0.51, 0.12, 0.62, 0.22, false);

            private SolidAppearance(
                string key,
                string shader,
                double red,
                double green,
                double blue,
                double reflectivity,
                double roughness,
                double specular,
                bool blurryReflections)
            {
                Key = key;
                Shader = shader;
                Red = red;
                Green = green;
                Blue = blue;
                Reflectivity = reflectivity;
                Roughness = roughness;
                Specular = specular;
                BlurryReflections = blurryReflections;
            }

            public string Key { get; private set; }
            public string Shader { get; private set; }
            public double Red { get; private set; }
            public double Green { get; private set; }
            public double Blue { get; private set; }
            public double Reflectivity { get; private set; }
            public double Roughness { get; private set; }
            public double Specular { get; private set; }
            public bool BlurryReflections { get; private set; }
        }
    }
}
