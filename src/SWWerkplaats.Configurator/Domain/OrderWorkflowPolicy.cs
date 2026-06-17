using System;

namespace SWWerkplaats.Configurator.Domain
{
    public enum OrderWorkflowRole
    {
        System,
        Werkvoorbereider,
        Uitvoerder
    }

    public static class OrderWorkflowPolicy
    {
        public static bool CanTransition(string fromStatus, string toStatus, OrderWorkflowRole role)
        {
            fromStatus = Normalize(fromStatus);
            toStatus = Normalize(toStatus);

            if (string.Equals(fromStatus, toStatus, StringComparison.OrdinalIgnoreCase)) return true;

            if (role == OrderWorkflowRole.System)
            {
                return IsTransition(fromStatus, toStatus, OrderWorkflowStatus.Nieuw, OrderWorkflowStatus.TeControleren);
            }

            if (role == OrderWorkflowRole.Werkvoorbereider)
            {
                return IsTransition(fromStatus, toStatus, OrderWorkflowStatus.TeControleren, OrderWorkflowStatus.Goedgekeurd)
                    || IsTransition(fromStatus, toStatus, OrderWorkflowStatus.TeControleren, OrderWorkflowStatus.InFreeswachtrij)
                    || IsTransition(fromStatus, toStatus, OrderWorkflowStatus.Goedgekeurd, OrderWorkflowStatus.InFreeswachtrij);
            }

            if (role == OrderWorkflowRole.Uitvoerder)
            {
                return IsTransition(fromStatus, toStatus, OrderWorkflowStatus.InFreeswachtrij, OrderWorkflowStatus.InProductie)
                    || IsTransition(fromStatus, toStatus, OrderWorkflowStatus.InProductie, OrderWorkflowStatus.Gereed);
            }

            return false;
        }

        public static void EnsureCanTransition(string fromStatus, string toStatus, OrderWorkflowRole role)
        {
            if (CanTransition(fromStatus, toStatus, role)) return;
            throw new InvalidOperationException("Statusovergang niet toegestaan voor " + role + ": " + Normalize(fromStatus) + " -> " + Normalize(toStatus));
        }

        public static bool IsKnownStatus(string status)
        {
            status = Normalize(status);
            foreach (var known in OrderWorkflowStatus.All())
            {
                if (string.Equals(status, known, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        private static bool IsTransition(string fromStatus, string toStatus, string expectedFrom, string expectedTo)
        {
            return string.Equals(fromStatus, expectedFrom, StringComparison.OrdinalIgnoreCase)
                && string.Equals(toStatus, expectedTo, StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string status)
        {
            return string.IsNullOrWhiteSpace(status) ? OrderWorkflowStatus.Nieuw : status.Trim();
        }
    }
}
