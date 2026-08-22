using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace SWWerkplaats.Configurator.SolidWorks
{
    /// <summary>
    /// SOLIDWORKS schaalt texture-UV's bij GLB-export opnieuw naar de totale modelmaat.
    /// Deze kleine naverwerkingsstap herstelt de betonplex-kopse-kanten en de
    /// oppervlaktestrepen van aluminium systeemprofielen naar een fysieke mapping.
    /// </summary>
    internal static class SolidWorksGlbTextureMapper
    {
        private const int GlbHeaderLength = 12;
        private const int ChunkHeaderLength = 8;
        private const uint JsonChunkType = 0x4E4F534A;
        private const uint BinChunkType = 0x004E4942;
        private const int FloatComponentType = 5126;
        private const int UnsignedByteComponentType = 5121;
        private const int UnsignedShortComponentType = 5123;
        private const int UnsignedIntComponentType = 5125;
        private const double TextureWidthMeters = 0.036;
        private const double TextureHeightMeters = 0.018;
        private static readonly Regex BetonplexAxes = new Regex(
            @"Betonplex_berken_kopse_kant_T([0-2])_N([0-2])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly Regex AluminiumProfileAxes = new Regex(
            @"Aluminium_profiel_sleuf_L([0-2])_N([0-2])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static void ApplyBetonplexEdgeMapping(string glbPath)
        {
            if (string.IsNullOrWhiteSpace(glbPath)) throw new ArgumentException("GLB-pad ontbreekt.", "glbPath");
            var bytes = File.ReadAllBytes(glbPath);
            if (bytes.Length < GlbHeaderLength + ChunkHeaderLength) throw new InvalidDataException("Het GLB-klantmodel is onvolledig.");
            if (Encoding.ASCII.GetString(bytes, 0, 4) != "glTF") throw new InvalidDataException("Het klantmodel is geen geldig GLB-bestand.");

            var jsonLength = ReadInt32(bytes, GlbHeaderLength);
            var jsonType = ReadUInt32(bytes, GlbHeaderLength + 4);
            var jsonStart = GlbHeaderLength + ChunkHeaderLength;
            if (jsonType != JsonChunkType || jsonLength <= 0 || jsonStart + jsonLength > bytes.Length)
                throw new InvalidDataException("De JSON-chunk van het GLB-klantmodel is ongeldig.");

            var binHeader = jsonStart + jsonLength;
            if (binHeader + ChunkHeaderLength > bytes.Length || ReadUInt32(bytes, binHeader + 4) != BinChunkType)
                throw new InvalidDataException("De binaire chunk van het GLB-klantmodel ontbreekt.");
            var binLength = ReadInt32(bytes, binHeader);
            var binStart = binHeader + ChunkHeaderLength;
            if (binLength < 0 || binStart + binLength > bytes.Length)
                throw new InvalidDataException("De binaire chunk van het GLB-klantmodel is ongeldig.");

            var json = Encoding.UTF8.GetString(bytes, jsonStart, jsonLength).TrimEnd(' ', '\0');
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var root = AsDictionary(serializer.DeserializeObject(json), "GLB-root");
            var materials = AsArray(root["materials"], "materials");
            var meshes = AsArray(root["meshes"], "meshes");
            var accessors = AsArray(root["accessors"], "accessors");
            var bufferViews = AsArray(root["bufferViews"], "bufferViews");
            var changed = false;

            foreach (var materialValue in materials)
            {
                var material = AsDictionary(materialValue, "material");
                object nameValue;
                if (!material.TryGetValue("name", out nameValue)) continue;
                changed |= NormalizePbrMaterial(material, Convert.ToString(nameValue) ?? "");
            }

            foreach (var meshValue in meshes)
            {
                var mesh = AsDictionary(meshValue, "mesh");
                object primitivesValue;
                if (!mesh.TryGetValue("primitives", out primitivesValue)) continue;
                foreach (var primitiveValue in AsArray(primitivesValue, "primitives"))
                {
                    var primitive = AsDictionary(primitiveValue, "primitive");
                    object materialValue;
                    object attributesValue;
                    if (!primitive.TryGetValue("material", out materialValue) || !primitive.TryGetValue("attributes", out attributesValue)) continue;
                    var materialIndex = Convert.ToInt32(materialValue);
                    if (materialIndex < 0 || materialIndex >= materials.Length) continue;
                    var material = AsDictionary(materials[materialIndex], "material");
                    object nameValue;
                    if (!material.TryGetValue("name", out nameValue)) continue;
                    var materialName = Convert.ToString(nameValue) ?? "";
                    var betonplexMatch = BetonplexAxes.Match(materialName);
                    var aluminiumMatch = AluminiumProfileAxes.Match(materialName);
                    if (!betonplexMatch.Success && !aluminiumMatch.Success) continue;

                    var attributes = AsDictionary(attributesValue, "attributes");
                    object positionValue;
                    object texcoordValue;
                    if (!attributes.TryGetValue("POSITION", out positionValue) || !attributes.TryGetValue("TEXCOORD_0", out texcoordValue))
                        throw new InvalidDataException("Een betonplex-rand in het GLB-model mist positie- of UV-data.");

                    var positions = ReadAccessor(accessors, bufferViews, Convert.ToInt32(positionValue), 3, "VEC3", binStart, binLength);
                    var texcoords = ReadAccessor(accessors, bufferViews, Convert.ToInt32(texcoordValue), 2, "VEC2", binStart, binLength);
                    if (positions.Count != texcoords.Count)
                        throw new InvalidDataException("De positie- en UV-aantallen van een betonplex-rand verschillen.");

                    var minimumU = float.PositiveInfinity;
                    var minimumV = float.PositiveInfinity;
                    var maximumU = float.NegativeInfinity;
                    var maximumV = float.NegativeInfinity;
                    if (aluminiumMatch.Success)
                    {
                        RewriteAluminiumProfileUvs(
                            bytes,
                            primitive,
                            accessors,
                            bufferViews,
                            positions,
                            texcoords,
                            binStart,
                            binLength);
                    }
                    else
                    {
                        var firstAxis = int.Parse(betonplexMatch.Groups[1].Value);
                        var normalAxis = int.Parse(betonplexMatch.Groups[2].Value);
                        var remainingAxis = RemainingAxis(firstAxis, normalAxis);
                        for (var index = 0; index < positions.Count; index++)
                        {
                            var positionOffset = positions.Offset + index * positions.Stride;
                            var texcoordOffset = texcoords.Offset + index * texcoords.Stride;
                            var firstPosition = ReadSingle(bytes, positionOffset + firstAxis * 4);
                            var remainingPosition = ReadSingle(bytes, positionOffset + remainingAxis * 4);
                            var thicknessRunsOnU = TextureUsesThicknessOnU(firstAxis, normalAxis);
                            var u = thicknessRunsOnU
                                ? (float)(firstPosition / TextureHeightMeters)
                                : (float)(remainingPosition / TextureWidthMeters);
                            var v = thicknessRunsOnU
                                ? (float)(remainingPosition / TextureWidthMeters)
                                : (float)(firstPosition / TextureHeightMeters);
                            WriteSingle(bytes, texcoordOffset, u);
                            WriteSingle(bytes, texcoordOffset + 4, v);
                        }
                    }

                    for (var index = 0; index < texcoords.Count; index++)
                    {
                        var texcoordOffset = texcoords.Offset + index * texcoords.Stride;
                        var u = ReadSingle(bytes, texcoordOffset);
                        var v = ReadSingle(bytes, texcoordOffset + 4);
                        minimumU = Math.Min(minimumU, u);
                        minimumV = Math.Min(minimumV, v);
                        maximumU = Math.Max(maximumU, u);
                        maximumV = Math.Max(maximumV, v);
                    }

                    var texcoordAccessor = AsDictionary(accessors[Convert.ToInt32(texcoordValue)], "texcoord accessor");
                    texcoordAccessor["min"] = new object[] { minimumU, minimumV };
                    texcoordAccessor["max"] = new object[] { maximumU, maximumV };

                    changed = true;
                }
            }

            if (changed) WriteGlb(glbPath, bytes, serializer.Serialize(root), binStart, binLength);
        }

        private static void RewriteAluminiumProfileUvs(
            byte[] bytes,
            Dictionary<string, object> primitive,
            object[] accessors,
            object[] bufferViews,
            AccessorLayout positions,
            AccessorLayout texcoords,
            int binStart,
            int binLength)
        {
            object indicesValue;
            if (!primitive.TryGetValue("indices", out indicesValue))
                throw new InvalidDataException("Een aluminium profielvlak in het GLB-model mist indexdata.");

            var indices = ReadIndexAccessor(
                accessors,
                bufferViews,
                Convert.ToInt32(indicesValue),
                binStart,
                binLength);
            if (indices.Count % 3 != 0)
                throw new InvalidDataException("De indexdata van een aluminium profiel bevat geen volledige driehoeken.");

            // SOLIDWORKS voegt profielvlakken met verschillende lengterichtingen in
            // een GLB samen onder één materiaal. De materiaalnaam bevat daardoor maar
            // één asrichting. Bepaal de lengte- en breedte-as opnieuw per vlakdriehoek:
            // de grootste coördinaatspanne is de extrusierichting en de kleinste de
            // vlaknormaal. Zo blijven sleuflijnen langslopend op X-, Y- én Z-profielen.
            for (var triangle = 0; triangle < indices.Count; triangle += 3)
            {
                var vertexIndices = new int[3];
                var minimum = new[] { float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity };
                var maximum = new[] { float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity };
                for (var corner = 0; corner < 3; corner++)
                {
                    var vertexIndex = ReadIndex(bytes, indices, triangle + corner);
                    if (vertexIndex < 0 || vertexIndex >= positions.Count)
                        throw new InvalidDataException("Een aluminium profielindex valt buiten de positiedata.");
                    vertexIndices[corner] = vertexIndex;
                    var positionOffset = positions.Offset + vertexIndex * positions.Stride;
                    for (var axis = 0; axis < 3; axis++)
                    {
                        var coordinate = ReadSingle(bytes, positionOffset + axis * 4);
                        minimum[axis] = Math.Min(minimum[axis], coordinate);
                        maximum[axis] = Math.Max(maximum[axis], coordinate);
                    }
                }

                var spans = new[]
                {
                    maximum[0] - minimum[0],
                    maximum[1] - minimum[1],
                    maximum[2] - minimum[2]
                };
                var normalAxis = SmallestAxis(spans);
                var lengthAxis = LongestAxis(spans, normalAxis);
                var widthAxis = RemainingAxis(lengthAxis, normalAxis);

                for (var corner = 0; corner < 3; corner++)
                {
                    var vertexIndex = vertexIndices[corner];
                    var positionOffset = positions.Offset + vertexIndex * positions.Stride;
                    var texcoordOffset = texcoords.Offset + vertexIndex * texcoords.Stride;
                    var widthPosition = ReadSingle(bytes, positionOffset + widthAxis * 4);
                    var lengthPosition = ReadSingle(bytes, positionOffset + lengthAxis * 4);
                    WriteSingle(bytes, texcoordOffset, (float)(widthPosition / 0.040));
                    WriteSingle(bytes, texcoordOffset + 4, (float)(lengthPosition / 0.080));
                }
            }
        }

        private static bool NormalizePbrMaterial(Dictionary<string, object> material, string name)
        {
            double[] baseColor;
            double metallic;
            double roughness;
            if (name.StartsWith("Betonplex_berken_kopse_kant", StringComparison.OrdinalIgnoreCase))
            {
                baseColor = new[] { 0.82, 0.67, 0.45, 1.0 };
                metallic = 0.0;
                roughness = 0.78;
            }
            else if (name.StartsWith("Aluminium_profiel_sleuf", StringComparison.OrdinalIgnoreCase))
            {
                baseColor = new[] { 0.62, 0.65, 0.68, 1.0 };
                metallic = 0.62;
                roughness = 0.68;
            }
            else if (name.Equals("Betonplex_donkere_fenolfilm", StringComparison.OrdinalIgnoreCase))
            {
                baseColor = new[] { 0.18, 0.09, 0.04, 1.0 };
                metallic = 0.0;
                roughness = 0.58;
            }
            else if (name.Equals("Aluminium_geanodiseerd_mat", StringComparison.OrdinalIgnoreCase))
            {
                baseColor = new[] { 0.43, 0.47, 0.50, 1.0 };
                metallic = 0.62;
                roughness = 0.68;
            }
            else if (name.Equals("HPL_wit_mat", StringComparison.OrdinalIgnoreCase))
            {
                baseColor = new[] { 0.72, 0.72, 0.69, 1.0 };
                metallic = 0.0;
                roughness = 0.68;
            }
            else if (name.Equals("Staal_satijn", StringComparison.OrdinalIgnoreCase))
            {
                baseColor = new[] { 0.42, 0.45, 0.48, 1.0 };
                metallic = 0.78;
                roughness = 0.52;
            }
            else if (name.Equals("RVS_gepolijst", StringComparison.OrdinalIgnoreCase))
            {
                baseColor = new[] { 0.16, 0.20, 0.24, 1.0 };
                metallic = 1.0;
                roughness = 0.08;
            }
            else if (name.Equals("Kunststof_zwart", StringComparison.OrdinalIgnoreCase))
            {
                baseColor = new[] { 0.045, 0.055, 0.065, 1.0 };
                metallic = 0.0;
                roughness = 0.66;
            }
            else if (name.Equals("Berken_multiplex", StringComparison.OrdinalIgnoreCase))
            {
                baseColor = new[] { 0.74, 0.59, 0.38, 1.0 };
                metallic = 0.0;
                roughness = 0.72;
            }
            else if (name.Equals("OSB_plaat", StringComparison.OrdinalIgnoreCase))
            {
                baseColor = new[] { 0.59, 0.40, 0.22, 1.0 };
                metallic = 0.0;
                roughness = 0.78;
            }
            else if (name.Equals("Materiaal_neutraal", StringComparison.OrdinalIgnoreCase))
            {
                baseColor = new[] { 0.44, 0.48, 0.51, 1.0 };
                metallic = 0.15;
                roughness = 0.62;
            }
            else
            {
                return false;
            }

            object pbrValue;
            Dictionary<string, object> pbr;
            if (material.TryGetValue("pbrMetallicRoughness", out pbrValue))
            {
                pbr = AsDictionary(pbrValue, "pbrMetallicRoughness");
            }
            else
            {
                pbr = new Dictionary<string, object>();
                material["pbrMetallicRoughness"] = pbr;
            }

            pbr["baseColorFactor"] = new object[] { baseColor[0], baseColor[1], baseColor[2], baseColor[3] };
            pbr["metallicFactor"] = metallic;
            pbr["roughnessFactor"] = roughness;
            return true;
        }

        private static void WriteGlb(string glbPath, byte[] original, string json, int binStart, int binLength)
        {
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var paddedJsonLength = (jsonBytes.Length + 3) & ~3;
            var paddedBinLength = (binLength + 3) & ~3;
            var totalLength = GlbHeaderLength + ChunkHeaderLength + paddedJsonLength + ChunkHeaderLength + paddedBinLength;
            var output = new byte[totalLength];
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("glTF"), 0, output, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(ReadInt32(original, 4)), 0, output, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(totalLength), 0, output, 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(paddedJsonLength), 0, output, 12, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(JsonChunkType), 0, output, 16, 4);
            Buffer.BlockCopy(jsonBytes, 0, output, 20, jsonBytes.Length);
            for (var index = 20 + jsonBytes.Length; index < 20 + paddedJsonLength; index++) output[index] = 0x20;
            var binHeader = 20 + paddedJsonLength;
            Buffer.BlockCopy(BitConverter.GetBytes(paddedBinLength), 0, output, binHeader, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(BinChunkType), 0, output, binHeader + 4, 4);
            Buffer.BlockCopy(original, binStart, output, binHeader + 8, binLength);
            File.WriteAllBytes(glbPath, output);
        }

        private static AccessorLayout ReadAccessor(
            object[] accessors,
            object[] bufferViews,
            int accessorIndex,
            int componentCount,
            string type,
            int binStart,
            int binLength)
        {
            if (accessorIndex < 0 || accessorIndex >= accessors.Length) throw new InvalidDataException("GLB-accessorindex valt buiten bereik.");
            var accessor = AsDictionary(accessors[accessorIndex], "accessor");
            if (Convert.ToInt32(accessor["componentType"]) != FloatComponentType || Convert.ToString(accessor["type"]) != type)
                throw new InvalidDataException("Betonplex-randdata gebruikt een onverwacht GLB-formaat.");
            var bufferViewIndex = Convert.ToInt32(accessor["bufferView"]);
            if (bufferViewIndex < 0 || bufferViewIndex >= bufferViews.Length) throw new InvalidDataException("GLB-bufferView-index valt buiten bereik.");
            var bufferView = AsDictionary(bufferViews[bufferViewIndex], "bufferView");
            if (Convert.ToInt32(bufferView["buffer"]) != 0) throw new InvalidDataException("Alleen de ingebedde GLB-buffer wordt ondersteund.");
            var count = Convert.ToInt32(accessor["count"]);
            var accessorOffset = OptionalInt(accessor, "byteOffset");
            var viewOffset = OptionalInt(bufferView, "byteOffset");
            var stride = OptionalInt(bufferView, "byteStride");
            if (stride == 0) stride = componentCount * 4;
            var offset = binStart + viewOffset + accessorOffset;
            var finalByte = offset + Math.Max(0, count - 1) * stride + componentCount * 4;
            if (count < 0 || stride < componentCount * 4 || offset < binStart || finalByte > binStart + binLength)
                throw new InvalidDataException("Betonplex-randdata valt buiten de GLB-buffer.");
            return new AccessorLayout(offset, stride, count);
        }

        private static IndexAccessorLayout ReadIndexAccessor(
            object[] accessors,
            object[] bufferViews,
            int accessorIndex,
            int binStart,
            int binLength)
        {
            if (accessorIndex < 0 || accessorIndex >= accessors.Length) throw new InvalidDataException("GLB-indexaccessor valt buiten bereik.");
            var accessor = AsDictionary(accessors[accessorIndex], "index accessor");
            if (Convert.ToString(accessor["type"]) != "SCALAR")
                throw new InvalidDataException("Aluminium profielindices gebruiken een onverwacht GLB-formaat.");
            var componentType = Convert.ToInt32(accessor["componentType"]);
            var componentSize = componentType == UnsignedByteComponentType
                ? 1
                : componentType == UnsignedShortComponentType
                    ? 2
                    : componentType == UnsignedIntComponentType ? 4 : 0;
            if (componentSize == 0)
                throw new InvalidDataException("Aluminium profielindices gebruiken een niet-ondersteund componenttype.");

            var bufferViewIndex = Convert.ToInt32(accessor["bufferView"]);
            if (bufferViewIndex < 0 || bufferViewIndex >= bufferViews.Length) throw new InvalidDataException("GLB-index-bufferView valt buiten bereik.");
            var bufferView = AsDictionary(bufferViews[bufferViewIndex], "index bufferView");
            if (Convert.ToInt32(bufferView["buffer"]) != 0) throw new InvalidDataException("Alleen de ingebedde GLB-indexbuffer wordt ondersteund.");
            var count = Convert.ToInt32(accessor["count"]);
            var accessorOffset = OptionalInt(accessor, "byteOffset");
            var viewOffset = OptionalInt(bufferView, "byteOffset");
            var stride = OptionalInt(bufferView, "byteStride");
            if (stride == 0) stride = componentSize;
            var offset = binStart + viewOffset + accessorOffset;
            var finalByte = offset + Math.Max(0, count - 1) * stride + componentSize;
            if (count < 0 || stride < componentSize || offset < binStart || finalByte > binStart + binLength)
                throw new InvalidDataException("Aluminium profielindices vallen buiten de GLB-buffer.");
            return new IndexAccessorLayout(offset, stride, count, componentType);
        }

        private static int ReadIndex(byte[] bytes, IndexAccessorLayout indices, int index)
        {
            var offset = indices.Offset + index * indices.Stride;
            if (indices.ComponentType == UnsignedByteComponentType) return bytes[offset];
            if (indices.ComponentType == UnsignedShortComponentType) return BitConverter.ToUInt16(bytes, offset);
            return checked((int)BitConverter.ToUInt32(bytes, offset));
        }

        private static Dictionary<string, object> AsDictionary(object value, string name)
        {
            var result = value as Dictionary<string, object>;
            if (result == null) throw new InvalidDataException("GLB-onderdeel '" + name + "' heeft een onverwacht formaat.");
            return result;
        }

        private static object[] AsArray(object value, string name)
        {
            var result = value as object[];
            if (result == null) throw new InvalidDataException("GLB-onderdeel '" + name + "' heeft een onverwacht formaat.");
            return result;
        }

        private static int OptionalInt(Dictionary<string, object> values, string name)
        {
            object value;
            return values.TryGetValue(name, out value) ? Convert.ToInt32(value) : 0;
        }

        private static int RemainingAxis(int firstAxis, int secondAxis)
        {
            for (var axis = 0; axis < 3; axis++)
            {
                if (axis != firstAxis && axis != secondAxis) return axis;
            }

            throw new InvalidDataException("De betonplex-mapping bevat geen geldige randas.");
        }

        private static int SmallestAxis(float[] spans)
        {
            if (spans[0] <= spans[1] && spans[0] <= spans[2]) return 0;
            return spans[1] <= spans[2] ? 1 : 2;
        }

        private static int LongestAxis(float[] spans, int excludedAxis)
        {
            var result = -1;
            for (var axis = 0; axis < 3; axis++)
            {
                if (axis == excludedAxis) continue;
                if (result < 0 || spans[axis] > spans[result]) result = axis;
            }
            return result;
        }

        private static bool TextureUsesThicknessOnU(int thicknessAxis, int normalAxis)
        {
            var projectionUAxis = normalAxis == 2 ? 0 : normalAxis == 1 ? 2 : 1;
            return thicknessAxis == projectionUAxis;
        }

        private static int ReadInt32(byte[] bytes, int offset)
        {
            return BitConverter.ToInt32(bytes, offset);
        }

        private static uint ReadUInt32(byte[] bytes, int offset)
        {
            return BitConverter.ToUInt32(bytes, offset);
        }

        private static float ReadSingle(byte[] bytes, int offset)
        {
            return BitConverter.ToSingle(bytes, offset);
        }

        private static void WriteSingle(byte[] bytes, int offset, float value)
        {
            var source = BitConverter.GetBytes(value);
            Buffer.BlockCopy(source, 0, bytes, offset, source.Length);
        }

        private sealed class AccessorLayout
        {
            public AccessorLayout(int offset, int stride, int count)
            {
                Offset = offset;
                Stride = stride;
                Count = count;
            }

            public int Offset { get; private set; }
            public int Stride { get; private set; }
            public int Count { get; private set; }
        }

        private sealed class IndexAccessorLayout
        {
            public IndexAccessorLayout(int offset, int stride, int count, int componentType)
            {
                Offset = offset;
                Stride = stride;
                Count = count;
                ComponentType = componentType;
            }

            public int Offset { get; private set; }
            public int Stride { get; private set; }
            public int Count { get; private set; }
            public int ComponentType { get; private set; }
        }
    }
}
