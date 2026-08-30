using System;
using System.Collections.Generic;
using System.Linq;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class PortalAccessDeniedException : InvalidOperationException
    {
        public PortalAccessDeniedException(string message) : base(message) { }
    }

    public static class PortalCapabilities
    {
        public const string DashboardRead = "dashboard.read";
        public const string ProjectReadAll = "project.read.all";
        public const string CustomerProjectRead = "project.read.customer";
        public const string CustomerPublish = "customer.publish";
        public const string JobsReadAll = "jobs.read.all";
        public const string JobsUpdateAll = "jobs.update.all";
        public const string PurchasingRead = "purchasing.read";
        public const string InventoryRead = "inventory.read";
        public const string InventoryUpdate = "inventory.update";
        public const string Configure = "configure";
        public const string SystemControl = "system.control";
        public const string LibraryUpdate = "library.update";
    }

    public sealed class PortalRolePolicy
    {
        private readonly List<PortalRoleDefinition> roles;

        public PortalRolePolicy()
        {
            roles = new List<PortalRoleDefinition>
            {
                Role("beheerder", "Systeembeheerder", "/app", "Overzicht", null, true, PortalCapabilities.DashboardRead, PortalCapabilities.ProjectReadAll, PortalCapabilities.CustomerPublish, PortalCapabilities.JobsReadAll, PortalCapabilities.JobsUpdateAll, PortalCapabilities.PurchasingRead, PortalCapabilities.InventoryRead, PortalCapabilities.InventoryUpdate, PortalCapabilities.Configure, PortalCapabilities.SystemControl, PortalCapabilities.LibraryUpdate),
                Role("verkoop", "Verkoop & offertes", "/app/projects", "Offertes & projecten", null, true, PortalCapabilities.DashboardRead, PortalCapabilities.ProjectReadAll, PortalCapabilities.CustomerPublish, PortalCapabilities.Configure),
                Role("werkvoorbereider", "Werkvoorbereiding", "/app/workshop", "Productieplanning", null, true, PortalCapabilities.DashboardRead, PortalCapabilities.ProjectReadAll, PortalCapabilities.JobsReadAll, PortalCapabilities.JobsUpdateAll, PortalCapabilities.PurchasingRead, PortalCapabilities.InventoryRead, PortalCapabilities.Configure),
                Role("inkoper", "Inkoper", "/app/purchasing", "Inkoopplanning", null, true, PortalCapabilities.DashboardRead, PortalCapabilities.ProjectReadAll, PortalCapabilities.PurchasingRead, PortalCapabilities.InventoryRead, PortalCapabilities.InventoryUpdate),
                Role("productiemedewerker", "Productiemedewerker", "/app/workshop", "Productieoverzicht", null, true, PortalCapabilities.DashboardRead, PortalCapabilities.JobsReadAll, PortalCapabilities.JobsUpdateAll),
                Role("profieloperator", "Profieloperator", "/app/workshop/profile-machine", "Mijn wachtrij", "profile-machine", false, PortalCapabilities.DashboardRead, PortalCapabilities.JobsReadAll, "jobs.update.profile-machine"),
                Role("cncoperator", "Plaat-CNC-operator", "/app/workshop/sheet-cnc", "Mijn wachtrij", "sheet-cnc", false, PortalCapabilities.DashboardRead, PortalCapabilities.JobsReadAll, "jobs.update.sheet-cnc"),
                Role("printoperator", "3D-printoperator", "/app/workshop/3d-print", "Mijn wachtrij", "3d-print", false, PortalCapabilities.DashboardRead, PortalCapabilities.JobsReadAll, "jobs.update.3d-print"),
                Role("assemblage", "Assemblagemedewerker", "/app/workshop/assembly", "Mijn wachtrij", "assembly", false, PortalCapabilities.DashboardRead, PortalCapabilities.JobsReadAll, "jobs.update.assembly"),
                Role("klant", "Klant", "/app/customer", "Mijn orders", null, true, PortalCapabilities.CustomerProjectRead)
            };
        }

        public List<PortalRoleDefinition> List()
        {
            return roles.Select(Clone).ToList();
        }

        public List<PortalRoleDefinition> ListSimulatorChoices(string currentRoleId)
        {
            return roles
                .Where(role => role.IsSimulatorVisible || string.Equals(role.RoleId, currentRoleId, StringComparison.OrdinalIgnoreCase))
                .Select(Clone)
                .ToList();
        }

        public PortalRoleDefinition GetRequired(string roleId)
        {
            var role = roles.FirstOrDefault(value => string.Equals(value.RoleId, roleId, StringComparison.OrdinalIgnoreCase));
            if (role == null) throw new InvalidOperationException("Onbekende portalsimulatie-rol: " + roleId);
            return Clone(role);
        }

        public static bool Has(PortalActorContext actor, string capability)
        {
            return actor != null && actor.Capabilities.Any(value => string.Equals(value, capability, StringComparison.OrdinalIgnoreCase));
        }

        public static void Ensure(PortalActorContext actor, string capability)
        {
            if (!Has(actor, capability)) throw new PortalAccessDeniedException("Actie niet toegestaan voor rol " + (actor == null ? "onbekend" : actor.RoleLabel) + ": " + capability);
        }

        public static void EnsureJobUpdate(PortalActorContext actor, string areaId)
        {
            if (Has(actor, PortalCapabilities.JobsUpdateAll) || Has(actor, "jobs.update." + (areaId ?? ""))) return;
            throw new PortalAccessDeniedException("Werkplaatstaak niet wijzigbaar voor rol " + (actor == null ? "onbekend" : actor.RoleLabel) + ".");
        }

        private static PortalRoleDefinition Role(string id, string label, string homeRoute, string homeLabel, string defaultWorkAreaId, bool isSimulatorVisible, params string[] capabilities)
        {
            return new PortalRoleDefinition { RoleId = id, Label = label, HomeRoute = homeRoute, HomeLabel = homeLabel, DefaultWorkAreaId = defaultWorkAreaId, IsSimulatorVisible = isSimulatorVisible, Capabilities = capabilities.ToList() };
        }

        private static PortalRoleDefinition Clone(PortalRoleDefinition source)
        {
            return new PortalRoleDefinition { RoleId = source.RoleId, Label = source.Label, HomeRoute = source.HomeRoute, HomeLabel = source.HomeLabel, DefaultWorkAreaId = source.DefaultWorkAreaId, IsSimulatorVisible = source.IsSimulatorVisible, Capabilities = new List<string>(source.Capabilities) };
        }
    }

    public sealed class PortalActorContextResolver
    {
        private readonly PortalRolePolicy roles;
        private readonly PortalSiteCatalog sites;

        public PortalActorContextResolver(PortalRolePolicy roles, PortalSiteCatalog sites)
        {
            this.roles = roles ?? new PortalRolePolicy();
            this.sites = sites ?? new PortalSiteCatalog();
        }

        public PortalActorContext Resolve(IDictionary<string, string> headers)
        {
            var roleId = Header(headers, "X-SW-Test-Role", "beheerder");
            var siteId = Header(headers, "X-SW-Test-Site", "internal");
            var role = roles.GetRequired(roleId);
            sites.GetRequired(siteId);
            var organization = Header(headers, "X-SW-Test-Organization", roleId == "klant" ? "demo-customer" : "internal");
            return new PortalActorContext
            {
                UserId = Header(headers, "X-SW-Test-User", "simulated-" + role.RoleId),
                OrganizationId = organization,
                SiteId = siteId,
                RoleId = role.RoleId,
                RoleLabel = role.Label,
                HomeRoute = role.HomeRoute,
                HomeLabel = role.HomeLabel,
                DefaultWorkAreaId = role.DefaultWorkAreaId,
                IsSimulated = true,
                Capabilities = new List<string>(role.Capabilities)
            };
        }

        private static string Header(IDictionary<string, string> headers, string name, string fallback)
        {
            string value;
            return headers != null && headers.TryGetValue(name, out value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : fallback;
        }
    }
}
