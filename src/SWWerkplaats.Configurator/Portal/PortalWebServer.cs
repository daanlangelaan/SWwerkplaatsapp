using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Portal
{
    public sealed class PortalWebServer : IDisposable
    {
        private readonly JavaScriptSerializer serializer;
        private readonly OrderApplicationService orders;
        private readonly HealthApplicationService health;
        private readonly PortalSiteCatalog sites;
        private readonly PortalRolePolicy rolePolicy;
        private readonly PortalActorContextResolver actorResolver;
        private readonly PortalWorkspaceApplicationService workspace;
        private TcpListener listener;
        private Thread worker;
        private bool disposed;

        public PortalWebServer(string rootFolder, string prefix)
            : this(new PortalRuntimeOptions { RootFolder = rootFolder, Prefix = prefix, OrderStorageProvider = "sqlite", DatabasePath = Path.Combine(rootFolder, "portal-orders.sqlite") })
        {
        }

        public PortalWebServer(PortalRuntimeOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");
            Prefix = options.Prefix;
            RootFolder = options.RootFolder;
            OrderStorageProvider = options.OrderStorageProvider;
            DatabasePath = options.DatabasePath;
            serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var repository = string.Equals(options.OrderStorageProvider, "files", StringComparison.OrdinalIgnoreCase)
                ? (IOrderRepository)new FileOrderRepository(RootFolder)
                : new SqliteOrderRepository(RootFolder, options.DatabasePath);
            orders = new OrderApplicationService(repository);
            health = new HealthApplicationService(DateTime.Now);
            sites = new PortalSiteCatalog();
            rolePolicy = new PortalRolePolicy();
            actorResolver = new PortalActorContextResolver(rolePolicy, sites);
            var workspaceDatabasePath = string.IsNullOrWhiteSpace(options.DatabasePath)
                ? Path.Combine(RootFolder, "portal-orders.sqlite")
                : options.DatabasePath;
            workspace = new PortalWorkspaceApplicationService(new SqlitePortalWorkspaceRepository(workspaceDatabasePath), orders, rolePolicy, sites);
        }

        public string Prefix { get; private set; }
        public string RootFolder { get; private set; }
        public string OrderStorageProvider { get; private set; }
        public string DatabasePath { get; private set; }
        public bool IsRunning { get; private set; }
        public string LastError { get; private set; }

        public void Start()
        {
            if (IsRunning) return;
            Directory.CreateDirectory(RootFolder);
            listener = new TcpListener(BindAddressFromPrefix(Prefix), PortFromPrefix(Prefix));
            listener.Start();
            IsRunning = true;
            worker = new Thread(ListenLoop);
            worker.IsBackground = true;
            worker.Start();
        }

        public void Stop()
        {
            IsRunning = false;
            try { if (listener != null) listener.Stop(); } catch { }
        }

        private void ListenLoop()
        {
            while (IsRunning)
            {
                try
                {
                    var client = listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(delegate { HandleClient(client); });
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                    if (IsRunning) Thread.Sleep(250);
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            using (client)
            {
                NetworkStream stream = null;
                try
                {
                    stream = client.GetStream();
                    var request = ReadRequest(stream);
                    Handle(request, stream);
                }
                catch (PortalAccessDeniedException ex)
                {
                    WriteClientError(client, stream, 403, ex);
                }
                catch (ArgumentException ex)
                {
                    WriteClientError(client, stream, 400, ex);
                }
                catch (Exception ex)
                {
                    if (IsClientDisconnect(ex)) return;
                    WriteClientError(client, stream, 500, ex);
                }
            }
        }

        private void WriteClientError(TcpClient client, NetworkStream stream, int status, Exception error)
        {
            LastError = error.Message;
            try
            {
                if (stream == null) stream = client.GetStream();
                WriteJson(stream, status, new { ok = false, error = error.Message });
            }
            catch (Exception writeError)
            {
                if (!IsClientDisconnect(writeError)) LastError = writeError.Message;
            }
        }

        private static bool IsClientDisconnect(Exception ex)
        {
            while (ex != null)
            {
                var socket = ex as SocketException;
                if (socket != null)
                {
                    return socket.SocketErrorCode == SocketError.ConnectionReset
                        || socket.SocketErrorCode == SocketError.ConnectionAborted
                        || socket.SocketErrorCode == SocketError.Shutdown;
                }

                if (ex is ObjectDisposedException) return true;
                ex = ex.InnerException;
            }

            return false;
        }

        private void Handle(HttpRequest request, Stream stream)
        {
            var path = request.Path.TrimEnd('/');
            if (path == "") path = "/";
            var actor = actorResolver.Resolve(request.Headers);

            if (request.Method == "GET" && path == "/")
            {
                WriteHtml(stream, 200, PortalHtml.Page());
                return;
            }

            if (request.Method == "GET" && path == "/library")
            {
                PortalRolePolicy.Ensure(actor, PortalCapabilities.Configure);
                WriteHtml(stream, 200, PortalLibraryHtml.Page());
                return;
            }

            if (request.Method == "GET" && (path == "/app" || path.StartsWith("/app/", StringComparison.OrdinalIgnoreCase)))
            {
                WriteHtml(stream, 200, PortalWorkspaceHtml.Page());
                return;
            }

            if (request.Method == "GET" && (path.StartsWith("/vendor/", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)))
            {
                WriteStaticFile(stream, path);
                return;
            }

            if (request.Method == "GET" && path == "/api/catalog")
            {
                var catalog = new CatalogApplicationService().GetCatalog();
                var site = sites.GetRequired(actor.SiteId);
                var products = site.AllowAllProducts
                    ? catalog.Products
                    : catalog.Products.Where(product => site.AllowedProductIds.Any(id => string.Equals(id, product.Product, StringComparison.OrdinalIgnoreCase))).ToArray();
                WriteJson(stream, 200, new { sheets = catalog.Sheets, profiles = catalog.Profiles, rails = catalog.Rails, linearGuides = catalog.LinearGuides, liftColumns = catalog.LiftColumns, shelfSupports = catalog.ShelfSupports, statuses = catalog.Statuses, products = products, presentation = catalog.Presentation, site = site });
                return;
            }

            if (request.Method == "GET" && path == "/api/workspace/context")
            {
                WriteJson(stream, 200, workspace.GetContext(actor));
                return;
            }

            if (request.Method == "GET" && path == "/api/workspace/dashboard")
            {
                WriteJson(stream, 200, workspace.GetDashboard(actor));
                return;
            }

            if (request.Method == "GET" && path == "/api/workspace/projects")
            {
                WriteJson(stream, 200, workspace.ListProjects(actor));
                return;
            }

            if (request.Method == "GET" && path.StartsWith("/api/workspace/projects/", StringComparison.OrdinalIgnoreCase))
            {
                var projectId = path.Substring("/api/workspace/projects/".Length);
                WriteJson(stream, 200, workspace.GetProject(actor, Uri.UnescapeDataString(projectId)));
                return;
            }

            if (request.Method == "GET" && path == "/api/workspace/jobs")
            {
                WriteJson(stream, 200, workspace.ListJobs(actor));
                return;
            }

            if (request.Method == "GET" && path == "/api/workspace/purchasing")
            {
                WriteJson(stream, 200, workspace.ListPurchaseRequirements(actor));
                return;
            }

            if (request.Method == "GET" && path == "/api/workspace/inventory")
            {
                WriteJson(stream, 200, workspace.ListInventory(actor));
                return;
            }

            if (request.Method == "GET" && path == "/api/workspace/inventory-candidates")
            {
                WriteJson(stream, 200, workspace.ListInventoryCandidates(actor));
                return;
            }

            if (request.Method == "POST" && path == "/api/workspace/inventory")
            {
                WriteJson(stream, 200, workspace.SaveInventoryItem(actor, serializer.Deserialize<PortalInventoryItem>(request.Body)));
                return;
            }

            if (request.Method == "POST" && path.StartsWith("/api/workspace/inventory/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/movement", StringComparison.OrdinalIgnoreCase))
            {
                var itemId = ExtractResourceId(path, "/api/workspace/inventory/", "/movement");
                WriteJson(stream, 200, workspace.ApplyInventoryMovement(actor, Uri.UnescapeDataString(itemId), serializer.Deserialize<PortalInventoryMovementRequest>(request.Body)));
                return;
            }

            if (request.Method == "POST" && path.StartsWith("/api/workspace/jobs/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/status", StringComparison.OrdinalIgnoreCase))
            {
                var jobId = ExtractResourceId(path, "/api/workspace/jobs/", "/status");
                WriteJson(stream, 200, workspace.ChangeJobStatus(actor, Uri.UnescapeDataString(jobId), serializer.Deserialize<PortalJobStatusRequest>(request.Body)));
                return;
            }

            if (request.Method == "POST" && path.StartsWith("/api/workspace/projects/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/publication", StringComparison.OrdinalIgnoreCase))
            {
                var projectId = ExtractResourceId(path, "/api/workspace/projects/", "/publication");
                WriteJson(stream, 200, workspace.SetCustomerPublication(actor, Uri.UnescapeDataString(projectId), serializer.Deserialize<PortalProjectPublicationRequest>(request.Body)));
                return;
            }

            if (request.Method == "POST" && path.StartsWith("/api/workspace/projects/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/delivery-pricing", StringComparison.OrdinalIgnoreCase))
            {
                var projectId = ExtractResourceId(path, "/api/workspace/projects/", "/delivery-pricing");
                WriteJson(stream, 200, workspace.UpdateDeliveryPricing(actor, Uri.UnescapeDataString(projectId), serializer.Deserialize<PortalDeliveryPricingRequest>(request.Body)));
                return;
            }

            if (request.Method == "GET" && path == "/api/library")
            {
                PortalRolePolicy.Ensure(actor, PortalCapabilities.Configure);
                WriteJson(stream, 200, new HardwareLibraryData
                {
                    rails = HardwareLibraryRepository.DrawerRails(),
                    shelfSupports = HardwareLibraryRepository.ShelfSupports()
                });
                return;
            }

            if (request.Method == "POST" && path == "/api/library")
            {
                PortalRolePolicy.Ensure(actor, PortalCapabilities.LibraryUpdate);
                var data = serializer.Deserialize<HardwareLibraryData>(request.Body) ?? new HardwareLibraryData();
                var savedPath = HardwareLibraryRepository.Save(data.rails, data.shelfSupports);
                WriteJson(stream, 200, new
                {
                    ok = true,
                    path = savedPath,
                    rails = HardwareLibraryRepository.DrawerRails(),
                    shelfSupports = HardwareLibraryRepository.ShelfSupports()
                });
                return;
            }

            if (request.Method == "GET" && path == "/api/health")
            {
                PortalRolePolicy.Ensure(actor, PortalCapabilities.SystemControl);
                WriteJson(stream, 200, health.GetHealth(RootFolder, Prefix, OrderStorageProvider, DatabasePath));
                return;
            }

            if (request.Method == "GET" && path == "/api/workflow")
            {
                WriteJson(stream, 200, new WorkflowApplicationService().GetWorkflow());
                return;
            }

            if (request.Method == "POST" && path == "/api/shutdown")
            {
                PortalRolePolicy.Ensure(actor, PortalCapabilities.SystemControl);
                WriteJson(stream, 200, new { ok = true, message = "Portal stopt. Start de configurator opnieuw om verse code te laden." });
                ThreadPool.QueueUserWorkItem(delegate
                {
                    Thread.Sleep(250);
                    Environment.Exit(0);
                });
                return;
            }

            if (request.Method == "POST" && path == "/api/quote")
            {
                var quoteRequest = serializer.Deserialize<PortalQuoteRequest>(request.Body);
                PrepareQuoteRequest(quoteRequest, actor);
                WriteJson(stream, 200, new QuoteApplicationService().BuildQuote(quoteRequest));
                return;
            }

            if (request.Method == "POST" && path == "/api/solidworks/export")
            {
                var quoteRequest = serializer.Deserialize<PortalQuoteRequest>(request.Body);
                PrepareQuoteRequest(quoteRequest, actor);
                WriteJson(stream, 200, new ProductionOutputService().GenerateSolidWorksControlFiles(quoteRequest, RootFolder));
                return;
            }

            if (request.Method == "POST" && path == "/api/output/open-folder")
            {
                PortalRolePolicy.Ensure(actor, PortalCapabilities.ProjectReadAll);
                var openRequest = serializer.Deserialize<OpenOutputFolderRequest>(request.Body);
                var openedFolder = OpenOutputFolder(openRequest == null ? null : openRequest.Path);
                WriteJson(stream, 200, new { ok = true, folder = openedFolder });
                return;
            }

            if (request.Method == "POST" && path == "/api/orders")
            {
                var quoteRequest = serializer.Deserialize<PortalQuoteRequest>(request.Body);
                PrepareQuoteRequest(quoteRequest, actor);
                var record = orders.CreateOrder(quoteRequest);
                workspace.Sync();
                WriteJson(stream, 200, new PortalOrderResponse { Ok = true, Message = "Order ontvangen en klaargezet voor controle.", Order = record });
                return;
            }

            if (request.Method == "GET" && path == "/api/orders")
            {
                PortalRolePolicy.Ensure(actor, PortalCapabilities.ProjectReadAll);
                WriteJson(stream, 200, orders.ListOrders());
                return;
            }

            if (request.Method == "POST" && path.StartsWith("/api/orders/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/status", StringComparison.OrdinalIgnoreCase))
            {
                var orderId = ExtractOrderId(path, "/status");
                var statusRequest = serializer.Deserialize<PortalOrderStatusRequest>(request.Body);
                var record = orders.ChangeStatus(Uri.UnescapeDataString(orderId), statusRequest.Status, WorkflowRole(actor, statusRequest.Status));
                workspace.Sync();
                WriteJson(stream, 200, new PortalOrderResponse { Ok = true, Message = "Orderstatus bijgewerkt.", Order = record });
                return;
            }

            if (request.Method == "POST" && path.StartsWith("/api/orders/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/release", StringComparison.OrdinalIgnoreCase))
            {
                var orderId = ExtractOrderId(path, "/release");
                PortalRolePolicy.Ensure(actor, PortalCapabilities.JobsUpdateAll);
                var record = orders.ReleaseToQueue(Uri.UnescapeDataString(orderId));
                workspace.Sync();
                WriteJson(stream, 200, new PortalOrderResponse { Ok = true, Message = "Order naar freeswachtrij gezet.", Order = record });
                return;
            }

            WriteHtml(stream, 404, "Niet gevonden");
        }

        private static HttpRequest ReadRequest(NetworkStream stream)
        {
            var buffer = new byte[1024 * 1024];
            var total = 0;
            var headerEnd = -1;
            while (total < buffer.Length)
            {
                var read = stream.Read(buffer, total, buffer.Length - total);
                if (read <= 0) break;
                total += read;
                headerEnd = FindHeaderEnd(buffer, total);
                if (headerEnd >= 0) break;
            }

            if (headerEnd < 0) throw new InvalidOperationException("Ongeldige HTTP request.");
            var headerText = Encoding.UTF8.GetString(buffer, 0, headerEnd);
            var lines = headerText.Split(new[] { "\r\n" }, StringSplitOptions.None);
            var first = lines[0].Split(' ');
            var contentLength = 0;
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 1; i < lines.Length; i++)
            {
                var separator = lines[i].IndexOf(':');
                if (separator <= 0) continue;
                var name = lines[i].Substring(0, separator).Trim();
                var value = lines[i].Substring(separator + 1).Trim();
                headers[name] = value;
                if (string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase)) int.TryParse(value, out contentLength);
            }

            var bodyStart = headerEnd + 4;
            while (total < bodyStart + contentLength && total < buffer.Length)
            {
                var read = stream.Read(buffer, total, buffer.Length - total);
                if (read <= 0) break;
                total += read;
            }

            var body = contentLength > 0 ? Encoding.UTF8.GetString(buffer, bodyStart, Math.Min(contentLength, total - bodyStart)) : "";
            return new HttpRequest { Method = first[0], Path = first.Length > 1 ? first[1].Split('?')[0] : "/", Body = body, Headers = headers };
        }

        private void PrepareQuoteRequest(PortalQuoteRequest request, PortalActorContext actor)
        {
            if (request == null) throw new ArgumentNullException("request");
            PortalRolePolicy.Ensure(actor, PortalCapabilities.Configure);
            var site = sites.GetRequired(actor.SiteId);
            sites.EnsureProductAllowed(site, request.Product);
            request.SourceSiteId = actor.SiteId;
            request.OrganizationId = actor.OrganizationId;
            request.RequestedByUserId = actor.UserId;
        }

        private static OrderWorkflowRole WorkflowRole(PortalActorContext actor, string nextStatus)
        {
            if (PortalRolePolicy.Has(actor, PortalCapabilities.JobsUpdateAll))
                return string.Equals(nextStatus, OrderWorkflowStatus.InProductie, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(nextStatus, OrderWorkflowStatus.Gereed, StringComparison.OrdinalIgnoreCase)
                    ? OrderWorkflowRole.Uitvoerder
                    : OrderWorkflowRole.Werkvoorbereider;
            if (PortalRolePolicy.Has(actor, PortalCapabilities.JobsReadAll)) return OrderWorkflowRole.Uitvoerder;
            throw new PortalAccessDeniedException("De gekozen portalrol mag de orderstatus niet wijzigen.");
        }

        private static string ExtractOrderId(string path, string suffix)
        {
            var orderId = path.Substring("/api/orders/".Length);
            return orderId.Substring(0, orderId.Length - suffix.Length);
        }

        private static string ExtractResourceId(string path, string prefix, string suffix)
        {
            var value = path.Substring(prefix.Length);
            return value.Substring(0, value.Length - suffix.Length);
        }

        private string OpenOutputFolder(string requestedPath)
        {
            if (string.IsNullOrWhiteSpace(requestedPath)) throw new ArgumentException("Outputpad ontbreekt.");
            var fullPath = Path.GetFullPath(requestedPath);
            if (File.Exists(fullPath)) fullPath = Path.GetDirectoryName(fullPath);
            var allowedRoot = Path.GetFullPath(RootFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var candidate = Path.GetFullPath(fullPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(fullPath))
                throw new InvalidOperationException("De outputmap valt buiten de portal-output of bestaat niet.");

            Process.Start(new ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            });
            return fullPath;
        }

        private static int FindHeaderEnd(byte[] buffer, int length)
        {
            for (var i = 3; i < length; i++)
            {
                if (buffer[i - 3] == 13 && buffer[i - 2] == 10 && buffer[i - 1] == 13 && buffer[i] == 10)
                {
                    return i - 3;
                }
            }

            return -1;
        }

        private void WriteJson(Stream stream, int status, object value)
        {
            WriteResponse(stream, status, "application/json; charset=utf-8", serializer.Serialize(value));
        }

        private static void WriteHtml(Stream stream, int status, string html)
        {
            WriteResponse(stream, status, "text/html; charset=utf-8", html);
        }

        private static void WriteStaticFile(Stream stream, string path)
        {
            var relative = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var baseFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PortalAssets");
            var fullPath = Path.GetFullPath(Path.Combine(baseFolder, relative));
            if (!fullPath.StartsWith(Path.GetFullPath(baseFolder), StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
            {
                WriteHtml(stream, 404, "Niet gevonden");
                return;
            }

            WriteBytes(stream, 200, ContentType(fullPath), File.ReadAllBytes(fullPath));
        }

        private static void WriteResponse(Stream stream, int status, string contentType, string body)
        {
            var bodyBytes = Encoding.UTF8.GetBytes(body ?? "");
            WriteBytes(stream, status, contentType, bodyBytes);
        }

        private static void WriteBytes(Stream stream, int status, string contentType, byte[] bodyBytes)
        {
            var header = "HTTP/1.1 " + status + " " + StatusText(status) + "\r\n"
                + "Content-Type: " + contentType + "\r\n"
                + "Content-Length: " + bodyBytes.Length + "\r\n"
                + "Cache-Control: no-cache\r\n"
                + "Connection: close\r\n\r\n";
            var headerBytes = Encoding.ASCII.GetBytes(header);
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(bodyBytes, 0, bodyBytes.Length);
        }

        private static string ContentType(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".js") return "application/javascript; charset=utf-8";
            if (extension == ".css") return "text/css; charset=utf-8";
            if (extension == ".json") return "application/json; charset=utf-8";
            if (extension == ".png") return "image/png";
            if (extension == ".jpg" || extension == ".jpeg") return "image/jpeg";
            if (extension == ".webp") return "image/webp";
            if (extension == ".svg") return "image/svg+xml; charset=utf-8";
            return "application/octet-stream";
        }

        private static int PortFromPrefix(string prefix)
        {
            var uri = new Uri(prefix);
            return uri.Port;
        }

        private static IPAddress BindAddressFromPrefix(string prefix)
        {
            var uri = new Uri(prefix);
            var host = (uri.Host ?? "").Trim();
            if (string.Equals(host, "0.0.0.0", StringComparison.OrdinalIgnoreCase) || string.Equals(host, "+", StringComparison.OrdinalIgnoreCase))
            {
                return IPAddress.Any;
            }

            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
            {
                return IPAddress.Loopback;
            }

            IPAddress parsed;
            if (IPAddress.TryParse(host, out parsed))
            {
                if (IPAddress.IsLoopback(parsed)) return IPAddress.Loopback;
                return parsed;
            }

            return IPAddress.Any;
        }

        private static string StatusText(int status)
        {
            if (status == 200) return "OK";
            if (status == 404) return "Not Found";
            if (status == 400) return "Bad Request";
            if (status == 403) return "Forbidden";
            if (status == 500) return "Internal Server Error";
            return "OK";
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Stop();
        }

        private sealed class HttpRequest
        {
            public string Method { get; set; }
            public string Path { get; set; }
            public string Body { get; set; }
            public Dictionary<string, string> Headers { get; set; }
        }

        private sealed class OpenOutputFolderRequest
        {
            public string Path { get; set; }
        }
    }
}
