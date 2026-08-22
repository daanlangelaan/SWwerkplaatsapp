using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class CamMasterSettings
    {
        public const string ThroughCutOvertravelKey = "cam_through_cut_overtravel_mm";
        public const string TabWidthKey = "cam_tab_width_mm";
        public const string TabHeightKey = "cam_tab_height_mm";
        public const string SafeTravelZKey = "cam_safe_travel_z_mm";
        public const string ContourOnionSkinKey = "cam_contour_onion_skin_mm";
        public const string FinalContourFeedRateKey = "cam_final_contour_feed_mm_min";
        public const string FinalContourRampLengthKey = "cam_final_contour_ramp_length_mm";

        public double ThroughCutOvertravelMm { get; private set; }
        public double TabWidthMm { get; private set; }
        public double TabHeightMm { get; private set; }
        public double SafeTravelZMm { get; private set; }
        public double ContourOnionSkinMm { get; private set; }
        public double FinalContourFeedRateMmMin { get; private set; }
        public double FinalContourRampLengthMm { get; private set; }
        public string SourcePath { get; private set; }

        public static CamMasterSettings LoadRequired()
        {
            var path = FindWorkbook();
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("CAM-master ontbreekt: config/product-master-data.xlsx is niet gevonden.");

            var values = ReadParameters(path);
            return new CamMasterSettings
            {
                ThroughCutOvertravelMm = RequiredValue(values, ThroughCutOvertravelKey, 0, 5),
                TabWidthMm = RequiredValue(values, TabWidthKey, 0.1, 100),
                TabHeightMm = RequiredValue(values, TabHeightKey, 0.1, 20),
                SafeTravelZMm = RequiredValue(values, SafeTravelZKey, 1, 100),
                ContourOnionSkinMm = RequiredValue(values, ContourOnionSkinKey, 0.1, 5),
                FinalContourFeedRateMmMin = RequiredValue(values, FinalContourFeedRateKey, 100, 10000),
                FinalContourRampLengthMm = RequiredValue(values, FinalContourRampLengthKey, 1, 500),
                SourcePath = path
            };
        }

        public void ApplyTo(Manufacturing.CamJobOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");
            options.ThroughCutOvertravelMm = ThroughCutOvertravelMm;
            options.TabWidthMm = TabWidthMm;
            options.TabHeightMm = TabHeightMm;
            options.SafeTravelZMm = SafeTravelZMm;
            options.ContourOnionSkinMm = ContourOnionSkinMm;
            options.FinalContourFeedRateMmMin = FinalContourFeedRateMmMin;
            options.FinalContourRampLengthMm = FinalContourRampLengthMm;
        }

        private static double RequiredValue(Dictionary<string, double> values, string key, double minimum, double maximum)
        {
            double value;
            if (!values.TryGetValue(key, out value))
                throw new InvalidOperationException("CAM-masterparameter ontbreekt of is niet numeriek: " + key + ".");
            if (value < minimum || value > maximum)
                throw new InvalidOperationException("CAM-masterparameter " + key + " valt buiten het veilige bereik " + minimum.ToString(CultureInfo.InvariantCulture) + ".." + maximum.ToString(CultureInfo.InvariantCulture) + " mm.");
            return value;
        }

        private static string FindWorkbook()
        {
            var fromBase = FindUpwards(AppDomain.CurrentDomain.BaseDirectory);
            return fromBase ?? FindUpwards(Environment.CurrentDirectory);
        }

        private static string FindUpwards(string startFolder)
        {
            if (string.IsNullOrWhiteSpace(startFolder)) return null;
            var folder = Path.GetFullPath(startFolder);
            for (var i = 0; i < 8 && !string.IsNullOrEmpty(folder); i++)
            {
                var candidate = Path.Combine(folder, "config", "product-master-data.xlsx");
                if (File.Exists(candidate)) return candidate;
                var parent = Directory.GetParent(folder);
                if (parent == null) break;
                folder = parent.FullName;
            }
            return null;
        }

        private static Dictionary<string, double> ReadParameters(string path)
        {
            try
            {
                using (var archive = ZipFile.OpenRead(path))
                {
                    var worksheet = FindWorksheet(archive, "CAM-parameters");
                    if (worksheet == null)
                        throw new InvalidOperationException("Werkblad CAM-parameters ontbreekt in " + path + ".");
                    var rows = ReadWorksheetRows(worksheet, ReadSharedStrings(archive));
                    Dictionary<string, int> headers = null;
                    var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                    foreach (var row in rows)
                    {
                        if (headers == null)
                        {
                            headers = HeaderMap(row);
                            if (!headers.ContainsKey("Sleutel") || !headers.ContainsKey("Waarde")) headers = null;
                            continue;
                        }

                        var key = Cell(row, headers, "Sleutel");
                        var text = Cell(row, headers, "Waarde");
                        double value;
                        if (!string.IsNullOrWhiteSpace(key) && TryDouble(text, out value)) result[key.Trim()] = value;
                    }
                    return result;
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("CAM-master kon niet worden gelezen uit " + path + ": " + ex.Message, ex);
            }
        }

        private static XmlDocument FindWorksheet(ZipArchive archive, string sheetName)
        {
            var workbook = LoadXml(archive, "xl/workbook.xml");
            var relationships = LoadXml(archive, "xl/_rels/workbook.xml.rels");
            if (workbook == null || relationships == null) return null;
            const string spreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            const string officeRelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            var manager = new XmlNamespaceManager(workbook.NameTable);
            manager.AddNamespace("m", spreadsheetNs);
            var sheet = workbook.SelectSingleNode("//m:sheet[@name='" + sheetName + "']", manager) as XmlElement;
            if (sheet == null) return null;
            var relationshipId = sheet.GetAttribute("id", officeRelNs);
            var relManager = new XmlNamespaceManager(relationships.NameTable);
            relManager.AddNamespace("r", "http://schemas.openxmlformats.org/package/2006/relationships");
            var relationship = relationships.SelectSingleNode("//r:Relationship[@Id='" + relationshipId + "']", relManager) as XmlElement;
            if (relationship == null) return null;
            var target = relationship.GetAttribute("Target").Replace('\\', '/').TrimStart('/');
            if (!target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)) target = "xl/" + target;
            return LoadXml(archive, target);
        }

        private static XmlDocument LoadXml(ZipArchive archive, string path)
        {
            var entry = archive.GetEntry(path);
            if (entry == null) return null;
            var document = new XmlDocument();
            using (var stream = entry.Open()) document.Load(stream);
            return document;
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            var result = new List<string>();
            var document = LoadXml(archive, "xl/sharedStrings.xml");
            if (document == null) return result;
            var manager = new XmlNamespaceManager(document.NameTable);
            manager.AddNamespace("m", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            foreach (XmlNode item in document.SelectNodes("//m:si", manager))
            {
                var text = string.Empty;
                foreach (XmlNode node in item.SelectNodes(".//m:t", manager)) text += node.InnerText;
                result.Add(text);
            }
            return result;
        }

        private static List<Dictionary<int, string>> ReadWorksheetRows(XmlDocument worksheet, List<string> sharedStrings)
        {
            var result = new List<Dictionary<int, string>>();
            var manager = new XmlNamespaceManager(worksheet.NameTable);
            manager.AddNamespace("m", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            foreach (XmlNode rowNode in worksheet.SelectNodes("//m:sheetData/m:row", manager))
            {
                var row = new Dictionary<int, string>();
                foreach (XmlElement cell in rowNode.SelectNodes("m:c", manager))
                {
                    var column = ColumnIndex(cell.GetAttribute("r"));
                    var type = cell.GetAttribute("t");
                    string value;
                    if (type == "inlineStr")
                    {
                        var textNode = cell.SelectSingleNode("m:is/m:t", manager);
                        value = textNode == null ? string.Empty : textNode.InnerText;
                    }
                    else
                    {
                        var valueNode = cell.SelectSingleNode("m:v", manager);
                        value = valueNode == null ? string.Empty : valueNode.InnerText;
                        int sharedIndex;
                        if (type == "s" && int.TryParse(value, out sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count) value = sharedStrings[sharedIndex];
                    }
                    row[column] = value;
                }
                result.Add(row);
            }
            return result;
        }

        private static int ColumnIndex(string reference)
        {
            var value = 0;
            foreach (var character in reference)
            {
                if (!char.IsLetter(character)) break;
                value = value * 26 + (char.ToUpperInvariant(character) - 'A' + 1);
            }
            return Math.Max(0, value - 1);
        }

        private static Dictionary<string, int> HeaderMap(Dictionary<int, string> row)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in row)
                if (!string.IsNullOrWhiteSpace(item.Value)) result[item.Value.Trim()] = item.Key;
            return result;
        }

        private static string Cell(Dictionary<int, string> row, Dictionary<string, int> headers, string name)
        {
            int column;
            string value;
            return headers.TryGetValue(name, out column) && row.TryGetValue(column, out value) ? value : string.Empty;
        }

        private static bool TryDouble(string value, out double result)
        {
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
                || double.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("nl-NL"), out result);
        }
    }
}
