using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using SWWerkplaats.Configurator.Application;

namespace SWWerkplaats.Configurator.Portal
{
    public sealed class PortalWebServer : IDisposable
    {
        private readonly JavaScriptSerializer serializer;
        private readonly PortalOrderService orders;
        private TcpListener listener;
        private Thread worker;
        private bool disposed;

        public PortalWebServer(string rootFolder, string prefix)
        {
            Prefix = prefix;
            RootFolder = rootFolder;
            serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            orders = new PortalOrderService(rootFolder);
        }

        public string Prefix { get; private set; }
        public string RootFolder { get; private set; }
        public bool IsRunning { get; private set; }
        public string LastError { get; private set; }

        public void Start()
        {
            if (IsRunning) return;
            Directory.CreateDirectory(RootFolder);
            listener = new TcpListener(IPAddress.Loopback, PortFromPrefix(Prefix));
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
                catch (Exception ex)
                {
                    if (IsClientDisconnect(ex)) return;

                    LastError = ex.Message;
                    try
                    {
                        if (stream == null) stream = client.GetStream();
                        WriteJson(stream, 500, new { ok = false, error = ex.Message });
                    }
                    catch (Exception writeEx)
                    {
                        if (!IsClientDisconnect(writeEx))
                        {
                            LastError = writeEx.Message;
                        }
                    }
                }
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

            if (request.Method == "GET" && path == "/")
            {
                WriteHtml(stream, 200, PortalHtml.Page());
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
                WriteJson(stream, 200, new { sheets = catalog.Sheets, profiles = catalog.Profiles, statuses = catalog.Statuses });
                return;
            }

            if (request.Method == "POST" && path == "/api/shutdown")
            {
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
                WriteJson(stream, 200, new QuoteApplicationService().BuildQuote(quoteRequest));
                return;
            }

            if (request.Method == "POST" && path == "/api/orders")
            {
                var quoteRequest = serializer.Deserialize<PortalQuoteRequest>(request.Body);
                var record = orders.CreateOrder(quoteRequest);
                WriteJson(stream, 200, new PortalOrderResponse { Ok = true, Message = "Order ontvangen en klaargezet voor controle.", Order = record });
                return;
            }

            if (request.Method == "GET" && path == "/api/orders")
            {
                WriteJson(stream, 200, orders.ListOrders());
                return;
            }

            if (request.Method == "POST" && path.StartsWith("/api/orders/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/release", StringComparison.OrdinalIgnoreCase))
            {
                var orderId = path.Substring("/api/orders/".Length);
                orderId = orderId.Substring(0, orderId.Length - "/release".Length);
                WriteJson(stream, 200, new PortalOrderResponse { Ok = true, Message = "Order naar freeswachtrij gezet.", Order = orders.ReleaseToQueue(Uri.UnescapeDataString(orderId)) });
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
            for (var i = 1; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(lines[i].Substring("Content-Length:".Length).Trim(), out contentLength);
                }
            }

            var bodyStart = headerEnd + 4;
            while (total < bodyStart + contentLength && total < buffer.Length)
            {
                var read = stream.Read(buffer, total, buffer.Length - total);
                if (read <= 0) break;
                total += read;
            }

            var body = contentLength > 0 ? Encoding.UTF8.GetString(buffer, bodyStart, Math.Min(contentLength, total - bodyStart)) : "";
            return new HttpRequest { Method = first[0], Path = first.Length > 1 ? first[1].Split('?')[0] : "/", Body = body };
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

        private static string StatusText(int status)
        {
            if (status == 200) return "OK";
            if (status == 404) return "Not Found";
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
        }
    }
}
