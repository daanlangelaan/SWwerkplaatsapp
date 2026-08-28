using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    /// <summary>Assigns stable, model-local identities before contacts are derived.</summary>
    public sealed class ProfileMemberIdentityService
    {
        public void Assign(WorkbenchModel model)
        {
            if (model == null) return;
            var used = new HashSet<string>(model.AssemblyPlacements
                .Where(p => !string.IsNullOrWhiteSpace(p.MemberId))
                .Select(p => p.MemberId), StringComparer.OrdinalIgnoreCase);
            var counters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var placement in model.AssemblyPlacements.Where(p => p.Kind == AssemblyComponentKind.Profile))
            {
                if (!string.IsNullOrWhiteSpace(placement.MemberId)) continue;
                var basis = StableId(placement.PartName);
                int number;
                counters.TryGetValue(basis, out number);
                do { number++; placement.MemberId = number == 1 ? basis : basis + "-" + number; }
                while (!used.Add(placement.MemberId));
                counters[basis] = number;
            }
        }

        private static string StableId(string value)
        {
            var builder = new StringBuilder();
            foreach (var character in (value ?? "profile").ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(character)) builder.Append(character);
                else if (builder.Length > 0 && builder[builder.Length - 1] != '-') builder.Append('-');
            }
            return builder.ToString().Trim('-');
        }
    }
}
