using System;
using System.Collections.Generic;

namespace RandomLoadout
{
    internal sealed class PickupAliasRegistry
    {
        private readonly Dictionary<string, int> _pickupIdsByAlias;

        private PickupAliasRegistry(PickupAliasEntry[] entries, Dictionary<string, int> pickupIdsByAlias)
        {
            Entries = entries ?? new PickupAliasEntry[0];
            _pickupIdsByAlias = pickupIdsByAlias ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        public static PickupAliasRegistry Empty
        {
            get { return new PickupAliasRegistry(new PickupAliasEntry[0], new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)); }
        }

        public PickupAliasEntry[] Entries { get; private set; }

        public int Count
        {
            get { return Entries.Length; }
        }

        public bool TryResolve(string alias, out int pickupId)
        {
            pickupId = 0;
            string normalizedAlias = NormalizeAlias(alias);
            if (string.IsNullOrEmpty(normalizedAlias))
            {
                return false;
            }

            return _pickupIdsByAlias.TryGetValue(normalizedAlias, out pickupId);
        }

        public static PickupAliasRegistry Create(
            AliasEntryModel[] fileEntries,
            IList<string> warnings,
            Func<int, bool> isSupportedPickupId)
        {
            Dictionary<string, int> pickupIdsByAlias = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            List<PickupAliasEntry> entries = new List<PickupAliasEntry>();
            AliasEntryModel[] rawEntries = fileEntries ?? new AliasEntryModel[0];

            for (int index = 0; index < rawEntries.Length; index++)
            {
                AliasEntryModel rawEntry = rawEntries[index];
                if (rawEntry == null)
                {
                    AddWarning(warnings, "Skipped alias entry #" + (index + 1) + " because it was null.");
                    continue;
                }

                string normalizedAlias = NormalizeAlias(rawEntry.Alias);
                if (string.IsNullOrEmpty(normalizedAlias))
                {
                    AddWarning(warnings, "Skipped alias entry #" + (index + 1) + " because the alias was empty.");
                    continue;
                }

                if (IsPureInteger(normalizedAlias))
                {
                    AddWarning(warnings, "Skipped alias '" + normalizedAlias + "' because pure numeric aliases are not allowed.");
                    continue;
                }

                if (isSupportedPickupId != null && !isSupportedPickupId(rawEntry.Id))
                {
                    AddWarning(
                        warnings,
                        "Skipped alias '" + normalizedAlias + "' because pickup ID '" + rawEntry.Id + "' was not a supported grantable pickup.");
                    continue;
                }

                if (pickupIdsByAlias.ContainsKey(normalizedAlias))
                {
                    AddWarning(warnings, "Skipped alias '" + normalizedAlias + "' because it was already defined.");
                    continue;
                }

                pickupIdsByAlias.Add(normalizedAlias, rawEntry.Id);
                entries.Add(new PickupAliasEntry(normalizedAlias, rawEntry.Id));
            }

            return new PickupAliasRegistry(entries.ToArray(), pickupIdsByAlias);
        }

        private static void AddWarning(IList<string> warnings, string message)
        {
            if (warnings != null)
            {
                warnings.Add(message);
            }
        }

        private static string NormalizeAlias(string alias)
        {
            return alias != null ? alias.Trim() : string.Empty;
        }

        private static bool IsPureInteger(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            int parsedValue;
            return int.TryParse(value, out parsedValue);
        }
    }
}
