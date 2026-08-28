using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class MasterDataRuntimeCatalog
    {
        private readonly Dictionary<string, List<Dictionary<string, string>>> tables;

        private MasterDataRuntimeCatalog(string sourcePath, Dictionary<string, List<Dictionary<string, string>>> tables)
        {
            SourcePath = sourcePath;
            this.tables = tables;
        }

        public string SourcePath { get; private set; }

        public static MasterDataRuntimeCatalog LoadRequired()
        {
            var path = FindUpwards(AppDomain.CurrentDomain.BaseDirectory)
                ?? FindUpwards(Environment.CurrentDirectory);
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Runtime-masterdata ontbreekt: config/runtime/masterdata-runtime.json is niet gevonden.");

            try
            {
                var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                var root = serializer.DeserializeObject(File.ReadAllText(path)) as Dictionary<string, object>;
                object rawTables;
                var tableObject = root != null && root.TryGetValue("tables", out rawTables)
                    ? rawTables as Dictionary<string, object>
                    : null;
                if (tableObject == null) throw new InvalidOperationException("Veld tables ontbreekt.");

                var tables = new Dictionary<string, List<Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);
                foreach (var table in tableObject)
                {
                    var records = new List<Dictionary<string, string>>();
                    var values = table.Value as object[];
                    if (values != null)
                    {
                        foreach (var value in values)
                        {
                            var rawRecord = value as Dictionary<string, object>;
                            if (rawRecord == null) continue;
                            var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            foreach (var field in rawRecord)
                                record[field.Key] = field.Value == null ? string.Empty : Convert.ToString(field.Value, System.Globalization.CultureInfo.InvariantCulture);
                            records.Add(record);
                        }
                    }
                    tables[table.Key] = records;
                }
                return new MasterDataRuntimeCatalog(path, tables);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Runtime-masterdata kan niet worden gelezen uit " + path + ": " + ex.Message, ex);
            }
        }

        public IList<Dictionary<string, string>> Records(string tableName)
        {
            List<Dictionary<string, string>> records;
            return tables.TryGetValue(tableName ?? string.Empty, out records)
                ? records.AsReadOnly()
                : new List<Dictionary<string, string>>().AsReadOnly();
        }

        public static string Value(Dictionary<string, string> record, string field)
        {
            string value;
            return record != null && record.TryGetValue(field, out value) ? value ?? string.Empty : string.Empty;
        }

        private static string FindUpwards(string startFolder)
        {
            if (string.IsNullOrWhiteSpace(startFolder)) return null;
            var folder = Path.GetFullPath(startFolder);
            for (var i = 0; i < 8 && !string.IsNullOrEmpty(folder); i++)
            {
                var candidate = Path.Combine(folder, "config", "runtime", "masterdata-runtime.json");
                if (File.Exists(candidate)) return candidate;
                var parent = Directory.GetParent(folder);
                if (parent == null) break;
                folder = parent.FullName;
            }
            return null;
        }
    }
}
