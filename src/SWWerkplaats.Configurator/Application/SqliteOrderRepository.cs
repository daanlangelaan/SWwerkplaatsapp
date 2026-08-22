using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.Script.Serialization;
using Microsoft.Data.Sqlite;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class SqliteOrderRepository : IOrderRepository
    {
        private readonly string databasePath;
        private readonly FileOrderRepository files;
        private readonly JavaScriptSerializer serializer;

        public SqliteOrderRepository(string rootFolder, string databasePath)
        {
            if (string.IsNullOrWhiteSpace(rootFolder)) throw new ArgumentException("PortalData-root ontbreekt.", "rootFolder");
            this.databasePath = Path.GetFullPath(string.IsNullOrWhiteSpace(databasePath)
                ? Path.Combine(rootFolder, "portal-orders.sqlite")
                : databasePath);
            files = new FileOrderRepository(rootFolder);
            serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            Initialize();
            ImportExistingFileOrders();
        }

        public string CreateOrderFolder(string orderId) { return files.CreateOrderFolder(orderId); }

        public List<PortalOrderRecord> ListOrders()
        {
            var result = new List<PortalOrderRecord>();
            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT record_json FROM orders ORDER BY created_at DESC, order_id DESC";
                using (var reader = command.ExecuteReader())
                    while (reader.Read()) result.Add(serializer.Deserialize<PortalOrderRecord>(reader.GetString(0)));
            }
            return result;
        }

        public PortalOrderRecord LoadOrder(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return null;
            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT record_json FROM orders WHERE order_id = $id";
                command.Parameters.AddWithValue("$id", orderId);
                var value = command.ExecuteScalar() as string;
                return string.IsNullOrWhiteSpace(value) ? null : serializer.Deserialize<PortalOrderRecord>(value);
            }
        }

        public void SaveRequest(string orderFolder, PortalQuoteRequest request)
        {
            files.SaveRequest(orderFolder, request);
            var orderId = Path.GetFileName(orderFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            using (var connection = Open())
            using (var transaction = connection.BeginTransaction())
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO order_requests(order_id, request_json, updated_at) VALUES($id,$json,$now) "
                    + "ON CONFLICT(order_id) DO UPDATE SET request_json=excluded.request_json, updated_at=excluded.updated_at";
                command.Parameters.AddWithValue("$id", orderId);
                command.Parameters.AddWithValue("$json", serializer.Serialize(request));
                command.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public void SaveRecord(PortalOrderRecord record)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.OrderId)) throw new ArgumentException("Ongeldig orderrecord.", "record");
            using (var connection = Open())
            using (var transaction = connection.BeginTransaction())
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO orders(order_id,created_at,status,record_json,updated_at) VALUES($id,$created,$status,$json,$now) "
                    + "ON CONFLICT(order_id) DO UPDATE SET status=excluded.status, record_json=excluded.record_json, updated_at=excluded.updated_at";
                command.Parameters.AddWithValue("$id", record.OrderId);
                command.Parameters.AddWithValue("$created", record.CreatedAt ?? string.Empty);
                command.Parameters.AddWithValue("$status", record.Status ?? string.Empty);
                command.Parameters.AddWithValue("$json", serializer.Serialize(record));
                command.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
                command.ExecuteNonQuery();
                transaction.Commit();
            }
            files.SaveRecord(record);
        }

        public void SaveOfferText(string orderFolder, string contents) { files.SaveOfferText(orderFolder, contents); }
        public void WriteNotifications(PortalOrderRecord record, PortalQuoteRequest request) { files.WriteNotifications(record, request); }
        public string CopyOrderToQueue(PortalOrderRecord record) { return files.CopyOrderToQueue(record); }

        private void Initialize()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath));
            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "CREATE TABLE IF NOT EXISTS orders("+
                    "order_id TEXT PRIMARY KEY, created_at TEXT NOT NULL, status TEXT NOT NULL, record_json TEXT NOT NULL, updated_at TEXT NOT NULL);"+
                    "CREATE INDEX IF NOT EXISTS ix_orders_created ON orders(created_at DESC);"+
                    "CREATE INDEX IF NOT EXISTS ix_orders_status ON orders(status);"+
                    "CREATE TABLE IF NOT EXISTS order_requests(order_id TEXT PRIMARY KEY, request_json TEXT NOT NULL, updated_at TEXT NOT NULL);";
                command.ExecuteNonQuery();
            }
        }

        private SqliteConnection Open()
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
                Pooling = true,
                DefaultTimeout = 30
            };
            var connection = new SqliteConnection(builder.ToString());
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=30000;";
                command.ExecuteNonQuery();
            }
            return connection;
        }

        private void ImportExistingFileOrders()
        {
            foreach (var record in files.ListOrders())
            {
                using (var connection = Open())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT OR IGNORE INTO orders(order_id,created_at,status,record_json,updated_at) VALUES($id,$created,$status,$json,$now)";
                    command.Parameters.AddWithValue("$id", record.OrderId);
                    command.Parameters.AddWithValue("$created", record.CreatedAt ?? string.Empty);
                    command.Parameters.AddWithValue("$status", record.Status ?? string.Empty);
                    command.Parameters.AddWithValue("$json", serializer.Serialize(record));
                    command.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
