using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Portal;

internal static class Program
{
    private static int Main()
    {
        var root = Path.Combine(Path.GetTempPath(), "sww-order-storage-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            VerifyFileMigration(root);
            VerifyConcurrentSqliteWrites(root);
            Console.WriteLine("PASS  SQLite-migratie, WAL-opslag, gelijktijdige writes en bestandsmirror werken.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL  " + ex);
            return 1;
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static void VerifyFileMigration(string root)
    {
        var files = new FileOrderRepository(root);
        var folder = files.CreateOrderFolder("SW-LEGACY-001");
        files.SaveRecord(Record("SW-LEGACY-001", folder, "2026-08-22T12:00:00"));

        var sql = new SqliteOrderRepository(root, Path.Combine(root, "orders.sqlite"));
        Require(sql.LoadOrder("SW-LEGACY-001") != null, "Bestaande bestandsorder is niet geïmporteerd");
    }

    private static void VerifyConcurrentSqliteWrites(string root)
    {
        var database = Path.Combine(root, "orders.sqlite");
        var tasks = new List<Task>();
        for (var i = 0; i < 24; i++)
        {
            var number = i;
            tasks.Add(Task.Run(() =>
            {
                var repository = new SqliteOrderRepository(root, database);
                var id = "SW-CONCURRENT-" + number.ToString("00");
                var folder = repository.CreateOrderFolder(id);
                repository.SaveRequest(folder, new PortalQuoteRequest { Product = "robotcel", WidthMm = 1200, DepthMm = 800, HeightMm = 900 });
                var record = Record(id, folder, "2026-08-22T13:" + number.ToString("00") + ":00");
                repository.SaveRecord(record);
                record.Status = "Goedgekeurd";
                repository.SaveRecord(record);
            }));
        }
        Task.WaitAll(tasks.ToArray());

        var verifier = new SqliteOrderRepository(root, database);
        var orders = verifier.ListOrders();
        Require(orders.Count == 25, "Verwacht 25 gemigreerde/nieuwe orders, kreeg " + orders.Count);
        Require(orders.Count(order => order.Status == "Goedgekeurd") == 24, "Niet alle gelijktijdige statusupdates zijn bewaard");
        Require(File.Exists(Path.Combine(root, "Orders", "SW-CONCURRENT-00", "order-status.json")), "Herstelbare ordermirror ontbreekt");
        Require(File.Exists(database), "SQLite-database ontbreekt");
    }

    private static PortalOrderRecord Record(string id, string folder, string created)
    {
        return new PortalOrderRecord
        {
            OrderId = id,
            Status = "Te controleren",
            CreatedAt = created,
            ProductName = "Robot cel",
            CustomerName = "Regressietest",
            CustomerEmail = "test@example.invalid",
            PriceExVat = 100,
            PriceIncVat = 121,
            OutputFolder = folder
        };
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
