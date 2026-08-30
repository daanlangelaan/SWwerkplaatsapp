using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Data.Sqlite;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Portal;

internal static class Program
{
    private static int Main()
    {
        var root = Path.Combine(Path.GetTempPath(), "sww-portal-workspace-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            VerifyWorkspaceFlow(root);
            VerifyHttpBoundaries(root);
            Console.WriteLine("PASS  Sitescope, rolgrenzen, klantpublicatie, werkplaats, inkoop en voorraad werken op één projectcontract.");
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

    private static void VerifyHttpBoundaries(string root)
    {
        var port = FreePort();
        var serverRoot = Path.Combine(root, "http");
        using (var server = new PortalWebServer(new PortalRuntimeOptions
        {
            RootFolder = serverRoot,
            Prefix = "http://localhost:" + port + "/",
            Port = port,
            PortalOnly = true,
            OrderStorageProvider = "sqlite",
            DatabasePath = Path.Combine(serverRoot, "portal.sqlite")
        }))
        {
            server.Start();
            var admin = new Dictionary<string, string> { { "X-SW-Test-Role", "beheerder" }, { "X-SW-Test-Site", "shipping-boxes" } };
            var assemblyOperator = new Dictionary<string, string> { { "X-SW-Test-Role", "assemblage" }, { "X-SW-Test-Site", "internal" }, { "X-SW-Test-Organization", "internal" } };
            var customer = new Dictionary<string, string> { { "X-SW-Test-Role", "klant" }, { "X-SW-Test-Site", "shipping-boxes" }, { "X-SW-Test-Organization", "customer-one" } };
            var context = Get(port, "/api/workspace/context", admin);
            Require(context.Status == 200 && context.Body.Contains("\"RoleId\":\"beheerder\"") && context.Body.Contains("\"HomeRoute\":\"/app\"") && context.Body.Contains("\"HomeLabel\":\"Overzicht\""), "HTTP-actorcontract of startwerkplek ontbreekt");
            Require(context.Body.Contains("\"RoleId\":\"productiemedewerker\"") && context.Body.Contains("\"RoleId\":\"klant\"") && !context.Body.Contains("\"RoleId\":\"verkoop\"") && !context.Body.Contains("\"RoleId\":\"cncoperator\"") && context.Body.Contains("\"AreaId\":\"dispatch\""), "Driedelige pilotrolverdeling of gescheiden productiewachtrijen klopt niet");
            var catalog = Get(port, "/api/catalog", admin);
            Require(catalog.Status == 200 && catalog.Body.Contains("\"Product\":\"shipping_box\"") && !catalog.Body.Contains("\"Product\":\"werktafel\""), "HTTP-sitefilter lekt producten");
            var shell = Get(port, "/app/workshop", admin);
            Require(shell.Status == 200 && !shell.Body.Contains("/*DESIGN_TOKENS*/") && shell.Body.Contains("Bekijk portal als") && shell.Body.Contains("role-choices") && !shell.Body.Contains("<select id=\"role\"") && !shell.Body.Contains("Pas testcontext toe"), "Directe rolkeuzes of designtokens ontbreken");
            var operatorDashboard = Get(port, "/api/workspace/dashboard", assemblyOperator);
            Require(operatorDashboard.Status == 200 && !operatorDashboard.Body.Contains("project.read.all"), "Operator-dashboard vraagt ten onrechte volledige projectrechten");
            Require(Get(port, "/api/orders", customer).Status == 403, "Klant kan legacy-orderlijst via HTTP lezen");
        }
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static HttpResult Get(int port, string path, IDictionary<string, string> headers)
    {
        var request = (HttpWebRequest)WebRequest.Create("http://localhost:" + port + path);
        request.Method = "GET";
        foreach (var header in headers) request.Headers[header.Key] = header.Value;
        try
        {
            using (var response = (HttpWebResponse)request.GetResponse()) return Read(response);
        }
        catch (WebException ex)
        {
            var response = ex.Response as HttpWebResponse;
            if (response == null) throw;
            using (response) return Read(response);
        }
    }

    private static HttpResult Read(HttpWebResponse response)
    {
        using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            return new HttpResult { Status = (int)response.StatusCode, Body = reader.ReadToEnd() };
    }

    private sealed class HttpResult
    {
        public int Status { get; set; }
        public string Body { get; set; }
    }

    private static void VerifyWorkspaceFlow(string root)
    {
        var orderRepository = new FileOrderRepository(root);
        var orders = new OrderApplicationService(orderRepository);
        var roles = new PortalRolePolicy();
        Require(roles.GetRequired("beheerder").Label == "Bedrijfsbeheer", "Zichtbare naam van de bedrijfsbeheerrol klopt niet");
        Require(roles.GetRequired("verkoop").Label == "Verkoop & offertes", "Zichtbare naam van de verkooprol klopt niet");
        Require(roles.GetRequired("werkvoorbereider").Label == "Werkvoorbereiding", "Zichtbare naam van de werkvoorbereidingsrol klopt niet");
        Require(roles.GetRequired("productiemedewerker").HomeLabel == "Productieoverzicht", "Productiemedewerker mist het productieoverzicht");
        var businessCapabilities = roles.GetRequired("beheerder").Capabilities;
        Require(new[] { "verkoop", "werkvoorbereider", "inkoper" }.SelectMany(roleId => roles.GetRequired(roleId).Capabilities).All(capability => businessCapabilities.Contains(capability)), "Bedrijfsbeheer dekt niet alle bewaarde kantoorrollen");
        Require(roles.ListSimulatorChoices("beheerder").Select(role => role.RoleId).SequenceEqual(new[] { "beheerder", "productiemedewerker", "klant" }), "Normale testkeuze bevat niet exact Bedrijfsbeheer, Productiemedewerker en Klant");
        Require(roles.ListSimulatorChoices("verkoop").Any(role => role.RoleId == "verkoop") && roles.ListSimulatorChoices("verkoop").All(role => role.RoleId != "werkvoorbereider" && role.RoleId != "inkoper"), "Bewaarde verkooprol kan niet afzonderlijk worden geladen");
        Require(roles.ListSimulatorChoices("cncoperator").Any(role => role.RoleId == "cncoperator") && roles.ListSimulatorChoices("cncoperator").All(role => role.RoleId != "profieloperator"), "CNC-werkplek kan de bewaarde specialistrol niet laden");
        var sites = new PortalSiteCatalog();
        var resolver = new PortalActorContextResolver(roles, sites);
        var repository = new SqlitePortalWorkspaceRepository(Path.Combine(root, "workspace.sqlite"));
        var service = new PortalWorkspaceApplicationService(repository, orders, roles, sites);
        var admin = Actor(resolver, "beheerder", "internal", "customer-one");
        var customer = Actor(resolver, "klant", "workstations", "customer-one");
        var otherCustomer = Actor(resolver, "klant", "workstations", "customer-two");
        var productionOperator = Actor(resolver, "productiemedewerker", "internal", "internal");
        var profileOperator = Actor(resolver, "profieloperator", "internal", "internal");
        var cncOperator = Actor(resolver, "cncoperator", "internal", "internal");
        Require(profileOperator.HomeRoute == "/app/workshop/profile-machine" && profileOperator.HomeLabel == "Mijn wachtrij" && profileOperator.DefaultWorkAreaId == "profile-machine", "Profieloperator mist rolgerichte startwerkplek");
        Require(cncOperator.HomeRoute == "/app/workshop/sheet-cnc" && cncOperator.HomeLabel == "Mijn wachtrij" && cncOperator.DefaultWorkAreaId == "sheet-cnc", "CNC-operator mist rolgerichte startwerkplek");

        var candidate = service.ListInventoryCandidates(admin).First(value => value.Category == "Component");
        var orderFolder = orderRepository.CreateOrderFolder("SW-PORTAL-001");
        var record = new PortalOrderRecord
        {
            OrderId = "SW-PORTAL-001",
            ProjectId = "P-PORTAL-001",
            SourceSiteId = "workstations",
            OrganizationId = "customer-one",
            ProductId = "werktafel",
            ProductName = "Werktafel",
            ProjectName = "Testwerktafel",
            CustomerName = "Testklant",
            DeliveryForm = "gemonteerd",
            ReceiptMethod = "verzenden",
            AssemblyPriceStatus = "Op aanvraag",
            ShippingPriceStatus = "Op aanvraag",
            Status = "Te controleren",
            CreatedAt = "2026-08-28T12:00:00Z",
            OutputFolder = orderFolder,
            PriceExVat = 100m,
            PriceIncVat = 121m
        };
        record.PurchaseLines.Add(new PortalPurchaseSnapshotLine { StableItemId = candidate.StableItemId, Description = candidate.Description, Category = "Beslag", RequiredQuantity = 120m, Unit = "stuks", PurchaseUnitPrice = 0.25m, Supplier = "Testleverancier" });
        record.ProductionAreas.Add(new PortalProductionAreaSnapshot { AreaId = "profile-machine", Label = "Profielenmachine", ItemCount = 8 });
        record.ProductionAreas.Add(new PortalProductionAreaSnapshot { AreaId = "sheet-cnc", Label = "Plaat-CNC", ItemCount = 3 });
        record.ProductionAreas.Add(new PortalProductionAreaSnapshot { AreaId = "assembly", Label = "Assemblage", ItemCount = 11 });
        record.ProductionAreas.Add(new PortalProductionAreaSnapshot { AreaId = "dispatch", Label = "Completeren & verzending", ItemCount = 1 });
        record.Files.Add(Path.Combine(orderFolder, "Klantbijlage.html"));
        record.Files.Add(Path.Combine(orderFolder, "Nesting", "intern-programma.tap"));
        record.Files.Add(Path.Combine(orderFolder, "AssemblageControle.csv"));
        orderRepository.SaveRecord(record);

        service.Sync();
        Require(service.ListProjects(admin).Count == 1, "Intern project ontbreekt");
        var priced = service.UpdateDeliveryPricing(admin, "P-PORTAL-001", new PortalDeliveryPricingRequest { AssemblyPriceExVat = 250m, ShippingPriceExVat = 65m });
        Require(priced.Project.AssemblyPriceExVat == 250m && priced.Project.ShippingPriceExVat == 65m && priced.Project.AssemblyPriceStatus == "Vastgesteld" && priced.Project.ShippingPriceStatus == "Vastgesteld", "Bedrijfsbeheer kon montage- en verzendkosten niet vaststellen");
        Require(service.ListProjects(customer).Count == 0, "Niet-gepubliceerd project lekt naar klant");
        service.SetCustomerPublication(admin, "P-PORTAL-001", new PortalProjectPublicationRequest { Published = true });
        var customerProject = service.ListProjects(customer).Single();
        Require(customerProject.OrganizationId == null && customerProject.CustomerName == null, "Klantcontract bevat interne organisatie- of klantvelden");
        Require(customerProject.DeliveryForm == "gemonteerd" && customerProject.ReceiptMethod == "verzenden" && customerProject.AssemblyPriceExVat == 250m && customerProject.ShippingPriceExVat == 65m, "Klantcontract mist vastgelegde levervorm of leveringskosten");
        var customerDetail = service.GetProject(customer, "P-PORTAL-001");
        Require(customerDetail.Documents.Count == 1 && customerDetail.Documents.All(document => document.CustomerVisible), "Klantdetail bevat een intern document");
        Require(customerDetail.PurchaseLines.Count == 0 && customerDetail.ProductionAreas.Count == 0, "Klantdetail bevat inkoop- of productiegegevens");
        Require(service.ListProjects(otherCustomer).Count == 0, "Project lekt naar een andere klantorganisatie");
        ExpectDenied(() => service.ListProjects(productionOperator), "Productiemedewerker kan volledige projectdossiers lezen");

        var profileJob = service.ListJobs(profileOperator).Single(job => job.AreaId == "profile-machine");
        Require(service.ListJobs(profileOperator).All(job => job.AreaId == "profile-machine"), "Profieloperator ziet taken buiten de eigen wachtrij");
        var productionJobs = service.ListJobs(productionOperator);
        Require(productionJobs.Count == 4 && productionJobs.Select(job => job.AreaId).Distinct().Count() == 4 && productionJobs.Any(job => job.AreaId == "dispatch"), "Productiemedewerker mist het totale productie- en verzendoverzicht");
        var sheetJob = productionJobs.Single(job => job.AreaId == "sheet-cnc");
        Require(sheetJob.Documents.Any(document => document.FileName == "intern-programma.tap"), "Plaat-CNC-taak mist het interne productieoverzicht");
        var productionDashboard = service.GetDashboard(productionOperator);
        Require(productionDashboard.ProjectCount == 0 && productionDashboard.ActiveJobs.Count == 4, "Productiedashboard bevat geen volledig taakoverzicht of lekt projecten");
        var operatorDashboard = service.GetDashboard(profileOperator);
        Require(operatorDashboard.ProjectCount == 0 && operatorDashboard.ActiveJobs.All(job => job.AreaId == "profile-machine"), "Operator-dashboard lekt projecten of andere werkgebieden");
        var dispatchJob = productionJobs.Single(job => job.AreaId == "dispatch");
        ExpectInvalid(() => service.ChangeJobStatus(productionOperator, dispatchJob.JobId, new PortalJobStatusRequest { Status = "Bezig" }), "Afrondingstaak startte voordat productie gereed was");
        service.ChangeJobStatus(productionOperator, sheetJob.JobId, new PortalJobStatusRequest { Status = "Bezig" });
        Require(repository.LoadJob(sheetJob.JobId).Status == "Bezig", "Productiemedewerker kon de CNC-taak niet starten");
        service.ChangeJobStatus(profileOperator, profileJob.JobId, new PortalJobStatusRequest { Status = "Bezig" });
        Require(repository.LoadJob(profileJob.JobId).Status == "Bezig", "Profieloperator kon eigen taak niet starten");
        ExpectDenied(() => service.ChangeJobStatus(cncOperator, profileJob.JobId, new PortalJobStatusRequest { Status = "Gereed" }), "CNC-operator wijzigde profielenmachinetaak");
        foreach (var job in service.ListJobs(productionOperator).Where(job => job.AreaId != "dispatch"))
            service.ChangeJobStatus(productionOperator, job.JobId, new PortalJobStatusRequest { Status = "Gereed" });
        Require(service.GetProject(admin, "P-PORTAL-001").Project.FulfillmentStatus == "Productie gereed", "Gereed productiewerk leidt niet tot Productie gereed");
        dispatchJob = service.ListJobs(productionOperator).Single(job => job.AreaId == "dispatch");
        service.ChangeJobStatus(productionOperator, dispatchJob.JobId, new PortalJobStatusRequest { Status = "Gereed" });
        Require(service.GetProject(customer, "P-PORTAL-001").Project.Status == "Klaar voor verzending", "Afrondingstaak leidt niet tot klantstatus Klaar voor verzending");

        var stock = service.SaveInventoryItem(admin, new PortalInventoryItem
        {
            StableItemId = candidate.StableItemId,
            StockUnit = "stuks",
            PurchaseUnit = "doos",
            UnitsPerPurchase = 100m,
            TrackStock = true
        });
        service.ApplyInventoryMovement(admin, stock.InventoryItemId, new PortalInventoryMovementRequest { MovementType = "receipt", Quantity = 100m });
        service.ApplyInventoryMovement(admin, stock.InventoryItemId, new PortalInventoryMovementRequest { MovementType = "reserve", Quantity = 25m, ProjectId = "P-PORTAL-001" });
        var afterMovement = service.ListInventory(admin).Single();
        Require(afterMovement.PhysicalQuantity == 100m && afterMovement.AvailableQuantity == 75m, "Voorraad of reservering onjuist");
        var requirement = service.ListPurchaseRequirements(admin).Single(value => value.StableItemId == candidate.StableItemId);
        Require(requirement.ShortageQuantity == 45m && requirement.SuggestedPurchaseUnits == 1m, "Inkooptekort of besteladvies onjuist");
        ExpectDenied(() => service.ListInventory(customer), "Klant kon voorraad lezen");

        var shippingSite = sites.GetRequired("shipping-boxes");
        sites.EnsureProductAllowed(shippingSite, "shipping_box");
        ExpectInvalid(() => sites.EnsureProductAllowed(shippingSite, "werktafel"), "Sitescope liet een niet-toegestaan product toe");
    }

    private static PortalActorContext Actor(PortalActorContextResolver resolver, string role, string site, string organization)
    {
        return resolver.Resolve(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "X-SW-Test-Role", role },
            { "X-SW-Test-Site", site },
            { "X-SW-Test-Organization", organization }
        });
    }

    private static void ExpectDenied(Action action, string message)
    {
        try { action(); }
        catch (PortalAccessDeniedException) { return; }
        throw new InvalidOperationException(message);
    }

    private static void ExpectInvalid(Action action, string message)
    {
        try { action(); }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException(message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
